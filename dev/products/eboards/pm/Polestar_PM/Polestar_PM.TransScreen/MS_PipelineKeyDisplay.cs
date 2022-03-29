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
	public class MS_PipelineKeyDisplay : System.Windows.Forms.UserControl
	{
		//Game Data 
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
		Point centrepoint = new Point(0,0);
		
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

		private System.ComponentModel.Container components = null;

		#region Constructor and Dispose Methods

		public MS_PipelineKeyDisplay(bool TrainingGame)
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			//Setup the paint optmisations
			this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			this.SetStyle(ControlStyles.UserPaint, true);
			this.SetStyle(ControlStyles.DoubleBuffer,true);

			//Connect up to the Network
			//Connect Up the Current Day and extract current value  
			RebuildBrushes(); 
			BuildIconImages();
			//BuildOperationPositioning();
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
		}
		#endregion

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
				}
					break;
			}
			return img; 
		}

		#endregion Utils

		protected override void OnPaint(PaintEventArgs e)
		{
			string option = string.Empty;
			Color opscolor = Color.White;
			SizeF textsize = new SizeF(0,0);

			e.Graphics.SmoothingMode = SmoothingMode.None;
			if (DrawBackground)
			{
				e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(241,241,241)), 0,0, this.Width, this.Height);
			}			

			//Setup the Quality and Rendering Options 
			e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
			e.Graphics.TextRenderingHint = TextRenderingHint.AntiAlias;

			StringFormat format2 = new StringFormat();
			format2.Alignment = StringAlignment.Center;
			format2.LineAlignment = StringAlignment.Center;

			//Draw the Title 			
			e.Graphics.DrawString("PIPELINE KEY",BoldFont11,Brushes.DimGray,0,0); 

			//Draw the Column Titles
			e.Graphics.DrawString("Projects",BoldFont8,Brushes.DimGray,10,20); 
			e.Graphics.DrawString("Events",BoldFont8,Brushes.DimGray,180,20); 

			//Draw the Project Items
			int ProjectStepY = 25;
			e.Graphics.FillRectangle(PrjDesignBrush, 10,40+ProjectStepY*0, 20,20);
			e.Graphics.DrawString("Design Phase",BoldFont8,Brushes.DimGray,40,40+ProjectStepY*0); 
			e.Graphics.FillRectangle(PrjBuildBrush, 10,40+ProjectStepY*1, 20,20);
			e.Graphics.DrawString("Build Phase",BoldFont8,Brushes.DimGray,40,40+ProjectStepY*1); 
			e.Graphics.FillRectangle(PrjTestBrush, 10,40+ProjectStepY*2, 20,20);
			e.Graphics.DrawString("Test Phase",BoldFont8,Brushes.DimGray,40,40+ProjectStepY*2); 
			e.Graphics.FillRectangle(PrjHandoverBrush, 10,40+ProjectStepY*3, 20,20);
			e.Graphics.DrawString("Handover Phase",BoldFont8,Brushes.DimGray,40,40+ProjectStepY*3); 
			e.Graphics.FillRectangle(PrjReadyBrush, 10,40+ProjectStepY*4, 20,20);
			e.Graphics.DrawString("Ready Phase",BoldFont8,Brushes.DimGray,40,40+ProjectStepY*4); 
			e.Graphics.FillRectangle(OpsCompletedOKBrush, 10,40+ProjectStepY*5, 20,20);
			e.Graphics.DrawString("Install OK ",BoldFont8,Brushes.DimGray,40,40+ProjectStepY*5); 
			e.Graphics.FillRectangle(OpsCompletedFailBrush, 10,40+ProjectStepY*6, 20,20);
			e.Graphics.DrawString("Install Fail",BoldFont8,Brushes.DimGray,40,40+ProjectStepY*6); 

			//Draw the Project Items
			//int EventStepY = 20;
			//int IconSize = 15;
			int EventStepY = 31;
			int IconSize = 30;
			int EventsOffsetX = 180;
			int EventsOffsetY = 40;

			e.Graphics.DrawImage(img_audit,EventsOffsetX,EventsOffsetY+EventStepY*0,IconSize,IconSize);
			e.Graphics.DrawString("Audit Day",BoldFont8,Brushes.DimGray,EventsOffsetX+IconSize+5,EventsOffsetY+EventStepY*0); 
			//e.Graphics.DrawImage(img_press,EventsOffsetX,EventsOffsetY+EventStepY*1,IconSize,IconSize);
			//e.Graphics.DrawString("Press Day",BoldFont8,Brushes.DimGray,EventsOffsetX+IconSize+5,EventsOffsetY+EventStepY*1); 
			e.Graphics.DrawImage(img_freeze,EventsOffsetX,EventsOffsetY+EventStepY*1,IconSize,IconSize);
			e.Graphics.DrawString("Configuration Freeze",BoldFont8,Brushes.DimGray,EventsOffsetX+IconSize+5,EventsOffsetY+EventStepY*1); 
			//e.Graphics.DrawImage(img_trade,EventsOffsetX,EventsOffsetY+EventStepY*2,IconSize,IconSize);
			//e.Graphics.DrawString("Travel ",BoldFont8,Brushes.DimGray,EventsOffsetX+IconSize+5,EventsOffsetY+EventStepY*2); 

			e.Graphics.DrawImage(ug_memory_due,EventsOffsetX,EventsOffsetY+EventStepY*2,IconSize,IconSize);
			e.Graphics.DrawString("Upgrade Memory ",BoldFont8,Brushes.DimGray,EventsOffsetX+IconSize+5,EventsOffsetY+EventStepY*2); 
			e.Graphics.DrawImage(ug_storage_due,EventsOffsetX,EventsOffsetY+EventStepY*3,IconSize,IconSize);
			e.Graphics.DrawString("Upgrade Disk ",BoldFont8,Brushes.DimGray,EventsOffsetX+IconSize+5,EventsOffsetY+EventStepY*3); 
			e.Graphics.DrawImage(ug_hardware_due,EventsOffsetX,EventsOffsetY+EventStepY*4,IconSize,IconSize);
			e.Graphics.DrawString("Upgrade Hardware ",BoldFont8,Brushes.DimGray,EventsOffsetX+IconSize+5,EventsOffsetY+EventStepY*4); 
			e.Graphics.DrawImage(ug_app_due,EventsOffsetX,EventsOffsetY+EventStepY*5,IconSize,IconSize);
			e.Graphics.DrawString("Upgrade App ",BoldFont8,Brushes.DimGray,EventsOffsetX+IconSize+5,EventsOffsetY+EventStepY*5); 

		}

	}
	
}
