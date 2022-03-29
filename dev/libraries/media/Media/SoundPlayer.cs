using System;
using System.Windows.Forms;

namespace Media
{
	public class SoundPlayer : IDisposable
	{
		static Control container;
		static bool useVlc;

	    readonly MediaPanel mediaPanel;

	    public string FileName => mediaPanel.Filename;

	    public static void SetContainer (Control container, bool useVlc)
		{
			SoundPlayer.container = container;
			SoundPlayer.useVlc = useVlc;
		}

		public static void SetUseVlc (bool useVlc)
		{
			SoundPlayer.useVlc = useVlc;
		}

		public SoundPlayer ()
		{
			if (container == null)
			{
				throw new Exception ("You must call SoundPlayer.SetContainer() with your top-level form before instantiating SoundPlayer");
			}
				
			if (container.IsDisposed)
			{
				throw new Exception ("SoundPlayer.Container has been disposed");
			}

			mediaPanel = MediaPanel.Create(useVlc);
			mediaPanel.PlaybackFinished += mediaPanel_PlaybackFinished;
			container.Controls.Add(mediaPanel);
		    container.Disposed += container_Disposed;
			mediaPanel.Hide();
		}

	    void container_Disposed (object sender, EventArgs args)
	    {
            Dispose();
	    }

	    public void Dispose ()
		{
		    if (! container.IsDisposed)
		    {
		        container.Controls.Remove(mediaPanel);
		    }

		    mediaPanel.Dispose();
		}

		public void Play (string file, bool loop)
		{
			mediaPanel.LoadMedia(file);
			mediaPanel.PlayLooped = loop;
			mediaPanel.Play();
		}

		public void Stop ()
		{
			mediaPanel.Stop();
		}

		public void Pause ()
		{
			mediaPanel.Pause();
		}

		public void Resume ()
		{
			mediaPanel.Play();
		}

		public double Volume
		{
			get => mediaPanel.Volume;

		    set => mediaPanel.Volume = value;
		}

		public double Speed
		{
			get => mediaPanel.Speed;

		    set => mediaPanel.Speed = value;
		}

		public event EventHandler PlaybackFinished;

		void OnPlaybackFinished ()
		{
		    PlaybackFinished?.Invoke(this, EventArgs.Empty);
		}

		void mediaPanel_PlaybackFinished (object sender, EventArgs args)
		{
			OnPlaybackFinished();
		}

		public MediaState MediaState => mediaPanel.State;
	}
}