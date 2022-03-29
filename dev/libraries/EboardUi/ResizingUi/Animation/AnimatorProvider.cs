using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ResizingUi.Animation
{
	public class AnimatorProvider : IDisposable
	{
		Timer timer;
		List<PulseAnimator> pulseAnimators;

		public AnimatorProvider ()
		{
			timer = new Timer { Interval = 1000 / 25 };
			timer.Tick += timer_Tick;
			timer.Start();

			pulseAnimators = new List<PulseAnimator> ();
		}

		public void Dispose ()
		{
			foreach (var pulseAnimator in pulseAnimators)
			{
				pulseAnimator.Dispose();
			}

			timer.Dispose();
		}

		void timer_Tick (object sender, EventArgs args)
		{
			foreach (var pulseAnimator in pulseAnimators)
			{
				pulseAnimator.UpdateFromTime(timer.Interval / 1000.0f);
			}
		}

		public PulseAnimator CreatePulseAnimator ()
		{
			var pulseAnimator = new PulseAnimator();
			pulseAnimators.Add(pulseAnimator);

			return pulseAnimator;
		}
	}
}