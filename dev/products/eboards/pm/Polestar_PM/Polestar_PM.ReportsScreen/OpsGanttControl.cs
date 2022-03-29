using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Text;

using CommonGUI;
using ChartScreens;
using ReportBuilder;
using LibCore;
using Network;
using Charts;
using System.Collections;
using Logging;
using GameManagement;
using CoreUtils;
using Polestar_PM.OpsGUI;
using Polestar_PM.DataLookup;

namespace Polestar_PM.ReportsScreen
{
	public enum emGanttChartDayType
	{
		DEFAULT,
		MONEYDAY,
		INSTALLING,
		INSTALL_OK,
		INSTALL_FAIL,
		COMPLETE,
	}

	/// <summary>
	/// The OpsGanttControl shows a Gannt based view of the Various Prjects and the Ops Change information
	/// In the lower half, we show the OpS Changes (Change items, Memory Upgrades and Storage Upgrades)
	/// In the upper half, we show the only install attempst and go live days of the Projects
	/// 
	/// The code was orignally designed to show all stages but this is not currently required (just the installs)
	/// 
	/// </summary>
	public class OpsGanttControl : System.Windows.Forms.UserControl
	{
		NetworkProgressionGameFile gameFile;

		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		//private int MaxGameDays = 25;
		private int MaxGameDays = GameConstants.MAX_NUMBER_DAYS;

		private Font font = CoreUtils.SkinningDefs.TheInstance.GetFont(10f, FontStyle.Bold);

		Color dayColour = Color.FromArgb(255, 255, 255);
		Color dayTextColour = Color.FromArgb(0,0,0);

		Color changeFreezeColour = Color.FromArgb(192, 192, 192);
		Color changeFreezeTextColour = Color.FromArgb(128,128,128);
		Color changeFreezeBorderColour = Color.FromArgb(0, 66, 109);
		Color [] columnColours = new Color[] { Color.White, Color.FromArgb(241, 242, 242) };
		Color[] projectColours = new Color[] { Color.FromArgb(96, 96, 96), Color.FromArgb(112, 112, 112) };
		Color[] opsColours = new Color[] { Color.FromArgb(96, 96, 96), Color.FromArgb(112, 112, 112) };

		private Color colorRed = Color.Red;
		private Color colorBlack = Color.Black;
		private Color colorDarkGrey = Color.DarkGray;
		private Brush brushWhite = new SolidBrush(Color.White);
		private Brush brushRed = new SolidBrush(Color.Red);
		private Brush brushSkyBlue = new SolidBrush(Color.SkyBlue);
		private Brush brushKhaki = new SolidBrush(Color.Khaki);
		private Brush brushPink = new SolidBrush(Color.Pink);
		private Brush brushSalmon = new SolidBrush(Color.Salmon);
		private Brush brushSilver = new SolidBrush(Color.Silver);
		private Brush brushBlack = new SolidBrush(Color.Black);
		private Brush brushGreen = new SolidBrush(Color.Green);

		private Brush brushMem = new SolidBrush(Color.CornflowerBlue);
		private Brush brushStorage = new SolidBrush(Color.LightSkyBlue);
		private Brush brushFSC_pass = new SolidBrush(Color.MediumAquamarine);
		private Brush brushFSC_fail = new SolidBrush(Color.Red);
		private Pen tmpPenActual = new Pen(Color.Salmon);

		Color FSCBorder = Color.DarkCyan;
		int FSCBorderThickness = 3;

		private Pen tmpPenRed = new Pen(Color.Red);
		private Brush brushProjectsSideBarBack = new SolidBrush(Color.Gainsboro);
		private Brush brushOperatsSideBarBack = new SolidBrush(Color.Khaki);
		private Brush brushCannedSideBarBack = new SolidBrush(Color.Khaki);

		private System.Drawing.Color framecolor = System.Drawing.Color.Blue;
		private int DisplayRound =1;
		private Point[] DiamondPts = new Point[5];

		protected NodeTree MyNodeTree = null;
		protected Node opswork_node = null;
		protected Node projectsNode = null;
		protected Hashtable opworkitemsByDay = new Hashtable();
		protected bool old_fixed_blocked_days = false;
		protected bool display_seven_projects = false;

		struct OpsActivity
		{
			public string Activity;
			public string Location;
			public bool Success;
		}

		Dictionary<int, string> projectNameBySlotNumber;
		Dictionary<string, Dictionary<double, string>> plannedStateNameByTimeByProjectName;
		Dictionary<string, Dictionary<double, string>> stateNameByTimeByProjectName;
		Dictionary<string, Dictionary<int, string>> InstallDataByTimeByProjectName;
		Dictionary<string, Dictionary<int, string>> TooEarlyInstallDataByTimeByProjectName;
		Dictionary<double, OpsActivity> opsActivityByTime;

		public OpsGanttControl(NetworkProgressionGameFile gameFile, NodeTree model, int round)
		{
			Node projectsNode = model.GetNamedNode("pm_projects_running");
			display_seven_projects = projectsNode.GetBooleanAttribute("display_seven_projects", false);
			projectsNode = null;

			this.gameFile = gameFile;
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
			// TODO: Add any initialization after the InitializeComponent call
			SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.DoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
			//Help object for drawing Diamonds
			DiamondPts[0] = new Point(0,0);
			DiamondPts[1] = new Point(0,0);
			DiamondPts[2] = new Point(0,0);
			DiamondPts[3] = new Point(0,0);
			DiamondPts[4] = new Point(0,0);

			SetRound(round);
			setModel(model);
		}

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		public void setModel(NodeTree _model)
		{
			MyNodeTree = _model;
			projectsNode = MyNodeTree.GetNamedNode("pm_projects_running");
			ClearData();
			ExtractData();
		}

		private void ClearData()
		{
			opworkitemsByDay.Clear();
			opswork_node = null;
		}

		private void ExtractData()
		{
			ExtractLog();
		}

		/// <summary>
		/// This handles the display of the blocking days 
		/// This day is Zero based but the action day starts at 1 
		/// </summary>
		/// <param name="day"></param>
		/// <returns></returns>
		private bool getOperationsActionsOnDay(int day, out string activity, out string location, out bool success)
		{
			return getOperationsActivityNote(day+1, out activity, out location, out success);
		}

		bool getOperationsActivityNote (int Day, out string activity, out string location, out bool success)
		{
			bool found_one = false;

			activity = "";
			location = "";
			success = false;

			List<double> sortedTimes = new List<double> (opsActivityByTime.Keys);
			sortedTimes.Sort();

			foreach (double time in sortedTimes)
			{
				if ((time >= ((Day - 1) * 60)) && (time < (Day * 60)))
				{
					OpsActivity opsActivity = opsActivityByTime[time];
					activity = opsActivity.Activity;
					success = opsActivity.Success;
					location = opsActivity.Location;
					found_one = true;
				}
			}

			return found_one;
		}

		/// <summary>
		/// This handles the display of the blocking days 
		/// This day is Zero based but the action day starts at 1 
		/// </summary>
		/// <param name="day"></param>
		/// <returns></returns>
		private string getOpsBlockActionOnDay(int day)
		{
			string action="";
			//do we have a blocking day on this day 
			Node ops_worklist_node = this.MyNodeTree.GetNamedNode("ops_worklist"); 
			if (ops_worklist_node != null)
			{
				foreach (Node item_node in ops_worklist_node.getChildren())
				{
					string job_type = item_node.GetAttribute("type");
					string job_action = item_node.GetAttribute("action");
					int action_day = item_node.GetIntAttribute("day",-1);

					if (action_day != -1)
					{
						//The item in question has a defined Day 
						//we are only showing the external blocking days at the moment
						if (action_day == (day+1))
						{
							if (job_action.ToLower()=="blockday")
							{
								action = job_action;
							}
							//we are not considering Change Cards at this time 
							//							if (job_action.ToLower()=="install_cc_app")
							//							{
							//								action = job_action;
							//							}
						}
					}
					else
					{
						//The item in question has NO defined Day (Upgrades, FSC)
						//we dont show these as blocking at this time 
					}
				}
			}
			return action;
		}

		private int getProjectNumber(int slot)
		{
			int project_number = 0;
			foreach (Node prjnode in this.projectsNode.getChildren())
			{
				if (prjnode != null)
				{
					ProjectReader pr = new ProjectReader(prjnode);
					int slot_id = pr.getProjectSlot();
					if (slot_id == (slot))
					{
						project_number = pr.getProjectID();
					}
					pr.Dispose();
				}
			}
			return project_number;
		}

		private bool isProjectCompleted(int slot)
		{
			bool check_state = false;

			foreach (Node prjnode in this.projectsNode.getChildren())
			{
				if (prjnode != null)
				{
					ProjectReader pr = new ProjectReader(prjnode);
					int slot_id = pr.getProjectSlot();
					if (slot_id == (slot))
					{
						bool installed_ok = pr.InState(emProjectOperationalState.PROJECT_STATE_INSTALLED_OK);
						bool completed = pr.InState(emProjectOperationalState.PROJECT_STATE_COMPLETED);
						if (installed_ok | completed)
						{
							check_state = true;
						}
					}
					pr.Dispose();
				}
			}
			return check_state;
		}

		private string getProjectInstallLocation(int slot)
		{
			string install_location = "";

			foreach (Node prjnode in this.projectsNode.getChildren())
			{
				if (prjnode != null)
				{
					ProjectReader pr = new ProjectReader(prjnode);
					int slot_id = pr.getProjectSlot();
					if (slot_id == (slot))
					{
						install_location = pr.getInstallLocation();
					}
					pr.Dispose();
				}
			}
			return install_location;
		}

		private string getProjectInstallLocation(int PrjStepper, int step)
		{
			string location = "";
			if (projectNameBySlotNumber.ContainsKey(PrjStepper))
			{
				string name = projectNameBySlotNumber[PrjStepper];
				if (InstallDataByTimeByProjectName.ContainsKey(name))
				{
					Dictionary<int, string> stateByTime = InstallDataByTimeByProjectName[name];

					if (stateByTime.ContainsKey(step + 1))
					{
						location = stateByTime[step + 1];
					}
				}
			}
			return location;
		}

		private string getProjectTooEarlyLocation(int PrjStepper, int step)
		{
			string location = "";
			if (projectNameBySlotNumber.ContainsKey(PrjStepper))
			{
				string name = projectNameBySlotNumber[PrjStepper];
				if (TooEarlyInstallDataByTimeByProjectName.ContainsKey(name))
				{
					Dictionary<int, string> stateByTime = TooEarlyInstallDataByTimeByProjectName[name];

					if (stateByTime.ContainsKey(step + 1))
					{
						location = stateByTime[step + 1];
					}
				}
			}
			return location;
		}

		private string getProjectActualPhaseByProjectByDay (int PrjStepper, int step)
		{
			emProjectOperationalState state = emProjectOperationalState.PROJECT_STATE_UNKNOWN;
			if (projectNameBySlotNumber.ContainsKey(PrjStepper))
			{
				string name = projectNameBySlotNumber[PrjStepper];
				if (stateNameByTimeByProjectName.ContainsKey(name))
				{
					Dictionary<double, string> stateByTime = stateNameByTimeByProjectName[name];
					List<double> orderedTimes = new List<double>(stateByTime.Keys);
					orderedTimes.Sort();

					foreach (double time in orderedTimes)
					{
						if (time < ((step + 1) * 60))
						{
							emProjectOperationalState newState = (emProjectOperationalState) Enum.Parse(typeof(emProjectOperationalState), stateByTime[time]);
							//System.Diagnostics.Debug.WriteLine("" + time.ToString() + "  " + (stateByTime[time]).ToString() + "  " + newState.ToString());

							// Most states persist until the next state change, but installation states only last a day.
							bool statePersists = true;
							switch (newState)
							{
								case emProjectOperationalState.PROJECT_STATE_INSTALL_NO_LOCATION:
								case emProjectOperationalState.PROJECT_STATE_INSTALLED_FAIL:
								case emProjectOperationalState.PROJECT_STATE_INSTALLED_OK:
								case emProjectOperationalState.PROJECT_STATE_CANCELLED:
								case emProjectOperationalState.PROJECT_STATE_COMPLETED:
									statePersists = false;
									break;
							}

							if ((time >= (step * 60)) || statePersists)
							{
								state = newState;
							}
							else
							{
								state = emProjectOperationalState.PROJECT_STATE_UNKNOWN;
							}
						}
						else
						{
							break;
						}
					}
				}
			}
			string code = GameConstants.ProjectShortCodeFromState(state);
			//System.Diagnostics.Debug.WriteLine("Code " + code + "    "+ state.ToString());
			return code;
		}

		private string getProjectPlannedPhaseByProjectByDay (int PrjStepper, int step)
		{
			emProjectOperationalState state = emProjectOperationalState.PROJECT_STATE_UNKNOWN;

			//establish state at the step point (step is the day number)
			if (projectNameBySlotNumber.ContainsKey(PrjStepper))
			{
				string name = projectNameBySlotNumber[PrjStepper];
				if (plannedStateNameByTimeByProjectName.ContainsKey(name))
				{
					Dictionary<double, string> plannedStateByTime = plannedStateNameByTimeByProjectName[name];
					List<double> orderedTimes = new List<double>(plannedStateByTime.Keys);
					orderedTimes.Sort();

					foreach (double time in orderedTimes)
					{
						if (time <= ((step + 1) * 60))
						{
							if (time > (step * 60))
							{
								state = (emProjectOperationalState) Enum.Parse(typeof(emProjectOperationalState), plannedStateByTime[time]);
							}
						}
						else
						{
							break;
						}
					}
				}
			}
			//Determine the code for the state 
			string code = GameConstants.ProjectShortCodeFromState(state);
			return code;
		}

		#region Component Designer generated code
		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.Name = "OpsGanttControl";
			this.Size = new System.Drawing.Size(736, 600);
			this.Paint += new System.Windows.Forms.PaintEventHandler(this.OpsGanttControl_Paint);

		}
		#endregion

		#region Text Utils

		public void drawTowerText2(Graphics g, float x, float y, string text)
		{
			Font textF = CoreUtils.SkinningDefs.TheInstance.GetFont(10F, FontStyle.Bold);
			StringFormat sf = new StringFormat();
			sf.Alignment = StringAlignment.Center;
			string t= string.Empty;
			for (int step=0;step<8;step++)
			{
				if (step < text.Length)
				{
					t = (text.Substring(step,1)).ToUpper();	
					g.DrawString(t, textF, brushBlack, x, y, sf);
					y=y+13;
				}
			}
			textF.Dispose();
		}

		public void drawVerticalText(Graphics g, float x, float y, string text)
		{
			//string drawString = "Sample Text";
			System.Drawing.Font drawFont = CoreUtils.SkinningDefs.TheInstance.GetFont(14, FontStyle.Bold);
			System.Drawing.SolidBrush drawBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Black);
			System.Drawing.StringFormat drawFormat = new System.Drawing.StringFormat(StringFormatFlags.DirectionVertical);
			g.DrawString(text, drawFont, drawBrush, x, y, drawFormat);
			drawFont.Dispose();
			drawBrush.Dispose();
		}

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

		#region Set Methods

		public void SetRound(int Round)
		{
			DisplayRound = Round;
		}

		#endregion Set Methods

		#region utils 

		private Boolean isDay(string ActivityStr, string ActivityDefinition)
		{
			Boolean Exists= false;

			if (ActivityStr.IndexOf(ActivityDefinition)>-1)
			{
				Exists = true;
			}
			return Exists;
		}

		private Brush GetBrushForLetter(string ActivityStr) 
		{
			if (ActivityStr.IndexOf("1")>-1)
			{
				return brushSkyBlue;
			}
			if (ActivityStr.IndexOf("2")>-1)
			{
				return brushSkyBlue;
			}
			if (ActivityStr.IndexOf("3")>-1)
			{
				return brushSkyBlue;
			}
			if (ActivityStr.IndexOf("4")>-1)
			{
				return brushSkyBlue;
			}
			if (ActivityStr.IndexOf("5")>-1)
			{
				return brushSkyBlue;
			}
			if (ActivityStr.IndexOf("6")>-1)
			{
				return brushSkyBlue;
			}
			if (ActivityStr.IndexOf("L")>-1)
			{
				return brushGreen;
			}
			return brushGreen;
		}

		private string getLocationZoneLetter(string targetlocation)
		{ 
			string location_zone = "";
			if (targetlocation.Length > 0)
			{
				location_zone = targetlocation.Substring(0, 1);
			}
			return location_zone;
		}

		private string getLocationNumber(string targetlocation)
		{
			string location_number= "";
			if (targetlocation.Length > 3)
			{
				location_number = targetlocation.Substring(1, 3);
			}
			return location_number;
		}

		private emGanttChartDayType getChartDayType (string dayCode)
		{
			emGanttChartDayType DayTypeValue = emGanttChartDayType.DEFAULT;

			if (string.IsNullOrEmpty(dayCode) == false)
			{
				if (isDay(dayCode, GameConstants.ACTIVITY_NOMONEY))
				{
					DayTypeValue = emGanttChartDayType.MONEYDAY;
				}
				else
				{
					if (isDay(dayCode, GameConstants.ACTIVITY_INSTALL_FAIL))
					{
						DayTypeValue = emGanttChartDayType.INSTALL_FAIL;
					}
					if (isDay(dayCode, GameConstants.ACTIVITY_INSTALLING))
					{
						DayTypeValue = emGanttChartDayType.INSTALLING;
					}
					if (isDay(dayCode, GameConstants.ACTIVITY_INSTALLED))
					{
						DayTypeValue = emGanttChartDayType.INSTALL_OK;
					}
					if (isDay(dayCode, GameConstants.ACTIVITY_COMPLETE))
					{
						DayTypeValue = emGanttChartDayType.COMPLETE;
					}
				}
			}
			return DayTypeValue;
		}

		#endregion utils 

		public void DrawKey(Graphics g, int KeyOffsetX, int KeyOffsetY)
		{
			int DiamondSize= 6;
				
			try
			{
				//=========================================================================
				//==Draw the Gantt Key Information=========================================
				//=========================================================================
				g.DrawString("Key", font, brushBlack, KeyOffsetX + 5, KeyOffsetY+5);
				g.DrawString("- Successful Install", font, brushBlack, KeyOffsetX + 60, KeyOffsetY+5);
				g.DrawString("- Failed Install", font, brushBlack, KeyOffsetX + 60, KeyOffsetY+22);
				g.DrawString("- Go Live Day", font, brushBlack, KeyOffsetX + 60, KeyOffsetY+39);
				
				g.FillRectangle(brushGreen, KeyOffsetX + 50, KeyOffsetY+5+2,10,10);
				g.FillRectangle(brushRed, KeyOffsetX + 50, KeyOffsetY+22+2,10,10);

				g.FillRectangle(brushFSC_pass, KeyOffsetX + 220, KeyOffsetY+5+2,10,10);
				int thickness = (int) Math.Ceiling(FSCBorderThickness / 2.0);
				using (Brush brush = new SolidBrush (FSCBorder))
				{
					g.FillRectangle(brush, KeyOffsetX + 220, KeyOffsetY + 5 + 2, 10, thickness);
					g.FillRectangle(brush, KeyOffsetX + 220, KeyOffsetY + 5 + 2, thickness, 10);
					g.FillRectangle(brush, KeyOffsetX + 220 + 10 - thickness, KeyOffsetY + 5 + 2, thickness, 10);
					g.FillRectangle(brush, KeyOffsetX + 220, KeyOffsetY + 5 + 2 + 10 - thickness, 10, thickness);
				}

				g.FillRectangle(brushMem, KeyOffsetX + 220, KeyOffsetY+22+2,10,10);
				g.FillRectangle(brushStorage, KeyOffsetX + 220, KeyOffsetY+39+2,10,10);

				g.DrawString("- Change", font, brushBlack, KeyOffsetX + 230, KeyOffsetY+5);
				g.DrawString("- Memory Upgrade", font, brushBlack, KeyOffsetX + 230, KeyOffsetY+22);
				g.DrawString("- Storage Upgrade", font, brushBlack, KeyOffsetX + 230, KeyOffsetY+39);

				//=========================================================================
				//==Draw the Diamonds====================================================== 
				//=========================================================================
				//Reusing the Points Array
				DiamondPts[0].X = KeyOffsetX + 200-145;
				DiamondPts[0].Y = KeyOffsetY + 40;
				DiamondPts[1].X = KeyOffsetX + 200+DiamondSize-145;
				DiamondPts[1].Y = KeyOffsetY + 40+DiamondSize;
				DiamondPts[2].X = KeyOffsetX + 200-145;
				DiamondPts[2].Y = KeyOffsetY + 40+(DiamondSize*2);
				DiamondPts[3].X = KeyOffsetX + 200-DiamondSize-145;
				DiamondPts[3].Y = KeyOffsetY + 40+DiamondSize;
				DiamondPts[4].X = KeyOffsetX + 200-145;
				DiamondPts[4].Y = KeyOffsetY + 40;
				g.FillPolygon(brushBlack, DiamondPts,System.Drawing.Drawing2D.FillMode.Alternate);
			}
			catch (Exception evc)
			{
				string st1= evc.Message;
			}
		}

		private void OpsGanttControl_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			g.SmoothingMode = SmoothingMode.HighQuality;
			int gridOffsetX=100; 
			int gridOffsetY=0;
			int gridCellWidth = (Width - gridOffsetX) / GameConstants.MAX_NUMBER_DAYS;
			int gridCellHeight=35; 
			Pen framePen = new Pen(colorDarkGrey, 1);
			Pen fPen = new Pen(colorRed, 1);
			Pen PenBounds = new Pen(System.Drawing.Color.Pink, 1);
			Brush tmpbrush = null;
			int KeyWidth = this.Width-6;
			string id_str = string.Empty;
			int id_number =0;
			emGanttChartDayType DayType2 = emGanttChartDayType.DEFAULT;
			emGanttChartDayType DayType3 = emGanttChartDayType.DEFAULT;
			int DollarHeight = 0;
			DollarHeight = (int)font.GetHeight(g);
			int DiamondSize= (2*gridCellWidth) /5;
			int DmdOffsetX = 0;
			int DmdOffsetY = 0; 
			string DisplayText = string.Empty;
			int max_slots = GameConstants.MAX_NUMBER_SLOTS;
			int seven_projects_shift_down = 0; //Used to shift boxes to cope with 7 projects display 

			try 
			{
				DrawKey(e.Graphics, gridOffsetX, gridOffsetY+gridCellHeight*14+(gridCellHeight/2)-15);

				if (display_seven_projects)
				{
					max_slots = 7;
					gridCellHeight = (35 * 6) / 7;
					seven_projects_shift_down = 1;
				}
				else
				{
					max_slots = 6;
				}
				//=========================================================================
				//==Drawing the Framework==================================================
				//=========================================================================
				// Column stripes.
				for (int day = 0; day < GameConstants.MAX_NUMBER_DAYS; day++)
				{
					using (Brush brush = new SolidBrush(columnColours[day % 2]))
					{
						e.Graphics.FillRectangle(brush,
												 (day * gridCellWidth) + gridOffsetX, gridOffsetY + gridCellHeight,
												 gridCellWidth, (13+seven_projects_shift_down) * gridCellHeight);
					}
				}

				//Horizontel lines
				for (int step = 0; step < (14 + seven_projects_shift_down); step++)
				{
					e.Graphics.DrawLine(framePen, gridOffsetX, gridOffsetY + ((step + 1) * gridCellHeight),
						(MaxGameDays * gridCellWidth) + gridOffsetX, gridOffsetY + ((step + 1) * gridCellHeight));
				}

				//Building the Framework (Top Timeline)
				DrawRectangle(e.Graphics,
							  0, gridOffsetY,
							  gridOffsetX - 0, gridCellHeight / 2,
								dayColour, dayTextColour, font,
							  StringAlignment.Center, StringAlignment.Near,
							  "Day");

				for (int step = 0; step < GameConstants.MAX_NUMBER_DAYS; step++)
				{
					Color colour = dayColour;
					Color textcolour = dayTextColour;
					int height = gridCellHeight;

					bool blockDay = (getOpsBlockActionOnDay(step) != string.Empty);
					if (blockDay)
					{
						colour = changeFreezeColour;
						textcolour = changeFreezeTextColour;
						height = gridCellHeight * (14 + seven_projects_shift_down);
					}
					
					DrawRectangle(e.Graphics,
								  (step * gridCellWidth) + gridOffsetX, gridOffsetY,
								  gridCellWidth, height,
									colour, textcolour, font,
								  StringAlignment.Center, StringAlignment.Near,
								  formatDaystr(step + 1));

					if (blockDay)
					{
						using (Brush brush = new SolidBrush(changeFreezeTextColour))
						{
							RectangleF rectangle = new RectangleF((step * gridCellWidth) + gridOffsetX, gridOffsetY,
																	 gridCellWidth, (14 + seven_projects_shift_down) * gridCellHeight);

							StringFormat format = new StringFormat();
							format.Alignment = StringAlignment.Center;
							format.LineAlignment = StringAlignment.Center;

							e.Graphics.DrawString(InsertNewlinesBetweenCharacters("CHANGE FREEZE"), font, brush, rectangle, format);
						}
					}

					e.Graphics.DrawLine(framePen, step * gridCellWidth + gridOffsetX, gridOffsetY, 
						step * gridCellWidth + gridOffsetX, gridOffsetY + gridCellHeight * (14 + seven_projects_shift_down));
				}

				e.Graphics.DrawLine(framePen, MaxGameDays * gridCellWidth + gridOffsetX, gridOffsetY, 
					MaxGameDays * gridCellWidth + gridOffsetX, gridOffsetY + gridCellHeight * (14 + seven_projects_shift_down));

				// Borders on the block days.
				for (int step = 0; step < GameConstants.MAX_NUMBER_DAYS; step++)
				{
					bool blockDay = (getOpsBlockActionOnDay(step) != string.Empty);
					if (blockDay)
					{
						using (Pen pen = new Pen(changeFreezeBorderColour, 1.0f))
						{
							e.Graphics.DrawRectangle(pen,
													 (step * gridCellWidth) + gridOffsetX, gridOffsetY,
													 gridCellWidth, (14 + seven_projects_shift_down) * gridCellHeight);
						}
					}
				}

				//Building the Framework (Project labels)
				DrawRectangle(e.Graphics, 0, (1 * gridCellHeight + gridOffsetY) - (gridCellHeight / 2),
							  gridOffsetX, gridCellHeight / 2,
							  projectColours[0], Color.White, font,
							  StringAlignment.Center, StringAlignment.Center,
							  SkinningDefs.TheInstance.GetData("projects_term_round_" + LibCore.CONVERT.ToStr(DisplayRound), "projects").ToUpper());


				for (int step2 = 0; step2 < max_slots; step2++)
				{
					id_number = getProjectNumber(step2);
					id_str = CONVERT.ToStr(step2 + 1);
					if (id_number > 0)
					{
						id_str = CONVERT.ToStr(id_number);
					}

					DrawRectangle(e.Graphics, 0, (step2 + 1) * gridCellHeight + gridOffsetY,
								  gridOffsetX, gridCellHeight,
								  projectColours[(step2 + 1) % 2], Color.White, font,
								  StringAlignment.Center, StringAlignment.Center,
								  id_str);
				}

				using (Pen pen = new Pen (Color.White, 2.0f))
				{
					e.Graphics.DrawRectangle(pen, 0, (1 * gridCellHeight + gridOffsetY) - (gridCellHeight / 2),
											 gridOffsetX, (gridCellHeight * max_slots) + (gridCellHeight / 2));
				}

				//Building the Framework (Operations)
				DrawRectangle(e.Graphics,
					0, ((7 + seven_projects_shift_down) * gridCellHeight + gridOffsetY),
					gridOffsetX, gridCellHeight - 0,
					opsColours[0], Color.White, font,
					StringAlignment.Center, StringAlignment.Center,
					"OPS CHANGE");

				DrawRectangle(e.Graphics,
				  0, ((8 + seven_projects_shift_down) * gridCellHeight + gridOffsetY),
					gridOffsetX, gridCellHeight * 3,
					opsColours[1], Color.White, font,
					StringAlignment.Center, StringAlignment.Center,
					"Memory");

				DrawRectangle(e.Graphics,
				  0, ((11+ seven_projects_shift_down) * gridCellHeight + gridOffsetY),
					gridOffsetX, gridCellHeight * 3,
					opsColours[0], Color.White, font,
					StringAlignment.Center, StringAlignment.Center,
					"Storage");

				using (Pen pen = new Pen (Color.White, 2.0f))
				{
					e.Graphics.DrawRectangle(pen, 0, ((7+ seven_projects_shift_down) * gridCellHeight + gridOffsetY),
											 gridOffsetX, (gridCellHeight * 7));
				}

				//=========================================================================
				//==displaying the Project Slots===========================================
				//=========================================================================
				for (int step2 = 0; step2 < max_slots; step2++)
				{
					if (this.MyNodeTree != null)
					{
						//PerfData_Project tmpPrjData = MyPerfData_RoundDataHandle.getProjData(step2);
						//if (tmpPrjData != null)
						if (true)
						{
								Boolean ProjectCompleted = isProjectCompleted(step2); 
								string Location = getProjectInstallLocation(step2); 
								string LocationZone = Location;
								string LocationNumber = Location;
								string TargetLocation = "";

								Boolean DisplayLocation = false;

								if (Location.Length>3)
								{
									LocationZone = Location.Substring(0,1);
									LocationNumber = Location.Substring(1,3);
									LocationZone = LocationZone.ToUpper();
									DisplayLocation = true;
								}

							//Extract the Project Ref
							//id_str = tmpPrjData.ProjectNumber.ToString();
							id_number = getProjectNumber(step2);
							id_str = "";
							if (id_number>0)
							{
								id_str = CONVERT.ToStr(id_number);
							}
							//Draw the Project Days
							bool ProcessedDay = false;

							for (int step1 = 0; step1< GameConstants.MAX_NUMBER_DAYS; step1++)
							{
								ProcessedDay = false;
								TargetLocation = getProjectInstallLocation(step2, step1);
								//string st2 = tmpPrjData.ActualProjectPhaseByDay[step1];
								string st2 = getProjectActualPhaseByProjectByDay(step2, step1);			//this Day
								string st3 = getProjectActualPhaseByProjectByDay(step2, step1 + 1);	//the following Day
								//System.Diagnostics.Debug.WriteLine("->>>Day" + step1.ToString() + " slot" + step2.ToString() + "  -> " + st2 + "    " + TargetLocation);
								//string st2 = tmpPrjData.ActualProjectPhaseByDay[step1];
								if ((st2 != string.Empty) & (st2 != " "))
								{
									tmpbrush = GetBrushForLetter(st2);

									DayType2 = getChartDayType(st2);
									DayType3 = getChartDayType(st3);

									switch ((int)DayType2)
									{
										case (int)emGanttChartDayType.INSTALLING:
											//we only showing installing if the next day is complete (ie Install was successfull)
											if (DayType3 == emGanttChartDayType.COMPLETE)
											{
												e.Graphics.FillRectangle(brushGreen,
													(step1 * gridCellWidth + gridOffsetX), ((step2 + 1) * gridCellHeight + gridOffsetY),
													gridCellWidth - 0, gridCellHeight - 0);
												if (DisplayLocation)
												{
													g.DrawString(getLocationZoneLetter(TargetLocation), font, brushWhite,
														step1 * gridCellWidth + gridOffsetX + (gridCellWidth / 2) - 5,
														(step2 + 1) * gridCellHeight + gridOffsetY + (gridCellHeight / 4) - 9 + 2);
													g.DrawString(getLocationNumber(TargetLocation), font, brushWhite,
														step1 * gridCellWidth + gridOffsetX - 5 + 6,
														(step2 + 1) * gridCellHeight + gridOffsetY + (gridCellHeight / 4) - 9 + 20 - 2);
													ProcessedDay = true;
												}
											}
											break;

										case (int)emGanttChartDayType.INSTALL_FAIL:
											//Failure should be location in a Red Box
											g.FillRectangle(brushRed, (step1 * gridCellWidth + gridOffsetX), ((step2 + 1) * gridCellHeight + gridOffsetY), gridCellWidth, gridCellHeight);
											if (DollarHeight < gridCellHeight)
											{
												g.DrawString(getLocationZoneLetter(TargetLocation), font, brushWhite,
													step1 * gridCellWidth + gridOffsetX + (gridCellWidth / 2) - 5,
													(step2 + 1) * gridCellHeight + gridOffsetY + (gridCellHeight / 4) - 9 + 2);
												g.DrawString(getLocationNumber(TargetLocation), font, brushWhite,
													step1 * gridCellWidth + gridOffsetX - 5 + 6,
													(step2 + 1) * gridCellHeight + gridOffsetY + (gridCellHeight / 4) - 9 + 20 - 2);
											}
											ProcessedDay = true;
											break;
										case (int)emGanttChartDayType.COMPLETE:
											//Complete, show the diamond (Reusing the Points Array)
											DmdOffsetX = (step1 * gridCellWidth + gridOffsetX) + (gridCellWidth / 2);
											DmdOffsetY = ((step2 + 1) * gridCellHeight + gridOffsetY) + (1 * gridCellHeight / 5);

											DiamondPts[0].X = DmdOffsetX + 0;
											DiamondPts[0].Y = DmdOffsetY + 0;
											DiamondPts[1].X = DmdOffsetX + DiamondSize;
											DiamondPts[1].Y = DmdOffsetY + DiamondSize;
											DiamondPts[2].X = DmdOffsetX + 0;
											DiamondPts[2].Y = DmdOffsetY + (DiamondSize * 2);
											DiamondPts[3].X = DmdOffsetX - DiamondSize;
											DiamondPts[3].Y = DmdOffsetY + DiamondSize;
											DiamondPts[4].X = DmdOffsetX + 0;
											DiamondPts[4].Y = DmdOffsetY + 0;

											g.FillPolygon(brushBlack, DiamondPts, System.Drawing.Drawing2D.FillMode.Alternate);
											ProcessedDay = true;
											break;
										case (int)emProjectPerfChartDayType.DEFAULT:
										default:
											//g.FillRectangle(tmpbrushActual,
											//	(step1*gridCellWidth+gridOffsetX), ((step2+1)*gridCellHeight+gridOffsetY),
											//	gridCellWidth-0,gridCellHeight-0);
											break;
									}
								}

								if (ProcessedDay==false)
								{ 
									string too_early_location = getProjectTooEarlyLocation(step2, step1);
									if (string.IsNullOrEmpty(too_early_location)==false)
									{
										g.FillRectangle(brushRed, (step1 * gridCellWidth + gridOffsetX), ((step2 + 1) * gridCellHeight + gridOffsetY), gridCellWidth, gridCellHeight);
										if (DollarHeight < gridCellHeight)
										{
											g.DrawString(getLocationZoneLetter(too_early_location), font, brushWhite,
												step1 * gridCellWidth + gridOffsetX + (gridCellWidth / 2) - 5,
												(step2 + 1) * gridCellHeight + gridOffsetY + (gridCellHeight / 4) - 9 + 2);
											g.DrawString(getLocationNumber(too_early_location), font, brushWhite,
												step1 * gridCellWidth + gridOffsetX - 5 + 6,
												(step2 + 1) * gridCellHeight + gridOffsetY + (gridCellHeight / 4) - 9 + 20 - 2);
										}
									}
								}
							}
						}
					}
					//=========================================================================
					//==Operational Actions====================================================
					//=========================================================================
					if (this.MyNodeTree != null)
					{
						Boolean IsFSC = false;
						Boolean IsMemory = false;
						Boolean IsStorage = false;
						string Location = string.Empty;
						string LocationZone = string.Empty;
						string LocationNumber = string.Empty;
						string servname = string.Empty;
						int OpsOffset = 1;
						int HalfColOffset = (gridCellWidth) / 2;
						bool ActivityFlag = false;
						string activity = "";
						string act_location = "";
						bool act_success = false;

						RectangleF currentActivityBlock = new RectangleF (0, 0, 0, 0);

						for (int step1 = 0; step1< GameConstants.MAX_NUMBER_DAYS; step1++)
						{
							ActivityFlag = this.getOperationsActionsOnDay(step1,out activity, out act_location, out act_success); 

							if (ActivityFlag)
							{
								IsMemory = false;
								IsStorage = false;
								IsFSC = false;
								switch (activity)
								{
									case "upgrade_fsc_app":
									case "rebuild_fsc_app":
											IsFSC = true;
										break;
									case "upgrade_memory":
										IsMemory = true;
										break;
									case "upgrade_disk":
										IsStorage = true;
										break;
									case "upgrade_both":
										IsMemory = true;
										IsStorage = true;
										break;
									case "install_cc_app":
										IsFSC = true;
										break;
								}

								// For change cards, see if we run over multiple days; if so, draw us specially.
								bool weAreStartOfABlock = false;
								bool weAreEndOfABlock = false;
								bool weAreInABlock = false;

								if (IsFSC)
								{
									weAreInABlock = true;
									weAreStartOfABlock = true;
									weAreEndOfABlock = true;

									string previousActivity;
									string previousLocation;
									bool previousSuccess;
									bool previousFlag = getOperationsActionsOnDay(step1 - 1, out previousActivity, out previousLocation, out previousSuccess);
									if ((previousActivity == activity) && (previousLocation == act_location) && (previousSuccess == act_success))
									{
										weAreStartOfABlock = false;
									}

									string nextActivity;
									string nextLocation;
									bool nextSuccess;
									bool nextFlag = getOperationsActionsOnDay(step1 + 1, out nextActivity, out nextLocation, out nextSuccess);
									if ((nextActivity == activity) && (nextLocation == act_location) && (nextSuccess == act_success))
									{
										weAreEndOfABlock = false;
									}
								}

								if (weAreStartOfABlock)
								{
									currentActivityBlock = new RectangleF ((step1 * gridCellWidth) + gridOffsetX, ((max_slots + OpsOffset) * gridCellHeight) + gridOffsetY, gridCellWidth, gridCellHeight);
								}
								if (weAreEndOfABlock)
								{
									currentActivityBlock.Width = (step1 * gridCellWidth) + gridOffsetX + gridCellWidth - currentActivityBlock.Left;
									currentActivityBlock.Height = ((max_slots + OpsOffset) * gridCellHeight) + gridOffsetY + gridCellHeight - currentActivityBlock.Top;
								}

								if ((IsFSC)|(IsMemory)|(IsStorage))
								{
									if (act_location.Length>3)
									{
										LocationZone = act_location.Substring(0,1);
										LocationNumber = act_location.Substring(1,3);
										LocationZone = LocationZone.ToUpper();
									}
									//Drawing the Background of the action 
									if (IsFSC)
									{
										OpsOffset=1;
										if (act_success)
										{
											e.Graphics.FillRectangle(this.brushFSC_pass,(step1*gridCellWidth+gridOffsetX),
												((max_slots + OpsOffset) * gridCellHeight + gridOffsetY),
												gridCellWidth-0,gridCellHeight);
										}
										else
										{
											e.Graphics.FillRectangle(this.brushFSC_fail,(step1*gridCellWidth+gridOffsetX),
												((max_slots + OpsOffset) * gridCellHeight + gridOffsetY),
												gridCellWidth-0,gridCellHeight);
										}

										using (Brush brush = new SolidBrush (FSCBorder))
										{
											e.Graphics.FillRectangle(brush,
																     (step1 * gridCellWidth) + gridOffsetX, ((max_slots + OpsOffset) * gridCellHeight) + gridOffsetY,
																     gridCellWidth, FSCBorderThickness);

											e.Graphics.FillRectangle(brush,
																	 (step1 * gridCellWidth) + gridOffsetX, ((max_slots + OpsOffset) * gridCellHeight) + gridOffsetY + gridCellHeight - FSCBorderThickness,
																	 gridCellWidth, FSCBorderThickness);

											if (weAreStartOfABlock)
											{
												e.Graphics.FillRectangle(brush,
																		 (step1 * gridCellWidth) + gridOffsetX, ((max_slots + OpsOffset) * gridCellHeight) + gridOffsetY,
																		 FSCBorderThickness, gridCellHeight);
											}
											if (weAreEndOfABlock)
											{
												e.Graphics.FillRectangle(brush,
																		 (step1 * gridCellWidth) + gridOffsetX + gridCellWidth - FSCBorderThickness, ((max_slots + OpsOffset) * gridCellHeight) + gridOffsetY,
																		 FSCBorderThickness, gridCellHeight);
											}
										}
									}
									if (IsMemory)
									{
										OpsOffset=2;
										e.Graphics.FillRectangle(brushMem,(step1*gridCellWidth+gridOffsetX),
											((max_slots + OpsOffset) * gridCellHeight + gridOffsetY),
											gridCellWidth-0,gridCellHeight*3);
									}
									if (IsStorage)
									{
										OpsOffset=5;
										e.Graphics.FillRectangle(brushStorage, (step1*gridCellWidth+gridOffsetX),
											((max_slots + OpsOffset) * gridCellHeight + gridOffsetY),
											gridCellWidth-0,gridCellHeight*3);
									}
									//Drawing the Background 
									if (IsFSC)
									{
										// Only render the string on the last day of the block, so we can
										// work out the formatting.
										if (weAreInABlock && weAreEndOfABlock)
										{
											StringFormat format = new StringFormat();
											format.Alignment = StringAlignment.Center;
											format.LineAlignment = StringAlignment.Near;

											g.DrawString(LocationZone, font, brushBlack,
														 new RectangleF (currentActivityBlock.Left, (max_slots + OpsOffset) * gridCellHeight + gridOffsetY + (gridCellHeight / 4) - 9 + 2, currentActivityBlock.Width, 20), format);

											g.DrawString(LocationNumber, font, brushBlack,
														 new RectangleF (currentActivityBlock.Left, (max_slots + OpsOffset) * gridCellHeight + gridOffsetY + (gridCellHeight / 4) - 9 + 20 - 2, currentActivityBlock.Width, 20), format);
										}
									}
									else
									{
										servname = string.Empty;
										servname = act_location;
										//Translate Location into Server Name 

										//if (this.MyNeworkManager != null)
										//{
										//servname = this.MyNeworkManager.TranslateServerLocationtoName(Location);
										//servname =
										//}

										//DisplayText = Location;
										//if (MyServerNames.Contains(Location))
										//{
										//DisplayText = (string)MyServerNames[Location];
										//}
										if (IsMemory)
										{
											OpsOffset=2;
											drawTowerText2(e.Graphics, (step1*gridCellWidth+gridOffsetX) + HalfColOffset,
												((max_slots + OpsOffset) * gridCellHeight + gridOffsetY), servname);
										}
										if (IsStorage)
										{
											OpsOffset=5;
											drawTowerText2(e.Graphics, (step1*gridCellWidth+gridOffsetX) + HalfColOffset,
												((max_slots + OpsOffset) * gridCellHeight + gridOffsetY), servname);
										}
										//drawTowerText2(e.Graphics, (step1*gridCellWidth+gridOffsetX) + HalfColOffset, 
										//	((max_slots+OpsOffset)*gridCellHeight+gridOffsetY), servname);
									}
								}
							}
						}
					}
				}
			}
			catch (Exception evc)
			{
				string st1 = evc.Message;//Just prevent the exception taking out the app
			}
		}

		/// <summary>
		/// We need to extract the interesting events from the log file 
		/// We only extract some of project status updates
		/// </summary>
		protected void ExtractLog ()
		{
			string filename =  gameFile.GetRoundFile(DisplayRound, "NetworkIncidents.log", GameFile.GamePhase.OPERATIONS);
			NodeTree model = MyNodeTree;

			projectNameBySlotNumber = new Dictionary<int, string> ();

			plannedStateNameByTimeByProjectName = new Dictionary<string, Dictionary<double, string>> ();
			stateNameByTimeByProjectName = new Dictionary<string, Dictionary<double, string>> ();
			InstallDataByTimeByProjectName = new Dictionary<string, Dictionary<int, string>>();
			TooEarlyInstallDataByTimeByProjectName = new Dictionary<string, Dictionary<int, string>>();

			opsActivityByTime = new Dictionary<double, OpsActivity> ();

			BasicIncidentLogReader reader = new BasicIncidentLogReader(filename);
			Node projects = model.GetNamedNode("pm_projects_running");
			foreach (Node project in projects.GetChildrenOfType("project"))
			{
				Node projectData = project.GetFirstChildOfType("project_data");

				projectNameBySlotNumber.Add(project.GetIntAttribute("slot", 0), project.GetAttribute("name"));

				reader.WatchApplyAttributes(project.GetAttribute("name"), new LogLineFoundDef.LineFoundHandler(reader_ProjectAttributesChanged));
				//reader.WatchApplyAttributes(projectData.GetAttribute("name"), new LogLineFoundDef.LineFoundHandler(reader_ProjectDataAttributesChanged));
			}
			reader.WatchCreatedNodes("ops_activity", new LogLineFoundDef.LineFoundHandler(reader_OpsActivityNodeAdded));
			reader.Run();

			Node projectPlans = model.GetNamedNode("project_plans");
			foreach (Node projectPlan in projectPlans.GetChildrenOfType("project_plan"))
			{
				Dictionary<double, string> plannedStateByTime = new Dictionary<double, string>();
				plannedStateNameByTimeByProjectName.Add(projectPlan.GetAttribute("project"), plannedStateByTime);

				foreach (Node dayPlan in projectPlan.GetChildrenOfType("day_plan"))
				{
					plannedStateByTime.Add(60 * dayPlan.GetIntAttribute("day", 0),
										   dayPlan.GetAttribute("stage"));
				}
			}
		}

		void reader_CreatedProjectNode (object sender, string key, string line, double time)
		{
		}

		/// <summary>
		/// We only care about some of the project status levels (INSTALLING, INSTALLED_FAIL, COMPLETED)
		/// While we catch (INSTALLED_OK), this never occurs in the current code 
		/// We catch the status changes to build a lookup structure for used when rendering
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="key"></param>
		/// <param name="line"></param>
		/// <param name="time"></param>
		void reader_ProjectAttributesChanged (object sender, string key, string line, double time)
		{
			string projectName = BasicIncidentLogReader.ExtractValue(line, "i_name");
			string stateName = BasicIncidentLogReader.ExtractValue(line, "state_name");
			string errorTooEarly_location = BasicIncidentLogReader.ExtractValue(line, "install_too_early");

			string target_location = BasicIncidentLogReader.ExtractValue(line, "target");
			string stateInstallingName = emProjectOperationalState.PROJECT_STATE_INSTALLING.ToString();
			string stateInstallFailName = emProjectOperationalState.PROJECT_STATE_INSTALLED_FAIL.ToString();
			string stateInstallOKName = emProjectOperationalState.PROJECT_STATE_INSTALLED_OK.ToString();
			string stateCompletedName = emProjectOperationalState.PROJECT_STATE_COMPLETED.ToString();

			bool isInstalling = stateInstallingName.ToLower().Equals(stateName.ToLower());
			bool isInstalledFail = stateInstallFailName.ToLower().Equals(stateName.ToLower());
			bool isInstalledOK = stateInstallOKName.ToLower().Equals(stateName.ToLower());
			bool isCompleted = stateCompletedName.ToLower().Equals(stateName.ToLower());

			int day = (int)(time / 60) + 1;

			if (stateName != string.Empty)
			{
				//catching the install locations 
				if ((isInstalling) | (isInstalledFail) | (isInstalledOK) | (isCompleted))
				{
					if (!stateNameByTimeByProjectName.ContainsKey(projectName))
					{
						stateNameByTimeByProjectName.Add(projectName, new Dictionary<double, string>());
					}

					if (stateNameByTimeByProjectName[projectName].ContainsKey(time))
					{
						stateNameByTimeByProjectName[projectName][time] = stateName;
						//System.Diagnostics.Debug.WriteLine(" captured A " + time.ToString() + "  " + stateName + "  " + target_location);
					}
					else
					{
						stateNameByTimeByProjectName[projectName].Add(time, stateName);
						//System.Diagnostics.Debug.WriteLine(" captured B " + time.ToString() + "  " + stateName + "  " + target_location);
					}

					//It's a state change 
					if (!InstallDataByTimeByProjectName.ContainsKey(projectName))
					{
						InstallDataByTimeByProjectName.Add(projectName, new Dictionary<int, string>());
					}

					if (InstallDataByTimeByProjectName[projectName].ContainsKey(day))
					{
						InstallDataByTimeByProjectName[projectName][day] = target_location;
					}
					else
					{
						InstallDataByTimeByProjectName[projectName].Add(day, target_location);
					}
				}
			}
			else
			{
				//Not a state Change that we wanted but we also need to change tooEarly Errors
				if (string.IsNullOrEmpty(errorTooEarly_location) == false)
				{
					//It's a too early
					if (!TooEarlyInstallDataByTimeByProjectName.ContainsKey(projectName))
					{
						TooEarlyInstallDataByTimeByProjectName.Add(projectName, new Dictionary<int, string>());
					}
					if (TooEarlyInstallDataByTimeByProjectName[projectName].ContainsKey(day))
					{
						TooEarlyInstallDataByTimeByProjectName[projectName][day] = errorTooEarly_location;
					}
					else
					{
						TooEarlyInstallDataByTimeByProjectName[projectName].Add(day, errorTooEarly_location);
					}
				}
			}
		}

		protected void reader_OpsActivityNodeAdded (object sender, string key, string line, double time)
		{
			OpsActivity activity = new OpsActivity ();

			activity.Success = CONVERT.ParseBool(BasicIncidentLogReader.ExtractValue(line, "success")) ?? false;
			activity.Activity = BasicIncidentLogReader.ExtractValue(line, "sub_type");
			activity.Location = BasicIncidentLogReader.ExtractValue(line, "location");

			opsActivityByTime.Add(time, activity);
		}

		protected void DrawRectangle(Graphics graphics, int left, int top, int width, int height,
			Color background, Color foreground, Font font,
			StringAlignment horizontalAlign, StringAlignment verticalAlign, string text)
		{
			RectangleF rectangle = new RectangleF (left, top, width, height);
			using (Brush brush = new SolidBrush (background))
			{
				graphics.FillRectangle(brush, left, top, width, height);
			}
			using (Brush brush = new SolidBrush (foreground))
			{
				StringFormat format = new StringFormat ();
				format.Alignment = horizontalAlign;
				format.LineAlignment = verticalAlign;
				graphics.DrawString(text, font, brush, rectangle, format);
			}
		}

		string InsertNewlinesBetweenCharacters (string input)
		{
			StringBuilder output = new StringBuilder ();
			foreach (char character in input)
			{
				output.Append(character);
				output.Append("\n");
			}

			return output.ToString();
		}
	}
}