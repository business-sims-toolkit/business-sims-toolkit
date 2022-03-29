using System.Collections;
using LibCore;
using Network;

using CoreUtils;

namespace BusinessServiceRules
{
	/// <summary>
	/// Summary description for ModelTimeManager.
	/// </summary>
	public class ModelTimeManager
	{
		static NodeTree MyNodeTree;
		Node currentTimeNode = null;
		Node currentRecoveryNode = null;
		Node currModelTimeNode = null;
		bool countdown_active = false;

		public ModelTimeManager(NodeTree nt)
		{
			MyNodeTree = nt;

			currModelTimeNode = MyNodeTree.GetNamedNode("CurrentModelTime");
			
			currentTimeNode = MyNodeTree.GetNamedNode("CurrentTime");
			if (currentTimeNode != null)
			{
				currentTimeNode.AttributesChanged +=currTimeNode_AttributesChanged;
			}

			currentRecoveryNode = MyNodeTree.GetNamedNode("RecoveryProcess");
			if (currentRecoveryNode != null)
			{
				currentRecoveryNode.AttributesChanged	+=currentRecoveryNode_AttributesChanged;
			}
		}

		public void Dispose()
		{
			if (currModelTimeNode != null)
			{
				currModelTimeNode = null;
			}

			if (currentTimeNode != null)
			{
				currentTimeNode.AttributesChanged -=currTimeNode_AttributesChanged;
				currentTimeNode = null;
			}
			if (currentRecoveryNode != null)
			{
				currentRecoveryNode.AttributesChanged	-=currentRecoveryNode_AttributesChanged;
				currentRecoveryNode = null;
			}
		}

		void currentRecoveryNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			bool countdown_running = sender.GetBooleanAttribute("countdown_running", false);
			countdown_active = countdown_running;
		}

		void currTimeNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			//Old Code 
			//int seconds = currentTimeNode.GetIntAttribute("seconds",0);
			//int sixtieths = currentTimeNode.GetIntAttribute("sixtieths",0);
			//New Code 
			//The Main Second Count Clock has changed 
			//So we need to add another minute onto the GameTime

			int hour_value=0;
			int min_value=0;

			string currentModelTimeStr = currModelTimeNode.GetAttribute("time");

			ParseTime (currentModelTimeStr, false, out hour_value, out min_value);

			min_value++;
			if (min_value==60)
			{
				hour_value++;
				min_value=0;
			}
			string NewTime = TwoDigitInt(hour_value) + ":" + TwoDigitInt(min_value);
			currModelTimeNode.SetAttribute("time", NewTime);

			if (countdown_active)
			{
				string currentModelTimeLeftStr = currModelTimeNode.GetAttribute("time_left");
				ParseTime (currentModelTimeLeftStr, false, out hour_value, out min_value);

				min_value--;
				if (min_value<0)
				{
					hour_value--;
					min_value=59;
				}
				if (hour_value<0)
				{
					hour_value=0;
					min_value=0;
				}
				string NewTimeLeft = TwoDigitInt(hour_value) + ":" + TwoDigitInt(min_value);
				currModelTimeNode.SetAttribute("time_left", NewTimeLeft);
			}
		}

		public static string DurationToString (int time)
		{
			int hour = (time / 60);
			int minute = time % 60;
			return TwoDigitInt(hour) + ":" + TwoDigitInt(minute);
		}

		public static string TimeToString (int time)
		{
			return TimeToString(time, 0);
		}

		static string TwoDigitInt (int a)
		{
			string s = "00" + CONVERT.ToStr(a);
			return s.Substring(s.Length - 2, 2);
		}

		public static string TimeToString (int time, int sixtieths)
		{
			int startHour = SkinningDefs.TheInstance.GetIntData("clock_start_hour", 0);
			int hour = (time / 60) + startHour;
			int minute = time % 60;

			return TwoDigitInt(hour) + ":" + TwoDigitInt(minute) + ":" + TwoDigitInt(sixtieths);
		}

		public static string TimeToStringFlattenDay (int time)
		{
			int startHour = SkinningDefs.TheInstance.GetIntData("clock_start_hour", 0);
			int hour = ((time / 60) + startHour) % 24;
			int minute = time % 60;

			return TwoDigitInt(hour) + ":" + TwoDigitInt(minute);
		}

		public static string TimeToStringWithDay (int time)
		{
			int startHour = SkinningDefs.TheInstance.GetIntData("clock_start_hour", 0);
			int hour = (time / 60) + startHour;
			int minute = time % 60;

			string [] days = new string [] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" };
			int day = hour / 24;
			hour -= (day * 24);

			return days[day] + " " + TwoDigitInt(hour) + ":" + TwoDigitInt(minute);
		}

		public static string TimeToDay (int time)
		{
			int startHour = SkinningDefs.TheInstance.GetIntData("clock_start_hour", 0);
			int hour = (time / 60) + startHour;
			int minute = time % 60;

			string [] days = new string [] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" };
			int day = hour / 24;
			hour -= (day * 24);

			return days[day];
		}

		public static string TimeToShortDay (int time)
		{
			int startHour = SkinningDefs.TheInstance.GetIntData("clock_start_hour", 0);
			int hour = (time / 60) + startHour;
			int minute = time % 60;

			string [] days = new string [] { "MON", "TUE", "WED", "THU", "FRI" };
			int day = hour / 24;
			hour -= (day * 24);

			return days[day];
		}


		public static int ParseTime (string s, bool UseStartHour)
		{
			string [] components = s.Split(':');
			int time;

			int hour = CONVERT.ParseInt(components[0]);
			int minute = CONVERT.ParseInt(components[1]);

			if (UseStartHour)
			{
				hour = hour - SkinningDefs.TheInstance.GetIntData("clock_start_hour", 0);
			}

			time = (hour * 60) + minute;
			return time;
		}

		public static void ParseTime (string s, bool UseStartHour, out int hour, out int min)
		{
			string [] components = s.Split(':');
			
			hour = CONVERT.ParseInt(components[0]);
			min = CONVERT.ParseInt(components[1]);

			if (UseStartHour)
			{
				hour = hour - SkinningDefs.TheInstance.GetIntData("clock_start_hour", 0);
			}
		}

		public static int GetTime (Node n)
		{
			return n.GetIntAttribute("time", 0);
		}


	}
}