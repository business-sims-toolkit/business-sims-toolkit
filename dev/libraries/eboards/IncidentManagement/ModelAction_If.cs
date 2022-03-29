using System;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Xml;

using LibCore;
using Network;

namespace IncidentManagement
{
	/// <summary>
	/// Summary description for IDef_If_Not.
	/// </summary>
	public class ModelAction_If : TargetedModelAction
	{
		enum Comparison
		{
			Equal,
			NotEqual,
			Greater,
			GreaterEqual,
			Less,
			LessEqual
		}

		ArrayList incidentActions = new ArrayList();

		ArrayList elseActions = new ArrayList();
		//
		string _requiredParent = "";

		ArrayList valuesToCheck = new ArrayList();
		NodeTree _model;
		bool _useParentOfTarget = false;
		bool _useZoneOfTarget = false;
		bool _invertLogic = false;
		bool ignoreCase = false;
		Comparison comparison;

		IncidentDefinition owningIncident;

		public ArrayList IncidentActions
		{
			get
			{
				return incidentActions;
			}
		}

		public ArrayList ElseActions
		{
			get
			{
				return elseActions;
			}
		}

		public override object Clone ()
		{
			ModelAction_If newApply = new ModelAction_If();
			newApply.target = this.target;
			newApply.doAfterSecs = this.doAfterSecs;
			newApply.owningIncident = this.owningIncident;
			newApply.ignoreCase = ignoreCase;
			foreach (AttributeValuePair avp in valuesToCheck)
			{
				newApply.valuesToCheck.Add(avp.Clone());
			}
			//
			foreach (ModelActionBase idef in incidentActions)
			{
				newApply.incidentActions.Add(idef.Clone());
			}
			return newApply;
		}

		ModelAction_If ()
		{
		}

		public ModelAction_If (XmlNode n, NodeTree model, bool invertLogic)
			: this(n, model, invertLogic, null)
		{
		}

		public ModelAction_If (XmlNode n, NodeTree model, bool invertLogic, IncidentDefinition owningIncident)
		{
			this.owningIncident = owningIncident;
			_invertLogic = invertLogic;
			_model = model;

			comparison = Comparison.Equal;

			foreach (XmlAttribute a in n.Attributes)
			{
				if (a.Name.StartsWith("i_"))
				{
					// This is an IDef command.
					if (a.Name == "i_name")
					{
						// This is the target node to apply the attributes to.
						target = a.Value;
					}
					else if (a.Name == "i_doAfterSecs")
					{
						doAfterSecs = CONVERT.ParseInt(a.Value);
					}
					else if (a.Name == "i_parent")
					{
						_requiredParent = a.Value;
					}
					else if (a.Name == "i_zoneOf")
					{
						_useZoneOfTarget = true;
						target = a.Value;
					}
					else if (a.Name == "i_parentOf")
					{
						_useParentOfTarget = true;
						target = a.Value;
					}
					else if (a.Name == "i_ignoreCase")
					{
						ignoreCase = CONVERT.ParseBool(a.Value, false);
					}
					else if (a.Name == "i_comparison")
					{
						switch (a.Value)
						{
							case "Equal":
							default:
								comparison = Comparison.Equal;
								break;

							case "NotEqual":
								comparison = Comparison.NotEqual;
								break;

							case "Greater":
								comparison = Comparison.Greater;
								break;

							case "GreaterEqual":
								comparison = Comparison.GreaterEqual;
								break;

							case "Less":
								comparison = Comparison.Less;
								break;

							case "LessEqual":
								comparison = Comparison.LessEqual;
								break;
						}
					}
				}
				else
				{
					// This is an attribute to check.
					AttributeValuePair avp = new AttributeValuePair();
					avp.Attribute = a.Name;
					avp.Value = a.Value;
					valuesToCheck.Add(avp);
				}
			}
			//
			// Pull all child definitions...
			//
			IncidentDefinition.ApplyNodes(n.SelectSingleNode("then"), model, ref incidentActions, owningIncident);
			XmlNode elseNode = n.SelectSingleNode("else");
			if (null != elseNode)
			{
				IncidentDefinition.ApplyNodes(elseNode, model, ref elseActions, owningIncident);
			}
			//
		}

		override public void ApplyAction (NodeTree nt)
		{
			if (doAfterSecs > 0)
			{
				GlobalEventDelayer.TheInstance.Delayer.AddEvent(this, doAfterSecs, nt);
			}
			else
			{
				ApplyActionNow(nt);
			}
		}

		bool Check (string attributeValue, string compareValue)
		{
			bool result;

			if (ignoreCase)
			{
				attributeValue = attributeValue.ToLower();
				compareValue = compareValue.ToLower();
			}

			switch (comparison)
			{
				case Comparison.Equal:
				default:
					result = (attributeValue == compareValue);
					break;

				case Comparison.NotEqual:
					result = (attributeValue != compareValue);
					break;

				case Comparison.Greater:
					result = (CONVERT.ParseDouble(attributeValue) > CONVERT.ParseDouble(compareValue));
					break;

				case Comparison.GreaterEqual:
					result = (CONVERT.ParseDouble(attributeValue) >= CONVERT.ParseDouble(compareValue));
					break;

				case Comparison.Less:
					result = (CONVERT.ParseDouble(attributeValue) < CONVERT.ParseDouble(compareValue));
					break;

				case Comparison.LessEqual:
					result = (CONVERT.ParseDouble(attributeValue) <= CONVERT.ParseDouble(compareValue));
					break;
			}

			return InvertCheck(result);
		}

		bool InvertCheck (bool b)
		{
			if (this._invertLogic) return !b;
			return b;
		}

		void ApplyElseActions (NodeTree nt)
		{
			foreach (ModelActionBase idef in elseActions)
			{
				idef.ApplyAction(_model);
			}
		}

		public override void ApplyActionNow (NodeTree nt)
		{
		    var oldTargetAttribute = new List<AttributeValuePair> { new AttributeValuePair("target", target) };
            var newTargetAttribute = Node.RemapSpecialAttributes(nt, oldTargetAttribute);

            var newTarget = newTargetAttribute[0].Value;

			// Check all our constraints first...
            Node n = nt.FindNodeWithSearchAttributesIfPresent(newTarget);

			if (null == n)
			{
				if (_invertLogic)
				{
					ApplyThenActions(nt);
				}
				else
				{
					ApplyElseActions(nt);
				}
				return;
			}

			if (_invertLogic
				&& (valuesToCheck.Count == 0)
				&& (n != null))
			{
				ApplyElseActions(nt);
				return;
			}


			if (_useParentOfTarget)
			{
				n = n.Parent;
			}

			if (_useZoneOfTarget)
			{
				string zone = n.GetAttribute("zone");
				if (zone == "")
				{
					zone = n.GetAttribute("proczone");
				}

				n = nt.GetNamedNode("Zone" + zone);
			}
			//
			if (this._requiredParent != "")
			{
				// Check that the target has the required parent...
				Node n_parent = _model.GetNamedNode(_requiredParent);
				if (null == n)
				{
					ApplyElseActions(nt);
					return;
				}
				if (null == n_parent)
				{
					ApplyElseActions(nt);
					return;
				}
				if (InvertCheck(!n_parent.HasChild(n)))
				{
					ApplyElseActions(nt);
					return;
				}
			}
			//
			foreach (AttributeValuePair avp in this.valuesToCheck)
			{
				if (!Check(n.GetAttribute(avp.Attribute), avp.Value))
				{
					// We have an incorrectly matching AVP.
					ApplyElseActions(nt);
					return;
				}
			}
			//

			ApplyThenActions(nt);
		}

		void ApplyThenActions (NodeTree nt)
		{
			foreach (ModelActionBase idef in incidentActions)
			{
				idef.ApplyAction(nt);
			}
		}

		public override void AlterTargets (StringDictionary mappedTargets)
		{
			base.AlterTargets(mappedTargets);

			foreach (ModelActionBase idef in incidentActions)
			{
				TargetedModelAction tIdef = idef as TargetedModelAction;
				if (null != tIdef)
				{
					tIdef.AlterTargets(mappedTargets);
				}
			}

			foreach (ModelActionBase idef in elseActions)
			{
				TargetedModelAction tIdef = idef as TargetedModelAction;
				if (null != tIdef)
				{
					tIdef.AlterTargets(mappedTargets);
				}
			}
		}

		public override IList<string> GetNamesOfNodesBrokenByAction ()
		{
			List<ICollection<string>> nameLists = new List<ICollection<string>> ();

			foreach (ModelActionBase action in incidentActions)
			{
				nameLists.Add(action.GetNamesOfNodesBrokenByAction());
			}
			foreach (ModelActionBase action in elseActions)
			{
				nameLists.Add(action.GetNamesOfNodesBrokenByAction());
			}

			return new List<string> (CollectionUtils.Union(nameLists.ToArray()));
		}

		public override void ApplyAction (INodeChanger nodeChanger)
		{
			throw new NotImplementedException();
		}

		public override List<Node> GetAllTargets (NodeTree model)
		{
			var targets = new List<Node> ();

			var node = model.GetNamedNode(target);
			if (node != null)
			{
				targets.Add(node);
			}

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

			foreach (ModelActionBase action in elseActions)
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
			var results = new Dictionary<Node, Dictionary<string, string>> ();

			foreach (var actions in new [] { incidentActions, elseActions })
			{
				foreach (ModelActionBase action in actions)
				{
					var actionResults = action.GetAllTargetsAndAttributes(model);

					foreach (var node in actionResults.Keys)
					{
						if (! results.ContainsKey(node))
						{
							results.Add(node, new Dictionary<string, string>());
						}

						foreach (var attribute in actionResults[node].Keys)
						{
							results[node][attribute] = actionResults[node][attribute];
						}
					}
				}
			}

			return results;
		}
	}
}