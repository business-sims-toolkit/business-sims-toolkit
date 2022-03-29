using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using LibCore;
using Network;

using CoreUtils;
using IncidentManagement;

namespace CommonGUI
{
	/// <summary>
	/// Summary description for UpgradeMemDiskControl.
	/// </summary>
	public class UpgradeMemDiskControl : FlickerFreePanel
	{
		protected IDataEntryControlHolder _mainPanel;
		protected NodeTree _Network;
		protected Node ServerUpgradeQueueNode;
		protected Hashtable ExistingActions = new Hashtable();

		protected Color AutoTimeBackColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("autotimebackcolor", Color.Silver);
		protected Color AutoTimeTextColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("autotimetextcolor", Color.Black);

		protected ImageTextButton okButton;
		protected ImageTextButton cancelButton;

		protected ArrayList buttonArray;

		protected Panel chooseUpgradePanel;
		protected Panel chooseServerPanel;
		protected Panel chooseTimePanel;

		protected Label title;
		protected Label timeLabel;
		protected Label ChosenServerNameLabel;
		protected Label ChosenUpgradeTypeLabel;
		protected EntryBox whenTextBox;
		protected Panel whenEntryPanel;
		protected Label AutoTimeLabel= null;
		protected Label ErrorLabel;

		protected Button memoryButton;
		protected Button diskButton;
		protected Button bothButton;
		protected Button hwareButton;
		protected Button firmwareButton;
		protected Button processorButton;

		protected Button memoryPendingButton;
		protected Button diskPendingButton;
		protected Button bothPendingButton;
		protected Button hwarePendingButton;
		protected Button firmwarePendingButton;
		protected Button processorPendingButton;

		protected ImageTextButton ManualTimeButton = null;
		protected List<ImageTextButton> AutoTimeButtons = new List<ImageTextButton> ();

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
		protected int bottomCornerOffset = 8;
		protected bool ignoreNonVisibleServers = false;

		protected enum UpgradeModeOptions
		{
			MEMORY,
			STORAGE,
			BOTH,
			HARDWARE,
			FIRMWARE, 
			PROCESSOR
		}

		protected UpgradeModeOptions upgradeMode;

		protected Node serverPicked;

		protected FocusJumper focusJumper;

		protected Color MyGroupPanelBackColor;
		protected Color MyOperationsBackColor;
		protected Color MyTextLabelBackColor;

	    protected Color TitleForeColor = Color.Black;
	    protected Color LabelForeColor = Color.Black;

        protected Font MyDefaultSkinFontNormal12 = null;
		protected Font MyDefaultSkinFontNormal11 = null;
		protected Font MyDefaultSkinFontNormal8 = null;
		protected Font MyDefaultSkinFontBold12 = null;
		protected Font MyDefaultSkinFontBold11 = null;
		protected Font MyDefaultSkinFontBold10 = null;
		protected Font MyDefaultSkinFontBold9 = null;
	    Font ciNameFont;

		protected bool playing = false;
		public Color errorLabel_foreColor = Color.Red;

		bool showVersionUpgrades;
		bool showCapacityUpgrades;

		#region Constructor and dispose


		public UpgradeMemDiskControl(IDataEntryControlHolder mainPanel, NodeTree model, bool usingmins, 
			IncidentApplier _iApplier, Boolean IsTrainingMode, Color OperationsBackColor, Color GroupPanelBackColor)
			:this( mainPanel, model, usingmins, _iApplier, IsTrainingMode, OperationsBackColor, GroupPanelBackColor, true, true, true)
		{
		}

		public UpgradeMemDiskControl(IDataEntryControlHolder mainPanel, NodeTree model, bool usingmins, 
			IncidentApplier _iApplier, Boolean IsTrainingMode, Color OperationsBackColor, Color GroupPanelBackColor, 
			bool _playing, bool showVersionUpgrades = true, bool showCapacityUpgrades = true)
		{
			playing = _playing;

			this.showVersionUpgrades = showVersionUpgrades;
			this.showCapacityUpgrades = showCapacityUpgrades;

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
				TitleForeColor =  SkinningDefs.TheInstance.GetColorData("race_paneltitleforecolor");
			}

		    LabelForeColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("race_panellabelforecolor", TitleForeColor);

			MyOperationsBackColor = OperationsBackColor;
			MyGroupPanelBackColor = GroupPanelBackColor;
			MyTextLabelBackColor = OperationsBackColor;

			if (SkinningDefs.TheInstance.GetIntData("race_panels_transparent_backs", 0) == 1)
			{
				MyTextLabelBackColor = Color.Transparent;
			}

			ignoreNonVisibleServers = SkinningDefs.TheInstance.GetBoolData("ignore_non_visible_servers", false);

			//We may wish to offset the ok and cancel buttons from the bottom right corner (standard is 5)
			bottomCornerOffset = SkinningDefs.TheInstance.GetIntData("race_panels_upgrade_memdisk_corneroffset", 5);

			string fontname =  SkinningDefs.TheInstance.GetData("fontname");
			MyDefaultSkinFontNormal12 = ConstantSizeFont.NewFont(fontname,12);
			MyDefaultSkinFontNormal11 = ConstantSizeFont.NewFont(fontname,11);
			MyDefaultSkinFontNormal8 = ConstantSizeFont.NewFont(fontname,8);
			MyDefaultSkinFontBold12 = ConstantSizeFont.NewFont(fontname,12,FontStyle.Bold);
			MyDefaultSkinFontBold11 = ConstantSizeFont.NewFont(fontname,11,FontStyle.Bold);
			MyDefaultSkinFontBold9 = ConstantSizeFont.NewFont(fontname, 9, FontStyle.Bold);
		    MyDefaultSkinFontBold10 = ConstantSizeFont.NewFont(fontname, 10, FontStyle.Bold);
            ciNameFont = ciNameFont = ConstantSizeFont.NewFont(fontname, (float)SkinningDefs.TheInstance.GetDoubleData("upgrade_popup_ci_name_font_size", 9), FontStyle.Bold);

			SuspendLayout();

			//this.BackColor = Color.Transparent;
			iApplier = _iApplier;
			UsingMinutes = usingmins;
			buttonArray = new ArrayList();
			_Network = model;
			_mainPanel = mainPanel;

//			this.BackgroundImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location +
//				"\\images\\panels\\race_panel_back.png");
			
			MyIsTrainingMode = IsTrainingMode;
			if (SkinningDefs.TheInstance.GetBoolData("popups_use_image_background", true))
			{
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
			}

		    title = new Label
		    {
		        Text = "Upgrade " + SkinningDefs.TheInstance.GetData("servername", "Server"),
		        Size = SkinningDefs.TheInstance.GetSizeData("ops_popup_title_size", new Size(500, 20)),
		        Location = new Point (0, 0),
		        Font = SkinningDefs.TheInstance.GetFont(SkinningDefs.TheInstance.GetFloatData("ops_popup_title_font_size", 12), 
		            SkinningDefs.TheInstance.GetBoolData("ops_title_use_bold_font", true) ? FontStyle.Bold : FontStyle.Regular),
                BackColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("popup_title_background_colour",
		            MyTextLabelBackColor),
		        ForeColor = TitleForeColor,
		        TextAlign = ContentAlignment.MiddleLeft
            };
		    Controls.Add(title);

			//Connect up to the Queue Server Actions 
			ServerUpgradeQueueNode = _Network.GetNamedNode("MachineUpgradeQueue");

			ArrayList existingkids = ServerUpgradeQueueNode.getChildren();
			foreach (Node kid in existingkids)
			{
				string servername = kid.GetAttribute("servername");
				int actiontime = kid.GetIntAttribute("when",0);
				string actiontype = kid.GetAttribute("type");
				string option = kid.GetAttribute("option");
				ExistingActions.Add(kid, actiontime);
			}

			chooseServerPanel = new FlickerFreePanel();
			chooseServerPanel.BackColor = MyOperationsBackColor;
			chooseServerPanel.Size = new Size(535,125);
			chooseServerPanel.Location = new Point(10,30 + SkinningDefs.TheInstance.GetIntData("popup_title_gap", 5));
			chooseServerPanel.SuspendLayout();
			BuildServerPanel(model);
			chooseServerPanel.ResumeLayout(false);
			Controls.Add( chooseServerPanel );

			chooseUpgradePanel = new Panel();
			chooseUpgradePanel.BackColor = MyOperationsBackColor;
			//chooseUpgradePanel.BackColor = Color.PowderBlue;
			chooseUpgradePanel.Size = new Size(535,120);
            chooseUpgradePanel.Location = new Point(10, 30 + SkinningDefs.TheInstance.GetIntData("popup_title_gap", 5));
			chooseUpgradePanel.Visible = false;
			Controls.Add(chooseUpgradePanel);
			//
			chooseTimePanel = new Panel();
			chooseTimePanel.BackColor = MyOperationsBackColor;
			//chooseTimePanel.BackColor = Color.PeachPuff;
			chooseTimePanel.Size = new Size(535, 100);
            chooseTimePanel.Location = new Point(10, title.Bottom + SkinningDefs.TheInstance.GetIntData("popup_title_gap", 5));
			chooseTimePanel.Visible = false;
			//BuildTimePanel();
			Controls.Add(chooseTimePanel);
			
			//

			cancelButton = new StyledDynamicButtonCommon ("standard", "Close");
			cancelButton.Font = MyDefaultSkinFontBold10;
			cancelButton.Size = new Size(80,20);
			cancelButton.Location = SkinningDefs.TheInstance.GetPointData("ops_popup_cancel_button_position", 485, 155);
			cancelButton.Click += cancelButton_Click;
			Controls.Add(cancelButton);
			cancelButton.BringToFront();
			focusJumper.Add(cancelButton);

			okButton = new StyledDynamicButtonCommon ("standard", "OK");
			okButton.Font = MyDefaultSkinFontBold10;
			okButton.Size = new Size(80, 20);
			okButton.Location = new Point(cancelButton.Left - 10 - okButton.Width, cancelButton.Top);
			okButton.Click += okButton_Click;
			okButton.Visible = false;
			Controls.Add(okButton);
			focusJumper.Add(okButton);

			ErrorLabel = new Label();
			ErrorLabel.Text = "Error";
			ErrorLabel.TextAlign = ContentAlignment.MiddleLeft;
			ErrorLabel.Size = new Size(420+85,20);
			ErrorLabel.Font = MyDefaultSkinFontBold12;
			ErrorLabel.BackColor = MyTextLabelBackColor;
			ErrorLabel.ForeColor = errorLabel_foreColor;
			ErrorLabel.Location = new Point(10, okButton.Top - 30);
			Controls.Add(ErrorLabel);
			ErrorLabel.BringToFront();

			ClearErrorMessage();

			ResumeLayout(false);

			GotFocus += UpgradeMemDiskControl_GotFocus;
			Resize += UpgradeMemDiskControl_Resize;
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

			// Grab all the Servers
			ArrayList types = new ArrayList();
			types.Add("Server");
			Hashtable servers = _Network.GetNodesOfAttribTypes(types);
			// Alphabetically sort the servers...
			
			ArrayList serverArray = new ArrayList();
			Hashtable serverNameToNode = new Hashtable();
			foreach (Node server in servers.Keys)
			{
				string name = server.GetAttribute("name");

				bool proceed = true;
				bool isMirror = name.EndsWith("(M)");
				bool isVisible = server.GetBooleanAttribute("visible", true);

				if (ignoreNonVisibleServers)
				{
					if (isVisible == false)
					{
						proceed = false;
					}
				}
				if (isMirror)
				{
					proceed = false;
				}

				if (! IsUnderHub(server))
				{
					proceed = false;
				}

				if (proceed)
				{
					serverArray.Add(name);
					serverNameToNode.Add(name,server);
				}
			}
			serverArray.Sort();
			// We can have 6 buttons wide before we have to go to a new line.
			int x0 = 0;
			int xoffset = x0;
			int yoffset = 0;
			int numOnRow = 0;
		    var serverButtonSize = SkinningDefs.TheInstance.GetSizeData("upgrade_server_popup_button_size", new Size(102, 20));
			//
			foreach(string server in serverArray)
			{
				Node serverNode = model.GetNamedNode(server);

				ImageTextButton newBtnServer = new StyledDynamicButtonCommon ("standard", server);
			    newBtnServer.Font = ciNameFont;
				newBtnServer.Location = new Point(xoffset,yoffset);
				newBtnServer.Size = serverButtonSize;
				newBtnServer.Tag = serverNameToNode[server];
				newBtnServer.Click += HandleServerButton_Click;
				chooseServerPanel.Controls.Add(newBtnServer);
				disposeControls.Add(newBtnServer);

				xoffset += newBtnServer.Width+3;
				buttonArray.Add(newBtnServer);

				focusJumper.Add(newBtnServer);

				++numOnRow;
				if(numOnRow == 5)
				{
					numOnRow = 0;
					xoffset = x0;
					yoffset += serverButtonSize.Height + 5;
				}
			}
		}

		bool IsUnderHub (Node node)
		{
			if (node.GetAttribute("name") == "Hub")
			{
				return true;
			}

			if (node.Parent != null)
			{
				return IsUnderHub(node.Parent);
			}

			return false;
		}

		protected void HandleServerButton_Click(object sender, EventArgs e)
		{
			ImageTextButton button = (ImageTextButton) sender;
			foreach(ImageTextButton b in buttonArray)
			{
				if(b == button)
				{
					string disptext = b.GetButtonText();
					serverPicked = (Node) b.Tag;
					title.Text += " > " + disptext;
					//
					BuildOptionsPanel();
					//
					chooseUpgradePanel.Visible = true;
					chooseUpgradePanel.Size = new Size (Width - (2 * chooseUpgradePanel.Left), Height - (2 * chooseUpgradePanel.Top));
					chooseServerPanel.Visible = false;
					return;
				}
			}
			//okButton.Enabled = true;
		}

		#endregion Server Selection Panel  

		#region Upgrade Type Selection Panel  

		protected void CheckForPending(string servername, 
			out Boolean CanUpMemPending, out Boolean CanUpDiskPending, out Boolean CanUpHwarePending, out Boolean CanUpFirmwarePending,
			out Boolean CanUpProcPending,
			out Node CanUpMem_PendingNode, out Node CanUpDisk_Pending, out Node CanUpHware_PendingNode, out Node CanUpFirmware_PendingNode,
			out Node CanUpProc_PendingNode,
			out int CanUpMem_PendingWhen, out int CanUpDisk_PendingWhen, out int CanUpHware_PendingWhen, out int CanUpFirmware_PendingWhen,
			out int CanUpProc_PendingWhen
			)
		{
			CanUpMemPending = false;
			CanUpDiskPending = false;
			CanUpHwarePending = false;
			CanUpFirmwarePending = false;
			CanUpProcPending = false;

			CanUpMem_PendingNode = null;
			CanUpDisk_Pending = null;
			CanUpHware_PendingNode = null;
			CanUpFirmware_PendingNode = null;
			CanUpProc_PendingNode = null;

			CanUpMem_PendingWhen = -1;
			CanUpDisk_PendingWhen = -1;
			CanUpHware_PendingWhen = -1;
			CanUpFirmware_PendingWhen = -1;
			CanUpProc_PendingWhen = -1;

			//read throught the current requests
			foreach (Node n1 in ExistingActions.Keys)
			{
				string pending_server_name = n1.GetAttribute("target");
				string pending_upgrade_type = n1.GetAttribute("type");
				string pending_upgrade_option = n1.GetAttribute("upgrade_option");
				int pending_when = n1.GetIntAttribute("when",0);
				if (servername.ToLower()==pending_server_name.ToLower())
				{
					if (pending_upgrade_option == "memory") 
					{
						CanUpMemPending = true;
						CanUpMem_PendingNode = n1;
						CanUpMem_PendingWhen = pending_when;
					}
					if (pending_upgrade_option == "storage")
					{
						CanUpDiskPending = true;
						CanUpDisk_Pending = n1;
						CanUpDisk_PendingWhen = pending_when;
					}
					if (pending_upgrade_option == "hardware")
					{
						CanUpHwarePending = true;
						CanUpHware_PendingNode = n1;
						CanUpHware_PendingWhen = pending_when;
					}
					if (pending_upgrade_option == "firmware")
					{
						CanUpFirmwarePending = true;
						CanUpFirmware_PendingNode = n1;
						CanUpFirmware_PendingWhen = pending_when;
					}
					if (pending_upgrade_option == "processor")
					{
						CanUpProcPending = true;
						CanUpProc_PendingNode = n1;
						CanUpProc_PendingWhen = pending_when;
					}
				}
			}
		}
		
		protected string BuildTimeString(int timevalue)
		{
			return FormatAutoTime(timevalue);
		}
		
		
		protected void BuildMemoryPanel(bool upgrade, int day, Node tag, int upgradecount)
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

			Panel memoryPanel = new Panel();
			memoryPanel.Size = new Size(160,100);
			memoryPanel.Location = new Point(40,10);
			memoryPanel.BorderStyle = BorderStyle.None;
			memoryPanel.BackColor = MyGroupPanelBackColor;
			chooseUpgradePanel.Controls.Add(memoryPanel);
            
			Label memoryLabel = new Label();
            memoryLabel.Text = SkinningDefs.TheInstance.GetData("esm_memory_label", "Memory") + countstr;
			memoryLabel.Font = MyDefaultSkinFontBold11;
			memoryLabel.Location = new Point(5,5);
			memoryLabel.Width = memoryPanel.Width - (2 * memoryLabel.Left);
			memoryPanel.Controls.Add(memoryLabel);

			if(upgrade)
			{
				Label upgrading = new Label();
				upgrading.Text = "Upgrade Due at "+BuildTimeString(day);
				upgrading.Size = new Size(145,30);
				upgrading.Font = MyDefaultSkinFontNormal8;
				upgrading.Location = new Point(5,28);
				memoryPanel.Controls.Add(upgrading);

				ImageTextButton cancelButton = new StyledDynamicButtonCommon ("standard", "Cancel");
				cancelButton.Font = MyDefaultSkinFontBold9;
				cancelButton.Tag = tag;
				cancelButton.Location = new Point(10,60);
				cancelButton.Size = new Size(80,20);
				cancelButton.Click += memoryPendingButton_Click;
				memoryPanel.Controls.Add(cancelButton);

				focusJumper.Add(cancelButton);
			}
			else
			{
				ImageTextButton upgradeButton = new StyledDynamicButtonCommon ("standard", "Upgrade");
				upgradeButton.Font = MyDefaultSkinFontBold9;
				upgradeButton.Tag = tag;
				upgradeButton.Location = new Point(10,60);
				upgradeButton.Size = new Size(80,20);
				upgradeButton.Click += memoryButton_Click;
				memoryPanel.Controls.Add(upgradeButton);

				focusJumper.Add(upgradeButton);
			}
		}

		protected void BuildDiskPanel(bool upgrade, int day, Node tag, int upgradecount)
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

			Panel diskPanel = new Panel();
			diskPanel.Size = new Size(160,100);
			diskPanel.Location = new Point(210,10);
			diskPanel.BorderStyle = BorderStyle.None;
			diskPanel.BackColor = MyGroupPanelBackColor;
			chooseUpgradePanel.Controls.Add(diskPanel);

			Label diskLabel = new Label();
			diskLabel.Text = SkinningDefs.TheInstance.GetData("esm_storage_label", "Storage") + countstr;
            diskLabel.Font = MyDefaultSkinFontBold11;
			diskLabel.Location = new Point(5,5);
			diskLabel.Width = diskPanel.Width - (2 * diskLabel.Left);
			diskPanel.Controls.Add(diskLabel);

			if(upgrade)
			{
				Label upgrading = new Label();
				upgrading.Text = "Upgrade Due at "+BuildTimeString(day);
				//upgrading.Text = "Upgrade Due \non minute "+CONVERT.ToStr(day/60);
				upgrading.Size = new Size(145,30);
				upgrading.Font = MyDefaultSkinFontNormal8;
				upgrading.Location = new Point(5,28);
				diskPanel.Controls.Add(upgrading);

				ImageTextButton cancelButton = new StyledDynamicButtonCommon ("standard", "Cancel");
				cancelButton.Font = MyDefaultSkinFontBold9;
				cancelButton.Tag = tag;
				cancelButton.Location = new Point(10,60);
				cancelButton.Size = new Size(80,20);
				cancelButton.Click += diskPendingButton_Click;
				diskPanel.Controls.Add(cancelButton);

				focusJumper.Add(cancelButton);
			}
			else
			{
				ImageTextButton upgradeButton = new StyledDynamicButtonCommon ("standard", "Upgrade");
				upgradeButton.Font = MyDefaultSkinFontBold9;
				upgradeButton.Tag = tag;
				upgradeButton.Location = new Point(10,60);
				upgradeButton.Size = new Size(80,20);
				upgradeButton.Click += diskButton_Click;
				diskPanel.Controls.Add(upgradeButton);

				focusJumper.Add(upgradeButton);
			}
		}

		protected virtual void BuildHardwarePanel(bool upgrade_pending, int day, Node tag, int upgradecount)
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

			Panel hardwarePanel = new Panel();
			hardwarePanel.Size = new Size(160,100);
			hardwarePanel.BorderStyle = BorderStyle.None;
			hardwarePanel.Location = new Point(380,10);
			hardwarePanel.BackColor = MyGroupPanelBackColor;
			chooseUpgradePanel.Controls.Add(hardwarePanel);

			Label hardwareLabel = new Label();
			hardwareLabel.Text = "Hardware"+countstr;
			hardwareLabel.Font = MyDefaultSkinFontBold11;
			hardwareLabel.Location = new Point(5,5);
			hardwarePanel.Controls.Add(hardwareLabel);

			if(upgrade_pending)
			{
				Label upgrading = new Label();
				upgrading.Text = "Upgrade Due at "+BuildTimeString(day);
				//upgrading.Text = "Upgrade Due \non minute "+CONVERT.ToStr(day/60);
				upgrading.Size = new Size(145,30);
				upgrading.Font = MyDefaultSkinFontNormal8;
				upgrading.Location = new Point(5,28);
				hardwarePanel.Controls.Add(upgrading);

				ImageTextButton cancelButton = new StyledDynamicButtonCommon ("standard", "Cancel");
				cancelButton.Font = MyDefaultSkinFontBold9;
				cancelButton.Tag = tag;
				cancelButton.Location = new Point(10,60);
				cancelButton.Size = new Size(80,20);
				cancelButton.Click += hwarePendingButton_Click;
				hardwarePanel.Controls.Add(cancelButton);

				focusJumper.Add(cancelButton);
			}
			else
			{
				ImageTextButton upgradeButton = new StyledDynamicButtonCommon ("standard", "Upgrade");
				upgradeButton.Font = MyDefaultSkinFontBold9;
				upgradeButton.Tag = tag;
				upgradeButton.Location = new Point(10,60);
				upgradeButton.Size = new Size(80,20);
				upgradeButton.Click += hwareButton_Click;
				hardwarePanel.Controls.Add(upgradeButton);

				focusJumper.Add(upgradeButton);
			}
		}

		protected virtual void BuildFirmwarePanel(bool upgrade_pending, int day, Node tag, int upgradecount)
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

			Panel firmwarePanel = new Panel();
			firmwarePanel.Size = new Size(160,100);
			firmwarePanel.BorderStyle = BorderStyle.None;
			firmwarePanel.Location = new Point(40,10);
			firmwarePanel.BackColor = MyGroupPanelBackColor;
			chooseUpgradePanel.Controls.Add(firmwarePanel);

			Label firmwareLabel = new Label();
			firmwareLabel.Text = "Firmware"+countstr;
			firmwareLabel.Font = MyDefaultSkinFontBold11;
			firmwareLabel.Location = new Point(5,5);
			firmwarePanel.Controls.Add(firmwareLabel);

			if(upgrade_pending)
			{
				Label upgrading = new Label();
				upgrading.Text = "Upgrade Due at "+BuildTimeString(day);
				//upgrading.Text = "Upgrade Due \non minute "+CONVERT.ToStr(day/60);
				upgrading.Size = new Size(145,30);
				upgrading.Font = MyDefaultSkinFontNormal8;
				upgrading.Location = new Point(5,28);
				firmwarePanel.Controls.Add(upgrading);

				ImageTextButton cancelButton = new StyledDynamicButtonCommon ("standard", "Cancel");
				cancelButton.Font = MyDefaultSkinFontBold9;
				cancelButton.Tag = tag;
				cancelButton.Location = new Point(10,60);
				cancelButton.Size = new Size(80,20);
				cancelButton.Click += firmwarePendingButton_Click;
				firmwarePanel.Controls.Add(cancelButton);

				focusJumper.Add(cancelButton);
			}
			else
			{
				ImageTextButton upgradeButton = new StyledDynamicButtonCommon ("standard", "Upgrade");
				upgradeButton.Font = MyDefaultSkinFontBold9;
				upgradeButton.Tag = tag;
				upgradeButton.Location = new Point(10,60);
				upgradeButton.Size = new Size(80,20);
				upgradeButton.Click += firmwareButton_Click;
				firmwarePanel.Controls.Add(upgradeButton);

				focusJumper.Add(upgradeButton);
			}
		}

		protected virtual void BuildProcessorPanel(bool upgrade_pending, int day, Node tag, int upgradecount)
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

			Panel procPanel = new Panel();
			procPanel.Size = new Size(160, 100);
			procPanel.BorderStyle = BorderStyle.None;
			procPanel.Location = new Point(40, 10);
			procPanel.BackColor = MyGroupPanelBackColor;
			chooseUpgradePanel.Controls.Add(procPanel);

			Label procLabel = new Label();
			procLabel.Text = "Processor" + countstr;
			procLabel.Size = new Size(140, 20);
			procLabel.Font = MyDefaultSkinFontBold11;
			procLabel.Location = new Point(5, 5);
			procPanel.Controls.Add(procLabel);

			if (upgrade_pending)
			{
				Label upgrading = new Label();
				upgrading.Text = "Upgrade Due at " + BuildTimeString(day);
				//upgrading.Text = "Upgrade Due \non minute "+CONVERT.ToStr(day/60);
				upgrading.Size = new Size(145, 30);
				upgrading.Font = MyDefaultSkinFontNormal8;
				upgrading.Location = new Point(5, 28);
				procPanel.Controls.Add(upgrading);

				ImageTextButton cancelButton = new StyledDynamicButtonCommon ("standard", "Cancel");
				cancelButton.Font = MyDefaultSkinFontBold9;
				cancelButton.Tag = tag;
				cancelButton.Location = new Point(10, 60);
				cancelButton.Size = new Size(80, 20);
				cancelButton.Click += firmwarePendingButton_Click;
				procPanel.Controls.Add(cancelButton);

				focusJumper.Add(cancelButton);
			}
			else
			{
				ImageTextButton upgradeButton = new StyledDynamicButtonCommon ("standard", "Upgrade");
				upgradeButton.Font = MyDefaultSkinFontBold9;
				upgradeButton.Tag = tag;
				upgradeButton.Location = new Point(10, 60);
				upgradeButton.Size = new Size(80, 20);
				upgradeButton.Click += procButton_Click;
				procPanel.Controls.Add(upgradeButton);
				focusJumper.Add(upgradeButton);
			}
		}

		/// <summary>
		/// This is built on demand on the basis of a selected Server
		/// </summary>
		protected virtual void BuildOptionsPanel()
		{
			string name = GetServerPickedName();

			Boolean canUpMemFlag_Node = false;
			Boolean canUpDiskFlag_Node = false; 
			Boolean canUpHwareFlag_Node = false;
			Boolean canUpFirmwareFlag_Node = false;
			Boolean canUpProcFlag_Node = false;

			Boolean canUpMemFlag_Pending = false;
			Boolean canUpDiskFlag_Pending = false;
			Boolean canUpHwareFlag_Pending = false;
			Boolean canUpFirmwareFlag_Pending = false;
			Boolean canUpProcFlag_Pending = false;

			Node canUpMemFlag_PendingNode = null;
			Node canUpDiskFlag_PendingNode = null;
			Node canUpHwareFlag_PendingNode = null;
			Node canUpFirmwareFlag_PendingNode = null;
			Node canUpProcFlag_PendingNode = null;

			int CanUpMem_PendingWhen = -1;
			int CanUpDisk_PendingWhen = -1;
			int CanUpHware_PendingWhen = -1;
			int CanUpFirmware_PendingWhen = -1;
			int CanUpProc_PendingWhen = -1;

			int UpMemCount_Node = 0;  	//How many times have we done this 
			int UpDiskCount_Node = 0; 	//How many times have we done this 
			int UpHwareCount_Node = 0;	//How many times have we done this 
			int UpFirmwareCount_Node = 0;	//How many times have we done this 
			int UpProcCount_Node = 0;	//How many times have we done this 

			//Determine wehther the Network will allow the Upgrades 
			canUpMemFlag_Node = serverPicked.GetBooleanAttribute("can_upgrade_mem",false);
			canUpDiskFlag_Node = serverPicked.GetBooleanAttribute("can_upgrade_disk",false);
			canUpHwareFlag_Node = serverPicked.GetBooleanAttribute("can_upgrade_hardware",false);
			canUpFirmwareFlag_Node = serverPicked.GetBooleanAttribute("can_upgrade_firmware",false);
			canUpProcFlag_Node = serverPicked.GetBooleanAttribute("can_upgrade_proc", false);

			//Determine how many times we have done each operation
			UpMemCount_Node = serverPicked.GetIntAttribute("count_mem_upgrades",0);
			UpDiskCount_Node = serverPicked.GetIntAttribute("count_disk_upgrades",0);
			UpHwareCount_Node = serverPicked.GetIntAttribute("count_hware_upgrades",0);
			UpFirmwareCount_Node = serverPicked.GetIntAttribute("count_firmware_upgrades",0);
			UpProcCount_Node = serverPicked.GetIntAttribute("count_proc_upgrades", 0);

			//Determine wehther the PendingActions will allow the Upgrades 
			CheckForPending(name,
											out canUpMemFlag_Pending, out canUpDiskFlag_Pending, out canUpHwareFlag_Pending, out canUpFirmwareFlag_Pending, out canUpProcFlag_Pending,
											out canUpMemFlag_PendingNode, out canUpDiskFlag_PendingNode, out canUpHwareFlag_PendingNode, out canUpFirmwareFlag_PendingNode, out canUpProcFlag_PendingNode,
											out CanUpMem_PendingWhen, out CanUpDisk_PendingWhen, out CanUpHware_PendingWhen, out CanUpFirmware_PendingWhen, out CanUpProc_PendingWhen);

			if (showCapacityUpgrades  && (canUpMemFlag_Node || canUpMemFlag_Pending))
			{
				if (canUpMemFlag_PendingNode != null)
					BuildMemoryPanel(true,CanUpMem_PendingWhen, canUpMemFlag_PendingNode, UpMemCount_Node);
				else
					BuildMemoryPanel(false,0,null, UpMemCount_Node);
			}

			if (showCapacityUpgrades && (canUpDiskFlag_Node || canUpDiskFlag_Pending))
			{
				if (canUpDiskFlag_PendingNode != null)
					BuildDiskPanel(true,CanUpDisk_PendingWhen, canUpDiskFlag_PendingNode, UpDiskCount_Node);
				else
					BuildDiskPanel(false,0,null, UpDiskCount_Node);
			}

			if( showVersionUpgrades && (canUpHwareFlag_Node || canUpHwareFlag_Pending))
			{
				if (canUpHwareFlag_PendingNode != null)
					BuildHardwarePanel(true,CanUpHware_PendingWhen, canUpHwareFlag_PendingNode, UpHwareCount_Node);
				else
					BuildHardwarePanel(false,0,null, UpHwareCount_Node);
			}

			if(showVersionUpgrades && (canUpFirmwareFlag_Node || canUpFirmwareFlag_Pending))
			{
				if (canUpFirmwareFlag_PendingNode != null)
					BuildFirmwarePanel(true,CanUpFirmware_PendingWhen, canUpFirmwareFlag_PendingNode, UpFirmwareCount_Node);
				else
					BuildFirmwarePanel(false,0,null, UpFirmwareCount_Node);
			}

			if (showCapacityUpgrades && (canUpProcFlag_Node || canUpProcFlag_Pending))
			{
				if (canUpProcFlag_PendingNode != null)
				{
					BuildProcessorPanel(true, CanUpProc_PendingWhen, canUpProcFlag_PendingNode, UpProcCount_Node);
				}
				else
				{
					BuildProcessorPanel(false, 0, null, UpProcCount_Node);
				}
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

		protected void diskButton_Click(object sender, EventArgs e)
		{
		    string titleString = SkinningDefs.TheInstance.GetData("esm_storage_label", "Storage");
            title.Text += " > " + titleString;
			upgradeMode = UpgradeModeOptions.STORAGE;
			handleOptionButtonClick();
		}

		protected void memoryButton_Click(object sender, EventArgs e)
		{
            string titleString = SkinningDefs.TheInstance.GetData("esm_memory_label", "Memory");
            title.Text += " > " + titleString;
			upgradeMode = UpgradeModeOptions.MEMORY;
			handleOptionButtonClick();
		}

		protected void hwareButton_Click(object sender, EventArgs e)
		{
            string titleString = SkinningDefs.TheInstance.GetData("esm_hardware_label", "Hardware");
            title.Text += " > " + titleString;
			upgradeMode = UpgradeModeOptions.HARDWARE;
			handleOptionButtonClick();
		}

		protected void firmwareButton_Click(object sender, EventArgs e)
		{
            string titleString = SkinningDefs.TheInstance.GetData("esm_firmware_label", "Firmware");
            title.Text += " > " + titleString;
			upgradeMode = UpgradeModeOptions.FIRMWARE;
			handleOptionButtonClick();
		}

		protected void procButton_Click(object sender, EventArgs e)
		{
            string titleString = SkinningDefs.TheInstance.GetData("esm_processor_label", "Processor");
            title.Text += " > " + titleString;
			upgradeMode = UpgradeModeOptions.PROCESSOR;
			handleOptionButtonClick();
		}

		protected void RemovingPendingActionNode(Node n1)
		{
		    string ServerReplacementText = SkinningDefs.TheInstance.GetData("servername", "Server");
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
					SetErrorMessage(ServerReplacementText + " Upgrade has already been completed");
				}
			}
			else
			{
				SetErrorMessage(ServerReplacementText + " Upgrade has already been completed");
			}	
		}

		protected void diskPendingButton_Click(object sender, EventArgs e)
		{
			Node n1 = (Node) ((ImageTextButton)sender).Tag; //Extract the Queue Node
			RemovingPendingActionNode(n1);
		}

		protected void memoryPendingButton_Click(object sender, EventArgs e)
		{
			Node n1 = (Node) ((ImageTextButton)sender).Tag; //Extract the Queue Node
			RemovingPendingActionNode(n1);
		}

		protected void hwarePendingButton_Click(object sender, EventArgs e)
		{
			Node n1 = (Node) ((ImageTextButton)sender).Tag; //Extract the Queue Node
			RemovingPendingActionNode(n1);
		}

		protected void firmwarePendingButton_Click(object sender, EventArgs e)
		{
			Node n1 = (Node) ((ImageTextButton)sender).Tag; //Extract the Queue Node
			RemovingPendingActionNode(n1);
		}

		protected void procPendingButton_Click(object sender, EventArgs e)
		{
			Node n1 = (Node)((ImageTextButton)sender).Tag; //Extract the Queue Node
			RemovingPendingActionNode(n1);
		}


		#endregion Upgrade Type Selection Panel  

		#region When Selection Panel  

		/// <summary>
		/// This is built on demand on the basis of a selected Server
		/// </summary>
		protected virtual void BuildTimePanel()
		{
			string name =  GetServerPickedName();

			Color upColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_text_colour", Color.Black);
			Color downColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_down_text_colour", Color.White);
			Color hoverColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_hover_text_colour", Color.Green);
			Color disabledColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_disabled_text_colour", Color.DarkGray);

			timeLabel = new Label();
			timeLabel.Size = new Size(140,25);
			timeLabel.Font = MyDefaultSkinFontNormal12;
			timeLabel.Location = new Point(5+150,42-32);
			timeLabel.ForeColor = LabelForeColor;
			chooseTimePanel.Controls.Add(timeLabel);
			//
			whenEntryPanel = CreateWhenEntryPanel();
			chooseTimePanel.Controls.Add(whenEntryPanel);

			//define the text for the different modes
			if(UsingMinutes)
			{
				timeLabel.Text = GetUpgradeTimeLabelText();
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
					ManualTimeButton = new StyledDynamicButtonCommon ("standard", "Manual");
					ManualTimeButton.Font = MyDefaultSkinFontBold9;
					ManualTimeButton.Size = new Size(130,20);
					ManualTimeButton.Location = new Point(10,yoffset);
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
						ImageTextButton AutoTimeButton = new StyledDynamicButtonCommon ("standard", displayname);
						AutoTimeButton.Font = MyDefaultSkinFontBold9;
						AutoTimeButton.Size = new Size(130,20);
						AutoTimeButton.Location = new Point(10,yoffset);
						AutoTimeButton.Tag = CONVERT.ParseInt(st);
						AutoTimeButton.Click += AutoTimeButton_Click;
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
				whenTextBox.Text = "Next Day";
			}

			SelectTimeButton(ManualTimeButton);
		}

		void SelectTimeButton (ImageTextButton sender)
		{
			ManualTimeButton.Active = (sender == ManualTimeButton);

			foreach (var button in AutoTimeButtons)
			{
				button.Active = (button == sender);
			}
		}

		protected virtual Panel CreateWhenEntryPanel ()
		{
			Panel panel = new Panel ();
			panel.Location = new Point (175, 40);
			panel.Size = new Size (90, 30);

			whenTextBox = new EntryBox();
			whenTextBox.DigitsOnly = true;
			whenTextBox.Font = MyDefaultSkinFontNormal11;
			whenTextBox.Size = panel.Size;
			whenTextBox.Text = "Now";
			whenTextBox.MaxLength = 2;
			whenTextBox.TextAlign = HorizontalAlignment.Center;
			panel.Controls.Add(whenTextBox);
			whenTextBox.GotFocus += whenTextBox_GotFocus;
			whenTextBox.LostFocus += whenTextBox_LostFocus;
			whenTextBox.KeyUp += whenTextBox_KeyUp;

			focusJumper.Add(whenTextBox);

			return panel;
		}

		protected virtual string GetUpgradeTimeLabelText ()
		{
			if (UsingMinutes)
			{
				return SkinningDefs.TheInstance.GetData("esm_install_at_min","Install At Min");
			}
			else
			{
				return SkinningDefs.TheInstance.GetData("esm_install_on_day","Install On Day");
			}
		}

		protected void AutoTimeButton_Click(object sender, EventArgs e)
		{
			ImageTextButton b1 = (ImageTextButton) sender;
			autoTimeSecs =(int) b1.Tag;

			ShowAutoTimeLabel(FormatAutoTime(autoTimeSecs));
			SelectTimeButton((ImageTextButton) sender);
		}

		protected virtual string FormatAutoTime (int time)
		{
			int minutes = time / 60;
			int seconds = time % 60;

			return CONVERT.Format("{0}:{1:00}", minutes, seconds);
		}

		void ShowAutoTimeLabel (string time)
		{
			if (AutoTimeLabel != null)
			{
				AutoTimeLabel.Text = time;
				AutoTimeLabel.Visible = true;
			}
			whenEntryPanel.Visible = false;
			ClearErrorMessage();
		}

		protected void ManualTimeButton_Click(object sender, EventArgs e)
		{
			autoTimeSecs = 0;
			if (AutoTimeLabel != null)
			{
				AutoTimeLabel.Visible = false;
			}
			whenEntryPanel.Visible = true;
			whenTextBox.Text = "Now";
			whenTextBox.Focus();
			ClearErrorMessage();

			SelectTimeButton((ImageTextButton) sender);
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

		protected void CreateUpgradeServerRequest(string ServerName, int whenValue)
		{
			ArrayList attrs = new ArrayList();
			attrs.Add( new AttributeValuePair("target", ServerName ) );
			attrs.Add( new AttributeValuePair("type", "upgrade_server" ) );

			switch(upgradeMode)
			{
				case UpgradeModeOptions.MEMORY:
					attrs.Add( new AttributeValuePair("upgrade_option","memory") );
					break;

				case UpgradeModeOptions.STORAGE:
					attrs.Add( new AttributeValuePair("upgrade_option","storage") );
					break;

				case UpgradeModeOptions.BOTH:
					attrs.Add( new AttributeValuePair("upgrade_option","both") );
					break;

				case UpgradeModeOptions.HARDWARE:
					attrs.Add( new AttributeValuePair("upgrade_option","hardware") );
					break;

				case UpgradeModeOptions.FIRMWARE:
					attrs.Add( new AttributeValuePair("upgrade_option","firmware") );
					break;

				case UpgradeModeOptions.PROCESSOR:
					attrs.Add(new AttributeValuePair("upgrade_option", "processor"));
					break;

			}
			attrs.Add( new AttributeValuePair("when",whenValue));

			Node upgradeEvent = new Node(ServerUpgradeQueueNode,"UpgradeServer","",attrs);
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


		protected virtual string GetServerPickedName()
		{
			return serverPicked.GetAttribute("name");
		}


		protected void okButton_Click(object sender, EventArgs e)
		{
			string name = GetServerPickedName();
			int currentSecond = getCurrentSecond();
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
							if (currentGameSecond < autoTimeSecs)
							{
								//need to check
								CreateUpgradeServerRequest(name, autoTimeSecs);
								_mainPanel.DisposeEntryPanel();
								return;
							}
							else
							{
								SetErrorMessage("Specified time is in the past. Please enter a valid time.");
							}
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
						time = GetRequestedTimeOffset();
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
							else if (time * 60 < currentSecond)
							{
								SetErrorMessage("Specified time is in the past. Please enter a valid time.");
							}
							else
							{
								int timeToFire = 0;
								timeToFire = (time * 60);
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

		protected virtual int GetRequestedTimeOffset ()
		{
			return CONVERT.ParseIntSafe(whenTextBox.Text, 0);
		}

		#endregion Overall Operational OK and Cancel

		protected void UpgradeMemDiskControl_GotFocus(object sender, EventArgs e)
		{
			cancelButton.Focus();
		}

		protected virtual void whenTextBox_KeyUp(object sender, KeyEventArgs e)
		{
			if(whenTextBox.Text.Length == 2)
			{
				if (whenTextBox.NextControl != null)
				{
					whenTextBox.NextControl.Focus();
				}
			}
		}

		protected void UpgradeMemDiskControl_Resize(object sender, EventArgs e)
		{
			if (string.IsNullOrEmpty(SkinningDefs.TheInstance.GetData("ops_popup_cancel_button_position")))
			{
				cancelButton.Location = new Point(Width - cancelButton.Width - bottomCornerOffset,
				                                       Height - cancelButton.Height - bottomCornerOffset);
				okButton.Location = new Point(cancelButton.Left - okButton.Width - bottomCornerOffset, cancelButton.Top);
			}

		    const int padding = 10;
            
		    var subPanelSize = new Size(Width - 2 * padding, (cancelButton.Top - title.Bottom) - 2 * padding);

		    chooseServerPanel.Size = subPanelSize;
            chooseServerPanel.Location = new Point(padding, title.Bottom + padding);

            chooseUpgradePanel.Size = subPanelSize;
		    chooseUpgradePanel.Location = new Point(padding, title.Bottom + padding);

		    chooseTimePanel.Size = subPanelSize;
		    chooseTimePanel.Location = new Point(padding, title.Bottom + padding);

            if (SkinningDefs.TheInstance.GetBoolData("ops_popup_title_use_full_width", false))
		    {
		        title.Size = new Size (Width, 25);
		    }
        }
	}
}
