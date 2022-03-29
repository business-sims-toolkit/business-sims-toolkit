using System;
using System.Text;
using System.Drawing;
using LibCore;
using Network;
using CoreUtils;
using CommonGUI;

namespace Cloud.OpsScreen
{
	public class CloudOpsScreenBanner : OpsScreenBanner
	{
		public CloudOpsScreenBanner (NodeTree nt, bool showDay)
			: base(nt, showDay)
		{
			if (TimeBoldFont != null)
			{
				TimeBoldFont.Dispose();
				TimeBoldFont = MyDefaultSkinFontBold12;
				lblGameTime.Font = TimeBoldFont;
				lblPrePlayTime.Font = TimeBoldFont;
			}
		}

		public override void setLabel ()
		{
#if !PASSEXCEPTIONS
			try
			{
#endif

				lblGameDay.Text = "";
				if (lblPrePlayTime.Visible)
				{
					lblPrePlayTime.TextAlign = ContentAlignment.MiddleLeft;
				}
				else
				{
					int seconds = _CurrentTimeNode.GetIntAttribute("seconds", 0);
					lblGameTime.TextAlign = ContentAlignment.MiddleLeft;

					StringBuilder display_value = new StringBuilder("Trading Period ");
					display_value.Append(CONVERT.ToStr((seconds / 60) + 1));
					display_value.Append(" [");

					if ((seconds % 60) < 10)
					{
						display_value.Append("0");
					}
					display_value.Append(CONVERT.ToStr(seconds % 60));
					display_value.Append("]");
					lblGameTime.Text = display_value.ToString();
				}

#if !PASSEXCEPTIONS
			}
			catch (Exception)
			{
				//AppLogger.TheInstance.WriteLine("OpsScreenBanner::setLabel Level Exception : " + ex.Message + ":\r\n" + ex.StackTrace);
			}
#endif
		}

		public override void handleSize ()
		{
			int timerOffset = SkinningDefs.TheInstance.GetIntData("timer_x_offset", 0);

			if (lblGameDay.Visible)
			{
				lblGameRound.Location = new Point(5, 0);
				lblGameRound.Size = new Size(100, 25);

				lblGameDay.Location = new Point(115 - 20, 0);
				lblGameDay.Size = new Size(75, 25);

				lblGamePhase.Location = new Point(170, 0);
				lblGamePhase.Size = new Size(125, 25);

				lblGameTime.Location = new Point(350-20, 0);
				lblGameTime.Size = new Size(200, 25);

				lblPrePlayTime.Location = new Point(420 - ClockOffset - timerOffset - 125, 0);
				lblPrePlayTime.Size = new Size(120 + ClockOffset, 28);
			}
			else
			{
				lblGameRound.Location = new Point(5, 0);
				lblGameRound.Size = new Size(100-20, 25);
				//lblGameRound.BackColor = Color.Pink;

				lblPrePlayTime.Location = new Point(420 - ClockOffset - timerOffset, 0);
				lblPrePlayTime.Size = new Size(120 + ClockOffset, 28);
				//lblPrePlayTime.BackColor = Color.Crimson;

				lblGamePhase.Location = new Point(135+15, 0);
				lblGamePhase.Size = new Size(130, 25);

				lblGameTime.Location = new Point(200+100, 0);
				lblGameTime.Size = new Size(180, 25);
				//lblGameTime.BackColor = Color.ForestGreen;

				//lblGameRound.BackColor = Color.Pink;
				//lblGameDay.BackColor = Color.PeachPuff;
				//lblGamePhase.BackColor = Color.LightBlue;
				//lblGameTime.BackColor = Color.LightGreen;
				//lblPrePlayTime.BackColor = Color.Yellow;
			}
		}

		public override void ShowPrePlay (bool showPrePlay)
		{
			base.ShowPrePlay(showPrePlay);

			showDay = !showPrePlay;
			setLabel();
		}
	}
}