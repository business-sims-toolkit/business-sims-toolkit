using System;
using System.Drawing;
using System.Windows.Forms;

using LibCore;
using IncidentManagement;
using Network;

namespace CommonGUI
{
	public class RealTimeUpgradeServerPanel : UpgradeMemDiskControl
	{
		Label hourLabel;

		public RealTimeUpgradeServerPanel (OpsControlPanelBase controlPanel, NodeTree model, bool showMinutesNotDays, IncidentApplier incidentApplier, bool isTraining, Color backColour, Color groupBackColour)
			: base (controlPanel, model, showMinutesNotDays, incidentApplier, isTraining, backColour, groupBackColour)
		{
		}

		protected override string GetUpgradeTimeLabelText ()
		{
			return "Install At:";
		}

		protected override string FormatAutoTime (int offset)
		{
			DateTime time = _Network.GetNamedNode("CurrentTime").GetDateTimeAttribute("world_time_at_zero_seconds", DateTime.Now).AddSeconds(offset);

			return CONVERT.Format("{0}:{1:00}:{2:00}", time.Hour, time.Minute, time.Second);
		}

		protected override Panel CreateWhenEntryPanel ()
		{
			int hourLabelWidth = 50;

			Panel panel = new Panel();
			panel.Location = new Point(180, 52);
			panel.Size = new Size(110, 25);

			hourLabel = new Label();
			hourLabel.Font = MyDefaultSkinFontNormal11;
			hourLabel.TextAlign = ContentAlignment.MiddleRight;
			hourLabel.Size = new Size(hourLabelWidth, panel.Height);
			hourLabel.Location = new Point(0, -2);
			panel.Controls.Add(hourLabel);

			whenTextBox = new EntryBox();
			whenTextBox.DigitsOnly = true;
			whenTextBox.Font = MyDefaultSkinFontNormal11;
			whenTextBox.Size = new Size(panel.Width - hourLabelWidth, panel.Height);
			whenTextBox.Location = new Point(hourLabelWidth, 0);
			whenTextBox.Text = "Now";
			whenTextBox.MaxLength = 2;
			whenTextBox.KeyUp += whenTextBox_KeyUp;
			whenTextBox.TextAlign = HorizontalAlignment.Center;
			whenTextBox.GotFocus += whenTextBox_GotFocus;
			whenTextBox.LostFocus += whenTextBox_LostFocus;
			whenTextBox.TextChanged += whenTextBox_TextChanged;
			whenTextBox.ForeColor = Color.Black;
			panel.Controls.Add(whenTextBox);

			UpdateHourLabel();

			return panel;
		}

		void whenTextBox_TextChanged (object sender, EventArgs e)
		{
			UpdateHourLabel();
		}

		void UpdateHourLabel ()
		{
			if (whenTextBox.Text.Trim().ToLower() == "now")
			{
				hourLabel.Hide();
			}
			else
			{
				int minute = CONVERT.ParseIntSafe(whenTextBox.Text, 0);
				int hour = DateTime.Now.Hour;
				if (minute < DateTime.Now.Minute)
				{
					hour++;
				}

				string minutePrefix = new string('0', Math.Min(1, 2 - whenTextBox.Text.Length));

				hourLabel.Text = CONVERT.Format("{0}:{1} ", hour, minutePrefix);
				hourLabel.Show();
			}
		}

		protected override int GetRequestedTimeOffset ()
		{
			int minute = CONVERT.ParseIntSafe(whenTextBox.Text, 0);
			int hour = DateTime.Now.Hour;
			if (minute < DateTime.Now.Minute)
			{
				hour++;
			}

			DateTime requestedClockTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, hour, minute, 0);
			DateTime initialClockTime = _Network.GetNamedNode("CurrentTime").GetDateTimeAttribute("world_time_at_zero_seconds", DateTime.Now);

			return ((int) (requestedClockTime.Subtract(initialClockTime).TotalSeconds)) / 60;
		}
	}
}