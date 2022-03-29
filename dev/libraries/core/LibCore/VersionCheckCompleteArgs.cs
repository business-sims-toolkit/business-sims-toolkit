using System;

namespace LibCore
{
	public class VersionCheckCompleteEventArgs : EventArgs
	{
		VersionCheckResults results;

		public VersionCheckResults Results
		{
			get
			{
				return results;
			}
		}

		public VersionCheckCompleteEventArgs (VersionCheckResults results)
		{
			this.results = results;
		}
	}
}