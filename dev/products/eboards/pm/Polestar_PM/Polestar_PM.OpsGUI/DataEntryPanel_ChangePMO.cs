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

	public class DataEntryPanel_ChangePMO  : FlickerFreePanel
	{
		protected IDataEntryControlHolder _mainPanel;

		protected NodeTree MyNodeTree = null;
		protected Node queueNode = null;
		protected Node BudgetNode = null;

		protected int current_budget;
		protected int requested_budget;
		protected int current_spend;
		//protected int current_cancellation_money;
		protected int current_budget_left;
		protected int requested_budget_left;

		protected Font MyDefaultSkinFontNormal8 = null;
		protected Font MyDefaultSkinFontNormal10 = null;
		protected Font MyDefaultSkinFontNormal12 = null;
		protected Font MyDefaultSkinFontBold8 = null;
		protected Font MyDefaultSkinFontBold10 = null;
		protected Font MyDefaultSkinFontBold12 = null;
		protected Font MyDefaultSkinFontBold24 = null;

		private ImageTextButton newBtnOK = new ImageTextButton(0);
		private ImageTextButton newBtnCancel = new ImageTextButton(0);

		private System.Windows.Forms.Label lblCurrent_PMO_Budget_Title;
		private System.Windows.Forms.TextBox tbNew_PMO_Budget_Value;
		private System.Windows.Forms.Label lblErrorMessage;

		private System.Windows.Forms.Label titleLabel;
		private System.Windows.Forms.Label helpLabel;

		public DataEntryPanel_ChangePMO(IDataEntryControlHolder mainPanel, NodeTree tree)
		{
			MyNodeTree = tree;
			_mainPanel = mainPanel;
			queueNode = tree.GetNamedNode("TaskManager");
			BudgetNode = tree.GetNamedNode("pmo_budget");
			current_budget = BudgetNode.GetIntAttribute("budget_allowed", 0);
			current_spend = BudgetNode.GetIntAttribute("budget_spent", 0);
			//current_cancellation_money = BudgetNode.GetIntAttribute("cancellation_money", 0);
			//current_budget_left = current_budget - (current_spend + current_cancellation_money);
			current_budget_left = current_budget - (current_spend);

			requested_budget = current_budget;
			//requested_budget_left = current_budget - (current_spend + current_cancellation_money);
			requested_budget_left = current_budget - (current_spend);

			current_budget = current_budget / 1000;
			requested_budget = requested_budget / 1000;
			current_spend = current_spend / 1000;
			//current_cancellation_money = current_cancellation_money / 1000;
			current_budget_left = current_budget_left / 1000;
			requested_budget_left = requested_budget_left / 1000;
			
			string fontname =  SkinningDefs.TheInstance.GetData("fontname");
			MyDefaultSkinFontNormal8 = ConstantSizeFont.NewFont(fontname,8);
			MyDefaultSkinFontNormal10 = ConstantSizeFont.NewFont(fontname,10);
			MyDefaultSkinFontNormal12 = ConstantSizeFont.NewFont(fontname,12);
			MyDefaultSkinFontBold8 = ConstantSizeFont.NewFont(fontname,8,FontStyle.Bold);
			MyDefaultSkinFontBold10 = ConstantSizeFont.NewFont(fontname,10,FontStyle.Bold);
			MyDefaultSkinFontBold12 = ConstantSizeFont.NewFont(fontname,12,FontStyle.Bold);
			MyDefaultSkinFontBold24 = ConstantSizeFont.NewFont(fontname,24,FontStyle.Bold);

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
			titleLabel.ForeColor = System.Drawing.Color.White;
			titleLabel.Location = new System.Drawing.Point(110-25, 10-2);
			titleLabel.Name = "titleLabel";
			titleLabel.Size = new System.Drawing.Size(380, 18);
			titleLabel.TabIndex = 11;
			titleLabel.Text = "Change PMO";
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
			helpLabel.Text = "Please enter the new level of PMO budget";
			helpLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.Controls.Add(helpLabel);

			//newBtnOK.SetButton(AppInfo.TheInstance.Location+"\\images\\buttons\\button_70x25.png");
			newBtnOK.SetVariants("images\\buttons\\button_70x25.png");
			newBtnOK.Location = new System.Drawing.Point(300, 220);
			newBtnOK.Name = "newBtnOK";
			newBtnOK.Size = new System.Drawing.Size(70, 25);
			newBtnOK.TabIndex = 21;
			newBtnOK.ButtonFont = MyDefaultSkinFontBold10;
			//newBtn.ForeColor = System.Drawing.Color.Black;
			newBtnOK.SetButtonText("OK",
				System.Drawing.Color.Black, System.Drawing.Color.Black,
				System.Drawing.Color.Green, System.Drawing.Color.Gray);
			//newBtn.ButtonPressed += new ProjectMngmntGamePanels.ImageButton.ImageButtonEventArgsHandler(playButton_ButtonPressed);
			newBtnOK.Click += new System.EventHandler(this.newBtnOK_Click);
			//newBtnOK.Visible = false;
			newBtnOK.Visible = true;
			newBtnOK.Enabled = true;
			this.Controls.Add(newBtnOK);

			//newBtnCancel.SetButton(AppInfo.TheInstance.Location+"\\images\\buttons\\button_70x25.png");
			newBtnCancel.SetVariants("images\\buttons\\button_70x25.png");
			newBtnCancel.Location = new System.Drawing.Point(400, 220);
			newBtnCancel.Name = "newBtnCancel";
			newBtnCancel.Size = new System.Drawing.Size(70, 25);
			newBtnCancel.TabIndex = 22;
			newBtnCancel.ButtonFont = MyDefaultSkinFontBold10;
			//newBtn.ForeColor = System.Drawing.Color.Black;
			newBtnCancel.SetButtonText("Cancel",
				System.Drawing.Color.Black, System.Drawing.Color.Black,
				System.Drawing.Color.Green, System.Drawing.Color.Gray);
			//newBtn.ButtonPressed += new ProjectMngmntGamePanels.ImageButton.ImageButtonEventArgsHandler(playButton_ButtonPressed);
			newBtnCancel.Click += new System.EventHandler(this.newBtnCancel_Click);
			this.Controls.Add(newBtnCancel);
		}

		new public void Dispose()
		{
			queueNode = null;
			BudgetNode = null;

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

		public void BuildBaseControls() 
		{
			int offset_x = 85;
			int offset_y = 55;

			this.lblCurrent_PMO_Budget_Title = new System.Windows.Forms.Label();
			this.tbNew_PMO_Budget_Value = new FilteredTextBox (TextBoxFilterType.Digits);
			this.lblErrorMessage = new System.Windows.Forms.Label();
			
			this.SuspendLayout();

			lblCurrent_PMO_Budget_Title.BackColor = System.Drawing.Color.Transparent;
			//lblCurrent_PMO_Budget_Title.BackColor = Color.Red;
			lblCurrent_PMO_Budget_Title.Font = MyDefaultSkinFontBold10;
			lblCurrent_PMO_Budget_Title.ForeColor = System.Drawing.Color.Black;
			lblCurrent_PMO_Budget_Title.Location = new System.Drawing.Point(offset_x + 5, offset_y + 40);
			lblCurrent_PMO_Budget_Title.Size = new System.Drawing.Size(160, 25);
			lblCurrent_PMO_Budget_Title.TabIndex = 11;
			lblCurrent_PMO_Budget_Title.Text = "PMO Budget ($K)";
			lblCurrent_PMO_Budget_Title.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

			tbNew_PMO_Budget_Value.BackColor = System.Drawing.Color.Black;
			tbNew_PMO_Budget_Value.Font = this.MyDefaultSkinFontBold10;
			tbNew_PMO_Budget_Value.ForeColor = System.Drawing.Color.White;
			tbNew_PMO_Budget_Value.Location = new System.Drawing.Point(offset_x + 315, offset_y + 40);
			tbNew_PMO_Budget_Value.Name = "tbPMOBudget";
			tbNew_PMO_Budget_Value.Size = new System.Drawing.Size(70, 26);
			tbNew_PMO_Budget_Value.TabIndex = 28;
			tbNew_PMO_Budget_Value.Text = CONVERT.ToStr(requested_budget);
			tbNew_PMO_Budget_Value.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
			tbNew_PMO_Budget_Value.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
			tbNew_PMO_Budget_Value.TextChanged += new EventHandler(tbNew_PMO_Budget_Value_TextChanged);

			lblErrorMessage.BackColor = System.Drawing.Color.Transparent;
			//lblErrorMessage.BackColor = Color.Red;
			lblErrorMessage.Font = MyDefaultSkinFontBold10;
			lblErrorMessage.ForeColor = System.Drawing.Color.Red;
			lblErrorMessage.Location = new System.Drawing.Point(offset_x + 5, offset_y + 80);
			lblErrorMessage.Size = new System.Drawing.Size(385, 40);
			lblErrorMessage.TabIndex = 11;
			lblErrorMessage.Text = "";
			lblErrorMessage.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			lblErrorMessage.Visible = false;

			Controls.Add(this.lblCurrent_PMO_Budget_Title);
			Controls.Add(this.tbNew_PMO_Budget_Value);
			Controls.Add(this.lblErrorMessage);

			this.Name = "ChangePMOControl";
			this.Size = new System.Drawing.Size(520,280);
			this.ResumeLayout(false);
		}

		private void tbNew_PMO_Budget_Value_TextChanged(object sender, EventArgs e)
		{
			//No longer used

			//int newValue = CONVERT.ParseIntSafe(this.tbNew_PMO_Budget_Value.Text, 0);

			//requested_budget = newValue;
			////requested_budget_left = requested_budget - (current_spend + current_cancellation_money);
			//requested_budget_left = requested_budget - (current_spend);

			//tbNew_PMO_Budget_Value.Text = CONVERT.ToStr(requested_budget);
			////lblNew_PMO_Spend_Value.Text = CONVERT.ToStr(current_spend + current_cancellation_money);

			//if (requested_budget_left > 0)
			//{
			//  this.newBtnOK.Enabled = true;
			//  lblErrorMessage.Visible = false;
			//}
			//else
			//{
			//  lblErrorMessage.Visible = true;
			//  lblErrorMessage.Text = "Unable to reduce Budget below current spend.";
			//  this.newBtnOK.Enabled = false;
			//}
		}

		private bool CheckOrder()
		{
			bool OpSuccess = true;
			//extract the data 
			int newValue = CONVERT.ParseIntSafe(this.tbNew_PMO_Budget_Value.Text, 0);

			requested_budget = newValue;
			//requested_budget_left = requested_budget - (current_spend + current_cancellation_money);
			requested_budget_left = requested_budget - (current_spend);

			tbNew_PMO_Budget_Value.Text = CONVERT.ToStr(requested_budget);
			//lblNew_PMO_Spend_Value.Text = CONVERT.ToStr(current_spend + current_cancellation_money);

			if (requested_budget_left > 0)
			{
				lblErrorMessage.Visible = false;
				OpSuccess = true;
			}
			else
			{
				lblErrorMessage.Visible = true;
				lblErrorMessage.Text = "Unable to reduce Budget below current spend.";
				OpSuccess = false;
			}
			return OpSuccess;
		}

		private bool HandleOrder()
		{
			//extract the data 
			requested_budget = CONVERT.ParseIntSafe(this.tbNew_PMO_Budget_Value.Text, 0);

			//Mind that money is displayed in $K but internally held as units 
			requested_budget = requested_budget * 1000;
			ArrayList attrs = new ArrayList ();
			attrs.Add(new AttributeValuePair ("cmd_type", "change_pmo"));
			attrs.Add(new AttributeValuePair("pmo_newvalue", CONVERT.ToStr(requested_budget)));
			new Node (queueNode, "task", "", attrs);	

			_mainPanel.DisposeEntryPanel();
			return true;
		}

		private void newBtnOK_Click(object sender, System.EventArgs e)
		{
			bool good_order_placed = CheckOrder();
			if (good_order_placed)
			{
				good_order_placed = HandleOrder();
			}
		}

		private void newBtnCancel_Click(object sender, System.EventArgs e)
		{
			_mainPanel.DisposeEntryPanel();
		}

		public void SetFocus ()
		{
			Focus();
			tbNew_PMO_Budget_Value.Focus();
		}
	}
}