using System;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;

using LibCore;
using Network;

using CoreUtils;
using IncidentManagement;
using CommonGUI;

namespace Polestar_PM.TransScreen
{
	/// <summary>
	/// Summary description for UpgradeMemDiskControl.
	/// </summary>
	public class MS_TransUpgradeMemDiskControl : TransUpgradeMemDiskControl
	{

		public MS_TransUpgradeMemDiskControl(IDataEntryControlHolder mainPanel, NodeTree model, bool usingmins, 
			IncidentApplier _iApplier, Color OperationsBackColor, Color GroupPanelBackColor)
			:base(mainPanel, model, usingmins, _iApplier, OperationsBackColor, GroupPanelBackColor)
		{
		}

		public override void BuildScreenControls()
		{
			ServerButtonsPerRow = 5;
			ServerButtonWidth = 85;
			ServerButtonHeight = 20;

			MemoryPanelWidth = 100;
			MemoryPanelHeight = 130;
			MemoryPanelOffsetX = 10;
			MemoryPanelOffsetY = 0;

			StoragePanelWidth = 100;
			StoragePanelHeight = 130;
			StoragePanelOffsetX = 120;
			StoragePanelOffsetY = 0;

			HardwarePanelWidth = 100;
			HardwarePanelHeight = 130;
			HardwarePanelOffsetX = 225;
			HardwarePanelOffsetY = 0;

			UseLongUpgradeMessage = true;
			UpgradeCancelButtonOffsetY = 75;
			UpgradeCancelButtonOffsetX = 10;


			title = new Label();
			title.Font = MyDefaultSkinFontBold12;
			title.Text = "Upgrade Server";
			title.TextAlign = ContentAlignment.MiddleLeft;
			title.Size = new Size(340,20);
			title.BackColor = MyOperationsBackColor;
			title.Location = new Point(5,5);
			this.Controls.Add(title);

			//Create the Error Message
			errorlabel = new Label();
			errorlabel.Text = "";
			errorlabel.TextAlign = ContentAlignment.MiddleLeft;
			errorlabel.BackColor = MyOperationsBackColor;
			errorlabel.ForeColor = Color.Red;
			errorlabel.Location = new Point(20,195);
			errorlabel.Size = new Size(310,20);
			errorlabel.Visible = false;
			errorlabel.Font = this.MyDefaultSkinFontBold10;
			this.Controls.Add(errorlabel);	

			clearErrorMessage();
			chooseServerPanel = new Panel();
			chooseServerPanel.Size = new Size(360,140);
			chooseServerPanel.Location = new Point(5,30);
			chooseServerPanel.BackColor = MyOperationsBackColor;
			//chooseServerPanel.BackColor = Color.Pink;

			BuildServerPanel(this._Network);
			this.Controls.Add( chooseServerPanel );

			chooseUpgradePanel = new Panel();
			chooseUpgradePanel.Size = new Size(360,140);
			chooseUpgradePanel.Location = new Point(5,30);
			chooseUpgradePanel.BackColor = MyOperationsBackColor;
			//chooseUpgradePanel.BackColor = Color.LightSalmon;
			chooseUpgradePanel.Visible = false;
			this.Controls.Add(chooseUpgradePanel);
			//
			chooseTimePanel = new Panel();
			chooseTimePanel.Size = new Size(360, 140);
			chooseTimePanel.Location = new Point(5,30);
			chooseTimePanel.BackColor = MyOperationsBackColor;
			//chooseTimePanel.BackColor = Color.LightSteelBlue;
			chooseTimePanel.Visible = false;
			this.Controls.Add(chooseTimePanel);

			okButton = new ImageTextButton(0);
			okButton.ButtonFont = MyDefaultSkinFontBold9;
			okButton.SetVariants(filename_short);
			okButton.Size = new Size(80,20);
			okButton.Location = new Point(180,300);
			okButton.SetButtonText("OK",
				System.Drawing.Color.Black,System.Drawing.Color.Black,
				System.Drawing.Color.Green,System.Drawing.Color.DarkGray);
			okButton.Click += new EventHandler(okButton_Click);
			okButton.Visible = false;
			this.Controls.Add(okButton);

			focusJumper.Add(okButton);

			cancelButton = new ImageTextButton(0);
			cancelButton.ButtonFont = MyDefaultSkinFontBold9;
			cancelButton.SetVariants(filename_short);
			cancelButton.Size = new Size(80,20);
			cancelButton.Location = new Point(270,300);
			cancelButton.SetButtonText("Close",
				System.Drawing.Color.Black,System.Drawing.Color.Black,
				System.Drawing.Color.Green,System.Drawing.Color.DarkGray);
			cancelButton.Click += new EventHandler(cancelButton_Click);
			cancelButton.Visible = true;
			this.Controls.Add(cancelButton);

			focusJumper.Add(cancelButton);
		}

	}
}
