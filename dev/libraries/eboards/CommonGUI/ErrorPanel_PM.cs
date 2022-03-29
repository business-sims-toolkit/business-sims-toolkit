using System;
using System.Windows.Forms;
using System.Drawing;

using LibCore;
using Network;
using CoreUtils;

namespace CommonGUI
{
	/// <summary>
	/// Summary description for ErrorPanel.
	/// </summary>
	public class ErrorPanel_PM : BasePanel
	{
		public delegate void PanelClosedHandler();
		public event PanelClosedHandler PanelClosed;

		TextBox errorList;
		Node errorNode;
		ImageTextButton ok;
		Label header;

		//skin stuff
		protected Font MyDefaultSkinFontNormal10 = null;
		protected Font MyDefaultSkinFontBold9 = null;
		protected Font MyDefaultSkinFontBold12 = null;

		Timer timer;
		bool flashing;
		bool flashOn;
		double flashTimeLeft;
		double flashStateTimeLeft;
		double flashOnTime;
		double flashOffTime;

		string initialTitle = "Error";

		/// <summary>
		/// Shows an error panel to the facilitator
		/// </summary>
		/// <param name="nt">The NodeTree gamefile</param>
		public ErrorPanel_PM(NodeTree nt, Boolean isTrainingGame)
		{
			timer = new Timer ();
			timer.Interval = 500;
			timer.Tick += timer_Tick;

			string fontname = SkinningDefs.TheInstance.GetData("fontname");
			FontStyle style = FontStyle.Regular;
			if (SkinningDefs.TheInstance.GetIntData("error_panel_font_bold", 1) != 0)
			{
				style = FontStyle.Bold;
			}
			int size = SkinningDefs.TheInstance.GetIntData("error_panel_font_size", 10);
			MyDefaultSkinFontNormal10 = ConstantSizeFont.NewFont(fontname, size, style);
			MyDefaultSkinFontBold9 = ConstantSizeFont.NewFont(fontname,9,FontStyle.Bold);
			MyDefaultSkinFontBold12 = ConstantSizeFont.NewFont(fontname,12,FontStyle.Bold);

			if (isTrainingGame)
			{
				BackgroundImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location +
					"\\images\\panels\\PM_opsback2.PNG");
			}
			else
			{
				BackgroundImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location +
					"\\images\\panels\\PM_opsback2.PNG");
			}
			Visible = false;

			header = new Label();
			header.Font = MyDefaultSkinFontBold12;
			header.Text = initialTitle;
			header.ForeColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("error_panel_title_colour", Color.Black);
			header.BackColor = Color.Transparent;
			header.Size = new Size(300,25);
			header.Location = new Point(10,10);
			Controls.Add(header);	
			
			errorList = new TextBox();
			errorList.Multiline = true;
			errorList.ReadOnly = true;
			errorList.Location = new Point(10,40+30);
			errorList.Size = new Size(585,100);
			errorList.Font = MyDefaultSkinFontNormal10;

			Controls.Add(errorList);

			Color upColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_text_colour", Color.Black);
			Color downColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_down_text_colour", Color.White);

			ok = new ImageTextButton (0);
			ok.SetVariants("\\images\\buttons\\button_70x25.png");
			ok.Font = MyDefaultSkinFontBold9;
			ok.Location = new Point(290, 150 + 30);
			ok.SetButtonText("OK", upColor, upColor, downColor, upColor);
			ok.Size = new Size(70, 25);
			ok.Click += ok_Click;
			Controls.Add(ok);

			errorNode = nt.GetNamedNode("FacilitatorNotifiedErrors");
			errorNode.ChildAdded += errorNode_ChildAdded;

			Resize += ErrorPanel_Resize;

			flashing = false;
			flashOn = false;
			UpdateDisplay();
		}

		/// <summary>
		/// Dispose .....
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			if(disposing)
			{
				if (errorNode != null)
				{
					errorNode.ChildAdded -= errorNode_ChildAdded;
					errorNode = null;
				}
			}
			base.Dispose (disposing);
		}

		void errorNode_ChildAdded(Node sender, Node child)
		{
			errorList.Text = child.GetAttribute("text")+errorList.Text;

			child.Parent.DeleteChildTree(child);
			BringToFront();
			Visible = true;

			string soundName = child.GetAttribute("sound");
			if (soundName != "")
			{
				KlaxonSingleton.TheInstance.PlayAudio(AppInfo.TheInstance.Location + "\\audio\\" + soundName, false);
			}

			if (child.GetBooleanAttribute("flash", false))
			{
				Flash(10);
			}
			else
			{
				Flash(0);
			}

			string title = child.GetAttribute("title");
			if (title != "")
			{
				header.Text = title;
			}
			else
			{
				header.Text = initialTitle;
			}

			ok.Focus();
		}

		void ok_Click(object sender, EventArgs e)
		{
			errorList.Text = "";
			Visible = false;

			PanelClosed?.Invoke();
		}

		void ErrorPanel_Resize(object sender, EventArgs e)
		{
			ok.Location = new Point( (Width-ok.Width)/2, ok.Top );

			errorList.Width = Width - (2 * errorList.Left);
		}

		void timer_Tick (object sender, EventArgs e)
		{
			double dt = timer.Interval / 1000.0;

			flashTimeLeft -= dt;
			flashStateTimeLeft -= dt;
			if (flashTimeLeft <= 0)
			{
				flashing = false;
				flashOn = false;

				timer.Stop();
			}
			else if (flashStateTimeLeft <= 0)
			{
				flashOn = ! flashOn;

				if (flashOn)
				{
					flashStateTimeLeft = flashOnTime;
				}
				else
				{
					flashStateTimeLeft = flashOffTime;
				}
			}

			UpdateDisplay();
		}

		void UpdateDisplay ()
		{
			if (flashOn)
			{
				errorList.BackColor = Color.FromArgb(0, 160, 0);
			}
			else
			{
				errorList.BackColor = Color.White;
			}
		}

		void Flash (double seconds)
		{
			flashTimeLeft = seconds;
			flashStateTimeLeft = 0;

			flashOnTime = 0.25;
			flashOffTime = 0.25;

			if (seconds <= 0)
			{
				flashOn = false;
				flashing = false;
				timer.Stop();
				UpdateDisplay();
			}
			else if (! flashing)
			{
				flashOn = false;
				flashing = true;
				timer.Start();
			}
		}
	}
}
