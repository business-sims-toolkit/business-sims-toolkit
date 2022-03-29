using System;
using System.Drawing;
using System.Windows.Forms;

using LibCore;

namespace Charts
{
	/// <summary>
	/// Summary description for XAxis.
	/// </summary>
	public class XAxis : Axis
	{
		protected bool shiftLabels;
		public bool ShiftLabels
		{
			set
			{
				shiftLabels = value;
			}
		}

		public bool _AutoLastLabelSlide = false;

		public bool AutoLastLabelSlide
		{
			set
			{
				_AutoLastLabelSlide = value;
			}
		}

		public bool _UseNewLabelCreation = false;

		public bool UseNewLabelCreation
		{
			set
			{
				_UseNewLabelCreation = value;
			}
		}

		public XAxis()
		{
			PrintLabel l = new PrintLabel();
			axisTitle = l;
			
			this.SuspendLayout();
			this.Controls.Add(axisTitle);
			this.ResumeLayout(false);

			align = ContentAlignment.TopCenter;
			l.TextAlign = align;

			shiftLabels = false;
		}

		public void adjustAlignment()
		{
			align = ContentAlignment.TopLeft;
			((PrintLabel)axisTitle).TextAlign = ContentAlignment.TopCenter;
		}

		/// <summary>
		/// We create an extra label at 0 position
		/// </summary>
		/// <param name="min"></param>
		/// <param name="max"></param>
		/// <param name="step"></param>
		public override void SetRange(int min, int max, int step)
		{
			this.SuspendLayout();

			if (_UseNewLabelCreation == false)
			{
				//Do the standard version 
				base.SetRange(min, max, step);
			}
			else
			{
				foreach (Label l in labels)
				{
					Controls.Remove(l);
					l.Dispose();
				}
				//
				_min = min;
				_max = max;
				_steps = (max - min) / step;
				//
				if (_steps >= 2)
				{
					labels = new PrintLabel[_steps+1];//-1];
					//
					for (int i = 0; i <= _steps; i++)
					{
						int num = min + step * i;
						labels[i] = new PrintLabel();
						labels[i].Text = CONVERT.ToStr(num);
						if (null != _font)
						{
							labels[i].Font = _font;
						}
						labels[i].TextAlign = align;
						labels[i].Visible = marksVisible;
						Controls.Add(labels[i]);
					}
				}
				//
				this.ResumeLayout(false);
				axisTitle.BringToFront();
				UpdateOmittedTopLabel();
				DoSize();
				this.Invalidate();
			}
		}

		/// <summary>
		/// 
		/// </summary>
		protected void newHandleSize()
		{
			int hh = (Height - 10) / 2;
			if (null != axisTitle)
			{
				axisTitle.Location = new Point(0, hh + 5);
				axisTitle.Size = new Size(Width, hh);
				//
				if (1 < _steps)
				{
					double stepWidth = Width / (double)_steps;
					double offset = 1;

					for (int i = 0; i <= labels.Length - 1; i++)
					{
						if ((int)offset + (int)stepWidth > Width)
						{
							offset -= 5;
						}
						int x = (int)offset;
						labels[i].Location = new Point(x, 5);
						//System.Diagnostics.Debug.WriteLine(labels[i].Text+"  "+labels[i].Location.X.ToString());

						// A wee hack to slide the final number left a bit...
						if (i == (labels.Length - 1))
						{
							if (_AutoLastLabelSlide)
							{
								int tw = this.Width;
								SizeF sf = MeasureString(labels[i].Font, labels[i].Text);
								int new_pos = tw - (((int)sf.Width) + 2);
								labels[i].Size = new Size((int)(sf.Width + 5)/2, hh);
								labels[i].Location = new Point(new_pos, 5);
								//System.Diagnostics.Debug.WriteLine(labels[i].Text + "  " + labels[i].Location.X.ToString()+ " SH");
							}
							else
							{
								labels[i].Size = new Size((int)(stepWidth - 10)/2, hh);
							}
						}
						else
						{
							labels[i].Size = new Size((int)(stepWidth/2), hh);
						}

						Color colour = Color.Transparent;
						if (stripeColours != null)
						{
							colour = stripeColours[i % stripeColours.Length];
						}
						labels[i].BackColor = colour;
						labels[i].ForeColor = textColour;

						offset += stepWidth;
					}
				}
			}
		}

		protected override void DoSize()
		{
			if (_UseNewLabelCreation == true)
			{
				newHandleSize();
			}
			else
			{
				int hh = (Height - 10) / 2;
				if (null != axisTitle)
				{
					axisTitle.Location = new Point(0, hh + 5);
					axisTitle.Size = new Size(Width, hh);
					//
					if (1 < _steps)
					{
						double stepWidth = Width / (double)_steps;
						double offset = (0.5 * stepWidth);

						for (int i = 0; i <= Math.Min(_steps, labels.Length) - 1; ++i)
						{
							if ((int)offset + (int)stepWidth > Width)
							{
								offset -= 5;
							}

							int x = (int)offset;

							if (shiftLabels)
							{
								// Shift the label left by half a column width (so it's centred on the bar).
								x -= (int)(stepWidth / 2);
							}

							labels[i].Location = new Point(x, 5);
							// A wee hack to slide the final number left a bit...
							if (i == _steps - 1)
							{
								if (_AutoLastLabelSlide)
								{
									int tw = this.Width;
									SizeF sf = MeasureString(labels[i].Font, labels[i].Text);
									int new_pos = tw - (((int)sf.Width) + 2);
									labels[i].Size = new Size((int)(sf.Width + 5), hh);
									labels[i].Location = new Point(new_pos, 5);
								}
								else
								{
									labels[i].Size = new Size((int)stepWidth - 10, hh);
								}
							}
							else
							{
								labels[i].Size = new Size((int)stepWidth, hh);
							}

							Color colour = Color.Transparent;
							if (stripeColours != null)
							{
								colour = stripeColours[i % stripeColours.Length];
							}
							labels[i].BackColor = colour;
							labels[i].ForeColor = textColour;

							offset += stepWidth;
						}
					}
				}
			}
		}

		protected override void OnPaint( PaintEventArgs e )
		{
			base.OnPaint(e);

			SetStroke( 4, textColour );
			MoveTo(0,0);
			LineTo(e, Width,0);
			//
			SetStroke( 2, textColour );

			if(1 < _steps)
			{
				double stepWidth = ((double)Width)/((double)_steps);
				double off = 0;// stepWidth;

				for(int i=0; i<=_steps; ++i)
				{
					double loff = off;

					if (i == _steps)
					{
						loff -= 3;
					}
					else if (i == 0)
					{
						loff += 2;
					}
					//
					MoveTo((int)loff,0);
					LineTo(e, (int)loff,6);
					//
					off += stepWidth;
				}
			}
		}
	}
}
