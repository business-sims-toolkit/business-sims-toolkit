using System;
using System.Windows.Forms;
using System.Drawing;
using CoreUtils;
using LibCore;

namespace CommonGUI
{
	public class AudioStyleMonitorBar : BasePanel
	{
		protected Panel[] lights;
		protected Panel[,] mirroredLights;
		protected Color[] colors;
		protected int mirrorCount = 0;

	    Color [] emptyColours;

		protected int current_level;
		protected int min_level = -1;
		protected int max_levels = 5;

        int verticalBorderSize = SkinningDefs.TheInstance.GetIntData("awt_vertical_border_height", 1);

		public virtual int CurrentLevel
		{
			set
			{
				current_level = value;
				if(current_level > lights.Length-1) current_level = lights.Length-1;
				if(current_level < min_level) current_level = min_level;
				SetupDisplay();
			}

			get
			{
				return current_level;
			}
		}

		public virtual void SetEmptyColor(Color newEmptyColor)
		{
		    if (! string.IsNullOrEmpty(SkinningDefs.TheInstance.GetData("awt_background_colour_level_0")))
		    {
		        return;
		    }

		    for(int i=0; i<max_levels; ++i)
			{
				if (i<lights.Length)
				{
					if (lights[i] != null)
					{
						if (lights[i].BackColor == emptyColours[i])
						{
							lights[i].BackColor = newEmptyColor;
						}
					}
				}
				if ((mirroredLights != null) && (i < mirroredLights.Length))
				{
					for (int j = 0; j < mirrorCount; j++)
					{
						if (mirroredLights[i, j] != null)
						{
                            if (mirroredLights[i, j].BackColor == emptyColours[i])
							{
								mirroredLights[i, j].BackColor = newEmptyColor;
							}
						}
					}
				}
			}

		    for (int i = 0; i < max_levels; i++)
		    {
		        emptyColours[i] = newEmptyColor;
		    }
		}

		protected AudioStyleMonitorBar ()
		{
		}
		
		public AudioStyleMonitorBar(int num_levels)
		{
			max_levels = num_levels;
			SuspendLayout();

            emptyColours = new Color [num_levels];
		    for (int level = 0; level < num_levels; level++)
		    {
                emptyColours[level] = SkinningDefs.TheInstance.GetColorDataGivenDefault(string.Format("awt_background_colour_level_{0}", level), Color.DarkGray);
		    }

		    lights = new Panel[num_levels];
			colors = new Color[num_levels];
			current_level = 0;

			for(int i=0; i<num_levels; ++i)
			{
				Panel panel = new Panel();
				panel.BackColor = emptyColours[i];
				Controls.Add(panel);
				lights[i] = panel;
			}

			ResumeLayout(false);

			SetupDisplay();

			Resize += AudioStyleMonitorBar_Resize;
		}


		public virtual void SetLevelColor(int level, Color color)
		{
			colors[level] = color;
		}

		protected virtual void SetupDisplay()
		{
			for(int i=0; i<lights.Length; ++i)
			{
				if(i <= current_level)
				{
					lights[i].BackColor = colors[i];
				}
				else
				{
					lights[i].BackColor = emptyColours[i];
				}

				if (mirroredLights != null)
				{
					for (int j = 0; j < mirrorCount; j++)
					{
						mirroredLights[i, j].BackColor = lights[i].BackColor;
					}
				}
			}
		}

		protected virtual void DoSize()
		{
			int mainCellStride = Height / lights.Length;
			Size cellSize = new Size (Width, mainCellStride);

			if (mirrorCount > 0)
			{
				cellSize.Height = cellSize.Height / mirrorCount;
				mainCellStride = cellSize.Height * mirrorCount;
			}
			cellSize.Height -= verticalBorderSize;

			for (int i = 0; i < lights.Length; i++)
			{
				lights[i].Size = cellSize;
				lights[i].Location = new Point (0, Height - (mainCellStride * (i + 1)));

				if (mirroredLights != null)
				{
					for (int j = 0; j < mirrorCount; j++)
					{
						mirroredLights[i, j].Size = cellSize;
                        mirroredLights[i, j].Location = new Point(0, Height - (mainCellStride * (i + 1)) - (((j + 1) * (cellSize.Height + verticalBorderSize))));
					}
				}
			}
		}

		void AudioStyleMonitorBar_Resize(object sender, EventArgs e)
		{
			DoSize();
		}

		public void AddDaftVerticalDividers (int mirrors)
		{
			SuspendLayout();

			mirrorCount = mirrors;

			mirroredLights = new Panel [lights.Length, mirrorCount];

			for (int i = 0; i < lights.Length; i++)
			{
				for (int j = 0; j < mirrorCount; j++)
				{
					Panel panel = new Panel();
                    panel.BackColor = emptyColours[i];
					Controls.Add(panel);
					mirroredLights[i, j] = panel;
				}
			}

			ResumeLayout(false);
		}
	}
}