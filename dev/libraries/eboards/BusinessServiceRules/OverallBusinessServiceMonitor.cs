using System.Collections;
using LibCore;
using Network;

namespace BusinessServiceRules
{
	/// <summary>
	/// An OverallBusinessServiceMonitor monitors a business service that has a series of business service
	/// users. If any one of the business service users go down then the overall business service is marked
	/// as being down / having problems.
	/// </summary>
	public class OverallBusinessServiceMonitor
	{
		Node _businessServiceNode;
		Hashtable linksTo_Biz_ServiceUserNodes;

		Hashtable nodeToWorkingAroundTime = new Hashtable();

		public OverallBusinessServiceMonitor(Node businessServiceNode)
		{
			_businessServiceNode = businessServiceNode;
			linksTo_Biz_ServiceUserNodes = new Hashtable();

			string incident_id = "";

			foreach(Node child in _businessServiceNode)
			{
				if(child.GetAttribute("type") == "Connection")
				{
					string _incident_id = "";
					SetupBusinessMonitor(child, ref _incident_id);
					if("" != _incident_id)
					{
						incident_id = _incident_id;
					}
				}
			}

			_businessServiceNode.ChildAdded += _businessServiceNode_ChildAdded;
			_businessServiceNode.ChildRemoved += _businessServiceNode_ChildRemoved;

			RefreshStatus(true,true,true,true,true,true, incident_id);
			//UpdateWorkingAround();
		}

		void SetupBusinessMonitor(Node child, ref string incident_id)
		{
			Node bizUser = child.Tree.GetNamedNode( child.GetAttribute("to") );
			linksTo_Biz_ServiceUserNodes.Add(child,bizUser);
			incident_id = bizUser.GetAttribute("incident_id");
			bizUser.AttributesChanged += bizUser_AttributesChanged;
			//
			int secs = bizUser.GetIntAttribute("workingAround", 0);
			if(secs > 0)
			{
				nodeToWorkingAroundTime[child] = secs;
			}
		}

		public void Dispose()
		{
			_businessServiceNode.ChildAdded -= _businessServiceNode_ChildAdded;
			_businessServiceNode.ChildRemoved -= _businessServiceNode_ChildRemoved;

			foreach(Node n in linksTo_Biz_ServiceUserNodes.Values)
			{
				n.AttributesChanged -= bizUser_AttributesChanged;
			}

			linksTo_Biz_ServiceUserNodes.Clear();
		}

		void _businessServiceNode_ChildAdded(Node sender, Node child)
		{
			//need to ignore dummy nodes 
			string nodetype = child.GetAttribute("type");

			//only concerned about actual Connection
			if(nodetype == "Connection")
			{
			//if (nodetype != "dummy")
			//{
				string incident_id = "";
				SetupBusinessMonitor(child, ref incident_id);

				// Refresh status...
				foreach(Node n in linksTo_Biz_ServiceUserNodes.Values)
				{
					string _incident_id = n.GetAttribute("incident_id");
					if(_incident_id != "")
					{
						incident_id = _incident_id;
					}
				}

				RefreshStatus(true,true,true,true,true,true, incident_id);
			}
		}

		void bizUser_AttributesChanged(Node sender, ArrayList attrs)
		{
			bool refreshStatus = false;
			bool refreshUpStatus = false;
			bool refreshBootingStatus = false;
			bool refreshUpByMirrorStatus = false;
			bool refreshUpByVirtualMirrorStatus = false;
			bool refresh_slabreach = false;
			bool refresh_incident_id = false;

			string incident_id = "";

			foreach(AttributeValuePair avp in attrs)
			{
				//
				if(avp.Attribute == "fixedduringworkaround")
				{
					if(avp.Value.ToLower() == "true")
					{
						// - 29-04-2007 : Need to let reports know that an event occured against
						// a particular business service user...
						Node ce = _businessServiceNode.Tree.GetNamedNode("CostedEvents");
						ArrayList attrs2 = new ArrayList();
						attrs2.Add( new AttributeValuePair("target",_businessServiceNode.GetAttribute("name")) );
						attrs2.Add( new AttributeValuePair("biz_service_function",_businessServiceNode.GetAttribute("biz_service_function")) );
						attrs2.Add( new AttributeValuePair( "type", "workaround fixed" ) );
						Node newCost = new Node(ce, "workaround fixed", "", attrs2);

						sender.SetAttribute("fixedduringworkaround","");
					}
				}
				else if(avp.Attribute == "rebootingForSecs")
				{
					if(_businessServiceNode.GetAttribute("rebootingForSecs") != avp.Value 
						&& !_businessServiceNode.GetBooleanAttribute("dont_show_rebooting", false))
					{
						refreshBootingStatus = true;
					}
				}
				else if(avp.Attribute == "slabreach")
				{
					if(_businessServiceNode.GetAttribute("slabreach") != avp.Value)
					{
						refresh_slabreach= true;
					}
				}
				else if(avp.Attribute == "incident_id")
				{
					if(refresh_incident_id || (_businessServiceNode.GetAttribute("incident_id") != avp.Value))
					{
						refresh_incident_id = true;
						incident_id = avp.Value;
					}
				}
				else if( ( (avp.Attribute == "upOnlyByMirror") || (avp.Attribute == "upByMirror") )
					&& !_businessServiceNode.GetBooleanAttribute("ignore_down_by_mirror", false))
				{
					refreshUpByMirrorStatus = true;
				}
				else if ((avp.Attribute == "virtualmirrored") || (avp.Attribute == "virtualmirrorinuse"))
				{
					refreshUpByVirtualMirrorStatus = true;

					if (avp.Value.ToLower() == "true")
					{
						refresh_incident_id = true;
					}
				}
				else if( (avp.Attribute == "up") || (avp.Attribute == "downforsecs")|| (avp.Attribute == "mirrorforsecs")
					|| (avp.Attribute == "fixable") || (avp.Attribute == "canworkaround") )
				{
					refreshUpStatus = true;
				}
				else if (avp.Attribute == "thermal")
				{
					refreshStatus = true;
					refreshUpStatus = true; // Don't really need to refresh up status, but want to recheck thermal!
				}
				else if(avp.Attribute == "workingAround")
				{
					if( (avp.Value == "") || (avp.Value == "0"))
					{
						// Remove this node from working around if we have it already.
						if(nodeToWorkingAroundTime.ContainsKey(sender))
						{
							nodeToWorkingAroundTime.Remove(sender);
							refreshUpStatus = true;
						}
					}
					else
					{
						// Add this node if we don't have it yet...
						if(!nodeToWorkingAroundTime.ContainsKey(sender))
						{
							nodeToWorkingAroundTime.Add(sender, CONVERT.ParseInt(avp.Value));
						}
						else
						{
							// Update the value in our hashtable...
							nodeToWorkingAroundTime[sender] = CONVERT.ParseInt(avp.Value);
						}
					}
					//
					// !!!! We could check further that we need to do this...
					refreshStatus = true;
				}
			}

			if(refreshUpStatus || refreshStatus || refreshBootingStatus || refreshUpByMirrorStatus || refreshUpByVirtualMirrorStatus
				|| refresh_slabreach || refresh_incident_id)
			{
				RefreshStatus(refreshUpStatus, refreshBootingStatus, refreshUpByMirrorStatus, refreshUpByVirtualMirrorStatus,
					refresh_slabreach, refresh_incident_id, incident_id);
			}
		}
		/// <summary>
		/// RefreshStatus should enumerate over all the business service users that are connected to
		/// this business and collate all information up so that it updates the main business service
		/// node correctly.
		/// </summary>
		/// <param name="refreshUpStatus"></param>
		/// <param name="refreshBootingStatus"></param>
		/// <param name="refreshUpByMirrorStatus"></param>
		/// <param name="refresh_slabreach"></param>
		/// <param name="refresh_incident_id"></param>
		/// <param name="incident_id"></param>
		protected void RefreshStatus(bool refreshUpStatus, bool refreshBootingStatus,
			bool refreshUpByMirrorStatus, bool refreshUpByVirtualMirrorStatus, bool refresh_slabreach, bool refresh_incident_id, string incident_id)
		{
			ArrayList attrs = new ArrayList();
			bool up = true;
			bool bubbleSLAbreach = false;

			ArrayList stuffDown = new ArrayList();
			string fixable = "false";

			string name = _businessServiceNode.GetAttribute("name");

			//System.Collections.Specialized.StringDictionary attributeStates = new StringDictionary();

			if(refreshUpStatus || refreshBootingStatus)
			{
				int downforsecs = 0;
				int mirrorforsecs = 0;
				bool canworkaround = false;
				bool denial_of_service = false;
				bool security_flaw = false;
				bool compliance_incident = false;
				bool thermal = false;
				bool power = false;

				foreach(Node n in linksTo_Biz_ServiceUserNodes.Values)
				{
					name = n.GetAttribute("name");

					if(! n.GetBooleanAttribute("up", false))
					{
						up = false;
						//
						// Also check to see if we have to bubble up the slabreach status.
						//
						if(n.GetBooleanAttribute("slabreach", false))
						{
							bubbleSLAbreach = true;
						}
						//
						// Following line assumes that Cars, Terminals and Shops always end with their number...
						//
						string entity = n.Parent.GetAttribute("name");
						entity = entity.Substring( entity.Length-1, 1 );
						//
						if(!stuffDown.Contains(entity))
						{
							stuffDown.Add(entity);
						}
					}
					else if(n.GetIntAttribute("workingaround",0) != 0)
					{
						string entity = n.Parent.GetAttribute("name");
						entity = entity.Substring( entity.Length-1, 1 );
						//
						if(!stuffDown.Contains(entity))
						{
							stuffDown.Add(entity);
						}
						//
					}

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

					if(n.GetBooleanAttribute("thermal", false))
					{
						thermal = true;
					}
					if (n.GetBooleanAttribute("nopower", false))
					{
						power = true;
					}

					if(n.GetBooleanAttribute("fixable", false))
					{
						fixable = "true";
					}
					int _dfs = n.GetIntAttribute("downforsecs",-1);
					if(_dfs != -1)
					{
						if(_dfs > downforsecs) downforsecs = _dfs;
					}

					int _mfs = n.GetIntAttribute("mirrorforsecs",-1);
					if(_mfs != -1)
					{
						if(_mfs > mirrorforsecs) mirrorforsecs = _mfs;
					}

					if(n.GetBooleanAttribute("canworkaround", false))
					{
						canworkaround = true;
					}
					if( n.GetBooleanAttribute("upOnlyByMirror", false) || n.GetBooleanAttribute("upByMirror", false))
					{
						string entity = n.Parent.GetAttribute("name");
						entity = entity.Substring( entity.Length-1, 1 );
						//
						if(!stuffDown.Contains(entity))
						{
							stuffDown.Add(entity);
						}
					}
				}

				if(denial_of_service)
				{
					AttributeValuePair.AddIfNotEqual(_businessServiceNode, attrs, "denial_of_service", "true");
				}
				else
				{
					AttributeValuePair.AddIfNotEqual(_businessServiceNode, attrs, "denial_of_service", "false");
				}
				// security_flaw
				if (security_flaw)
				{
					AttributeValuePair.AddIfNotEqual(_businessServiceNode, attrs, "security_flaw", "true");
				}
				else
				{
					AttributeValuePair.AddIfNotEqual(_businessServiceNode, attrs, "security_flaw", "false");
				}

				if (compliance_incident)
				{
					AttributeValuePair.AddIfNotEqual(_businessServiceNode, attrs, "compliance_incident", "true");
				}
				else
				{
					AttributeValuePair.AddIfNotEqual(_businessServiceNode, attrs, "compliance_incident", "false");
				}

				if(thermal)
				{
					AttributeValuePair.AddIfNotEqual(_businessServiceNode, attrs, "thermal", "true");
				}
				else
				{
					AttributeValuePair.AddIfNotEqual(_businessServiceNode, attrs, "thermal", "false");
				}

				if (power)
				{
					AttributeValuePair.AddIfNotEqual(_businessServiceNode, attrs, "nopower", "true");
				}
				else
				{
					AttributeValuePair.AddIfNotEqual(_businessServiceNode, attrs, "nopower", "false");
				}
					
				if(_businessServiceNode.GetBooleanAttribute("canworkaround", false) != canworkaround)
				{
					AttributeValuePair.AddIfNotEqual(_businessServiceNode, attrs, "canworkaround", canworkaround);
				}

				if(_businessServiceNode.GetIntAttribute("downforsecs",0) != downforsecs)
				{
					AttributeValuePair.AddIfNotEqual(_businessServiceNode, attrs, "downforsecs", downforsecs);
				}

				if(_businessServiceNode.GetIntAttribute("mirrorforsecs",0) != mirrorforsecs)
				{
					AttributeValuePair.AddIfNotEqual(_businessServiceNode, attrs, "mirrorforsecs", mirrorforsecs);
				}

				stuffDown.Sort();

				string stuffDownString = "";
				foreach(string str in stuffDown)
				{
					if("" != stuffDownString)
					{
						stuffDownString += ",";
					}
					stuffDownString += str;
				}
				// : Fix for 4064: update users_down always, even if there are zero of them now
				// (since otherwise it will stick on the last value).
				AttributeValuePair.AddIfNotEqual(_businessServiceNode, attrs, "users_down", stuffDownString);
				AttributeValuePair.AddIfNotEqual(_businessServiceNode, attrs, "up", up);

				if (up)
				{
					AttributeValuePair.AddIfNotEqual(_businessServiceNode, attrs, "klaxon_triggered", false);
				}
			}

			if(bubbleSLAbreach)
			{
				AttributeValuePair.AddIfNotEqual(_businessServiceNode,attrs, "slabreach", "true");
			}

			if(refreshUpStatus || refreshBootingStatus)
			{
				if("true" == fixable.ToLower())
				{
					AttributeValuePair.AddIfNotEqual(_businessServiceNode,attrs, "fixable", "true");
				}
				else
				{
					AttributeValuePair.AddIfNotEqual(_businessServiceNode,attrs, "fixable", "false");
				}
			}

			if(refresh_incident_id)
			{
				AttributeValuePair.AddIfNotEqual(_businessServiceNode,attrs, "incident_id", incident_id);
			}

			if(refresh_slabreach)
			{
				bool slabreach = false;

				foreach(Node n in linksTo_Biz_ServiceUserNodes.Values)
				{
					if(n.GetBooleanAttribute("slabreach", false))
					{
						slabreach = true;
					}
				}

				if(slabreach)
				{
					if(_businessServiceNode.GetAttribute("slabreach") != "true")
					{
						attrs.Add(new AttributeValuePair("slabreach","true"));
						//
						// Extract the various values.
						//
						Node node_breach = _businessServiceNode.Tree.GetNamedNode("SLA_Breach");
						int breach_bs_count = node_breach.GetIntAttribute("biz_serv_count",0);
						//
						// Update the various values.
						//
						breach_bs_count++;
						node_breach.SetAttribute("biz_serv_count",CONVERT.ToStr(breach_bs_count));
					}
				}
				else
				{
					AttributeValuePair.AddIfNotEqual(_businessServiceNode,attrs, "slabreach", "");
				}
			}

			if(refreshUpByMirrorStatus)
			{
				bool upByMirror = false;

				foreach(Node n in linksTo_Biz_ServiceUserNodes.Values)
				{
					if(n.GetBooleanAttribute("upByMirror", false))
					{
						upByMirror = true;
					}
				}

				if(upByMirror)
				{
					if (! _businessServiceNode.GetBooleanAttribute("upByMirror", false))
					{
						attrs.Add(new AttributeValuePair ("upByMirror", true));
					}
				}
				else
				{
					if (_businessServiceNode.GetBooleanAttribute("upByMirror", true))
					{
						attrs.Add(new AttributeValuePair("upByMirror","false"));
					}
				}
			}

			if (refreshUpByVirtualMirrorStatus)
			{
				bool upByVirtualMirror = false;

				foreach (Node n in linksTo_Biz_ServiceUserNodes.Values)
				{
					if (n.GetBooleanAttribute("virtualmirrorinuse", false))
					{
						upByVirtualMirror = true;
					}
				}

				if (upByVirtualMirror)
				{
					if (! _businessServiceNode.GetBooleanAttribute("virtualmirrorinuse", false))
					{
						attrs.Add(new AttributeValuePair ("virtualmirrorinuse", "true"));
					}
				}
				else
				{
					if (_businessServiceNode.GetBooleanAttribute("virtualmirrorinuse", false))
					{
						attrs.Add(new AttributeValuePair ("virtualmirrorinuse", "false"));
					}
				}
			}

			if(refreshBootingStatus)
			{
				int rebootingForSecs = 0;

				foreach(Node n in linksTo_Biz_ServiceUserNodes.Values)
				{
					int rfs = n.GetIntAttribute("rebootingForSecs",0);

					if(rebootingForSecs < rfs)
					{
						rebootingForSecs = rfs;
					}
				}

				if(rebootingForSecs == 0)
				{
					if(_businessServiceNode.GetAttribute("rebootingForSecs") != "")
					{
						attrs.Add(new AttributeValuePair("rebootingForSecs",""));
					}
				}
				else if(rebootingForSecs != _businessServiceNode.GetIntAttribute("rebootingForSecs",0))
				{
					attrs.Add(new AttributeValuePair("rebootingForSecs", rebootingForSecs));
				}
			}

			int lowestWorkAroundTime = int.MaxValue;
			//
			foreach(int secs in nodeToWorkingAroundTime.Values)
			{
				if(secs < lowestWorkAroundTime) lowestWorkAroundTime = secs;
			}
			//
			if(nodeToWorkingAroundTime.Keys.Count == 0)
			{
				int curWA = _businessServiceNode.GetIntAttribute("workingAround",0);
				if(curWA != 0)
				{
					attrs.Add(new AttributeValuePair("workingAround","0"));
				}
			}
			else
			{
				// Always refresh upOnlyByMirror for the reports depend on this behaviour for now...
				attrs.Add(new AttributeValuePair("workingAround",CONVERT.ToStr(lowestWorkAroundTime)));
			}

			if(attrs.Count > 0)
			{
				_businessServiceNode.SetAttributes(attrs);
			}
		}

		void _businessServiceNode_ChildRemoved(Node sender, Node child)
		{
			//need to ignore dummy nodes 
			string nodetype = child.GetAttribute("type");
			if (nodetype.ToLower() != "dummy")
			{
				if (linksTo_Biz_ServiceUserNodes.ContainsKey(child))
				{
					Node bizUser = (Node) linksTo_Biz_ServiceUserNodes[child];
					bizUser.AttributesChanged -= bizUser_AttributesChanged;
					linksTo_Biz_ServiceUserNodes.Remove(child);
				}
			}
		}
	}
}