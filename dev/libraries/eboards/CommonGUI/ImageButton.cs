using System;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using LibCore;

namespace CommonGUI
{
	public class ImageButtonEventArgs : EventArgs
	{
		protected int code;

		public ImageButtonEventArgs(int buttonCode)
		{
			code = buttonCode;
		}

		public int Code
		{
			get { return code; }
		}
	}

	public class ImageButton : FlickerFreePanel, iTabPressed
	{
		protected bool active = false;
		public bool Active
		{
			get => active;

		    set
			{
				active = value;
				Invalidate();
			}
		}
		

        protected int m_code = 0;
		protected Image up = null;
		protected Image down = null;
		protected Image hover = null;
		protected Image disabled = null;
		protected Image focus = null;
		protected Image activeDisabled = null;
		protected ToolTip toolTip2;

		protected bool mouseHover = false;
		protected bool mouseDown = false;

		string imageFileStem;

		Color? iconModulateColour;

		public static string DefaultDisabledFilename = "blank.png";

		public delegate void ImageButtonEventArgsHandler(object sender, ImageButtonEventArgs args);
		public event ImageButtonEventArgsHandler ButtonPressed;

		public bool IsButtonPressed
		{
			get
			{	if(ButtonPressed!=null)
					return true;
				else if(ButtonPressed==null)
					return false;
				else
					return false;
			}
	
		}

		public Image ActiveImage
		{
			get
			{
				return down;
			}

			set
			{
				down = value;
			}
		}

		public string ImageFileStem
		{
			get
			{
				return imageFileStem;
			}
		}

		protected override bool ProcessDialogKey(Keys keyData)
		{
			// If we are a tab....
			if(keyData == Keys.Tab)
			{
				if(null != tabPressed)
				{
					tabPressed(this);
					return true;
				}
			}

			return base.ProcessDialogKey(keyData);
		}

		public void SetTransparent()
		{
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			BackColor = Color.Transparent;
		}

		public int Code
		{
			get { return m_code; }
			set { m_code = value; }
		}

		// Set all variants of a button to a single image.
		public bool SetButton (string filebase)
		{
			Image i = Repository.TheInstance.GetImage(filebase);

			up = i;
			down = i;
			hover = i;
			focus = i;
			disabled = i;
			activeDisabled = i;

			if (i == null)
			{
				return false;
			}

			return (i != null);
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);

			Invalidate();
		}

		// Given a base filename (lacking a dot extension), load the normal, active, hover, highlighted and
		// disabled versions from standard suffices, using reasonable defaults for missing ones.
		public virtual bool SetVariants (string filebase)
		{
			int dotIndex = filebase.LastIndexOf(".");
			if (dotIndex < 0)
			{
				dotIndex = filebase.Length;
			}

			string filestem = AppInfo.TheInstance.Location + "\\" + filebase.Substring(0, dotIndex);
			imageFileStem = filebase;

			Image up = Repository.TheInstance.GetImage(filestem + ".png");
			Image down = Repository.TheInstance.GetImage(filestem + "_active.png"); 
			Image hover = Repository.TheInstance.GetImage(filestem + "_hover.png");
			Image highlight = Repository.TheInstance.GetImage(filestem + "_focus.png");
			Image disabled = Repository.TheInstance.GetImage(filestem + "_disabled.png");
			Image activeDisabled = Repository.TheInstance.GetImage(filestem + "_active_disabled.png");

			if (up == null)
			{
				return false;
			}

			if (down == null)
			{
				down = up;
			}

			if (hover == null)
			{
				hover = up;
			}

			if (highlight == null)
			{
				highlight = up;
			}

			if (disabled == null)
			{
				disabled = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\buttons\\disabled.png");
			}

			if (activeDisabled == null)
			{
				activeDisabled = disabled;
			}

			this.up = up;
			this.down = down;
			this.hover = hover;
			this.disabled = disabled;
			focus = highlight;
			this.activeDisabled = activeDisabled;

			return true;
		}

		public static ImageButton FromAutoVariants (int code, string filebase)
		{
			ImageButton b = new ImageButton(code);

			if (! b.SetVariants(filebase))
			{
				b.Dispose();
				b = null;
			}

			return b;
		}

		public ImageButton (int code)
			: this (code, true)
		{
		}

		public ImageButton (int code, bool flickerFree)
			: base (flickerFree)
		{
			antiAlias = true;

			SetStyle(ControlStyles.Selectable, true);

			m_code = code;
			SetTransparent();
			MouseEnter += ImageButton_MouseEnter;
			MouseLeave += ImageButton_MouseLeave;
			MouseDown += ImageButton_MouseDown;
			MouseUp += ImageButton_MouseUp;

			toolTip2 = new ToolTip();
			//
			GotFocus += ImageButton_GotFocus;
			LostFocus += ImageButton_LostFocus;

			EnabledChanged += ImageButton_EnabledChanged;
		}

		public string SetToolTipText
		{
			set { toolTip2.SetToolTip(this,value); }
		}

		protected virtual Image GetImageForState ()
		{
			var image = up;

			var gotFocus = Focused;

			if (gotFocus && (focus != null))
			{
				image = focus;
			}

			if (!Enabled && (null != disabled))
			{
				image = Active ? activeDisabled : disabled;
			}
			else if (mouseHover && mouseDown && (null != down) || Active)
			{
				image = down;
			}
			else if (mouseHover && (null != hover))
			{
				image = hover;
			}

			return image;
		}

		protected override void OnPaint(PaintEventArgs e)
		{
		    base.OnPaint(e);

			var i = GetImageForState();

			backgroundFill?.Draw(this, e.Graphics);

			if (antiAlias)
			{
				e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
				e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
			}

			
			//
			if(null != i)
			{
				Rectangle src = new Rectangle(0,0,i.Width,i.Height);

				if (iconModulateColour.HasValue)
				{
					ImageAttributes transformAttributes = new ImageAttributes ();
					transformAttributes.SetColorMatrix(Modulate(iconModulateColour.Value));
					e.Graphics.DrawImage(i, ClientRectangle, src.X, src.Y, src.Width, src.Height, GraphicsUnit.Pixel, transformAttributes);
				}
				else
				{
                    e.Graphics.DrawImage(i, ClientRectangle, src, GraphicsUnit.Pixel);
                }
			}
		}

		bool antiAlias;

		public bool Antialias
		{
			get
			{
				return antiAlias;
			}

			set
			{
				antiAlias = value;
				Invalidate();
			}
		}

		ColorMatrix Modulate (Color colour)
		{
			return new ColorMatrix (new float [] []
			                        {
										new float [] { colour.R / 255.0f, 0, 0, 0, 0 },
										new float [] { 0, colour.G / 255.0f, 0, 0, 0 },
										new float [] { 0, 0, colour.B / 255.0f, 0, 0 },
										new float [] { 0, 0, 0, colour.A / 255.0f, 0 },
										new float [] { 0, 0, 0, 0, 1 },
									});
		}

		protected virtual void ImageButton_MouseEnter(object sender, EventArgs e)
		{
			mouseHover = true;
			Invalidate();
		}

		protected virtual void ImageButton_MouseLeave(object sender, EventArgs e)
		{
			mouseHover = false;
			Invalidate();
		}

		protected virtual void ImageButton_MouseDown(object sender, MouseEventArgs e)
		{
			mouseDown = true;
			Invalidate();
		}

        protected virtual void ImageButton_MouseUp(object sender, MouseEventArgs e)
		{
			if(Enabled)
			{
				if (mouseDown && mouseHover)
				{
				    ButtonPressed?.Invoke(this, new ImageButtonEventArgs (m_code));
				}

				
			}

		    mouseDown = false;
		    Invalidate();
        }
	
		protected override void OnKeyPress(KeyPressEventArgs e)
		{
			if(Enabled)
			{
				// If enter is pressed then fire the butotn pressed event.
				if(ButtonPressed != null)
				{
					if(e.KeyChar == 13)
					{
						ButtonPressed(this, new ImageButtonEventArgs(Code) );
					}
				}
				base.OnKeyPress (e);
			}
		}

		#region iTabPressed Members

		public event cTabPressed.TabEventArgsHandler tabPressed;

		#endregion

		void ImageButton_GotFocus(object sender, EventArgs e)
		{
			if(null != focus)
			{
				Invalidate();
			}
		}

		void ImageButton_LostFocus(object sender, EventArgs e)
		{
			if(null != focus)
			{
				Invalidate();
			}
		}

		void ImageButton_EnabledChanged(object sender, EventArgs e)
		{
			Invalidate();
		}
	
		protected override void Dispose(bool disposing)
		{
			if(disposing)
			{
				// Drop images so that we don't get tied to the Image Singleton.
				up = null;
				down = null;
				hover = null;
				disabled = null;
				focus = null;
			}
			base.Dispose (disposing);
		}

		public void SetAutoSize ()
		{
			if (up != null)
			{
				Size = up.Size;
			}
		}

		public override Size GetPreferredSize (Size proposedSize)
		{
			if (up != null)
			{
				return up.Size;
			}
			else
			{
				return new Size (10, 5);
			}
		}

		public Color? IconModulateColour
		{
			get
			{
				return iconModulateColour;
			}

			set
			{
				iconModulateColour = value;
				Invalidate();
			}
		}

		public void PressButton ()
		{
			OnClick(EventArgs.Empty);
			ButtonPressed?.Invoke(this, new ImageButtonEventArgs (Code));
		}
	}
}