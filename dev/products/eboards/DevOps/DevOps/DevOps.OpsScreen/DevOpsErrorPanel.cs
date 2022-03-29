using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

using CommonGUI;
using CoreUtils;
using LibCore;
using Network;
using ResizingUi.Button;

namespace DevOps.OpsScreen
{
	public class DevOpsErrorPanel : FlickerFreePanel
	{
		public delegate void PanelClosedHandler();
		public event PanelClosedHandler PanelClosed;

	    readonly TextBox errorList;
		Node errorNode;
	    readonly StyledDynamicButton ok;
	    readonly Label header;

        protected Font MyDefaultSkinFontBoldItalic10;
		protected Font MyDefaultSkinFontBold12;

	    readonly Timer timer;
		bool flashing;
		bool flashOn;
		double flashTimeLeft;
		double flashStateTimeLeft;
		double flashOnTime;
		double flashOffTime;

	    readonly int borderThickness;

		IWatermarker watermarker;

	    readonly Color panelBackgroundColour = Color.FromArgb(37, 37, 37);
	    readonly Color panelTextColour = Color.FromArgb(225, 232, 237);

	    readonly string initialTitle = "Error";

        public DevOpsErrorPanel(NodeTree nt, bool isTrainingGame)
		{
            timer = new Timer { Interval = 500 }; 
            timer.Tick += timer_Tick;

			var fontname = SkinningDefs.TheInstance.GetData("fontname");
			var style = FontStyle.Bold | FontStyle.Italic;
			if (SkinningDefs.TheInstance.GetIntData("error_panel_font_bold", 0) != 0)
			{
				style = FontStyle.Bold;
			}
			var size = SkinningDefs.TheInstance.GetIntData("error_panel_font_size", 10);
			MyDefaultSkinFontBoldItalic10 = ConstantSizeFont.NewFont(fontname, size, style);
			MyDefaultSkinFontBold12 = ConstantSizeFont.NewFont(fontname, 20, FontStyle.Bold);

			Visible = false;

			borderThickness = 5;

            header = new Label
                     {
                         Font = MyDefaultSkinFontBold12,
                         Text = initialTitle,
                         ForeColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("error_panel_title_colour", Color.White),
                         BackColor = Color.FromArgb(255, 43, 43),
                         Size = new Size(300, 30),
                         Location = new Point(10, SkinningDefs.TheInstance.GetIntData("error_panel_title_y", 20))
                     };
            Controls.Add(header);

            errorList = new ReadOnlyTextBoxNoCursor
                        {
                            Multiline = true,
                            ReadOnly = true,
                            Location = new Point(15, 95),
                            Size = new Size(487, 126),
                            Font = MyDefaultSkinFontBoldItalic10,
                            BorderStyle = BorderStyle.None,
                            ForeColor = panelTextColour,
                            BackColor = Color.FromArgb(255, 43, 43)
                        };
            Controls.Add(errorList);

            ok = new StyledDynamicButton("standard", "Ok")
                 {
                     Font = SkinningDefs.TheInstance.GetFontWithStyle("standard_popup_control_button_font"),
                     Location = new Point(400, 252),
                     Size = new Size(100, 30)
                 };
            
			ok.Click += ok_Click;
			Controls.Add(ok);

			errorNode = nt.GetNamedNode("FacilitatorNotifiedErrors");
			errorNode.ChildAdded += errorNode_ChildAdded;
            errorNode.ChildRemoved += errorNode_ChildRemoved;

            BackColor = panelBackgroundColour;

			flashing = false;
			flashOn = false;
			UpdateDisplay();

			DoSize();
		}

		public IWatermarker Watermarker
		{
			get => watermarker;

			set
			{
				watermarker = value;
				Invalidate();
			}
		}

		protected override void Dispose(bool disposing)
		{
			if(disposing)
			{
				if (errorNode != null)
				{
					errorNode.ChildAdded -= errorNode_ChildAdded;
					errorNode.ChildRemoved -= errorNode_ChildRemoved;
					errorNode = null;
				}
			}
			base.Dispose (disposing);
		}

		public void adjustTitlePostion(int x, int y)
		{
			header.Location = new Point(x, y);
			Refresh();
		}

		void errorNode_ChildAdded(Node sender, Node child)
		{
			RebuildErrorText();
			
			var soundName = child.GetAttribute("sound");
			if (soundName != "")
			{
				KlaxonSingleton.TheInstance.PlayAudioThenResume(AppInfo.TheInstance.Location + "\\audio\\" + soundName);
			}

		    Flash(child.GetBooleanAttribute("flash", false) ? 10 : 0);

		    // wait for a tick before make visible 
			// so can use other panel closing event to hide an error panel after it has been displayed
			// see bug #8395
			var makeVisibleTimer = new Timer();
			makeVisibleTimer.Tick += makeVisibleTimer_Tick;
			makeVisibleTimer.Interval = 1;
			makeVisibleTimer.Start();
		}

		void errorNode_ChildRemoved (Node sender, Node child)
		{
			RebuildErrorText();
		}

		void RebuildErrorText ()
		{
			var titles = new List<string> ();
			var titleBuilder = new StringBuilder ();
			var textBuilder = new StringBuilder ();
			foreach (Node error in errorNode.getChildren())
			{
				textBuilder.Append(error.GetAttribute("text"));

				var title = error.GetAttribute("title", null);
				if (!string.IsNullOrEmpty(title))
				{
					if (! titles.Contains(title))
					{
						titles.Add(title);
						titleBuilder.Append(title);
					}
				}
			}

			errorList.Text = textBuilder.ToString();

			header.Text = titles.Count > 0 ? titleBuilder.ToString() : initialTitle;

			if (textBuilder.Length == 0)
			{
				Close();
			}
		}

		void makeVisibleTimer_Tick(object sender, EventArgs e)
		{
			var timer = ((Timer)sender);
			timer.Stop();
			timer.Dispose();

			BringToFront();
			Visible = true;
			ok.Focus();
		}

		void ok_Click(object sender, EventArgs e)
		{
			Close();
		}

		public void Close()
		{
			errorList.Text = "";
			Visible = false;
			errorNode.DeleteChildren();

		    PanelClosed?.Invoke();
		}

		void timer_Tick (object sender, EventArgs e)
		{
			var dt = timer.Interval / 1000.0;

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

			    flashStateTimeLeft = flashOn ? flashOnTime : flashOffTime;
			}

			UpdateDisplay();
		}

		void UpdateDisplay ()
		{
		    errorList.BackColor = flashOn ? Color.FromArgb(0, 160, 0) : panelBackgroundColour;
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

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);
			DoSize();
		}

		void DoSize ()
		{
			var titleHeight = 50;
			header.Bounds = new Rectangle (0, 0, Width, titleHeight);

			var widthPadding = 5;
			var heightPadding = 5;
			ok.Location = new Point (Width - borderThickness - (2 * widthPadding) - ok.Width, Height - borderThickness - ok.Height - heightPadding - 2);

			var instep = 20;
			errorList.Bounds = new Rectangle (instep, header.Bottom + instep, Width - (2 * instep), ok.Top - instep - (header.Bottom + instep));

			Invalidate();
		}

		protected override void OnPaint (PaintEventArgs e)
		{
			base.OnPaint(e);

			using (var brush = new SolidBrush (Color.FromArgb(255, 43, 43)))
			{
				e.Graphics.FillRectangle(brush, new Rectangle (0, 0, Width, borderThickness));
				e.Graphics.FillRectangle(brush, new Rectangle (0, Height - borderThickness, Width, borderThickness));
				e.Graphics.FillRectangle(brush, new Rectangle (0, 0, borderThickness, Height));
				e.Graphics.FillRectangle(brush, new Rectangle (Width - borderThickness, 0, borderThickness, Height));
			}

			watermarker?.Draw(this, e.Graphics);
		}
	}
}