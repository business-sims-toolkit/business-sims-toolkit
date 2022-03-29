using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using LibCore;
using CoreUtils;
using CommonGUI;

namespace TransitionScreens
{
	/// <summary>
	/// Summary description for StatusDualBox.
	/// </summary>
	public enum TransitionStatus 
	{
		/// <summary> Stage Status </summary>
		WORKING,	
		/// <summary> Stage Status, this stage is still to do  </summary>
		TO_DO,		
		/// <summary> Stage Status, we are actuually working in this stage </summary>
		IN_STAGE,
		/// <summary> Display Status : Just showing the Title </summary>
		TITLE,
		/// <summary> Display Status : Handover Not Reached </summary>
		HANDOVER_TODO,		
		/// <summary> Display Status : Handover Completed </summary>
		HANDOVER_DONE,		
		/// <summary> Display Status : Displaying the Ready Day </summary>
		GOLIVE,				
		/// <summary> Display Status : Ready is NOW OR PASSED </summary>
		READY,
		/// <summary> Display Status : Ready is Not applicable </summary>
		NOTREADY,
		/// <summary> Display Status : Install not reached </summary>
		INSTALL_TODO,		
		/// <summary> Display Status : Install completed and Fail </summary>
		INSTALL_FAILED,	
		/// <summary> Display Status : Install completed and OK  </summary>
		INSTALL_DONE,
		/// <summary> Display Status of Money </summary>
		MONEY
	};
	/// <summary>
	/// Summary description for StatusDualBox.
	/// </summary>
	public class StatusDualBox : FlickerFreePanel
	{
		Font f;
		Label lblDisplayText;

		TransitionStatus status;
		/// <summary>
		/// Status of the project
		/// </summary>
		public  TransitionStatus Status 
		{
			get { return status; }
			set { status = value; }
		}

		int requested;
		/// <summary>
		/// Requested days
		/// </summary>
		public  int Requested
		{
			get { return requested; }
			set { requested = value; }
		}

		int actual;
		/// <summary>
		/// Days Given
		/// </summary>
		public  int Actual
		{
			get { return actual; }
			set { actual = value; }
		}

		int days;
		/// <summary>
		/// Days the project will take
		/// </summary>
		public  int Days
		{
			get { return days; }
			set { days = value; }
		}

		Boolean overtime;
		/// <summary>
		/// Using overtime
		/// </summary>
		public  bool Overtime
		{
			get { return overtime; }
			set { overtime = value; }
		}

		string reason;

		Image lgrey_button = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\Images\\transitionbuttons\\lightgrey_button.png");
		Image grey_button = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\Images\\transitionbuttons\\grey_button.png");
		Image red_button = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\Images\\transitionbuttons\\red_button.png");
		Image green_button = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\Images\\transitionbuttons\\green_button.png");
		Image lgreen_button = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\Images\\transitionbuttons\\lgreen_button.png");
		Image yellow_button = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\Images\\transitionbuttons\\yellow_button.png");

		Image man = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\Images\\transitionbuttons\\group.png");
		Image clock = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\Images\\transitionbuttons\\clock.png");
		Image clock_overtime = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\Images\\transitionbuttons\\clock_error.png");
		Image mpause = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\Images\\transitionbuttons\\clock_red.png");

		/// <summary> 
		/// Required designer variable.
		/// </summary>
		System.ComponentModel.Container components = null;

		Color inStageTextColour = SkinningDefs.TheInstance.GetColorDataGivenDefault("project_progress_panel_current_stage_text_colour", Color.Black);
		Color moneyTextColour = SkinningDefs.TheInstance.GetColorDataGivenDefault("project_progress_panel_money_text_colour", Color.Black);

		/// <summary>
		/// Displays the status of a project
		/// </summary>
		/// <param name="status"></param>
		public StatusDualBox(TransitionStatus status)
		{
			base_construction();
			Status = status;
			reason = string.Empty;
		}

		/// <summary>
		/// Displays the status of a project
		/// </summary>
		/// <param name="requested"></param>
		/// <param name="actual"></param>
		/// <param name="days"></param>
		/// <param name="overtime"></param>
		public StatusDualBox(int requested, int actual,int days, bool overtime)
		{
			base_construction();

			Status = TransitionStatus.TO_DO;
			Actual = actual;
			Requested = requested;
			Days = days;
			Overtime = overtime;
			reason = string.Empty;
		}

		/// <summary>
		/// Displays the status of a project
		/// </summary>
		/// <param name="status"></param>
		/// <param name="requested"></param>
		/// <param name="actual"></param>
		/// <param name="days"></param>
		/// <param name="overtime"></param>
		public StatusDualBox(TransitionStatus status, int requested, int actual,int days, bool overtime)
		{
			base_construction();
			Status = status;
			Actual = actual;
			Requested = requested;
			Days = days;
			Overtime = overtime;
		}

		void base_construction()
		{
			f = SkinningDefs.TheInstance.GetFont(9f, FontStyle.Bold);
			InitializeComponent();
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

		public override Color ForeColor
		{
			set
			{
				lblDisplayText.ForeColor = value;
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
			// StatusDualBox
			// 
			lblDisplayText = new Label();
			lblDisplayText.Location = new Point(0,0);
			lblDisplayText.Size = new Size(55,48);
			lblDisplayText.TextAlign = ContentAlignment.MiddleCenter;
			lblDisplayText.BackColor = Color.Transparent;
			lblDisplayText.Font = this.f;

			this.SuspendLayout();
			this.Controls.Add(lblDisplayText);
			this.ResumeLayout(false);

			this.Size = new Size(52,48);

			this.Name = "StatusDualBox";
			this.Paint += new PaintEventHandler(this.StatusDualBox_Paint);
		}
		#endregion		

		/// <summary>
		/// 
		/// </summary>
		/// <param name="newFont"></param>
		public void setDisplayFont (Font newFont)
		{
			lblDisplayText.Font = newFont;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="newDisplayText"></param>
		public void setDisplayText (string newDisplayText)
		{
			newDisplayText = TextTranslator.TheInstance.Translate(newDisplayText);
			lblDisplayText.Text = newDisplayText;
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public string getDisplayText()
		{
			return lblDisplayText.Text;
		}

		void StatusDualBox_Paint(object sender, PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			g.SmoothingMode = SmoothingMode.HighQuality;

			switch (Status)
			{
				case TransitionStatus.TO_DO:
                    g.DrawImage(grey_button, new Rectangle (0, 0, Width, Height), new Rectangle (0, 0, grey_button.Width, grey_button.Height), GraphicsUnit.Pixel);
                    break;
				case TransitionStatus.IN_STAGE:
                    g.DrawImage(yellow_button, new Rectangle(0, 0, Width, Height), new Rectangle(0, 0, yellow_button.Width, yellow_button.Height), GraphicsUnit.Pixel);

					using (Brush brush = new SolidBrush(inStageTextColour))
					{
						g.DrawString(CONVERT.ToStr(Days), f, brush, 30, 28 - 10);
					}

					g.DrawImage( ((Overtime) ? clock_overtime : clock) ,5,28-10,16,16);
					break;
				case TransitionStatus.WORKING:
                    g.DrawImage(green_button, new Rectangle(0, 0, Width, Height), new Rectangle(0, 0, green_button.Width, green_button.Height), GraphicsUnit.Pixel);
					break;
				case TransitionStatus.HANDOVER_TODO:
                    g.DrawImage(lgrey_button, new Rectangle(0, 0, Width, Height), new Rectangle(0, 0, lgrey_button.Width, lgrey_button.Height), GraphicsUnit.Pixel);
					break;
				case TransitionStatus.HANDOVER_DONE:
                    g.DrawImage(lgreen_button, new Rectangle(0, 0, Width, Height), new Rectangle(0, 0, lgreen_button.Width, lgreen_button.Height), GraphicsUnit.Pixel);
					break;
				case TransitionStatus.GOLIVE:
                    g.DrawImage(lgrey_button, new Rectangle(0, 0, Width, Height), new Rectangle(0, 0, lgrey_button.Width, lgrey_button.Height), GraphicsUnit.Pixel);
					break;
				case TransitionStatus.NOTREADY:
                    g.DrawImage(lgrey_button, new Rectangle(0, 0, Width, Height), new Rectangle(0, 0, lgrey_button.Width, lgrey_button.Height), GraphicsUnit.Pixel);
					break;
				case TransitionStatus.READY:
                    g.DrawImage(lgreen_button, new Rectangle(0, 0, Width, Height), new Rectangle(0, 0, lgreen_button.Width, lgreen_button.Height), GraphicsUnit.Pixel);
					break;
				case TransitionStatus.TITLE:
                    g.DrawImage(lgreen_button, new Rectangle(0, 0, Width, Height), new Rectangle(0, 0, lgreen_button.Width, lgreen_button.Height), GraphicsUnit.Pixel);
					break;
				case TransitionStatus.MONEY:
                    g.DrawImage(lgreen_button, new Rectangle(0, 0, Width, Height), new Rectangle(0, 0, lgreen_button.Width, lgreen_button.Height), GraphicsUnit.Pixel);

					using (Brush brush = new SolidBrush (moneyTextColour))
					{
						g.DrawString( CONVERT.ToStr((Actual/1000)),f,brush,3+6,0+17);
					}
					break;
				case TransitionStatus.INSTALL_TODO:
                    g.DrawImage(lgrey_button, new Rectangle(0, 0, Width, Height), new Rectangle(0, 0, lgrey_button.Width, lgrey_button.Height), GraphicsUnit.Pixel);
					break;
				case TransitionStatus.INSTALL_DONE:
                    g.DrawImage(lgreen_button, new Rectangle(0, 0, Width, Height), new Rectangle(0, 0, lgreen_button.Width, lgreen_button.Height), GraphicsUnit.Pixel);
					break;
				case TransitionStatus.INSTALL_FAILED:
                    g.DrawImage(red_button, new Rectangle(0, 0, Width, Height), new Rectangle(0, 0, red_button.Width, red_button.Height), GraphicsUnit.Pixel);
					break;
			}
		}
	}
}
