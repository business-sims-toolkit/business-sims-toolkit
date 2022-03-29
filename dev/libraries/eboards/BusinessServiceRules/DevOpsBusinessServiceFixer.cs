using System.Linq;

using IncidentManagement;
using Network;

namespace BusinessServiceRules
{
	public class DevOpsBusinessServiceFixer : BusinessServiceFixer
	{
		public DevOpsBusinessServiceFixer (ServiceDownCounter sdc, IncidentApplier applier)
			: base(sdc, applier)
		{
		}

		protected override int GetDangerLevelForAction(Action action)
		{
			return action == Action.WORKAROUND ? 80 : 0;
		}
		
		protected override void OnNodeFinishedWorkAround(LinkNode bsuLink)
		{
			var bsu = bsuLink.To.BackLinks.Cast<LinkNode>().FirstOrDefault(n => n.To.Type == "biz_service_user")?.To;

			if (bsu == null)
			{
				return;
			}

			var bizService = bsu.BackLinks.Cast<LinkNode>().FirstOrDefault(l => l.Parent.Type == "biz_service")?.Parent;
			
			bizService?.SetAttributeIfNotEqual("danger_level", 100);
			bizService?.SetAttribute("klaxon_triggered", false);
		}

	}
}
