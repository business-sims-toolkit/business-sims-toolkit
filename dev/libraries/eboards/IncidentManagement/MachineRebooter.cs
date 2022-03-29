using System;
using System.Collections;
using LibCore;
using Network;

namespace IncidentManagement
{
	/// <summary>
	/// MachineRebooter watches servers in a network and reboots them as required.
	/// </summary>
	public class MachineRebooter
	{
		//private StopControlledTimer _timer = new StopControlledTimer();
		protected bool stopped = false;

		protected NodeTree targetTree;
		protected Hashtable watchedNodes = new Hashtable();
		protected Node AWT = null;

		protected ArrayList downedNodes = new ArrayList();
		protected ArrayList nodesGoingDown = new ArrayList();
		protected ArrayList nodesRebooting = new ArrayList();

		ArrayList types = new ArrayList ();

		protected Node currentTimeNode;
		protected int OverrideTime = 0; 
		protected Boolean OverrideEngaged = false; 

		public void Start()
		{
			stopped = false;
			//_timer.Start();
		}

		public void Stop()
		{
			stopped = true;
			//_timer.Stop();
		}

		public NodeTree TargetTree
		{
			get { return targetTree; }
			set
			{
				if(null != targetTree)
				{
					// Unregister notifications from old tree.
				}
				targetTree = value;
				//
				targetTree.NodeAdded += targetTree_NodeAdded;
				//
				AWT = targetTree.GetNamedNode("AdvancedWarningTechnology");
				//
				AttachToServers();
			}
		}

		public MachineRebooter(NodeTree nt)
		{
			currentTimeNode = nt.GetNamedNode("CurrentTime");
			currentTimeNode.AttributesChanged += currentTimeNode_AttributesChanged;
		}

		protected void AttachToServers()
		{
			if(watchedNodes.Keys.Count > 0)
			{
				foreach(Node n in watchedNodes.Keys)
				{
					n.AttributesChanged -= n_AttributesChanged;
				}
			}
			//
			watchedNodes.Clear();
			//
			types.Add("Server");
			types.Add("App");
			types.Add("Connection");
			types.Add("Slot");
			watchedNodes = targetTree.GetNodesOfAttribTypes(types);
			//
			foreach(Node n in watchedNodes.Keys)
			{
				n.AttributesChanged += n_AttributesChanged;
				//
				// This may have a "goingDown" set at startup!
				//
				string val = n.GetAttribute("goingDown");
				string incident_id = n.GetAttribute("incident_id");
				//
				NodeGoingDown(n,val,incident_id);
			}
		}
	
		public void setOverride(int OverrideRebootTime)
		{
			OverrideTime = OverrideRebootTime; 
			OverrideEngaged = true;
		}

		protected void NodeGoingDown(Node n, string val, string incident_id)
		{
			if( "true" == val )
			{
				if(null == AWT)
				{
					this.PushDownNode(n,false,incident_id);
				}
				else
				{
					if("true" == AWT.GetAttribute("enabled"))
					{
						// The node to bring down gets a counter put on it and it goes down in 60 seconds.
						if(!nodesGoingDown.Contains(n))
						{
							nodesGoingDown.Add(n);
							n.SetAttribute("incident_id",incident_id);
							n.SetAttribute("goingDownInSecs","10");
						}
					}
					else
					{
						this.PushDownNode(n,false,incident_id);
					}
				}
			}
		}

		/// <summary>
		/// Dispose ,,,
		/// </summary>
		public void Dispose()
		{
			if(watchedNodes.Keys.Count > 0)
			{
				foreach(Node n in watchedNodes.Keys)
				{
					n.AttributesChanged -= n_AttributesChanged;
				}
			}
			//
			watchedNodes.Clear();
		}

		void n_AttributesChanged(Node sender, ArrayList attrs)
		{ 
			if (attrs != null)
			{
				if (attrs.Count > 0)
				{
					int rebootingForSecs = sender.GetIntAttribute("rebootingForSecs", 0);

					foreach (AttributeValuePair avp in attrs)
					{
						if (avp.Attribute.ToLower() == "rebootingforsecs")
						{
							rebootingForSecs = CONVERT.ParseIntSafe(avp.Value, 0);
						}
					}

					foreach(AttributeValuePair avp in attrs)
					{
						//Extraction of the data attribute
						string attribute = avp.Attribute;
						string newValue = avp.Value;
						string ndename = sender.GetAttribute("name");

						//System.Diagnostics.Debug.WriteLine("Name:"+ndename + " Attr:"+attribute + " Value:"+newValue);

						//Do the work
						// Have we been told to reboot?
						if(attribute == "goingDown")
						{
							// Is the world set for AdvancedWarningTechnology ?
							// If this is switched on then the machine goes down in 60 seconds.
							// If it is switched off then the machine goes down with immediate effect.
							NodeGoingDown(sender,newValue, sender.GetAttribute("incident_id"));
						}
						else if("rebootFor" == attribute)
						{

							if(newValue != "0")
							{
								// Start reboot sequence.
								// If we are already rebooting extend the reboot time to the largest
								// of the two possibles.
								int irbt = rebootingForSecs;

								int rebootFor = CONVERT.ParseInt(newValue);
								if(rebootFor > irbt) irbt = rebootFor;

								// TODO : Don't call this override. Call it something like TransitionFastReboot.
								// We have several "overrides" and when reading back over the code you can't see
								// why without going into SVN logs and Fogbugz.
								if (this.OverrideEngaged)
								{
									irbt = this.OverrideTime;
								}

								//
								if(!nodesRebooting.Contains(sender))
								{
									nodesRebooting.Add(sender);
								}
								sender.SetAttribute("rebootingForSecs", irbt );
								sender.SetAttribute("up", "false" );
								//
								if(!stopped)
								{
									//_timer.Start();
								}
							}
						}
					}
				}
			}
		}

		void targetTree_NodeAdded(NodeTree sender, Node newNode)
		{
			string t = newNode.GetAttribute("type");
			if( types.Contains(t) )
			{
				if(!watchedNodes.ContainsKey(newNode))
				{
					string ndname = newNode.GetAttribute("name");
					//System.Diagnostics.Debug.WriteLine("MR Added "+ndname+" to Watch");
					newNode.AttributesChanged += n_AttributesChanged;
					watchedNodes.Add(newNode,1);
				}
			}
		}

		protected void PullUpNode(Node n)
		{
			downedNodes.Add(n);
			// Case 4385:   ALL: install from workaround doesn't clear incident 
			// wipe workarounds and any incident number on the node at the same time.
			// Also refactored so that all attribute changes are sent at the same time.
			ArrayList att = new ArrayList();
			att.Add( new AttributeValuePair("goingDown","") );
			att.Add( new AttributeValuePair("workingaround","") );
			att.Add( new AttributeValuePair("incident_id","") );
			att.Add( new AttributeValuePair("rebootingForSecs","") );
			att.Add( new AttributeValuePair("up","true") );
			att.Add( new AttributeValuePair("reasondown","") );
			n.SetAttributes(att);

			/* was pre 4385...
			n.SetAttribute("goingDown","");
			n.SetAttribute("rebootingForSecs","");
			n.SetAttribute("up","true");
			n.SetAttribute("reasondown","");*/
		}

		protected void PushDownNode(Node n, bool addToDownedNodes, string incident_id)
		{
			if(addToDownedNodes)
			{
				downedNodes.Add(n);
			}
			ArrayList att = new ArrayList();

			att.Add( new AttributeValuePair("goingDown","") );
			att.Add( new AttributeValuePair("goingDownInSecs","") );
			att.Add( new AttributeValuePair("up","false") );
			if(incident_id != "")
			{
				att.Add( new AttributeValuePair("incident_id",incident_id) );
			}
			n.SetAttributes(att);
			//
			// We also have to add a CostedEvent that states that this incident has now been applied.
			//
			Node costedEventsNode = n.Tree.GetNamedNode("CostedEvents");
			ArrayList attrs = new ArrayList();
			attrs.Add( new AttributeValuePair( "type", "incident" ) );
			string i_id = n.GetAttribute("incident_id");
			if("" == incident_id)
			{
				attrs.Add( new AttributeValuePair( "incident_id", i_id ) );
			}
			else
			{
				attrs.Add( new AttributeValuePair( "incident_id", incident_id ) );
			}
			attrs.Add( new AttributeValuePair( "desc", n.GetAttribute("name") + " Down." ) );
			Node costedEvent = new Node(costedEventsNode,"incident", "", attrs);
			//
		}

		//private void _timer_Tick(object sender, EventArgs e)
		protected void Tick()
		{
				if( (nodesGoingDown.Count == 0) && (nodesRebooting.Count == 0) )
				{
					//System.Diagnostics.Debug.WriteLine("Doing Nothing");
					//_timer.Stop();
				}
				else
				{
					// Process nodes going down...
					foreach(Node n in nodesGoingDown)
					{
						string nnname1 = n.GetAttribute("name");
						//System.Diagnostics.Debug.WriteLine("Process Node going down "+nnname1);	

						string s_secs = n.GetAttribute("goingDownInSecs");
						if("" == s_secs)
						{
							// Someone has prevented this incident from occuring!
							this.downedNodes.Add(n);
							//
						}
						else
						{
							int secs = CONVERT.ParseInt(s_secs);
							if(secs <= 0)
							{
								PushDownNode(n,true,"");
							}
							else
							{
								--secs;
								if(secs <= 0)
								{
									PushDownNode(n,true,"");
								}
								else
								{
									//
									// 11_04_2007 : Don't count down going down in secs as the going
									// down state is now infinite. Um, design changed radically again
									// so we do count down the secs... (18-04-2007).
									//
									n.SetAttribute("goingDownInSecs", secs);
								}
							}
						}
					}
					// Clear downed nodes from list...
					foreach(Node n in downedNodes)
					{
						string nnname2 = n.GetAttribute("name");
						//System.Diagnostics.Debug.WriteLine("Clean Down Node (a)  "+nnname2);	

						if(nodesGoingDown.Contains(n))
						{
							nodesGoingDown.Remove(n);
						}
					}
					downedNodes.Clear();
					//
					// Process nodes that are rebooting...
					//
					foreach(Node n in nodesRebooting)
					{
						string nnname3 = n.GetAttribute("name");
						//System.Diagnostics.Debug.WriteLine("Process Nodes that are rebooting  "+nnname3);	

						string s_secs = n.GetAttribute("rebootingForSecs");
						if("" == s_secs)
						{
							// Error! Shouldn't have happned. Somebody set the attribute wrong.
						}
						else
						{
							int secs = CONVERT.ParseInt(s_secs);
							if(secs <= 0)
							{
								PullUpNode(n);
							}
							else
							{
								--secs;
								if(secs <= 0)
								{
									PullUpNode(n);
								}
								else
								{
									n.SetAttribute("rebootingForSecs", secs);
								}
							}
						}
					}
					// Clear downed nodes from list...
					foreach(Node n in downedNodes)
					{
						string nnname4 = n.GetAttribute("name");
						//System.Diagnostics.Debug.WriteLine("Clean Down Node (a)  "+nnname4);

						if(nodesRebooting.Contains(n))
						{
							nodesRebooting.Remove(n);
						}
					}
					downedNodes.Clear();
				}
		}

		void currentTimeNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			foreach(AttributeValuePair avp in attrs)
			{
				if(avp.Attribute == "seconds")
				{
					//System.Diagnostics.Debug.WriteLine("MRTime "+avp.Value);
					Tick();
				}
			}
		}
	}
}
