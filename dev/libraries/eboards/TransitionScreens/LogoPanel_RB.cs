using System;
using System.Drawing;
using System.Windows.Forms;
using LibCore;

namespace TransitionScreens
{
	/// <summary>
	/// Summary description for LogoPanel.
	/// </summary>
	public class LogoPanel_RB : LogoPanelBase
	{
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		System.ComponentModel.Container components = null;
		PictureBox FacLogoPicBox = null;
		PictureBox TeamLogoPicBox = null;

		/// <summary>
		/// In Reckitt Benckiser Logo, We have 2 logo and one badge 
		/// The badge is supplied as part of the Background Graphic 
		/// </summary>
		public LogoPanel_RB()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
			// TODO: Add any initialization after the InitializeComponent call
			BackColor = Color.DeepSkyBlue;

			SuspendLayout();

			//The supplied Team Logo
			TeamLogoPicBox = new PictureBox();
			TeamLogoPicBox.BackColor = Color.White;
			TeamLogoPicBox.Location = new Point(20,14);
			TeamLogoPicBox.Size = new Size(140,70);
			TeamLogoPicBox.Image = null;
			TeamLogoPicBox.Visible = true;
			TeamLogoPicBox.SizeMode = PictureBoxSizeMode.StretchImage;
			Controls.Add(TeamLogoPicBox);

			//The supplied Facilitor Logo
			FacLogoPicBox = new PictureBox();
			FacLogoPicBox.BackColor = Color.White;
			FacLogoPicBox.Location = new Point(400,14);
			FacLogoPicBox.Size = new Size(140,70);
			FacLogoPicBox.Image = null;
			FacLogoPicBox.Visible = true;
			FacLogoPicBox.SizeMode = PictureBoxSizeMode.StretchImage;
			Controls.Add(FacLogoPicBox);

			ResumeLayout(false);
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
					"\\images\\panels\\t_bg_logos.png");
				TeamLogoPicBox.Visible = false;
				FacLogoPicBox.Visible = false;
			}
			else
			{
				BackgroundImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + 
					"\\images\\panels\\bg_logos.png");
				BuildLogoContents();
			}
		}

		/// <summary>
		/// Handling the work for RB version
		/// </summary>
		public override void BuildLogoContents()
		{
			//Need to fill the Team Logo 
			string TeamLogoPath = ImageDirectory+"\\global\\logo.png";
			string FacilLogoPath = ImageDirectory+"\\global\\facil_logo.png";

			string DefCustLogoPath = AppInfo.TheInstance.Location+"\\images\\DefCustLogo.png";
			string DefFacLogoPath = AppInfo.TheInstance.Location+"\\images\\DefFacLogo.png";
				
			TeamLogoPicBox.Image = GetIsolatedImage(TeamLogoPath, DefCustLogoPath);
			FacLogoPicBox.Image = GetIsolatedImage(FacilLogoPath, DefFacLogoPath);
		}
	}
}
