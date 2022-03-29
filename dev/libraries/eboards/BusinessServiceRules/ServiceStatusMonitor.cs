using System;
using System.Collections;
using System.Threading;

using LibCore;
using Network;

namespace BusinessServiceRules
{
	/// <summary>
	/// A ServiceStatusMonitor attaches to a business service user in the model and watches dependent
	/// servers and applications. If any go down then the business service is also marked as
	/// down.
	/// </summary>
	public class ServiceStatusMonitor
	{
		public Mutex mutex = new Mutex();
		/// <summary>
		/// Store the business service node in the model that is being monitored.
		/// </summary>
		protected Node monitoredEntity;
		/// <summary>
		/// Keep a list of nodes that we have attached to so that we can detach if required.
		/// </summary>
		protected Hashtable attachedNodes = new Hashtable();

		protected Hashtable nodeToRequiredNodes = new Hashtable();
		/// <summary>
		/// Store a Hashtable of nodes that we know are already down (mapped to ???).
		/// This allows us to register changes on the business service that do not alter
		/// the up/down status but do register that the reasons for the service being down
		/// have changed. E.G. one server out of two required has come back up or a third
		/// dependent application ahs gone down. This allows the facilitator to query the
		/// EBoard as to why something has happened and for changes in reasons to be
		/// entered into reports (via the log file).
		/// </summary>
		protected Hashtable downdedNodes = new Hashtable();

		protected ServiceDownCounter _sdc;
		/// <summary>
		/// The ServiceStatusMonitor constructor.
		/// </summary>
		/// <param name="entity">The business service in the model that should be monitored.</param>
		/// /// <param name="sdc"></param>
		public ServiceStatusMonitor(Node entity, ServiceDownCounter sdc)
		{
			_sdc = sdc;
			monitoredEntity = entity;
			BuildNotificationTree();
			//
			// A mirror might appear or be removed.
			// Therefore we have to watch all our backlink connections to see if things change.
			//
			monitoredEntity.BackLinkAdded += monitoredEntity_BackLinkAdded;
			monitoredEntity.BackLinkRemoved += monitoredEntity_BackLinkRemoved;
			//
		}

		/// <summary>
		/// An explicit Dispose method that should be called before releasing this class so that
		/// it can detach from the model correctly.
		/// </summary>
		public void Dispose()
		{
			WipeAttachedNodes();

			if (monitoredEntity != null)
			{
				monitoredEntity.BackLinkAdded -= monitoredEntity_BackLinkAdded;
				monitoredEntity.BackLinkRemoved -= monitoredEntity_BackLinkRemoved;
				monitoredEntity = null;
			}
		}

		public Node getMonitoredItem()
		{
			return monitoredEntity;
		}

		protected void BuildNotificationTree()
		{
			this.WipeAttachedNodes();
			BuildNotificationTree(monitoredEntity);
			//
			ApplyStateToMonitoredEntity();
		}
		/// <summary>
		/// Runs through all the links attached to the business service node and then climbs the
		/// network tree watching each signifanct node as it goes. Calls ClimbBuildTree().
		/// </summary>
		protected void BuildNotificationTree(Node n)
		{
			foreach(LinkNode l in n.BackLinks)
			{
				// Don't walk up any back links that are dependsOn type...
				if(l.GetAttribute("type") != "dependsOn")
				{
					// Walk up the parent tree.
					ClimbBuildTree( (Node) l);
				}
			}
		}
		/// <summary>
		/// Climbs the tree attaching event notifiers to each significant node to watch.
		/// </summary>
		/// <param name="n"></param>
		protected void ClimbBuildTree(Node n)
		{
			mutex.WaitOne();

			try
			{
				while(null != n)
				{
					// If we have any dependsOn links then make sure we follow those for notification as well.
					foreach(Node deNode in n)
					{
						if("dependsOn" == deNode.GetAttribute("type"))
						{
							Node requiredNodeAvailable = n.Tree.GetNamedNode(deNode.GetAttribute("item"));
							ClimbBuildTree( requiredNodeAvailable );
							// Add this required dependency into a hashtable that can be looked up
							// when checking the system is up tree climb...
							if(nodeToRequiredNodes.ContainsKey(n))
							{
								ArrayList requiredUpNodes = (ArrayList) nodeToRequiredNodes[n];

								// : fix for 4237 (very slow adding/removing mirrors):
								// only put each required node into the list once!
								// This vastly improves things, but presumably we are somewhere
								// failing to remove a watch.  Thanks to this fix, we now leave
								// only one stray watch rather than dozens or hundreds.
								if (requiredUpNodes.IndexOf(requiredNodeAvailable) == -1)
								{
									requiredUpNodes.Add( requiredNodeAvailable );
								}
							}
							else
							{
								ArrayList requiredUpNodes = new ArrayList();
								requiredUpNodes.Add( requiredNodeAvailable );
								nodeToRequiredNodes.Add(n,requiredUpNodes);
							}
						}
					}
					//
					if(!attachedNodes.ContainsKey(n))
					{
						// Do NOT watch biz_services..
						if(n.GetAttribute("type") == "biz_service")
						{
							return;
						}
						// We have to watch any changes in state...
						n.PreAttributesChanged += n_PreAttributesChanged;
						n.AttributesChanged += n_AttributesChanged;
						n.ParentChanged += n_ParentChanged;
						n.ChildAdded += n_ChildAdded;
						n.ChildRemoved += n_ChildRemoved;
						// We have to watch if our parental status changes...
						// TODO : We can't seem to be able to do this!
						// TODO : We should have an event ParentChanged that then causes us to
						// re-evaluate our dependency tree.
						attachedNodes.Add(n,1);
					}

					BuildNotificationTree(n);

					n = n.Parent;
				}
			}
			finally
			{
				mutex.ReleaseMutex();
			}
		}

		protected void WipeAttachedNodes()
		{
			foreach(Node n in attachedNodes.Keys)
			{
				n.PreAttributesChanged -= n_PreAttributesChanged;
				n.AttributesChanged -= n_AttributesChanged;
				n.ParentChanged -= n_ParentChanged;
				n.ChildAdded -= n_ChildAdded;
				n.ChildRemoved -= n_ChildRemoved;
			}
			attachedNodes.Clear();
		}

		/// <summary>
		/// Method that climbs the tree from the business service assessing what has occured to
		/// the dependency tree and duly marks the monitored model noe accordingly.
		/// </summary>
		/*
		protected void ApplyStateToMonitoredEntity()
		{
			WalkDependencyGraph();
		}*/

		/// <summary>
		/// Store the maximum time any dependent node is rebooting as we climb the
		/// depencency tree.
		/// </summary>
		protected int rebootingSecs;
		/// <summary>
		/// Store the minimum time left on work arounds on the dependency tree.
		/// </summary>
		protected int workAroundSecs;
		protected int goingDownSecs;

		protected int taskRemainingSecs;

		/// <summary>
		/// A constant to set the initial working around time to the maximum value
		/// possible.
		/// </summary>
		static protected int MAX_WORKAROUND_SECS = int.MaxValue;
		/// <summary>
		/// Store a human readable description of why the service has failed.
		/// </summary>
		protected string reasonDown = "";
		/// <summary>
		/// Store a human readable description of why we think that the service is actually up.
		/// </summary>
		protected string reasonUp = "";
		/// <summary>
		/// Store a flag when we climb the tree to show that even though the business service
		/// may not have come back up the reason it is down has altered significantly so that
		/// the reason attribute attatched to the business service should be changed.
		/// </summary>
		protected bool majorReasonChanged;
		/// <summary>
		/// Method that climbs the tree from the business service assessing what has occured to
		/// the dependency tree and duly marks the monitored model node accordingly.
		/// </summary>
		/// <returns>True if service is up otherwise false.</returns>
		protected bool ApplyStateToMonitoredEntity()
		{
			mutex.WaitOne();

			bool gotToRoot = false;

			try
			{
				reasonUp = "";
				reasonDown = "";
				majorReasonChanged = false;
				//reason = "Status For " + monitoredEntity.GetAttribute("name") + "\r\n";

				// We want to find the highest rebooting secs so set initially to 0.
				rebootingSecs = 0;
				// Likewise the highest task remaining secs.
				taskRemainingSecs = 0;
				// We want to find the lowest workAroundSecs so set to a maximum value.
				workAroundSecs = MAX_WORKAROUND_SECS;
				goingDownSecs = MAX_WORKAROUND_SECS;
				bool onlyThroughMirror = true;
				bool onlyThroughVirtualMirror = true;
				bool haveSomethingFixable = false;
				bool haveSomethingWeCanWorkAround = false;
				bool haveReboot = false;
				string Incidents = "";

				bool haveMirror = false;
				bool haveVirtualMirror = false;

				string _name = monitoredEntity.GetAttribute("name");

				bool up_instore = true;
				bool up_online = true;

				bool denial_of_service = false;
				bool security_flaw = false;
				bool compliance_incident = false;
				bool thermal = false;
				bool power = false;

				// Initially don't go to the parent as that is always guaranteed
				// to reach the NodeTree Root node. Go via the first set of back links
				// and then up the tree.
				ArrayList serviceConnections = monitoredEntity.BackLinks;

				//If this is an unconnected BSU, we can probably count as being up!
				//So set the gotToRoot to true;
				//This was added for BizCon with revision 20271
				//This causes a problem with DC and AOSE (only apps with single unattached lozenges)
				//so switch it off based on the game type in the skin 
				bool use_BSU_Root = true;

				string gametype = CoreUtils.SkinningDefs.TheInstance.GetData("gametype");
				if (string.IsNullOrEmpty(gametype) == false)
				{
					gametype = gametype.ToLower();
					if ((gametype.Equals("hpdc")) | (gametype.Equals("aose")) | (gametype.Equals("hp_rtr_sm")))
					{
						use_BSU_Root = false;
					}
				}

				if (use_BSU_Root)
				{
					if (serviceConnections.Count == 0)
					{
						gotToRoot = true;
					}
				}

				foreach(LinkNode first_link in serviceConnections)
				{
					// 27-02-2007 - biz_service_user is now a link beneath biz_service
					// which in turn jumps up to the supporting App/network. Therefore,
					// we now need to jump up another back link level before we look at
					// our multiple dependency connections.
					//

					//
					// Walk up the parent tree.
					// Check if it reaches the root by going through a mirror.
					//
					bool throughMirror = false;
					bool throughVirtualMirror = false;
					bool fixable = false;
					bool haveWA = false;
					bool _haveReboot = false;
					//
					bool _up_instore = true;
					bool _up_online = true;
					//
					string incidentID = "";

					bool _denial_of_service = false;
					bool _security_flaw = false;
					bool _compliance_incident = false;
					bool _thermal = false;
					bool _power = false;

					bool _gotToRoot = ClimbTree( (Node) first_link , out throughMirror, out fixable,
						out haveWA, out _haveReboot, out incidentID, out _up_instore, out _up_online,
						out _denial_of_service, out _security_flaw, out _compliance_incident, out _thermal, out _power, out throughVirtualMirror);

					if(_gotToRoot)
					{
						gotToRoot = true;
						// Fix for bug 3404 : If we have a WA on our main server and we have a mirror then
						// we are still counted as up by mirror. This is the "plumber" metaphor for SLA also
						// being upplied to the counter for how long you have been dependent on the mirror.
						if(onlyThroughMirror && !throughMirror)// && !haveWA)
						{
							// Aaargh, if we are in WA and we don't have a mirror then we never set this.
							onlyThroughMirror = false;
						}
						//
						if (onlyThroughVirtualMirror && ! throughVirtualMirror)
						{
							onlyThroughVirtualMirror = false;
						}
					}
					//
					if(!_up_instore)
					{
						up_instore = false;
					}
					//
					if(!_up_online)
					{
						up_online = false;
					}
					//
					if(_denial_of_service)
					{
						denial_of_service = true;
					}
					// security_flaw
					if (_security_flaw)
					{
						security_flaw = true;
					}

					if (_compliance_incident)
					{
						compliance_incident = true;
					}

					if(_thermal)
					{
						thermal = true;
					}
					if (_power)
					{
						power = true;
					}
					//
					if(throughMirror)
					{
						haveMirror = true;
					}
					if (throughVirtualMirror)
					{
						haveVirtualMirror = true;
					}
					//
					if(fixable)
					{
						haveSomethingFixable = true;
					}
					//
					if(haveWA)
					{
						haveSomethingWeCanWorkAround = true;
					}
					//
					if(_haveReboot)
					{
						haveReboot = true;
					}

					//
					if(incidentID != string.Empty)
					{
						if(Incidents == string.Empty)
						{
							Incidents = incidentID;
						}
						else
						{
							string[] each_incident = Incidents.Split(',');
							bool found = false;
							for(int i=0; i<each_incident.Length; ++i)
							{
								if(each_incident[i] == incidentID) found = true;
							}
							if(!found)
							{
								Incidents += "," + incidentID;
							}
						}
					}
				}


				ArrayList atts = new ArrayList();
				AttributeValuePair.AddIfNotEqual(monitoredEntity,atts, "task_time_remaining", taskRemainingSecs);


				//
				if(denial_of_service)
				{
					AttributeValuePair.AddIfNotEqual(monitoredEntity,atts, "denial_of_service", "true");
				}
				else
				{
					AttributeValuePair.AddIfNotEqual(monitoredEntity,atts, "denial_of_service", "false");
				}
				// security_flaw
				if (security_flaw)
				{
					AttributeValuePair.AddIfNotEqual(monitoredEntity, atts, "security_flaw", "true");
				}
				else
				{
					AttributeValuePair.AddIfNotEqual(monitoredEntity, atts, "security_flaw", "false");
				}

				if (compliance_incident)
				{
					AttributeValuePair.AddIfNotEqual(monitoredEntity, atts, "compliance_incident", "true");
				}
				else
				{
					AttributeValuePair.AddIfNotEqual(monitoredEntity, atts, "compliance_incident", "false");
				}
				
				if(thermal)
				{
					AttributeValuePair.AddIfNotEqual(monitoredEntity, atts, "thermal", "true");
				}
				else
				{
					AttributeValuePair.AddIfNotEqual(monitoredEntity, atts, "thermal", "false");
				}

				if (power)
				{
					AttributeValuePair.AddIfNotEqual(monitoredEntity, atts, "nopower", "true");
				}
				else
				{
					AttributeValuePair.AddIfNotEqual(monitoredEntity, atts, "nopower", "false");
				}
				
				//
				// Case 4332:   CA: access denied incidents should not be prevented by mirror.
				//
				// If we have a denial_of_service incident and we gotToRoot but we did it onlyThroughMirror
				// then claim that we did not get to root so that we are indeed down. This allows players to
				// see that they do indeed have a denial_of_service incident and act accordingly.
				if(denial_of_service && gotToRoot && onlyThroughMirror)
				{					
					gotToRoot = false;
				}
				// security_flaws behave the same way.
				if (security_flaw && gotToRoot && onlyThroughMirror)
				{
					security_flaw = false;
				}

				if (compliance_incident && gotToRoot && onlyThroughMirror)
				{
					compliance_incident = false;
				}

				//
				if(gotToRoot)
				{
					// If we only managed it by going through a mirror say so...
					if(haveSomethingFixable || haveSomethingWeCanWorkAround || haveReboot)
					{
						// If we have something fixable or something that can be worked around and
						// we are still up this means that we can only be up by being mirrored in
						// some way.
						if( haveMirror && ! monitoredEntity.GetBooleanAttribute("upByMirror", false))
						{
							atts.Add( new AttributeValuePair( "upByMirror","true" ) );
							reasonUp += monitoredEntity.GetAttribute("name") + " Is Only Up Because It Is Mirrored.|";
						}
							// 22-08-2007 : wants it to go Amber if either of a mirrored entity is down.
							// Therefore, easiest to just lie and say that it is up only by mirror so that
							// the display and ?????
					}
					else
					{
						if(monitoredEntity.GetBooleanAttribute("upByMirror", false))
						{
							atts.Add( new AttributeValuePair( "upByMirror","false" ) );
						}
					}

					if (haveVirtualMirror && monitoredEntity.GetBooleanAttribute("up", false))
					{
						if (! monitoredEntity.GetBooleanAttribute("virtualmirrorinuse", false))
						{
							atts.Add(new AttributeValuePair ("virtualmirrorinuse", "true"));
							reasonUp += monitoredEntity.GetAttribute("name") + " Is Only Up Because It Is Mirrored.|";
						}
					}
					else
					{
						if (monitoredEntity.GetBooleanAttribute("virtualmirrorinuse", false))
						{
							atts.Add(new AttributeValuePair ("virtualmirrorinuse", "false"));
						}
					}

					//
					if(onlyThroughMirror)
					{
						if(! monitoredEntity.GetBooleanAttribute("upOnlyByMirror", false))
						{
							atts.Add( new AttributeValuePair( "upOnlyByMirror","true" ) );
							reasonUp += monitoredEntity.GetAttribute("name") + " Is Only Up Because The Mirror Stayed Up.\r\n";
						}
						// 30-07-2007 : We need to count how long this has been down as well.
						_sdc.AddNode(monitoredEntity);

					}
					else
					{
						// 3404 - If we only managed to avoid remying on our mirror because we were in
						// WA then don't come out of mirror status!
						if(false == haveSomethingWeCanWorkAround)
						{
							if(monitoredEntity.GetBooleanAttribute("upOnlyByMirror", false))
							{
								atts.Add( new AttributeValuePair( "upOnlyByMirror","false" ) );
							}
						}
					}

					if (onlyThroughVirtualMirror)
					{
						if (! monitoredEntity.GetBooleanAttribute("virtualmirrorinuse", false))
						{
							atts.Add(new AttributeValuePair ("virtualmirrorinuse", "true"));
							reasonUp += monitoredEntity.GetAttribute("name") + " Is Only Up Because The Mirror Stayed Up.\r\n";
						}
						_sdc.AddNode(monitoredEntity);
					}
					else
					{
						if (! haveSomethingWeCanWorkAround)
						{
							if (monitoredEntity.GetBooleanAttribute("virtualmirrorinuse", false))
							{
								atts.Add(new AttributeValuePair ("virtualmirrorinuse", "false"));
							}
						}
					}
				}
				else
				{
					atts.Add( new AttributeValuePair( "upByMirror","false" ) );
					atts.Add( new AttributeValuePair( "upOnlyByMirror","false" ) );
					atts.Add(new AttributeValuePair ("virtualmirrorinuse","false"));
				}
				// If we have something fixable then say so...
				if(haveSomethingFixable)
				{
					AttributeValuePair.AddIfNotEqual(monitoredEntity,atts, "fixable", "true");
				}
				else
				{
					AttributeValuePair.AddIfNotEqual(monitoredEntity,atts, "fixable", "false");
				}
				// haveSomethingWeCanWorkAround
				if(haveSomethingWeCanWorkAround)
				{
					AttributeValuePair.AddIfNotEqual(monitoredEntity,atts, "canWorkAround", "true");
				}
				else
				{
					AttributeValuePair.AddIfNotEqual(monitoredEntity,atts, "canWorkAround", "false");
				}
				// If we have stuff rebooting state the longest time until they have all rebooted.
				AttributeValuePair.AddIfNotEqual(monitoredEntity,atts, "rebootingForSecs", rebootingSecs);

				// If we have stuff in workaround state the shortest time until at least one
				// of the workarounds fail.
				if(MAX_WORKAROUND_SECS == workAroundSecs)
				{
					// No workarounds in operation!
					AttributeValuePair.AddIfNotEqual(monitoredEntity,atts, "workingAround", 0);
					if (gotToRoot && !(onlyThroughMirror || onlyThroughVirtualMirror))
					{
						// fix for Case 2585:   error when fix incident from workaround
						//Yes, Clear the downforsecs flag
						_sdc.RemoveNode(monitoredEntity,true);
					}
				}
				else
				{
					// Tell the biz_service how long until a workaround fails.
					atts.Add( new AttributeValuePair( "workingAround", workAroundSecs ) );
				}

				if(MAX_WORKAROUND_SECS == goingDownSecs)
				{
					AttributeValuePair.AddIfNotEqual(monitoredEntity,atts, "goingDownInSecs", "");
				}
				else
				{
					AttributeValuePair.AddIfNotEqual(monitoredEntity,atts, "goingDownInSecs", goingDownSecs);
				}

				if(gotToRoot)
				{
					if(! monitoredEntity.GetBooleanAttribute("up", false))
					{
						// Case 2537:   SLA breaches should continue after a workaround. 
						// We only stop counting up our failed time if we have truly been fixed and are not in workaround...
						if(MAX_WORKAROUND_SECS != workAroundSecs)
						{
							// Do not remove fail count.
							//_sdc.RemoveNode(monitoredEntity);
						}
						else
						{
							//Yes, Clear the downforsecs flag
							_sdc.RemoveNode(monitoredEntity,true);
						}
						atts.Add( new AttributeValuePair( "up","true" ) );
						//clear the incident number
						AttributeValuePair.AddIfNotEqual(monitoredEntity,atts, "incident_id", "");
//						reason = "Status For " + monitoredEntity.GetAttribute("name") + "|" + reason;
						AttributeValuePair.AddIfNotEqual(monitoredEntity,atts, "fixable", "");
						AttributeValuePair.AddIfNotEqual(monitoredEntity,atts, "workingAround", 0);
						AttributeValuePair.AddIfNotEqual(monitoredEntity,atts, "canworkaround", "");
					}
					else
					{
						// double check to see if we have stopped working around...
						if (0 == monitoredEntity.GetIntAttribute("workingAround",0) && !(onlyThroughMirror || onlyThroughVirtualMirror))
						{
							// 3404 - Do not remove if we have a work around!
							if(false == haveSomethingWeCanWorkAround)
							{
								_sdc.RemoveNode(monitoredEntity,false);
							}
						}
					}

					// We may have to wipe the old reason...
					AttributeValuePair.AddIfNotEqual(monitoredEntity,atts, "reasondown", "");
				}
				else
				{
					// We are not up but we may already be marked as not being up...
					if(! monitoredEntity.GetBooleanAttribute("up", false))
					{
						// We may still be down but there's been a major reason change for us
						// still being down. Theerfore update the reason attribute.
						if(majorReasonChanged)
						{
							AttributeValuePair.AddIfNotEqual(monitoredEntity,atts, "up", "false");
							AttributeValuePair.AddIfNotEqual(monitoredEntity,atts, "reasondown", reasonDown);
						}
					}
					else
					{
						// Don't countdown on the service being down if we are being rebooted!
						string rbts = monitoredEntity.GetAttribute("rebootingForSecs");
						if( ("" == rbts) || ("0" == rbts) )
						{
							_sdc.AddNode(monitoredEntity);
						}
						//
						AttributeValuePair.AddIfNotEqual(monitoredEntity,atts, "up", "false");
						AttributeValuePair.AddIfNotEqual(monitoredEntity,atts, "reasondown", reasonDown);
					}
				}

				string str_up_online = monitoredEntity.GetAttribute("up_online").ToLower();
				string str_up_instore = monitoredEntity.GetAttribute("up_instore").ToLower();

				if(up_online && (str_up_online!="true"))
				{
					atts.Add( new AttributeValuePair( "up_online", "true" ) );
				}
				else if(!up_online && (str_up_online!="false"))
				{
					atts.Add( new AttributeValuePair( "up_online", "false" ) );
				}

				if(up_instore && (str_up_instore!="true"))
				{
					atts.Add( new AttributeValuePair( "up_instore", "true" ) );
				}
				else if(!up_instore && (str_up_instore!="false"))
				{
					atts.Add( new AttributeValuePair( "up_instore", "false" ) );
				}


				if(gotToRoot)
				{
					// We are up...
					AttributeValuePair.AddIfNotEqual(monitoredEntity,atts, "reasonUp", reasonUp);
				}
				else
				{
					// We are down...
					AttributeValuePair.AddIfNotEqual(monitoredEntity,atts, "reasonUp", "");
				}

				AttributeValuePair.AddIfNotEqual(monitoredEntity,atts, "incident_id", Incidents);

				if(atts.Count > 0)
				{
					monitoredEntity.SetAttributes( atts );
				}
			}
			finally
			{
				mutex.ReleaseMutex();
			}

			return gotToRoot;
		}

		/// <summary>
		/// Method that climbs a particular leaf to root branch checking on the status of any
		/// dependent nodes.
		/// </summary>
		/// <param name="n">Model node to start climbing the tree from.</param>
		/// <param name="throughMirror">Sets whether it has had to pass through a mirror.</param>
		/// <param name="fixable">Sets whether any problem hit is fixable or not.</param>
		/// <param name="haveWA">Sets whether any problem hit has a workaround applied.</param>
		/// <param name="incidentID"></param>
		protected bool ClimbTree(
			Node n,
			out bool throughMirror,
			out bool fixable,
			out bool haveWA,
			out bool haveReboot,
			out string incidentID,
			out bool up_online,
			out bool up_instore,
			out bool denial_of_service,
			out bool security_flaw,
			out bool compliance_incident,
			out bool thermal,
			out bool power,
			out bool throughVirtualMirror)
		{
			string _name = "(null)";

			if (n != null)
			{
				_name = n.GetAttribute("name");
			}

			bool okay = true;
			fixable = false;
			haveWA = false;
			haveReboot = false;
			throughMirror = false;
			throughVirtualMirror = false;
			incidentID = "";
			//
			up_online = true;
			up_instore = true;
			//
			denial_of_service = false;
			security_flaw = false;
			compliance_incident = false;
			thermal = false;
			power = false;
			//
			// 12-03-2007 : We need to check any "depends on" links and climb those trees as well.
			// Any dependsOn link must be up and will set our upByMirror if any of these are only
			// up by a mirror then we mark this tree climb as up by a mirror.
			//
			while(null != n)
			{
				// Have we found the root node?
				if(n == n.Tree.Root)
				{
					return okay; // XX
				}

				// Have we found a notInNetwork node?
				if(n.GetBooleanAttribute("notInNetwork", false))
				{
					return false; // XX
				}

				if(n.GetAttribute("type") == "biz_service")
				{
					// Biz services are not in the IT netwotk!
					return false;
				}

				if (n.GetBooleanAttribute("always_up", false))
				{
					// We are defined to be up no mattter what else is going (no need to go further
					// Use in CA SE 2 as an Appp is promoted to the cloud
					return okay; 
				}

				string __name = n.GetAttribute("name");
				reasonUp += "Checking status of " + __name + "\r\n";

				// First check our dependsOn links...
				if(nodeToRequiredNodes.ContainsKey(n))
				{
					ArrayList requiredUpNodes = (ArrayList) nodeToRequiredNodes[n];
					foreach(Node definitelyRequiredToBeUp in requiredUpNodes)
					{
						string name = "(null)";

						if (definitelyRequiredToBeUp != null)
						{
							name = definitelyRequiredToBeUp.GetAttribute("name");
						}
						//
						reasonUp += __name + " depends on " + name + " being up.\r\n";
						//
						bool _fixable = false;
						bool _haveWA = false;
						bool _haveReboot = false;
						bool _throughMirror = false;
						bool _throughVirtualMirror = false;
						string _incidentID = "";
						//
						bool _up_online = true;
						bool _up_instore = true;
						//
						bool _denial_of_service = false;
						bool _security_flaw = false;
						bool _compliance_incident = false;
						bool _thermal = false;
						bool _power = false;
						//
						bool _okay = ClimbTree(definitelyRequiredToBeUp, out _throughMirror, out _fixable,
							out _haveWA, out _haveReboot, out _incidentID, out _up_online, out _up_instore, 
							out _denial_of_service, out _security_flaw, out _compliance_incident, out _thermal, out _power, out _throughVirtualMirror);
						//
						if(!_okay) okay = false; // XX
						if (_fixable)
						{
							fixable = true;
						}
						if(_haveWA)
						{
							haveWA = true;
						}
						if(_haveReboot) haveReboot = true;
						if(_throughMirror)
						{
							throughMirror = true;
						}
						if (_throughVirtualMirror)
						{
							throughVirtualMirror = true;
						}
						if (_incidentID != string.Empty)
						{
							incidentID = _incidentID;
						}
						if(_denial_of_service)
						{
							denial_of_service = true;
						}
						if (_security_flaw)
						{
							security_flaw = true;
						}
						if (_compliance_incident)
						{
							compliance_incident = true;
						}
						if (_thermal)
						{
							thermal = true;
						}
						if (_power)
						{
							 power = true;
						}
					}
				}
				// Are we going through a mirror?
				if("true" == n.GetAttribute("isMirror").ToLower())
				{
					throughMirror = true;
				}

				// Are we going through a virtual mirror?
				if (n.GetBooleanAttribute("virtualmirrorinuse", false))
				{
					throughVirtualMirror = true;
					fixable = true;
					if ((incidentID == "") && (n.GetAttribute("incident_id") != ""))
					{
						incidentID = n.GetAttribute("incident_id");
					}
				}

				// Do we have a denial of service?
				if(n.GetBooleanAttribute("denial_of_service", false))
				{
					denial_of_service = true;
				}
				// security_flaw
				if (n.GetBooleanAttribute("security_flaw", false))
				{
					security_flaw = true;
				}

				if (n.GetBooleanAttribute("compliance_incident", false))
				{
					compliance_incident = true;
				}
				
				
				// Do we have a thermal?
				if(n.GetBooleanAttribute("thermal", false))
				{
					thermal = true;
				}
				// Do we have a power problem?
				if (n.GetBooleanAttribute("nopower", false))
				{
					power = true;
				}
				
				// Has this node got a workaround applied?
				int workAround_t = n.GetIntAttribute("workingAround",0);
				if(0 != workAround_t)
				{
					// This has a workAround on it!
					//grab the incident id
					string _incidentID = n.GetAttribute("incident_id");
					if (_incidentID != string.Empty)
					{
						reasonDown += "Incident ID " + incidentID + " is active on " + __name + "\r\n";

						// : 30-4-2007 - Do not put the same incident id on twice!
						if(incidentID != string.Empty)
						{
							char[] comma = { ',' };
							string[] incidents = incidentID.Split(comma);
							bool found = false;
							for(int i=0; i<incidents.Length; ++i)
							{
								if(incidents[i] == _incidentID) found = true;
							}
							if(!found)
							{
								incidentID += "," + _incidentID;
							}
						}
						else
						{
							incidentID += _incidentID;
						}
					}

					//int curNode_workAroundSecs = CONVERT.ParseInt(workAround_t);
					if(workAround_t < workAroundSecs)
					{
						workAroundSecs = workAround_t;//curNode_workAroundSecs;
					}
					// WorkArounds are fixable so say so...
					fixable = true;
					// haveWA not being set correct (Bug 3404).
					haveWA = true;
					reasonDown += n.GetAttribute("name") + " Has A WorkAround Applied That Will Expire.|";// In " + workAround_t + " Seconds.|";
				}
				// Have we hit a rebooting node?
				if(n.GetIntAttribute("rebootingForSecs",0) > 0)
				{
					haveReboot = true;
				}
				// Have we hit a downed node?
				if(! n.GetBooleanAttribute("up", true))
				{
					reasonUp += __name + " Is down.\r\n";

					//grab the incident id
					string _incidentID = n.GetAttribute("incident_id");
					if (_incidentID != string.Empty)
					{
						reasonDown += "Incident ID " + incidentID + " is active on " + __name + "\r\n";

						// : 30-4-2007 - Do not put the same incident id on twice!
						if(incidentID != string.Empty)
						{
							char[] comma = { ',' };
							string[] incidents = incidentID.Split(comma);
							bool found = false;
							for(int i=0; i<incidents.Length; ++i)
							{
								if(incidents[i] == _incidentID) found = true;
							}
							if(!found)
							{
								incidentID += "," + _incidentID;
							}
						}
						else
						{
							incidentID += _incidentID;
						}
					}

					okay = false; // XX
					//up = false;
					// If we are not rebooting or turned off/overheating then this particular problem is fixable.
					string rebooting = n.GetAttribute("rebootingForSecs");
					bool isRebooting = ((rebooting != "") && (rebooting != "0"));
					bool isOff = n.GetBooleanAttribute("nopower", false);
					bool isHVAC = n.GetBooleanAttribute("thermal", false);

					if (isOff)
					{
						// Do we have a reason set for being down?
						string reasonAtt = n.GetAttribute("reasondown");
						if("" != reasonAtt)
						{
							reasonDown += reasonAtt + "\r\n";
						}
						reasonDown += n.GetAttribute("name") + " Has No Power.|";
					}
					else if (isRebooting)
					{
						// See if this entity has a rebooting time longer than our current rebooting time.
						int rbt = CONVERT.ParseInt(rebooting);
						if(rbt > rebootingSecs)
						{
							rebootingSecs = rbt;
						}
						// Do we have a reason set for being down?
						string reasonAtt = n.GetAttribute("reasondown");
						if("" != reasonAtt)
						{
							reasonDown += reasonAtt + "\r\n";
						}
						reasonDown += n.GetAttribute("name") + " Is Down And Rebooting.|";// Will Be Up In " + rebooting + " Seconds.|";
					}
					else
					{
						// This is fixable.
						fixable = n.GetBooleanAttribute("fixable", true)
						          && ! n.GetBooleanAttribute("is_saas", false);
						// This can be worked around.
						haveWA = n.GetBooleanAttribute("canworkaround", true)
								 && ! n.GetBooleanAttribute("is_saas", false)
								 && ! IsVirtualised(n);

						string fixableString = "Not Fixable";
						if (fixable)
						{
							fixableString = "Fixable";
						}

						string workString = "Can Not Be Worked Around";
						if (haveWA)
						{
							workString = "Can Be Worked Around";
						}

						reasonDown += n.GetAttribute("name") + " Is Down But Is " + fixableString + " Or " + workString + ".|";
					}

					//
					// If we don't have this node marked as down already then store it and
					// flag that we have to update our reason string becuse an extra dependency
					// has gone down.
					//
					if(!downdedNodes.ContainsKey(n))
					{
						// Flag that the reason has to be updated...
						downdedNodes[n] = 1;
						majorReasonChanged = true;
					}
					//
				}
				else
				{
					// This node is up...
					reasonUp += __name + " Is up.\r\n";
					//
					// If this node is marked as being known as being down by us then we should
					// flag that a major reason for being down has changed since our business
					// service may still be down but this node has come back up.
					//
					if(downdedNodes.ContainsKey(n))
					{
						downdedNodes.Remove(n);
						majorReasonChanged = true;
					}
					//
				}

				taskRemainingSecs = Math.Max(taskRemainingSecs, n.GetIntAttribute("task_time_remaining", 0));

				// Have we hit a node that is goingDown?
				string gd = n.GetAttribute("goingDownInSecs");
				if( (gd != "0") && (gd != "") )
				{
					int gdSecs = CONVERT.ParseInt(gd);
					if(gdSecs < goingDownSecs)
					{
						goingDownSecs = gdSecs;
					}

					//grab the incident id
					string _incidentID = n.GetAttribute("incident_id");
					if (_incidentID != string.Empty)
					{

						// : 30-4-2007 - Do not put the same incident id on twice!
						if(incidentID != string.Empty)
						{
							char[] comma = { ',' };
							string[] incidents = incidentID.Split(comma);
							bool found = false;
							for(int i=0; i<incidents.Length; ++i)
							{
								if(incidents[i] == _incidentID) found = true;
							}
							if(!found)
							{
								incidentID += "," + _incidentID;
							}
						}
						else
						{
							incidentID += _incidentID;
						}
					}
					// This can be fixed or worked around.
					fixable = true;
					haveWA = true;
					//
					reasonDown += n.GetAttribute("name") + " Has A Problem And Will Fail.|";
				}
				//
				reasonUp += __name + " sits on " + n.Parent.GetAttribute("name") + "\r\n";
				//
				n = n.Parent;
			}

			return okay; // XX
		}

		bool IsVirtualised (Node n)
		{
			if (n.GetBooleanAttribute("is_virtual", false))
			{
				return true;
			}

			if (n.Parent != null)
			{
				return IsVirtualised(n.Parent);
			}

			return false;
		}

		/// <summary>
		/// Event recieving method that is informed when any watched model node has an
		/// attribute changed.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="attrs"></param>
		void n_AttributesChanged(Node sender, ArrayList attrs)
		{ 
			if (attrs != null)
			{
				if (attrs.Count > 0)
				{
					bool applyState = false;

					foreach(AttributeValuePair avp in attrs)
					{
						//Extraction of the data attribute
						string name = sender.GetAttribute("name");
						string attribute = avp.Attribute;
						string newValue = avp.Value;

						//Do the work )
						// Has this entity gone down?
						if( "up" == attribute )
						{
							// Walk tree from service and see if we can still find a viable path...
							//ApplyStateToMonitoredEntity();
							applyState = true;
						}
						else if ("thermal" == attribute)
						{
							applyState = true;
						}
						else if("has_impact" == attribute)
						{
							applyState = true;
							//ApplyStateToMonitoredEntity();
						}
						else if("rebootingForSecs" == attribute)
						{
							applyState = true;
							//ApplyStateToMonitoredEntity();
						}
						else if ("task_time_remaining" == attribute)
						{
							applyState = true;
						}
						else if("workingAround" == attribute)
						{
							applyState = true;
							//ApplyStateToMonitoredEntity();
						}
						else if("incident_id" == attribute)
						{
							applyState = true;
							//ApplyStateToMonitoredEntity();
						}
							// Is entity going down?
						else if("goingDownInSecs" == attribute)
						{
							// Mark service as going down so that it knows that it is in danger.
							monitoredEntity.SetAttribute("goingDownInSecs",newValue);
							// Say that we have something fixable or can workaround...
							if( (newValue=="") || (newValue=="0") )
							{
								// Have to climb tree to check!
								applyState = true;
								//ApplyStateToMonitoredEntity();
							}
							else
							{
								ArrayList atts = new ArrayList();
								atts.Add( new AttributeValuePair("fixable","true") );
								atts.Add( new AttributeValuePair("canWorkAround","true") );
								//
								monitoredEntity.SetAttributes(atts);
							}
						}
					}

					if(applyState)
					{
						if (! ApplyStateToMonitoredEntity())
						{
							// If we're down, check if it's caused by a server unplug.
							if (sender.GetBooleanAttribute("turnedoff", false))
							{
								// Has an event already been generated for this unplug?
								if (! sender.GetBooleanAttribute("penalised", false))
								{
									sender.SetAttribute("penalised", "true");

									Node CostedEvents = monitoredEntity.Tree.GetNamedNode("CostedEvents");
									ArrayList costAttrs = new ArrayList ();

									if (sender.GetBooleanAttribute("power_tripped", false))
									{
										costAttrs.Add(new AttributeValuePair ("desc", sender.GetAttribute("name") + " Power Overload"));
										costAttrs.Add(new AttributeValuePair ("reasondown", sender.GetAttribute("name") + " Power Overload"));
										costAttrs.Add(new AttributeValuePair ("incident_id", sender.GetAttribute("name") + "_power_overload"));
									}
									else
									{
										costAttrs.Add(new AttributeValuePair ("desc", sender.GetAttribute("name") + " Turned Off"));
										costAttrs.Add(new AttributeValuePair ("reasondown", sender.GetAttribute("name") + " Turned Off"));
										costAttrs.Add(new AttributeValuePair ("incident_id", sender.GetAttribute("name") + "_turn_off"));
									}

									costAttrs.Add(new AttributeValuePair ("type", "incident"));
									costAttrs.Add(new AttributeValuePair ("fixable", "false"));
									costAttrs.Add(new AttributeValuePair ("power", "true"));
									costAttrs.Add(new AttributeValuePair ("ops", "false"));
									costAttrs.Add(new AttributeValuePair ("facilities", "true"));
									costAttrs.Add(new AttributeValuePair ("canworkaround", "false"));
									costAttrs.Add(new AttributeValuePair ("zoneOf", sender.GetAttribute("name")));
									Node costedEvent = new Node (CostedEvents, "incident", "", costAttrs);
								}
							}
						}
					}
				}
			}
		}

		void n_PreAttributesChanged (Node sender, ref ArrayList attrs)
		{
			if ((attrs != null) && (attrs.Count > 0))
			{
				AttributeValuePair upAttribute = null;
				bool changing = false;
				bool goingUp = false;

				foreach (AttributeValuePair avp in attrs)
				{
					// Changing up status?
					if (avp.Attribute == "up")
					{
						upAttribute = avp;
						goingUp = (avp.Value.ToLower() == "true");
						changing = (avp.Value != sender.GetAttribute("up").ToLower());
					}
				}

				if ((upAttribute != null) && changing)
				{
					if (goingUp)
					{
						// Coming back up...
					}
					else
					{
						// Going down...
						// If we have a virtual mirror, then we might stay up.
						if (sender.GetBooleanAttribute("virtualmirrored", false))
						{
							if (sender.GetBooleanAttribute("virtualmirrorinuse", false))
							{
								// It's already up only because of the mirror: do nothing.
							}
							else
							{
								// Send it back up because of the mirror.
								attrs.Add(new AttributeValuePair ("virtualmirrorinuse", "true"));
								attrs.Remove(upAttribute);
							}
						}
					}
				}
			}
		}

		void monitoredEntity_BackLinkAdded(Node sender, LinkNode link)
		{
			// We've has a new connection added so re-evaluate our dependencies...
			this.BuildNotificationTree();
			ApplyStateToMonitoredEntity();
		}

		void monitoredEntity_BackLinkRemoved(Node sender, LinkNode link)
		{
			// We've has a connection removed so re-evaluate our dependencies...
			this.BuildNotificationTree();
			ApplyStateToMonitoredEntity();
		}

		void n_ParentChanged(Node sender, Node child)
		{
			// Re-assess out notification tree!
			BuildNotificationTree();
			ApplyStateToMonitoredEntity();
		}

		void n_ChildAdded(Node sender, Node child)
		{
			// If we have been given a dependsOn type child then we have to
			// rebuild our notification tree.
			if(child.GetAttribute("dependsOn") != "")
			{
				// Re-assess out notification tree!
				BuildNotificationTree();
				ApplyStateToMonitoredEntity();
			}
			else if((child.Type.ToLower() == "link")
				    || (child.Type.ToLower() == "connection"))
			{
				// Re-assess out notification tree!
				BuildNotificationTree();
				ApplyStateToMonitoredEntity();
			}
		}

		void n_ChildRemoved(Node sender, Node child)
		{
			// If we have had a dependsOn type child removed then we have to
			// rebuild our notification tree.
			if(child.GetAttribute("dependsOn") != "")
			{
				// Re-assess out notification tree!
				BuildNotificationTree();
				ApplyStateToMonitoredEntity();
			}
			else if ((child.Type.ToLower() == "link")
				     || (child.Type.ToLower() == "connection"))
			{
				// Re-assess out notification tree!
				BuildNotificationTree();
				ApplyStateToMonitoredEntity();
			}
		}
	}
}
