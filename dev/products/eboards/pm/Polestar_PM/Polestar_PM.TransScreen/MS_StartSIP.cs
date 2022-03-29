using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;

using LibCore;
using Network;

using CoreUtils;
using CommonGUI;

using IncidentManagement;
using TransitionScreens;

namespace Polestar_PM.TransScreen
{
	/// <summary>
	/// Summary description for TransitionControlPanel.
	/// </summary>
	public class MS_StartSIP : StartSIP
	{
		public MS_StartSIP(TransitionControlPanel tcp, NodeTree nt, int Round, Color OperationsBackColor)
		: base (tcp, nt, Round, OperationsBackColor)
		{
		}
		
		public override void BuildScreenControls()
		{
			Label header = new Label();
			header.Text = "Add Product";
			header.Font = MyDefaultSkinFontBold12;
			header.Location = new Point(10,10);
			header.Size = new Size(250,25);
			this.Controls.Add(header);

			error = new Label();
			error.ForeColor = Color.Red;
			error.Font = MyDefaultSkinFontBold12; 
			error.Location = new Point(10,110);
			error.TextAlign = ContentAlignment.MiddleCenter;
			error.Size = new Size(350,20);
			this.Controls.Add(error);

			Label platform_label = new Label();
			platform_label.Text = "Platform";
			platform_label.Size = new Size(90,20);
			platform_label.Font = MyDefaultSkinFontBold12;
			platform_label.Location = new Point(310-45-180+100,58);
			this.Controls.Add(platform_label);

			platformID = new EntryBox();
			platformID.TextAlign = HorizontalAlignment.Center;
			platformID.Location = new Point(310-45-180+100,75+5);
			platformID.Size = new Size(80,30);
			platformID.Font = MyDefaultSkinFontNormal12;
			platformID.GotFocus += new EventHandler(platformID_GotFocus);
			platformID.numChars = 1;
			platformID.CharIsShortFor('1','X');
			platformID.CharIsShortFor('2','Y');
			platformID.CharIsShortFor('3','Z');
			platformID.CharIsShortFor('x','X');
			platformID.CharIsShortFor('y','Y');
			platformID.CharIsShortFor('z','Z');
			this.Controls.Add(platformID);
		
			Label product_label = new Label();
			product_label.Text = "Product";
			product_label.Size = new Size(90,20);
			product_label.Font = MyDefaultSkinFontBold12;
			product_label.Location = new Point(225-45-180+100,58);
			this.Controls.Add(product_label);

			productNumber = new EntryBox();
			productNumber.TextAlign = HorizontalAlignment.Center;
			productNumber.Location = new Point(225-45-180+100,75+5);
			productNumber.Size = new Size(80,30);
			productNumber.Font = MyDefaultSkinFontNormal12;
			productNumber.GotFocus += new EventHandler(productNumber_GotFocus);
			productNumber.DigitsOnly = true;
			productNumber.AcceptsReturn = false;
			productNumber.KeyPress += new KeyPressEventHandler(productNumber_KeyPress);
			this.Controls.Add(productNumber);

			okButton = new ImageTextButton(0);
			okButton.ButtonFont = this.MyDefaultSkinFontBold9;
			okButton.SetVariants(filename_short);
			okButton.Size = new Size(80,20);
			okButton.Location = new Point(350-180,160);
			okButton.SetButtonText("OK",
				System.Drawing.Color.Black,System.Drawing.Color.Black,
				System.Drawing.Color.Green,System.Drawing.Color.DarkGray);
			okButton.Click += new EventHandler(okButton_Click);
			okButton.Visible = true;
			this.Controls.Add(okButton);

			cancelButton = new ImageTextButton(0);
			cancelButton.ButtonFont = this.MyDefaultSkinFontBold9;
			cancelButton.SetVariants(filename_short);
			cancelButton.Size = new Size(80,20);
			cancelButton.Location = new Point(445-180,160);
			cancelButton.SetButtonText("Cancel",
				System.Drawing.Color.Black,System.Drawing.Color.Black,
				System.Drawing.Color.Green,System.Drawing.Color.DarkGray);
			cancelButton.Click += new EventHandler(cancelButton_Click);
			cancelButton.Visible = true;
			this.Controls.Add(cancelButton);
		}

	}

}
