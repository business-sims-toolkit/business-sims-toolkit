using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.IO;
using System.Windows.Forms;
using LibCore;

using TransitionScreens;

namespace Polestar_PM.TransScreen
{
	/// <summary>
	/// Summary description for LogoPanel.
	/// </summary>
	public class LogoPanel_MS : LogoPanelBase
	{
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
		PictureBox FacLogoPicBox = null;
		PictureBox TeamLogoPicBox = null;

		/// <summary>
		/// In Pole Star Logo, We have 2 logo and one badge 
		/// The badge is supplied as part of the Background Graphic 
		/// </summary>
		public LogoPanel_MS()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
			// TODO: Add any initialization after the InitializeComponent call
			this.BackColor = Color.DeepSkyBlue;

			this.SuspendLayout();

			//The supplied Team Logo
			TeamLogoPicBox = new PictureBox();
			TeamLogoPicBox.BackColor = System.Drawing.Color.White;
			TeamLogoPicBox.Location = new Point(20+12-18+2,14-11+7);
			TeamLogoPicBox.Size = new Size(140,70);
			TeamLogoPicBox.Image = null;
			TeamLogoPicBox.Visible = true;
			TeamLogoPicBox.SizeMode = PictureBoxSizeMode.StretchImage;
			this.Controls.Add(TeamLogoPicBox);

			//The supplied Facilitor Logo
			FacLogoPicBox = new PictureBox();
			FacLogoPicBox.BackColor = System.Drawing.Color.White;
			FacLogoPicBox.Location = new Point(200-12+7+5,14-11+7);
			FacLogoPicBox.Size = new Size(140,70);
			FacLogoPicBox.Image = null;
			FacLogoPicBox.Visible = true;
			FacLogoPicBox.SizeMode = PictureBoxSizeMode.StretchImage;
			this.Controls.Add(FacLogoPicBox);

			this.ResumeLayout(false);
		}

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Component Designer generated code
		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			components = new System.ComponentModel.Container();
		}
		#endregion

		/// <summary>
		/// Handling the work for PoleStar version
		/// </summary>
		/// <param name="TrainingGameFlag"></param>
		public override void SetTrainingMode(Boolean TrainingGameFlag)
		{
			if (TrainingGameFlag)
			{
				BackgroundImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + 
					"images\\t_bg_logos.png");
				TeamLogoPicBox.Visible = false;
				FacLogoPicBox.Visible = false;
			}
			else
			{
				BackgroundImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + 
					"images\\bg_logos.png");
				BuildLogoContents();
			}
		}

		/// <summary>
		/// Handling the work for PoleStar version
		/// </summary>
		public override void BuildLogoContents()
		{
			//Need to fill the Team Logo 
			string TeamLogoPath = ImageDirectory+"\\global\\logo.png";
			string FacilLogoPath = ImageDirectory+"\\global\\facil_logo.png";

			string DefCustLogoPath = AppInfo.TheInstance.Location+"images\\DefCustLogo.png";
			string DefFacLogoPath = AppInfo.TheInstance.Location+"images\\DefFacLogo.png";
				
			TeamLogoPicBox.Image = GetIsolatedImage(TeamLogoPath, DefCustLogoPath);
			FacLogoPicBox.Image = GetIsolatedImage(FacilLogoPath, DefFacLogoPath);
		}
	}
}
