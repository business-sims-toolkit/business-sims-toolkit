using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

using CommonGUI;

using CoreUtils;

using LibCore;

namespace DevOps.OpsScreen
{
	internal class DevErrorImageTextButton : ImageTextButton
    {
        string iconImage;
        int errorCount;

        public DevErrorImageTextButton() : base(0)
        {
        }

        public DevErrorImageTextButton (int code) : base(code)
        {
        }

        public DevErrorImageTextButton (int code, bool flickerFree) : base(code, flickerFree)
        {
        }

        public DevErrorImageTextButton (string fileBase, bool flickerFree) : base(fileBase, flickerFree)
        {
        }

        public DevErrorImageTextButton (string fileBase) : base(fileBase)
        {
        }

        public int ErrorCount
        {
            set { errorCount = value; }
        }

        public string IconImage
        {
            set { iconImage = value; }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            bool gotFocus = Focused;

            Color colour = upColor;

            Image image = up;
            if (gotFocus && (focus != null))
            {
                image = focus;
                colour = focusColor;
            }

            if (!Enabled)
            {
                if (Active)
                {
                    image = activeDisabled;
                    colour = activeDisabledColor;
                }
                else
                {
                    image = disabled;
                    colour = disabledColor;
                }
            }
            else if (((mouseHover && mouseDown) || Active) && (null != down))
            {
                image = down;
                colour = downColor;
            }
            else if (mouseHover && (null != hover))
            {
                image = hover;
                colour = hoverColor;
            }

            if (image != null)
            {
                Rectangle src = new Rectangle(0, 0, Width, Height);
                e.Graphics.DrawImage(image, ClientRectangle, src, GraphicsUnit.Pixel);
            }
            int padding = 4;
            int iconWidth = Height - (2 * padding);

            if (iconImage != null)
            {
                Image icon = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\icons\\" + iconImage + "_default.png");

                e.Graphics.DrawImage(icon, new Rectangle(padding, padding, iconWidth, iconWidth));
            }

            if (buttonText != null)
            {
                using (Brush brush = new SolidBrush(colour))
                {
                    int fontSize = 10;
                    Font font = SkinningDefs.TheInstance.GetFont(fontSize);

                    int textY = (Height - fontSize) / 2;
                    
                    e.Graphics.DrawString(buttonText, font, brush, 80, textY);

                    string errorText = Plurals.Format(errorCount, "Error", "Errors");
                    int errorX = Width - (5) - (int)e.Graphics.MeasureString(errorText, font).Width;
                    e.Graphics.DrawString(errorText, 
                        font, brush, errorX, textY);
                }
            }

            
        }	
    }
}