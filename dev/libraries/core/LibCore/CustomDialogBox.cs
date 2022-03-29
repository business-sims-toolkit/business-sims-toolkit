using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace LibCore
{
    public class CustomDialogBox : Form
    {
        protected Panel blurbBackground;
        protected Label blurb;

        protected int margin = 10;
        protected int gap = 20;
        protected Icon icon;
        protected PictureBox image;
        protected Button ok;
        protected Button cancel;
        protected Button close;

	    struct Rect
	    {
		    public int Left { get; set; }
		    public int Top { get; set; }
		    public int Right { get; set; }
		    public int Bottom { get; set; }
		    public int Width => Right - Left;
		    public int Height => Bottom - Top;
	    }

		[DllImport("user32.dll")]
	    static extern bool GetWindowRect (IntPtr hwnd, ref Rect rectangle);


		public enum MessageType
        {
            Warning,
            Question,
            Error,
            Standard
        }

        public CustomDialogBox()
            : this(MessageType.Standard)
        {
        }

	    public new DialogResult ShowDialog (IWin32Window parent)
	    {
		    var parentBounds = new Rect();
			GetWindowRect(parent.Handle, ref parentBounds);
		    Location = new Point (parentBounds.Left + ((parentBounds.Width - Width) / 2), parentBounds.Top + ((parentBounds.Height / 2)));
		    return base.ShowDialog(parent);
	    }
		
        public CustomDialogBox(MessageType messageType)
        {
	        StartPosition = FormStartPosition.Manual;

            blurbBackground = new Panel() { BackColor = Color.White };
            Controls.Add(blurbBackground);
           

            this.MinimizeBox = false;
            this.MaximizeBox = false;
            blurb = new Label
            {
                Text =
                    "NEED TO SET TEXT IN BLURB",
                BackColor = Color.White
            };
            blurbBackground.Controls.Add(blurb);

            
            

            ok = new Button { Text = "OK" };
            ok.Click += ok_Click;
            Controls.Add(ok);

            cancel = new Button { Text = "Cancel" };
            cancel.Click += cancel_Click;
            Controls.Add(cancel);

            close = new Button { Text = "Close" };
            close.Click += close_Click;
            Controls.Add(close);

            close.Hide();

            
           
            FormBorderStyle = FormBorderStyle.FixedDialog;
            ShowInTaskbar = false;

            this.setFont(SystemFonts.MessageBoxFont);
            this.ForeColor = SystemColors.WindowText;

            this.Size = new Size(375, 175);


            StartPosition = FormStartPosition.Manual;
            this.Location = new Point((1024- this.Size.Width)/2, (786 - this.Size.Height) / 2);

            UpdateButtons();


            if (messageType == MessageType.Warning)
            {
                this.Text = "WARNING!";
                this.AddImage(SystemIcons.Warning);
            }

            if (messageType == MessageType.Error)
            {
                this.Text = "ERROR!";
                this.AddImage(SystemIcons.Error);
            }

            if (messageType == MessageType.Error)
            {
                this.Text = "Question";
                this.AddImage(SystemIcons.Question);
            }
            


        }

        public void setFont(Font font)
        {
            blurb.Font = font;
        }

        public void AddImage(Icon _icon)
        {
            //icon = _icon;
            ShowIcon = true;
            this.Icon = _icon;

            image = new PictureBox();
            
                
            Bitmap iconImage = System.Drawing.SystemIcons.Warning.ToBitmap();
            image.Image = iconImage;
            image.Size = iconImage.Size;
            DoSize();
        }

        public void setText(string _blurb)
        {
            blurb.Text = _blurb;
            blurb.TextAlign = ContentAlignment.MiddleLeft;
            DoSize();
        }

        public void setOKButtonText(string text)
        {
            ok.Text = text;
        }   

        protected virtual void textBox_TextChanged(object sender, EventArgs e)
        {
            UpdateButtons();
        }


        protected virtual void UpdateButtons()
        {
        }


        protected virtual void DoSize()
        {
            int iconBuffer = 0;
            int labelHeight = 0;
            int stayOnScreenBuffer = 0;
            using (Graphics g = CreateGraphics())
            {
                SizeF size = g.MeasureString(blurb.Text, blurb.Font, ClientSize.Width - margin*2);

                iconBuffer = (image == null) ? 0 : (int)(ClientSize.Width - size.Width - image.Width)/2;
                labelHeight = (int)size.Height;
            }
            
            int imageRight = 0;
            if (image != null)
            {

                if (!blurbBackground.Controls.Contains(image))
                {
                    blurbBackground.Controls.Add(image);
                    image.BringToFront();

                    
                }


                if (iconBuffer >= 15)
                {
                    stayOnScreenBuffer = 10;
                }
                    image.Location = new Point(iconBuffer - stayOnScreenBuffer, blurb.Location.Y + 20);
                
                imageRight = image.Right;
            }

            Size buttonSize = new Size(80, 26);

            cancel.Size = buttonSize;
            cancel.Location = new Point(ClientSize.Width - margin - cancel.Width, ClientSize.Height - margin - cancel.Height);

            close.Size = buttonSize;
            close.Location = cancel.Location;

            ok.Size = buttonSize;
            ok.Location = new Point(cancel.Left - gap - ok.Width, cancel.Top);



            blurbBackground.Location = new Point(0, 0);
            blurbBackground.Size = new Size(ClientSize.Width - blurbBackground.Left, ok.Top - gap - blurbBackground.Top);

            if (imageRight + stayOnScreenBuffer == 0)
            {
                imageRight = 10;
            }

            blurb.Location = new Point(imageRight + stayOnScreenBuffer, margin);
            blurb.Size = new Size(blurbBackground.Width - imageRight - iconBuffer , blurbBackground.Height - margin - blurb.Top);
        }



        protected virtual void textBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if ((e.KeyChar == 10) || (e.KeyChar == 13))
            {
                if (ok.Enabled)
                {
                    ok_Click(sender, e);
                }
            }
            else if (e.KeyChar == 27)
            {
                cancel_Click(sender, e);
            }
        }

        protected void cancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        protected void close_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        protected virtual void ok_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }


        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);

            DoSize();
        }


    }
}
