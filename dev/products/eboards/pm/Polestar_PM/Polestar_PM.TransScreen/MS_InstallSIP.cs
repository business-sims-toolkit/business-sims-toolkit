using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;

using LibCore;
using Network;

using GameEngine;
using IncidentManagement;
using CoreUtils;
using CommonGUI;
using TransitionScreens;

namespace Polestar_PM.TransScreen
{
	/// <summary>
	/// Summary description for MS_InstallSIP.
	/// </summary>
	public class MS_InstallSIP : InstallSIP
	{

		public MS_InstallSIP(TransitionControlPanel tcp, NodeTree nt, int round, Color OperationsBackColor)
		:base(tcp, nt, round, OperationsBackColor)
		{
		}

		public override void BuildScreenControls()
		{
			int OffsetX = -80; 

			header = new Label();
			header.Text = "Install Product";
			header.Size = new Size(170,25);
			header.Font = MyDefaultSkinFontBold12;
			header.Location = new Point(10,10);
			this.Controls.Add(header);

			product_label = new Label();
			product_label.Text = "Product";
			product_label.Size = new Size(80,15);
			product_label.Location = new Point(90+OffsetX,55);
			product_label.GotFocus +=new EventHandler(product_label_GotFocus);
			product_label.Font = MyDefaultSkinFontBold10;
			this.Controls.Add(product_label);

			productNumber = new EntryBox();
			productNumber.TextAlign = HorizontalAlignment.Center;
			productNumber.Location = new Point(90+OffsetX,75);
			productNumber.Size = new Size(80,30);
			productNumber.Font = MyDefaultSkinFontNormal12;
			productNumber.numChars = 4;
			productNumber.KeyPress += new KeyPressEventHandler(productNumber_KeyPress);
			productNumber.KeyUp += new KeyEventHandler(productNumber_KeyUp);
			productNumber.GotFocus +=new EventHandler(productNumber_GotFocus);
			productNumber.DigitsOnly = true;
			this.Controls.Add(productNumber);

			location_title = new Label();
			location_title.Text = "Location";
			location_title.Size = new Size(80,15);
			location_title.Location = new Point(175+OffsetX,55);
			location_title.Font = MyDefaultSkinFontBold10;
			this.Controls.Add(location_title);

			locationBox = new EntryBox();
			locationBox.TextAlign = HorizontalAlignment.Center;
			locationBox.Location = new Point(175+OffsetX,75);
			locationBox.Size = new Size(80,30);
			locationBox.Font = MyDefaultSkinFontNormal12;
			locationBox.NextControl = dayBox;
			locationBox.numChars = 4;
			locationBox.DigitsOnly = false;
			locationBox.GotFocus +=new EventHandler(locationBox_GotFocus);
			this.Controls.Add(locationBox);

			Label day_label = new Label();
			day_label.Text = "Day";
			day_label.Size = new Size(60,15);
			day_label.Location = new Point(260+OffsetX,55);
			day_label.Font = MyDefaultSkinFontBold10;
			this.Controls.Add(day_label);

			dayBox = new EntryBox();
			dayBox.TextAlign = HorizontalAlignment.Center;
			dayBox.Location = new Point(260+OffsetX,75);
			dayBox.Size = new Size(80,30);
			dayBox.Font = MyDefaultSkinFontNormal12;
			dayBox.numChars = 2;
			dayBox.DigitsOnly = true;
			dayBox.GotFocus +=new EventHandler(dayBox_GotFocus);
			this.Controls.Add(dayBox);

			Label sla_label = new Label();
			sla_label.Text = "MTRS";
			sla_label.Size = new Size(50,15);
			sla_label.Location = new Point(345+OffsetX,55);
			sla_label.Font =  MyDefaultSkinFontBold10;
			this.Controls.Add(sla_label);

			slaNumber = new EntryBox();
			slaNumber.TextAlign = HorizontalAlignment.Center;
			slaNumber.Location = new Point(345+OffsetX,75);
			slaNumber.KeyPress += new KeyPressEventHandler(slaNumber_KeyPress);
			slaNumber.Size = new Size(80,30);
			slaNumber.Font = MyDefaultSkinFontNormal12;
			slaNumber.DigitsOnly = true;
			slaNumber.MouseUp += new MouseEventHandler(slaNumber_MouseUp);
			slaNumber.GotFocus +=new EventHandler(slaNumber_GotFocus);
			this.Controls.Add(slaNumber);

			errorDisplayText = new Label();
			errorDisplayText.Text = "";
			errorDisplayText.Size = new Size(360,35);
			errorDisplayText.Font = MyDefaultSkinFontBold10;
			errorDisplayText.TextAlign = ContentAlignment.MiddleCenter;
			errorDisplayText.ForeColor = Color.Red;
			errorDisplayText.Location = new Point(10,110);
			errorDisplayText.Visible = false;
			this.Controls.Add(errorDisplayText);

			dayBox.PrevControl = locationBox;

			okButton = new ImageTextButton(0);
			okButton.ButtonFont = this.MyDefaultSkinFontBold9;
			okButton.SetVariants(filename_short);
			okButton.Size = new Size(80,20);
			okButton.Location = new Point(350-165,160);
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
			cancelButton.Location = new Point(445-165,160);
			cancelButton.SetButtonText("Cancel",
				System.Drawing.Color.Black,System.Drawing.Color.Black,
				System.Drawing.Color.Green,System.Drawing.Color.DarkGray);
			cancelButton.Click += new EventHandler(cancelButton_Click);
			cancelButton.Visible = true;
			this.Controls.Add(cancelButton);
		}
		
	}
}
