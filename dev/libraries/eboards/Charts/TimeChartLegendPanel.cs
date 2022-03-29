using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Charts.Enums;
using CommonGUI;
using CoreUtils;
using ResizingUi;

namespace Charts
{
	internal class CustomRenderPanel : FlickerFreePanel
	{
		public CustomRenderPanel (Action<Graphics, RectangleF, Color?> customRenderer)
		{
			this.customRenderer = customRenderer;
		}

		protected override void OnPaint (PaintEventArgs e)
		{
			customRenderer?.Invoke(e.Graphics, new RectangleF(0, 0, Width, Height), null);
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			Invalidate();
		}

		readonly Action<Graphics, RectangleF, Color?> customRenderer;
	}

	public class TimeChartLegendPanel : FlickerFreePanel
	{
	    readonly List<string> keyLegends;
	    readonly Dictionary<string, CascadedBackgroundPanel> keyLegendToLabel;
	    readonly Dictionary<string, Control> keyLegendToBlockPanel;
		
		int preferredHeight;

		Control parent;

		List<string> optionalKeyOrder;

		public TimeChartLegendPanel (IReadOnlyCollection<CloudTimeChart.Section> sections, IReadOnlyCollection<string> legendNames, IReadOnlyDictionary<string, Color> legendNameToColour, 
		                             IReadOnlyDictionary<string, Image> legendNameToPattern, Dictionary<CustomFillType, Action<Graphics, RectangleF, Color?>> customFillToRenderAction,
		                             Dictionary<string, CustomFillType> legendNameToCustomFill, bool sortKeyEntries = false, List<string> optionalKeyOrder = null)
		{
		    keyLegends = new List<string>();
		    keyLegendToBlockPanel = new Dictionary<string, Control>();
		    keyLegendToLabel = new Dictionary<string, CascadedBackgroundPanel> ();

			this.optionalKeyOrder = optionalKeyOrder;
            
            var allBlocks = new List<CloudTimeChart.Block>(sections.SelectMany(s => s.Blocks).ToList());

		    allBlocks.AddRange(sections.SelectMany(s => s.Rows).SelectMany(r => r.Blocks).ToList());
            
		    var uniqueBlocks = allBlocks.GroupBy(b => b.Legend).Select(g => g.First()).ToList();
			var legendsToShowScaledPattern = new List<string> ();

			if (legendNames != null)
			{
				foreach (var legend in legendNames)
				{
					if (uniqueBlocks.All(block => block.Legend != legend))
					{
						legendNameToColour.TryGetValue(legend, out var colour);
						legendNameToPattern.TryGetValue(legend, out var pattern);
						

						if (pattern != null)
						{
							legendsToShowScaledPattern.Add(legend);
						}

						uniqueBlocks.Add(new CloudTimeChart.Block(legend, colour, pattern));
					}
				}
			}

			uniqueBlocks.Sort(CompareUniqueBlocks);

			foreach (var block in uniqueBlocks)
		    {
		        keyLegends.Add(block.Legend);

			    if (legendNameToCustomFill?.ContainsKey(block.Legend) ?? false)
			    {
					var customBlock = new CustomRenderPanel(customFillToRenderAction[legendNameToCustomFill[block.Legend]]);
					keyLegendToBlockPanel.Add(block.Legend, customBlock);
					Controls.Add(customBlock);
			    }
			    else
			    {
					var hatchPanel = new HatchFillLegendPanel(block.HatchFill)
				    {
					    BackColor = block.Fill == null ? block.Colour : Color.Transparent,
					    BackgroundImage = block.Fill,
					    BackgroundImageLayout = (legendsToShowScaledPattern.Contains(block.Legend) ? ImageLayout.Zoom : ImageLayout.None)
				    };

				    keyLegendToBlockPanel.Add(block.Legend, hatchPanel);
				    Controls.Add(hatchPanel);
				}

		        var label = new CascadedBackgroundPanel
		        {
		            Text = block.Legend,
					ForeColor = SkinningDefs.TheInstance.GetColorData("gantt_chart_legend_text_colour", Color.Black),
					UseCascadedBackground = UseCascadedBackground
		        };

				keyLegendToLabel.Add(block.Legend, label);
		        Controls.Add(label);
		    }

			ForeColor = SkinningDefs.TheInstance.GetColorData("gantt_chart_legend_text_colour", Color.Black);

			if (sortKeyEntries)
			{
				keyLegends.Sort();
			}

			DoLayout();
		}

		int CompareUniqueBlocks (CloudTimeChart.Block a, CloudTimeChart.Block b)
		{
			if (optionalKeyOrder == null)
			{
				return a.Legend.CompareTo(b.Legend);
			}

			if (optionalKeyOrder.Contains(a.Legend) && ! optionalKeyOrder.Contains(b.Legend))
			{
				return -1;
			}
			else if (optionalKeyOrder.Contains(b.Legend) && ! optionalKeyOrder.Contains(a.Legend))
			{
				return 1;
			}
			else
			{
				return optionalKeyOrder.IndexOf(a.Legend).CompareTo(optionalKeyOrder.IndexOf(b.Legend));
			}
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing)
			{
				if (parent != null)
				{
					parent.Paint -= parent_Paint;
				}
			}

			base.Dispose(disposing);
		}

		protected override void OnParentBackgroundImageChanged (EventArgs e)
		{
			base.OnParentBackgroundImageChanged(e);

			Invalidate();
		}

		protected override void OnParentChanged (EventArgs e)
		{
			base.OnParentChanged(e);

			if (parent != null)
			{
				parent.Paint -= parent_Paint;
			}

			parent = Parent;

			if (parent != null)
			{
				parent.Paint += parent_Paint;
			}

			Invalidate();
		}

		bool useCascadedBackground;

		public bool UseCascadedBackground
		{
			get => useCascadedBackground;

			set
			{
				useCascadedBackground = value;

				foreach (var child in Controls)
				{
					var cascadedBackgoundChild = child as CascadedBackgroundPanel;
					if (cascadedBackgoundChild != null)
					{
						cascadedBackgoundChild.UseCascadedBackground = value;
					}
				}

				Invalidate();
			}
		}

		void parent_Paint (object sender, EventArgs args)
		{
			Invalidate();
		}

		protected override void OnFontChanged (EventArgs e)
		{
			base.OnFontChanged(e);
			DoLayout();
		}

		protected override void OnForeColorChanged (EventArgs e)
		{
			base.OnForeColorChanged(e);
			Invalidate();
		}

		protected override void OnBackColorChanged (EventArgs e)
		{
			base.OnBackColorChanged(e);
			Invalidate();
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);
			DoLayout();
		}

		void DoLayout ()
		{
			var maxLegendWidth = 0;

			using (var graphics = CreateGraphics())
			{
				foreach (var legend in keyLegends)
				{
					maxLegendWidth = Math.Max(maxLegendWidth, (int) graphics.MeasureString(legend, Font).Width);
				}
			}

			var lineHeight = 3 + (int) Font.GetHeight();
			var blockGap = 10;
			var columnGap = 25;
			var rowGap = 5;
			var rowHeight = rowGap + lineHeight;

			var legendWidth = blockGap + maxLegendWidth;
			var columnWidth = lineHeight + legendWidth + columnGap;

			var x = 0;
			var y = 0;
			foreach (var legend in keyLegends)
			{
				var block = keyLegendToBlockPanel[legend];
				block.Location = new Point(x, y);
				block.Size = new Size(lineHeight, lineHeight);

				var label = keyLegendToLabel[legend];
				label.Font = Font;
				label.Location = new Point(x + lineHeight + blockGap, y);
				label.Size = new Size (legendWidth, lineHeight);
				label.ForeColor = ForeColor;

				x += columnWidth;
				if ((x + columnWidth) > Width)
				{
					x = 0;
					y += rowHeight;
				}
			}

			preferredHeight = y + rowHeight;

			Invalidate();
		}

		public int PreferredHeight => preferredHeight;

		protected override void OnPaint (PaintEventArgs e)
		{
			base.OnPaint(e);

			if (useCascadedBackground)
			{
				BackgroundPainter.Paint(this, e.Graphics);
			}
		}
	}
}