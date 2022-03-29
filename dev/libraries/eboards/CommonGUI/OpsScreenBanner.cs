using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;

using LibCore;
using Network;
using CoreUtils;

using Logging;

namespace CommonGUI
{
    /// <summary>
    /// Summary description for RaceRaceScreenBanner.
    /// </summary>
    public class OpsScreenBanner : BasePanel
    {
        protected Label lblGameRound;
        protected Label lblGamePhase;
        protected Label lblGameDay;
        protected Label lblGameTime;
        protected Label lblPrePlayTime;

        /// <summary> 
        /// Required designer variable.
        /// </summary>
        protected System.ComponentModel.Container components = null;

        protected NodeTree _NetworkModel;
        protected Node _CurrentTimeNode;
        protected Node _CurrentDayNode;
        protected Node _CurrentPrePlayTime;
        protected Node calendarNode;
        protected string RoundName = "Round";
        protected int ClockOffset = 0;

        //skin stuff
        protected Font MyDefaultSkinFontBold10 = null;
        protected Font MyDefaultSkinFontBold12 = null;
        protected Font MyDefaultSkinFontBold24 = null;
        protected Font MyDefaultSkinFontBold16 = null;
        protected Font TimeBoldFont = null;

        protected int hour = 0;

        protected bool showDay;
        protected int hideDayAfter = -1;
        protected int day_adjust = 0;

        bool keepColonFixedPosition;
        int colonFixedXRelativeToParent;

        //Allows Manun control of positioning and size for altered PoleStar with Courseware
        public int phase_adjustment_x = 0;
        public int phase_adjustment_width = 0;
        public int time_adjustment_x = 0;
        public int time_adjustment_width = 0;

        Color timeColour;

        Timer realClockTimer;
        bool isESM = SkinningDefs.TheInstance.GetBoolData("esm_sim", false);

        void UpdateRealClockStatus ()
        {
            Node prePlayNode = _NetworkModel.GetNamedNode("preplay_control");
            if (_CurrentTimeNode.GetBooleanAttribute("show_real_clock_before_preplay", false)
                && ! prePlayNode.GetBooleanAttribute("start", false))
            {
                if (realClockTimer == null)
                {
                    realClockTimer = new Timer();
                    realClockTimer.Interval = 250;
                    realClockTimer.Tick += realClockTimer_Tick;
                    realClockTimer.Start();
                }
            }
            else
            {
                if (realClockTimer != null)
                {
                    realClockTimer.Dispose();
                    realClockTimer = null;
                }
            }
        }

        void realClockTimer_Tick (object sender, EventArgs e)
        {
            setLabel();
        }

        public int Hour
        {
            set
            {
                hour = value;
                setLabel();
            }
        }

        public OpsScreenBanner ()
        {
        }

        public OpsScreenBanner (NodeTree nt)
        {
            _NetworkModel = nt;
        }

        public OpsScreenBanner (NodeTree nt, bool showDay)
        {
            // This call is required by the Windows.Forms Form Designer.
            RoundName = SkinningDefs.TheInstance.GetData("ops_banner");
            //some skins want the day to be displayed as Day 1, 2 .....
            day_adjust = SkinningDefs.TheInstance.GetIntData("ops_banner_day_adjust", 0);

            string fontname = SkinningDefs.TheInstance.GetData("fontname");
            MyDefaultSkinFontBold10 = ConstantSizeFont.NewFont(fontname, 10, FontStyle.Bold);
            MyDefaultSkinFontBold12 = ConstantSizeFont.NewFont(fontname, 12, FontStyle.Bold);
            MyDefaultSkinFontBold16 = ConstantSizeFont.NewFont(fontname, 16, FontStyle.Bold);
            MyDefaultSkinFontBold24 = ConstantSizeFont.NewFont(fontname, 24, FontStyle.Bold);


            InitializeComponent();

            Color BannerTextColor = Color.White;
            lblGameRound.ForeColor = BannerTextColor;
            lblGamePhase.ForeColor = BannerTextColor;
            lblGameDay.ForeColor = BannerTextColor;
            lblGameTime.ForeColor = BannerTextColor;
            timeColour = BannerTextColor;
            lblPrePlayTime.ForeColor = Color.Orange;

            Font BannerBoldFont = MyDefaultSkinFontBold12;
            Font PhaseBoldFont = MyDefaultSkinFontBold10;
            TimeBoldFont = ConstantSizeFont.NewFont(fontname, SkinningDefs.TheInstance.GetIntData("timer_font_size", 24),
                                                    FontStyle.Bold);

            if (isESM)
            {
                lblGameRound.Font = MyDefaultSkinFontBold16;
            }
            else
            {
                lblGameRound.Font = BannerBoldFont;
            }

            lblGamePhase.Font = PhaseBoldFont;
            lblGameDay.Font = BannerBoldFont;
            lblGameTime.Font = TimeBoldFont;
            lblPrePlayTime.Font = TimeBoldFont;

            lblGameDay.Visible = showDay;
            this.showDay = showDay;

            //Connect up the Required Node
            _NetworkModel = nt;
            _CurrentTimeNode = _NetworkModel.GetNamedNode("CurrentTime");
            _CurrentTimeNode.AttributesChanged +=
                _CurrentTimeNode_AttributesChanged;

            _CurrentPrePlayTime = _NetworkModel.GetNamedNode("preplay_status");
            _CurrentPrePlayTime.AttributesChanged +=
                _CurrentPrePlayTime_AttributesChanged;

            _CurrentDayNode = _NetworkModel.GetNamedNode("CurrentDay");
            _CurrentDayNode.AttributesChanged +=
                _CurrentDayNode_AttributesChanged;

            calendarNode = _NetworkModel.GetNamedNode("Calendar");
            calendarNode.AttributesChanged +=
                calendarNode_AttributesChanged;
            hideDayAfter = calendarNode.GetIntAttribute("showdays", -1);

            setLabel();

            if (isESM)
            {
                CreateNewPanel();
            }
            else
            {
                handleSize();
            }
            Resize += RaceScreenBanner_Resize;

            UpdateRealClockStatus();
        }

        /// <summary>
        /// The Round of the current game
        /// </summary>
        public virtual int Round
        {
            set { lblGameRound.Text = RoundName + " " + CONVERT.ToStr(value); }
        }

        /// <summary>
        /// Phase of the current game
        /// </summary>
        public string Phase
        {
            set { lblGamePhase.Text = value; }
        }


        public virtual void EndOnMinute ()
        {

            try
            {
                DateTime time;
                Node timeNode = _NetworkModel.GetNamedNode("CurrentTime");
                bool shortTimeFormat = false;

                //CHECK FOR MCKINLEY
                if (lblGameTime.Text.Length < 7)
                {
                    shortTimeFormat = true;
                }

                if (timeNode.GetBooleanAttribute("show_world_time", false))
                {
                    time = Convert.ToDateTime(lblGameTime.Text);
                    if (time.Second == 58 || time.Second == 59)
                    {
                        time = time.AddSeconds(60 - time.Second);
                    }
                    string secondsLabel = time.Second < 10 ? "0" + time.Second : CONVERT.ToStr(time.Second);

                    lblGameTime.Text = time.Hour + ":" + time.Minute + ":" + secondsLabel;
                }
                else
                {
                    if (SkinningDefs.TheInstance.GetBoolData("use_clock_rounding_hack", false))
                    {
                        if (shortTimeFormat)
                        {
                            string newTime = "12:" + lblGameTime.Text;
                            time = Convert.ToDateTime(newTime);
                        }
                        else
                        {
                            time = Convert.ToDateTime(lblGameTime.Text);
                        }

                        int seconds = timeNode.GetIntAttribute("seconds", 0);
                        if (seconds % 60 != 0)
                        {
                            string hourToAdd = shortTimeFormat ? "" : time.Hour + ":";
                            if (seconds % 60 < 3) // if we are less than 3 seconds ahead of the new minute)
                            {
                                lblGameTime.Text = hourToAdd + Math.Floor((float) (seconds / 60)) + ":00";
                            }
                            else if (seconds % 60 > 57)
                            {
                                lblGameTime.Text = hourToAdd + ":" + (Math.Floor((float) (seconds / 60)) + 1) +
                                                        ":00";
                            }
                        }
                    }
                }
            }
            catch (FormatException)
            {
            }
        }

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose (bool disposing)
        {
            if (disposing)
            {
                if (realClockTimer != null)
                {
                    realClockTimer.Dispose();
                }

                if (components != null)
                {
                    components.Dispose();
                }

                if (_CurrentDayNode != null)
                {
                    _CurrentDayNode.AttributesChanged -=
                        _CurrentDayNode_AttributesChanged;
                    _CurrentDayNode = null;
                }

                if (_CurrentTimeNode != null)
                {
                    _CurrentTimeNode.AttributesChanged -=
                        _CurrentTimeNode_AttributesChanged;
                    _CurrentTimeNode = null;
                }

                if (_CurrentPrePlayTime != null)
                {
                    _CurrentPrePlayTime.AttributesChanged -=
                        _CurrentPrePlayTime_AttributesChanged;
                    _CurrentPrePlayTime = null;
                }
                _NetworkModel = null;
            }
            base.Dispose(disposing);
        }

        public void ReplaceTimeNode (string new_seconds_node_name)
        {
            if (_CurrentTimeNode != null)
            {
                _CurrentTimeNode.AttributesChanged -=
                    _CurrentTimeNode_AttributesChanged;
                _CurrentTimeNode = null;
            }
            _CurrentTimeNode = _NetworkModel.GetNamedNode(new_seconds_node_name);
            _CurrentTimeNode.AttributesChanged +=
                _CurrentTimeNode_AttributesChanged;
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        protected void InitializeComponent ()
        {
            this.SuspendLayout();

            this.BackColor = Color.Transparent;
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
            // lblGamePhase
            // 
            //this.lblGamePhase.Dock = System.Windows.Forms.DockStyle.Right;
            lblGamePhase = new Label();
            lblGamePhase.BackColor = Color.Transparent;
            lblGamePhase.ForeColor = Color.Black;
            lblGamePhase.Font = lblGameRound.Font;
            lblGamePhase.Location = new System.Drawing.Point(140, 2);
            lblGamePhase.Name = "lblGamePhase";
            lblGamePhase.Size = new System.Drawing.Size(260, 25);
            lblGamePhase.TabIndex = 0;
            lblGamePhase.Text = RoundName + " Screen";
            lblGamePhase.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblGameDate
            // 
            //this.lblGameDay.Dock = System.Windows.Forms.DockStyle.Right;
            this.lblGameTime = new System.Windows.Forms.Label();
            lblGameTime.Font = lblGameRound.Font;
            lblGameTime.ForeColor = Color.Black;
            lblGameTime.BackColor = Color.Transparent;
            lblGameTime.Location = SkinningDefs.TheInstance.GetPointData("timer_position", 426, 2);
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

            // 
            // RaceScreenBanner
            // 
            lblGameDay = new Label();
            lblGameDay.BackColor = Color.Transparent;
            lblGameDay.ForeColor = Color.Black;
            lblGameDay.Font = lblGameRound.Font;
            lblGameDay.Location = new System.Drawing.Point(30, 2);
            lblGameDay.Name = "lblGameDay";
            lblGameDay.Size = new System.Drawing.Size(100, 25);
            lblGameDay.TabIndex = 0;
            lblGameDay.Text = "Day 0";
            lblGameDay.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            AddControl(lblGameDay);
            AddControl(lblGameRound);
            AddControl(lblGamePhase);
            AddControl(lblGameTime);
            AddControl(lblPrePlayTime);
            this.DockPadding.All = 2;
            this.Name = "RaceScreenBanner";
            this.Size = new System.Drawing.Size(600, 32);
            this.ResumeLayout(false);

#if DEBUG
            LibCore.PanelLabeller.LabelControl(lblGameDay);
            LibCore.PanelLabeller.LabelControl(lblGameRound);
            LibCore.PanelLabeller.LabelControl(lblGamePhase);
            LibCore.PanelLabeller.LabelControl(lblGameTime);
            LibCore.PanelLabeller.LabelControl(lblPrePlayTime);
#endif

        }

        #endregion

        public virtual void ChangeBannerTextForeColour (Color fc)
        {
            lblGameRound.ForeColor = fc;
            lblGamePhase.ForeColor = fc;
            lblGameDay.ForeColor = fc;
            lblGameTime.ForeColor = fc;

            timeColour = fc;
        }

        void AddControl (Control control)
        {
            Controls.Add(control);
            control.MouseDown += control_MouseDown;
        }

        public virtual void SetRaceViewOn (bool RaceViewOn)
        {
        }

        public virtual void ChangeBannerPrePlayTextForeColour (Color ppfc)
        {
            lblPrePlayTime.ForeColor = ppfc;
        }

        public virtual void ShowPrePlay (Boolean ShowPrePlay)
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
        public virtual void setLabel ()
        {
#if !PASSEXCEPTIONS
            try
            {
#endif
                int day = CONVERT.ParseInt(_CurrentDayNode.GetAttribute("Day"));

                if (day_adjust > 0)
                {
                    lblGameDay.Text = "Day " + CONVERT.ToStr(day + day_adjust);
                }
                else
                {
                    lblGameDay.Text = "Day " + CONVERT.ToStr(day);
                }

                Node prePlayNode = _NetworkModel.GetNamedNode("preplay_control");
                if ((! showDay)
                    && _CurrentTimeNode.GetBooleanAttribute("show_real_clock_before_preplay", false)
                    && ! prePlayNode.GetBooleanAttribute("start", false))
                {
                    DateTime time = DateTime.Now;
                    lblGameTime.Text = CONVERT.Format("{0:00}:{1:00}:{2:00}",
                                                      time.Hour, time.Minute, time.Second);
                    lblGameTime.ForeColor = Color.Red;
                }
                else if (_CurrentTimeNode.GetBooleanAttribute("show_world_time", false))
                {
                    DateTime time = _CurrentTimeNode.GetDateTimeAttribute("world_time_at_zero_seconds", DateTime.Now)
                                    + TimeSpan.FromSeconds(_CurrentTimeNode.GetIntAttribute("seconds", 0));
                    string text = CONVERT.Format("{0:00}:{1:00}:{2:00}",
                                                 time.Hour, time.Minute, time.Second);

                    lblGameTime.Text = text;
                    lblGameTime.ForeColor = timeColour;
                }
                else
                {
                    int seconds = _CurrentTimeNode.GetIntAttribute("seconds", 0);
                    int minutes = seconds / 60;
                    seconds -= minutes * 60;
                    string minutesStr = CONVERT.ToStr(minutes).PadLeft(2, '0');
                    string secondsStr = CONVERT.ToStr(seconds).PadLeft(2, '0');

                    if (showDay && (hideDayAfter != -1))
                    {
                        lblGameDay.Visible = (day <= hideDayAfter);
                    }

                    if (hour == 0)
                    {
                        lblGameTime.Text = minutesStr + ":" + secondsStr;
                    }
                    else
                    {
                        string hourStr = CONVERT.ToStr(hour).PadLeft(2, '0');
                        lblGameTime.Text = hourStr + ":" + minutesStr + ":" + secondsStr;
                    }
                    lblGameTime.ForeColor = timeColour;
                }

                if (keepColonFixedPosition)
                {
                    using (Graphics graphics = CreateGraphics())
                    {
                        int colonIndex = Text.IndexOf(":");
                        string textToColon = Text.Substring(0, colonIndex);
                        SizeF size = graphics.MeasureString(textToColon, Font);
                        Left = colonFixedXRelativeToParent - (int) size.Width;
                    }
                }
#if !PASSEXCEPTIONS
            }
            catch (Exception ex)
            {
                AppLogger.TheInstance.WriteLine("OpsScreenBanner::setLabel Level Exception : " + ex.Message + ":\r\n" +
                                                ex.StackTrace);
            }
#endif
        }

        protected void _CurrentDayNode_AttributesChanged (Node sender, ArrayList attrs)
        {
            setLabel();
        }

        protected void _CurrentTimeNode_AttributesChanged (Node sender, ArrayList attrs)
        {
            UpdateRealClockStatus();
            ShowPrePlay(false);
            setLabel();
        }

        public void SetHourMode (int requiredHour)
        {
            Hour = requiredHour;
            ClockOffset = 60;
            if (isESM)
            {
                CreateNewPanel();
            }
            else
            {
                handleSize();
            }
        }

        public void SetHourMode (int requiredHour, int clockOffset)
        {
            Hour = requiredHour;
            ClockOffset = clockOffset;
            if (isESM)
            {
                CreateNewPanel();
            }
            else
            {
                handleSize();
            }
        }

	    void CreateNewPanel ()
        {
            int timerOffset = SkinningDefs.TheInstance.GetIntData("timer_x_offset", 0);
            lblGameDay.TextAlign = ContentAlignment.MiddleCenter;
            lblGamePhase.TextAlign = ContentAlignment.MiddleCenter;
            lblGameRound.TextAlign = ContentAlignment.MiddleCenter;
            lblGameTime.TextAlign = ContentAlignment.MiddleCenter;
            lblPrePlayTime.TextAlign = ContentAlignment.MiddleCenter;

            if (lblGameDay.Visible)
            {
                lblGameRound.Location = new Point(0, 0);
                lblGameRound.Size = new Size(Width, 25);

                lblGameDay.Location = new Point(lblGameRound.Left, lblGameRound.Bottom + 1);
                lblGameDay.Size = new Size(Width, 25);

                lblGamePhase.Location = new Point(lblGameDay.Left, lblGameDay.Bottom + 1);
                lblGamePhase.Size = new Size(Width, 25);

                lblGameTime.Location = new Point(lblGamePhase.Left, lblGamePhase.Bottom + 3);
                lblGameTime.Size = new Size(Width, 28);
                lblPrePlayTime.Location = new Point(lblGameTime.Left, lblGameTime.Top);
                lblPrePlayTime.Size = new Size(Width, 28);
            }
            else
            {
                lblGameRound.Location = new Point(0, 0);
                lblGameRound.Size = new Size(Width, 25);

                lblGamePhase.Location = new Point(lblGameRound.Left, lblGameRound.Bottom + 3);
                lblGamePhase.Size = new Size(Width, 25);

                lblGameTime.Location = new Point(lblGamePhase.Left, lblGamePhase.Bottom + 5);
                lblGameTime.Size = new Size(Width, 28);
                lblPrePlayTime.Location = new Point(lblGameTime.Left, lblGameTime.Top);
                lblPrePlayTime.Size = new Size(Width, 28);
            }
        }

        public virtual void handleSize ()
        {
            int timerOffset = SkinningDefs.TheInstance.GetIntData("timer_x_offset", 0);

            if (lblGameDay.Visible)
            {
                lblGameRound.Location = new Point(5, 3);
                lblGameRound.Size = new Size(100, 25);
                lblGameDay.Location = new Point(115, 3);
                lblGameDay.Size = new Size(75, 25);

                lblGamePhase.Location = new Point(200 - phase_adjustment_x, 3);
                lblGamePhase.Size = new Size(210 - phase_adjustment_width, 25);

                lblGameTime.Location = new Point(480 - ClockOffset - timerOffset - time_adjustment_x, 2);
                lblGameTime.Size = new Size(120 + ClockOffset - time_adjustment_width, 28);
                lblPrePlayTime.Location = new Point(480 - ClockOffset - timerOffset - time_adjustment_x, 2);
                lblPrePlayTime.Size = new Size(120 + ClockOffset - time_adjustment_width, 28);
            }
            else
            {
                lblGameRound.Location = new Point(5, 3);
                lblGameRound.Size = new Size(100, 25);

                lblGamePhase.Location = new Point(200 - phase_adjustment_x, 3);
                lblGamePhase.Size = new Size(210 - phase_adjustment_width, 25);

                lblGameTime.Location = new Point(480 - ClockOffset - timerOffset - time_adjustment_x, 3);
                lblGameTime.Size = new Size(120 + ClockOffset - time_adjustment_width, 28);
                lblPrePlayTime.Location = new Point(480 - ClockOffset - timerOffset - time_adjustment_x, 3);
                lblPrePlayTime.Size = new Size(120 + ClockOffset - time_adjustment_width, 28);
            }
        }

        protected void RaceScreenBanner_Resize (object sender, EventArgs e)
        {
            if (isESM)
            {
                CreateNewPanel();
            }
            else
            {
                handleSize();
            }
        }

        protected string BuildTimeString (int timevalue)
        {
            int time_mins = timevalue / 60;
            int time_secs = timevalue % 60;
            string displaystr = CONVERT.ToStr(time_mins) + ":";
            if (time_secs < 10)
            {
                displaystr += "0";
            }
            displaystr += CONVERT.ToStr(time_secs);
            if (time_mins < 10)
            {
                displaystr = "0" + displaystr;
            }
            return displaystr;
        }

        protected virtual void _CurrentPrePlayTime_AttributesChanged (Node sender, ArrayList attrs)
        {
            if (attrs != null && attrs.Count > 0)
            {
                foreach (AttributeValuePair avp in attrs)
                {
                    if (avp.Attribute == "time_left")
                    {
                        int time_left = sender.GetIntAttribute("time_left", 0);
                        if (time_left > 0)
                        {
                            lblPrePlayTime.Text = BuildTimeString(time_left);
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

	    void calendarNode_AttributesChanged (Node sender, ArrayList attrs)
        {
            // : a bit ugly, but we can't just watch for changes to the showdays
            // attribute, as it might be deleted in which case we want to default to -1.
            hideDayAfter = sender.GetIntAttribute("showdays", -1);
        }

        public void ClipTime (int offset)
        {
            lblGameTime.Left = Width - lblGameTime.Width - offset;
            lblPrePlayTime.Left = lblGameTime.Left;
        }

        public void FixColon (int position)
        {
            keepColonFixedPosition = true;
            colonFixedXRelativeToParent = position;
            lblGameTime.TextAlign = ContentAlignment.MiddleLeft;

            setLabel();
        }

        public void RepositionClock (int clockX)
        {
            lblGameTime.Left = clockX;
            lblPrePlayTime.Left = clockX;
        }

        public void RepositionClock (int clockX, int clockY)
        {
            lblGameTime.Location = new Point(clockX, clockY);
            lblPrePlayTime.Location = new Point(clockX, clockY);
        }

        public void RepositionPhase (int phaseX)
        {
            lblGamePhase.Left = phaseX;
        }

        public void SetRoundColour (Color colour)
        {
            lblGameRound.ForeColor = colour;
        }

        public void SetPhaseColour (Color colour)
        {
            lblGamePhase.ForeColor = colour;
        }

        public void SetDayColour(Color colour)
        {
            lblGameDay.ForeColor = colour;
        }

        protected override void OnMouseDown (MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.Button == MouseButtons.Left)
            {
                ((Form) TopLevelControl).DragMove();
            }
        }

        void control_MouseDown (object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ((Form) TopLevelControl).DragMove();
            }
        }
    }
}