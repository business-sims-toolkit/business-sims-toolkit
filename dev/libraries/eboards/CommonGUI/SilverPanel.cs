using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

using LibCore;

namespace CommonGUI
{
	/// <summary>
	/// Summary description for SilverPanel.
	/// </summary>
	public class SilverPanel : BasePanel
	{
		Image helmets;
		Bitmap Symbol; //either the badge or the icons
		Image trainingIcon;
		Image IconSurround;
		Boolean showAll = true;
		PictureBox LogoPicBox = null;
		Boolean MyTrainingFlag = false;
		Boolean showSurround = false;

	    public bool ShowSurround
	    {
	        get
            {
                return showSurround;
            }

	        set
	        {
	            showSurround = value;
	            Invalidate();
	        }
	    }

	    public SilverPanel(string logo, int whichHelmets, Color BackRequested, Boolean IsTraining)
		{
			MyTrainingFlag = IsTraining;
			//set the background color
			BackColor = BackRequested;

			Resize += SilverPanel_Resize;

			trainingIcon = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\DefCustLogo.png");
			IconSurround = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\panels\\screen_custlogo.png");

			string BadgeName = CoreUtils.SkinningDefs.TheInstance.GetData("badge");
			Symbol = (Bitmap) Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\"+BadgeName);

			//Fill the Picture with the Badge as Default
			LogoPicBox = new PictureBox();
			LogoPicBox.BackColor = Color.Transparent;
			LogoPicBox.Location = new Point(5,90);
			LogoPicBox.Size = new Size(140,70);
			LogoPicBox.Image = Symbol;
			LogoPicBox.Visible = !(MyTrainingFlag);
			LogoPicBox.SizeMode = PictureBoxSizeMode.StretchImage;
			Controls.Add(LogoPicBox);

			if (whichHelmets==1)
			{
				helmets = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\helmets_1.png");
			}
			else
			{
				helmets = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\helmets_2.png");
			}
			
			if(logo != "default")
			{
				Bitmap tmp_img;
				if(File.Exists(logo))
				{
					tmp_img = new Bitmap(logo);
					tmp_img = (Bitmap) BitmapUtils.ConvertToApectRatio((Bitmap)tmp_img, 2).Clone();
				}
				else
				{
					tmp_img = new Bitmap(AppInfo.TheInstance.Location+"\\images\\DefCustLogo.png");
				}

				Symbol = new Bitmap(tmp_img.Width, tmp_img.Height);
				Graphics g = Graphics.FromImage(Symbol);
				g.DrawImage(tmp_img,0,0,(int)Symbol.Width,(int)Symbol.Height);
				g.Dispose();

				tmp_img.Dispose();
				tmp_img = null;
				GC.Collect();

				LogoPicBox.Image = Symbol;
				ShowSurround = true;
			}	
			else
			{
				ShowSurround = false;
				LogoPicBox.Size = Symbol.Size;
			}
		}

		void SilverPanel_Resize(object sender, EventArgs e)
		{
			int offset = Width - LogoPicBox.Width; 
 
			if (offset>0)
			{
				LogoPicBox.Left = offset / 2;
			}
			else
			{
				LogoPicBox.Left = 0;
			}

			if (Height < 180)
			{
				showAll = false;
			}
			else
			{
				showAll = true;
			}
			Invalidate(); //Refresh();
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			if (MyTrainingFlag)
			{
				if (helmets != null)
				{
					e.Graphics.DrawImage(trainingIcon,30,0);
					if (showAll)
					{
						e.Graphics.DrawImage(trainingIcon,30,100);
					}	
				}
			}
			else
			{
				//e.Graphics.DrawRectangle(Pens.Black,0,0,this.Width-1,this.Height-1);
				if (showAll)
				{
					if (helmets != null)
					{
						e.Graphics.DrawImage(helmets,0,0);
					}
					if (IconSurround != null)
					{
						if (ShowSurround)
						{
							int x = LogoPicBox.Left - 6;
							int y = LogoPicBox.Top - 5;
							int w = 150;
							int h = 80;
							e.Graphics.DrawImage(IconSurround, x, y, w, h);
						}
					}
				}
				else
				{
					LogoPicBox.Top = 5;
					if (IconSurround != null)
					{
						if (ShowSurround)
						{
							int x = LogoPicBox.Left - 6;
							int y = LogoPicBox.Top - 5;
							int w = 150;
							int h = 80;
							e.Graphics.DrawImage(IconSurround, x, y, w, h);
						}
					}

				}
			}
		}

	}
}
