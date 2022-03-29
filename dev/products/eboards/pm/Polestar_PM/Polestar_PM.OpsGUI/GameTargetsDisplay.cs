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

namespace Polestar_PM.OpsGUI
{
	/// <summary>
	/// Summary description for ProjectedRevenueView.
	/// </summary>
	public class GameTargetsDisplay : FlickerFreePanel
	{
		static System.Globalization.CultureInfo myCI_enGB = new System.Globalization.CultureInfo( "en-GB", false );

		protected Font MyDefaultSkinFontNormal10 = null;
		protected Font MyDefaultSkinFontBold10 = null;
		protected Font MyDefaultSkinFontBold12 = null;
		protected Font MyDefaultSkinFontBold14 = null;
		protected Font MyDefaultSkinFontBold24 = null;
		protected Image backImage = null;
		protected bool showempty = false;
		
		/// <summary>
		/// Show Availability / Revenue
		/// </summary>
		/// <param name="model"></param>
		public GameTargetsDisplay(NodeTree model, bool IsTrainingGame, bool ShowEmpty)
		{
			showempty = ShowEmpty;

			//Just display in GB number format for the time being 
			string fontname =  SkinningDefs.TheInstance.GetData("fontname");
			MyDefaultSkinFontNormal10 = ConstantSizeFont.NewFont(fontname,10f);
			MyDefaultSkinFontBold10 = ConstantSizeFont.NewFont(fontname,10f,FontStyle.Bold);
			MyDefaultSkinFontBold12 = ConstantSizeFont.NewFont(fontname,12f);//,FontStyle.Bold);
			MyDefaultSkinFontBold14 = ConstantSizeFont.NewFont(fontname,14f,FontStyle.Bold);
			MyDefaultSkinFontBold24 = ConstantSizeFont.NewFont(fontname,24f,FontStyle.Bold);

			if (IsTrainingGame)
			{
				backImage = loadImage("t_BusinessTargetsPanel.png");
			}
			else
			{
				backImage = loadImage("BusinessTargetsPanel.png");
			}

			if (showempty==false)
			{
				//Build any none empty controls in here 
			}
		}

		/// <summary>
		/// Dispose ...
		/// </summary>
		new public void Dispose()
		{
			//get rid of the Font
			if (MyDefaultSkinFontNormal10 != null)
			{
				MyDefaultSkinFontNormal10.Dispose();
				MyDefaultSkinFontNormal10 = null;
			}
			if (MyDefaultSkinFontBold10 != null)
			{
				MyDefaultSkinFontBold10.Dispose();
				MyDefaultSkinFontBold10 = null;
			}
			//get rid of the Font
			if (MyDefaultSkinFontBold12 != null)
			{
				MyDefaultSkinFontBold12.Dispose();
				MyDefaultSkinFontBold12 = null;
			}
			//get rid of the Font
			if (MyDefaultSkinFontBold14 != null)
			{
				MyDefaultSkinFontBold14.Dispose();
				MyDefaultSkinFontBold14 = null;
			}
			//get rid of the Font
			if (MyDefaultSkinFontBold24 != null)
			{
				MyDefaultSkinFontBold24.Dispose();
				MyDefaultSkinFontBold24 = null;
			}
		}

		public Image loadImage(string imagename)
		{
			return Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images\\panels\\"+imagename);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			//Setup the Quality and Rendering Options 
			e.Graphics.SmoothingMode = SmoothingMode.None;
			e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
			e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
			SizeF textsize = new SizeF(0,0);
				
			try 
			{
				if (backImage != null)
				{
					e.Graphics.DrawImage(backImage,0,0,this.Width,this.Height);
				}
				Brush textBrush = new SolidBrush(Color.FromArgb(51,51,51));  //dark color deep Gray #333333
				e.Graphics.DrawString("Business",MyDefaultSkinFontBold12,textBrush,5,5-2);
				textBrush.Dispose();

				if (this.showempty == false)
				{
					//draw what you need
				}

			}
			catch (Exception evc)
			{
				string st = evc.Message;
			}
		}

	}
}
