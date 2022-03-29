using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml;

using Algorithms;
using CommonGUI;
using CoreUtils;
using Events;
using LibCore;
using ResizingUi;

namespace Charts
{
	public class DevOpsErrorReportPanel : SharedMouseEventControl
	{
		class GuidMessage
		{
			public string Guid { get; set; }
			public string Message { get; set; }
		}

		class ErrorComparator : IComparer<ErrorType>
		{
			readonly List<string> typesInOrder;

			public ErrorComparator(List<string> errorTypes)
			{
				typesInOrder = errorTypes;
			}


			public int Compare(ErrorType x, ErrorType y)
			{
				var xIndex = typesInOrder.IndexOf(x.Type);
				var yIndex = typesInOrder.IndexOf(y.Type);

				return xIndex.CompareTo(yIndex);
			}
		}

		class ErrorType
		{
			public List<GuidMessage> Messages { get; }
			public string Type { get; }

			public ErrorType(XmlElement xmlError)
			{
				Messages = new List<GuidMessage>();

				Type = xmlError.GetAttribute("error_type");
				foreach (XmlElement messageXml in xmlError.ChildNodes)
				{
					var message = messageXml.GetAttribute("message");
					var guid = messageXml.GetAttribute("guid");

					Messages.Add(new GuidMessage
					{
						Guid = guid,
						Message = message
					});
				}
			}
		}

		class DevErrorServicePanel : FlickerFreePanel
		{
			readonly string serviceName;

			public List<ErrorType> Errors { get; }

			public int ErrorCount => Errors.Count;

			bool isActive;
			public bool IsActive
			{
				get => IsActive;
				set
				{
					isActive = value;
					Invalidate();
				}
			}

			bool isHover;

			public Image IconImage { get; }

			readonly Color defaultColour;
			readonly Color activeColour;
			readonly Color hoverColour;

			readonly Color defaultTextColour;
			readonly Color activeTextColour;
			readonly Color hoverTextColour;

			public DevErrorServicePanel(XmlElement xmlService)
			{
				serviceName = xmlService.GetAttribute("service_name");
				Errors = new List<ErrorType>();

				var iconName = xmlService.GetAttribute("icon_name");
				IconImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\icons\\" + iconName + ".png");

				foreach (XmlElement child in xmlService.ChildNodes)
				{
					Errors.Add(new ErrorType(child));
				}

				Errors.Sort(new ErrorComparator(new List<string>
												{
													"product",
													"dev",
													"test",
													"release",
													"deploy"
												}));

				activeColour = Color.FromArgb(201, 214, 223);
				defaultColour = Color.FromArgb(34, 40, 49);
				hoverColour = Color.FromArgb(69, 69, 83);

				defaultTextColour = Color.White;
				activeTextColour = Color.Black;
				hoverTextColour = Color.White;

			}

			protected override void OnPaint(PaintEventArgs e)
			{
				base.OnPaint(e);

				e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;


				var backColour = defaultColour;
				var textColour = defaultTextColour;

				if (isActive)
				{
					backColour = activeColour;
					textColour = activeTextColour;
				}
				else if (isHover)
				{
					backColour = hoverColour;
					textColour = hoverTextColour;
				}


				using (Brush backColourBrush = new SolidBrush(backColour))
				{
					// Draw left side circle
					e.Graphics.FillEllipse(backColourBrush, 0, 0, Height, Height);

					// Draw the remainder of the width as a rectangle
					e.Graphics.FillRectangle(backColourBrush, Height / 2, 0, Width - Height / 2, Height);

				}

				var padding = 2;
				var iconCircleWidth = Height - (2 * padding);

				e.Graphics.DrawImage(IconImage, padding + 1, padding + 1, iconCircleWidth, iconCircleWidth);

				var textFont = SkinningDefs.TheInstance.GetFont(10);

				using (Brush textBrush = new SolidBrush(textColour))
				{
					var textHeight = (int)e.Graphics.MeasureString(serviceName, textFont).Height;

					var textY = (Height - textHeight) / 2;

					e.Graphics.DrawString(serviceName, textFont, textBrush, 80, textY);

					var errorText = Plurals.Format(ErrorCount, "Error", "Errors");

					var errorWidth = (int)e.Graphics.MeasureString(errorText, textFont).Width;

					var errorX = Width - errorWidth - 5;

					e.Graphics.DrawString(errorText, textFont, textBrush, errorX, textY);
				}


			}

			protected override void OnMouseEnter(EventArgs e)
			{
				base.OnMouseEnter(e);

				isHover = true;
				Invalidate();
			}

			protected override void OnMouseLeave(EventArgs e)
			{
				base.OnMouseLeave(e);

				isHover = false;
				Invalidate();
			}
		}

		readonly XmlNodeList services;

		DevErrorServicePanel activeServiceErrorPanel;

		readonly Dictionary<string, string> errorTypesToDisplayNames;

		public DevOpsErrorReportPanel(XmlElement xml)
		{
			if (xml == null)
			{
				throw new Exception("XML is null in DevErrorReport ctor.");
			}

			foreach (var child in xml.ChildNodes.Cast<XmlElement>().Where(child => child.Name == "Services"))
			{
				services = child.ChildNodes;
				break;
			}

			errorTypesToDisplayNames = new Dictionary<string, string>
									   {
										   {"product", "Product"},
										   {"dev", "Development"},
										   {"test", "Test"},
										   {"release", "Release"},
										   {"deploy", "Deploy"}
									   };


			DisplayServiceList();
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);

			DoSize();
		}

		void DoSize()
		{
			var yOffset = 50;
			var minHeight = 20;
			var maxHeight = 50;

			var padding = 10;

			var heightMinusTotalPadding = Height - ((services.Count - 1) * padding) - yOffset;

			var serviceButtonHeight = Maths.Clamp(heightMinusTotalPadding / Math.Max(1, services.Count), minHeight, maxHeight);

			var x = 45;
			var width = 604;

			var y = yOffset;
			foreach (DevErrorServicePanel serviceButton in Controls)
			{
				serviceButton.Size = new Size(width, serviceButtonHeight);
				serviceButton.Location = new Point(x, y);

				y += serviceButtonHeight + padding;
			}
		}

		void DisplayServiceList()
		{
			if (services != null)
			{
				foreach (XmlElement service in services)
				{
					var devErrorServicePanel = new DevErrorServicePanel(service);

					devErrorServicePanel.Click += serviceErrors_Click;
					Controls.Add(devErrorServicePanel);

					if (activeServiceErrorPanel == null)
					{
						activeServiceErrorPanel = devErrorServicePanel;
						activeServiceErrorPanel.IsActive = true;
					}

				}

			}
		}

		void serviceErrors_Click(object sender, EventArgs e)
		{
			if (activeServiceErrorPanel != null)
			{
				activeServiceErrorPanel.IsActive = false;
			}
			activeServiceErrorPanel = (DevErrorServicePanel)sender;

			activeServiceErrorPanel.IsActive = true;

			Invalidate();

		}


		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			var titleFont = SkinningDefs.TheInstance.GetFont(14);
			var headingFont = SkinningDefs.TheInstance.GetFont(10, FontStyle.Bold);
			var messageFont = SkinningDefs.TheInstance.GetFont(10);

			var x = 670;
			var y = 50;
			var yTitleOffset = 25;
			var yHeadingOffset = 20;
			var yMessageOffset = 15;
			var yOffset = 20;

			using (var brush = new SolidBrush(Color.White))
			{
				e.Graphics.FillRectangle(brush, new Rectangle(660, y, Width - 700, Height - 100));
			}
			using (var pen = new Pen(Color.LightGray, 1.0f))
			{
				e.Graphics.DrawRectangle(pen, new Rectangle(660, y, Width - 700, Height - 100));
			}

			y += 25;


			if (activeServiceErrorPanel != null)
			{
				using (var brush = new SolidBrush(Color.Black))
				{
					var errorsText = "Errors";

					var errorsSize = e.Graphics.MeasureString(errorsText, titleFont);

					e.Graphics.DrawString(errorsText, titleFont, brush, x, y);

					var iconHeight = errorsSize.Height * 2f;
					var iconY = (y + errorsSize.Height / 2f) - (iconHeight / 2f);

					e.Graphics.DrawImage(activeServiceErrorPanel.IconImage,
						new RectangleF(x + errorsSize.Width + 10, iconY, iconHeight, iconHeight));

					y += yTitleOffset;
					y += yMessageOffset;

					foreach (var errorType in activeServiceErrorPanel.Errors)
					{
						var allMessages = errorType.Messages.Select(guidMessage => guidMessage.Message).ToList();

						var guids = new List<string>();
						foreach (var guidMessage in errorType.Messages.Where(guidMessage => !guids.Contains(guidMessage.Guid)))
						{
							guids.Add(guidMessage.Guid);
						}

						var numFailuresForStage = guids.Count;

						var heading = CONVERT.Format("{0} ({1})", errorTypesToDisplayNames[errorType.Type], numFailuresForStage);
						e.Graphics.DrawString(heading, headingFont, brush, x, y);
						y += yHeadingOffset;

						var distinctMessages = new List<string>();

						foreach (var message in allMessages.Where(message => !distinctMessages.Contains(message)))
						{
							distinctMessages.Add(message);
						}


						foreach (var message in distinctMessages)
						{
							var count = allMessages.Count(msg => msg == message);

							if (count == 0)
							{
								throw new Exception("This count shouldn't be zero.");
							}

							var displayMessage = message;

							if (count > 1)
							{
								displayMessage += CONVERT.Format(" ({0})", count);
							}

							e.Graphics.DrawString(displayMessage, messageFont, brush, x, y);
							y += yMessageOffset;

						}

						y += yOffset;
					}
				}

			}
			else
			{
				using (var brush = new SolidBrush(Color.Black))
				{
					y += yTitleOffset;
					e.Graphics.DrawString("No Errors", titleFont, brush, 670, y);
				}
			}
		}

		public override List<KeyValuePair<string, Rectangle>> BoundIdsToRectangles =>
			new List<KeyValuePair<string, Rectangle>>
			{
				new KeyValuePair<string, Rectangle>("dev_error", RectangleToScreen(ClientRectangle))
			};


		public override void ReceiveMouseEvent(SharedMouseEventArgs args)
		{
			throw new NotImplementedException();
		}
	}
}
