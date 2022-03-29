using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;

using LibCore;
using Network;
using CoreUtils;
using CommonGUI;


//using Logging;

namespace Polestar_PM.OpsGUI
{
	/// <summary>
	/// Summary description for RaceRaceScreenBanner.
	/// </summary>
	public class PM_OpsScreenBanner : OpsScreenBanner
	{
		public PM_OpsScreenBanner(NodeTree nt, bool showDay)
			:base (nt, showDay)
		{
			if (TimeBoldFont != null)
			{
				TimeBoldFont.Dispose();
				TimeBoldFont = this.MyDefaultSkinFontBold12;
				lblGameTime.Font = TimeBoldFont;
				lblPrePlayTime.Font = TimeBoldFont; 
			}
		}

		public override void setLabel()
		{
#if !PASSEXCEPTIONS
			try
			{
#endif
				int day = CONVERT.ParseInt(_CurrentDayNode.GetAttribute("Day"));
				this.lblGameDay.Text = "Day " + CONVERT.ToStr(day);

				int seconds = _CurrentTimeNode.GetIntAttribute("seconds",0);
				int minutes = (seconds/60);
				seconds -= minutes*60;

				//no padding got minutes and start at 1 
				//string minutesStr = CONVERT.ToStr(minutes+1).PadLeft(2,'0');
				string minutesStr = CONVERT.ToStr(minutes+1);
				string secondsStr = CONVERT.ToStr(seconds).PadLeft(2,'0');

				if (showDay && (hideDayAfter != -1))
				{
					lblGameDay.Visible = (day <= hideDayAfter);
				}

				if(hour == 0)
				{
					this.lblGameTime.Text = minutesStr + ":" + secondsStr;
				}
				else
				{
					string hourStr = CONVERT.ToStr(hour).PadLeft(2,'0');
					this.lblGameTime.Text = hourStr + ":" + minutesStr + ":" + secondsStr;
				}

#if !PASSEXCEPTIONS
			}
			catch(Exception)
			{
				//AppLogger.TheInstance.WriteLine("OpsScreenBanner::setLabel Level Exception : " + ex.Message + ":\r\n" + ex.StackTrace);
			}
#endif
		}

		public override void handleSize()
		{
			int timerOffset = SkinningDefs.TheInstance.GetIntData("timer_x_offset", 0);

			if (this.lblGameDay.Visible)
			{
				lblGameRound.Location = new Point(5,-1);
				lblGameRound.Size = new Size(100,25);
				//lblGameRound.BackColor = Color.Teal;
				lblGameDay.Location = new Point(115-20, -1);
				lblGameDay.Size = new Size(75, 25);
				//lblGameDay.BackColor = Color.LightCyan;

				//lblGamePhase.BackColor = Color.Peru;
				lblGamePhase.Location = new Point(170,0);
				lblGamePhase.Size = new Size(125,25);

				lblGameTime.Location = new Point(420-ClockOffset - timerOffset-125,-3);
				lblGameTime.Size = new Size(120+ClockOffset,28);
				//lblGameTime.BackColor = Color.YellowGreen;
				lblPrePlayTime.Location = new Point(420-ClockOffset - timerOffset-125,-3);
				lblPrePlayTime.Size = new Size(120+ClockOffset,28);
				//lblPrePlayTime.BackColor = Color.Tomato;
			}
			else
			{
				lblGameRound.Location = new Point(5,-1);
				lblGameRound.Size = new Size(100,25);

				//lblGamePhase.Location = new Point(200,0);
				//lblGamePhase.Size = new Size(210,25);
				
				lblGameTime.Location = new Point(420-ClockOffset - timerOffset,-3);
				lblGameTime.Size = new Size(120+ClockOffset,28);
				lblPrePlayTime.Location = new Point(420-ClockOffset - timerOffset,-3);
				lblPrePlayTime.Size = new Size(120+ClockOffset,28);
			}
		}

	}
}