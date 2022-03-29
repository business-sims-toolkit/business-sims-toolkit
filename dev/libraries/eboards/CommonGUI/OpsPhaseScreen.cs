using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using CoreUtils;

using GameManagement;

using LibCore;
using GameEngine;

using Network;

namespace CommonGUI
{
	public abstract class OpsPhaseScreen : BasePanel
	{
		public delegate void PlayPressedHandler(GameFile game);
		public event PlayPressedHandler PlayPressed;

		public event PhaseFinishedHandler PhaseFinished;

		public delegate void ActionHandler(object sender);

		protected BaseOpsPhaseEngine baseOpsEngine;

		bool gameFinished;

		public OpsPhaseScreen ()
		{
		}

		protected void FirePhaseFinished ()
		{
			OnPhaseFinished();
		}

		protected void OnPhaseFinished ()
		{
			PhaseFinished?.Invoke(this);
		}

		protected void FirePlayPressed(GameFile game)
		{
			PlayPressed?.Invoke(game);
		}

		public virtual void ForwardSkip (int amount, int speed)
		{
		}

		protected void SetBaseOpsEngine (BaseOpsPhaseEngine baseOpsEngine)
		{
			if (this.baseOpsEngine != null)
			{
				this.baseOpsEngine.GameStarted -= baseOpsEngine_GameStarted;
				baseOpsEngine.PhaseFinished -= baseOpsEngine_PhaseFinished;
			}

			this.baseOpsEngine = baseOpsEngine;

			baseOpsEngine.GameStarted += baseOpsEngine_GameStarted;
			baseOpsEngine.PhaseFinished += baseOpsEngine_PhaseFinished;
		}

		void baseOpsEngine_PhaseFinished (object sender)
		{
			OnGameFinished();
		}

		protected override void Dispose (bool disposing)
		{
			if (baseOpsEngine != null)
			{
				baseOpsEngine.GameStarted -= baseOpsEngine_GameStarted;
				baseOpsEngine.PhaseFinished -= baseOpsEngine_PhaseFinished;
			}

			base.Dispose(disposing);
		}

		void baseOpsEngine_GameStarted (object sender, EventArgs e)
		{
			OnGameStarted();
		}

		public virtual void ShowPopup (Control popup)
		{
		}

		public abstract void Play();
		public abstract void Pause();
		public abstract void Reset();
		public abstract void FastForward(double speed);

		public abstract void ImportIncidents(string incidentsFile);

		public event EventHandler GameStarted;
		public event EventHandler GameFinished;

		protected virtual void OnGameStarted ()
		{
			gameFinished = false;

			GameStarted?.Invoke(this, new EventArgs());
		}

		protected virtual void OnGameFinished ()
		{
			gameFinished = true;

			GameFinished?.Invoke(this, new EventArgs());
		}

		public virtual bool HasGameStartedToOurKnowledge ()
		{
			// If we crash here with a null pointer reference, then you probably need
			// to add a call to SetBaseOpsEngine() after you create your OpsPhaseEngine.
			return baseOpsEngine.IsStarted;
		}

		public virtual bool IsGameFinished ()
		{
			return gameFinished;
		}

		protected virtual OpsControlPanel CreateOpsControlPanel (NodeTree model,
														         IncidentManagement.IncidentApplier incidentApplier, IncidentManagement.MirrorApplier mirrorApplier, TransitionObjects.ProjectManager projectManager,
														         int round, int roundLengthMins,
											                     bool isTrainingGame,
											                     Color opPanelBackColor, Color groupHighlightColor,
											                     NetworkProgressionGameFile gameFile)
		{
			if (SkinningDefs.TheInstance.GetBoolData("use_impact_based_slas", false))
			{
				return new OpsControlPanelWithImpactBasedSla(model, incidentApplier, mirrorApplier, projectManager,
															  round, roundLengthMins, isTrainingGame, opPanelBackColor, groupHighlightColor, gameFile);
			}
			else
			{
				return new OpsControlPanel(model, incidentApplier, mirrorApplier, projectManager, round, roundLengthMins,
											isTrainingGame, opPanelBackColor, groupHighlightColor, gameFile);
			}
		}

		protected virtual void DoSize ()
		{
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);

			DoSize();
			Invalidate();
		}

		public abstract void DisableUserInteraction ();
	}
}