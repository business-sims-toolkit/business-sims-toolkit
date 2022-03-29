using System.Collections.Generic;
using Network;
using IncidentManagement;
using GameEngine;
using GameManagement;

namespace DevOps.OpsEngine
{
	public class TradingOpsEngine : OpsPhaseEngine
	{
	    readonly TransactionManager transactionManager;

	    readonly DangerLevelFlickerer dangerLevelFlickerer;

	    readonly MaxRevenueManager maxRevenueManager;
	    readonly SystemImpactMonitor systemImpactMonitor;
	    public RequestsManager TheRequestsApplier { get; }

	    public DevelopingAppTerminator AppTerminator { get; }

        public TradingOpsEngine(NodeTree model, NetworkProgressionGameFile gameFile, string incidentDefsFile, int round, bool logResults, IIncidentSlotTracker slotTracker)
				: base(gameFile, gameFile.CurrentRoundDir,incidentDefsFile, round, logResults)
		{
            TheRequestsApplier = new RequestsManager(model, round);

			transactionManager = new TransactionManager(model, round);
            maxRevenueManager = new MaxRevenueManager(model);
			systemImpactMonitor = new SystemImpactMonitor(model);

			dangerLevelFlickerer = new DangerLevelFlickerer (model);

		    AppTerminator = new DevelopingAppTerminator(model);

            ((DevOpsIncidentApplier)iApplier).IncidentSlotTracker = slotTracker;
		}

		public override void Dispose()
		{
			transactionManager.Dispose();
            maxRevenueManager.Dispose();
			systemImpactMonitor.Dispose();
            TheRequestsApplier.Dispose();
			dangerLevelFlickerer.Dispose();
			base.Dispose();
		}

		public NodeTree Model => _Network;

        protected override IncidentApplier CreateIncidentApplier(NodeTree _Network)
        {
            return new DevOpsIncidentApplier(_Network);
        }

        protected override RecurringIncidentDetector CreateRecurringIncidentDetector(NodeTree network)
        {
            return new DevOpsRecurringIncidentDetector(network);
        }

		protected override MirrorApplier CreateMirrorApplier (NodeTree model)
		{
			return null;
		}

		protected override MachineUpgrader CreateMachineUpgrader (NodeTree model)
		{
			return null;
		}

		protected override AppUpgrader CreateAppUpgrader (NodeTree model)
		{
			return null;
		}

        public override void BringEverythingUp()
        {
            base.BringEverythingUp();

            foreach (Node businessService in _Network.GetNodesWithAttribute("danger_level").Keys)
            {
                businessService.SetAttributes(new List<AttributeValuePair>
                                              {
                                                  new AttributeValuePair("danger_level", 0),
                                                  new AttributeValuePair("fixable", false),
                                                  new AttributeValuePair("incident_id", string.Empty)
                                              });
            }

            foreach (Node node in _Network.GetNodesWithAttributeValue("up_instore", "false"))
            {
                node.SetAttribute("up_instore", true);
            }

            foreach (Node node in _Network.GetNodesWithAttributeValue("up_online", "false"))
            {
                node.SetAttribute("up_online", true);
            }

            _Network.GetNamedNode("TestEnvironments").SetAttribute("virtual_test_enabled", round >= 3);
        }

        protected override void OnPhaseFinished()
        {
            TheRequestsApplier.AutoInstallServicesAtRoundEnd();

            base.OnPhaseFinished();
        }
	}
}
