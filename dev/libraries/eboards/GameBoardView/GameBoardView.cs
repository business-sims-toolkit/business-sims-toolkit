using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Drawing;
using System.Windows.Forms;

using LibCore;
using CoreUtils;
using CommonGUI;
using Algorithms;

using Network;

namespace GameBoardView
{
	public class GameBoardView : FlickerFreePanel
	{
		string imageParentPath;
		Image backgroundImage;

		Timer timer;

		double transitionTimer;
		double transitionDuration;
		RectangleF transitionStartBounds;
		RectangleF transitionTargetBounds;
		ZoomZone targetZoomZone;

		RectangleF currentBounds;

		NodeTree model;
		List<Node> locationNodes;

		bool showLocationLabels;
		Node highlightLocation;

		bool showHidden;

		List<ZoomZone> zoomZones;
		Dictionary<string, IconInfo> iconNameToInfo;
		Dictionary<string, LocationInfo> locationNameToInfo;

		public IList<ZoomZone> Zones
		{
			get
			{
				return zoomZones;
			}
		}

		public GameBoardView (NodeTree model, string xmlFilename)
			: this (model, xmlFilename, AppInfo.TheInstance.Location)
		{
		}

		public GameBoardView (NodeTree model, string xmlFilename, string imageParentPath)
		{
			this.imageParentPath = imageParentPath;

			BasicXmlDocument xml = BasicXmlDocument.CreateFromFile(xmlFilename);

			zoomZones = new List<ZoomZone> ();
			XmlElement zooms = (XmlElement) xml.DocumentElement.SelectSingleNode("zooms");
			foreach (XmlElement zoom in zooms.ChildNodes)
			{
				ZoomZone zone = new ZoomZone (zoom);
				zoomZones.Add(zone);
			}

			iconNameToInfo = new Dictionary<string, IconInfo> ();
			XmlElement icons = (XmlElement) xml.DocumentElement.SelectSingleNode("icons");
			foreach (XmlElement icon in icons.ChildNodes)
			{
				IconInfo iconInfo = new IconInfo (icon, imageParentPath);
				iconNameToInfo.Add(iconInfo.Name, iconInfo);
			}

			locationNameToInfo = new Dictionary<string, LocationInfo> ();
			XmlElement locations = (XmlElement) xml.DocumentElement.SelectSingleNode("locations");
			foreach (XmlElement location in locations.ChildNodes)
			{
				LocationInfo locationInfo = new LocationInfo (location);
				locationInfo.ApplyOffset(((XmlElement) xml.DocumentElement.SelectSingleNode("locations")).GetDoubleAttribute("x_offset", 0),
										 ((XmlElement) xml.DocumentElement.SelectSingleNode("locations")).GetDoubleAttribute("y_offset", 0));
				locationNameToInfo.Add(locationInfo.Name, locationInfo);
			}

			timer = new Timer { Interval = 1000 / 15 };
			timer.Tick += timer_Tick;

			ReadNetwork(model);

			ResetView();
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing)
			{
				timer.Dispose();
			}

			base.Dispose(disposing);
		}

		void timer_Tick (object sender, EventArgs e)
		{
			transitionTimer += (timer.Interval / 1000.0);
			if (transitionTimer >= transitionDuration)
			{
				timer.Stop();
			}

			double t = Maths.SmoothStep(Maths.Clamp(transitionTimer / transitionDuration, 0, 1));
			currentBounds = Maths.Lerp(t, transitionStartBounds, transitionTargetBounds);
			Invalidate();
		}

		void StartTransition (double duration, RectangleF target)
		{
			transitionTimer = 0;
			transitionDuration = duration;
			transitionStartBounds = currentBounds;
			transitionTargetBounds = target;

			timer.Start();
		}

		public void ReadNetwork ()
		{
			ReadNetwork(model);
		}

		public void ReadNetwork (NodeTree model)
		{
			this.model = model;

			Node backgroundNode = model.GetNamedNode("flashboard_override");
			string filename = "board.png";
			if (backgroundNode != null)
			{
				filename = backgroundNode.GetAttribute("board_name", filename).Replace(".swf", ".png");
			}
			backgroundImage = Repository.TheInstance.GetImage(imageParentPath + @"\images\gameboard\" + filename);

			locationNodes = new List<Node> ();
			locationNodes.AddRange(model.GetNodesOfAttribTypesAsDictionary(new [] { "Hub", "Router", "Server", "MegaServer", "App", "Database", "Cooling", "powerstation", "zone" }).Keys);

			Invalidate();
		}

		ZoomZone GetDefaultZoomZone ()
		{
			return zoomZones.FirstOrDefault(zoomZone => zoomZone.Name == "All");
		}

		public void ResetView ()
		{
			timer.Stop();

			ZoomZone allZone = GetDefaultZoomZone();
			currentBounds = ((allZone != null) ? allZone.Bounds : new RectangleF (0, 0, 1, 1));

			targetZoomZone = null;
		}

		RectangleF FrameAspectCorrectedRectangle (RectangleF source, RectangleF screen)
		{
			float xScale = screen.Width / source.Width;
			float yScale = screen.Height / source.Height;
			float scale = Math.Min(xScale, yScale);

			RectangleF aspectCorrectedSourcePixelBounds = new RectangleF (source.Left + (source.Width / 2) - (screen.Width / (2 * scale)), source.Top + (source.Height / 2) - (screen.Height / (2 * scale)),
				                                                          screen.Width / scale, screen.Height / scale);

			return aspectCorrectedSourcePixelBounds;
		}

		protected override void OnPaint (PaintEventArgs e)
		{
			base.OnPaint(e);

			e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
			e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

			RectangleF windowBounds = new RectangleF (0, 0, Width, Height);

			RectangleF uncorrectedSourcePixelBounds = new RectangleF (currentBounds.Left * backgroundImage.Width, currentBounds.Top * backgroundImage.Height,
																      currentBounds.Width * backgroundImage.Width, currentBounds.Height * backgroundImage.Height);

			RectangleF clippedAspectCorrectedSourcePixelBounds = FrameAspectCorrectedRectangle(uncorrectedSourcePixelBounds, windowBounds);
			RectangleF clippedAspectCorrectedSourceFractionBounds = new RectangleF (clippedAspectCorrectedSourcePixelBounds.Left / backgroundImage.Width, clippedAspectCorrectedSourcePixelBounds.Top / backgroundImage.Height,
				                                                                    clippedAspectCorrectedSourcePixelBounds.Width / backgroundImage.Width, clippedAspectCorrectedSourcePixelBounds.Height / backgroundImage.Height);

			e.Graphics.DrawImage(backgroundImage, windowBounds,
			                     new RectangleF (clippedAspectCorrectedSourcePixelBounds.Left, clippedAspectCorrectedSourcePixelBounds.Top, clippedAspectCorrectedSourcePixelBounds.Width, clippedAspectCorrectedSourcePixelBounds.Height),
								 GraphicsUnit.Pixel);

			foreach (Node node in locationNodes)
			{
				string locationName = node.GetAttribute("location");
				string locationType = node.GetAttribute("icontype", node.GetAttribute("type"));

				if (locationNameToInfo.ContainsKey(locationName)
					&& iconNameToInfo.ContainsKey(locationType)
					&& (node.GetBooleanAttribute("visible", true)
					    || showHidden))
				{
					LocationInfo locationInfo = locationNameToInfo[locationName];
					IconInfo iconInfo = iconNameToInfo[locationType];

					PointF iconLocation = new PointF (locationInfo.Location.X + (iconInfo.Anchor.X * iconInfo.Scale.Width),
					                                  locationInfo.Location.Y + (iconInfo.Anchor.Y * iconInfo.Scale.Height));
					SizeF iconSize = iconInfo.Scale;

					RectangleF iconBounds = MapRectangle(new RectangleF (iconLocation, iconSize), clippedAspectCorrectedSourceFractionBounds, windowBounds);

					e.Graphics.DrawImage(iconInfo.Image, iconBounds);

					if (showLocationLabels
						&& ((highlightLocation == null)
						    || (highlightLocation == node)))
					{
						using (Brush brush = new SolidBrush (Color.FromArgb(50, 0, 0, 0)))
						{
							e.Graphics.FillRectangle(brush, iconBounds);
						}
						e.Graphics.DrawRectangles(Pens.White, new [] { iconBounds });

						StringFormat format = new StringFormat
						                      { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
						e.Graphics.DrawString(locationInfo.Name, SkinningDefs.TheInstance.GetFont(7), Brushes.White, iconBounds, format);
					}
				}
			}
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);

			Invalidate();
		}

		static RectangleF RectangleFFromBounds (double left, double top, double right, double bottom)
		{
			return new RectangleF ((float) left, (float) top, (float) (right - left), (float) (bottom - top));
		}

		RectangleF MapRectangle (RectangleF inSource, RectangleF sourceBoardRegion, RectangleF screenDestBoardBounds)
		{
			return RectangleFFromBounds (Maths.MapBetweenRanges(inSource.Left, sourceBoardRegion.Left, sourceBoardRegion.Right, screenDestBoardBounds.Left, screenDestBoardBounds.Right),
								         Maths.MapBetweenRanges(inSource.Top, sourceBoardRegion.Top, sourceBoardRegion.Bottom, screenDestBoardBounds.Top, screenDestBoardBounds.Bottom),
								         Maths.MapBetweenRanges(inSource.Right, sourceBoardRegion.Left, sourceBoardRegion.Right, screenDestBoardBounds.Left, screenDestBoardBounds.Right),
								         Maths.MapBetweenRanges(inSource.Bottom, sourceBoardRegion.Top, sourceBoardRegion.Bottom, screenDestBoardBounds.Top, screenDestBoardBounds.Bottom));
		}

		PointF MapPoint (PointF inSource, RectangleF sourceRegion, RectangleF destRegion)
		{
			return new PointF ((float) Maths.MapBetweenRanges(inSource.X, sourceRegion.Left, sourceRegion.Right, destRegion.Left, destRegion.Right),
					           (float) Maths.MapBetweenRanges(inSource.Y, sourceRegion.Top, sourceRegion.Bottom, destRegion.Top, destRegion.Bottom));
		}

		public void ZoomToZone (ZoomZone zoomZone)
		{
			StartTransition(1.0, zoomZone.Bounds);
			targetZoomZone = zoomZone;
		}

		public bool ShowLocationLabels
		{
			get
			{
				return showLocationLabels;
			}

			set
			{
				showLocationLabels = value;
				Invalidate();
			}
		}

		public Node HighlightLocation
		{
			get
			{
				return highlightLocation;
			}

			set
			{
				highlightLocation = value;
				Invalidate();
			}
		}

		public void SaveBoardXml (string filename)
		{
			BasicXmlDocument xml = BasicXmlDocument.Create();

			XmlElement root = xml.AppendNewChild("board");

			XmlElement zooms = root.AppendNewChild("zooms");
			foreach (ZoomZone zoomZone in zoomZones)
			{
				zooms.AppendChild(zoomZone.ToXml(xml));
			}

			XmlElement icons = root.AppendNewChild("icons");
			foreach (string iconName in iconNameToInfo.Keys)
			{
				icons.AppendChild(iconNameToInfo[iconName].ToXml(xml));
			}

			XmlElement locations = root.AppendNewChild("locations");
			List<string> sortedLocationNames = new List<string> (locationNameToInfo.Keys);
			sortedLocationNames.Sort();
			foreach (string locationName in sortedLocationNames)
			{
				locations.AppendChild(locationNameToInfo[locationName].ToXml(xml));
			}

			xml.Save(filename);
		}

		string GetZone (Node node)
		{
			if (node == null)
			{
				return null;
			}
			else if (! string.IsNullOrEmpty(node.GetAttribute("zone")))
			{
				return node.GetAttribute("zone");
			}
			else if (! string.IsNullOrEmpty(node.GetAttribute("proczone")))
			{
				return node.GetAttribute("proczone");
			}
			else
			{
				return GetZone(node.Parent);
			}
		}

		public void MoveIcons (PointF amount)
		{
			List<LocationInfo> selectedLocations = new List<LocationInfo> ();
			foreach (LocationInfo locationInfo in locationNameToInfo.Values)
			{
				System.Collections.ArrayList locationNodes = model.GetNodesWithAttributeValue("location", locationInfo.Name);
				if (locationNodes.Count > 0)
				{
					Node locationNode = (Node) (locationNodes[0]);
					string locationZone = GetZone(locationNode);

					if ((targetZoomZone != null)
						&& ! string.IsNullOrEmpty(locationZone)
						&& targetZoomZone.Name.EndsWith(locationZone))
					{
						selectedLocations.Add(locationInfo);
					}
				}
			}

			foreach (LocationInfo locationInfo in selectedLocations)
			{
				locationInfo.Move(0.001, amount);
			}

			Invalidate();
		}

		public class IconMouseDownEventArgs
		{
			public Node Location;
			public PointF GrabOffset;

			public IconMouseDownEventArgs (Node location, PointF grabOffset)
			{
				Location = location;
				GrabOffset = grabOffset;
			}
		}

		public class BackgroundMouseDownEventArgs
		{
			public PointF GrabMapLocation;
			public Point GrabScreenLocation;

			public BackgroundMouseDownEventArgs (PointF grabMapLocation, Point grabScreenLocation)
			{
				GrabMapLocation = grabMapLocation;
				GrabScreenLocation = grabScreenLocation;
			}
		}

		public delegate void IconMouseDownHandler (object sender, IconMouseDownEventArgs args);
		public event IconMouseDownHandler IconMouseDown;

		public delegate void BackgroundMouseDownHandler (object sender, BackgroundMouseDownEventArgs args);
		public event BackgroundMouseDownHandler BackgroundMouseDown;

		protected override void OnMouseDown (MouseEventArgs e)
		{
 			base.OnMouseDown(e);

			RectangleF windowBounds = new RectangleF (0, 0, Width, Height);

			RectangleF uncorrectedSourceBackgroundBounds = new RectangleF (currentBounds.Left * backgroundImage.Width, currentBounds.Top * backgroundImage.Height,
																           currentBounds.Width * backgroundImage.Width, currentBounds.Height * backgroundImage.Height);

			float xScaleNeeded = windowBounds.Width / uncorrectedSourceBackgroundBounds.Width;
			float yScaleNeeded = windowBounds.Height / uncorrectedSourceBackgroundBounds.Height;
			float scale = Math.Min(xScaleNeeded, yScaleNeeded);

			RectangleF sourceBackgroundBounds = new RectangleF (uncorrectedSourceBackgroundBounds.Left, uncorrectedSourceBackgroundBounds.Top,
																uncorrectedSourceBackgroundBounds.Width * xScaleNeeded / scale, uncorrectedSourceBackgroundBounds.Height * yScaleNeeded / scale);

			RectangleF unclippedAspectCorrectedBounds = new RectangleF (sourceBackgroundBounds.Left / backgroundImage.Width, sourceBackgroundBounds.Top / backgroundImage.Height,
			                                                            sourceBackgroundBounds.Width / backgroundImage.Width, sourceBackgroundBounds.Height / backgroundImage.Height);

//			RectangleF aspectCorrectedBounds = FrameRectangle(unclippedAspectCorrectedBounds, ref windowBounds);
			RectangleF aspectCorrectedBounds = unclippedAspectCorrectedBounds;

			RectangleF clippedWindowBounds = windowBounds;

			bool clickedOnIcon = false;
			foreach (Node node in locationNodes)
			{
				string locationName = node.GetAttribute("location");
				string locationType = node.GetAttribute("icontype", node.GetAttribute("type"));

				if (locationNameToInfo.ContainsKey(locationName)
				    && iconNameToInfo.ContainsKey(locationType)
				    && (node.GetBooleanAttribute("visible", true)
				        || showHidden))
				{
					LocationInfo locationInfo = locationNameToInfo[locationName];
					IconInfo iconInfo = iconNameToInfo[locationType];

					PointF iconLocation = new PointF (locationInfo.Location.X + (iconInfo.Anchor.X * iconInfo.Scale.Width),
					                                  locationInfo.Location.Y + (iconInfo.Anchor.Y * iconInfo.Scale.Height));
					SizeF iconSize = iconInfo.Scale;

					RectangleF iconBounds = MapRectangle(new RectangleF (iconLocation, iconSize), aspectCorrectedBounds, windowBounds);

					if (iconBounds.Contains(e.X, e.Y))
					{
						OnIconMouseDown(node, new PointF ((float) Maths.MapBetweenRanges(e.X,
						                                                                 iconBounds.Left, iconBounds.Right,
						                                                                 0, iconSize.Width),
						                                  (float) Maths.MapBetweenRanges(e.Y,
						                                                                 iconBounds.Top, iconBounds.Bottom,
						                                                                 0, iconSize.Height)));
						clickedOnIcon = true;
						break;
					}
				}
			}

			if (! clickedOnIcon)
			{
				Point screenPoint = new Point (e.X, e.Y);
				OnBackgroundMouseDown(MapPoint(screenPoint, windowBounds, aspectCorrectedBounds), screenPoint);
			}
		}

		void OnIconMouseDown (Node location, PointF grabOffset)
		{
			if (IconMouseDown != null)
			{
				IconMouseDown(this, new IconMouseDownEventArgs (location, grabOffset));
			}
		}

		void OnBackgroundMouseDown (PointF grabMapPosition, Point grabScreenPosition)
		{
			if (BackgroundMouseDown != null)
			{
				BackgroundMouseDown(this, new BackgroundMouseDownEventArgs (grabMapPosition, grabScreenPosition));
			}
		}

		public void SetLocationPosition (Node node, Point screenPosition, PointF grabOffset)
		{
			RectangleF windowBounds = new RectangleF (0, 0, Width, Height);

			RectangleF uncorrectedSourceBackgroundBounds = new RectangleF (currentBounds.Left * backgroundImage.Width, currentBounds.Top * backgroundImage.Height,
																		   currentBounds.Width * backgroundImage.Width, currentBounds.Height * backgroundImage.Height);

			float xScaleNeeded = windowBounds.Width / uncorrectedSourceBackgroundBounds.Width;
			float yScaleNeeded = windowBounds.Height / uncorrectedSourceBackgroundBounds.Height;
			float scale = Math.Min(xScaleNeeded, yScaleNeeded);

			RectangleF sourceBackgroundBounds = new RectangleF(uncorrectedSourceBackgroundBounds.Left, uncorrectedSourceBackgroundBounds.Top,
																uncorrectedSourceBackgroundBounds.Width * xScaleNeeded / scale, uncorrectedSourceBackgroundBounds.Height * yScaleNeeded / scale);

			RectangleF unclippedAspectCorrectedBounds = new RectangleF(sourceBackgroundBounds.Left / backgroundImage.Width, sourceBackgroundBounds.Top / backgroundImage.Height,
																		sourceBackgroundBounds.Width / backgroundImage.Width, sourceBackgroundBounds.Height / backgroundImage.Height);

//			RectangleF aspectCorrectedBounds = FrameRectangle(unclippedAspectCorrectedBounds, ref windowBounds);
			RectangleF aspectCorrectedBounds = unclippedAspectCorrectedBounds;

			RectangleF clippedWindowBounds = windowBounds;

			string locationName = node.GetAttribute("location");
			string locationType = node.GetAttribute("icontype", node.GetAttribute("type"));

			if (locationNameToInfo.ContainsKey(locationName)
				&& iconNameToInfo.ContainsKey(locationType))
			{
				LocationInfo locationInfo = locationNameToInfo[locationName];
				IconInfo iconInfo = iconNameToInfo[locationType];

				PointF iconLocation = new PointF (locationInfo.Location.X + (iconInfo.Anchor.X * iconInfo.Scale.Width),
												  locationInfo.Location.Y + (iconInfo.Anchor.Y * iconInfo.Scale.Height));
				SizeF iconSize = iconInfo.Scale;

				RectangleF iconBounds = MapRectangle(new RectangleF (iconLocation, iconSize), aspectCorrectedBounds, windowBounds);

				PointF mappedPosition = new PointF ((float) Maths.MapBetweenRanges(screenPosition.X,
				                                                                   clippedWindowBounds.Left, clippedWindowBounds.Right,
																				   aspectCorrectedBounds.Left, aspectCorrectedBounds.Right),
													(float) Maths.MapBetweenRanges(screenPosition.Y,
																				   clippedWindowBounds.Top, clippedWindowBounds.Bottom,
																				   aspectCorrectedBounds.Top, aspectCorrectedBounds.Bottom));

//				mappedPosition.X -= (iconInfo.Anchor.X * iconInfo.Scale.Width) - grabOffset.X;
//				mappedPosition.Y -= (iconInfo.Anchor.Y * iconInfo.Scale.Height) - grabOffset.Y;

//				mappedPosition.X += grabOffset.X;
//				mappedPosition.Y +=  grabOffset.Y;

				locationNameToInfo[locationName].Location = mappedPosition;
				Invalidate();
			}
		}

		public bool ShowHidden
		{
			get
			{
				return showHidden;
			}

			set
			{
				showHidden = value;
				Invalidate();
			}
		}
	}
}