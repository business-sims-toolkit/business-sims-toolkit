using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

using Algorithms;
using CommonGUI;
using CoreUtils;
using DevOpsEngine;
using DevOpsEngine.StringConstants;
using Events;
using LibCore;
using Network;
using ResizingUi;
using ResizingUi.Component;
using ResizingUi.Enums;
using Orientation = System.Windows.Forms.Orientation;

namespace DevOpsUi.FacilitatorControls.ServiceLozenge
{
	internal class LozengeFeatureRow : FlickerFreePanel
	{
		struct DevelopmentStatus
		{
			public const string Dev = "dev";
			public const string Test = "test";
			public const string Release = "release";
			public const string Live = "live";
			public const string LivePrototype = "live_prototype";

			public static IEnumerable<string> AllStatuses { get; } = new List<string>
			{
				Dev, Test, Release, Live, LivePrototype
			};
		}

		public string FeatureId { get; }

		public event EventHandler<LozengeFeatureRow> RowToBeRemoved; 


		public Node FeatureNode => GetLatestProductNode();

		public event EventHandler<string> FeatureRedeveloped;
		public event EventHandler<Node> ProgressionPanelOpened;

		readonly Color borderColour;
		const int borderThickness = 1;

		readonly List<RectangleSides> borderedSides;
		public LozengeFeatureRow(NodeTree model, int rowIndex, Node beginServicesNode, Node featureNode, string serviceName, bool isLiveGame, DevelopingAppTerminator appTerminator, List<RectangleSides> borderedSides)
		{
			this.borderedSides = borderedSides;

			borderColour = SkinningDefs.TheInstance.GetColorData("lozenge_border_colour", CONVERT.ParseHtmlColor("#00202d"));

			modelOfficeNode = model.GetNamedNode("ModelOffice");
			RowIndex = rowIndex;

			animationComponent = new ControlAnimationComponent();
			animationComponent.AnimationTick += animationComponent_AnimationTick;

			productNodes = new List<Node>();

			if (featureNode != null)
			{
				TrackDevelopingProduct(featureNode);
				FeatureId = featureNode.GetAttribute("service_id");
			}

			this.serviceName = serviceName;
			this.isLiveGame = isLiveGame; 
			this.appTerminator = appTerminator;

			this.beginServicesNode = beginServicesNode;
			beginServicesNode.ChildAdded += beginServicesNode_ChildAdded;
			beginServicesNode.ChildRemoved += beginServicesNode_ChildRemoved;

			developmentStatusToColours = DevelopmentStatus.AllStatuses.ToDictionary(s => s, s =>
				new StatusColours
				{
					ReticuleColour = SkinningDefs.TheInstance.GetColorData($"feature_{s}_reticule_colour", Color.HotPink),
					CircleFillColour = SkinningDefs.TheInstance.GetColourData($"feature_{s}_back_colour"),
					CircleOutlineColour = SkinningDefs.TheInstance.GetColourData($"feature_{s}_outline_colour"),
					ForeColour = SkinningDefs.TheInstance.GetColorData($"feature_{s}_fore_colour", Color.HotPink)
				});

			flashInterval = 1000;
			var flashDuration = flashInterval * 5;
			
			developmentCompletedFlashTimer = new Timer
			{
				Interval = flashInterval
			};
			developmentCompletedFlashTimer.Tick += (sender, args) =>
			{
				elapsedTime += flashInterval;
				if (elapsedTime >= flashDuration)
				{
					developmentCompletedFlashTimer.Stop();
				}

				Invalidate();
			};
		}

		void animationComponent_AnimationTick(object sender, EventArgs<AnimationProperties> e)
		{
			if (e.Parameter.Location.HasValue)
			{
				Location = e.Parameter.Location.Value.ToPoint();
			}
		}

		public void AnimateTo (Point newLocation)
		{
			if (newLocation == Location)
			{
				return;
			}

			animationComponent.AnimateTo(
				new AnimationProperties
				{
					Location = Location
				},
				new AnimationProperties
				{
					Location = newLocation
				}, 0.1f);
		}

		readonly ControlAnimationComponent animationComponent;

		protected override void Dispose (bool disposing)
		{
			if (disposing)
			{
				beginServicesNode.ChildAdded -= beginServicesNode_ChildAdded;
				beginServicesNode.ChildRemoved -= beginServicesNode_ChildRemoved;

				foreach (var product in productNodes)
				{
					product.AttributesChanged -= productNode_AttributesChanged;
				}
			}

			base.Dispose(disposing);
		}

		bool IsValidFeatureNode (Node featureNode)
		{
			if (string.IsNullOrEmpty(FeatureId))
			{
				return false;
			}
			return featureNode.GetAttribute("biz_service_function") == serviceName &&
			       featureNode.GetAttribute("service_id") == FeatureId;
		}

		void beginServicesNode_ChildAdded(Node sender, Node child)
		{
			if (!IsValidFeatureNode(child))
			{
				return;
			}

			TrackDevelopingProduct(child);
			Invalidate();
		}

		void beginServicesNode_ChildRemoved(Node sender, Node child)
		{
			if (!IsValidFeatureNode(child))
			{
				return;
			}

			StopTrackingNode(child);
			Invalidate();
		}

		void productNode_AttributesChanged (Node sender, ArrayList attrs)
		{
			var attrsList = attrs.Cast<AttributeValuePair>().ToList();

			if (attrsList.Any(avp => avp.Attribute == StageAttribute.DeploymentStage && avp.Value == StageStatus.Completed))
			{
				elapsedTime = 0;
				developmentCompletedFlashTimer.Start();
			}

			Invalidate();
		}

		void TrackDevelopingProduct (Node nodeToTrack)
		{
			if (productNodes.Contains(nodeToTrack))
			{
				return;
			}
			productNodes.Add(nodeToTrack);
			nodeToTrack.AttributesChanged += productNode_AttributesChanged;

			Invalidate();
		}

		void StopTrackingNode (Node productNode)
		{
			productNode.AttributesChanged -= productNode_AttributesChanged;
			productNodes.Remove(productNode);

			if (GetLatestProductNode() == null)
			{
				RowToBeRemoved?.Invoke(this, this);
			}

			Invalidate();
		}

		public int RowIndex { get; }

		public PositioningProperties Positioning
		{
			set
			{
				positioning = value;
				Invalidate();
			}
		}
		
		protected override void OnMouseDown (MouseEventArgs e)
		{
			if (! isLiveGame)
			{
				return;
			}

			var latestFeatureNode = GetLatestProductNode();

			if (latestFeatureNode == null)
			{
				return;
			}

			if (e.Button == MouseButtons.Right)
			{
				ShowMenu(e.Location, latestFeatureNode);
			}
			else
			{
				var isDeployedPrototype = latestFeatureNode.GetBooleanAttribute("is_prototype", false) &&
				                          latestFeatureNode.GetAttribute(StageAttribute.DeploymentStage) ==
				                          StageStatus.Completed;

				if (isDeployedPrototype)
				{
					RedevelopFeature(latestFeatureNode);
				}
				else
				{
					OpenProgressionPanel(latestFeatureNode);
				}
			}

		}
		
		protected override void OnPaint(PaintEventArgs e)
		{
			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
			e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
			e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
			e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

			if (positioning == null)
			{
				return;
			}

			var latestProductNode = GetLatestProductNode();

			if (latestProductNode == null)
			{
				e.Graphics.FillRectangle(Brushes.HotPink, ClientRectangle);
				return;
			}

			var developmentStatus = GetColourStatusOfDevelopmentStatus(latestProductNode);

			var statusColours = !string.IsNullOrEmpty(developmentStatus) ? 
				developmentStatusToColours[developmentStatus] : 
				new StatusColours
				{
					CircleFillColour = Color.GhostWhite,
					ReticuleColour = CONVERT.ParseHtmlColor("#aeaea7")
				};

			var flashBackground = developmentCompletedFlashTimer.Enabled && elapsedTime / flashInterval % 2 == 0;
			var bounds = new RectangleF(0, 0, Width, Height);

			var backColour = flashBackground ? BackColor.Shade(0.2f) : BackColor;
			
			using (var backBrush = new SolidBrush(backColour))
			{
				e.Graphics.FillRectangle(backBrush, bounds);
				RenderBorder(e.Graphics, bounds);
			}
			
			var iconBounds = bounds.AlignRectangle(positioning.IconWidth, positioning.IconWidth, StringAlignment.Near, StringAlignment.Center, positioning.SidePadding);

			RenderIcon(this, e.Graphics, latestProductNode, iconBounds, statusColours);

			var metricLeft = iconBounds.Right + 10;

			var metricBounds = new RectangleFFromBounds
			{
				Left = metricLeft,
				Right = bounds.Right - positioning.SidePadding,
				Top = iconBounds.Top,
				Height = iconBounds.Height / 2
			}.ToRectangleF();
			
			// Metrics can be revealed following TestDelay (so any status after TestDelay) if the model office is active (r2+ Concord/Spitfire)
			// If the model office is not active, the metrics are only to be revealed when the status is Live
			// The logic to work this out then is if the status is greater than the target status
			// or equal to it if it's the last development status.

			// Default to true for situations where the modelOffice isn't in the nodetree
			// (e.g. G Agile is refactored to use this common piece of code but they're not using the model office malarkey in this way)
			var metricRevealStatus = (modelOfficeNode?.GetBooleanAttribute("is_active", true) ?? true) ? FeatureStatus.TestDelay : FeatureStatus.Live;
			const string barchartRevealStatus = FeatureStatus.TestDelay;

			var status = latestProductNode.GetAttribute("status");
			
			var currentStatusIndex = FeatureStatus.All.IndexOf(status);
			var metricTargetStatusIndex = FeatureStatus.All.IndexOf(metricRevealStatus);
			var barchartRevealStatusIndex = FeatureStatus.All.IndexOf(barchartRevealStatus);

			var showMetrics = currentStatusIndex > metricTargetStatusIndex || currentStatusIndex == metricTargetStatusIndex && metricTargetStatusIndex == FeatureStatus.IndexOfLastDevelopmentStatus;
			var showBarchart = currentStatusIndex > barchartRevealStatusIndex;

			RenderMetrics(e.Graphics, metricBounds, latestProductNode, showMetrics);

			var barchartAreaBounds = new RectangleF(metricBounds.X, metricBounds.Bottom, metricBounds.Width, metricBounds.Height);

			var barchartBounds = barchartAreaBounds.AlignRectangle(barchartAreaBounds.Width,
				barchartAreaBounds.Height * 0.75f, StringAlignment.Center, StringAlignment.Far);

			var effectivenessPercentage = latestProductNode.GetIntAttribute("effectiveness_percent", 100);
			
			if (!showBarchart)
			{
				effectivenessPercentage = 0;
			}
			
			RenderBarChart(e.Graphics, barchartBounds, Orientation.Horizontal, 0, 100,
				effectivenessPercentage,
				(f, t) =>
				{
					if (f <= 50)
					{
						return SkinningDefs.TheInstance.GetColorDataGivenDefault("effectiveness_bad_colour", CONVERT.ParseHtmlColor("#d74e14"));
					}
					else if (f <= 90)
					{
						return SkinningDefs.TheInstance.GetColorDataGivenDefault("effectiveness_ok_colour", CONVERT.ParseHtmlColor("#fccf12"));
					}
					else
					{
						return SkinningDefs.TheInstance.GetColorDataGivenDefault("effectiveness_good_colour", CONVERT.ParseHtmlColor("#62b34f"));
					}
				});
		}

		void RenderBorder (Graphics graphics, RectangleF bounds)
		{
			using (var borderPen = new Pen(borderColour, borderThickness))

			{
				foreach (var side in borderedSides)
				{
					float startX = 0f, startY = 0f, endX = 0f, endY = 0f;
					switch (side)
					{
						case RectangleSides.Left:
							startX = endX = bounds.X;
							startY = bounds.Y;
							endY = bounds.Bottom;
							break;
						case RectangleSides.Right:
							startX = endX = bounds.Right - borderThickness;
							startY = 0;
							endY = bounds.Bottom;
							break;
						case RectangleSides.Top:
							startX = bounds.X;
							endX = bounds.Right - borderThickness;
							startY = bounds.Top;
							endY = bounds.Top;

							break;
						case RectangleSides.Bottom:
							startX = bounds.X;
							endX = bounds.Right - borderThickness;
							startY = bounds.Bottom;
							endY = bounds.Bottom;
							break;
					}

					graphics.DrawLine(borderPen, startX, startY, endX, endY);
				}
			}
		}

		static void RenderIcon (Control control, Graphics graphics, Node featureNode, RectangleF iconBounds, StatusColours statusColours)
		{
			

			LozengeIconRenderer.RenderIconReticuleAndBackground(graphics, iconBounds, statusColours);

			if (featureNode == null)
			{
				return;
			}
			
			if (featureNode.GetAttribute("status") == FeatureStatus.TestDelay)
			{
				using (Brush myBrush = new SolidBrush(Color.FromArgb(166, 94, 100, 104)))
				{
					float delayRemaining = featureNode.GetIntAttribute("delayremaining", -2);
					float testDelay = featureNode.GetIntAttribute("test_time", 1);

					const int borderThickness = 5;
					const float offsetCorrection = 0.35f;
					graphics.FillPie(myBrush, iconBounds.X + borderThickness + offsetCorrection, iconBounds.Y + borderThickness + offsetCorrection, iconBounds.Width - 2 * borderThickness,
						iconBounds.Height - 2 * borderThickness, -90, 360 * (delayRemaining / testDelay));
				}
			}

			var installFailed = featureNode.GetAttribute(StageAttribute.DeploymentStage) == StageStatus.Failed;
			if (installFailed)
			{
				var size = iconBounds.Width / 10;
				using (var pen = new Pen(Color.Red, size))
				{
					graphics.DrawEllipse(pen, iconBounds.X - size, iconBounds.Y - size, iconBounds.Width + size * 2, iconBounds.Height + size * 2);
				}
			}

			var serviceId = featureNode.GetAttribute("service_id");
			var platform = featureNode.GetAttribute("platform");

			var isPrototype = featureNode.GetBooleanAttribute("is_prototype", false);
			var isLive = featureNode.GetAttribute(StageAttribute.DeploymentStage) == StageStatus.Completed;

			var infoSize = iconBounds.Width * 0.75f;

			var infoText = $"{serviceId}\r\n{(isPrototype && !isLive ? "" : platform)}";

			var fontSize = control.GetFontSizeInPixelsToFit(FontStyle.Regular, "W00\r\nW", new SizeF(infoSize, infoSize));

			using (var font = SkinningDefs.TheInstance.GetPixelSizedFont(fontSize))
			using (var textBrush = new SolidBrush(statusColours.ForeColour))
			{
				graphics.DrawString(infoText, font, textBrush, iconBounds.AlignRectangle(infoSize, infoSize),
					new StringFormat
					{
						LineAlignment = StringAlignment.Center,
						Alignment = StringAlignment.Center
					});
			}
		}

		void RenderMetrics (Graphics graphics, RectangleF bounds, Node featureNode, bool showMetrics)
		{
			var metrics = new List<string> { "metric_1" };
			var rowHeight = bounds.Height / metrics.Count;

			var valueReference = "000,000";

			var valueWidth = bounds.Width * 0.5f;
			var titleWidth = bounds.Width - valueWidth;

			var strings = metrics.Select(m => SkinningDefs.TheInstance.GetData($"{m}_short_name_display_case")).ToList();
			var widths = metrics.Select(m => new SizeF (titleWidth, rowHeight)).ToList();
			strings.Add(valueReference);
			widths.Add(new SizeF (valueWidth, rowHeight));

			using (var font = this.GetFontToFit(FontStyle.Regular, strings, widths))
			{
				var y = bounds.Top;

				foreach (var metric in metrics)
				{
					var rowBounds = new RectangleF (bounds.Left, y, bounds.Width, rowHeight);
					y += rowHeight;

					var titleBounds = new RectangleF (rowBounds.Left, rowBounds.Top, titleWidth, rowBounds.Height);
					var valueBounds = new RectangleF (titleBounds.Right, rowBounds.Top, rowBounds.Right - titleBounds.Right, rowBounds.Height);

					var title = SkinningDefs.TheInstance.GetData($"{metric}_short_name_display_case");

					var alignment = new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Far };

					if (!showMetrics)
					{
						using (var emptyBrush = new SolidBrush(CONVERT.ParseHtmlColor("#aeaea7")))
						{
							graphics.DrawString(title, font, emptyBrush, titleBounds, alignment);
							graphics.DrawString("--- ---", font, emptyBrush, valueBounds, alignment);
						}
					}
					else
					{
						var expectedValue = featureNode.GetIntAttribute($"{metric}_change_expected", 0);
						var actualValue = (int) Math.Ceiling(featureNode.GetDoubleAttribute($"{metric}_change_actual", 0));

						var metricSign = SkinningDefs.TheInstance.GetIntData($"{metric}_sign", 1);
						var comparison = (actualValue * metricSign).CompareTo(expectedValue * metricSign);

						// TODO get colours from skin file. Confirm what the colours should be. 
						var textColour = comparison < 0 ? CONVERT.ParseHtmlColor("#d74e14") : comparison > 0 ? CONVERT.ParseHtmlColor("#62b34f") : CONVERT.ParseHtmlColor("#2380e4");

						// TODO Colour will be based on if actual value matches expected
						using (var textBrush = new SolidBrush(textColour))
						{
							graphics.DrawString(title, font, textBrush, titleBounds, alignment);
							graphics.DrawString(CONVERT.ToPaddedStrWithThousands(actualValue, 0), font, textBrush, valueBounds, alignment);
						}
					}
				}
			}
		}

		static void RenderBarChart (Graphics graphics, RectangleF bounds, Orientation orientation, float minValue, float maxValue, float value,
		                            Func<float, float, Color> colourFunc, bool originateFromTarget = false,
		                            float? targetValue = null, bool showTargetLine = false)
		{
			var startValue = originateFromTarget ? targetValue ?? minValue : minValue;

			var lowerBounds = bounds.Left;
			var upperBounds = bounds.Right;

			switch (orientation)
			{
				case Orientation.Horizontal:
					lowerBounds = bounds.Left;
					upperBounds = bounds.Right;
					break;
				case Orientation.Vertical:
					lowerBounds = bounds.Bottom;
					upperBounds = bounds.Top;
					break;
			}

			PointF barStartValuePosition;
			PointF valuePosition;

			const float targetOffset = 3;
			const float targetMarkerThickness = 4;
			const float minBarThickness = 10;
			var barThickness = Math.Max(orientation == Orientation.Horizontal ? bounds.Height * 0.65f : bounds.Width * 0.7f, minBarThickness);

			var barBackPosition = new PointF();
			var barBackSize = new SizeF();

			var barFillPosition = new PointF();
			var barFillSize = new SizeF();

			switch (orientation)
			{
				case Orientation.Horizontal:
					barStartValuePosition = new PointF(Maths.MapBetweenRanges(startValue, minValue, maxValue, lowerBounds, upperBounds), bounds.Y + targetOffset);
					valuePosition = new PointF(Maths.MapBetweenRanges(value, minValue, maxValue, lowerBounds, upperBounds), bounds.Y + targetOffset);

					barBackPosition = new PointF(bounds.Left, bounds.Top + targetOffset);
					barBackSize = new SizeF(bounds.Width, barThickness);

					barFillPosition = new PointF(Math.Min(barStartValuePosition.X, valuePosition.X), barBackPosition.Y);
					barFillSize = new SizeF(Math.Abs(barStartValuePosition.X - valuePosition.X), barThickness);

					break;
				case Orientation.Vertical:
					barStartValuePosition = new PointF(bounds.X + targetOffset, Maths.MapBetweenRanges(startValue, minValue, maxValue, lowerBounds, upperBounds));
					valuePosition = new PointF(bounds.X + targetOffset, Maths.MapBetweenRanges(value, minValue, maxValue, lowerBounds, upperBounds));

					barBackPosition = new PointF(bounds.Left + targetOffset, bounds.Top);
					barBackSize = new SizeF(barThickness, bounds.Height);

					barFillPosition = new PointF(barBackPosition.X, Math.Min(barStartValuePosition.Y, valuePosition.Y));
					barFillSize = new SizeF(barThickness, Math.Abs(barStartValuePosition.Y - valuePosition.Y));

					break;
			}

			var targetMarkerLength = barThickness + 2 * targetOffset;

			using (var barFillBrush = new SolidBrush(colourFunc(value, targetValue ?? minValue)))
			{
				var oldMode = graphics.SmoothingMode;
				graphics.SmoothingMode = SmoothingMode.None;

				var barFillBounds = new RectangleF(barFillPosition, barFillSize);
				graphics.FillRectangle(barFillBrush, barFillBounds);

				if (showTargetLine && targetValue != null)
				{
					var targetPosition = new PointF();
					var targetSize = new SizeF();

					switch (orientation)
					{
						case Orientation.Horizontal:
							targetPosition =
								new PointF(
									Maths.MapBetweenRanges(targetValue.Value, minValue, maxValue, lowerBounds, upperBounds) -
									targetMarkerThickness / 2, barBackPosition.Y - targetOffset);
							targetSize = new SizeF(targetMarkerThickness, targetMarkerLength);
							break;
						case Orientation.Vertical:
							targetPosition = new PointF(barBackPosition.X - targetOffset,
								Maths.MapBetweenRanges(targetValue.Value, minValue, maxValue, lowerBounds, upperBounds) +
								targetMarkerThickness / 2);
							targetSize = new SizeF(targetMarkerLength, targetMarkerThickness);
							break;
					}

					graphics.FillRectangle(value >= targetValue ? barFillBrush : Brushes.Black,
						new RectangleF(targetPosition, targetSize));
				}

				using (var pen = new Pen(Color.White, 5))
				{
					graphics.DrawRectangle(pen, new Rectangle((int) barBackPosition.X, (int) barBackPosition.Y, (int) barBackSize.Width, (int) barBackSize.Height));
				}

				graphics.DrawRectangle(Pens.Black, new Rectangle((int) barBackPosition.X, (int) barBackPosition.Y, (int) barBackSize.Width, (int) barBackSize.Height));

				graphics.SmoothingMode = oldMode;
			}
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			Invalidate();
		}

		Node GetLatestProductNode ()
		{
			return productNodes.Any() ? productNodes.Aggregate((p1, p2) => p1.GetIntAttribute("time_started", 0) > p2.GetIntAttribute("time_started", 0) ? p1 : p2) : null;
		}

		readonly Node modelOfficeNode;
		readonly string serviceName;
		readonly Node beginServicesNode;

		readonly Dictionary<string, StatusColours> developmentStatusToColours;

		readonly List<Node> productNodes;

		readonly bool isLiveGame;
		readonly DevelopingAppTerminator appTerminator;


		PositioningProperties positioning;

		readonly Timer developmentCompletedFlashTimer;
		readonly int flashInterval;
		int elapsedTime;

		PopupMenu menu;

		void ShowMenu(Point location, Node developmentNode)
		{
			CloseMenu();

			menu = new PopupMenu
			{
				BackColor = SkinningDefs.TheInstance.GetColorData("lozenge_menu_colour", Color.ForestGreen)
			};

			menu.AddHeading(serviceName, null);
			menu.AddDivider(8, false);

			var isDevelopmentActive = developmentNode != null;
			var isFeatureLive = developmentNode?.GetAttribute("deployment_stage_status") == StageStatus.Completed;
			var isPrototype = developmentNode?.GetBooleanAttribute("is_prototype", false) ?? false;

			PopupMenu.AddMenuItem(menu, "Open", open_Chosen, true);
			PopupMenu.AddMenuItem(menu, "Undo", undo_Chosen, isPrototype || (isDevelopmentActive && ! isFeatureLive));
			PopupMenu.AddMenuItem(menu, "Abort", abort_Chosen, isDevelopmentActive && !isFeatureLive);
			PopupMenu.AddMenuItem(menu, "Redevelop", redevelop_Chosen, isDevelopmentActive && isFeatureLive);
			if (isDevelopmentActive)
			{
				PopupMenu.AddMenuItem(menu, "Retire", retire_Chosen, isFeatureLive);
			}

			menu.AddDivider(8, true);

			PopupMenu.AddMenuItem(menu, "Close Menu", close_Chosen, true);

			menu.FormClosed += menu_Closed;

			menu.Show(TopLevelControl, this, PointToScreen(location), true);
		}

		void close_Chosen(object sender, EventArgs args)
		{
			CloseMenu();
		}

		void CloseMenu()
		{
			menu?.Close();
		}

		void menu_Closed(object sender, EventArgs args)
		{
			menu?.Dispose();
			menu = null;
		}

		void open_Chosen(object sender, EventArgs args)
		{
			OpenProgressionPanel(GetLatestProductNode());
			CloseMenu();
		}

		void undo_Chosen(object sender, EventArgs args)
		{
			var serviceDisplayName = SkinningDefs.TheInstance.GetData("service_display_name");
			var message = $"Are you sure you want to undo this {serviceDisplayName}?\r\nThis should only be used if the {serviceDisplayName} was started accidentally.";
			var caption = $"Undo {Strings.SentenceCase(serviceDisplayName)}";

			if (ShowConfirmationPopup(caption, message))
			{
				appTerminator.TerminateApp(GetLatestProductNode(), CommandTypes.UndoService);
			}
		}

		void abort_Chosen(object sender, EventArgs args)
		{
			var serviceDisplayName = SkinningDefs.TheInstance.GetData("service_display_name");
			var message = $"Are you sure you want to cancel this {serviceDisplayName}?";
			var caption = $"Cancel {Strings.SentenceCase(serviceDisplayName)}";

			if (ShowConfirmationPopup(caption, message))
			{
				appTerminator.TerminateApp(GetLatestProductNode(), CommandTypes.CancelService);
			}
		}

		void RedevelopFeature(Node featureNode)
		{
			if (featureNode == null)
			{
				return;
			}

			appTerminator.TerminateApp(featureNode, CommandTypes.RedevelopService);
			OnFeatureRedeveloped(featureNode);

		}

		void OnFeatureRedeveloped(Node featureNode)
		{
			CloseMenu();

			FeatureRedeveloped?.Invoke(this, featureNode.GetAttribute("service_id"));
		}

		void OpenProgressionPanel(Node featureNode)
		{
			if (featureNode == null)
			{
				return;
			}

			ProgressionPanelOpened?.Invoke(this, featureNode);
		}

		void redevelop_Chosen(object sender, EventArgs args)
		{
			RedevelopFeature(GetLatestProductNode());
		}

		void retire_Chosen(object sender, EventArgs args)
		{
			var serviceDisplayName = SkinningDefs.TheInstance.GetData("service_display_name");
			var message = $"Are you sure you want to retire this {serviceDisplayName}?";
			var caption = $"Retire {Strings.SentenceCase(serviceDisplayName)}";

			if (ShowConfirmationPopup(caption, message))
			{
				appTerminator.TerminateApp(GetLatestProductNode(), CommandTypes.CancelService);
			}
		}

		bool ShowConfirmationPopup(string caption, string message)
		{
			const MessageBoxButtons buttons = MessageBoxButtons.YesNo;

			return MessageBox.Show(this, message, caption, buttons) == DialogResult.Yes;
		}

		static string GetColourStatusOfDevelopmentStatus(Node developmentNode)
		{
			var status = developmentNode?.GetAttribute("status");

			if (string.IsNullOrEmpty(status))
			{
				return null;
			}

			var isPrototype = developmentNode.GetBooleanAttribute("is_prototype", false);
			switch (status)
			{
				case FeatureStatus.Dev:
					return DevelopmentStatus.Dev;

				case FeatureStatus.Test:
				case FeatureStatus.TestDelay:
					return DevelopmentStatus.Test;

				case FeatureStatus.Release:
				case "finishedRelease":
					return DevelopmentStatus.Release;

				case FeatureStatus.Live:
				case FeatureStatus.Redevelop:
					return isPrototype ? DevelopmentStatus.LivePrototype : DevelopmentStatus.Live;

			}

			return null;
		}
	}

}
