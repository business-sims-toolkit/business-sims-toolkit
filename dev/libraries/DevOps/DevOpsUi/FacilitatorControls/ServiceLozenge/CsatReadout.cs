using System;
using System.Drawing;
using System.Windows.Forms;

using Algorithms;
using CommonGUI;
using CoreUtils;
using LibCore;
using Network;
using ResizingUi;

namespace DevOpsUi.FacilitatorControls.ServiceLozenge
{
	class CsatReadoutPanel : FlickerFreePanel
	{
		public CsatReadoutPanel(Node serviceNode)
		{
			this.serviceNode = serviceNode;

			serviceNode.AttributesChanged += serviceNode_AttributesChanged;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				serviceNode.AttributesChanged -= serviceNode_AttributesChanged;
			}

			base.Dispose(disposing);
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			Invalidate();
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			var bounds = new RectangleF(0, 0, Width, Height);

			var csatPercent = serviceNode?.GetIntAttribute("csat_percent", 100) ?? 80;

			var csatTextBounds = bounds.ExpandByFraction(-0.35f);

			var fontSize = this.GetFontSizeInPixelsToFit(FontStyle.Regular, "100%", csatTextBounds.Size);

			using (var backBrush = new SolidBrush(BackColor))
			{
				e.Graphics.FillRectangle(backBrush, bounds);
			}

			using (var textBrush = new SolidBrush(SkinningDefs.TheInstance.GetColorData("csat_readout_text_colour", Color.Black)))
			{
				using (var font = SkinningDefs.TheInstance.GetPixelSizedFont(fontSize))
				{
					e.Graphics.DrawString(CONVERT.ToPaddedPercentageString(csatPercent, 0), font, textBrush,
						bounds.AlignRectangle(csatTextBounds.Size), new StringFormat
						{
							Alignment = StringAlignment.Center,
							LineAlignment = StringAlignment.Center
						});
				}
			}
		}

		void serviceNode_AttributesChanged(Node sender, System.Collections.ArrayList attrs)
		{
			Invalidate();
		}

		readonly Node serviceNode;
	}
}
