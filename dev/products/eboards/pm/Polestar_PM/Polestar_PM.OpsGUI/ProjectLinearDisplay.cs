using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Data;
using System.Windows.Forms;
using System.Xml;
using System.Globalization;
using System.Drawing.Text;
using System.IO;
using System.Runtime.InteropServices;

using LibCore;
using Network;
using CoreUtils;
using CommonGUI;
using Polestar_PM.DataLookup;

namespace Polestar_PM.OpsGUI
{
	/// <summary>
	/// This class display all the information for a project in one line. 
	/// 
	/// It allows people to provide a backk image that the information is displayed against
	/// It also has a delayed refresh (any event will kick off a 1 second timer and then we display)
	///   This helps reduce the flicker when all the activity happens at the start of the day. 
	/// 
	/// </summary>
	public class ProjectLinearDisplay :FlickerFreePanel
	{
		private Font MyDefaultSkinFontBold8 = null;
		private Font MyDefaultSkinFontBold10 = null;
		private Font MyDefaultSkinFontBold12 = null;

		protected Image mainback=null;
		protected Image projectBoxBack_green = null;
		protected Image productBoxBack_green = null;
		protected Image stageBoxBack_white = null;
		protected Image stageBoxBack_green = null;
		protected Image stageBoxBack_yellow = null;
		protected Image stageBoxBack_blue = null;
		protected Image stageBoxBack_amber = null;
		protected Image stageBoxBack_redrun = null;
		protected Image stageBoxBack_redpaused = null;
		protected Image stageBoxBack_empty = null;

		protected Image handoverBack_white = null;
		protected Image handoverBack_green = null;

		protected Image installBack_white = null;
		protected Image installBack_green = null;
		protected Image installBack_red = null;
		protected Image installBack_yellow = null;
		protected Image installBack_predict_fail = null;

		protected Image moneyBack_white = null;
		protected Image moneyBack_red = null;
		protected Image scopeBack_white = null;
		protected Image goliveBack_white = null;
		protected Image moneyBack_green = null;
		protected Image scopeBack_green = null;
		protected Image goliveBack_green = null;
		protected Image recycle_white = null;

		private NumberFormatInfo nfi = null;

		protected Node day_counter_node = null;
		protected Node project_main_node = null;
		protected Node project_subnode_prj = null;
		protected Node project_subnode_fin = null;
		protected ProjectReader pr = null;	
		protected ArrayList stages = new ArrayList();

		private StopControlledTimer _timer = null;
		protected int _round = 1;
		protected bool changed = false;
		protected bool ShowInternalGoLiveDayForDebug = false;
		
		/// <summary>
		/// The data has changed
		/// </summary>
		public bool Changed
		{
			set
			{
				changed = value;
				if(!_timer.Enabled)// && !stopped)
				{
					_timer.Start();
				}
				this.Invalidate();
			}
		}

		bool hideITDetails;

		/// <summary>
		/// Constructor 
		/// </summary>
		/// <param name="tree"></param>
		/// <param name="projectnode"></param>
		public ProjectLinearDisplay(NodeTree tree,  Node projectnode, int round, bool hideITDetails)
		{
			_timer = new StopControlledTimer();
			_timer.Interval = 1000;
			_timer.Tick += new EventHandler(_timer_Tick);

			this.hideITDetails = hideITDetails;

			_round = round;

			day_counter_node = tree.GetNamedNode("CurrentDay");
			if (day_counter_node != null)
			{
				day_counter_node.AttributesChanged +=new Network.Node.AttributesChangedEventHandler(day_counter_node_AttributesChanged);
			}

			nfi = new CultureInfo( "en-GB", false).NumberFormat;

			string fontname =  SkinningDefs.TheInstance.GetData("fontname");
			MyDefaultSkinFontBold8 = ConstantSizeFont.NewFont(fontname,8f,FontStyle.Bold);
			MyDefaultSkinFontBold10 = ConstantSizeFont.NewFont(fontname,10f,FontStyle.Bold);
			MyDefaultSkinFontBold12 = ConstantSizeFont.NewFont(fontname,12f,FontStyle.Bold);

			projectBoxBack_green = loadImage("project_back_green.png");
			productBoxBack_green = loadImage("product_back_green.png"); 

			stageBoxBack_white = loadImage("stage_back_white.png");
			stageBoxBack_green = loadImage("stage_back_green.png");
			stageBoxBack_blue = loadImage("stage_back_blue.png");
			stageBoxBack_amber = loadImage("stage_back_amber.png");
			stageBoxBack_redrun = loadImage("stage_back_redrun.png");
			stageBoxBack_redpaused = loadImage("stage_back_redpaused.png");
			stageBoxBack_empty = loadImage("stage_back_empty.png");
			stageBoxBack_yellow  = loadImage("stage_back_yellow.png");
			
			handoverBack_white = loadImage("handover_back_white.png");
			handoverBack_green = loadImage("handover_back_green.png");

			installBack_white = loadImage("install_back_white.png");
			installBack_green = loadImage("install_back_green.png");
			installBack_red = loadImage("install_back_red.png");
			installBack_yellow = loadImage("install_back_yellow.png");
			installBack_predict_fail = loadImage("install_back_predict_fail.png");

			moneyBack_white = loadImage("money_back_white.png");
			moneyBack_red = loadImage("money_back_red.png");
			scopeBack_white = loadImage("scope_back_white.png");
			goliveBack_white = loadImage("golive_back_white.png");
			moneyBack_green = loadImage("money_back_green.png");
			scopeBack_green = loadImage("scope_back_green.png");
			goliveBack_green = loadImage("golive_back_green.png");
			recycle_white = loadImage("recycle.png");

			//connect up to all information changes in the project
			
			if (projectnode != null)
			{
				project_main_node = projectnode;
				pr = new ProjectReader(project_main_node);

				//connect up to the Changing Overall information 
				project_main_node.AttributesChanged +=new Network.Node.AttributesChangedEventHandler(project_main_node_AttributesChanged);
				//connect up to the Changing Overall information 
				foreach (Node project_subnode in project_main_node.getChildren())
				{
					string subnode_type = project_subnode.GetAttribute("type");
					switch (subnode_type.ToLower())
					{
						case "financial_data": 
							project_subnode_fin = project_subnode;
							project_subnode_fin.AttributesChanged +=new Network.Node.AttributesChangedEventHandler(project_subnode_fin_AttributesChanged);
							break;

						case "project_data": 
							project_subnode_prj = project_subnode;
							project_subnode_prj.AttributesChanged +=new Network.Node.AttributesChangedEventHandler(project_subnode_prj_AttributesChanged);
							break;
						case "stages": 
							foreach (Node stage in project_subnode.getChildren())
							{
								stages.Add(stage);
								stage.AttributesChanged +=new Network.Node.AttributesChangedEventHandler(stage_AttributesChanged);
							}
							break;
						default:
							break;
					}
				}
			}
		}

		/// <summary>
		/// Dispose ...
		/// </summary>
		new public void Dispose()
		{
			if (_timer != null)
			{
				_timer.Stop();
				_timer.Dispose();
				_timer = null;
			}

			if (day_counter_node != null)
			{
				day_counter_node.AttributesChanged -=new Network.Node.AttributesChangedEventHandler(day_counter_node_AttributesChanged);
				day_counter_node = null;
			}

			if (stages.Count>0)
			{
				foreach (Node stage in stages)
				{
					stage.AttributesChanged -=new Network.Node.AttributesChangedEventHandler(stage_AttributesChanged);
				}
			}
			if (project_main_node != null)
			{
				project_main_node.AttributesChanged -=new Network.Node.AttributesChangedEventHandler(project_main_node_AttributesChanged);
				project_main_node = null;
			}
			if (project_subnode_fin != null)
			{
				project_subnode_fin.AttributesChanged -= new Network.Node.AttributesChangedEventHandler(project_subnode_fin_AttributesChanged);
				project_subnode_fin = null;
			}

			if (project_subnode_prj != null)
			{
				project_subnode_prj.AttributesChanged -= new Network.Node.AttributesChangedEventHandler(project_subnode_prj_AttributesChanged);
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

			//Disconnect from the nodes
			base.Dispose();
		}

		protected void AddAdditionalStages()
		{
			foreach (Node project_subnode in project_main_node.getChildren())
			{
				string subnode_type = project_subnode.GetAttribute("type");
				switch (subnode_type.ToLower())
				{
					case "stages":
						foreach (Node stage in project_subnode.getChildren())
						{
							if (stages.Contains(stage) == false)
							{
								stages.Add(stage);
								stage.AttributesChanged += new Network.Node.AttributesChangedEventHandler(stage_AttributesChanged);
							}
						}
						break;
				}
			}
		}

		/// <summary>
		/// We don't store the project slot in this class the project runner can get it
		/// storing local leads to problems when nodes change 
		/// </summary>
		/// <returns></returns>
		public int getSlotNumber()
		{
			int slot = 0;
			if (pr != null)
			{
				slot = pr.getProjectSlot();
			}
			return slot;
		}

		private Image loadImage(string imagename)
		{
			return Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images\\project\\"+imagename);
		}

		public void setBackImage(Image newback)
		{
			this.mainback = newback;
		}

		private void day_counter_node_AttributesChanged(Node sender, ArrayList attrs)
		{
			Changed = true;
			//this.Refresh(); //Old direct refesh on data change
		}

		private void project_main_node_AttributesChanged(Node sender, ArrayList attrs)
		{
			bool catching = false;

			//We dont want to update the inside of the project control when we are changing slot 
			//Changing Slot is related to the position of the project control on the screen
			//It is handled by the Project Status Display (which positions the project controls on screen)
			foreach (AttributeValuePair avp in attrs)
			{
				if (avp.Attribute != "slot")
				{
					catching = true;
				}
			}
			if (catching)
			{
				Changed = true;
			}
			//this.Refresh();//Old direct refesh on data change
		}

		private void project_subnode_fin_AttributesChanged(Node sender, ArrayList attrs)
		{
			Changed = true;
			//this.Refresh();//Old direct refesh on data change
		}

		private void project_subnode_prj_AttributesChanged(Node sender, ArrayList attrs)
		{
			//need to check if it the recycyle count 
			//if so then cause the project reader to refresh it's work nodes 
			bool ws_refresh_required = false;
			foreach (AttributeValuePair avp in attrs)
			{
				bool RequestCountFlag = (avp.Attribute == "recycle_request_count");
				bool RequestPendingFlag = (avp.Attribute == "recycle_request_pending");
				bool RequestStagesFlag = (avp.Attribute == "recycle_stages_changed");
				if ((RequestCountFlag)|(RequestPendingFlag)|(RequestStagesFlag))
				{
					ws_refresh_required = true;
				}
			}
			if (ws_refresh_required)
			{
				pr.AddAdditionalStagestoList();
				AddAdditionalStages();
			}
			Changed = true;
			//this.Refresh();//Old direct refesh on data change
		}

		private void stage_AttributesChanged(Node sender, ArrayList attrs)
		{
			Changed = true;
			//this.Refresh();//Old direct refesh on data change
		}

		/// <summary>
		/// Helper method for drawing the stage information
		/// </summary>
		/// <param name="g"></param>
		/// <param name="status_back"></param>
		/// <param name="drawdisplaytext"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="w"></param>
		/// <param name="h"></param>
		/// <param name="allocated"></param>
		/// <param name="requested"></param>
		/// <param name="days"></param>
		/// <param name="recycle_count"></param>
		protected void paintStageDualBox(System.Drawing.Graphics g, Image status_back, Brush textColorBrush, 
			bool drawdisplaytext,	bool drawdisplay_day_text, int x, int y, int w, int h,	
			int allocated, int requested, int days, int recycle_count)
		{
			SizeF textsize = new SizeF(0,0);

			g.DrawImage(status_back,x,y,w,h);

			string staff = CONVERT.ToStr(allocated) + "/" + CONVERT.ToStr(requested);
			textsize = g.MeasureString(staff,MyDefaultSkinFontBold8);
			int text_halfwidth = ((int)textsize.Width) / 2;
			int box_halfwidth = w / 2;

			if (drawdisplaytext)
			{
				g.DrawString(staff,MyDefaultSkinFontBold8,textColorBrush,x+box_halfwidth-text_halfwidth,8);
				if (drawdisplay_day_text)
				{
					g.DrawString(CONVERT.ToStr(days), MyDefaultSkinFontBold8, textColorBrush, x + 25 - 11, 37 + 4 - 9);
				}
			}
			if (recycle_count>0)
			{
				for (int step=0; step<recycle_count; step++)
				{
					g.FillRectangle(Brushes.Black,x+40+-1*(step*7),1,5,5);
				}
			}
		}

		/// <summary>
		/// Helper method for determining which stage background to use
		/// </summary>
		/// <param name="display"></param>
		/// <param name="paused"></param>
		/// <param name="inhand"></param>
		/// <param name="allocated"></param>
		/// <param name="requested"></param>
		/// <param name="status_image"></param>
		/// <param name="drawdisplaytext"></param>
		protected void getTaskStageImage(bool display, bool paused, bool inhand, int allocated, int requested,
			out Image status_image, out bool drawdisplaytext, out bool draw_days_displaytext,  out Brush textBrush)
		{
			textBrush = Brushes.White;

			status_image = this.stageBoxBack_empty;
			drawdisplaytext = false;
			draw_days_displaytext = false;

			if (display)
			{
				if (inhand)
				{
					if (paused)
					{
						status_image = this.stageBoxBack_redpaused;
						drawdisplaytext = true;
						draw_days_displaytext = false;
						textBrush = Brushes.White;
					}
					else
					{
						if (allocated==0)
						{
							status_image = this.stageBoxBack_redrun;
							drawdisplaytext = true;
							draw_days_displaytext = true;
							textBrush = Brushes.White;
						}
						else
						{
							if (allocated<requested)
							{
								status_image = this.stageBoxBack_amber;
								drawdisplaytext = true;
								draw_days_displaytext = true;
								textBrush = Brushes.Black;
							}
							else
							{
								//status_image = this.stageBoxBack_yellow;
								status_image = this.stageBoxBack_blue;
								drawdisplaytext = true;
								draw_days_displaytext = true;
								textBrush = Brushes.White;
							}
						}
					}
				}
				else
				{
					status_image = this.stageBoxBack_green;
					textBrush = Brushes.White;
				}
			}
			else
			{
				status_image = this.stageBoxBack_empty;
				textBrush = Brushes.White;
			}		
		}
		
		protected void splitErrorMsg(string errmsg, out string err1, out string err2)
		{
			err1 = "";
			err2 = "";

			string[] parts = errmsg.Split(' ');
			if (parts.Length==2)
			{
				err1 = parts[0];
				err2 = parts[1];
			}
			else
			{
				err1 = errmsg;
				err2 = "";
			}
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			bool display = false;
			bool inhand = false;
			int allocated = 0;
			int requested = 0;
			int days = 0;
			Image status_image = null;
			bool drawdisplaytext = false;
			bool drawdisplay_day_text = false;

			//Setup the Quality and Rendering Options 
			e.Graphics.SmoothingMode = SmoothingMode.None;
			e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
			e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

			//=======================================================================
			//==If we have a background image provided (otherwise paint a white back)
			//=======================================================================
			if (mainback != null)
			{
				e.Graphics.DrawImage(mainback,0,0,1010,52);
			}
			else
			{
				e.Graphics.FillRectangle(Brushes.White,0,0,this.Width, this.Height);
			}

			//=======================================================================
			//==The Project and Product are always green)
			//=======================================================================
			//e.Graphics.DrawImage(projectBoxBack_green,0,0,61,54);
			//e.Graphics.DrawImage(productBoxBack_green,86,0,92,54);
			
			bool stage_paused= false;
			Brush textColorBrush = Brushes.White;

			//=======================================================================
			//==Drawing the stages (if needed)
			//==  we only show the recycle box on those stages which are recycled
			//=======================================================================
			int recycle_count = pr.getRecycleProcessedCount();
			pr.shouldDisplayStateForTaskStage(emProjectOperationalState.PROJECT_STATE_A, 
				out display, out stage_paused, out inhand, out allocated, out requested, out days);
			getTaskStageImage(display, stage_paused, inhand, allocated, requested, out status_image, out drawdisplaytext, out drawdisplay_day_text, out textColorBrush);
			paintStageDualBox(e.Graphics, status_image, textColorBrush, drawdisplaytext, drawdisplay_day_text, 196 + 26-8, 0 + 4, 47, 44, allocated, requested, days, 0);

			pr.shouldDisplayStateForTaskStage(emProjectOperationalState.PROJECT_STATE_B,
				out display, out stage_paused, out inhand, out allocated, out requested, out days);
			getTaskStageImage(display, stage_paused, inhand, allocated, requested, out status_image, out drawdisplaytext, out drawdisplay_day_text, out textColorBrush);
			paintStageDualBox(e.Graphics, status_image, textColorBrush, drawdisplaytext, drawdisplay_day_text, 246 + 25 - 8, 0 + 4, 47, 44, allocated, requested, days, 0);

			pr.shouldDisplayStateForTaskStage(emProjectOperationalState.PROJECT_STATE_C,
				out display, out stage_paused, out inhand, out allocated, out requested, out days);
			getTaskStageImage(display, stage_paused, inhand, allocated, requested, out status_image, out drawdisplaytext, out drawdisplay_day_text, out textColorBrush);
			paintStageDualBox(e.Graphics, status_image, textColorBrush, drawdisplaytext, drawdisplay_day_text, 296 + 24 - 8, 0 + 4, 47, 44, allocated, requested, days, 0);

			pr.shouldDisplayStateForTaskStage(emProjectOperationalState.PROJECT_STATE_D,
				out display, out stage_paused, out inhand, out allocated, out requested, out days);
			getTaskStageImage(display, stage_paused, inhand, allocated, requested, out status_image, out drawdisplaytext, out drawdisplay_day_text, out textColorBrush);
			paintStageDualBox(e.Graphics, status_image, textColorBrush, drawdisplaytext, drawdisplay_day_text, 354 + 17 - 8, 0 + 4, 47, 44, allocated, requested, days, 0);

			pr.shouldDisplayStateForTaskStage(emProjectOperationalState.PROJECT_STATE_E,
				out display, out stage_paused, out inhand, out allocated, out requested, out days);
			getTaskStageImage(display, stage_paused, inhand, allocated, requested, out status_image, out drawdisplaytext, out drawdisplay_day_text, out textColorBrush);
			paintStageDualBox(e.Graphics, status_image, textColorBrush, drawdisplaytext, drawdisplay_day_text, 404 + 16 - 8, 0 + 4, 47, 44, allocated, requested, days, recycle_count);

			pr.shouldDisplayStateForTaskStage(emProjectOperationalState.PROJECT_STATE_F, 
				out display, out stage_paused, out inhand, out allocated, out requested, out days);
			getTaskStageImage(display, stage_paused, inhand, allocated, requested, out status_image, out drawdisplaytext, out drawdisplay_day_text, out textColorBrush);
			paintStageDualBox(e.Graphics, status_image, textColorBrush, drawdisplaytext, drawdisplay_day_text, 466 + 5 - 8, 0 + 4, 47, 44, allocated, requested, days, 0);

			pr.shouldDisplayStateForTaskStage(emProjectOperationalState.PROJECT_STATE_G, 
				out display, out stage_paused, out inhand, out allocated, out requested, out days);
			getTaskStageImage(display, stage_paused, inhand, allocated, requested, out status_image, out drawdisplaytext, out drawdisplay_day_text, out textColorBrush);
			paintStageDualBox(e.Graphics, status_image, textColorBrush, drawdisplaytext, drawdisplay_day_text, 516 + 4 - 8, 0 + 4, 47, 44, allocated, requested, days, 0);

			pr.shouldDisplayStateForTaskStage(emProjectOperationalState.PROJECT_STATE_H, 
				out display, out stage_paused, out inhand, out allocated, out requested, out days);
			getTaskStageImage(display, stage_paused, inhand, allocated, requested, out status_image, out drawdisplaytext, out drawdisplay_day_text, out textColorBrush);
			paintStageDualBox(e.Graphics, status_image, textColorBrush, drawdisplaytext, drawdisplay_day_text, 565 + 4 - 8, 0 + 4, 47, 44, allocated, requested, days, recycle_count);

			//displaying First Recycle
			if (pr.InState(emProjectOperationalState.PROJECT_STATE_I))
			{
				pr.shouldDisplayStateForTaskStage(emProjectOperationalState.PROJECT_STATE_I,
					out display, out stage_paused, out inhand, out allocated, out requested, out days);
				getTaskStageImage(display, stage_paused, inhand, allocated, requested, out status_image, out drawdisplaytext, out drawdisplay_day_text, out textColorBrush);
				paintStageDualBox(e.Graphics, status_image, textColorBrush, drawdisplaytext, drawdisplay_day_text, 404 + 16-8, 0 + 4, 47, 44, allocated, requested, days, recycle_count);
				
				//this shows the blank required for the pending state
				pr.shouldDisplayStateForTaskStage(emProjectOperationalState.PROJECT_STATE_J,
					out display, out stage_paused, out inhand, out allocated, out requested, out days);
				getTaskStageImage(display, stage_paused, inhand, allocated, requested, out status_image, out drawdisplaytext, out drawdisplay_day_text, out textColorBrush);
				paintStageDualBox(e.Graphics, status_image, textColorBrush, drawdisplaytext, drawdisplay_day_text, 565 + 4 - 8, 0 + 4, 47, 44, allocated, requested, days, recycle_count);
			}
			if (pr.InState(emProjectOperationalState.PROJECT_STATE_J))
			{
				pr.shouldDisplayStateForTaskStage(emProjectOperationalState.PROJECT_STATE_J,
					out display, out stage_paused, out inhand, out allocated, out requested, out days);
				getTaskStageImage(display, stage_paused, inhand, allocated, requested, out status_image, out drawdisplaytext, out drawdisplay_day_text, out textColorBrush);
				paintStageDualBox(e.Graphics, status_image, textColorBrush, drawdisplaytext, drawdisplay_day_text, 565 + 4 - 8, 0 + 4, 47, 44, allocated, requested, days, recycle_count);
			}

			//displaying Second Recycle
			if (pr.InState(emProjectOperationalState.PROJECT_STATE_K))
			{
				pr.shouldDisplayStateForTaskStage(emProjectOperationalState.PROJECT_STATE_K,
					out display, out stage_paused, out inhand, out allocated, out requested, out days);
				getTaskStageImage(display, stage_paused, inhand, allocated, requested, out status_image, out drawdisplaytext, out drawdisplay_day_text, out textColorBrush);
				paintStageDualBox(e.Graphics, status_image, textColorBrush, drawdisplaytext, drawdisplay_day_text, 404 + 16 - 8, 0 + 4, 47, 44, allocated, requested, days, recycle_count);

				//this shows the blank required for the pending state
				pr.shouldDisplayStateForTaskStage(emProjectOperationalState.PROJECT_STATE_L,
					out display, out stage_paused, out inhand, out allocated, out requested, out days);
				getTaskStageImage(display, stage_paused, inhand, allocated, requested, out status_image, out drawdisplaytext, out drawdisplay_day_text, out textColorBrush);
				paintStageDualBox(e.Graphics, status_image, textColorBrush, drawdisplaytext, drawdisplay_day_text, 565 + 4 - 8, 0 + 4, 47, 44, allocated, requested, days, recycle_count);
			}
			if (pr.InState(emProjectOperationalState.PROJECT_STATE_L))
			{
				pr.shouldDisplayStateForTaskStage(emProjectOperationalState.PROJECT_STATE_L,
					out display, out stage_paused, out inhand, out allocated, out requested, out days);
				getTaskStageImage(display, stage_paused, inhand, allocated, requested, out status_image, out drawdisplaytext, out drawdisplay_day_text, out textColorBrush);
				paintStageDualBox(e.Graphics, status_image, textColorBrush, drawdisplaytext, drawdisplay_day_text, 565 + 4 - 8, 0 + 4, 47, 44, allocated, requested, days, recycle_count);
			}

			//displaying Third Recycle
			if (pr.InState(emProjectOperationalState.PROJECT_STATE_M))
			{
				pr.shouldDisplayStateForTaskStage(emProjectOperationalState.PROJECT_STATE_M,
					out display, out stage_paused, out inhand, out allocated, out requested, out days);
				getTaskStageImage(display, stage_paused, inhand, allocated, requested, out status_image, out drawdisplaytext, out drawdisplay_day_text, out textColorBrush);
				paintStageDualBox(e.Graphics, status_image, textColorBrush, drawdisplaytext, drawdisplay_day_text, 404 + 16 - 8, 0 + 4, 47, 44, allocated, requested, days, recycle_count);

				//this shows the blank required for the pending state
				pr.shouldDisplayStateForTaskStage(emProjectOperationalState.PROJECT_STATE_N,
					out display, out stage_paused, out inhand, out allocated, out requested, out days);
				getTaskStageImage(display, stage_paused, inhand, allocated, requested, out status_image, out drawdisplaytext, out drawdisplay_day_text, out textColorBrush);
				paintStageDualBox(e.Graphics, status_image, textColorBrush, drawdisplaytext, drawdisplay_day_text, 565 + 4 - 8, 0 + 4, 47, 44, allocated, requested, days, recycle_count);
			}
			if (pr.InState(emProjectOperationalState.PROJECT_STATE_N))
			{
				pr.shouldDisplayStateForTaskStage(emProjectOperationalState.PROJECT_STATE_N,
					out display, out stage_paused, out inhand, out allocated, out requested, out days);
				getTaskStageImage(display, stage_paused, inhand, allocated, requested, out status_image, out drawdisplaytext, out drawdisplay_day_text, out textColorBrush);
				paintStageDualBox(e.Graphics, status_image, textColorBrush, drawdisplaytext, drawdisplay_day_text, 565 + 4 - 8, 0 + 4, 47, 44, allocated, requested, days, recycle_count);
			}

			//=======================================================================
			//==Drawing the Handover
			//=======================================================================
			bool handover_display_done = false;

			if ((pr.isCurrentStateOrLater(emProjectOperationalState.PROJECT_STATE_IN_HANDOVER))&&
				(pr.isCurrentStateRecycle()==false))
			{
				e.Graphics.DrawImage(this.handoverBack_green,622,0+2,78,46);
				//draw the handover string
				int hvalue = pr.getDisplayedHandoverEffect();
				e.Graphics.DrawString(CONVERT.ToStr(hvalue)+"%",MyDefaultSkinFontBold10,Brushes.White,642,10+12-3);
				handover_display_done = true;
			}
			if (handover_display_done == false)
			{
				e.Graphics.DrawImage(this.handoverBack_white,622,0+2,78,46);
			}

			//=======================================================================
			//==Drawing the Install Box
			//=======================================================================
			bool install_display_done = false;
			if (pr.isCurrentState(emProjectOperationalState.PROJECT_STATE_COMPLETED))
			{
				e.Graphics.DrawImage(installBack_green,720-6,0+2,83,48);
				install_display_done = true;
			}
			if (pr.isCurrentState(emProjectOperationalState.PROJECT_STATE_INSTALLING))
			{
				e.Graphics.DrawImage(installBack_yellow,720-6,0+2,83,48);

				if (! hideITDetails)
				{
					string install_location = pr.getInstallLocation();
					e.Graphics.DrawString(install_location, MyDefaultSkinFontBold10, Brushes.Black, 720 + 12, 6 + 1 + 14);
				}

				install_display_done = true;
			}
			if (pr.isCurrentState(emProjectOperationalState.PROJECT_STATE_INSTALLED_FAIL))
			{
				e.Graphics.DrawImage(installBack_red,720-6,0+2,83,48);
				//draw the failure reason text 
				string install_fail_reason = pr.getInstallError();
				string install_fail_reason_upper = "";
				string install_fail_reason_lower = ""; 
				splitErrorMsg(install_fail_reason, out install_fail_reason_upper, out install_fail_reason_lower);

				e.Graphics.DrawString(install_fail_reason_upper,MyDefaultSkinFontBold10,Brushes.White,720,15-6);
				e.Graphics.DrawString(install_fail_reason_lower,MyDefaultSkinFontBold10,Brushes.White,720,35-6);
				install_display_done = true;
			}

			Brush tmpTextBrush_Location = Brushes.Black;
			Brush tmpTextBrush_Day = Brushes.Black; 
			if (install_display_done==false)
			{
				string install_location = pr.getInstallLocation();
				int install_day = pr.getInstallDay();
				bool install_day_time_failure = pr.getInstallDayTimeFailure();

				if (install_day_time_failure)
				{
					e.Graphics.DrawImage(installBack_red, 720 - 4, 0 + 2, 83, 48);
					tmpTextBrush_Location = Brushes.White;
					//Draw the Install location and the Install Day
					e.Graphics.DrawString("Not Ready", MyDefaultSkinFontBold10, tmpTextBrush_Location, 720, 9 + 9);
				}
				else
				{
					int use_image_width = 86;
					int text_offset = 720 + 10;
					//Display the Recycle if we need to (this will affect the positioning of the install Day
					if (pr.getRecycleRequestPendingStatus())
					{
						e.Graphics.DrawImage(recycle_white, 720 + 53, 11, 26, 32);
						use_image_width = 52;
						text_offset = 720 - 10;
					}

					//Check whether we will be ready by the prediucted day
					bool predict_not_ready = pr.getInstallDayPredictedNotReady();

					if (predict_not_ready)
					{
						//Display the failure back
						e.Graphics.DrawImage(installBack_predict_fail, 710 + 2, 0 + 2, use_image_width, 48);
						tmpTextBrush_Location = Brushes.Black;
						tmpTextBrush_Day = Brushes.White; 
					}
					else
					{
						//Display the Good back
						e.Graphics.DrawImage(installBack_white, 710 + 2, 0 + 2, use_image_width, 48);
						tmpTextBrush_Location = Brushes.Black;
						tmpTextBrush_Day = Brushes.Black; 
					}
					//e.Graphics.DrawImage(installBack_white, 710 - 4, 0 + 2, 83, 48);
					//Draw the Install location and the Install Day (if we have a valid install day)
					if (install_day > 0)
					{
						if (hideITDetails)
						{
							e.Graphics.DrawString("Day " + (CONVERT.ToStr(install_day)), MyDefaultSkinFontBold10, tmpTextBrush_Day, text_offset, 18);
						}
						else
						{
							e.Graphics.DrawString("Day " + (CONVERT.ToStr(install_day)), MyDefaultSkinFontBold10, tmpTextBrush_Day, text_offset, 29);
							if (install_location != "")
							{
								e.Graphics.DrawString(install_location, MyDefaultSkinFontBold10, tmpTextBrush_Location, text_offset, 7);
							}
						}
					}
				}
			}

			//=======================================================================
			//==Drawing the Last Boxes (Money, Scope and Go Live Day) 
			//=======================================================================
			if (pr.isCurrentState(emProjectOperationalState.PROJECT_STATE_COMPLETED))
			{
				e.Graphics.DrawImage(moneyBack_green,814-5+3,0+4,70,44);
				e.Graphics.DrawImage(scopeBack_green,884+12+3+11-20-5,0+5-1,45+5,44);
				e.Graphics.DrawImage(goliveBack_green,956-10+11-20,0+5-1,49+20,44);
			}
			else
			{
				//show red moneyBack if out of money 
				if (pr.GetGoodMoneyFlag())
				{
					e.Graphics.DrawImage(moneyBack_white,814-5+3,0+4,70,44);
				}
				else
				{
					e.Graphics.DrawImage(moneyBack_red,814-5+3,0+4,70,44);
				}
				e.Graphics.DrawImage(scopeBack_white,884+12+3+11-20-5,0+5-1,45+5,44);
				e.Graphics.DrawImage(goliveBack_white,956-10+11-20,0+5-1,49+20,44);
			}

			//=======================================================================
			//==Drawing the text for the Project details and Go Live
			//=======================================================================
			if (project_main_node != null)
			{
				string project_id_display = project_main_node.GetAttribute("project_id");
				string product_id_display = project_main_node.GetAttribute("product_id");
				int platform_id = project_main_node.GetIntAttribute("platform_id",0);
				string platform_display = DataLookup.ProjectLookup.TheInstance.TranslatePlatformToStr(platform_id);
				int go_live_day = pr.getProjectDisplayGoLiveDay();
					
				//	project_main_node.GetIntAttribute("project_display_golive_day",0);

				e.Graphics.DrawString(project_id_display,MyDefaultSkinFontBold10,Brushes.White,10+12,20);
				e.Graphics.DrawString(product_id_display,MyDefaultSkinFontBold10,Brushes.White,90-8,20);
				e.Graphics.DrawString(platform_display,MyDefaultSkinFontBold10,Brushes.White,140+15,20);

				if (pr.isCurrentState(emProjectOperationalState.PROJECT_STATE_COMPLETED))
				{
					e.Graphics.DrawString(CONVERT.ToStr(go_live_day),MyDefaultSkinFontBold10,Brushes.White,952+15-8,20);
				}
				else
				{
					e.Graphics.DrawString(CONVERT.ToStr(go_live_day),MyDefaultSkinFontBold10,Brushes.Black,952+15-8,20);
				}

				if (ShowInternalGoLiveDayForDebug)
				{
					int pgld = project_main_node.GetIntAttribute("project_golive_day", 0);
					e.Graphics.DrawString(CONVERT.ToStr(pgld), MyDefaultSkinFontBold10, Brushes.Violet, 952 + 15 - 8, 30);
				}

			}

			//Hiding the Pre Request displays 
//			string status_request = pa.getStatusRequest();
//			if (status_request != "")
//			{
//				if (pr.isStatusRequestPreCancel())
//				{
//					e.Graphics.DrawString("Cancelling",MyDefaultSkinFontBold8,Brushes.Yellow,1,10+22);
//				}
//				if (pr.isStatusRequestPrePause())
//				{
//					e.Graphics.DrawString("Pausing",MyDefaultSkinFontBold8,Brushes.Yellow,1,10+22);
//				}
//				if (pr.isStatusRequestPreResume())
//				{
//					e.Graphics.DrawString("Resuming",MyDefaultSkinFontBold8,Brushes.Yellow,1,10+22);
//				}
//			}

			//=======================================================================
			//==Drawing the text for the Project Scope and Money
			//=======================================================================
			if (project_subnode_prj != null)
			{
				int project_scope = pr.getProjectScope();
				Brush tmpBrush = Brushes.Black;
				if (pr.isCurrentState(emProjectOperationalState.PROJECT_STATE_COMPLETED))
				{
					tmpBrush = Brushes.White;
				}

				SizeF scopeSize = e.Graphics.MeasureString(CONVERT.ToStr(project_scope) + "%", MyDefaultSkinFontBold10);
				e.Graphics.DrawString(CONVERT.ToStr(project_scope)+"%",MyDefaultSkinFontBold10,tmpBrush,930 - (int) scopeSize.Width,20-2);
			}

			if (project_subnode_fin != null)
			{
				Brush tmpBrush = Brushes.Black;
				if (pr.isCurrentState(emProjectOperationalState.PROJECT_STATE_COMPLETED))
				{
					tmpBrush = Brushes.White;
				}
				else if (!pr.GetGoodMoneyFlag())
				{
					tmpBrush = Brushes.White;
				}

				string tempstr;
				//displaying the budget 
				int budget_amount = project_subnode_fin.GetIntAttribute("budget_player",0);
				tempstr = budget_amount.ToString( "N", nfi );
				tempstr = tempstr.Replace(".00","");
				string budget_str = "" + tempstr;

				//displaying the spend
				int spend_amount = project_subnode_fin.GetIntAttribute("spend",0);
				tempstr = spend_amount.ToString( "N", nfi );
				tempstr = tempstr.Replace(".00","");
				string spend_str = "" + tempstr;

				SizeF budgetSize = e.Graphics.MeasureString(budget_str, MyDefaultSkinFontBold8);
				SizeF spendSize = e.Graphics.MeasureString(spend_str, MyDefaultSkinFontBold8);

				e.Graphics.DrawString(budget_str, MyDefaultSkinFontBold8, tmpBrush, 880 - (int) budgetSize.Width, 5 + 4);
				e.Graphics.DrawString(spend_str, MyDefaultSkinFontBold8, tmpBrush, 880 - (int) spendSize.Width, 35-3);
			}
		}

		/// <summary>
		/// If the data has been changed then trigger the timer for 1 second and then refresh the screen 
		/// This has the effect of reducing the flicker by bunling a lot of changes into 1 refresh
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void _timer_Tick(object sender, EventArgs e)
		{
			if(!changed)
			{
				_timer.Stop();
			}
			else
			{
				//RefreshDisplay();
				this.Invalidate();
			}
			changed = false;
		}

	}
}
