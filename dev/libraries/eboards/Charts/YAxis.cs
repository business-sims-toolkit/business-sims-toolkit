using System;
using System.Drawing;
using CoreUtils;

namespace Charts
{
	public class YAxis : Axis
	{
		protected int labelHeight = 24;
		//
		public YAxis()
		{
			LocationChanged += YAxis_LocationChanged;
		}

		void YAxis_LocationChanged(object sender, EventArgs e)
		{
			DoSize();
		}

		protected override void DoSize()
		{
			int ww = (Width-10)/2;
			if(ww > 20) ww = 20;
			//
			if(null != axisTitle)
			{
				axisTitle.Size = new Size(ww,Height);

				int labelLeft;

				if(align == ContentAlignment.MiddleRight)
				{
					axisTitle.Location = new Point(0,0);
					labelLeft = axisTitle.Right;
				}
				else
				{
					axisTitle.Location = new Point(Width-5-ww,0);
					labelLeft = 0;
				}
				//

				if(0 < _steps)
				{
					double stepHeight = Height / (double)_steps;
					int lHeight = labelHeight/2;
				    int gap = 10;

					for(int i=1; i<=_steps; ++i)
					{
						if (labels.Length > i-1)
						{
							double off = Height-i*stepHeight;
							int y = (int)off;

							if (LabelAlignment == "middle")
							{
								//draw in the center between this step and one below
								labels[i - 1].Location = new Point(labelLeft, (y + (int) (stepHeight / 2))- (lHeight / 2));
								labels[i - 1].TextAlign = ContentAlignment.MiddleCenter;
								labels[i - 1].Size = new Size (Width - ww - 5 + 5, (int) (Height - ((i - 1) * stepHeight)) - y - 1);
							}
							else if (LabelAlignment == "centre_on_tick")
							{
								labels[i - 1].Location = new Point(labelLeft, y - lHeight);
								labels[i - 1].TextAlign = ContentAlignment.MiddleCenter;
								labels[i - 1].Size = new Size(Width - ww - 5 + 5, labelHeight);
							}
							else if (LabelAlignment == "centre")
							{
								labels[i - 1].Location = new Point(labelLeft, y - (lHeight/2));
								labels[i - 1].TextAlign = ContentAlignment.MiddleCenter;
								labels[i - 1].Size = new Size(Width - ww - 5 + 5, (int)(Height - ((i - 1) * stepHeight)) - y - 1);
							}
							else if (LabelAlignment == "right")
							{
								labels[i - 1].Location = new Point(axisTitle.Right-gap, y - (lHeight / 2));
								labels[i - 1].TextAlign = ContentAlignment.MiddleRight;
								labels[i - 1].Size = new Size(Width - ww - 5 + 5, (int)(Height - ((i - 1) * stepHeight)) - y - 1);
							}
							else if (LabelAlignment == "centre_properly")
							{
							    labels[i - 1].Location = new Point (labelLeft, y);
							    labels[i - 1].TextAlign = ContentAlignment.MiddleRight;
							    labels[i - 1].Size = new Size (Width - ww - gap, (int) (Height - ((i - 1) * stepHeight)) - y - 1);
							}
							else
                            {
                                labels[i - 1].Location = new Point(labelLeft, y - lHeight);
                                labels[i - 1].TextAlign = ContentAlignment.TopCenter;
								labels[i - 1].Size = new Size(Width - ww - 5 + 5, (int)(Height - ((i - 1) * stepHeight)) - y - 1);
							}

						    Color colour = SkinningDefs.TheInstance.GetColorData("gantt_text_colour", Color.Black);
                            labels[i - 1].ForeColor = colour;
						    labels[i - 1].BackColor = Color.Transparent;
						}
					}
				}
			}
		}
	}
}
