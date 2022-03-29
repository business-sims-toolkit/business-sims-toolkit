using System.Collections.Generic;
using System.Windows.Forms;
using System.Xml;
using LibCore;
using Network;

namespace IncidentManagement
{
	public class ModelAction_IfConfirm : TargetedModelAction
	{
		NodeTree model;
		IncidentDefinition owningIncident;

		string text;
		List<ModelActionBase> thenActions;
		List<ModelActionBase> elseActions;

		public static IWin32Window TopLevelControl;
			
		public ModelAction_IfConfirm (XmlElement element, NodeTree model, IncidentDefinition owningIncident)
		{
			this.model = model;
			this.owningIncident = owningIncident;

			text = element.GetStringAttribute("text", "");

			thenActions = new List<ModelActionBase> ();
			elseActions = new List<ModelActionBase> ();

			IncidentDefinition.ApplyNodes(element.SelectSingleNode("then"), model, ref thenActions, owningIncident);
			IncidentDefinition.ApplyNodes(element.SelectSingleNode("else"), model, ref elseActions, owningIncident);
		}

		public override object Clone ()
		{
			throw new System.NotImplementedException ();
		}

		public override void ApplyAction (NodeTree model)
		{
			if (doAfterSecs > 0)
			{
				GlobalEventDelayer.TheInstance.Delayer.AddEvent(this, doAfterSecs, model);
			}
			else
			{
				ApplyActionNow(model);
			}
		}

		public override void ApplyAction (INodeChanger nodeChanger)
		{
			throw new System.NotImplementedException();
		}

		void ApplyActions (List<ModelActionBase> actions)
		{
			foreach (var action in actions)
			{
				action.ApplyAction(model);
			}
		}

		public override void ApplyActionNow (NodeTree nt)
		{
			if (MessageBoxHandling.ShowMessageBox(TopLevelControl, text, "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
			{
				ApplyActions(thenActions);
			}
			else
			{
				ApplyActions(elseActions);
			}
		}

		public override List<Node> GetAllTargets (NodeTree model)
		{
			var targets = new List<Node>();

			targets.Add(model.GetNamedNode(target));

			foreach (var action in thenActions)
			{
				var targetedAction = action as TargetedModelAction;

				if (targetedAction != null)
				{
					foreach (var target in targetedAction.GetAllTargets(model))
					{
						if (!targets.Contains(target))
						{
							targets.Add(target);
						}
					}
				}
			}

			foreach (var action in elseActions)
			{
				var targetedAction = action as TargetedModelAction;

				if (targetedAction != null)
				{
					foreach (var target in targetedAction.GetAllTargets(model))
					{
						if (!targets.Contains(target))
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
			var results = new Dictionary<Node, Dictionary<string, string>>();

			foreach (var actions in new [] { thenActions, elseActions })
			{
				foreach (ModelActionBase action in actions)
				{
					var actionResults = action.GetAllTargetsAndAttributes(model);

					foreach (var node in actionResults.Keys)
					{
						if (!results.ContainsKey(node))
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