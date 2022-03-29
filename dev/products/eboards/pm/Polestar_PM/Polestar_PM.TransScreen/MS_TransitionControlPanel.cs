using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;

using LibCore;
using Network;

using CoreUtils;
using CommonGUI;

using IncidentManagement;
using TransitionScreens;

namespace Polestar_PM.TransScreen
{
	/// <summary>
	/// Summary description for TransitionControlPanel.
	/// </summary>
	public class MS_TransitionControlPanel : TransitionControlPanel
	{

		public MS_TransitionControlPanel(TransitionScreen ts, NodeTree nt, IncidentApplier iApplier, 
			MirrorApplier mirrorApplier, int round, Color OperationsBackColor, Color GroupPanelBackColor)
			: base(ts, nt, iApplier, mirrorApplier, round, OperationsBackColor, GroupPanelBackColor)
		{
		
		}

		public override void GenerateOperationPanel_StartSIP()
		{
			startSIPButton.Active = true;
			MS_StartSIP startSIP = new MS_StartSIP(this,_network,_currentround, this.MyOperationsBackColor);
			startSIP.Size = new Size(popup_width,popup_height);
			startSIP.Location  = new Point(popup_xposition,popup_yposition);
			this.Parent.SuspendLayout();
			this.Parent.Controls.Add(startSIP);
			this.Parent.ResumeLayout(false);
			startSIP.BringToFront();
			//
			DisposeEntryPanel();
			//toggleView.Visible = false;
			startSIPButton.Active = true;
			shownControl = startSIP;
			startSIP.Focus();		
		}

		public override void GenerateOperationPanel_CancelSIP()
		{
			CancelSIP cancelSIP = new MS_CancelSIP(this,_network, _currentround, this.MyOperationsBackColor);
			cancelSIP.Size = new Size(popup_width,popup_height);
			cancelSIP.Location  = new Point(popup_xposition,popup_yposition);
				
			this.Parent.SuspendLayout();
			this.Parent.Controls.Add(cancelSIP);
			this.Parent.ResumeLayout(false);

			cancelSIP.BringToFront();
			//
			DisposeEntryPanel();
			//toggleView.Visible = false;
			cancelSIPButton.Active = true;
			shownControl = cancelSIP;
			cancelSIP.Focus();
		}

		public override void GenerateOperationPanel_InstallSIP()
		{
			InstallSIP installSIP = new MS_InstallSIP(this,_network,_currentround,this.MyOperationsBackColor);
			installSIP.Size = new Size(popup_width,popup_height);
			installSIP.Location  = new Point(popup_xposition,popup_yposition);
			this.Parent.SuspendLayout();
			this.Parent.Controls.Add(installSIP);
			this.Parent.ResumeLayout(false);
			//installSIP.PrevControl = installSIPButton;
			installSIP.BringToFront();
			//
			DisposeEntryPanel();
			//toggleView.Visible = false;
			installSIPButton.Active = true;
			shownControl = installSIP;
			installSIP.Focus();
		}

		public override void GenerateOperationPanel_DefineSLA()
		{
			definesla = new MS_DefineSLA(this,_network,this.MyOperationsBackColor, round);
			definesla.Size = new Size(popup_width,popup_height);
			definesla.Location  = new Point(popup_xposition,popup_yposition);
			this.Parent.SuspendLayout();
			this.Parent.Controls.Add(definesla);
			this.Parent.ResumeLayout(false);
			definesla.BringToFront();
			DisposeEntryPanel();
			//toggleView.Visible = false;
			slaButton.Active = true;
			shownControl = definesla;
			definesla.Focus();
		}

		public override void GenerateOperationPanel_UpgradeApp()
		{
			TransUpgradeAppControl upgradePanel = new MS_TransUpgradeAppControl(this, _iApplier, _network, false,
				MyOperationsBackColor, MyGroupPanelBackColor);

			upgradePanel.Size = new Size(popup_width,popup_height);
			upgradePanel.Location  = new Point(popup_xposition,popup_yposition);

			this.Parent.SuspendLayout();
			this.Parent.Controls.Add(upgradePanel);
			this.Parent.ResumeLayout(false);

			//UpgradeAppControl.PrevControl = installSIPButton;
			upgradePanel.BringToFront();
			DisposeEntryPanel();
			//toggleView.Visible = false;
			upgradeAppButton.Active = true;
			shownControl = upgradePanel;
			upgradePanel.Focus();
		}

		public override void GenerateOperationPanel_UpgradeMemDisk()
		{
			MS_TransUpgradeMemDiskControl upgradePanel = new MS_TransUpgradeMemDiskControl(this, _network, 
				false, this._iApplier, MyOperationsBackColor, MyGroupPanelBackColor);
			upgradePanel.Size = new Size(popup_width,popup_height);
			upgradePanel.Location  = new Point(popup_xposition,popup_yposition);

			this.Parent.SuspendLayout();
			this.Parent.Controls.Add(upgradePanel);
			this.Parent.ResumeLayout(false);

			upgradePanel.BringToFront();
			DisposeEntryPanel();
			//toggleView.Visible = false;
			upgradeMemDiskButton.Active = true;
			shownControl = upgradePanel;
			upgradePanel.Focus();
		}

		public override void GenerateOperationPanel_Mirror()
		{
			MS_TransAddOrRemoveMirrorControl addOrRemoveMirrorControl = new MS_TransAddOrRemoveMirrorControl(
				this,_network, _mirrorApplier, MyOperationsBackColor, MyGroupPanelBackColor);
			addOrRemoveMirrorControl.Size= new Size(popup_width,popup_height);
			addOrRemoveMirrorControl.Location  = new Point(popup_xposition,popup_yposition);

			this.Parent.SuspendLayout();
			this.Parent.Controls.Add(addOrRemoveMirrorControl);
			this.Parent.ResumeLayout(false);

			addOrRemoveMirrorControl.BringToFront();
			DisposeEntryPanel();
			//toggleView.Visible = false;
			addMirrorButton.Active = true;
			shownControl = addOrRemoveMirrorControl;
			addOrRemoveMirrorControl.Focus();
		}

	}

}
