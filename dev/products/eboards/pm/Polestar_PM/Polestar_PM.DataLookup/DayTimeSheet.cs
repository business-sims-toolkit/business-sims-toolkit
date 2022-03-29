using System;
using System.Collections;
using System.IO;
using System.Xml;

using Network;
using BaseUtils;
using CoreUtils;
using LibCore;

namespace Polestar_PM.DataLookup
{
	/// <summary>
	/// The DayTimeSheet is used to record how many people are working/idle on a particular day
	/// This is used to prepare the Resource Utilisation Graphs
	/// In order to draw the graphs, we also need the maximum counts 
	/// </summary>
	public class DayTimeSheet
	{
		private int staff_int_dev_max_count = 0;
		private int staff_ext_dev_max_count = 0;
		private int staff_int_test_max_count = 0;
		private int staff_ext_test_max_count = 0;

		public int staff_int_dev_day_employed_count = 0;
		public int staff_int_dev_day_idle_count = 0;
		public int staff_ext_dev_day_employed_count = 0;
		public int staff_ext_dev_day_idle_count = 0;

		public int staff_int_test_day_employed_count = 0;
		public int staff_int_test_day_idle_count = 0;
		public int staff_ext_test_day_employed_count = 0;
		public int staff_ext_test_day_idle_count = 0;

		public void SetMaximumStaffNumbers(int DevIntMax, int DevExtMax, int TestIntMax, int TestExtMax)
		{
			staff_int_dev_max_count = DevIntMax;
			staff_ext_dev_max_count = DevExtMax;
			staff_int_test_max_count = TestIntMax;
			staff_ext_test_max_count = TestExtMax;
		}

		public void GetMaximumStaffNumbers(out int DevIntMax, out int DevExtMax, out int TestIntMax, out int TestExtMax)
		{
			DevIntMax = staff_int_dev_max_count;
			DevExtMax = staff_ext_dev_max_count;
			TestIntMax = staff_int_test_max_count;
			TestExtMax = staff_ext_test_max_count;
		}

		public void ReadFromXmlNode(XmlNode node)
		{
			string nn1 = node.InnerText;
			string nn2 = node.InnerXml;
			string nn3 = node.InnerText;

			foreach (XmlNode sub_node in node.ChildNodes)
			{
				int day = CONVERT.ParseInt(sub_node.Attributes["num"].Value);
				string nnn1 = sub_node.InnerText;
				string nnn2 = sub_node.InnerXml;
				string nnn3 = sub_node.InnerText;

				if (sub_node.Name.ToLower() == "staff_int_dev_day_employed")
				{
					staff_int_dev_day_employed_count =  CONVERT.ParseInt(sub_node.Attributes["num"].Value);
				}
				if (sub_node.Name.ToLower() == "staff_int_dev_day_idle")
				{
					staff_int_dev_day_idle_count = CONVERT.ParseInt(sub_node.Attributes["num"].Value);
				}

				if (sub_node.Name.ToLower() == "staff_ext_dev_day_employed")
				{
					staff_ext_dev_day_employed_count = CONVERT.ParseInt(sub_node.Attributes["num"].Value);
				}
				if (sub_node.Name.ToLower() == "staff_ext_dev_day_idle")
				{
					staff_ext_dev_day_idle_count = CONVERT.ParseInt(sub_node.Attributes["num"].Value);
				}

				if (sub_node.Name.ToLower() == "staff_int_test_day_employed")
				{
					staff_int_test_day_employed_count = CONVERT.ParseInt(sub_node.Attributes["num"].Value);
				}
				if (sub_node.Name.ToLower() == "staff_int_test_day_idle")
				{
					staff_int_test_day_idle_count = CONVERT.ParseInt(sub_node.Attributes["num"].Value);
				}

				if (sub_node.Name.ToLower() == "staff_ext_test_day_employed")
				{
					staff_ext_test_day_employed_count = CONVERT.ParseInt(sub_node.Attributes["num"].Value);
				}
				if (sub_node.Name.ToLower() == "staff_ext_test_day_idle")
				{
					staff_ext_test_day_idle_count = CONVERT.ParseInt(sub_node.Attributes["num"].Value);
				}

			}
		}
	}
}
