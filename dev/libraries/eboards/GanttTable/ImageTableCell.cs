using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using LibCore;

using System.Xml;

using Charts;

namespace GanttTable
{
	/// <summary>
	/// Summary description for ImageTableCell.
	/// </summary>
	public class ImageTableCell : TextTableCell
	{
		protected Bitmap _bitmap;

		StringAlignment align, verticalAlign;

		public ImageTableCell ()
			: base (null)
		{
			align = StringAlignment.Center;
			verticalAlign = StringAlignment.Center;
		}

		public bool SetImageFile(string file)
		{
			if (file != "")
			{
				try
				{
					_bitmap = LibCore.Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images\\chart\\" + file);
				}
				catch { }
			}
			//
			return false;
		}

		public void SetImageAlign (string align, string verticalAlign)
		{
			switch (align.ToLower())
			{
				case "right":
					this.align = StringAlignment.Far;
					break;

				case "center":
				case "centre":
					this.align = StringAlignment.Center;
					break;

				case "left":
				default:
					this.align = StringAlignment.Near;
					break;
			}

			switch (verticalAlign.ToLower())
			{
				case "bottom":
					this.verticalAlign = StringAlignment.Far;
					break;

				case "center":
				case "centre":
					this.verticalAlign = StringAlignment.Center;
					break;

				case "top":
				default:
					this.verticalAlign = StringAlignment.Near;
					break;
			}
		}

		public override void Paint(DestinationDependentGraphics ddg)
		{
			if (! String.IsNullOrEmpty(text))
			{
				base.Paint(ddg);
			}
			else
			{
				WindowsGraphics g = (WindowsGraphics) ddg;

				Brush brush = Brushes.White;

				if (backBrush != null)
				{
					brush = backBrush;
				}

				g.Graphics.FillRectangle(brush, 0, 0, width, height);
				g.Graphics.DrawRectangle(border_pen, 0, 0, width - 1, height - 1);
			}

			if (_bitmap != null)
			{
				WindowsGraphics g = (WindowsGraphics)ddg;

				if (_bitmap != null)
				{
					Rectangle dest_rect = new Rectangle(0, 0, this.width, this.height);

					int x;
					switch (align)
					{
						case StringAlignment.Center:
							x = (width - _bitmap.Width) / 2;
							break;

						case StringAlignment.Far:
							x = width - _bitmap.Width;
							break;

						case StringAlignment.Near:
						default:
							x = 0;
							break;
					}

					int y;
					switch (verticalAlign)
					{
						case StringAlignment.Center:
							y = (height - _bitmap.Height) / 2;
							break;

						case StringAlignment.Far:
							y = height - _bitmap.Height;
							break;

						case StringAlignment.Near:
						default:
							y = 0;
							break;
					}

					g.Graphics.DrawImageUnscaled(_bitmap, x, y);
				}

				g.Graphics.DrawRectangle(border_pen,0,0,width-1, height-1);
			}
		}
	}
}