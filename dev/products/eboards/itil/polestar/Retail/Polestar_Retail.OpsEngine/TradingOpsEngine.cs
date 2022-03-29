using System;
using System.Collections;
using System.ComponentModel;
using System.Data;

using LibCore;
using Network;
using IncidentManagement;

using System.Xml;

using CoreUtils;
using GameEngine;
using GameManagement;

using System.IO;

namespace Polestar_Retail.OpsEngine
{
	public class TradingOpsEngine : OpsPhaseEngine
	{
		TransactionManager transactionManager;
		MaxRevenueManager  maxRevenueManager;
		SystemImpactMonitor systemImpactMonitor;

		public TradingOpsEngine(NodeTree model, NetworkProgressionGameFile gameFile, string incidentDefsFile, int round, bool logResults)
				: base(gameFile,gameFile.CurrentRoundDir,incidentDefsFile, round, logResults)
		{
			transactionManager = new TransactionManager(model, round);
			maxRevenueManager = new MaxRevenueManager(model);
			systemImpactMonitor = new SystemImpactMonitor(model);
		}

		public override void Dispose()
		{
			transactionManager.Dispose();
			maxRevenueManager.Dispose();
			systemImpactMonitor.Dispose();

			base.Dispose();
		}
	}
}