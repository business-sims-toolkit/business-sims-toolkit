using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Algorithms;
using CommonGUI;
using CoreUtils;
using LibCore;
using ResizingUi;

namespace TransitionScreens
{
	public class ProgressLozenge : FlickerFreePanel
	{
		ProgressLozengeStatus status;
		List<string> text;
		bool isActive;

		public ProgressLozengeStatus Status
		{
			get => status;

			set
			{
				status = value;

				switch (status)
				{
					case ProgressLozengeStatus.NotStarted:
						BackColor = SkinningDefs.TheInstance.GetColorData("transition_project_progress_colour_unstarted");
						ForeColor = Color.White;
						break;

					case ProgressLozengeStatus.Running:
						BackColor = SkinningDefs.TheInstance.GetColorData("transition_project_progress_colour_running");
						ForeColor = Color.Black;
						break;

					case ProgressLozengeStatus.Failed:
						BackColor = SkinningDefs.TheInstance.GetColorData("transition_project_progress_colour_failed");
						ForeColor = Color.White;
						break;

					case ProgressLozengeStatus.Completed:
						BackColor = SkinningDefs.TheInstance.GetColorData("transition_project_progress_colour_succeeded");
						ForeColor = Color.White;
						break;
				}

				Invalidate();
			}
		}

		public ProgressLozenge ()
		{
			status = ProgressLozengeStatus.NotStarted;
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);
			Invalidate();
		}

		public bool IsActive
		{
			get => isActive;

			set
			{
				isActive = value;
				Invalidate();
			}
		}

		protected override void OnPaint (PaintEventArgs e)
		{
			base.OnPaint(e);

			using (var backBrush = new SolidBrush (Maths.Lerp(isActive ? 0 : 0.75, BackColor, Parent.BackColor)))
			{
				e.Graphics.FillRectangle(backBrush, 0, 0, Width, Height);
			}

			if (text != null)
			{
				using (var textBrush = new SolidBrush (ForeColor))
				using (var font = SkinningDefs.TheInstance.GetPixelSizedFont(Math.Min(Width, Height) / 3, FontStyle.Bold))
				{
					var normalisedText = TextWrapper.NormaliseLines(text.ToArray());
					var wrappedText = TextWrapper.WordWrapText(e.Graphics, font, Width, normalisedText);

					bool fontTooBig = false;
					float? fontSizeNeeded = null;
					foreach (var line in wrappedText)
					{
						if (e.Graphics.MeasureString(line, font).Width > Width)
						{
							fontTooBig = true;

							float fontSizeForThisLine = this.GetFontSizeInPixelsToFit(font.Name, font.Style, line, new SizeF(Width, Height));

							if (fontSizeNeeded == null)
							{
								fontSizeNeeded = fontSizeForThisLine;
							}
							else
							{
								fontSizeNeeded = Math.Min(fontSizeForThisLine, fontSizeNeeded.Value);
							}
						}
					}

					if (fontTooBig)
					{
						using (var emergencyFont = SkinningDefs.TheInstance.GetPixelSizedFont(fontSizeNeeded.Value, font.Style))
						{
							TextWrapper.DrawText(e.Graphics, emergencyFont, textBrush, wrappedText, new RectangleF (0, 0, Width, Height), StringAlignment.Center, StringAlignment.Center);
						}
					}
					else
					{
						TextWrapper.DrawText(e.Graphics, font, textBrush, wrappedText, new RectangleF (0, 0, Width, Height), StringAlignment.Center, StringAlignment.Center);
					}
				}
			}
		}
		
		public void SetText (params string [] lines)
		{
			text = new List<string> (lines);
			Invalidate();
		}

		public void SetTimer (int daysLeft)
		{
			SetText(CONVERT.ToStr(daysLeft));
		}
	}
}