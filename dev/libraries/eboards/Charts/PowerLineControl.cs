using System;
using System.Drawing;
using System.Drawing.Drawing2D;
//using BaseUtils;
//using Basement;

using LibCore;

namespace Charts
{
	/// <summary>
	/// Summary description for PowerLineControl.
	/// </summary>
	public class PowerLineControl : System.Windows.Forms.UserControl
	{
		Font font = CoreUtils.SkinningDefs.TheInstance.GetFont(10f);
		Pen PenGrid =  new Pen(Color.DarkGray, 1);
		Pen PenBounds = new Pen(Color.Red, 1);
		Pen PenMoneySpend_SP = new Pen(Color.DarkSeaGreen, 3);
		Pen PenMoneyIncome_SP = new Pen(Color.Orange, 3);
		Pen[] PenMoneySpend_Prjs = new Pen[6];

		Brush brushTextBlack = new SolidBrush(Color.Black);
		Brush brushTextWhite = new SolidBrush(Color.White);

		int DisplayRound =1;
		int DisplayProject =1;
		Boolean GraphBuffer = true;
		Boolean ShowDebugConstructions = true;

		/// <summary> 
		/// Required designer variable.
		/// </summary>
		System.ComponentModel.Container components = null;

		public PowerLineControl()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			// TODO: Add any initialization after the InitializeComponent call
			PenMoneySpend_Prjs[0] = new Pen(Color.DeepPink, 3);
			PenMoneySpend_Prjs[1] = new Pen(Color.DarkSlateBlue, 3);
			PenMoneySpend_Prjs[2] = new Pen(Color.DarkOrchid, 3);	
			PenMoneySpend_Prjs[3] = new Pen(Color.DarkSalmon, 3);
			PenMoneySpend_Prjs[4] = new Pen(Color.DarkRed, 3);
			PenMoneySpend_Prjs[5] = new Pen(Color.DarkViolet, 3);
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

				//Dispose of My Pens and Brushes
//				PenMoneySpend_Prjs[0].Dispose();
//				PenMoneySpend_Prjs[1].Dispose();
//				PenMoneySpend_Prjs[2].Dispose();
//				PenMoneySpend_Prjs[3].Dispose();
//				PenMoneySpend_Prjs[4].Dispose();
//				PenMoneySpend_Prjs[5].Dispose();
//
//				font.Dispose();
//				PenGrid.Dispose();
//				PenBounds.Dispose();
//				PenMoneySpend_SP.Dispose();
//				PenMoneyIncome_SP.Dispose();
//				brushTextBlack.Dispose();
//				brushTextWhite.Dispose();
			}
			base.Dispose( disposing );
		}

		#region Component Designer generated code
		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			// 
			// PowerLineControl
			// 
			this.Name = "PowerLineControl";
			this.Paint += new System.Windows.Forms.PaintEventHandler(this.PowerLineControl_Paint);

		}
		#endregion

		#region Set Methods

		public void SetRound(int Round)
		{
			DisplayRound = Round;
		}

		public void SetDisplayScope(int Scope)
		{
//			if ((Scope<1)|(Scope>GameConstants.MAX_NUMBER_SLOTS))
//			{
//				DisplayProject = -1;
//			}
//			else
//			{
//				DisplayProject = Scope;
//			}
		}		
		
//		public void SetRounddataHandle(PerfData_Round RoundDataObj)
//		{
//			MyPerfData_RoundDataHandle = RoundDataObj;
//			MyPerfData_MaturityHandle = MyPerfData_RoundDataHandle.getMaturityData();
//			MyPerfData_RacePosDataHandle = MyPerfData_RoundDataHandle.getRacePosData();
//			MyPerfData_DeptDataHandle = MyPerfData_RoundDataHandle.getDeptPerfData();
//
//			for (int step =0; step < GameConstants.MAX_NUMBER_SLOTS; step++)
//			{
//				MyPerfData_Projects[step] = MyPerfData_RoundDataHandle.getProjData(step);
//			}
//		}

		#endregion Set Methods

		#region Limits Methods

		int getBudgetIncomeMaxValueForProject(int ProjectSlot)
		{
			int ProjectMoneyIncome=0;
			int ProjectMoneyIncomeHighPt=0;
//			for (int step2=0; step2 < GameConstants.MAX_NUMBER_DAYS; step2++)
//			{
//				ProjectMoneyIncome += MyPerfData_Projects[ProjectSlot-1].BudgetIncome[step2];
//				if (ProjectMoneyIncome>ProjectMoneyIncomeHighPt)
//				{
//					ProjectMoneyIncomeHighPt=ProjectMoneyIncome;
//				}
//			}

			if (ProjectMoneyIncomeHighPt>ProjectMoneyIncome)
			{
				ProjectMoneyIncome = ProjectMoneyIncomeHighPt;
			}

//			if (ProjectMoneyIncome < MyPerfData_Projects[ProjectSlot-1].ProjectSIPBudget)
//			{
//				ProjectMoneyIncome = MyPerfData_Projects[ProjectSlot-1].ProjectSIPBudget;
//			}

			if (GraphBuffer)
			{ProjectMoneyIncome = (ProjectMoneyIncome * 110) / 100; }

			return ProjectMoneyIncome;
		}

//		private int getBudgetExpendMaxValueForProject(int ProjectSlot)
//		{
//			int ProjectMoneySpend=0;
//			for (int step2=0; step2 < 25; step2++)
//			{
//				ProjectMoneySpend += MyPerfData_Projects[ProjectSlot-1].BudgetExpenditure[step2];
//			}
//			if (GraphBuffer)
//			{ProjectMoneySpend = (ProjectMoneySpend * 110) / 100; }
//			return ProjectMoneySpend;
//		}

//		private int getMaxProjectCulmExpenditureForAllProjects()
//		{
//			int MaxProjectSpend=-1;
//			for (int step1 =0; step1 < GameConstants.MAX_NUMBER_SLOTS; step1++)
//			{
//				int ProjectMoneySpend=0;
//				for (int step2=0; step2 < GameConstants.MAX_NUMBER_DAYS; step2++)
//				{
//					ProjectMoneySpend += MyPerfData_Projects[step1].BudgetExpenditure[step2];
//				}
//				if (ProjectMoneySpend > MaxProjectSpend)
//				{
//					MaxProjectSpend = ProjectMoneySpend;
//				}
//			}
//
//			if (GraphBuffer)
//			{MaxProjectSpend = (MaxProjectSpend * 110) / 100; }
//			return MaxProjectSpend;
//		}

		#endregion Limits Methods

		void PowerLineControl_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
		{
			Boolean DrawGrid = true;
			Graphics g = e.Graphics;
			g.SmoothingMode = SmoothingMode.HighQuality;
			int gridOffsetX=3; 
			int gridOffsetY=3; 
			int GraphOffsetX = 40;
			int GraphOffsetY = 15;

			int KeyHeight = 22;
			int KeyWidth = this.Width-6;

			//int StageHeight = 30;
			//int StageWidth = this.Width-6;
			int StageHeight = 0;

			//int TotalAllowedHeight = this.Height-10;
			int graphWidth=this.Width - (2*gridOffsetX) - (GraphOffsetX);
			int graphHeight=this.Height - ((2*gridOffsetY) + (KeyHeight) + (StageHeight) + (GraphOffsetY)); //was 2

			int columnWidth = (graphWidth) / 25;
			int rowHeight = (graphHeight-30) / 20;
			int rowCount = 20;

			Pen PenRBounds = Pens.Pink;
			Pen PenPBounds = Pens.Red;
			Pen PenBBounds = Pens.Blue;
			
			//Using different colours in the Grid really helps when debug 
			Pen PenGridV1 = Pens.LightGray;
			Pen PenGridV2 = Pens.LightGray;
			Pen PenGridH1 = Pens.LightGray;
			Pen PenGridH2 = Pens.LightGray;
			Pen PenGridXX = Pens.LightGreen;
			Pen PenGridBB = Pens.Green;
				
			try 
			{
				if (ShowDebugConstructions)
				{
					//Debug: Draw the Allowed Size for the entire Area 
					e.Graphics.DrawLine(PenRBounds, 0,0, this.Width, 0);
					e.Graphics.DrawLine(PenRBounds, this.Width-1,0, this.Width-1, this.Height-1);
					e.Graphics.DrawLine(PenRBounds, 0,this.Height-1, this.Width-1, this.Height-1);
					e.Graphics.DrawLine(PenRBounds, 0,0, 0, this.Height-1);
					//Debug: Draw the Allowed Size for the Chart Key 
					e.Graphics.DrawLine(PenPBounds, 3+3,this.Height-KeyHeight, KeyWidth, this.Height-KeyHeight); //h1
					e.Graphics.DrawLine(PenPBounds, 3+KeyWidth,this.Height-KeyHeight, 3+KeyWidth, this.Height-3); //v1
					e.Graphics.DrawLine(PenPBounds, 3+3,this.Height-3, KeyWidth, this.Height-3);	//h2
					e.Graphics.DrawLine(PenPBounds, 3+3,this.Height-KeyHeight, 3+3, this.Height-3); //v2

					//Debug: Draw the Allowed Size for the Stage Chart
					//e.Graphics.DrawLine(PenPBounds, 3+3,this.Height-StageHeight-KeyHeight, StageWidth, this.Height-StageHeight-KeyHeight); //h1
					//e.Graphics.DrawLine(PenPBounds, 3+KeyWidth,this.Height-StageHeight-KeyHeight, 3+StageWidth, this.Height-3-KeyHeight); //v1
					//e.Graphics.DrawLine(PenPBounds, 3+3,this.Height-3-KeyHeight, StageWidth, this.Height-3-KeyHeight);	//h2
					//e.Graphics.DrawLine(PenPBounds, 3+3,this.Height-StageHeight-KeyHeight, 3+3, this.Height-3-KeyHeight); //v2

					//Draw Bounds Box allowed for Graph
					e.Graphics.DrawLine(PenBBounds, gridOffsetX, gridOffsetY, gridOffsetX+(graphWidth-1)+GraphOffsetX, gridOffsetY);
					e.Graphics.DrawLine(PenBBounds, (graphWidth-1)+gridOffsetX+GraphOffsetX,gridOffsetY, (graphWidth-1)+gridOffsetX+GraphOffsetX, (graphHeight-1)+gridOffsetY+GraphOffsetY);
					e.Graphics.DrawLine(PenBBounds, gridOffsetX,(graphHeight-1)+gridOffsetY+GraphOffsetY, (graphWidth-1)+gridOffsetX+GraphOffsetX, (graphHeight-1)+gridOffsetY+GraphOffsetY);
					e.Graphics.DrawLine(PenBBounds, gridOffsetX, gridOffsetY, gridOffsetX, (graphHeight-1)+gridOffsetY);
				}

				//Draw the Key
				//Draw the Key Text Elements 
				e.Graphics.DrawString("Key", font, brushTextBlack, 10, this.Height-KeyHeight);
				e.Graphics.DrawString("Project 1", font, brushTextBlack, gridOffsetX+16, this.Height-KeyHeight);
				e.Graphics.DrawString("Project 2", font, brushTextBlack, gridOffsetX+170, this.Height-KeyHeight);
				e.Graphics.DrawString("Project 3", font, brushTextBlack, gridOffsetX+340, this.Height-KeyHeight);
				e.Graphics.DrawString("Project 4", font, brushTextBlack, gridOffsetX+490, this.Height-KeyHeight);
				//Draw the Key Sample Colour patchs
				//e.Graphics.FillRectangle(brushIntStaffNormal,gridOffsetX,(this.Height-KeyHeight)+5,10,10);
				//e.Graphics.FillRectangle(brushIntStaffNormalWait,gridOffsetX+160,(this.Height-KeyHeight)+5,10,10);
				//e.Graphics.FillRectangle(brushExtStaffOverTime,gridOffsetX+330,(this.Height-KeyHeight)+5,10,10);
				//e.Graphics.FillRectangle(brushExtStaffNormalWait,gridOffsetX+480,(this.Height-KeyHeight)+5,10,10);

				//Check that we have a good Handle before extracting the data and Drawing the Graph
				//If no Data then we can't draw the Axis and we don't know the scaling required so draw nothing 
				//if (MyPerfData_DeptDataHandle != null)
				if (true)
					{
					//Drawing Horizontal Grid Lines Lines (only always draw bottom axis line)
					for (int step =0; step <= (rowCount); step++)
					{
						int xline = gridOffsetX+GraphOffsetX+10;
						int yline = gridOffsetY+GraphOffsetY+step*rowHeight;
						if (step == rowCount)
						{
							e.Graphics.DrawLine(PenGridH1, xline, yline, xline+(columnWidth*(25)), yline);
						}
						else
						{
							if (DrawGrid)
							{
								e.Graphics.DrawLine(PenGridH1, xline, yline, xline+(columnWidth*(25)), yline);
							}
						}
					}
					//Drawing the Vertical Lines and the Day Labels
					for (int step =0; step <= 25; step++)
					{
						int LowLimitY = gridOffsetY+GraphOffsetY;
						int HighLimitY = gridOffsetY+GraphOffsetY + (rowCount*rowHeight);

						if (step == 0)
						{
							e.Graphics.DrawLine(PenGridV1,gridOffsetX+GraphOffsetX+step*columnWidth+10, LowLimitY, gridOffsetX+GraphOffsetX+step*columnWidth+10, HighLimitY);
						}
						else
						{
							if (DrawGrid)
							{
								e.Graphics.DrawLine(PenGridV1,gridOffsetX+GraphOffsetX+step*columnWidth+10, LowLimitY, gridOffsetX+GraphOffsetX+step*columnWidth+10, HighLimitY);
							}
						}
						if (step < 25)
						{
							//Draw the Day number Text 
							if (step<9)
							{ 
								e.Graphics.DrawString(CONVERT.ToStr(step+1), font, brushTextBlack, gridOffsetX+GraphOffsetX+step*columnWidth+((columnWidth*10)/20)+5, HighLimitY  + 10);
							}
							else
							{ 
								e.Graphics.DrawString(CONVERT.ToStr(step+1), font, brushTextBlack, gridOffsetX+GraphOffsetX+step*columnWidth+((columnWidth*10)/20)+1, HighLimitY + 10);
							}
						}
					}

					int MaxMoney = 0;
					//Draw the Money Axis Numbers 
					//
					if (DisplayProject == -1)
					{
						//No project selected, so draw all projects 
						//so we need to scale by the max overall projects 
						//MaxMoney = getMaxProjectCulmExpenditureForAllProjects();
					}
					else
					{
						//Wish to display a single project 
//						MaxMoneySpend = getBudgetExpendMaxValueForProject(DisplayProject);
//						MaxMoneyIncome = getBudgetIncomeMaxValueForProject(DisplayProject);
//						if (MaxMoneyIncome > MaxMoneySpend)
//						{
//							if (MaxMoneyIncome > MaxMoney)
//							{
//								MaxMoney = MaxMoneyIncome;
//							}
//						}
//						else
//						{
//							if (MaxMoneySpend > MaxMoney)
//							{
//								MaxMoney = MaxMoneySpend;
//							}
//						}
//						//If we have no Money then we need to display a flat line scaled the the projects
//						if (!(MaxMoney >0))
//						{
//							MaxMoney = getMaxProjectCulmExpenditureForAllProjects();
//						}
					}
					if (MaxMoney > 0)
					{
						//Drawing the Money Axis in Thousands based on Max Money
						for (int step =0; step <= (rowCount); step++)
						{
							int xline = gridOffsetX+GraphOffsetX+10-28;
							int yline = gridOffsetY+GraphOffsetY+step*rowHeight-6;
							int val = (MaxMoney * (rowCount-step)) / rowCount;
							int kval = val /1000;
							e.Graphics.DrawString(CONVERT.ToStr(kval), font, brushTextBlack,xline,yline);
						}

						// Iterate through all projects 	
						for (int PrjStepper =0; PrjStepper < 25; PrjStepper++)
						{
							if ((DisplayProject == -1)|((DisplayProject-1)==PrjStepper))
							{
								int halfstep = (columnWidth*10)/20;

								//Drawing the specific project 
								if ((DisplayProject != -1))
								{
									for (int step=0; step < 25; step++)
									{
										//Drawing the Budget Income Line 
//										MoneyValue += MyPerfData_Projects[PrjStepper].BudgetIncome[step];
//										MaxLimitY = gridOffsetY+GraphOffsetY + (rowCount*rowHeight);
//										if (MoneyValue >0)
//										{
//											ylevel = (MaxLimitY) - ((rowCount*rowHeight)*MoneyValue)/MaxMoney;
//											int StartX=gridOffsetX+GraphOffsetX+10;
//											int StopX=gridOffsetX+GraphOffsetX+(columnWidth*(25));
//											e.Graphics.DrawLine(PenMoneyIncome_SP, StartX, ylevel, StopX, ylevel);
//											e.Graphics.DrawLine(PenMoneyIncome_SP, StartX, ylevel+1, StopX, ylevel+1);
//											e.Graphics.DrawLine(PenMoneyIncome_SP, StartX, ylevel-1, StopX, ylevel-1);
//										}
										//Drawing the Vertical Lines for the Stages
//										string PhaseStr = MyPerfData_Projects[PrjStepper].ActualProjectPhaseByDay[step];
//										if (PhaseStr != string.Empty)
//										{
//											if (PhaseStr.Length>1)
//											{
//												string firstValue = PhaseStr.Substring(1,1);
//												if (firstValue != "1")
//												{
//													int xpos = gridOffsetX+GraphOffsetX+step*columnWidth+((columnWidth*10)/20)+5;
//													int ypos = gridOffsetY+GraphOffsetY + ((rowCount-1)*rowHeight) + 5;
//													e.Graphics.DrawString(firstValue, font, brushTextBlack, xpos, ypos);
//												}
//											}
//										}
									}
								}

								//Drawing the Money Expenditure Line 
//								for (int step=0; step < 25; step++)
//								{
//									MoneySpendCounter += MyPerfData_Projects[PrjStepper].BudgetExpenditure[step];
//									MaxLimitY = gridOffsetY+GraphOffsetY + (rowCount*rowHeight);
//
//									if (MaxMoney>0)
//									{
//										ylevel = (MaxLimitY) - ((rowCount*rowHeight)*MoneySpendCounter)/MaxMoney;
//									}
//
//									NowX=gridOffsetX+GraphOffsetX+step*columnWidth + halfstep + 10;
//									NowY=ylevel;
//									if ((prevX != -1)&(prevY != -1))
//									{
//										e.Graphics.DrawLine(PenMoneySpend_Prjs[PrjStepper],prevX, prevY, NowX, NowY);
//									}
//									//e.Graphics.DrawLine(PenGridXX,NowX, ylevel, NowX, MaxLimitY);
//									//e.Graphics.DrawLine(PenGridXX,NowX+1, ylevel, NowX+1, MaxLimitY);
//									//e.Graphics.DrawLine(PenGridXX,NowX+2, ylevel, NowX+2, MaxLimitY);
//									prevX=NowX;
//									prevY=NowY;
//								}
							}
						}
					}
				}
				//it.Stop();
				//System.Diagnostics.Debug.WriteLine("##### "+ it.GetSeconds().ToString());
			}
			catch (Exception evc)
			{
				string e12 = evc.Message;
				string e13 = evc.StackTrace;
				e12= e12+"";
			}
		}
	}
}


					//Mode 1 all projects displayed with no budget lines 
					//Mode 2 Individual projects with Budget Lines 
					//Calculate 

//					DisplayScopeReference = 1;
//					if (DisplayScopeReference<=GameConstants.MAX_NUMBER_SLOTS)
//					{
//						if (MyPerfData_Projects[DisplayScopeReference-1] != null)
//						{
//							//Dtermine the Maximum Amount of Money
//							int MaxMoneySpend =0; 
//							int MaxMoneyIncome =0; 
//							int MaxMoney =0; 
//							for (int step=0; step < GameConstants.MAX_NUMBER_DAYS; step++)
//							{
//								MaxMoneySpend += MyPerfData_Projects[DisplayScopeReference-1].BudgetExpenditure[step];
//								MaxMoneyIncome += MyPerfData_Projects[DisplayScopeReference-1].BudgetIncome[step];
//								if (MaxMoneyIncome > MaxMoneySpend)
//								{
//									if (MaxMoneyIncome > MaxMoney)
//									{
//										MaxMoney = MaxMoneyIncome;
//									}
//								}
//								else
//								{
//									if (MaxMoneySpend > MaxMoney)
//									{
//										MaxMoney = MaxMoneySpend;
//									}
//								}
//							}
							
							//Drawing the Money Axis in Thousands
//							for (int step =0; step <= (rowCount); step++)
//							{
//								int xline = gridOffsetX+GraphOffsetX+10-28;
//								int yline = gridOffsetY+GraphOffsetY+step*rowHeight-6;
//								int val = (MaxMoney * (rowCount-step)) / rowCount;
//								int kval = val /1000;
//								e.Graphics.DrawString((kval).ToString(), font, brushTextBlack,xline,yline);
//							}
//
//							int MoneySpendCounter=0;
//							int MaxLimitY=0;
//							int ylevel=0;
//							int prevX=-1;
//							int	prevY=-1;
//							int NowX=-1;
//							int	NowY=-1;
//							int halfstep = (columnWidth*10)/20;
//
//							//Drawing the Money Income Line 
//							int MoneyValue =0;
//
//							for (int step=0; step < GameConstants.MAX_NUMBER_DAYS; step++)
//							{
//								MoneyValue = MyPerfData_Projects[DisplayScopeReference-1].BudgetIncome[step];
//								MaxLimitY = gridOffsetY+GraphOffsetY + (rowCount*rowHeight);
//
//								if (MoneyValue >0)
//								{
//									ylevel = (MaxLimitY) - ((rowCount*rowHeight)*MoneyValue)/MaxMoney;
//									int StartX=gridOffsetX+GraphOffsetX+10;
//									int StopX=gridOffsetX+GraphOffsetX+(columnWidth*(GameConstants.MAX_NUMBER_DAYS));
//									e.Graphics.DrawLine(PenMoneyIncome, StartX, ylevel, StopX, ylevel);
//									e.Graphics.DrawLine(PenMoneyIncome, StartX, ylevel+1, StopX, ylevel+1);
//									e.Graphics.DrawLine(PenMoneyIncome, StartX, ylevel-1, StopX, ylevel-1);
//								}
//							}
//
//							//Drawing the Money Expenditure Line 
//							for (int step=0; step < GameConstants.MAX_NUMBER_DAYS; step++)
//							{
//								MoneySpendCounter += MyPerfData_Projects[DisplayScopeReference-1].BudgetExpenditure[step];
//								MaxLimitY = gridOffsetY+GraphOffsetY + (rowCount*rowHeight);
//
//								if (MaxMoney>0)
//								{
//									ylevel = (MaxLimitY) - ((rowCount*rowHeight)*MoneySpendCounter)/MaxMoney;
//								}
//
//								NowX=gridOffsetX+GraphOffsetX+step*columnWidth + halfstep + 10;
//								NowY=ylevel;
//								if ((prevX != -1)&(prevY != -1))
//								{
//									e.Graphics.DrawLine(PenMoneySpend,prevX, prevY, NowX, NowY);
//								}
//								//e.Graphics.DrawLine(PenGridXX,NowX, ylevel, NowX, MaxLimitY);
//								//e.Graphics.DrawLine(PenGridXX,NowX+1, ylevel, NowX+1, MaxLimitY);
//								//e.Graphics.DrawLine(PenGridXX,NowX+2, ylevel, NowX+2, MaxLimitY);
//								prevX=NowX;
//								prevY=NowY;
//							}
//						}
//					}
//
//		public int[] BudgetExpenditure = new int[GameConstants.MAX_NUMBER_DAYS];				//Money Going Out per Day
//		public int[] BudgetIncome = new int[GameConstants.MAX_NUMBER_DAYS];							//Money Going In per Day
					//Process the Data to build the Numbers and Scaling for the Upper and Lower Graphs
//					if (DisplayUsingDevData)
//					{
//						MaxIntStaff = MyPerfData_DeptDataHandle.MaxIntDevStaff;
//						MaxExtStaff = MyPerfData_DeptDataHandle.MaxExtDevStaff;
//					}
//					else
//					{
//						MaxIntStaff = MyPerfData_DeptDataHandle.MaxIntTestStaff;
//						MaxExtStaff = MyPerfData_DeptDataHandle.MaxExtTestStaff;
//					}
//
//					if (TakeOvertimeIntoAccountForGraph)
//					{
//						UG_MaxHoursForThisDay = (MaxExtStaff * (int)(emWorkTimeMode.OVERTIME));
//						LG_MaxHoursForThisDay = (MaxIntStaff * (int)(emWorkTimeMode.OVERTIME));
//					}
//					else
//					{
//						UG_MaxHoursForThisDay = (MaxExtStaff * (int)(emWorkTimeMode.NORMAL));
//						LG_MaxHoursForThisDay = (MaxIntStaff * (int)(emWorkTimeMode.NORMAL));
//					}

//					Tot_MaxHoursForThisDay = UG_MaxHoursForThisDay + LG_MaxHoursForThisDay;
//					UG_StaffCount = MaxExtStaff;
//					LG_StaffCount = MaxIntStaff;
//
//					UG_graphHeight = ((TotalAllowedHeight-TotalGaps) * UG_MaxHoursForThisDay) / Tot_MaxHoursForThisDay;
//					LG_graphHeight = ((TotalAllowedHeight-TotalGaps) * LG_MaxHoursForThisDay) / Tot_MaxHoursForThisDay;
//
//					UG_PixelsPerSingleHour = UG_graphHeight / UG_MaxHoursForThisDay;
//					LG_PixelsPerSingleHour = LG_graphHeight / LG_MaxHoursForThisDay;
					
					//Draw the Overall Chart Text Elements 
//					int MidPoint = gridOffsetY + UpperGap + UG_graphHeight;
//					e.Graphics.DrawString("Day Number", font, brushTextBlack, (graphWidth / 2) - 10 , TotalAllowedHeight - 35);
//					e.Graphics.DrawString("Normal", font, brushTextBlack, gridOffsetX - 75 ,MidPoint - 40);
//					e.Graphics.DrawString("Work", font, brushTextBlack, gridOffsetX - 75 , MidPoint - 20);
//					e.Graphics.DrawString("Days", font, brushTextBlack, gridOffsetX - 75 , MidPoint);

					//Debug Guidance Lines 
					//e.Graphics.DrawLine(PenBBounds, 2, UpperGap, 2, UG_graphHeight+UpperGap);
					//e.Graphics.DrawLine(PenBBounds, 3, UG_graphHeight+UpperGap+MiddleGap, 3, UG_graphHeight+LG_graphHeight+UpperGap+MiddleGap);

					//Draw the Horizontal Lines and Axis Text for the Upper Graph  
//					UG_PixelsPerSingleHour = UG_graphHeight / UG_MaxHoursForThisDay;
//					int UG_verticalstep = ((int)(emWorkTimeMode.NORMAL) * UG_graphHeight)/ UG_MaxHoursForThisDay;  //how many normal work days of 8 hours
//					int UG_verticalcount = UG_MaxHoursForThisDay / (int)(emWorkTimeMode.NORMAL);  //how many normal work days equivalent
//					int step2a=0;
//					for (int step =0; step <= (UG_verticalcount+1); step++)
//					{
//						int xline = gridOffsetX;
//						int yline = gridOffsetY+UG_graphHeight-((step*1)*UG_verticalstep);
//						//draw the horizontal grid line 
//						if (step == UG_StaffCount)
//						{
//							e.Graphics.DrawLine(PenGridH1,xline, yline-1, xline+(gridCellWidth*(GameConstants.MAX_NUMBER_DAYS-1))-1, yline-1);
//							e.Graphics.DrawLine(PenGridH1,xline, yline, xline+(gridCellWidth*(GameConstants.MAX_NUMBER_DAYS-1))-1, yline);
//							e.Graphics.DrawLine(PenGridH1,xline, yline+1, xline+(gridCellWidth*(GameConstants.MAX_NUMBER_DAYS-1))-1, yline+1);
//						}
//						else
//						{
//							e.Graphics.DrawLine(PenGridH1,xline, yline, xline+(gridCellWidth*(GameConstants.MAX_NUMBER_DAYS-1))-1, yline);
//						}
//						if (step < (UG_verticalcount))
//						{
//							//draw the text (normal work day)
//							e.Graphics.DrawString((step2a+1).ToString(), font, brushTextBlack, gridOffsetX-20, (yline-(UG_verticalstep))-7);
//							step2a++;
//						}
//					}

					//Draw the Horizontal Lines and Axis Text for the Lower Graph  
					//int LG_Offset = UpperGap+UG_graphHeight+MiddleGap;
//					int LG_Offset = UpperGap+0+MiddleGap;
//					LG_PixelsPerSingleHour = LG_graphHeight / LG_MaxHoursForThisDay;
//					int LG_verticalstep = ((int)(emWorkTimeMode.NORMAL) * LG_graphHeight)/ LG_MaxHoursForThisDay;  //how many normal work days of 8 hours
//					int LG_verticalcount = LG_MaxHoursForThisDay / (int)(emWorkTimeMode.NORMAL);  //how many normal work days equivalent
//					int step2b=0;
//					for (int step =0; step <= (LG_verticalcount+1); step++)
//					{
//						int xline = gridOffsetX;
//						int yline = gridOffsetY+LG_graphHeight-((step*1)*UG_verticalstep);
//						//draw the horizontal grid line 
//						if (step == LG_StaffCount)
//						{
//							e.Graphics.DrawLine(PenGridH2, xline, LG_Offset+yline-1, xline+(gridCellWidth*(GameConstants.MAX_NUMBER_DAYS-1))-1, LG_Offset+yline-1);
//							e.Graphics.DrawLine(PenGridH2, xline, LG_Offset+yline, xline+(gridCellWidth*(GameConstants.MAX_NUMBER_DAYS-1))-1, LG_Offset+yline);
//							e.Graphics.DrawLine(PenGridH2, xline, LG_Offset+yline+1, xline+(gridCellWidth*(GameConstants.MAX_NUMBER_DAYS-1))-1, LG_Offset+yline+1);
//						}
//						else
//						{
//							e.Graphics.DrawLine(PenGridH2, xline, LG_Offset+yline, xline+(gridCellWidth*(GameConstants.MAX_NUMBER_DAYS-1))-1, LG_Offset+yline);
//						}
//						if (step < (LG_verticalcount))
//						{
//							//draw the text (normal work day)
//							e.Graphics.DrawString((step2b+1).ToString(), font, brushTextBlack, gridOffsetX-20, LG_Offset+(yline-(LG_verticalstep))-7);
//							step2b++;
//						}
//					}

					//Draw the Vertical lines between the days and text labels for day count along the bottom
					//There is a Upper Graph Vertical line and Lower Graph Vertical Line and a gap
//						if (step < GameConstants.MAX_NUMBER_DAYS)
//						{
//							//Draw the Day number Text 
//							if (step<9)
//							{ 
//								e.Graphics.DrawString((step+1).ToString(), font, brushTextBlack, step*columnwidth+gridOffsetX+10, TotalAllowedHeight - 50);
//							}
//							else
//							{ 
//								e.Graphics.DrawString((step+1).ToString(), font, brushTextBlack, step*columnwidth+gridOffsetX+4, TotalAllowedHeight - 50);
//							}
//						}
						//Draw the Vertical Grid for the Upper Grid 
						//minyline = gridOffsetY+UG_graphHeight-(((UG_verticalcount+1)*1)*UG_verticalstep);
						//maxyline = gridOffsetY+UG_graphHeight-((0*1)*UG_verticalstep);
						//e.Graphics.DrawLine(PenGridV1,step*columnwidth+gridOffsetX, minyline,step*columnwidth+gridOffsetX, maxyline);

						//Draw the Vertical Grid for the Lower Grid 
						//minyline = LG_Offset+gridOffsetY+LG_graphHeight-(((LG_verticalcount+1)*1)*LG_verticalstep);
						//maxyline = LG_Offset+gridOffsetY+LG_graphHeight-((0*1)*LG_verticalstep);
						//e.Graphics.DrawLine(PenGridV2,step*columnwidth+gridOffsetX, minyline,step*columnwidth+gridOffsetX, maxyline);

						//Determine Where the Data is coming From 

//						IntNormalWorkPixelHeight = (IntHoursBooked_Work * LG_verticalstep) / ((int)(emWorkTimeMode.NORMAL));
//						IntNormalWaitPixelHeight = (IntHoursBooked_Wait * LG_verticalstep) / ((int)(emWorkTimeMode.NORMAL));
//						ExtNormalWorkPixelHeight = (ExtHoursBooked_Work * UG_verticalstep) / ((int)(emWorkTimeMode.NORMAL));
//						ExtNormalWaitPixelHeight = (ExtHoursBooked_Wait * UG_verticalstep) / ((int)(emWorkTimeMode.NORMAL));
//
//						//Draw the stacked boxes if needed in the Lower Grid 
//						toppoint= gridOffsetY + LG_Offset + LG_graphHeight;
//						if (IntNormalWorkPixelHeight > 0) 
//						{
//							//First box Internal Staff Normal Work
//							e.Graphics.FillRectangle(brushIntStaffNormal,step*columnwidth+gridOffsetX,
//								(toppoint - IntNormalWorkPixelHeight), columnwidth, IntNormalWorkPixelHeight);
//							toppoint-= IntNormalWorkPixelHeight;
//						}
//						if (IntNormalWaitPixelHeight > 0) 
//						{
//							//Second box Internal Staff Overtime Work
//							e.Graphics.FillRectangle(brushIntStaffNormalWait,step*columnwidth+gridOffsetX,
//								(toppoint - IntNormalWaitPixelHeight), columnwidth, IntNormalWaitPixelHeight);
//							toppoint-= IntNormalWaitPixelHeight;
//						}
//
//						//toppoint= gridOffsetY + UG_graphHeight;
//						toppoint= gridOffsetY + 0;
//						if (ExtNormalWorkPixelHeight > 0) 							
//						{
//							//Third box External Staff Combined Work
//							e.Graphics.FillRectangle(brushExtStaffOverTime,step*columnwidth+gridOffsetX,
//								(toppoint - ExtNormalWorkPixelHeight), columnwidth, ExtNormalWorkPixelHeight);
//							toppoint-= ExtNormalWorkPixelHeight;
//						}
//						if (ExtNormalWaitPixelHeight > 0) 							
//						{
//							//Third box External Staff Combined Work
//							e.Graphics.FillRectangle(brushExtStaffNormalWait,step*columnwidth+gridOffsetX,
//								(toppoint - ExtNormalWaitPixelHeight), columnwidth, ExtNormalWaitPixelHeight);
//							toppoint-= ExtNormalWaitPixelHeight;
//						}
						//Set Default 0 
						//IntHoursBooked_Work = 0; 
						//IntHoursBooked_Wait = 0;
						//ExtHoursBooked_Work = 0; 
						//ExtHoursBooked_Wait = 0;
						//IntHoursBooked = 0;
						//ExtHoursBooked = 0;
						//IntPeopleUsed = 0;
						//ExtPeopleUsed = 0;
						//NormalHoursPossible_Int = 0;
						//NormalHoursPossible_Ext = 0;


						//Showing the number of People actually employed in that Day for Internal 
						//This was linked to the Vertical step above the axis  as (LG_CountOffset - LG_verticalstep) + 3
						//but the vertical box height changes according to number of people 
						//so just make it -15 above the axis
//						int LG_CountOffset = gridOffsetY + UpperGap + UG_graphHeight + MiddleGap + LG_graphHeight;
//						if (IntPeopleUsed>0)
//						{
//							//
//							if (step<9)
//							{ 
//								e.Graphics.DrawString((IntPeopleUsed).ToString(), font, brushTextWhite, 
//									step*columnwidth+gridOffsetX+10,  (LG_CountOffset) - 17 );
//							}
//							else
//							{ 
//								e.Graphics.DrawString((IntPeopleUsed).ToString(), font, brushTextWhite, 
//									step*columnwidth+gridOffsetX+4, (LG_CountOffset) - 17 );
//							}
//						}

//						//Showing the number of People actually employed in that Day for Internal 
//						int UG_CountOffset = gridOffsetY + UpperGap + UG_graphHeight;
//						if (ExtPeopleUsed>0)
//						{
//							if (step<9)
//							{ 
//								e.Graphics.DrawString((ExtPeopleUsed).ToString(), font, brushTextWhite, 
//									step*columnwidth+gridOffsetX+10,	(UG_CountOffset) - 25 );
//							}
//							else
//							{ 
//								e.Graphics.DrawString((ExtPeopleUsed).ToString(), font, brushTextWhite, 
//									step*columnwidth+gridOffsetX+4, (UG_CountOffset) - 25 );
//							}
//						}
