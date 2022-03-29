using System.Collections;
using LibCore;
using Network;
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
	/// 
	/// This UpgradeonFix is used for the CA Security Application 
	/// We also take the time of the upgrade for reporting purposes 
	/// 
	/// </summary>
	public class BusinessServiceFixerAutoUpgrade : BusinessServiceFixer
	{

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sdc"></param>
		/// <param name="applier"></param>
		public BusinessServiceFixerAutoUpgrade(ServiceDownCounter sdc, IncidentApplier applier)
			: base(sdc, applier)
		{
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="n"></param>
		protected override void Fix(Node n, bool autoFix)
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
			AttributeValuePair.AddIfNotEqual(n, attrs, "just_been_auto_fixed", autoFix);

			bool node_upgrade_on_fix = n.GetBooleanAttribute("upgrade_on_fix",false);
			if (node_upgrade_on_fix)
			{
				int version = n.GetIntAttribute("version", 0);
				version = version + 1;
				attrs.Add(new AttributeValuePair("version", CONVERT.ToStr(version)));

				attrs.Add(new AttributeValuePair ("secure_level", "upgraded"));
				attrs.Add(new AttributeValuePair ("opcode", "fixed"));

				int time = n.Tree.GetNamedNode("CurrentTime").GetIntAttribute("seconds", 0);
				attrs.Add(new AttributeValuePair("opcode_time", CONVERT.ToStr(time)));
			}
			n.SetAttributes(attrs);
		}

	}
}