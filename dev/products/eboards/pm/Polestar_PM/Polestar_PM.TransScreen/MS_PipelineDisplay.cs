using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Text;
using System.Data;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;

using LibCore;
using Network;
using CoreUtils;

namespace Polestar_PM.TransScreen
{
	/// <summary>
	/// The MS_PipelineDisplay is a circular control which display both projects and ops data 
	/// Due to the complicated drawing routines, it might be an execise to cut this control into smaller ones.
	/// The redraw flicker of multiple might be quite hard to manage. 
	/// 
	/// The Pipeline in McKinley is defined as 20 days
	/// There are only 2 project allowed in the Service Pipeline
	/// 
	/// There are 5 strands to this control 
	///   Presentation and Mathematical methods for drawing the circular display
	///   Extracting, Monitoring and Management of Projects Data from the network 
	///   Extracting, Monitoring and Management of Operations Data from the network 
	///   Extracting, Monitoring and Management of Current Day from the network
	///   Extracting the current Time for Display in the centre of the Control 
	/// </summary>
	public class MS_PipelineDisplay : System.Windows.Forms.UserControl
	{
		[DllImport("User32.dll", CharSet=CharSet.Auto)]
		private static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool redraw);

		//Game Data Network 
		protected NodeTree _NetworkModel;
		protected Node CalendarNode = null;
		protected Node MyCurrentDayNode = null;
		protected Node MyCurrentTimeNode = null;
		protected Node MyCurrentPrePlayTimeNode = null;
		protected Node MyProjectsNode = null; 
		protected Node MyDevelopmentSpendNode = null;
		
		//Game Data 
		protected int CurrentRound = 1;
		protected int calendarLength = 30;
		protected int CurrentDay = 1;
		protected int MaxDay = 20; 
		protected string ccImageFileName = "";
		protected int roundbudgetleft = 0;
		protected int currentspendtotal = 0; 
		protected int actualcosttotal = 0; 
		protected Hashtable ProjectNodes = new Hashtable();
		protected Hashtable ProjectNodesByName = new Hashtable();
		
		//Presentation Variables : Fonts
		Font BoldFont14 = CoreUtils.SkinningDefs.TheInstance.GetFont(14, FontStyle.Bold);
		Font NormalFont11 = CoreUtils.SkinningDefs.TheInstance.GetFont(11, FontStyle.Regular);
		Font BoldFont11 = CoreUtils.SkinningDefs.TheInstance.GetFont(11, FontStyle.Bold);
		Font NormalFont8 = CoreUtils.SkinningDefs.TheInstance.GetFont(8, FontStyle.Regular);
		Font BoldFont8 = CoreUtils.SkinningDefs.TheInstance.GetFont(8, FontStyle.Bold);
		//Presentation Variables : Pens
		Pen White2Pen = new Pen(Color.White,2);
		//Presentation Variables : Brush
		Brush PrePlayTimeBrush = new SolidBrush(Color.Plum);
		Brush PlayTimeBrush = new SolidBrush(Color.DimGray);
		Brush DayBrush = new SolidBrush(Color.FromArgb(165,196,197));
		Brush EmptyBrush = new SolidBrush(Color.Gainsboro);
		Brush PrjDefineBrush = new SolidBrush(Color.FromArgb(104,55,3));
		Brush PrjDesignBrush = new SolidBrush(Color.FromArgb(158,83,4));
		Brush PrjBuildBrush = new SolidBrush(Color.FromArgb(208,112,6));
		Brush PrjTestBrush = new SolidBrush(Color.FromArgb(221,146,70));
		Brush PrjHandoverBrush = new SolidBrush(Color.FromArgb(245,219,191)); //233,181,128));
		Brush PrjInstallBrush = new SolidBrush(Color.FromArgb(255,229,22)); //233,181,128));
		Brush PrjReadyBrush = new SolidBrush(Color.PeachPuff);
		Brush OpsBookedBrush = new SolidBrush(Color.DarkGray);
		Brush OpsCompletedOKBrush = new SolidBrush(Color.OliveDrab);
		Brush OpsCompletedFailBrush = new SolidBrush(Color.FromArgb(183,56,42)); //Rust

		//Presentation Variables : Misc
		Boolean DrawBackground = true;
		Boolean DrawTimePrePlay = false;
		Boolean DrawTimePlay = false;
		string CurrentTimeStr = "";
		Point centrepoint = new Point(0,0);
		int con_wd = 10;
		int con_hd = 10;
		Boolean showconstruction = false;
		
		//Presentation Image
		private Image img_cross = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images\\calendar\\cross.png");
		private Image img_audit = null;
		private Image img_trade = null;
		private Image img_travel = null;
		private Image img_freeze = null;
		private Image img_dataprep = null; 
		private Image img_race = null;
		private Image img_press = null; 
		private Image img_test = null; 
		private Image img_qual = null; 
		private Image ug_memory_due = null;
		private Image ug_hardware_due = null; 
		private Image ug_storage_due = null; 
		private Image ug_app_due = null; 
		private Image install_due = null; 
		private Image ug_memory_done = null; 
		private Image ug_hardware_done = null; 
		private Image ug_storage_done = null; 
		private Image ug_app_done = null; 
		private Image install_done = null; 
		private Image ug_memory_error = null; 
		private Image ug_hardware_error = null; 
		private Image ug_storage_error = null; 
		private Image ug_app_error = null; 
		private Image install_error = null; 

		protected Hashtable CalendarEventNodes = new Hashtable();
		protected Hashtable OpLogoPositions = new Hashtable();
		protected Hashtable OpTextPositions = new Hashtable();
		private System.ComponentModel.Container components = null;

		#region Constructor and Dispose Methods

		public MS_PipelineDisplay(NodeTree nt, int Round, bool TrainingGame)
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			//Setup the paint optmisations
			this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			this.SetStyle(ControlStyles.UserPaint, true);
			this.SetStyle(ControlStyles.DoubleBuffer,true);

			//Connect up to the Network
			_NetworkModel = nt;
			CurrentRound = Round;

			//Connect Up the Calendar Node and extract information from calender events 
			CalendarNode = _NetworkModel.GetNamedNode("Calendar");
			calendarLength = CalendarNode.GetIntAttribute("days",0);
			foreach(Node n in CalendarNode)
			{
				string cday = n.GetAttribute("day");
				if (cday != null)
				{
					//We only show items which are blocking 
					string block = n.GetAttribute("block");
					if (block.ToLower() == "true")
					{	
						CalendarEventNodes.Add(cday, n);
						n.AttributesChanged += new Network.Node.AttributesChangedEventHandler(n_AttributesChanged);
					}
				}
			}
			
			CalendarNode.AttributesChanged +=new Network.Node.AttributesChangedEventHandler(OpsEventsNode_AttributesChanged);
			CalendarNode.ChildAdded +=new Network.Node.NodeChildAddedEventHandler(OpsEventsNode_ChildAdded);
			CalendarNode.ChildRemoved +=new Network.Node.NodeChildRemovedEventHandler(OpsEventsNode_ChildRemoved);
			CalendarNode.Deleting +=new Network.Node.NodeDeletingEventHandler(OpsEventsNode_Deleting);
			
			//Connect Up the Current Day and extract current value  
			MyCurrentDayNode = _NetworkModel.GetNamedNode("CurrentDay");
			CurrentDay = MyCurrentDayNode.GetIntAttribute("day",0);
			MyCurrentDayNode.AttributesChanged += new Network.Node.AttributesChangedEventHandler(MyCurrentDayNode_AttributesChanged);
			
			MyCurrentTimeNode = _NetworkModel.GetNamedNode("CurrentTime");
			MyCurrentTimeNode.AttributesChanged +=new Network.Node.AttributesChangedEventHandler(MyCurrentTimeNode_AttributesChanged);

			MyCurrentPrePlayTimeNode = _NetworkModel.GetNamedNode("preplay_status");
			MyCurrentPrePlayTimeNode.AttributesChanged +=new Network.Node.AttributesChangedEventHandler(MyCurrentPrePlayTimeNode_AttributesChanged);

			MyProjectsNode = _NetworkModel.GetNamedNode("Projects");
			MyProjectsNode.ChildAdded += new Network.Node.NodeChildAddedEventHandler(MyProjectsNode_ChildAdded);
			MyProjectsNode.ChildRemoved +=new Network.Node.NodeChildRemovedEventHandler(MyProjectsNode_ChildRemoved);
			
			foreach(Node n in MyProjectsNode)
			{
				int projectround = n.GetIntAttribute("createdinround",0);
				if (projectround == CurrentRound)
				{
					AddProjectNode(n);
				}
			}

			MyDevelopmentSpendNode = _NetworkModel.GetNamedNode("DevelopmentSpend");
			MyDevelopmentSpendNode.AttributesChanged +=new Network.Node.AttributesChangedEventHandler(MyDevelopmentSpendNode_AttributesChanged);
			
			roundbudgetleft = MyDevelopmentSpendNode.GetIntAttribute("roundbudgetleft",0);
			currentspendtotal = MyDevelopmentSpendNode.GetIntAttribute("currentspendtotal",0);
			actualcosttotal = MyDevelopmentSpendNode.GetIntAttribute("actualcosttotal",0);

			RebuildBrushes(); 
			
			BuildIconImages();
			Resize += new EventHandler(CT_Resize);
			AddMask();

			BuildOperationPositioning();
		}

		private void AddMask()
		{
			int h = this.Height;
			int w = this.Width;
			int hd = (w * 3) /20;
			int wd = (w * 3) /20;

			hd = hd * 2;
			wd = wd * 2;

			con_wd = wd;
			con_hd = hd;
			Point[] pp = new Point[8];

			//simple cut corners mask 
//			Point[] pp = new Point[8];
//			pp[0] = new Point(wd,0);		//Upper Left Corner
//			pp[1] = new Point(w-wd,0);	//Upper Right Corner
//			pp[2] = new Point(w,hd);		//Upper Right Corner
//			pp[3] = new Point(w,h-hd);	//Lower Right Corner
//			pp[4] = new Point(w-wd,h);	//Lower Right Corner
//			pp[5] = new Point(0+wd,h);	//Lower Left Corner
//			pp[6] = new Point(0,h-hd);	//Lower Left Corner
//			pp[7] = new Point(0,hd);		//Upper Left Corner

			//72 point (5 degree steps) circular mask 
			int OuterBorder = 2;
			int InnerBorder = 40;
			int p1_Radius;
			int p2_Radius;
			int	p3_Radius;
			int	p4_Radius;
			int p5_Radius;
			GenerateNewFiveRadi(OuterBorder, InnerBorder, 22, 22, 36, 20,
				out p1_Radius, out p2_Radius, out p3_Radius, out p4_Radius, out p5_Radius);
			ArrayList al = new ArrayList();
			Point edgepoint = new Point(0,0);
			int startA =0;
			for (int step = 0; step < (24*3); step++) //24
			{
				startA = (step+0)*5; //15
				edgepoint = this.GeneratePointAtRadius(true,startA,centrepoint.X, centrepoint.Y,p1_Radius+2);
				al.Add(edgepoint);
			}
			pp = new Point[al.Count];
			int newstep=0;
			foreach(Point pxy in al)
			{
				pp[newstep]=pxy;
				newstep++;
			}

			GraphicsPath mPath = new GraphicsPath(); 
			mPath.AddPolygon(pp);
			//crreate a region from the Path 
			Region region = new Region(mPath);
			//create a graphics object 
			Graphics graphics = this.CreateGraphics();
			//get the handle to the region 
			IntPtr ptr = region.GetHrgn(graphics);
			//Call the Win32 window region code 
			SetWindowRgn((IntPtr)this.Handle, ptr, true);
		}

		/// <summary>
		/// Cheap hardcoded poditional information 
		/// </summary>
		protected void BuildOperationPositioning()
		{
			OpLogoPositions.Add(1, new Point(470+14,74));
			OpLogoPositions.Add(2, new Point(530,124));
			OpLogoPositions.Add(3, new Point(571,188));
			OpLogoPositions.Add(4, new Point(583,260));
			OpLogoPositions.Add(5, new Point(587,332));
			OpLogoPositions.Add(6, new Point(568,404));
			OpLogoPositions.Add(7, new Point(535,468));
			OpLogoPositions.Add(8, new Point(480,520));
			OpLogoPositions.Add(9, new Point(413,556));
			OpLogoPositions.Add(10, new Point(348,581));
			OpLogoPositions.Add(11, new Point(267+5,581));
			OpLogoPositions.Add(12, new Point(206,556));
			OpLogoPositions.Add(13, new Point(123+20,520));
			OpLogoPositions.Add(14, new Point(74+5,468));
			OpLogoPositions.Add(15, new Point(34+10,404));
			OpLogoPositions.Add(16, new Point(15+10,332));
			OpLogoPositions.Add(17, new Point(28,260));
			OpLogoPositions.Add(18, new Point(42,188));
			OpLogoPositions.Add(19, new Point(82,124));
			OpLogoPositions.Add(20, new Point(139,74));

			OpTextPositions.Add(1, new Point(470-14,106));
			OpTextPositions.Add(2, new Point(509,156));
			OpTextPositions.Add(3, new Point(559-5,220));
			OpTextPositions.Add(4, new Point(572,292));
			OpTextPositions.Add(5, new Point(571,364));
			OpTextPositions.Add(6, new Point(549,436));
			OpTextPositions.Add(7, new Point(515,500));
			OpTextPositions.Add(8, new Point(466,552));
			OpTextPositions.Add(9, new Point(403,588));
			OpTextPositions.Add(10, new Point(329,613));
			OpTextPositions.Add(11, new Point(250,613));
			OpTextPositions.Add(12, new Point(177,588));
			OpTextPositions.Add(13, new Point(113,552));
			OpTextPositions.Add(14, new Point(65,500));
			OpTextPositions.Add(15, new Point(31,436));
			OpTextPositions.Add(16, new Point(13,364));
			OpTextPositions.Add(17, new Point(11,292));
			OpTextPositions.Add(18, new Point(30,220));
			OpTextPositions.Add(19, new Point(70,156));
			OpTextPositions.Add(20, new Point(126,106));
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

			ArrayList KillList = new ArrayList();
			foreach (Node n in ProjectNodes.Values)
			{
				KillList.Add(n);
			}
			foreach (Node n in KillList)
			{
				this.RemoveProjectNode(n);
			}

			foreach (Node n in CalendarEventNodes.Values)
			{
				n.AttributesChanged -= new Network.Node.AttributesChangedEventHandler(n_AttributesChanged);
			}

			if (MyCurrentTimeNode != null)
			{
				MyCurrentTimeNode.AttributesChanged -= new Network.Node.AttributesChangedEventHandler(MyCurrentTimeNode_AttributesChanged);
				MyCurrentTimeNode = null;
			}

			if (MyCurrentPrePlayTimeNode != null)
			{
				MyCurrentPrePlayTimeNode.AttributesChanged -= new Network.Node.AttributesChangedEventHandler(MyCurrentPrePlayTimeNode_AttributesChanged);
				MyCurrentPrePlayTimeNode = null;
			}

			if (MyCurrentDayNode != null)
			{
				MyCurrentDayNode.AttributesChanged -= new Network.Node.AttributesChangedEventHandler(MyCurrentDayNode_AttributesChanged);
				MyCurrentDayNode = null;
			}

			base.Dispose( disposing );
		}

		#endregion Constructor and Dispose Methods

		#region Component Designer generated code
		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			// CT
			this.Name = "CT";
			this.Resize += new System.EventHandler(this.CT_Resize);
		}
		#endregion

		#region Handling Calender Events Methods

		//Handle the change of the Calender Length (Not allowed in McKinley) 
		
		/// <summary>
		/// Handle the change of the Calender Length 
		/// (Not allowed in McKinley, it's fixed to 20 days) 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="attrs"></param>
		private void OpsEventsNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			calendarLength = CalendarNode.GetIntAttribute("days",0);
		}

		/// <summary>
		/// Handling the 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="attrs"></param>
		private void n_AttributesChanged(Node sender, ArrayList attrs)
		{
			Refresh();
		}

		private void OpsEventsNode_Deleting(Node sender)
		{
			Reset();
			Refresh();
		}

		private void OpsEventsNode_ChildAdded(Node sender, Node child)
		{
			if (child != null)
			{
				string cday = child.GetAttribute("day");
				if (cday != null)
				{
					//We only show items which are blocking 
					string block = child.GetAttribute("block");
					if (block.ToLower() == "true")
					{
						CalendarEventNodes.Add(cday, child);
						MyCurrentDayNode.AttributesChanged += new Network.Node.AttributesChangedEventHandler(MyCurrentDayNode_AttributesChanged);
						//do we need to attach to the deleting event
						Refresh();
					}
				}
			}
		}

		private void OpsEventsNode_ChildRemoved(Node sender, Node child)
		{
			if (child != null)
			{
				string cday = child.GetAttribute("day");
				if (cday != null)
				{
					CalendarEventNodes.Remove(cday);
					child.AttributesChanged -= new Network.Node.AttributesChangedEventHandler(MyCurrentDayNode_AttributesChanged);
					Refresh();
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public void Reset()
		{
			foreach(Node n in CalendarEventNodes.Values)
			{
				n.AttributesChanged -= new Network.Node.AttributesChangedEventHandler(MyCurrentDayNode_AttributesChanged);
			}
			CalendarEventNodes.Clear();
		}

		#endregion 

		#region Current Day Monitoring 

		private string BuildTimeString(int timevalue)
		{
			int time_mins = timevalue / 60;
			int time_secs = timevalue % 60;
			string displaystr = CONVERT.ToStr(time_mins)+":";
			if (time_secs<10)
			{
				displaystr += "0";
			}
			displaystr += CONVERT.ToStr(time_secs);
			if (time_mins<10)
			{
				displaystr = "0" + displaystr;
			}
			return displaystr;
		}


		private void MyCurrentTimeNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			int seconds = MyCurrentTimeNode.GetIntAttribute("seconds",0);
			this.CurrentTimeStr = BuildTimeString(seconds);
			DrawTimePrePlay = false;
			DrawTimePlay = true;
			Refresh();
		}

		private void MyCurrentPrePlayTimeNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			if (attrs != null && attrs.Count > 0)
			{
				foreach(AttributeValuePair avp in attrs)
				{
					if (avp.Attribute == "time_left")
					{
						int time_left = sender.GetIntAttribute("time_left",0);
						if (time_left>0)
						{
							this.CurrentTimeStr = BuildTimeString(time_left);
							DrawTimePrePlay = true;
							DrawTimePlay = false;
						}
						else
						{
							DrawTimePrePlay = false;
							DrawTimePlay = true;
						}
					}
				}
			}
		}

		private void MyCurrentDayNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			if (attrs != null)
			{
				if (attrs.Count > 0)
				{
					int newDay = sender.GetIntAttribute("day",0);
					setHighlightDay(newDay);
					Refresh();
				}
			}
		}

		#endregion Current Day Monitoring  

		#region Project Data Monitoring 

		private void MyProjectsNode_ChildAdded(Node sender, Node child)
		{
			AddProjectNode(child);
		}

		protected virtual void MyProjectsNode_ChildRemoved(Node sender, Node child)
		{
			RemoveProjectNode(child);
		}

		private void MyDevelopmentSpendNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			if (attrs != null && attrs.Count > 0)
			{
				foreach(AttributeValuePair avp in attrs)
				{
					//Extraction of the data attribute
					string attribute = avp.Attribute;
					string newValue = avp.Value;
					if ((attribute=="CurrentSpendTotal"))
					{
						int nv = CONVERT.ParseInt(newValue);
						nv = nv / 1000;
						currentspendtotal = nv;
					}
					if ((attribute=="ActualCostTotal"))
					{
						int nv = CONVERT.ParseInt(newValue);
						nv = nv / 1000;
						actualcosttotal = nv;
					}
					if ((attribute=="RoundBudgetLeft"))
					{
						int nv = CONVERT.ParseInt(newValue);
						nv = nv / 1000;
						roundbudgetleft = nv;
					}
				}
			}
		}

		private void PrjNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			Refresh();
		}

		protected void AddProjectNode(Node PrjNode)
		{
			string name = PrjNode.GetAttribute("name");
			if (ProjectNodesByName.ContainsKey(name)== false)
			{
				if (ProjectNodes.Count < 2)
				{
					if (ProjectNodes.ContainsKey(1)==false)
					{
						ProjectNodesByName.Add(name,1);
						ProjectNodes.Add(1, PrjNode);
						PrjNode.AttributesChanged +=new Network.Node.AttributesChangedEventHandler(PrjNode_AttributesChanged);
						Refresh();
					}
					else
					{
						if (ProjectNodes.ContainsKey(2)==false)
						{
							ProjectNodesByName.Add(name,2);
							ProjectNodes.Add(2, PrjNode);
							PrjNode.AttributesChanged +=new Network.Node.AttributesChangedEventHandler(PrjNode_AttributesChanged);
							Refresh();
						}
					}
				}
			}


		}

		protected void RemoveProjectNode(Node PrjNode)
		{
			ArrayList KillList = new ArrayList();
			string name = PrjNode.GetAttribute("name");

			foreach (int sn in ProjectNodes.Keys)
			{
				Node pn = (Node) ProjectNodes[sn];
				if (PrjNode == pn)
				{
					KillList.Add(sn);
					pn.AttributesChanged -=new Network.Node.AttributesChangedEventHandler(PrjNode_AttributesChanged);
				}
			}
			foreach (int x in KillList)
			{
				ProjectNodes.Remove(x);
			}

			if (ProjectNodesByName.ContainsKey(name))
			{
				ProjectNodesByName.Remove(name);
			}
			Refresh();
		}

		protected void getProjectData(int ProjectSelect, out bool PrjStarted, out string PrjName, 
			out string PrjBudget, out string PrjSpend)
		{
			PrjStarted = false;
			PrjName = "";
			PrjBudget = "";
			PrjSpend = "";

			if (ProjectNodes.ContainsKey(ProjectSelect))
			{
				Node n = (Node)ProjectNodes[ProjectSelect];
				string nm = n.GetAttribute("name");
				string productid  = n.GetAttribute("productid");
				string platformid  = n.GetAttribute("platformid");

				PrjName = productid + "-" + platformid;

				int firstday = n.GetIntAttribute("firstday",-1);
				PrjStarted = false;
				if (firstday != -1)
				{
					PrjStarted = true;
				}
				int PrjCurrentSpend = n.GetIntAttribute("currentspend",-1);
				if (PrjCurrentSpend != -1)
				{
					PrjCurrentSpend = PrjCurrentSpend / 1000;
					PrjSpend = CONVERT.ToStr(PrjCurrentSpend);
				}
				else
				{
					PrjSpend = "0";
				}

				int PrjActualCost = n.GetIntAttribute("actual_cost",-1);
				if (PrjActualCost != -1)
				{
					PrjActualCost = PrjActualCost / 1000;
					PrjBudget = CONVERT.ToStr(PrjActualCost);
				}
				else
				{
					PrjBudget = "0";
				}
			}
		}

		protected void getProjectDayData(int ProjectSelect, int DaySelect, int currentDay,
			out Brush FillBrush, out bool ShowHandover, out string HandoverValue)
		{
			Brush tmpBrush = this.EmptyBrush;
			ShowHandover = false;
			HandoverValue = "";

			if (ProjectNodes.ContainsKey(ProjectSelect))
			{
				Node n = (Node)ProjectNodes[ProjectSelect];
				string nm = n.GetAttribute("name");
				int firstday = n.GetIntAttribute("firstday",-1);

				int designdays = n.GetIntAttribute("designdays",-1);
				int builddays = n.GetIntAttribute("builddays",-1);
				int testdays = n.GetIntAttribute("builddays",-1);
				HandoverValue = n.GetAttribute("handovervalue");
				HandoverValue = HandoverValue + "%";

				int designdays_start = firstday+1;
				int designdays_end = designdays_start + designdays;
				int builddays_start = designdays_end;
				int builddays_end = builddays_start + builddays;
				int testdays_start = builddays_end;
				int testdays_end = testdays_start + testdays;
				int handover_start = testdays_end;
				int handover_end = testdays_end;

				tmpBrush = this.EmptyBrush;
				if (DaySelect == firstday)	
				{
					tmpBrush = PrjDefineBrush;
				}
				else
				{
					if ((DaySelect >= designdays_start)&(DaySelect < designdays_end))
					{
						tmpBrush = PrjDesignBrush;
					}
					else
					{
						if ((DaySelect >= builddays_start)&(DaySelect < builddays_end))
						{
							tmpBrush = PrjBuildBrush;
						}
						else
						{
							if ((DaySelect >= testdays_start)&(DaySelect < testdays_end))
							{
								tmpBrush = PrjTestBrush;
							}
							else
							{
								if ((DaySelect >= handover_start)&(DaySelect < handover_end))
								{
									tmpBrush = PrjHandoverBrush;
									if (currentDay >= handover_start)
									{
										ShowHandover = true;
									}
								}
								else
								{
									if ((DaySelect >= handover_end))
									{
										tmpBrush = PrjReadyBrush;
										if (currentDay >= handover_start)
										{
											ShowHandover = true;
										}
									}
								}
							}
						}
					}
				}
			}
			FillBrush = tmpBrush; 
		}

		#endregion Project Data Monitoring 

		#region Game Data Helpers 

		public int getHighlightDay()
		{
			return CurrentDay;
		}

		public void setHighlightDay(int cday)
		{
			if (cday<1)
			{
				CurrentDay = 1;
			}
			else
			{
				CurrentDay = cday;
			}
			if (CurrentDay > MaxDay)
			{
				CurrentDay = MaxDay;
			}
		}



//		protected void getOperationsDayData(int DaySelect, out Brush FillBrush, out Image EventIcon)
//		{
//			FillBrush = null;
//			EventIcon = null;
//			if (DaySelect == 5)
//			{
//				FillBrush = OpsBookedBrush;
//			}
//		}

		#endregion Game Data Helpers 

		#region Mathematical Routines 

		/// <summary>
		/// This routine generate the radiius for the different bands 
		/// </summary>
		/// <param name="outerBorder"></param>
		/// <param name="innerBorder"></param>
		/// <param name="p1Share">Percentage of usable width given to band 1</param>
		/// <param name="p2Share">Percentage of usable width given to band 2</param>
		/// <param name="p3Share">Percentage of usable width given to band 3</param>
		/// <param name="p4Share">Percentage of usable width given to band 4</param>
		/// <param name="OuterRadius"></param>
		/// <param name="p1_Radius"></param>
		/// <param name="p2_Radius"></param>
		/// <param name="p3_Radius"></param>
		/// <param name="p4_Radius"></param>
		public void GenerateNewFiveRadi(int outerBorder, int innerBorder, 
			int p1Share, int p2Share, int p3Share, int p4Share,
			out int OuterRadius, out int p1_Radius, out int p2_Radius, out int p3_Radius, out int p4_Radius )
		{
			int FullWidth = this.Width / 2;
			int UsableWidth = FullWidth - (outerBorder + innerBorder);
			int p1_width =  (p1Share * UsableWidth) / 100;
			int p2_width =  (p2Share * UsableWidth) / 100;
			int p3_width =  (p3Share * UsableWidth) / 100;
			int p4_width =  (p4Share * UsableWidth) / 100;

			OuterRadius = FullWidth - outerBorder;
			p1_Radius = (FullWidth - outerBorder) - p1_width;
			p2_Radius = (p1_Radius) - p2_width;
			p3_Radius = (p2_Radius) - p3_width;
			p4_Radius = (p3_Radius) - p4_width;
		}

		/// <summary>
		/// Used to swap the angle around for different quadrants
		/// </summary>
		/// <param name="x"></param>
		/// <returns></returns>
		private int reflect(int x)
		{
			int xx = 90-x;
			if (xx< 0)
			{
				xx=360-(x-90);
			}
			return xx;
		}

		/// <summary>
		/// Generate a point and angle and radius from a origin
		/// </summary>
		/// <param name="useAngleshift"></param>
		/// <param name="req_angle"></param>
		/// <param name="originX"></param>
		/// <param name="originY"></param>
		/// <param name="radius"></param>
		/// <returns></returns>
		private Point GeneratePointAtRadius(Boolean useAngleshift, int req_angle, int originX, int originY, int radius)
		{
			double angle = 0.0f;
			double RadInner = (double) radius;
			double orgX = (double) originX;
			double orgY = (double) originY; 
			int x1=0;
			int y1=0;

			if (useAngleshift)
			{
				angle = reflect(req_angle);
			}
			else
			{
				angle = (req_angle);
			}
			x1 = (int)((orgX)+(RadInner*Math.Cos(angle*Math.PI/180)));
			y1 = (int)((orgY)-(RadInner*Math.Sin(angle*Math.PI/180)));
			return new Point(x1,y1);
		}

		/// <summary>
		/// This generates an array of points representing a patch.
		/// It is defined by a start angle, end angle, inner radius and outer radius
		/// The curved sides are a sequence of points generated each degree (from the start angle to the end angle)   
		/// </summary>
		/// <param name="UseAngleshift"></param>
		/// <param name="start_angle"></param>
		/// <param name="end_angle"></param>
		/// <param name="originX"></param>
		/// <param name="originY"></param>
		/// <param name="radiusInner"></param>
		/// <param name="radiusOuter"></param>
		/// <returns></returns>
		private Point[] GeneratePatch(Boolean UseAngleshift, int start_angle, int end_angle, int originX, int originY,
			int radiusInner, int radiusOuter) 
		{

			//System.Diagnostics.Debug.WriteLine(" Generate Patch  "+UseAngleshift.ToString()+" angle:"+start_angle.ToString()+ " -> "+end_angle.ToString()); 
			int pointCount = ((end_angle - start_angle) * 2) + 2;
			//System.Diagnostics.Debug.WriteLine("pointCount:"+pointCount);
			Point[] points = new Point[pointCount];

			int index=0;
			if (points.Length >0)
			{
				for (int step= start_angle; step <= end_angle; step++)
				{
					points[index]= GeneratePointAtRadius(UseAngleshift, step, originX, originY, radiusInner);
					//System.Diagnostics.Debug.WriteLine(" index  "+index.ToString()+" angle:"+step.ToString()+ " -> "+points[index].ToString()); 
					index++;
				}
				for (int step= end_angle; step >= start_angle; step--)
				{
					points[index]= GeneratePointAtRadius(UseAngleshift, step, originX, originY,  radiusOuter);
					//System.Diagnostics.Debug.WriteLine(" index  "+index.ToString()+" angle:"+step.ToString()+ " -> "+points[index].ToString()); 
					index++;
				}
				//points[index]= points[0];
			}
			return points;
		}

		#endregion Mathematical Routines 

		#region Utils

		public void DisposeBrush(Brush br)
		{
			if (br != null)
			{
				br.Dispose();
			}
		}

		public void RebuildBrushes()
		{
			Color tmpColor = Color.White;
			DisposeBrush(PrePlayTimeBrush);
			tmpColor = SkinningDefs.TheInstance.GetColorData("transition_pipeline_preplay_time_forecolor");
			PrePlayTimeBrush = new SolidBrush(tmpColor);

			DisposeBrush(PlayTimeBrush);
			tmpColor = SkinningDefs.TheInstance.GetColorData("transition_pipeline_play_time_forecolor");
			PlayTimeBrush = new SolidBrush(tmpColor);

			DisposeBrush(DayBrush);
			tmpColor = SkinningDefs.TheInstance.GetColorData("transition_pipeline_day_time_forecolor");
			DayBrush = new SolidBrush(tmpColor);

			DisposeBrush(EmptyBrush);
			tmpColor = SkinningDefs.TheInstance.GetColorData("transition_pipeline_empty_time_forecolor");
			EmptyBrush = new SolidBrush(tmpColor);

			DisposeBrush(PrjDefineBrush);
			tmpColor = SkinningDefs.TheInstance.GetColorData("transition_pipeline_project_define_forecolor");
			PrjDefineBrush = new SolidBrush(tmpColor);
	
			DisposeBrush(PrjDesignBrush);
			tmpColor = SkinningDefs.TheInstance.GetColorData("transition_pipeline_project_design_forecolor");
			PrjDesignBrush = new SolidBrush(tmpColor);
	
			DisposeBrush(PrjBuildBrush);
			tmpColor = SkinningDefs.TheInstance.GetColorData("transition_pipeline_project_build_forecolor");
			PrjBuildBrush = new SolidBrush(tmpColor);
	
			DisposeBrush(PrjTestBrush);
			tmpColor = SkinningDefs.TheInstance.GetColorData("transition_pipeline_project_test_forecolor");
			PrjTestBrush = new SolidBrush(tmpColor);

			DisposeBrush(PrjHandoverBrush);
			tmpColor = SkinningDefs.TheInstance.GetColorData("transition_pipeline_project_handover_forecolor");
			PrjHandoverBrush = new SolidBrush(tmpColor);

			DisposeBrush(PrjInstallBrush);
			tmpColor = SkinningDefs.TheInstance.GetColorData("transition_pipeline_project_install_forecolor");
			PrjInstallBrush = new SolidBrush(tmpColor);

			DisposeBrush(PrjReadyBrush);
			tmpColor = SkinningDefs.TheInstance.GetColorData("transition_pipeline_project_ready_forecolor");
			PrjReadyBrush = new SolidBrush(tmpColor);

			DisposeBrush(OpsBookedBrush);
			tmpColor = SkinningDefs.TheInstance.GetColorData("transition_pipeline_ops_booked_forecolor");
			OpsBookedBrush = new SolidBrush(tmpColor);

			DisposeBrush(OpsCompletedOKBrush);
			tmpColor = SkinningDefs.TheInstance.GetColorData("transition_pipeline_ops_completedok_forecolor");
			OpsCompletedOKBrush = new SolidBrush(tmpColor);

			DisposeBrush(OpsCompletedFailBrush);
			tmpColor = SkinningDefs.TheInstance.GetColorData("transition_pipeline_ops_completedfail_forecolor");
			OpsCompletedFailBrush = new SolidBrush(tmpColor);

//			Brush PrePlayTimeBrush = new SolidBrush(Color.Plum);
//			Brush PlayTimeBrush = new SolidBrush(Color.DimGray);
//			Brush DayBrush = new SolidBrush(Color.FromArgb(165,196,197));
//			Brush EmptyBrush = new SolidBrush(Color.Gainsboro);
//			Brush PrjDefineBrush = new SolidBrush(Color.FromArgb(104,55,3));
//			Brush PrjDesignBrush = new SolidBrush(Color.FromArgb(158,83,4));
//			Brush PrjBuildBrush = new SolidBrush(Color.FromArgb(208,112,6));
//			Brush PrjTestBrush = new SolidBrush(Color.FromArgb(221,146,70));
//			Brush PrjHandoverBrush = new SolidBrush(Color.FromArgb(245,219,191)); //233,181,128));
//			Brush PrjInstallBrush = new SolidBrush(Color.FromArgb(255,229,22)); //233,181,128));
//			Brush PrjReadyBrush = new SolidBrush(Color.PeachPuff);
//			Brush OpsBookedBrush = new SolidBrush(Color.DarkGray);
//			Brush OpsCompletedOKBrush = new SolidBrush(Color.OliveDrab);
//			Brush OpsCompletedFailBrush = new SolidBrush(Color.FromArgb(183,56,42)); //Rust	
		
		}

		/// <summary>
		/// Helper method to load all the ops icons 
		/// 
		/// </summary>
		public void BuildIconImages()
		{
			//Common icons (always used)
			ug_memory_due = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images\\calendar\\memory_due.png");
			ug_hardware_due = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images\\calendar\\hardware_due.png");
			ug_storage_due = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images\\calendar\\storage_due.png");
			ug_app_due = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images\\calendar\\app_due.png");
			install_due = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images\\calendar\\install_due.png");

			ug_memory_done = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images\\calendar\\memory_done.png");
			ug_hardware_done = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images\\calendar\\hardware_done.png");
			ug_storage_done = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images\\calendar\\storage_done.png");
			ug_app_done = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images\\calendar\\app_done.png");
			install_done = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images\\calendar\\install_done.png");

			ug_memory_error = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images\\calendar\\memory_error.png");
			ug_hardware_error = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images\\calendar\\hardware_error.png");
			ug_storage_error = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images\\calendar\\storage_error.png");
			ug_app_error = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images\\calendar\\app_error.png");
			install_error = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images\\calendar\\install_error.png");

			img_press = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images\\calendar\\press.png");

			//Skin
			string skinname = CoreUtils.SkinningDefs.TheInstance.GetData("skinname");

			// : Fix for 3662 (Reckitt doesn't show correct icons for days).
			// The icon-loading code switched on the skin name, and had no case for RB.
			// Instead, we just try to load everything, relying on coping gracefully with
			// ones that aren't loaded.

			// These are used by just about everyone.
			img_trade = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images\\calendar\\trade.png");
			img_freeze = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images\\calendar\\freeze.png");
			img_dataprep= Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images\\calendar\\data_prep.png");

			// These are used by HP.
			img_race = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images\\calendar\\race.png");
			img_test = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images\\calendar\\test.png");
			img_qual = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images\\calendar\\qual.png");					
			img_travel = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images\\calendar\\travel.png");
			img_audit = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images\\calendar\\audit.png");

		}

		private void CT_Resize(object sender, System.EventArgs e)
		{
			centrepoint = new Point(this.Width / 2, this.Height / 2);
			this.AddMask();
		}

		private void DetermineWhichColorBrush(string status, out Brush tmpFillBrush, out Brush tmpTextBrush)
		{
			tmpFillBrush = EmptyBrush;
			tmpTextBrush = Brushes.Gray;

			if(status == "completed_fail")
			{
				tmpFillBrush = OpsCompletedFailBrush;
				tmpTextBrush = Brushes.White;
			}
			if(status == "completed_ok")
			{
				tmpFillBrush = OpsCompletedOKBrush;
				tmpTextBrush = Brushes.White;
			}
			if(status == "active")
			{
				tmpFillBrush = EmptyBrush;
				tmpTextBrush = Brushes.Gray;
			}
		}

		private Image CheckNotNull(Image SuppliedImage)
		{
			if (SuppliedImage != null)
			{
				return SuppliedImage;
			}
			return img_cross;
		}

		private Image DetermineWhichImage(string type, string status, string option, string name)
		{
			Image img = null;
			switch (type)	
			{
				case "Install":

				switch (status)
				{
					case "active":			img = CheckNotNull(install_due);	break;
					case "completed_ok":	img = CheckNotNull(install_done);	break;
					case "completed_fail":	img = CheckNotNull(install_error);break;
				}break;

				case "server_upgrade":
					if (option == "hardware")
					{
						switch (status)
						{
							case "active":			img = CheckNotNull(ug_hardware_due);	break;
							case "completed_ok":	img = CheckNotNull(ug_hardware_done);	break;
							case "completed_fail":	img = CheckNotNull(ug_hardware_error);break;
						}
					}
					if (option == "memory")	//server memory upgrade
					{
						switch (status)
						{
							case "active":			img = CheckNotNull(ug_memory_due);  break;
							case "completed_ok":	img = CheckNotNull(ug_memory_done); break;
							case "completed_fail":	img = CheckNotNull(ug_memory_error); break;
						}
					}
					if (option == "storage")	//server memory upgrade
					{
						switch (status)
						{
							case "active":			img = CheckNotNull(ug_storage_due);	break;
							case "completed_ok":	img = CheckNotNull(ug_storage_done);	break;
							case "completed_fail":	img = CheckNotNull(ug_storage_error);	break;
						}
					}
					break;

				case "app_upgrade":
				switch (status)
				{
					case "active":			img = CheckNotNull(ug_app_due);	break;
					case "completed_ok":	img = CheckNotNull(ug_app_done);	break;
					case "completed_fail":	img = CheckNotNull(ug_app_error);	break;
				}break;

				case "external":
				switch (name)
				{
					case "Data Prep":		img = CheckNotNull(img_dataprep); break;
					case "Freeze":			img = CheckNotNull(img_freeze); break;
					case "Travel":
					case "Trading":			img = CheckNotNull(img_trade); break;
					case "Shipping":		img = CheckNotNull(img_trade); break;
						//
					case "Race":		img = CheckNotNull(img_race);	break;
					case "Press":		img = CheckNotNull(img_press);break;
					case "Testing":		img = CheckNotNull(img_test);break;
					case "Qualify":	
					case "Qualifying":	img = CheckNotNull(img_qual);break;
					case "Audit":	img = CheckNotNull(img_audit);break;
				}
					break;
			}
			return img; 
		}

		#endregion Utils

		protected override void OnPaint(PaintEventArgs e)
		{
			int OuterBorder = 2;
			int InnerBorder = 40;
			int p1_Radius=0; 
			int p2_Radius=0; 
			int p3_Radius=0; 
			int p4_Radius=0; 
			int p5_Radius=0; 

			string name;
			string type; 
			string product; 
			string status; 
			string target; 
			string option = string.Empty;

			bool Prj1_Started = false;
			string Prj1_Name = "";
			string Prj1_Budget = "";
			string Prj1_Spend = "";
			
			bool Prj2_Started = false;
			string Prj2_Name = "";
			string Prj2_Budget = "";
			string Prj2_Spend = "";

			string projectSpendStr = "";
			Color opscolor = Color.White;
			int startA = 0;
			int endA = 0; 
			int midA = 0; 
			Point endRayPoint;
			Point startRayPoint;
			SizeF textsize = new SizeF(0,0);
			Brush tmpFillBrush = null;
			Brush tmpTextBrush = null;
			Image OpsIcon = null;
			
			//int innertire  = (this.Width / 3)+15;
			//int outertire  = (this.Width / 2)-15;
			Point[] pts = new Point[21];

			bool ShowHO = false;
			string HOvalue = "";

			//Setup the Quality and Rendering Options 
			e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
			e.Graphics.TextRenderingHint = TextRenderingHint.AntiAlias;

			StringFormat format2 = new StringFormat();
			format2.Alignment = StringAlignment.Center;
			format2.LineAlignment = StringAlignment.Center;

			//Recalculate the Radius extents 
			GenerateNewFiveRadi(OuterBorder, InnerBorder, 22+10, 20, 20, 28,
				out p1_Radius, out p2_Radius, out p3_Radius, out p4_Radius, out p5_Radius);
			//
			if (DrawBackground)
			{
				e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(241,241,241)), 1,1, this.Width-2, this.Height-2);
			}
			//=======================================================================
			//==Draw the various Titles in the top central space=====================
			//=======================================================================
			//Draw the Operations Title
			textsize = e.Graphics.MeasureString("OPERATIONS",BoldFont11);
			e.Graphics.DrawString("OPERATIONS",BoldFont11,Brushes.DimGray, centrepoint.X-(textsize.Width /2), centrepoint.Y- (p1_Radius-18));
			//Draw the Project 1
			getProjectData(1, out Prj1_Started, out Prj1_Name, out Prj1_Budget, out Prj1_Spend);
			if (Prj1_Started)
			{
				textsize = e.Graphics.MeasureString("Project 1 "+Prj1_Name,BoldFont11);
				e.Graphics.DrawString("Project 1 "+Prj1_Name,BoldFont11,Brushes.DimGray, centrepoint.X-(textsize.Width /2), centrepoint.Y- (p2_Radius-15));
				projectSpendStr = "Budget "+Prj1_Budget+"K" + "  " + "Spend "+Prj1_Spend+"K";
				textsize = e.Graphics.MeasureString(projectSpendStr,NormalFont8);
				e.Graphics.DrawString(projectSpendStr,NormalFont8,Brushes.DimGray,centrepoint.X-(textsize.Width /2), centrepoint.Y- (p2_Radius-33));
			}
			else
			{
				textsize = e.Graphics.MeasureString("Project 1",BoldFont11);
				e.Graphics.DrawString("Project 1",BoldFont11,Brushes.DimGray, centrepoint.X-(textsize.Width /2), centrepoint.Y- (p2_Radius-15));
			}

			//Draw the Project 2
			getProjectData(2, out Prj2_Started, out Prj2_Name, out Prj2_Budget, out Prj2_Spend);
			if (Prj2_Started)
			{
				textsize = e.Graphics.MeasureString("Project 2 "+Prj2_Name,BoldFont11);
				e.Graphics.DrawString("Project 2 "+Prj2_Name,BoldFont11,Brushes.DimGray, centrepoint.X-(textsize.Width /2), centrepoint.Y- (p3_Radius-15));
				projectSpendStr = "Budget "+Prj2_Budget+"K" + "  " + "Spend "+Prj2_Spend+"K";
				textsize = e.Graphics.MeasureString(projectSpendStr,NormalFont8);
				e.Graphics.DrawString(projectSpendStr,NormalFont8,Brushes.DimGray,centrepoint.X-(textsize.Width /2), centrepoint.Y- (p3_Radius-33));
			}
			else
			{
				textsize = e.Graphics.MeasureString("Project 2",BoldFont11);
				e.Graphics.DrawString("Project 2",BoldFont11,Brushes.DimGray, centrepoint.X-(textsize.Width /2), centrepoint.Y- (p3_Radius-15));
			}
			//Draw the Day Title and current day 
			textsize = e.Graphics.MeasureString("DAY",BoldFont11);
			e.Graphics.DrawString("DAY",BoldFont11,Brushes.DimGray,centrepoint.X-(textsize.Width /2), centrepoint.Y- (p4_Radius-10));
			if (CurrentDay <10)
			{
				e.Graphics.DrawString(this.CurrentDay.ToString(), BoldFont11,Brushes.DimGray,centrepoint.X-(textsize.Width /2)+13, centrepoint.Y- (p4_Radius-10)+15);
			}
			else
			{
				e.Graphics.DrawString(this.CurrentDay.ToString(), BoldFont11,Brushes.DimGray,centrepoint.X-(textsize.Width /2)+6, centrepoint.Y- (p4_Radius-10)+15);
			}
			//=======================================================================
			//==Iterate over the days and draw the segments==========================
			//=======================================================================
			for (int step = 0; step < 20; step++)
			{
				startA = 30+(step+0)*15;			//Starting angle of the segment
				endA = 30+((step+1)*15);			//Ending angle of the segment
				midA = (startA + endA) / 2;		//Mid angle of the segment

				//The Day Segment
				pts = GeneratePatch(true, startA, endA, centrepoint.X, centrepoint.Y, p5_Radius, p4_Radius);
				e.Graphics.FillPolygon(DayBrush, pts, System.Drawing.Drawing2D.FillMode.Alternate);

				//The Operations Segments
				OpsIcon = null;
				pts = GeneratePatch(true, startA, endA, centrepoint.X, centrepoint.Y, p2_Radius, p1_Radius);

				if (CalendarEventNodes.ContainsKey( CONVERT.ToStr(step+1)))
				{
					Node n1 = (Node)CalendarEventNodes[(step+1).ToString()];
					if (n1 != null)
					{
						name = n1.GetAttribute("showName");
						type = n1.GetAttribute("type");
						product = n1.GetAttribute("productid");
						status = n1.GetAttribute("status");
						target = n1.GetAttribute("target");
						
						if (type=="server_upgrade")
						{
							option = n1.GetAttribute("upgrade_option");
						}
						name = n1.GetAttribute("showName");
						
						OpsIcon = DetermineWhichImage(type, status, option, name);
						DetermineWhichColorBrush(status, out tmpFillBrush, out tmpTextBrush);
						name = n1.GetAttribute("showName");

						e.Graphics.FillPolygon(tmpFillBrush, pts, System.Drawing.Drawing2D.FillMode.Alternate);
						Point midpoint11 = GeneratePointAtRadius(true, midA, centrepoint.X, centrepoint.Y, p1_Radius-20);
						//e.Graphics.DrawImage(OpsIcon,midpoint11.X-9,midpoint11.Y-9,24,24);

						RectangleF rect = new RectangleF (midpoint11.X,midpoint11.Y,50,20); //Rectangle to wrap text
						StringFormat format3 = new StringFormat ();

						if (OpTextPositions.ContainsKey(step+1))
						{
							Point textStart = (Point)OpTextPositions[step+1];
							rect = new RectangleF (textStart.X,textStart.Y,70,16); //Rectangle to wrap text
						}

						if(type == "external")
						{
							textsize = e.Graphics.MeasureString(name,NormalFont8);
							rect.X = rect.X + (35-(textsize.Width / 2));
							e.Graphics.DrawString(name, NormalFont8, tmpTextBrush, rect, format3);
						}
						else if(type == "Install")
						{
							string installlocation = n1.GetAttribute("location");
							string installProjectDisplayStr = string.Empty;

							installProjectDisplayStr = product;
							if (installlocation.Length > 0)
							{
								installProjectDisplayStr += " " + installlocation.ToUpper();
							}
							textsize = e.Graphics.MeasureString(installProjectDisplayStr,NormalFont8);
							rect.X = rect.X + (35-(textsize.Width / 2));
							e.Graphics.DrawString(installProjectDisplayStr, NormalFont8, tmpTextBrush, rect, format3);
						}
						else if(type == "server_upgrade" || type == "app_upgrade")
						{
							textsize = e.Graphics.MeasureString(target,NormalFont8);
							rect.X = rect.X + (35-(textsize.Width / 2));
							e.Graphics.DrawString(target, NormalFont8, tmpTextBrush, rect, format3);
						}
					}
				}
				else
				{
					//No Calendar Eevnt for this Day 
					e.Graphics.FillPolygon(EmptyBrush, pts, System.Drawing.Drawing2D.FillMode.Alternate);
				}

//				getOperationsDayData((step+1), out FillBrush, out OpsIcon);
//				if (FillBrush != null)
//				{
//					e.Graphics.FillPolygon(FillBrush, pts, System.Drawing.Drawing2D.FillMode.Alternate);
//				}
//				else
//				{
//					e.Graphics.FillPolygon(EmptyBrush, pts, System.Drawing.Drawing2D.FillMode.Alternate);
//				}
				if (OpsIcon != null)
				{
					//Point midpoint11 = GeneratePointAtRadius(true, midA, centrepoint.X, centrepoint.Y, p1_Radius-22);
					if (OpLogoPositions.ContainsKey(step+1))
					{
						Point midpoint11 = (Point)OpLogoPositions[step+1];
						e.Graphics.DrawImage(OpsIcon,midpoint11.X,midpoint11.Y,30,30);
					}
				}

				//The Project 2 Segments
				pts = GeneratePatch(true, startA, endA, centrepoint.X, centrepoint.Y, p4_Radius, p3_Radius);
				getProjectDayData(2, (step+1), CurrentDay, out tmpFillBrush, out ShowHO, out HOvalue);
				if (tmpFillBrush != null)
				{
					e.Graphics.FillPolygon(tmpFillBrush, pts, System.Drawing.Drawing2D.FillMode.Alternate);
				}
				else
				{
					e.Graphics.FillPolygon(EmptyBrush, pts, System.Drawing.Drawing2D.FillMode.Alternate);
				}
				if (ShowHO)
				{
					Point midpoint1 = GeneratePointAtRadius(true, midA, centrepoint.X, centrepoint.Y, (p4_Radius+p3_Radius)/2);
					textsize = e.Graphics.MeasureString(HOvalue,this.Font);
					midpoint1.X = midpoint1.X + (14-(((int)textsize.Width) / 2));
					e.Graphics.DrawString(HOvalue,this.Font,Brushes.Black,midpoint1.X, midpoint1.Y,format2);
					//e.Graphics.DrawRectangle(Pens.Red,midpoint1.X,midpoint1.Y,3,3);
				}

				//The Project 1 Segments
				pts = GeneratePatch(true, startA, endA, centrepoint.X, centrepoint.Y, p3_Radius, p2_Radius);
				getProjectDayData(1, (step+1), CurrentDay, out tmpFillBrush, out ShowHO, out HOvalue);
				if (tmpFillBrush != null)
				{
					e.Graphics.FillPolygon(tmpFillBrush, pts, System.Drawing.Drawing2D.FillMode.Alternate);
				}
				else
				{
					e.Graphics.FillPolygon(EmptyBrush, pts, System.Drawing.Drawing2D.FillMode.Alternate);
				}
				if (ShowHO)
				{
					Point midpoint1 = GeneratePointAtRadius(true, midA, centrepoint.X, centrepoint.Y, (p3_Radius+p2_Radius)/2);
					textsize = e.Graphics.MeasureString(HOvalue,this.Font);
					midpoint1.X = midpoint1.X + (14-(((int)textsize.Width) / 2));
					e.Graphics.DrawString(HOvalue,this.Font,Brushes.Black,midpoint1.X, midpoint1.Y,format2);
					//e.Graphics.DrawRectangle(Pens.Red,midpoint1.X,midpoint1.Y,3,3);
				}

				//e.Graphics.DrawPolygon(Pens.Goldenrod,pts);
				//endRayPoint = this.GeneratePointAtRadius(true,endA,centrepoint.X, centrepoint.Y,p1_Radius);
				//e.Graphics.DrawLine(White2Pen, centrepoint, endRayPoint);
				textsize = e.Graphics.MeasureString("Day",BoldFont11);
				//Draw the highlighted Day with a colour
				if (step==(CurrentDay-1))
				{
					pts = GeneratePatch(true, startA, endA, centrepoint.X, centrepoint.Y, p5_Radius, p1_Radius-1);
					e.Graphics.FillPolygon(new SolidBrush(Color.FromArgb(80,32,200,255)), pts, System.Drawing.Drawing2D.FillMode.Alternate);
					e.Graphics.DrawPolygon(new Pen(Color.FromArgb(180,32,32,255)), pts);
				}
				//drawing the day legend for each day 
				Point midpoint = GeneratePointAtRadius(true, midA, centrepoint.X, centrepoint.Y, p4_Radius-13);
				string daystr = (step+1).ToString();
				textsize = e.Graphics.MeasureString(daystr,this.Font);
				e.Graphics.DrawString(daystr,this.Font,Brushes.Black,midpoint.X, midpoint.Y,format2);
				//Draw the Central Circle (was two color)
				e.Graphics.FillEllipse(Brushes.Gainsboro, centrepoint.X - ((p5_Radius-1)), centrepoint.Y  - ((p5_Radius-1)), (p5_Radius*2-2), (p5_Radius*2-2));
				e.Graphics.FillEllipse(Brushes.Gainsboro, centrepoint.X -(p5_Radius/2), centrepoint.Y-(p5_Radius/2), (p5_Radius), (p5_Radius));
			}

			//Draw the Central Current Time 
			textsize = e.Graphics.MeasureString(CurrentTimeStr, BoldFont14);
			if (this.DrawTimePrePlay)
			{
				e.Graphics.DrawString(CurrentTimeStr, BoldFont14, PrePlayTimeBrush, centrepoint.X, centrepoint.Y, format2);
			}
			if (this.DrawTimePlay)
			{
				e.Graphics.DrawString(CurrentTimeStr, BoldFont14, PlayTimeBrush, centrepoint.X , centrepoint.Y, format2);
			}

			//=======================================================================
			//==Draw the White Radial Lines and the white circles ===================
			//====These hide the roughness of the calculated circles================= 
			//=======================================================================
			for (int step = 0; step < 21; step++)
			{
				startA = 30+(step+0)*15;
				startRayPoint = this.GeneratePointAtRadius(true,startA,centrepoint.X, centrepoint.Y,p5_Radius);
				endRayPoint = this.GeneratePointAtRadius(true,startA,centrepoint.X, centrepoint.Y,p1_Radius);
				e.Graphics.DrawLine(White2Pen, startRayPoint, endRayPoint);
			}

			e.Graphics.DrawEllipse(White2Pen,centrepoint.X- (p1_Radius), centrepoint.Y- (p1_Radius), p1_Radius*2, p1_Radius*2);
			e.Graphics.DrawEllipse(White2Pen,centrepoint.X- (p2_Radius), centrepoint.Y- (p2_Radius), p2_Radius*2, p2_Radius*2);
			e.Graphics.DrawEllipse(White2Pen,centrepoint.X- (p3_Radius), centrepoint.Y- (p3_Radius), p3_Radius*2, p3_Radius*2);
			e.Graphics.DrawEllipse(White2Pen,centrepoint.X- (p4_Radius), centrepoint.Y- (p4_Radius), p4_Radius*2, p4_Radius*2);
			e.Graphics.DrawEllipse(White2Pen,centrepoint.X- (p5_Radius), centrepoint.Y- (p5_Radius), p5_Radius*2, p5_Radius*2);

			e.Graphics.DrawEllipse(White2Pen,centrepoint.X- (p1_Radius+1), centrepoint.Y- (p1_Radius+1), (p1_Radius+1)*2, (p1_Radius+1)*2);
			e.Graphics.DrawEllipse(White2Pen,centrepoint.X- (p1_Radius-1), centrepoint.Y- (p1_Radius-1), (p1_Radius-1)*2, (p1_Radius-1)*2);

			//=======================================================================
			//==Draw the Positional boxes for the Icon and Text for the operations=== 
			//=======================================================================
			if (showconstruction)
			{
				foreach (Point pt in OpLogoPositions.Values)
				{
					e.Graphics.DrawRectangle(Pens.Purple,pt.X, pt.Y, 30, 30);
				}
				int cc = 1;
				foreach (Point pt in OpTextPositions.Values)
				{
					e.Graphics.DrawRectangle(Pens.Indigo,pt.X, pt.Y, 70, 14);
					e.Graphics.DrawRectangle(Pens.PaleVioletRed,pt.X, pt.Y+16, 70, 14);
					e.Graphics.DrawString(cc.ToString(),BoldFont8, Brushes.Aquamarine,pt);
					cc++;
				}
			}
//			e.Graphics.DrawRectangle(Pens.Blue, 0, con_wd, 2, 2);
//			ArrayList al = new ArrayList();
//			for (int step = 0; step < 24; step++)
//			{
//				startA = (step+0)*15;
//				startRayPoint = this.GeneratePointAtRadius(true,startA,centrepoint.X, centrepoint.Y,p1_Radius+2);
//				e.Graphics.DrawRectangle(Pens.Blue, startRayPoint.X, startRayPoint.Y, 3,3);
//				al.Add(startRayPoint);
//			}
//			Point[] pa = new Point[al.Count];
//			int newstep=0;
//			foreach(Point pp in al)
//			{
//				pa[newstep]=pp;
//				newstep++;
//			}
//			e.Graphics.DrawPolygon(Pens.LightBlue, pa);
		}

	}
	
}
