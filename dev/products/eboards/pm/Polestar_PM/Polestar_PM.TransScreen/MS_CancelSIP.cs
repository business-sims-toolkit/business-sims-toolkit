using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;

using LibCore;
using Network;
using IncidentManagement;

using CoreUtils;
using CommonGUI;
using TransitionScreens;

namespace Polestar_PM.TransScreen
{
	/// <summary>
	/// This is the Cancel SIP Operations Panel
	/// It allows the entire SIP to be cancelled or just the install day for that SIP
	/// 
	/// This needs to be refactored to use common methods from ProjectManager and ProjectRunner
	/// These Project Objects should contain all the important methods and status information 
	/// Currently we have business rules in 2 places which we need to rationalise
	/// </summary>
	public class MS_CancelSIP : CancelSIP
	{

		public MS_CancelSIP(TransitionControlPanel tcp, NodeTree nt, int current_round, Color OperationsBackColor)
		:base(tcp, nt, current_round, OperationsBackColor)
		{
		}

		public override void BuildScreenControls()
		{
			UseShortInstallMsg = true;

			CancelProductButtonWidth = 135;
			CancelProductButtonOffsetX = 5;
			CancelInstallButtonWidth = 215;
			CancelInstallButtonOffsetX = 145;
			ButtonGap = 5;

			Label header = new Label();
			header.Text = "Cancel Product";
			header.Size = new Size(135,25);
			header.Font = MyDefaultSkinFontBold12;
			header.Location = new Point(5,10);
			this.Controls.Add(header);

			Label header2 = new Label();
			header2.Text = "Cancel Install";
			header2.Size = new Size(160,25);
			header2.Font = MyDefaultSkinFontBold12;
			header2.Location = new Point(145,10);
			this.Controls.Add(header2);

			cancelButton = new ImageTextButton(0);
			cancelButton.ButtonFont = this.MyDefaultSkinFontBold9;
			cancelButton.SetVariants(filename_short);
			cancelButton.Size = new Size(80,20);
			cancelButton.Location = new Point(445-175-81+85,180);
			cancelButton.SetButtonText("Close",
				System.Drawing.Color.Black,System.Drawing.Color.Black,
				System.Drawing.Color.Green,System.Drawing.Color.DarkGray);
			cancelButton.Click += new EventHandler(cancelButton_Click);
			cancelButton.Visible = true;
			this.Controls.Add(cancelButton);
		}

		protected override void confirm(string str, object cancelobject)
		{
			int OffsetX = -100; 
			this.SuspendLayout();

			foreach(Control c in Controls)
			{
				if(c!=null) c.Dispose();
			}
			this.Controls.Clear();

			Label header = new Label();
			header.Text = "Confirm Cancel";
			header.Size = new Size(180,25);
			header.Font = this.MyDefaultSkinFontBold12;
			header.Location = new Point(10,10);
			this.Controls.Add(header);

			Label header2 = new Label();
			header2.Text = "Do you really want to "+str;
			header2.Size = new Size(350,15);
			header2.TextAlign = ContentAlignment.MiddleCenter;
			header2.Font = this.MyDefaultSkinFontNormal10;
			header2.Location = new Point(0,100);
			this.Controls.Add(header2);

			ImageTextButton okButton = new ImageTextButton(0);
			okButton.ButtonFont = this.MyDefaultSkinFontBold9;
			okButton.SetVariants(filename_short);
			okButton.Size = new Size(80,20);
			okButton.Location = new Point(180+OffsetX,120);
			okButton.Tag = cancelobject;
			okButton.SetButtonText("Yes",
				System.Drawing.Color.Black,System.Drawing.Color.Black,
				System.Drawing.Color.Green,System.Drawing.Color.DarkGray);
			okButton.Click += new EventHandler(ok_Click);
			okButton.Visible = true;
			this.Controls.Add(okButton);

			ImageTextButton cancelButton = new ImageTextButton(0);
			cancelButton.ButtonFont = this.MyDefaultSkinFontBold9;
			cancelButton.SetVariants(filename_short);
			cancelButton.Size = new Size(80,20);
			cancelButton.Location = new Point(270+OffsetX,120);
			cancelButton.SetButtonText("No",
				System.Drawing.Color.Black,System.Drawing.Color.Black,
				System.Drawing.Color.Green,System.Drawing.Color.DarkGray);
			cancelButton.Click += new EventHandler(dispose_Click);
			cancelButton.Visible = true;
			this.Controls.Add(cancelButton);

			ErrMsgDisplay = new Label();
			ErrMsgDisplay.ForeColor = Color.Red;
			ErrMsgDisplay.Text = "Cancel Product";
			ErrMsgDisplay.Size = new Size(350,15);
			ErrMsgDisplay.Font = this.MyDefaultSkinFontBold9;
			ErrMsgDisplay.Location = new Point(10,155);
			ErrMsgDisplay.TextAlign = ContentAlignment.MiddleCenter;
			ErrMsgDisplay.Visible = false;
			this.Controls.Add(ErrMsgDisplay);

			focusJumper.Dispose();
			focusJumper = new FocusJumper();
			focusJumper.Add(okButton);
			focusJumper.Add(cancelButton);

			this.ResumeLayout(false);

			cancelButton.Focus();
		}



	}
}
