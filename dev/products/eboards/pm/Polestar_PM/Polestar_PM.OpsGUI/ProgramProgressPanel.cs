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
	public class ProgramProgressPanel : FlickerFreePanel
	{
		Node program;

		Font font;
		Image manImage;
		Image clockImage;

		int stages = 8;
		int [] tabs;

		public ProgramProgressPanel (Node program)
		{
			this.program = program;

			if (program != null)
			{
				program.AttributesChanged += new Node.AttributesChangedEventHandler (program_AttributesChanged);
			}

			font = ConstantSizeFont.NewFont(CoreUtils.SkinningDefs.TheInstance.GetData("fontname"), 8, FontStyle.Bold);
			manImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images/panels/man.png");
			clockImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images/panels/clock.png");
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing)
			{
				if (program != null)
				{
					program.AttributesChanged -= new Node.AttributesChangedEventHandler (program_AttributesChanged);
				}

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
			if (tabs != null)
			{
				int verticalGap = 2;
				int stripHeight = (Height - verticalGap) / 2;

				e.Graphics.FillRectangle(Brushes.White, 0, 0, Width, stripHeight);
				e.Graphics.DrawRectangle(Pens.Black, 0, 0, Width - 1, stripHeight - 1);

				e.Graphics.FillRectangle(Brushes.White, 0, stripHeight + verticalGap, Width, stripHeight);
				e.Graphics.DrawRectangle(Pens.Black, 0, stripHeight + verticalGap, Width - 1, Height - 1 - (stripHeight + verticalGap));

				e.Graphics.DrawImage(manImage, 2, 1, stripHeight - 2, stripHeight - 2);
				e.Graphics.DrawImage(clockImage, 2, stripHeight + verticalGap + 1, stripHeight - 2, stripHeight - 2);

				string completedStageString = program.GetAttribute("stage_completed");
				int completedStage = -1;
				if (completedStageString != "")
				{
					completedStage = ((int) completedStageString[0]) - ((int) 'A');
				}

				string workingStageString = program.GetAttribute("stage_working");
				int workingStage = -1;
				if (workingStageString != "")
				{
					workingStage = ((int) workingStageString[0]) - ((int) 'A');
				}

				for (int stage = 0; stage < stages; stage++)
				{
					if (stage <= Math.Max(workingStage, completedStage))
					{
						Color colour = Color.FromArgb(102, 204, 51);
						Color topColour = Color.FromArgb(179, 230, 153);
						Color bottomColour = Color.FromArgb(82, 163, 41);

						int x;
						if (stage == 0)
						{
							x = 3 + stripHeight;
						}
						else
						{
							x = tabs[stage - 1];
						}

						int width;
						if (stage == tabs.Length)
						{
							width = Width - 1 - x;
						}
						else
						{
							width = tabs[stage] - x;
						}

						int gap = 2;
						x += gap;
						width -= (2 * gap);

						Rectangle topRectangle = new Rectangle (x, 3, width, stripHeight - (2 * 3));
						Rectangle bottomRectangle = new Rectangle (x, stripHeight + verticalGap + 3, width, stripHeight - (2 * 3));

						RectangleF topRectangleF = new RectangleF (topRectangle.Left, topRectangle.Top, topRectangle.Width, topRectangle.Height);
						RectangleF bottomRectangleF = new RectangleF (bottomRectangle.Left, bottomRectangle.Top, bottomRectangle.Width, bottomRectangle.Height);

						bool stageInProgress = (stage == workingStage);

						int staffAssigned = program.GetIntAttribute("resources", 0);
						int staffRequested = staffAssigned;

						if (stageInProgress)
						{
							if (program.GetBooleanAttribute("over_budget", false))
							{
								colour = Color.FromArgb(183, 43, 33);
								topColour = Color.FromArgb(219, 149, 144);
								bottomColour = Color.FromArgb(146, 34, 28);

								staffAssigned = 0;
							}
							else
							{
								colour = Color.FromArgb(0, 132, 201);
								topColour = Color.FromArgb(128, 194, 228);
								bottomColour = Color.FromArgb(0, 106, 161);
							}
						}

						using (Brush brush = new SolidBrush(colour))
						{
							e.Graphics.FillRectangle(brush, topRectangle);
							e.Graphics.FillRectangle(brush, bottomRectangle);
						}

						using (Brush brush = new SolidBrush(topColour))
						{
							e.Graphics.FillRectangle(brush, topRectangle.Left, 3, topRectangle.Width, 2);
							e.Graphics.FillRectangle(brush, topRectangle.Left, stripHeight + verticalGap + 3, topRectangle.Width, 2);
						}

						using (Brush brush = new SolidBrush(bottomColour))
						{
							e.Graphics.FillRectangle(brush, bottomRectangle.Left, stripHeight - 3 - 2, bottomRectangle.Width, 2);
							e.Graphics.FillRectangle(brush, bottomRectangle.Left, stripHeight + verticalGap + stripHeight - 3 - 2, bottomRectangle.Width, 2);
						}

						if (stageInProgress)
						{
							StringFormat format = new StringFormat();
							format.Alignment = StringAlignment.Center;
							format.LineAlignment = StringAlignment.Center;

							e.Graphics.DrawString(CONVERT.ToStr(staffAssigned) + "/" + CONVERT.ToStr(staffRequested), font, Brushes.White, topRectangleF, format);
							e.Graphics.DrawString(CONVERT.ToStr(program.GetIntAttribute("calendar_days_left_in_stage", 0)), font, Brushes.White, bottomRectangleF, format);
						}
					}
				}
			}
		}

		internal void SetTabs (int [] tabs)
		{
			this.tabs = tabs;
		}
	}
}