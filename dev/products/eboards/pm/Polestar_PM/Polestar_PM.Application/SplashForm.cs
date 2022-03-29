using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using System.Xml;

using Polestar_PM.OpsScreen;
using GameDetails;

using LibCore;

using System.Threading;
using GameManagement;

using Logging;
using System.IO;
using Licensor;
using ConfPack = GameDetails.ConfPack;

namespace Polestar_PM.Application
{
	public class SplashForm : Form
	{
		protected System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
		protected MainGameForm mf;
		private Label version;
		ProgressBar progressBar;
		protected IGameLicence license;

		List<string> extractedFiles;

		public SplashForm()
		{
			InitializeComponent();
			string splash = LibCore.AppInfo.TheInstance.InstallLocation + "/v3SplashScreen.png";
			if (!File.Exists(splash))
			{
				splash = LibCore.AppInfo.TheInstance.InstallLocation + "/images/v3SplashScreen.png";
			}
			//
			this.BackgroundImage = new Bitmap(splash);

			this.ShowInTaskbar = false;

			version.Text = System.Windows.Forms.Application.ProductVersion;
		}

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.version = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// version
			// 
			this.version.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.version.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.version.Location = new System.Drawing.Point(350, 230);
			this.version.Name = "version";
			this.version.Size = new System.Drawing.Size(134, 74);
			this.version.TabIndex = 0;
			version.BackColor = Color.White;
			version.ForeColor = Color.Black;
			this.version.TextAlign = ContentAlignment.MiddleCenter;

			progressBar = new ProgressBar();
			progressBar.Location = new Point(10, version.Top);
			progressBar.Size = new Size(version.Left - 10 - progressBar.Left, version.Height);
			progressBar.Style = ProgressBarStyle.Continuous;
			Controls.Add(progressBar);
			// 
			// SplashForm
			// 
			this.ClientSize = new System.Drawing.Size(499, 319);
			this.Controls.Add(this.version);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
			this.Name = "SplashForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = BaseUtils.AssemblyExtensions.MainAssemblyTitle;
			this.Load += new System.EventHandler(this.SplashForm_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private void ConnectLicenceHandlers()
		{
		}

		private void DisConnectLicenceHandlers()
		{
		}

		private void SplashForm_Load(object sender, EventArgs e)
		{
			timer.Tick += new EventHandler(timer_Tick);
			timer.Interval = 100;
			//timer.Start();

			//timer_Tick(this, null);
		}

		public void Start()
		{
			timer.Start();
		}

//		public void ConnectUptoExternalLicensor()
//		{
//
//		}

		void timer_Tick(object sender, EventArgs e)
		{
			timer.Stop();

#if !PASSEXCEPTIONS
			try
			{
#endif
				ConfPack cp = new ConfPack();
				extractedFiles = cp.ExtractAllFilesFromZip(LibCore.AppInfo.TheInstance.InstallLocation + "conf.xnd", LibCore.AppInfo.TheInstance.Location, "");


#if !PASSEXCEPTIONS
			}
			catch (Exception exception)
			{
				AppLogger.TheInstance.WriteException("App Level Exception", exception);
				this.Close();
			}
#endif
		}

		public void license_ConfigFilesLoaded(bool success, string prdname)
		{
			if (success)
			{
				HandleNextSection(success, prdname);
			}
		}

		public void HandleNextSection(bool Loaded, string prdname)
		{

			timer.Stop();

#if !PASSEXCEPTIONS
			try
			{
#endif
				if (Loaded)
				{
					//
					if (mf == null)
					{
						//
						if (true)
						{
							mf = new MainGameForm(license, null);
							AppLoader.context.MainForm = mf;
							mf.Disposed += new EventHandler(mf_Disposed);
							mf.Show();
							this.Hide();
						}
						else
						{
							//MessageBox.Show("Failed to acquire a license!");
							AppLoader.context.MainForm = this;
							DisConnectLicenceHandlers();
							this.Close();
						}
					}
				}
				else
				{
					MessageBox.Show("Failed to establish valid license!");
					this.Close();
				}

#if !PASSEXCEPTIONS
			}
			catch (Exception e)
			{
				AppLogger.TheInstance.WriteException("App Level Exception", e);
				DisConnectLicenceHandlers();
				this.Close();
			}
#endif
		}

		void license_ActUpdateComplete (object sender, TacPermissions tacPermissions)
		{
			if (null != mf)
			{
				mf.SetTacPermissions(tacPermissions);
			}
		}

		void mf_Disposed(object sender, EventArgs e)
		{
			mf = null;
			license = null;
			DisConnectLicenceHandlers();
			this.Close();

			BaseUtils.AppTidyUp.DeleteUnzippedFiles(extractedFiles);
		}
	}
}