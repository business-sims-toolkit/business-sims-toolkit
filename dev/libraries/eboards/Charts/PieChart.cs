using System;
using System.Drawing;
using System.Xml;
using System.Collections;

using LibCore;

namespace Charts
{
	/// <summary>
	/// Summary description for MaturityGraph.
	/// </summary>
	public class PieChart : Chart
	{
		protected PieChartControl pieChartCtrl;

		int NumSegs;

		class PointInfo
		{
			public string Title;
			public int Val;
		}

		public bool ShowDropShadow
		{
			set
			{
				pieChartCtrl.use_drop_shadow = value;
			}
		}

		public int KeyYOffset
		{
			set
			{
				pieChartCtrl.keyYOffset = value;
			}
		}

		public PieChart ()
		{
			CreatePieChartControl();
			this.Resize += MaturityGraph_Resize;

			this.SuspendLayout();
			this.Controls.Add(pieChartCtrl);			
			this.ResumeLayout(false);
		}

		protected virtual void CreatePieChartControl ()
		{
			pieChartCtrl = new PieChartControl();
		}

		public void SetBackColorOverride(Color newColor)
		{
			this.BackColor = newColor;
			pieChartCtrl.SetBackColorOverride(newColor);
			pieChartCtrl.Refresh();
		}

		void DoSize()
		{
			pieChartCtrl.Size = new Size(this.Width, this.Height-50);
			pieChartCtrl.Location = new Point(0,0);

			int radius = 0;
			if (pieChartCtrl.Width < pieChartCtrl.Height)
			{
				radius = pieChartCtrl.Width/2 - 60;
			}
			else
			{
				radius = pieChartCtrl.Height/2 - 60;
			}
			radius = Math.Max(0, radius);
			pieChartCtrl.PieRadius = radius;
			pieChartCtrl.PieX = pieChartCtrl.Width/2 - pieChartCtrl.PieRadius;
			pieChartCtrl.PieY = pieChartCtrl.Height/2 - pieChartCtrl.PieRadius;

            Invalidate();
		}


		public void SetCoreImage(Image CoreImg, int CoreOffset)
		{
			if (pieChartCtrl != null)
			{
				pieChartCtrl.SetCoreImage(CoreImg, CoreOffset);
			}
		}

		public void SetBackImage(Image BackImg)
		{
			if (pieChartCtrl != null)
			{
				pieChartCtrl.SetBackImage(BackImg);
			}
		}

		//this class just need to normalise the score between 1 and 10 
		//as the piechartcontrol has the implementation of getting the number of pixels
		//the output of this is a number of pixels, which is then scaled by the angle
		int CalcPieScore(int score)
		{ 
			if (score > 10) score = 10;
			if (score <= 0) score = 1;
			return pieChartCtrl.CalcPieScore(score);
		}

		int CalcProcessingRate(int prate)
		{
			int prScore = 0;

			if (prate > 4)
				prScore = 0;
			else
				prScore = (5 - prate) * 2;

			return prScore;
		}

		void MaturityGraph_Resize(object sender, EventArgs e)
		{
			DoSize();
		}

		void ReadPointsData(XmlNode child, bool outer)
		{
			//add to points
			foreach(XmlNode n in child.ChildNodes)
			{
				int segnum = 0;
				ArrayList ptArray = new ArrayList();
				if(n.NodeType == XmlNodeType.Element)
				{
					if(n.Name == "seg")
					{
						foreach(XmlAttribute att in n.Attributes)
						{
							if(att.Name == "val")
							{
								segnum = CONVERT.ParseInt(att.Value);
							}
						}
						foreach (XmlNode n2 in n.ChildNodes)
						{
							if(n2.NodeType == XmlNodeType.Element)
							{
								if(n2.Name == "point")
								{
									//add a point to given segment
									PointInfo pt = new PointInfo();
									foreach(XmlAttribute att in n2.Attributes)
									{
										if(att.Name == "title")
										{
											pt.Title = att.Value;
										}
										if(att.Name == "val")
										{
											pt.Val = CONVERT.ParseInt(att.Value);
										}
									}
									ptArray.Add(pt);
								}
							}
						}
						//need to know how many points we have before we add them
						int count = 1;
						foreach (PointInfo pt in ptArray)
						{
							if (outer)
							{
								if (pt.Title == "Processing Rate")
								{
									pieChartCtrl.AddPoint(segnum, NumSegs, count, ptArray.Count, CalcPieScore(CalcProcessingRate(pt.Val)), pt.Title);
								}
								else
								{
									pieChartCtrl.AddPoint(segnum, NumSegs, count, ptArray.Count, CalcPieScore(pt.Val), pt.Title);
								}
							}
							else
							{
								if (pt.Title == "Processing Rate")
								{
									pieChartCtrl.AddPoint2(segnum, NumSegs, count, ptArray.Count, CalcPieScore(CalcProcessingRate(pt.Val)));
								}
								else
								{
									pieChartCtrl.AddPoint2(segnum, NumSegs, count, ptArray.Count, CalcPieScore(pt.Val));
								}
							}
							count++;
						}
					}
				}
			}
		}

		public override void LoadData(string xmldata)
		{
			//clear any old data from pie chart
			pieChartCtrl.ClearData();

			NumSegs = 0;

			//need to read definition for control from xml file
			BasicXmlDocument xdoc = LibCore.BasicXmlDocument.Create(xmldata);
			XmlNode rootNode = xdoc.DocumentElement;

			foreach(System.Xml.XmlAttribute att in rootNode.Attributes)
			{
				switch (att.Name)
				{
					case "bands":
						pieChartCtrl.PieBands = CONVERT.ParseInt(att.Value);
						break;

					case "angle_offset":
						pieChartCtrl.PieAngleOffset = CONVERT.ParseInt(att.Value);
						break;
                    case "drawkey":
                        pieChartCtrl.KeyRequired = CONVERT.ParseBool(att.Value, false);
                        break;
				}
			}

			foreach(XmlNode child in rootNode.ChildNodes)
			{
				if(child.NodeType == XmlNodeType.Element)
				{
					if(child.Name == "segment")
					{
						//read segment data
						string title = "";
						Color colour = Color.Transparent;
						foreach(XmlAttribute att in child.Attributes)
						{
							if(att.Name == "title")
							{
								title = att.Value;
							}
							else if(att.Name == "colour")
							{
								colour = CONVERT.ParseComponentColor(att.Value);
							}
						}
						if (colour != Color.Transparent)
						{
							pieChartCtrl.AddSegment(title, colour);
						}
						else
						{
							pieChartCtrl.AddSegment(title);
						}
						NumSegs++;
					}
					else if(child.Name == "inner")
					{
						ReadPointsData(child, false);
					}
					else if(child.Name == "outer")
					{
						ReadPointsData(child, true);
					}
				}
			}

			pieChartCtrl.Invalidate(); //Refresh();
		}
	}
}