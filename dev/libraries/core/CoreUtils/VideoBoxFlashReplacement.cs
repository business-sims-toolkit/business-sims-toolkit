using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Algorithms;
using Media;
using LibCore;

namespace CoreUtils
{
	public class VideoBoxFlashReplacement : MediaPanel, ITimedClass
	{
		public VideoBoxFlashReplacement ()
		{
			mediaPanel = MediaPanel.Create(false);
			Controls.Add(mediaPanel);

			DoSize();

			TimeManager.TheInstance.ManageClass(this);
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing)
			{
				mediaPanel.Dispose();
				TimeManager.TheInstance.UnmanageClass(this);
			}

			base.Dispose(disposing);
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);
			DoSize();
		}

		void DoSize ()
		{
			mediaPanel.Bounds = new Rectangle (0, 0, Width, Height);
		}

		static string GetFullPathWithoutExtension (string path)
		{
			return Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path));
		}

		static List<string> allFilesLoaded = new List<string> ();
		MediaPanel mediaPanel;

		public void LoadFile (string filename)
		{
			string [] parts = Paths.Split(filename);
			if (parts[parts.Length - 2] == "flash")
			{
				parts[parts.Length - 2] = "video";
				filename = Paths.Combine(parts);
			}

			if (! File.Exists(filename))
			{
				foreach (string extension in new [] { ".avi", ".wmv", ".mp4", ".mpg", ".mov", ".png", ".gif", ".jpg", ".jpeg", ".mpeg" })
				{
					filename = Paths.ChangeExtension(filename, extension);
					if (File.Exists(filename))
					{
						break;
					}
				}
			}

			string stub = System.IO.Path.GetFileName(filename);
			if (! allFilesLoaded.Contains(stub))
			{
				allFilesLoaded.Add(stub);
			}

			if (File.Exists(filename))
			{
				LoadMedia(filename);
			}
			else
			{
				if (! SuppressFileErrors)
				{
					throw new Exception(string.Format("Can't find a media file '{0}' with any usable dot extension!", GetFullPathWithoutExtension(filename)));
				}
			}
		}

		public void SetFlashBackground (int argb)
		{
			BackColor = Color.FromArgb(argb);
		}

		public override void Pause ()
		{
			mediaPanel.Pause();
		}

		public override void Rewind ()
		{
			if (State != MediaState.Unloaded)
			{
				mediaPanel.Rewind();
			}
		}

		public override void Stop ()
		{
			mediaPanel.Stop();
		}

		public override bool Paused
		{
			get
			{
				return mediaPanel.Paused;
			}
			set
			{
				mediaPanel.Paused = value;
			}
		}

		public override bool IsPlaying
		{
			get
			{
				return mediaPanel.IsPlaying;
			}
		}

		public override bool PlayLooped
		{
			get
			{
				return mediaPanel.PlayLooped;
			}
			set
			{
				mediaPanel.PlayLooped = value;
			}
		}

		public override bool HasVideo
		{
			get
			{
				return mediaPanel.HasVideo;
			}
		}

		public override bool HasAudio
		{
			get
			{
				return mediaPanel.HasAudio;
			}
		}

		public override double Duration
		{
			get
			{
				return mediaPanel.Duration;
			}
		}

		public override void Seek (double position)
		{
			mediaPanel.Seek(position);
		}

		public override double CurrentPosition
		{
			get
			{
				return mediaPanel.CurrentPosition;
			}
			set
			{
				mediaPanel.CurrentPosition = value;
			}
		}

		public override double Speed
		{
			get
			{
				return mediaPanel.Speed;
			}
			set
			{
				mediaPanel.Speed = value;
			}
		}

		public override double Volume
		{
			get
			{
				return mediaPanel.Volume;
			}
			set
			{
				mediaPanel.Volume = value;
			}
		}

		public override string Filename
		{
			get
			{
				return mediaPanel.Filename;
			}
			set
			{
				mediaPanel.Filename = value;
			}
		}

		public override Image TakeSnapshot ()
		{
			return mediaPanel.TakeSnapshot();
		}

		public override MediaState State
		{
			get
			{
				return mediaPanel.State;
			}
		}

		public override Size VideoSize
		{
			get
			{
				return mediaPanel.VideoSize;
			}
		}

		public override double AverageFrameRate
		{
			get
			{
				return mediaPanel.AverageFrameRate;
			}
		}

		public override bool ThrowOnSpeedErrors
		{
			get
			{
				return mediaPanel.ThrowOnSpeedErrors;
			}
			set
			{
				mediaPanel.ThrowOnSpeedErrors = value;
			}
		}

		public override ZoomMode ZoomMode => mediaPanel.ZoomMode;

		public override void ZoomWithLetterboxing ()
		{
			mediaPanel.ZoomWithLetterboxing();
		}

		public override void ZoomWithCropping (PointF windowReferencePoint, PointF videoReferencePoint)
		{
			mediaPanel.ZoomWithCropping(windowReferencePoint, videoReferencePoint);
		}

		public override void LoadMedia (string filename)
		{
			mediaPanel.LoadMedia(filename);
		}

		public override void UnloadMedia ()
		{
			mediaPanel.UnloadMedia();
		}

		public override void Play ()
		{
			if (State != MediaState.Unloaded)
			{
				Speed = TimeManager.TheInstance.CurrentSpeed;
				mediaPanel.Play();
			}
		}

		public bool Loop
		{
			get
			{
				return PlayLooped;
			}

			set
			{
				PlayLooped = value;
			}
		}

		public void CallFunction (string command, string args)
		{
		}

		public int CurrentFrame ()
		{
			if (State == MediaState.Unloaded)
			{
				return 0;
			}
			else
			{
				return FrameNum;
			}
		}

		public int TotalFrames
		{
			get
			{
				if (State == MediaState.Unloaded)
				{
					return 0;
				}
				else
				{
					return (int) Math.Ceiling(Duration / AverageFrameRate);
				}
			}
		}

		public int FrameNum
		{
			get
			{
				if (State == MediaState.Unloaded)
				{
					return 0;
				}
				else
				{
					return (int) (CurrentPosition / AverageFrameRate);
				}
			}

			set
			{
				if (State != MediaState.Unloaded)
				{
					CurrentPosition = value * AverageFrameRate;
				}
			}
		}

		void ITimedClass.Stop ()
		{
			Pause();
		}

		public void SetSize (int width, int height)
		{
			Size = new Size (width, height);
		}

		public virtual void Start ()
		{
			Play();
		}

		public virtual void Reset ()
		{
			Pause();
			Rewind();
		}

		public virtual void FastForward (double timesRealTime)
		{
			Speed = timesRealTime;
		}
	}
}