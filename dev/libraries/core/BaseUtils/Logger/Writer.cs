namespace BaseUtils
{
	/// <summary>
	/// Abstract base class for writing log entries.
	/// </summary>
	public abstract class Writer
	{
		int writes;
		int maxWrites = 10;

		/// <summary>
		/// Writes the specified message.
		/// </summary>
		/// <param name="message">The message to write.</param>
		public void Write(string message)
		{
			DoWrite(message);

			writes++;
			if (writes == maxWrites)
				Flush();
		}

		/// <summary>
		/// Flush the message stream.
		/// </summary>
		public void Flush()
		{
			writes = 0;
			DoFlush();
		}

		/// <summary>
		/// Close the message stream.
		/// </summary>
		public void Close()
		{
			Flush();
			DoClose();
		}

		internal abstract void DoWrite(string message);
		internal abstract void DoFlush();
		internal abstract void DoClose();

		public int MaxWrites
		{
			get { return maxWrites; }
			set { Flush(); maxWrites = value; }
		}
	}
}
