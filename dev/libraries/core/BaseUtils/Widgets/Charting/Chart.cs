namespace BaseUtils
{
	/// <summary>
	/// Abstract base class defining the basic properties and actions
	/// of a Chart.
	/// </summary>
	public abstract class Chart
	{
		protected Axis axis;
		protected Series series;

		/// <summary>
		/// Creates an instance of Cart.
		/// </summary>
		/// <param name="axis"></param>
		/// <param name="series"></param>
		public Chart(Axis axis, Series series)
		{
			this.axis = axis;
			this.series = series;
		}

		/// <summary>
		/// Draws the Chart on the specified ChartContainer.
		/// </summary>
		/// <param name="container">The ChartContainer to draw the chart in.</param>
		public virtual void Draw(ChartContainer container)
		{
		}

		/// <summary>
		/// The axis associated with the Chart.
		/// </summary>
		public Axis Axis
		{
			get { return axis; }
		}

		/// <summary>
		/// The series associated with the Chart.
		/// </summary>
		public Series Series
		{
			get { return series; }
		}
	}
}
