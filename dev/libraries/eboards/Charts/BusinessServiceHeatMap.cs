using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using System.Xml;

using Algorithms;
using CoreUtils;
using LibCore;
using ResizingUi;

namespace Charts
{
	public class BusinessServiceHeatMap : SharedMouseEventControl
	{
		class Category
		{
			public string Id { get; }
			public string Description { get; }

			public IList<Category> Subcategories { get; }
			readonly Category parentCategory;

			public Category (XmlElement element, Category parent = null)
			{
				Id = element.GetAttribute("id");
				Description = element.GetAttribute("desc");

				Subcategories = new List<Category> ();
				parentCategory = parent;

				foreach (XmlElement child in element.ChildNodes)
				{
					Subcategories.Add(new Category (child, this));
				}
			}
		}

		class BusinessService
		{
			readonly string name;
			public string Description { get; }
			public string ShortDescription { get; }

			readonly Dictionary<string, CategoryState> categoryIdToState;

			public List<int> Effectivenesses
			{
				get
				{
					var effectivenesses = GetCategoryState("Effectiveness");
					if (effectivenesses != null)
					{
						return effectivenesses.Errors.Select(e => CONVERT.ParseInt(e.Details)).ToList();
					}

					return null;
				}
			}

			public BusinessService (XmlElement element)
			{
				name = element.GetAttribute("name");
				Description = element.GetAttribute("desc");
				ShortDescription = element.GetAttribute("shortdesc");

				categoryIdToState = new Dictionary<string, CategoryState> ();
				foreach (XmlElement child in element.ChildNodes)
				{
					var stage = child.GetAttribute("stage");

					if (! categoryIdToState.ContainsKey(stage))
					{
						categoryIdToState.Add(stage, new CategoryState ());
					}

					categoryIdToState[stage].Errors.Add(new Error
					{
						Ok = child.GetBooleanAttribute("ok"),
						Details = child.GetAttribute("details")
					});

					foreach (XmlElement extra in child.ChildNodes)
					{
						categoryIdToState[stage].Extras.Add(extra.GetAttribute("type"));
					}
				}
			}

			public CategoryState GetCategoryState (string categoryId)
			{
				if (categoryIdToState.ContainsKey(categoryId))
				{
					return categoryIdToState[categoryId];
				}
				else
				{
					return null;
				}
			}

			public struct Error
			{
				public bool? Ok { get; set; }
				public string Details { get; set; }
			}

			public class CategoryState
			{
				public List<string> Extras { get; }
				public List<Error> Errors { get; }

				public CategoryState ()
				{
					Extras = new List<string> ();
					Errors = new List<Error> ();
				}
			}
		}

		IList<Category> categories;
		IList<BusinessService> businessServices;
		bool useFullBusinessServiceNames;

		float leftColumnWidth;
		float rightColumnWidth;
		float headerRowHeight;
		float rowHeight;
		float iconSize;
		float fontSize;
		bool showHeader;

		Color gridColour;

		public BusinessServiceHeatMap ()
		{
			categories = new List<Category> ();
			businessServices = new List<BusinessService> ();

			boundIdsDictionary = new Dictionary<string, Rectangle>();

			leftColumnWidth = 150;
			rightColumnWidth = 100;
			headerRowHeight = SkinningDefs.TheInstance.GetFloatData("product_quality_report_header_row_height", 20);
			rowHeight = 50;
			iconSize = 30;
			fontSize = SkinningDefs.TheInstance.GetFloatData("product_quality_report_service_name_size", 10);
			gridColour = SkinningDefs.TheInstance.GetColorData("product_quality_report_grid_colour", Color.Black);

			showHeader = true;
		}

		void DrawHeaderString (IGraphics graphics, string text, Font font, Color colour, RectangleF bounds)
		{
			var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

			if (SkinningDefs.TheInstance.GetBoolData("product_quality_report_left_align_headers", false))
			{
				format.Alignment = StringAlignment.Near;
			}

			graphics.DrawString(text, font, colour, bounds, format);
		}

		public bool UseFullServiceNames
		{
			get => useFullBusinessServiceNames;

			set
			{
				useFullBusinessServiceNames = value;
				Invalidate();
			}
		}

		public void LoadData (XmlElement xml)
		{
			categories = new List<Category> ();
			foreach (XmlElement category in xml.SelectSingleNode("Categories"))
			{
				categories.Add(new Category (category));
			}

			businessServices = new List<BusinessService> ();
			foreach (XmlElement businessService in xml.SelectSingleNode("Services"))
			{
				businessServices.Add(new BusinessService (businessService));
			}
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);

			Invalidate();
		}

		IList<string> GetLeafCategoryIds (IList<Category> categories)
		{
			var leaves = new List<string> ();

			foreach (var category in categories)
			{
				if (category.Subcategories.Count == 0)
				{
					leaves.Add(category.Id);
				}
				else
				{
					leaves.AddRange(GetLeafCategoryIds(category.Subcategories));
				}
			}

			return leaves;
		}

		int GetMaxSubCategoryDepth (IList<Category> categories)
		{
			return categories.Max(category => ((category.Subcategories.Count == 0) ? 1 : (1 + GetMaxSubCategoryDepth(category.Subcategories))));
		}

		void CalculateColumnWidths (IDictionary<string, float> idToWidth, IList<Category> categories, float singleWidth)
		{
			foreach (var category in categories)
			{
				if (category.Subcategories.Count == 0)
				{
					idToWidth[category.Id] = singleWidth;
				}
				else
				{
					CalculateColumnWidths(idToWidth, category.Subcategories, singleWidth);
					idToWidth[category.Id] = category.Subcategories.Sum(subcategory => idToWidth[subcategory.Id]);
				}
			}
		}

		protected override void OnPaint (PaintEventArgs e)
		{
			base.OnPaint(e);

			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
			e.Graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;

			var wrapper = new GdiGraphicsWrapper (e.Graphics);
			Render(wrapper, new RectangleF (0, 0, Width, Height), true);
		}

		public float LeftColumnWidth
		{
			get => leftColumnWidth;

			set
			{
				leftColumnWidth = value;
				Invalidate();
			}
		}

		public float RightColumnWidth
		{
			get => rightColumnWidth;

			set
			{
				rightColumnWidth = value;
				Invalidate();
			}
		}

		public float HeaderRowHeight
		{
			get => headerRowHeight;

			set
			{
				headerRowHeight = value;
				Invalidate();
			}
		}

		public float RowHeight
		{
			get => rowHeight;

			set
			{
				rowHeight = value;
				Invalidate();
			}
		}

		public float IconSize
		{
			get => iconSize;

			set
			{
				iconSize = value;
				Invalidate();
			}
		}

		public float FontSize
		{
			get => fontSize;

			set
			{
				fontSize = value;
				Invalidate();
			}
		}

		public bool ShowHeader
		{
			get => showHeader;

			set
			{
				showHeader = value;
				Invalidate();
			}
		}

		public float Render (IGraphics graphics, RectangleF bounds, bool colourHeader)
		{
			var headerDepth = GetMaxSubCategoryDepth(categories);
			var headerHeight = headerDepth * headerRowHeight;

			var categoryIdToWidth = new Dictionary<string, float>();

			var leafCategoryIds = GetLeafCategoryIds(categories);
			var categoryWidth = (bounds.Width - leftColumnWidth - rightColumnWidth) / leafCategoryIds.Count;

			CalculateColumnWidths(categoryIdToWidth, categories, categoryWidth);

			Color [] rowBackColours =
			{
				SkinningDefs.TheInstance.GetColorData("gantt_chart_row_back_colour_one", Color.White),
				SkinningDefs.TheInstance.GetColorData("gantt_chart_row_back_colour_two", Color.GhostWhite)
			};

			var y = bounds.Top;

			var headerBox = new RectangleF(bounds.Left, y, bounds.Width, headerHeight);

			boundIdsDictionary["headers"] = headerBox.ToRectangle();

			if (showHeader)
			{
				y = headerBox.Bottom;
			}

			var topOfChart = y;

			var baseFont = SkinningDefs.TheInstance.GetFont(10);

			var row = 0;
			var startingY = y;

			var effectivenessStrings = new List<string> ();
			var effectivenessBoxes = new List<RectangleF> ();

			foreach (var service in businessServices)
			{
				var rowBox = new RectangleF(bounds.Left, y, bounds.Width, rowHeight * GetNumberOfDevelopmentAttempts(service));
				y = rowBox.Bottom;

				var background = rowBackColours[row % rowBackColours.Length];

				graphics.FillRectangle(background, rowBox);

				var serviceBox = new RectangleF(bounds.Left, rowBox.Top, leftColumnWidth, rowBox.Height);
				graphics.DrawString(useFullBusinessServiceNames ? service.Description : service.ShortDescription,
					SkinningDefs.TheInstance.GetFont(fontSize, FontStyle.Bold), Color.Black, serviceBox,
					new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center });

				var x = serviceBox.Right;
				foreach (var leafCategoryId in leafCategoryIds)
				{
					var stageBox = new RectangleF (x, rowBox.Top, categoryIdToWidth[leafCategoryId], rowBox.Height);
					x = stageBox.Right;

					var results = service.GetCategoryState(leafCategoryId);
					if (results != null)
					{
						var errors = results.Errors.Skip(Math.Max(0, results.Errors.Count - 3)).ToList();

						var infoRows = errors.Count;
						var infoRowHeight = Math.Min(iconSize, stageBox.Height / (infoRows * 1.5f));
						var infoRowLeading = infoRowHeight / 2;

						var infoY = stageBox.Top + (stageBox.Height / 2) -
						            (((infoRows * infoRowHeight) + ((infoRows - 1) * infoRowLeading)) / 2);
						foreach (var result in errors)
						{
							var imageBox = new RectangleF(stageBox.Left + (stageBox.Width / 20), infoY, infoRowHeight,
								infoRowHeight);

							if (result.Ok.HasValue)
							{
								using (var image = ImageUtils.CloneImageOntoColour(
									Repository.TheInstance.GetImage(
										AppInfo.TheInstance.Location + @"\images\chart\" +
										(result.Ok.Value ? "tick.png" : "cross.png")),
									background))
								{
									graphics.DrawImage(image, imageBox);
								}
							}

							var leftOffset = stageBox.Width / 20;
							var availableSpace = new RectangleF(imageBox.Right + leftOffset, infoY,
								stageBox.Right - imageBox.Right - leftOffset, infoRowHeight);

							var sizeInBaseFont = graphics.MeasureString(result.Details, baseFont);
							var sizeToFitColumnWidth = baseFont.Size * availableSpace.Width / sizeInBaseFont.Width;

							var font = SkinningDefs.TheInstance.GetPixelSizedFont(Math.Min(infoRowHeight,
								sizeToFitColumnWidth));
							graphics.DrawString(result.Details, font, Color.Black, availableSpace,
								new StringFormat { LineAlignment = StringAlignment.Center });

							infoY += infoRowHeight + infoRowLeading;
						}

						var extrasLeading = 5;
						var extrasRowHeight = infoRowHeight;// (stageBox.Height - extrasLeading) / 2;
						var extraY = stageBox.Top + (stageBox.Height / 2) -
						             (((results.Extras.Count * extrasRowHeight) +
						               ((results.Extras.Count - 1) * extrasLeading)) / 2);
						foreach (var extra in results.Extras)
						{
							var imageBox = new RectangleF(stageBox.Right - (stageBox.Width / 20) - extrasRowHeight,
								extraY, extrasRowHeight, extrasRowHeight);

							using (var image = ImageUtils.CloneImageOntoColour(
								Repository.TheInstance.GetImage(
									AppInfo.TheInstance.Location + @"\images\chart\" +
									(extra == "cost" ? "cash.png" : "clock.png")),
								background))
							{
								graphics.DrawImage(image, imageBox);
							}

							extraY += extrasRowHeight + extrasLeading;
						}
					}
				}

				var effectivenessesBox = new RectangleF (x, rowBox.Top, rightColumnWidth, rowBox.Height);

				var effectivenesses = service.Effectivenesses;
				for (int i = 0; i < (effectivenesses?.Count ?? 0); i++)
				{
					var effectiveness = service.Effectivenesses[i];
					var effectivenessBox = new RectangleF (effectivenessesBox.Left, effectivenessesBox.Top + (i * effectivenessesBox.Height / effectivenesses.Count),
															effectivenessesBox.Width, effectivenessesBox.Height / effectivenesses.Count);

					var colour = SkinningDefs.TheInstance.GetColorData("quality_report_high_effectiveness_background_colour", Color.FromArgb(140, 198, 63));
					if (effectiveness <= 50)
					{
						colour = SkinningDefs.TheInstance.GetColorData("quality_report_low_effectiveness_background_colour", Color.FromArgb(237, 28, 36));
					}
					else if (effectiveness <= 90)
					{
						colour = SkinningDefs.TheInstance.GetColorData("quality_report_mid_effectiveness_background_colour", Color.FromArgb(251, 176, 59));
					}

					graphics.FillRectangle(colour, effectivenessBox);

					var effectivenessString = CONVERT.Format("{0}%", effectiveness);

					effectivenessBoxes.Add(effectivenessBox);
					effectivenessStrings.Add(effectivenessString);
				}

				if (row > 0)
				{
					graphics.DrawLine(gridColour, 1, rowBox.Left, rowBox.Top, rowBox.Right, rowBox.Top);
				}

				row++;
			}

			using (var font = FontScalerExtensions.GetFontToFit(this, FontStyle.Regular, effectivenessStrings, effectivenessBoxes.Select(box => box.Size).ToList()))
			{
				for (var i = 0; i < effectivenessStrings.Count; i++)
				{
					var effectivenessString = effectivenessStrings [i];
					var effectivenessBox = effectivenessBoxes [i];

					graphics.DrawString(effectivenessString,
						font, Color.Black,
						effectivenessBox,
						new StringFormat
						{
							Alignment = StringAlignment.Center,
							LineAlignment = StringAlignment.Center
						});
				}
			}

			var contentHeight = y - startingY;

			var titlesBound = new RectangleF(bounds.X, startingY, leftColumnWidth, contentHeight);

			boundIdsDictionary["service_titles"] = titlesBound.ToRectangle();

			var categoriesBounds = new RectangleF(titlesBound.Right, startingY, leafCategoryIds.Select(c => categoryIdToWidth[c]).Sum(), contentHeight);

			boundIdsDictionary["categories"] = categoriesBounds.ToRectangle();

			boundIdsDictionary["effectiveness_column"] = new RectangleF(categoriesBounds.Right, startingY, rightColumnWidth, contentHeight).ToRectangle();

			if (showHeader)
			{
				RenderHeader(graphics, new RectangleF(headerBox.Left + leftColumnWidth, headerBox.Top, headerBox.Width - leftColumnWidth, headerBox.Height),
								categories, categoryIdToWidth, headerRowHeight, GetMaxSubCategoryDepth(categories), 0, colourHeader);
			}

			{
				var x = bounds.Left + leftColumnWidth;
				foreach (var leafCategoryId in leafCategoryIds)
				{
					graphics.DrawLine(gridColour, 1, x, topOfChart, x, y);
					x += categoryIdToWidth[leafCategoryId];
				}

				graphics.DrawLine(gridColour, 1, x, topOfChart, x, y);
			}
			
			return y;
		}

		int GetNumberOfDevelopmentAttempts (BusinessService service)
		{
			return Math.Max(service.Effectivenesses?.Count ?? 0,
							GetLeafCategoryIds(categories).Max(id => service.GetCategoryState(id)?.Errors?.Count ?? 0));
		}

		RectangleF GetEffectiveHeaderItemBounds (RectangleF sourceBox)
		{
			if (SkinningDefs.TheInstance.GetBoolData("product_quality_report_left_align_headers", false))
			{
				return new RectangleF (sourceBox.Left + 20, sourceBox.Top, sourceBox.Width - 20, sourceBox.Height);
			}
			else
			{
				return sourceBox;
			}
		}

		string GetEffectiveHeaderItemString (string source)
		{
			if (SkinningDefs.TheInstance.GetBoolData("product_quality_report_headers_in_caps", false))
			{
				return source.ToUpper();
			}
			else
			{
				return source;
			}
		}

		void RenderHeader (IGraphics graphics, RectangleF bounds, IList<Category> categories, IDictionary<string, float> categoryIdToWidth, float rowHeight, int deepestCategory, int currentDepth, bool colourHeader)
		{
			if (colourHeader && (currentDepth == 0))
			{
				graphics.FillRectangle(SkinningDefs.TheInstance.GetColorData("table_header_color"), bounds);
			}
			
			var effectivenessBox = new RectangleF(bounds.Right - rightColumnWidth, bounds.Top, rightColumnWidth, headerRowHeight);
			var effectivenessString = "Effectiveness";

			var colour = SkinningDefs.TheInstance.GetColorData("product_quality_report_header_text_colour", colourHeader ? Color.White : Color.Black);

			var strings = categories.Select(c => GetEffectiveHeaderItemString(c.Description)).ToList();
			var sectionBoxes = new List<RectangleF>();
			var effectiveSectionBoxes = new List<RectangleF>();
			var x = bounds.Left;
			foreach (var category in categories)
			{
				var sourceBounds = new RectangleF(x, bounds.Top, categoryIdToWidth[category.Id], rowHeight);
				sectionBoxes.Add(sourceBounds);
				var effectiveBounds = GetEffectiveHeaderItemBounds(sourceBounds);
				effectiveSectionBoxes.Add(effectiveBounds);
				x = sourceBounds.Right;
			}

			if (currentDepth == 0)
			{
				strings.Add(GetEffectiveHeaderItemString(effectivenessString));
				sectionBoxes.Add(effectivenessBox);
				effectiveSectionBoxes.Add(GetEffectiveHeaderItemBounds(effectivenessBox));
			}

			using (var font = ResizingUi.FontScalerExtensions.GetFontToFit(this, FontStyle.Bold, strings, effectiveSectionBoxes.Select(s => s.Size).ToList()))
			{
				for (var i = 0; i < categories.Count; i++)
				{
					var sectionBox = sectionBoxes[i];
					var effectiveSectionBox = effectiveSectionBoxes[i];

					DrawHeaderString(graphics, strings[i], font, colour, effectiveSectionBox);

					graphics.DrawLine(gridColour, 1, sectionBox.Left, sectionBox.Top, sectionBox.Left, bounds.Bottom);
					graphics.DrawLine(gridColour, 1, sectionBox.Right, sectionBox.Top, sectionBox.Right, bounds.Bottom);

					if (categories[i].Subcategories.Count > 0)
					{
						RenderHeader(graphics, new RectangleF(sectionBox.Left, sectionBox.Top + rowHeight, sectionBox.Width, bounds.Height - rowHeight), categories[i].Subcategories, categoryIdToWidth, rowHeight, deepestCategory, currentDepth + 1, colourHeader);
					}
				}

				DrawHeaderString(graphics, strings[strings.Count - 1], font, colour, effectiveSectionBoxes[effectiveSectionBoxes.Count - 1]);
			}
		}

		public int NaturalHeight => (int) ((GetMaxSubCategoryDepth(categories) * headerRowHeight * (showHeader ? 1 : 0)) + (businessServices.Sum(s => GetNumberOfDevelopmentAttempts(s)) * rowHeight));

		readonly Dictionary<string, Rectangle> boundIdsDictionary;

		public override List<KeyValuePair<string, Rectangle>> BoundIdsToRectangles => 
			boundIdsDictionary.Select(kvp => new KeyValuePair<string, Rectangle>(kvp.Key, RectangleToScreen(kvp.Value))).ToList();
	}
}