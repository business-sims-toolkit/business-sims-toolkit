using System;

namespace DevOpsEngine
{
	public static class AgileComplaints
	{
		public static string[] CustomerComplaintTypes { get; } = { "S", "U", "R", "F" };

		public static int[] CustomerTypes { get; } = { 1, 2, 3, 4 };

		public static string GetDisplayNameForComplaint(string complaint)
		{
			switch (complaint)
			{
				case "S":
					return "Speed";

				case "U":
					return "Useability";

				case "R":
					return "Reliability";

				case "F":
					return "Functionality";

				default:
					throw new Exception($"Unknown complaint type '{complaint}'");
			}
		}
	}
}
