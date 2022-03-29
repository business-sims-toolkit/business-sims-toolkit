namespace BaseUtils
{
	/// <summary>
	/// Extends Formatter to provide a basic implementation
	/// that transforms the LogEntry.Data attribute
	/// into an XML fragment.
	/// </summary>
	internal class BasicDataFormatter : Formatter
	{
		/// <summary>
		/// Creates an instance of BasicDataFormatter.
		/// </summary>
		public BasicDataFormatter()
		{
		}

		/// <summary>
		/// Transforms the specified LogEntry.Data attribute into an XML fragment.
		/// </summary>
		/// <param name="entry">The LogEntry to transform.</param>
		/// <returns>string</returns>
		public override string Format(LogEntry entry)
		{
			return TypeFormatterFactory.GetTypeFormatter(entry.Data).Format(entry.Data);
		}
	}
}
