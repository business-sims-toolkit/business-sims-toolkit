using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

using CommonGUI;
using CoreUtils;
using LibCore;
using Network;

namespace ResizingUi.TimeDisplays
{
	public class TimerView : FlickerFreePanel
	{
		public TimerView (NodeTree model)
		{
			var fontSize = SkinningDefs.TheInstance.GetIntData("timer_font_size", 12);
			font = SkinningDefs.TheInstance.GetFont(fontSize, FontStyle.Bold);

			gameTimeColour = SkinningDefs.TheInstance.GetColorDataGivenDefault("timer_text_colour", Color.Black);
			preGameCountdownColour = SkinningDefs.TheInstance.GetColorDataGivenDefault("timer_text_colour", Color.Orange);

			prePlayNode = model.GetNamedNode("preplay_status");
			timeNode = model.GetNamedNode("CurrentTime");

			timeNode.AttributesChanged += timeNode_AttributesChanged;
			prePlayNode.AttributesChanged += timeNode_AttributesChanged;
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing)
			{
				timeNode.AttributesChanged -= timeNode_AttributesChanged;
				prePlayNode.AttributesChanged -= timeNode_AttributesChanged;
			}

			base.Dispose(disposing);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

			if (prePlayNode.GetBooleanAttribute("preplay_running").Equals(true))
			{
				DrawTimer(e.Graphics, prePlayNode.GetIntAttribute("time_left", 0), preGameCountdownColour);
			}
			else
			{
				DrawTimer(e.Graphics, timeNode.GetHmsAttribute("round_start_clock_time", 0) + timeNode.GetIntAttribute("seconds", 0), gameTimeColour);
			}
		}

		void DrawTimer(Graphics g, int time, Color timerColour)
		{
			using (Brush brush = new SolidBrush(timerColour))
			{
				g.DrawString(CONVERT.ToHmsFromSeconds(time), font, brush, Width / 2f, 5, new StringFormat { Alignment = StringAlignment.Center });
			}
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);
			Invalidate();
		}
		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);

			if (e.Button == MouseButtons.Left)
			{
				((Form)TopLevelControl).DragMove();
			}
		}

		void timeNode_AttributesChanged (Node sender, ArrayList attrs)
		{
			Invalidate();
		}
		
		readonly Font font;
		readonly Color gameTimeColour;
		readonly Color preGameCountdownColour;

		readonly Node timeNode;
		readonly Node prePlayNode;
	}
}
