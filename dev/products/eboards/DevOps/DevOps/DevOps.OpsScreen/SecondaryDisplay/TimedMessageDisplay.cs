using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using Algorithms;
using CommonGUI;
using CoreUtils;
using LibCore;
using Network;

namespace DevOps.OpsScreen.SecondaryDisplay
{
    internal class TimedMessageDisplay : FlickerFreePanel
    {
        public TimedMessageDisplay (int displayDurationInSeconds = 10)
        {
            Visible = false;

            watchedNodesToAttributes = new Dictionary<Node, WatchedNodeAttributes>();
            messageQueue = new List<TextToDisplay>();
            
            displayDurationMs = displayDurationInSeconds * 1000;
            displayIntervalMs = 100;
            displayTimer = new Timer
            {
                Interval = displayIntervalMs
            };
            displayTimer.Tick += displayTimer_Tick;
        }

        public void AddNodeToWatch (Node nodeToWatch, string titleAttribute, string textAttribute)
        {
            watchedNodesToAttributes.Add(nodeToWatch, new WatchedNodeAttributes
            {
                TitleAttribute = titleAttribute,
                TextAttribute = textAttribute
            });

            nodeToWatch.ChildAdded += watchedNode_ChildAdded;
        }

        internal struct WatchedNodeAttributes
        {
            public string TitleAttribute { get; set; }
            public string TextAttribute { get; set; }
        }

        readonly Dictionary<Node, WatchedNodeAttributes> watchedNodesToAttributes;

        protected override void Dispose (bool disposing)
        {
            if (disposing)
            {
                foreach (var node in watchedNodesToAttributes.Keys)
                {
                    node.ChildAdded -= watchedNode_ChildAdded;
                }
            }

            base.Dispose(disposing);
        }

        protected override void OnSizeChanged (EventArgs e)
        {
            Invalidate();
        }

        protected override void OnPaint (PaintEventArgs e)
        {
            if (! messageQueue.Any())
                return;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

            using (var backBrush = new SolidBrush(SkinningDefs.TheInstance.GetColorData("pop_up_panel_background_colour")))
            using (var textBrush = new SolidBrush(CONVERT.ParseHtmlColor("#ff2d4e")))
            using (var countdownBrush = new SolidBrush(Color.FromArgb(166, 94, 100, 104)))
            {
                e.Graphics.FillRectangle(backBrush, new Rectangle(0, 0, Width, Height));

                const int countdownWidth = 40;
                const int padding = 10;

                var countdownBounds = new Rectangle(Width - countdownWidth - (int)(padding * 1.5), padding, countdownWidth, countdownWidth);

                var message = messageQueue[0];
                

                RectangleF? titleBounds = null;

                if (!string.IsNullOrEmpty(message.Title))
                {
                    titleBounds = new RectangleFFromBounds
                    {
                        Left = 0,
                        Top = 0,
                        Bottom = Height * 0.3f,
                        Right = countdownBounds.Left - padding
                    }.ToRectangleF();
                }
                
                if (titleBounds != null)
                {
                    using (var titleFont = SkinningDefs.TheInstance.GetFont(20))// this.GetFontToFit(FontStyle.Regular, message.Title, titleBounds.Value.Size))
                    {
                        e.Graphics.DrawString(message.Title, titleFont, textBrush, titleBounds.Value, new StringFormat
                        {
                            LineAlignment = StringAlignment.Center,
                            Alignment = StringAlignment.Near
                        });
                    }
                }
                
                e.Graphics.FillPie(countdownBrush, countdownBounds, -90, -360 * ((displayElapsedTimeMs + displayIntervalMs) / displayDurationMs));
                
                if (messageQueue.Skip(1).Any())
                {
                    e.Graphics.DrawString($"+{(messageQueue.Count - 1)}", SkinningDefs.TheInstance.GetFont(12), Brushes.White, countdownBounds, new StringFormat
                    {
                        LineAlignment = StringAlignment.Center,
                        Alignment = StringAlignment.Center
                    });
                }
                
                var textHeight = Height - Math.Max(titleBounds?.Bottom + padding ?? padding,
                                     countdownBounds.Bottom + padding);
                var textBounds = new RectangleF(0, Math.Max(titleBounds?.Bottom + padding ?? padding,
                        countdownBounds.Bottom + padding), Width, textHeight)
                    .AlignRectangle(Width * 0.85f, textHeight, StringAlignment.Center, StringAlignment.Near);
                
                using (var textFont = SkinningDefs.TheInstance.GetFont(35))// this.GetFontToFit(FontStyle.Regular, message.Text, textBounds.Size))
                {
                    e.Graphics.DrawString(message.Text, textFont, textBrush, textBounds, new StringFormat
                    {
                        LineAlignment = StringAlignment.Center,
                        Alignment = StringAlignment.Center
                    });
                }
            }
        }

        void watchedNode_ChildAdded(Node sender, Node child)
        {
            if (!watchedNodesToAttributes.ContainsKey(sender))
            {
                return;
            }

            if (! child.GetBooleanAttribute("display_to_participants", false))
            {
                return;
            }

            var attributes = watchedNodesToAttributes[sender];
            messageQueue.Add(new TextToDisplay
            {
                Title = !string.IsNullOrEmpty(attributes.TitleAttribute) ? child.GetAttribute(attributes.TitleAttribute) : null,
                Text = child.GetAttribute(attributes.TextAttribute)
            });

            if (!displayTimer.Enabled)
            {
                displayElapsedTimeMs = 0;
                displayTimer.Start();
            }
            
            BringToFront();
            Visible = true;

            Invalidate();
            
        }

        void displayTimer_Tick(object sender, EventArgs e)
        {
            displayElapsedTimeMs += displayIntervalMs;

            if (displayElapsedTimeMs >= displayDurationMs)
            {
                messageQueue.RemoveAt(0);

                if (! messageQueue.Any())
                {
                    displayTimer.Stop();
                    Visible = false;
                    return;
                }

                displayElapsedTimeMs = 0;
            }

            Invalidate();
        }
        
        struct TextToDisplay
        {
            public string Title { get; set; }
            public string Text { get; set; }
        }

        readonly List<TextToDisplay> messageQueue;

        //readonly string titleAttribute;
        //readonly string textAttribute;

        //readonly Node nodeToWatch;
        readonly Timer displayTimer;
        int displayElapsedTimeMs;
        readonly int displayIntervalMs;
        readonly float displayDurationMs;
    }
}
