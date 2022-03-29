using System;
using System.Drawing;
using Network;
using IncidentManagement;
using TransitionObjects;
using GameManagement;

namespace CommonGUI
{
	public class OpsControlPanelWithImpactBasedSla : OpsControlPanel
	{
		public OpsControlPanelWithImpactBasedSla (NodeTree model, IncidentApplier incidentApplier, MirrorApplier mirrorApplier, ProjectManager projectManager,
		                                          int round, int roundLengthMins, bool isTraining,
		                                          Color backColour, Color panelBackColour,
												  NetworkProgressionGameFile gameFile)
			: base (model, incidentApplier, mirrorApplier, projectManager,
					round, roundLengthMins, isTraining,
					backColour, panelBackColour,
					gameFile)
		{
		}

		protected override void GenerateOperationPanel_MTRS ()
		{
			DisposeEntryPanel();

			ImpactBasedSlaEditor panel = new ImpactBasedSlaEditor (this, _round, _network, MyIsTrainingFlag);
			panel.Size = new Size (popup_width, popup_height);
			panel.Location = new Point (popup_xposition, popup_yposition);
			Parent.Controls.Add(panel);
			panel.BringToFront();
			shownControl = panel;
			panel.Focus();
		}

		public delegate InstallBusinessServiceControl CreateInstallBusinessServicePanelHandler (OpsControlPanelBase controlPanel, NodeTree model, int round, ProjectManager projectManager, bool isTraining, Color backgroundColour, Color groupBackgroundColour);
		public event CreateInstallBusinessServicePanelHandler CreateInstallBusinessServicePanel;

		public delegate UpgradeAppControl CreateUpgradeAppPanelHandler (OpsControlPanelBase mainPanel, IncidentApplier incidentApplier, NodeTree model, bool showMinutesNotDays, bool isTraining, Color backColour, Color groupBackColour);
		public event CreateUpgradeAppPanelHandler CreateUpgradeAppPanel;

		public delegate UpgradeMemDiskControl CreateUpgradeServerPanelHandler (OpsControlPanelWithImpactBasedSla controlPanel, NodeTree model, bool showMinutesNotDays, IncidentApplier incidentApplier, bool isTraining, Color backColour, Color groupBackColour);
		public event CreateUpgradeServerPanelHandler CreateUpgradeServerPanel;


        public delegate PendingActionsControlWithIncidents CreatePendingPanelHandler(OpsControlPanelBase mainPanel, NodeTree model, ProjectManager prjmanager, Boolean IsTrainingMode, Color OperationsBackColor, Color GroupPanelBackColor, NetworkProgressionGameFile gameFile);
        public event CreatePendingPanelHandler CreatePendingPanel;

        PendingActionsControlWithIncidents OnCreatePendingActionsControlWithIncidents(OpsControlPanelBase mainPanel, NodeTree model, ProjectManager prjmanager, Boolean IsTrainingMode,
                                                   Color OperationsBackColor, Color GroupPanelBackColor,
                                                   NetworkProgressionGameFile gameFile)
        {
            if (CreatePendingPanel != null)
            {
                return CreatePendingPanel(mainPanel, model, prjmanager, IsTrainingMode, OperationsBackColor, GroupPanelBackColor, gameFile);
            }
            return new PendingActionsControlWithIncidents(this, _network, _prjmanager, MyIsTrainingFlag, MyOperationsBackColor, MyGroupPanelBackColor, gameFile);
        }

		InstallBusinessServiceControl OnCreateInstallBusinessServicePanel (OpsControlPanelBase controlPanel, NodeTree model, int round, ProjectManager projectManager, bool isTraining, Color backgroundColour, Color groupBackgroundColour)
		{
			if (CreateInstallBusinessServicePanel != null)
			{
				return CreateInstallBusinessServicePanel(controlPanel, model, round, projectManager, isTraining, backgroundColour, groupBackgroundColour);
			}

			return new InstallBusinessServiceControl (controlPanel, model, round, projectManager, isTraining, backgroundColour, groupBackgroundColour);
		}

		UpgradeAppControl OnCreateUpgradeAppPanel (OpsControlPanelBase mainPanel, IncidentApplier incidentApplier, NodeTree model, bool showMinutesNotDays, bool isTraining, Color backColour, Color groupBackColour)
		{
			if (CreateUpgradeAppPanel != null)
			{
				return CreateUpgradeAppPanel(mainPanel, incidentApplier, model, showMinutesNotDays, isTraining, backColour, groupBackColour);
			}

			return new UpgradeAppControl (mainPanel, incidentApplier, model, showMinutesNotDays, isTraining, backColour, groupBackColour);
		}


		UpgradeMemDiskControl OnCreateUpgradeServerPanel (OpsControlPanelWithImpactBasedSla controlPanel, NodeTree model, bool showMinutesNotDays, IncidentApplier incidentApplier, bool isTraining, Color backColour, Color groupBackColour)
		{
			if (CreateUpgradeServerPanel != null)
			{
				return CreateUpgradeServerPanel(controlPanel, model, showMinutesNotDays, incidentApplier, isTraining, backColour, groupBackColour);
			}

			return new UpgradeMemDiskControl (this, _network, true,
                                              _iApplier, MyIsTrainingFlag, MyOperationsBackColor, MyGroupPanelBackColor);
		}

        protected override void pendingActions_ButtonPressed(object sender, ImageButtonEventArgs args)
        {
	        if (playing)
	        {
		        PendingActionsControl pac = OnCreatePendingActionsControlWithIncidents(this, _network, _prjmanager,
			        MyIsTrainingFlag, MyOperationsBackColor, MyGroupPanelBackColor, gameFile);
		        pac.Size = new Size(popup_width, popup_height);
		        pac.Location = new Point(popup_xposition, popup_yposition);
		        Parent.Controls.Add(pac);
		        pac.BringToFront();
		        DisposeEntryPanel();
		        shownControl = pac;
		        pac.Focus();
		        pendingActions.Active = true;
			}
        }

		protected override void installBusinessServiceButton_ButtonPressed (object sender, ImageButtonEventArgs args)
		{
			if (playing)
			{
				InstallBusinessServiceControl IBS_Panel = OnCreateInstallBusinessServicePanel(this, _network,
				                                                                                _round, _prjmanager,
																								MyIsTrainingFlag,
																								MyOperationsBackColor, MyGroupPanelBackColor);

				IBS_Panel.Size = new Size (popup_width, popup_height);
				IBS_Panel.Location = new Point (popup_xposition, popup_yposition);
				IBS_Panel.SetMaxMins(_round_maxmins);
				Parent.Controls.Add(IBS_Panel);
				IBS_Panel.BringToFront();
				DisposeEntryPanel();
				shownControl = IBS_Panel;
				IBS_Panel.Focus();
				((ImageButton) sender).Active = true;
			}
		}

		protected override void upgradeAppButton_ButtonPressed (object sender, ImageButtonEventArgs args)
		{
			if (playing)
			{
				UpgradeAppControl upgradePanel = OnCreateUpgradeAppPanel(this, _iApplier, _network, true, MyIsTrainingFlag, MyOperationsBackColor, MyGroupPanelBackColor);
				upgradePanel.Size = new Size (popup_width, popup_height);
				upgradePanel.Location = new Point (popup_xposition, popup_yposition);
				upgradePanel.SetMaxMins(_round_maxmins);
				Parent.Controls.Add(upgradePanel);
				upgradePanel.BringToFront();	
				DisposeEntryPanel();
				shownControl = upgradePanel;
				((ImageButton) sender).Active = true;
				upgradePanel.Focus();
			}
		}

		protected override void upgradeMemDiskButton_ButtonPressed (object sender, ImageButtonEventArgs args)
		{
			if (playing)
			{
				UpgradeMemDiskControl upgradePanel = OnCreateUpgradeServerPanel(this, _network, true, _iApplier, MyIsTrainingFlag, MyOperationsBackColor, MyGroupPanelBackColor);
				upgradePanel.Size = new Size (popup_width, popup_height);
				upgradePanel.Location = new Point (popup_xposition, popup_yposition);
				upgradePanel.SetMaxMins(_round_maxmins);
				Parent.Controls.Add(upgradePanel);
				upgradePanel.BringToFront();
				DisposeEntryPanel();
				shownControl = upgradePanel;
				upgradePanel.Focus();
				((ImageButton) sender).Active = true;
			}
		}

	    protected override void OnSizeChanged (EventArgs e)
	    {
	        base.OnSizeChanged(e);

	        int x = 0;
	        int gap = 2;
	        for (int i = 0; i < Controls.Count; i++)
	        {
	            var control = Controls[i];
                control.Bounds = new Rectangle (x, control.Top, (Width - x - ((Controls.Count - i - 1) * gap)) / (Controls.Count - i), Height - control.Top);
	            x = control.Right + gap;
	        }
	    }
	}
}