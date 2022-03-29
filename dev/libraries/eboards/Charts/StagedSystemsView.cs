using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using Algorithms;
using CoreUtils;
using LibCore;
using Network;
using ResizingUi;

namespace Charts
{
	public class StagedSystemsView : Panel
	{
		NodeTree model;
		int round;

		Dictionary<Node, Image> level1StageToIcon;

		public StagedSystemsView (NodeTree model, int round)
		{
			this.model = model;
			this.round = round;

			level1StageToIcon = new Dictionary<Node, Image> ();
			var stages = model.GetNamedNode("Stages").GetChildrenAsList();
			foreach (var stage in stages)
			{
				var service = model.GetNamedNode(stage.GetAttribute("desc"));
				var iconName = service.GetAttribute("icon");
				var icon = LibCore.Repository.TheInstance.GetImage(LibCore.AppInfo.TheInstance.Location + @"\images\icons\" + iconName + ".png");
				var recolouredIcon = BitmapExtensions.ConvertColours(icon, Color.White, Color.Black, Color.Pink, Color.Pink);
				level1StageToIcon.Add(stage, recolouredIcon);
			}
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);
			Invalidate();
		}

		string GetLevel1StageLabel (Node stage)
		{
			var description = stage.GetAttribute("desc");
			var number = stage.GetAttribute("level_1_stage");
			return $"{number}: {description}";
		}

		string GetLevel2StageLabel (Node stage)
		{
			var description = stage.GetAttribute("desc");
			var number = stage.GetAttribute("full_stage");
			return $"{number}: {description}";
		}

		protected override void OnPaint (PaintEventArgs e)
		{
			base.OnPaint(e);
			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

			var stages = model.GetNamedNode("Stages").GetChildrenAsList();
			var level1StageSize = new Size(Width / stages.Count, Height);

			var level1StageTitles = new List<string> ();
			var level1StageTitleBoxes = new List<RectangleF> ();

			using (var dividerPen = new Pen(Color.FromArgb(242, 242, 242), 2) { DashStyle = DashStyle.Dot })
			{
				for (var i = 0; i < stages.Count; i++)
				{
					var stageBounds = new Rectangle(i * level1StageSize.Width, 0, level1StageSize.Width, level1StageSize.Height);

					DrawLevel1Stage(e.Graphics, stages[i], stageBounds, (i == 0), (i == (stages.Count - 1)), level1StageTitles, level1StageTitleBoxes);

					if (i > 0)
					{
						e.Graphics.DrawLine(dividerPen, stageBounds.Left, 0, stageBounds.Left, Height);
					}
				}
			}

			using (var font = this.GetFontToFit(FontStyle.Regular, level1StageTitles, level1StageTitleBoxes.Select(box => box.Size).ToList()))
			{
				for (int i = 0; i < stages.Count; i++)
				{
					e.Graphics.DrawString(level1StageTitles[i], font, Brushes.Black, level1StageTitleBoxes[i], new StringFormat
					{
						Alignment = StringAlignment.Center,
						LineAlignment = StringAlignment.Center,
						FormatFlags = StringFormatFlags.NoClip | StringFormatFlags.NoWrap
					});
				}
			}
		}

		void DrawLevel1Stage (Graphics graphics, Node stage, Rectangle bounds, bool isFirst, bool isLast, List<string> level1StageTitles, List<RectangleF> level1StageTitleBoxes)
		{
			var level2Stages = stage.GetChildrenAsList();
			var isParallel = (stage.GetAttribute("layout").ToLower() == "parallel");

			var lineThickness = 4;
			var circleRadius = 20;

			using (var crossStagePen = new Pen(Color.FromArgb(232, 78, 16), lineThickness) { StartCap = LineCap.Flat, EndCap = LineCap.Flat, DashStyle = DashStyle.Dot })
			using (var withinStagePen = new Pen(Color.FromArgb(232, 78, 16), lineThickness) { StartCap = LineCap.Flat, EndCap = LineCap.Flat })
			using (var retiredPen = new Pen(Maths.Lerp(0.9f, crossStagePen.Color, Color.White), lineThickness) { StartCap = LineCap.Flat, EndCap = LineCap.Flat })
			using (var level2Font = SkinningDefs.TheInstance.GetFont(14, FontStyle.Bold))
			using (var featureFont = SkinningDefs.TheInstance.GetFont(11, FontStyle.Bold))
			using (var iconBackgroundBrush = new SolidBrush(Color.FromArgb(242, 242, 242)))
			{
				var instepFraction = 0.08f;
				var instep = (int) (bounds.Width * instepFraction);
				var iconGap = 10;

				var legendHeight = 50;

				var headerBounds = new RectangleFromBounds
				{
					Left = bounds.Left,
					Top = bounds.Top,
					Right = bounds.Right,
					Height = (int) (legendHeight * 3.5f)
				}.ToRectangle();

				var iconSize = (int) (headerBounds.Height - legendHeight - iconGap);
				var iconBounds = new Rectangle ((headerBounds.Left + headerBounds.Right - iconSize) / 2, headerBounds.Top, iconSize, iconSize);
				graphics.FillEllipse(iconBackgroundBrush, iconBounds);
				graphics.DrawImage(level1StageToIcon [stage], iconBounds);

				var legendBounds = new RectangleFromBounds
				{
					Left = headerBounds.Left,
					Top = iconBounds.Bottom + iconGap,
					Right = headerBounds.Right,
					Height = legendHeight
				}.ToRectangle();

				var label = GetLevel1StageLabel(stage);
				level1StageTitles.Add(label);
				level1StageTitleBoxes.Add(legendBounds);

				var contentBounds = new RectangleFromBounds
				{
					Left = bounds.Left + (isFirst ? 0 : instep),
					Top = headerBounds.Bottom + 10,
					Right = bounds.Right - (isLast ? 0 : instep),
					Bottom = bounds.Bottom
				}.ToRectangle();

				var midY = contentBounds.Top + (contentBounds.Height / 2);

				if (!isFirst)
				{
					graphics.DrawLine(crossStagePen, bounds.Left, midY, contentBounds.Left, midY);
				}

				if (level2Stages.Count > 0)
				{
					if (isParallel)
					{
						var level2StageHeight = contentBounds.Height / level2Stages.Count;

						foreach (var showOnlyRetired in new [] { true, false })
						{
							for (int i = 0; i < level2Stages.Count; i++)
							{
								var level2Stage = level2Stages [i];
								var isRetired = level2Stage.GetBooleanAttribute("is_disabled", false);

								if (isRetired != showOnlyRetired)
								{
									continue;
								}

								var linePen = (isRetired ? retiredPen : withinStagePen);

								var subStageBounds = new Rectangle(contentBounds.Left, contentBounds.Top + (level2StageHeight * i), contentBounds.Width, level2StageHeight);

								if (!isFirst)
								{
									graphics.DrawLine(linePen, subStageBounds.Left, midY, subStageBounds.Left, subStageBounds.Top + (level2StageHeight / 2));
								}

								DrawLevel2Stage(graphics, withinStagePen, retiredPen, level2Font, featureFont, circleRadius, level2Stage,
									bounds, subStageBounds,
									isFirst, isLast, isParallel,
									ContentAlignment.BottomCenter);

								if (!isLast)
								{
									graphics.DrawLine(linePen, subStageBounds.Right, midY, subStageBounds.Right, subStageBounds.Top + (level2StageHeight / 2));
								}
							}
						}
					}
					else
					{
						var level2StageWidth = contentBounds.Width / level2Stages.Count;
						var drawOnTop = true;

						for (int i = 0; i < level2Stages.Count; i++)
						{
							DrawLevel2Stage(graphics, withinStagePen, retiredPen, level2Font, featureFont, circleRadius, level2Stages [i],
								bounds, new Rectangle(contentBounds.Left + (level2StageWidth * i), contentBounds.Top, level2StageWidth, contentBounds.Height),
								false, false, isParallel,
								drawOnTop ? ContentAlignment.TopCenter : ContentAlignment.BottomCenter);
							drawOnTop = !drawOnTop;
						}
					}
				}

				if (!isLast)
				{
					graphics.DrawLine(crossStagePen, contentBounds.Right, midY, bounds.Right, midY);
				}
			}
		}

		void DrawLevel2Stage (Graphics graphics, Pen mainPen, Pen retiredPen, Font font, Font featureFont, int circleRadius, Node stage, Rectangle overallBounds, Rectangle bounds,
		                      bool isFirst, bool isLast, bool isParallel, ContentAlignment textAlignment)
		{
			var isRetired = stage.GetBooleanAttribute("is_disabled", false);
			var pen = (isRetired ? retiredPen : mainPen);

			var midPoint = new Point(bounds.Left + (bounds.Width / 2), bounds.Top + (bounds.Height / 2));

			if (! isFirst)
			{
				graphics.DrawLine(pen, bounds.Left, midPoint.Y, midPoint.X, midPoint.Y);
			}

			if (! isLast)
			{
				graphics.DrawLine(pen, midPoint.X, midPoint.Y, bounds.Right, midPoint.Y);
			}

			var circleBounds = new Rectangle(midPoint.X - circleRadius, midPoint.Y - circleRadius, 2 * circleRadius, 2 * circleRadius);
			graphics.FillEllipse(Brushes.White, circleBounds);
			graphics.DrawEllipse(pen, circleBounds);

			using (var labelBrush = new SolidBrush (isRetired ? retiredPen.Color : Color.Black))
			{
				int textLeft;
				int textRight;

				if (isParallel)
				{
					if (isFirst)
					{
						textLeft = Math.Max(overallBounds.Left, bounds.Left - bounds.Width);
					}
					else
					{
						textLeft = bounds.Left;
					}

					if (isLast)
					{
						textRight = Math.Min(overallBounds.Right, bounds.Right + bounds.Width);
					}
					else
					{
						textRight = bounds.Right;
					}
				}
				else
				{
					textLeft = Math.Max(overallBounds.Left, bounds.Left - bounds.Width);
					textRight = Math.Min(overallBounds.Right, bounds.Right + bounds.Width);
				}

				var nameFormat = new StringFormat
				{
					Alignment = StringAlignment.Center,
					LineAlignment = Alignment.GetVerticalAlignment(textAlignment)
				};
				var featureFormat = new StringFormat
				{
					Alignment = StringAlignment.Center,
					LineAlignment = Alignment.ReverseAlignment(Alignment.GetVerticalAlignment(textAlignment))
				};

				var text = GetLevel2StageLabel(stage);
				var allowableTextArea = new Rectangle (textLeft, bounds.Top, textRight - textLeft, bounds.Height);
				var textSize = graphics.MeasureString(text, font, new SizeF (allowableTextArea.Width, 0), nameFormat);

				var features = GetFeaturesAddedToLevel2Stage(stage, out var retiringFeatureSpec);
				var featuresText = string.Join(" ", features);
				var featuresTextSize = graphics.MeasureString(featuresText, featureFont, new SizeF(bounds.Width * 3, 0), featureFormat);

				float textMidX = midPoint.X;
				var maxTextWidth = Math.Max(textSize.Width, featuresTextSize.Width);

				textMidX = Maths.Clamp(textMidX, allowableTextArea.Left + (maxTextWidth / 2), allowableTextArea.Right - (maxTextWidth / 2));
				var textMidPoint = new Point ((int) textMidX, allowableTextArea.Top + (allowableTextArea.Height / 2));

				Point referencePoint;
				Point featuresReferencePoint;
				var leading = 10;
				if (textAlignment == ContentAlignment.BottomCenter)
				{
					referencePoint = new Point(textMidPoint.X, (int) (circleBounds.Bottom + leading));
					featuresReferencePoint = new Point(textMidPoint.X, (int) (referencePoint.Y + textSize.Height + leading));
				}
				else
				{
					referencePoint = new Point(textMidPoint.X, (int) (circleBounds.Top - textSize.Height - leading));
					featuresReferencePoint = new Point(textMidPoint.X, (int) (referencePoint.Y - leading - featuresTextSize.Height));
				}

				textSize.Width *= 1.1f;
				var nameBounds = new RectangleF(referencePoint.X - (textSize.Width / 2), referencePoint.Y, textSize.Width, textSize.Height);
				var featureBounds = new RectangleF(featuresReferencePoint.X - (featuresTextSize.Width / 2), featuresReferencePoint.Y, featuresTextSize.Width, featuresTextSize.Height);

				var featuresColour = Color.FromArgb(24, 197, 50);
				if (isRetired)
				{
					featuresColour = pen.Color;
				}

				graphics.DrawString(text, font, labelBrush, nameBounds, nameFormat);

				using (var featuresBrush = new SolidBrush(featuresColour))
				{
					graphics.DrawString(featuresText, featureFont, featuresBrush, featureBounds, featureFormat);
				}

				if (isRetired)
				{
					var retiredFormat = new StringFormat
					{
						Alignment = StringAlignment.Center,
						LineAlignment = StringAlignment.Center,
						Trimming = StringTrimming.None
					};

					graphics.DrawString(retiringFeatureSpec, font, labelBrush, midPoint, retiredFormat);
				}
			}
		}

		List<string> GetFeaturesAddedToLevel2Stage (Node level2stage, out string retiringFeatureSpec)
		{
			var level2StageNumber = level2stage.GetAttribute("full_stage");
			retiringFeatureSpec = null;
			var features = new List<string>();

			for (var round = 1; round <= model.GetNamedNode("CurrentRound").GetIntAttribute("round", 0); round++)
			{
				foreach (var completedFeature in model.GetNamedNode($"Round {round} Completed New Services").GetChildrenAsList())
				{
					var featureSpec = model.GetNodesWithAttributeValue("service_id", completedFeature.GetAttribute("service_id"))
						.Cast<Node>().Single(f => ((f.GetAttribute("type") == "New_Service") &&
						                           (f.GetIntAttribute("round", 0) == round)));
					var featureId = featureSpec.GetAttribute("service_id");
					var featureLevel2Stage = featureSpec.GetAttribute("level_2_stage_affected");
					var featureLevel2StageRetired = featureSpec.GetAttribute("level_2_stage_removed");
					var featureBusinessService = model.GetNamedNode(featureSpec.GetAttribute("biz_service_function"));
					var featureLevel1Stage = model.GetNamedNode("Stages").GetChildWithAttributeValue("desc", featureBusinessService.GetAttribute("name")).GetAttribute("level_1_stage");
					var featureFullStage = featureLevel1Stage + "." + featureLevel2Stage;

					if (completedFeature.GetBooleanAttribute("is_prototype", false))
					{
						featureId = "MVP-" + featureId;
					}

					if (! string.IsNullOrEmpty(featureLevel2StageRetired))
					{
						var featureRetiringFullStage = featureLevel1Stage + "." + featureLevel2StageRetired;
						if (featureRetiringFullStage == level2StageNumber)
						{
							retiringFeatureSpec = featureId;
						}
					}

					if ((! completedFeature.GetBooleanAttribute("is_prototype", false))
						&& (featureFullStage == level2StageNumber)
						&& (round == this.round))
					{
						features.Add(featureId);
					}
				}
			}

			return features;
		}
	}
}