using System.Collections;
using System.Collections.Generic;
using LibCore;
using Network;

using CoreUtils;

using IncidentManagement;

namespace BusinessServiceRules
{
	/// <summary>
	/// The BusinessServiceFixer watches a fix queue "FixItQueue" and fixes all things up the dependency
	/// tree that it can if told to. It will also push into workaround anything fixable on the tree if
	/// told to workaround.
	///
	/// If it sees a "fix it" command it should climb the service dependency paths fixing all jobs that
	/// can be fixed, even if they are in workaround.
	///
	/// If it see a "Workaround" command it should climb the service dependency paths applying workarounds
	/// to all fixable incidents that are not already in workaround.
	/// </summary>
	public class BusinessServiceFixer : ITimedClass
	{
		protected IncidentApplier incidentApplier;
		ServiceDownCounter _sdc;
		/// <summary>
		/// There is a Node in the model that is an incoming fix it command queue...
		/// </summary>
		protected Node fixItQueue;
		/// <summary>
		/// There is a Node in the model that counts up the number of workarounds you have used
		/// in this round.
		/// </summary>
		protected Node workAroundCounter;
		/// <summary>
		/// We store the model we are working with.
		/// </summary>
		protected NodeTree targetTree;
		/// <summary>
		/// We need to store which Nodes in the model we ar applying workarounds to so that we
		/// don't try to double workaround on a Node.
		/// </summary>
		protected ArrayList workingAroundNodes = new ArrayList();
		/// <summary>
		/// There is a Node in the model that CostedEvents are added to.
		/// </summary>
		protected Node costedEvents;
		/// <summary>
		/// There is a Node in the model that shows the current time in seconds.
		/// </summary>
		protected Node currentTimeNode;


		/// <summary>
		/// Amount of time that we can be in workAround
		/// </summary>
		protected int workAround_TimePeriod;

		protected ArrayList costedFixes = new ArrayList();
		/// <summary>
		/// We can do three types of actions; FIXes, WORKAROUNDs, and FIX_BY_CONSULTANCY.
		/// </summary>
		protected enum Action
		{
			/// <summary> Fix a node </summary>
			FIX,
			/// <summary> Apply A Workaround</summary>
			WORKAROUND,
			/// <summary> Fix by workaround </summary>
			FIX_BY_CONSULTANCY,
			/// <summary> Apply a First Line Fix </summary>
			FIRSTLINEFIX,
			/// <summary> Apply an Automated Fix </summary>
			AUTOMATED
		}

		#region ITimedClass Methods
		/// <summary>
		/// We clear our workarounds on a reset.
		/// </summary>
		public void Reset()
		{
			lock(this)
			{
				// TODO : Actually clear the workarounds.
				workingAroundNodes.Clear();
			}
		}
		/// <summary>
		/// We don't care if we the game is stopped or not. We don't have a timer.
		/// </summary>
		public void Start() { /* NOP */ }
		/// <summary>
		/// We don't care if we the game is stopped or not. We don't have a timer.
		/// </summary>
		public void Stop() { /* NOP */ }
		/// <summary>
		/// We don't care if we the game is fast forwarded. We don't have a timer.
		/// </summary>
		public void FastForward(double timesRealTime) { /* NOP */ }

		#endregion

		public NodeTree TargetTree
		{
			get { return targetTree; }
			set
			{
				targetTree = value;
				// Attach to "FixItQueue". If it doesn't exist create it.
				fixItQueue = targetTree.GetNamedNode("FixItQueue");
				currentTimeNode = targetTree.GetNamedNode("CurrentTime");
				//
				fixItQueue.ChildAdded += fixItQueue_ChildAdded;
				currentTimeNode.AttributesChanged += currentTimeNode_AttributesChanged;
				//
				workAroundCounter = targetTree.GetNamedNode("AppliedWorkArounds");
				//
				costedEvents = value.GetNamedNode("CostedEvents");
			}
		}

		/// <summary>
		/// Displose ...
		/// </summary>
		public void Dispose()
		{
			CoreUtils.TimeManager.TheInstance.UnmanageClass(this);
			if(fixItQueue != null)
			{
				fixItQueue.ChildAdded -= fixItQueue_ChildAdded;
			}

			if(currentTimeNode != null)
			{
				currentTimeNode.AttributesChanged -= currentTimeNode_AttributesChanged;
			}
		}

		public BusinessServiceFixer(ServiceDownCounter sdc, IncidentApplier applier)
		{
			//If you are changing the workaround time, mind to change the value in ignore attributes
			//We set the start value in the ignore attribute (which we need to see) and ignore all others
			workAround_TimePeriod = SkinningDefs.TheInstance.GetIntData("workaround_time", 120);

			_sdc = sdc;
			this.incidentApplier = applier;
			CoreUtils.TimeManager.TheInstance.ManageClass(this);
		}

		bool IsIntegerStateHappening(string state)
		{
			if( (""!=state) && ("0"!=state) )
			{
				return true;
			}
			return false;
		}

		protected bool IsPenalty (Node n)
		{
			return (n.GetAttribute("penalty").ToLower() == "yes");
		}

		void WipeGoingDown(Node n)
		{
			if ((! IsPenalty(n)) && ("" != n.GetAttribute("goingDownInSecs")))
			{
				//
				// We also have to add a CostedEvent that states that this incident has been prevented.
				//
				Node costedEventsNode = n.Tree.GetNamedNode("CostedEvents");
				ArrayList attrs2 = new ArrayList();
				attrs2.Add( new AttributeValuePair( "type", "prevented_incident" ) );
				attrs2.Add( new AttributeValuePair( "id", n.GetAttribute("id") ) );
				Node costedEvent = new Node(costedEventsNode,"incident", "", attrs2);
				//
				ArrayList attrs = new ArrayList();
				attrs.Add( new AttributeValuePair("goingDown","") );
				attrs.Add( new AttributeValuePair("goingDownInSecs","") );
				n.SetAttributes( attrs );
			}
		}

		protected bool fixedAWorkAround = false;

		protected void WipeWorkAround(Node n)
		{
			workingAroundNodes.Remove(n);
			//
			// - 29-04-2007 : Need to let reports know that an event occured against
			// a particular business service user...
			fixedAWorkAround = true;
		}

		/// <summary>
		/// We need to reset the AWT danger level when we are fixing the problem
		/// so that we go back to normal operational display
		/// Better code would just walk up the parents until it get s to the App or Server
		/// and then clear the danger_level setting for that
		/// </summary>
		/// <param name="n"></param>
		protected virtual void ResetDangerLeveltoNormal(Node n)
		{
			string node_type = n.GetAttribute("type");

			//We are fixing the direct item which failled
			if ((node_type.ToLower() == "server") | (node_type.ToLower() == "app") | (node_type.ToLower() == "database")
				| (node_type.ToLower() == "router") | (node_type.ToLower() == "hub"))
			{
				n.SetAttribute("danger_level","20");
				n.SetAttribute("procdown","false");
			}
			//We are fixing the connection underneath the item that failed
			//We have failled a sub part of the thing rather than than the whole item
			// Three different suituation
			// 1, A connection directly under the App (or Server)
			// 2, A support tech directly under the App (or Server)
			// 3, A connection under a support tech which directly under the App (or Server)
			if (node_type.ToLower() == "connection")
			{
				if (n.Parent != null)
				{
					//check what the parent is
					string node_type2 = n.Parent.GetAttribute("type");
					if ((node_type2.ToLower() == "server")|(node_type2.ToLower() == "app"))
					{
						//Type 1 situation, just fix the parent
						n.Parent.SetAttribute("danger_level","20");
						n.Parent.SetAttribute("procdown","false");
					}
					if ((node_type2.ToLower() == "supporttech"))
					{
						//Type 3 situation, just fix the Grandparent
						//need to one higher to clear the problem
						Node Grandparent = n.Parent.Parent;
						if (Grandparent != null)
						{
							string node_type3 = Grandparent.GetAttribute("type");
							if ((node_type3.ToLower() == "server")|(node_type3.ToLower() == "app"))
							{
								Grandparent.SetAttribute("danger_level","20");
								Grandparent.SetAttribute("procdown","false");
							}
						}
					}
				}
			}
			//We are fixing the supporttech underneath the item that failled
			//We have failed more than one sub part of the thing rather than than the whole item
			if (node_type.ToLower() == "supporttech")
			{
				//Type 2 situation, just fix the Parent
				if (n.Parent != null)
				{
					string node_type2 = n.Parent.GetAttribute("type");
					if ((node_type2.ToLower() == "server")|(node_type2.ToLower() == "app"))
					{
						n.Parent.SetAttribute("danger_level","20");
						n.Parent.SetAttribute("procdown","false");
					}
				}
			}
		}

		protected virtual void Fix(Node n, bool isAutoFix)
		{
			ArrayList attrs = new ArrayList();
			AttributeValuePair.AddIfNotEqual(n,attrs, "up", "true");
			AttributeValuePair.AddIfNotEqual(n,attrs, "incident_id", "");
			AttributeValuePair.AddIfNotEqual(n,attrs, "virtualmirrorinuse", "false");
			AttributeValuePair.AddIfNotEqual(n,attrs, "up_online", "true");
			AttributeValuePair.AddIfNotEqual(n,attrs, "up_instore", "true");
			AttributeValuePair.AddIfNotEqual(n,attrs, "downForSecs", "");
			AttributeValuePair.AddIfNotEqual(n,attrs, "dos", "");
			AttributeValuePair.AddIfNotEqual(n,attrs, "workingAround", 0);
			AttributeValuePair.AddIfNotEqual(n,attrs, "denial_of_service", "false");
			AttributeValuePair.AddIfNotEqual(n, attrs, "security_flaw", "false");
			AttributeValuePair.AddIfNotEqual(n, attrs, "compliance_incident", "false");
			AttributeValuePair.AddIfNotEqual(n, attrs, "thermal", "false");
			AttributeValuePair.AddIfNotEqual(n, attrs, "nopower", "false");
			AttributeValuePair.AddIfNotEqual(n, attrs, "just_been_auto_fixed", isAutoFix);
			AttributeValuePair.AddIfNotEqual(n, attrs, "klaxon_triggered", false);
			n.SetAttributes(attrs);
		}

		protected virtual int GetDangerLevelForAction (Action action)
		{
			return 0;
		}

		protected void ClimbTreeAndApplyAction(Node n, Action action, bool isAutoFix, out bool is_thermal, out bool is_power)
		{
			is_thermal = false;
			is_power = false;

            if (n == targetTree.Root)
            {
                return;
            }

            // Fix for 4696 (klaxon triggers during fixing incidents).
            // During a fix, this was climbing too high up the tree from the BSU to
            // the parent BS, and prematurely claiming the BS was fixed, causing the
            // klaxon to retrigger when the BS was then forced down again by the
            // other BSUs that hadn't been fixed yet.  This is a result of the
            // change in structure from BSUs nested inside BSes, to BSUs linking
            // to BSes.
            if (n.GetAttribute("type") == "biz_service")            
            {
                if (n.GetBooleanAttribute("fixable", false))
                {
                    if (n.GetIntAttribute("danger_level") > 0)
                    {
                        List<AttributeValuePair> attributes = new List<AttributeValuePair> { new AttributeValuePair ("danger_level", GetDangerLevelForAction(action)) };

                        if (action != Action.WORKAROUND)
                        {
                            attributes.Add(new AttributeValuePair ("fixable", false));
                            attributes.Add(new AttributeValuePair ("incident_id", ""));
                        }

                        n.SetAttributes(attributes);
                    }
                }

                return;
            }

			// Have we found a notInNetwork node?
			if(n.GetBooleanAttribute("notInNetwork", false))
			{
				return;
			}

			string goingDown = n.GetAttribute("goingDownInSecs");
			string workingAround = n.GetAttribute("workingAround");
			string name = n.GetAttribute("name");

			string incident_id = n.GetAttribute("incident_id");
			string node_type = n.GetAttribute("type");
			bool addIncidentID = false;
			//
			bool is_thermal_incident = n.GetBooleanAttribute("thermal", false);
			bool is_power_incident = n.GetBooleanAttribute("nopower", false);

			is_thermal = is_thermal_incident;
			is_power = is_power_incident;

			bool hvac = n.GetBooleanAttribute("thermal", false) || n.GetBooleanAttribute("nopower", false);

			//
			//
			if( IsIntegerStateHappening(workingAround) )
			{
				if(action == Action.WORKAROUND)
				{
					// NOP - It is already in workaround.
					// says yes...
					ArrayList attrs = new ArrayList();

                    attrs.Add( new AttributeValuePair("workingAround",CONVERT.ToStr(workAround_TimePeriod)) );
					attrs.Add( new AttributeValuePair("up","true") );

					ResetDangerLeveltoNormal(n);
					n.SetAttributes(attrs);
					if(!workingAroundNodes.Contains(n))
					{
						workingAroundNodes.Add(n);
					}
					// Add the fix to the costing queue...
					string reason = "Work Around Applied By Business Service Fixer To " + name + ".";
					attrs.Clear();
					attrs.Add( new AttributeValuePair("desc",reason) );
					attrs.Add( new AttributeValuePair("incident_id",incident_id) );
					attrs.Add( new AttributeValuePair("node_category",node_type) );
					//
					if(is_thermal_incident)
					{
						attrs.Add( new AttributeValuePair("type","entity_thermal_workaround") );
						Node newCost = new Node(costedEvents, "entity_thermal_workaround", "", attrs);
					}
					else
					{
						attrs.Add( new AttributeValuePair("type","entity_workaround") );
						Node newCost = new Node(costedEvents, "entity_workaround", "", attrs);
					}

                    addIncidentID = true;

                    costedFixes.Add(reason);
				}
				else if (action == Action.FIX || action == Action.FIRSTLINEFIX || action == Action.AUTOMATED)
				{
					Fix(n, isAutoFix);
					ResetDangerLeveltoNormal(n);
					WipeWorkAround(n);
					// Add the fix to the costing queue...
					string reason = "Fix Applied By Business Service Fixer To "+ name + ".";
					ArrayList attrs = new ArrayList();
					attrs.Add( new AttributeValuePair("desc",reason) );
					attrs.Add( new AttributeValuePair("incident_id",incident_id) );
					attrs.Add( new AttributeValuePair("node_category",node_type) );
					//
					if (action == Action.FIX)
					{
						if (is_thermal_incident)
						{
							attrs.Add(new AttributeValuePair("type", "entity thermal fix"));
							Node newCost = new Node(costedEvents, "entity_thermal_fix", "", attrs);
						}
						else
						{
							attrs.Add(new AttributeValuePair("type", "entity fix"));
							Node newCost = new Node(costedEvents, "entity fix", "", attrs);
						}
					}
					else if (action == Action.FIRSTLINEFIX)
					{
						if (is_thermal_incident)
						{
							attrs.Add(new AttributeValuePair("type", "entity thermal fix"));
							Node newCost = new Node(costedEvents, "entity_thermal_first_line_fix", "", attrs);
						}
						else
						{
							attrs.Add(new AttributeValuePair("type", "entity fix"));
							Node newCost = new Node(costedEvents, "entity first line fix", "", attrs);
						}
					}
					else if (action == Action.AUTOMATED) //Redundant condition, but it reads better ;)
					{
						if (is_thermal_incident)
						{
							attrs.Add(new AttributeValuePair("type", "entity thermal fix"));
							Node newCost = new Node(costedEvents, "entity_thermal_automated_fix", "", attrs);
						}
						else
						{
							attrs.Add(new AttributeValuePair("type", "entity fix"));
							Node newCost = new Node(costedEvents, "entity automated fix", "", attrs);
						}
					}
					addIncidentID = true;
					//
					costedFixes.Add(reason);
				}
				else if(action == Action.FIX_BY_CONSULTANCY)
				{
					Fix(n, false);

					// Add the fix to the costing queue...
					string reason = "Fix By Consultancy Applied By Business Service Fixer To "+ name + ".";
					ArrayList attrs = new ArrayList();
					attrs.Add( new AttributeValuePair("desc",reason) );
					attrs.Add( new AttributeValuePair("incident_id",incident_id) );
					attrs.Add( new AttributeValuePair("node_category",node_type) );
					if (! string.IsNullOrEmpty(n.GetAttribute("function")))
					{
						attrs.Add(new AttributeValuePair ("function", n.GetAttribute("function")));
					}
					//
					if(is_thermal_incident)
					{
						attrs.Add( new AttributeValuePair("type","entity_thermal_fix_by_consultancy") );
						Node newCost = new Node(costedEvents, "entity_thermal_fix_by_consultancy", "", attrs);
					}
					else
					{
						attrs.Add( new AttributeValuePair("type","entity_fix_by_consultancy") );
						Node newCost = new Node(costedEvents, "entity_fix_by_consultancy", "", attrs);
					}
					//
					addIncidentID = true;
					//
					costedFixes.Add(reason);
				}
			}
			else if ((! n.GetBooleanAttribute("up", true))
                     || n.GetBooleanAttribute("virtualmirrorinuse", false)
                     || ((n.GetIntAttribute("danger_level", 0) > 0) && (action != Action.WORKAROUND)))
			{
				// This is node is down.
				// If it is rebooting then we can't fix it.
				string rbt = n.GetAttribute("rebootingForSecs");
				if( true == IsIntegerStateHappening(rbt) )
				{
					// This is rebooting so we cannot fix it.
				}
				else
				{
					// Not rebooting so can apply fix/workaround.
					if (action == Action.FIX || action == Action.FIRSTLINEFIX || action == Action.AUTOMATED)
					{
						Fix(n, isAutoFix);
						ResetDangerLeveltoNormal(n);

						// Add the fix to the costing queue...
						string reason = "Fix Applied By Business Service Fixer To " + name + ".";
						ArrayList attrs = new ArrayList();
						attrs.Add( new AttributeValuePair("type","entity fix") );
						attrs.Add( new AttributeValuePair("desc",reason) );
						attrs.Add( new AttributeValuePair("incident_id",incident_id) );
						attrs.Add( new AttributeValuePair("node_category",node_type) );
						//
						if (action == Action.FIX)
						{
							if (is_thermal_incident)
							{
								Node newCost = new Node(costedEvents, "entity_thermal_fix", "", attrs);
							}
							else
							{
								Node newCost = new Node(costedEvents, "entity fix", "", attrs);
							}
						}
						else if (action == Action.FIRSTLINEFIX)
						{
							if (is_thermal_incident)
							{
								Node newCost = new Node(costedEvents, "entity_thermal_first_line_fix", "", attrs);
							}
							else
							{
								Node newCost = new Node(costedEvents, "entity first line fix", "", attrs);
							}
						}
						else if (action == Action.AUTOMATED)
						{
							if (is_thermal_incident)
							{
								Node newCost = new Node(costedEvents, "entity_thermal_automated_fix", "", attrs);
							}
							else
							{
								Node newCost = new Node(costedEvents, "entity automated fix", "", attrs);
							}
						}
						//
						addIncidentID = true;
						//
						costedFixes.Add(reason);
					}
					else if (action == Action.FIX_BY_CONSULTANCY)
					{
						Fix(n, isAutoFix);
						ResetDangerLeveltoNormal(n);

						// Add the fix to the costing queue...
						string reason = "Fix By Consultancy Applied By Business Service Fixer To "+ name + ".";
						ArrayList attrs = new ArrayList();
						attrs.Add( new AttributeValuePair("desc",reason) );
						attrs.Add( new AttributeValuePair("incident_id",incident_id) );
						attrs.Add( new AttributeValuePair("node_category",node_type) );
						if (! string.IsNullOrEmpty(n.GetAttribute("function")))
						{
							attrs.Add(new AttributeValuePair ("function", n.GetAttribute("function")));
						}
						//
						if(is_thermal_incident)
						{
							attrs.Add( new AttributeValuePair("type","entity_thermal_fix_by_consultancy") );
							Node newCost = new Node(costedEvents, "entity_thermal_fix_by_consultancy", "", attrs);
						}
						else if (! (is_thermal_incident || is_power_incident))
						{
							attrs.Add( new AttributeValuePair("type","entity_fix_by_consultancy") );
							Node newCost = new Node(costedEvents, "entity_fix_by_consultancy", "", attrs);
						}
						//
						addIncidentID = true;
						//
						costedFixes.Add(reason);
					}
					else if(action == Action.WORKAROUND)
					{
						if(!workingAroundNodes.Contains(n))
						{
							//System.Diagnostics.Debug.WriteLine("WorkAround Being Applied To " + n.GetAttribute("name") + ".");
							//
							ArrayList attrs = new ArrayList();
							//
							attrs.Add(new AttributeValuePair("workingAround", CONVERT.ToStr(workAround_TimePeriod)));
							attrs.Add( new AttributeValuePair("up","true") );

                            if (!n.GetBooleanAttribute("up_instore", true))
                            {
                                attrs.Add(new AttributeValuePair("up_instore", "true"));
                                attrs.Add(new AttributeValuePair("workaround_was_up_instore", false));
                            }

                            if (!n.GetBooleanAttribute("up_online", true))
                            {
                                attrs.Add(new AttributeValuePair("up_online", "true"));
                                attrs.Add(new AttributeValuePair("workaround_was_up_online", false));
                            }

							ResetDangerLeveltoNormal(n);
							n.SetAttributes(attrs);
							workingAroundNodes.Add(n);
							// Add the fix to the costing queue...
							string reason = "Work Around Applied By Business Service Fixer To " + name + ".";
							attrs.Clear();
							attrs.Add( new AttributeValuePair("desc",reason) );
							attrs.Add( new AttributeValuePair("incident_id",incident_id) );
							attrs.Add( new AttributeValuePair("node_category",node_type) );
							//
							if(is_thermal_incident)
							{
								attrs.Add( new AttributeValuePair("type","entity_thermal_workaround") );
								Node newCost = new Node(costedEvents, "entity_thermal_workaround", "", attrs);
							}
							else
							{
								attrs.Add( new AttributeValuePair("type","entity_workaround") );
								Node newCost = new Node(costedEvents, "entity_workaround", "", attrs);
							}
							//
							addIncidentID = true;
							//
							costedFixes.Add(reason);
						}
					}
				}

				if(addIncidentID)
				{
					// : 3-5-2007 - Do not put the same incident id on twice!
					if(global_incident_id != string.Empty)
					{
						char[] comma = { ',' };
						string[] incidents = global_incident_id.Split(comma);
						bool found = false;
						for(int i=0; i<incidents.Length; ++i)
						{
							if(incidents[i] == incident_id) found = true;
						}
						if(!found)
						{
							global_incident_id += "," + incident_id;
						}
					}
					else
					{
						global_incident_id += incident_id;
					}
				}
			}
			else if( IsIntegerStateHappening(goingDown) )
			{
				// This is in countdown to failure so should still be fixed.
				if (action == Action.FIX || action == Action.FIRSTLINEFIX || action == Action.AUTOMATED)
				{
					WipeGoingDown(n);
					n.SetAttribute("incident_id","");
					ResetDangerLeveltoNormal(n);
					// Add the fix to the costing queue...
					ArrayList attrs = new ArrayList();
					string reason = "Fix Applied By Business Service Fixer To "+ name + ".";

					attrs.Add( new AttributeValuePair("desc",reason) );
					attrs.Add( new AttributeValuePair("incident_id",incident_id) );
					attrs.Add( new AttributeValuePair("node_category",node_type) );
					//
					if (action == Action.FIX)
					{
						if (is_thermal_incident)
						{
							attrs.Add(new AttributeValuePair("type", "entity thermal fix"));
							Node newCost = new Node(costedEvents, "entity_thermal_fix", "", attrs);
						}
						else
						{
							attrs.Add(new AttributeValuePair("type", "entity fix"));
							Node newCost = new Node(costedEvents, "entity fix", "", attrs);
						}
					}
					else if (action == Action.FIRSTLINEFIX)
					{
						if (is_thermal_incident)
						{
							attrs.Add(new AttributeValuePair("type", "entity thermal fix"));
							Node newCost = new Node(costedEvents, "entity_thermal_first_line_fix", "", attrs);
						}
						else
						{
							attrs.Add(new AttributeValuePair("type", "entity fix"));
							Node newCost = new Node(costedEvents, "entity first line fix", "", attrs);
						}
					}
					else if (action == Action.AUTOMATED)
					{
						if (is_thermal_incident)
						{
							attrs.Add(new AttributeValuePair("type", "entity automated fix"));
							Node newCost = new Node(costedEvents, "entity_thermal_automated_fix", "", attrs);
						}
						else
						{
							attrs.Add(new AttributeValuePair("type", "entity fix"));
							Node newCost = new Node(costedEvents, "entity automated fix", "", attrs);
						}
					}

					costedFixes.Add(reason);
				}
				else if (action == Action.FIX_BY_CONSULTANCY)
				{
					WipeGoingDown(n);
					n.SetAttribute("incident_id","");
					ResetDangerLeveltoNormal(n);
					// Add the fix to the costing queue...
					ArrayList attrs = new ArrayList();
					string reason = "Fix By Consultancy Applied By Business Service Fixer To "+ name + ".";

					attrs.Add( new AttributeValuePair("desc",reason) );
					attrs.Add( new AttributeValuePair("incident_id",incident_id) );
					attrs.Add( new AttributeValuePair("node_category",node_type) );
					if (! string.IsNullOrEmpty(n.GetAttribute("function")))
					{
						attrs.Add(new AttributeValuePair ("function", n.GetAttribute("function")));
					}
					//
					if(is_thermal_incident)
					{
						attrs.Add( new AttributeValuePair("type","entity_thermal_fix_by_consultancy") );
						Node newCost = new Node(costedEvents, "entity_thermal_fix_by_consultancy", "", attrs);
					}
					else
					{
						attrs.Add( new AttributeValuePair("type","entity_fix_by_consultancy") );
						Node newCost = new Node(costedEvents, "entity_fix_by_consultancy", "", attrs);
					}

					costedFixes.Add(reason);
				}
				else if(action == Action.WORKAROUND)
				{
					// This is in countdown to failure so should still be worked around.
					WipeGoingDown(n);
					//
					if(!workingAroundNodes.Contains(n))
					{
						workingAroundNodes.Add(n);
						ResetDangerLeveltoNormal(n);
						//
						string reason = "Work Around Applied By Business Service Fixer To " + name + ".";
						ArrayList attrs = new ArrayList();

						attrs.Add( new AttributeValuePair("workingAround",CONVERT.ToStr(workAround_TimePeriod)) );
						attrs.Add( new AttributeValuePair("up","true") );
						n.SetAttributes(attrs);
						attrs.Clear();
						attrs.Add( new AttributeValuePair("desc",reason) );
						attrs.Add( new AttributeValuePair("incident_id",incident_id) );
						attrs.Add( new AttributeValuePair("node_category",node_type) );
						//
						if(is_thermal_incident)
						{
							attrs.Add( new AttributeValuePair("type","entity_thermal_workaround") );
							Node newCost = new Node(costedEvents, "entity_thermal_workaround", "", attrs);
						}
						else
						{
							attrs.Add( new AttributeValuePair("type","entity_workaround") );
							Node newCost = new Node(costedEvents, "entity_workaround", "", attrs);
						}
						//
						costedFixes.Add(reason);
					}
				}
			}

			// Does this node have any depends on links to other nodes?
			// If so then fix those trees too...
			foreach(Node deNode in n)
			{
				if("dependsOn" == deNode.GetAttribute("type"))
				{
					bool _is_thermal;
					bool _is_power;
					// This is a dependsOn link so follow it and fix it...
					Node target = n.Tree.GetNamedNode( deNode.GetAttribute("item") );
					ClimbTreeAndApplyAction(target,action, isAutoFix, out _is_thermal, out _is_power);
					//
					if(_is_thermal) is_thermal = true;
					if(_is_power) is_power = true;
				}
			}

            if ((costedFixes.Count == 0) || SkinningDefs.TheInstance.GetBoolData("climb_tree_to_apply_workarounds", true))
            {
                if (null != n.Parent)
                {
                    bool _is_thermal;
                    bool _is_power;
                    ClimbTreeAndApplyAction(n.Parent, action, isAutoFix, out _is_thermal, out _is_power);
                    if (_is_thermal) is_thermal = true;
                    if (_is_power) is_power = true;
                }
            }
		}

		protected string global_incident_id = "";

		protected int ApplyFixesToVirtualMirroredApp (Node app, out string reason, out string prevented_incident, out bool is_thermal, out bool is_power, Action action)
		{
			is_thermal = false;
			is_power = false;
			reason = "";
			prevented_incident = "";
			int beenDownFor = app.GetIntAttribute("downforsecs",999);

			global_incident_id = "";

			costedFixes.Clear();

			bool _is_thermal;
			bool _is_power;
			ClimbTreeAndApplyAction(app, action, false, out _is_thermal, out _is_power);
			if(_is_thermal) is_thermal = true;
			if(_is_power) is_power = true;

			if(costedFixes.Count > 0)
			{
				foreach(string str in costedFixes)
				{
					if(reason.Length > 0) reason += "\r\n";
					reason += str;
				}
				if(beenDownFor <= 10)
				{
					// : fix for 4937 (quick fixes to install penalties still count as
					// prevented incidents).
					bool isPenalty = false;

					if (global_incident_id != "")
					{
						IncidentDefinition incident = incidentApplier.GetIncident(global_incident_id);
						isPenalty = (incident != null) && incident.IsPenalty;
					}

					if (! isPenalty)
					{
						prevented_incident = global_incident_id;
					}
					/*
					attrs.Clear();
					attrs.Add( new AttributeValuePair("type","prevented_incident") );
					attrs.Add( new AttributeValuePair("incident_id",global_incident_id) );
					Node newCost2 = new Node(costedEvents, "prevented_incident", "", attrs);*/
				}
			}

			return costedFixes.Count;
		}

		protected int ApplyFixByConsultancyToVirtualMirroredApp(Node app, out string reason, out bool is_thermal, out bool is_power, Action action)
		{
			is_thermal = false;
			is_power = false;
			reason = "";
			//
			// 28-03-2007 : If a mirrored item is fixed then we only cost the fix as one fix!
			//
			costedFixes.Clear();
			//
			bool _is_thermal;
			bool _is_power;
			ClimbTreeAndApplyAction(app, action, false, out _is_thermal, out _is_power);
			if(_is_thermal) is_thermal = true;
			if(_is_power) is_power = true;
			//
			if(costedFixes.Count > 0)
			{
				foreach(string str in costedFixes)
				{
					if(reason.Length > 0) reason += "\r\n";
					reason += str;
				}
				/*
				ArrayList attrs = new ArrayList();
				attrs.Add( new AttributeValuePair("type","fix_by_consultancy") );
				attrs.Add( new AttributeValuePair("desc",reason) );
				Node newCost = new Node(costedEvents, "fix_by_consultancy", "", attrs);*/
			}

			return costedFixes.Count;
		}

		protected int ApplyWorkaroundsToVirtualMirroredApp(Node app, out string reason, out bool is_thermal, out bool is_power)
		{
			is_thermal = false;
			is_power = false;
			reason = "";
			//
			// 28-03-2007 : If a mirrored item is fixed then we only cost the fix as one fix!
			//
			costedFixes.Clear();
			//
			bool _is_thermal;
			bool _is_power;
			ClimbTreeAndApplyAction(app, Action.WORKAROUND, false, out _is_thermal, out _is_power);
			if(_is_thermal) is_thermal = true;
			//
			if(costedFixes.Count > 0)
			{
				foreach(string str in costedFixes)
				{
					if(reason.Length > 0) reason += "\r\n";
					reason += str;
				}
				/*
				ArrayList attrs = new ArrayList();
				attrs.Add( new AttributeValuePair("type","workaround") );
				attrs.Add( new AttributeValuePair("desc",reason) );
				Node newCost = new Node(costedEvents, "workaround", "", attrs);*/
			}

			return costedFixes.Count;
		}

		protected int ApplyFirstLineFixToBizServiceUser(Node biz_service_user, out string reason, out string prevented_incident, out bool is_thermal, out bool is_power, Action action, bool isAutoFix)
		{
			return ApplyFixesToBizServiceUser(biz_service_user, out reason, out prevented_incident, out is_thermal, out is_power, action, isAutoFix);
		}

		protected int ApplyFixesToBizServiceUser(Node biz_service_user, out string reason, out string prevented_incident, out bool is_thermal, out bool is_power, Action action, bool isAutoFix)
		{
			is_thermal = false;
			is_power = false;
			reason = "";
			prevented_incident = "";
			int beenDownFor = biz_service_user.GetIntAttribute("downforsecs",999);

			global_incident_id = "";
			//
			// 28-03-2007 : If a mirrored item is fixed then we only cost the fix as one fix!
			//
			costedFixes.Clear();
			//
			ArrayList serviceConnections = biz_service_user.BackLinks;
			foreach(LinkNode l in serviceConnections)
			{
				bool _is_thermal;
				bool _is_power;
				ClimbTreeAndApplyAction((Node) l, action, isAutoFix, out _is_thermal, out _is_power);
				if(_is_thermal) is_thermal = true;
				if(_is_power) is_power = true;
			}
			//
			if(costedFixes.Count > 0)
			{
				foreach(string str in costedFixes)
				{
					if(reason.Length > 0) reason += "\r\n";
					reason += str;
				}
				/*
				ArrayList attrs = new ArrayList();
				attrs.Add( new AttributeValuePair("type","fix") );
				attrs.Add( new AttributeValuePair("desc",reason) );
				attrs.Add( new AttributeValuePair("incident_id",global_incident_id) );
				Node newCost = new Node(costedEvents, "fix", "", attrs);
				*/

				// : 17-5-2007
				// Case 2317:   Preventable incidents don't appear for 10 seconds
				// Case 2473:   Reduce time for prevented incidents to occur from 15 seconds to 10
				// If the service has been fixed and it's been down for 10 seconds
				// or less then mark as a prevented incident...

				//  27-05-2009
				// 6249: remove this ability altogether!
				if(beenDownFor < 0)
				{
					// : fix for 4937 (quick fixes to install penalties still count as
					// prevented incidents).
					bool isPenalty = false;

					if (global_incident_id != "")
					{
						IncidentDefinition incident = incidentApplier.GetIncident(global_incident_id);
						isPenalty = (incident != null) && incident.IsPenalty;
					}

					if (! isPenalty)
					{
						prevented_incident = global_incident_id;
					}
					/*
					attrs.Clear();
					attrs.Add( new AttributeValuePair("type","prevented_incident") );
					attrs.Add( new AttributeValuePair("incident_id",global_incident_id) );
					Node newCost2 = new Node(costedEvents, "prevented_incident", "", attrs);*/
				}
			}

			return costedFixes.Count;
		}

		void RegisterFix(string reason, string prevented_incidents, bool is_thermal, bool is_power)
		{
			ArrayList attrs = new ArrayList();
			attrs.Add(new AttributeValuePair("desc", reason));
			attrs.Add(new AttributeValuePair("incident_id", global_incident_id));

			if (is_thermal)
			{
				attrs.Add(new AttributeValuePair("type", "fix_thermal"));
				Node newCost = new Node(costedEvents, "fix_thermal", "", attrs);
			}
			else
			{
				attrs.Add(new AttributeValuePair("type", "fix"));
				Node newCost = new Node(costedEvents, "fix", "", attrs);
			}

			if (prevented_incidents != "")
			{
				// Don't register a prevented incident if it was a penalty.
				IncidentDefinition preventedIncident = incidentApplier.GetIncident(prevented_incidents);
				bool isPenalty = false;
				if (preventedIncident != null)
				{
					object attribute = preventedIncident._Attributes["penalty"];
					isPenalty = ((attribute != null) && (((string)attribute).ToLower() == "yes"));
				}

				if (!isPenalty)
				{
					attrs.Clear();
					attrs.Add(new AttributeValuePair("type", "prevented_incident"));
					attrs.Add(new AttributeValuePair("incident_id", prevented_incidents));
					Node newCost2 = new Node(costedEvents, "prevented_incident", "", attrs);
				}
			}
		}

		void RegisterFirstLineFix(string reason, string prevented_incidents, bool is_thermal, bool is_power)
		{
			ArrayList attrs = new ArrayList();
			attrs.Add(new AttributeValuePair("desc", reason));
			attrs.Add(new AttributeValuePair("incident_id", global_incident_id));

			if (is_thermal)
			{
				attrs.Add(new AttributeValuePair("type", "fix_first_line_thermal"));
				Node newCost = new Node(costedEvents, "fix_thermal", "", attrs);
			}
			else
			{
				attrs.Add(new AttributeValuePair("type", "first_line_fix"));
				Node newCost = new Node(costedEvents, "fix", "", attrs);
			}

			if (prevented_incidents != "")
			{
				// Don't register a prevented incident if it was a penalty.
				IncidentDefinition preventedIncident = incidentApplier.GetIncident(prevented_incidents);
				bool isPenalty = false;
				if (preventedIncident != null)
				{
					object attribute = preventedIncident._Attributes["penalty"];
					isPenalty = ((attribute != null) && (((string)attribute).ToLower() == "yes"));
				}

				if (!isPenalty)
				{
					attrs.Clear();
					attrs.Add(new AttributeValuePair("type", "prevented_incident"));
					attrs.Add(new AttributeValuePair("incident_id", prevented_incidents));
					Node newCost2 = new Node(costedEvents, "prevented_incident", "", attrs);
				}
			}
		}

		void RegisterAutomatedFix(string reason, string prevented_incidents, bool is_thermal, bool is_power)
		{
			ArrayList attrs = new ArrayList();
			attrs.Add(new AttributeValuePair("desc", reason));
			attrs.Add(new AttributeValuePair("incident_id", global_incident_id));

			if (is_thermal)
			{
				attrs.Add(new AttributeValuePair("type", "fix_automated_thermal"));
				Node newCost = new Node(costedEvents, "fix_thermal", "", attrs);
			}
			else
			{
				attrs.Add(new AttributeValuePair("type", "automated_fix"));
				Node newCost = new Node(costedEvents, "fix", "", attrs);
			}

			if (prevented_incidents != "")
			{
				// Don't register a prevented incident if it was a penalty.
				IncidentDefinition preventedIncident = incidentApplier.GetIncident(prevented_incidents);
				bool isPenalty = false;
				if (preventedIncident != null)
				{
					object attribute = preventedIncident._Attributes["penalty"];
					isPenalty = ((attribute != null) && (((string)attribute).ToLower() == "yes"));
				}

				if (!isPenalty)
				{
					attrs.Clear();
					attrs.Add(new AttributeValuePair("type", "prevented_incident"));
					attrs.Add(new AttributeValuePair("incident_id", prevented_incidents));
					Node newCost2 = new Node(costedEvents, "prevented_incident", "", attrs);
				}
			}
		}

		protected int ApplyFixByConsultancyToBizServiceUser(Node biz_service_user, out string reason, out bool is_thermal, out bool is_power, Action action)
		{
			is_thermal = false;
			is_power = false;
			reason = "";
			//
			// 28-03-2007 : If a mirrored item is fixed then we only cost the fix as one fix!
			//
			costedFixes.Clear();
			//
			ArrayList serviceConnections = biz_service_user.BackLinks;
			foreach(LinkNode l in serviceConnections)
			{
				bool _is_thermal;
				bool _is_power;
				ClimbTreeAndApplyAction((Node) l, action, false, out _is_thermal, out _is_power);
				if(_is_thermal) is_thermal = true;
				if(_is_power) is_power = true;
			}
			//
			if(costedFixes.Count > 0)
			{
				foreach(string str in costedFixes)
				{
					if(reason.Length > 0) reason += "\r\n";
					reason += str;
				}
				/*
				ArrayList attrs = new ArrayList();
				attrs.Add( new AttributeValuePair("type","fix_by_consultancy") );
				attrs.Add( new AttributeValuePair("desc",reason) );
				Node newCost = new Node(costedEvents, "fix_by_consultancy", "", attrs);*/
			}

			return costedFixes.Count;
		}

		void RegisterFixByConsultancy(string reason, bool is_thermal, bool is_power)
		{
			ArrayList attrs = new ArrayList();
			attrs.Add( new AttributeValuePair("desc",reason) );

			if(is_thermal)
			{
				attrs.Add( new AttributeValuePair("type","fix_thermal_by_consultancy") );
				attrs.Add(new AttributeValuePair ("incident_id", this.global_incident_id));
				Node newCost = new Node(costedEvents, "fix_thermal_by_consultancy", "", attrs);
			}
			else if (is_power)
			{
				attrs.Add( new AttributeValuePair("type","fix_power_by_consultancy") );
				attrs.Add(new AttributeValuePair ("incident_id", this.global_incident_id));
				Node newCost = new Node(costedEvents, "fix_power_by_consultancy", "", attrs);
			}
			else
			{
				attrs.Add( new AttributeValuePair("type","fix_by_consultancy") );
				Node newCost = new Node(costedEvents, "fix_by_consultancy", "", attrs);
			}
		}

		protected int ApplyWorkaroundsToBizServiceUser(Node biz_service_user, out string reason, out bool is_thermal, out bool is_power)
		{
			is_thermal = false;
			is_power = false;
			reason = "";
			//
			// 28-03-2007 : If a mirrored item is fixed then we only cost the fix as one fix!
			//
			costedFixes.Clear();
			//
			ArrayList serviceConnections = biz_service_user.BackLinks;
			foreach(LinkNode l in serviceConnections)
			{
				bool _is_thermal;
				bool _is_power;
				ClimbTreeAndApplyAction((Node) l, Action.WORKAROUND, false, out _is_thermal, out _is_power);
				if(_is_thermal) is_thermal = true;
			}
			//
			if(costedFixes.Count > 0)
			{
				foreach(string str in costedFixes)
				{
					if(reason.Length > 0) reason += "\r\n";
					reason += str;
				}
				/*
				ArrayList attrs = new ArrayList();
				attrs.Add( new AttributeValuePair("type","workaround") );
				attrs.Add( new AttributeValuePair("desc",reason) );
				Node newCost = new Node(costedEvents, "workaround", "", attrs);*/
			}

			return costedFixes.Count;
		}

		void RegisterWorkaround(string reason, bool is_thermal, bool is_power)
		{
			ArrayList attrs = new ArrayList();
			attrs.Add( new AttributeValuePair("desc",reason) );

			if(is_thermal)
			{
				attrs.Add( new AttributeValuePair("type","workaround_thermal") );
				Node newCost = new Node(costedEvents, "workaround_thermal", "", attrs);
			}
			else
			{
				attrs.Add( new AttributeValuePair("type","workaround") );
				Node newCost = new Node(costedEvents, "workaround", "", attrs);
			}
		}

		void fixItQueue_ChildAdded(Node sender, Node child)
		{
            //Request will now be fixed so increment counter
            Node fixedRequests = targetTree.GetNamedNode("FixedRequests");
            if (fixedRequests != null)
            {
                int? counter = fixedRequests.GetIntAttribute("fixedRequestsCounter");
                if (counter.HasValue)
                {
                    counter++;
                    fixedRequests.SetAttribute("fixedRequestsCounter", counter.Value);
                }                
            }

			string fixIncident = child.GetAttribute("incident_id") + "_fix";
			if (incidentApplier.GetIncident(fixIncident) != null)
			{
				incidentApplier.FireIncident(fixIncident);
				sender.DeleteChildTree(child);
				return;
			}

			fixItQueue.SetAttribute("fixing","true");

			bool isAutoRestoreFix = child.GetBooleanAttribute("is_auto_fix", false);
			//
			// A fix it or workaround command has arrived.
			//
			Action action = Action.FIX;
			switch (child.Type)
			{
				case "fix":
					action = Action.FIX;
					break;
				case "fix_by_consultancy":
				case "fix by consultancy":
					action = Action.FIX_BY_CONSULTANCY;
					break;
				case "automated":
					action = Action.AUTOMATED;
					break;
				case "workaround":
					action = Action.WORKAROUND;
					break;
				case "first_line_fix":
				case "first line fix":
					action = Action.FIRSTLINEFIX;
					break;
			}

			string target = child.GetAttribute("target");
			string incident_id = child.GetAttribute("incident_id");
			//Node tnode = null;
			//
			// Wipe the flag that we have fixed a workaround on this climb...
			//
			fixedAWorkAround = false;
			//
			ArrayList service_users_to_fix = new ArrayList();
			ArrayList thermal_nodes_to_fix = new ArrayList ();
			ArrayList thermal_servers_to_fix = new ArrayList ();
			ArrayList virtual_mirrored_apps_to_fix = new ArrayList ();
			ArrayList leftovers_to_fix = new ArrayList ();
			List<Node> servicesToFix = new List<Node> ();
			//
			if("" != target)
			{
				//tnode = this.targetTree.GetNamedNode(target);
				// 24-04-2007 : Grab the incident_id affecting this business service user and then
				// use that one as the incident id!
				Node n = this.targetTree.GetNamedNode(target);
				incident_id = n.GetAttribute("incident_id");
			}

			if("" != incident_id)
			{
				// This only works if we prevent overlapping incidents.
				// Find a business service that has the incident on it...
				// TODO : Can we make this work even if we allow overlapping incidents?
				ArrayList nodes = targetTree.GetNodesWithAttributeValue("incident_id", incident_id);
				foreach(Node n in nodes)
				{
					if (n.GetBooleanAttribute("fixable", true))
					{
						bool addedSomewhere = false;

						switch (n.GetAttribute("type").ToLower())
						{
							case "biz_service":
								servicesToFix.Add(n);
								addedSomewhere = true;
								if (n.GetIntAttribute("danger_level", 0) == 100)
								{
									n.SetAttribute("danger_level", 20);
								}
								break;

							case "biz_service_user":
								service_users_to_fix.Add(n);
								addedSomewhere = true;
								break;

								// : if we have a thermal incident that doesn't bring anything down,
								// we still need to be able to fix it.
							case "cooling":
								thermal_nodes_to_fix.Add(n);
								addedSomewhere = true;
								break;

							case "server":
								if (n.GetBooleanAttribute("thermal", false))
								{
									thermal_servers_to_fix.Add(n);
									addedSomewhere = true;
								}
								break;
						}

						if (n.GetBooleanAttribute("virtualmirrorinuse", false))
						{
							virtual_mirrored_apps_to_fix.Add(n);
							addedSomewhere = true;
						}

						if (! addedSomewhere)
						{
							leftovers_to_fix.Add(n);
						}
					}
				}

				// If there are some servers in thermal warning, but no cooling nodes, then
				// we want to fix the servers.  But only if we've not found any normal BSUs to fix.
				if ((thermal_nodes_to_fix.Count + service_users_to_fix.Count) == 0)
				{
					thermal_nodes_to_fix.AddRange(thermal_servers_to_fix);
				}
			}

			// : Implementing 3890 (can fix an incident before it's raised, which clears the relevant AWT
			// and records a prevented incident).
			if ((service_users_to_fix.Count + thermal_nodes_to_fix.Count + servicesToFix.Count) == 0)
			{
				// Find the targets of the incident.
				IncidentDefinition incident = incidentApplier.GetIncident(incident_id);
				int affected = 0;

				if (incident != null)
				{
					ArrayList targets = incident.GetTargets();

					// And reset their slots in the AWT (if they are active there).
					foreach (string targetName in targets)
					{
						Node node = targetTree.GetNamedNode(targetName);
						if (node != null)
						{
							int danger = node.GetIntAttribute("danger_level", -1);
							if (danger >= 33)
							{
								affected++;
								node.SetAttribute("danger_level", 20);
							}

							// : implementing 5278 (be able to fix a thermal incident before it happens).
							if (node.GetBooleanAttribute("thermal", false))
							{
								affected++;
								node.SetAttribute("thermal", "false");
							}

							// Implementing 4908 (HPDC: be able to fix a breaking thermal incident by
							// specifying the original thermal warning incident number).
							if (node.GetAttribute("type") == "Cooling")
							{
								// This incident affects a cooling node.
								if (node.GetAttribute("thermal") == "true")
								{
									// And the node is currently in overheat!
									thermal_nodes_to_fix.Add(node);
								}
							}
						}
					}
				}

				if (affected > 0)
				{
					if (! IsPenalty(child))
					{
						Node costedEventsNode = targetTree.GetNamedNode("CostedEvents");
						ArrayList attrs = new ArrayList();
						attrs.Add(new AttributeValuePair("type", "prevented_incident"));
						attrs.Add(new AttributeValuePair("id", incident_id));
						Node costedEvent = new Node(costedEventsNode, "incident", "", attrs);
					}
				}
			}

			// Not an else branch of the if above, because the code above can change thermal_nodes_to_fix.
			if ((service_users_to_fix.Count + thermal_nodes_to_fix.Count + virtual_mirrored_apps_to_fix.Count + leftovers_to_fix.Count + servicesToFix.Count) > 0)
			{
				// The incident has already been raised, so fix it normally.
				//
				string reason = "";
				int countFixed = 0;
				string prevented_incidents = "";
				//
				bool is_thermal = false;
				bool is_power = false;

				// If there are any overheating cooling nodes, then don't fix the ops incidents this time.
				if (thermal_nodes_to_fix.Count > 0)
				{
					foreach (Node tnode in thermal_nodes_to_fix)
					{
						is_thermal = true;

						bool fix = false;
						bool consultancy = false;

						switch (action)
						{
							case Action.FIX:
								fix = true;
								break;

							case Action.FIX_BY_CONSULTANCY:
								fix = true;
								consultancy = true;
								break;

							case Action.FIRSTLINEFIX:
								fix = true;
								break;

							case Action.AUTOMATED:
								fix = true;
								break;
						}

						if (fix)
						{
							ArrayList attrs = new ArrayList ();

							attrs.Add(new AttributeValuePair ("thermal", "false"));
							attrs.Add(new AttributeValuePair ("nopower", "false"));
							attrs.Add(new AttributeValuePair ("incident_id", ""));

							string baselineTemperature = "72.3";
							Node coolingNode = targetTree.GetNamedNode("C" + tnode.GetAttribute("zone"));
							if (coolingNode != null)
							{
								baselineTemperature = coolingNode.GetAttribute("baseline_temperature");
							}
							attrs.Add(new AttributeValuePair ("goal_temperature", baselineTemperature));

							if (consultancy)
							{
								attrs.Add(new AttributeValuePair ("goal_temperature_change_duration", CONVERT.ToStr(60 * 3)));
							}
							else
							{
								attrs.Add(new AttributeValuePair ("goal_temperature_change_duration", "5"));
							}

							tnode.SetAttributes(attrs);

							countFixed++;

							// Remove the thermal component from all servers in this zone, so they
							// can be fixed in ops next time.
							string zone = tnode.GetAttribute("name").Substring(1);
							ArrayList affectedServers = tnode.Tree.GetNodesWithAttributeValue("zone", zone);
							foreach (Node snode in affectedServers)
							{
								ArrayList serverAttrs = new ArrayList ();
								serverAttrs.Add(new AttributeValuePair ("fixable", "true"));
								serverAttrs.Add(new AttributeValuePair ("canworkaround", "true"));
								serverAttrs.Add(new AttributeValuePair ("thermal", "false"));

								snode.SetAttributes(serverAttrs);
							}
						}
					}

					// Now we've fixed the cooling nodes, tag the broken service nodes as fixable.
					foreach (Node tnode in service_users_to_fix)
					{
						ArrayList attrs = new ArrayList ();
						attrs.Add(new AttributeValuePair ("fixable", "true"));
						attrs.Add(new AttributeValuePair ("canworkaround", "true"));
						attrs.Add(new AttributeValuePair ("thermal", "false"));

						tnode.SetAttributes(attrs);
					}
				}
				else
				{
					foreach(Node tnode in service_users_to_fix)
					{
						bool _is_thermal = false;
						bool _is_power = false;
						//
						if (action == Action.FIX)
						{
							string prevented_incident;
							countFixed += ApplyFixesToBizServiceUser(tnode, out reason, out prevented_incident, out _is_thermal, out _is_power, action, isAutoRestoreFix);
							if(prevented_incidents.IndexOf(prevented_incident) == -1)
							{
								if(prevented_incidents != "") prevented_incidents += ",";
								prevented_incidents += prevented_incident;
							}
						}
						else if (action == Action.FIX_BY_CONSULTANCY)
						{
							countFixed += ApplyFixByConsultancyToBizServiceUser(tnode, out reason, out _is_thermal, out _is_power, action);
						}
						else if(action == Action.WORKAROUND)
						{
							// Run up dependency paths putting all fixable incidents that are not in workaround into
							// workaround.
							countFixed += ApplyWorkaroundsToBizServiceUser(tnode, out reason, out _is_thermal, out _is_power);
						}
						else if (action == Action.FIRSTLINEFIX || action == Action.AUTOMATED)
						{
							string prevented_incident;
							countFixed += ApplyFirstLineFixToBizServiceUser(tnode, out reason, out prevented_incident, out _is_thermal, out _is_power, action, isAutoRestoreFix);

							if(prevented_incidents.IndexOf(prevented_incident) == -1)
							{
								if(prevented_incidents != "") prevented_incidents += ",";
								prevented_incidents += prevented_incident;
							}
						}
						if(_is_thermal) is_thermal = true;
						if (_is_power) is_power = true;
					}

                    if (SkinningDefs.TheInstance.GetBoolData("apply_actions_to_virtual_mirrored_apps", true))
                    {
                        ArrayList onesToFix = virtual_mirrored_apps_to_fix;
                        if (onesToFix.Count == 0)
                        {
                            onesToFix = leftovers_to_fix;
                        }

                        onesToFix.AddRange(servicesToFix);

                        foreach (Node tnode in onesToFix)
                        {
                            bool _is_thermal = false;
                            bool _is_power = false;

                            switch (action)
                            {
                                case Action.FIX:
                                case Action.FIRSTLINEFIX:
                                    string prevented_incident;
                                    countFixed += ApplyFixesToVirtualMirroredApp(tnode, out reason, out prevented_incident, out _is_thermal, out _is_power, action);
                                    if (prevented_incidents.IndexOf(prevented_incident) == -1)
                                    {
                                        if (prevented_incidents != "") prevented_incidents += ",";
                                        prevented_incidents += prevented_incident;
                                    }
                                    break;

                                case Action.FIX_BY_CONSULTANCY:
                                    countFixed += ApplyFixByConsultancyToVirtualMirroredApp(tnode, out reason, out _is_thermal, out _is_power, action);
                                    break;

                                case Action.WORKAROUND:
                                    countFixed += ApplyWorkaroundsToVirtualMirroredApp(tnode, out reason, out _is_thermal, out _is_power);
                                    break;
                            }

                            if (_is_thermal)
                            {
                                is_thermal = true;
                            }

                            if (_is_power)
                            {
                                is_power = true;
                            }
                        }
                    }
					
				}

				//
				if(countFixed > 0)
				{
					if (action == Action.FIX)
					{
						// : Fix so the incident ID actually gets applied to the costed event
						// This might apply generally, but to prevent risk of breaking anything,
						// only do it for thermal fixes (which is when we really need it).
						if (is_thermal && (incident_id != ""))
						{
							global_incident_id = incident_id;
						}
						RegisterFix(reason, prevented_incidents, is_thermal, is_power);

					}
					else if (action == Action.FIX_BY_CONSULTANCY)
					{
						RegisterFixByConsultancy(reason, is_thermal, is_power);
					}
					else if(action == Action.WORKAROUND)
					{
						RegisterWorkaround(reason, is_thermal, is_power);
					}
					else if (action == Action.FIRSTLINEFIX)
					{
						if ((incident_id != ""))
						{
							global_incident_id = incident_id;
						}
						RegisterFirstLineFix(reason, prevented_incidents, is_thermal, is_power);
					}
					else if (action == Action.AUTOMATED)
					{
						if ((incident_id != ""))
						{
							global_incident_id = incident_id;
						}
						RegisterAutomatedFix(reason, prevented_incidents, is_thermal, is_power);
					}
				}
				//
				if(fixedAWorkAround)
				{
					// - 29-04-2007 : Need to let reports know that an event occured against
					// a particular business service user...
					foreach(Node tnode in service_users_to_fix)
					{
						ArrayList attrs = new ArrayList();
						attrs.Add( new AttributeValuePair("target",tnode.GetAttribute("name")) );
						attrs.Add( new AttributeValuePair( "type", "workaround fixed" ) );
						Node newCost = new Node(costedEvents, "workaround fixed", "", attrs);
						//
						tnode.SetAttribute("fixedduringworkaround","true");
					}
					//
				}
				//
				if(action == Action.WORKAROUND)
				{
					// Increment the workaround counter!
					if(null != workAroundCounter)
					{
						string num = workAroundCounter.GetAttribute("num");
						if(num != "")
						{
							int num_t = CONVERT.ParseInt(num);
							++num_t;
							workAroundCounter.SetAttribute("num",num_t);
						}
					}
				}
			}

			// Now cancel any pending breakages that are marked as being suitable to cancel when
			// the matching incident number is fixed.
			if (incident_id != "")
			{
				Hashtable events = GlobalEventDelayer.TheInstance.Delayer.GetAllFutureIDefEvents();
				foreach (ModelActionBase incident in events.Keys)
				{
					if (CONVERT.ToStr(incident.cancelWithIncident) == incident_id)
					{
						GlobalEventDelayer.TheInstance.Delayer.RemoveEvent(incident);
					}
				}
			}

			child.Parent.DeleteChildTree(child);

			fixItQueue.SetAttribute("fixing","false");
		}

		protected virtual void OnNodeFinishedWorkAround (LinkNode bsuLink)
		{
		}

		void currentTimeNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			foreach(AttributeValuePair avp in attrs)
			{
				if(avp.Attribute == "seconds")
				{
					ArrayList finishedNodes = new ArrayList();

					foreach(Node n in workingAroundNodes)
					{
						int was_t = n.GetIntAttribute("workingAround",0);
						if(was_t == 0)
						{
							// Workaround has finished working.
							finishedNodes.Add(n);
						}
						else
						{
							// Countdown to workaround failing.
							if(--was_t <= 0)
							{
								was_t = 0;
								ArrayList _attrs = new ArrayList();
								_attrs.Add(new AttributeValuePair("up","false") );
								_attrs.Add(new AttributeValuePair("workingAround","0") );
								
								OnNodeFinishedWorkAround(n as LinkNode);

                                if (!n.GetBooleanAttribute("workaround_was_up_online", true))
                                {
                                    _attrs.Add(new AttributeValuePair("up_online", false));
                                }

                                if (!n.GetBooleanAttribute("workaround_was_up_instore", true))
                                {
                                    _attrs.Add(new AttributeValuePair("up_instore", false));
                                }

								n.SetAttributes(_attrs);
								finishedNodes.Add(n);
							}
							else
							{
								n.SetAttribute("workingAround",was_t);
							}
						}
					}
					// Clear finished nodes.
					foreach(Node n in finishedNodes)
					{
						if(workingAroundNodes.Contains(n))
						{
							workingAroundNodes.Remove(n);
						}
					}
				}
			}
		}
	}
}