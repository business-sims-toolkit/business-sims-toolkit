using System;
using System.Collections;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Text;
using System.Drawing.Drawing2D;
using System.Xml;
using System.Globalization;
using System.IO;

using CommonGUI;
using LibCore;
using Network;
using CoreUtils;
using Polestar_PM.DataLookup;

using GameManagement;

namespace Polestar_PM.OpsGUI
{
	/// <summary>
	/// The GamePlanPanel displays the resource utilisation graph.
	/// This shows how we are using the staff both internal and external over the variuous days over the projects.
	/// The players are trying to even out the demand for staff over all the days to get max utlisation
	/// but usually there is a few people at the start and at the end who are idle.
	///  
	/// This graph will always show both the internal and external resource utilisation 
	/// As the current moves on, the past days will be shown as well the future 
	/// </summary>
	public class GamePlanPanel : System.Windows.Forms.UserControl
	{
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
		private int MaxGameDays = 25;

		private Font MyDefaultSkinFontNormal10 = null;
		private Font MyDefaultSkinFontBold6 = null;
		private Font MyDefaultSkinFontBold8 = null;
		private Font MyDefaultSkinFontBold10 = null;
		private Font MyDefaultSkinFontBold12 = null;
		private Pen PenHighLight = new Pen(Color.Black, 1);
		private Pen PenBounds = new Pen(Color.Pink, 1);			
		private Pen PenKey = new Pen(Color.Cyan, 1);			
		private Pen ChartGridPen = new Pen(Color.DarkGray, 1);

		private int DisplayRound = 1;
		private int DisplayCurrentDay;
		private Rectangle DrawingBox = new Rectangle(0,0,0,0);
		private Rectangle ExtStaffBox = new Rectangle(0,0,0,0);
		private Rectangle IntStaffBox = new Rectangle(0,0,0,0);
		private int DisplayTitleHeight = 40;
		private int HighLightDay = 0;
		
		private int CostGridCellWidth = 0;
		private int CostGridCellHeight = 0;
		private int GanttGridCellWidth = 0;
		private int GanttGridCellHeight = 0;
		private int CostGridCellOffsetX = 0;
		private int CostGridCellOffsetY = 0;
		private int GanttGridCellOffsetX = 0;
		private int GanttGridCellOffsetY = 0;

		bool compactMode = false;

		private Boolean DrawProjectInKeyArea = false;

		protected Brush OverDemandTextBrush= null;  // The Over Demand text Back colour Brush
		protected Brush backBrush= null;

		protected NetworkProgressionGameFile _game_file;
		protected Hashtable past_days = new Hashtable();
		protected Hashtable future_days = new Hashtable();
		
		protected bool ViewingDev = true;
		protected bool use_single_staff_section = false;
		protected bool show_resutil_debug = false;
		protected NodeTree currentModel = null;
		protected bool isEndRoundReport = false;  //If true then we show the conflicts 

		ComboBox cmb_phasechoice = null;

		Font dayLabelFont;
		Font dayLegendFont;
		Font yAxisTextFont;
		Font yAxisNumberFont;

		//Color dayColour = Color.FromArgb(85, 183, 221);
		Color dayColour = Color.FromArgb(192,192,192);
		Color dayTextColour = Color.FromArgb(0,0,0);
		Color[] columnBackgroundColours = new Color[] { Color.White, Color.FromArgb(241, 242, 242) };
		//The colours for the Graph Axis Colours 
		//Color [] contractorAxisColours = new Color [] { Color.FromArgb(0, 66, 109), Color.FromArgb(10, 92, 133) };
		Color[] contractorAxisColours = new Color[] { Color.FromArgb(96, 96, 96), Color.FromArgb(112, 112, 112) };
		//Color [] staffAxisColours = new Color [] { Color.FromArgb(69, 150, 31), Color.FromArgb(46, 120, 76) };
		Color [] staffAxisColours = new Color [] { Color.FromArgb(96, 96, 96), Color.FromArgb(112, 112, 112) };

		//////The colours for the Individual Day Bars for the Past Data
		////Color [] contractorBarColours_Past = new Color [] { Color.FromArgb(10, 101, 120), Color.FromArgb(0, 101, 177) };
		////Color[] staffBarColours_Past = new Color[] { Color.FromArgb(46, 120, 76), Color.FromArgb(69, 150, 31) };
		//////The colours for the Individual Day Bars for the Past Data 
		////Color[] contractorBarColours_Future = new Color[] { Color.FromArgb(20, 194, 229), Color.FromArgb(0, 148, 255) };
		////Color[] staffBarColours_Future = new Color[] { Color.FromArgb(68, 175, 111), Color.FromArgb(106, 224, 47) };

		//////Two Yellows
		//////The colours for the Individual Day Bars for the Past Data
		////Color[] contractorBarColours_Past = new Color[] { Color.Goldenrod, Color.Goldenrod };
		////Color[] staffBarColours_Past = new Color[] { Color.Goldenrod, Color.Goldenrod }; 
		//////The colours for the Individual Day Bars for the Past Data 
		////Color[] contractorBarColours_Future = new Color[] { Color.Yellow, Color.Yellow };
		////Color[] staffBarColours_Future = new Color[] { Color.Yellow, Color.Yellow };

		//Two Yellows
		//The colours for the Individual Day Bars for the Past Data
		Color[] contractorBarColours_Past = new Color[] { Color.Gold, Color.Gold };
		Color[] staffBarColours_Past = new Color[] { Color.Gold, Color.Gold };
		//The colours for the Individual Day Bars for the Past Data 
		Color[] contractorBarColours_Future = new Color[] { Color.Gold, Color.Gold };
		Color[] staffBarColours_Future = new Color[] { Color.Gold, Color.Gold };


		public GamePlanPanel(int round, bool isEndReport)
		{
			DisplayRound = round;
			isEndRoundReport = isEndReport;

			string fontname =  SkinningDefs.TheInstance.GetData("fontname");
			MyDefaultSkinFontBold6 = ConstantSizeFont.NewFont(fontname, 6f, FontStyle.Bold);
			MyDefaultSkinFontBold8 = ConstantSizeFont.NewFont(fontname,8f,FontStyle.Bold);
			MyDefaultSkinFontBold10 = ConstantSizeFont.NewFont(fontname,10f,FontStyle.Bold);
			MyDefaultSkinFontBold12 = ConstantSizeFont.NewFont(fontname,12f,FontStyle.Bold);
			MyDefaultSkinFontNormal10 = ConstantSizeFont.NewFont(fontname,10f,FontStyle.Regular);
			dayLegendFont = ConstantSizeFont.NewFont(fontname, 10f, FontStyle.Bold);
			dayLabelFont = ConstantSizeFont.NewFont(fontname, 10f, FontStyle.Bold);
			yAxisTextFont = ConstantSizeFont.NewFont(fontname, 8f, FontStyle.Bold);
			yAxisNumberFont = ConstantSizeFont.NewFont(fontname, 10f, FontStyle.Bold);

			string UseIncidentsAsElapsedDays_str = SkinningDefs.TheInstance.GetData("use_single_staff_section", "false");
			if (UseIncidentsAsElapsedDays_str.IndexOf("true") > -1)
			{
				use_single_staff_section = true;
			}

			string show_resutil_debug_str = SkinningDefs.TheInstance.GetData("show_res_util_debug_display", "false");
			if (show_resutil_debug_str.IndexOf("true") > -1)
			{
				show_resutil_debug = true;
			}


			backBrush = new SolidBrush(Color.FromArgb(218,218,203)); 
			OverDemandTextBrush = new SolidBrush(Color.FromArgb(255,255,255)); 

			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
			// TODO: Add any initialization after the InitializeComponent call
			SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.DoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);

			cmb_phasechoice = new ComboBox();
			cmb_phasechoice.DropDownStyle = ComboBoxStyle.DropDownList;

			if (use_single_staff_section)
			{
				cmb_phasechoice.Items.Add("Round 1");
				cmb_phasechoice.Items.Add("Round 2");
				cmb_phasechoice.Items.Add("Round 3");
			}
			else
			{
				cmb_phasechoice.Items.Add("Round 1 Development");
				cmb_phasechoice.Items.Add("Round 1 Test");
				cmb_phasechoice.Items.Add("Round 2 Development");
				cmb_phasechoice.Items.Add("Round 2 Test");
				cmb_phasechoice.Items.Add("Round 3 Development");
				cmb_phasechoice.Items.Add("Round 3 Test");
			}
			cmb_phasechoice.Location = new Point(0, 30);
			cmb_phasechoice.Size = new Size(170,25);
			cmb_phasechoice.SelectedIndexChanged += new EventHandler(cmb_phasechoice_SelectedIndexChanged);

			if (use_single_staff_section)
			{
				cmb_phasechoice.SelectedIndex = Math.Min(cmb_phasechoice.Items.Count - 1, Math.Max(0, (round - 1)));
			}
			else
			{
				cmb_phasechoice.SelectedIndex = Math.Min(cmb_phasechoice.Items.Count - 1, Math.Max(0, 2 * (round - 1)));
			}

			if (isEndReport)
			{
				this.Controls.Add(cmb_phasechoice);
			}
			// 
		}

		public void setModel(NodeTree model)
		{
			currentModel = model;
		}

		public void cmb_phasechoice_SelectedIndexChanged(object sender, EventArgs e)
		{
			ComboBox cmb2 = sender as ComboBox;

			if (use_single_staff_section)
			{
				ViewingDev = true;
				DisplayRound = 1 + (cmb2.SelectedIndex);
			}
			else
			{
				ViewingDev = ((cmb2.SelectedIndex % 2) == 0);
				DisplayRound = 1 + (cmb2.SelectedIndex / 2);
			}
			ExtractData();
			Invalidate();
		}

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			backBrush.Dispose();

			if (OverDemandTextBrush != null)
			{
				OverDemandTextBrush.Dispose();
				OverDemandTextBrush= null;
			}
			//get rid of the Font
			if (MyDefaultSkinFontNormal10 != null)
			{
				MyDefaultSkinFontNormal10.Dispose();
				MyDefaultSkinFontNormal10 = null;
			}
			//get rid of the Font
			if (MyDefaultSkinFontBold6 != null)
			{
				MyDefaultSkinFontBold6.Dispose();
				MyDefaultSkinFontBold6 = null;
			}
			//get rid of the Font
			if (MyDefaultSkinFontBold8 != null)
			{
				MyDefaultSkinFontBold8.Dispose();
				MyDefaultSkinFontBold8 = null;
			}
			//get rid of the Font
			if (MyDefaultSkinFontBold10 != null)
			{
				MyDefaultSkinFontBold10.Dispose();
				MyDefaultSkinFontBold10 = null;
			}
			//get rid of the Font
			if (MyDefaultSkinFontBold12 != null)
			{
				MyDefaultSkinFontBold12.Dispose();
				MyDefaultSkinFontBold12 = null;
			}

			if (dayLegendFont != null)
			{
				dayLegendFont.Dispose();
			}
			if (dayLabelFont != null)
			{
				dayLabelFont.Dispose();
			}
			if (yAxisNumberFont != null)
			{
				yAxisNumberFont.Dispose();
			}
			if (yAxisTextFont != null)
			{
				yAxisTextFont.Dispose();
			}

			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		public void LoadData(NetworkProgressionGameFile game_file)
		{
			_game_file = game_file;
			ClearData();
			ExtractData();
		}

		public void SetBackColor(Color newColor)
		{
			backBrush.Dispose();
			backBrush = new SolidBrush(newColor); 
		}

		private void ExtractData()
		{
			DisplayCurrentDay = 1;
			string filename;

			if (currentModel != null)
			{
				Node CurrDayNode = currentModel.GetNamedNode("CurrentDay");
				DisplayCurrentDay = CurrDayNode.GetIntAttribute("day", 0);
			}
			else
			{
				DisplayCurrentDay = 30;
			}

			this.future_days.Clear();
			this.past_days.Clear();

			if ((_game_file != null))// && (DisplayRound <= _game_file.LastRoundPlayed))
			{
				//NodeTree mynodetree = _game_file.GetNetworkModel(DisplayRound, GameFile.GamePhase.OPERATIONS);
				//Node CurrDayNode = mynodetree.GetNamedNode("CurrentDay");
				//DisplayCurrentDay = CurrDayNode.GetIntAttribute("day", 0);
				
				//Extract the Time Sheets (for past execution information) 
				filename = _game_file.GetRoundFile(DisplayRound, "past_timesheet.xml", GameFile.GamePhase.OPERATIONS);
				if (File.Exists(filename))
				{
					XmlDocument doc = new XmlDocument();
					doc.Load(filename);
					XmlNode fn = doc.FirstChild;

					int DevIntMax = CONVERT.ParseIntSafe(fn.Attributes["maximum_int_dev_staff_count"].InnerText, 0);
					int DevExtMax = CONVERT.ParseIntSafe(fn.Attributes["maximum_ext_dev_staff_count"].InnerText, 0);
					int TestIntMax = CONVERT.ParseIntSafe(fn.Attributes["maximum_int_test_staff_count"].InnerText, 0);
					int TestExtMax = CONVERT.ParseIntSafe(fn.Attributes["maximum_ext_test_staff_count"].InnerText, 0);

					foreach (XmlNode node in doc.DocumentElement.ChildNodes)
					{
						int reportday = CONVERT.ParseIntSafe(node.Attributes["value"].Value,0);
						DayTimeSheet day_sheet = new DayTimeSheet();
						day_sheet.SetMaximumStaffNumbers(DevIntMax, DevExtMax, TestIntMax, TestExtMax);
						day_sheet.ReadFromXmlNode(node);

						if (past_days.ContainsKey(reportday) == false)
						{
							this.past_days.Add(reportday, day_sheet);
						}
					}
				}

				//Extract the Time Sheets (for future execution information) 
				filename = _game_file.GetRoundFile(DisplayRound, "future_timesheet.xml", GameFile.GamePhase.OPERATIONS);
				if (File.Exists(filename))
				{
					XmlDocument doc = new XmlDocument();
					doc.Load(filename);
					XmlNode fn = doc.FirstChild;

					int DevIntMax = CONVERT.ParseIntSafe(fn.Attributes["maximum_int_dev_staff_count"].InnerText, 0);
					int DevExtMax = CONVERT.ParseIntSafe(fn.Attributes["maximum_ext_dev_staff_count"].InnerText, 0);
					int TestIntMax = CONVERT.ParseIntSafe(fn.Attributes["maximum_int_test_staff_count"].InnerText, 0);
					int TestExtMax = CONVERT.ParseIntSafe(fn.Attributes["maximum_ext_test_staff_count"].InnerText, 0);

					foreach (XmlNode node in doc.DocumentElement.ChildNodes)
					{
						int reportday = CONVERT.ParseIntSafe(node.Attributes["value"].Value,0);
						DayTimeSheet day_sheet = new DayTimeSheet();
						day_sheet.SetMaximumStaffNumbers(DevIntMax, DevExtMax, TestIntMax, TestExtMax);
						day_sheet.ReadFromXmlNode(node);

						if (past_days.ContainsKey(reportday) == false)
						{
							this.future_days.Add(reportday, day_sheet);
						}
					}
				}
			}

			Invalidate();
		}
		
		private void ClearData()
		{
			this.past_days.Clear();
			this.future_days.Clear();
		}

		protected void getStatsForDay(bool isPast, int day, bool isDEV,  
			out int IntMax, out int IntEmployed, out int IntIdle, 
			out int ExtMax, out int ExtEmployed, out int ExtIdle)
		{
			int DevIntMax = 0;
			int DevExtMax = 0;
			int TestIntMax = 0;
			int TestExtMax = 0;

			IntMax =0;
			IntEmployed =0;
			IntIdle =0;
			ExtMax =0;
			ExtEmployed =0;
			ExtIdle =0;

			DayTimeSheet time_sheet = null;

			if (isPast)
			{
				if (past_days.ContainsKey(day))
				{
					time_sheet = (DayTimeSheet)past_days[day];
				}
			}
			else
			{
				if (future_days.ContainsKey(day))
				{
					time_sheet = (DayTimeSheet)future_days[day];
				}
			}

			if (time_sheet != null)
			{
				//Extract out the maximums fro the timesheet
				time_sheet.GetMaximumStaffNumbers(out DevIntMax, out DevExtMax, out TestIntMax, out TestExtMax);
				if (isDEV)
				{
					IntMax = DevIntMax;
					ExtMax = DevExtMax;
					//IntEmployed = time_sheet.Staff.Dev.Employed;
					//IntIdle = time_sheet.Staff.Dev.Idle;
					//ExtEmployed = time_sheet.Contractors.Dev.Employed;
					//ExtIdle = time_sheet.Contractors.Dev.Idle;
					IntEmployed = time_sheet.staff_int_dev_day_employed_count;
					IntIdle = time_sheet.staff_int_dev_day_idle_count;
					ExtEmployed = time_sheet.staff_ext_dev_day_employed_count;
					ExtIdle = time_sheet.staff_ext_dev_day_idle_count;
				}
				else
				{
					IntMax = TestIntMax;
					ExtMax = TestExtMax;
					//IntEmployed = time_sheet.Staff.Test.Employed;
					//IntIdle = time_sheet.Staff.Test.Idle;
					//ExtEmployed = time_sheet.Contractors.Test.Employed;
					//ExtIdle = time_sheet.Contractors.Test.Idle;
					IntEmployed = time_sheet.staff_int_test_day_employed_count;
					IntIdle = time_sheet.staff_int_test_day_idle_count;
					ExtEmployed = time_sheet.staff_ext_test_day_employed_count;
					ExtIdle = time_sheet.staff_ext_test_day_idle_count;
				}
			}
		}

		#region Component Designer generated code
		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			// 
			// GamePlanPanel
			// 
			this.Name = "GamePlanPanel";
			this.Size = new System.Drawing.Size(664, 504);
			this.Paint += new System.Windows.Forms.PaintEventHandler(this.GamePlanPanel_Paint);
			this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.GamePlanPanel_MouseDown);
		}
		#endregion

		#region Text Utils

		private string formatDaystr(int day)
		{
			if (day < 10)
			{
				return " "+day.ToString();
			}
			else
			{
				return day.ToString();
			}
		}

		#endregion Text Utils

		#region Chart Drawing Methods  

		private int getOrderedExcess(int day, bool isInt, int MaxLevel) 
		{ 
			int required_level =0;

			NodeTree mynodetree = _game_file.GetNetworkModel(DisplayRound, GameFile.GamePhase.OPERATIONS);

			string node_name = "demand_sheet"+CONVERT.ToStr(day);
			Node demandnode = mynodetree.GetNamedNode(node_name);
			if (demandnode != null)
			{
				int int_dev_staff_requested = demandnode.GetIntAttribute("IntDevPeopleRequired", 0);
				int int_test_staff_requested = demandnode.GetIntAttribute("IntTestPeopleRequired", 0);
				int ext_dev_staff_requested = demandnode.GetIntAttribute("ExtDevPeopleRequired", 0);
				int ext_test_staff_requested = demandnode.GetIntAttribute("ExtTestPeopleRequired", 0);

				if (isInt)
				{
					required_level = int_dev_staff_requested + int_test_staff_requested;
					required_level = Math.Max(required_level - MaxLevel, 0);
				}
				else
				{
					required_level = ext_dev_staff_requested + ext_test_staff_requested;
					required_level = Math.Max(required_level - MaxLevel, 0);
				}
			}
			return required_level;
		}

		/// <summary>
		/// This draws the chart showing the Utilisation of External staff (Contractors)
		/// </summary>
		/// <param name="g"></param>
		/// <param name="test"></param>
		protected void DrawExternalStaffChart (Graphics g, bool test)
		{
			g.SmoothingMode = SmoothingMode.HighQuality;

			int dayStripTop = ExtStaffBox.Top;
			int dayStripHeight = 35;
			int graphTop = dayStripTop + dayStripHeight;
			int graphHeight = ExtStaffBox.Height - (graphTop - ExtStaffBox.Top);

			int IntMax = 0;
			int IntEmployed = 0;
			int IntIdle = 0;
			int ExtMax = 0;
			int ExtEmployed = 0;
			int ExtIdle = 0;

			int maxYValue = 4;
			bool isPast = false;

			// Get a scale for the y. (need the max day 
			for (int day = 1; day <= GameConstants.MAX_NUMBER_DAYS; day++)
			{
				if (day <= DisplayCurrentDay)
				{
					isPast = true;
					this.getStatsForDay(isPast, day, ViewingDev, out IntMax, out IntEmployed, out IntIdle,
											out ExtMax, out ExtEmployed, out ExtIdle);
				}
				else
				{
					isPast = false;
					this.getStatsForDay(isPast, day, ViewingDev, out IntMax, out IntEmployed, out IntIdle,
											out ExtMax, out ExtEmployed, out ExtIdle);
				}

				maxYValue = Math.Max(ExtMax, maxYValue);
			}

			int maxVerticalDivisions = 8;
			double interval = Math.Ceiling(RoundToNiceInterval(maxYValue / (double) maxVerticalDivisions));
			int MaxRows = 1 + (int) Math.Ceiling(maxYValue / interval);
			maxYValue = (int) (interval * Math.Ceiling(maxYValue / interval));

			int gridOffsetX = 100;
			int gridOffsetY = graphTop;
			int gridCellWidth = (ExtStaffBox.Width - gridOffsetX) / MaxGameDays;
			int gridCellHeight = (int) (graphHeight / (MaxRows + 0.5));
			graphHeight = (int) (gridCellHeight * (MaxRows + 0.5));
			string id_str = string.Empty;

			GanttGridCellWidth = gridCellWidth;
			GanttGridCellHeight = gridCellHeight;
			GanttGridCellOffsetX = gridOffsetX;
			GanttGridCellOffsetY = gridOffsetY;

			// Draw the day legends and stripes.
			DrawRectangleWithStr(g, 0, dayStripTop, gridOffsetX, dayStripHeight, dayColour, dayTextColour, dayLabelFont, StringAlignment.Center, StringAlignment.Near, "Day");
			for (int day = 1; day <= GameConstants.MAX_NUMBER_DAYS; day++)
			{
				using (Brush brush = new SolidBrush(columnBackgroundColours[day % columnBackgroundColours.Length]))
				{
					g.FillRectangle(brush, gridOffsetX + ((day - 1) * gridCellWidth), graphTop, gridCellWidth, graphHeight);
				}
				DrawRectangleWithStr(g, gridOffsetX + ((day - 1) * gridCellWidth), dayStripTop, gridCellWidth, dayStripHeight, dayColour, Color.White, dayLegendFont, StringAlignment.Center, StringAlignment.Near, formatDaystr(day));
				g.DrawLine(ChartGridPen, gridOffsetX + ((day - 1) * gridCellWidth), dayStripTop, gridOffsetX + ((day - 1) * gridCellWidth), graphTop + graphHeight);
			}
			g.DrawLine(ChartGridPen, GameConstants.MAX_NUMBER_DAYS * gridCellWidth + gridOffsetX, gridOffsetY, GameConstants.MAX_NUMBER_DAYS * gridCellWidth + gridOffsetX, gridOffsetY + graphHeight);

			// Left key and horizontal lines.
			DrawRectangleWithStr(g, 0, dayStripTop + (dayStripHeight / 2), gridOffsetX, dayStripHeight / 2, contractorAxisColours[0], Color.White, yAxisTextFont, StringAlignment.Center, StringAlignment.Center, "CONTRACTORS");
			for (int step = 0; step <= MaxRows; step++)
			{
				int height = gridCellHeight;
				if (step == MaxRows)
				{
					height = gridCellHeight / 2;
				}

				if (step == 0)
				{
					g.DrawLine(Pens.Black, gridOffsetX, gridOffsetY + (step * gridCellHeight) + height, GameConstants.MAX_NUMBER_DAYS * gridCellWidth + gridOffsetX, gridOffsetY + (step * gridCellHeight) + height);
				}
				else 
				{
					g.DrawLine(ChartGridPen, gridOffsetX, gridOffsetY + (step * gridCellHeight) + height, GameConstants.MAX_NUMBER_DAYS * gridCellWidth + gridOffsetX, gridOffsetY + (step * gridCellHeight) + height);
				}

				DrawRectangleWithStr(g, 0, graphTop + (step * gridCellHeight), gridOffsetX, height, contractorAxisColours[(1 + step) % contractorAxisColours.Length], Color.White, yAxisNumberFont, StringAlignment.Center, StringAlignment.Near,
					CONVERT.ToStr((MaxRows - step) * interval));
				g.DrawLine(Pens.White, 0, gridOffsetY + (step * gridCellHeight), gridOffsetX, gridOffsetY + (step * gridCellHeight));
			}

			// Outline the axis legend.
			using (Pen pen = new Pen (Color.White, 2.0f))
			{
				g.DrawRectangle(pen, 0, ExtStaffBox.Top + (dayStripHeight / 2), gridOffsetX, (dayStripHeight / 2) + graphHeight + (dayStripHeight / 2));
			}

			// Draw the data.
			for (int day = 1; day <= GameConstants.MAX_NUMBER_DAYS; day++)
			{
				//=======================================================================================
				//==NEW SYSTEM===========================================================================
				//=======================================================================================
				isPast = (day <= DisplayCurrentDay);

				this.getStatsForDay(isPast, day, ViewingDev, out IntMax, out IntEmployed, out IntIdle,
						out ExtMax, out ExtEmployed, out ExtIdle);
				String display_value = CONVERT.ToStr(ExtEmployed + ExtIdle);

				//Just show the full staff total 
				int total_ext_staff = ExtEmployed + ExtIdle;
				int over_demand_ext_staff = total_ext_staff - ExtMax;  //If +ve then we have over booked.

				string ffg = "[isPast]" + isPast.ToString();
				ffg += "[day]" + day.ToString();
				ffg += "[ViewingDev]" + ViewingDev.ToString();
				ffg += "[IntMax]" + IntMax.ToString();
				ffg += "[IntEmployed]" + IntEmployed.ToString();
				ffg += "[IntIdle]" + IntEmployed.ToString();
				ffg += "[ExtMax]" + ExtMax.ToString();
				ffg += "[ExtEmployed]" + ExtEmployed.ToString();
				ffg += "[ExtIdle]" + ExtEmployed.ToString();
				ffg += "[disp]" + display_value.ToString();
				ffg += "[TES]" + total_ext_staff.ToString();
				ffg += "[OD]" + over_demand_ext_staff.ToString();
				



				if (total_ext_staff > 0)
				{
					int bottom = graphTop + graphHeight ;
					int height = (int)(((total_ext_staff / interval) + 0.5) * gridCellHeight) ;

					if (over_demand_ext_staff > 0)
					{
						height = (int)(((MaxRows) + 0.5) * gridCellHeight);
					}

					if (isPast)
					{
						using (Brush brush = new SolidBrush(staffBarColours_Past[day % staffBarColours_Past.Length]))
						{
							g.FillRectangle(brush, (day - 1) * gridCellWidth + gridOffsetX + 1, bottom - height, gridCellWidth - 2, height);
						}
					}
					else
					{
						using (Brush brush = new SolidBrush(staffBarColours_Future[day % staffBarColours_Future.Length]))
						{
							g.FillRectangle(brush, (day - 1) * gridCellWidth + gridOffsetX + 1, bottom - height, gridCellWidth - 2, height);
						}
					}
				}

				if (isEndRoundReport || isPast)
				{
					over_demand_ext_staff = getOrderedExcess(day, false, ExtMax);
					ffg += "[ODER]" + over_demand_ext_staff.ToString();
				}
				System.Diagnostics.Debug.WriteLine(ffg); 

				if (over_demand_ext_staff > 0)
				{
					//Draw the red back
					int gtop = graphTop;
					int block_height = (int)(gridCellHeight);
					g.FillRectangle(Brushes.Red, (day-1) * gridCellWidth + gridOffsetX, gtop, gridCellWidth, block_height);

					//Draw the Excess Demand
					if (compactMode)
					{
						g.DrawString("+" + CONVERT.ToStr(over_demand_ext_staff), MyDefaultSkinFontBold6, OverDemandTextBrush,
							(day - 1) * gridCellWidth + gridOffsetX + 0, gridOffsetY + (gridCellHeight / 2));
					}
					else
					{
						g.DrawString("+" + CONVERT.ToStr(over_demand_ext_staff), MyDefaultSkinFontBold6, OverDemandTextBrush,
							(day - 1) * gridCellWidth + gridOffsetX + 2, gridOffsetY + (gridCellHeight / 2));
					}
				}

				//====================
				//==DEBUG TEXT
				//====================
				if (show_resutil_debug)
				{
					this.getStatsForDay(true, day, ViewingDev, out IntMax, out IntEmployed, out IntIdle,
							out ExtMax, out ExtEmployed, out ExtIdle);
					String past_value = CONVERT.ToStr(ExtEmployed + ExtIdle);
					this.getStatsForDay(false, day, ViewingDev, out IntMax, out IntEmployed, out IntIdle,
							out ExtMax, out ExtEmployed, out ExtIdle);
					String future_value = CONVERT.ToStr(ExtEmployed + ExtIdle);

					g.DrawString("D" + display_value, MyDefaultSkinFontBold10, Brushes.Maroon,
						(day - 1) * gridCellWidth + gridOffsetX + 2, gridOffsetY + ((gridCellHeight * MaxRows) - 40));
					g.DrawString("P" + past_value, MyDefaultSkinFontBold10, Brushes.Maroon,
						(day - 1) * gridCellWidth + gridOffsetX + 2, gridOffsetY + ((gridCellHeight * MaxRows) - 20));
					g.DrawString("F" + future_value, MyDefaultSkinFontBold10, Brushes.Maroon,
						(day - 1) * gridCellWidth + gridOffsetX + 2, gridOffsetY + ((gridCellHeight * MaxRows) - 0));
				}
			}

			//Draw the Current Day Marker
			if ((DisplayCurrentDay >= 1) && (DisplayCurrentDay <= 28))
			{
				using (Brush brush = new SolidBrush (Color.FromArgb(50, 0, 0, 150)))
				{
					g.FillRectangle(brush,
									gridOffsetX + ((DisplayCurrentDay - 1) * gridCellWidth) + 1,
									dayStripTop + 1,
									gridCellWidth - 2,
									graphHeight + dayStripHeight - 2);
				}

				using (Brush brush = new SolidBrush (Color.FromArgb(0, 0, 150)))
				{
					StringFormat format = new StringFormat ();
					format.Alignment = StringAlignment.Center;
					format.LineAlignment = StringAlignment.Center;

					g.DrawString("Current Day", MyDefaultSkinFontBold10, brush,
								 gridOffsetX + ((DisplayCurrentDay - 1) * gridCellWidth) + (gridCellWidth / 2),
								 dayStripTop - 20, format);
				}
			}

			//Draw the Highlight day 
			if ((HighLightDay != -1) && ! compactMode)
			{
				g.DrawRectangle(PenHighLight, (HighLightDay * gridCellWidth + gridOffsetX),
					(gridOffsetY + gridCellHeight) - gridCellHeight, gridCellWidth - 0, (int) (gridCellHeight * (MaxRows + 0.5)));
				g.DrawRectangle(PenHighLight, (HighLightDay * gridCellWidth + gridOffsetX) + 1,
					((gridOffsetY + gridCellHeight) - gridCellHeight) + 1, gridCellWidth - 0 - 2, (int) (gridCellHeight * (MaxRows + 0.5)) - 2);
			}
		}

		/// <summary>
		/// This draws the chart showing the Utilisation of Internal staff (Staff)
		/// </summary>
		/// <param name="g"></param>
		/// <param name="test"></param>
		protected void DrawInternalStaffChart(Graphics g, bool test)
		{
			g.SmoothingMode = SmoothingMode.HighQuality;

			int legendTop = IntStaffBox.Top;
			int legendPlusGraphHeight = IntStaffBox.Height - (legendTop - IntStaffBox.Top);

			int IntMax = 0;
			int IntEmployed = 0;
			int IntIdle = 0;
			int ExtMax = 0;
			int ExtEmployed = 0;
			int ExtIdle = 0;

			int maxYValue = 5;
			bool isPast = false;

			// Get a scale for the y. (need the max)
			for (int day = 1; day <= GameConstants.MAX_NUMBER_DAYS; day++)
			{
				if (day <= DisplayCurrentDay)
				{
					isPast = true;
					this.getStatsForDay(isPast, day, ViewingDev, out IntMax, out IntEmployed, out IntIdle,
											out ExtMax, out ExtEmployed, out ExtIdle);
				}
				else
				{
					isPast = false;
					this.getStatsForDay(isPast, day, ViewingDev, out IntMax, out IntEmployed, out IntIdle,
											out ExtMax, out ExtEmployed, out ExtIdle);
				}
				//maxYValue = Math.Max(maxYValue, IntEmployed + IntIdle);
				maxYValue = Math.Max(IntMax, maxYValue);
			}

			int maxVerticalDivisions = 8;
			double interval = Math.Ceiling(RoundToNiceInterval(maxYValue / (double) maxVerticalDivisions));
			int MaxRows = 1 + (int) Math.Ceiling(maxYValue / interval);
			maxYValue = (int) (interval * Math.Ceiling(maxYValue / interval));

			int gridOffsetX = 100;
			int gridCellWidth = (IntStaffBox.Width - gridOffsetX) / MaxGameDays;
			int gridCellHeight = (int) (legendPlusGraphHeight / (MaxRows + 1));
			int graphHeight = (int) (gridCellHeight * (MaxRows + 0.5));
			int legendHeight = gridCellHeight / 2;
			int graphTop = legendTop + legendHeight;
			int gridOffsetY = graphTop;
			string id_str = string.Empty;

			CostGridCellWidth = gridCellWidth;
			CostGridCellHeight = gridCellHeight;
			CostGridCellOffsetX = gridOffsetX;
			CostGridCellOffsetY = gridOffsetY;

			int extraPad = 2;

			// Draw the vertical lines and columns.
			for (int day = 1; day <= GameConstants.MAX_NUMBER_DAYS; day++)
			{
				using (Brush brush = new SolidBrush(columnBackgroundColours[day % columnBackgroundColours.Length]))
				{
					g.FillRectangle(brush, gridOffsetX + ((day - 1) * gridCellWidth), graphTop, gridCellWidth, graphHeight);
				}
				g.DrawLine(ChartGridPen, gridOffsetX + ((day - 1) * gridCellWidth), graphTop, gridOffsetX + ((day - 1) * gridCellWidth), graphTop + graphHeight);
			}
			g.DrawLine(ChartGridPen, GameConstants.MAX_NUMBER_DAYS * gridCellWidth + gridOffsetX, graphTop, GameConstants.MAX_NUMBER_DAYS * gridCellWidth + gridOffsetX, graphTop + graphHeight);

			// Left key and horizontal lines.
			DrawRectangleWithStr(g, 0, legendTop, gridOffsetX, legendHeight, staffAxisColours[0], Color.White, yAxisTextFont, StringAlignment.Center, StringAlignment.Center, "INTERNAL");
			g.DrawLine(ChartGridPen, gridOffsetX, gridOffsetY, GameConstants.MAX_NUMBER_DAYS * gridCellWidth + gridOffsetX, gridOffsetY);
			for (int step = 0; step <= MaxRows; step++)
			{
				int height = gridCellHeight;
				if (step == MaxRows)
				{
					height = (gridCellHeight / 2) + extraPad;
				}
				if (step==0)
				{
					g.DrawLine(Pens.Black, gridOffsetX, gridOffsetY + (step * gridCellHeight) + height, GameConstants.MAX_NUMBER_DAYS * gridCellWidth + gridOffsetX, gridOffsetY + (step * gridCellHeight) + height);
				}
				else
				{
					g.DrawLine(ChartGridPen, gridOffsetX, gridOffsetY + (step * gridCellHeight) + height, GameConstants.MAX_NUMBER_DAYS * gridCellWidth + gridOffsetX, gridOffsetY + (step * gridCellHeight) + height);
				}

				if (step == 0)
				{
					DrawRectangle(g, 0, graphTop + (step * gridCellHeight), gridOffsetX, height, staffAxisColours[(1 + step) % staffAxisColours.Length]);
				}
				else
				{
					DrawRectangleWithStr(g, 0, graphTop + (step * gridCellHeight), gridOffsetX, height, staffAxisColours[(1 + step) % staffAxisColours.Length], Color.White, yAxisNumberFont, StringAlignment.Center, StringAlignment.Near,
						CONVERT.ToStr((MaxRows - step) * interval));
				}
				//was white
				g.DrawLine(Pens.White, 0, gridOffsetY + (step * gridCellHeight), gridOffsetX, gridOffsetY + (step * gridCellHeight));
			}
			

			// Draw the data.
			for (int day = 1; day <= GameConstants.MAX_NUMBER_DAYS; day++)
			{
				//=======================================================================================
				//==NEW SYSTEM===========================================================================
				//=======================================================================================
				isPast = (day <= DisplayCurrentDay);

				this.getStatsForDay(isPast, day, ViewingDev, out IntMax, out IntEmployed, out IntIdle,
						out ExtMax, out ExtEmployed, out ExtIdle);
				String display_value = CONVERT.ToStr(IntEmployed + IntIdle);

				//Just show the full staff total 
				int total_int_staff = IntEmployed + IntIdle;
				int over_demand_int_staff = total_int_staff - IntMax;  //If +ve then we have over booked.

				if (total_int_staff > 0)
				{
					int bottom = graphTop + graphHeight + extraPad;
					int height = (int)(((total_int_staff / interval) + 0.5) * gridCellHeight) + extraPad;

					if (over_demand_int_staff > 0)
					{
						height = (int)(((MaxRows) + 0.5) * gridCellHeight) + extraPad;
					}

					if (isPast)
					{
						using (Brush brush = new SolidBrush(staffBarColours_Past[day % staffBarColours_Past.Length]))
						{
							g.FillRectangle(brush, (day - 1) * gridCellWidth + gridOffsetX + 1, bottom - height, gridCellWidth - 2, height);
						}
					}
					else
					{
						using (Brush brush = new SolidBrush(staffBarColours_Future[day % staffBarColours_Future.Length]))
						{
							g.FillRectangle(brush, (day - 1) * gridCellWidth + gridOffsetX + 1, bottom - height, gridCellWidth - 2, height);
						}
					}
				}

				if (isEndRoundReport || isPast)
				{
					over_demand_int_staff = getOrderedExcess(day, true, IntMax);
				}

				if (over_demand_int_staff > 0)
				{
					//Draw the red back
					int gtop = graphTop;
					int block_height = (int)(gridCellHeight);
					g.FillRectangle(Brushes.Red, (day - 1) * gridCellWidth + gridOffsetX, gtop, gridCellWidth, block_height);

					//Draw the Excess Demand
					g.DrawString("+" + CONVERT.ToStr(over_demand_int_staff), MyDefaultSkinFontBold10, OverDemandTextBrush,
						(day - 1) * gridCellWidth + gridOffsetX + 2, gridOffsetY + (gridCellHeight / 2));
				}

				//====================
				//==DEBUG TEXT
				//====================
				if (show_resutil_debug)
				{
					this.getStatsForDay(true, day, ViewingDev, out IntMax, out IntEmployed, out IntIdle,
							out ExtMax, out ExtEmployed, out ExtIdle);
					String past_value = CONVERT.ToStr(IntEmployed + IntIdle);
					this.getStatsForDay(false, day, ViewingDev, out IntMax, out IntEmployed, out IntIdle,
							out ExtMax, out ExtEmployed, out ExtIdle);
					String future_value = CONVERT.ToStr(IntEmployed + IntIdle);

					g.DrawString("D" + display_value, MyDefaultSkinFontBold10, Brushes.Teal,
						(day - 1) * gridCellWidth + gridOffsetX + 2, gridOffsetY + ((gridCellHeight * MaxRows) - 40));
					g.DrawString("P" + past_value, MyDefaultSkinFontBold10, Brushes.Teal,
						(day - 1) * gridCellWidth + gridOffsetX + 2, gridOffsetY + ((gridCellHeight * MaxRows) - 20));
					g.DrawString("F" + future_value, MyDefaultSkinFontBold10, Brushes.Teal,
						(day - 1) * gridCellWidth + gridOffsetX + 2, gridOffsetY + ((gridCellHeight * MaxRows) - 0));
				}
			}

			//Draw the Current Day Marker
			if ((DisplayCurrentDay >= 1) && (DisplayCurrentDay <= 28))
			{
				using (Brush brush = new SolidBrush (Color.FromArgb(50, 0, 0, 150)))
				{
					g.FillRectangle(brush,
									gridOffsetX + ((DisplayCurrentDay - 1) * gridCellWidth) + 1,
									(gridOffsetY + gridCellHeight) - gridCellHeight + 1,
									gridCellWidth - 2,
									graphHeight);
				}
			}

			//Draw the Highlight day 
			if ((HighLightDay != -1) && ! compactMode)
			{
				g.DrawRectangle(PenHighLight, (HighLightDay * gridCellWidth + gridOffsetX),
					(gridOffsetY + gridCellHeight) - gridCellHeight, gridCellWidth - 0, (int) (gridCellHeight * (MaxRows + 0.5)) + extraPad);
				g.DrawRectangle(PenHighLight, (HighLightDay * gridCellWidth + gridOffsetX) + 1,
					((gridOffsetY + gridCellHeight) - gridCellHeight) + 1, gridCellWidth - 0 - 2, ((int) (gridCellHeight * (MaxRows + 0.5))) - 2 + extraPad);
			}
		}

		#endregion Chart Drawing Methods 
 
		public void SetCompactMode (bool compact)
		{
			dayLegendFont.Dispose();
			dayLabelFont.Dispose();
			yAxisNumberFont.Dispose();
			yAxisTextFont.Dispose();
			if (compact)
			{
				dayLegendFont = ConstantSizeFont.NewFont(SkinningDefs.TheInstance.GetData("fontname"), 6.5f);
				dayLabelFont = ConstantSizeFont.NewFont(SkinningDefs.TheInstance.GetData("fontname"), 8, FontStyle.Bold);
				yAxisNumberFont = ConstantSizeFont.NewFont(SkinningDefs.TheInstance.GetData("fontname"), 6, FontStyle.Bold);
				yAxisTextFont = ConstantSizeFont.NewFont(SkinningDefs.TheInstance.GetData("fontname"), 6, FontStyle.Bold);
			}
			else
			{
				dayLegendFont = ConstantSizeFont.NewFont(SkinningDefs.TheInstance.GetData("fontname"), 10, FontStyle.Bold);
				dayLabelFont = ConstantSizeFont.NewFont(SkinningDefs.TheInstance.GetData("fontname"), 10, FontStyle.Bold);
				yAxisNumberFont = ConstantSizeFont.NewFont(SkinningDefs.TheInstance.GetData("fontname"), 10, FontStyle.Bold);
				yAxisTextFont = ConstantSizeFont.NewFont(SkinningDefs.TheInstance.GetData("fontname"), 8, FontStyle.Bold);
			}

			this.compactMode = compact;

			Invalidate();
		}

		#region Generic Paint Methods

		/// <summary>
		/// As we can choose which graphs are shown, we need to calculate the regions  
		/// </summary>
		protected void BuildBoundaryBoxes()
		{
			DrawingBox.X = 0;
			DrawingBox.Y = 30 + DisplayTitleHeight + 10;
			DrawingBox.Width = this.Width;

			int yGap = 10;

			if (compactMode)
			{
				DisplayTitleHeight = 0;
				DrawingBox.Y = DisplayTitleHeight;
			}

			DrawingBox.Height = this.Height - DrawingBox.Y;

			DrawProjectInKeyArea = false;
			ExtStaffBox.X = 0;
			ExtStaffBox.Y = DrawingBox.Top;
			ExtStaffBox.Width = DrawingBox.Width - (2 * ExtStaffBox.X);
			ExtStaffBox.Height = (DrawingBox.Height - yGap) / 2;

			IntStaffBox.X = 0;
			IntStaffBox.Y = ExtStaffBox.Bottom + yGap;
			IntStaffBox.Width = DrawingBox.Width - (2 * IntStaffBox.X);
			IntStaffBox.Height = DrawingBox.Height - IntStaffBox.Top;
		}

		void DrawRectangleWithStr(Graphics graphics, int left, int top, int width, int height,
							Color background, Color foreground, Font font,
							StringAlignment horizontalAlign, StringAlignment verticalAlign,
							string text)
		{
			using (Brush brush = new SolidBrush(background))
			{
				graphics.FillRectangle(brush, left, top, width, height);
			}
			using (Brush brush = new SolidBrush(foreground))
			{
				RectangleF rectangle = new RectangleF(left, top, width, height);
				StringFormat format = new StringFormat();
				format.Alignment = horizontalAlign;
				format.LineAlignment = verticalAlign;
				graphics.DrawString(text, font, brush, rectangle, format);
			}
		}

		void DrawRectangle(Graphics graphics, int left, int top, int width, int height, Color background)
		{
			using (Brush brush = new SolidBrush(background))
			{
				graphics.FillRectangle(brush, left, top, width, height);
			}
		}

		/// <summary>
		/// Given an interval for a graph, rounds it up to the next "nice" figure (ie 10^n * {1, 2, 2.5 or 5}).
		/// </summary>
		double RoundToNiceInterval(double start)
		{
			int exponent = (int)Math.Floor(Math.Log10(start));

			double radix = start / Math.Pow(10, exponent);

			if (radix <= 1)
			{
				radix = 1;
			}
			else if (radix <= 2)
			{
				radix = 2;
			}
			else if (radix <= 2.5)
			{
				radix = 2.5;
			}
			else if (radix <= 5)
			{
				radix = 5;
			}
			else
			{
				radix = 10;
			}

			return radix * Math.Pow(10, exponent);
		}

		protected void DrawHighLight(int NewHighlightDay)
		{
			if (! compactMode)
			{
				this.HighLightDay = NewHighlightDay;
				Refresh();
			}
		}

		protected void DrawTitle(Graphics g, int OffsetX, int OffsetY)
		{
			string ProjectTitle = string.Empty;
			//if (this.MyNodeTree != null)
			{
				ProjectTitle = "Department Resource Utilization Chart";
				SizeF textsize = g.MeasureString(ProjectTitle ,MyDefaultSkinFontBold12);
				int centre_offset = ((int)textsize.Width)/2;
				g.DrawString(ProjectTitle, MyDefaultSkinFontBold12, Brushes.Black,  OffsetX-centre_offset, OffsetY);
			}
		}

		protected void DrawStats (Graphics g, int OffsetX, int OffsetY)
		{
			string DayStats = "";
			int IntMax = 0;
			int IntEmployed = 0;
			int IntIdle = 0;
			int ExtMax = 0;
			int ExtEmployed = 0;
			int ExtIdle = 0;
			SizeF textsize = new SizeF(0, 0);
			bool isPast = true;

			if (this.HighLightDay != -1)
			{
				if ((this.HighLightDay+1) > DisplayCurrentDay)
				{
					isPast = false;
				}
				this.getStatsForDay(isPast, this.HighLightDay+1, ViewingDev, out IntMax, out IntEmployed, out IntIdle,
						out ExtMax, out ExtEmployed, out ExtIdle);

				int total_int_staff = IntEmployed + IntIdle;
				int total_ext_staff = ExtEmployed + ExtIdle;

				int internalExtra = getOrderedExcess(HighLightDay + 1, true, IntMax);
				int externalExtra = getOrderedExcess(HighLightDay + 1, false, ExtMax);

				total_int_staff += internalExtra;
				total_ext_staff += externalExtra;

				DayStats += "Day ";
				DayStats += CONVERT.ToStr(this.HighLightDay + 1);
				if (total_int_staff > 0)
				{
					DayStats += " Internal (" + CONVERT.ToStr(total_int_staff) + " required from " + CONVERT.ToStr(IntMax) + ")";
				}
				if (total_ext_staff > 0)
				{
					DayStats += " Contractors (" + CONVERT.ToStr(total_ext_staff) + " required from " + CONVERT.ToStr(ExtMax) + ")";
				}
				textsize = g.MeasureString(DayStats, MyDefaultSkinFontBold10);
			}
			g.DrawString(DayStats, MyDefaultSkinFontBold10, Brushes.Black, OffsetX - ((int) textsize.Width) - 10, OffsetY);
		}

		private void GamePlanPanel_Paint (object sender, System.Windows.Forms.PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			g.SmoothingMode = SmoothingMode.HighQuality;

			BuildBoundaryBoxes();

			e.Graphics.FillRectangle(backBrush, 0, 0, this.Width, this.Height);

				if (DrawProjectInKeyArea == false)
				{
					DrawTitle(e.Graphics, this.Width / 2, 30);
					DrawStats(e.Graphics, IntStaffBox.Right - 100, IntStaffBox.Bottom + 10);
				}


			this.DrawExternalStaffChart(e.Graphics, !ViewingDev);
			this.DrawInternalStaffChart(e.Graphics, !ViewingDev);
		}

		#endregion Generic Paint Methods

		#region Highlight Region  
		
		/// <summary>
		/// This is used to determine where the user clicked, to move the highlighted day 
		/// </summary>
		/// <param name="mx"></param>
		/// <param name="my"></param>
		/// <param name="GridCellOffsetX"></param>
		/// <param name="GridCellOffsetY"></param>
		/// <param name="GridCellWidth"></param>
		/// <param name="GridCellHeight"></param>
		/// <param name="cellx"></param>
		/// <param name="celly"></param>
		/// <returns></returns>
		private Boolean ResolveHitPoint(int mx, int my, int GridCellOffsetX, int GridCellOffsetY, 
			int GridCellWidth, int GridCellHeight, out int cellx, out int celly)
		{
			int HitPointX=0;
			int HitPointY=0;
			int HitCellX=-1;
			int HitCellY=-1;
			Boolean OpSuccess = false;

			//System.Diagnostics.Debug.WriteLine("HitPointX:"+HitPointX.ToString()+" HitPointY:"+HitPointY.ToString());

			HitPointX = mx - GridCellOffsetX;
			HitPointY = my - GridCellOffsetY;
			cellx = 0;
			celly = 0;

			//prevent crash on a div by zero (clicking on a empty screen haven't played the round)
			if ((GridCellWidth > 0) & (GridCellHeight > 0))
			{ 
				if ((HitPointX >-1)&(HitPointY > -1))
				{
					HitCellX = HitPointX / GridCellWidth;
					HitCellY = HitPointY / GridCellHeight; 

					//System.Diagnostics.Debug.WriteLine("HitPointX:"+HitPointX.ToString()+" HitPointY:"+HitPointY.ToString());
					//System.Diagnostics.Debug.WriteLine("CostGridCellWidth:"+CostGridCellWidth.ToString()+" HitPointHeight:"+CostGridCellHeight.ToString());
					//System.Diagnostics.Debug.WriteLine("HitCellX:"+HitCellX.ToString()+" HitCellY:"+HitCellY.ToString());
					cellx = HitCellX;
					celly = HitCellY;

					// the columns are limited to 0 and 29 
					if ((cellx > -1)& (cellx<GameConstants.MAX_NUMBER_DAYS))
					{
						OpSuccess = true;
					}
				}
			}
			return OpSuccess;
		}

		private void GamePlanPanel_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			if (compactMode)
			{
				return;
			}

			int HitPointX=0;
			int HitPointY=0;
			int cellx =0;
			int celly =0;
			Boolean proceed = false;

			if (e.Button != MouseButtons.Left) return;
				
			HitPointX = e.X;
			HitPointY = e.Y;
			
			if (IntStaffBox.Contains(HitPointX,HitPointY))
			{
				if (ResolveHitPoint(HitPointX, HitPointY, CostGridCellOffsetX, CostGridCellOffsetY, 
					CostGridCellWidth, CostGridCellHeight, out cellx, out celly))
				{
					proceed = true;
				}
			}
			if (ExtStaffBox.Contains(HitPointX,HitPointY))
			{
				if (ResolveHitPoint(HitPointX, HitPointY, GanttGridCellOffsetX, GanttGridCellOffsetY, 
					GanttGridCellWidth, GanttGridCellHeight, out cellx, out celly))
				{
					proceed = true;
				}
			}
			//
			if (proceed)
			{
				DrawHighLight(cellx);
			}
		}

		#endregion Highlight Region  

	}
}