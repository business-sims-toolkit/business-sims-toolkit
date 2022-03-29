namespace Media
{
	public class FileNotLoadedException : MediaPanelException
	{
		public FileNotLoadedException ()
			: base ("No media file is loaded")
		{
		}
	}
}