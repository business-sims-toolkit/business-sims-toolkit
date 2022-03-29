using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

using CommonGUI;
using CoreUtils;
using LibCore;
using Network;

namespace DevOps.OpsScreen
{
	public class TimerViewer : FlickerFreePanel
	{
		Font font;
		Color GameTimeColour;
		Color PreGameCountdownColour;

		NodeTree model;
		Node timeNode;
		Node prePlay;

		public TimerViewer(NodeTree model)
		{
			this.model = model;

			Setup();
		}

		void Setup()
		{
			int fontsize = SkinningDefs.TheInstance.GetIntData("timer_font_size", 12);
			font = SkinningDefs.TheInstance.GetFont(fontsize, FontStyle.Bold);

			GameTimeColour = SkinningDefs.TheInstance.GetColorDataGivenDefault("timer_text_colour", Color.Black);
			PreGameCountdownColour = SkinningDefs.TheInstance.GetColorDataGivenDefault("timer_text_colour", Color.Orange);

			prePlay = model.GetNamedNode("preplay_status");
			timeNode = model.GetNamedNode("CurrentTime");

			prePlay.AttributesChanged += prePlay_AttributesChanged;
			timeNode.AttributesChanged += timeNode_AttributesChanged;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				timeNode.AttributesChanged -= timeNode_AttributesChanged;
			}

			base.Dispose(disposing);
		}

		void timeNode_AttributesChanged(Node sender, ArrayList attributes)
		{
			Invalidate();
		}

		void prePlay_AttributesChanged(Node sender, ArrayList attributes)
		{
			Invalidate();
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

			if (prePlay.GetBooleanAttribute("preplay_running").Equals(true))
			{
				drawTimer(e.Graphics, prePlay.GetIntAttribute("time_left", 0), PreGameCountdownColour);
			}
			else
			{
				drawTimer(e.Graphics, timeNode.GetHmsAttribute("round_start_clock_time", 0) + timeNode.GetIntAttribute("seconds", 0), GameTimeColour);
			}
		}

		void drawTimer(Graphics g, int time, Color timerColour)
		{
			using (Brush brush = new SolidBrush(timerColour))
			{
				g.DrawString(CONVERT.ToHmsFromSeconds(time), font, brush, Width / 2, 5, new StringFormat { Alignment = StringAlignment.Center });
			}
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);
			Invalidate();
		}
		protected override void OnMouseDown (MouseEventArgs e)
		{
			base.OnMouseDown(e);

			if (e.Button == MouseButtons.Left)
			{
				((Form) TopLevelControl).DragMove();
			}
		}
	}
}