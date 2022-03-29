namespace CoreUtils
{
	/// <summary>
	/// Summary description for ITimedClass.
	/// </summary>
	public interface ITimedClass
	{
		void Start();
		void Stop();
		void Reset();
		void FastForward(double timesRealTime);
	}

	public interface IPostNotifiedTimedClass
	{
		void BeforeStart();
		void AfterStop();
	}
}
