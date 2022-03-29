using System;
using System.Collections.Generic;
using System.Drawing;
using System.Xml;
using System.Windows.Forms;

using CoreUtils;
using Events;
using LibCore;
using ResizingUi;

namespace DevOps.ReportsScreen
{
    internal class GroupedBoxChart : SharedMouseEventControl
    {
        class EnclosurePanel : Panel
        {
            class ServicesList : Panel
            {
                readonly Label titleLabel;

                readonly int titleHeight = 20;
                readonly int labelHeight = 15;
                readonly int heightPadding = 5;

                List<Label> serviceLabels;
                readonly List<string> services;
                readonly Font servicesFont;
                readonly Color servicesBackColour;
                readonly Color servicesForeColour;

                public ServicesList(XmlElement section)
                {
                    var sectionName = section.GetAttribute("name");

                    titleLabel = new Label
                    {
                        Text = sectionName,
                        TextAlign = ContentAlignment.MiddleCenter,
                        Font = SkinningDefs.TheInstance.GetFont(10, FontStyle.Bold),
                        ForeColor = SkinningDefs.TheInstance.GetColorData("network_report_section_text_colour",Color.Black),
                        BackColor = SkinningDefs.TheInstance.GetColorData("network_report_section_back_colour",Color.FromArgb(238, 238, 238)),
                        Location = new Point(0, 0),
                        Name = "title"
                    };

                    Controls.Add(titleLabel);

                    serviceLabels = new List<Label>();

                    servicesFont = SkinningDefs.TheInstance.GetFont(9);
                    servicesBackColour = Color.White;
                    servicesForeColour = Color.Black;

                    services = new List<string>();
                    using (var g = this.CreateGraphics())
                    {
                        
                        foreach (XmlElement service in section.ChildNodes)
                        {
                            var serviceText = service.GetAttribute("display_text");
                            labelHeight = Math.Max(labelHeight, (int)g.MeasureString(serviceText, servicesFont).Height);
                            services.Add(serviceText);
                        }
                    }
                }

                public int GetPreferredHeight()
                {
                    var height = titleHeight + heightPadding;
                    
                    height += labelHeight * services.Count;

                    return height;
                }

                protected override void OnSizeChanged(EventArgs e)
                {
                    base.OnSizeChanged(e);

                    DoSize();
                }

                void DoSize()
                {
                    titleLabel.Size = new Size(Width, titleHeight);
                }

                protected override void OnPaint(PaintEventArgs e)
                {
                    base.OnPaint(e);

                    var y = titleLabel.Bottom;

                    var maxServiceHeight = 0;

                    foreach (var service in services)
                    {
                        maxServiceHeight = Math.Max(maxServiceHeight,
                            (int) e.Graphics.MeasureString(service, servicesFont).Height);
                    }

                    var remainingHeight = Height - y;

                    var preferredHeight = services.Count * Math.Min(labelHeight, maxServiceHeight);

                    if (preferredHeight > remainingHeight)
                    {
                        throw new Exception(CONVERT.Format(
                            "Insufficient height remaining to display {0} services.",services.Count));
                    }

                    var sf = new StringFormat
                                      {
                                          Alignment = StringAlignment.Center,
                                          LineAlignment = StringAlignment.Center
                                      };

                    using (Brush textBrush = new SolidBrush(servicesForeColour))
                    using (Brush backBrush = new SolidBrush(servicesBackColour))
                    {
                        foreach (var service in services)
                        {
                            e.Graphics.FillRectangle(backBrush, new Rectangle(0, y, Width, maxServiceHeight));
                            e.Graphics.DrawString(service, servicesFont, textBrush,
                                new RectangleF(0, y, Width, maxServiceHeight), sf);

                            y += maxServiceHeight;
                        }
                    }
                }
            }

            readonly Label titleLabel;

            readonly Panel containingPanel;
            readonly List<ServicesList> sections;

            public EnclosurePanel(XmlElement xml)
            {
                titleLabel = new Label
                             {
                                 Text = xml.GetAttribute("name"),
                                 TextAlign = ContentAlignment.MiddleCenter,
                                 Font = SkinningDefs.TheInstance.GetFont(12, FontStyle.Bold),
                                 ForeColor = Color.Black,
                                 BackColor = Color.White,
                                 Location = new Point(0,0)
                             };

                Controls.Add(titleLabel);

                containingPanel = new Panel
                                  {
                                      BackColor = Color.Transparent,
                                      AutoScroll = true
                                  };

                Controls.Add(containingPanel);

                sections = new List<ServicesList>();

                foreach (XmlElement section in xml.ChildNodes)
                {
                    var sectionList = new ServicesList(section);
                    sections.Add(sectionList);
                    containingPanel.Controls.Add(sectionList);
                }
            }

            protected override void OnSizeChanged(EventArgs e)
            {
                base.OnSizeChanged(e);

                DoSize();
            }

            void DoSize()
            {
                DoubleBuffered = true;

                var padding = 4;

                titleLabel.Size = new Size(Width - 2*padding, 30);
                titleLabel.Location = new Point(padding, padding);

                containingPanel.Location = new Point(titleLabel.Left, titleLabel.Bottom);
                var remainingHeight = Height - titleLabel.Height - 2 * padding;
                containingPanel.Size = new Size(titleLabel.Width, remainingHeight);

                var listWidth = titleLabel.Width - padding;

                var y = 0;
                foreach (var section in sections)
                {
                    var preferredHeight = section.GetPreferredHeight();

                    var height = preferredHeight;
                    
                    section.Size = new Size(listWidth, height);
                    section.Location = new Point(padding / 2, y);

                    y = section.Bottom;
                }
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                
                using (var pen = new Pen(Color.FromArgb(229, 229, 229), 4))
                {
                    var bounds = new Rectangle(0, 0, Width, Height);

                    e.Graphics.DrawRectangle(pen, bounds);
                }
            }
        }

        readonly List<EnclosurePanel> enclosurePanels;

        int numEnclosuresAcross = 5;

        public int NumEnclosuresAcross
        {
            get => numEnclosuresAcross;
            set
            {
                if (value < 1)
                {
                    throw new Exception("Invalid number of enclosures across.");
                }

                numEnclosuresAcross = value;
                DoSize();
            }
        }

        int numEnclosuresDown = 3;

        public int NumEnclosuresDown
        {
            get => numEnclosuresDown;
            set
            {
                if (value < 1)
                {
                    throw new Exception("Invalid number of enclosures down.");
                }

                numEnclosuresDown = value;
                DoSize();
            }
        }

        public GroupedBoxChart(XmlElement xml)
        {

            enclosurePanels = new List<EnclosurePanel>();

            foreach (XmlElement child in xml.ChildNodes)
            {
                switch(child.Name)
                {
                    case "Enclosures":
                        var panelBackColour = CONVERT.ParseComponentColor(child.GetStringAttribute("back_colour", "255,255,255"));
                        foreach (XmlElement enclosure in child.ChildNodes)
                        {
                            var panel = new EnclosurePanel(enclosure)
                                                   {
                                                       BackColor = panelBackColour
                                                   };
                            enclosurePanels.Add(panel);
                            Controls.Add(panel);
                        }
                        break;
                }
            }
            
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);

            DoSize();
        }

        void DoSize()
        {
            DoubleBuffered = true;

            float heightPadding = 5;
            float widthPadding = 5;

            float innerPadding = 5;

            var width = Width - 50;
            var height = Height - 50;

            var totalWidth = Width - 2 * widthPadding - (numEnclosuresAcross - 1) * innerPadding;
            var panelWidth = totalWidth / numEnclosuresAcross;
            var totalHeight = Height - 2 * heightPadding - (numEnclosuresDown - 1) * innerPadding;
            var panelHeight = totalHeight / numEnclosuresDown;

            for (var i = 0; i < enclosurePanels.Count; i++)
            {
                var x = widthPadding + i % numEnclosuresAcross * (panelWidth + innerPadding);
                var y = heightPadding + i / numEnclosuresAcross * (panelHeight + innerPadding);

                enclosurePanels[i].Location = new Point((int)x, (int)y);
                enclosurePanels[i].Size = new Size((int)panelWidth, (int)panelHeight);

                enclosurePanels[i].Invalidate();
            }
        }

	    public override List<KeyValuePair<string, Rectangle>> BoundIdsToRectangles =>
		    new List<KeyValuePair<string, Rectangle>>
		    {
			    new KeyValuePair<string, Rectangle>("box_chart_all", RectangleToScreen(ClientRectangle))
		    };

	    public override void ReceiveMouseEvent (SharedMouseEventArgs args)
        {
            throw new NotImplementedException();
        }
    }
}