using System;
using CoreUtils;

using Media;

namespace CommonGUI
{
	/// <summary>
	/// Summary description for KlaxonSingleton.
	/// </summary>
	public class KlaxonSingleton : ITimedClass
	{
		SoundPlayer soundPlayer;

		public static readonly KlaxonSingleton TheInstance = new KlaxonSingleton();
		protected System.Windows.Forms.Timer timer;
		protected System.Windows.Forms.Timer resume_timer;
		protected string lastFilePlayed = "";
		protected string resumeFile = "";
		protected bool ignoreStopTimer = false;
		string lastLoopingFilePlayed;

		public KlaxonSingleton()
		{
			soundPlayer = new SoundPlayer();
			timer = new System.Windows.Forms.Timer();
			timer.Tick += timer_Tick;

			resume_timer = new System.Windows.Forms.Timer();
			resume_timer.Tick += resume_timer_Tick;
		}

		public void Dispose()
		{
			soundPlayer.Dispose();
			timer.Enabled = false;
			timer.Dispose();
		}

		public void setIgnoreStop(bool flag)
		{
			ignoreStopTimer = flag;
		}

		public void PlayAudio(string file, bool loop)
		{
			if (loop)
			{
				lastLoopingFilePlayed = file;
			}

			lock(this)
			{
				// : Fix for 3992: full (non-PlayRounds) builds use the .NET 2 class
				// SoundPlayer, rather than our own internal stuff, and they don't expose a Length
				// property, so the old protection against re-triggering a sound didn't work.

				// Don't play if we've recently started playing the same sound.
				if ((file == lastFilePlayed) && timer.Enabled)
				{
					return;
				}

				soundPlayer.Play(file, loop);
				lastFilePlayed = file;

				timer.Interval = (int)(1000 * 0.1); // Can't trigger sounds more frequently than every 0.1s.
				timer.Start();
			}
		}


		public void PlayAudioWithAutoResume(string file, string resumeSoundfile)
		{
            PlayAudioWithAutoResume(file, resumeSoundfile, false);
		}

        public void PlayAudioWithAutoResume(string file, string resumeSoundfile, bool loop)
        {
            lock (this)
            {
                resume_timer.Stop();

                resumeFile = resumeSoundfile;
                soundPlayer.Play(file, loop);

                resume_timer.Interval = 5 * 1000;
                resume_timer.Start();
            }
        }



		#region ITimedClass Members

		public void Start()
		{
			soundPlayer.Resume();
		}

		public void Stop()
		{
			if (ignoreStopTimer==false)
			{
				soundPlayer.Pause();
			}
		}

		public void Reset()
		{
		}

		public void FastForward(double timesRealTime)
		{
		}

		#endregion

		void timer_Tick(object sender, EventArgs e)
		{
			timer.Stop();
		}

		void resume_timer_Tick(object sender, EventArgs e)
		{
			resume_timer.Stop();
			if (string.IsNullOrEmpty(resumeFile) == false)
			{
                soundPlayer.Play(resumeFile, true);
			}
		}

		public void PlayAudioThenResume (string filename)
		{
			PlayAudioWithAutoResume(filename, lastLoopingFilePlayed);
		}

		public void SetDefaultLoop (string filename)
		{
			lastLoopingFilePlayed = filename;
		}
	}
}