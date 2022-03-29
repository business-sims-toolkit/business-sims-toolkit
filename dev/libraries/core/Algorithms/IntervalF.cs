namespace Algorithms
{
	public struct IntervalF
	{
		public float Min;
		public float Size;

		public float Max
		{
			get => Min + Size;

			set => Size = value - Min;
		}

		public IntervalF (float min, float size)
		{
			Min = min;
			Size = size;
		}
	}
}