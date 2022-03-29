namespace LibCore
{
	public class TacPermissions
	{
		public string LicensorVersion;

		public int DaysUntilRenew;
		public int RemainingCredits;

		public bool CoursewareAllowed;
		public bool GameAllowed;
		public bool PathfinderReportsAllowed;
		public bool isDirectPurchase;
		public string websiteName_DP;
		public bool useCreditPool;
		public bool LicensorRequestedQuit;

		public bool AllowPlayWithoutLicence;

		public TacPermissions ()
		{
			LicensorVersion = "None";

			DaysUntilRenew = -1;
			RemainingCredits = -1;
			LicensorRequestedQuit = false;

			CoursewareAllowed = false;
			GameAllowed = false;
			PathfinderReportsAllowed = false;

			isDirectPurchase = false;
			useCreditPool = false;
			websiteName_DP = string.Empty;

			AllowPlayWithoutLicence = false;
		}

		public TacPermissions (string licensorVersion, int remainingCredits, int daysUntilRenew,
			bool coursewareAllowed, bool gameAllowed, bool pathfinderReportsAllowed, bool useCreditPool, bool isDirectPurchaseFlag,
			string dp_website, bool tmpLicensorRequestedQuit)
		{
			LicensorVersion = licensorVersion;
			RemainingCredits = remainingCredits;
			DaysUntilRenew = daysUntilRenew;
			CoursewareAllowed = coursewareAllowed;
			GameAllowed = gameAllowed;
			PathfinderReportsAllowed = true;
			isDirectPurchase = isDirectPurchaseFlag;
			websiteName_DP = dp_website;
			this.useCreditPool = useCreditPool;
			LicensorRequestedQuit = tmpLicensorRequestedQuit;
		}

#if DEBUG
		public static TacPermissions CreateForPlayRoundsOnly ()
		{
			TacPermissions tacPermissions = new TacPermissions ("*** PLAYROUNDS ***", 15, 0, true, true, true, false, false, "", false);
			tacPermissions.AllowPlayWithoutLicence = true;
			return tacPermissions;
		}
#endif

		public string ToDebugStr()
		{
			string db = "";
			db += "[LV]" + LicensorVersion.ToString();
			db += "[RC]" + RemainingCredits.ToString();
			db += "[DR]" + DaysUntilRenew.ToString();
			db += "[CA]" + CoursewareAllowed.ToString();
			db += "[PR]" + PathfinderReportsAllowed.ToString();
			db += "[DP]" + isDirectPurchase.ToString();
			db += "[DW]" + websiteName_DP.ToString();
			db += "[CP]" + useCreditPool.ToString();
			db += "[RQ]" + this.LicensorRequestedQuit.ToString();
			return db;
		}
	}
}