using System.Drawing;
using System.Windows.Forms;

using System.Collections;
using CoreUtils;
using LibCore;
using Network;

namespace Cloud.OpsScreen
{
	public class ShadedViewPanel_GlobalUtilisation : ShadedViewPanel_Base
	{
		protected Node timeNode = null;
		
		protected Font Font_Rack;
		protected Font Font_dbg;

		protected Hashtable displayThresholds_Min = new Hashtable();
		protected Hashtable displayThresholds_Max = new Hashtable();

		protected Hashtable PowerLevels = new Hashtable();

		protected int productionAmount = 0;
		protected int productionCount = 0;

		protected int devtestAmount = 0;
		protected int devtestCount = 0;

		protected int productionPercentage = 0;
		protected int devtestPercentage = 0;

		protected bool ShowAbsoluteValue = false;
		
		public ShadedViewPanel_GlobalUtilisation(NodeTree nt, bool isTraining)
			: base(nt, isTraining)
		{
			string font = SkinningDefs.TheInstance.GetData("fontname");
			Font_Rack = FontRepository.GetFont(font, 10, FontStyle.Regular);
			Font_dbg = FontRepository.GetFont(font, 6, FontStyle.Regular);

			timeNode = _model.GetNamedNode("CurrentTime");
			timeNode.AttributesChanged += new Node.AttributesChangedEventHandler(timeNode_AttributesChanged);

			addBand(1, 1, 10);
			addBand(2, 11, 20);
			addBand(3, 21, 30);
			addBand(4, 31, 40);
			addBand(5, 41, 50);
			addBand(6, 51, 60);
			addBand(7, 61, 70);
			addBand(8, 71, 80);
			addBand(9, 81, 90);
			addBand(10, 91, 100);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (timeNode != null)
				{
					timeNode.AttributesChanged -= new Node.AttributesChangedEventHandler(timeNode_AttributesChanged);
					timeNode = null;
				}
			}
			base.Dispose(disposing);
		}

		private void addBand(int bandnumber, int minLevel, int maxLevel)
		{
			if (displayThresholds_Min.ContainsKey(bandnumber) == false)
			{
				displayThresholds_Min.Add(bandnumber, minLevel);
			}
			if (displayThresholds_Max.ContainsKey(bandnumber) == false)
			{
				displayThresholds_Max.Add(bandnumber, maxLevel);
			}
		}

		protected void timeNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			int seconds = sender.GetIntAttribute("seconds", 0);
			if (seconds % 60 == 1)
			{
				ReBuildDataLevels();
			}
		}

		private void ReBuildDataLevels()
		{ 
			Hashtable ht = new Hashtable();
			ArrayList al = new ArrayList();

			al.Add("rack");

			productionAmount = 0;
			productionCount = 0;
			devtestAmount = 0;
			devtestCount = 0;
			productionPercentage = 0;
			devtestPercentage = 0; 

			ht = _model.GetNodesOfAttribTypes(al);

			PowerLevels.Clear();


			foreach (Node n in ht.Keys)
			{
				string type = n.GetAttribute("type");
				string owner = n.GetAttribute("owner");
				int percent = n.GetIntAttribute("cpu_usage_percent", 0);

				if (owner.IndexOf("dev&test") > -1)
				{ 
					devtestAmount += percent;
					devtestCount ++;
				}
				else
				{
					productionAmount += percent;
					productionCount++;
				}
			}

			if (productionCount > 0)
			{
				productionPercentage = (productionAmount) / (productionCount);
				PowerLevels.Add("Production", productionPercentage);
			}
			if (devtestCount > 0)
			{
				devtestPercentage = (devtestAmount) / (devtestCount);
				PowerLevels.Add("Dev & Test", devtestPercentage);
			}
			Refresh();
		}

		private void UseDummyData()
		{
			PowerLevels.Clear();
			PowerLevels.Add("Production", 40);
			PowerLevels.Add("Dev & Test", 90);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			e.Graphics.FillRectangle(Brushes.Black, 0, 0, Width, Height);
			if (ControlBackgroundImage != null)
			{
				e.Graphics.DrawImageUnscaled(BackgroundImage, 0, 0, ControlBackgroundImage.Width, ControlBackgroundImage.Height);
			}
			if (string.IsNullOrEmpty(TitleText) == false)
			{
				e.Graphics.DrawString(TitleText, Font_Title, brush_Title, 0, 0);
			}

			//drawing the 

			//e.Graphics.DrawString("PR "+CONVERT.ToStr(productionPercentage), Font_Title, brush_Title, 0, 30);
			//e.Graphics.DrawString("DT " + CONVERT.ToStr(devtestPercentage), Font_Title, brush_Title, 0, 60);

			int ElementXGap = 3;
			int ElementYGap = 5;
			int rack_element_width = 6;
			int rack_element_height = 12;
			int BorderGap = 5;
			int xpos = BorderGap;
			int ypos = BorderGap + 20;
			int drawing_x_pos = 0;
			int barStartX = 90;
			int far_edge = Width - (BorderGap*3 + 4);
			Brush tmpDrawingBrushHandle = br_hiGreen;

			//UseDummyData();


			foreach (string name in PowerLevels.Keys)
			{
				int precentage = (int)PowerLevels[name];
				
				for (int step = 1; step < 11; step++)
				{
					int min_level = (int)displayThresholds_Min[step];
					int max_level = (int)displayThresholds_Max[step];

					bool inBand = ((precentage >= (min_level)) && (precentage <= (max_level)));
					bool aboveBand = (precentage >= (max_level));


					//if (percent > (test_level))
					if ((inBand) | (aboveBand))
					{
						switch (step)
						{
							case 1:
							case 2:
							case 3:
							case 4:
							case 5:
								tmpDrawingBrushHandle = br_hiGreen;
								break;
							case 6:
							case 7:
							case 8:
								tmpDrawingBrushHandle = br_hiAmber;
								break;
							case 9:
							case 10:
								tmpDrawingBrushHandle = br_hiRed;
								break;
						}
						drawing_x_pos = barStartX + ((step - 1) * (rack_element_width + ElementXGap));
						e.Graphics.FillRectangle(tmpDrawingBrushHandle, drawing_x_pos, ypos, rack_element_width, rack_element_height);
						//e.Graphics.DrawString(CONVERT.ToStr(step), Font_Rack, this.br_hiWhite, drawing_x_pos, ypos);
					}
					if (ShowAbsoluteValue)
					{
						e.Graphics.DrawString(name + CONVERT.ToStr(productionPercentage), Font_Rack, brush_Title, xpos, ypos);
					}
					else
					{
						e.Graphics.DrawString(name, Font_Rack, brush_Title, xpos, ypos);
					}
				}
				ypos += (ElementYGap + rack_element_height);
			}
		}


	}
}
