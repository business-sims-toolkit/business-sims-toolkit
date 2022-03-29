using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;
using CommonGUI;
using GameManagement;
using LibCore;
using Network;

namespace ResizingUi
{
    public class RoundTimeViewPanel : FlickerFreePanel
    {
        readonly NetworkProgressionGameFile gameFile;
        readonly Node preplayNode;
	    readonly Node preplayControlNode;
        readonly Node timeNode;

        int? round;
        GameFile.GamePhase? phase;

        public RoundTimeViewPanel (NetworkProgressionGameFile gameFile)
        {
            this.gameFile = gameFile;

            preplayNode = gameFile.NetworkModel.GetNamedNode("preplay_status");
            preplayNode.AttributesChanged += prePlayNode_AttributesChanged;

            timeNode = gameFile.NetworkModel.GetNamedNode("CurrentTime");
            timeNode.AttributesChanged += timeNode_AttributesChanged;

	        preplayControlNode = gameFile.NetworkModel.GetNamedNode("preplay_control");
        }

		protected override void Dispose (bool disposing)
        {
            if (disposing)
            {
                preplayNode.AttributesChanged -= prePlayNode_AttributesChanged;
                timeNode.AttributesChanged -= timeNode_AttributesChanged;
            }

            base.Dispose(disposing);
        }

        protected override void OnSizeChanged (EventArgs e)
        {
            base.OnSizeChanged(e);

            Invalidate();
        }

        protected override void OnPaint (PaintEventArgs e)
        {
            base.OnPaint(e);

            if (round == null || gameFile.CurrentPhaseHasStarted)
            {
                round = gameFile.CurrentRound;
            }

            if (phase == null || gameFile.CurrentPhaseHasStarted)
            {
                phase = gameFile.CurrentPhase;
            }

            var roundBounds = new RectangleF(0, 0, Width * 0.475f, Height);
            var clockBounds = new RectangleF(Width * 0.5f, 0, Width * 0.5f, Height);
            
            using (var textBrush = new SolidBrush(Color.FromArgb(80, 84, 77)))
            {
                string clockString;
                Brush clockBrush;

                if (phase == GameFile.GamePhase.OPERATIONS)
                {
                    if (preplayNode.GetBooleanAttribute("preplay_running", false)
						&& preplayNode.GetIntAttribute("time_left") < preplayControlNode.GetIntAttribute("time_ref", 0))
                    {
                        clockString = CONVERT.FormatTime(preplayNode.GetIntAttribute("time_left", 0));
                        clockBrush = Brushes.Orange;
                    }
                    else
                    {
                        clockString = CONVERT.FormatTimeHms(timeNode.GetIntAttribute("seconds", 0) +
                                                            timeNode.GetHmsAttribute("round_start_clock_time", 0));
                        clockBrush = textBrush;
                    }
                }
                else
                {
                    clockString = $"{CONVERT.FormatTime(timeNode.GetIntAttribute("seconds", 0))}";
                    clockBrush = textBrush;
                }
                
                var roundString = $"Round {round}";
                using (var font = this.GetFontToFit(FontStyle.Regular, roundString, roundBounds.Size))
                {
                    e.Graphics.DrawString(roundString, font, textBrush, roundBounds, new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Far });
                }
                
                using (var font = this.GetFontToFit(FontStyle.Bold, clockString, clockBounds.Size))
                {
                    e.Graphics.DrawString(clockString, font, clockBrush, clockBounds, new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Near });
                }
            }
        }

        void timeNode_AttributesChanged(Node sender, ArrayList attrs)
        {
            Invalidate();
        }

        void prePlayNode_AttributesChanged(Node sender, ArrayList attrs)
        {
            Invalidate();
        }

	    protected override void OnMouseDown (MouseEventArgs e)
	    {
		    base.OnMouseDown(e);

		    if (e.Button == MouseButtons.Left)
		    {
			    ((Form) TopLevelControl).DragMove();
		    }
	    }
    }
}
