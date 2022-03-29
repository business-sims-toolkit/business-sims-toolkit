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
	/// Summary description for UpgradeMemDiskControl.
	/// </summary>
	public class TransUpgradeMemDiskControl : FlickerFreePanel
	{
		protected Color upColor;
		protected Color downColor;
		protected Color hoverColor;
		protected Color disabledColor;

		protected int xoffset;
		protected int yoffset;
		protected int numOnRow;

		protected IDataEntryControlHolder _mainPanel;
		protected NodeTree _Network;
		protected Node ServerUpgradeQueueNode;
		protected Hashtable ExistingActions = new Hashtable();

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
		protected Label errorlabel;

		protected Button memoryButton;
		protected Button diskButton;
		protected Button bothButton;
		protected Button hwareButton;

		protected Button memoryPendingButton;
		protected Button diskPendingButton;
		protected Button bothPendingButton;
		protected Button hwarePendingButton;

		protected ArrayList disposeControls = new ArrayList();

		protected bool UsingMinutes = false; //Control in Race Phase Using Minutes 
		protected IncidentApplier iApplier;

		protected string filename_huge = "\\images\\buttons\\blank_huge.png";
		protected string filename_long = "\\images\\buttons\\blank_big.png";
		protected string filename_mid = "\\images\\buttons\\blank_med.png";
		protected string filename_short = "\\images\\buttons\\blank_small.png";
		protected string filename_ok = "\\images\\buttons\\ok_blank_small.png";

		protected int ServerButtonsPerRow = 6;
		protected int ServerButtonsHorizontalGap = 3;
		protected int ServerButtonsVerticalGap = 5;
		protected int ServerButtonWidth = 102;
		protected int ServerButtonHeight = 20;
		protected int MemoryPanelWidth = 140;
		protected int MemoryPanelHeight = 130;
		protected int MemoryPanelOffsetX = 40;
		protected int MemoryPanelOffsetY = 0;

		protected int StoragePanelWidth = 140;
		protected int StoragePanelHeight = 130;
		protected int StoragePanelOffsetX = 200;
		protected int StoragePanelOffsetY = 0;

		protected int HardwarePanelWidth = 140;
		protected int HardwarePanelHeight = 130;
		protected int HardwarePanelOffsetX = 360;
		protected int HardwarePanelOffsetY = 0;
		protected Boolean UseLongUpgradeMessage = false;
		protected int UpgradeCancelButtonOffsetY = 60;
		protected int UpgradeCancelButtonOffsetX = 10;
		
		protected enum UpgradeModeOptions
		{
			MEMORY,
			STORAGE,
			BOTH,
			HARDWARE
		}

		protected UpgradeModeOptions upgradeMode;

		protected Node serverPicked;

		protected FocusJumper focusJumper;

		//skin stuff
		protected Color MyGroupPanelBackColor;
		protected Color MyOperationsBackColor;
		protected Font MyDefaultSkinFontNormal12 = null;
		protected Font MyDefaultSkinFontNormal11 = null;
		protected Font MyDefaultSkinFontNormal8 = null;
		protected Font MyDefaultSkinFontBold10 = null;
		protected Font MyDefaultSkinFontBold11 = null;
		protected Font MyDefaultSkinFontBold12 = null;
		protected Font MyDefaultSkinFontBold9 = null;
		public Color errorLabel_foreColor = Color.Red;

		#region Constructor and dispose

		public TransUpgradeMemDiskControl(IDataEntryControlHolder mainPanel, NodeTree model, bool usingmins, 
			IncidentApplier _iApplier, Color OperationsBackColor, Color GroupPanelBackColor)
		{
			focusJumper = new FocusJumper();

			string errmsg_overridecolor = SkinningDefs.TheInstance.GetData("race_errormsg_override_color");
			if (errmsg_overridecolor != "")
			{
				errorLabel_foreColor = SkinningDefs.TheInstance.GetColorData("race_errormsg_override_color");
			}

			//all transition panel 
			MyOperationsBackColor = OperationsBackColor;
			MyGroupPanelBackColor = GroupPanelBackColor;
			
			string fontname =  SkinningDefs.TheInstance.GetData("fontname");
			MyDefaultSkinFontNormal12 = ConstantSizeFont.NewFont(fontname,12);
			MyDefaultSkinFontNormal11 = ConstantSizeFont.NewFont(fontname,11);
			MyDefaultSkinFontNormal8 = ConstantSizeFont.NewFont(fontname,8);
			MyDefaultSkinFontBold12 = ConstantSizeFont.NewFont(fontname,12,FontStyle.Bold);
			MyDefaultSkinFontBold11 = ConstantSizeFont.NewFont(fontname,11,FontStyle.Bold);
			MyDefaultSkinFontBold10 = ConstantSizeFont.NewFont(fontname,10,FontStyle.Bold);
			MyDefaultSkinFontBold9 = ConstantSizeFont.NewFont(fontname,9,FontStyle.Bold);

			BorderStyle = BorderStyle.None;//.Fixed3D;
			BackColor = MyOperationsBackColor;
			//this.BackColor = Color.Cyan;
			iApplier = _iApplier;
			UsingMinutes = usingmins;
			buttonArray = new ArrayList();
			_Network = model;
			_mainPanel = mainPanel;

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

			BuildScreenControls();

			GotFocus += TransUpgradeMemDiskControl_GotFocus;
		}

		public virtual void BuildScreenControls()
		{
			upColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_text_colour", Color.Black);
			downColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_down_text_colour", Color.White);
			hoverColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_hover_text_colour", Color.Green);
			disabledColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_disabled_text_colour", Color.DarkGray);

			title = new Label();
			title.Font = MyDefaultSkinFontBold12;
			title.Text = "Upgrade " + SkinningDefs.TheInstance.GetData("servername", "Server");
			title.TextAlign = ContentAlignment.MiddleLeft;
			title.Size = new Size(500,20);
			title.BackColor = MyOperationsBackColor;
            title.ForeColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("transition_text_colour", Color.Black); 
            title.Location = new Point(5, 5);
			Controls.Add(title);

			//Create the Error Message
			errorlabel = new Label();
			errorlabel.Text = "";
			errorlabel.TextAlign = ContentAlignment.MiddleLeft;
			errorlabel.BackColor = MyOperationsBackColor;
			errorlabel.ForeColor = errorLabel_foreColor;
			errorlabel.Location = new Point(40,120);
			errorlabel.Size = new Size(410,40);
			errorlabel.Visible = false;
			errorlabel.Font = MyDefaultSkinFontBold10;
			Controls.Add(errorlabel);	

			clearErrorMessage();

			chooseServerPanel = new Panel();
			chooseServerPanel.Size = new Size(600,140);
			chooseServerPanel.Location = new Point(5,30);
			chooseServerPanel.BackColor = MyOperationsBackColor;
			//chooseServerPanel.BackColor = Color.Pink;
			BuildServerPanel(_Network);
			Controls.Add( chooseServerPanel );

			chooseUpgradePanel = new Panel();
			chooseUpgradePanel.Size = new Size(525,140);
			chooseUpgradePanel.Location = new Point(5,30);
			chooseUpgradePanel.BackColor = MyOperationsBackColor;
			//chooseUpgradePanel.BackColor = Color.LightSalmon;
			chooseUpgradePanel.Visible = false;
			Controls.Add(chooseUpgradePanel);
			//
			chooseTimePanel = new Panel();
			chooseTimePanel.Size = new Size(525, 140);
			chooseTimePanel.Location = new Point(5,30);
			chooseTimePanel.BackColor = MyOperationsBackColor;
			//chooseTimePanel.BackColor = Color.LightSteelBlue;
			chooseTimePanel.Visible = false;
			Controls.Add(chooseTimePanel);

			okButton = new StyledDynamicButtonCommon ("standard", "OK");
			okButton.Font = MyDefaultSkinFontBold9;
			okButton.Size = new Size(80,20);
			okButton.Location = new Point(350,180);
			okButton.Click += okButton_Click;
			Controls.Add(okButton);
			okButton.Hide();

			focusJumper.Add(okButton);

			cancelButton = new StyledDynamicButtonCommon ("standard", "Close");
			cancelButton.Font = MyDefaultSkinFontBold9;
			cancelButton.Size = new Size(80,20);
			cancelButton.Location = new Point(445,180);
			cancelButton.Click += cancelButton_Click;
			Controls.Add(cancelButton);

			focusJumper.Add(cancelButton);
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

		#endregion Constructor and dispose

		#region Server Selection Panel 

		/// <summary>
		/// Build the Server Selection Panel
		/// </summary>
		/// <param name="model"></param>
		protected void BuildServerPanel(NodeTree model)
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
			//System.Collections.Specialized.StringCollection serverArray = new StringCollection();
			ArrayList serverArray = new ArrayList();
			Hashtable serverNameToNode = new Hashtable();
			foreach(Node server in servers.Keys)
			{
				string name = server.GetAttribute("name");
				if(!name.EndsWith("(M)"))
				{
					serverArray.Add(name);
					serverNameToNode.Add(name,server);
				}
			}
			serverArray.Sort();
			// We can have 6 buttons wide before we have to go to a new line.
			xoffset = 0;
			yoffset = 0;
			numOnRow = 0;

			int itemsOnRow = 6;
			ServerButtonWidth = (chooseServerPanel.Width - ((itemsOnRow - 1) * ServerButtonsHorizontalGap)) / itemsOnRow;
			//
			foreach(string server in serverArray)
			{
				Node serverNode = model.GetNamedNode(server);

				string canUpMem = serverNode.GetAttribute("can_upgrade_mem");
				string canUpDisk = serverNode.GetAttribute("can_upgrade_disk");
				string canUpHware = serverNode.GetAttribute("can_upgrade_hardware");

				ImageTextButton newBtnServer = new StyledDynamicButtonCommon ("standard", server);
				newBtnServer.Font = MyDefaultSkinFontBold9;
				newBtnServer.Location = new Point(xoffset,yoffset);
				newBtnServer.Size = new Size(ServerButtonWidth, ServerButtonHeight);
				newBtnServer.Tag = serverNameToNode[server];
				newBtnServer.Click += HandleServerButton_Click;
				chooseServerPanel.Controls.Add(newBtnServer);
				disposeControls.Add(newBtnServer);

				xoffset += newBtnServer.Width+ServerButtonsHorizontalGap;
				buttonArray.Add(newBtnServer);

				focusJumper.Add(newBtnServer);

				++numOnRow;
				if(numOnRow == (ServerButtonsPerRow-1)) //we started from 0
				{
					numOnRow = 0;
					xoffset = 0;
					yoffset += ServerButtonHeight + ServerButtonsVerticalGap;
				}
			}
		}

		protected void HandleServerButton_Click(object sender, EventArgs e)
		{
			ImageTextButton Imagebutton = (ImageTextButton) sender;
			foreach(ImageTextButton b in buttonArray)
			{
				if(b == Imagebutton)
				{
					serverPicked = (Node) b.Tag;
			
					title.Text += " > "+b.GetButtonText();
								
					BuildOptionsPanel();
					
					chooseUpgradePanel.Visible = true;
					chooseServerPanel.Visible = false;
					return;
				}
			}
		}

		#endregion Server Selection Panel  

		#region Upgrade Type Selection Panel  

		protected void CheckForPending(string servername, 
			out Boolean CanUpMemPending, out Boolean CanUpDiskPending, out Boolean CanUpHwarePending,
			out Node CanUpMem_PendingNode, out Node CanUpDisk_Pending, out Node CanUpHware_PendingNode,
			out int CanUpMem_PendingWhen, out int CanUpDisk_PendingWhen, out int CanUpHware_PendingWhen)
		{
			CanUpMemPending = false;
			CanUpDiskPending = false;
			CanUpHwarePending = false;
			CanUpMem_PendingNode = null;
			CanUpDisk_Pending = null;
			CanUpHware_PendingNode = null;
			CanUpMem_PendingWhen = -1;
			CanUpDisk_PendingWhen = -1;
			CanUpHware_PendingWhen = -1;

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
				}
			}
		}

		protected void BuildMemoryPanel(bool upgrade, int day, Node tag, int upgradecount)
		{
			Color upColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_text_colour", Color.Black);
			Color downColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_down_text_colour", Color.White);
			Color hoverColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_hover_text_colour", Color.Green);
			Color disabledColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_disabled_text_colour", Color.DarkGray);

			string countstr = "";
			if (upgradecount >0)
			{
				countstr = " (" + CONVERT.ToStr(upgradecount) + ")";
			}

			string fileName = "\\images\\buttons\\blank_small.png";
			Image img = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + fileName);

			Panel memoryPanel = new Panel();
			memoryPanel.Size = new Size(MemoryPanelWidth,MemoryPanelHeight);
			memoryPanel.Location = new Point(MemoryPanelOffsetX,MemoryPanelOffsetY);
			memoryPanel.BorderStyle = BorderStyle.None;
			//memoryPanel.BackColor = Color.LightGray;
			memoryPanel.BackColor = MyGroupPanelBackColor;
			chooseUpgradePanel.Controls.Add(memoryPanel);

			Label memoryLabel = new Label();
			memoryLabel.Text = "Memory"+countstr;
			memoryLabel.Font = MyDefaultSkinFontBold11;
			memoryLabel.Location = new Point(5,5);
			memoryLabel.Size = new Size (memoryPanel.Width - (2 * memoryLabel.Left), 40);
            memoryLabel.ForeColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("transition_text_colour", Color.Black);
            memoryPanel.Controls.Add(memoryLabel);

			if(upgrade)
			{
				Label upgrading = new Label();
				if (UseLongUpgradeMessage)
				{
					upgrading.Text = "Upgrade Booked for Day "+CONVERT.ToStr(day);
					upgrading.Size = new Size(memoryPanel.Width-10,40);
					upgrading.Location = new Point(5,28);
                    upgrading.TextAlign = ContentAlignment.MiddleCenter;
				}
				else
				{
					upgrading.Text = "Upgrade Due Day "+CONVERT.ToStr(day);
					upgrading.Size = new Size(125,30);
					upgrading.Location = new Point(5,28);
				}
				upgrading.Font = MyDefaultSkinFontNormal8;
                upgrading.ForeColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("transition_text_colour", Color.Black);
				memoryPanel.Controls.Add(upgrading);
				upgrading.BringToFront();

				ImageTextButton cancelButton = new StyledDynamicButtonCommon ("standard", "Cancel");
				cancelButton.Font = MyDefaultSkinFontBold9;
				cancelButton.Tag = tag;
				cancelButton.Location = new Point(UpgradeCancelButtonOffsetX,UpgradeCancelButtonOffsetY);
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
				upgradeButton.Location = new Point(UpgradeCancelButtonOffsetX,UpgradeCancelButtonOffsetY);
				upgradeButton.Size = new Size(80,20);
				upgradeButton.Click += memoryButton_Click;
				memoryPanel.Controls.Add(upgradeButton);

				focusJumper.Add(upgradeButton);
			}
		}

		protected void BuildDiskPanel(bool upgrade, int day, Node tag, int upgradecount)
		{
			Color upColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_text_colour", Color.Black);
			Color downColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_down_text_colour", Color.White);
			Color hoverColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_hover_text_colour", Color.Green);
			Color disabledColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_disabled_text_colour", Color.DarkGray);

			string countstr = "";
			if (upgradecount >0)
			{
				countstr = " (" + CONVERT.ToStr(upgradecount) + ")";
			}

			string fileName = "\\images\\buttons\\blank_small.png";
			Image img = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + fileName);

			Panel diskPanel = new Panel();
			diskPanel.Size = new Size(StoragePanelWidth,StoragePanelHeight);
			diskPanel.Location = new Point(StoragePanelOffsetX,StoragePanelOffsetY);

			diskPanel.BorderStyle = BorderStyle.None;
			//diskPanel.BackColor = Color.LightGray;
			diskPanel.BackColor = MyGroupPanelBackColor;
			chooseUpgradePanel.Controls.Add(diskPanel);

			Label diskLabel = new Label();
			diskLabel.Text = "Storage"+countstr;
			diskLabel.Font = MyDefaultSkinFontBold11;
			diskLabel.Location = new Point(5,5);
			diskLabel.Size = new Size (diskPanel.Width - (2 * diskLabel.Left), 40);
            diskLabel.ForeColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("transition_text_colour", Color.Black);
            diskPanel.Controls.Add(diskLabel);

			if(upgrade)
			{
				Label upgrading = new Label();
				if (UseLongUpgradeMessage)
				{
					upgrading.Text = "Upgrade Booked for Day "+CONVERT.ToStr(day);
					upgrading.Size = new Size(diskPanel.Width-10,40);
					upgrading.Location = new Point(5,28);
					upgrading.TextAlign = ContentAlignment.MiddleCenter;
				}
				else
				{
					upgrading.Text = "Upgrade Due Day "+CONVERT.ToStr(day);
					upgrading.Size = new Size(125,30);
					upgrading.Location = new Point(5,28);
				}
				upgrading.Font = MyDefaultSkinFontNormal8;
                upgrading.ForeColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("transition_text_colour", Color.Black);
                diskPanel.Controls.Add(upgrading);
				upgrading.BringToFront();

				ImageTextButton cancelButton = new StyledDynamicButtonCommon ("standard", "Cancel");
				cancelButton.Font = MyDefaultSkinFontBold9;
				cancelButton.Tag = tag;
				cancelButton.Location = new Point(UpgradeCancelButtonOffsetX,UpgradeCancelButtonOffsetY);
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
				upgradeButton.Location = new Point(UpgradeCancelButtonOffsetX,UpgradeCancelButtonOffsetY);
				upgradeButton.Size = new Size(80,20);
				upgradeButton.Click += diskButton_Click;
				diskPanel.Controls.Add(upgradeButton);

				focusJumper.Add(upgradeButton);
			}
		}

		protected void BuildHardwarePanel(bool upgrade, int day, Node tag, int upgradecount)
		{
			Color upColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_text_colour", Color.Black);
			Color downColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_down_text_colour", Color.White);
			Color hoverColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_hover_text_colour", Color.Green);
			Color disabledColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_disabled_text_colour", Color.DarkGray);

			string countstr = "";
			if (upgradecount >0)
			{
				countstr = " (" + CONVERT.ToStr(upgradecount) + ")";
			}

			string fileName = "\\images\\buttons\\blank_small.png";
			Image img = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + fileName);

			Panel hardwarePanel = new Panel();
			hardwarePanel.Size = new Size(HardwarePanelWidth,HardwarePanelHeight);
			hardwarePanel.BorderStyle = BorderStyle.None;
			hardwarePanel.Location = new Point(HardwarePanelOffsetX,HardwarePanelOffsetY);
			//hardwarePanel.BackColor = Color.LightGray;
			hardwarePanel.BackColor = MyGroupPanelBackColor;
			chooseUpgradePanel.Controls.Add(hardwarePanel);

			Label hardwareLabel = new Label();
			hardwareLabel.Text = "Hardware"+countstr;
			hardwareLabel.Font = MyDefaultSkinFontBold11;
			hardwareLabel.Location = new Point(5,5);
			hardwareLabel.Size = new Size (hardwarePanel.Width - (2 * hardwareLabel.Left), 40);
            hardwareLabel.ForeColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("transition_text_colour", Color.Black);
            hardwarePanel.Controls.Add(hardwareLabel);

			if(upgrade)
			{
				Label upgrading = new Label();
				if (UseLongUpgradeMessage)
				{
					upgrading.Text = "Upgrade Booked for Day "+CONVERT.ToStr(day);
					upgrading.Size = new Size(hardwarePanel.Width-10,40);
					upgrading.Location = new Point(5,28);
					upgrading.TextAlign = ContentAlignment.MiddleCenter;
				}
				else
				{
					upgrading.Text = "Upgrade Due Day "+CONVERT.ToStr(day);
					upgrading.Size = new Size(125,30);
					upgrading.Location = new Point(5,28);
				}
				upgrading.Font = MyDefaultSkinFontNormal8;
                upgrading.ForeColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("transition_text_colour", Color.Black);
                hardwarePanel.Controls.Add(upgrading);
				upgrading.BringToFront();

				ImageTextButton cancelButton = new StyledDynamicButtonCommon ("standard", "Cancel");
				cancelButton.Font = MyDefaultSkinFontBold9;
				cancelButton.Tag = tag;
				cancelButton.Location = new Point(UpgradeCancelButtonOffsetX,UpgradeCancelButtonOffsetY);
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
				upgradeButton.Location = new Point(UpgradeCancelButtonOffsetX,UpgradeCancelButtonOffsetY);
				upgradeButton.Size = new Size(80,20);
				upgradeButton.Click += hwareButton_Click;
				hardwarePanel.Controls.Add(upgradeButton);

				focusJumper.Add(upgradeButton);
			}
		}

		/// <summary>
		/// This is built on demand on the basis of a selected Server
		/// </summary>
		protected virtual void BuildOptionsPanel()
		{
			string name = serverPicked.GetAttribute("name");

			Boolean canUpMemFlag_Node = false;
			Boolean canUpDiskFlag_Node = false; 
			Boolean canUpHwareFlag_Node = false;
			Boolean canUpMemFlag_Pending = false;
			Boolean canUpDiskFlag_Pending = false;
			Boolean canUpHwareFlag_Pending = false;
			Node canUpMemFlag_PendingNode = null;
			Node canUpDiskFlag_PendingNode = null;
			Node canUpHwareFlag_PendingNode = null;
			int CanUpMem_PendingWhen = -1;
			int CanUpDisk_PendingWhen = -1;
			int CanUpHware_PendingWhen = -1;
			int UpMemCount_Node = 0;  	//How many times have we done this 
			int UpDiskCount_Node = 0; 	//How many times have we done this 
			int UpHwareCount_Node = 0;	//How many times have we done this 

			//Determine wehther the Network will allow the Upgrades 
			canUpMemFlag_Node = serverPicked.GetBooleanAttribute("can_upgrade_mem",false);
			canUpDiskFlag_Node = serverPicked.GetBooleanAttribute("can_upgrade_disk",false);
			canUpHwareFlag_Node = serverPicked.GetBooleanAttribute("can_upgrade_hardware",false);
			
			//Determine how many times we have done each operation
			UpMemCount_Node = serverPicked.GetIntAttribute("count_mem_upgrades",0);
			UpDiskCount_Node = serverPicked.GetIntAttribute("count_disk_upgrades",0);
			UpHwareCount_Node = serverPicked.GetIntAttribute("count_hware_upgrades",0);


			//Determine wehther the PendingActions will allow the Upgrades 
			CheckForPending(name, out canUpMemFlag_Pending, out canUpDiskFlag_Pending, out canUpHwareFlag_Pending, 
				out canUpMemFlag_PendingNode, out canUpDiskFlag_PendingNode, out canUpHwareFlag_PendingNode,
				out CanUpMem_PendingWhen, out CanUpDisk_PendingWhen, out CanUpHware_PendingWhen);

			if(canUpMemFlag_Node || canUpMemFlag_Pending)
			{
				if (canUpMemFlag_PendingNode != null)
					BuildMemoryPanel(true,CanUpMem_PendingWhen, canUpMemFlag_PendingNode, UpMemCount_Node);
				else
					BuildMemoryPanel(false,0,null, UpMemCount_Node);
			}

			if(canUpDiskFlag_Node || canUpDiskFlag_Pending)
			{
				if (canUpDiskFlag_PendingNode != null)
					BuildDiskPanel(true,CanUpDisk_PendingWhen, canUpDiskFlag_PendingNode, UpDiskCount_Node);
				else
					BuildDiskPanel(false,0,null, UpDiskCount_Node);
			}

			if(canUpHwareFlag_Node || canUpHwareFlag_Pending)
			{
				if (canUpHwareFlag_PendingNode != null)
					BuildHardwarePanel(true,CanUpHware_PendingWhen, canUpHwareFlag_PendingNode, UpHwareCount_Node);
				else
					BuildHardwarePanel(false,0,null, UpHwareCount_Node);
			}
			cancelButton.Focus();
		}

		protected void handleOptionButtonClick ()
		{
			handleOptionButtonClick(serverPicked.GetAttribute("name"));
		}

		protected void handleOptionButtonClick (string name)
		{
			Color upColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_text_colour", Color.Black);
			Color downColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_down_text_colour", Color.White);
			Color hoverColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_hover_text_colour", Color.Green);
			Color disabledColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_disabled_text_colour", Color.DarkGray);

			BuildTimePanel(name);
			chooseTimePanel.Visible = true;
			okButton.Visible = true;
			whenTextBox.Focus();
			cancelButton.SetButtonText("Cancel",
				upColor,upColor,
				hoverColor,disabledColor);
			chooseUpgradePanel.Visible = false;
		}

		protected void diskButton_Click(object sender, EventArgs e)
		{
			title.Text += " > Storage";
			upgradeMode = UpgradeModeOptions.STORAGE;
			handleOptionButtonClick();
		}

		protected void memoryButton_Click(object sender, EventArgs e)
		{
			title.Text += " > Memory";
			upgradeMode = UpgradeModeOptions.MEMORY;
			handleOptionButtonClick();
		}

		protected void hwareButton_Click(object sender, EventArgs e)
		{
			title.Text += " > Hardware";
			upgradeMode = UpgradeModeOptions.HARDWARE;
			handleOptionButtonClick();
		}

		protected void RemovingPendingActionNode(Node n1)
		{
			bool success = false;

			//we need to check that tagged node is good 
			if (n1 != null)
			{
				//we need to that no one has removed it already
				//the ui might still be open but the operation and the node has been completed
				//TODO is this best way to do it. 
				if (n1.Parent.HasChild(n1))
				{
					ExistingActions.Remove(n1);		//Delete from the Local Pending Actions actions list
					n1.Parent.DeleteChildTree(n1);  //Delete from the Tree
					BuildOptionsPanel();			//Refresh the Current Options Panel
					success = true;
				}
			}

			if (success)
			{
				_mainPanel.DisposeEntryPanel();
			}
			else
			{
				setErrorMessage(Strings.SentenceCase(SkinningDefs.TheInstance.GetData("servername", "Server")) + " upgrade has already started.");
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

		#endregion Upgrade Type Selection Panel  

		#region When Selection Panel  

		protected void BuildTimePanel ()
		{
			BuildTimePanel(serverPicked.GetAttribute("name"));
		}

		/// <summary>
		/// This is built on demand on the basis of a selected Server
		/// </summary>
		protected void BuildTimePanel (string name)
		{
			timeLabel = new Label();
			timeLabel.Size = new Size(140,25);
			timeLabel.Font = MyDefaultSkinFontNormal12;
			timeLabel.Location = new Point(5,42);
            timeLabel.ForeColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("transition_text_colour", Color.Black);
            chooseTimePanel.Controls.Add(timeLabel);
			//
			whenTextBox = new EntryBox();
			whenTextBox.DigitsOnly = true;
			whenTextBox.Font = MyDefaultSkinFontNormal11;
			whenTextBox.Size = new Size(90,30);
			whenTextBox.Text = "Now";
			whenTextBox.MaxLength = 2;
			whenTextBox.Location = new Point(145,40);
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
			}
			else
			{
				timeLabel.Text = "Install On Day";
				whenTextBox.Text = "Next Day";
			}
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
		/// <param name="DayBooked">Day has been Booked</param>
		/// <param name="DayPastLimit">Day is outside the Calendar limit</param>
		/// <returns></returns>
		protected Boolean IsDayFreeInCalendar(int day, out Boolean DayBooked, out Boolean DayPastLimit)
		{
			DayBooked = false;
			DayPastLimit = false;
			Node CalendarNode = _Network.GetNamedNode("Calendar");
			int CalendarLastDay = CalendarNode.GetIntAttribute("days",-1);

			if((day > (CalendarLastDay-1)))
			{
				DayPastLimit = true;
				return false;
			}

				//Need to iterate over children 
			foreach(Node calendarEvent in CalendarNode.getChildren())
			{
				int cday = calendarEvent.GetIntAttribute("day",0);
				string block = calendarEvent.GetAttribute("block");
				if (day == cday)
				{
					if (block.ToLower() == "true")
					{
						DayBooked = true;
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
			}
			attrs.Add( new AttributeValuePair("when",whenValue));

			Node upgradeEvent = new Node(ServerUpgradeQueueNode,"UpgradeServer","",attrs);
		}

		protected void setErrorMessage(string errmsg)
		{
			errorlabel.Text = errmsg;
			errorlabel.Visible = true;
		}

		protected void clearErrorMessage()
		{
			errorlabel.Visible = false;
		}

		protected virtual void okButton_Click(object sender, EventArgs e)
		{
			string name = serverPicked.GetAttribute("name");
			Boolean DayBookedCheck = false;
			Boolean DayPastLimitCheck = false;

			if((whenTextBox.Text == "Next Day")|(whenTextBox.Text == "Now"))
			{
				if (UsingMinutes)
				{
					CreateUpgradeServerRequest(name, 0);
					_mainPanel.DisposeEntryPanel();
					return;
				}
				else
				{
					//Need to check that Tomorrow is Free
					int day = GetToday();  
					string requestedDayStr = (day+1).ToString();
					if (IsDayFreeInCalendar(day+1,out DayBookedCheck, out DayPastLimitCheck))
					{
						CreateUpgradeServerRequest(name, day+1);
						_mainPanel.DisposeEntryPanel();
						return;
					}
					else
					{
						if (DayBookedCheck)
						{
							setErrorMessage("Specified day is already booked. Please enter a valid day.");
						}
						else
						{
							setErrorMessage("Specified day is outwith the round. Please enter a valid day.");
						}
					}				
				}
			}
			else
			{
				int time = 0;
				Boolean conversionfail = false; 
				try
				{
					//Handling the user provided time (In mins or days) 
					time = CONVERT.ParseInt(whenTextBox.Text);
				}
				catch (Exception)
				{
					conversionfail = true;
				}
				if (conversionfail)
				{
					setErrorMessage("Invalid day requested");
				}
				else
				{
					if(UsingMinutes)
					{
						int timeToFire = 0;
						timeToFire = (time*60);
						CreateUpgradeServerRequest(name, timeToFire);
						_mainPanel.DisposeEntryPanel();
					}
					else
					{
						if (time <= GetToday())
						{
							setErrorMessage("Specified day is in the past. Please enter a valid day.");
						}
						else
						{
							//Need to check that provided day is free
							if (IsDayFreeInCalendar(time,out DayBookedCheck, out DayPastLimitCheck))
							{
								CreateUpgradeServerRequest(name, time);
								_mainPanel.DisposeEntryPanel();
							}
							else
							{
								if (DayBookedCheck)
								{
									setErrorMessage("Specified day is already booked. Please enter a valid day.");
								}
								else
								{
									setErrorMessage("Specified day is outwith the round. Please enter a valid day.");
								}
							}
						}
					}
				}
			}
		}

		#endregion Overall Operational OK and Cancel

		protected void TransUpgradeMemDiskControl_GotFocus(object sender, EventArgs e)
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

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);
			DoSize();
		}

		void DoSize ()
		{
			int instep = 20;
			cancelButton.Location = new Point(Width - instep - cancelButton.Width, Height - instep - cancelButton.Height);
			okButton.Location = new Point(cancelButton.Left - instep - okButton.Width, cancelButton.Top);

			title.Bounds = new Rectangle(0, 0, Width, 25);
			title.BackColor = SkinningDefs.TheInstance.GetColorData("popup_title_background_colour", Color.White);
			title.ForeColor = SkinningDefs.TheInstance.GetColorData("popup_title_foreground_colour", Color.Black);
		}
	}
}