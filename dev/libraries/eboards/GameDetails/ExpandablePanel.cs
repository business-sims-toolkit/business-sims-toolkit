using System;
using System.Drawing;
using System.Windows.Forms;
using LibCore;
using CoreUtils;
using CommonGUI;

using BaseUtils;

namespace GameDetails
{
	public class ExpandablePanel : System.Windows.Forms.UserControl
	{
		protected Control panelHeader;
		protected TabBar panelTabBar;

		protected Panel panel;
		protected bool expanded;
		protected int _width = 100;
		protected int _height = 100;

		protected Label title;

		ImageButton btnExpandDetails;

		protected Font MyDefaultSkinFontBold12 = null;

		bool drawUsingImages;

		public Panel ThePanel
		{
			get
			{
				return panel;
			}
		}

		public ExpandablePanel()
		{
			string fontname =  SkinningDefs.TheInstance.GetData("fontname");
			MyDefaultSkinFontBold12 = ConstantSizeFont.NewFont(fontname,12,FontStyle.Bold);

			Color normalColor =  SkinningDefs.TheInstance.GetColorData("generated_normal_tab_color");
			Color darkColor =  SkinningDefs.TheInstance.GetColorData("generated_dark_tab_color");
			Color tintColor =  SkinningDefs.TheInstance.GetColorDataGivenDefault("generated_tint_tab_color", Color.White);
			Color textColor =  SkinningDefs.TheInstance.GetColorData("generated_tab_text_normal_color");

			drawUsingImages = SkinningDefs.TheInstance.GetBoolData("generated_draw_tabs_with_images", false);
			if (drawUsingImages)
			{
				panelHeader = new ImageBox () { ImageLocation = LibCore.AppInfo.TheInstance.Location + @"\images\tabs\generated_tab.png" };
				((ImageBox) panelHeader).SetAutoSize();
			}
			else
			{
				panelHeader = new GradientPanel (normalColor, darkColor, tintColor);
			    ((GradientPanel) panelHeader).SetAutoSize();
			}

			panelHeader.Location = new Point(0, 0);
			this.Controls.Add(panelHeader);

			panel = new Panel();
			this.Controls.Add(panel);

			btnExpandDetails = new ImageButton(0);
			btnExpandDetails.ButtonPressed += btnExpandDetails_ButtonPressed;
			btnExpandDetails.Visible = true;
			btnExpandDetails.Name = "Expand Details Button";

			if (SkinningDefs.TheInstance.GetBoolData("generated_draw_buttons_in_code", false))
			{
				btnExpandDetails.Paint += btnExpandDetails_Paint;
				btnExpandDetails.Size = new Size (SkinningDefs.TheInstance.GetIntData("generated_draw_buttons_width", 10), SkinningDefs.TheInstance.GetIntData("generated_draw_buttons_height", 10));
			}
			panelHeader.Controls.Add(btnExpandDetails);

			title = new Label();
			title.Font = MyDefaultSkinFontBold12;
			title.ForeColor = textColor;
			title.TextAlign = ContentAlignment.MiddleLeft;
			panelHeader.Controls.Add(title);

		    Expanded = false;
		}

		protected override void Dispose(bool disposing)
		{
			if(disposing)
			{
				panelHeader.Dispose();
				panel.Dispose();
			}

			base.Dispose (disposing);
		}

		public string Title
		{
			get { return title.Text; }
			set { title.Text = value; }
		}

		public Panel Panel
		{
			get
			{
				return panel;
			}
		}

		public bool Collapsible
		{
			set
			{
				if(value)
				{
					btnExpandDetails.Visible = true;
				}
				else
				{
					btnExpandDetails.Visible = false;
					Expanded = true;
				}
			}
		}

		public bool Expanded
		{
			get
			{
				return expanded;
			}

		    set
		    {
		        expanded = value;

		        if (expanded)
		        {
		            Size = new Size(_width, _height + panelHeader.Height + SkinningDefs.TheInstance.GetIntData("generated_tab_extra_gap", 0));
		        }
		        else
		        {
		            Size = new Size(_width, panelHeader.Height);
		        }
		    }
		}

		public void SetSize(int width, int height)
		{
			_width = width;
		    _height = height;

		    Expanded = Expanded;
		}

		public Size GetSize ()
		{
			return new Size (_width, _height);
		}

		protected void DoSize()
		{
			if (expanded)
			{
				if (SkinningDefs.TheInstance.GetBoolData("generated_draw_buttons_in_code", false))
				{
					btnExpandDetails.Invalidate();
				}
				else
				{
					btnExpandDetails.SetVariants("/images/buttons/nav_up.png");
				}
			}
			else
			{
				if (SkinningDefs.TheInstance.GetBoolData("generated_draw_buttons_in_code", false))
				{
					btnExpandDetails.Invalidate();
				}
				else
				{
					btnExpandDetails.SetVariants("/images/buttons/nav_down.png");
				}
			}

		    btnExpandDetails.SetAutoSize();

		    panelHeader.Width = Width;
		    panel.Bounds = new Rectangle (0, panelHeader.Bottom + SkinningDefs.TheInstance.GetIntData("generated_tab_extra_gap", 0), Width, _height);
		    btnExpandDetails.Location = new System.Drawing.Point(Width - 20 - btnExpandDetails.Width, (panelHeader.Height - btnExpandDetails.Height) / 2);

		    if (title != null)
		    {
		        title.Bounds = new Rectangle (20, panelHeader.Top, Width, panelHeader.Height);
                title.TextAlign = ContentAlignment.MiddleLeft;
		    }
		}

        protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);
		    DoSize();
		}

		void btnExpandDetails_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			ClickHandler();
		}

		void ClickHandler()
		{
			Expanded = ! Expanded;
		}

		void btnExpandDetails_Click(object sender, EventArgs e)
		{
			ClickHandler();
		}

		void btnExpandDetails_Paint (object sender, PaintEventArgs args)
		{
			ImageButton button = (ImageButton) sender;

			args.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

			if (expanded)
			{
				using (Brush brush = new SolidBrush (SkinningDefs.TheInstance.GetColorData("generated_draw_buttons_expanded_colour")))
				{
					args.Graphics.FillPolygon(brush, new [] { new Point(button.Width / 2, 0), new Point(0, button.Height - 1), new Point(button.Width - 1, button.Height - 1) });
				}
			}
			else
			{
				using (Brush brush = new SolidBrush (SkinningDefs.TheInstance.GetColorData("generated_draw_buttons_collapsed_colour")))
				{
					args.Graphics.FillPolygon(brush, new [] { new Point (0, 0), new Point (button.Width - 1, 0), new Point (button.Width / 2, button.Height - 1) });
				}
			}
		}

		protected override void OnGotFocus (EventArgs e)
		{
			base.OnGotFocus(e);

			if (panel != null)
			{
				panel.Focus();
				panel.Select();
			}
		}
	}
}