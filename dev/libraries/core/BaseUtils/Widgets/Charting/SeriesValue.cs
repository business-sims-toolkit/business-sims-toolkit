namespace BaseUtils
{
	/// <summary>
	/// Represents an element in a collection of Series Values.
	/// </summary>
	public class SeriesValue
	{
		float val;

		/// <summary>
		/// Creates an instance of SeriesValue.
		/// </summary>
		/// <param name="val"></param>
		public SeriesValue(float val)
		{
			this.val = val;
		}

		/// <summary>
		/// The value.
		/// </summary>
		public float Value
		{
			get { return val; }
			set { val = value; }
		}
	}
}
