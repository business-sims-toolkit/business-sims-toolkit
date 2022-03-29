using System;
using System.IO;
using System.Media;
using System.Windows.Forms;

using CommonGUI;
using DevOpsEngine.Interfaces;
using DevOpsUi.FacilitatorControls.Sla;
using GameManagement;
using IncidentManagement;
using Network;
using ResizingUi;

namespace DevOpsUi.FacilitatorScreen.DevOps
{
	public class DevOpsFacilitatorScreen : OpsPhaseScreen, IDataEntryControlHolderWithShowPanel
	{
		public DevOpsFacilitatorScreen (IDevOpsGameEngine gameEngine, NodeTree model)
		{
			this.gameEngine = gameEngine;
			this.model = model;
		}

		public override void Play ()
		{
			throw new NotImplementedException();
		}

		public override void Pause ()
		{
			throw new NotImplementedException();
		}

		public override void Reset ()
		{
			gameEngine.Reset();
		}

		public override void FastForward (double speed)
		{
			
		}

		public override void ImportIncidents (string incidentsFile)
		{
			gameEngine.IncidentApplier.SetIncidentDefinitions(File.ReadAllText(incidentsFile), model);
		}

		public override void DisableUserInteraction ()
		{
			throw new NotImplementedException();
		}

		public void DisposeEntryPanel ()
		{
			throw new NotImplementedException();
		}

		public void DisposeEntryPanel_indirect (int which)
		{
			throw new NotImplementedException();
		}

		public void SwapToOtherPanel (int which_operation)
		{
			throw new NotImplementedException();
		}

		public IncidentApplier IncidentApplier => gameEngine.IncidentApplier;


		public void ShowEntryPanel (Control panel)
		{
			throw new NotImplementedException();
		}

		readonly IDevOpsGameEngine gameEngine;

		readonly NodeTree model;
	}
}