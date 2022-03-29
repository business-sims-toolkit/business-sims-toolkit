using System;
using System.Text;

namespace BaseUtils
{
	/// <summary>
	/// Extends Formatter to provide a basic implementation
	/// that transforms LogEntry instances
	/// into XML fragments.
	/// </summary>
	internal class BasicEntryFormatter : Formatter
	{
		Formatter dataFormatter;

		/// <summary>
		/// Creates an instance of BasicEntryFormatter.
		/// </summary>
		public BasicEntryFormatter()
		{
			dataFormatter = new BasicDataFormatter();
		}

		/// <summary>
		/// Transforms the specified LogEntry instance into an XML fragment.
		/// </summary>
		/// <param name="entry">The LogEntry instance to transform.</param>
		/// <returns>bool</returns>
		public override string Format(LogEntry entry)
		{
			StringBuilder sb = new StringBuilder();

			sb.Append("<LogEntry>\r\n");
			sb.Append(String.Format("<TimeStamp>{0}</TimeStamp>\r\n", entry.TimeStamp));
			sb.Append(String.Format("<Level>{0}</Level>\r\n", entry.Level));
			sb.Append(String.Format("<Category>{0}</Category>\r\n", entry.Category));
			sb.Append(String.Format("<Message>{0}</Message>\r\n", entry.Message));
			sb.Append(String.Format("<Data>\r\n{0}</Data>\r\n", dataFormatter.Format(entry)));
			sb.Append("</LogEntry>\r\n");

			return sb.ToString();
		}
	}
}
