using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Data;
using System.Windows.Forms;
using System.Xml;
using System.Globalization;
using System.Drawing.Text;
using System.IO;
using System.Runtime.InteropServices;

using LibCore;
using Network;
using CoreUtils;
using CommonGUI;

namespace OpsGUI
{
	/// <summary>
	/// No Longer Used
	/// </summary>
	public class StaticImageDisplay: FlickerFreePanel
	{
		protected Image mainImage = null;
		/// <summary>
		/// Show Availability / Revenue
		/// </summary>
		/// <param name="model"></param>
		public StaticImageDisplay(NodeTree model, bool isTrainingGame)
		{
			mainImage = loadImage("projects_status_back.png");
		}

		public Image loadImage(string imagename)
		{
			return Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images\\panels\\"+imagename);
		}

		/// <summary>
		/// Dispose ...
		/// </summary>
		public new void Dispose()
		{
			//get rid of the Font
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			//Setup the Quality and Rendering Options 
			e.Graphics.SmoothingMode = SmoothingMode.None;
			e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
			e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
			int start_x =0;

			e.Graphics.FillRectangle(Brushes.Black,0,0,this.Width, this.Height);
			if (mainImage != null)
			{
				e.Graphics.DrawImage(mainImage,0,0,this.Width, this.Height);
			}
		}

	}
}
