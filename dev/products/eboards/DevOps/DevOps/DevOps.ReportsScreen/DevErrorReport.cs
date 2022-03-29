using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml;

using Algorithms;

using CommonGUI;
using CoreUtils;
using Events;
using LibCore;

using Network;
using ResizingUi;

namespace DevOps.ReportsScreen
{
	internal class DevErrorReport : SharedMouseEventControl
    {
        class GuidMessage
        {
            public string Guid;
            public string Message;
        }

        class ErrorComparator : IComparer<ErrorType>
        {
            List<string> typesInOrder;

            public ErrorComparator(List<string> errorTypes)
            {
                typesInOrder = errorTypes;
            }
            

            public int Compare (ErrorType x, ErrorType y)
            {
                int xIndex = typesInOrder.IndexOf(x.Type);
                int yIndex = typesInOrder.IndexOf(y.Type);

                return xIndex.CompareTo(yIndex);
            }
        }

        class ErrorType
        {
            public List<GuidMessage> Messages;
            public string Type;

            public ErrorType(XmlElement xmlError)
            {
                Messages = new List<GuidMessage>();

                Type = xmlError.GetAttribute("error_type");
                foreach (XmlElement messageXml in xmlError.ChildNodes)
                {
                    string message = messageXml.GetAttribute("message");
                    string guid = messageXml.GetAttribute("guid");

                    Messages.Add(new GuidMessage
                                 {
                                     Guid = guid,
                                     Message = message
                                 });
                }
            }
        }
        
        class DevErrorServicePanel : FlickerFreePanel
        {
            public string ServiceName;

            public List<ErrorType> Errors;

            public int ErrorCount
            {
                get
                {
                    return Errors.Count;
                }
            }

            bool isActive;
            public bool IsActive
            {
                get
                {
                    return IsActive;
                }
                set
                {
                    isActive = value;
                    Invalidate();
                }
            }

            bool isHover = false;

            public Image IconImage;

            Color defaultColour;
            Color activeColour;
            Color hoverColour;

            Color defaultTextColour;
            Color activeTextColour;
            Color hoverTextColour;

            public DevErrorServicePanel(XmlElement xmlService, NodeTree model)
            {
                ServiceName = xmlService.GetAttribute("service_name");
                Errors = new List<ErrorType>();

                string iconName = model.GetNamedNode("NS " + ServiceName).GetAttribute("icon");
                IconImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\icons\\" + iconName + "_default.png");

                foreach (XmlElement child in xmlService.ChildNodes)
                {
                    Errors.Add(new ErrorType(child));
                }

                Errors.Sort(new ErrorComparator(new List<string>
                                                {
                                                    "product",
                                                    "dev",
                                                    "test",
                                                    "release",
                                                    "deploy"
                                                }));

                activeColour = Color.FromArgb(201, 214, 223);
                defaultColour = Color.FromArgb(34, 40, 49);
                hoverColour = Color.FromArgb(69, 69, 83);

                defaultTextColour = Color.White;
                activeTextColour = Color.Black;
                hoverTextColour = Color.White;

            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);

                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

                
                Color backColour = defaultColour;
                Color textColour = defaultTextColour;

                if (isActive)
                {
                    backColour = activeColour;
                    textColour = activeTextColour;
                }
                else if (isHover)
                {
                    backColour = hoverColour;
                    textColour = hoverTextColour;
                }
                

                using (Brush backColourBrush = new SolidBrush(backColour))
                {
                    // Draw left side circle
                    e.Graphics.FillEllipse(backColourBrush, 0, 0, Height, Height);

                    // Draw the remainder of the width as a rectangle
                    e.Graphics.FillRectangle(backColourBrush, Height / 2, 0, Width - Height / 2, Height);

                }

                int padding = 2;
                int iconCircleWidth = Height - (2 * padding);

                e.Graphics.DrawImage(IconImage, padding + 1, padding + 1, iconCircleWidth, iconCircleWidth);

                Font textFont = SkinningDefs.TheInstance.GetFont(10);

                using (Brush textBrush = new SolidBrush(textColour))
                {
                    int textHeight = (int)e.Graphics.MeasureString(ServiceName, textFont).Height;

                    int textY = (Height - textHeight) / 2;

                    e.Graphics.DrawString(ServiceName, textFont, textBrush, 80, textY);

                    string errorText = Plurals.Format(ErrorCount, "Error", "Errors");

                    int errorWidth = (int) e.Graphics.MeasureString(errorText, textFont).Width;

                    int errorX = Width - errorWidth - 5;

                    e.Graphics.DrawString(errorText, textFont, textBrush, errorX, textY);
                }


            }
            
            protected override void OnMouseEnter(EventArgs e)
            {
                base.OnMouseEnter(e);

                isHover = true;
                Invalidate();
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                base.OnMouseLeave(e);

                isHover = false;
                Invalidate();
            }
        }




        NodeTree model;
        XmlNodeList services;
        
        DevErrorServicePanel activeServiceErrorPanel;

        Dictionary<string, string> errorTypesToDisplayNames;

        public DevErrorReport (NodeTree model, XmlElement xml)
        {
            this.model = model;

            if (xml == null)
            {
                throw new Exception("XML is null in DevErrorReport ctor.");
            }

            foreach (XmlElement child in xml.ChildNodes.Cast<XmlElement>().Where(child => child.Name == "Services"))
            {
                services = child.ChildNodes;
                break;
            }

            errorTypesToDisplayNames = new Dictionary<string, string>
                                       {
                                           {"product", "Product"},
                                           {"dev", "Development"},
                                           {"test", "Test"},
                                           {"release", "Release"},
                                           {"deploy", "Deploy"}
                                       };


            DisplayServiceList();
            
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);

            DoSize();
        }

        void DoSize()
        {
            int yOffset = 50;
            int minHeight = 20;
            int maxHeight = 50;

            int padding = 10;

            int heightMinusTotalPadding = Height - ((services.Count - 1) * padding) - yOffset;

            int serviceButtonHeight = Maths.Clamp(heightMinusTotalPadding / Math.Max(1, services.Count), minHeight, maxHeight);

            int x = 45;
            int width = 604;

            int y = yOffset;
            foreach (DevErrorServicePanel serviceButton in Controls)
            {
                serviceButton.Size = new Size(width, serviceButtonHeight);
                serviceButton.Location = new Point(x, y);

                y += serviceButtonHeight + padding;
            }
        }

        void DisplayServiceList ()
        {
            if (services != null)
            {
                foreach (XmlElement service in services)
                {
                    DevErrorServicePanel devErrorServicePanel = new DevErrorServicePanel(service, model);

                    devErrorServicePanel.Click += serviceErrors_Click;
                    Controls.Add(devErrorServicePanel);

                    if (activeServiceErrorPanel == null)
                    {
                        activeServiceErrorPanel = devErrorServicePanel;
                        activeServiceErrorPanel.IsActive = true;
                    }
                    
                }
                
            }
        }

        void serviceErrors_Click(object sender, EventArgs e)
        {
            if (activeServiceErrorPanel != null)
            {
                activeServiceErrorPanel.IsActive = false;
            }
            activeServiceErrorPanel = (DevErrorServicePanel)sender;

            activeServiceErrorPanel.IsActive = true;

            Invalidate();

        }
        

        protected override void OnPaint (PaintEventArgs e)
        {
            base.OnPaint(e);

            Font titleFont = SkinningDefs.TheInstance.GetFont(14);
            Font headingFont = SkinningDefs.TheInstance.GetFont(10, FontStyle.Bold);
            Font messageFont = SkinningDefs.TheInstance.GetFont(10);

            int x = 670;
            int y = 50;
            int yTitleOffset = 25;
            int yHeadingOffset = 20;
            int yMessageOffset = 15;
            int yOffset = 20;
            
            using (SolidBrush brush = new SolidBrush(Color.White))
            {
                e.Graphics.FillRectangle(brush, new Rectangle(660, y, Width - 700, Height - 100));
            }
            using (Pen pen = new Pen(Color.LightGray, 1.0f))
            {
                e.Graphics.DrawRectangle(pen, new Rectangle(660, y, Width - 700, Height - 100));
            }

            y += 25;


            if (activeServiceErrorPanel != null)
            {
                using (SolidBrush brush = new SolidBrush(Color.Black))
                {
                    string errorsText = "Errors";

                    SizeF errorsSize = e.Graphics.MeasureString(errorsText, titleFont);

                    e.Graphics.DrawString(errorsText, titleFont, brush, x, y);

                    float iconHeight = errorsSize.Height * 2f;
                    float iconY = (y + errorsSize.Height / 2f) - (iconHeight / 2f);

                    e.Graphics.DrawImage(activeServiceErrorPanel.IconImage,
                        new RectangleF(x + errorsSize.Width + 10, iconY, iconHeight, iconHeight));

                    y += yTitleOffset;
                    y += yMessageOffset;

                    foreach (ErrorType errorType in activeServiceErrorPanel.Errors)
                    {

                        int numFailuresForStage = 0;

                        List<string> allMessages = errorType.Messages.Select(guidMessage => guidMessage.Message).ToList();

                        List<string> guids = new List<string>();
                        foreach (GuidMessage guidMessage in errorType.Messages.Where(guidMessage => !guids.Contains(guidMessage.Guid)))
                        {
                            guids.Add(guidMessage.Guid);
                        }

                        numFailuresForStage = guids.Count;

                        string heading = CONVERT.Format("{0} ({1})",errorTypesToDisplayNames[errorType.Type], numFailuresForStage);
                        e.Graphics.DrawString(heading, headingFont, brush, x, y);
                        y += yHeadingOffset;

                        List<string> distinctMessages = new List<string>();

                        foreach (string message in allMessages.Where(message => !distinctMessages.Contains(message)))
                        {
                            distinctMessages.Add(message);
                        }

                        
                        foreach (string message in distinctMessages)
                        {
                            int count = allMessages.Count(msg => msg == message);
                            
                            if (count == 0)
                            {
                                throw new Exception("This count shouldn't be zero.");
                            }

                            string displayMessage = message;

                            if (count > 1)
                            {
                                displayMessage += CONVERT.Format(" ({0})", count);
                            }

                            e.Graphics.DrawString(displayMessage, messageFont, brush, x, y);
                            y += yMessageOffset;
                            
                        }

                        y += yOffset;
                    }
                }

            }
            else
            {
                using (SolidBrush brush = new SolidBrush(Color.Black))
                {
                    y += yTitleOffset;
                    e.Graphics.DrawString("No Errors", titleFont, brush, 670, y);
                }
            }
        }

	    public override List<KeyValuePair<string, Rectangle>> BoundIdsToRectangles { get; }

	    public override void ReceiveMouseEvent (SharedMouseEventArgs args)
        {
            throw new NotImplementedException();
        }
    }

    
}
