using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

using CommonGUI;
using CoreUtils;
using Network;

namespace ResizingUi.TimeDisplays
{
	public class TimeLine : FlickerFreePanel
	{
		public TimeLine (NodeTree model)
		{
			timeNode = model.GetNamedNode("CurrentTime");
			prePlayStatusNode = model.GetNamedNode("preplay_status");

			prePlayTotalDuration = model.GetNamedNode("preplay_control").GetIntAttribute("time_ref", 0);
			roundDuration = timeNode.GetIntAttribute("round_duration_secs", 0);

			timeNode.AttributesChanged += node_AttributesChanged;
			prePlayStatusNode.AttributesChanged += node_AttributesChanged;
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing)
			{
				timeNode.AttributesChanged -= node_AttributesChanged;
				prePlayStatusNode.AttributesChanged -= node_AttributesChanged;
			}

			base.Dispose(disposing);
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);
			Invalidate();
		}

		protected override void OnPaint (PaintEventArgs e)
		{
			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

			var backColour = SkinningDefs.TheInstance.GetColorDataGivenDefault("timer_panel_back_colour", Color.Black);
			Color progressColour;
			int currentTime;
			int totalTime;


			if (prePlayStatusNode.GetBooleanAttribute("preplay_running").Equals(true))
			{
				progressColour = SkinningDefs.TheInstance.GetColorDataGivenDefault("timer_pre_game_countdown_colour", Color.Violet);

				currentTime = prePlayStatusNode.GetIntAttribute("time_left", 0);
				totalTime = prePlayTotalDuration;
			}
			else
			{
				progressColour = SkinningDefs.TheInstance.GetColorDataGivenDefault("timer_game_time_colour", Color.Cyan);

				currentTime = timeNode.GetIntAttribute("seconds", 0);
				totalTime = roundDuration;
			}

			using (var backPen = new Pen(backColour, Height * 2))
			{
				e.Graphics.DrawLine(backPen, 0, 0, Width, 0);
			}

			if (totalTime != 0)
			{
				var progress = (int)(((float)currentTime / totalTime) * Width);
				using (var pen = new Pen(progressColour, Height * 2))
				{
					e.Graphics.DrawLine(pen, 0, 0, progress, 0);
				}
			}
		}
		
		void node_AttributesChanged (Node sender, ArrayList attrs)
		{
			Invalidate();
		}

		readonly Node timeNode;
		readonly Node prePlayStatusNode;

		readonly int prePlayTotalDuration;
		readonly int roundDuration;
	}
}
