using System;
using System.Drawing;
using System.Windows.Forms;
using GameManagement;

using LibCore;
using CommonGUI;

namespace maturity_check
{
	/// <summary>
	/// Summary description for OpenOrNewReportPanel.
	/// </summary>
	public class OpenOrNewReportPanel : OpenOrNewTemplatePanel
	{
		protected Label errorText;
		protected ImageButton load;
		protected ImageButton passwordOk;
		protected TextBox passwordBox;

		ImageBox background;

		protected MaturityInfoFile report_file;

		protected MaturityEditSessionPanel sessionPanel;

		public OpenOrNewReportPanel()
		{
			background = new ImageBox ();
			background.Image = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "/images/maturity_tool/CA MT Intro.png");
			background.Size = background.Image.Size;
			Controls.Add(background);

			passwordOk = new ImageButton (0);
			passwordOk.SetVariants("/images/maturity_tool/CA_MT_ok");
			passwordOk.SetAutoSize();
			passwordOk.Location = new Point (330 - passwordOk.Width, 160);
			passwordOk.ButtonPressed += passwordOk_ButtonPressed;
			background.Controls.Add(passwordOk);

			passwordBox = new TextBox();
			passwordBox.PasswordChar = '*';
			passwordBox.Size = new Size (100, 20);
			passwordBox.Location = new Point (100, (passwordOk.Top + ((passwordOk.Height - passwordBox.Height) / 2)));
			passwordBox.Width = passwordOk.Left - 10 - passwordBox.Left;
			passwordBox.TextChanged += passwordBox_TextChanged;
			background.Controls.Add(passwordBox);

			errorText = new Label ();
			errorText.Location = new Point (passwordBox.Left, passwordBox.Bottom + 10);
			errorText.Size = new Size (500, 20);
			errorText.ForeColor = Color.Red;
			background.Controls.Add(errorText);

			load = new ImageButton (0);
			load.SetVariants("/images/maturity_tool/CA_MT_load");
			load.SetAutoSize();
			load.Location = new Point (passwordOk.Right - load.Width, 270);
			background.Controls.Add(load);
			load.ButtonPressed += load_ButtonPressed;

			this.Resize += OpenOrNewReportPanel_Resize;

			this.ClientSize = background.Size;
		}

		void load_ButtonPressed (object sender, ImageButtonEventArgs args)
		{
			OpenFileDialog dialog = new OpenFileDialog();
			dialog.Filter = "Maturity Reports (*.mrp)|*.mrp";
			if (dialog.ShowDialog(TopLevelControl) == DialogResult.OK)
			{
				LoadFile(dialog.FileName);
			}
		}

		void passwordOk_ButtonPressed (object sender, ImageButtonEventArgs args)
		{
			//
			// TODO : Check the real license password in the full app.
			//
			if (passwordBox.Text != "")
			{
				errorText.Visible = false;

				SaveFileDialog dialog = new SaveFileDialog();
				//dialog.Filter = "Maturity XML reports (*.XML)|*.XML";
				dialog.Title = "Please provide a filename for your Maturity Report";
				dialog.Filter = "Maturity Reports (*.mrp)|*.mrp";
				if (dialog.ShowDialog(TopLevelControl) == DialogResult.OK)
				{
					report_file = new MaturityInfoFile(dialog.FileName, true, true, true);
					//string rfile = report_file.GetFile("eval.xml");
					OpenSession(report_file, true);
				}
			}
			else
			{
				errorText.Text = "Incorrect Password";
				errorText.Visible = true;
			}
		}

		public override void SaveToXML()
		{
			if(sessionPanel != null)
			{
				sessionPanel.SaveToXML();
				report_file.Save(true);
			}
		}

		void OpenOrNewReportPanel_Resize(object sender, EventArgs e)
		{
			DoSize();
		}

		void DoSize ()
		{
			background.Location = new Point ((ClientSize.Width - background.Width) / 2, (ClientSize.Height - background.Height) / 2);

			if (sessionPanel != null)
			{
				sessionPanel.Location = new Point (0, 0);
				sessionPanel.Size = this.ClientSize;
			}
		}

		void passwordBox_TextChanged (object sender, EventArgs e)
		{
			errorText.Visible = false;
		}

		void OpenSession (MaturityInfoFile _report_file, bool is_new)
		{
			sessionPanel = new MaturityEditSessionPanel (_report_file, this, is_new, true);
			sessionPanel.Location = new Point (0, 0);
			sessionPanel.Size = this.ClientSize;
			this.Controls.Add(sessionPanel);
			sessionPanel.BringToFront();
			sessionPanel.CloseSession += sessionPanel_CloseSession;
			sessionPanel.Disposed += sessionPanel_Disposed;
		}

		void sessionPanel_CloseSession (object sender, EventArgs e)
		{
			errorText.Hide();
			passwordBox.Clear();
		}

		void sessionPanel_Disposed (object sender, EventArgs e)
		{
			sessionPanel = null;
			report_file = null;
		}
		#region ICustomerInfo Members

		public override void Cancelled()
		{
			this.Controls.Remove(sessionPanel);
			sessionPanel = null;
			report_file = null;
		}

		public override void Accepted()
		{
		}

		public override void SaveCustomerDetails ()
		{
		}

		#endregion

		public void Done ()
		{
			if (sessionPanel != null)
			{
				sessionPanel.Done();
			}
		}

		public void LoadFile (string filename)
		{
			report_file = new MaturityInfoFile(filename);
			// assumes it's a good file for now.
			//string rfile = report_file.GetFile("eval.xml");
			OpenSession(report_file, false);
		}
	}
}