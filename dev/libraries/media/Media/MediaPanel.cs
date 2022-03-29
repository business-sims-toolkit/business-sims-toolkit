using System;
using System.Drawing;
using System.Windows.Forms;
using Algorithms;

namespace Media
{
	public abstract class MediaPanel : Panel
	{
		static bool suppressFileErrors;

		public static bool SuppressFileErrors
		{
			get => suppressFileErrors;

			set
			{
				suppressFileErrors = value;
			}
		}

		public static MediaPanel Create (bool useVlc)
		{
			if (useVlc)
			{
				throw new NotImplementedException ();
			}
			else
			{
				return new DirectShowMediaPanel ();
			}
		}

		public abstract void LoadMedia (string filename);

		public abstract void UnloadMedia ();

		public abstract void Play ();
		public abstract void Pause ();
		public abstract void Rewind ();
		public abstract void Stop ();

		public abstract bool Paused { get; set; }
		public abstract bool IsPlaying { get; }

		public abstract bool PlayLooped { get; set; }
		public abstract bool HasVideo { get; }
		public abstract bool HasAudio { get; }

		public abstract double Duration { get; }

		public abstract void Seek (double position);

		public abstract double CurrentPosition { get; set; }

		public abstract double Speed { get; set; }

		public abstract double Volume { get; set; }

		public abstract string Filename { get; set; }

		public abstract Image TakeSnapshot ();

		public abstract MediaState State { get; }

		public abstract Size VideoSize { get; }

		public abstract double AverageFrameRate { get; }

		public abstract bool ThrowOnSpeedErrors { get; set; }

		public abstract ZoomMode ZoomMode { get; }
		public abstract void ZoomWithLetterboxing ();
		public abstract void ZoomWithCropping (PointF windowReferencePoint, PointF videoReferencePoint);

		public event EventHandler PlaybackFinished;

		protected virtual void OnPlaybackFinished ()
		{
			PlaybackFinished?.Invoke(this, EventArgs.Empty);
		}
	}
}