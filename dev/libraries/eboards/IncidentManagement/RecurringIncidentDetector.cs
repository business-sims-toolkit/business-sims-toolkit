using System.Collections;
using LibCore;
using Network;

namespace IncidentManagement
{
	/// <summary>
	/// Summary description for RecurringIncidentDetector.
	/// </summary>
	public class RecurringIncidentDetector
	{
		protected Hashtable watchedNodes;
		protected NodeTree _model;

		protected Hashtable previousIncidents = new Hashtable();

		protected Node costedEventsNode;

		protected void WipeWatchedNodes()
		{
			foreach(Node n in watchedNodes.Keys)
			{
				n.AttributesChanged -= n_AttributesChanged;
				n.Deleting -= n_Deleting;
			}
			//
			watchedNodes.Clear();
		}
		
		public RecurringIncidentDetector(NodeTree model)
		{
			_model = model;
			ArrayList wantedNodes = new ArrayList();
			wantedNodes.Add("Server");
			wantedNodes.Add("App");
			wantedNodes.Add("Hub");
			wantedNodes.Add("Router");
			wantedNodes.Add("Connection");
			wantedNodes.Add("SupportTech");
			watchedNodes = model.GetNodesOfAttribTypes(wantedNodes);
			foreach(Node n in watchedNodes.Keys)
			{
				n.AttributesChanged += n_AttributesChanged;
				n.Deleting += n_Deleting;
			}
			//
			model.NodeAdded += model_NodeAdded;

			costedEventsNode = model.GetNamedNode("CostedEvents");
		}

		public void Dispose()
		{
			WipeWatchedNodes();
			//
			_model.NodeAdded -= model_NodeAdded;
		}

		protected void AddWatchedNode(Node n, string type)
		{
			this.watchedNodes.Add(n,type);
			n.AttributesChanged += n_AttributesChanged;
			n.Deleting += n_Deleting;
		}

		protected void RemoveWatchedNode(Node n)
		{
			n.AttributesChanged -= n_AttributesChanged;
			n.Deleting -= n_Deleting;
		}
		
		protected virtual void n_AttributesChanged(Node sender, ArrayList attrs)
		{
			Node effectiveNodeDown = sender;
			int numDown = 1;
			string incident_id = "";

			bool penalty = false;
			foreach (AttributeValuePair avp in attrs)
			{
				if (avp.Attribute.ToLower() == "penalty")
				{
					penalty = (avp.Value.ToLower() == "yes");
				}
			}

			string thermal = "";
			if (sender.GetBooleanAttribute("thermal", false))
			{
				thermal = ";thermal";
			}

			string nopower = "";
			if (sender.GetBooleanAttribute("nopower", false))
			{
				nopower = ";nopower";
			}

			foreach(AttributeValuePair avp in attrs)
			{
				if(avp.Attribute == "incident_id")
				{
					incident_id = avp.Value;

					if("" != incident_id)
					{
						// Find out what we should set as the number of biz services affected and what
						// the effective node down is...
						string type = sender.GetAttribute("type");
						if("App" == type)
						{
							numDown = 0;
							foreach(Node child in sender)
							{
								if(child.GetAttribute("type") == "Connection")
								{
									++numDown;
								}
								else if(child.GetAttribute("type") == "SupportTech")
								{
									numDown += child.getChildren().Count;
								}
							}
						}
						else if("SupportTech" == type)
						{
							effectiveNodeDown = sender.Parent;
							numDown = sender.getChildren().Count;
						}
						else if("Connection" == type)
						{
							effectiveNodeDown = sender.Parent;
							if(effectiveNodeDown.GetAttribute("type") == "SupportTech")
							{
								effectiveNodeDown = effectiveNodeDown.Parent;
							}
						}

						string record = CONVERT.ToStr(numDown) + thermal + nopower;

						// Is this a recurring incident?
						ArrayList numDownArray = null;
						if(previousIncidents.ContainsKey(effectiveNodeDown))
						{
							// Yes, So check to see if the same number down occured...
							numDownArray = (ArrayList) previousIncidents[effectiveNodeDown];
							if(numDownArray.Contains(record))
							{
								// This is a recurring incident!

								// : fix for 4571 (install penalties shouldn't count as recurring incidents).
								if (! penalty)
								{
									ArrayList attrs2 = new ArrayList();
									attrs2.Add(new AttributeValuePair("type","recurring_incident") );
									attrs2.Add(new AttributeValuePair("incident_id",incident_id) );
									Node ce = new Node(costedEventsNode,"recurring_incident","",attrs2);
									//
									return;
								}
							}
						}
						else
						{
							// No.
							numDownArray = new ArrayList();
							previousIncidents.Add(effectiveNodeDown, numDownArray);
						}

						numDownArray.Add(record);
					}
				}
			}
		}

		void n_Deleting(Node sender)
		{
			RemoveWatchedNode(sender);
			this.watchedNodes.Remove(sender);
		}

		void model_NodeAdded(NodeTree sender, Node newNode)
		{
			string type = newNode.GetAttribute("type");

			if( (type == "App") || (type == "Server") || (type == "Connection") ||
				(type == "SupportTech") || (type == "Hub") || (type == "Router") )
			{
				AddWatchedNode(newNode, type);
			}
		}
	}
}
