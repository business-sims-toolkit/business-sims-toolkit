using System;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml;

using LibCore;
using Network;

namespace IncidentManagement
{
	/// <summary>
	/// Summary description for IncidentDefinition.
	/// </summary>
	[DebuggerDisplay("Id = {_id}")]
	public class IncidentDefinition : ModelActionBase
	{
		string _id = "";
		string _description = "";
		string _type = "";
		bool _allow_overlap = false;
		bool isPenalty = false;
		public bool IsPenalty => isPenalty;

		public Hashtable _Attributes = new Hashtable();

        public Dictionary<string, string> Attributes
        {
            get
            {
                return _Attributes.Cast<DictionaryEntry>()
                    .ToDictionary(kvp => (string) kvp.Key, kvp => (string) kvp.Value);
            }
        }

		/// <summary>
		/// Incident definitions that are marked as allow_overlap="true" will allow th incident
		/// to proceed even if there are overlapping incidents.
		/// </summary>
		public bool AllowOverlap => _allow_overlap;

		// An array of actions to apply when this incident fires.
		protected ArrayList incidentActions = new ArrayList();

		public override object Clone()
		{
			IncidentDefinition inc = new IncidentDefinition();
			inc._id = _id;
			inc._description = _description;
			inc._type = _type;
			inc._allow_overlap = _allow_overlap;
			//
			foreach(ModelActionBase idef in incidentActions)
			{
				inc.incidentActions.Add( idef.Clone() );
			}
			//
			return inc;
		}

		public void AlterTargets(StringDictionary mappedTargets)
		{
			// TODO! Run through all IDefs held in this incident definition and alter the
			// targets specified to their new corresponding targets...
			foreach(ModelActionBase idef in incidentActions)
			{
				TargetedModelAction tIdef = idef as TargetedModelAction;
				if(null != tIdef)
				{
					tIdef.AlterTargets(mappedTargets);
				}
			}
		}

		protected IncidentDefinition()
		{
		}

		public ArrayList IncidentActions => incidentActions;

		public ArrayList GetTargets ()
		{
			ArrayList targets = new ArrayList ();

			foreach (ModelActionBase idef in incidentActions)
			{
				TargetedModelAction tIdef = idef as TargetedModelAction;
				if (null != tIdef)
				{
					string target = tIdef.GetTarget();
					if (targets.IndexOf(target) == -1)
					{
						targets.Add(target);
					}
				}
			}

			return targets;
		}

		public List<Node> GetBusinessServicesAffected (NodeTree model)
		{
			return GetAllTargets(model).Where(n => n.GetAttribute("type") == "biz_service").ToList();
		}

		public List<Node> GetBusinessServiceUsersAffected (NodeTree model)
		{
			var bsus = new List<Node> ();

			foreach (var target in GetAllTargets(model))
			{
				switch (target.GetAttribute("type"))
				{
					case "biz_service_user":
						if (! bsus.Contains(target))
						{
							bsus.Add(target);
						}
						break;

					case "Connection":
					{
						var link = (LinkNode) target;
						if (link.To.GetAttribute("type") == "biz_service_user")
						{
							if (! bsus.Contains(target))
							{
								bsus.Add(link.To);
							}
						}
						break;
					}
				}
			}

			return bsus;
		}

		public List<string> GetChannelsAffected (NodeTree model)
		{
			var channels = new List<string> ();

			foreach (var targetAndAttributes in GetAllTargetsAndAttributes(model))
			{
				foreach (var avp in targetAndAttributes.Value)
				{
					var up = CONVERT.ParseBoolSafe(avp.Value);
					if (up == false)
					{
						string channel = null;
						switch (avp.Key)
						{
							case "up_instore":
								channel = "instore";
								break;

							case "up_online":
								channel = "online";
								break;
						}

						if (! string.IsNullOrEmpty(channel))
						{
							if (! channels.Contains(channel))
							{
								channels.Add(channel);
							}
						}
					}
				}
			}

			return channels;
		}

		public List<Node> GetAllTargets (NodeTree model)
		{
			var targets = new List<Node> ();

			foreach (ModelActionBase action in incidentActions)
			{
				var targetedAction = action as TargetedModelAction;

				if (targetedAction != null)
				{
					foreach (var target in targetedAction.GetAllTargets(model))
					{
						if (! targets.Contains(target))
						{
							targets.Add(target);
						}
					}
				}
			}

			return targets;
		}

		public override Dictionary<Node, Dictionary<string, string>> GetAllTargetsAndAttributes (NodeTree model)
		{
			var targets = new Dictionary<Node, Dictionary<string, string>> ();

			foreach (ModelActionBase action in incidentActions)
			{
				var nodeToAttributeToValue = action.GetAllTargetsAndAttributes(model);

				foreach (var node in nodeToAttributeToValue.Keys)
				{
					targets.Add(node, nodeToAttributeToValue[node]);
				}
			}

			return targets;
		}

		public string Type => _type;

		public string Description => _description;

		public string ID => _id;

		override public void ApplyAction(NodeTree nt)
		{
			if(doAfterSecs == 0)
			{
				ApplyActionNow(nt);
			}
			else
			{
				// Don't use the real time EventDelayer. Instead use OnAttributeHitApplier
				// to link to CurrentTime...
				GlobalEventDelayer.TheInstance.Delayer.AddEvent( this, doAfterSecs, nt);
			}
		}

		public override void ApplyActionNow(NodeTree nt)
		{
			foreach(ModelActionBase idef in incidentActions)
			{
				idef.ApplyAction(nt);
			}
		}

		public IncidentDefinition(XmlNode n, NodeTree tree)
		{
			Setup(n,tree);
		}

		public IncidentDefinition(string xmlDef, NodeTree tree)
		{
			BasicXmlDocument xdoc = BasicXmlDocument.Create(xmlDef);
			XmlNode rootNode = xdoc.DocumentElement;
			Setup(rootNode,tree);
		}

		protected void Setup(XmlNode n, NodeTree tree)
		{
			foreach(XmlAttribute a in n.Attributes)
			{
				_Attributes[a.Name] = a.Value;

				if(a.Name == "id")
				{
					_id = a.Value;
				}
				else if(a.Name == "description")
				{
					_description = a.Value;
				}
				else if(a.Name == "type")
				{
					_type = a.Value;
				}
				else if(a.Name == "allow_overlap")
				{
					_allow_overlap = CONVERT.ParseBool(a.Value, false);
				}
				else if (a.Name.ToLower() == "penalty")
				{
					isPenalty = (a.Value.ToLower() == "yes");
				}
			}
			//
			ApplyNodes(n, tree, ref incidentActions, this);
		}

		public static void ApplyNodes(XmlNode n, NodeTree tree, ref ArrayList _incidentActions)
		{
			ApplyNodes(n, tree, ref _incidentActions, null);
		}

		public static void ApplyNodes (XmlNode n, NodeTree tree, ref ArrayList _incidentActions,
		                               IncidentDefinition owningIncident)
		{
			var incidentActions = new List<ModelActionBase> ();
			ApplyNodes(n, tree, ref incidentActions, owningIncident);

			_incidentActions = new ArrayList ();
			_incidentActions.AddRange(incidentActions);
		}

		public static void ApplyNodes(XmlNode n, NodeTree tree, ref List<ModelActionBase> _incidentActions, IncidentDefinition owningIncident)
		{
			if (n == null)
			{
				return;
			}

			foreach(XmlNode child in n.ChildNodes)
			{
				if(child.NodeType == XmlNodeType.Element)
				{
					if(child.Name == "apply")
					{
						// This is an instruction to apply attributes to a node.
						ModelAction_Apply apply = new ModelAction_Apply(child);
						_incidentActions.Add(apply);
					}
					else if ((child.Name == "deleteNode") || (child.Name == "delete") || (child.Name == "deleteNodes"))
					{
						ModelAction_Delete dnodes = new ModelAction_Delete(child);
						_incidentActions.Add(dnodes);
					}
					else if(child.Name == "createNodes")
					{
						ModelAction_CreateNodes cnodes = new ModelAction_CreateNodes(child);
						cnodes.OwningIncident = owningIncident;
						_incidentActions.Add(cnodes);
					}
					else if( (child.Name == "addNodes") || (child.Name == "moveNodes") )
					{
						// This is an instruction to add a series of nodes to another node.
						ModelAction_AddNodes addnode = new ModelAction_AddNodes(child);
						_incidentActions.Add(addnode);
					}
					else if(child.Name == "incrementAtt")
					{
						// Instruction to increment an integer attribute on a node.
						ModelAction_IncrementAttribute inc = new ModelAction_IncrementAttribute(child);
						_incidentActions.Add(inc);
					}
					else if(child.Name == "moveAllMatchingNodes")
					{
						ModelAction_MoveMatching move = new ModelAction_MoveMatching(child);
						_incidentActions.Add(move);
					}
					else if(child.Name == "copyAttributes")
					{
						ModelAction_CopyAttributes copy = new ModelAction_CopyAttributes(child);
						_incidentActions.Add(copy);
					}
					else if(child.Name == "if_not")
					{
						ModelAction_If not = new ModelAction_If(child, tree, true, owningIncident);
						_incidentActions.Add(not);
					}
					else if(child.Name == "if")
					{
						ModelAction_If _if = new ModelAction_If(child, tree, false, owningIncident);
						_incidentActions.Add(_if);
					}
					else if (child.Name == "if_confirm")
					{
						_incidentActions.Add(new ModelAction_IfConfirm ((XmlElement) child, tree, owningIncident));
					}
				}
			}
		}

		public override IList<string> GetNamesOfNodesBrokenByAction ()
		{
			string [] nodeNames = new string [0];
			foreach (ModelActionBase action in incidentActions)
			{
				nodeNames = CollectionUtils.Union(nodeNames, action.GetNamesOfNodesBrokenByAction());
			}

			return nodeNames;
		}

		public string GetAttribute (string attributeName)
		{
			return (string) _Attributes[attributeName];
		}

		public override void ApplyAction (INodeChanger nodeChanger)
		{
			throw new NotImplementedException();
		}
	}
}