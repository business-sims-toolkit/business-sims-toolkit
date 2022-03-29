namespace Media
{
	public class NoVideoException : MediaPanelException
	{
		public NoVideoException ()
			: base ("No video is loaded")
		{
		}
	}
}