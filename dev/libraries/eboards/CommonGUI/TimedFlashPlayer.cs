using System;
using System.Drawing;
using Algorithms;
using LibCore;
using CoreUtils;

namespace CommonGUI
{
	/// <summary>
	/// The TimedFlashPlayer plays a piece of flash and holds it to a precise time so that
	/// it matches other views...
	/// </summary>
	public class TimedFlashPlayer : BasePanel, ITimedClass
	{
		protected VideoBoxFlashReplacement flashBox;
	    protected bool stopped;

		protected string loadedFile = "";

		public TimedFlashPlayer()
		{
			flashBox = new VideoBoxFlashReplacement ();
			Controls.Add(flashBox);

			flashBox.SetFlashBackground(0);

			Resize += TimedFlashPlayer_Resize;

			TimeManager.TheInstance.ManageClass(this);

		    stopped = ! TimeManager.TheInstance.TimeIsRunning;
		}

		void TimedFlashPlayer_Resize(object sender, EventArgs e)
		{
			flashBox.Size = Size;
		}

		protected double numSeconds = 0;

		public void PlayFile(string file)
		{
			PlayFile(file,0);
		}

		protected int counter = 0;

		public void PlayFile (string file, long seconds)
		{
			string fullFilename = AppInfo.TheInstance.Location + "\\flash\\" + file;

			try
			{
				loadedFile = fullFilename;
				flashBox.LoadFile(fullFilename);
                flashBox.Play();
				if(stopped)
				{
					flashBox.Pause();
				}
				counter = 1;
				numSeconds = seconds;
			}
			catch
			{
				loadedFile = null;
				throw;
			}
		}

		public void Rewind()
		{
			flashBox.Rewind();
		}

		public bool Loop
		{
			set
			{
				flashBox.Loop = value;
			}
		}

		protected override void Dispose(bool disposing)
		{
			if(disposing)
			{
				TimeManager.TheInstance.UnmanageClass(this);
				flashBox.Dispose();
			}
			base.Dispose (disposing);
		}
		#region ITimedClass Members

		public void Start()
		{
			stopped = false;
			flashBox.Play();
		}

		public void FastForward(double timesRealTime)
		{
			flashBox.Speed = timesRealTime;
		}

		public void Reset()
		{
			flashBox.Rewind();
			counter = 0;
		}

		public void Stop()
		{
			stopped = true;
			flashBox.Pause();
		}

		#endregion

		public ZoomMode ZoomMode => flashBox.ZoomMode;

		public void ZoomWithLetterboxing ()
		{
			flashBox.ZoomWithLetterboxing();
		}

		public void ZoomWithCropping (PointF windowReferencePoint, PointF videoReferencePoint)
		{
			flashBox.ZoomWithCropping(windowReferencePoint, videoReferencePoint);
		}
	}
}