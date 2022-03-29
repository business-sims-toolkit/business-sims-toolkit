using System;
using System.Drawing;
using System.Xml;
using System.Collections;

using LibCore;

namespace Charts
{
	/// <summary>
	/// This is a seperate development of the standard Pie chart to show multiple levels at once
	/// It is currently built to all recorded levels as overlapping shaded areas
	/// It uses a pretty similar xml data structure
	/// </summary>
	public class PieChart_MultiLevel : Chart
	{
		protected PieChartControl_MultiLevel pieChartCtrl;

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

		public PieChart_MultiLevel ()
		{
			CreatePieChartControl();
			this.Resize += MaturityGraph_Resize;

			this.SuspendLayout();
			this.Controls.Add(pieChartCtrl);			
			this.ResumeLayout(false);
		}

		protected virtual void CreatePieChartControl ()
		{
			pieChartCtrl = new PieChartControl_MultiLevel();
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
			pieChartCtrl.PieRadius = radius;
			pieChartCtrl.PieX = pieChartCtrl.Width/2 - pieChartCtrl.PieRadius;
			pieChartCtrl.PieY = pieChartCtrl.Height/2 - pieChartCtrl.PieRadius;
		}

		public void SetCoreImage(Image CoreImg, int CoreOffset)
		{
			if (pieChartCtrl != null)
			{
				pieChartCtrl.SetCoreImage(CoreImg, CoreOffset);
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

		/// <summary>
		/// We have encoded the round into the xml structure 
		/// so each block of data can be mapped to the round number 
		/// </summary>
		/// <param name="child"></param>
		void ReadPointsData(XmlNode child)
		{
			int data_round = 1;

			if (child != null)
			{
				//Extract out the round value for this data 
				foreach (XmlAttribute att in child.Attributes)
				{
					if (att.Name == "round")
					{
						data_round = CONVERT.ParseInt(att.Value);
					}
				}
				//add to points
				foreach (XmlNode n in child.ChildNodes)
				{
					int segnum = 0;
					ArrayList ptArray = new ArrayList();
					if (n.NodeType == XmlNodeType.Element)
					{
						if (n.Name == "seg")
						{
							foreach (XmlAttribute att in n.Attributes)
							{
								if (att.Name == "val")
								{
									segnum = CONVERT.ParseInt(att.Value);
								}
							}
							foreach (XmlNode n2 in n.ChildNodes)
							{
								if (n2.NodeType == XmlNodeType.Element)
								{
									if (n2.Name == "point")
									{
										//add a point to given segment
										PointInfo pt = new PointInfo();
										foreach (XmlAttribute att in n2.Attributes)
										{
											if (att.Name == "title")
											{
												pt.Title = att.Value;
											}
											if (att.Name == "val")
											{
												pt.Val = CONVERT.ParseInt(att.Value);
											}
										}
										ptArray.Add(pt);
									}
								}
							}
							//need to know how many points we have before we add them
							int count = 0;
							foreach (PointInfo pt in ptArray)
							{
								if (pt.Title == "Processing Rate")
								{
									pieChartCtrl.AddPoint(data_round, segnum, NumSegs, count, ptArray.Count, CalcPieScore(CalcProcessingRate(pt.Val)), pt.Title);
								}
								else
								{
									pieChartCtrl.AddPoint(data_round, segnum, NumSegs, count, ptArray.Count, CalcPieScore(pt.Val), pt.Title);
								}
								count++;
							}
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
					else if(child.Name == "level")
					{
						ReadPointsData(child);
					}
				}
			}
			pieChartCtrl.Invalidate(); //Refresh();
		}
	}
}