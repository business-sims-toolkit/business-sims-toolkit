using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

using LibCore;
using CommonGUI;

namespace DiscreteSimGUI
{
	/// <summary>
	/// This display 2 logos in 2 different styles
	///  Vertical Stack (default) and Horizontal InLine
	///  Each logo is the standard 2:1 aspect ration (standard size 140 by 70) 
	/// </summary>
	public class LogoPanel : BasePanel
	{
		//private Image top,topRight,right,bottomRight,bottom,bottomLeft,left,topLeft,bg;
		//private Image helmets;
		Bitmap Symbol; //either the badge or the icons

		Image trainingIcon;
		//private Boolean showBadge= false;
		//private Boolean showAll = true;
        protected PictureBox TeamLogoPicBox = null;
        protected PictureBox FacilLogoPicBox = null;

		Boolean MyTrainingFlag = false;
		//private Boolean ShowSurround = false;
		Boolean UseVerticalStackMode = true;


		//private Panel b = null;
		//private Panel helmets = null;

		public LogoPanel(Boolean IsTraining, string ImageDir)
		{
			 MyTrainingFlag = IsTraining;
			//set the background color
			this.BackColor = Color.Pink;
			this.BackColor = Color.White;
	
			//Fill the Picture with the Badge as Default
			TeamLogoPicBox = new PictureBox();
			TeamLogoPicBox.BackColor = System.Drawing.Color.White;
			TeamLogoPicBox.Location = new Point(2,2);
			TeamLogoPicBox.Size = new Size(140,70);
			TeamLogoPicBox.Image = trainingIcon;
			//TeamLogoPicBox.Visible = !(MyTrainingFlag);
			TeamLogoPicBox.SizeMode = PictureBoxSizeMode.StretchImage;
			this.Controls.Add(TeamLogoPicBox);


			//Fill the Picture with the Badge as Default
			FacilLogoPicBox = new PictureBox();
			FacilLogoPicBox.BackColor = System.Drawing.Color.White;
			FacilLogoPicBox.Location = new Point(2,100);
			FacilLogoPicBox.Size = new Size(140,70);
			FacilLogoPicBox.Image = Symbol;
			//FacilLogoPicBox.Visible = !(MyTrainingFlag);
			FacilLogoPicBox.SizeMode = PictureBoxSizeMode.StretchImage;
			this.Controls.Add(FacilLogoPicBox);

			trainingIcon = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\logo_company_small.png");
			string BadgeName = CoreUtils.SkinningDefs.TheInstance.GetData("badge");
			Symbol = (Bitmap) Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\"+BadgeName);

			string TeamLogoPath = ImageDir+"\\global\\logo.png";
			string FacilLogoPath = ImageDir+"\\global\\facil_logo.png";

			string DefCustLogoPath = AppInfo.TheInstance.Location+"\\images\\DefCustLogo.png";
			string DefFacLogoPath = AppInfo.TheInstance.Location+"\\images\\DefFacLogo.png";
			
			TeamLogoPicBox.Image = GetIsolatedImage(TeamLogoPath, DefCustLogoPath);
			FacilLogoPicBox.Image = GetIsolatedImage(FacilLogoPath, DefFacLogoPath);

            this.Resize += LogoPanel_Resize;
		}

		void LogoPanel_Resize(object sender, EventArgs e)
		{
			//int spare = this.Height - (LogoPicBox.Height + UpperLogoPicBox.Height + 10);
			
			//UpperLogoPicBox.Top = 5;
			//LogoPicBox.Top = this.Height - (LogoPicBox.Height + 10);
			//Refresh();

			FitLogosToPanelSize();
		}

		/// <summary>
		/// This gets the Image Data for a particular File.
		/// When you build a bitmap from a file, the file handle is not released until the bitmap is disposed.
		/// So we build a temp image which is copied to a new image 
		/// when the temp image is destroyed, the file handle is released. 
		/// </summary>
		/// <param name="PreferedImageFileName"></param>
		/// <param name="BackupImageFileImage"></param>
		/// <returns></returns>
		protected Image GetIsolatedImage(string PreferedImageFileName, string BackupImageFileImage)
		{
			Bitmap tmp_img = null;
			Bitmap hack = null;

			if (File.Exists(PreferedImageFileName))
			{
				tmp_img = new Bitmap(PreferedImageFileName);
				tmp_img = (Bitmap) BitmapUtils.ConvertToApectRatio((Bitmap)tmp_img, 2).Clone();
			}
			else
			{
				if (File.Exists(BackupImageFileImage))
				{
					tmp_img = new Bitmap(BackupImageFileImage);
				}
			}
			if (tmp_img != null)
			{
				hack = new Bitmap(tmp_img.Width, tmp_img.Height);
				Graphics g = Graphics.FromImage(hack);
				g.DrawImage(tmp_img,0,0,(int)hack.Width,(int)hack.Height);
				g.Dispose();
				tmp_img.Dispose();
				tmp_img = null;
				System.GC.Collect();
			}
			return hack;
		}

		public void SetDisplayVertical(Boolean ShowVert)
		{
			UseVerticalStackMode = ShowVert;
			FitLogosToPanelSize();
		}

		//Used to display a totally invisiable control when training HPOBISM 
		//letting the main background shine through
		public void SetTrainingEmpty(Boolean isTraining)
		{
			if (isTraining)
			{
				this.BackColor = Color.Transparent;
				TeamLogoPicBox.Visible = false;
				FacilLogoPicBox.Visible = false;
			}
		}

		public void FitLogosToPanelSize ()
		{
			if (UseVerticalStackMode)
			{
				FacilLogoPicBox.Width = this.Width - 4;
				FacilLogoPicBox.Height = FacilLogoPicBox.Width / 2;

				int hh = (this.Height - 4)/2;
				int top1 = (hh-FacilLogoPicBox.Height)/2;
				int top2 = hh + top1;

				FacilLogoPicBox.Location = new Point (2, 2 + top2);
				//FacilLogoPicBox.Location = new Point (2, this.Height - FacilLogoPicBox.Height);

				TeamLogoPicBox.Width = this.Width - 4;
				TeamLogoPicBox.Height = TeamLogoPicBox.Width / 2;
				TeamLogoPicBox.Location = new Point (2, 2 + top1);
				//TeamLogoPicBox.Location = new Point (2, 2);
			}
			else
			{
				int UsableWidth = this.Width-4;
				int UsableHeight = this.Height-4;

				FacilLogoPicBox.Width = (UsableWidth/2);
				FacilLogoPicBox.Height = UsableHeight;
				FacilLogoPicBox.Location = new Point (2, 2);
				//FacilLogoPicBox.Location = new Point (2, this.Height - FacilLogoPicBox.Height);

				TeamLogoPicBox.Width = (UsableWidth/2);
				TeamLogoPicBox.Height = UsableHeight;
				TeamLogoPicBox.Location = new Point (2 + FacilLogoPicBox.Width +2 , 2);
				//TeamLogoPicBox.Location = new Point (2, 2);
			}
		}

		protected override void OnPaint(PaintEventArgs e)
		{
		}
	}
}
