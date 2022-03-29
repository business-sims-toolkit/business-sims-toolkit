using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;
using CommonGUI;
using GameManagement;
using LibCore;
using Network;

namespace ResizingUi
{
	public class ClockPanel : FlickerFreePanel
	{
		NetworkProgressionGameFile gameFile;
		Node preplayNode;
		Node timeNode;

		int? round;
		GameFile.GamePhase? phase;

		public ClockPanel (NetworkProgressionGameFile gameFile)
		{
			this.gameFile = gameFile;

			preplayNode = gameFile.NetworkModel.GetNamedNode("preplay_status");
			preplayNode.AttributesChanged += preplayNode_AttributesChanged;

			timeNode = gameFile.NetworkModel.GetNamedNode("CurrentTime");
			timeNode.AttributesChanged += timeNode_AttributesChanged;
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing)
			{
				preplayNode.AttributesChanged -= preplayNode_AttributesChanged;
				timeNode.AttributesChanged -= timeNode_AttributesChanged;
			}

			base.Dispose(disposing);
		}

		void preplayNode_AttributesChanged (Node sender, ArrayList attributes)
		{
			Invalidate();
		}

		void timeNode_AttributesChanged (Node sender, ArrayList attributes)
		{
			Invalidate();
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);

			Invalidate();
		}

		protected override void OnPaint (PaintEventArgs e)
		{
			base.OnPaint(e);

			// Awful design of NetworkTransitionGameFile means that CurrentRound increments at the end of this round, not the start of the next.
			if ((round == null) || gameFile.CurrentPhaseHasStarted)
			{
				round = gameFile.CurrentRound;
			}

			if ((phase == null) || gameFile.CurrentPhaseHasStarted)
			{
				phase = gameFile.CurrentPhase;
			}

			var roundBounds = new RectangleF (0, 0, Width, Height / 4);
			var phaseBounds = new RectangleF (0, roundBounds.Bottom, Width, Height / 6);
			var clockBounds = new RectangleF (0, phaseBounds.Bottom, Width, Height - phaseBounds.Bottom);

			var roundString = $"Round {round}";

			string phaseString;
			string clockString;
			Brush clockBrush;
			if (phase == GameFile.GamePhase.OPERATIONS)
			{
				phaseString = "Game Screen";

				if (preplayNode.GetBooleanAttribute("preplay_running", false))
				{
					clockString = CONVERT.FormatTime(preplayNode.GetIntAttribute("time_left", 0));
					clockBrush = Brushes.Orange;
				}
				else
				{
					clockString = CONVERT.FormatTimeHms(timeNode.GetIntAttribute("seconds", 0) + timeNode.GetHmsAttribute("round_start_clock_time", 0));
					clockBrush = Brushes.Black;
				}
			}
			else
			{
				phaseString = "Transition Screen";
				clockString = CONVERT.FormatTime(timeNode.GetIntAttribute("seconds", 0)) + " (Day " + gameFile.NetworkModel.GetNamedNode("CurrentDay").GetIntAttribute("day", 1) + ")";
				clockBrush = Brushes.Black;
			}

			using (var font = this.GetFontToFit(FontStyle.Bold, roundString, roundBounds.Size))
			{
				e.Graphics.DrawString(roundString, font, Brushes.Black, roundBounds, new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Center });
			}

			using (var font = this.GetFontToFit(FontStyle.Regular, phaseString, phaseBounds.Size))
			{
				e.Graphics.DrawString(phaseString, font, Brushes.Black, phaseBounds, new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Center });
			}

			using (var font = this.GetFontToFit(FontStyle.Bold, clockString, clockBounds.Size))
			{
				e.Graphics.DrawString(clockString, font, clockBrush, clockBounds, new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Center });
			}
		}
	}
}