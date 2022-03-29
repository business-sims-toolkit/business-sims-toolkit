using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using LibCore;

namespace ApplicationUi
{
	public class SplashScreen : Form
	{
		Image image;

		RectangleF progressBounds;
		Color progressColour;

		RectangleF versionBounds;
		Color versionColour;
		string version;

		ProgressProperties progress;

		public SplashScreen (string imageName, string title, Icon icon, string version, RectangleF progressBounds, Color progressColour, RectangleF versionBounds, Color versionColour)
		{
			foreach (var tryName in new [] { AppInfo.TheInstance.Location + @"\images\" + imageName, AppInfo.TheInstance.InstallLocation + @"\" + imageName })
			{
				if (File.Exists(tryName))
				{
					image = Repository.TheInstance.GetImage(tryName);
					break;
				}
			}

			this.progressBounds = progressBounds;
			this.progressColour = progressColour;

			this.versionBounds = versionBounds;
			this.versionColour = versionColour;
			this.version = version;

			Icon = icon;
			FormBorderStyle = FormBorderStyle.None;
			Text = title;
			StartPosition = FormStartPosition.CenterScreen;
			ClientSize = image.Size;

			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
		}

		public SplashScreen (string imageName, RectangleF progressBounds, Color progressColour, RectangleF versionBounds, Color versionColour)
			: this (imageName, Application.ProductName, Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetEntryAssembly().Location), Application.ProductVersion, progressBounds, progressColour, versionBounds, versionColour)
		{
		}

		public SplashScreen (RectangleF progressBounds, Color progressColour, RectangleF versionBounds, Color versionColour)
			: this ("v3SplashScreen.png", progressBounds, progressColour, versionBounds, versionColour)
		{
		}

		public ProgressProperties Progress
		{
			get => progress;

			set
			{
				progress = value;
				Refresh();
			}
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);
			Invalidate();
		}

		protected override void OnPaint (PaintEventArgs e)
		{
			var xScale = ClientSize.Width / (float) image.Width;
			var yScale = ClientSize.Height / (float) image.Height;
			var scale = Math.Min(xScale, yScale);
			var imageBounds = new RectangleF ((ClientSize.Width - (scale * image.Width)) / 2, (ClientSize.Height - (scale * image.Height)) / 2, image.Width * scale, image.Height * scale);
			e.Graphics.DrawImage(image, imageBounds);

			using (var font = ResizingUi.FontScalerExtensions.GetFontToFit(this, FontStyle.Bold, version, versionBounds.Size))
			using (var brush = new SolidBrush (versionColour))
			{
				e.Graphics.DrawString(version, font, brush, new RectangleF (versionBounds.Left * scale, versionBounds.Top * scale, versionBounds.Width * scale, versionBounds.Height * scale));
			}

			using (var brush = new SolidBrush (progressColour))
			{
				e.Graphics.FillRectangle(brush, new RectangleF (progressBounds.Left * scale, progressBounds.Top * scale, progressBounds.Width * scale * progress.Value / progress.Max, progressBounds.Height * scale));
			}
		}
	}
}
