using System;
using System.Collections.Generic;
using System.Text;

using System.Drawing;
using System.Windows.Forms;

using Network;

using LibCore;
using CoreUtils;

using CommonGUI;

namespace Polestar_PM.OpsGUI
{
	public class ProgramLabelPanel : FlickerFreePanel
	{
		Node program;

		Font font;

		public ProgramLabelPanel (Node program)
		{
			this.program = program;

			program.AttributesChanged += new Node.AttributesChangedEventHandler (program_AttributesChanged);

			font = ConstantSizeFont.NewFont(CoreUtils.SkinningDefs.TheInstance.GetData("fontname"), 10, FontStyle.Bold);
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing)
			{
				program.AttributesChanged -= new Node.AttributesChangedEventHandler (program_AttributesChanged);

				font.Dispose();
			}

			base.Dispose(disposing);
		}

		void program_AttributesChanged (Node sender, System.Collections.ArrayList attrs)
		{
			Invalidate();
		}

		protected override void OnPaint (PaintEventArgs e)
		{
			e.Graphics.FillRectangle(Brushes.White, 0, 0, Width, Height);
			e.Graphics.DrawRectangle(Pens.Black, 0, 0, Width - 1, Height - 1);

			using (Brush brush = new SolidBrush (Color.FromArgb(102, 204, 51)))
			{
				e.Graphics.FillRectangle(brush, 3, 3, Width - (2 * 3), Height - (2 * 3));
			}

			using (Brush brush = new SolidBrush (Color.FromArgb(179, 230, 153)))
			{
				e.Graphics.FillRectangle(brush, 3, 3, Width - (2 * 3), 2);
			}

			using (Brush brush = new SolidBrush (Color.FromArgb(82, 163, 41)))
			{
				e.Graphics.FillRectangle(brush, 3, Height - 3 - 2, Width - (2 * 3), 2);
			}

			using (StringFormat format = new StringFormat ())
			{
				format.Alignment = StringAlignment.Center;
				format.LineAlignment = StringAlignment.Center;

				RectangleF rectangle = new RectangleF (3, 3, Width - (2 * 3), Height - (2 * 3));

				e.Graphics.DrawString(program.GetAttribute("shortdesc"), font, Brushes.White, rectangle, format);
			}
		}
	}
}