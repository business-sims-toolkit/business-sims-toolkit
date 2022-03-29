using System.Drawing;
using System.Windows.Forms;

using System.Collections;
using CoreUtils;
using LibCore;
using Network;
using CommonGUI;

namespace Cloud.OpsScreen
{

	/// <summary>
	/// This shows a awt style display based on activity as follows 
	/// 0 ---- zero
	/// 01-10--- 1  Green Bar
	/// 11-20--- 2  Green Bar
	/// 21-30--- 3  Green Bar
	/// 31-40--- 4  Green Bar
	/// 41-50--- 5  Green Bar
	/// 51-60--- 6  Amber Bar
	/// 61-70--- 7  Amber Bar
	/// 71-80--- 8  Amber Bar
	/// 81-90--- 9  Red Bar
	/// 91-00--- 10	Red Bar
	/// 
	/// Its a simple data structure that we check through when the draw the bars
	/// Just showDebug_Percentage to False to hide the Percentage numbers 
	/// Just showBlanks to True to show dark gray blanks 
	/// </summary>
	public class RackUtilisationPanel: FlickerFreePanel
	{
		protected Node DCNode = null;
		protected Node timeNode = null;
		protected bool autoAdjustElementWidth = false;
		protected bool autoAdjustElementHeight = true;
		protected Font Font_Rack;
		protected Font Font_dbg;

		protected Brush br_hiBaseAWT = new SolidBrush(Color.FromArgb(51, 102, 51));
		protected Brush br_hiMidAWT = new SolidBrush(Color.FromArgb(51, 153, 0));
		protected Brush br_hiTopAWT = new SolidBrush(Color.FromArgb(51, 102, 51));

        protected Brush br_hiWhite = new SolidBrush(Color.FromArgb(224, 224, 224));
        protected Brush br_hiRed = new SolidBrush(Color.FromArgb(255, 0, 0));
        protected Brush br_hiGreen = new SolidBrush(Color.FromArgb(102, 204, 0));
        protected Brush br_hiAmber = new SolidBrush(Color.FromArgb(255, 204, 0));

		protected bool UseBlueCodedDisplay = true;

		protected Hashtable displayThresholds_Min = new Hashtable();
		protected Hashtable displayThresholds_Max = new Hashtable();

		protected Hashtable displayNodeLookup = new Hashtable();
		protected ArrayList displayNodeLookupList = new ArrayList();
		protected bool showDebug_Percentage = false;
		protected bool showDebug_disp_no = false;
		protected bool showBlanks = true;

	    NodeTree model;
	    bool isTraining;

	    string datacentreTitle;

	    Color titleBackColour;
	    Color backColour;

	    Brush brush_Title;
	    Brush brush_Body;

		public RackUtilisationPanel(NodeTree nt, Node dataCentreNode, bool isTraining)
		{
            model = nt;
		    this.isTraining = isTraining;
		    DCNode = dataCentreNode;

            timeNode = model.GetNamedNode("CurrentTime");
            timeNode.AttributesChanged += new Node.AttributesChangedEventHandler(timeNode_AttributesChanged);

			string font = SkinningDefs.TheInstance.GetData("fontname");
			Font_Rack = FontRepository.GetFont(font, 8, FontStyle.Regular);
			Font_dbg = FontRepository.GetFont(font, 6, FontStyle.Regular);

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


		    Setup();
		}

        void Setup()
        {
            DoubleBuffered = true;

            SetTitle();
            SetNodeName(true);

            brush_Title = brush_Body = new SolidBrush(Color.FromArgb(244, 244, 244));
        }

        void SetTitle()
        {
            datacentreTitle = DCNode.GetAttribute("desc");

            titleBackColour = SkinningDefs.TheInstance.GetColorDataGivenDefault(datacentreTitle + "_title_back_colour", Color.Orange);
            backColour = SkinningDefs.TheInstance.GetColorDataGivenDefault(datacentreTitle + "_back_colour", Color.HotPink);
        }

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (brush_Title != null)
				{
					brush_Title.Dispose();
				}
				if (brush_Body != null)
				{
					brush_Body.Dispose();
				}

				if (br_hiBaseAWT != null)
				{
					br_hiBaseAWT.Dispose();
				}
				if (br_hiMidAWT != null)
				{
					br_hiMidAWT.Dispose();
				}
				if (br_hiTopAWT != null)
				{
					br_hiTopAWT.Dispose();
				}
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

		void SetNodeName(bool IndividualDCMode)
		{

			displayNodeLookup.Clear();
			displayNodeLookupList.Clear();

			//WE need a new ordering system as we need spaces between the racks 
			//dependant on the purpose of each rack 
			//each rack has a display index which relates to what the initail puropose of the rack as at the start 
			//the display indexer of each rack is (display index*10) + monitor_index;
			//then we iterate through the groups (as teh dispaly index chnages , we add 5 pixels space horizontally)

			foreach (Node child in DCNode.GetChildrenOfType("rack"))
			{
				int display_grp_index = child.GetIntAttribute("monitor_index", 0) + (10 * child.GetIntAttribute("monitor_group", 0));
				//int display_grp_index = child.GetIntAttribute("monitor_index", 0);
				//int percent = child.GetIntAttribute("cpu_usage_percent", 0);

				if (displayNodeLookup.ContainsKey(display_grp_index) == false)
				{
					displayNodeLookup.Add(display_grp_index, child);
					displayNodeLookupList.Add(display_grp_index);
				}
			}
			displayNodeLookupList.Sort();
		}

		protected void timeNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			Refresh();
		}

		protected override void OnPaint(PaintEventArgs e)
		{

            int titleHeight = 25;
            Font titleFont = SkinningDefs.TheInstance.GetFont(12, FontStyle.Bold);
            
            using (Brush titleBack = new SolidBrush(titleBackColour))
            {
                Rectangle titleRect = new Rectangle(0, 0, Width, titleHeight);
                e.Graphics.FillRectangle(titleBack, titleRect);

                titleRect.X = 5;

                e.Graphics.DrawString(datacentreTitle, titleFont, Brushes.White, titleRect,
                    new StringFormat { LineAlignment = StringAlignment.Far, Alignment = StringAlignment.Near });
            }

            using (Brush backBrush = new SolidBrush(backColour))
            {
                e.Graphics.FillRectangle(backBrush, new Rectangle(0, titleHeight, Width, Height - titleHeight));
            }

			

			int countRacks = 0;
			int ElementXGap = 3;
			int ElementYGap = 3;
			int GroupYGap = 12;  //additional space between different groups
			int rack_element_width = 12;
			int rack_element_height = 8;
			int BorderGap = 10;
			int xpos = BorderGap;
			int ypos = BorderGap;
			int drawing_y_pos = 0;
			int bottom_edge = Height - (BorderGap + 4);

			//determine how many racks we need to display
			countRacks = displayNodeLookup.Count;

			if (countRacks > 0)
			{
				//adjust the width to the number of racks
				if (autoAdjustElementWidth)
				{
					rack_element_width = (Width - (2 * BorderGap) - ((countRacks + 1) * ElementXGap)) / countRacks;
				}
				if (autoAdjustElementHeight)
				{
					rack_element_height = (Height - 30 - (1 * BorderGap) - ((10 + 1) * ElementXGap)) / 10;
				}

				//
				Brush tmpDrawingBrushHandle = Brushes.Green;
				Brush tmpDrawingBrushBlankHandle = new SolidBrush(Color.FromArgb(32, 32, 32));
				int index = 0;
				int Group_ID = 1;

				//New code start 
				foreach (int disp_no in displayNodeLookupList)
				{
					Node child = (Node)displayNodeLookup[disp_no];
					if (child != null)
					{
						string type = child.GetAttribute("type");
						index = child.GetIntAttribute("monitor_index", 0);
						int percent = child.GetIntAttribute("cpu_usage_percent", 0);

						if (type.ToLower() == "rack")
						{
							//x adjustments for different groups 
							if ((disp_no / 10) != Group_ID)
							{
								xpos += GroupYGap;
								Group_ID = (disp_no / 10);
							}

							for (int step = 1; step < 11; step++)
							{
								int min_level = (int)displayThresholds_Min[step];
								int max_level = (int)displayThresholds_Max[step];

								bool inBand = ((percent >= (min_level)) && (percent <= (max_level)));
								bool aboveBand = (percent >= (max_level));

								drawing_y_pos = ypos + bottom_edge - (2 * (rack_element_height + ElementYGap)) - ((step - 1) * (rack_element_height + ElementYGap));

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
											if (UseBlueCodedDisplay)
											{
												tmpDrawingBrushHandle = br_hiBaseAWT;
											}
											else
											{
												tmpDrawingBrushHandle = br_hiGreen;
											}
											break;
										case 6:
										case 7:
										case 8:
											if (UseBlueCodedDisplay)
											{
												tmpDrawingBrushHandle = br_hiMidAWT;
											}
											else
											{
												tmpDrawingBrushHandle = br_hiAmber;
											}
											break;
										case 9:
										case 10:
											if (UseBlueCodedDisplay)
											{
												tmpDrawingBrushHandle = br_hiTopAWT;
											}
											else
											{
												tmpDrawingBrushHandle = br_hiRed;
											}
											break;
									}
									e.Graphics.FillRectangle(tmpDrawingBrushHandle, xpos, drawing_y_pos, rack_element_width, rack_element_height);
								}
								else
								{
									if (showBlanks)
									{
										e.Graphics.FillRectangle(tmpDrawingBrushBlankHandle, xpos, drawing_y_pos, rack_element_width, rack_element_height);
									}
								}

								e.Graphics.DrawString(CONVERT.ToStr(index), Font_Rack, br_hiWhite, xpos, bottom_edge);
								if (showDebug_Percentage)
								{
									e.Graphics.DrawString(CONVERT.ToStr(percent), Font_dbg, br_hiWhite, xpos, bottom_edge - 30);
								}
								if (showDebug_disp_no)
								{
									e.Graphics.DrawString(CONVERT.ToStr(disp_no), Font_dbg, br_hiWhite, xpos, bottom_edge - 50);
								}
							}
							xpos += (ElementXGap + rack_element_width);
						}
					}
				}
				tmpDrawingBrushBlankHandle.Dispose();
			}
		}

	}
}
