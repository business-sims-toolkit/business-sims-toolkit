using System;
using System.Drawing;
using LibCore;
using CoreUtils;

namespace CommonGUI
{
	/// <summary>
	/// An ImageButton with no active form, because it changes its entire appearance, including its normal,
	/// focussed, hover and disabled variants, when selected (eg one which flips from saying "show" to "hide").
	/// </summary>
	public class ImageToggleButton : ImageButton
	{
		protected string [] filebase = new string [2];
		protected string [] displaytext = new string [2];
		string disabledFilename;
		string hoverFilename;
		protected int state = 0;
		protected Font buttonFont = SkinningDefs.TheInstance.GetFont(SkinningDefs.TheInstance.GetFloatData("toggle_button_font_size", 9), FontStyle.Bold);
		protected bool showText = false;
		protected Color textforeground_color = Color.White;

		public int State
		{
			get
			{
				return state;
			}

			set
			{
				state = Math.Max(0, Math.Min(1, value));
				SetVariants(filebase[state]);
				Invalidate();
			}
		}

		/// <summary>
		/// Standard constructor with no overlaid text, just the buttons 
		/// </summary>
		/// <param name="code"></param>
		/// <param name="filebase0"></param>
		/// <param name="filebase1"></param>
		public ImageToggleButton (int code, string filebase0, string filebase1)
			: base (code)
		{
			filebase[0] = filebase0;
			filebase[1] = filebase1;
			State = 0;
		}

		Image LoadImageFromBaseFilename (string filebase)
		{
			int dotIndex = filebase.LastIndexOf(".");
			if (dotIndex < 0)
			{
				dotIndex = filebase.Length;
			}

			string filestem = AppInfo.TheInstance.Location + "\\" + filebase.Substring(0, dotIndex);

			return Repository.TheInstance.GetImage(filestem + ".png");
		}

		public override bool SetVariants (string filebase)
		{
			bool returnValue = base.SetVariants(filebase);

			if (! string.IsNullOrEmpty(disabledFilename))
			{
				disabled = LoadImageFromBaseFilename(disabledFilename);
			}

			if (! string.IsNullOrEmpty(hoverFilename))
			{
				hover = LoadImageFromBaseFilename(hoverFilename);
			}

			return returnValue;
		}

		public ImageToggleButton (int code, string filebase0, string filebase1, string displaytext0, string displaytext1)
			: this (code, filebase0, filebase1, displaytext0, displaytext1, null, null)
		{
		}

		/// <summary>
		/// extended Constructor that allows overlaid text
		/// This was added to support the new translated text funtionality
		/// </summary>
		/// <param name="code"></param>
		/// <param name="filebase0"></param>
		/// <param name="filebase1"></param>
		/// <param name="displaytext0"></param>
		/// <param name="displaytext1"></param>
		public ImageToggleButton (int code, string filebase0, string filebase1, string displaytext0, string displaytext1,
		                          string disabledFilename, string hoverFilename)
			: base (code)
		{
			filebase[0] = filebase0;
			filebase[1] = filebase1;

			displaytext[0] = TextTranslator.TheInstance.Translate(displaytext0);
			displaytext[1] = TextTranslator.TheInstance.Translate(displaytext1);

			this.disabledFilename = disabledFilename;
			this.hoverFilename = hoverFilename;

			string fontname =  SkinningDefs.TheInstance.GetData("fontname");			
			buttonFont.Dispose();
			buttonFont = ConstantSizeFont.NewFont(TextTranslator.TheInstance.GetTranslateFont(fontname), SkinningDefs.TheInstance.GetFloatData("toggle_button_font_size", 10), FontStyle.Bold);

			//allow the skin file to overide the text foreground color 
			textforeground_color = SkinningDefs.TheInstance.GetColorDataGivenDefault("toggle_button_text_forecolor", Color.White);

			showText= true; 
			State = 0;
		}

		public void setTextForeColor(Color newForeColor)
		{
			textforeground_color = newForeColor;
		}

		protected override void OnPaint(System.Windows.Forms.PaintEventArgs e)
		{
			Image i = up;

			bool gotFocus = Focused;

			if(gotFocus && (focus!=null))
			{
				i = focus;
			}

			if( !Enabled && (null!=disabled) )
			{
				i = disabled;
			}
			else if(mouseHover && mouseDown && (null != down) || Active)
			{
				i = down;
			}
			else if(mouseHover && (null != hover))
			{
				i = hover;
			}
			//
			if(null != i)
			{
				Rectangle src = new Rectangle(0,0,i.Width,i.Height);
				e.Graphics.DrawImage(i,ClientRectangle,src,GraphicsUnit.Pixel);
			}
			
			if (showText)
			{
				StringFormat sf = new StringFormat(StringFormatFlags.MeasureTrailingSpaces);
				string strDisplay = "";
				if (State==0)
				{
					strDisplay = displaytext[0];
				}
				else
				{
					strDisplay = displaytext[1];
				}
				SizeF tmpMeasure = e.Graphics.MeasureString(strDisplay,buttonFont,Width,sf);
				int offset_x = (Width/2)-((int)(tmpMeasure.Width/2));
				int offset_y = (Height/2)-((int)(tmpMeasure.Height/2));

				Color textColour = textforeground_color;
				if (! Enabled)
				{
					textColour = Color.FromArgb(50, textColour.R, textColour.G, textColour.B);
				}

				using (Brush brush = new SolidBrush(textColour))
				{
					e.Graphics.DrawString(strDisplay, buttonFont, brush, offset_x, offset_y);
				}
			}
			
		}

	}
}
