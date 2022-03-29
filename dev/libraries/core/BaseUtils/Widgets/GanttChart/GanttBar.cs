using System.Drawing;

namespace BaseUtils
{
	/// <summary>
	/// Represents a bar in a Gantt Chart.
	/// </summary>
	public class GanttBar
	{
		string desc;
		int startPeriod;
		int duration;
		int cost;
		Color barColor;
		Color barGradientColor;
		RectangleF rect;
		/// <summary>
		/// Constructor 1: Represents one bar in a Gantt chart
		/// </summary>
		/// <param name="desc">The description.</param>
		/// <param name="startPeriod">The start period of the bar in seconds since midnight</param>
		/// <param name="duration">The duration in seconds</param>
		/// <param name="cost">The cost associated with the bar</param>
		public GanttBar(string desc, int startPeriod, int duration, int cost)
		{
			Initialise(desc, startPeriod, duration, cost, Color.Black);
		}
		/// <summary>
		/// Constructor 2: Represents one bar in a Gantt chart
		/// </summary>
		/// <param name="desc">The description.</param>
		/// <param name="startPeriod">The start period of the bar in seconds since midnight</param>
		/// <param name="duration">The duration in seconds</param>
		/// <param name="cost">The cost associated with the bar</param>
		/// <param name="barColor">The colour associated with the bar</param>
		/// 
		public GanttBar(string desc, int startPeriod, int duration, int cost, Color barColor)
		{
			Initialise(desc, startPeriod, duration, cost, barColor);
		}

		/// <summary>
		/// Constructor 3: Used when loading this bar from an xml file
		/// </summary>
		public GanttBar()
		{
		}

		/// <summary>
		/// Initializes an instance of GanttBar.
		/// </summary>
		/// <param name="desc"></param>
		/// <param name="startPeriod"></param>
		/// <param name="duration"></param>
		/// <param name="cost"></param>
		/// <param name="barColor"></param>
		void Initialise(string desc, int startPeriod, int duration, int cost, Color barColor)
		{
			this.desc = desc;
			this.startPeriod = startPeriod;
			this.duration = duration;
			this.cost = cost;
			this.BarColorStart = barColor;
		}

		/// <summary>
		/// Description of the incident.
		/// </summary>
		public string Desc
		{
			get { return desc; }
			set { desc = value; }
		}

		/// <summary>
		/// The start period in seconds since midnight.
		/// </summary>
		public int Start
		{
			get { return startPeriod; }
			set { startPeriod = value; }
		}

		/// <summary>
		/// The duration in seconds.
		/// </summary>
		public int Duration
		{
			get { return duration; }
			set { duration = value; }
		}

		/// <summary>
		/// The cost associated with this bar.
		/// </summary>
		public int Cost
		{
			get { return cost; }
			set { cost = value; }
		}

		/// <summary>
		/// The colour of the bar.
		/// </summary>
		public Color BarColorStart
		{
			get { return barColor; }
			set 
			{
				barColor = value; 
				barGradientColor = DarkerColor(barColor);
			}
		}

		/// <summary>
		/// The gradient color of the bar.
		/// </summary>
		public Color BarColorEnd
		{
			get { return barGradientColor; }
		}

		/// <summary>
		/// Converts the specified colour to a darker
		/// colour.
		/// </summary>
		/// <param name="c">The colour to convert.</param>
		/// <returns>Color</returns>
		Color DarkerColor(Color c)
		{
			byte r, g, b;
			r = (byte)(c.R * 0.55);
			g = (byte)(c.G * 0.55);
			b = (byte)(c.B * 0.55);

			return Color.FromArgb(255, r, g, b);
		}

		/// <summary>
		/// The bounds of the bar.
		/// </summary>
		public RectangleF Bounds
		{
			get { return rect; }
			set { rect = value; }
		}

		public override string ToString()
		{
			return "start: " + startPeriod.ToString() + " dur: " + duration.ToString();
		}
	}
}
