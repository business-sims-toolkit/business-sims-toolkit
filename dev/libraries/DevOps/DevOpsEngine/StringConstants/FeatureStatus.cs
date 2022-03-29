using System.Collections.Generic;

namespace DevOpsEngine.StringConstants
{
	public class FeatureStatus
	{
		public const string Dev = "dev";
		public const string Test = "test";
		public const string TestDelay = "testDelay";
		public const string Release = "release";
		public const string Deploy = "deploy";
		public const string Installing = "installing";
		public const string Live = "live";
		public const string Cancelled = "cancelled";
		public const string Undo = "undo";
		public const string Redevelop = "redevelop";

		public static int IndexOfLastDevelopmentStatus => All.IndexOf(Live);

		public static List<string> All => new List<string>
		{
			Dev, Test, TestDelay, Release, Installing, Live, Cancelled, Undo, Redevelop
		};
	}
}
