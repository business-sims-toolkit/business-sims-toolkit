using System;
using System.Drawing;

namespace BaseUtils
{
	/// <summary>
	/// Defines the type of scale to use
	/// for the Axis.
	/// </summary>
	public enum AxisType
	{
		Numeric,
		Categorical
	}

	/// <summary>
	/// Abstract base class representing an Axis
	/// on a chart.
	/// </summary>
	public abstract class Axis
	{
		protected int maxValue;
		protected int minValue;
		protected int majorInterval;
		protected int minorInterval;
		protected string label;
		protected Color labelColor;
		protected bool drawAxis;
		protected ChartContainer container;
		protected CategoryCollection cats;

		/// <summary>
		/// Creates an instance of Axis.
		/// </summary>
		/// <param name="container">The parent ChartContainer.</param>
		public Axis(ChartContainer container)
		{
			this.container = container;
			this.maxValue = 100;
			this.minValue = 0;
			this.majorInterval = 10;
			this.minorInterval = 5;
			this.label = String.Empty;
			this.labelColor = Color.Black;
			this.drawAxis = true;
			this.cats = new CategoryCollection(container);
		}

		/// <summary>
		/// Draws the Axis.
		/// </summary>
		public virtual void Draw()
		{
			cats.Draw();
		}

		/// <summary>
		/// Converts the specified value into chart coordinates.
		/// </summary>
		/// <param name="val">The value to convert.</param>
		/// <returns></returns>
		public virtual float ChartUnits(float val)
		{
			return 0f;
		}

		/// <summary>
		/// The maximum value for the scale.
		/// </summary>
		public int MaxValue
		{
			get 
			{ 
				if (AxisType == AxisType.Numeric)
					return maxValue; 
				else
					return cats.Count;
			}
			set 
			{ 
				if (AxisType == AxisType.Numeric)
					maxValue = value; 
			}
		}

		/// <summary>
		/// The minimum value for the scale.
		/// </summary>
		public int MinValue
		{
			get 
			{ 
				if (AxisType == AxisType.Numeric)
					return minValue; 
				else
					return 0;
			}
			set 
			{ 
				if (AxisType == AxisType.Numeric)
					minValue = value; 
			}
		}

		/// <summary>
		/// The major interval for the scale.
		/// </summary>
		public int MajorInterval
		{
			get 
			{ 
				if (AxisType == AxisType.Numeric)
					return majorInterval; 
				else
					return 1;
			}
			set 
			{ 
				if (AxisType == AxisType.Numeric)
					if (majorInterval > 0)
						majorInterval = value; 
					else
						majorInterval = 1;
				else
					majorInterval = 1;
			}
		}

		/// <summary>
		/// The minor interval for the scale.
		/// </summary>
		public int MinorInterval
		{
			get 
			{ 
				if (AxisType == AxisType.Numeric)
					return minorInterval; 
				else
					return 1;
			}
			set 
			{ 
				if (AxisType == AxisType.Numeric)
					if (value > 0)
						minorInterval = value; 
					else
						minorInterval = 1;
				else
					minorInterval = 1;
			}
		}

		/// <summary>
		/// The label for the Axis.
		/// </summary>
		public string Label
		{
			get { return label; }
			set { label = value; }
		}

		/// <summary>
		/// The colour for the Axis label.
		/// </summary>
		public Color LabelColor
		{
			get { return labelColor; }
			set { labelColor = value; }
		}

		/// <summary>
		/// Determines whether or not the Axis should be drawn.
		/// </summary>
		public bool DrawAxis
		{
			get { return drawAxis; }
			set { drawAxis = value; }
		}

		/// <summary>
		/// Used to map Series values to Chart coordinates.
		/// </summary>
		public virtual float PlotUnit
		{
			get { return 1; }
		}

		/// <summary>
		/// The parent ChartContainer.
		/// </summary>
		public ChartContainer Container
		{
			get { return container; }
		}

		/// <summary>
		/// The type of the Axis.
		/// </summary>
		public virtual AxisType AxisType
		{
			get { return AxisType.Numeric; }
		}

		/// <summary>
		/// The Categories associated with the Axis.
		/// </summary>
		public CategoryCollection Categories
		{
			get { return cats; }
		}
	}
}
