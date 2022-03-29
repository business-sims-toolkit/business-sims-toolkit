using System;
using System.Collections;
using System.IO;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

using Network;
using CommonGUI;
using LibCore;
using CoreUtils;
using BusinessServiceRules;
using Polestar_PM.DataLookup;
using Polestar_PM.OpsEngine;

namespace Polestar_PM.OpsGUI
{
	/// <summary>
	/// Change PMO allows the players to change a number PMO based attributes
	/// Only one attribute is editable at this time "PMO Budget"
	/// </summary>

	public class DataEntryPanel_MiscChoice  : FlickerFreePanel
	{
		protected IDataEntryControlHolderWithShowPanel _mainPanel;
		protected int _round;
		protected NodeTree MyNodeTree;

		protected Font MyDefaultSkinFontNormal8 = null;
		protected Font MyDefaultSkinFontNormal10 = null;
		protected Font MyDefaultSkinFontNormal12 = null;
		protected Font MyDefaultSkinFontBold8 = null;
		protected Font MyDefaultSkinFontBold10 = null;
		protected Font MyDefaultSkinFontBold12 = null;
		protected Font MyDefaultSkinFontBold24 = null;

		private ImageTextButton btnPMOChange = new ImageTextButton(0);
		private ImageTextButton btnShowBubbles = new ImageTextButton(0);
		private ImageTextButton btnShowAnalysisCharts = new ImageTextButton(0);
		private ImageTextButton btnShowPlan = new ImageTextButton(0);
		private ImageTextButton btnSetPredictedMarket = new ImageTextButton(0);
		private ImageTextButton btnShowProjectedGains = new ImageTextButton(0);
		private ImageTextButton btnFixExperts = new ImageTextButton(0);
		private ImageTextButton btnChangeExpertsStatus = new ImageTextButton(0);
		ImageTextButton btnTestInstall = new ImageTextButton(0);
		ImageTextButton btnShowResources = new ImageTextButton (0);

		private ImageTextButton newBtnOK = new ImageTextButton(0);
		private ImageTextButton newBtnCancel = new ImageTextButton(0);

		private System.Windows.Forms.Label titleLabel;
		private System.Windows.Forms.Label helpLabel;
		protected bool AllowResourcePlaninRound1 = false;

		Polestar_PM.OpsEngine.PM_OpsEngine opsEngine;
		GameManagement.NetworkProgressionGameFile gameFile;

		public DataEntryPanel_MiscChoice (IDataEntryControlHolderWithShowPanel mainPanel, NodeTree tree, int round, Polestar_PM.OpsEngine.PM_OpsEngine opsEngine, GameManagement.NetworkProgressionGameFile gameFile)
		{
			MyNodeTree = tree;
			_mainPanel = mainPanel;
			_round = round;
			this.gameFile = gameFile;
			this.opsEngine = opsEngine;
			
			string fontname =  SkinningDefs.TheInstance.GetData("fontname");
			MyDefaultSkinFontNormal8 = ConstantSizeFont.NewFont(fontname,8);
			MyDefaultSkinFontNormal10 = ConstantSizeFont.NewFont(fontname,10);
			MyDefaultSkinFontNormal12 = ConstantSizeFont.NewFont(fontname,12);
			MyDefaultSkinFontBold8 = ConstantSizeFont.NewFont(fontname,8,FontStyle.Bold);
			MyDefaultSkinFontBold10 = ConstantSizeFont.NewFont(fontname,10,FontStyle.Bold);
			MyDefaultSkinFontBold12 = ConstantSizeFont.NewFont(fontname,12,FontStyle.Bold);
			MyDefaultSkinFontBold24 = ConstantSizeFont.NewFont(fontname,24,FontStyle.Bold);

			this.BackgroundImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images\\panels\\PM_opsback.png");

			string AllowResourcePlaninRound1_str = SkinningDefs.TheInstance.GetData("show_resource_plan_in_round1", "false");
			if (AllowResourcePlaninRound1_str.IndexOf("true") > -1)
			{
				AllowResourcePlaninRound1 = true;
			}

			//this.ShowInTaskbar = false;
			//this.ClientSize = new Size (520,280);
			//this.FormBorderStyle = FormBorderStyle.None;
			//this.Opacity = 1.00;
			this.Size = new Size(520,255);

			//Build the Title and Help text buttons
			titleLabel = new System.Windows.Forms.Label();
			titleLabel.BackColor = System.Drawing.Color.Transparent;
			titleLabel.Font = MyDefaultSkinFontBold12;
			titleLabel.ForeColor = System.Drawing.Color.White;
			titleLabel.Location = new System.Drawing.Point(110-25, 10-2);
			titleLabel.Name = "titleLabel";
			titleLabel.Size = new System.Drawing.Size(380, 18);
			titleLabel.TabIndex = 11;
			titleLabel.Text = "Select Option";
			titleLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.Controls.Add(titleLabel);

			helpLabel = new System.Windows.Forms.Label();
			helpLabel.BackColor = System.Drawing.Color.Transparent;
			helpLabel.Font = MyDefaultSkinFontNormal10;
			helpLabel.ForeColor = System.Drawing.Color.White;
			helpLabel.Location = new System.Drawing.Point(110-25, 50-20-1);
			helpLabel.Name = "helpLabel";
			helpLabel.Size = new System.Drawing.Size(380, 18);
			helpLabel.TabIndex = 20;
			helpLabel.Text = "Please select the option that you require";
			helpLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.Controls.Add(helpLabel);

			//Build the choices 
			int NextButtonSlot = 0;

			//btnPMOChange.SetButton(AppInfo.TheInstance.Location+"\\images\\buttons\\button_255x25.png");
			btnPMOChange.SetVariants("images\\buttons\\button_255x25.png");
			btnPMOChange.Location = new System.Drawing.Point(90, 55 + 35 * NextButtonSlot);
			btnPMOChange.Name = "Button1";
			btnPMOChange.Size = new System.Drawing.Size(255, 25);
			btnPMOChange.TabIndex = 8;
			btnPMOChange.ButtonFont = MyDefaultSkinFontBold10;
			btnPMOChange.Tag = 1;
			btnPMOChange.SetButtonText("Change PMO Budget",
				System.Drawing.Color.Black, System.Drawing.Color.Black,
				System.Drawing.Color.White, System.Drawing.Color.Gray);
			btnPMOChange.Click += new System.EventHandler(this.btnPMOChange_Click);
			Controls.Add(btnPMOChange);
			NextButtonSlot++;

			if (round == 3)
			{
				//btnFixExperts.SetButton(AppInfo.TheInstance.Location + "\\images\\buttons\\button_255x25.png");
				btnChangeExpertsStatus.SetVariants("images\\buttons\\button_255x25.png");
				btnChangeExpertsStatus.Location = new System.Drawing.Point(90, 55 + 35 * NextButtonSlot);
				btnChangeExpertsStatus.Size = new System.Drawing.Size(255, 25);
				btnChangeExpertsStatus.TabIndex = 8;
				btnChangeExpertsStatus.ButtonFont = MyDefaultSkinFontBold10;
				btnChangeExpertsStatus.Tag = 1;
				btnChangeExpertsStatus.SetButtonText("Change Experts Status",
					System.Drawing.Color.Black, System.Drawing.Color.Black,
					System.Drawing.Color.White, System.Drawing.Color.Gray);
				btnChangeExpertsStatus.Click += new System.EventHandler(this.btnChangeExpertsStatus_Click);
				Controls.Add(btnChangeExpertsStatus);
				NextButtonSlot++;
			}

			if (round == 3)
			{
				//btnFixExperts.SetButton(AppInfo.TheInstance.Location + "\\images\\buttons\\button_255x25.png");
				btnFixExperts.SetVariants("images\\buttons\\button_255x25.png");
				btnFixExperts.Location = new System.Drawing.Point(90, 55 + 35 * NextButtonSlot);
				btnFixExperts.Size = new System.Drawing.Size(255, 25);
				btnFixExperts.TabIndex = 8;
				btnFixExperts.ButtonFont = MyDefaultSkinFontBold10;
				btnFixExperts.Tag = 1;
				btnFixExperts.SetButtonText("Fix Project Experts",
					System.Drawing.Color.Black, System.Drawing.Color.Black,
					System.Drawing.Color.White, System.Drawing.Color.Gray);
				btnFixExperts.Click += new System.EventHandler(this.btnFixExperts_Click);
				Controls.Add(btnFixExperts);
				NextButtonSlot++;
			}

			if (round >= 2)
			{
				btnTestInstall.SetVariants("images\\buttons\\button_255x25.png");
				btnTestInstall.Location = new System.Drawing.Point(90, 55 + 35 * NextButtonSlot);
				btnTestInstall.Size = new System.Drawing.Size(255, 25);
				btnTestInstall.TabIndex = 8;
				btnTestInstall.ButtonFont = MyDefaultSkinFontBold10;
				btnTestInstall.SetButtonText("Test Handover",
					System.Drawing.Color.Black, System.Drawing.Color.Black,
					System.Drawing.Color.White, System.Drawing.Color.Gray);
				btnTestInstall.Click += new System.EventHandler(btnTestInstall_Click);
				Controls.Add(btnTestInstall);
				NextButtonSlot++;
			}

			if (round >= 2)
			{
				btnShowResources.SetVariants("\\images\\buttons\\button_255x25.png");
				btnShowResources.Location = new System.Drawing.Point(90, 55 + 35 * NextButtonSlot);
				btnShowResources.Size = new System.Drawing.Size(255, 25);
				btnShowResources.TabIndex = 8;
				btnShowResources.ButtonFont = MyDefaultSkinFontBold10;
				btnShowResources.SetButtonText("Show Resource Plan",
					System.Drawing.Color.Black, System.Drawing.Color.Black,
					System.Drawing.Color.White, System.Drawing.Color.Gray);
				btnShowResources.Click += new System.EventHandler(this.btnShowPlan_Click);
				Controls.Add(btnShowResources);
				NextButtonSlot++;
			}

			//Form Cancel 
			//newBtnCancel.SetButton(AppInfo.TheInstance.Location+"\\images\\buttons\\button_70x25.png");
			newBtnCancel.SetVariants("images\\buttons\\button_70x25.png");
			newBtnCancel.Location = new System.Drawing.Point(400, 220);
			newBtnCancel.Name = "newBtnCancel";
			newBtnCancel.Size = new System.Drawing.Size(70, 25);
			newBtnCancel.TabIndex = 22;
			newBtnCancel.ButtonFont = MyDefaultSkinFontBold10;
			//newBtn.ForeColor = System.Drawing.Color.Black;
			newBtnCancel.SetButtonText("Close",
				System.Drawing.Color.Black, System.Drawing.Color.Black,
				System.Drawing.Color.White, System.Drawing.Color.Gray);
			//newBtn.ButtonPressed += new ProjectMngmntGamePanels.ImageButton.ImageButtonEventArgsHandler(playButton_ButtonPressed);
			newBtnCancel.Click += new System.EventHandler(this.newBtnCancel_Click);
			this.Controls.Add(newBtnCancel);
		}

		new public void Dispose()
		{
			if (MyDefaultSkinFontNormal8 != null)
			{
				MyDefaultSkinFontNormal8.Dispose();
				MyDefaultSkinFontNormal8 = null;
			}
			if (MyDefaultSkinFontNormal10 != null)
			{
				MyDefaultSkinFontNormal10.Dispose();
				MyDefaultSkinFontNormal10 = null;
			}
			if (MyDefaultSkinFontNormal12 != null)
			{
				MyDefaultSkinFontNormal12.Dispose();
				MyDefaultSkinFontNormal12 = null;
			}
			if (MyDefaultSkinFontBold8 != null)
			{
				MyDefaultSkinFontBold8.Dispose();
				MyDefaultSkinFontBold8 = null;
			}
			if (MyDefaultSkinFontBold10 != null)
			{
				MyDefaultSkinFontBold10.Dispose();
				MyDefaultSkinFontBold10 = null;
			}
			if (MyDefaultSkinFontBold12 != null)
			{
				MyDefaultSkinFontBold12.Dispose();
				MyDefaultSkinFontBold12 = null;
			}
			if (MyDefaultSkinFontBold24 != null)
			{
				MyDefaultSkinFontBold24.Dispose();
				MyDefaultSkinFontBold24 = null;
			}
		}

		private void btnPMOChange_Click(object sender, System.EventArgs e)
		{
			_mainPanel.SwapToOtherPanel(1);
		}
		private void btnShowBubbles_Click(object sender, System.EventArgs e)
		{
			_mainPanel.SwapToOtherPanel(2);
		}
		private void btnSetPredictedMarket_Click(object sender, System.EventArgs e)
		{
			_mainPanel.SwapToOtherPanel(3);
		}
		private void btnShowPlan_Click(object sender, System.EventArgs e)
		{
			_mainPanel.SwapToOtherPanel(4);
		}
		private void btnShowAnalysisCharts_Click(object sender, System.EventArgs e)
		{
			_mainPanel.SwapToOtherPanel(5);
		}
		private void btnFixExperts_Click(object sender, System.EventArgs e)
		{
			_mainPanel.SwapToOtherPanel(6);
		}
		private void btnChangeExpertsStatus_Click(object sender, System.EventArgs e)
		{
			_mainPanel.SwapToOtherPanel(7);
		}

		void btnShowProjectedGains_Click (object sender, System.EventArgs e)
		{
			opsEngine.CalculateProjectedBenefits();
			_mainPanel.DisposeEntryPanel();
		}

		private void newBtnCancel_Click(object sender, System.EventArgs e)
		{
			_mainPanel.DisposeEntryPanel();
		}

		void btnTestInstall_Click (object sender, System.EventArgs e)
		{
			DataEntryPanel_Handover myPrjHandover_EntryPanel = new DataEntryPanel_Handover (_mainPanel, MyNodeTree, gameFile, true);
			myPrjHandover_EntryPanel.Size = Size;
			myPrjHandover_EntryPanel.Location = Location;
			this.Parent.Parent.Controls.Add(myPrjHandover_EntryPanel);
			myPrjHandover_EntryPanel.BringToFront();

			_mainPanel.DisposeEntryPanel();
			_mainPanel.ShowEntryPanel(myPrjHandover_EntryPanel);
			((ImageButton) sender).Active = true;
			myPrjHandover_EntryPanel.Focus();
		}

		public void SetFocus ()
		{
			Focus();
			btnPMOChange.Focus();
		}
	}
}