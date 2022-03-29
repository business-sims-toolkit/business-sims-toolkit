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
	public enum emProjectPerfChartType
	{
		BOTH,
		COST,
		GANTT
	}

	public enum emProjectPerfChartDayType
	{
		DEFAULT,
		MONEYDAY,
		INSTALL_OK,
		INSTALL_FAIL,
		COMPLETE,
	}

	/// <summary>
	/// Summary description for PrjPerfControl.
	/// </summary>
	public class PrjPerfControl : System.Windows.Forms.UserControl
	{
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		/// 
		private int MaxGameDays = 25; 

		private System.ComponentModel.Container components = null;

		private Font font = CoreUtils.SkinningDefs.TheInstance.GetFont(10f, FontStyle.Bold);
		private Font fontPrj = CoreUtils.SkinningDefs.TheInstance.GetFont(12f, FontStyle.Bold);

		Color dayColour = Color.FromArgb(255, 255, 255);
		//Color dayTextColour = Color.FromArgb(255, 255, 255);
		Color dayTextColour = Color.FromArgb(0, 0, 0);
		//Color bottomDayColour = Color.FromArgb(69, 150, 31);
		Color bottomDayColour = Color.FromArgb(255, 255, 255);
		//Color bottomDayTextColour = Color.FromArgb(255, 255, 255);
		Color bottomDayTextColour = Color.FromArgb(0, 0, 0);

		Color changeFreezeColour = Color.FromArgb(192, 192, 192);  //Chnage Freeze body fill colour 177, 220, 239
		Color changeFreezeTextColour = Color.FromArgb(128, 128, 128);//50, 75, 85
		Color changeFreezeBorderColour = Color.FromArgb(128, 128, 128); //0, 66, 109
		Color [] columnColours = new Color [] { Color.White, Color.FromArgb(241, 242, 242) };
		//Color [] stageColours = new Color [] { Color.FromArgb(0, 66, 109), Color.FromArgb(10, 92, 133) };
		Color[] stageColours = new Color[] { Color.FromArgb(96, 96, 96), Color.FromArgb(112, 112, 112) };
		//Color [] costColours = new Color [] { Color.FromArgb(69, 150, 31), Color.FromArgb(46, 120, 76) };
		Color[] costColours = new Color[] { Color.FromArgb(96, 96, 96), Color.FromArgb(112, 112, 112) };

		private Pen PenHighLight = new Pen(Color.Black, 1);
		private Pen PenBounds = new Pen(Color.Pink, 1);
		private Pen PenKey = new Pen(Color.Cyan, 1);

		Color spendColour = Color.FromArgb(75, 175, 0);
		Color budgetColour = Color.FromArgb(0, 0, 200);
		Color predictedSpendColour = Color.FromArgb(200, 0, 0);
		Color plannedSpendColour = Color.FromArgb(235, 95, 1);

		private Pen PenMoneyExSpend_SP;
		Pen plannedSpendPen;
		private Pen PenMoneyIncome_SP;
		private Pen PenMoneyIncome_SIP;

		private Pen PenPlanBlocks = new Pen(Color.FromArgb(235,95,1),1);
		private Pen PenActualBlocks = new Pen(Color.FromArgb(75,175,0),1);
		private Pen ChartGridPen = new Pen(Color.DarkGray, 1);

		private Brush brushSilver = new SolidBrush(Color.Silver);
		private Brush brushWhite = new SolidBrush(Color.White);
		private Brush brushBlack = new SolidBrush(Color.Black);
		private Brush brushPlanBlocks = new SolidBrush(Color.FromArgb(235,95,1));
		private Brush brushActualBlocks = new SolidBrush(Color.FromArgb(75,175,0));
		private Brush brushProjectsSideBarBack = new SolidBrush(Color.Gainsboro);

		private System.Drawing.Color framecolor = System.Drawing.Color.White;
		private int DisplayRound = 1;
		private int DisplayProject = 0;						//It's zero base to match the slot in Journal
		private Boolean DisplayGanttGraph = true; 
		private Boolean DisplayCostGraph = true;
		private Rectangle DrawingBox = new Rectangle(0,0,0,0);
		private Rectangle GanttBox = new Rectangle(0,0,0,0);
		private Rectangle CostBox = new Rectangle(0,0,0,0);
		private Rectangle GanttKeyBox = new Rectangle(0,0,0,0);
		private Rectangle CostKeyBox = new Rectangle(0,0,0,0);
		private int DisplayTitleHeight = 20;
		private int GanttKeyBoxHeight = 40;
		private int CostKeyBoxHeight = 40;
		private int HighLightDay = 5;
		private Boolean GraphBuffer = false;
		private Boolean ShowDebugConstructions = false;
		private emProjectPerfChartType WhichChart = emProjectPerfChartType.BOTH;

		private int CostGridCellWidth = 0;
		private int CostGridCellHeight = 0;
		private int GanttGridCellWidth = 0;
		private int GanttGridCellHeight = 0;

		private int CostGridCellOffsetX = 0;
		private int CostGridCellOffsetY = 0;
		private int GanttGridCellOffsetX = 0;
		private int GanttGridCellOffsetY = 0;
		private Point[] DiamondPts = new Point[5];
		private bool display_seven_projects = false;

		protected NodeTree MyNodeTree = null;
		protected Node opswork_node = null;
		protected Node projectsNode = null;
		protected Hashtable opworkitemsByDay = new Hashtable();

		PMNetworkProgressionGameFile gameFile;

		Dictionary<int, string> projectNameByUniqueNumber;
		Dictionary<int, string> projectNameByDisplaySlotNumber;
		
		Dictionary<string, string> projectNameByFinancialNodeName;

		Dictionary<string, Dictionary<double, int>> plannedSpendByTimeByProjectName;

		Dictionary<string, Dictionary<double, string>> plannedStateNameByTimeByProjectName;
		Dictionary<string, Dictionary<double, string>> stateNameByTimeByProjectName;
		Dictionary<string, Dictionary<double, bool>> pausedStatusByTimeByProjectName;

		Dictionary<string, Dictionary<double, int>> spendByTimeByProjectName;
		Dictionary<string, Dictionary<double, int>> budgetByTimeByProjectName;
		Dictionary<string, Dictionary<double, int>> projectedCostByTimeByProjectName;

		int slots;
		int allProjectsSlot;
		int totalProjectsSlot;

		public PrjPerfControl (PMNetworkProgressionGameFile gameFile, NodeTree model, int round, int slots, int allProjectsSlot, int totalProjectsSlot)
		{
			this.gameFile = gameFile;
			this.MyNodeTree = model;
			this.DisplayRound = round;

			this.slots = slots;
			this.allProjectsSlot = allProjectsSlot;
			this.totalProjectsSlot = totalProjectsSlot;

			Node projectsNodexx = MyNodeTree.GetNamedNode("pm_projects_running");
			display_seven_projects = projectsNodexx.GetBooleanAttribute("display_seven_projects", false);
			projectsNodexx = null;

			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
			// TODO: Add any initialization after the InitializeComponent call
			SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.DoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
			//Help object for drawing Diamonds
			DiamondPts[0] = new Point(0, 0);
			DiamondPts[1] = new Point(0, 0);
			DiamondPts[2] = new Point(0, 0);
			DiamondPts[3] = new Point(0, 0);
			DiamondPts[4] = new Point(0, 0);

			PenMoneyExSpend_SP = new Pen(spendColour, 3);
			PenMoneyIncome_SP = new Pen(budgetColour, 3);
			PenMoneyIncome_SIP = new Pen(predictedSpendColour, 3);
			plannedSpendPen = new Pen (plannedSpendColour, 3);

			projectsNode = MyNodeTree.GetNamedNode("pm_projects_running");
			ExtractLog();
		}

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			MyNodeTree = null;
			projectsNode = null;

			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		private void ClearData()
		{
			opworkitemsByDay.Clear();
			opswork_node = null;
		}

		private int getProjectSelectionDay(int slot)
		{
			int project_selection_day = 0;
			foreach (Node prjnode in this.projectsNode.getChildren())
			{
				if (prjnode != null)
				{
					ProjectRunner pr = new ProjectRunner(prjnode);
					int slot_id = pr.getProjectSlot();
					if (slot_id == (slot))
					{
						project_selection_day = pr.getProjectSelectionDay();
					}
					pr.Dispose();
				}
			}
			return project_selection_day;
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
		private int getProductNumber(int slot)
		{
			int product_number = 0;
			foreach (Node prjnode in this.projectsNode.getChildren())
			{
				if (prjnode != null)
				{
					ProjectReader pr = new ProjectReader(prjnode);
					int pa_slot = pr.getProjectSlot();
					if (pa_slot == (slot))
					{
						product_number = pr.getProductID();
					}
					pr.Dispose();
				}
			}
			return product_number;
		}
		private int getPlatformNumber(int slot)
		{
			return 1;
		}

		private int getProjectSIPBudget(int slot)
		{
			if (slot == totalProjectsSlot)
			{
				int total = 0;

				foreach (int i in GetSlotsToConsider(totalProjectsSlot))
				{
					total += getProjectSIPBudget(i);
				}

				return total;
			}

			int project_budget = 0;

			foreach (Node prjnode in this.projectsNode.getChildren())
			{
				if (prjnode != null)
				{
					ProjectReader pr = new ProjectReader(prjnode);
					int slot_id = pr.getProjectSlot();
					if (slot_id == (slot))
					{
						project_budget = pr.getSIPDefinedBudget();
					}
					pr.Dispose();
				}
			}
			return project_budget;
		}

		private int getBudgetExpenditureByProjectByDay(int PrjStepper, int day)
		{
			if (PrjStepper == totalProjectsSlot)
			{
				int total = 0;

				foreach (int slot in GetSlotsToConsider(totalProjectsSlot))
				{
					total += getBudgetExpenditureByProjectByDay(slot, day);
				}

				return total;
			}

			int spend = 0;

			if (projectNameByDisplaySlotNumber.ContainsKey(PrjStepper))
			{
				string name = projectNameByDisplaySlotNumber[PrjStepper];
				if (spendByTimeByProjectName.ContainsKey(name))
				{
					Dictionary<double, int> spendByTime = spendByTimeByProjectName[name];
					List<double> orderedTimes = new List<double>(spendByTime.Keys);
					orderedTimes.Sort();

					int previousDayTotalSpend = 0;
					int thisDayTotalSpend = 0;

					foreach (double time in orderedTimes)
					{
						if (time < (day * 60))
						{
							previousDayTotalSpend = spendByTime[time];
						}

						if (time < ((day + 1) * 60))
						{
							thisDayTotalSpend = spendByTime[time];
						}
						else
						{
							break;
						}
					}
					spend = thisDayTotalSpend - previousDayTotalSpend;
				}
			}
			return spend;
		}

		int GetPlannedBudgetExpenditureByProjectByDay (int PrjStepper, int day)
		{
			if (PrjStepper == totalProjectsSlot)
			{
				int total = 0;

				foreach (int slot in GetSlotsToConsider(totalProjectsSlot))
				{
					total += GetPlannedBudgetExpenditureByProjectByDay(slot, day);
				}

				return total;
			}

			int spend = 0;

			if (projectNameByDisplaySlotNumber.ContainsKey(PrjStepper))
			{
				string name = projectNameByDisplaySlotNumber[PrjStepper];
				if (plannedSpendByTimeByProjectName.ContainsKey(name))
				{
					Dictionary<double, int> spendByTime = plannedSpendByTimeByProjectName[name];
					List<double> orderedTimes = new List<double> (spendByTime.Keys);
					orderedTimes.Sort();

					int previousDayTotalSpend = 0;
					int thisDayTotalSpend = 0;

					foreach (double time in orderedTimes)
					{
						if (time < (day * 60))
						{
							previousDayTotalSpend = spendByTime[time];
						}

						if (time < ((day + 1) * 60))
						{
							thisDayTotalSpend = spendByTime[time];
						}
						else
						{
							break;
						}
					}
					spend = thisDayTotalSpend - previousDayTotalSpend;
				}
			}
			return spend;
		}

		private int getBudgetIncomeByProjectByDay(int PrjStepper, int day)
		{
			if (PrjStepper == totalProjectsSlot)
			{
				int total = 0;

				foreach (int slot in GetSlotsToConsider(totalProjectsSlot))
				{
					total += getBudgetIncomeByProjectByDay(slot, day);
				}

				return total;
			}

			int budgetBeforeRequestedDay = 0;
			int budgetAtEndOfRequestedDay = 0;

			if (projectNameByDisplaySlotNumber.ContainsKey(PrjStepper))
			{
				string name = projectNameByDisplaySlotNumber[PrjStepper];
				if (budgetByTimeByProjectName.ContainsKey(name))
				{
					Dictionary<double, int> budgetByTime = budgetByTimeByProjectName[name];
					List<double> orderedTimes = new List<double>(budgetByTime.Keys);
					orderedTimes.Sort();

					foreach (double time in orderedTimes)
					{
						int budget = budgetByTime[time];

						if (time < (day * 60))
						{
							budgetBeforeRequestedDay = budget;
							budgetAtEndOfRequestedDay = budget;
						}

						if ((time >= (day * 60))
							&& (time < ((day + 1) * 60)))
						{
							budgetAtEndOfRequestedDay = budget;
						}
					}
				}
			}

			return budgetAtEndOfRequestedDay - budgetBeforeRequestedDay;
		}

		private bool isProjectDeclared(int PrjStepper)
		{
			return true;
		}

		private string getProjectActualPhaseByProjectByDay (int PrjStepper, int step)
		{
			emProjectOperationalState state = emProjectOperationalState.PROJECT_STATE_UNKNOWN;
			if (projectNameByDisplaySlotNumber.ContainsKey(PrjStepper))
			{
				string name = projectNameByDisplaySlotNumber[PrjStepper];
				if (stateNameByTimeByProjectName.ContainsKey(name))
				{
					Dictionary<double, string> stateByTime = stateNameByTimeByProjectName[name];
					List<double> orderedTimes = new List<double> (stateByTime.Keys);
					orderedTimes.Sort();

					foreach (double time in orderedTimes)
					{
						if (time < ((step + 1) * 60))
						{
							emProjectOperationalState newState = emProjectOperationalState.PROJECT_STATE_UNKNOWN;
							if (stateByTime[time] != "")
							{
								newState = (emProjectOperationalState) Enum.Parse(typeof(emProjectOperationalState), stateByTime[time]);
							}

							//Most states persist until the next state change, but installation states only last a day.
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

			return code;
		}

		private string getProjectPlannedPhaseByProjectByDay(int PrjStepper, int step)
		{
			emProjectOperationalState state = emProjectOperationalState.PROJECT_STATE_UNKNOWN;

			if (projectNameByDisplaySlotNumber.ContainsKey(PrjStepper))
			{
				string name = projectNameByDisplaySlotNumber[PrjStepper];
				if (plannedStateNameByTimeByProjectName.ContainsKey(name))
				{
					Dictionary<double, string> plannedStateByTime = plannedStateNameByTimeByProjectName[name];
					List<double> orderedTimes = new List<double>(plannedStateByTime.Keys);
					orderedTimes.Sort();

					foreach (double time in orderedTimes)
					{
						if (time <= ((step) * 60))
						{
							if (time >= (step * 60))
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
			string code = GameConstants.ProjectShortCodeFromState(state);

			return code;
		}

		int GetProjectPlannedSpendByProjectByDay (int PrjStepper, int step)
		{
			int spend = 0;

			if (projectNameByDisplaySlotNumber.ContainsKey(PrjStepper))
			{
				string name = projectNameByDisplaySlotNumber[PrjStepper];
				if (plannedSpendByTimeByProjectName.ContainsKey(name))
				{
					Dictionary<double, int> plannedSpendByTime = plannedSpendByTimeByProjectName[name];
					List<double> orderedTimes = new List<double> (plannedSpendByTime.Keys);
					orderedTimes.Sort();

					foreach (double time in orderedTimes)
					{
						if (time <= ((step) * 60))
						{
							if (time >= (step * 60))
							{
								spend = plannedSpendByTime[time];
							}
						}
						else
						{
							break;
						}
					}
				}
			}

			return spend;
		}

		/// <summary>
		/// This day is Zero based but the action daty starts at 1 
		/// </summary>
		/// <param name="day"></param>
		/// <returns></returns>
		private string getOpsWorkActionOnDay(int day)
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

		#region Component Designer generated code
		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.Name = "PrjPerfControl";
			this.Size = new System.Drawing.Size(664, 504);
			this.Paint += new System.Windows.Forms.PaintEventHandler(this.PrjPerfControl_Paint);
			this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.PrjPerfControl_MouseDown);
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

		#region Set Methods

		public void setChartType(emProjectPerfChartType WhichChartType)
		{
			WhichChart = WhichChartType;
			if (WhichChart == emProjectPerfChartType.BOTH)
			{
				DisplayGanttGraph = true; 
				DisplayCostGraph = true;
			}
			if (WhichChart == emProjectPerfChartType.COST)
			{
				DisplayGanttGraph = false; 
				DisplayCostGraph = true;
			}
			if (WhichChart == emProjectPerfChartType.GANTT)
			{
				DisplayGanttGraph = true; 
				DisplayCostGraph = false;
			}
		}

		public void SetProjectNumber(int ProjectNumber)
		{
			DisplayProject = ProjectNumber;
			ExtractLog();
		}

		#endregion Set Methods

		#region DrawingUtils 

		private Pen GetPenForLetter(Boolean isPlan, string ActivityStr) 
		{
			Pen tmpPen = null;

			if (isPlan)
			{
				tmpPen = PenPlanBlocks;
			}
			else
			{
				tmpPen = PenActualBlocks;
			}

			if (ActivityStr.IndexOf(GameConstants.ACTIVITY_PROJECT_SELECT)>-1)
			{
				return tmpPen;
			}
			if (ActivityStr.IndexOf(GameConstants.ACTIVITY_PRODUCT_SELECT)>-1)
			{
				return tmpPen;
			}
			if (ActivityStr.IndexOf(GameConstants.ACTIVITY_DEV)>-1)
			{
				return tmpPen;
			}
			if (ActivityStr.IndexOf(GameConstants.ACTIVITY_TEST)>-1)
			{
				return tmpPen;
			}
			if (ActivityStr.IndexOf(GameConstants.ACTIVITY_HANDOVER)>-1)
			{
				return tmpPen;
			}
			if (ActivityStr.IndexOf(GameConstants.ACTIVITY_INSTALLING)>-1)
			{
				return tmpPen;
			}
			if (ActivityStr.IndexOf(GameConstants.ACTIVITY_INSTALLED)>-1)
			{
				return tmpPen;
			}
			if (ActivityStr.IndexOf("L")>-1)
			{
				return tmpPen;
			}
			return tmpPen;
		}

		private Brush GetBrushForLetter(Boolean isPlan, string ActivityStr) 
		{
			Brush tmpBrush = null;

			if (isPlan)
			{
				tmpBrush = brushPlanBlocks;
			}
			else
			{
				tmpBrush = brushActualBlocks;
			}

			if (ActivityStr.IndexOf(GameConstants.ACTIVITY_PROJECT_SELECT)>-1)
			{
				return tmpBrush;
			}
			if (ActivityStr.IndexOf(GameConstants.ACTIVITY_PRODUCT_SELECT)>-1)
			{
				return tmpBrush;
			}
			if (ActivityStr.IndexOf(GameConstants.ACTIVITY_DEV)>-1)
			{
				return tmpBrush;
			}
			if (ActivityStr.IndexOf(GameConstants.ACTIVITY_TEST)>-1)
			{
				return tmpBrush;
			}
			if (ActivityStr.IndexOf(GameConstants.ACTIVITY_HANDOVER)>-1)
			{
				return tmpBrush;
			}
			if (ActivityStr.IndexOf(GameConstants.ACTIVITY_INSTALLING)>-1)
			{
				return tmpBrush;
			}
			if (ActivityStr.IndexOf(GameConstants.ACTIVITY_INSTALLED)>-1)
			{
				return tmpBrush;
			}

			if (ActivityStr.IndexOf("L")>-1)
			{
				return tmpBrush;
			}
			return tmpBrush;
		}

		private int GetRowForLetter(string ActivityStr) 
		{
			if (ActivityStr.IndexOf("A")>-1)
			{
				return 0;
			}
			if (ActivityStr.IndexOf("B")>-1)
			{
				return 1;
			}
			if (ActivityStr.IndexOf("C")>-1)
			{
				return 2;
			}
			if (ActivityStr.IndexOf("D")>-1)
			{
				return 3;
			}
			if (ActivityStr.IndexOf("E")>-1)
			{
				return 4;
			}
			if (ActivityStr.IndexOf("F")>-1)
			{
				return 5;
			}
			if (ActivityStr.IndexOf("G")>-1)
			{
				return 6;
			}
			if (ActivityStr.IndexOf("H")>-1)
			{
				return 7;
			}
			if (ActivityStr.IndexOf(GameConstants.ACTIVITY_HANDOVER)>-1)
			{
				return 8;
			}
			if (ActivityStr.IndexOf(GameConstants.ACTIVITY_INSTALL_FAIL)>-1)
			{
				return 9;
			}
			if (ActivityStr.IndexOf(GameConstants.ACTIVITY_INSTALLING)>-1)
			{
				return 9;
			}
			if (ActivityStr.IndexOf(GameConstants.ACTIVITY_INSTALLED)>-1)
			{
				return 9;
			}
			if (ActivityStr.IndexOf(GameConstants.ACTIVITY_COMPLETE)>-1)
			{
				return 9;
			}

			return -1;
		}

		private Boolean isDay(string ActivityStr, string ActivityDefinition)
		{
			Boolean Exists= false;

			if (ActivityStr.IndexOf(ActivityDefinition)>-1)
			{
				Exists = true;
			}
			return Exists;
		}

		#endregion DrawingUtils 

		#region Money Limits Methods

		private int getBudgetIncomeMaxValueForProject(int ProjectSlot)
		{
			List<int> slotsToConsider = GetSlotsToConsider(ProjectSlot);

			int ProjectMoneyIncome = 0;
			int ProjectMoneyIncomeHighPt=0;
			for (int day=0; day < GameConstants.MAX_NUMBER_DAYS; day++)
			{
				ProjectMoneyIncome += getBudgetIncomeByProjectByDay(ProjectSlot, day);
				if (ProjectMoneyIncome>ProjectMoneyIncomeHighPt)
				{
					ProjectMoneyIncomeHighPt=ProjectMoneyIncome;
				}
			}

			if (ProjectMoneyIncomeHighPt>ProjectMoneyIncome)
			{
				ProjectMoneyIncome = ProjectMoneyIncomeHighPt;
			}

			if (ProjectMoneyIncome < this.getProjectSIPBudget(ProjectSlot))
			{
				ProjectMoneyIncome = this.getProjectSIPBudget(ProjectSlot);
			}

			if (GraphBuffer)
			{
				ProjectMoneyIncome = (ProjectMoneyIncome * 110) / 100;
			}

			return ProjectMoneyIncome;
		}

		List<int> GetSlotsToConsider (int projectSlot)
		{
			List<int> slotsToConsider = new List<int>();
			for (int i = 0; i < slots; i++)
			{
				if ((i == projectSlot) || (projectSlot == totalProjectsSlot))
				{
					slotsToConsider.Add(i);
				}
			}

			return slotsToConsider;
		}

		private int getBudgetExpendMaxValueForProject(int ProjectSlot)
		{
			List<int> slotsToConsider = GetSlotsToConsider(ProjectSlot);

			int ProjectMoneySpend=0;
			for (int day=0; day < GameConstants.MAX_NUMBER_DAYS; day++)
			{
				foreach (int slot in slotsToConsider)
				{
					ProjectMoneySpend += this.getBudgetExpenditureByProjectByDay(slot, day);
				}
			}
			if (GraphBuffer)
			{
				ProjectMoneySpend = (ProjectMoneySpend * 110) / 100;
			}

			return ProjectMoneySpend;
		}

		#endregion  Money Limits Methods

		#region Panel Graphical Methods

		public void BuildBoundaryBoxes()
		{
			DrawingBox.X = 0;
			DrawingBox.Y = DisplayTitleHeight;
			DrawingBox.Width = this.Width;
			DrawingBox.Height = this.Height-(1+DisplayTitleHeight);

			if ((DisplayGanttGraph)&(DisplayCostGraph))
			{
				GanttBox.X = 2;
				GanttBox.Y = 2 + DisplayTitleHeight;
				GanttBox.Width = DrawingBox.Width - 4;
				GanttBox.Height = (DrawingBox.Height - 4) / 2;

				CostBox.X = 2;
				CostBox.Y = GanttBox.Height + DisplayTitleHeight;
				CostBox.Width = DrawingBox.Width - 4;
				CostBox.Height = (DrawingBox.Height - 4) / 2;
			}
			else
			{
				if (DisplayGanttGraph)
				{
					GanttBox.X = 2;
					GanttBox.Y = 2 + DisplayTitleHeight;
					GanttBox.Width = DrawingBox.Width - 4;
					GanttBox.Height = DrawingBox.Height - (4 + GanttKeyBoxHeight);

					GanttKeyBox.X = 2;
					GanttKeyBox.Y = DrawingBox.Height - 2;
					GanttKeyBox.Width = DrawingBox.Width - 4;
					GanttKeyBox.Height =  GanttKeyBoxHeight;

				}
				if (DisplayCostGraph)
				{
					CostBox.X = 2;
					CostBox.Y = 2 + DisplayTitleHeight;
					CostBox.Width = DrawingBox.Width - 4;
					CostBox.Height = DrawingBox.Height - (4 + CostKeyBoxHeight);

					CostKeyBox.X = 2;
					CostKeyBox.Y = DrawingBox.Height - 2;
					CostKeyBox.Width = DrawingBox.Width - 4;
					CostKeyBox.Height =  CostKeyBoxHeight;
				}
			}
		}
	
		#endregion Panel Graphical Methods

		bool DoWeShowPlans ()
		{
			return (DisplayRound > 1);
		}

		#region Gantt Drawing Methods 

		public void DrawGanttChart (Graphics g)
		{
			bool dontShowHandoverOrInstall = false;

			g.SmoothingMode = SmoothingMode.HighQuality;
			int MaxRows = 21;
			int gridOffsetX = 100;
			int gridOffsetY = 10 + 10;
			int gridCellWidth = (GanttBox.Width - gridOffsetX) / MaxGameDays;
			int gridCellHeight = (GanttBox.Height - 10) / MaxRows;

			Brush tmpbrushActual = null;
			Brush tmpbrushPlan = null;
			Pen tmpPenActual = null;
			Pen tmpPenPlan = null;

			string id_str = string.Empty;
			int tmpRowPlan = 0;
			int tmpRowActual = -1;
			int tmpDrawRow = 0;
			int DollarHeight = 0;
			
			emProjectPerfChartDayType DayType = emProjectPerfChartDayType.DEFAULT;

			GanttGridCellWidth = gridCellWidth;
			GanttGridCellHeight = gridCellHeight;
			GanttGridCellOffsetX = gridOffsetX;
			GanttGridCellOffsetY = gridOffsetY;
			DollarHeight = (int) font.GetHeight(g);

			Font dollarFont = font;
			bool disposeDollarFont = false;
			if (DollarHeight >= gridCellHeight)
			{
				dollarFont = ConstantSizeFont.NewFont (font.FontFamily, font.Size * gridCellHeight * 0.75f / DollarHeight);
				disposeDollarFont = true;
				DollarHeight = (int) dollarFont.GetHeight(g);
			}

			if (dontShowHandoverOrInstall)
			{
				MaxRows -= 4;
			}

			//=========================================================================
			//==Framework==============================================================
			//=========================================================================
			// Column stripes.
			for (int day = 0; day < GameConstants.MAX_NUMBER_DAYS; day++)
			{
				using (Brush brush = new SolidBrush (columnColours[day % 2]))
				{
					g.FillRectangle(brush,
									(day * gridCellWidth) + gridOffsetX, gridOffsetY + gridCellHeight,
									gridCellWidth, (MaxRows - 1) * gridCellHeight);
				}
			}

			//Horizontel lines
			for (int step = 0; step < MaxRows; step++)
			{
				g.DrawLine(ChartGridPen, gridOffsetX, gridOffsetY + ((step + 1) * gridCellHeight), GameConstants.MAX_NUMBER_DAYS * gridCellWidth + gridOffsetX, gridOffsetY + ((step + 1) * gridCellHeight));
			}

			//Building the Framework (Top Timeline)

			DrawRectangle(g,
						  0, 0, gridOffsetX - 0, gridOffsetY + gridCellHeight,
							dayColour, dayTextColour, font,
						  StringAlignment.Center, StringAlignment.Near,
						  "Day");
			for (int step = 0; step < GameConstants.MAX_NUMBER_DAYS; step++)
			{
				Color colour = dayColour;
				Color textcolour = dayTextColour;
				int height = gridOffsetY + gridCellHeight;

				bool blockDay = (getOpsWorkActionOnDay(step) != string.Empty);
				if (blockDay)
				{
					colour = changeFreezeColour;
					textcolour = changeFreezeTextColour;
					height = (gridCellHeight * 21) + gridOffsetY;
				}

				DrawRectangle(g,
							  gridOffsetX + (step * gridCellWidth), 0,
							  gridCellWidth, height,
								colour, textcolour, font,
							  StringAlignment.Center, StringAlignment.Near,
							  formatDaystr(step + 1));

				g.DrawLine(ChartGridPen, step * gridCellWidth + gridOffsetX, gridOffsetY, step * gridCellWidth + gridOffsetX, gridOffsetY + gridCellHeight * MaxRows);
			}

			g.DrawLine(ChartGridPen, GameConstants.MAX_NUMBER_DAYS * gridCellWidth + gridOffsetX, gridOffsetY, GameConstants.MAX_NUMBER_DAYS * gridCellWidth + gridOffsetX, gridOffsetY + gridCellHeight * MaxRows);

			// Borders on the block days.
			for (int step = 0; step < GameConstants.MAX_NUMBER_DAYS; step++)
			{
				bool blockDay = (getOpsWorkActionOnDay(step) != string.Empty);
				if (blockDay)
				{
					using (Pen pen = new Pen(changeFreezeBorderColour, 1.0f))
					{
						g.DrawRectangle(pen,
										(step * gridCellWidth) + gridOffsetX, 0,
										gridCellWidth, (21 * gridCellHeight) + gridOffsetY);
					}
				}
			}

			// Stage column.
			DrawRectangle(g,
						  0, gridOffsetY - 5, gridOffsetX - 0, gridCellHeight + 5,
						  stageColours[0], Color.White, font,
						  StringAlignment.Center, StringAlignment.Center,
						  "STAGE");

			int maxStage = 10;
			if (dontShowHandoverOrInstall)
			{
				maxStage = 8;
			}

			for (int stage = 0; stage < maxStage; stage++)
			{
				string name;
				switch (stage)
				{
					case 8:
						name = "Handover";
						break;

					case 9:
						name = "Install";
						break;

					default:
						name = ((char) ('A' + stage)).ToString();
						break;
				}

				DrawRectangle(g,
							  0, gridOffsetY + (stage * gridCellHeight * 2) + gridCellHeight,
							  gridOffsetX - 0, gridCellHeight * 2,
							  stageColours[(stage + 1) % 2], Color.White, font,
							  StringAlignment.Center, StringAlignment.Center,
							  name);
			}

			using (Pen pen = new Pen(Color.White, 2.0f))
			{
				g.DrawRectangle(pen, 0, gridOffsetY - 5,
								gridOffsetX - 0, (gridCellHeight * 2 * 10) + gridCellHeight + 5);
			}

			//=========================================================================
			//==Draw the Project Stages================================================ 
			//=========================================================================
			if (this.MyNodeTree != null)
			{
				int currentDay = MyNodeTree.GetNamedNode("CurrentDay").GetIntAttribute("day", 0);

				if (isProjectDeclared(this.DisplayProject))
				{
					emProjectOperationalState [] stateOrderArray = new emProjectOperationalState [] { emProjectOperationalState.PROJECT_STATE_UNKNOWN, emProjectOperationalState.PROJECT_STATE_A, emProjectOperationalState.PROJECT_STATE_B, emProjectOperationalState.PROJECT_STATE_C, emProjectOperationalState.PROJECT_STATE_D, emProjectOperationalState.PROJECT_STATE_E, emProjectOperationalState.PROJECT_STATE_F, emProjectOperationalState.PROJECT_STATE_G, emProjectOperationalState.PROJECT_STATE_H, emProjectOperationalState.PROJECT_STATE_COMPLETED };
					List<emProjectOperationalState> stateOrder = new List<emProjectOperationalState> (stateOrderArray);

					for (int step1 = 0; step1 < GameConstants.MAX_NUMBER_DAYS; step1++)
					{
						//===============================================================
						//Draw the Planned Aspect first==================================
						//===============================================================

						List<string> plannedStates = new List<string> ();

						if (DoWeShowPlans())
						{
							plannedStates.Add(getProjectPlannedPhaseByProjectByDay(this.DisplayProject, step1));
						}

						foreach (string st in plannedStates)
						{
							if ((st != string.Empty) & (st != " "))
							{
								if (isDay(st, GameConstants.ACTIVITY_NOMONEY) == false)
								{
									tmpPenPlan = GetPenForLetter(true, st);
									tmpbrushPlan = GetBrushForLetter(true, st);
									tmpRowPlan = GetRowForLetter(st);
								}

								if (dontShowHandoverOrInstall && (tmpRowPlan >= 8))
								{
									tmpRowPlan = -1;
								}

								if (tmpRowPlan != -1)
								{
									tmpDrawRow = tmpRowPlan * 2 + 1;
									//This is the Plan so there 
									if (isDay(st, GameConstants.ACTIVITY_COMPLETE))
									{
										//Reusing the Points Array
										DiamondPts[0].X = (step1 * gridCellWidth + gridOffsetX) + (gridCellWidth / 2);
										DiamondPts[0].Y = ((tmpDrawRow) * gridCellHeight + gridOffsetY);
										DiamondPts[1].X = (step1 * gridCellWidth + gridOffsetX);
										DiamondPts[1].Y = ((tmpDrawRow) * gridCellHeight + gridOffsetY + (gridCellHeight / 2));
										DiamondPts[2].X = (step1 * gridCellWidth + gridOffsetX) + (gridCellWidth / 2);
										DiamondPts[2].Y = ((tmpDrawRow + 1) * gridCellHeight + gridOffsetY);
										DiamondPts[3].X = (step1 + 1) * gridCellWidth + gridOffsetX;
										DiamondPts[3].Y = (tmpDrawRow) * gridCellHeight + gridOffsetY + (gridCellHeight / 2);
										DiamondPts[4].X = (step1 * gridCellWidth + gridOffsetX) + (gridCellWidth / 2);
										DiamondPts[4].Y = ((tmpDrawRow) * gridCellHeight + gridOffsetY);
										g.FillPolygon(tmpbrushPlan, DiamondPts, System.Drawing.Drawing2D.FillMode.Alternate);
									}
									else
									{
										g.FillRectangle(tmpbrushPlan,
											(step1 * gridCellWidth + gridOffsetX), ((tmpDrawRow) * gridCellHeight + gridOffsetY),
											gridCellWidth - 0, gridCellHeight - 0);
									}
								}
							}
						}
								
						//===============================================================
						//Draw the Actual Aspect second==================================
						//===============================================================
						List<string> actualStates = new List<string> ();
						{
							string newActualStateString = getProjectActualPhaseByProjectByDay(this.DisplayProject, step1);
							actualStates.Add(newActualStateString);
						}

						foreach (string st2 in actualStates)
						{
							if ((step1 < currentDay) && (st2 != string.Empty) && (st2 != " "))
							{
								if (isDay(st2, GameConstants.ACTIVITY_NOMONEY) == false)
								{
									tmpPenActual = GetPenForLetter(false, st2);
									tmpbrushActual = GetBrushForLetter(false, st2);
									tmpRowActual = GetRowForLetter(st2);
								}

								if (dontShowHandoverOrInstall && (tmpRowActual >= 8))
								{
									tmpRowActual = -1;
								}

								if (tmpRowActual != -1)
								{
									//Determine the Row that we need to draw on 
									tmpDrawRow = tmpRowActual * 2 + 2;
									//Determine What type of Day, we are drawing 
									DayType = emProjectPerfChartDayType.DEFAULT;

									if (isDay(st2, GameConstants.ACTIVITY_NOMONEY))
									{
										DayType = emProjectPerfChartDayType.MONEYDAY;
									}
									else
									{
										if (isDay(st2, GameConstants.ACTIVITY_INSTALL_FAIL))
										{
											DayType = emProjectPerfChartDayType.INSTALL_FAIL;
										}
										if (isDay(st2, GameConstants.ACTIVITY_INSTALLED))
										{
											DayType = emProjectPerfChartDayType.INSTALL_OK;
										}
										if (isDay(st2, GameConstants.ACTIVITY_COMPLETE))
										{
											DayType = emProjectPerfChartDayType.COMPLETE;
										}
									}
									//Draw the Day 
									switch ((int) DayType)
									{
										case (int) emProjectPerfChartDayType.MONEYDAY:
											if (DollarHeight < gridCellHeight)
											{
												RectangleF gridSquare = new RectangleF(gridOffsetX + (step1 * gridCellWidth), gridOffsetY + (tmpDrawRow * gridCellHeight),
																						gridCellWidth, gridCellHeight);

												StringFormat format = new StringFormat();
												format.Alignment = StringAlignment.Center;
												format.LineAlignment = StringAlignment.Center;

												g.DrawString("$", dollarFont, brushBlack, gridSquare, format);
											}
											g.DrawRectangle(tmpPenActual,
												(step1 * gridCellWidth + gridOffsetX), ((tmpDrawRow) * gridCellHeight + gridOffsetY),
												gridCellWidth - 0, gridCellHeight - 0);
											g.DrawRectangle(tmpPenActual,
												(step1 * gridCellWidth + gridOffsetX) + 1, ((tmpDrawRow) * gridCellHeight + gridOffsetY) + 1,
												gridCellWidth - 0 - 2, gridCellHeight - 0 - 2);
											g.DrawRectangle(tmpPenActual,
												(step1 * gridCellWidth + gridOffsetX) + 2, ((tmpDrawRow) * gridCellHeight + gridOffsetY) + 2,
												gridCellWidth - 0 - 4, gridCellHeight - 0 - 4);
											break;
										case (int) emProjectPerfChartDayType.INSTALL_FAIL:

											if (DollarHeight < gridCellHeight)
											{
												g.DrawString("F", font, brushBlack,
													step1 * gridCellWidth + gridOffsetX + (gridCellWidth / 2) - 5,
													(tmpDrawRow) * gridCellHeight + gridOffsetY + (gridCellHeight / 2) - 9);
											}
											g.DrawRectangle(tmpPenActual,
												(step1 * gridCellWidth + gridOffsetX), ((tmpDrawRow) * gridCellHeight + gridOffsetY),
												gridCellWidth - 0, gridCellHeight - 0);
											g.DrawRectangle(tmpPenActual,
												(step1 * gridCellWidth + gridOffsetX) + 1, ((tmpDrawRow) * gridCellHeight + gridOffsetY) + 1,
												gridCellWidth - 0 - 2, gridCellHeight - 0 - 2);
											g.DrawRectangle(tmpPenActual,
												(step1 * gridCellWidth + gridOffsetX) + 2, ((tmpDrawRow) * gridCellHeight + gridOffsetY) + 2,
												gridCellWidth - 0 - 4, gridCellHeight - 0 - 4);

											break;
										case (int) emProjectPerfChartDayType.COMPLETE:
											//Reusing the Points Array
											DiamondPts[0].X = (step1 * gridCellWidth + gridOffsetX) + (gridCellWidth / 2);
											DiamondPts[0].Y = ((tmpDrawRow) * gridCellHeight + gridOffsetY);
											DiamondPts[1].X = (step1 * gridCellWidth + gridOffsetX);
											DiamondPts[1].Y = ((tmpDrawRow) * gridCellHeight + gridOffsetY + (gridCellHeight / 2));
											DiamondPts[2].X = (step1 * gridCellWidth + gridOffsetX) + (gridCellWidth / 2);
											DiamondPts[2].Y = ((tmpDrawRow + 1) * gridCellHeight + gridOffsetY);
											DiamondPts[3].X = (step1 + 1) * gridCellWidth + gridOffsetX;
											DiamondPts[3].Y = (tmpDrawRow) * gridCellHeight + gridOffsetY + (gridCellHeight / 2);
											DiamondPts[4].X = (step1 * gridCellWidth + gridOffsetX) + (gridCellWidth / 2);
											DiamondPts[4].Y = ((tmpDrawRow) * gridCellHeight + gridOffsetY);

											g.FillPolygon(brushBlack, DiamondPts, System.Drawing.Drawing2D.FillMode.Alternate);
											break;
										case (int) emProjectPerfChartDayType.INSTALL_OK:
										case (int) emProjectPerfChartDayType.DEFAULT:
										default:
											g.FillRectangle(tmpbrushActual,
												(step1 * gridCellWidth + gridOffsetX), ((tmpDrawRow) * gridCellHeight + gridOffsetY),
												gridCellWidth - 0, gridCellHeight - 0);
											break;
									}

									// Are we paused?
									string projectName = projectNameByDisplaySlotNumber[DisplayProject];
									if (pausedStatusByTimeByProjectName.ContainsKey(projectName))
									{
										List<double> sortedTimes = new List<double> (pausedStatusByTimeByProjectName[projectName].Keys);
										sortedTimes.Sort();

										bool pausedToday = false;
										foreach (double time in sortedTimes)
										{
											if (time <= (step1 * 60))
											{
												pausedToday = pausedStatusByTimeByProjectName[projectName][time];
											}
											else
											{
												break;
											}
										}

										if (pausedToday)
										{
											Rectangle dayRectangle = new Rectangle ((step1 * gridCellWidth) + gridOffsetX,
											                                        (tmpDrawRow * gridCellHeight) + gridOffsetY,
										                                            gridCellWidth, gridCellHeight);

											int verticalMargin = 2;
											int barHeight = dayRectangle.Height - (2 * verticalMargin);
											int barThickness = barHeight * 2 / 5;
											int barGap = barThickness;

											Point centre = new Point (dayRectangle.Left + (dayRectangle.Width / 2),
											                          dayRectangle.Top + (dayRectangle.Height / 2));


											Rectangle leftBar = new Rectangle (centre.X - (barGap / 2) - barThickness,
																			   centre.Y - (barHeight / 2),
																			   barThickness, barHeight);

											Rectangle rightBar = new Rectangle (centre.X + (barGap / 2),
																			    centre.Y - (barHeight / 2),
																			    barThickness, barHeight);

											g.FillRectangle(Brushes.Red, leftBar);
											g.FillRectangle(Brushes.Red, rightBar);
										}
									}
								}
							}
						}
					}
				}
			}

			// Draw the "change freeze" text.
			for (int step = 0; step < GameConstants.MAX_NUMBER_DAYS; step++)
			{
				int height = (gridCellHeight * 21) + gridOffsetY;

				bool blockDay = (getOpsWorkActionOnDay(step) != string.Empty);
				if (blockDay)
				{
					using (Brush brush = new SolidBrush(changeFreezeTextColour))
					{
						RectangleF rectangle = new RectangleF((step * gridCellWidth) + gridOffsetX, 0,
															   gridCellWidth, height);

						StringFormat format = new StringFormat();
						format.Alignment = StringAlignment.Center;
						format.LineAlignment = StringAlignment.Center;

						g.DrawString(InsertNewlinesBetweenCharacters("CHANGE FREEZE"), font, brush, rectangle, format);
					}
				}
			}

			//Draw the Highlight day 
			if (HighLightDay != -1)
			{
				g.DrawRectangle(PenHighLight, (HighLightDay * gridCellWidth + gridOffsetX),
					(gridOffsetY + gridCellHeight), gridCellWidth - 0, gridCellHeight * (MaxRows - 1));
				g.DrawRectangle(PenHighLight, (HighLightDay * gridCellWidth + gridOffsetX) + 1,
					(gridOffsetY + gridCellHeight) + 1, gridCellWidth - 0 - 2, (gridCellHeight * (MaxRows - 1)) - 2);
			}

			if (disposeDollarFont)
			{
				dollarFont.Dispose();
			}
		}

		public void DrawGanttKeyChart(Graphics g)
		{
			int LeftX = 0;
			int RightX = 0; 
			int TopY = 0; 
			int BottomY = 0; 
			int DiamondSize= 6;
				
			try
			{
				LeftX = GanttBox.X; 
				RightX = GanttBox.Width - GanttBox.X;
				TopY = GanttBox.Y; 
				BottomY = GanttBox.Height - GanttBox.Y;

				if (ShowDebugConstructions)
				{
					g.DrawLine(PenKey, LeftX, TopY, LeftX , BottomY);
					g.DrawLine(PenKey, LeftX, BottomY, RightX , BottomY);
					g.DrawLine(PenKey, RightX, TopY, RightX , BottomY);
					g.DrawLine(PenKey, LeftX, TopY, RightX , TopY);
				}

				LeftX = GanttKeyBox.X; 
				RightX = GanttKeyBox.Width - GanttKeyBox.X;
				TopY = GanttKeyBox.Y; 
				BottomY = GanttKeyBox.Y - GanttKeyBox.Height;

				if (ShowDebugConstructions)
				{
					g.DrawLine(PenKey, LeftX, TopY, LeftX , BottomY);
					g.DrawLine(PenKey, LeftX, BottomY, RightX , BottomY);
					g.DrawLine(PenKey, RightX, TopY, RightX , BottomY);
					g.DrawLine(PenKey, LeftX, TopY, RightX , TopY);
				}

				//=========================================================================
				//==Draw the Gantt Key Information=========================================
				//=========================================================================

				if (DoWeShowPlans())
				{
					g.DrawString("- Planned Progress", font, brushBlack, LeftX + 20, BottomY + 5);
					g.FillRectangle(brushPlanBlocks, LeftX + 10, BottomY + 5 + 2, 10, 10);
				}

				g.DrawString("- Actual Progress", font, brushBlack, LeftX + 20, BottomY+22);
				g.FillRectangle(brushActualBlocks,LeftX + 10,BottomY+22+2,10,10);

				g.DrawString("$", font, brushBlack, LeftX + 375, BottomY+5);
				g.DrawString("- Delay due to No Budget ", font, brushBlack, LeftX + 375+15, BottomY+5);

				g.DrawString("F", font, brushBlack, LeftX + 375, BottomY+5+17);
				g.DrawString("- Failed Installation ", font, brushBlack, LeftX + 375+15, BottomY+5+17);
				//=========================================================================
				//==Draw the Diamonds====================================================== 
				//=========================================================================

				if (DoWeShowPlans())
				{
					//Reusing the Points Array
					DiamondPts[0].X = LeftX + 180;
					DiamondPts[0].Y = BottomY + 5 + 2;
					DiamondPts[1].X = LeftX + 180 + DiamondSize;
					DiamondPts[1].Y = BottomY + 5 + DiamondSize + 2;
					DiamondPts[2].X = LeftX + 180;
					DiamondPts[2].Y = BottomY + 5 + (DiamondSize * 2) + 2;
					DiamondPts[3].X = LeftX + 180 - DiamondSize;
					DiamondPts[3].Y = BottomY + 5 + DiamondSize + 2;
					DiamondPts[4].X = LeftX + 180;
					DiamondPts[4].Y = BottomY + 5 + 2;
					g.FillPolygon(brushPlanBlocks, DiamondPts, System.Drawing.Drawing2D.FillMode.Alternate);
					g.DrawString("- Planned Go Live Day ", font, brushBlack, LeftX + 190, BottomY + 5);
				}

				//Reusing the Points Array
				DiamondPts[0].X = LeftX + 180;
				DiamondPts[0].Y = BottomY+5+2+16;
				DiamondPts[1].X = LeftX + 180 + DiamondSize;
				DiamondPts[1].Y = BottomY+5+DiamondSize+2+16;
				DiamondPts[2].X = LeftX + 180;
				DiamondPts[2].Y = BottomY+5+(DiamondSize*2)+2+16;
				DiamondPts[3].X = LeftX + 180 - DiamondSize;
				DiamondPts[3].Y = BottomY+5+DiamondSize+2+16;
				DiamondPts[4].X = LeftX + 180;
				DiamondPts[4].Y = BottomY+5+2+16;
				g.FillPolygon(brushBlack, DiamondPts,System.Drawing.Drawing2D.FillMode.Alternate);
				g.DrawString("- Actual Go Live Day ", font, brushBlack, LeftX + 190, BottomY+5+16);
			}
			catch (Exception)
			{
			}
		}

		#endregion Gantt Drawing Methods 

		#region Cost Drawing Methods 

		public void DrawCostChart(Graphics g)
		{
			PenMoneyExSpend_SP.DashStyle = System.Drawing.Drawing2D.DashStyle.Solid;
			PenMoneyIncome_SP.DashStyle = System.Drawing.Drawing2D.DashStyle.DashDot;
			PenMoneyIncome_SIP.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;

			Boolean DrawGrid = true;
			Boolean DrawPhaseString = false;
			int MaxRows = 10;
			int gridOffsetX = 100; 
			int gridOffsetY = CostBox.Y + 10;

			int graphWidth = this.CostBox.Width;
			int graphHeight = this.CostBox.Height - 25;
			
			int gridCellWidth = (CostBox.Width - gridOffsetX) / MaxGameDays; 
			int gridCellHeight = graphHeight / MaxRows; 

			Boolean ConformalBudgetLine = true;

			string id_str = string.Empty;


			int columnWidth = (CostBox.Width - gridOffsetX) / MaxGameDays;
			int rowHeight = (graphHeight-30) / MaxRows;
			int rowCount = MaxRows;
			
			//Using different colours in the Grid really helps when debug 
			Pen PenGridV1 = ChartGridPen;
			Pen PenGridV2 = ChartGridPen;
			Pen PenGridH1 = ChartGridPen;
			Pen PenGridH2 = ChartGridPen;
			Boolean DrawEndofDay = true;
				
			try 
			{
				CostGridCellWidth = columnWidth;
				CostGridCellHeight = rowHeight;
				CostGridCellOffsetX = gridOffsetX;
				CostGridCellOffsetY = gridOffsetY;

				//Check that we have a good Handle before extracting the data and Drawing the Graph
				//If no Data then we can't draw the Axis and we don't know the scaling required so draw nothing 
				if (this.MyNodeTree != null)
				{
					// Column stripes.
					for (int day = 0; day < GameConstants.MAX_NUMBER_DAYS; day++)
					{
						using (Brush brush = new SolidBrush(columnColours[day % 2]))
						{
							g.FillRectangle(brush,
											(day * gridCellWidth) + gridOffsetX, gridOffsetY,
											gridCellWidth, (rowCount - 1) * gridCellHeight);
						}
					}					
					
					//===============================================================================
					//Step 1, Drawing Horizontal Grid Lines Lines (only always draw bottom axis line)
					//===============================================================================
					for (int step =0; step <= (rowCount); step++)
					{
						int xline = gridOffsetX+10-10;
						int yline = gridOffsetY+step*rowHeight;
						if (step == rowCount)
						{
							g.DrawLine(PenGridH1, xline, yline, xline+(columnWidth*(GameConstants.MAX_NUMBER_DAYS)), yline);
						}
						else
						{
							if (DrawGrid)
							{
								g.DrawLine(PenGridH1, xline, yline, xline+(columnWidth*(GameConstants.MAX_NUMBER_DAYS)), yline);
							}
						}
					}

					int ylineAxisTitle = gridOffsetY + graphHeight / 2;

					// Day labels along bottom.
					DrawRectangle(g,
								  0, gridOffsetY + (rowCount * rowHeight),
								  gridOffsetX - 0, 20,
									bottomDayColour, bottomDayTextColour, font,
								  StringAlignment.Near, StringAlignment.Center,
								  "");
					for (int step = 0; step < GameConstants.MAX_NUMBER_DAYS; step++)
					{
						Color colour = bottomDayColour;
						Color textcolour = bottomDayTextColour;
						int height = gridOffsetY + gridCellHeight;

						DrawRectangle(g,
									  gridOffsetX + (step * gridCellWidth), gridOffsetY + (rowCount * rowHeight),
									  gridCellWidth, 20,
										colour, textcolour, font,
									  StringAlignment.Center, StringAlignment.Near,
									  formatDaystr(step + 1));

						g.DrawLine(ChartGridPen, step * gridCellWidth + gridOffsetX, gridOffsetY, step * gridCellWidth + gridOffsetX, gridOffsetY + (rowHeight * rowCount));
					}

					g.DrawLine(ChartGridPen, GameConstants.MAX_NUMBER_DAYS * gridCellWidth + gridOffsetX, gridOffsetY, GameConstants.MAX_NUMBER_DAYS * gridCellWidth + gridOffsetX, gridOffsetY + (rowHeight * rowCount));

					//===============================================================================
					//Step 4, Calculating the Maximum Money Involved to determine scaling for axis===
					//===============================================================================
					int MaxMoney = 0;
					int MaxMoneySpend = 0;
					int MaxMoneyIncome = 0;
					//Draw the Money Axis Numbers 
					//Wish to display a single project 
					MaxMoneySpend = getBudgetExpendMaxValueForProject(DisplayProject);
					MaxMoneyIncome = getBudgetIncomeMaxValueForProject(DisplayProject);
					MaxMoney = Math.Max(10000, Math.Max(MaxMoneyIncome, MaxMoneySpend));

					// Tweak the upper limit of the axis so that the intervals are nice round figures.
					double verticalInterval = MaxMoney / rowCount;
					verticalInterval = RoundToNiceInterval(verticalInterval);
					MaxMoney = (int) (verticalInterval * rowCount);

					//===============================================================================
					//Step 5, Drawing the Money Lines================================================ 
					//===============================================================================

					// Cost legend.
					int logoHeight = (gridCellHeight / 2) + 5;
					DrawRectangle(g,
								  0, gridOffsetY - logoHeight,
								  gridOffsetX - 0, logoHeight,
								  costColours[1], Color.White, font,
								  StringAlignment.Center, StringAlignment.Center,
								  "COST ($K)");

					if (MaxMoney > 0)
					{
						int bottom = gridOffsetY + (rowCount * rowHeight) + 20;

						//Drawing the Money Axis in Thousands based on Max Money
						for (int step =0; step <= rowCount; step++)
						{
							int xline = gridOffsetX+10-28-10;
							int yline = gridOffsetY+step*rowHeight;
							int val = (MaxMoney * (rowCount-step)) / rowCount;

							DrawRectangle(g,
										  0, yline,
										  gridOffsetX - 0, Math.Min(rowHeight + 1, bottom - yline),
										  costColours[step % 2], Color.White, font,
										  StringAlignment.Center, StringAlignment.Near,
										  CONVERT.ToStr(val / 1000));
							g.DrawLine(Pens.White, 0, yline, gridOffsetX, yline);
						}

						// Iterate through all projects, only drawing the required one
						//
						int max_projects = GameConstants.MAX_NUMBER_SLOTS;
						if (display_seven_projects)
						{
							max_projects = 7;
						}

						for (int PrjStepper = 0; PrjStepper < max_projects; PrjStepper++)
						{
							if ((DisplayProject == allProjectsSlot)|((DisplayProject)==PrjStepper))
							{
								int MoneySpendCounter=0;
								int plannedSpendCounter = 0;
								int MoneyIncomeCounter=0;
								int MaxLimitY=0;
								int ylevel=0;
								int ylevel_Min=0;
								int minPlannedSpendY = 0;
								int prevX_Expend=-1;
								int	prevY_Expend=-1;
								int previousPlannedSpendY = -1;
								int NowX_Expend=-1;
								int	NowY_Expend=-1;
								int plannedSpendY = -1;
								int prevX_Income=-1;
								int	prevY_Income=-1;
								int NowX_Income=-1;
								int	NowY_Income=-1;
								int MoneyIncomeCounter_SIP=0;
								int NowX_Income_SIP=-1;
								int NowY_Income_SIP=-1;
								int prevX_Income_SIP=-1;
								int prevY_Income_SIP=-1;
								int prevX_AA_Income=-1;
								int prevY_AA_Income=-1;
								int prevX_AA_IncomeSIP=-1;
								int prevY_AA_IncomeSIP=-1;
								int prevX_AA2_Income=-1;
								int prevY_AA2_Income=-1;


								int halfstep = (columnWidth*10)/20;

								//Drawing the specific project 
								if ((DisplayProject != -1))
								{
									// _SP is blue: the budget line
									int MoneyValue =0;
									for (int step=0; step < GameConstants.MAX_NUMBER_DAYS; step++)
									{
										//Drawing the Budget Income Line 
										MoneyValue += getBudgetIncomeByProjectByDay(PrjStepper,step);
										MaxLimitY = gridOffsetY+ (rowCount*rowHeight);
										if (MoneyValue >0)
										{
											ylevel = (MaxLimitY) - ((rowCount*rowHeight)*MoneyValue)/MaxMoney;
											int StartX=gridOffsetX+10-10;
											int StopX=gridOffsetX+(columnWidth*(GameConstants.MAX_NUMBER_DAYS));
											if (ConformalBudgetLine == false)
											{
												g.DrawLine(PenMoneyIncome_SP, StartX, ylevel, StopX, ylevel);
											}
											
										}
										//Drawing the Phase String 
										if (DrawPhaseString)
										{
											string PhaseStr = getProjectActualPhaseByProjectByDay(PrjStepper, step);

											if (PhaseStr != string.Empty)
											{
												if (PhaseStr.Length>1)
												{
													string firstValue = PhaseStr.Substring(1,1);
													if (firstValue != "1")
													{
														int xpos = gridOffsetX+ step*columnWidth+((columnWidth*10)/20)+5-10;
														int ypos = gridOffsetY+ ((rowCount-1)*rowHeight);
														if (this.DisplayGanttGraph == false)
														{
															g.DrawString(firstValue, font, brushBlack, xpos, ypos);
														}
													}
												}
											}
										}
									}
								}

								//=======================================================================
								//==Drawing the Money Income Line (Budget Income)========================
								//== This is the old diagonal version ===================================
								//=======================================================================
								if ((ConformalBudgetLine == true))
								{
									for (int step=0; step < GameConstants.MAX_NUMBER_DAYS; step++)
									{
										MoneyIncomeCounter += getBudgetIncomeByProjectByDay(PrjStepper, step);
										MaxLimitY = gridOffsetY+ (rowCount*rowHeight);

										if (MaxMoney>0)
										{
											ylevel = (MaxLimitY) - ((rowCount*rowHeight)*MoneyIncomeCounter)/MaxMoney;
										}

										NowX_Income=gridOffsetX+step*columnWidth + halfstep + 10-10;
										NowY_Income=ylevel;

										prevX_Income=NowX_Income;
										prevY_Income=NowY_Income;

										if (DrawEndofDay == false)
										{
											//Drawing with start of Day
											g.DrawLine(this.PenMoneyIncome_SP, gridOffsetX+step*columnWidth + 10-10 , ylevel, gridOffsetX+(step+1)*columnWidth + 10, ylevel);
											if ((prevX_AA_Income != -1)&(prevY_AA_Income != -1))
											{
												g.DrawLine(this.PenMoneyIncome_SP, prevX_AA_Income , prevY_AA_Income, gridOffsetX+step*columnWidth + 10, ylevel);
											}
										}
										prevX_AA_Income = gridOffsetX+(step+1)*columnWidth + 10-10;
										prevY_AA_Income = ylevel;


										if (DrawEndofDay)
										{
											//Redesign for delayed jump (end of Day	
											if ((prevX_AA2_Income == -1)&(prevY_AA2_Income == -1))
											{
												g.DrawLine(this.PenMoneyIncome_SP, gridOffsetX+step*columnWidth + 10 -10, ylevel, gridOffsetX+(step+1)*columnWidth + 10, ylevel);
											}
											else
											{
												g.DrawLine(this.PenMoneyIncome_SP, gridOffsetX+step*columnWidth + 10-10, prevY_AA2_Income,  prevX_AA2_Income, prevY_AA2_Income);
												g.DrawLine(this.PenMoneyIncome_SP, prevX_AA2_Income, prevY_AA2_Income, prevX_AA2_Income, ylevel);
											}
										}
										prevX_AA2_Income = gridOffsetX+(step+2)*columnWidth + 10-10;
										prevY_AA2_Income = ylevel;

									}
								}
								
								//=======================================================================
								//==Drawing the Money Income Line (the SIP Budget Line)==================
								//== This is the old diagonal version==================================== 
								//=======================================================================
								if ((ConformalBudgetLine == true))
								{
									// SIP is green: the SIP expenditure.
									if (this.getProjectSIPBudget(PrjStepper)>0)
									{
										for (int step=0; step < GameConstants.MAX_NUMBER_DAYS; step++)
										{
											if (step==0)
											{
												MoneyIncomeCounter_SIP += getProjectSIPBudget(PrjStepper);
											}
											else
											{
												MoneyIncomeCounter_SIP += 0;
											}
											MaxLimitY = gridOffsetY+ (rowCount*rowHeight);
											if (MaxMoney>0)
											{
												ylevel = (MaxLimitY) - ((rowCount*rowHeight)*MoneyIncomeCounter_SIP)/MaxMoney;
											}
											NowX_Income_SIP=gridOffsetX+step*columnWidth + halfstep + 10-10;
											NowY_Income_SIP=ylevel;

											prevX_Income_SIP=NowX_Income_SIP;
											prevY_Income_SIP=NowY_Income_SIP;

											g.DrawLine(PenMoneyIncome_SIP, gridOffsetX+step*columnWidth, ylevel, gridOffsetX+(step+1)*columnWidth, ylevel);
											if ((prevX_AA_IncomeSIP != -1)&(prevY_AA_IncomeSIP != -1))
											{
												g.DrawLine(PenMoneyIncome_SIP, prevX_AA_IncomeSIP, prevY_AA_IncomeSIP, gridOffsetX+step*columnWidth, ylevel);
											}
											prevX_AA_IncomeSIP = gridOffsetX+(step+1)*columnWidth;
											prevY_AA_IncomeSIP = ylevel;

										}
									}
								}

								int currentDay = MyNodeTree.GetNamedNode("CurrentDay").GetIntAttribute("day", 0);

								//=======================================================================
								//==Drawing the Money Expenditure Lines=================================== 
								//=======================================================================
								for (int step = 0; step < GameConstants.MAX_NUMBER_DAYS; step++)
								{
									MoneySpendCounter += getBudgetExpenditureByProjectByDay(PrjStepper, step);
									plannedSpendCounter += GetPlannedBudgetExpenditureByProjectByDay(PrjStepper, step);

									MaxLimitY = gridOffsetY + (rowCount * rowHeight);

									if (MaxMoney > 0)
									{
										ylevel = (MaxLimitY) - ((rowCount * rowHeight) * MoneySpendCounter) / MaxMoney;
										plannedSpendY = MaxLimitY - ((rowCount * rowHeight * plannedSpendCounter) / MaxMoney);

										ylevel_Min = (MaxLimitY) - ((rowCount * rowHeight) * 0) / MaxMoney;
										minPlannedSpendY = MaxLimitY;
									}

									NowX_Expend = gridOffsetX + step * columnWidth + halfstep + 10 - 10;
									NowY_Expend = ylevel;
									if (prevX_Expend != -1)
									{
										if (DoWeShowPlans())
										{
											g.DrawLine(plannedSpendPen, prevX_Expend + halfstep, previousPlannedSpendY, NowX_Expend + halfstep, plannedSpendY);
										}

										if (step < currentDay)
										{
											g.DrawLine(PenMoneyExSpend_SP, prevX_Expend + halfstep, prevY_Expend, NowX_Expend + halfstep, NowY_Expend);
										}
									}
									else
									{
										if (DoWeShowPlans())
										{
											g.DrawLine(plannedSpendPen, gridOffsetX + 10 - 10, minPlannedSpendY, NowX_Expend + halfstep, NowY_Expend);
										}

										if (step < currentDay)
										{
											g.DrawLine(PenMoneyExSpend_SP, gridOffsetX + 10 - 10, ylevel_Min, NowX_Expend + halfstep, NowY_Expend);
										}
									}

									prevX_Expend = NowX_Expend;
									prevY_Expend = NowY_Expend;

									previousPlannedSpendY = plannedSpendY;
								}
							}
						}
					}
				}

				if (HighLightDay != -1)
				{
					g.DrawRectangle(PenHighLight, (HighLightDay*gridCellWidth+gridOffsetX+10-10),
						(gridOffsetY), gridCellWidth-0, (rowCount*rowHeight));
					g.DrawRectangle(PenHighLight, (HighLightDay*gridCellWidth+gridOffsetX+10-10)+1,
						(gridOffsetY)+1, gridCellWidth-0-2, (rowCount*rowHeight)-2);
				}
			}
			catch (Exception)
			{
			}
		}

		public void DrawCostKeyChart(Graphics g)
		{
			int LeftX = 0;
			int RightX = 0; 
			int TopY = 0; 
			int BottomY = 0; 
			
			PenMoneyExSpend_SP.DashStyle = System.Drawing.Drawing2D.DashStyle.Solid;
			PenMoneyIncome_SP.DashStyle = System.Drawing.Drawing2D.DashStyle.Solid;
			PenMoneyIncome_SIP.DashStyle = System.Drawing.Drawing2D.DashStyle.Solid;

			try
			{
				LeftX = CostBox.X; 
				RightX = CostBox.Width - CostBox.X;
				TopY = CostBox.Y; 
				BottomY = CostBox.Height - CostBox.Y;


				if (ShowDebugConstructions)
				{
					g.DrawLine(PenKey, LeftX, TopY, LeftX , BottomY);
					g.DrawLine(PenKey, LeftX, BottomY, RightX , BottomY);
					g.DrawLine(PenKey, RightX, TopY, RightX , BottomY);
					g.DrawLine(PenKey, LeftX, TopY, RightX , TopY);
				}

				LeftX = CostKeyBox.X; 
				RightX = CostKeyBox.Width - CostKeyBox.X;
				TopY = CostKeyBox.Y; 
				BottomY = CostKeyBox.Y - CostKeyBox.Height;

				if (ShowDebugConstructions)
				{
					g.DrawLine(PenKey, LeftX, TopY, LeftX , BottomY);
					g.DrawLine(PenKey, LeftX, BottomY, RightX , BottomY);
					g.DrawLine(PenKey, RightX, TopY, RightX , BottomY);
					g.DrawLine(PenKey, LeftX, TopY, RightX , TopY);
				}
				//=========================================================================
				//==Draw the Gantt Key Information=========================================
				//=========================================================================
				g.DrawString("- Budget Amount", font, brushBlack, LeftX + 25, BottomY+5);
				g.DrawString("- Estimated Investment", font, brushBlack, LeftX + 25, BottomY+22);

				g.DrawRectangle(PenMoneyIncome_SP, LeftX + 10,BottomY+5+2,3,10);
				g.DrawRectangle(PenMoneyIncome_SIP, LeftX + 10,BottomY+22+2,3,10);
				g.DrawRectangle(PenMoneyIncome_SP, LeftX + 10+5,BottomY+5+2,3,10);
				g.DrawRectangle(PenMoneyIncome_SIP, LeftX + 10+5,BottomY+22+2,3,10);

				g.DrawRectangle(PenMoneyExSpend_SP, LeftX + 220,BottomY+22+2,3,10);
				g.DrawRectangle(PenMoneyExSpend_SP, LeftX + 220+5,BottomY+22+2,3,10);
				g.DrawString("- Actual Expenditure", font, brushBlack, LeftX + 230, BottomY + 22);

				if (DoWeShowPlans())
				{
					g.DrawString("- Planned Expenditure", font, brushBlack, LeftX + 230, BottomY + 5);
					g.DrawRectangle(plannedSpendPen, LeftX + 220, BottomY + 5 + 2, 3, 10);
					g.DrawRectangle(plannedSpendPen, LeftX + 220 + 5, BottomY + 5 + 2, 3, 10);
				}
			}
			catch (Exception)
			{
			}
		}

		#endregion Cost Drawing Methods 

		#region Generic Paint Methods

		private void DrawHighLight(int NewHighlightDay)
		{
			this.HighLightDay = NewHighlightDay;
			Refresh();
		}

		public string GetPlatformDisplay(string PlatformID)
		{
			string str = string.Empty;
			switch (PlatformID)
			{
				case "1":str = "X";break;
				case "2":str = "Y";break;
				case "3":str = "Z";break;
			}
			return str;
		}
		
		private void PrjPerfControl_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			g.SmoothingMode = SmoothingMode.HighQuality;

			try 
			{
				BuildBoundaryBoxes();

				if (ShowDebugConstructions)
				{
					e.Graphics.DrawLine(PenBounds, DrawingBox.X,DrawingBox.Y, DrawingBox.Width, DrawingBox.Y);
					e.Graphics.DrawLine(PenBounds, DrawingBox.Width,DrawingBox.Y, DrawingBox.Width, DrawingBox.Height);
					e.Graphics.DrawLine(PenBounds, DrawingBox.X,DrawingBox.Height, DrawingBox.Width, DrawingBox.Height);
					e.Graphics.DrawLine(PenBounds, DrawingBox.X,DrawingBox.Y, DrawingBox.X, DrawingBox.Height);
				}

				if (this.DisplayGanttGraph)
				{
					DrawGanttChart(e.Graphics);
					if (this.DisplayCostGraph == false)
					{
						DrawGanttKeyChart(e.Graphics);
					}
				}
				if (this.DisplayCostGraph)
				{
					DrawCostChart(e.Graphics);

					if (this.DisplayGanttGraph == false)
					{
						DrawCostKeyChart(e.Graphics);
					}
				}
			}
			catch (Exception evc)
			{
				string st1 = evc.Message;//Just prevent the exception taking out the app
			}
		}

		#endregion Generic Paint Methods

		#region Highlight Region  
		
		private Boolean ResolveHitPoint(int mx, int my, int GridCellOffsetX, int GridCellOffsetY, 
			int GridCellWidth, int GridCellHeight, out int cellx, out int celly)
		{
			int HitPointX=0;
			int HitPointY=0;
			int HitCellX=-1;
			int HitCellY=-1;
			Boolean OpSuccess = false;

			HitPointX = mx - GridCellOffsetX;
			HitPointY = my - GridCellOffsetY;
			cellx = 0;
			celly = 0;

			if ((HitPointX >-1)&(HitPointY > -1))
			{
				HitCellX = HitPointX / GridCellWidth;
				HitCellY = HitPointY / GridCellHeight; 

				cellx = HitCellX;
				celly = HitCellY;

				// the columns are limited to 0 and 29 
				if ((cellx > -1)& (cellx<GameConstants.MAX_NUMBER_DAYS))
				{
					OpSuccess = true;
				}
			}
			return OpSuccess;
		}

		private void PrjPerfControl_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			int HitPointX=0;
			int HitPointY=0;
			int cellx =0;
			int celly =0;
			Boolean proceed = false;

			if (e.Button != MouseButtons.Left) return;
				
			HitPointX = e.X;
			HitPointY = e.Y;
			
			if ((int)WhichChart == (int)emProjectPerfChartType.BOTH)	
			{
				if (CostBox.Contains(HitPointX,HitPointY))
				{
					if (ResolveHitPoint(HitPointX, HitPointY, CostGridCellOffsetX, CostGridCellOffsetY, 
						CostGridCellWidth, CostGridCellHeight, out cellx, out celly))
					{
						proceed = true;
					}
				}
				if (GanttBox.Contains(HitPointX,HitPointY))
				{
					if (ResolveHitPoint(HitPointX, HitPointY, GanttGridCellOffsetX, GanttGridCellOffsetY, 
						GanttGridCellWidth, GanttGridCellHeight, out cellx, out celly))
					{
						proceed = true;
					}
				}
			}
			if ((int)WhichChart == (int)emProjectPerfChartType.COST)	
			{
				if (CostBox.Contains(HitPointX,HitPointY))
				{
					if (ResolveHitPoint(HitPointX, HitPointY, CostGridCellOffsetX, CostGridCellOffsetY, 
						CostGridCellWidth, CostGridCellHeight, out cellx, out celly))
					{
						proceed = true;
					}
				}
			}
			if ((int)WhichChart == (int)emProjectPerfChartType.GANTT)	
			{
				if (GanttBox.Contains(HitPointX,HitPointY))
				{
					if (ResolveHitPoint(HitPointX, HitPointY, GanttGridCellOffsetX, GanttGridCellOffsetY, 
						GanttGridCellWidth, GanttGridCellHeight, out cellx, out celly))
					{
						proceed = true;
					}
				}
			}

			//
			if (proceed)
			{
				DrawHighLight(cellx);
			}
		}

		#endregion Highlight Region  

		void ExtractLog ()
		{
			string filename = gameFile.GetRoundFile(DisplayRound, "NetworkIncidents.log", GameFile.GamePhase.OPERATIONS);
			NodeTree model = MyNodeTree;

			if (DisplayRound != gameFile.CurrentRound)
			{
				model = gameFile.GetNetworkModel(DisplayRound, GameFile.GamePhase.OPERATIONS);
			}

			projectNameByDisplaySlotNumber = new Dictionary<int, string>();
			projectNameByUniqueNumber = new Dictionary<int, string>();
			projectNameByFinancialNodeName = new Dictionary<string,string> ();

			plannedSpendByTimeByProjectName = new Dictionary<string, Dictionary<double, int>> ();

			plannedStateNameByTimeByProjectName = new Dictionary<string, Dictionary<double, string>> ();
			stateNameByTimeByProjectName = new Dictionary<string, Dictionary<double, string>> ();
			pausedStatusByTimeByProjectName = new Dictionary<string, Dictionary<double, bool>> ();

			spendByTimeByProjectName = new Dictionary<string, Dictionary<double, int>> ();
			budgetByTimeByProjectName = new Dictionary<string, Dictionary<double, int>> ();
			projectedCostByTimeByProjectName = new Dictionary<string, Dictionary<double, int>> ();

			BasicIncidentLogReader reader = new BasicIncidentLogReader (filename);

			Node projects = model.GetNamedNode("pm_projects_running");
			foreach (Node project in projects.GetChildrenOfType("project"))
			{
				Node financialData = project.GetFirstChildOfType("financial_data");
				Node projectData = project.GetFirstChildOfType("project_data");

				projectNameByDisplaySlotNumber.Add(project.GetIntAttribute("slot", 0), project.GetAttribute("name"));
				projectNameByUniqueNumber.Add(project.GetIntAttribute("uid", 0), project.GetAttribute("name"));
				projectNameByFinancialNodeName.Add(financialData.GetAttribute("name"), project.GetAttribute("name"));

				reader.WatchApplyAttributes(project.GetAttribute("name"), new LogLineFoundDef.LineFoundHandler (reader_ProjectAttributesChanged));
				reader.WatchApplyAttributes(financialData.GetAttribute("name"), new LogLineFoundDef.LineFoundHandler (reader_ProjectFinancialAttributesChanged));

				reader.WatchApplyAttributes(projectData.GetAttribute("name"), new LogLineFoundDef.LineFoundHandler (reader_ProjectDataAttributesChanged));				
			}
			reader.WatchCreatedNodes("pm_projects", new LogLineFoundDef.LineFoundHandler (reader_CreatedProjectNode));
			
			reader.Run();

			Node projectPlans = model.GetNamedNode("project_plans");
			foreach (Node projectPlan in projectPlans.GetChildrenOfType("project_plan"))
			{
				Dictionary<double, string> plannedStateByTime = new Dictionary<double,string> ();
				Dictionary<double, int> plannedSpendByTime = new Dictionary<double, int> ();

				plannedStateNameByTimeByProjectName.Add(projectPlan.GetAttribute("project"), plannedStateByTime);
				plannedSpendByTimeByProjectName.Add(projectPlan.GetAttribute("project"), plannedSpendByTime);

				int spend = 0;

				foreach (Node dayPlan in projectPlan.GetChildrenOfType("day_plan"))
				{
					double time = 60 * (dayPlan.GetIntAttribute("day", 0) - 1);

					plannedStateByTime.Add(time, dayPlan.GetAttribute("stage"));

					spend += dayPlan.GetIntAttribute("spend", 0);
					plannedSpendByTime.Add(time, spend);
				}
			}
		}

		void reader_ProgramAttributesChanged (object sender, string key, string line, double time)
		{
			string programName = BasicIncidentLogReader.ExtractValue(line, "i_name");

			string workingStageString = BasicIncidentLogReader.ExtractValue(line, "stage_working");
			string completedStageString = BasicIncidentLogReader.ExtractValue(line, "stage_completed");
			emProjectOperationalState stage = emProjectOperationalState.PROJECT_STATE_UNKNOWN;
			if (completedStageString == "H")
			{
				stage = emProjectOperationalState.PROJECT_STATE_COMPLETED;
			}
			else if (workingStageString != "")
			{
				stage = GameConstants.ProjectStateFromStageName(workingStageString);
			}

			if (stage != emProjectOperationalState.PROJECT_STATE_UNKNOWN)
			{
				if (! stateNameByTimeByProjectName.ContainsKey(programName))
				{
					stateNameByTimeByProjectName.Add(programName, new Dictionary<double, string>());
				}

				if (stateNameByTimeByProjectName[programName].ContainsKey(time))
				{
					stateNameByTimeByProjectName[programName][time] = stage.ToString();
				}
				else
				{
					stateNameByTimeByProjectName[programName].Add(time, stage.ToString());
				}
			}

			string spendString = BasicIncidentLogReader.ExtractValue(line, "spend");
			if (spendString != "")
			{
				int spend = CONVERT.ParseIntSafe(spendString, 0);

				if (! spendByTimeByProjectName.ContainsKey(programName))
				{
					spendByTimeByProjectName.Add(programName, new Dictionary<double, int> ());
				}

				if (spendByTimeByProjectName[programName].ContainsKey(time))
				{
					spendByTimeByProjectName[programName][time] = spend;
				}
				else
				{
					spendByTimeByProjectName[programName].Add(time, spend);
				}
			}

			string budgetString = BasicIncidentLogReader.ExtractValue(line, "budget");
			if (budgetString != string.Empty)
			{
				int budget = CONVERT.ParseInt(budgetString);

				if (! budgetByTimeByProjectName.ContainsKey(programName))
				{
					budgetByTimeByProjectName.Add(programName, new Dictionary<double, int> ());
				}

				if (budgetByTimeByProjectName[programName].ContainsKey(time))
				{
					budgetByTimeByProjectName[programName][time] = budget;
				}
				else
				{
					budgetByTimeByProjectName[programName].Add(time, budget);
				}
			}

			string solventString = BasicIncidentLogReader.ExtractValue(line, "over_budget");
			if (solventString != string.Empty)
			{
				if (! stateNameByTimeByProjectName.ContainsKey(programName))
				{
					stateNameByTimeByProjectName.Add(programName, new Dictionary<double, string> ());
				}

				bool solvent = ! CONVERT.ParseBool(solventString, false);
				emProjectOperationalState state = emProjectOperationalState.PROJECT_STATE_UNKNOWN;

				if (! solvent)
				{
					state = emProjectOperationalState.PROJECT_STATE_STALLED_NO_MONEY;
				}
				else
				{
					List<double> orderedTimes = new List<double>(stateNameByTimeByProjectName[programName].Keys);
					orderedTimes.Sort();

					if (orderedTimes.Count > 0)
					{
						// Are we currently stalled?
						string lastKnownStage = stateNameByTimeByProjectName[programName][orderedTimes[orderedTimes.Count - 1]];
						if ((emProjectOperationalState) Enum.Parse(typeof(emProjectOperationalState), lastKnownStage) == emProjectOperationalState.PROJECT_STATE_STALLED_NO_MONEY)
						{
							// Find the last non-stalled state and return to that.
							int index = orderedTimes.Count - 1;

							while (index >= 0)
							{
								emProjectOperationalState oldState = (emProjectOperationalState) Enum.Parse(typeof(emProjectOperationalState), stateNameByTimeByProjectName[programName][orderedTimes[index]]);
								if (oldState != emProjectOperationalState.PROJECT_STATE_STALLED_NO_MONEY)
								{
									state = oldState;
									break;
								}
								index--;
							}

							// But we only change back to working at the start of the next day.
							time = 60 * (int) Math.Ceiling(time / 60.0);
						}
					}
				}

				if (state != emProjectOperationalState.PROJECT_STATE_UNKNOWN)
				{
					if (stateNameByTimeByProjectName[programName].ContainsKey(time))
					{
						stateNameByTimeByProjectName[programName][time] = state.ToString();
					}
					else
					{
						stateNameByTimeByProjectName[programName].Add(time, state.ToString());
					}
				}
			}
		}

		void reader_CreatedProjectNode (object sender, string key, string line, double time)
		{
		}

		void reader_ProjectAttributesChanged (object sender, string key, string line, double time)
		{
			string projectName = BasicIncidentLogReader.ExtractValue(line, "i_name");

			bool changePauseState = false;
			bool newPauseState = false;
			string statusRequest = BasicIncidentLogReader.ExtractValue(line, "status_request");
			switch (statusRequest)
			{
				case "prepause":
					changePauseState = true;
					newPauseState = true;
					break;

				case "preresume":
					changePauseState = true;
					newPauseState = false;
					break;
			}
			if (changePauseState)
			{
				if (! pausedStatusByTimeByProjectName.ContainsKey(projectName))
				{
					pausedStatusByTimeByProjectName.Add(projectName, new Dictionary<double, bool> ());
				}

				pausedStatusByTimeByProjectName[projectName][time] = newPauseState;
			}

			string stateName = BasicIncidentLogReader.ExtractValue(line, "state_name");
			if (stateName != string.Empty)
			{
				if (! stateNameByTimeByProjectName.ContainsKey(projectName))
				{
					stateNameByTimeByProjectName.Add(projectName, new Dictionary<double, string> ());
				}

				if (stateNameByTimeByProjectName[projectName].ContainsKey(time))
				{
					stateNameByTimeByProjectName[projectName][time] = stateName;
				}
				else
				{
					stateNameByTimeByProjectName[projectName].Add(time, stateName);
				}
			}

			string cancelString = BasicIncidentLogReader.ExtractValue(line, "user_cancel");
			if (cancelString != string.Empty)
			{
				if (CONVERT.ParseBool(cancelString, false))
				{
					// Delete this project entirely from the logs.
					stateNameByTimeByProjectName.Remove(projectName);
					plannedStateNameByTimeByProjectName.Remove(projectName);

					spendByTimeByProjectName.Remove(projectName);
					budgetByTimeByProjectName.Remove(projectName);
					projectedCostByTimeByProjectName.Remove(projectName);
				}
			}
		}

		void reader_ProjectDataAttributesChanged (object sender, string key, string line, double time)
		{
			string tail = "_project_data";
			string projectName = BasicIncidentLogReader.ExtractValue(line, "i_name");
			projectName = projectName.Substring(0, projectName.Length - tail.Length);

			string timeFailureString = BasicIncidentLogReader.ExtractValue(line, "install_timefailure");
			if (timeFailureString != "")
			{
				bool timeFailure = CONVERT.ParseBool(timeFailureString, false);

				if (timeFailure)
				{
					string stateName = emProjectOperationalState.PROJECT_STATE_INSTALLED_FAIL.ToString();

					if (! stateNameByTimeByProjectName.ContainsKey(projectName))
					{
						stateNameByTimeByProjectName.Add(projectName, new Dictionary<double, string>());
					}

					// Get the prior state.
					List<double> orderedTimes = new List<double>(stateNameByTimeByProjectName[projectName].Keys);
					string priorStateName = "";
					orderedTimes.Sort();
					{
						if (orderedTimes.Count > 0)
						{
							priorStateName = stateNameByTimeByProjectName[projectName][orderedTimes[orderedTimes.Count - 1]];
						}
					}

					// That prior state is only worth reapplying after the install failure if it's a working stage.
					switch ((emProjectOperationalState) Enum.Parse(typeof(emProjectOperationalState), priorStateName))
					{
						case emProjectOperationalState.PROJECT_STATE_A:
						case emProjectOperationalState.PROJECT_STATE_B:
						case emProjectOperationalState.PROJECT_STATE_C:
						case emProjectOperationalState.PROJECT_STATE_D:
						case emProjectOperationalState.PROJECT_STATE_E:
						case emProjectOperationalState.PROJECT_STATE_F:
						case emProjectOperationalState.PROJECT_STATE_G:
						case emProjectOperationalState.PROJECT_STATE_H:
							break;

						default:
							priorStateName = "";
							break;
					}

					if (stateNameByTimeByProjectName[projectName].ContainsKey(time))
					{
						stateNameByTimeByProjectName[projectName][time] = stateName;
					}
					else
					{
						stateNameByTimeByProjectName[projectName].Add(time, stateName);
					}

					if (priorStateName != "")
					{
						stateNameByTimeByProjectName[projectName][time + 60] = priorStateName;
					}
				}
			}
		}

		void reader_ProjectFinancialAttributesChanged (object sender, string key, string line, double time)
		{
			string financialNodeName = BasicIncidentLogReader.ExtractValue(line, "i_name");
			string projectName = projectNameByFinancialNodeName[financialNodeName];

			string spendString = BasicIncidentLogReader.ExtractValue(line, "spend");
			if (spendString != string.Empty)
			{
				int spend = CONVERT.ParseInt(spendString);

				if (! spendByTimeByProjectName.ContainsKey(projectName))
				{
					spendByTimeByProjectName.Add(projectName, new Dictionary<double, int> ());
				}

				if (spendByTimeByProjectName[projectName].ContainsKey(time))
				{
					spendByTimeByProjectName[projectName][time] = spend;
				}
				else
				{
					spendByTimeByProjectName[projectName].Add(time, spend);
				}
			}

			string budgetString = BasicIncidentLogReader.ExtractValue(line, "budget_player");
			if (budgetString != string.Empty)
			{
				int budget = CONVERT.ParseInt(budgetString);

				if (! budgetByTimeByProjectName.ContainsKey(projectName))
				{
					budgetByTimeByProjectName.Add(projectName, new Dictionary<double, int>());
				}

				if (budgetByTimeByProjectName[projectName].ContainsKey(time))
				{
					budgetByTimeByProjectName[projectName][time] = budget;
				}
				else
				{
					budgetByTimeByProjectName[projectName].Add(time, budget);
				}
			}

			string projectedSpendString = BasicIncidentLogReader.ExtractValue(line, "budget_defined");
			if (projectedSpendString != string.Empty)
			{
				int projectedSpend = CONVERT.ParseInt(projectedSpendString);

				if (! projectedCostByTimeByProjectName.ContainsKey(projectName))
				{
					projectedCostByTimeByProjectName.Add(projectName, new Dictionary<double, int>());
				}

				if (projectedCostByTimeByProjectName[projectName].ContainsKey(time))
				{
					projectedCostByTimeByProjectName[projectName][time] = projectedSpend;
				}
				else
				{
					projectedCostByTimeByProjectName[projectName].Add(time, projectedSpend);
				}
			}

			string solventString = BasicIncidentLogReader.ExtractValue(line, "solvent");
			if (solventString != string.Empty)
			{
				if (! stateNameByTimeByProjectName.ContainsKey(projectName))
				{
					stateNameByTimeByProjectName.Add(projectName, new Dictionary<double, string>());
				}

				bool solvent = CONVERT.ParseBool(solventString, false);
				emProjectOperationalState state = emProjectOperationalState.PROJECT_STATE_UNKNOWN;

				if (! solvent)
				{
					state = emProjectOperationalState.PROJECT_STATE_STALLED_NO_MONEY;
				}
				else
				{
					List<double> orderedTimes = new List<double> (stateNameByTimeByProjectName[projectName].Keys);
					orderedTimes.Sort();

					if (orderedTimes.Count > 0)
					{
						// Are we currently stalled?
						if ((emProjectOperationalState) Enum.Parse(typeof(emProjectOperationalState), stateNameByTimeByProjectName[projectName][orderedTimes[orderedTimes.Count - 1]]) == emProjectOperationalState.PROJECT_STATE_STALLED_NO_MONEY)
						{
							// Find the last non-stalled state and return to that.
							int index = orderedTimes.Count - 1;

							while (index >= 0)
							{
								emProjectOperationalState oldState = (emProjectOperationalState) Enum.Parse(typeof(emProjectOperationalState), stateNameByTimeByProjectName[projectName][orderedTimes[index]]);
								if (oldState != emProjectOperationalState.PROJECT_STATE_STALLED_NO_MONEY)
								{
									state = oldState;
									break;
								}
								index--;
							}

							// But we only change back to working at the start of the next day.
							time = 60 * (int) Math.Ceiling(time / 60.0);
						}
					}
				}

				if (state != emProjectOperationalState.PROJECT_STATE_UNKNOWN)
				{
					if (stateNameByTimeByProjectName[projectName].ContainsKey(time))
					{
						stateNameByTimeByProjectName[projectName][time] = state.ToString();
					}
					else
					{
						stateNameByTimeByProjectName[projectName].Add(time, state.ToString());
					}
				}
			}
		}

		void DrawRectangle (Graphics graphics, int left, int top, int width, int height,
			Color background, Color foreground, Font font,
			StringAlignment horizontalAlign, StringAlignment verticalAlign,
			string text)
		{
			using (Brush brush = new SolidBrush (background))
			{
				graphics.FillRectangle(brush, left, top, width, height);
			}

			using (Brush brush = new SolidBrush (foreground))
			{
				RectangleF rectangle = new RectangleF(left, top, width, height);

				StringFormat format = new StringFormat();
				format.Alignment = horizontalAlign;
				format.LineAlignment = verticalAlign;

				graphics.DrawString(text, font, brush, rectangle, format);
			}
		}

		string InsertNewlinesBetweenCharacters (string input)
		{
			StringBuilder output = new StringBuilder();
			foreach (char character in input)
			{
				output.Append(character); 
				output.Append("\n");
			}

			return output.ToString();
		}

		/// <summary>
		/// Given an interval for a graph, rounds it up to the next "nice" figure (ie 10^n * {1, 2, 2.5, 3 or 5}).
		/// </summary>
		double RoundToNiceInterval (double start)
		{
			int exponent = (int) Math.Floor(Math.Log10(start));

			double radix = start / Math.Pow(10, exponent);

			if (radix <= 1)
			{
				radix = 1;
			}
			else if (radix <= 1.8)
			{
				radix = 2;
			}
			else if (radix <= 2)
			{
				radix = 2.2;
			}
			else if (radix <= 2.2)
			{
				radix = 2.5;
			}
			else if (radix <= 2.5)
			{
				radix = 3.0;
			}
			else if (radix <= 3.0)
			{
				radix = 3.5;
			}
			else if (radix <= 3.5)
			{
				radix = 4.0;
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
	}
}