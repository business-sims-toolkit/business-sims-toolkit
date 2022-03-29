namespace BaseUtils
{
	/// <summary>
	/// Abstract base class for transforming LogEntry instances into
	/// strings.
	/// </summary>
	public abstract class Formatter
	{
		public abstract string Format(LogEntry entry);
	}
}
