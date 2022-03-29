using System;
using System.IO;

namespace BaseUtils
{
	/// <summary>
	/// Extends Writer to provide a basic implementation
	/// that writes to a file.
	/// </summary>
	public class FileWriter : Writer, IDisposable
	{
		StreamWriter sw;

		/// <summary>
		/// Creates an instance of FileWriter. Open a file stream
		/// for writing to.
		/// </summary>
		/// <param name="path"></param>
		public FileWriter(string path)
		{
			try
			{
				sw = new StreamWriter(path, true);
				sw.AutoFlush = true;
			}
			catch
			{
				sw = null;
			}
		}

		/// <summary>
		/// Write the specified message to the file.
		/// </summary>
		/// <param name="message"></param>
		internal override void DoWrite(string message)
		{
			if (sw != null)
			{
				try
				{
					sw.WriteLine(message);
				}
				catch
				{
					// Do not want logging to break
					// the host app
				}
			}
		}

		/// <summary>
		/// Flush the contents of the file stream.
		/// </summary>
		internal override void DoFlush()
		{
			if (sw != null)
			{
				try
				{
					sw.Flush();
				}
				catch
				{
					// Do not want logging to break
					// the host app
				}
			}
		}

		/// <summary>
		/// Close the file stream.
		/// </summary>
		internal override void DoClose()
		{
			if (sw != null)
			{
				try
				{
					sw.Close();
					sw = null;
				}
				catch
				{
					// Do not want logging to break
					// the host app
				}
			}
		}

		#region IDisposable Members

		public void Dispose()
		{
			if (sw != null)
				sw.Close();
		}

		#endregion
	}
}
