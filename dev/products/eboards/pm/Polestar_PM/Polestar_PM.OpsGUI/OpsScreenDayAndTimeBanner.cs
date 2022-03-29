using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Data;
using System.Windows.Forms;

using LibCore;
using Network;
using CoreUtils;
using CommonGUI;
using BusinessServiceRules;

namespace Polestar_PM.OpsGUI
{
	/// <summary>
	/// Summary description for RaceRaceScreenBanner.
	/// </summary>
	public class OpsScreenDayAndTimeBanner : FlickerFreePanel
	{
		protected System.Windows.Forms.Label lblGameRound;
		protected System.Windows.Forms.Label lblGameTime;
		protected Label lblIncidentTime;
		protected System.Windows.Forms.Label lblPrePlayTime;
		protected System.Windows.Forms.Label lblDisplayDay;
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		protected System.ComponentModel.Container components = null;

		protected NodeTree _NetworkModel;
		protected Node _CurrentTimeNode = null;
		protected Node _CurrentModelTimeNode = null;
		protected Node _CurrentPrePlayTime;
		//private Node _ThreatStatusNode = null;
		protected Node _RecoveryProcessNode = null;
		protected string RoundName = "R"; //Round
		protected int ClockOffset = 0;

		//skin stuff
		protected Font MyDefaultSkinFontBold10 = null;
		protected Font MyDefaultSkinFontBold12 = null;
		protected Font MyDefaultSkinFontBold14 = null;
		protected Font MyDefaultSkinFontBold24 = null;
		protected Font BannerBoldFont = null;
		protected Font TimeBoldFont = null;

		protected int hour = 0;
		protected bool showTitle = true;
		protected bool showMTPD = false;
		protected bool showRoundIndicator = false;
		protected Brush tmpTitleRedBrush = new SolidBrush(Color.FromArgb(153,0,0));		

		public OpsScreenDayAndTimeBanner(NodeTree nt, bool showDay)
		{
			// This call is required by the Windows.Forms Form Designer.
			RoundName = SkinningDefs.TheInstance.GetData("ops_banner");
			this.BackColor = Color.FromArgb(188,188,188);

			string fontname =  SkinningDefs.TheInstance.GetData("fontname");
			MyDefaultSkinFontBold10 = ConstantSizeFont.NewFont(fontname,10,FontStyle.Bold);
			MyDefaultSkinFontBold12 = ConstantSizeFont.NewFont(fontname,12,FontStyle.Bold);
			MyDefaultSkinFontBold14 = ConstantSizeFont.NewFont(fontname,14,FontStyle.Bold);
			MyDefaultSkinFontBold24 = ConstantSizeFont.NewFont(fontname,24,FontStyle.Bold);

			InitializeComponent();

			Color BannerTextColor = Color.White;
			lblGameRound.ForeColor = BannerTextColor;
			lblGameTime.ForeColor = BannerTextColor;
			lblPrePlayTime.ForeColor = Color.FromArgb(200, 200, 0);
			lblDisplayDay.ForeColor = BannerTextColor;

//			lblGameRound.BackColor = Color.Thistle;
//			lblGameTime.BackColor = Color.LightGreen;
//			lblPrePlayTime.BackColor = Color.Turquoise;
//			lblIncidentTime.BackColor = Color.LightBlue; 
//			lblDisplayDay.BackColor = Color.MediumOrchid;
			
			lblGameRound.BackColor = Color.Black;
			lblGameTime.BackColor = Color.Black;
			lblPrePlayTime.BackColor = Color.Black;
			lblIncidentTime.BackColor = Color.Black;
			lblDisplayDay.BackColor = Color.Black;

			BannerBoldFont = MyDefaultSkinFontBold12;
			TimeBoldFont = ConstantSizeFont.NewFont(fontname, SkinningDefs.TheInstance.GetIntData("timer_font_size", 18), FontStyle.Bold);

			lblGameRound.Font = BannerBoldFont;
			lblGameTime.Font = TimeBoldFont;
			lblPrePlayTime.Font = TimeBoldFont; 
			lblIncidentTime.Font = TimeBoldFont;
			lblDisplayDay.Font = TimeBoldFont;

			//Connect up the Required Node
			_NetworkModel = nt;
			_CurrentTimeNode = _NetworkModel.GetNamedNode("CurrentTime");
//			_CurrentTimeNode.AttributesChanged +=new Network.Node.AttributesChangedEventHandler(_CurrentTimeNode_AttributesChanged);
			_CurrentModelTimeNode = _NetworkModel.GetNamedNode("CurrentModelTime");
			_CurrentModelTimeNode.AttributesChanged +=new Network.Node.AttributesChangedEventHandler(_CurrentModelTimeNode_AttributesChanged);

			_CurrentPrePlayTime = _NetworkModel.GetNamedNode("preplay_status");
			_CurrentPrePlayTime.AttributesChanged +=new Network.Node.AttributesChangedEventHandler(_CurrentPrePlayTime_AttributesChanged);

			_RecoveryProcessNode = _NetworkModel.GetNamedNode("RecoveryProcess");
			_RecoveryProcessNode.AttributesChanged +=new Network.Node.AttributesChangedEventHandler(_RecoveryProcessNode_AttributesChanged);

			lblGameRound.Visible = false;
			if (showRoundIndicator)
			{
				lblGameRound.Visible = true;
			}

			setLabel();
			handleSize();
			this.Resize += new EventHandler(RaceScreenBanner_Resize);
			//HandleThreatChange();
		}



		/// <summary>
		/// The Round of the current game
		/// </summary>
		public int Round
		{
			set
			{
				this.lblGameRound.Text = RoundName + " " + CONVERT.ToStr(value);
			}
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

				//if(_CurrentTimeNode != null)
				//{
				//	_CurrentTimeNode.AttributesChanged -= new Network.Node.AttributesChangedEventHandler(_CurrentTimeNode_AttributesChanged);
				//	_CurrentTimeNode = null;
				//}
				
				if(_CurrentModelTimeNode != null)
				{
					_CurrentModelTimeNode.AttributesChanged -= new Network.Node.AttributesChangedEventHandler(_CurrentModelTimeNode_AttributesChanged);
					_CurrentModelTimeNode = null;
				}

				if(_CurrentPrePlayTime != null)
				{
					_CurrentPrePlayTime.AttributesChanged -=new Network.Node.AttributesChangedEventHandler(_CurrentPrePlayTime_AttributesChanged);
					_CurrentPrePlayTime = null;
				}
				_NetworkModel = null;
			}

			if (_RecoveryProcessNode != null)
			{
				_RecoveryProcessNode.AttributesChanged -=new Network.Node.AttributesChangedEventHandler(_RecoveryProcessNode_AttributesChanged);
				_RecoveryProcessNode = null;
			}

			if (MyDefaultSkinFontBold10 != null)
			{
				MyDefaultSkinFontBold10.Dispose();
			}
			if (MyDefaultSkinFontBold12 != null)
			{
				MyDefaultSkinFontBold12.Dispose();
			}
			if (MyDefaultSkinFontBold14 != null)
			{
				MyDefaultSkinFontBold14.Dispose();
			}
			if (MyDefaultSkinFontBold24 != null)
			{
				MyDefaultSkinFontBold24.Dispose();
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
			this.SuspendLayout();
			// 
			// lblGameRound
			// 
			//this.lblGameDay.Dock = System.Windows.Forms.DockStyle.Right;
			lblGameRound = new Label();
			lblGameRound.BackColor = Color.Black;
			lblGameRound.ForeColor = Color.White;
			lblGameRound.Location = new System.Drawing.Point(30, 2);
			lblGameRound.Font = CoreUtils.SkinningDefs.TheInstance.GetFont(8);
			lblGameRound.Name = "lblGameRound";
			lblGameRound.Size = new System.Drawing.Size(115, 25);
			lblGameRound.TabIndex = 0;
			lblGameRound.Text = "Round 1";
			lblGameRound.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			lblGameRound.Visible = false;

			lblIncidentTime = new Label ();
			lblIncidentTime.BackColor = Color.Transparent;
			lblIncidentTime.ForeColor = Color.Black;
			lblIncidentTime.Location = new Point (0, 0);
			lblIncidentTime.Font = lblGameRound.Font;
			lblIncidentTime.Size = new Size (Width, Height);
			lblIncidentTime.Text = "";
      // 
			// lblGameDate
			// 
			//this.lblGameDay.Dock = System.Windows.Forms.DockStyle.Right;
			this.lblGameTime = new System.Windows.Forms.Label();
			lblGameTime.Font = lblGameRound.Font;
			lblGameTime.ForeColor = Color.White;
			lblGameTime.BackColor = Color.Black;
			//lblGameTime.BackColor = Color.Red;
			lblGameTime.Location = new System.Drawing.Point(0, 30);
			lblGameTime.Name = "lblGameTime";
			lblGameTime.Size = new System.Drawing.Size(196, 25);
			lblGameTime.TabIndex = 0;
			lblGameTime.Text = "1";
			lblGameTime.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

			// 
			// lblDisplayDay
			// 
			this.lblDisplayDay = new System.Windows.Forms.Label();
			lblDisplayDay.Font = lblGameRound.Font;
			lblDisplayDay.ForeColor = Color.White;
			lblDisplayDay.BackColor = Color.Black;
			//lblGameTime.BackColor = Color.Red;
			lblDisplayDay.Location = new System.Drawing.Point(0, 0);
			lblDisplayDay.Name = "lblDisplayDay";
			lblDisplayDay.Size = new System.Drawing.Size(196, 25);
			lblDisplayDay.TabIndex = 0;
			lblDisplayDay.Text = "1";
			lblDisplayDay.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// PrePlayTime
			// 
			this.lblPrePlayTime = new System.Windows.Forms.Label();
			lblPrePlayTime.Font = lblGameRound.Font;
			lblPrePlayTime.ForeColor = Color.White;
			lblPrePlayTime.BackColor = Color.Black;
			lblPrePlayTime.Location = new System.Drawing.Point(426, 2);
			lblPrePlayTime.Name = "lblGameTime";
			lblPrePlayTime.Size = new System.Drawing.Size(96, 25);
			lblPrePlayTime.TabIndex = 0;
			lblPrePlayTime.Text = "1";
			lblPrePlayTime.Visible = false;
			lblPrePlayTime.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

			this.Controls.Add(this.lblGameRound);
			this.Controls.Add(this.lblGameTime);
			this.Controls.Add(lblDisplayDay);
			this.Controls.Add(this.lblPrePlayTime);
			this.Controls.Add(this.lblIncidentTime);
			this.DockPadding.All = 2;
			this.Name = "RaceScreenBanner";
			this.Size = new System.Drawing.Size(600, 32);
			this.ResumeLayout(false);
		}
		#endregion

		public void ChangeBannerTextForeColour(Color fc)
		{
			lblGameRound.ForeColor = fc; 
			lblGameTime.ForeColor = fc; 
			lblIncidentTime.ForeColor = fc;
			lblDisplayDay.ForeColor = fc; 
		}

		public void ChangeBannerPrePlayTextForeColour(Color ppfc)
		{
			lblPrePlayTime.ForeColor = ppfc;
		}

		public void ShowPrePlay(Boolean ShowPrePlay)
		{
			if (ShowPrePlay)
			{
				lblPrePlayTime.Visible = true;
				lblGameTime.Visible = false;
				lblDisplayDay.Visible = false;
			}
			else
			{
				lblPrePlayTime.Visible = false;
				lblGameTime.Visible = true;
				lblDisplayDay.Visible = true;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public void setLabel()
		{
			if (_CurrentTimeNode != null)
			{
				int time = _CurrentTimeNode.GetIntAttribute("seconds", 0);

				//this.lblGameTime.Text = ModelTimeManager.TimeToStringWithDay(time);
				this.lblGameTime.Text = ModelTimeManager.TimeToStringFlattenDay(time);
				this.lblDisplayDay.Text = ModelTimeManager.TimeToDay(time);

				Node initialIncidentNode = _NetworkModel.GetNamedNode("InitialIncident");

				if (showMTPD)
				{
					lblIncidentTime.Visible = true;
					lblIncidentTime.Text = "MTPD " + this._CurrentModelTimeNode.GetAttribute("time_left");
				}
				else
				{
					lblIncidentTime.Text = "";
					lblIncidentTime.Visible = false;
				}

//				lblIncidentTime.Text = "";
//				if (initialIncidentNode != null)
//				{
//					int initialIncidentTime = initialIncidentNode.GetIntAttribute("time", 0);
//
//					if ((initialIncidentTime >= 0) && (time >= initialIncidentTime))
//					{
//						lblIncidentTime.Text = "T + " + ModelTimeManager.DurationToString(time - initialIncidentTime);
//					}
//				}
			}
		}

		private void _CurrentDayNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			setLabel();
		}


		private void _CurrentTimeNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			ShowPrePlay(false);
			setLabel();
		}

		private void _CurrentModelTimeNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			setLabel();
		}

		public virtual void handleSize()
		{
			int dropToAllowforTitle = 20;
			if (this.showTitle==false)
			{
				dropToAllowforTitle = 0;
			}
			int status_width = this.Width - 4;
			int incident_width  = this.Width - 75;

			lblGameRound.Location = new Point(2,55+10+dropToAllowforTitle);
			lblGameRound.Size = new Size(73,20);

			lblDisplayDay.Location = new Point(2,0+dropToAllowforTitle);
			lblDisplayDay.Size = new Size(status_width,30);

			lblGameTime.Location = new Point(2,25+8+dropToAllowforTitle);
			lblGameTime.Size = new Size(status_width,30);
			
			lblPrePlayTime.Location = lblGameTime.Location;
			lblPrePlayTime.Size = lblGameTime.Size;
			
			lblIncidentTime.Location = new Point (30, 55+10+dropToAllowforTitle);
			lblIncidentTime.Size = new Size (incident_width+30, 30);
			
			this.Refresh();
		}

		private void RaceScreenBanner_Resize(object sender, EventArgs e)
		{
			handleSize();
		}

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


		private void _CurrentPrePlayTime_AttributesChanged(Node sender, ArrayList attrs)
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
							this.lblPrePlayTime.Text = BuildTimeString(time_left);
							ShowPrePlay(true);
						}
						else
						{
							ShowPrePlay(false);
						}
					}
				}
			}
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			//Setup the Quality and Rendering Options 
			e.Graphics.SmoothingMode = SmoothingMode.None;
			e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
			e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
			int start_x =0;

			e.Graphics.FillRectangle(Brushes.Black,0,0,this.Width, this.Height);
			//e.Graphics.FillRectangle(Brushes.Crimson,0,0,this.Width, this.Height);

			if (showTitle)
			{
				e.Graphics.FillRectangle(tmpTitleRedBrush,2,2,this.Width-4, 18);
				SizeF textsize = new SizeF(0,0);
				textsize = e.Graphics.MeasureString("STATUS",MyDefaultSkinFontBold10);
				start_x = (this.Width - (int)textsize.Width) / 2;
				//e.Graphics.DrawString("Daily Financials", MyDefaultSkinFontBold14, Brushes.White,start_x,0);
				e.Graphics.DrawString("STATUS", MyDefaultSkinFontBold10, Brushes.White,start_x,0);
			}
		}

		private void setBackColors(Color NewColor)
		{
			lblGameRound.BackColor = NewColor;
			lblGameTime.BackColor = NewColor;
			lblDisplayDay.BackColor = NewColor;
			//lblPrePlayTime.BackColor = NewColor;
			lblIncidentTime.BackColor = NewColor;
		}

		private void setForeColors(Color NewColor)
		{
			lblGameRound.ForeColor = NewColor;
			lblGameTime.ForeColor = NewColor;
			lblDisplayDay.ForeColor = NewColor;
			//lblPrePlayTime.ForeColor = NewColor;
			lblIncidentTime.ForeColor = NewColor;
		}

		private void _RecoveryProcessNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			string threat_level_str = _RecoveryProcessNode.GetAttribute("threat_level");
			switch (threat_level_str.ToLower())
			{
				case "normal":
					//setBackColors(Color.Green);
					//setForeColors(Color.Black);
					setBackColors(Color.Black);
					setForeColors(Color.Green);
					break;
				case "low":
					//setBackColors(Color.Yellow);
					//setForeColors(Color.Black);
					setBackColors(Color.Black);
					setForeColors(Color.Yellow);
					break;
				case "med":
					//setBackColors(Color.Orange);
					//setForeColors(Color.Black);
					setBackColors(Color.Black);
					setForeColors(Color.Orange);
					break;
				case "high":
					//setBackColors(Color.Red);
					//setForeColors(Color.Black);
					setBackColors(Color.Black);
					setForeColors(Color.Red);
					break;
			}
			//Check if we need to show the MTPD for now on.
			bool show_mtpd = _RecoveryProcessNode.GetBooleanAttribute("show_countdown",false);
			if ((show_mtpd)&(showMTPD==false))
			{
				showMTPD = true;
			}
		}
	}
}
