using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CoreUtils;
using LibCore;

namespace CommonGUI
{
	
	public class cTabPressed
	{
		public delegate void TabEventArgsHandler(Control sender);
	}

	public interface iTabPressed
	{
		event cTabPressed.TabEventArgsHandler tabPressed;
	}

	public class ImageTextButton : ImageButton
	{
		protected string buttonText = "";

		protected Color upColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("image_button_text_colour", Color.White);
		protected Color downColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("image_button_active_text_colour", Color.Black);
		protected Color hoverColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("image_button_hover_text_colour", Color.Green);
		protected Color disabledColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("image_button_disabled_text_colour", Color.Gray);
        protected Color activeDisabledColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("image_button_active_disabled_text_colour", Color.Gray);
        protected Color focusColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("image_button_focus_text_colour", Color.White);
        
		protected Font buttonFont = SkinningDefs.TheInstance.GetFont(9, FontStyle.Bold);

	    bool overrideColours;

		public Font ButtonFont
		{
			get { return buttonFont; }
			set { buttonFont = value; Invalidate(); }
		}

		public string GetButtonText()
		{
			return buttonText;
		}

		public void SetButtonText(string text)
		{
			buttonText = text;
			Invalidate();
		}

		int margin = 0;
		public new int Margin
		{
			get
			{
				return margin;
			}

			set
			{
				margin = value;
				Invalidate();
			}
		}

	    ContentAlignment textAlign = ContentAlignment.MiddleCenter;
        public ContentAlignment TextAlign
        {
            get
            {
                return textAlign;
            }
            set
            {
                textAlign = value;
                Invalidate();
            }
        }

		public void SetButtonText (string text, Color _upColor, Color _downColor, Color _hoverColor, Color _disabledColor)
		{
			SetButtonText(text, _upColor, _downColor, _hoverColor, _disabledColor, _disabledColor);
		}

		public void SetButtonText(string text, Color _upColor, Color _downColor, Color _hoverColor, Color _disabledColor, Color activeDisabledColor)
		{
			buttonText = text;
			upColor = _upColor;
			downColor = _downColor;
			HoverTextColour = _hoverColor;
			FocussedTextColour = _upColor;
			disabledColor = _disabledColor;
			this.activeDisabledColor = activeDisabledColor;

		    if (overrideColours)
		    {
		        upColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_normal_text_colour", upColor);
		        downColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_active_text_colour", downColor);
		        disabledColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_disabled_text_colour", disabledColor);
		        FocussedTextColour = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_focussed_text_colour", FocussedTextColour);
		        HoverTextColour = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_hover_text_colour", HoverTextColour);
		    }

            Invalidate();
		}

		public void SetFocusColours(Color colour1, Color colour2)
		{
			HoverTextColour = colour1;
			FocussedTextColour = colour2;
		}

		public void SetFocusColours(Color colour1)
		{
			SetFocusColours(colour1, colour1);
		}

		public ImageTextButton (int code)
			: this (code, true)
		{
		}

		public ImageTextButton (int code, bool flickerFree)
			: base (code, flickerFree)
		{
		    overrideColours = SkinningDefs.TheInstance.GetBoolData("buttons_override_colours", false);

			MouseMove += ImageTextButton_MouseMove;		
		}

		public ImageTextButton (string fileBase, bool flickerFree)
			: this (0, flickerFree)
		{
			SetVariants(fileBase);
		}

		public ImageTextButton (string fileBase)
			: this (fileBase, true)
		{
		}

		protected override void OnKeyPress(KeyPressEventArgs e)
		{
			if(Enabled)
			{
				if(!IsButtonPressed)
				{
					if(e.KeyChar == 13)
					{
						OnClick( new EventArgs() );
					}
				}
				else
				{
					base.OnKeyPress(e);
				}						
			}
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			if (backgroundFill != null)
			{
				backgroundFill.Draw(this, e.Graphics);
			}
            
			Graphics g = e.Graphics;
			g.SmoothingMode = SmoothingMode.AntiAlias;
			StringFormat sf = new StringFormat(StringFormatFlags.MeasureTrailingSpaces);

		    sf.LineAlignment = Alignment.GetVerticalAlignment(textAlign);
		    sf.Alignment = Alignment.GetHorizontalAlignment(textAlign);
            
			SizeF textsize = g.MeasureString(buttonText,buttonFont,Width - (2 * margin),sf);

			bool gotFocus = Focused;

			Color colour = upColor;

			Image i = up;
			if(gotFocus && (focus!=null))
			{
				i = focus;
				colour = focusColor;
			}
            
			if(!Enabled)
			{
				if (Active)
				{
					i = activeDisabled;
					colour = activeDisabledColor;
				}
				else
				{
					i = disabled;
					colour = disabledColor;
				}
			}
			else if(((mouseHover && mouseDown) || Active) && (null != down))
			{
				i = down;
				colour = downColor;
			}
			else if(mouseHover && (null != hover))
			{
				i = hover;
				colour = hoverColor;
			}
			//
			if(null != i)
			{
				Rectangle src = new Rectangle(0,0,i.Width,i.Height);
				e.Graphics.DrawImage(i,ClientRectangle,src,GraphicsUnit.Pixel);
			}
			//
			if("" != buttonText)
			{
			    float verticalOffset = (Height - textsize.Height) / 2f;
				
			    float horizontalOffset = margin;
				using (Brush brush = new SolidBrush (colour))
				{
				    RectangleF textRect = new RectangleF(new PointF(horizontalOffset, verticalOffset),
				        new SizeF(Width - (2 * horizontalOffset), Height - (2 * verticalOffset)));
                    

				    g.DrawString(buttonText, buttonFont, brush,textRect, sf);

                    
				}
			}
		}	
		
		protected void ImageTextButton_MouseMove(object sender, MouseEventArgs e)
		{
			// Check to see if the mouse is over the button or not.
			if( (e.X < 0) || (e.Y < 0) )
			{
				if(mouseHover)
				{
					mouseHover = false;
					Invalidate();
				}
			}
			else if( (e.X > Width) || (e.Y > Height) )
			{
				if(mouseHover)
				{
					mouseHover = false;
					Invalidate();
				}
			}
			else
			{
				if(!mouseHover)
				{
					mouseHover = true;
					Invalidate();
				}
			}
		}

		public Color HoverTextColour
		{
			get
			{
				return hoverColor;
			}

			set
			{
				hoverColor = value;
				Invalidate();
			}
		}

		public Color FocussedTextColour
		{
			get
			{
				return focusColor;
			}

			set
			{
				focusColor = value;
				Invalidate();
			}
		}

		public override string Text
		{
			get
			{
				return buttonText;
			}

			set
			{
				SetButtonText(value);
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
				return new Size (50, 30);
			}
		}

	    protected override void OnGotFocus (EventArgs e)
	    {
	        base.OnGotFocus(e);

            Invalidate();
	    }

		public static ImageTextButton CreateButton (string text, string image = "blank_small.png")
		{
			var button = new ImageTextButton (@"\images\buttons\" + image, true);
			button.SetButtonText(text, SkinningDefs.TheInstance.GetColorDataGivenDefault("button_normal_text_colour", Color.White),
				SkinningDefs.TheInstance.GetColorDataGivenDefault("button_active_text_colour", Color.White),
				SkinningDefs.TheInstance.GetColorDataGivenDefault("button_hover_text_colour", Color.White),
				SkinningDefs.TheInstance.GetColorDataGivenDefault("button_disabled_text_colour", Color.White),
				SkinningDefs.TheInstance.GetColorDataGivenDefault("button_disabled_text_colour", Color.White));

			return button;
		}

		public void SetForeColour (Color colour)
		{
			upColor = colour;
			hoverColor = colour;
		}
	}
}