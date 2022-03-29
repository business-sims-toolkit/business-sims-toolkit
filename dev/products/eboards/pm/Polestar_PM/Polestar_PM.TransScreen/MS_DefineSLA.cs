using System;
using System.Collections;
using System.Drawing;
using System.IO;
using System.Data;
using System.Windows.Forms;

using CommonGUI;
using LibCore;
using Network;
using CoreUtils;
using TransitionScreens;

namespace Polestar_PM.TransScreen
{
	/// <summary>
	/// Summary description for MS_DefineSLA.
	/// </summary>
	public class MS_DefineSLA : DefineSLA
	{

		/// <summary>
		/// Set the service level agreement for projects 
		/// </summary>
		/// <param name="tcp"></param>
		/// <param name="tree"></param>
		public MS_DefineSLA(TransitionControlPanel tcp, NodeTree tree, Color OperationsBackColor, int round)
		: base (tcp, tree, OperationsBackColor, round)
		{
		}

		public override void BuildScreenControls()
		{
			//No changes for the base layout numbers 
			smi_columns = 3;
			smi_width = 116;
			smi_height = 30;
			smi_textEntryOffset = 98;

			header = new Label();
			header.Text = "Set MTRS for Services";
			header.Size = new Size(300,25);
			header.Font = MyDefaultSkinFontBold12;
			header.Location = new Point(5,5);
			this.Controls.Add(header);

			cancelButton = new ImageTextButton(0);
			cancelButton.ButtonFont = this.MyDefaultSkinFontBold9;
			cancelButton.SetVariants(filename_short);
			cancelButton.Size = new Size(80,20);
			cancelButton.Location = new Point(445-175-81+85,5+200+50);
			cancelButton.SetButtonText("Close",
				System.Drawing.Color.Black,System.Drawing.Color.Black,
				System.Drawing.Color.Green,System.Drawing.Color.DarkGray);
			cancelButton.Click += new EventHandler(cancelButton_Click);
			cancelButton.Visible = true;
			this.Controls.Add(cancelButton);
		}



	}
}
