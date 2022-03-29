using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Net;
using System.IO;

using CoreUtils;
using CommonGUI;

using LibCore;

namespace GameDetails
{
	public class VersionUpdatePanel : Panel
	{
		Label label;
		Button downloadButton;
		Button cancelButton;
		ProgressBar progressBar;
		WebClient webClient;
        ConfPack zipper;
        string unzipFolder;

        string releaseNotes = null;
        string installer = null;
        string miniReleaseNote = null;

		string downloadUrl;
		string downloadFilename;
        string downloadUsername;
        string downloadPassword;

		bool downloadAvailable;
		bool downloading;

		bool showFull;

		public VersionUpdatePanel ()
		{
			label = new Label ();
			Controls.Add(label);

			downloadButton = SkinningDefs.TheInstance.CreateWindowsButton(FontStyle.Bold);
			downloadButton.Text = "Download";
			downloadButton.Click += downloadButton_Click;
			Controls.Add(downloadButton);

			cancelButton = SkinningDefs.TheInstance.CreateWindowsButton(FontStyle.Bold);
			cancelButton.Text = "Cancel";
			cancelButton.Click += cancelButton_Click;
			Controls.Add(cancelButton);

			progressBar = new ProgressBar { Style = ProgressBarStyle.Continuous, BackColor = Color.LightGray };
			Controls.Add(progressBar);

			downloadAvailable = false;
			downloading = false;

			downloadUrl = "";
			downloadFilename = "";

			webClient = new WebClient ();
			webClient.DownloadFileCompleted += webClient_DownloadFileCompleted;
			webClient.DownloadProgressChanged += webClient_DownloadProgressChanged;

			showFull = true;

			UpdateButtons();
			DoSize();
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing)
			{
                webClient.CancelAsync();
                downloading = false;
				webClient.Dispose();
                
			}

			base.Dispose(disposing);
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);
			DoSize();
		}

		void downloadButton_Click (object sender, EventArgs e)
		{
			using (WaitCursor cursor = new WaitCursor (this))
			{
				try
				{
                    webClient.Credentials = new NetworkCredential(downloadUsername, downloadPassword);
					webClient.DownloadFileAsync(new Uri (downloadUrl), downloadFilename);

					progressBar.Minimum = 0;
					progressBar.Maximum = 1;
					progressBar.Value = 0;

					downloading = true;
                    label.Text = "Downloading Update";
				}
				catch
				{
				}

				UpdateButtons();
				DoSize();
			}
		}

		void cancelButton_Click (object sender, EventArgs e)
		{
			downloading = false;
			webClient.CancelAsync();

			UpdateButtons();
			DoSize();
		}

		void DoSize ()
		{
			label.Location = new Point (0, 0);
			label.Size = new Size (Width, 25);

			downloadButton.Location = new Point (0, 25);
		    if (SkinningDefs.TheInstance.GetBoolData("windows_buttons_styled", true))
		    {
		        downloadButton.BackColor =
		            SkinningDefs.TheInstance.GetColorDataGivenDefault("windows_button_colour", Color.LightSteelBlue);
		        downloadButton.ForeColor =
		            SkinningDefs.TheInstance.GetColorDataGivenDefault("windows_button_foreground_colour", Color.Black);
		    }
		    downloadButton.Size = new Size(downloadButton.GetPreferredSize(new Size (0, 0)).Width, 22);

			cancelButton.Location = new Point (0, 25);
		    if (SkinningDefs.TheInstance.GetBoolData("windows_buttons_styled", true))
		    {
		        cancelButton.BackColor =
		            SkinningDefs.TheInstance.GetColorDataGivenDefault("windows_button_colour", Color.LightSteelBlue);
		        cancelButton.ForeColor =
		            SkinningDefs.TheInstance.GetColorDataGivenDefault("windows_button_foreground_colour", Color.Black);
		    }
		    cancelButton.Size = new Size(cancelButton.GetPreferredSize(new Size (0, 0)).Width, 22);

			progressBar.Location = new Point (cancelButton.Right + 10, cancelButton.Top);
			progressBar.Size = new Size (Width - 4 - progressBar.Left, cancelButton.Height);
		}

		void UpdateButtons ()
		{
			label.Font = SkinningDefs.TheInstance.GetFont(8, (downloadAvailable ? FontStyle.Bold : FontStyle.Regular));
            label.ForeColor = (downloadAvailable ? CONVERT.ParseComponentColor(SkinningDefs.TheInstance.GetData("download_available_colour", "255,0,0")) : CONVERT.ParseComponentColor(SkinningDefs.TheInstance.GetData("download_uptodate_colour", "0,0,0")));
			downloadButton.Visible = downloadAvailable && ! downloading;
			cancelButton.Visible = downloading;
			progressBar.Visible = downloading;           
		}

		void webClient_DownloadProgressChanged (object sender, DownloadProgressChangedEventArgs e)
		{
			progressBar.Minimum = 0;
			progressBar.Maximum = (int) e.TotalBytesToReceive;
			progressBar.Value = (int) e.BytesReceived;
		}

		void webClient_DownloadFileCompleted (object sender, AsyncCompletedEventArgs e)
		{
			downloading = false;

			if (e.Cancelled)
			{
			}
			else if (e.Error != null)
			{
				Invoke(new MethodInvoker (() => MessageBox.Show(TopLevelControl,
				                                               "There was an error downloading the update.\n\n"
				                                               + e.Error.Message,
				                                               "Download Error",
				                                               MessageBoxButtons.OK)));
			}
			else
			{
				downloadAvailable = false;

				label.Text = "Latest version downloaded to desktop";

				if (InvokeRequired)
				{
					Invoke(new MethodInvoker (AskToRunInstaller));
				}
				else
				{
					AskToRunInstaller();
				}                
			}

			UpdateButtons();
			DoSize();
		}

		void AskToRunInstaller ()
		{
            DetermineFiles();

            int dialogHeight = miniReleaseNote == null ? 150 : 400;
            using (DownloadCompleteDialog dialog = new DownloadCompleteDialog(miniReleaseNote) { Size = new Size(600, dialogHeight) })
            {
                dialog.ShowDialog(TopLevelControl);

                if (dialog.DialogResult == DialogResult.OK)
                {
                    RunInstaller();
                }
            }			        
		}

        void DetermineFiles ()
        {           
            if (Path.GetExtension(downloadFilename).ToLowerInvariant() == ".zip")
            {
                zipper = new ConfPack();

                // Unzip the game file.
                unzipFolder = Path.GetTempFileName();
                File.Delete(unzipFolder);
                Directory.CreateDirectory(unzipFolder);
            }
            else
            {
                installer = downloadFilename;
            }
            
            List<string> unzippedFiles = zipper.ExtractAllFilesFromZip(downloadFilename, unzipFolder, "");
            foreach (string filename in unzippedFiles)
            {
                switch (Path.GetExtension(filename).ToLowerInvariant())
                {
                    case ".exe":
                        {

                            installer = filename;
                            break;
                        }

                    case ".doc":
                    case ".docx":
                    case ".rtf":
                    case ".txt":
                    case ".pdf":
                        {
                            string dirname = Path.GetDirectoryName(filename);
                            if (dirname.EndsWith("ShownReleaseNote"))
                            {
                                releaseNotes = filename;
                                break;
                            }
                            else
                            {
                                miniReleaseNote = filename;
                                break;
                            }
                        }
                }
            }
        }
        

		void RunInstaller()
		{

			if (! string.IsNullOrEmpty(installer))
			{
				try
				{
					System.Diagnostics.Process.Start(installer);
					System.Environment.Exit(0);
				}
				catch (System.ComponentModel.Win32Exception)
				{
				}
			}
		}

        public void SetInfo (bool upToDate, string message, string url, string username, string password)
        {
            if (upToDate
                && string.IsNullOrEmpty(message))
            {
                message = "Software version is up-to-date";
            }

            label.Text = message;

            downloadUrl = url;
            downloadUsername = username;
            downloadPassword = password;
            downloadAvailable = (! upToDate) && (! string.IsNullOrEmpty(url));
            downloadFilename = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.DesktopDirectory),
			                                Path.GetFileName(url ?? ""));

            UpdateButtons();
            DoSize();
		}

		public void ShowError ()
		{
			label.Text = "";

			downloadAvailable = false;

			UpdateButtons();
			DoSize();
		}

		public bool ShowFull
		{
			get
			{
				return showFull;
			}

			set
			{
				showFull = value;

				Height = (showFull ? 50 : 25);
			}
		}

		public bool Downloading
		{
			get
			{
				return downloading;
			}
		}
	}
}