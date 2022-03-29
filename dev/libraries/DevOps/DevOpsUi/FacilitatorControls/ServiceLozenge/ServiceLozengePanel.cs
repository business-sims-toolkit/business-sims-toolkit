using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Algorithms;
using Charts;
using CoreUtils;
using DevOpsEngine;
using DevOpsEngine.RequestsManagers;
using DevOpsUi.FacilitatorControls.FeatureDevelopment.Agile;
using DiscreteSimGUI;
using LibCore;
using Network;
using ResizingUi;
using ResizingUi.Animation;
using ResizingUi.Button;
using ResizingUi.Enums;
using ResizingUi.Interfaces;
using HatchFillProperties = ResizingUi.HatchFillProperties;
using SizeF = System.Drawing.SizeF;

// ReSharper disable ObjectCreationAsStatement

namespace DevOpsUi.FacilitatorControls.ServiceLozenge
{
	public class ServiceLozengePanel : CascadedBackgroundPanel
	{
		struct IncidentStatus
		{
			public const string Up = "up";
			public const string Down = "down";
			public const string DownBreached = "down_breached";
			public const string DownFlash = "down_flash";
			public const string DownBreachedFlash = "down_breached_flash";

			public static IEnumerable<string> AllStatuses { get; } = new List<string>
			{
				Up, Down, DownBreached, DownFlash, DownBreachedFlash
			};
		}

		readonly bool includeCsatReadout;
		readonly List<RectangleSides> borderedSides;
		readonly Color borderColour;
		public ServiceLozengePanel (int columnIndex, string serviceName, NodeTree model, IDialogOpener dialogOpener,
		                            DevelopingAppTerminator appTerminator, AgileRequestsManager requestsManager,
		                            bool includeHeatmap, bool includeCsatReadout, AnimatorProvider animatorProvider, IEnumerable<RectangleSides> borderedSides)
		{
			this.borderedSides = new List<RectangleSides>(borderedSides);
			borderColour = SkinningDefs.TheInstance.GetColorData("lozenge_border_colour", CONVERT.ParseHtmlColor("#00202d"));


			UseCascadedBackground = true;

			this.includeCsatReadout = includeCsatReadout;
			this.appTerminator = appTerminator;

			isLiveGame = requestsManager != null && appTerminator != null && dialogOpener != null;

			this.dialogOpener = dialogOpener;
			this.requestsManager = requestsManager;

			ColumnIndex = columnIndex - 1;

			this.model = model;
			this.serviceName = serviceName;

			titleBarHeight = 30;

			incidentStatusToProperties = IncidentStatus.AllStatuses.ToDictionary(s => s, s =>
			{
				var alternateHatchColour = SkinningDefs.TheInstance.GetColourData($"service_icon_{s}_hatch_colour");

				return new StatusColours
				{
					ReticuleColour = Color.Black,
					CircleFillColour =
						SkinningDefs.TheInstance.GetColorData($"service_icon_{s}_back_colour", Color.HotPink),
					ForeColour = Color.White,
					HatchFillProperties = alternateHatchColour != null
						? new HatchFillProperties
						{
							Angle = SkinningDefs.TheInstance.GetIntData($"service_icon_hatch_angle", 0),
							LineColour =
								SkinningDefs.TheInstance.GetColorData($"service_icon_{s}_back_colour", Color.HotPink),
							AltLineColour = alternateHatchColour.Value,
							LineWidth = SkinningDefs.TheInstance.GetIntData($"service_icon_line_width", 20),
							AltLineWidth = SkinningDefs.TheInstance.GetIntData($"service_icon_alt_line_width", 20),
						}
						: null
				};
			});

			var service = model.GetNamedNode(serviceName);

			if (service != null)
			{
				TrackServiceNode(service);
			}

			if (isLiveGame)
			{
				upgradesButton = new StyledDynamicButton("standard", "Upgrades", true)
				{
					Width = 80,
					Height = 25,
					Font = SkinningDefs.TheInstance.GetFont(7),
					CornerRadius = 12.5f
				};
				Controls.Add(upgradesButton);
				upgradesButton.Click += upgradesButton_Click;
				upgradesButton.BringToFront();
			}


			beginServicesNode = model.GetNamedNode("BeginNewServicesInstall");
			beginServicesNode.ChildAdded += beginServicesNode_ChildAdded;

			featureRows = new List<LozengeFeatureRow>();

			

			var featureNodesForService = beginServicesNode.GetChildrenAsList()
				.Where(bs => bs.GetAttribute("biz_service_function") == serviceName).ToList();

			var featureIdToFeatureNodes = featureNodesForService
				.GroupBy(bs => bs.GetAttribute("service_id"))
				.ToDictionary(g => g.Key, g => g.ToList());

			// Order the feature IDs by the earliest started
			var orderedFeatureIds = featureIdToFeatureNodes.Select(kvp =>
				new
				{
					Id = kvp.Key,
					StartTime = kvp.Value.Min(n => n.GetIntAttribute("time_started", 0))
				}).OrderBy(f => f.StartTime).Select(f => f.Id).ToList();

			if (includeHeatmap)
			{
				heatMap = new CustomerComplaintHeatMap.Lozenge(model.GetNamedNode(serviceName),
					AgileComplaints.CustomerComplaintTypes, AgileComplaints.CustomerTypes, animatorProvider)
				{
					ShowHeader = false
				};
				Controls.Add(heatMap);
				heatMap.BringToFront();
				heatMap.MouseEnter += heatMap_MouseEnter;
				heatMap.MouseDown += heatMap_MouseDown;
				heatMap.MouseLeave += heatMap_MouseLeave;
			}

			if (includeCsatReadout)
			{
				csatReadout = new CsatReadoutPanel(serviceNode);
				Controls.Add(csatReadout);
				csatReadout.BringToFront();
			}
			
			foreach (var featureId in orderedFeatureIds)
			{
				// But then pass in the latest started node to be displayed in the row
				var featureNode = featureIdToFeatureNodes[featureId].Aggregate((a, b) =>
					a.GetIntAttribute("time_started", 0) > b.GetIntAttribute("time_started", 0) ? a : b);

				AddFeatureRow(featureNode);
				
			}

			

			flashInterval = 500;
			flashDuration = 10000;

			flashTimer = new Timer
			{
				Interval = flashInterval
			};
			flashTimer.Tick += flashTimer_Tick;

			tooltipTimer = new Timer
			{
				Interval = 500
			};
			tooltipTimer.Tick += tooltipTimer_Tick;

		}

		void upgradesButton_Click(object sender, EventArgs e)
		{
			OpenUpgradesDialog();
		}

		Rectangle CalculateDialogBounds ()
		{
			var y = (int)featureRowsBounds.Top;
			var height = (int)featureRowsBounds.Height;
			var width = (int)featureRowsBounds.Width - 1;

			return RectangleToScreen(new Rectangle(0, y, width, height));
		}

		void row_ProgressionPanelOpened(object sender, Node e)
		{
			dialogOpener.OpenDialog(new FeatureProgressionPanel(model, e, requestsManager, dialogOpener), CalculateDialogBounds);
		}

		void row_FeatureRedeveloped(object sender, string e)
		{
			OpenUpgradesDialog(e);
		}

		void OpenUpgradesDialog (string preselectedFeatureId = null)
		{
			dialogOpener.OpenDialog(new UpgradesPanel(requestsManager, serviceName, dialogOpener, preselectedFeatureId), CalculateDialogBounds);
		}

		public int ColumnIndex { get; }

		protected override void Dispose (bool disposing)
		{
			if (disposing)
			{
				if (serviceNode != null)
				{
					serviceNode.AttributesChanged -= serviceNode_AttributesChanged;
				}

				beginServicesNode.ChildAdded -= beginServicesNode_ChildAdded;

			}

			base.Dispose(disposing);
		}

		RectangleF serviceInfoBounds;
		
		RectangleF featureRowsBounds;

		protected override void OnSizeChanged (EventArgs e)
		{
			DoSize();
		}

		void DoSize ()
		{
			var heatMapIncluded = heatMap != null;

			var isUpgradeButtonIncluded = upgradesButton != null;
			
			serviceInfoBounds = new RectangleF(0, 0, Width, Height * 0.23f);

			var heatMapHeight = Height * 0.17f;

			if (heatMapIncluded)
			{
				heatMap.Bounds = new RectangleF(sidePadding, serviceInfoBounds.Bottom, Width - 2 * sidePadding, heatMapHeight).ToRectangle();
			}

			if (includeCsatReadout)
			{
				csatReadout.Bounds = new Rectangle(sidePadding, (int)(serviceInfoBounds.Bottom), Width - 2 * sidePadding, (int)heatMapHeight);
			}

			var upgradeAreaHeight = (int)(Height * 0.05f);
			
			var upgradeButtonArea = new RectangleF(0, heatMap?.Bottom ?? csatReadout?.Bottom ?? serviceInfoBounds.Bottom, Width, upgradeAreaHeight);
			
			if (isUpgradeButtonIncluded)
			{
				upgradesButton.Bounds = upgradeButtonArea.ToRectangle().AlignRectangle(upgradesButton.Size);
			}

			var rowY = isUpgradeButtonIncluded ? upgradeButtonArea.Bottom : csatReadout?.Bounds.Bottom ?? heatMap?.Bounds.Bottom ?? serviceInfoBounds.Bottom;
			

			var totalRowsHeight = Height - rowY;

			var maxFeatureRows = SkinningDefs.TheInstance.GetIntData("max_number_of_features_per_service", 6);

			var rowHeight = totalRowsHeight / maxFeatureRows;


			featureRowsBounds = new RectangleF(0, rowY, Width, totalRowsHeight);

			foreach (var row in featureRows)
			{
				var newLocation = new Point(0, (int) rowY);

				row.Size = new Size(Width, (int) rowHeight);

				if (isLiveGame)
				{
					row.AnimateTo(newLocation);
				}
				else
				{
					row.Location = newLocation;
				}

				rowY += rowHeight;
			}

			Invalidate();
		}

		protected override void OnPaint (PaintEventArgs e)
		{
			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
			e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
			e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
			e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

			var isUp = (lozenge?.VisualState ?? VisualState.UpAndGreen) == VisualState.UpAndGreen;
			var backColour = isUp ? BackColor : ColorTranslator.FromHtml("#0xffececec");
			
			var useFlashColour = flashCount > 0 && flashCount % 2 == 0;
			
			
			using (var backBrush = new SolidBrush(backColour))
			{
				e.Graphics.FillRectangle(backBrush, new RectangleFFromBounds
				{
					Left = 0,
					Right = Width,
					Top = 0,
					Bottom = featureRowsBounds.Top
				}.ToRectangleF());
			}

			var colourStatus = GetColourStatusOfVisualState(useFlashColour);

			if (colourStatus == null)
			{
				return;
			}

			var positioning = RenderServiceInfoSection(e.Graphics, serviceInfoBounds, colourStatus.Value);

			using (var borderPen = new Pen(borderColour, borderThickness))
			{
				foreach (var side in borderedSides)
				{
					float startX = 0, startY = 0, endX = 0, endY = 0;
					switch (side)
					{
						case RectangleSides.Left:
							startX = endX = 0;
							startY = 0;
							endY = featureRowsBounds.Top;
							break;
						case RectangleSides.Right:
							startX = endX = Width - borderThickness;
							startY = 0;
							endY = featureRowsBounds.Top;
							break;
						default:
							break;
					}

					e.Graphics.DrawLine(borderPen, new PointF(startX, startY), new PointF(endX, endY));
				}
				
				e.Graphics.DrawLine(borderPen, new PointF(0, featureRowsBounds.Top), new PointF(Right, featureRowsBounds.Top));
			}
			
			foreach (var row in featureRows)
			{
				row.Positioning = positioning;
			}
		}

		const int borderThickness = 1;

		void RenderTitleSection (Graphics graphics, RectangleF bounds)
		{
			var level1Stage = serviceNode.Tree.GetNamedNode("Stages").GetChildWithAttributeValue("desc", serviceName).GetAttribute("level_1_stage");
			var name = $" {level1Stage}.{serviceName}";

			var titleBackColour = SkinningDefs.TheInstance.GetColorData("lozenge_title_back_colour", CONVERT.ParseHtmlColor("#F39325"));

			using (var titleBackBrush = new SolidBrush(titleBackColour))
			using (var borderPen = new Pen(borderColour, borderThickness))
			{
				graphics.FillRectangle(titleBackBrush, bounds);

				graphics.DrawLine(borderPen, bounds.Location, new PointF(bounds.Right, bounds.Y));
				var bottomBorderY = bounds.Bottom - borderThickness * 0.5f;
				graphics.DrawLine(borderPen, new PointF(bounds.X, bottomBorderY), new PointF(bounds.Right, bottomBorderY));
			}

			var titleTextBounds = bounds.AlignRectangle(bounds.Width, bounds.Height * 0.95f, StringAlignment.Near);

			using (var font = this.GetFontToFit(FontStyle.Bold, "0.SHOPPING CARTXXX", titleTextBounds.Size))
			{
				graphics.DrawString(name.ToUpper(), font, Brushes.White, titleTextBounds, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
			}
		}

		int titleBarHeight;

		public int TitleBarHeight
		{
			get => titleBarHeight;

			set
			{
				titleBarHeight = value;
				Invalidate();
			}
		}

		PositioningProperties RenderServiceInfoSection (Graphics graphics, RectangleF bounds, StatusColours colourStatus)
		{
			var titleBarBounds = bounds.AlignRectangle(bounds.Width, titleBarHeight, StringAlignment.Center, StringAlignment.Near);

			RenderTitleSection(graphics, titleBarBounds);

			const int innerPadding = 10;

			var availableWidth = bounds.Width - 2 * sidePadding;
			var iconSize = Math.Min(availableWidth * 0.35f, bounds.Height * 0.47f);

			var iconsRowBounds = new RectangleF(sidePadding, titleBarBounds.Bottom + innerPadding, availableWidth, iconSize);
			

			var iconBounds = iconsRowBounds.AlignRectangle(iconSize, iconSize, StringAlignment.Near);

			var csatBounds = iconsRowBounds.AlignRectangle(iconSize, iconSize, StringAlignment.Far); 

			var storesImpactedBounds = new RectangleFFromBounds
			{
				Left = iconBounds.Right,
				Right = csatBounds.Left,
				Top = iconBounds.Top,
				Bottom = iconBounds.Bottom
			}.ToRectangleF();

			RenderServiceIcon(graphics, iconBounds, colourStatus);

			RenderStoresImpacted(graphics, storesImpactedBounds);

			RenderCsat(graphics, csatBounds);

			var metricBounds = new RectangleFFromBounds
			{
				Left = iconsRowBounds.Left,
				Top = iconsRowBounds.Bottom + Maths.Clamp(bounds.Height * 0.05f, 5, 10),
				Bottom = bounds.Bottom,
				Right = iconsRowBounds.Right
			}.ToRectangleF();

			RenderMetrics(graphics, metricBounds);

			var positioning = new PositioningProperties
			{
				IconWidth = iconSize * 0.8f,
				SidePadding = sidePadding
			};

			return positioning;
		}

		const int sidePadding = 15;

		void RenderServiceIcon(Graphics graphics, RectangleF bounds, StatusColours statusColours)
		{
			LozengeIconRenderer.RenderIconReticuleAndBackground(graphics, bounds, statusColours);
			
			var iconName = serviceNode.GetAttribute("icon");

			var icon = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + $@"\images\icons\{iconName}.png");
			
			graphics.DrawImage(icon, bounds);
			
			var workAroundRemaining = serviceNode.GetIntAttribute("workingAround", 0);

			if (workAroundRemaining > 0)
			{
				var totalWorkAroundTime = serviceNode.GetIntAttribute("workaround_time", 120);
				var progressPercent = workAroundRemaining / (float)totalWorkAroundTime;

				const float circleThickness = 6f;

				using (var workAroundPen = new Pen(SkinningDefs.TheInstance.GetColorData("lozenge_incident_workaround_colour", Color.DeepSkyBlue), circleThickness))
				{
					graphics.DrawArc(workAroundPen, bounds, -90, progressPercent * 360);
				}
			}
		}

		void RenderMetrics (Graphics graphics, RectangleF bounds)
		{
			var round = model.GetNamedNode("CurrentRound").GetIntAttribute("round", 0);
			var metricTargets = model.GetNamedNode($"MetricTargets_Round{round}").GetChildrenAsList().Single(n => n.GetAttribute("business_service") == serviceName);

			var metrics = new [] { "metric_1" };
			var leading = 5;
			var gap = 20;
			var rowHeight = (bounds.Height - ((metrics.Length - 1) * leading)) / metrics.Length;
			var y = bounds.Top;
			foreach (var metric in metrics)
			{
				var metricDisplayName = SkinningDefs.TheInstance.GetData($"{metric}_short_name_display_case");

				var target = metricTargets?.GetIntAttribute($"{metric}_target", 0) ?? 0;
				var targetString = "TARGET " + CONVERT.ToPaddedStrWithThousands(target, 0);

				var metricNode = serviceNode.GetFirstChildOfType("metrics").GetChildWithAttributeValue("metric_name", metric);
				var value = metricNode.GetIntAttribute("value", 0);
				var valueString = metricDisplayName + " " + CONVERT.ToPaddedStrWithThousands(value, 0);

				var rowBounds = new RectangleF (bounds.Left, y, bounds.Width, rowHeight);
				var targetBounds = new RectangleF (rowBounds.Left, rowBounds.Top, (rowBounds.Width / 2) - (gap / 2), rowBounds.Height);
				var valueBounds = new RectangleFFromBounds
				{
					Left = rowBounds.Left + (rowBounds.Width / 2) + (gap / 2),
					Top = rowBounds.Top,
					Right = rowBounds.Right,
					Bottom = rowBounds.Bottom
				}.ToRectangleF();

				var strings = new List<string> { targetString, valueString };
				var boundses = new List<RectangleF> { targetBounds, valueBounds };
				var sizes = new List<SizeF> { targetBounds.Size, valueBounds.Size };

				using (var font = FontScalerExtensions.GetFontToFit(this, FontStyle.Bold, strings, sizes))
				{
					for (var i = 0; i < strings.Count; i++)
					{
						bool isFirst = (i == 0);
						bool isLast = (i == (strings.Count - 1));
						graphics.DrawString(strings[i], font, Brushes.Black, boundses[i],
							new StringFormat
							{
								LineAlignment = StringAlignment.Center,
								Alignment = (isFirst ? StringAlignment.Near : (isLast ? StringAlignment.Far : StringAlignment.Center))
							});
					}}

					y += rowHeight + leading;
			}
		}

		void RenderStoresImpacted(Graphics graphics, RectangleF bounds)
		{
			var storesAffected = serviceNode.GetAttribute("users_down");

			if (string.IsNullOrEmpty(storesAffected))
			{
				return;
			}

			var stores = storesAffected.Split(',').ToList();

			var textHeight = bounds.Height / 2;
			var textSize = new SizeF(bounds.Width, textHeight);

			const string referenceText = "X,X,";

			var fontSize = this.GetFontSizeInPixelsToFit(FontStyle.Bold, referenceText, textSize);

			using (var font = SkinningDefs.TheInstance.GetPixelSizedFont(fontSize, FontStyle.Bold))
			{
				var verticalAlignment = stores.Count > 2 ? StringAlignment.Near : StringAlignment.Center;

				while (stores.Any())
				{
					var subStores = stores.Take(2);
					stores = stores.Skip(2).ToList();

					var line = new StringBuilder();
					line.Append(string.Join(",", subStores));
					if (stores.Any())
					{
						line.Append(",");
					}

					graphics.DrawString(line.ToString(), font, Brushes.Black, bounds.AlignRectangle(bounds.Width, textHeight, StringAlignment.Center, verticalAlignment), new StringFormat
					{
						LineAlignment = StringAlignment.Center,
						Alignment = StringAlignment.Center
					});

					verticalAlignment = StringAlignment.Far;
				}
			}
		}

		void RenderCsat(Graphics graphics, RectangleF bounds)
		{
			var imageSize = Math.Min(bounds.Width * 0.7f, bounds.Height * 0.7f);

			var csatPercent = serviceNode?.GetIntAttribute("csat_percent", 100) ?? 80;

			var csatWeightingsNode = serviceNode?.Tree?.GetNamedNode("CustomerSatisfactionWeightings");
			var negativeToNeutralThreshold = csatWeightingsNode?.GetIntAttribute("negative_neutral_threshold") ?? 75;
			var neutralToPositiveThreshold = csatWeightingsNode?.GetIntAttribute("neutral_positive_threshold") ?? 90;

			var csatLevel = "negative";

			if (csatPercent > neutralToPositiveThreshold)
			{
				csatLevel = "positive";
			}
			else if (csatPercent > negativeToNeutralThreshold)
			{
				csatLevel = "neutral";
			}
			var csatColour = SkinningDefs.TheInstance.GetColorData($"csat_{csatLevel}_colour", Color.HotPink);

			LozengeIconRenderer.RenderIconReticuleAndBackground(graphics, bounds, new StatusColours
			{
				ReticuleColour = Color.Black,
				CircleFillColour = Color.Transparent
			});

			using (var font = this.GetFontToFit(FontStyle.Regular, "100%", bounds.Size))
			using (var brush = new SolidBrush(csatColour))
			{
				graphics.DrawString(CONVERT.ToPaddedPercentageString(csatPercent, 0), font, brush, bounds, new StringFormat
				{
					Alignment = StringAlignment.Center,
					LineAlignment = StringAlignment.Center,
					FormatFlags = StringFormatFlags.NoClip | StringFormatFlags.NoWrap
				});
			}
		}

		StatusColours? GetColourStatusOfVisualState (bool useFlashColour)
		{
			if (lozenge == null)
			{
				return null;
			}

			if (lozenge.VisualState == VisualState.UpAndGreen)
			{
				return incidentStatusToProperties[IncidentStatus.Up];
			}

			var status = lozenge.IsSlaBreached ? 
				useFlashColour ? IncidentStatus.DownBreachedFlash : IncidentStatus.DownBreached :
				useFlashColour ? IncidentStatus.DownFlash : IncidentStatus.Down;

			return incidentStatusToProperties[status];
		}

		void heatMap_MouseDown (object sender, MouseEventArgs e)
		{
			MouseClicked(e);
		}

		void heatMap_MouseEnter (object sender, EventArgs e)
		{
			MouseEntered();
		}

		void heatMap_MouseLeave(object sender, EventArgs e)
		{
			MouseLeft();
		}

		void MouseEntered ()
		{
			if (serviceNode == null)
			{
				return;
			}

			mouseEntered = true;
		}

		void MouseMoved ()
		{
			if (mouseEntered && menu == null)
			{
				mouseEntered = false;
			}
		}

		void MouseLeft ()
		{
			CloseTooltip();
			mouseEntered = false;
		}

		void MouseClicked (MouseEventArgs e)
		{
			if (!isLiveGame)
			{
				return;
			}

			if (e.Button == MouseButtons.Right)
			{
				ShowMenu(e.Location);
			}

		}

		bool mouseEntered;
		protected override void OnMouseEnter(EventArgs e)
		{
			MouseEntered();
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			MouseMoved();
		}

		protected override void OnMouseLeave(EventArgs e)
		{
			MouseLeft();
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			MouseClicked(e);
		}

		void TrackServiceNode (Node nodeToTrack)
		{
			Debug.Assert(serviceNode == null, "\"There can be only one\" biz service with this name");

			if (nodeToTrack == null)
			{
				return;
			}

			serviceNode = nodeToTrack;

			serviceNode.AttributesChanged += serviceNode_AttributesChanged;

			lozenge?.Dispose();

			lozenge = new QuadStatusLozenge(serviceNode, new Random(0), false);

			lozenge.VisualStateChanged += lozenge_VisualStateChanged;
		}

		void lozenge_VisualStateChanged(object sender, EventArgs e)
		{
			Invalidate();
		}

		void serviceNode_AttributesChanged(Node sender, System.Collections.ArrayList attrs)
		{
			var attrsList = attrs.Cast<AttributeValuePair>().ToList();

			foreach (var avp in attrsList)
			{
				switch (avp.Attribute)
				{
					case "up":

						if (!CONVERT.ParseBool(avp.Value, true))
						{
							ResetFlashTimer();

						}
						break;
					case "danger_level":
						var currentDangerLevel = CONVERT.ParseIntSafe(avp.Value, 0);
						if (currentDangerLevel == 100 && dangerLevel != 100)
						{
							ResetFlashTimer();
						}

						dangerLevel = currentDangerLevel;

						break;
				}
			}

			Invalidate();
		}

		void beginServicesNode_ChildAdded (Node sender, Node child)
		{
			var bizServiceName = child.GetAttribute("biz_service_function");

			if (bizServiceName != serviceName)
			{
				return;
			}

			var featureId = child.GetAttribute("service_id");

			if (featureRows.Any(r => r.FeatureId == featureId))
			{
				return;
			}

			AddFeatureRow(child);
		}
		
		void AddFeatureRow (Node featureNode)
		{
			var rowIndex = featureRows.Count;

			var featureBorderedSides = new List<RectangleSides>(borderedSides);

			if (! featureRows.Any())
			{
				featureBorderedSides.Add(RectangleSides.Top);
			}

			featureBorderedSides.Add(RectangleSides.Bottom);

			var row = new LozengeFeatureRow(model, rowIndex, beginServicesNode, featureNode, serviceName, isLiveGame, appTerminator, featureBorderedSides)
			{
				Location = new Point((int)featureRowsBounds.X, (int)featureRowsBounds.Y - 60)
			};
			featureRows.Add(row);
			row.SendToBack();

			row.FeatureRedeveloped += row_FeatureRedeveloped;
			row.ProgressionPanelOpened += row_ProgressionPanelOpened;
			row.RowToBeRemoved += featureRow_RowToBeRemoved;

			Controls.Add(row);

			DoSize();
		}

		void featureRow_RowToBeRemoved(object sender, LozengeFeatureRow e)
		{
			var row = e;
			featureRows.Remove(row);
			row.Dispose();

			DoSize();
		}

		readonly IDialogOpener dialogOpener;
		readonly DevelopingAppTerminator appTerminator;
		readonly AgileRequestsManager requestsManager;

		readonly bool isLiveGame;
		readonly NodeTree model;
		readonly string serviceName;
		Node serviceNode;

		QuadStatusLozenge lozenge;

		readonly CustomerComplaintHeatMap.Lozenge heatMap;
		readonly CsatReadoutPanel csatReadout;
		readonly List<LozengeFeatureRow> featureRows;

		readonly Node beginServicesNode;
		//readonly Node businessServicesGroup;

		readonly Dictionary<string, StatusColours> incidentStatusToProperties;

		readonly StyledDynamicButton upgradesButton;

		readonly Timer flashTimer;
		readonly int flashInterval;
		readonly int flashDuration;

		int flashCount;

		int dangerLevel;
		PopupMenu menu;
		PopupMenu tooltip;

		readonly Timer tooltipTimer;

		static readonly List<string> csatLevels = new List<string>
		{
			"positive",
			"neutral",
			"negative"
		};

		static readonly Dictionary<string, Image> csatLevelToImage = csatLevels.ToDictionary(l => l,
			l => (Image)Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + $@"\images\csat\csat_{l}.png"));

		void ShowMenu(Point location)
		{
			CloseMenu();

			menu = new PopupMenu
			{
				BackColor = SkinningDefs.TheInstance.GetColorData("lozenge_menu_colour", Color.ForestGreen)
			};

			menu.AddHeading(serviceName, null);
			menu.AddDivider(8, false);

			var incidentActive = lozenge?.VisualState != VisualState.UpAndGreen;

			PopupMenu.AddMenuItem(menu, "Fix", fix_Chosen, incidentActive, @"lozenges\server_edit.png");
			PopupMenu.AddMenuItem(menu, "Fix By Consultancy", fixByConsultancy_Chosen, incidentActive, @"lozenges\server_lightning.png");
			PopupMenu.AddMenuItem(menu, "Workaround", workaround_Chosen, incidentActive, @"lozenges\arrow_rotate_clockwise.png");
			PopupMenu.AddMenuItem(menu, "First Line Fix", firstLineFix_Chosen, incidentActive, @"lozenges\1stline_fix.png");

			menu.AddDivider(8, true);
			
			PopupMenu.AddMenuItem(menu, "Close Menu", close_Chosen, true);

			menu.FormClosed += menu_Closed;

			menu.Show(TopLevelControl, this, PointToScreen(location), true);
		}
		
		void Fix(string type)
		{
			new Node(model.GetNamedNode("FixItQueue"), type, "", new AttributeValuePair("incident_id", serviceNode.GetAttribute("incident_id")));
			CloseMenu();
		}

		void fix_Chosen(object sender, EventArgs args)
		{
			Fix("fix");
		}

		void fixByConsultancy_Chosen(object sender, EventArgs args)
		{
			Fix("fix by consultancy");
		}

		void workaround_Chosen(object sender, EventArgs args)
		{
			Fix("workaround");
		}

		void firstLineFix_Chosen(object sender, EventArgs args)
		{
			Fix("first_line_fix");
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


		void ResetFlashTimer()
		{
			flashCount = flashDuration / flashInterval;
			flashTimer.Start();
		}

		void flashTimer_Tick(object sender, EventArgs e)
		{
			if (--flashCount <= 0)
			{
				flashTimer.Stop();
			}

			Invalidate();
		}

		void tooltipTimer_Tick(object sender, EventArgs e)
		{
			tooltipTimer.Stop();

			ShowTooltip();
		}

		void ShowTooltip()
		{
			tooltip?.Close();

			tooltip = new PopupMenu
			{
				BackColor = SkinningDefs.TheInstance.GetColorData("lozenge_menu_colour", Color.ForestGreen),
				ForeColor = Color.White
			};

			tooltip.AddHeading(serviceName, null);
			tooltip.FormClosed += tooltip_Closed;
			tooltip.Show(TopLevelControl, this, PointToScreen(new Point((Width - tooltip.Width) / 2, 0)));
		}

		void CloseTooltip()
		{
			tooltipTimer.Stop();

			tooltip?.Close();
		}

		void tooltip_Closed(object sender, EventArgs e)
		{
			tooltip?.Dispose();
			tooltip = null;
		}

		public void OpenFeaturePopup (Node feature)
		{
			OpenUpgradesDialog(feature.GetAttribute("service_id"));
		}
	}
}