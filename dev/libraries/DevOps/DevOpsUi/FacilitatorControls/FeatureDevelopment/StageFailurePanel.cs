using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using CommonGUI;
using CoreUtils;
using ResizingUi;
using ResizingUi.Extensions;

namespace DevOpsUi.FeatureDevelopment
{
	public class StageFailurePanel : FlickerFreePanel
	{
		public StageFailurePanel()
		{
			Visible = false;

			stageToFailureMessage = new Dictionary<ILinkedStage, string>();

			elapsedTime = 0;
			interval = 1000;
			duration = 5 * interval;
			timer = new Timer
			{
				Interval = interval
			};

			timer.Tick += timer_Tick;
		}


		public void AddStageFailureMessage(ILinkedStage stage, string failureMessage)
		{
			stage.StageStatusChanged += stage_StageStatusChanged;

			stageToFailureMessage[stage] = failureMessage;
		}

		Color? textOutlineColour;

		public Color? TextOutlineColour
		{
			set
			{
				textOutlineColour = value;
				Invalidate();
			}
		}

		FontStyle fontStyle = FontStyle.Bold;

		public FontStyle FontStyle
		{
			set => fontStyle = value;
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			var bounds = new RectangleF(0, 0, Width, Height);
			using (var backBrush = new SolidBrush(BackColor))
			{
				e.Graphics.FillRectangle(backBrush, bounds);
			}

			var fontSize = this.GetFontSizeInPixelsToFit(fontStyle, failureMessageText, new SizeF(Width, Height));

			using (var font = SkinningDefs.TheInstance.GetPixelSizedFont(fontSize, fontStyle))
			{
				var stringFormat = new StringFormat
				{
					Alignment = StringAlignment.Center,
					LineAlignment = StringAlignment.Center
				};

				if (textOutlineColour != null)
				{
					e.Graphics.DrawOutlinedString(failureMessageText, font.FontFamily, fontStyle, fontSize,
						bounds, textOutlineColour.Value, ForeColor, stringFormat);
				}
				else
				{
					using (var textBrush = new SolidBrush(ForeColor))
					{
						e.Graphics.DrawString(failureMessageText, font, textBrush, bounds, stringFormat);
					}
				}
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				foreach (var stage in stageToFailureMessage.Keys)
				{
					stage.StageStatusChanged -= stage_StageStatusChanged;
				}
			}

			base.Dispose(disposing);
		}

		protected override void OnSizeChanged(EventArgs e)
		{

			Invalidate();
		}

		void ShowPanel()
		{
			BringToFront();
			Invalidate();
			Show();

			elapsedTime = 0;
			timer.Start();

		}

		void HidePanel()
		{
			if (Visible)
			{
				Hide();
			}
		}

		void stage_StageStatusChanged(object sender, EventArgs e)
		{
			var stage = (ILinkedStage)sender;

			if (!stage.HasFailedStage || !stageToFailureMessage.ContainsKey(stage)) return;

			failureMessageText = stageToFailureMessage[stage];

			ShowPanel();
		}

		void timer_Tick(object sender, EventArgs e)
		{
			elapsedTime += interval;

			if (elapsedTime >= duration)
			{
				timer.Stop();
				HidePanel();
			}
		}


		string failureMessageText;

		readonly Dictionary<ILinkedStage, string> stageToFailureMessage;
		readonly Timer timer;
		readonly int interval;
		int elapsedTime;
		readonly int duration;

	}
}
