using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace NeowUi
{
	public class Flasher<T> where T: struct, IEquatable<T>
	{
		T? lastKnownState;

		Color offColour;
		Color onColour;

		double offTime;
		double onTime;
		double totalActiveDuration;

		bool flashing;
		bool flashStateIsOn;
		double time;

		public Flasher (Color onColour, double onTime, Color offColour, double offTime, double totalActiveDuration)
		{
			this.onColour = onColour;
			this.onTime = onTime;

			this.offColour = offColour;
			this.offTime = offTime;

			this.totalActiveDuration = totalActiveDuration;

			lastKnownState = null;

			flashing = false;
		}

		public void SetState (T state)
		{
			IEqualityComparer<T> comparer = EqualityComparer<T>.Default;

			if (lastKnownState.HasValue
				&& (! comparer.Equals(lastKnownState.Value, state)))
			{
				time = 0;
				flashing = true;
				flashStateIsOn = true;
			}

			lastKnownState = state;
		}

		public T? LastKnownState
		{
			get
			{
				return lastKnownState;
			}
		}

		public void AdvanceTime (double dt)
		{
			time += dt;

			double reducedTime = time;
			if (flashing)
			{
				bool changed;

				do
				{
					changed = false;

					if (flashStateIsOn
						&& (reducedTime >= onTime))
					{
						reducedTime -= onTime;
						flashStateIsOn = false;
						changed = true;
					}

					if ((! flashStateIsOn)
						&& (reducedTime >= offTime))
					{
						reducedTime -= offTime;
						flashStateIsOn = true;
						changed = true;
					}
				}
				while (changed);

				if (time >= totalActiveDuration)
				{
					flashStateIsOn = false;
					flashing = false;
				}
			}
		}

		public bool Active
		{
			get
			{
				return flashing;
			}
		}

		public Color GetColour ()
		{
			if (flashing && flashStateIsOn)
			{
				return onColour;
			}
			else
			{
				return offColour;
			}
		}
	}
}