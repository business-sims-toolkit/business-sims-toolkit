namespace Media
{
	public class VideoFormatNotUnderstoodException : MediaPanelException
	{
		public VideoFormatNotUnderstoodException ()
			: base ("Video format not understood")
		{
		}
	}
}