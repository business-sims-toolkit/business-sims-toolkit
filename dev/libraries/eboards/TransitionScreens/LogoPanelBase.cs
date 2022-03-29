using System;
using System.Drawing;
using System.IO;
using CommonGUI;

using LibCore;

namespace TransitionScreens
{
	/// <summary>
	/// This is the base class for the Transition Phase Logo Panel 
	/// </summary>
	public class LogoPanelBase : BasePanel
	{
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		System.ComponentModel.Container components = null;
		protected string ImageDirectory = "";

		public LogoPanelBase()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
			// TODO: Add any initialization after the InitializeComponent call
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
		public virtual void SwitchOffImageBackground()
		{
		}

		/// <summary>
		/// Common method for setting the Image Directory data
		/// </summary>
		/// <param name="ImgDir"></param>
		public void SetImageDir(string ImgDir)
		{
			ImageDirectory = ImgDir;
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
				GC.Collect();
			}
			return hack;
		}

		/// <summary>
		/// Overrided by the skin dependant versions
		/// </summary>
		/// <param name="Tr"></param>
		public virtual void SetTrainingMode(Boolean Tr)
		{
		}

		/// <summary>
		/// Overrided by the skin dependant versions
		/// </summary>
		/// <param name="Tr"></param>
		public virtual void BuildLogoContents()
		{
		}



	}
}
