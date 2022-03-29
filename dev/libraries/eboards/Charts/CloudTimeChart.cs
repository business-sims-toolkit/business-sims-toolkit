using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Xml;
using System.Windows.Forms;
using Algorithms;
using Charts.Enums;
using LibCore;
using CoreUtils;
using Events;
using ResizingUi;
using ResizingUi.Extensions;

namespace Charts
{
	public class CloudTimeChart : SharedMouseEventControl
	{
		List<string> keyLegends;
		Dictionary<string, Color> keyLegendToColour;
		Dictionary<string, Image> keyLegendToPattern;
		Dictionary<string, CustomFillType> keyLegendToCustomFill;

		readonly Dictionary<string, Rectangle> boundsIdToRectangle;

		public class ErrorBlock
		{
			public string RowLegend { get; }
			public float ErrorTime { get; }
			public float ErrorBlockLength { get; }
			public Color Colour { get; }
			public bool CentreOnTime { get; }

			public ErrorBlock(XmlElement root)
			{
				RowLegend = BasicXmlDocument.GetStringAttribute(root, "row_legend");
				ErrorTime = BasicXmlDocument.GetFloatAttribute(root, "error_time", 0);
				ErrorBlockLength = BasicXmlDocument.GetFloatAttribute(root, "block_length", 0.25f);
				Colour = BasicXmlDocument.GetColourAttribute(root, "block_colour", Color.GhostWhite);
				CentreOnTime = BasicXmlDocument.GetBoolAttribute(root, "centre_on_time", false);
			}
		}

		public class DependentRows
		{
			public string DependentRowLegend { get; }
			public string PrerequisiteRowLegend { get; }
			public float DependentTime { get; }
			public float PrerequisiteTime { get; }

			public DependentRows(XmlNode root)
			{
				DependentRowLegend = BasicXmlDocument.GetStringAttribute(root, "dependent_legend");
				PrerequisiteRowLegend = BasicXmlDocument.GetStringAttribute(root, "prerequisite_legend");

				DependentTime = BasicXmlDocument.GetFloatAttribute(root, "dependent_time", 0);
				PrerequisiteTime = BasicXmlDocument.GetFloatAttribute(root, "prerequisite_time", 0);
			}
		}

		public class SubBlock
		{
			public float Start { get; }
			public float End {get;}
			public Color Colour {get;}
			public Image Fill {get;}

            public SubBlock(XmlNode root)
            {
                Start = (float) BasicXmlDocument.GetDoubleAttribute(root, "start", 0);
                End = (float) BasicXmlDocument.GetDoubleAttribute(root, "end", 0);
                Colour = BasicXmlDocument.GetColourAttribute(root, "colour", Color.Black);
                var patternName = BasicXmlDocument.GetStringAttribute(root, "fill", string.Empty);
                Fill = (string.IsNullOrEmpty(patternName))
                    ? null
                    : Repository.TheInstance.GetImage(GetFilenameFromPatternName(patternName));
            }
        }

		public class Block
		{
			public float Start { get; }
			public float End { get; }
            public string Legend { get; }
            public string SmallLegend { get; }
            public string BottomLegend { get; }
            public Color Colour { get; }
            public Color TextColour { get; }

            public Image Fill { get; }
            public bool Hollow { get; }
            public HatchFillImage HatchFill { get; }
            public List<SubBlock> SubBlocks { get; }

			public Block (string legend, Color colour, Image fill, bool hollow = false)
			{
				Legend = legend;
				Colour = colour;
				Fill = fill;
				Hollow = hollow;

				SubBlocks = new List<SubBlock> ();
			}

            public Block(XmlElement root)
			{
				Legend = BasicXmlDocument.GetStringAttribute(root, "legend");
				SmallLegend = BasicXmlDocument.GetStringAttribute(root, "small_legend");
				BottomLegend = BasicXmlDocument.GetStringAttribute(root, "bottom_legend");
				Colour = CONVERT.ParseComponentColor(BasicXmlDocument.GetStringAttribute(root, "colour", "0, 0, 0"));
				TextColour = CONVERT.ParseComponentColor(BasicXmlDocument.GetStringAttribute(root, "textcolour", "0,0,0"));
				Start = (float)BasicXmlDocument.GetDoubleAttribute(root, "start", 0);
				End = (float)BasicXmlDocument.GetDoubleAttribute(root, "end", 0);
                var patternName = BasicXmlDocument.GetStringAttribute(root, "fill", string.Empty);
			    Fill = (string.IsNullOrEmpty(patternName))
			        ? null : Repository.TheInstance.GetImage(GetFilenameFromPatternName(patternName));
				Hollow = BasicXmlDocument.GetBoolAttribute(root, "hollow", false);

			    SubBlocks = new List<SubBlock>();

			    HatchFill = null;

                foreach (XmlNode child in root.ChildNodes)
                {
                    switch(child.Name)
                    {
                        case "sub_block":
                            SubBlocks.Add(new SubBlock(child));
                            break;
                        case "hatch_fill":
                            HatchFill = new HatchFillImage(child);
                            break;
                    }
                }
			}
		}

		public class MouseoverBlock
		{
			public float Start { get; }
			public float End { get; }
			public string Legend { get; }

			public MouseoverBlock(XmlElement root)
			{
				Legend = BasicXmlDocument.GetStringAttribute(root, "legend");
				Start = (float)BasicXmlDocument.GetDoubleAttribute(root, "start", 0);
				End = (float)BasicXmlDocument.GetDoubleAttribute(root, "end", 0);
			}

			public bool IsPointInside(Timeline timeline, RectangleF fullRectangle, Point point)
			{
				var left = MapFromRange(Start, timeline.Start, timeline.End, fullRectangle.Left, fullRectangle.Right);
				var right = MapFromRange(End, timeline.Start, timeline.End, fullRectangle.Left, fullRectangle.Right);
				var rectangle = new RectangleF(left, (int)fullRectangle.Top, right - left, (int)fullRectangle.Height);

				return rectangle.Contains(point);
			}
		}

       public class MouseoverBlockWithHoverBar : MouseoverBlock
        {
            public string LostRevenue { get; }
			public int Length { get; }

			public MouseoverBlockWithHoverBar(XmlElement root):
                base(root)
            {
                LostRevenue = BasicXmlDocument.GetStringAttribute(root, "value");
                Length = BasicXmlDocument.GetIntAttribute(root, "length", 0);

            }
        }

		public class Row
		{
			public string Legend { get; }
			public string RegionsCode { get; }
			public int HeaderWidth { get; } = -1;

			public string IconName { get; }

			public List<Block> Blocks { get; }
			public List<MouseoverBlock> MouseoverBlocks { get; }

			public Row(XmlElement root)
			{
				Legend = BasicXmlDocument.GetStringAttribute(root, "legend");
				RegionsCode = BasicXmlDocument.GetStringAttribute(root, "regions_code");
				HeaderWidth = BasicXmlDocument.GetIntAttribute(root, "header_width", -1);
				IconName = root.GetStringAttribute("icon", null);

				Blocks = new List<Block>();
				MouseoverBlocks = new List<MouseoverBlock>();
				foreach (XmlElement child in root.ChildNodes)
				{
					switch (child.Name)
					{
						case "block":
							Blocks.Add(new Block(child));
							break;

						case "mouseover_block":
							MouseoverBlocks.Add(new MouseoverBlock(child));
							break;

                        case "mouseover_block_bar":
					        MouseoverBlocks.Add(new MouseoverBlockWithHoverBar(child));
                            break;
					}
				}
			}
		}

		public class Section
		{
			public string Legend { get; }
			public Color TextForeColour { get; }
			public Color TextBackColour { get; }
			public int HeaderWidth { get; }

			public List<Block> Blocks { get; }
			public List<MouseoverBlock> MouseoverBlocks { get; }
			public List<Row> Rows { get; }

			public Section(XmlNode root)
			{
				Legend = BasicXmlDocument.GetStringAttribute(root, "legend");
				HeaderWidth = BasicXmlDocument.GetIntAttribute(root, "header_width", -1);

				TextForeColour = CONVERT.ParseComponentColor(BasicXmlDocument.GetStringAttribute(root, "row_forecolour"));
				TextBackColour = CONVERT.ParseComponentColor(BasicXmlDocument.GetStringAttribute(root, "row_backcolour"));

				Blocks = new List<Block>();
				MouseoverBlocks = new List<MouseoverBlock>();
				Rows = new List<Row>();
				foreach (XmlElement child in root.ChildNodes)
				{
					switch (child.Name)
					{
						case "block":
							Blocks.Add(new Block(child));
							break;

						case "mouseover_block":
							MouseoverBlocks.Add(new MouseoverBlock(child));
							break;

                        case "mouseover_block_bar":
					        MouseoverBlocks.Add(new MouseoverBlockWithHoverBar(child));
                            break;

						case "row":
							Rows.Add(new Row(child));
							break;
					}
				}
			}
		}

		public class Timeline
		{
			public float Start { get; }
			public float End { get; }
			public float Interval { get; }
			public string Legend { get; }
			public Color TitleForeColour { get; }
			public Color TitleBackColour { get; }
			public Color MinutesForeColour { get; set; }
			public Color MinutesBackColour { get; set; }
			public Color MinutesNegativeBackColour { get; }

			public bool UseTimelineFont { get; }
			public Font TimelineFont { get; }
			public bool ShouldDrawMarkings { get; }

			public Timeline(XmlElement root)
			{
				Start = (float)BasicXmlDocument.GetDoubleAttribute(root, "start", 0);
				End = (float)BasicXmlDocument.GetDoubleAttribute(root, "end", 0);
				Interval = (float)BasicXmlDocument.GetDoubleAttribute(root, "interval", 0);
				Legend = BasicXmlDocument.GetStringAttribute(root, "legend");
			    TitleForeColour =
			        CONVERT.ParseComponentColor(BasicXmlDocument.GetStringAttribute(root, "title_fore_colour", "0,0,0"));
			    TitleBackColour =
			        CONVERT.ParseComponentColor(BasicXmlDocument.GetStringAttribute(root, "title_back_colour", "255, 255, 255"));
				MinutesForeColour = CONVERT.ParseComponentColor(BasicXmlDocument.GetStringAttribute(root, "minutes_fore_colour", "0,0,0"));
				MinutesBackColour = CONVERT.ParseComponentColor(BasicXmlDocument.GetStringAttribute(root, "minutes_back_colour", "255,255,255"));
				MinutesNegativeBackColour = CONVERT.ParseComponentColor(BasicXmlDocument.GetStringAttribute(root, "minutes_negative_back_colour", CONVERT.ToComponentStr(MinutesBackColour)));

				UseTimelineFont = BasicXmlDocument.GetBoolAttribute(root,"use_timeline_font", false);
                if (UseTimelineFont)
                {
                    var fontStyleStr = BasicXmlDocument.GetStringAttribute(root, "font_style", "bold");
                    FontStyle fontStyle;
                    switch (fontStyleStr.ToLower())
                    {
                        case "bold":
                            fontStyle = FontStyle.Bold;
                            break;

                        case "italic":
                            fontStyle = FontStyle.Italic;
                            break;
                        default:
                            fontStyle = FontStyle.Regular;
                            break;
                    }
                    var fontSize = (float) BasicXmlDocument.GetDoubleAttribute(root, "font_size", 10.0);

                    TimelineFont = SkinningDefs.TheInstance.GetFont(fontSize, fontStyle);
                }

			    ShouldDrawMarkings = BasicXmlDocument.GetBoolAttribute(root, "should_draw_markings", true);
			}
		}

	    class Regions
	    {
	        public List<string> RegionNames { get; }

			public Regions (XmlNode root)
	        {
                RegionNames = new List<string>();
                foreach (XmlElement region in root.ChildNodes)
                {
                    RegionNames.Add(region.GetAttribute("name"));
                }
            }
	    }

	    Timeline timeline;
	    Regions regions;
	    List<Section> sections;
		readonly List<DependentRows> dependentRows;
		List<ErrorBlock> errorBlocks;
			
		Point? mouseoverPoint;

		bool keyEnabled;
		Bitmap BackImage;
		bool UseBackImage;
		bool showRowLegends;

		Color TimeLine_ForeColor = Color.Black;
	    readonly Font Font_Body;
	    readonly Font Font_SmallBody;
	    readonly Font Font_VerticalRegion;

	    readonly Bitmap RegionOnIndicator;
	    readonly Bitmap RegionOffIndicator;
		int timelineLowEdge;

		Control legendPanel;

	    readonly bool drawRegionIcons;
	    bool useGradient;

		double? highlightX;

		Control parent;

	    bool sortKeyEntries;

		List<string> optionalKeyEntryOrder;

		public CloudTimeChart ()
		{
			var font = SkinningDefs.TheInstance.GetData("fontname");
			Font_Body = FontRepository.GetFont(font, 10, FontStyle.Regular);
			Font_VerticalRegion = FontRepository.GetFont(font, 10, FontStyle.Bold);
			Font_SmallBody = FontRepository.GetFont(font, 8, FontStyle.Regular);

			RegionOnIndicator = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\reports\\region_on.png");
			RegionOffIndicator = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\reports\\region_off.png");

			sections = new List<Section> ();
			dependentRows = new List<DependentRows>();
			errorBlocks = new List<ErrorBlock>();
			boundsIdToRectangle = new Dictionary<string, Rectangle>();
		}

		public CloudTimeChart (XmlElement root, bool showRegionIcons = true)
			: this ()
		{
			drawRegionIcons = showRegionIcons;

			LoadData(root);
		}

		public void LoadData (XmlElement root)
		{
			sections = new List<Section>();
			foreach (XmlElement child in root.ChildNodes)
			{
				switch (child.Name)
				{
					case "timeline":
						timeline = new Timeline(child);
						break;

					case "optional_key_order":
						ReadOptionalKeyOrder(child);
						break;

					case "key":
						ReadKey(child);
						break;

                    case "regions":
				        regions = new Regions(child);
                        break;

					case "sections":
				        useGradient = child.GetBooleanAttribute("use_gradient", true);
						foreach (XmlElement grandchild in child.ChildNodes)
						{
							switch (grandchild.Name)
							{
								case "section":
									sections.Add(new Section(grandchild));
									break;
							}
						}
						break;
					case "errors":
						// TODO use something more generic than "error"
						foreach (XmlElement grandchild in child.ChildNodes)
						{
							if (grandchild.Name == "error")
							{
								errorBlocks.Add(new ErrorBlock(grandchild));
							}
						}

						break;
					case "dependentRows":
						foreach (XmlElement grandchild in child.ChildNodes)
						{
							if (grandchild.Name == "dependency")
							{
								dependentRows.Add(new DependentRows(grandchild));
							}
						}
						break;
				}
			}

			showRowLegends = root.GetBooleanAttribute("show_row_legends", false);

			highlightX = root.GetDoubleAttribute("highlight_x");
            
			keyEnabled = true;

			sortKeyEntries = root.GetBooleanAttribute("order_key_items", false);

			Invalidate();
		}

		public int PreferredHeight { get; private set; }

		// TODO move the rendering of the actual chart content into a separate control so it can be scrolled independently 

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);

	        using (var graphics = CreateGraphics())
	        {
		        OnPaint(new PaintEventArgs(graphics, new Rectangle (0, 0, Width, Height)));
	        }

			Refresh();
        }

		void ReadKey (XmlElement key)
		{
			keyLegends = new List<string> ();
			keyLegendToColour = new Dictionary<string,Color> ();
		    keyLegendToPattern = new Dictionary<string, Image>();

			keyLegendToCustomFill = new Dictionary<string, CustomFillType>();

			foreach (XmlElement child in key.ChildNodes)
			{
				var legend = BasicXmlDocument.GetStringAttribute(child, "legend");
				var colour = BasicXmlDocument.GetColourAttribute(child, "colour", Color.White);
                var patternName = BasicXmlDocument.GetStringAttribute(child, "fill", string.Empty);
				var imageName = BasicXmlDocument.GetStringAttribute(child, "image", string.Empty);

				var customFill = BasicXmlDocument.GetEnumAttribute(child, "custom_fill", CustomFillType.None);

				keyLegends.Add(legend);

				if (customFill != CustomFillType.None)
				{
					keyLegendToCustomFill[legend] = customFill;
					keyLegendToColour[legend] = colour;
				}
				else
				{
					Image pattern = null;

					if (!string.IsNullOrEmpty(imageName))
					{
						pattern = Repository.TheInstance.GetImage(imageName);
					}
					else if (!string.IsNullOrEmpty(patternName))
					{
						pattern = Repository.TheInstance.GetImage(CloudTimeChart.GetFilenameFromPatternName(patternName));
					}

					if (pattern == null)
					{
						keyLegendToColour[legend] = colour;
					}
					else
					{
						keyLegendToPattern[legend] = pattern;
					}
				}
			}
		}

		void ReadOptionalKeyOrder (XmlElement key)
		{
			optionalKeyEntryOrder = new List<string> ();

			foreach (XmlElement child in key.ChildNodes)
			{
				optionalKeyEntryOrder.Add(child.GetAttribute("legend"));
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
				Invalidate();
			}
		}

		void parent_Paint (object sender, EventArgs args)
		{
			Invalidate();
		}

		public int getTimeLineLowEdge()
		{
			return timelineLowEdge;
		}

		public void SetBackImageWithFile (string BackImageName, bool useBack)
		{
			SetBackImageWithFile(BackImageName, useBack, Color.White, Color.Transparent);
		}

		public void SetBackImageWithFile(string BackImageName, bool useBack, Color foreColour, Color backColour)
		{
			SetBackImageWithBitmap((Bitmap)Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\" + BackImageName), useBack);

			TimeLine_ForeColor = foreColour;
			timeline.MinutesForeColour = TimeLine_ForeColor;
			timeline.MinutesBackColour = backColour;
		}

		public void SetBackImageWithBitmap (Bitmap bmpBackImage, bool useBack)
		{
			SetBackImageWithBitmap(bmpBackImage, useBack, Color.White, Color.Transparent);
		}

		public void SetBackImageWithBitmap (Bitmap bmpBackImage, bool useBack, Color foreColour, Color backColour)
		{
			BackImage = bmpBackImage;
			UseBackImage = useBack;

			TimeLine_ForeColor = foreColour;
			timeline.MinutesForeColour = TimeLine_ForeColor;
			timeline.MinutesBackColour = backColour;
		}

		public static float MapFromRange(float t, float t0, float t1, float y0, float y1)
		{
			return y0 + ((t - t0) * (y1 - y0) / (t1 - t0));
		}

        public static string GetFilenameFromPatternName(string patternName)
        {
            var filename = AppInfo.TheInstance.Location + "\\images\\chart\\";

            switch (patternName)
            {
                case "orange_hatch":
                    filename += "hatch.png";
                    break;
                case "blue_hatch":
                    filename += "blue_hatch.png";
                    break;
                case "red_hatch":
                    filename += "red_hatch.png";
                    break;
                default:
                    filename += patternName + ".png";
                    break;
            }

            return filename;
        }

	    int rowLeading;

	    public int RowLeading
	    {
		    get => rowLeading;

		    set
		    {
				rowLeading = value;
				Invalidate();
		    }
	    }

	    int sectionLeading;

	    public int SectionLeading
	    {
		    get => sectionLeading;

		    set
		    {
			    sectionLeading = value;

				Invalidate();
			}
	    }

		void RenderBlock(Graphics graphics, Block block, RectangleF fullRectangle)
		{
            RenderTimedBlock(graphics, block.Colour, block.Hollow, block.Start, block.End, fullRectangle, 
                block.Legend, block.SmallLegend, block.BottomLegend, block.TextColour, Font_Body, block.Fill, block.HatchFill);
		}

		void RenderTimedBlock(Graphics graphics, Color colour, bool borderOnly, float startTime, float endTime, RectangleF fullRectangle, string legend, 
            string smallLegend, string bottomLegend, Color textColour, Font font, Image fillImage = null, HatchFillImage hatchFill = null)
		{
			var left = MapFromRange(startTime, timeline.Start, timeline.End, fullRectangle.Left, fullRectangle.Right);
			var right = MapFromRange(endTime, timeline.Start, timeline.End, fullRectangle.Left, fullRectangle.Right);

			var rectangle = new RectangleF(left, fullRectangle.Top, right - left, fullRectangle.Height);

			if (borderOnly)
			{
				float thickness = 4;

				using (var pen = new Pen(colour, thickness))
				{
					graphics.DrawRectangle(pen, (int)(rectangle.Left + (thickness / 2)), (int)(rectangle.Top + (thickness / 2)), (int) (rectangle.Width - thickness), (int)(rectangle.Height - thickness));
				}
			}
			else
			{
			    if (fillImage != null)
			    {
			        if (SkinningDefs.TheInstance.GetBoolData("cloud_time_chart_use_texture_brush", false) != true)
			        {
			            using (var textureBrush = new TextureBrush(fillImage)
			            {
			                WrapMode = WrapMode.Tile
			            })
			            {
                            graphics.FillRectangle(textureBrush, rectangle);
			            }
			        }
			        else
			        {
			            for (var y = (int)rectangle.Top; y <= rectangle.Bottom; y += fillImage.Height)
			            {
			                for (var x = (int)rectangle.Left; x <= rectangle.Right; x += fillImage.Width)
			                {
			                    graphics.DrawImageUnscaledAndClipped(fillImage, new Rectangle((int)x, (int)y, (int)Math.Min(fillImage.Width, rectangle.Right + 1 - x), (int)Math.Min(fillImage.Height, rectangle.Bottom + 1 - y)));
			                }
			            }
                    }

				    
                }
                else if (hatchFill != null)
			    {
                    hatchFill.RenderToBounds(graphics, rectangle);
			    }
			    else
			    {
                    Brush brush;
                    if (useGradient)
                    {
                        // There's a bug in LinearGradientBrush that can render the edges wrongly: expand the brush rectangle to work around it.
                        var gradientRectangle = new RectangleF(rectangle.Left - 1, rectangle.Top - 1, rectangle.Width + 2, rectangle.Height + 2);

                        var lightColour = Lerp(0.2f, colour, Color.White);
                        var darkColour = Lerp(0.2f, colour, Color.Black);
                        brush = new LinearGradientBrush(gradientRectangle, lightColour, darkColour, 90);
                    }
                    else
                    {
                        brush = new SolidBrush(colour);
                    }

                    graphics.FillRectangle(brush, rectangle.Left - 1, rectangle.Top, rectangle.Width + 2, rectangle.Height);
			    }
			}
            

			if (!keyEnabled)
			{
				RenderText(graphics, Font_Body, legend, textColour, rectangle, 
                    StringAlignment.Center, StringAlignment.Center);
			}

			if (smallLegend != "")
			{
				RenderText(graphics, Font_SmallBody, smallLegend, textColour, rectangle, 
                    StringAlignment.Center, StringAlignment.Near);
			}

			if (bottomLegend != "")
			{
				RenderText(graphics, Font_SmallBody, bottomLegend, textColour, rectangle, 
                    StringAlignment.Center, StringAlignment.Far);
			}
		}

		int Lerp(float t, int a, int b)
		{
			return ((int)(a + (t * (b - a))));
		}

		Color Lerp(float t, Color a, Color b)
		{
			return Color.FromArgb(Lerp(t, a.A, b.A), Lerp(t, a.R, b.R), Lerp(t, a.G, b.G), Lerp(t, a.B, b.B));
		}

		void RenderText(Graphics graphics, Font font, string text, Color colour, RectangleF rectangle, StringAlignment alignment, StringAlignment lineAlignment)
		{
		    var format = new StringFormat
		    {
		        Alignment = alignment,
		        LineAlignment = lineAlignment
		    };

		    using (Brush brush = new SolidBrush(colour))
			{
				graphics.DrawString(text, font, brush, rectangle, format);
			}
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			if (useCascadedBackground)
			{
				BackgroundPainter.Paint(this, e.Graphics);
			}

			if (UseBackImage)
			{
				if (BackImage != null)
				{
					e.Graphics.DrawImage(BackImage, 0, 0, this.Width, this.Height);
				}
			}

		    var sectionRowHeaderWidths = sections.Select(s =>
		        new
		        {
		            SectionWidth = s.HeaderWidth,
		            RowWidth = s.Rows.Max(r => r.HeaderWidth),
                    RowCount = s.Rows.Count
		        }
		    ).ToList();

		    var maxSectionNameWidth = sectionRowHeaderWidths.Max(s => s.SectionWidth as int?) ?? 0;
			var maxRowWidth = sectionRowHeaderWidths.Max(s => s.RowWidth as int?) ?? 0;
		    var rows = sectionRowHeaderWidths.Sum(s => s.RowCount);
            
            float sectionHeaderWidth = maxSectionNameWidth;
            float rowHeaderWidth = maxRowWidth;

			float timelineHeight = 45;
			float timelineLeftMargin = 10;
			float timelineRightMargin = 10;
			var timelineLeft = sectionHeaderWidth + rowHeaderWidth;
			float timelineRight = Width;
			var timelineWidth = timelineRight - timelineLeft;
			float timelineLabelHeight = 20;
			float timelineLegendHeight = 20;

			const float minRowHeight = 10;
			const float maxRowHeight = 30;

			float rowHeight = (int)Math.Max(minRowHeight, Math.Min(maxRowHeight, (Height - timelineHeight) / Math.Max(1, rows)));

			float y;

			var regionLabels_start_x = 0;
			var regionLabels_step_x = 0;
			var regionLabels_height = 70;
            y = regionLabels_height;

			MouseoverBlock activeMouseover = null;

			y += 15;
		    var startingY = y;

		    var contentWidth = timelineWidth - (timelineLeftMargin + timelineRightMargin);

			var accumulativeHeight = 0f;

			var rowLegendToContentBounds = new Dictionary<string, RectangleF>();
			var rowLegendToRowBounds = new Dictionary<string, RectangleF>();
			
			foreach (var section in sections)
			{
				var sectionHeight = (rowHeight * section.Rows.Count) + (2 * sectionLeading);
				var sectionPaddedRectangle = new RectangleF(0, (int) y, Width - timelineRightMargin, (int) sectionHeight);
				var sectionInnerRectangle = new RectangleF(sectionPaddedRectangle.Left, sectionPaddedRectangle.Top + sectionLeading, sectionPaddedRectangle.Width, sectionPaddedRectangle.Height - (2 * sectionLeading));
				var sectionTitleRectangle = new RectangleF(sectionPaddedRectangle.Left, sectionPaddedRectangle.Top, sectionHeaderWidth, sectionPaddedRectangle.Height);
				var sectionContentsRectangle = new RectangleF(timelineLeft + timelineLeftMargin, sectionInnerRectangle.Top, contentWidth, sectionInnerRectangle.Height);

				accumulativeHeight += sectionHeight;

				using (Brush sectionBrush = new SolidBrush(section.TextBackColour))
				{
					e.Graphics.FillRectangle(sectionBrush, sectionPaddedRectangle);
				}

				var sectionTextForeColor = section.TextForeColour;

				RenderText(e.Graphics, Font_Body, section.Legend, sectionTextForeColor, sectionTitleRectangle, StringAlignment.Near, StringAlignment.Center);

				foreach (var block in section.Blocks)
				{
					RenderBlock(e.Graphics, block, sectionContentsRectangle);

                    if (block.SubBlocks.Count > 0)
                    {
                        var subHeight = sectionHeight / block.SubBlocks.Count;
                        var subRectangle = sectionContentsRectangle;
                        for (var i = 0; i < block.SubBlocks.Count; i++ )
                        {
                            var subBlock = block.SubBlocks[i];
                            subRectangle.Y = y + (subHeight * i);
                            subRectangle.Height = subHeight;

                            RenderTimedBlock(e.Graphics, subBlock.Colour, false, subBlock.Start, subBlock.End, subRectangle, "", "", "", Color.White, Font_Body, subBlock.Fill);
                        }
                    }
				}

				if (mouseoverPoint.HasValue)
				{
					foreach (var block in section.MouseoverBlocks)
					{
						if (block.IsPointInside(timeline, sectionContentsRectangle, mouseoverPoint.Value))
						{
							activeMouseover = block;
						}
					}
				}

				y = sectionInnerRectangle.Top;
				foreach (var row in section.Rows)
				{
					if (row.HeaderWidth != -1)
					{
						rowHeaderWidth = row.HeaderWidth;
					}

					var rowRectangle = new RectangleF(sectionHeaderWidth, (int)y, Width - sectionHeaderWidth, (int)rowHeight);
					var rowTitleRectangle = new RectangleF(sectionHeaderWidth, (int)y, rowHeaderWidth, (int)rowHeight);
					var rowContentsRectangle = new RectangleF(timelineLeft + timelineLeftMargin, (int) y + rowLeading, timelineWidth - (timelineLeftMargin + timelineRightMargin), (int) rowHeight - (2 * rowLeading));

					rowLegendToRowBounds[row.Legend] = new RectangleFFromBounds
					{
						Left = sectionTitleRectangle.Left,
						Top = sectionTitleRectangle.Top,
						Right = rowContentsRectangle.Right,
						Height = sectionTitleRectangle.Height
					}.ToRectangleF();

					rowLegendToContentBounds[row.Legend] = rowContentsRectangle;

					if (showRowLegends)
					{
						//draw the text indicators 
						RenderText(e.Graphics, Font_Body, row.Legend, sectionTextForeColor, rowTitleRectangle, StringAlignment.Near, StringAlignment.Center);
					}
					else
					{
                        if (drawRegionIcons)
                        {
						    var indicatorSize = 18;

                            // replaced the if/elses with a ternary operator to reduce code duplication.

                            e.Graphics.DrawImage((row.RegionsCode.Contains("m")) ? RegionOnIndicator : RegionOffIndicator,
                                regionLabels_start_x + 3 * regionLabels_step_x, y + (rowContentsRectangle.Height - indicatorSize) / 2, indicatorSize, indicatorSize);

                            e.Graphics.DrawImage((row.RegionsCode.Contains("o")) ? RegionOnIndicator : RegionOffIndicator,
                                regionLabels_start_x + 2 * regionLabels_step_x, y + (rowContentsRectangle.Height - indicatorSize) / 2, indicatorSize, indicatorSize);

                            e.Graphics.DrawImage((row.RegionsCode.Contains("f")) ? RegionOnIndicator : RegionOffIndicator,
                                regionLabels_start_x + 1 * regionLabels_step_x, y + (rowContentsRectangle.Height - indicatorSize) / 2, indicatorSize, indicatorSize);

                            e.Graphics.DrawImage((row.RegionsCode.Contains("s")) ? RegionOnIndicator : RegionOffIndicator,
                                regionLabels_start_x + 0 * regionLabels_step_x, y + (rowContentsRectangle.Height - indicatorSize) / 2, indicatorSize, indicatorSize);
                        }
					}

					if (row.IconName != null)
					{
						var margin = 5;
						var iconRectangle = new RectangleFFromBounds
						{
							Right = rowTitleRectangle.Right - margin,
							Top = rowTitleRectangle.Top + margin,
							Width = rowTitleRectangle.Height - (2 * margin),
							Height = rowTitleRectangle.Height - (2 * margin)
						}.ToRectangleF();

						e.Graphics.DrawImage(Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + @"\images\" + row.IconName), iconRectangle);
					}

					foreach (var block in row.Blocks)
					{
						RenderBlock(e.Graphics, block, rowContentsRectangle);
					}
					
					if (mouseoverPoint.HasValue)
					{
						foreach (var block in row.MouseoverBlocks)
						{
							if (block.IsPointInside(timeline, rowContentsRectangle, mouseoverPoint.Value))
							{
								activeMouseover = block;
							}
						}
					}

					y = (int)Math.Ceiling(rowRectangle.Bottom);
				}

				if (section != sections[0])
				{
				    using (var borderPen = new Pen(SkinningDefs.TheInstance.GetColorData("cloud_time_chart_border_pen_colour", Color.Black),
				            SkinningDefs.TheInstance.GetFloatData("cloud_time_chart_border_width", 1f)))
				    {
				        e.Graphics.DrawLine(borderPen, sectionPaddedRectangle.Left, sectionPaddedRectangle.Top, sectionPaddedRectangle.Right, sectionPaddedRectangle.Top);
                    }
				}

				y = (int) Math.Ceiling(sectionPaddedRectangle.Bottom);
			}

			if (dependentRows.Any(r => rowLegendToContentBounds.ContainsKey(r.DependentRowLegend)))
			{
				RenderDependencies(e.Graphics, rowLegendToContentBounds, rowLegendToRowBounds);
			}

			boundsIdToRectangle["titles"] = new Rectangle(0, (int)startingY, (int)(timelineLeft + timelineLeftMargin), (int)(y - startingY));
			boundsIdToRectangle["chart"] = new Rectangle((int) (timelineLeft + timelineLeftMargin), (int) startingY, (int) contentWidth, (int) (y - startingY));

			PreferredHeight = (int) (accumulativeHeight + regionLabels_height) + 300;

            var hoverBarHeight = y - regionLabels_height; 

			// Render the timeline.
			var TimeLine_Offset_y = 26;
			y = TimeLine_Offset_y;
			timelineHeight = 45;
			float timelineTickHeight = 5;

			var timelineRectangle = new RectangleF(timelineLeft, y, timelineWidth, timelineHeight);

			var zeroPoint = MapFromRange(0, timeline.Start, timeline.End, timelineRectangle.Left + timelineLeftMargin, timelineRectangle.Right - timelineRightMargin);

			var timelineNegativeRectangle = new RectangleFFromBounds { Left = timelineLeft, Top = y, Right = zeroPoint, Height = timelineHeight}.ToRectangleF();
			var timelinePositiveRectangle = new RectangleFFromBounds { Left = zeroPoint, Top = y, Right = timelineRectangle.Right, Height = timelineHeight }.ToRectangleF();
			boundsIdToRectangle["timeline"] = timelineRectangle.ToRectangle();

			timelineLowEdge = (int)y + (int)timelineHeight;
			//Only draw the time line if there are services to display 
			if (rows > 0)
			{
				if (timelineNegativeRectangle.Width > 0)
				{
					using (Brush brush = new SolidBrush(timeline.MinutesNegativeBackColour))
					{
						e.Graphics.FillRectangle(brush, timelineNegativeRectangle);
					}
				}
				if (timelinePositiveRectangle.Width > 0)
				{
					using (Brush brush = new SolidBrush(timeline.MinutesBackColour))
					{
						e.Graphics.FillRectangle(brush, timelinePositiveRectangle);
					}
				}

				var timelineFont = Font_Body;

                if (timeline.UseTimelineFont)
                {
                    timelineFont = timeline.TimelineFont;
                }
                var sz = MeasureString(timelineFont, "20");
				
				var legendRectangle = new RectangleF(timelineRectangle.Left, timelineRectangle.Top, timelineRectangle.Width, timelineRectangle.Height - timelineTickHeight - timelineLabelHeight);
                RenderText(e.Graphics, timelineFont, timeline.Legend, timeline.TitleForeColour, legendRectangle, StringAlignment.Center, StringAlignment.Center);

				using (var tp = new Pen(timeline.MinutesForeColour))
				{
					//Draw the Horizontal Axis Line 
                    if (timeline.ShouldDrawMarkings)
                    {
                        e.Graphics.DrawLine(tp, timelineRectangle.Left + timelineLeftMargin, timelineRectangle.Top + legendRectangle.Height, timelineRectangle.Right - timelineRightMargin, timelineRectangle.Top + legendRectangle.Height);
                    }
					
					for (var t = timeline.Start; t <= timeline.End; t = AdvancePausingAtEnd(t, timeline.Interval, timeline.End))
					{
						var x = MapFromRange(t, timeline.Start, timeline.End, timelineRectangle.Left + timelineLeftMargin, timelineRectangle.Right - timelineRightMargin);
						var width = MapFromRange(t + timeline.Interval, timeline.Start, timeline.End, timelineRectangle.Left + timelineLeftMargin, timelineRectangle.Right - timelineRightMargin) - x;
						//Draw the tick Line for each time linetime point
                        if (timeline.ShouldDrawMarkings)
                        {
                            e.Graphics.DrawLine(tp, x, timelineRectangle.Top + legendRectangle.Height, x, timelineRectangle.Top + legendRectangle.Height + timelineTickHeight);
                        }
						//Draw the text for each time linetime point
						var labelRectangle = new RectangleF(x - (width / 2), timelineRectangle.Top + ((int)(3*sz.Height)/2), width, timelineLegendHeight);
                        RenderText(e.Graphics, timelineFont, CONVERT.ToStr(t), timeline.MinutesForeColour, labelRectangle, StringAlignment.Center, StringAlignment.Near);
					}
				}
			}

			//Draw the Regions Headers
			// Added flag as West Incident Gantt Report  
			// shouldn't display the region names and icons
			if (drawRegionIcons)
			{
				if (rows > 0)
				{
					regionLabels_start_x = maxSectionNameWidth;
					regionLabels_step_x = maxRowWidth / 4;

					var sf = new StringFormat(StringFormatFlags.DirectionVertical) { Alignment = StringAlignment.Far };

					if (regions != null)
					{
						var step = 0;
						foreach (var regionName in regions.RegionNames)
						{
							var rf = new RectangleF(regionLabels_start_x + step * regionLabels_step_x, 0, regionLabels_step_x, regionLabels_height);

							using (Brush brush = new SolidBrush(TimeLine_ForeColor))
							{
								e.Graphics.DrawString(regionName, Font_VerticalRegion, brush, rf, sf);
							}

							step++;
						}
					}
					else
					{
						for (var step = 0; step < 4; step++)
						{
							var data_name = "America";
							switch (step)
							{
								case 3:
									data_name = "America";
									break;
								case 2:
									data_name = "Europe";
									break;
								case 1:
									data_name = "Africa";
									break;
								case 0:
									data_name = "Asia";
									break;
							}
							var rf = new RectangleF(regionLabels_start_x + step * regionLabels_step_x, 0, regionLabels_step_x, regionLabels_height);

							using (Brush brush = new SolidBrush(TimeLine_ForeColor))
							{
								e.Graphics.DrawString(data_name, Font_VerticalRegion, brush, rf, sf);
							}
						}
					}

				}
			}

			if (activeMouseover != null)
			{
                if (activeMouseover is MouseoverBlockWithHoverBar)
                {
                    var rev = ((MouseoverBlockWithHoverBar)activeMouseover).LostRevenue;
                    RenderHoverBar(e.Graphics, activeMouseover.Start, activeMouseover.End, rev, timelineLeft + timelineLeftMargin,
                        timelineWidth - (timelineLeftMargin + timelineRightMargin), hoverBarHeight, regionLabels_height);
                }

				var size = e.Graphics.MeasureString(activeMouseover.Legend, Font_Body, 200);
				var margin = 6;
				size = new SizeF(size.Width + (margin * 2), size.Height + (margin * 2));
				var rectangle = new Rectangle(mouseoverPoint.Value.X - (int)(size.Width / 2), mouseoverPoint.Value.Y + 20, (int)(0.5f + size.Width), (int)(0.5f + size.Height));

				var bottom = Height;
				if (legendPanel != null)
				{
					bottom = legendPanel.Top - Top;
				}
				rectangle = PushRectangleWithinBounds(rectangle, new Rectangle (0, 0, Width, bottom));
				e.Graphics.FillRectangle(Brushes.White, rectangle);
				e.Graphics.DrawRectangle(Pens.Black, rectangle);
				e.Graphics.DrawString(activeMouseover.Legend, Font_Body, Brushes.Black, new Rectangle(rectangle.Left + margin, rectangle.Top + margin, rectangle.Width - margin, rectangle.Height - margin));
			}

			if (highlightX.HasValue)
			{
				var x = MapFromRange((float) highlightX.Value, timeline.Start, timeline.End, timelineRectangle.Left + timelineLeftMargin, timelineRectangle.Right - timelineRightMargin);
				using (var pen = new Pen (SkinningDefs.TheInstance.GetColorData("gantt_current_time_colour", Color.FromArgb(40, 0, 0, 0)), 5))
				{
					e.Graphics.DrawLine(pen, x, 0, x, Height);
				}
			}
		}

		void RenderDependencies (Graphics graphics, Dictionary<string, RectangleF> rowLegendToBounds, Dictionary<string, RectangleF> rowLegendToRowBounds)
		{
			var greyedOutRows = rowLegendToRowBounds.Where(kvp => dependentRows.All(r => r.DependentRowLegend != kvp.Key && r.PrerequisiteRowLegend != kvp.Key)).Select(kvp => kvp.Value).ToList();

			using (var greyOutBrush = new SolidBrush(Color.FromArgb(210, 200, 200, 200)))
			{
				foreach (var rowBounds in greyedOutRows)
				{
					graphics.FillRectangle(greyOutBrush, rowBounds);
				}
			}

			graphics.SmoothingMode = SmoothingMode.AntiAlias;
			graphics.CompositingQuality = CompositingQuality.HighQuality;
			graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
			graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

			const float arrowLineWidth = 3;
			using(var arrowBrush = new SolidBrush(Color.Black))
			using (var arrowPen = new Pen(arrowBrush, arrowLineWidth))
			{
				foreach (var row in dependentRows)
				{
					if (!rowLegendToBounds.ContainsKey(row.DependentRowLegend))
					{
						continue;
					}
					if (!rowLegendToBounds.ContainsKey(row.PrerequisiteRowLegend))
					{
						continue;
					}
					var dependentRowBounds = rowLegendToBounds[row.DependentRowLegend];
					var prerequisiteRowBounds = rowLegendToBounds[row.PrerequisiteRowLegend];

					var startY = new[] { dependentRowBounds.Y, dependentRowBounds.Bottom }.Select(v =>
					   new
					   {
						   Value = v,
						   Delta = Math.Abs(prerequisiteRowBounds.Y - v)
					   }).Aggregate((a, b) => a.Delta < b.Delta ? a : b).Value;

					var endY = new[] { prerequisiteRowBounds.Y, prerequisiteRowBounds.Bottom }.Select(v =>
					   new
					   {
						   Value = v,
						   Delta = Math.Abs(dependentRowBounds.Y - v)
					   }).Aggregate((a, b) => a.Delta < b.Delta ? a : b).Value;

					var startX = MapFromRange(row.DependentTime, timeline.Start, timeline.End, dependentRowBounds.Left, dependentRowBounds.Right);

					var endX = MapFromRange(row.PrerequisiteTime, timeline.Start, timeline.End, prerequisiteRowBounds.Left, prerequisiteRowBounds.Right);

					var offsetStartX = startX;
					var offsetEndX = endX;

					var lineDirectionSign = dependentRowBounds.Y < prerequisiteRowBounds.Y ? 1 : -1;

					var lineXs = new[]
					{
						offsetStartX,
						offsetEndX
					};

					var lineYs = new[]
					{
						startY,
						startY + (sectionLeading + rowLeading - arrowLineWidth) * lineDirectionSign,
						endY
					};

					var linePoints = new List<PointF>();

					for (var xIndex = 0; xIndex < lineXs.Length; xIndex++)
					{
						for (var yIndex = xIndex; yIndex < xIndex + 2; yIndex++)
						{
							linePoints.Add(new PointF(lineXs[xIndex], lineYs[yIndex]));
						}
					}

					graphics.DrawLines(arrowPen, linePoints.ToArray());

					//graphics.DrawOutlinedLines(linePoints.ToArray(), Color.Red, Color.Black, 1, 1);

					const float arrowTipAngle = 30f * 180 / (float)Math.PI;
					const float arrowTipLength = 4f;
					var triangleXOffset = arrowTipLength * (float)Math.Atan(arrowTipAngle);

					graphics.FillPolygon(arrowBrush, new []
					{
						new PointF(offsetEndX, endY),
						new PointF(offsetEndX + triangleXOffset, endY - arrowTipLength * lineDirectionSign),
						new PointF(offsetEndX - triangleXOffset, endY - arrowTipLength * lineDirectionSign)
					});

					foreach (var errorBlock in errorBlocks.Where(b => b.RowLegend == row.DependentRowLegend))
					{
						var errorStartX = MapFromRange(errorBlock.ErrorTime, timeline.Start, timeline.End, prerequisiteRowBounds.Left, prerequisiteRowBounds.Right);
						var errorEndX = MapFromRange(errorBlock.ErrorTime + errorBlock.ErrorBlockLength, timeline.Start, timeline.End, prerequisiteRowBounds.Left, prerequisiteRowBounds.Right);

						RenderErrorBlock(graphics, new RectangleFFromBounds
						{
							Left = errorStartX,
							Right = errorEndX,
							Top = dependentRowBounds.Top - rowLeading,
							Height = dependentRowBounds.Height + 2 * rowLeading
						}.ToRectangleF(), Color.FromArgb(199, Color.Black));

					}
				}
			}
		}

		static void RenderErrorBlock (Graphics graphics, RectangleF bounds, Color? fillColour = null)
		{
			var backColour = fillColour ?? Color.Black;

			using (var backBrush = new SolidBrush(backColour))
			using (var crossPen = new Pen(Color.Red, 2f))
			{
				graphics.FillRectangle(backBrush, bounds);

				graphics.DrawLine(crossPen, new PointF(bounds.Left, bounds.Top), new PointF(bounds.Right, bounds.Bottom));
				graphics.DrawLine(crossPen, new PointF(bounds.Left, bounds.Bottom), new PointF(bounds.Right, bounds.Top));

			}
		}

	    void RenderHoverBar (Graphics g, float start, float end, string value, 
            float timelineLeft, float timelineWidth, float height, float yPos)
	    {

	        var left = MapFromRange(start, timeline.Start, timeline.End, timelineLeft, timelineWidth + timelineLeft);
	        var right = MapFromRange(end, timeline.Start, timeline.End, timelineLeft, timelineWidth + timelineLeft);

	        var rect = new RectangleF(left, yPos, right - left, height);

	        using (Brush brush = new SolidBrush(Color.FromArgb(125, Color.LightBlue)))
	        {
	            g.FillRectangle(brush, rect);
	        }

	        float textHeight = 25;
	        float textWidth = 100;
            var textRect = new RectangleF(rect.Right - textWidth, yPos - 5, textWidth, textHeight);
            
	        var revFont = SkinningDefs.TheInstance.GetFont(10, FontStyle.Bold);
            
            RenderText(g, revFont, value, Color.Black, textRect, StringAlignment.Far, StringAlignment.Center);
        }

		static Rectangle PushRectangleWithinBounds(Rectangle a, Rectangle bounds)
		{
			var rightOverstep = a.Right - bounds.Right;
			if (rightOverstep > 0)
			{
				a = new Rectangle(a.Left - rightOverstep, a.Top, a.Width, a.Height);
			}

			var leftOverstep = bounds.Left - a.Left;
			if (leftOverstep > 0)
			{
				a = new Rectangle(a.Left + leftOverstep, a.Top, a.Width, a.Height);
			}

			var bottomOverstep = a.Bottom - bounds.Bottom;
			if (bottomOverstep > 0)
			{
				a = new Rectangle(a.Left, a.Top - bottomOverstep, a.Width, a.Height);
			}

			var topOverstep = bounds.Top - a.Top;
			if (topOverstep > 0)
			{
				a = new Rectangle(a.Left, a.Top + topOverstep, a.Width, a.Height);
			}

			return a;
		}

		float AdvancePausingAtEnd(float current, float increment, float end)
		{
			if (current < end)
			{
				return Math.Min(end, current + increment);
			}

			return end + increment;
		}

		public TimeChartLegendPanel CreateLegendPanel (Rectangle bounds, Color backColor, Color foreColor)
		{
			var panel = new TimeChartLegendPanel (sections, keyLegends, keyLegendToColour, keyLegendToPattern, 
				new Dictionary<CustomFillType, Action<Graphics, RectangleF, Color?>>
				{
					{CustomFillType.Error, RenderErrorBlock}
				}, keyLegendToCustomFill,  sortKeyEntries, optionalKeyEntryOrder)
			{
                ForeColor = foreColor
			};

			keyEnabled = true;
			legendPanel = panel;

			return panel;
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);

		    SetMouseoverPoint(e.Location);

			string id = null;
			Rectangle? bounds = null;

			foreach (var titledBounds in boundsIdToRectangle)
			{
				if (titledBounds.Value.Contains(e.Location))
				{
					id = titledBounds.Key;
					bounds = titledBounds.Value;
					break;
				}
			}

			if (id == null )
			{
				id = "All";
				bounds = ClientRectangle;
			}

            OnMouseEventFired( new SharedMouseEventArgs(new Point(e.Location.X - bounds.Value.X, e.Location.Y - bounds.Value.Y), MouseButtons.None, bounds.Value.Size, id));
		}
        
		protected override void OnMouseLeave(EventArgs e)
		{
			base.OnMouseLeave(e);
            SetMouseoverPoint(null);
            OnMouseEventFired(new SharedMouseEventArgs(null, MouseButtons.None, ClientSize));
		}

        void SetMouseoverPoint(Point? mouseLocation)
        {
            mouseoverPoint = mouseLocation;
            Invalidate();
        }

	    public override List<KeyValuePair<string, Rectangle>> BoundIdsToRectangles => boundsIdToRectangle.Select(kvp => new KeyValuePair<string, Rectangle>(kvp.Key, RectangleToScreen(kvp.Value))).ToList();

	    public override void ReceiveMouseEvent (SharedMouseEventArgs args)
        {
            base.ReceiveMouseEvent(args);

	        var boundsId = args.BoundsId;
	        var bounds = ClientRectangle;

	        if (!string.IsNullOrEmpty(boundsId) && boundsIdToRectangle.ContainsKey(boundsId))
	        {
		        bounds =boundsIdToRectangle[boundsId];
	        }
			
            SetMouseoverPoint(args.CalculateMouseLocation(bounds));
        }
    }
}