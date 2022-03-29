using System;

namespace GameDetails
{
	public class GenerateReportEventArgs : EventArgs
	{
		public ReportType Type;
		public string Filename;
		public DateTime Date;

		public GenerateReportEventArgs (ReportType type, string filename, DateTime date)
		{
			Type = type;
			Filename = filename;
			Date = date;
		}
	}
}