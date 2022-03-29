using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ResizingUi.Interfaces;

namespace ResizingUi.Animation
{
	public class PulseAnimator : imator<float>
	{
		float maxScale;
		float time;
		int pulsesInDuration;
		float duration;
		bool isPulsing;

		public PulseAnimator ()
		{
		}

		public void Dispose ()
		{
		}

		public float Timer => time;
		public float Duration => duration;

		public float GetValue ()
		{
			if (isPulsing)
			{
				var pulseDuration = duration / (0.5f + pulsesInDuration);
				return 1 + (((maxScale - 1) / 2) * (1 + (float) Math.Cos(2 * Math.PI * time / pulseDuration)));
			}
			else
			{
				return 1;
			}
		}

		void OnUpdate ()
		{
			Update?.Invoke(this, EventArgs.Empty);
		}

		public event EventHandler Update;

		internal void UpdateFromTime (float dt)
		{
			bool wasPulsing = isPulsing;

			time += dt;
			if (time >= duration)
			{
				time = duration;
				isPulsing = false;
			}

			if (wasPulsing)
			{
				OnUpdate();
			}
		}

		public void StartPulsing (float totalDuration, int numberOfPulsesInDuration, float maxScale)
		{
			this.maxScale = maxScale;
			pulsesInDuration = numberOfPulsesInDuration;
			duration = totalDuration;
			time = 0;
			isPulsing = true;
		}
	}
}
