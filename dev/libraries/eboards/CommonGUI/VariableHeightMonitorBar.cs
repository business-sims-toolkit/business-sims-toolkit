using System;
using System.Windows.Forms;
using System.Drawing;

namespace CommonGUI
{
	public class VariableHeightMonitorBar : AudioStyleMonitorBar
	{
		protected struct ColouredZone
		{
			public Panel [] lights;
			public Color activeColour;
			public Color inactiveColour;
		}

		protected ColouredZone [] zones;

		public override int CurrentLevel
		{
			set
			{
				current_level = Math.Max(-1, Math.Min(zones.Length, value));
				SetupDisplay();
			}

			get
			{
				return current_level;
			}
		}
		
		public VariableHeightMonitorBar (int [] lightCounts)
			: base ()
		{
			zones = new ColouredZone [lightCounts.Length];
			for (int i = 0; i < lightCounts.Length; i++)
			{
				zones[i].inactiveColour = Color.Gray;
				zones[i].activeColour = Color.Black;
				zones[i].lights = new Panel [lightCounts[i]];
				for (int j = 0; j < zones[i].lights.Length; j++)
				{
					zones[i].lights[j] = new Panel ();
					Controls.Add(zones[i].lights[j]);
				}
			}

			SuspendLayout();
			ResumeLayout(false);

			Resize += VariableHeightMonitorBar_Resize;
		}

		public override void SetLevelColor (int level, Color color)
		{
			zones[level].activeColour = color;
		}

		public void SetLevelColors (Color [] colors)
		{
			if (colors.Length != zones.Length)
			{
				throw new Exception ("Number of zones doesn't match number of colours!");
			}
			
			for (int i = 0; i < colors.Length; i++)
			{
				zones[i].activeColour = colors[i];
			}
		}

		public override void SetEmptyColor(Color newEmptyColor)
		{
			for (int i = 0; i < zones.Length; i++)
			{
				zones[i].inactiveColour = newEmptyColor;
			}

			SetupDisplay();
		}

		protected override void SetupDisplay ()
		{
			for (int i = 0; i < zones.Length; i++)
			{
				Color color;
				if (i <= current_level)
				{
					color = zones[i].activeColour;
				}
				else
				{
					color = zones[i].inactiveColour;
				}

				foreach (Panel p in zones[i].lights)
				{
					p.BackColor = color;
				}
			}
		}

		new protected void DoSize()
		{
			// Count the lights.
			int lights = 0;
			foreach (ColouredZone zone in zones)
			{
				lights += zone.lights.Length;
			}

			int lightHeight = Height / lights;
			int y = Height - lightHeight - 1;
			for (int i = 0; i < zones.Length; i++)
			{
				for (int j = 0; j < zones[i].lights.Length; j++)
				{
					zones[i].lights[j].Location = new Point (0, y);
					zones[i].lights[j].Size = new Size (Width, lightHeight - 1);
					y -= lightHeight;
				}
			}
		}

		void VariableHeightMonitorBar_Resize (object sender, EventArgs e)
		{
			DoSize();
		}
	}
}