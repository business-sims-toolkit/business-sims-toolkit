using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Algorithms;
using CommonGUI;
using CoreUtils;
using LibCore;
using Network;

namespace ResizingUi
{
	public class PowerLevelPanel : CascadedBackgroundPanel
	{
	    readonly string title;
	    readonly Node powerNode;
	    readonly int zones;
	    readonly ZonePowerLevelGetter powerLevelGetter;

		public delegate int ZonePowerLevelGetter (Node powerNode, int zoneNumber);

		IWatermarker watermarker;

		int leftMargin;
		int rightMargin;
		int topMargin;
		int bottomMargin;
		int columnGap;
		int rowGap;

		public IWatermarker Watermarker
		{
			get => watermarker;

			set
			{
				watermarker = value;
				Invalidate();
			}
		}

		public PowerLevelPanel (string title, Node powerNode, int zones, ZonePowerLevelGetter powerLevelGetter)
		{
			this.title = title;
			this.powerNode = powerNode;
			this.zones = zones;
			this.powerLevelGetter = powerLevelGetter;

			powerNode.AttributesChanged += powerNode_AttributesChanged;

			leftMargin = 20;
			rightMargin = 20;
			topMargin = 20;
			bottomMargin = 30;
			columnGap = 4;
			rowGap = 4;
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing)
			{
				powerNode.AttributesChanged -= powerNode_AttributesChanged;
			}

			base.Dispose(disposing);
		}

		void powerNode_AttributesChanged (Node sender, ArrayList attributes)
		{
			Invalidate();
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);

			Invalidate();
		}

		int GetColourIndex (int row, int rows, int maxValue, int [] transitionValues)
		{
			for (int i = 0; i < transitionValues.Length; i++)
			{
				if ((row * maxValue / rows) <= transitionValues[i])
				{
					return i;
				}
			}

			return transitionValues.Length - 1;
		}

		protected override void OnPaint (PaintEventArgs e)
		{
			base.OnPaint(e);

			using (var backBrush = new SolidBrush (Color.FromArgb(CoreUtils.SkinningDefs.TheInstance.GetIntData("cascaded_background_transparency", 255), BackColor)))
			{
				e.Graphics.FillRectangle(backBrush, new Rectangle(0, 0, Width, Height));
			}

			watermarker?.Draw(this, e.Graphics);

			Color [] fullColours =
			{
				SkinningDefs.TheInstance.GetColorDataGivenDefault("power_graph_colour_ok", Color.Green),
				SkinningDefs.TheInstance.GetColorDataGivenDefault("power_graph_colour_high", Color.Yellow),
				SkinningDefs.TheInstance.GetColorDataGivenDefault("power_graph_colour_warning", Color.Orange),
				SkinningDefs.TheInstance.GetColorDataGivenDefault("power_graph_colour_overload", Color.Red)
			};

			Color [] emptyColours =
			{
				SkinningDefs.TheInstance.GetColorDataGivenDefault("power_graph_background_colour_ok", Color.Gray),
				SkinningDefs.TheInstance.GetColorDataGivenDefault("power_graph_background_colour_high", Color.Gray),
				SkinningDefs.TheInstance.GetColorDataGivenDefault("power_graph_background_colour_warning", Color.Gray),
				SkinningDefs.TheInstance.GetColorDataGivenDefault("power_graph_background_colour_overload", Color.Gray)
			};

			int [] transitionValues =
			{
				55,
				70,
				80
			};

			int max = 120;
			int rows = 24;
			int yStep = 20;

			int columnWidth = (Width - (leftMargin + rightMargin) - ((zones - 1) * columnGap)) / zones;
			int rowHeight = (Height - topMargin - bottomMargin - ((rows - 1) * rowGap)) / rows;
			int maxDigits = 3;

			float fontSize = Math.Min(yStep * ((rowHeight + rowGap) * rows) * 1.0f / max, leftMargin / maxDigits);

			using (var textBrush = new SolidBrush (ForeColor))
			using (var axisFont = SkinningDefs.TheInstance.GetPixelSizedFont(fontSize, FontStyle.Bold))
			using (var columnFont = FontScalerExtensions.GetFontToFit(this, FontStyle.Bold, "9", new SizeF (columnWidth, bottomMargin)))
			{
				for (int level = 0; level <= max; level += yStep)
				{
					int tickLength = 4;
					int row = level * rows / max;
					int y = Height - bottomMargin - (row * (rowHeight + rowGap));
					int height = rowHeight * rows * yStep / max;

					e.Graphics.DrawString(CONVERT.ToStr(level), axisFont, textBrush,
						new RectangleFFromBounds { Left = 0, Right = leftMargin - tickLength, Top = y - (height / 2), Height = height }.ToRectangleF(),
						new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center });

					using (var pen = new Pen(ForeColor, 1))
					{
						e.Graphics.DrawLine(pen, leftMargin - tickLength, y, leftMargin, y);
					}
				}

				int x = leftMargin;

				for (int j = 0; j < zones; j++)
				{
					int blocksLit = powerLevelGetter(powerNode, 1 + j) * rows / max;

					for (int i = 0; i < rows; i++)
					{
						int colourIndex = GetColourIndex(i, rows, max, transitionValues);

						Color colour = emptyColours[colourIndex];

						if (i < blocksLit)
						{
							colour = fullColours[colourIndex];
						}

						using (Brush brush = new SolidBrush(colour))
						{
							e.Graphics.FillRectangle(brush,
								new Rectangle(x, Height - bottomMargin - (i * (rowHeight + rowGap)) - rowHeight, columnWidth, rowHeight));
						}
					}

					e.Graphics.DrawString(CONVERT.Format("{0}", 1 + j),
						columnFont, textBrush,
						new Rectangle(x, Height - bottomMargin, columnWidth, bottomMargin),
						new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near });

					x += columnWidth + columnGap;
				}
			}

			var style = (SkinningDefs.TheInstance.GetBoolData("awt_titles_in_bold", false) ? FontStyle.Bold : FontStyle.Regular);
			using (var font = SkinningDefs.TheInstance.GetPixelSizedFont(topMargin, style))
			using (var brush = new SolidBrush(ForeColor))
			{
				e.Graphics.DrawString(title, font, brush, new Rectangle(0, 0, Width, topMargin),
					new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Center });
			}
		}

		public int LeftMargin
		{
			get => leftMargin;

			set
			{
				leftMargin = value;
				Invalidate();
			}
		}

		public int RightMargin
		{
			get => rightMargin;

			set
			{
				rightMargin = value;
				Invalidate();
			}
		}

		public int TopMargin
		{
			get => topMargin;

			set
			{
				topMargin = value;
				Invalidate();
			}
		}

		public int BottomMargin
		{
			get => bottomMargin;

			set
			{
				bottomMargin = value;
				Invalidate();
			}
		}

		public int ColumnGap
		{
			get => columnGap;

			set
			{
				columnGap = value;
				Invalidate();
			}
		}

		public int RowGap
		{
			get => rowGap;

			set
			{
				rowGap = value;
				Invalidate();
			}
		}
	}
}