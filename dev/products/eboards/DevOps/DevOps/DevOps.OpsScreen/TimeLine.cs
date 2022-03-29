using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

using CommonGUI;
using CoreUtils;
using Network;

namespace DevOps.OpsScreen
{
	internal class TimeLine : FlickerFreePanel
	{
		Node timeNode;
		Node prePlayStatus;
		Node prePlayTime;

		public TimeLine(NodeTree model)
		{
			timeNode = model.GetNamedNode("CurrentTime");
			prePlayStatus = model.GetNamedNode("preplay_status");
			prePlayTime = model.GetNamedNode("preplay_control");

			prePlayStatus.AttributesChanged += prePlayStatus_AttributesChanged;
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

		void prePlayStatus_AttributesChanged(Node sender, ArrayList attributes)
		{
			Invalidate();
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

		    var backColour = SkinningDefs.TheInstance.GetColorDataGivenDefault("timer_panel_back_colour", Color.Black);
            Color progressColour;
			int currentTime;
			int totalTime;
            

            if (prePlayStatus.GetBooleanAttribute("preplay_running").Equals(true))
			{
				progressColour = SkinningDefs.TheInstance.GetColorDataGivenDefault("timer_pre_game_countdown_colour", Color.Violet);
			    
                currentTime = prePlayStatus.GetIntAttribute("time_left", 0);
				totalTime = prePlayTime.GetIntAttribute("time_ref", 0);
			}
			else
			{
				progressColour = SkinningDefs.TheInstance.GetColorDataGivenDefault("timer_game_time_colour", Color.Cyan);
			    
                currentTime = timeNode.GetIntAttribute("seconds", 0);
				totalTime = timeNode.GetIntAttribute("round_duration_secs", 0);
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

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);
			Invalidate();
		}
	}
}
