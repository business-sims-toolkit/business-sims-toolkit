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
	/// ChangePredictedMarketPos allows the players to change the predicted Market Place 
	/// </summary>

	public class ChangePredictedMarketPos  : FlickerFreePanel
	{
		protected IDataEntryControlHolder _mainPanel;

		protected NodeTree MyNodeTree = null;
		protected Node queueNode = null;
		protected Node OpsResultsNode = null;

		protected int current_predicted_market_pos;

		protected Font MyDefaultSkinFontNormal8 = null;
		protected Font MyDefaultSkinFontNormal10 = null;
		protected Font MyDefaultSkinFontNormal12 = null;
		protected Font MyDefaultSkinFontBold8 = null;
		protected Font MyDefaultSkinFontBold10 = null;
		protected Font MyDefaultSkinFontBold12 = null;
		protected Font MyDefaultSkinFontBold24 = null;

		private ImageTextButton newBtnOK = new ImageTextButton(0);
		private ImageTextButton newBtnCancel = new ImageTextButton(0);

		private System.Windows.Forms.Label lblPredictedMarketPos_Title;
		private System.Windows.Forms.TextBox tbPredictedMarketPos;
		private System.Windows.Forms.Label lblErrorMessage;

		private System.Windows.Forms.Label titleLabel;
		private System.Windows.Forms.Label helpLabel;

		public ChangePredictedMarketPos(IDataEntryControlHolder mainPanel, NodeTree tree)
		{
			MyNodeTree = tree;
			_mainPanel = mainPanel;
			queueNode = tree.GetNamedNode("TaskManager");
			OpsResultsNode = tree.GetNamedNode("operational_results");
			current_predicted_market_pos = OpsResultsNode.GetIntAttribute("predicted_market_position", 0);
			
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
			titleLabel.Text = "Change Predicted Market Position";
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
			helpLabel.Text = "Please enter the new Predicted Market Position";
			helpLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.Controls.Add(helpLabel);

			//newBtnOK.SetButton(AppInfo.TheInstance.Location + "\\images\\buttons\\button_70x25.png");
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

			//newBtnCancel.SetButton(AppInfo.TheInstance.Location + "\\images\\buttons\\button_70x25.png");
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
			OpsResultsNode = null;

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

			this.lblPredictedMarketPos_Title = new System.Windows.Forms.Label();
			this.tbPredictedMarketPos = new FilteredTextBox (TextBoxFilterType.Digits);
			this.lblErrorMessage = new System.Windows.Forms.Label();
			
			this.SuspendLayout();

			lblPredictedMarketPos_Title.BackColor = System.Drawing.Color.Transparent;
			//lblCurrent_PMO_Budget_Title.BackColor = Color.Red;
			lblPredictedMarketPos_Title.Font = MyDefaultSkinFontBold10;
			lblPredictedMarketPos_Title.ForeColor = System.Drawing.Color.Black;
			lblPredictedMarketPos_Title.Location = new System.Drawing.Point(offset_x + 5, offset_y + 40);
			lblPredictedMarketPos_Title.Size = new System.Drawing.Size(260, 25);
			lblPredictedMarketPos_Title.TabIndex = 11;
			lblPredictedMarketPos_Title.Text = "Predicted Market Position";
			lblPredictedMarketPos_Title.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

			tbPredictedMarketPos.BackColor = System.Drawing.Color.Black;
			tbPredictedMarketPos.Font = this.MyDefaultSkinFontBold10;
			tbPredictedMarketPos.ForeColor = System.Drawing.Color.White;
			tbPredictedMarketPos.Location = new System.Drawing.Point(offset_x + 315, offset_y + 40);
			tbPredictedMarketPos.Name = "tbPredictedMarketPos";
			tbPredictedMarketPos.Size = new System.Drawing.Size(70, 26);
			tbPredictedMarketPos.TabIndex = 28;
			tbPredictedMarketPos.Text = CONVERT.ToStr(current_predicted_market_pos);
			tbPredictedMarketPos.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
			tbPredictedMarketPos.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
			tbPredictedMarketPos.TextChanged += new EventHandler(tbNew_PMO_Budget_Value_TextChanged);

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

			Controls.Add(this.lblPredictedMarketPos_Title);
			Controls.Add(this.tbPredictedMarketPos);
			Controls.Add(this.lblErrorMessage);

			this.Name = "ChangePMOControl";
			this.Size = new System.Drawing.Size(520,280);
			this.ResumeLayout(false);
		}

		void tbNew_PMO_Budget_Value_TextChanged(object sender, EventArgs e)
		{
			//int newValue = CONVERT.ParseIntSafe(this.tbPredictedMarketPos.Text, 0);

			//if ((newValue > 20) | (newValue < 1))
			//{
			//  lblErrorMessage.Visible = true;
			//  lblErrorMessage.Text = "Invalid Market position.";
			//  this.newBtnOK.Enabled = false;
			//}
			//else
			//{ 
			//  this.newBtnOK.Enabled = true;
			//  lblErrorMessage.Visible = false;
			//}
		}

		private bool CheckOrder()
		{
			bool OpSuccess = true;
			//extract the data 
			int newValue = CONVERT.ParseIntSafe(this.tbPredictedMarketPos.Text, 0);

			if ((newValue > 20) | (newValue < 1))
			{
				lblErrorMessage.Visible = true;
				lblErrorMessage.Text = "Invalid Market position.";
				OpSuccess = false;
			}
			else
			{
				lblErrorMessage.Visible = false;
				OpSuccess = true;
			}
			return OpSuccess;
		}

		private bool HandleOrder()
		{
			//extract the data 
			int newValue = CONVERT.ParseIntSafe(this.tbPredictedMarketPos.Text, 0);
			
			ArrayList attrs = new ArrayList ();
			attrs.Add(new AttributeValuePair ("cmd_type", "change_predict_pos"));
			attrs.Add(new AttributeValuePair("predictpos_newvalue", CONVERT.ToStr(newValue)));
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
			tbPredictedMarketPos.Focus();
		}
	}
}