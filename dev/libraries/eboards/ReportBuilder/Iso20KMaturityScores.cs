using System;
using System.Collections.Generic;
using System.Drawing;
using System.Xml;

using LibCore;
using GameManagement;

namespace ReportBuilder
{
	public class Iso20KMaturityScores
	{
		NetworkProgressionGameFile gameFile;
		RoundScores [] scores;
		string imagePath;

		public class Iso20KRoundMaturityRating : ITrafficLightRateable
		{
			string name;
			int score;
			Point location;
			Size size;
			Image icon;

			public Iso20KRoundMaturityRating (string name, int score, Image icon, Point location, Size size)
			{
				this.name = name;
				this.score = score;
				this.icon = icon;
				this.location = location;
				this.size = size;
			}

			public Point Location
			{
				get
				{
					return location;
				}
			}

			public Size Size
			{
				get
				{
					return size;
				}
			}

			public Image Icon
			{
				get
				{
					return icon;
				}
			}
		}

		public Iso20KMaturityScores (NetworkProgressionGameFile gameFile, RoundScores [] scores, string imagePath)
		{
			this.gameFile = gameFile;
			this.scores = scores;
			this.imagePath = imagePath;
		}

		public Iso20KRoundMaturityRating [] GetRatings (int round)
		{
			string maturityFilename = gameFile.GetMaturityRoundFile(round);

			List<Iso20KRoundMaturityRating> ratings = new List<Iso20KRoundMaturityRating> ();

			Dictionary<string, int> tagNameToScore = new Dictionary<string, int> ();
			List<XmlElement> collatedSpots = new List<XmlElement> ();

			if (System.IO.File.Exists(maturityFilename))
			{
				BasicXmlDocument xml = BasicXmlDocument.CreateFromFile(maturityFilename);
				foreach (XmlElement section in xml.DocumentElement.ChildNodes)
				{
					if (section.Name != "section")
					{
						continue;
					}

					foreach (XmlElement sectionChild in section.ChildNodes)
					{
						if (sectionChild.Name == "spot")
						{
							collatedSpots.Add(sectionChild);
						}
						else if (sectionChild.Name == "aspects")
						{
							foreach (XmlElement aspect in sectionChild.ChildNodes)
							{
								if (aspect.Name != "aspect")
								{
									continue;
								}

								string name = "";
								string tagName = "";
								int score = 0;
								string iconName = null;
								Point location = new Point(0, 0);
								Size size = new Size(0, 0);
								foreach (XmlElement child in aspect.ChildNodes)
								{
									switch (child.Name)
									{
										case "aspect_name":
											name = child.InnerText;
											break;

										case "dest_tag_name":
											tagName = child.InnerText;
											break;

										case "dest_tag_data":
											score = CONVERT.ParseIntSafe(child.InnerText, 0);
											break;

										case "spot":
											iconName = BasicXmlDocument.GetStringAttribute(child, "icon");
											location = new Point (BasicXmlDocument.GetIntAttribute(child, "x", 0),
																  BasicXmlDocument.GetIntAttribute(child, "y", 0));
											size = new Size (BasicXmlDocument.GetIntAttribute(child, "width", 0),
															 BasicXmlDocument.GetIntAttribute(child, "height", 0));

											if (! BasicXmlDocument.GetBoolAttribute(child, "centred", true))
											{
												location = new Point (location.X + (size.Width / 2), location.Y + (size.Height / 2));
											}
											break;
									}
								}

								if (iconName != null)
								{
									Image icon = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + @"\" + imagePath + @"\"
																				 + iconName + "_" + GetIconSuffixByScore(score) + ".png");
									ratings.Add(new Iso20KRoundMaturityRating(name, score, icon, location, size));
								}

								if (!string.IsNullOrEmpty(tagName))
								{
									tagNameToScore.Add(tagName, score);
								}
							}
						}
					}
				}

				foreach (XmlElement spot in collatedSpots)
				{
					string iconName = BasicXmlDocument.GetStringAttribute(spot, "icon");
					Point location = new Point (BasicXmlDocument.GetIntAttribute(spot, "x", 0),
												BasicXmlDocument.GetIntAttribute(spot, "y", 0));
					Size size = new Size (BasicXmlDocument.GetIntAttribute(spot, "width", 0),
					                      BasicXmlDocument.GetIntAttribute(spot, "height", 0));

					int? score = null;
					foreach (XmlElement spotChild in spot.ChildNodes)
					{
						if (spotChild.Name == "collate_score")
						{
							string tagName = BasicXmlDocument.GetStringAttribute(spotChild, "dest_tag_name");
							int spotChildScore = tagNameToScore[tagName];

							if (score.HasValue)
							{
								score = Math.Min(score.Value, spotChildScore);
							}
							else
							{
								score = spotChildScore;
							}
						}
					}

					string spotIconName = BasicXmlDocument.GetStringAttribute(spot, "icon");
					Point spotLocation = new Point (BasicXmlDocument.GetIntAttribute(spot, "x", 0),
													BasicXmlDocument.GetIntAttribute(spot, "y", 0));
					Size spotSize = new Size (BasicXmlDocument.GetIntAttribute(spot, "width", 0),
											  BasicXmlDocument.GetIntAttribute(spot, "height", 0));
					if (! BasicXmlDocument.GetBoolAttribute(spot, "centred", true))
					{
						spotLocation = new Point (spotLocation.X + (spotSize.Width / 2), spotLocation.Y + (spotSize.Height / 2));
					}
					Image icon = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + @"\" + imagePath + @"\"
																 + spotIconName + "_" + GetIconSuffixByScore(score.Value) + ".png");
					ratings.Add(new Iso20KRoundMaturityRating (spotIconName, score.Value, icon, spotLocation, spotSize));
				}
			}

			return ratings.ToArray();
		}

		string GetIconSuffixByScore (int score)
		{
			if (score == 5)
			{
				return "green";
			}
			else if (score >= 3)
			{
				return "amber";
			}
			else
			{
				return "red";
			}
		}
	}
}