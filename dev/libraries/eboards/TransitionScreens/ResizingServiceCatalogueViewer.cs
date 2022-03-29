using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CommonGUI;
using CoreUtils;
using Network;

namespace TransitionScreens
{
	public class ResizingServiceCatalogueViewer : FlickerFreePanel
	{
		Node servicesNode;
		Node retiredServicesNode;

		List<ResizingServiceCatalogueLozenge> lozenges;
		int titleHeight;
		int leading;

		public ResizingServiceCatalogueViewer (Node servicesNode, Node retiredServicesNode)
		{
			this.servicesNode = servicesNode;
			this.retiredServicesNode = retiredServicesNode;

			servicesNode.ChildAdded += servicesNode_ChildAdded;
			retiredServicesNode.ChildAdded += retiredServicesNode_ChildAdded;

			lozenges = new List<ResizingServiceCatalogueLozenge> ();

			foreach (Node service in servicesNode.GetChildrenOfType("biz_service"))
			{
				var lozenge = new ResizingServiceCatalogueLozenge (service);
				Controls.Add(lozenge);
				lozenges.Add(lozenge);
				lozenge.Changed += lozenge_Changed;
				lozenge.DesiredFontSizeChanged += lozenge_DesiredFontSizeChanged;
			}

			foreach (Node service in retiredServicesNode.GetChildrenOfType("retired_biz_service"))
			{
				var lozenge = new ResizingServiceCatalogueLozenge (service);
				Controls.Add(lozenge);
				lozenges.Add(lozenge);
				lozenge.Changed += lozenge_Changed;
				lozenge.DesiredFontSizeChanged += lozenge_DesiredFontSizeChanged;
			}

			DoSize();
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing)
			{
				servicesNode.ChildAdded -= servicesNode_ChildAdded;
				retiredServicesNode.ChildAdded -= retiredServicesNode_ChildAdded;

				foreach (var lozenge in lozenges)
				{
					lozenge.Dispose();
				}
			}

			base.Dispose(disposing);
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);
			DoSize();
		}

		void DoSize ()
		{
			titleHeight = Height / 15;
			leading = titleHeight / 8;

			DoLayout();
		}

		void servicesNode_ChildAdded (Node parent, Node child)
		{
			var lozenge = new ResizingServiceCatalogueLozenge (child);
			Controls.Add(lozenge);
			lozenges.Add(lozenge);
			lozenge.Changed += lozenge_Changed;
			lozenge.DesiredFontSizeChanged += lozenge_DesiredFontSizeChanged;

			DoLayout();
		}

		void retiredServicesNode_ChildAdded (Node parent, Node child)
		{
			var lozenge = new ResizingServiceCatalogueLozenge (child);
			Controls.Add(lozenge);
			lozenges.Add(lozenge);
			lozenge.Changed += lozenge_Changed;
			lozenge.DesiredFontSizeChanged += lozenge_DesiredFontSizeChanged;

			DoLayout();
		}

		void lozenge_Changed (object sender, EventArgs args)
		{
			DoLayout();
		}

		void DoLayout ()
		{
			int margin = Width / 40;
			var columnGap = Width / 40;
			var rowGap = columnGap / 4;

			var columns = 3;
			var rows = 10;

			int y = titleHeight + leading;
			int x = margin;

			var lozengeSize = new Size ((Width - (2 * margin) - ((columns - 1) * columnGap)) / columns, (Height - y - ((rows - 1) * rowGap)) / rows);

			lozenges.Sort();
			foreach (var lozenge in lozenges)
			{
				lozenge.Bounds = new Rectangle (x, y, lozengeSize.Width, lozengeSize.Height);
				x = lozenge.Right + columnGap;
				if ((x + lozengeSize.Width) > (Width - margin))
				{
					x = margin;
					y = lozenge.Bottom + rowGap;
				}
			}

			Invalidate();

			var fontSize = lozenges.Min(l => l.DesiredFontSize);
			foreach (var lozenge in lozenges)
			{
				lozenge.FontSize = fontSize;
			}
		}

		protected override void OnPaint (PaintEventArgs e)
		{
			base.OnPaint(e);

			string title = "Service Catalog";
			var titleBox = new RectangleF (0, 0, Width, titleHeight);
			float titleSize = ResizingUi.FontScalerExtensions.GetFontSizeInPixelsToFit(this, FontStyle.Regular, title, titleBox.Size);
			using (var font = SkinningDefs.TheInstance.GetPixelSizedFont(titleSize, FontStyle.Regular))
			{
				e.Graphics.DrawString(title, font, Brushes.LightGray, titleBox, new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Near });
			}
		}

		void lozenge_DesiredFontSizeChanged (object sender, EventArgs args)
		{
			DoLayout();
		}
	}
}