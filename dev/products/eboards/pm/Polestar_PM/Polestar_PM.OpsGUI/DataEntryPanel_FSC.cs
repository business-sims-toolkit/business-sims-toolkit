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

namespace Polestar_PM.OpsGUI
{
	public class DataEntryPanel_FSC  : FlickerFreePanel
	{
		protected IDataEntryControlHolder _mainPanel;

		protected NodeTree MyNodeTree;
		protected Node queueNode;
		protected Node fsc_list_node;
		protected int fscID = 0;
		protected Hashtable fsc_nodes = new Hashtable();

		protected Font MyDefaultSkinFontNormal8 = null;
		protected Font MyDefaultSkinFontNormal10 = null;
		protected Font MyDefaultSkinFontNormal12 = null;
		protected Font MyDefaultSkinFontBold8 = null;
		protected Font MyDefaultSkinFontBold10 = null;
		protected Font MyDefaultSkinFontBold12 = null;
		protected Font MyDefaultSkinFontBold24 = null;

		private ImageTextButton newBtnFSC1 = new ImageTextButton(0);
		private ImageTextButton newBtnFSC2 = new ImageTextButton(0);
		private ImageTextButton newBtnFSC3 = new ImageTextButton(0);
		private ImageTextButton newBtnFSC4 = new ImageTextButton(0);
		private ImageTextButton newBtnFSC5 = new ImageTextButton(0);
		private ImageTextButton newBtnFSC6 = new ImageTextButton(0);

		private ImageTextButton newBtnOK = new ImageTextButton(0);
		private ImageTextButton newBtnCancel = new ImageTextButton(0);

		private System.Windows.Forms.Panel chooseSlotPanel;
		private System.Windows.Forms.Panel locationPanel;
		private System.Windows.Forms.TextBox locTextBox;
		private System.Windows.Forms.Label titleLabel;
		private System.Windows.Forms.Label helpLabel;

		
		public DataEntryPanel_FSC (IDataEntryControlHolder mainPanel, NodeTree tree)
		{
			MyNodeTree = tree;
			_mainPanel = mainPanel;
			queueNode = tree.GetNamedNode("TaskManager");
			fsc_list_node = tree.GetNamedNode("fsc_list");

			Build_FSC_data();

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
			BuildPanelButtons();

			titleLabel = new System.Windows.Forms.Label();
			titleLabel.BackColor = System.Drawing.Color.Transparent;
			titleLabel.Font = MyDefaultSkinFontBold12;
			titleLabel.ForeColor = System.Drawing.Color.White;
			titleLabel.Location = new System.Drawing.Point(110-25, 10-2);
			titleLabel.Name = "titleLabel";
			titleLabel.Size = new System.Drawing.Size(380, 18);
			titleLabel.TabIndex = 11;
			titleLabel.Text = "Forward Schedule of Change";
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
			helpLabel.Text = "Select Change ID to implement ";
			helpLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.Controls.Add(helpLabel);

			chooseSlotPanel.Visible = true;
			locationPanel.Visible = false;
		}

		new public void Dispose()
		{
			fsc_nodes.Clear();

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

		public void Build_FSC_data()
		{
			foreach (Node fsc_node in fsc_list_node.getChildren())
			{
				string fsc_name = fsc_node.GetAttribute("name");
				if (fsc_nodes.ContainsKey(fsc_name)==false)
				{
					fsc_nodes.Add(fsc_name, fsc_node);
				}
			}
		}

		public bool isFSC_notDone(int fsc_number)
		{
			string fsc_name = "fsc"+CONVERT.ToStr(fsc_number);
			bool isNotDone = false;

			if (fsc_nodes.ContainsKey(fsc_name))
			{
				Node fsc_node = (Node) fsc_nodes[fsc_name];
				if (fsc_node != null)
				{
					string status = fsc_node.GetAttribute("status");
					if (status.ToLower()=="todo")
					{
						isNotDone = true;
					}
				}
			}
			return isNotDone;
		}

		public void BuildPanelButtons()
		{
			//newBtnCancel.SetButton(AppInfo.TheInstance.Location+"\\images\\buttons\\button_70x25.png");
			newBtnCancel.SetVariants("images\\buttons\\button_70x25.png");
			newBtnCancel.Location = new System.Drawing.Point(400, 220);
			newBtnCancel.Name = "newBtnCancel";
			newBtnCancel.Size = new System.Drawing.Size(70, 25);
			newBtnCancel.TabIndex = 22;
			newBtnCancel.ButtonFont = MyDefaultSkinFontBold10;
			//newBtn.ForeColor = System.Drawing.Color.Black;
			newBtnCancel.SetButtonText("Cancel",
				System.Drawing.Color.Black,System.Drawing.Color.Black,
				System.Drawing.Color.Green,System.Drawing.Color.Gray);
			//newBtn.ButtonPressed += new ProjectMngmntGamePanels.ImageButton.ImageButtonEventArgsHandler(playButton_ButtonPressed);
			newBtnCancel.Click += new System.EventHandler(this.newBtnCancel_Click);
			this.Controls.Add(newBtnCancel);

			//newBtnOK.SetButton(AppInfo.TheInstance.Location+"\\images\\buttons\\button_70x25.png");
			newBtnOK.SetVariants("images\\buttons\\button_70x25.png");
			newBtnOK.Location = new System.Drawing.Point(300, 220);
			newBtnOK.Name = "newBtnOK";
			newBtnOK.Size = new System.Drawing.Size(70, 25);
			newBtnOK.TabIndex = 21;
			newBtnOK.ButtonFont = MyDefaultSkinFontBold10;
			//newBtn.ForeColor = System.Drawing.Color.Black;
			newBtnOK.SetButtonText("OK",
				System.Drawing.Color.Black,System.Drawing.Color.Black,
				System.Drawing.Color.Green,System.Drawing.Color.Gray);
			//newBtn.ButtonPressed += new ProjectMngmntGamePanels.ImageButton.ImageButtonEventArgsHandler(playButton_ButtonPressed);
			newBtnOK.Click += new System.EventHandler(this.newBtnOK_Click);
			newBtnOK.Visible = false;
			this.Controls.Add(newBtnOK);
		}

		public void BuildSlotButtonControls() 
		{
			//newBtnFSC1.SetButton(AppInfo.TheInstance.Location+"\\images\\buttons\\button_50x40.png");
			newBtnFSC1.SetVariants("images\\buttons\\button_50x40.png");
			newBtnFSC1.Location = new System.Drawing.Point(30, 10);
			newBtnFSC1.Name = "Button1";
			newBtnFSC1.Size = new System.Drawing.Size(50, 40);
			newBtnFSC1.TabIndex = 8;
			newBtnFSC1.ButtonFont = MyDefaultSkinFontNormal10;
			newBtnFSC1.Tag = 1;
			newBtnFSC1.SetButtonText("1",
				System.Drawing.Color.Black,System.Drawing.Color.Black,
				System.Drawing.Color.Green,System.Drawing.Color.Gray);
			newBtnFSC1.Click += new System.EventHandler(this.FSC_Button_Click);
			this.chooseSlotPanel.Controls.Add(newBtnFSC1);
			newBtnFSC1.Enabled = isFSC_notDone(1);

			//newBtnFSC2.SetButton(AppInfo.TheInstance.Location+"\\images\\buttons\\button_50x40.png");
			newBtnFSC2.SetVariants("images\\buttons\\button_50x40.png");
			newBtnFSC2.Location = new System.Drawing.Point(90, 10);
			newBtnFSC2.Name = "Button2";
			newBtnFSC2.Size = new System.Drawing.Size(50, 40);
			newBtnFSC2.TabIndex = 14;
			newBtnFSC2.ButtonFont = MyDefaultSkinFontNormal10;
			newBtnFSC2.Tag = 2;
			//newBtn.ForeColor = System.Drawing.Color.Black;
			newBtnFSC2.SetButtonText("2",
				System.Drawing.Color.Black,System.Drawing.Color.Black,
				System.Drawing.Color.Green,System.Drawing.Color.Gray);
			//newBtn.ButtonPressed += new ProjectMngmntGamePanels.ImageButton.ImageButtonEventArgsHandler(playButton_ButtonPressed);
			newBtnFSC2.Click += new System.EventHandler(this.FSC_Button_Click);
			this.chooseSlotPanel.Controls.Add(newBtnFSC2);
			newBtnFSC2.Enabled = isFSC_notDone(2);

			//newBtnFSC3.SetButton(AppInfo.TheInstance.Location+"\\images\\buttons\\button_50x40.png");
			newBtnFSC3.SetVariants("images\\buttons\\button_50x40.png");
			newBtnFSC3.Location = new System.Drawing.Point(150, 10);
			newBtnFSC3.Name = "Button3";
			newBtnFSC3.Size = new System.Drawing.Size(50, 40);
			newBtnFSC3.TabIndex = 13;
			newBtnFSC3.ButtonFont = MyDefaultSkinFontNormal10;
			newBtnFSC3.Tag = 3;
			//newBtn.ForeColor = System.Drawing.Color.Black;
			newBtnFSC3.SetButtonText("3",
				System.Drawing.Color.Black,System.Drawing.Color.Black,
				System.Drawing.Color.Green,System.Drawing.Color.Gray);
			//newBtn.ButtonPressed += new ProjectMngmntGamePanels.ImageButton.ImageButtonEventArgsHandler(playButton_ButtonPressed);
			newBtnFSC3.Click += new System.EventHandler(this.FSC_Button_Click);
			this.chooseSlotPanel.Controls.Add(newBtnFSC3);
			newBtnFSC3.Enabled = isFSC_notDone(3);

			//newBtnFSC4.SetButton(AppInfo.TheInstance.Location+"\\images\\buttons\\button_50x40.png");
			newBtnFSC4.SetVariants("images\\buttons\\button_50x40.png");
			newBtnFSC4.Location = new System.Drawing.Point(30, 60);
			newBtnFSC4.Name = "Button4";
			newBtnFSC4.Size = new System.Drawing.Size(50, 40);
			newBtnFSC4.TabIndex = 10;
			newBtnFSC4.ButtonFont =MyDefaultSkinFontNormal10;
			newBtnFSC4.Tag = 4;
			//newBtn.ForeColor = System.Drawing.Color.Black;
			newBtnFSC4.SetButtonText("4",
				System.Drawing.Color.Black,System.Drawing.Color.Black,
				System.Drawing.Color.Green,System.Drawing.Color.Gray);
			//newBtn.ButtonPressed += new ProjectMngmntGamePanels.ImageButton.ImageButtonEventArgsHandler(playButton_ButtonPressed);
			newBtnFSC4.Click += new System.EventHandler(this.FSC_Button_Click);
			this.chooseSlotPanel.Controls.Add(newBtnFSC4);
			newBtnFSC4.Enabled = isFSC_notDone(4);

			//newBtnFSC5.SetButton(AppInfo.TheInstance.Location+"\\images\\buttons\\button_50x40.png");
			newBtnFSC5.SetVariants("images\\buttons\\button_50x40.png");
			newBtnFSC5.Location = new System.Drawing.Point(90, 60);
			newBtnFSC5.Name = "Button5";
			newBtnFSC5.Size = new System.Drawing.Size(50, 40);
			newBtnFSC5.TabIndex = 11;
			newBtnFSC5.ButtonFont = MyDefaultSkinFontNormal10;
			newBtnFSC5.Tag = 5;
			//newBtn.ForeColor = System.Drawing.Color.Black;
			newBtnFSC5.SetButtonText("5",
				System.Drawing.Color.Black,System.Drawing.Color.Black,
				System.Drawing.Color.Green,System.Drawing.Color.Gray);
			//newBtn.ButtonPressed += new ProjectMngmntGamePanels.ImageButton.ImageButtonEventArgsHandler(playButton_ButtonPressed);
			newBtnFSC5.Click += new System.EventHandler(this.FSC_Button_Click);
			this.chooseSlotPanel.Controls.Add(newBtnFSC5);
			newBtnFSC5.Enabled = isFSC_notDone(5);

			//newBtnFSC6.SetButton(AppInfo.TheInstance.Location+"\\images\\buttons\\button_50x40.png");
			newBtnFSC6.SetVariants("images\\buttons\\button_50x40.png");
			newBtnFSC6.Location = new System.Drawing.Point(150, 60);
			newBtnFSC6.Name = "Button6";
			newBtnFSC6.Size = new System.Drawing.Size(50, 40);
			newBtnFSC6.TabIndex = 12;
			newBtnFSC6.ButtonFont = MyDefaultSkinFontNormal10;
			newBtnFSC6.Tag = 6;
			//newBtn.ForeColor = System.Drawing.Color.Black;
			newBtnFSC6.SetButtonText("6",
				System.Drawing.Color.Black,System.Drawing.Color.Black,
				System.Drawing.Color.Green,System.Drawing.Color.Gray);
			//newBtn.ButtonPressed += new ProjectMngmntGamePanels.ImageButton.ImageButtonEventArgsHandler(playButton_ButtonPressed);
			newBtnFSC6.Click += new System.EventHandler(this.FSC_Button_Click);
			this.chooseSlotPanel.Controls.Add(newBtnFSC6);
			newBtnFSC6.Enabled = isFSC_notDone(6);
		}

		public void BuildBaseControls() 
		{
			this.chooseSlotPanel = new System.Windows.Forms.Panel();
			this.locationPanel = new System.Windows.Forms.Panel();
			this.locTextBox = new System.Windows.Forms.TextBox();
			this.chooseSlotPanel.SuspendLayout();
			this.locationPanel.SuspendLayout();
			this.SuspendLayout();

			BuildSlotButtonControls();
			// 
			// chooseSlotPanel
			// 
			this.chooseSlotPanel.Location = new System.Drawing.Point(90+90-22, 30+75-40);
			this.chooseSlotPanel.Name = "chooseSlotPanel";
			this.chooseSlotPanel.Size = new System.Drawing.Size(230, 110);
			//this.chooseSlotPanel.BackColor = Color.FromArgb(176,196,222); //TODO SKIN
			this.chooseSlotPanel.BackColor = Color.Transparent;
			this.chooseSlotPanel.TabIndex = 13;

			// 
			// locTextBox
			// 
			this.locTextBox.BackColor = System.Drawing.Color.Black;
			this.locTextBox.Font = this.MyDefaultSkinFontBold24;
			this.locTextBox.ForeColor = System.Drawing.Color.White;
			this.locTextBox.Location = new System.Drawing.Point(10, 10);
			this.locTextBox.Name = "locTextBox";
			this.locTextBox.Size = new System.Drawing.Size(210, 43);
			this.locTextBox.TabIndex = 28;
			this.locTextBox.Text = "";
			this.locTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
			this.locTextBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;

			// 
			// locationPanel
			// 
			this.locationPanel.Controls.Add(this.locTextBox);
			this.locationPanel.Location = new System.Drawing.Point(90+90-22, 50+75);
			this.locationPanel.Name = "locationPanel";
			this.locationPanel.Size = new System.Drawing.Size(230, 70);
			//this.locationPanel.BackColor = Color.FromArgb(176,196,222); //TODO SKIN
			this.locationPanel.BackColor = Color.Transparent;
			this.locationPanel.TabIndex = 14;

			// 
			// ForwardScheduleControl
			// 
			this.Controls.Add(this.locationPanel);
			this.Controls.Add(this.chooseSlotPanel);
			this.Name = "ForwardScheduleControl";
			this.Size = new System.Drawing.Size(520,280);
			this.chooseSlotPanel.ResumeLayout(false);
			this.locationPanel.ResumeLayout(false);
			this.ResumeLayout(false);
		}

		private void FSC_Button_Click(object sender, System.EventArgs e)
		{
			int forwardscheduledchange = 1;
			Boolean proceed = false;

			//Extract out the FSC from the control that was used
			if (sender is ImageTextButton)
			{
				ImageTextButton ib = (ImageTextButton)sender;
				if (ib != null)
				{
					proceed = true;
					forwardscheduledchange = (int) (ib.Tag);
				}
			}
//			ImageTextButton ib = sender as ImageTextButton;
//			if(null != ib)
//			{
//				//forwardscheduledchange = int.Parse(ib.g.GetButtonText());
//				//proceed = true;
//				//_cp.ShowOK = true;
//			}
			//Extract out the FSC from the control that was used
			if (proceed)
			{
				fscID = forwardscheduledchange;
				if (forwardscheduledchange == 5)
				{
					this.helpLabel.Text = "Enter Server Name for Change";
				}
				else
				{
					this.helpLabel.Text = "Enter Installation Location for Change";
				}
				chooseSlotPanel.Visible = false;
				locationPanel.Visible = true;
				locTextBox.Text = "";
				newBtnOK.Visible = true;
				//_cp.ShowOK = true;
			}

		}

		private void newBtnOK_Click(object sender, System.EventArgs e)
		{
			string ErrPrompt = string.Empty;
			string supplied_data = locTextBox.Text;

			if (supplied_data != string.Empty)
			{
				string fsc_name = "fsc"+CONVERT.ToStr(fscID);

				if (fsc_nodes.ContainsKey(fsc_name))
				{
					Node fsc_node = (Node) fsc_nodes[fsc_name];
					if (fsc_node != null)
					{
						string correct_entry_data = fsc_node.GetAttribute("entry_data");
						if (correct_entry_data.ToLower() != supplied_data.ToLower())
						{
							if (fscID==5)
							{
								ErrPrompt = "Invalid Server Provided";
							}
							else
							{
								ErrPrompt = "Invalid Location Provided";
							}
							MessageBox.Show(ErrPrompt, "Forward Schedule of Change Fault",
								MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
						}
						else
						{
							//data matches the required entry data 
							ArrayList attrs = new ArrayList ();
							attrs.Add(new AttributeValuePair ("cmd_type", "request_fsc"));
							attrs.Add(new AttributeValuePair ("fsc_id", CONVERT.ToStr(fscID)));
							new Node (queueNode, "task", "", attrs);	
							_mainPanel.DisposeEntryPanel();
						}
					}
				}
			}
			else
			{
				ErrPrompt = "No Location Provided";
				if (fscID == 5)
				{
					ErrPrompt = "No Server Provided";
				}
				else
				{
					ErrPrompt = "No Location Provided";
				}
				MessageBox.Show(ErrPrompt, "Forward Schedule of Change Fault",
					MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}

//			if (forwardScheduleControl.FSCLocation != string.Empty)
//			{	
//				string proposedloc = 	forwardScheduleControl.FSCLocation;
//				if (forwardScheduleControl.fscID == 5)
//				{
//					string fscloc = this.MyGameEngineHandle.MyGame.getFSC_Location(forwardScheduleControl.fscID);
//					string svrloc = this.MyGameEngineHandle.MyGame.TranslateServerNametoLocation(proposedloc);
//					if (fscloc.ToUpper() == svrloc.ToUpper())
//					{
//						sp.DoFSC(forwardScheduleControl.fscID, svrloc);
//					}
//					else
//					{
//						MessageBox.Show("Incorrect Server Provided","Forward Schedule of Change Fault",
//							MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
//						return false;
//					}
//				}
//				else
//				{
//					string fscloc = this.MyGameEngineHandle.MyGame.getFSC_Location(forwardScheduleControl.fscID);
//					if (fscloc.ToUpper() == forwardScheduleControl.FSCLocation.ToUpper())
//					{
//						sp.DoFSC(forwardScheduleControl.fscID, forwardScheduleControl.FSCLocation);
//					}
//					else
//					{
//						MessageBox.Show("Incorrect Location Provided","Forward Schedule of Change Fault",
//							MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
//						return false;
//					}
//				}
//			}
//			else
//			{
//				string ErrPrompt = "No Location Provided";
//				if (forwardScheduleControl.fscID == 5)
//				{
//					ErrPrompt = "No Server Provided";
//				}
//				else
//				{
//					ErrPrompt = "No Location Provided";
//				}
//				MessageBox.Show(ErrPrompt, "Forward Schedule of Change Fault",
//					MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
//				return false;
//			}

			//Close();
		}

		private void newBtnCancel_Click(object sender, System.EventArgs e)
		{
			_mainPanel.DisposeEntryPanel();
		}

		public void SetFocus ()
		{
			//throw new Exception("The method or operation is not implemented.");
		}
	}
}