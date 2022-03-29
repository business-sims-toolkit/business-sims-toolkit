using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Diagnostics;

using System.Windows.Forms;

using System.Collections;
using System.Collections.Specialized;

using LibCore;
using Network;

using CommonGUI;

namespace Polestar_PM.TransScreen
{
	/// <summary>
	/// Summary description for CA_ServiceMonitorItem.
	/// </summary>
	public class MS_ServiceMonitorItem : ServiceMonitorItem
	{
		protected SolidBrush brush;

		public MS_ServiceMonitorItem(NodeTree tree, Node n, string functional_name) : base(tree, n, functional_name)
		{
			// : Fix for 3763 (service icons not created with correct colours).
			// By the time we get here, the base class may have changed colour to be other
			// than the default, so pay attention to it politely rather than overwriting.
			brush = new SolidBrush(this.BackColor);
		}

		public override void SetNormalBackground(Boolean RefreshRequired)
		{
			ForeColor = Color.SteelBlue;
			BackColor = Color.DarkSeaGreen;
			brush = new SolidBrush(this.BackColor);
			//
			if (RefreshRequired)
			{
				this.Refresh();
			}
		}

		public override void SetUpgradeBackground(Boolean RefreshRequired)
		{
			ForeColor = Color.Black;
			BackColor = Color.LightBlue;
			brush = new SolidBrush(this.BackColor);
			//
			if (RefreshRequired)
			{
				this.Refresh();
			}
		}

		public override void SetCreatedBackground(Boolean RefreshRequired)
		{
			ForeColor = Color.Black;
			BackColor = Color.LightGray;
			brush = new SolidBrush(this.BackColor);
			//
			if (RefreshRequired)
			{
				this.Refresh();
			}
		}

		public override void SetRetiredBackground(Boolean RefreshRequired)
		{
			ForeColor = Color.Black;
			BackColor = Color.LightGray;
			brush = new SolidBrush(this.BackColor);
			//
			if (RefreshRequired)
			{
				this.Refresh();
			}
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics g = e.Graphics;

//			g.SmoothingMode = SmoothingMode.None;
//			g.PixelOffsetMode = PixelOffsetMode.Default;
//			Rectangle textRect2 = new Rectangle(padLeft + 34+text_offset, padTop + 3, Size.Width - ((int)padLeft+icon_offset +30), Size.Height - padTop );
//			g.DrawRectangle(Pens.Purple,textRect2);
			
			g.SmoothingMode = SmoothingMode.AntiAlias;
			g.CompositingQuality = CompositingQuality.HighQuality;
			g.PixelOffsetMode = PixelOffsetMode.HighQuality;

			g.FillRectangle(brush, 0,0,this.Width, this.Height);
			g.DrawRectangle(Pens.LightGray,0,0,Width-1, Height-1);
		
			//Image back = backGraphic;

			RectangleF textRect = new RectangleF(padLeft + 34+text_offset, padTop + 3, Size.Width - ((int)padLeft+icon_offset +30), Size.Height - padTop );
			//RectangleF roundedRect = new RectangleF(padLeft, + padTop, Size.Width - padLeft - padRight, Size.Height - padTop - padBottom);
			
			//g.DrawImage(back, roundedRect.X + 26+box_offset, roundedRect.Y, 100, 28);
			g.DrawImage(icon, (int)padLeft+icon_offset, (int)padTop - 1 + 3 , 30, 30);
			
			if (desc != null && desc.Length > 0)
			{
				StringFormat sf = new StringFormat(StringFormatFlags.NoClip);
				sf.Trimming = StringTrimming.EllipsisCharacter;
				g.DrawString(desc, font, new SolidBrush(this.ForeColor), textRect, sf);
			}
		}
	}
}
