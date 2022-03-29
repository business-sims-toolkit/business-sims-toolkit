using System;
using System.Drawing;
using System.Windows.Forms;
using LibCore;

namespace TransitionScreens
{
	/// <summary>
	/// Summary description for LogoPanel.
	/// </summary>
	public class LogoPanel_HP : LogoPanelBase
	{
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		System.ComponentModel.Container components = null;
		PictureBox TeamLogoPicBox = null;

		/// <summary>
		/// In Pole Star Logo, We have 1 Logo and 2 badges 
		/// The badges are supplied as part of the Background Graphic 
		/// </summary>
		public LogoPanel_HP()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
			// TODO: Add any initialization after the InitializeComponent call
			BackColor = Color.LightSkyBlue;

//			Label product_label = new Label();
//			product_label.Text = "HP";
//			product_label.Size = new Size(80,15);
//			product_label.Location = new Point(0,0);
//			this.Controls.Add(product_label);

			SuspendLayout();

			//The supplied Team Logo
			TeamLogoPicBox = new PictureBox();
			TeamLogoPicBox.BackColor = Color.White;
			//TeamLogoPicBox.BackColor = System.Drawing.Color.Plum;
			TeamLogoPicBox.Location = new Point(212,12);
			TeamLogoPicBox.Size = new Size(140,70);
			TeamLogoPicBox.Image = null;
			TeamLogoPicBox.Visible = true;
			TeamLogoPicBox.SizeMode = PictureBoxSizeMode.StretchImage;
			Controls.Add(TeamLogoPicBox);

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
		/// </summary>
		/// <param name="TrainingGameFlag"></param>
		public override void SetTrainingMode(Boolean TrainingGameFlag)
		{
			if (TrainingGameFlag)
			{
				BackgroundImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + 
					"\\images\\panels\\t_bg_logos.png");
				TeamLogoPicBox.Visible = false;
			}
			else
			{
				BackgroundImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + 
					"\\images\\panels\\bg_logos.png");
				TeamLogoPicBox.Visible = true;
				BuildLogoContents();
			}
		}

		/// <summary>
		/// </summary>
		public override void BuildLogoContents()
		{
			//Need to fill the Team Logo 
			string TeamLogoPath = ImageDirectory+"\\global\\logo.png";
			string DefCustLogoPath = AppInfo.TheInstance.Location+"\\images\\DefCustLogo.png";
				
			TeamLogoPicBox.Image = GetIsolatedImage(TeamLogoPath, DefCustLogoPath);
		}

	}
}
