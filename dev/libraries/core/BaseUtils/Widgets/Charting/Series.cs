using System.Drawing;

namespace BaseUtils
{
	/// <summary>
	/// Represents a Series on a Chart. Contains a set of SeriesValues.
	/// </summary>
	public class Series
	{
		string label;
		Color color;
		float weight;
		bool showValues;
		SeriesValueCollection values;

		/// <summary>
		/// Creates an instance of Series.
		/// </summary>
		/// <param name="label">The label of the Series.</param>
		public Series(string label)
		{
			Init(label, Color.Black, 1f);
		}

		/// <summary>
		/// Creates an instance of Series.
		/// </summary>
		/// <param name="label">The label of the Series.</param>
		/// <param name="color">The colour of the Series.</param>
		public Series(string label, Color color, float weight)
		{
			Init(label, color, weight);
		}

		void Init(string label, Color color, float weight)
		{
			this.label = label;
			this.color = color;
			this.weight = weight;
			this.showValues = false;
			this.values = new SeriesValueCollection();
		}

		/// <summary>
		/// The label of the Series.
		/// </summary>
		public string Label
		{
			get { return label; }
			set { label = value; }
		}

		/// <summary>
		/// The colour of the Series.
		/// </summary>
		public Color Color
		{
			get { return color; }
			set { color = value; }
		}

		/// <summary>
		/// The pen weight of the Series.
		/// </summary>
		public float Weight
		{
			get { return weight; }
			set { weight = value; }
		}

		/// <summary>
		/// Whether or not to display the values above the Series points.
		/// </summary>
		public bool ShowValues
		{
			get { return showValues; }
			set { showValues = value; }
		}

		/// <summary>
		/// The collection of Series Values.
		/// </summary>
		public SeriesValueCollection Values
		{
			get { return values; }
		}
	}
}
