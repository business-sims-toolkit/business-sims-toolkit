using System;
using System.Windows.Forms;
using System.Drawing;

using LibCore;
using CoreUtils;
using Network;
using CommonGUI;

namespace OpsGUI
{
	/// <summary>
	/// It should be able to alter the TimedFlashPlayer to be optionally unmanaged 
	/// But the altered class seems to have problems 
	/// 
	/// This is just a simple seperate class 
	/// </summary>
	public class FreeTimedFlashPlayer : FlickerFreePanel
	{
		protected FlashBox flashBox;
		protected StopControlledTimer timer;
		protected bool stopped = false;

		protected string loadedFile = "";
		protected int lastFrameNum;

		public FreeTimedFlashPlayer()
		{
			flashBox = new FlashBox();
			this.Controls.Add(flashBox);

			flashBox.SetFlashBackground(Color.LightGray.ToArgb());
			//flashBox.SetFlashBackground(0);

			timer = new StopControlledTimer();
			timer.Tick += new EventHandler(timer_Tick);

			this.Resize += new EventHandler(FreeTimedFlashPlayer_Resize);
		}

		private void FreeTimedFlashPlayer_Resize(object sender, EventArgs e)
		{
			//flashBox.Size = this.Size;
			flashBox.SetSize(this.Width, this.Height);
		}

		protected double numSeconds = 0;

		public void PlayFile(string file)
		{
			PlayFile(file,0);
		}

		protected int counter = 0;

		public void PlayFile(string file, long seconds)
		{
			string fullfilename = string.Empty;
 
			fullfilename = AppInfo.TheInstance.Location + "\\flash\\" + file;
			//
			if (System.IO.File.Exists(fullfilename))
			{
				loadedFile = fullfilename;
				flashBox.LoadFile(fullfilename);
				flashBox.Rewind();
				if(!stopped)
				{
					flashBox.Play();
				}
				counter = 1;
				numSeconds = seconds;
				if(seconds > 0)
				{
					timer.Interval = 1000;
					if(!stopped)
					{
						timer.Start();
					}
				}
				else
				{
					timer.Stop();
				}
			}
			else
			{
				//throw( new Exception("Flash file " + fullfilename + " does not exist."));
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

		private void timer_Tick(object sender, EventArgs e)
		{
			double curFrame = flashBox.CurrentFrame();
			double maxFrames = flashBox.TotalFrames;

			flashBox.FrameNum = (int)(counter*(maxFrames/numSeconds));
			flashBox.Play();
			++counter;
		}

		protected override void Dispose(bool disposing)
		{
			if(disposing)
			{
				timer.Stop();
				flashBox.Dispose();
			}
			base.Dispose (disposing);
		}

		public void Start()
		{
			// Flash doesn't always start the flash if it was stopped before it got
			// going. Therefore reload it...
			stopped = false;
			/*
						if(loadedFile != "")
						{
							flashBox.LoadFile(loadedFile);
							flashBox.FrameNum = this.lastFrameNum;
						}
						else
						{
							flashBox.Rewind();
						}*/

			if(numSeconds > 0)
			{
				timer.Start();
			}
			flashBox.Play();
		}

		public void FastForward(double timesRealTime)
		{
			// TODO:  Add FreeTimedFlashPlayer.FastForward implementation
		}

		public void Reset()
		{
			flashBox.Rewind();
			counter = 0;
		}

		public void Stop()
		{
			stopped = true;
			timer.Stop();
			lastFrameNum = flashBox.CurrentFrame();
			flashBox.Stop();
		}

	}
}
