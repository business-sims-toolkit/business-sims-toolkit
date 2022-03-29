using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.IO;
using System.Windows.Forms;
using LibCore;

namespace Polestar_PM.OpsGUI
{
	/// <summary>
	/// Summary description for LogoPanel.
	/// </summary>
	public class PM_LogoPanel : PM_LogoPanelBase
	{
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
		PictureBox TeamLogoPicBox = null;
		bool SwitchOffImageBack = false;

		/// <summary>
		/// In Pole Star Logo, We have 2 logo and one badge 
		/// The badge is supplied as part of the Background Graphic 
		/// </summary>
		public PM_LogoPanel()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
			// TODO: Add any initialization after the InitializeComponent call
			this.BackColor = Color.DeepSkyBlue;

			this.SuspendLayout();

			//The supplied Team Logo
			TeamLogoPicBox = new PictureBox();
			TeamLogoPicBox.BackColor = System.Drawing.Color.White;
			TeamLogoPicBox.Location = new Point(5+30,5+4);
			TeamLogoPicBox.Size = new Size(140,70);
			TeamLogoPicBox.Image = null;
			TeamLogoPicBox.Visible = true;
			TeamLogoPicBox.SizeMode = PictureBoxSizeMode.StretchImage;
			this.Controls.Add(TeamLogoPicBox);

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
		/// Stub for new operation
		/// Normal behaviour but no background image (McKinley Reports)
		/// </summary>
		/// <param name="ImgDir"></param>
		public override void SwitchOffImageBackground()
		{
			SwitchOffImageBack = true;
		}


		/// <summary>
		/// Handling the work for PoleStar version
		/// </summary>
		/// <param name="TrainingGameFlag"></param>
		public override void SetTrainingMode(Boolean TrainingGameFlag)
		{
			if (TrainingGameFlag)
			{
				BackgroundImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location +
					"images\\panels\\t_logo_panel.png");
				TeamLogoPicBox.Visible = false;
			}
			else
			{
				if (SwitchOffImageBack==false)
				{
					BackgroundImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + 
						"images\\panels\\logo_panel.png");
				}
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
			string DefCustLogoPath = AppInfo.TheInstance.Location+"images\\DefCustLogo.png";
			TeamLogoPicBox.Image = GetIsolatedImage(TeamLogoPath, DefCustLogoPath);
		}
	}
}
