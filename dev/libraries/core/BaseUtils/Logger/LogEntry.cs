using System;

namespace BaseUtils
{
	/// <summary>
	/// Describes the importance of the
	/// log entry.
	/// </summary>
	public enum LogLevel
	{
		Low,
		Medium,
		High
	}

	/// <summary>
	/// The LogEntry class encapsulates all of the information
	/// about error and system messages.
	/// </summary>
	public class LogEntry
	{
		LogLevel level;
		string category;
		string message;
		object data;
		DateTime timeStamp;

		/// <summary>
		/// Creates an instance of LogEntry.
		/// </summary>
		/// <param name="level">The importance of the message.</param>
		/// <param name="category">The category for the message.</param>
		/// <param name="message">The message.</param>
		public LogEntry(LogLevel level, string category, string message)
		{
			Init(level, category, message, null);
		}

		/// <summary>
		/// Creates an instance of LogEntry.
		/// </summary>
		/// <param name="level">The importance of the message.</param>
		/// <param name="category">The category for the message.</param>
		/// <param name="message">The message.</param>
		/// <param name="data">The data associated with the message.</param>
		public LogEntry(LogLevel level, string category, string message, object data)
		{
			Init(level, category, message, data);
		}

		void Init(LogLevel level, string category, string message, object data)
		{
			this.level = level;
			this.category = category;
			this.message = message;
			this.data = data;
			this.timeStamp = DateTime.Now;
		}

		public LogLevel Level
		{
			get { return level; }
		}

		public string Category
		{
			get { return category; }
		}

		public string Message
		{
			get { return message; }
		}

		public object Data
		{
			get { return data; }
		}

		public DateTime TimeStamp
		{
			get { return timeStamp; }
		}
	}
}
