using System;

namespace BaseUtils
{
	/// <summary>
	/// The main entry point for the Logging system. The Logger class
	/// contains several static methods for logging messages.
	/// </summary>
	public class Logger
	{
		static Formatter formatter = new BasicEntryFormatter();
		static Writer writer;

		/// <summary>
		/// Log the specified message.
		/// </summary>
		/// <param name="category">The category for the message.</param>
		/// <param name="message">The message.</param>
		public static void Log(string category, string message)
		{
			Log(LogLevel.Medium, category, message, null);
		}

		/// <summary>
		/// Log the specified message.
		/// </summary>
		/// <param name="level">The importance of the message.</param>
		/// <param name="category">The category for the message.</param>
		/// <param name="message">The message.</param>
		public static void Log(LogLevel level, string category, string message)
		{
			Log(level, category, message, null);
		}

		public static void LogXX(string st1, string st2)
		{
			Console.WriteLine(st1+st2);
			if (writer != null)
			{
				writer.Write(st1+st2);
				writer.Flush();
			}
		}

		/// <summary>
		/// Log the specified message.
		/// </summary>
		/// <param name="level">The importance of the message.</param>
		/// <param name="category">The category for the message.</param>
		/// <param name="message">The message.</param>
		/// <param name="data">The associated data for the message. More than one data parameter can be specified.</param>
		public static void Log(LogLevel level, string category, string message, params object[] data)
		{
			string result = formatter.Format(new LogEntry(level, category, message, data));

			Console.WriteLine(result);
			if (writer != null)
			{
				writer.Write(result);
				writer.Flush();
			}
		}

		/// <summary>
		/// Close the Log writer.
		/// </summary>
		public static void Close()
		{
			if (writer != null)
				writer.Close();
		}

		/// <summary>
		/// Gets or Sets the Formatter for the Logger.
		/// </summary>
		public static Formatter LogFormatter
		{
			get { return formatter; }
			set 
			{ 
				if (value == null)
					formatter = new BasicEntryFormatter();
				else
					formatter = value; 
			}
		}

		/// <summary>
		/// Gets or Sets the Writer for the Logger.
		/// </summary>
		public static Writer LogWriter
		{
			get { return writer; }
			set { writer = value; }
		}
	}
}
