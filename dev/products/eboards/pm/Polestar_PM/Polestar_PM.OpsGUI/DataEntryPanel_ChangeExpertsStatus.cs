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
	/// This allows the facilitator to switch on/off the Experts system in Round 3 
	/// </summary>

	public class DataEntryPanel_ChangeExpertsStatus  : FlickerFreePanel
	{
		protected IDataEntryControlHolder _mainPanel;

		protected NodeTree MyNodeTree = null;
		protected Node queueNode = null;
		protected Node ExpertsNode = null;
		protected bool expertsEnabled = false;

		protected Font MyDefaultSkinFontNormal10 = null;
		protected Font MyDefaultSkinFontBold10 = null;
		protected Font MyDefaultSkinFontBold12 = null;

		private ImageTextButton newBtnOK = new ImageTextButton(0);
		private ImageTextButton newBtnCancel = new ImageTextButton(0);

		private System.Windows.Forms.Label lblChangeExpertsStatusTitle;
		private ImageTextButton newBtnEnableExperts = new ImageTextButton(0);
		private ImageTextButton newBtnDisableExperts = new ImageTextButton(0);

		private System.Windows.Forms.Label lblErrorMessage;
		private System.Windows.Forms.Label titleLabel;
		private System.Windows.Forms.Label helpLabel;

		public DataEntryPanel_ChangeExpertsStatus(IDataEntryControlHolder mainPanel, NodeTree tree)
		{
			MyNodeTree = tree;
			_mainPanel = mainPanel;
			queueNode = tree.GetNamedNode("TaskManager");

			//<experts name="experts" enabled="false">
			ExpertsNode = tree.GetNamedNode("experts");
			expertsEnabled = ExpertsNode.GetBooleanAttribute("enabled", false);

			string fontname =  SkinningDefs.TheInstance.GetData("fontname");
			MyDefaultSkinFontNormal10 = ConstantSizeFont.NewFont(fontname,10);
			MyDefaultSkinFontBold10 = ConstantSizeFont.NewFont(fontname,10,FontStyle.Bold);
			MyDefaultSkinFontBold12 = ConstantSizeFont.NewFont(fontname,12,FontStyle.Bold);

			this.BackgroundImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images\\panels\\PM_opsback.png");

			//this.ShowInTaskbar = false;
			//this.ClientSize = new Size (520,280);
			//this.FormBorderStyle = FormBorderStyle.None;
			//this.Opacity = 1.00;
			this.Size = new Size(520,255);

			BuildBaseControls();

			titleLabel = new System.Windows.Forms.Label();
			titleLabel.BackColor = System.Drawing.Color.Transparent;
			titleLabel.Font = MyDefaultSkinFontBold12;
			titleLabel.ForeColor = System.Drawing.Color.Black;
			titleLabel.Location = new System.Drawing.Point(110-25, 10-2);
			titleLabel.Name = "titleLabel";
			titleLabel.Size = new System.Drawing.Size(380, 18);
			titleLabel.TabIndex = 11;
			titleLabel.Text = "Change Experts Status";
			titleLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.Controls.Add(titleLabel);

			helpLabel = new System.Windows.Forms.Label();
			helpLabel.BackColor = System.Drawing.Color.Transparent;
			helpLabel.Font = MyDefaultSkinFontNormal10;
			helpLabel.ForeColor = System.Drawing.Color.Black;
			helpLabel.Location = new System.Drawing.Point(110-25, 50-20-1);
			helpLabel.Name = "helpLabel";
			helpLabel.Size = new System.Drawing.Size(380, 18);
			helpLabel.TabIndex = 20;
			helpLabel.Text = "Please use the button below.";
			helpLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.Controls.Add(helpLabel);

			newBtnCancel.SetVariants("images\\buttons\\button_70x25.png");
			newBtnCancel.Location = new System.Drawing.Point(400, 220);
			newBtnCancel.Name = "newBtnCancel";
			newBtnCancel.Size = new System.Drawing.Size(70, 25);
			newBtnCancel.TabIndex = 22;
			newBtnCancel.ButtonFont = MyDefaultSkinFontBold10;
			newBtnCancel.SetButtonText("Cancel",
				System.Drawing.Color.Black, System.Drawing.Color.Black,
				System.Drawing.Color.Green, System.Drawing.Color.Gray);
			//newBtn.ButtonPressed += new ProjectMngmntGamePanels.ImageButton.ImageButtonEventArgsHandler(playButton_ButtonPressed);
			newBtnCancel.Click += new System.EventHandler(this.newBtnCancel_Click);
			this.Controls.Add(newBtnCancel);
		}

		new public void Dispose()
		{
			ExpertsNode = null;

			if (MyDefaultSkinFontNormal10 != null)
			{
				MyDefaultSkinFontNormal10.Dispose();
				MyDefaultSkinFontNormal10 = null;
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
		}

		public void BuildBaseControls() 
		{
			this.lblChangeExpertsStatusTitle = new System.Windows.Forms.Label();
			this.lblErrorMessage = new System.Windows.Forms.Label();
			this.SuspendLayout();

			//lblChangeExpertsStatusTitle.BackColor = System.Drawing.Color.Transparent;
			//lblChangeExpertsStatusTitle.Font = MyDefaultSkinFontBold10;
			//lblChangeExpertsStatusTitle.ForeColor = System.Drawing.Color.Black;
			//lblChangeExpertsStatusTitle.Location = new System.Drawing.Point(90, 100);
			//lblChangeExpertsStatusTitle.Size = new System.Drawing.Size(240, 25);
			//lblChangeExpertsStatusTitle.TabIndex = 11;
			//lblChangeExpertsStatusTitle.Text = "Change Experts System Status";
			//lblChangeExpertsStatusTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			//this.Controls.Add(this.lblChangeExpertsStatusTitle);

			newBtnEnableExperts.SetVariants("images\\buttons\\button_255x25.png");
			newBtnEnableExperts.Location = new System.Drawing.Point(140, 100);
			newBtnEnableExperts.Name = "newBtnOK";
			newBtnEnableExperts.Size = new System.Drawing.Size(255, 25);
			newBtnEnableExperts.TabIndex = 21;
			newBtnEnableExperts.ButtonFont = MyDefaultSkinFontBold10;
			newBtnEnableExperts.SetButtonText("Enable Experts System",
				System.Drawing.Color.Black, System.Drawing.Color.Black,
				System.Drawing.Color.White, System.Drawing.Color.Gray);
			newBtnEnableExperts.Click += new System.EventHandler(this.newBtnEnableExperts_Click);
			newBtnEnableExperts.Visible = !expertsEnabled;
			newBtnEnableExperts.Enabled = !expertsEnabled;
			this.Controls.Add(newBtnEnableExperts);

			newBtnDisableExperts.SetVariants("images\\buttons\\button_255x25.png");
			newBtnDisableExperts.Location = new System.Drawing.Point(140, 100);
			newBtnDisableExperts.Name = "newBtnDisableExperts";
			newBtnDisableExperts.Size = new System.Drawing.Size(255, 25);
			newBtnDisableExperts.TabIndex = 21;
			newBtnDisableExperts.ButtonFont = MyDefaultSkinFontBold10;
			//newBtn.ForeColor = System.Drawing.Color.Black;
			newBtnDisableExperts.SetButtonText("Disable Experts System",
				System.Drawing.Color.Black, System.Drawing.Color.Black,
				System.Drawing.Color.White, System.Drawing.Color.Gray);
			newBtnDisableExperts.Click += new System.EventHandler(this.newBtnDisableExperts_Click);
			newBtnDisableExperts.Visible = expertsEnabled;
			newBtnDisableExperts.Enabled = expertsEnabled;
			this.Controls.Add(newBtnDisableExperts);

			lblErrorMessage.BackColor = System.Drawing.Color.Transparent;
			//lblErrorMessage.BackColor = Color.Red;
			lblErrorMessage.Font = MyDefaultSkinFontBold10;
			lblErrorMessage.ForeColor = System.Drawing.Color.Red;
			lblErrorMessage.Location = new System.Drawing.Point(70, 140);
			lblErrorMessage.Size = new System.Drawing.Size(385, 40);
			lblErrorMessage.TabIndex = 11;
			lblErrorMessage.Text = "";
			lblErrorMessage.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			lblErrorMessage.Visible = false;
			Controls.Add(this.lblErrorMessage);

			this.Name = "ChangeExpertsStatus";
			this.Size = new System.Drawing.Size(520,280);
			this.ResumeLayout(false);
		}

		private bool HandleOrder(bool status)
		{
			ArrayList attrs = new ArrayList ();
			attrs.Add(new AttributeValuePair("cmd_type", "enable_all_experts"));

			if (status)
			{
				attrs.Add(new AttributeValuePair("status", "true"));
			}
			else
			{
				attrs.Add(new AttributeValuePair("status", "false"));
			}
			new Node (queueNode, "task", "", attrs);	
			_mainPanel.DisposeEntryPanel();
			return true;
		}

		private void newBtnCancel_Click(object sender, System.EventArgs e)
		{
			_mainPanel.DisposeEntryPanel();
		}

		private void newBtnEnableExperts_Click(object sender, System.EventArgs e)
		{
			HandleOrder(true);
			_mainPanel.DisposeEntryPanel();
		}
		private void newBtnDisableExperts_Click(object sender, System.EventArgs e)
		{
			HandleOrder(false);
			_mainPanel.DisposeEntryPanel();
		}

		public void SetFocus ()
		{
			Focus();
		}
	}
}