using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;

using LibCore;
using Network;
using CoreUtils;
using CommonGUI;

namespace Polestar_PM.OpsGUI

{
	/// <summary>
	/// Summary description for RaceRaceScreenBanner.
	/// </summary>
	public class OpsScreenTimeBanner : BasePanel
	{
		protected System.Windows.Forms.Label lblGameRound;
		protected System.Windows.Forms.Label lblGameTime;
		protected System.Windows.Forms.Label lblPrePlayTime;
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		protected System.ComponentModel.Container components = null;

		protected NodeTree _NetworkModel;
		protected Node _CurrentTimeNode;
		protected Node _CurrentPrePlayTime;
		protected string RoundName = "Round";
		protected int ClockOffset = 0;

		//skin stuff
		protected Font MyDefaultSkinFontBold10 = null;
		protected Font MyDefaultSkinFontBold12 = null;
		protected Font MyDefaultSkinFontBold24 = null;
		protected Font BannerBoldFont = null;
		protected Font TimeBoldFont = null;

		protected int hour = 0;

		public OpsScreenTimeBanner(NodeTree nt, bool showDay)
		{
			// This call is required by the Windows.Forms Form Designer.
			RoundName = SkinningDefs.TheInstance.GetData("ops_banner");
			this.BackColor = Color.FromArgb(188,188,188);

			string fontname =  SkinningDefs.TheInstance.GetData("fontname");
			MyDefaultSkinFontBold10 = ConstantSizeFont.NewFont(fontname,10,FontStyle.Bold);
			MyDefaultSkinFontBold12 = ConstantSizeFont.NewFont(fontname,12,FontStyle.Bold);
			MyDefaultSkinFontBold24 = ConstantSizeFont.NewFont(fontname,24,FontStyle.Bold);

			InitializeComponent();

			Color BannerTextColor = Color.White;
			lblGameRound.ForeColor = BannerTextColor;
			lblGameTime.ForeColor = BannerTextColor;
			lblPrePlayTime.ForeColor = Color.DarkGoldenrod;

			BannerBoldFont = MyDefaultSkinFontBold12;
			TimeBoldFont = ConstantSizeFont.NewFont(fontname, SkinningDefs.TheInstance.GetIntData("timer_font_size", 24), FontStyle.Bold);

			lblGameRound.Font = BannerBoldFont;
			lblGameTime.Font = TimeBoldFont;
			lblPrePlayTime.Font = TimeBoldFont; 

			//Connect up the Required Node
			_NetworkModel = nt;
			_CurrentTimeNode = _NetworkModel.GetNamedNode("CurrentTime");
			_CurrentTimeNode.AttributesChanged +=new Network.Node.AttributesChangedEventHandler(_CurrentTimeNode_AttributesChanged);

			_CurrentPrePlayTime = _NetworkModel.GetNamedNode("preplay_status");
			_CurrentPrePlayTime.AttributesChanged +=new Network.Node.AttributesChangedEventHandler(_CurrentPrePlayTime_AttributesChanged);

			setLabel();
			handleSize();
			this.Resize += new EventHandler(RaceScreenBanner_Resize);
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

				if (BannerBoldFont != null)
				{
					BannerBoldFont.Dispose();
				}
				if (TimeBoldFont != null)
				{
					TimeBoldFont.Dispose();
				}

				if(_CurrentTimeNode != null)
				{
					_CurrentTimeNode.AttributesChanged -= new Network.Node.AttributesChangedEventHandler(_CurrentTimeNode_AttributesChanged);
					_CurrentTimeNode = null;
				}

				if(_CurrentPrePlayTime != null)
				{
					_CurrentPrePlayTime.AttributesChanged -=new Network.Node.AttributesChangedEventHandler(_CurrentPrePlayTime_AttributesChanged);
					_CurrentPrePlayTime = null;
				}
				_NetworkModel = null;
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
			lblGameRound.BackColor = Color.Transparent;
			lblGameRound.ForeColor = Color.Black;
			lblGameRound.Location = new System.Drawing.Point(30, 2);
			lblGameRound.Font = CoreUtils.SkinningDefs.TheInstance.GetFont(12);
			lblGameRound.Name = "lblGameRound";
			lblGameRound.Size = new System.Drawing.Size(115, 25);
			lblGameRound.TabIndex = 0;
			lblGameRound.Text = "Round 1";
			lblGameRound.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// lblGameDate
			// 
			//this.lblGameDay.Dock = System.Windows.Forms.DockStyle.Right;
			this.lblGameTime = new System.Windows.Forms.Label();
			lblGameTime.Font = lblGameRound.Font;
			lblGameTime.ForeColor = Color.Black;
			lblGameTime.BackColor = Color.Transparent;
			lblGameTime.Location = new System.Drawing.Point(426, 2);
			lblGameTime.Name = "lblGameTime";
			lblGameTime.Size = new System.Drawing.Size(96, 25);
			lblGameTime.TabIndex = 0;
			lblGameTime.Text = "1";
			lblGameTime.TextAlign = System.Drawing.ContentAlignment.MiddleRight;

			// 
			// PrePlayTime
			// 
			this.lblPrePlayTime = new System.Windows.Forms.Label();
			lblPrePlayTime.Font = lblGameRound.Font;
			lblPrePlayTime.ForeColor = Color.Black;
			lblPrePlayTime.BackColor = Color.Transparent;
			lblPrePlayTime.Location = new System.Drawing.Point(426, 2);
			lblPrePlayTime.Name = "lblGameTime";
			lblPrePlayTime.Size = new System.Drawing.Size(96, 25);
			lblPrePlayTime.TabIndex = 0;
			lblPrePlayTime.Text = "1";
			lblPrePlayTime.Visible = false;
			lblPrePlayTime.TextAlign = System.Drawing.ContentAlignment.MiddleRight;

			this.Controls.Add(this.lblGameRound);
			this.Controls.Add(this.lblGameTime);
			this.Controls.Add(this.lblPrePlayTime);
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
			}
			else
			{
				lblPrePlayTime.Visible = false;
				lblGameTime.Visible = true;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public void setLabel()
		{
			//this.lblGameDay.Text = "Day " + _CurrentDayNode.GetAttribute("Day");

			//In McKinley, we turns seconds to minutes
			int minutes = _CurrentTimeNode.GetIntAttribute("seconds",0);
			int hours = minutes/60;
			minutes -= hours*60;
			string hoursStr = CONVERT.ToStr(hours).PadLeft(2,'0');
			string minutesStr = CONVERT.ToStr(minutes).PadLeft(2,'0');

			this.lblGameTime.Text = hoursStr + ":" + minutesStr;
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

//		public void SetHourMode(int requiredHour)
//		{
//			Hour = requiredHour;
//			ClockOffset = 60;
//			handleSize();
//		}

		public virtual void handleSize()
		{
			lblGameRound.Location = new Point(0,3);
			lblGameRound.Size = new Size(80,28);
			lblGameTime.Location = new Point(85-10,3);
			lblGameTime.Size = new Size(145,28);
			lblPrePlayTime.Location = new Point(85-10,3);
			lblPrePlayTime.Size = new Size(145,28);
		}

		private void RaceScreenBanner_Resize(object sender, EventArgs e)
		{
			handleSize();
//				lblGameRound.Location = new Point(2,2);
//				lblGameRound.Size = new Size(this.Width/3,this.Height-4);
//				lblGamePhase.Size = new Size(this.Width/3,this.Height-4);
//				lblGamePhase.Location = new Point(lblGameRound.Width,2);
//				lblGameTime.Size = new Size(this.Width/3,this.Height-4);
//				lblGameTime.Location = new Point(lblGamePhase.Left + lblGamePhase.Width,2);
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
	}
}
