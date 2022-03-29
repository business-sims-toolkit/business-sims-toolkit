using System;
using System.Collections;
using System.Collections.Generic;

using System.Drawing;
using System.Windows.Forms;

using Network;

using CommonGUI;

using LibCore;
using CoreUtils;

namespace Polestar_PM.OpsGUI
{
	public class DataEntryPanel_AddProgram : FlickerFreePanel
	{
		IDataEntryControlHolder mainPanel;

		Polestar_PM.OpsEngine.PM_OpsEngine_Round3 opsEngine;

		NodeTree model;
		Node portfoliosNode;
		Node portfolioNode;
		Node programNode;

		protected Font MyDefaultSkinFontNormal8 = null;
		protected Font MyDefaultSkinFontNormal10 = null;
		protected Font MyDefaultSkinFontNormal12 = null;
		protected Font MyDefaultSkinFontBold8 = null;
		protected Font MyDefaultSkinFontBold10 = null;
		protected Font MyDefaultSkinFontBold12 = null;
		protected Font MyDefaultSkinFontBold20 = null;

		ArrayList disposableButtons;

		private ImageTextButton newBtnOK = new ImageTextButton(0);
		private ImageTextButton newBtnCancel = new ImageTextButton(0);

		private System.Windows.Forms.Panel pnl_ChooseSlot;
		private System.Windows.Forms.Label titleLabel;
		private System.Windows.Forms.Label helpLabel;

		Label programLegendLabel;
		ImageTextButton programLegendButton;

		public DataEntryPanel_AddProgram (IDataEntryControlHolder mainPanel, NodeTree model, Polestar_PM.OpsEngine.PM_OpsEngine_Round3 opsEngine)
		{
			this.opsEngine = opsEngine;

			this.mainPanel = mainPanel;

			this.model = model;
			portfoliosNode = model.GetNamedNode("Portfolios");

			this.BackgroundImage = LibCore.Repository.TheInstance.GetImage(LibCore.AppInfo.TheInstance.Location + "images\\panels\\PM_opsback.png");
			string fontname =  SkinningDefs.TheInstance.GetData("fontname");
			MyDefaultSkinFontNormal8 = ConstantSizeFont.NewFont(fontname,8);
			MyDefaultSkinFontNormal10 = ConstantSizeFont.NewFont(fontname,10);
			MyDefaultSkinFontNormal12 = ConstantSizeFont.NewFont(fontname,12);
			MyDefaultSkinFontBold8 = ConstantSizeFont.NewFont(fontname,8,FontStyle.Bold);
			MyDefaultSkinFontBold10 = ConstantSizeFont.NewFont(fontname,10,FontStyle.Bold);
			MyDefaultSkinFontBold12 = ConstantSizeFont.NewFont(fontname,12,FontStyle.Bold);
			MyDefaultSkinFontBold20 = ConstantSizeFont.NewFont(fontname,20,FontStyle.Bold);

			titleLabel = new System.Windows.Forms.Label();
			titleLabel.BackColor = System.Drawing.Color.Transparent;
			titleLabel.Font = MyDefaultSkinFontBold12;
			titleLabel.ForeColor = System.Drawing.Color.Black;
			titleLabel.Location = new System.Drawing.Point(110-25, 10-2);
			titleLabel.Name = "titleLabel";
			titleLabel.Size = new System.Drawing.Size(380,18);
			titleLabel.TabIndex = 11;
			titleLabel.Text = "Add Program";
			titleLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.Controls.Add(titleLabel);

			helpLabel = new System.Windows.Forms.Label();
			helpLabel.BackColor = System.Drawing.Color.Transparent;
			helpLabel.Font = MyDefaultSkinFontNormal10;
			helpLabel.ForeColor = System.Drawing.Color.Black;
			helpLabel.Location = new System.Drawing.Point(110-25, 50-20-1);
			helpLabel.Name = "helpLabel";
			helpLabel.Size = new System.Drawing.Size(380,18);
			helpLabel.TabIndex = 20;
			helpLabel.Text = "Select Portfolio";
			helpLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.Controls.Add(helpLabel);

			programLegendLabel = new Label ();
			programLegendLabel.BackColor = System.Drawing.Color.Transparent;
			programLegendLabel.Font = MyDefaultSkinFontNormal10;
			programLegendLabel.ForeColor = System.Drawing.Color.Black;
			programLegendLabel.Location = new System.Drawing.Point(3, 0+4);
			programLegendLabel.Size = new System.Drawing.Size(70, 20);
			programLegendLabel.Text = "Program";
			programLegendLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			programLegendLabel.Visible = false;
			this.Controls.Add(programLegendLabel);

			programLegendButton = new ImageTextButton (0);
			programLegendButton.SetButton(AppInfo.TheInstance.Location+"\\images\\buttons\\selection_tab.png");
			programLegendButton.Location = new System.Drawing.Point(10-2+14, 20+4);
			programLegendButton.Size = new System.Drawing.Size(58, 27);
			programLegendButton.ButtonFont = this.MyDefaultSkinFontBold12;
			programLegendButton.SetButtonText("No",
				System.Drawing.Color.Black,System.Drawing.Color.Black,
				System.Drawing.Color.Green,System.Drawing.Color.Gray);
			programLegendButton.Click += new System.EventHandler (programLegendButton_Click);
			programLegendButton.Text = "";
			programLegendButton.Visible = false; 
			this.Controls.Add(programLegendButton);
			
			BuildBaseControls();
			BuildPanelButtons();
		}

		public void BuildPanelButtons()
		{
			newBtnCancel.SetButton(AppInfo.TheInstance.Location+"\\images\\buttons\\button_70x25.png");
			newBtnCancel.Location = new System.Drawing.Point(430-30, 240-20);
			newBtnCancel.Name = "newBtnCancel";
			newBtnCancel.Size = new System.Drawing.Size(70, 25);
			newBtnCancel.TabIndex = 22;
			newBtnCancel.ButtonFont = MyDefaultSkinFontBold10;
			newBtnCancel.SetButtonText("Close",
				System.Drawing.Color.Black,System.Drawing.Color.Black,
				System.Drawing.Color.Green,System.Drawing.Color.Gray);
			newBtnCancel.Click += new System.EventHandler (this.newBtnCancel_Click);
			this.Controls.Add(newBtnCancel);

			newBtnOK.SetButton(AppInfo.TheInstance.Location+"\\images\\buttons\\button_70x25.png");
			newBtnOK.Location = new System.Drawing.Point(330-30, 240-20);
			newBtnOK.Name = "newBtnOK";
			newBtnOK.Size = new System.Drawing.Size(70, 25);
			newBtnOK.TabIndex = 21;
			newBtnOK.ButtonFont = MyDefaultSkinFontBold10;
			newBtnOK.SetButtonText("OK",
				System.Drawing.Color.Black,System.Drawing.Color.Black,
				System.Drawing.Color.Green,System.Drawing.Color.Gray);
			newBtnOK.Click += new System.EventHandler(this.newBtnOK_Click);
			newBtnOK.Visible = false;
			this.Controls.Add(newBtnOK);
		}

		public void BuildBaseControls() 
		{
			pnl_ChooseSlot = new System.Windows.Forms.Panel();
			pnl_ChooseSlot.SuspendLayout();
			this.SuspendLayout();

			pnl_ChooseSlot.Location = new System.Drawing.Point(90+10-22, 30+75-40);
			pnl_ChooseSlot.Name = "pnl_ChooseSlot";
			pnl_ChooseSlot.Size = new System.Drawing.Size(230+90+80, 110);
			pnl_ChooseSlot.BackColor = Color.Transparent;

			GoToFirstStage();
			
			this.Controls.Add(pnl_ChooseSlot);
			this.Size = new System.Drawing.Size(520,280);
			this.pnl_ChooseSlot.ResumeLayout(false);
			this.ResumeLayout(false);
		}

		public void BuildProgramButtonControls (Node portfolioNode) 
		{
			int offset_x=5;
			int offset_y=10;
			int button_width=50;
			int button_height=40;
			int button_sep=5;

			helpLabel.Text = "Select Program";

			disposableButtons = new ArrayList ();

			int x = offset_x;
			int y = offset_y;

			List<Node> programs = new List<Node> ();
			ArrayList activePrograms = portfolioNode.GetChildrenOfType("Program");
			ArrayList availablePrograms = portfoliosNode.GetChildrenOfType("Program");
			programs.AddRange((Node []) activePrograms.ToArray(typeof (Node)));
			programs.AddRange((Node []) availablePrograms.ToArray(typeof (Node)));
			programs.Sort(new Comparison<Node> (delegate (Node a, Node b) { return a.GetAttribute("shortdesc").CompareTo(b.GetAttribute("shortdesc")); }));

			foreach (Node programNode in programs)
			{
				if ((x + button_width) >= (this.pnl_ChooseSlot.Width - offset_x))
				{
					x = offset_x;
					y += button_height + button_sep;
				}

				ImageTextButton button = new ImageTextButton(0);
				button.SetButton(AppInfo.TheInstance.Location+"\\images\\buttons\\button_50x40.png");
				button.Location = new System.Drawing.Point(x, y);
				button.Size = new System.Drawing.Size(button_width, button_height);
				button.ButtonFont = MyDefaultSkinFontNormal8;
				button.Tag = programNode;
				button.SetButtonText(programNode.GetAttribute("shortdesc"),
					System.Drawing.Color.Black,System.Drawing.Color.Black,
					System.Drawing.Color.Green,System.Drawing.Color.Gray);
				button.Click += new System.EventHandler(this.programButton_Click);
				button.Enabled = availablePrograms.Contains(programNode);
				this.pnl_ChooseSlot.Controls.Add(button);
				disposableButtons.Add(button);

				x = button.Right + button_sep;
			}
		}

		public void BuildResourceControls (Node programNode) 
		{
			helpLabel.Text = "Confirm Program";

			programLegendButton.SetButtonText(programNode.GetAttribute("shortdesc"));
			programLegendLabel.Show();
			programLegendButton.Show();

			newBtnOK.Visible = true;
			newBtnOK.Enabled = (portfolioNode.GetChildrenOfType("Program").Count < 6);

			disposableButtons = new ArrayList ();
		}

		void newBtnCancel_Click (object sender, EventArgs args)
		{
			mainPanel.DisposeEntryPanel();
		}

		void newBtnOK_Click (object sender, EventArgs args)
		{
			AcceptValues();
		}

		void ClearDisposableButtons ()
		{
			newBtnOK.Visible = false;

			if (disposableButtons != null)
			{
				foreach (Control control in disposableButtons)
				{
					this.pnl_ChooseSlot.Controls.Remove(control);
				}
			}
		}

		void GoToFirstStage ()
		{
			ClearDisposableButtons();

			portfolioNode = portfoliosNode.GetFirstChildOfType("Portfolio");

			programLegendLabel.Hide();
			programLegendButton.Hide();

			GoToChosenPortfolioStage(portfolioNode);
		}

		void portfolioButton_Click (object sender, EventArgs args)
		{
			Control button = sender as Control;
			portfolioNode = button.Tag as Node;

			GoToChosenPortfolioStage(portfolioNode);
		}

		void GoToChosenPortfolioStage (Node portfolioNode)
		{
			ClearDisposableButtons();

			programLegendLabel.Hide();
			programLegendButton.Hide();

			BuildProgramButtonControls(portfolioNode);
		}

		void programButton_Click (object sender, EventArgs args)
		{
			Control button = sender as Control;
			programNode = button.Tag as Node;

			bool showConfirmScreen = false;
			if (showConfirmScreen)
			{
				GoToChosenProgramStage(programNode);
			}
			else
			{
				AcceptValues();
			}
			//GoToChosenProgramStage(programNode);
		}

		void GoToChosenProgramStage (Node programNode)
		{
			ClearDisposableButtons();

			programLegendLabel.Show();
			programLegendButton.Show();

			BuildResourceControls(programNode);
		}

		void AcceptValues ()
		{
			Polestar_PM.OpsEngine.PM_OpsEngine_Round3.AddProgram(portfoliosNode, portfolioNode, programNode);

			mainPanel.DisposeEntryPanel();
		}

		void portfolioLegendButton_Click (object sender, EventArgs args)
		{
			GoToFirstStage();
		}

		void programLegendButton_Click (object sender, EventArgs args)
		{
			GoToChosenPortfolioStage(portfolioNode);
		}
	}
}