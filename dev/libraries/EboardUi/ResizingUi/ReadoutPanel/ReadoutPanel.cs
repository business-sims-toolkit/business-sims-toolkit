using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using Network;
using CommonGUI;
using CoreUtils;
using LibCore;

namespace ResizingUi.ReadoutPanel
{
	public class ReadoutPanel : FlickerFreePanel
	{
	    readonly List<Entry> entries;
		Layout entryLayout;
		Layout seriesLayout;

		Dictionary<Entry, RectangleF> entryToLegendBounds;
		Dictionary<Entry, RectangleF> entryToValueBounds;
		float fontSize;

	    readonly bool useCascadedBackground;

	    CascadedBackgroundProperties cascadedBackgroundProperties;
	    public CascadedBackgroundProperties CascadedBackgroundProperties
	    {
	        set
	        {
	            if (cascadedBackgroundProperties != null)
	            {
	                cascadedBackgroundProperties.PropertiesChanged -= cascadedBackgroundProperties_PropertiesChanged;
	            }

	            cascadedBackgroundProperties = value;

	            if (cascadedBackgroundProperties != null)
	            {
	                cascadedBackgroundProperties.PropertiesChanged += cascadedBackgroundProperties_PropertiesChanged;
	            }

	            Invalidate(new Rectangle(0, 0, Width, Height), true);
	        }
	    }

        IWatermarker watermarker;

		public IWatermarker Watermarker
		{
			get => watermarker;

			set
			{
				watermarker = value;
				Invalidate();
			}
		}

	    StringAlignment? legendAlignment;
	    public StringAlignment? LegendAlignment
	    {
	        get => legendAlignment;
	        set
	        {
	            legendAlignment = value;
                DoSize();
	        }
	    }

	    StringAlignment? valueAlignment;

	    public StringAlignment? ValueAlignment
	    {
	        get => valueAlignment;
	        set
	        {
	            valueAlignment = value;
                DoSize();
	        }
	    }

	    float? horizontalMarginFraction;

	    public float? HorizontalMarginFraction
	    {
	        get => horizontalMarginFraction;
	        set
	        {
	            horizontalMarginFraction = value;
	            DoSize();
	        }
	    }

	    float? horizontalBoundsDivideFraction;

	    public float? HorizontalBoundsDivideFraction
	    {
	        get => horizontalBoundsDivideFraction;
	        set
	        {
	            horizontalBoundsDivideFraction = value;
                DoSize();
	        }
	    }

		public ReadoutPanel (bool useCascadedBackground = false)
		{
		    this.useCascadedBackground = useCascadedBackground;
			entries = new List<Entry> ();

			entryLayout = ResizingUi.ReadoutPanel.Layout.Horizontal;
			seriesLayout = ResizingUi.ReadoutPanel.Layout.Vertical;

			DoSize();
		}

		public void AddEntry (string legend, string referenceString, Color colour, IList<Node> dependencyNodes, ReadoutFetcher fetcher)
		{
			var entry = new Entry (legend, referenceString, colour, dependencyNodes, fetcher);
			entry.Invalidated += entry_Invalidate;
			entries.Add(entry);
			Invalidate();
		}

		public void AddIntegerEntry (string legend, int maxInteger, Color colour, Node node, string attributeName)
		{
			AddEntry(legend, CONVERT.ToStr(maxInteger), colour, new [] { node }, nodes => CONVERT.ToStr(node.GetIntAttribute(attributeName, 0)));
		}

		public void AddCurrencyEntry (string legend, int maxAmount, Color colour, Node node, string attributeName)
		{
			AddEntry(legend, CONVERT.FormatMoney(maxAmount), colour, new [] { node }, nodes => CONVERT.FormatMoney(node.GetDoubleAttribute(attributeName, 0)));
		}

		public void AddPercentageEntry (string legend, Color colour, Node node, string attributeName)
		{
			AddEntry(legend, "100%", colour, new [] { node }, nodes => CONVERT.Format("{0}%", (int) node.GetDoubleAttribute(attributeName, 0)));
		}

		void entry_Invalidate (object sender, EventArgs args)
		{
			Invalidate();
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);
			DoSize();
		}

		public Layout EntryLayout
		{
			get => entryLayout;

			set
			{
				entryLayout = value;
				DoSize();
			}
		}

		public Layout SeriesLayout
		{
			get => seriesLayout;

			set
			{
				seriesLayout = value;
				DoSize();
			}
		}

		void DoSize ()
		{
			entryToLegendBounds = new Dictionary<Entry, RectangleF> ();
			entryToValueBounds = new Dictionary<Entry, RectangleF> ();

			for (int i = 0; i < entries.Count; i++)
			{
				var entry = entries[i];
				RectangleF entryBounds;

				switch (seriesLayout)
				{
					case ResizingUi.ReadoutPanel.Layout.Horizontal:
						entryBounds = new RectangleF (Width * 1.0f * i / entries.Count, 0, Width * 1.0f / entries.Count, Height);
						break;

					case ResizingUi.ReadoutPanel.Layout.Vertical:
						entryBounds = new RectangleF (0, Height * 1.0f * i / entries.Count, Width, Height * 1.0f / entries.Count);
						break;

					default:
						throw new Exception ("Unhandled layout type");
				}

				RectangleF legendBounds;
				RectangleF valueBounds;
				switch (entryLayout)
				{
					case ResizingUi.ReadoutPanel.Layout.Horizontal:
					{
						int margin = 0;

						if (seriesLayout == ResizingUi.ReadoutPanel.Layout.Horizontal)
						{
							margin = (int) (entryBounds.Width * HorizontalMarginFraction ?? 0.125f);
						}

						legendBounds = new RectangleF (entryBounds.Left + margin, entryBounds.Top, (int)(entryBounds.Width * horizontalBoundsDivideFraction ?? 0.5f) - margin, entryBounds.Height);
						valueBounds = new RectangleF (legendBounds.Right, entryBounds.Top, entryBounds.Right - margin - legendBounds.Right, entryBounds.Height);
						break;
					}

					case ResizingUi.ReadoutPanel.Layout.Vertical:
						legendBounds = new RectangleF (entryBounds.Left, entryBounds.Top, entryBounds.Width, entryBounds.Height / 2);
						valueBounds = new RectangleF (entryBounds.Left, legendBounds.Bottom, entryBounds.Width, entryBounds.Bottom - legendBounds.Bottom);
						break;

					default:
						throw new Exception("Unhandled layout type");
				}

				entryToLegendBounds.Add(entry, legendBounds);
				entryToValueBounds.Add(entry, valueBounds);
			}

			var strings = new List<string> ();
			var sizes = new List<SizeF> ();
			strings.AddRange(entries.Select(entry => entry.Legend).ToList());
			sizes.AddRange(entries.Select(entry => entryToLegendBounds[entry].Size).ToList());
			strings.AddRange(entries.Select(entry => entry.ReferenceString).ToList());
			sizes.AddRange(entries.Select(entry => entryToValueBounds[entry].Size).ToList());

			if (entries.Count > 0)
			{
				fontSize = this.GetFontSizeInPixelsToFit(FontStyle.Bold, strings, sizes);
			}

			Invalidate();
		}

		protected override void OnPaint (PaintEventArgs e)
		{

		    if (useCascadedBackground)
		    {
                BackgroundPainter.Paint(this, e.Graphics, cascadedBackgroundProperties);
		    }

		    var backColour = useCascadedBackground
		        ? Color.FromArgb(SkinningDefs.TheInstance.GetIntData("cascaded_background_transparency", 255), BackColor)
		        : BackColor;

		    using (var backBrush = new SolidBrush(backColour))
		    {
                e.Graphics.FillRectangle(backBrush, new Rectangle(0, 0, Width, Height));
		    }

            watermarker?.Draw(this, e.Graphics);
            
			if (entries.Count > 0)
			{
				using (var boldFont = SkinningDefs.TheInstance.GetPixelSizedFont(fontSize, FontStyle.Bold))
				using (var normalFont = SkinningDefs.TheInstance.GetPixelSizedFont(fontSize))
				{
					foreach (var entry in entries)
					{
						using (var brush = new SolidBrush (entry.Colour))
						{
                            e.Graphics.DrawString(entry.Legend, normalFont, brush, entryToLegendBounds[entry],
								new StringFormat
								{
									Alignment = LegendAlignment ?? ((entryLayout == ResizingUi.ReadoutPanel.Layout.Horizontal)
										? StringAlignment.Near
										: StringAlignment.Center),

									LineAlignment = ((entryLayout == ResizingUi.ReadoutPanel.Layout.Horizontal)
										? StringAlignment.Center
										: StringAlignment.Far)
								});

                            e.Graphics.DrawString(entry.Value, boldFont, brush, entryToValueBounds[entry],
								new StringFormat
								{
									Alignment = ValueAlignment ?? ((entryLayout == ResizingUi.ReadoutPanel.Layout.Horizontal)
										? StringAlignment.Far
										: StringAlignment.Center),

									LineAlignment = ((entryLayout == ResizingUi.ReadoutPanel.Layout.Horizontal)
										? StringAlignment.Center
										: StringAlignment.Near)
								});
                            
						}
					}
				}
			}
		}

	    void cascadedBackgroundProperties_PropertiesChanged(object sender, EventArgs e)
	    {
	        Invalidate(new Rectangle(0, 0, Width, Height), true);
	    }
    }
}