using System.Collections.Generic;

namespace DevOpsEngine.StringConstants
{
	public class FeedbackImageName
	{
		public const string Cash = "cash";
		public const string Clock = "clock";
		public const string Cross = "cross";
		public const string Tick = "tick";

		public static List<string> All => new List<string> { Cash, Clock, Cross, Tick };
	}
}
