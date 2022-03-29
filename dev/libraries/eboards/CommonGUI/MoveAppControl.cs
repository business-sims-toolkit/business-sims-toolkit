using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;

using LibCore;
using Network;

using CoreUtils;
using IncidentManagement;

namespace CommonGUI
{
	/// <summary>
	/// Summary description for MoveAppControl.
	/// </summary>
	public class MoveAppControl : FlickerFreePanel
	{

		protected IDataEntryControlHolder _mainPanel;
		protected NodeTree _Network;
		protected Node AppMoveQueueNode;
		protected Hashtable ExistingActions = new Hashtable();

		protected Color AutoTimeBackColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("autotimebackcolor", Color.Silver);
		protected Color AutoTimeTextColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("autotimetextcolor", Color.Black);

		protected ImageTextButton okButton;
		protected ImageTextButton cancelButton;

		protected ArrayList buttonArray;

		protected Panel chooseUpgradePanel;
		protected Panel chooseRouterPanel;
		protected Panel chooseTimePanel;

		protected Label title;
		protected Label timeLabel;
		protected Label ChosenAppNameLabel;
		protected Label ChosenUpgradeTypeLabel;
		protected EntryBox whenTextBox;
		protected Label AutoTimeLabel= null;
		protected Label ErrorLabel;

		protected Button moveButton;
		protected Button movePendingButton;
		
		protected ImageTextButton ManualTimeButton = null;
		protected ArrayList AutoTimeButtons = new ArrayList();

		protected ArrayList disposeControls = new ArrayList();

		protected bool UsingMinutes = false; //Control in Race Phase Using Minutes 
		protected IncidentApplier iApplier;

		protected string filename_long = "\\images\\buttons\\blank_big.png";
		protected string filename_mid = "\\images\\buttons\\blank_med.png";
		protected string filename_short = "\\images\\buttons\\blank_small.png";
		protected string filename_ok = "\\images\\buttons\\ok_blank_small.png";

		protected int maxmins = 24;
		protected int autoTimeSecs = 0; 
		protected Boolean MyIsTrainingMode = false;
		protected int bottomCornerOffset = 5;

		protected enum MoveOptions
		{
			MoveVirtual,
			MoveCloud
		}

		protected MoveOptions moveMode;

		protected Node appPicked;

		protected FocusJumper focusJumper;

		protected Color MyGroupPanelBackColor;
		protected Color MyOperationsBackColor;
		protected Color MyTextLabelBackColor;

		protected Color MyTitleForeColor = Color.Black;
		protected Font MyDefaultSkinFontNormal12 = null;
		protected Font MyDefaultSkinFontNormal11 = null;
		protected Font MyDefaultSkinFontNormal8 = null;
		protected Font MyDefaultSkinFontBold12 = null;
		protected Font MyDefaultSkinFontBold11 = null;
		protected Font MyDefaultSkinFontBold10 = null;
		protected Font MyDefaultSkinFontBold9 = null;

		protected bool playing = false;
		public Color errorLabel_foreColor = Color.Red;

		#region Constructor and dispose

		public MoveAppControl(IDataEntryControlHolder mainPanel, NodeTree model, bool usingmins, 
			IncidentApplier _iApplier, Boolean IsTrainingMode, Color OperationsBackColor, Color GroupPanelBackColor)
			:this( mainPanel, model, usingmins, _iApplier, IsTrainingMode, OperationsBackColor, GroupPanelBackColor, true)
		{
		}

		public MoveAppControl(IDataEntryControlHolder mainPanel, NodeTree model, bool usingmins, 
			IncidentApplier _iApplier, Boolean IsTrainingMode, Color OperationsBackColor, Color GroupPanelBackColor, 
			bool _playing)
		{
			playing = _playing;

			string errmsg_overridecolor = SkinningDefs.TheInstance.GetData("race_errormsg_override_color");
			if (errmsg_overridecolor != "")
			{
				errorLabel_foreColor = SkinningDefs.TheInstance.GetColorData("race_errormsg_override_color");
			}

			SetStyle(ControlStyles.Selectable, true);
			focusJumper = new FocusJumper();

			Color upColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_text_colour", Color.Black);
			Color downColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_down_text_colour", Color.White);
			Color hoverColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_hover_text_colour", Color.Green);
			Color disabledColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_disabled_text_colour", Color.DarkGray);

			string autotimebackcolor =  SkinningDefs.TheInstance.GetData("autotimebackcolor");
			if (autotimebackcolor != "")
			{
				AutoTimeBackColor =  SkinningDefs.TheInstance.GetColorData("autotimebackcolor");
			}
			string autotimetextcolor =  SkinningDefs.TheInstance.GetData("autotimetextcolor");
			if (autotimetextcolor != "")
			{
				AutoTimeTextColor =  SkinningDefs.TheInstance.GetColorData("autotimetextcolor");
			}

			//Is there an overriding Title Foreground colour
			string racetitlecolour =  SkinningDefs.TheInstance.GetData("race_paneltitleforecolor");
			if (racetitlecolour != "")
			{
				MyTitleForeColor =  SkinningDefs.TheInstance.GetColorData("race_paneltitleforecolor");
			}

			MyOperationsBackColor = OperationsBackColor;
			MyGroupPanelBackColor = GroupPanelBackColor;
			MyTextLabelBackColor = OperationsBackColor;

			if (SkinningDefs.TheInstance.GetIntData("race_panels_transparent_backs", 0) == 1)
			{
				MyTextLabelBackColor = Color.Transparent;
			}

			//We may wish to offset the ok and cancel buttons from the bottom right corner (standard is 5)
			bottomCornerOffset = SkinningDefs.TheInstance.GetIntData("race_panels_upgrade_memdisk_corneroffset", 5);

			string fontname =  SkinningDefs.TheInstance.GetData("fontname");
			MyDefaultSkinFontNormal12 = ConstantSizeFont.NewFont(fontname,12);
			MyDefaultSkinFontNormal11 = ConstantSizeFont.NewFont(fontname,11);
			MyDefaultSkinFontNormal8 = ConstantSizeFont.NewFont(fontname,8);
			MyDefaultSkinFontBold12 = ConstantSizeFont.NewFont(fontname,12,FontStyle.Bold);
			MyDefaultSkinFontBold11 = ConstantSizeFont.NewFont(fontname,11,FontStyle.Bold);
			MyDefaultSkinFontBold9 = ConstantSizeFont.NewFont(fontname,9,FontStyle.Bold);

			SuspendLayout();

			//this.BackColor = Color.Transparent;
			iApplier = _iApplier;
			UsingMinutes = usingmins;
			buttonArray = new ArrayList();
			_Network = model;
			_mainPanel = mainPanel;

			MyIsTrainingMode = IsTrainingMode;
			if (MyIsTrainingMode) 
			{
				BackgroundImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location +
					"\\images\\panels\\race_panel_back_training.png");
			}
			else
			{
				BackgroundImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location +
					"\\images\\panels\\race_panel_back_normal.png");
			}

			title = new Label();
			title.Text = "Move Application";
			title.Size = new Size(500,20);
			title.Location = new Point(5,5);
			title.Font = MyDefaultSkinFontBold12;
			title.BackColor = MyTextLabelBackColor;
			title.ForeColor = MyTitleForeColor;
			Controls.Add(title);

			//Connect up to the Queue Server Actions 
			AppMoveQueueNode = _Network.GetNamedNode("AppMoveQueue");

			ArrayList existingkids = AppMoveQueueNode.getChildren();
			foreach (Node kid in existingkids)
			{
				string servername = kid.GetAttribute("appname");
				int actiontime = kid.GetIntAttribute("when",0);
				string actiontype = kid.GetAttribute("type");
				string option = kid.GetAttribute("option");
				ExistingActions.Add(kid, actiontime);
			}

			chooseRouterPanel = new FlickerFreePanel();
			chooseRouterPanel.BackColor = MyOperationsBackColor;
			//chooseRouterPanel.BackColor = Color.CadetBlue;
			chooseRouterPanel.Size = new Size(545,125);
			chooseRouterPanel.Location = new Point(5,25);
			chooseRouterPanel.SuspendLayout();
			BuildServerPanel(model);
			chooseRouterPanel.ResumeLayout(false);
			Controls.Add( chooseRouterPanel );

			chooseUpgradePanel = new Panel();
			chooseUpgradePanel.BackColor = MyOperationsBackColor;
			//chooseUpgradePanel.BackColor = Color.PowderBlue;
			chooseUpgradePanel.Size = new Size(545,120);
			chooseUpgradePanel.Location = new Point(5,25);
			chooseUpgradePanel.Visible = false;
			Controls.Add(chooseUpgradePanel);
			//
			chooseTimePanel = new Panel();
			chooseTimePanel.BackColor = MyOperationsBackColor;
			//chooseTimePanel.BackColor = Color.PeachPuff;
			chooseTimePanel.Size = new Size(575, 100);
			chooseTimePanel.Location = new Point(5,title.Bottom + 5);
			chooseTimePanel.Visible = false;
			//BuildTimePanel();
			Controls.Add(chooseTimePanel);
			
			//
			okButton = new ImageTextButton(0);
			okButton.ButtonFont = MyDefaultSkinFontBold9;
			okButton.SetVariants(filename_short);
			okButton.Size = new Size(80,20);
			okButton.Location = new Point(400,155);
			okButton.SetButtonText("OK",
				upColor,upColor,
				hoverColor,disabledColor);
			okButton.Click += okButton_Click;
			okButton.Visible = false;
			Controls.Add(okButton);

			focusJumper.Add(okButton);

			cancelButton = new ImageTextButton(0);
			cancelButton.ButtonFont = MyDefaultSkinFontBold9;
			cancelButton.SetVariants(filename_short);
			cancelButton.Size = new Size(80,20);
			cancelButton.Location = new Point(485,155);
			cancelButton.SetButtonText("Close",
				upColor,upColor,
				hoverColor,disabledColor);
			cancelButton.Click += cancelButton_Click;
			cancelButton.Visible = true;
			Controls.Add(cancelButton);

			focusJumper.Add(cancelButton);

			ErrorLabel = new Label();
			ErrorLabel.Text = "Error";
			ErrorLabel.TextAlign = ContentAlignment.MiddleLeft;
			ErrorLabel.Size = new Size(420-70+85,20);
			ErrorLabel.Location = new Point(40+120,chooseTimePanel.Top+chooseTimePanel.Height);
			ErrorLabel.Font = MyDefaultSkinFontBold12;
			ErrorLabel.BackColor = MyTextLabelBackColor;
			ErrorLabel.ForeColor = errorLabel_foreColor;
			Controls.Add(ErrorLabel);

			ClearErrorMessage();

			ResumeLayout(false);

			GotFocus += UpgradeRouterControl_GotFocus;
			Resize += UpgradeRouterControl_Resize;
		}

		protected override void Dispose(bool disposing)
		{
			if(disposing)
			{
				focusJumper.Dispose();

				foreach(Control c in disposeControls)
				{
					if(c!=null) c.Dispose();
				}
				disposeControls.Clear();
			}
			//
			base.Dispose (disposing);
		}

		/// <summary>
		/// Setting the permitted limit for the requested action time
		/// </summary>
		/// <param name="maxminutes"></param>
		public void SetMaxMins(int maxminutes)
		{
			maxmins = maxminutes;
		}

		#endregion Constructor and dispose

		#region Error Display Methods 
		
		protected void SetErrorMessage(string ErrMsg)
		{
			ErrorLabel.Text = ErrMsg;
			ErrorLabel.Visible = true;
		}

		protected void ClearErrorMessage()
		{
			ErrorLabel.Visible = false;
		}

		#endregion Error Display Methods 

		#region Server Selection Panel 

		/// <summary>
		/// Build the Server Selection Panel
		/// </summary>
		/// <param name="model"></param>
		protected virtual void BuildServerPanel(NodeTree model)
		{
			Color upColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_text_colour", Color.Black);
			Color downColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_down_text_colour", Color.White);
			Color hoverColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_hover_text_colour", Color.Green);
			Color disabledColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_disabled_text_colour", Color.DarkGray);

			// Grab all the Appplication that can be Moved 
			ArrayList types = new ArrayList();
			types.Add("App");
			Hashtable apps = _Network.GetNodesOfAttribTypes(types);
			// Alphabetically sort the servers...
			//System.Collections.Specialized.StringCollection serverArray = new StringCollection();
			ArrayList appArray = new ArrayList();
			Hashtable appNameToNode = new Hashtable();
			foreach (Node app in apps.Keys)
			{
				string name = app.GetAttribute("name");
				bool move_virtual_allowed = app.GetBooleanAttribute("can_move_virtual", false);
				bool move_cloud_allowed = app.GetBooleanAttribute("can_move_cloud", false);
				if ((move_virtual_allowed) | (move_cloud_allowed))
				{
					if (appArray.Contains(name) == false)
					{
						appArray.Add(name);
						appNameToNode.Add(name, app);
					}
				}
			}
			appArray.Sort();
			// We can have 6 buttons wide before we have to go to a new line.
			int xoffset = 5;
			int yoffset = 0;
			int numOnRow = 0;
			int serverbuttonwidth = 102;
			int serverbuttonheight = 20;
			
			//
			foreach (string app in appArray)
			{
				Node appNode = model.GetNamedNode(app);

				string canMove = appNode.GetAttribute("can_move");

				ImageTextButton newBtnServer = new ImageTextButton(0);
				newBtnServer.ButtonFont = MyDefaultSkinFontBold9;
				newBtnServer.SetVariants(filename_mid);
				newBtnServer.Location = new Point(xoffset,yoffset);
				newBtnServer.Size = new Size(serverbuttonwidth, serverbuttonheight);
				newBtnServer.Tag = appNameToNode[app];
				//newBtnServer.TabIndex = btn.TabIndex;
				//newBtnServer.ButtonFont = btn.Font;
				newBtnServer.Enabled = true;
				//newBtn.ForeColor = System.Drawing.Color.Black;
				newBtnServer.SetButtonText(app,upColor,upColor,hoverColor,disabledColor);
				//newBtn.ButtonPressed += new ProjectMngmntGamePanels.ImageButton.ImageButtonEventArgsHandler(playButton_ButtonPressed);
				newBtnServer.Click += HandleServerButton_Click;
				chooseRouterPanel.Controls.Add(newBtnServer);
				disposeControls.Add(newBtnServer);

				xoffset += newBtnServer.Width+3;
				buttonArray.Add(newBtnServer);

				focusJumper.Add(newBtnServer);

				++numOnRow;
				if(numOnRow == 5)
				{
					numOnRow = 0;
					xoffset = 5;
					yoffset += serverbuttonheight + 5;
				}
			}
		}

		protected void HandleServerButton_Click(object sender, EventArgs e)
		{
			ImageTextButton button = (ImageTextButton) sender;
			foreach(ImageTextButton b in buttonArray)
			{
				if(b == button)
				{
					string disptext = b.GetButtonText();
					appPicked = (Node) b.Tag;
					title.Text += " > " + disptext;
					//
					BuildOptionsPanel();
					//
					chooseUpgradePanel.Visible = true;
					chooseRouterPanel.Visible = false;
					return;
				}
			}
			//okButton.Enabled = true;
		}

		#endregion Server Selection Panel  

		#region Upgrade Type Selection Panel  

		protected void CheckForPending(string appname, 
			out Boolean CanMoveVirtualPending, out Node CanMoveVirtual_PendingNode, out int CanMoveVirtual_PendingWhen,
			out Boolean CanMoveCloudPending, out Node CanMoveCloud_PendingNode, out int CanMoveCloud_PendingWhen)
		{
			CanMoveVirtualPending = false;
			CanMoveVirtual_PendingNode = null;
			CanMoveVirtual_PendingWhen = -1;

			CanMoveCloudPending = false;
			CanMoveCloud_PendingNode = null;
			CanMoveCloud_PendingWhen = -1;


			//read throught the current requests
			foreach (Node n1 in ExistingActions.Keys)
			{
				string pending_app_name = n1.GetAttribute("target");
				string pending_upgrade_type = n1.GetAttribute("type");
				string pending_upgrade_option = n1.GetAttribute("upgrade_option");
				int pending_when = n1.GetIntAttribute("when",0);
				if (appname.ToLower()==pending_app_name.ToLower())
				{
					if (pending_upgrade_option == "move_virtual") 
					{
						CanMoveVirtualPending = true;
						CanMoveVirtual_PendingNode = n1;
						CanMoveVirtual_PendingWhen = pending_when;
					}
					if (pending_upgrade_option == "move_cloud") 
					{
						CanMoveCloudPending = true;
						CanMoveCloud_PendingNode = n1;
						CanMoveCloud_PendingWhen = pending_when;
					}
				}
			}
		}
		
		protected string BuildTimeString(int timevalue)
		{
			int time_mins = timevalue / 60;
			int time_secs = timevalue % 60;
			string displaystr = CONVERT.ToStr(time_mins)+":";
			if (time_secs<10)
			{
				displaystr += "0";
			}
			displaystr += CONVERT.ToStr(time_secs);
			return displaystr;
		}
		
		
		protected void BuildMoveVirtualPanel(bool upgrade, int day, Node tag, int upgradecount, int panel_index)
		{
			string countstr = "";
			if (upgradecount >0)
			{
				countstr = " (" + CONVERT.ToStr(upgradecount) + ")";
			}

			Color upColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_text_colour", Color.Black);
			Color downColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_down_text_colour", Color.White);
			Color hoverColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_hover_text_colour", Color.Green);
			Color disabledColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_disabled_text_colour", Color.DarkGray);

			Panel movePanel = new Panel();
			movePanel.Size = new Size(160, 100);
			movePanel.Location = new Point(40+200*panel_index, 10);
			movePanel.BorderStyle = BorderStyle.None;
			movePanel.BackColor = MyGroupPanelBackColor;
			chooseUpgradePanel.Controls.Add(movePanel);

			Label moveLabel = new Label();
			moveLabel.Text = "Virtual " + countstr;
			moveLabel.Font = MyDefaultSkinFontBold11;
			moveLabel.Location = new Point(5, 5);
			moveLabel.Width = movePanel.Width - (2 * moveLabel.Left);
			movePanel.Controls.Add(moveLabel);

			if(upgrade)
			{
				Label upgrading = new Label();
				upgrading.Text = "Move Due at "+BuildTimeString(day);
				upgrading.Size = new Size(145,30);
				upgrading.Font = MyDefaultSkinFontNormal8;
				upgrading.Location = new Point(5,28);
				movePanel.Controls.Add(upgrading);

				ImageTextButton cancelButton = new ImageTextButton(0);
				cancelButton.ButtonFont = MyDefaultSkinFontBold9;
				cancelButton.SetVariants(filename_short);
				cancelButton.SetButtonText("Cancel",upColor,upColor,hoverColor,disabledColor);
				cancelButton.Tag = tag;
				cancelButton.Enabled = true;
				cancelButton.Location = new Point(10,60);
				cancelButton.Size = new Size(80,20);
				cancelButton.Click += movePendingButton_Click;
				movePanel.Controls.Add(cancelButton);

				focusJumper.Add(cancelButton);
			}
			else
			{
				ImageTextButton upgradeButton = new ImageTextButton(0);
				upgradeButton.ButtonFont = MyDefaultSkinFontBold9;
				upgradeButton.SetVariants(filename_short);
				upgradeButton.SetButtonText("Move to Virtual",upColor,upColor,hoverColor,disabledColor);
				upgradeButton.Tag = tag;
				upgradeButton.Enabled = true;
				upgradeButton.Location = new Point(10,60);
				upgradeButton.Size = new Size(140,20);
				upgradeButton.Click += moveVirtualButton_Click;
				movePanel.Controls.Add(upgradeButton);

				focusJumper.Add(upgradeButton);
			}
		}

		protected void BuildMoveCloudPanel(bool upgrade, int day, Node tag, int upgradecount,int panel_index)
		{
			string countstr = "";
			if (upgradecount > 0)
			{
				countstr = " (" + CONVERT.ToStr(upgradecount) + ")";
			}

			Color upColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_text_colour", Color.Black);
			Color downColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_down_text_colour", Color.White);
			Color hoverColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_hover_text_colour", Color.Green);
			Color disabledColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_disabled_text_colour", Color.DarkGray);

			Panel movePanel = new Panel();
			movePanel.Size = new Size(160, 100);
			movePanel.Location = new Point(40 + 200 * panel_index, 10);
			movePanel.BorderStyle = BorderStyle.None;
			movePanel.BackColor = MyGroupPanelBackColor;
			chooseUpgradePanel.Controls.Add(movePanel);

			Label moveLabel = new Label();
			moveLabel.Text = "Cloud " + countstr;
			moveLabel.Font = MyDefaultSkinFontBold11;
			moveLabel.Location = new Point(5, 5);
			moveLabel.Width = movePanel.Width - (2 * moveLabel.Left);
			movePanel.Controls.Add(moveLabel);

			if (upgrade)
			{
				Label upgrading = new Label();
				upgrading.Text = "Move Due at " + BuildTimeString(day);
				upgrading.Size = new Size(145, 30);
				upgrading.Font = MyDefaultSkinFontNormal8;
				upgrading.Location = new Point(5, 28);
				movePanel.Controls.Add(upgrading);

				ImageTextButton cancelButton = new ImageTextButton(0);
				cancelButton.ButtonFont = MyDefaultSkinFontBold9;
				cancelButton.SetVariants(filename_short);
				cancelButton.SetButtonText("Cancel", upColor, upColor, hoverColor, disabledColor);
				cancelButton.Tag = tag;
				cancelButton.Enabled = true;
				cancelButton.Location = new Point(10, 60);
				cancelButton.Size = new Size(80, 20);
				cancelButton.Click += movePendingButton_Click;
				movePanel.Controls.Add(cancelButton);

				focusJumper.Add(cancelButton);
			}
			else
			{
				ImageTextButton upgradeButton = new ImageTextButton(0);
				upgradeButton.ButtonFont = MyDefaultSkinFontBold9;
				upgradeButton.SetVariants(filename_short);
				upgradeButton.SetButtonText("Move to Cloud", upColor, upColor, hoverColor, disabledColor);
				upgradeButton.Tag = tag;
				upgradeButton.Enabled = true;
				upgradeButton.Location = new Point(10, 60);
				upgradeButton.Size = new Size(140, 20);
				upgradeButton.Click += moveCloudButton_Click;
				movePanel.Controls.Add(upgradeButton);

				focusJumper.Add(upgradeButton);
			}
		}

		/// <summary>
		/// This is built on demand on the basis of a selected Server
		/// </summary>
		protected virtual void BuildOptionsPanel()
		{
			string name = GetAppPickedName();

			Boolean canMoveVirtualFlag_Node = false;
			Boolean canMoveVirtualFlag_Pending = false;
			Node canMoveVirtualFlag_PendingNode = null;
			int CanMoveVirtual_PendingWhen = -1;
			int MoveVirtualCount_Node = 0;  	//How many times have we done this 

			Boolean canMoveCloudFlag_Node = false;
			Boolean canMoveCloudFlag_Pending = false;
			Node canMoveCloudFlag_PendingNode = null;
			int CanMoveCloud_PendingWhen = -1;
			int MoveCloudCount_Node = 0;  	//How many times have we done this 
			int panel_index = 0;

			//Determine wehther the Network will allow the Upgrades 
			canMoveVirtualFlag_Node = appPicked.GetBooleanAttribute("can_move_virtual", false);
			canMoveCloudFlag_Node = appPicked.GetBooleanAttribute("can_move_cloud", false);

			//Determine how many times we have done each operation
			MoveVirtualCount_Node = appPicked.GetIntAttribute("count_move_virtual", 0);
			MoveCloudCount_Node = appPicked.GetIntAttribute("count_move_cloud", 0);

			//Determine wehther the PendingActions will allow the Upgrades 
			CheckForPending(name, out canMoveVirtualFlag_Pending, out canMoveVirtualFlag_PendingNode, out CanMoveVirtual_PendingWhen,
				out canMoveCloudFlag_Pending, out canMoveCloudFlag_PendingNode, out CanMoveCloud_PendingWhen);

			panel_index = 0;
			if (canMoveVirtualFlag_Node || canMoveVirtualFlag_Pending)
			{
				if (canMoveVirtualFlag_PendingNode != null)
					BuildMoveVirtualPanel(true, CanMoveVirtual_PendingWhen, canMoveVirtualFlag_PendingNode, MoveVirtualCount_Node, panel_index);
				else
					BuildMoveVirtualPanel(false, 0, null, MoveVirtualCount_Node, panel_index);
			}
			panel_index++;

			if (canMoveCloudFlag_Node || canMoveCloudFlag_Pending)
			{
				if (canMoveCloudFlag_PendingNode != null)
					BuildMoveCloudPanel(true, CanMoveCloud_PendingWhen, canMoveCloudFlag_PendingNode, MoveCloudCount_Node, panel_index);
				else
					BuildMoveCloudPanel(false, 0, null, MoveCloudCount_Node, panel_index);
			}

			cancelButton.Focus();
		}

		protected virtual void handleOptionButtonClick()
		{
			Color upColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_text_colour", Color.Black);
			Color downColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_down_text_colour", Color.White);
			Color hoverColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_hover_text_colour", Color.Green);
			Color disabledColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_disabled_text_colour", Color.DarkGray);

			ClearErrorMessage();
			BuildTimePanel();
			chooseTimePanel.Visible = true;
			whenTextBox.Focus();
			okButton.Visible = true;
			cancelButton.SetButtonText("Cancel", upColor, upColor, hoverColor, disabledColor);
			chooseUpgradePanel.Visible = false;
		}

		protected void moveVirtualButton_Click(object sender, EventArgs e)
		{
			title.Text += " > Move Virtual";
			moveMode = MoveOptions.MoveVirtual;
			handleOptionButtonClick();
		}

		protected void moveCloudButton_Click(object sender, EventArgs e)
		{
			title.Text += " > Move Cloud";
			moveMode = MoveOptions.MoveCloud;
			handleOptionButtonClick();
		}

		protected void RemovingPendingActionNode(Node n1)
		{
			//we need to check that tagged node is good 
			if (n1 != null)
			{
				//we need to that no one has removed it already
				//the ui might still be open but the operation and the node has been completed
				//TODO is this best way to do it. 
				if (n1.Parent.HasChild(n1))
				{
					ExistingActions.Remove(n1);			//Delete from the Local Pending Actions actions list
					n1.Parent.DeleteChildTree(n1);  //Delete from the Tree
					BuildOptionsPanel();						//Refresh the Current Options Panel
					_mainPanel.DisposeEntryPanel();
				}
				else
				{
					SetErrorMessage("Server Upgrade has already been completed");
				}
			}
			else
			{
				SetErrorMessage("Server Upgrade has already been completed");
			}	
		}

		protected void movePendingButton_Click(object sender, EventArgs e)
		{
			Node n1 = (Node) ((ImageTextButton)sender).Tag; //Extract the Queue Node
			RemovingPendingActionNode(n1);
		}


		#endregion Upgrade Type Selection Panel  

		#region When Selection Panel  

		/// <summary>
		/// This is built on demand on the basis of a selected Server
		/// </summary>
		protected virtual void BuildTimePanel()
		{
			string name =  GetAppPickedName();

			Color upColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_text_colour", Color.Black);
			Color downColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_down_text_colour", Color.White);
			Color hoverColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_hover_text_colour", Color.Green);
			Color disabledColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_disabled_text_colour", Color.DarkGray);

			timeLabel = new Label();
			timeLabel.Size = new Size(140,25);
			timeLabel.Font = MyDefaultSkinFontNormal12;
			timeLabel.Location = new Point(5+150,42-32);
			timeLabel.ForeColor = MyTitleForeColor;
			chooseTimePanel.Controls.Add(timeLabel);
			//
			whenTextBox = new EntryBox();
			whenTextBox.DigitsOnly = true;
			whenTextBox.Font = MyDefaultSkinFontNormal11;
			whenTextBox.Size = new Size(90,30);
			whenTextBox.Text = "Now";
			whenTextBox.MaxLength = 2;
			whenTextBox.Location = new Point(145+30,40);
			whenTextBox.TextAlign = HorizontalAlignment.Center;
			chooseTimePanel.Controls.Add(whenTextBox);
			whenTextBox.GotFocus += whenTextBox_GotFocus;
			whenTextBox.LostFocus += whenTextBox_LostFocus;
			whenTextBox.KeyUp +=whenTextBox_KeyUp;

			focusJumper.Add(whenTextBox);

			//define the text for the different modes
			if(UsingMinutes)
			{
				timeLabel.Text = "Install At Min";
				whenTextBox.Text = "Now";

				string AllowedChangeWindowsActions_str  = SkinningDefs.TheInstance.GetData("changewindowactions");
				if (AllowedChangeWindowsActions_str.ToLower()=="true")
				{
					//extract the change windows times 
					string AllowedChangeWindowTimes_str  = SkinningDefs.TheInstance.GetData("changewindowtimes");
					string[] time_strs = AllowedChangeWindowTimes_str.Split(',');
					//Build the controls 

					int yoffset = 5;
					//General Manual Timing Button
					ManualTimeButton = new ImageTextButton(0);
					ManualTimeButton.ButtonFont = MyDefaultSkinFontBold9;
					ManualTimeButton.SetVariants(filename_mid);
					ManualTimeButton.Size = new Size(130,20);
					ManualTimeButton.Location = new Point(10,yoffset);
					ManualTimeButton.SetButtonText("Manual", upColor, upColor, hoverColor, disabledColor);
					ManualTimeButton.Click += ManualTimeButton_Click;
					chooseTimePanel.Controls.Add(ManualTimeButton);
					yoffset += ManualTimeButton.Height + 5;
					int count = 1;

					focusJumper.Add(ManualTimeButton);
					AutoTimeButtons.Clear();

					foreach (string st in time_strs)
					{
						if (count==1)
						{
							AutoTimeLabel = new Label();
							AutoTimeLabel.Text = "";
							AutoTimeLabel.Font = MyDefaultSkinFontNormal11;
							AutoTimeLabel.TextAlign = ContentAlignment.MiddleCenter;
							AutoTimeLabel.BackColor = AutoTimeBackColor;
							AutoTimeLabel.ForeColor = AutoTimeTextColor;
							AutoTimeLabel.Size = new Size(90,30);
							AutoTimeLabel.Location = new Point(145+30,40);
							AutoTimeLabel.Visible = false;
							chooseTimePanel.Controls.Add(AutoTimeLabel);
						}

						string displayname = "Auto "+CONVERT.ToStr(count);
						ImageTextButton AutoTimeButton = new ImageTextButton(0);
						AutoTimeButton.ButtonFont = MyDefaultSkinFontBold9;
						AutoTimeButton.SetVariants(filename_mid);
						AutoTimeButton.Size = new Size(130,20);
						AutoTimeButton.Location = new Point(10,yoffset);
						AutoTimeButton.Tag = CONVERT.ParseInt(st);
						AutoTimeButton.SetButtonText(displayname, upColor, upColor,	hoverColor, disabledColor);
						AutoTimeButton.Click += AutoTimeButton_Click;
						AutoTimeButton.Visible = true;
						chooseTimePanel.Controls.Add(AutoTimeButton);
						AutoTimeButtons.Add(AutoTimeButton);
						yoffset += AutoTimeButton.Height + 5;
						count++;

						focusJumper.Add(AutoTimeButton);
					}
				}
			}
			else
			{
				timeLabel.Text = "Install On Day";
				whenTextBox.Text = "Next Day";
			}
		}

		protected void AutoTimeButton_Click(object sender, EventArgs e)
		{
			ImageTextButton b1 = (ImageTextButton) sender;
			int time_fullsecs =(int) b1.Tag;

			autoTimeSecs = time_fullsecs;

			int time_mins = time_fullsecs / 60;
			int time_secs = time_fullsecs % 60;

			//string timestr = CONVERT.ToStr(time_mins)+":"+CONVERT.ToStr(time_secs);
			string timestr = BuildTimeString(time_fullsecs);

			if (AutoTimeLabel != null)
			{
				AutoTimeLabel.Text = timestr;
				AutoTimeLabel.Visible = true;
			}
			whenTextBox.Visible = false;
			ClearErrorMessage();
		}

		protected void ManualTimeButton_Click(object sender, EventArgs e)
		{
			autoTimeSecs = 0;
			if (AutoTimeLabel != null)
			{
				AutoTimeLabel.Visible = false;
			}
			whenTextBox.Visible = true;
			whenTextBox.Text = "Now";
			whenTextBox.Focus();
			ClearErrorMessage();
		}


		protected void whenTextBox_GotFocus(object sender, EventArgs e)
		{
			string displaystr = string.Empty;

			if(UsingMinutes)
			{
				displaystr = "Now";
			}
			else
			{
				displaystr = "Next Day";
			}
			//dayTextBox.SelectAll();
			if(whenTextBox.Text == displaystr)
			{
				whenTextBox.SelectAll();
				//whenTextBox.Text = "";
			}
		}

		protected void whenTextBox_LostFocus(object sender, EventArgs e)
		{
			string displaystr = string.Empty;

			if(UsingMinutes)
			{
				displaystr = "Now";
			}
			else
			{
				displaystr = "Next Day";
			}

			try
			{
				if(whenTextBox.Text == "")
				{
					whenTextBox.Text = displaystr;
				}
			}
			catch
			{
				whenTextBox.Text = displaystr;
			}
		}

		#endregion When Selection Panel  
		
		#region Calendar 

		protected int GetToday()
		{
			Node currentDayNode = _Network.GetNamedNode("CurrentDay");
			int CurrentDay = currentDayNode.GetIntAttribute("day",0);
			return CurrentDay;
		}

		/// <summary>
		/// This is used to prewarn the user that the day is booked
		/// </summary>
		/// <param name="day"></param>
		/// <returns></returns>
		protected Boolean IsDayFreeInCalendar(int day)
		{
			Node CalendarNode = _Network.GetNamedNode("Calendar");

			if(day > CalendarNode.GetIntAttribute("days",-1))
				return false;

			//Need to iterate over children 
			foreach(Node calendarEvent in CalendarNode.getChildren())
			{
				int cday = calendarEvent.GetIntAttribute("day",0);
				string block = calendarEvent.GetAttribute("block");
				if (day == cday)
				{
					if (block.ToLower() == "true")
					{
						return false;
					}
				}
			}
			return true;
		}

		#endregion Calendar

		#region Overall Operational OK and Cancel

		protected void cancelButton_Click(object sender, EventArgs e)
		{
			_mainPanel.DisposeEntryPanel();
		}

		protected virtual bool ApplyIfValid(int timeVal, Node server)
		{
			return true;
		}

		protected void CreateUpgradeServerRequest(string AppName, int whenValue)
		{
			ArrayList attrs = new ArrayList();
			attrs.Add( new AttributeValuePair("target", AppName ) );
			attrs.Add( new AttributeValuePair("type", "move_app" ) );

			switch(moveMode)
			{
				case MoveOptions.MoveVirtual:
					attrs.Add( new AttributeValuePair("upgrade_option","move_virtual") );
					break;
				case MoveOptions.MoveCloud:
					attrs.Add(new AttributeValuePair("upgrade_option", "move_virtual"));
					break;
			}
			attrs.Add( new AttributeValuePair("when",whenValue));

			Node upgradeEvent = new Node(AppMoveQueueNode,"MoveApp","",attrs);
		}

		protected int getCurrentSecond()
		{
			int currentSecond = 0;
			if (_Network != null)
			{
				Node tn = _Network.GetNamedNode("CurrentTime");
				if (tn != null)
				{
					currentSecond = tn.GetIntAttribute("seconds",0);
				}
			}
			return currentSecond;
		}


		protected virtual string GetAppPickedName()
		{
			return appPicked.GetAttribute("name");
		}


		protected void okButton_Click(object sender, EventArgs e)
		{
			string name = GetAppPickedName();

			if((whenTextBox.Text == "Next Day")|(whenTextBox.Text == "Now"))
			{
				if (UsingMinutes)
				{
					int currentGameSecond = getCurrentSecond();
					if (currentGameSecond > ((maxmins*60)-SkinningDefs.TheInstance.GetIntData("ops_install_time", 120)))
					{
						SetErrorMessage("Not enough time to complete operation.");
					}
					else
					{
						if (autoTimeSecs ==0)
						{
							CreateUpgradeServerRequest(name, 0);
							_mainPanel.DisposeEntryPanel();
							return;
						}
						else
						{
							//need to check
							CreateUpgradeServerRequest(name, autoTimeSecs);
							_mainPanel.DisposeEntryPanel();
							return;
						}
					}
				}
				else
				{
					//Need to check that Tomorrow is Free
					int day = GetToday();  
					if (IsDayFreeInCalendar(day+1))
					{
						CreateUpgradeServerRequest(name, day+1);
						_mainPanel.DisposeEntryPanel();
						return;
					}
					else
					{
						SetErrorMessage("Specified day is already booked. Please enter a valid day.");
					}				
				}
			}
			else
			{
				if (autoTimeSecs == 0)
				{
					// Got to catch the possability that the user enters too much stuff!
					int time = -1;
					try
					{
						time = CONVERT.ParseInt(whenTextBox.Text);
					}
					catch
					{
						whenTextBox.Text = "Now";
						return;
					}
					if(UsingMinutes) 
					{
						//New section for Time Range and Prevention in the last 2 mins
						if (time>=maxmins)
						{
							SetErrorMessage("Specified time is outwith the round. Please enter a valid time.");
						}
						else
						{
							if ((time * 60) > (((maxmins * 60) - SkinningDefs.TheInstance.GetIntData("ops_install_time", 120))))
							{
								SetErrorMessage("Not enough time to complete operation.");
							}
							else
							{
								int timeToFire = 0;
								timeToFire = (time*60);
								CreateUpgradeServerRequest(name, timeToFire);
								_mainPanel.DisposeEntryPanel();
							}
						}
					}
					else
					{
						if (time <= GetToday())
						{
							SetErrorMessage("Specified day is in the past. Please enter a valid day.");
						}
						else
						{
							//Need to check that provided day is free
							if (IsDayFreeInCalendar(time))
							{
								CreateUpgradeServerRequest(name, time);
								_mainPanel.DisposeEntryPanel();
							}
							else
							{
								SetErrorMessage("Specified day is already booked. Please enter a valid day.");
							}
						}
					}
				}
				else
				{
					//auto not Zero, So we are using Auto in Race Mode
					if(UsingMinutes)
					{
						int timeToFire = 0;
						timeToFire = autoTimeSecs;
						CreateUpgradeServerRequest(name, timeToFire);
						_mainPanel.DisposeEntryPanel();
					}
				}
			}
		}

		#endregion Overall Operational OK and Cancel

		protected void UpgradeRouterControl_GotFocus(object sender, EventArgs e)
		{
			cancelButton.Focus();
		}

		protected void whenTextBox_KeyUp(object sender, KeyEventArgs e)
		{
			if(whenTextBox.Text.Length == 2)
			{
				if (whenTextBox.NextControl != null)
				{
					whenTextBox.NextControl.Focus();
				}
			}
		}

		protected void UpgradeRouterControl_Resize(object sender, EventArgs e)
		{
			cancelButton.Location = new Point( Width-cancelButton.Width-bottomCornerOffset, Height-cancelButton.Height-bottomCornerOffset);
			okButton.Location = new Point( cancelButton.Left-okButton.Width-bottomCornerOffset, cancelButton.Top);
		}



	}
}



