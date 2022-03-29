using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using Algorithms;
using CommonGUI;
using CoreUtils;
using LibCore;
using Network;
using ResizingUi.Animation;
using ResizingUi.Interfaces;

namespace Charts
{
	public class CustomerComplaintHeatMap : Panel
	{
		Dictionary<int, Dictionary<int, Lozenge>> rowToColumnToLozenge;
		string [] complaintTypes;
		int [] customerTypes;
		string [] complaintDisplayNames;
		bool showFeatureIds;

		Rectangle keySection;

		public CustomerComplaintHeatMap ()
		{
		}

		public void LoadData (string data)
		{
			var xml = BasicXmlDocument.Create(data);

			showFeatureIds = xml.DocumentElement.GetBooleanAttribute("show_feature_ids", true);

			rowToColumnToLozenge = new Dictionary<int, Dictionary<int, Lozenge>>();

			var complaintTypes = new List<string>();
			var complaintDisplayNames = new List<string>();
			foreach (XmlElement complaint in xml.DocumentElement.SelectSingleNode("complaints").ChildNodes)
			{
				complaintTypes.Add(complaint.GetAttribute("code"));
				complaintDisplayNames.Add(complaint.GetAttribute("desc"));
			}
			this.complaintTypes = complaintTypes.ToArray();
			this.complaintDisplayNames = complaintDisplayNames.ToArray();

			var customerTypes = new List<int>();
			foreach (XmlElement customer in xml.DocumentElement.SelectSingleNode("customers").ChildNodes)
			{
				customerTypes.Add(customer.GetIntAttribute("code").Value);
			}
			this.customerTypes = customerTypes.ToArray();

			var y = 0;
			foreach (XmlElement group in xml.DocumentElement.ChildNodes)
			{
				if (group.Name != "group")
				{
					continue;
				}

				var row = new Dictionary<int, Lozenge>();
				rowToColumnToLozenge.Add(y, row);

				var x = 0;
				foreach (XmlElement service in group.ChildNodes)
				{
					var lozenge = new Lozenge (service, this.complaintTypes, this.customerTypes) { ShowFeatureIds = showFeatureIds };
					Controls.Add(lozenge);

					row.Add(x, lozenge);

					x++;
				}

				y++;
			}

			DoSize();
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);
			DoSize();
		}

		void DoSize ()
		{
			if ((rowToColumnToLozenge == null)
			    || rowToColumnToLozenge.Count == 0)
			{
				return;
			}

			keySection = new RectangleFromBounds { Left = 0, Bottom = Height, Width = Width, Height = Math.Max(30, Height / 30) }.ToRectangle();
			var mainSection = new RectangleFromBounds { Left = 0, Top = 0, Width = Width, Bottom = keySection.Top }.ToRectangle();

			var rows = rowToColumnToLozenge.Count;
			var columns = 1 + rowToColumnToLozenge.Values.Max(ctl => ctl.Keys.Max());
			var lozengeSize = new SizeF(mainSection.Width / (float) columns, mainSection.Height / (float) rows);

			foreach (var y in rowToColumnToLozenge.Keys)
			{
				foreach (var x in rowToColumnToLozenge [y].Keys)
				{
					rowToColumnToLozenge [y] [x].Bounds = new Rectangle(mainSection.Left + ((int) (x % columns * lozengeSize.Width)),
						mainSection.Top + ((int) (x / columns * lozengeSize.Height)), (int) lozengeSize.Width, (int) lozengeSize.Height);
				}
			}

			Invalidate();
		}

		protected override void OnPaint (PaintEventArgs e)
		{
			base.OnPaint(e);
			e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

			var x = keySection.Left;
			for (var i = 0; i < complaintTypes.Length; i++)
			{
				var complaintType = complaintTypes [i];
				var complaintDisplayName = complaintDisplayNames [i];

				var iconSize = keySection.Height;
				var iconBounds = new Rectangle(x, keySection.Top, iconSize, iconSize);

				using (var complaintBackBrush = new SolidBrush(SkinningDefs.TheInstance.GetColorData($"complaint_{complaintType.ToLower()}_colour", Color.Bisque)))
				{
					e.Graphics.FillRectangle(complaintBackBrush, iconBounds);
				}

				e.Graphics.DrawImage(Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + @"\images\complaints\" + complaintType + ".png"),
					iconBounds);

				x = iconBounds.Right + 20;

				var font = SkinningDefs.TheInstance.GetPixelSizedFont(keySection.Height * 0.75f);
				var size = e.Graphics.MeasureString(complaintDisplayName, font);

				var textBounds = new Rectangle(x, keySection.Top, (int) (1.25f * size.Width), keySection.Height);
				x = textBounds.Right + 50;

				e.Graphics.DrawString(complaintDisplayName, font, Brushes.Black, textBounds);
			}
		}

		class ComplaintState : IEquatable<ComplaintState>
		{
			public bool IsBestInClass { get; set; }
			public bool IsFixed { get; set; }
			public bool IsFalselyPromisedFixed;
			public string CausedBy { get; set; }

			public List<string> WastedWorkFeatureIds { get; set; }
			
			public bool Equals (ComplaintState other)
			{
				if (ReferenceEquals(null, other)) return false;
				if (ReferenceEquals(this, other)) return true;

				return IsBestInClass == other.IsBestInClass
				       && IsFixed == other.IsFixed
				       && string.Equals(CausedBy, other.CausedBy)
					   && (WastedWorkFeatureIds == null) == (other.WastedWorkFeatureIds == null)
				       && (WastedWorkFeatureIds?.SequenceEqual(other.WastedWorkFeatureIds) ?? true);
			}

			public override bool Equals (object obj)
			{
				if (ReferenceEquals(null, obj)) return false;
				if (ReferenceEquals(this, obj)) return true;
				if (obj.GetType() != this.GetType()) return false;
				return Equals((ComplaintState) obj);
			}

			public override int GetHashCode ()
			{
				unchecked
				{
					var hashCode = IsBestInClass.GetHashCode();
					hashCode = (hashCode * 397) ^ IsFixed.GetHashCode();
					hashCode = (hashCode * 397) ^ (CausedBy != null ? CausedBy.GetHashCode() : 0);
					foreach (var featureId in WastedWorkFeatureIds)
					{
						hashCode = (hashCode * 397) ^ featureId.GetHashCode();
					}
					return hashCode;
				}
			}
		}

		public class Lozenge : FlickerFreePanel
		{
			string title;
			string iconName;
			string [] complaintTypes;
			int [] customerTypes;
			Node node;
			Dictionary<string, Dictionary<int, ComplaintState>> complaintTypeToCustomerTypeToState;
			Dictionary<string, Dictionary<int, PulseAnimator>> complaintTypeToCustomerTypeToAnimator;
			AnimatorProvider animatorProvider;

			public Lozenge (XmlElement xml, string [] complaintTypes, int [] customerTypes)
			{
				title = xml.GetAttribute("title");
				iconName = xml.GetAttribute("icon");
				showHeader = true;
				node = null;

				this.complaintTypes = complaintTypes;
				this.customerTypes = customerTypes;

				complaintTypeToCustomerTypeToState = new Dictionary<string, Dictionary<int, ComplaintState>>();
				foreach (XmlElement complaint in xml.ChildNodes)
				{
					var complaintType = complaint.GetAttribute("complaint");
					var customerType = complaint.GetIntAttribute("customer_type", 0);

					if (! complaintTypeToCustomerTypeToState.ContainsKey(complaintType))
					{
						complaintTypeToCustomerTypeToState.Add(complaintType, new Dictionary<int, ComplaintState>());
					}

					var isBest = complaint.GetBooleanAttribute("is_best_in_class", false);
					var fixedBy = complaint.GetStringAttribute("fixed_by", null);
					var brokenBy = complaint.GetStringAttribute("broken_by", null);
					var falsePredictedFixBy = complaint.GetStringAttribute("false_predicted_fix_by", null);

					var state = new ComplaintState
					{
						IsBestInClass = isBest,
						IsFixed = (fixedBy != null),
						IsFalselyPromisedFixed = ! string.IsNullOrEmpty(falsePredictedFixBy),
						CausedBy = fixedBy ?? brokenBy ?? falsePredictedFixBy
					};

					state.WastedWorkFeatureIds = new List<string>();
					foreach (XmlElement child in complaint.ChildNodes)
					{
						state.WastedWorkFeatureIds.Add(child.GetAttribute("feature_id"));
					}

					complaintTypeToCustomerTypeToState[complaintType].Add(customerType, state);
				}

				foreach (var complaintType in complaintTypes)
				{
					if (! complaintTypeToCustomerTypeToState.ContainsKey(complaintType))
					{
						complaintTypeToCustomerTypeToState.Add(complaintType, new Dictionary<int, ComplaintState>());
					}

					foreach (var customerType in customerTypes)
					{
						if (! complaintTypeToCustomerTypeToState[complaintType].ContainsKey(customerType))
						{
							complaintTypeToCustomerTypeToState[complaintType]
								.Add(customerType, new ComplaintState { IsFixed = true, CausedBy = "" });
						}
					}
				}

				complaintTypeToCustomerTypeToAnimator = new Dictionary<string, Dictionary<int, PulseAnimator>>();

				includeCustomerHeader = true;
			}

			readonly bool includeCustomerHeader;
			public Lozenge (Node node, string [] complaintTypes, int [] customerTypes, AnimatorProvider animatorProvider)
			{
				includeCustomerHeader = false;

				this.node = node;
				showHeader = false;

				this.complaintTypes = complaintTypes;
				this.customerTypes = customerTypes;

				this.animatorProvider = animatorProvider;

				var serviceName = node.GetAttribute("name");
				var heatMap = node.Tree.GetNamedNode($"{serviceName}.HeatMap.Current");

				node.AttributesChanged += node_AttributesChanged;
				heatMap.ChildAdded += heatMap_ChildAdded;
				heatMap.ChildRemoved += heatMap_ChildRemoved;
				foreach (var entry in heatMap.GetChildrenAsList())
				{
					entry.AttributesChanged += entry_AttributesChanged;
				}

				this.customerTypes = customerTypes;
				this.complaintTypes = complaintTypes;

				complaintTypeToCustomerTypeToAnimator = new Dictionary<string, Dictionary<int, PulseAnimator>>();

				UpdateFromNode();
			}

			void pulseAnimator_Update (object sender, EventArgs args)
			{
				Invalidate();
			}

			bool showHeader;

			public bool ShowHeader
			{
				get => showHeader;

				set
				{
					showHeader = value;
					Invalidate();
				}
			}

			bool showFeatureIds;

			public bool ShowFeatureIds
			{
				get => showFeatureIds;

				set
				{
					showFeatureIds = value;
					Invalidate();
				}
			}

			protected override void Dispose (bool disposing)
			{
				if (disposing)
				{
					if (node != null)
					{
						node.AttributesChanged -= node_AttributesChanged;

						var serviceName = node.GetAttribute("name");
						var heatMap = node.Tree.GetNamedNode($"{serviceName}.HeatMap.Current");
						heatMap.ChildAdded -= heatMap_ChildAdded;
						heatMap.ChildRemoved -= heatMap_ChildRemoved;
						foreach (var entry in heatMap.GetChildrenAsList())
						{
							entry.AttributesChanged -= entry_AttributesChanged;
						}
					}

					foreach (var complaintType in complaintTypeToCustomerTypeToAnimator.Keys)
					{
						foreach (var customerType in complaintTypeToCustomerTypeToAnimator[complaintType].Keys)
						{
							complaintTypeToCustomerTypeToAnimator[complaintType][customerType].Dispose();
						}
					}
				}

				base.Dispose(disposing);
			}

			void heatMap_ChildAdded (Node complaints, Node complaint)
			{
				complaint.AttributesChanged += entry_AttributesChanged;
				UpdateFromNode();
			}

			void heatMap_ChildRemoved (Node complaints, Node complaint)
			{
				complaint.AttributesChanged -= entry_AttributesChanged;
				UpdateFromNode();
			}

			void entry_AttributesChanged (Node sender, System.Collections.ArrayList attributesChanged)
			{
				UpdateFromNode();
			}

			void node_AttributesChanged (Node sender, System.Collections.ArrayList attributesChanged)
			{
				UpdateFromNode();
			}

			void UpdateFromNode ()
			{
				title = node.GetAttribute("desc");
				iconName = node.GetAttribute("icon");

				Dictionary<string, Dictionary<int, ComplaintState>> previousStates = null;

				if (complaintTypeToCustomerTypeToState != null)
				{
					previousStates = new Dictionary<string, Dictionary<int, ComplaintState>>(complaintTypeToCustomerTypeToState);
				}

				complaintTypeToCustomerTypeToState = new Dictionary<string, Dictionary<int, ComplaintState>>();
				var serviceName = node.GetAttribute("name");
				var heatMapState = node.Tree.GetNamedNode($"{serviceName}.HeatMap.Current");
				foreach (var complaint in heatMapState.GetChildrenAsList())
				{
					var complaintType = complaint.GetAttribute("complaint");
					var customerType = complaint.GetIntAttribute("customer_type", 0);

					if (! complaintTypeToCustomerTypeToState.ContainsKey(complaintType))
					{
						complaintTypeToCustomerTypeToState.Add(complaintType, new Dictionary<int, ComplaintState>());
					}

					var isBest = complaint.GetBooleanAttribute("is_best_in_class", false);
					var isOk = complaint.GetBooleanAttribute("is_ok", false);

					var lastChangedBy = complaint.GetAttribute("last_changed_by");
					var falsePromisedFixBy = complaint.GetAttribute("last_false_promised_change_by");

					complaintTypeToCustomerTypeToState[complaintType].Add(customerType,
						new ComplaintState
						{
							IsBestInClass = isBest,
							IsFixed = isOk,
							IsFalselyPromisedFixed = ((! isOk) && ! string.IsNullOrEmpty(falsePromisedFixBy)),
							CausedBy = (! string.IsNullOrEmpty(lastChangedBy) ? lastChangedBy : falsePromisedFixBy)
						});
				}

				foreach (var complaintType in complaintTypes)
				{
					if (! complaintTypeToCustomerTypeToState.ContainsKey(complaintType))
					{
						complaintTypeToCustomerTypeToState.Add(complaintType, new Dictionary<int, ComplaintState>());
					}

					foreach (var customerType in customerTypes)
					{
						if (! complaintTypeToCustomerTypeToState[complaintType].ContainsKey(customerType))
						{
							complaintTypeToCustomerTypeToState[complaintType].Add(customerType,
								new ComplaintState { IsBestInClass = false, IsFixed = true, CausedBy = "" });
						}
					}
				}

				if (previousStates != null)
				{
					foreach (var complaintType in complaintTypes)
					{
						foreach (var customerType in customerTypes)
						{
							var previousState = previousStates[complaintType][customerType];
							var currentState = complaintTypeToCustomerTypeToState[complaintType][customerType];

							if ((! previousState.Equals(currentState))
							    && (animatorProvider != null))
							{
								if (! complaintTypeToCustomerTypeToAnimator.ContainsKey(complaintType))
								{
									complaintTypeToCustomerTypeToAnimator.Add(complaintType, new Dictionary<int, PulseAnimator>());
								}

								if (! complaintTypeToCustomerTypeToAnimator[complaintType].ContainsKey(customerType))
								{
									var pulseAnimator = animatorProvider.CreatePulseAnimator();
									pulseAnimator.Update += pulseAnimator_Update;
									complaintTypeToCustomerTypeToAnimator[complaintType].Add(customerType, pulseAnimator);
								}

								complaintTypeToCustomerTypeToAnimator[complaintType][customerType].StartPulsing(10, 10, 1.25f);
							}
						}
					}
				}

				Invalidate();
			}

			protected override void OnSizeChanged (EventArgs e)
			{
				base.OnSizeChanged(e);
				Invalidate();
			}

			protected override void OnPaint (PaintEventArgs e)
			{
				e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
				e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

				RectangleF bodyBox;
				if (showHeader)
				{
					var headerBox = new Rectangle(Width / 8, 0, Width * 6 / 8, Height / 3);
					var iconBox = new Rectangle(headerBox.Left, headerBox.Top, headerBox.Width, headerBox.Height / 2);
					var titleBox = new RectangleFromBounds
					{
						Left = headerBox.Left,
						Top = iconBox.Bottom,
						Width = headerBox.Width,
						Bottom = headerBox.Bottom
					}.ToRectangle();

					var iconSize = Math.Min(iconBox.Width, iconBox.Height);
					var iconBounds = new Rectangle(iconBox.Left + ((iconBox.Width - iconSize) / 2),
						iconBox.Top + ((iconBox.Height - iconSize) / 2), iconSize, iconSize);
					e.Graphics.FillEllipse(Brushes.Black, iconBounds);
					e.Graphics.DrawImage(
						Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + @"\images\icons\" + iconName + ".png"),
						iconBounds);
					e.Graphics.DrawString(title, SkinningDefs.TheInstance.GetFont(15), Brushes.Black,
						titleBox,
						new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });

					bodyBox = new RectangleFFromBounds
					{
						Left = Width / 8,
						Top = titleBox.Bottom,
						Right = Width * 7 / 8,
						Bottom = Height * 4 / 5
					}.ToRectangleF();
				}
				else
				{
					bodyBox = new Rectangle(0, 0, Width, Height);
				}

				using (var borderBrush = new SolidBrush(SkinningDefs.TheInstance.GetColorData("heatmap_border_colour", Color.Blue)))
				{
					e.Graphics.FillRectangle(borderBrush, bodyBox);
				}
				
				var customerTypeCount = complaintTypeToCustomerTypeToState[complaintTypeToCustomerTypeToState.Keys.ToList()[0]].Count;
				var cellSize = new SizeF(bodyBox.Width / (1 + customerTypeCount),
					bodyBox.Height / (1 + complaintTypeToCustomerTypeToState.Count));
				var border = 2;

				var customerBackColour = SkinningDefs.TheInstance.GetColorData("complaint_customer_back_colour", Color.HotPink);
				var customerHeadingBackColour = includeCustomerHeader ? SkinningDefs.TheInstance.GetColorData("complaint_customer_heading_back_colour", Color.Orange) : customerBackColour;

				using (var customerHeadingBackBrush = new SolidBrush(customerHeadingBackColour))
				{
					e.Graphics.FillRectangle(customerHeadingBackBrush, new RectangleF(bodyBox.Location, cellSize));
				}

				float y = bodyBox.Top;
				float labelX = bodyBox.Left + cellSize.Width;
				var diameter = Math.Min(cellSize.Width, cellSize.Height);
				var featureIdSize = new SizeF(diameter, diameter);
				foreach (var complaintType in complaintTypes)
				{
					var cellBounds = new RectangleF(labelX, y, cellSize.Width, cellSize.Height);
					var circleBounds = cellBounds.AlignRectangle(diameter - (2 * border), diameter - (2 * border));

					using (var complaintBackBrush = new SolidBrush(SkinningDefs.TheInstance.GetColorData($"complaint_{complaintType.ToLower()}_colour", Color.Bisque)))
					{
						e.Graphics.FillRectangle(complaintBackBrush, new RectangleF(new PointF(labelX, y), cellSize));
					}

					using (var borderPen = new Pen (SkinningDefs.TheInstance.GetColorData("heatmap_border_colour"), 1))
					{
						e.Graphics.DrawRectangle(borderPen, cellBounds.ToRectangle());
					}

					e.Graphics.DrawImage(Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + @"\images\complaints\" + complaintType + ".png"), circleBounds);
					labelX += cellSize.Width;
				}
				y += cellSize.Height;

				using (var font = ResizingUi.FontScalerExtensions.GetFontToFit(this, FontStyle.Bold, "W99", featureIdSize))
				using (var customerBackBrush = new SolidBrush(customerBackColour))
				using (var cellBackBrush = new SolidBrush(SkinningDefs.TheInstance.GetColorData("heatmap_back_colour")))
				{
					foreach (var customerType in customerTypes)
					{
						var x = bodyBox.Left;

						var customerTypeCellBounds = new RectangleF(x, y, cellSize.Width, cellSize.Height);

						e.Graphics.FillRectangle(customerBackBrush, customerTypeCellBounds);

						using (var borderPen = new Pen(SkinningDefs.TheInstance.GetColorData("heatmap_border_colour"), 1))
						{
							e.Graphics.DrawRectangle(borderPen, customerTypeCellBounds.ToRectangle());
						}

						e.Graphics.DrawString(CONVERT.ToStr(customerType), SkinningDefs.TheInstance.GetFont(12, FontStyle.Bold),
							Brushes.Black,
							new RectangleF(x, y, cellSize.Width, cellSize.Height),
							new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
						x += cellSize.Width;

						foreach (var complaintType in complaintTypes)
						{
							var cellBounds = new RectangleF(x, y, cellSize.Width, cellSize.Height);

							e.Graphics.FillRectangle(cellBackBrush, cellBounds);

							using (var borderPen = new Pen (SkinningDefs.TheInstance.GetColorData("heatmap_border_colour"), 1))
							{
								e.Graphics.DrawRectangle(borderPen, cellBounds.ToRectangle());
							}

							var featureIdBounds = cellBounds.AlignRectangle(featureIdSize);

							var state = complaintTypeToCustomerTypeToState[complaintType][customerType];

							var legend = new List<string>();
							if ((!string.IsNullOrEmpty(state.CausedBy))
								&& !state.CausedBy.StartsWith("Incident"))
							{
								legend.Add(state.CausedBy);
							}

							if (state.WastedWorkFeatureIds != null)
							{
								legend.AddRange(state.WastedWorkFeatureIds);
							}

							var circleDiameter = diameter * 0.9f;

							PulseAnimator pulseAnimator = null;
							if (complaintTypeToCustomerTypeToAnimator.ContainsKey(complaintType)
							    && complaintTypeToCustomerTypeToAnimator[complaintType].ContainsKey(customerType))
							{
								pulseAnimator = complaintTypeToCustomerTypeToAnimator[complaintType][customerType];
							}
							if (pulseAnimator != null)
							{
								circleDiameter *= pulseAnimator.GetValue();
							}

							var circleSize = circleDiameter - (2 * border);
							var circleBounds = cellBounds.AlignRectangle(circleSize, circleSize);

							if (state.IsBestInClass)
							{
								var starColour = SkinningDefs.TheInstance.GetColorData("complaints_best_in_class_colour");
								using (var brush = new SolidBrush (starColour))
								{
									e.Graphics.FillStar(brush, circleBounds.ToRectangle(), 0.5f, 5);

									RenderEntryLegend(e.Graphics, legend, font, brush, featureIdBounds);
								}

								if (! string.IsNullOrEmpty(state.CausedBy))
								{
									RenderEntryLegend(e.Graphics, legend, font, Brushes.Black, featureIdBounds);
								}
							}
							else if (state.IsFixed)
							{
								var blobColour = SkinningDefs.TheInstance.GetColorData("complaints_ok_colour");
								if (string.IsNullOrEmpty(state.CausedBy))
								{
									blobColour = Color.FromArgb(64, blobColour);
								}

								using (var brush = new SolidBrush(blobColour))
								{
									e.Graphics.FillEllipse(brush, circleBounds);
								}

								if (! string.IsNullOrEmpty(state.CausedBy))
								{
									RenderEntryLegend(e.Graphics, legend, font, Brushes.Black, featureIdBounds);
								}
							}
							else if (string.IsNullOrEmpty(state.CausedBy)
							         || state.IsFalselyPromisedFixed)
							{
								using (var badBrush = new SolidBrush(SkinningDefs.TheInstance.GetColorData("complaints_bad_colour")))
								using (var pen = new Pen(badBrush, 1))
								{
									e.Graphics.DrawEllipse(pen, circleBounds);

									var centre = new PointF(circleBounds.Left + (circleBounds.Width / 2),
										circleBounds.Top + (circleBounds.Height / 2));
									var scaledRadius = (int) (circleBounds.Width / (2 * Math.Sqrt(2)));
									e.Graphics.DrawLine(pen, centre.X - scaledRadius, centre.Y - scaledRadius, centre.X + scaledRadius,
										centre.Y + scaledRadius);
									e.Graphics.DrawLine(pen, centre.X + scaledRadius, centre.Y - scaledRadius, centre.X - scaledRadius,
										centre.Y + scaledRadius);

									RenderEntryLegend(e.Graphics, legend, font, Brushes.Black, featureIdBounds);
								}
							}
							else
							{
								using (var brush = new SolidBrush(SkinningDefs.TheInstance.GetColorData("complaints_bad_colour")))
								{
									e.Graphics.FillEllipse(brush, circleBounds);
								}

								RenderEntryLegend(e.Graphics, legend, font, Brushes.Black, featureIdBounds);
							}

							x += cellSize.Width;
						}

						y += cellSize.Height;
					}
				}
			}

			void RenderEntryLegend (Graphics graphics, IList<string> legend, Font font, Brush brush, RectangleF bounds)
			{
				if (! showFeatureIds)
				{
					return;
				}

				var lineHeights = legend.Select(line => graphics.MeasureString(line, font).Height * 0.75f).ToList();
				var totalHeight = lineHeights.Sum();

				var y = (bounds.Top + bounds.Bottom - totalHeight) / 2;
				var format = new StringFormat
				{
					Alignment = StringAlignment.Center,
					LineAlignment = StringAlignment.Center,
					FormatFlags = StringFormatFlags.NoClip
				};

				for (var i = 0; i < legend.Count; i++)
				{
					var lineBounds = new RectangleF (bounds.Left, y, bounds.Width, lineHeights[i]);
					graphics.DrawString(legend[i], font, brush, lineBounds, format);
					y = lineBounds.Bottom;
				}
			}
		}
	}
}