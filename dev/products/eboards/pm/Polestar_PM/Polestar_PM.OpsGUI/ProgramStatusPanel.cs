using System;
using System.Collections.Generic;
using System.Text;

using System.Drawing;
using System.Windows.Forms;

using Network;

using CommonGUI;

using LibCore;

namespace Polestar_PM.OpsGUI
{
	public class ProgramStatusPanel : FlickerFreePanel
	{
		Node program;
		Font bigFont, smallFont;

		int margin = 3;

		public ProgramStatusPanel (Node program)
		{
			this.program = program;

			program.AttributesChanged += new Node.AttributesChangedEventHandler (program_AttributesChanged);

			bigFont = ConstantSizeFont.NewFont(CoreUtils.SkinningDefs.TheInstance.GetData("fontname"), 10, FontStyle.Bold);
			smallFont = ConstantSizeFont.NewFont(CoreUtils.SkinningDefs.TheInstance.GetData("fontname"), 8, FontStyle.Bold);
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing)
			{
				program.AttributesChanged -= new Node.AttributesChangedEventHandler (program_AttributesChanged);

				bigFont.Dispose();
				smallFont.Dispose();
			}

			base.Dispose(disposing);
		}

		void program_AttributesChanged (Node sender, System.Collections.ArrayList attrs)
		{
			Invalidate();
		}

		protected override void OnPaint (PaintEventArgs e)
		{
			base.OnPaint(e);

			int budget = program.GetIntAttribute("budget", 0);
			int spend = program.GetIntAttribute("spend", 0);
			string stage = program.GetAttribute("projected_stage_complete");
			bool overBudget = program.GetBooleanAttribute("over_budget", false);
			bool finished = (program.GetAttribute("stage_completed") == "H");

			e.Graphics.FillRectangle(Brushes.White, 0, 0, Width, Height);
			e.Graphics.DrawRectangle(Pens.Black, 0, 0, Width - 1, Height - 1);

			int division = Width / 2;
			int centralGap = 2;
			int verticalGap = 2;
			int stripHeight = (Height - verticalGap) / 2;

			Rectangle topRectangle = new Rectangle (margin, 2, division - margin - centralGap, stripHeight - 3);
			Rectangle bottomRectangle = new Rectangle (margin, stripHeight + verticalGap + 1, division - margin - centralGap, stripHeight - 3);

			Rectangle rightRectangle = new Rectangle (division + centralGap, 3, Width - margin - (division + centralGap), Height - (2 * 3));

			bool showSolid = false;
			Color top = Color.White;
			Color middle = Color.White;
			Color bottom = Color.White;
			if (finished)
			{
				showSolid = true;
				middle = Color.FromArgb(102, 204, 51);
				top = Color.FromArgb(179, 230, 153);
				bottom = Color.FromArgb(82, 163, 41);
			}
			else if (overBudget)
			{
				showSolid = true;
				middle = Color.FromArgb(183, 43, 33);
				top = Color.FromArgb(219, 149, 144);
				bottom = Color.FromArgb(146, 34, 28);
			}

			DrawBox(e.Graphics, topRectangle, FormatThousands(budget), smallFont, showSolid, middle, top, bottom);
			DrawBox(e.Graphics, bottomRectangle, FormatThousands(spend), smallFont, showSolid, middle, top, bottom);

			DrawBox(e.Graphics, rightRectangle, stage, bigFont, finished, Color.FromArgb(102, 204, 51), Color.FromArgb(179, 230, 153), Color.FromArgb(82, 163, 41));
		}

		string FormatThousands (int a)
		{
			if (a < 0)
			{
				return "-" + FormatThousands(Math.Abs(a));
			}

			string raw = LibCore.CONVERT.ToStr(a);

			StringBuilder builder = new StringBuilder("");
			int digits = 0;
			for (int character = raw.Length - 1; character >= 0; character--)
			{
				builder.Insert(0, raw[character]);
				digits++;

				if (((digits % 3) == 0) && (character > 0))
				{
					builder.Insert(0, ",");
				}
			}

			return builder.ToString();
		}

		void DrawBox (Graphics graphics, Rectangle rectangle, string text, Font font, bool showSolid, Color body, Color top, Color bottom)
		{
			if (showSolid)
			{
				using (Brush brush = new SolidBrush (body))
				{
					graphics.FillRectangle(brush, rectangle);
				}

				using (Brush brush = new SolidBrush (top))
				{
					graphics.FillRectangle(brush, rectangle.Left, rectangle.Top, rectangle.Width, 2);
				}

				using (Brush brush = new SolidBrush (bottom))
				{
					graphics.FillRectangle(brush, rectangle.Left, rectangle.Bottom - 2, rectangle.Width, 2);
				}
			}
			else
			{
				graphics.FillRectangle(Brushes.White, rectangle);
				graphics.DrawRectangle(Pens.Black, rectangle);
			}

			using (StringFormat format = new StringFormat ())
			{
				RectangleF rectangleF = new RectangleF (rectangle.Left, rectangle.Top, rectangle.Width, rectangle.Height);

				format.Alignment = StringAlignment.Center;
				format.LineAlignment = StringAlignment.Center;

				Color textColour = Color.Black;
				if (showSolid)
				{
					textColour = Color.White;
				}

				using (Brush brush = new SolidBrush (textColour))
				{
					graphics.DrawString(text, font, brush, rectangleF, format);
				}
			}
		}
	}
}