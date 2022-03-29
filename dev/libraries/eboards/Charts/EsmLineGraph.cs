using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Xml;

using CoreUtils;

using LibCore;

namespace Charts
{
    internal class LinePoint
    {
        public Point Point;

        public LinePoint(XmlNode xml)
        {
            Debug.Assert(xml.Attributes != null);

            Point = new Point();

            if (xml.Attributes != null)
            {
                foreach (XmlAttribute attr in xml.Attributes)
                {
                    switch (attr.Name)
                    {
                        case "x":
                        case "X":
                            Point.X = CONVERT.ParseInt(attr.Value);
                            break;
                        case "y":
                        case "Y":
                            Point.Y = CONVERT.ParseInt(attr.Value);
                            break;
                    }
                }
            }
        }
    }

    internal class Data
    {
        public string Name;
        public List<PointF> Points;
        public Color Colour;
        public float Thickness;

        public Data(XmlNode dataNode)
        {
            if (dataNode.Attributes != null)
            {
                foreach (XmlAttribute attr in dataNode.Attributes)
                {
                    switch (attr.Name)
                    {
                        case "name":
                            Name = attr.Value;
                            break;
                        case "colour":
                            Colour = CONVERT.ParseComponentColor(attr.Value);
                            break;
                        case "thickness":
                            Thickness = (float)CONVERT.ParseDouble(attr.Value);
                            break;
                    }
                }
            }

            Points = new List<PointF>();

            foreach (XmlNode child in dataNode.ChildNodes)
            {
                float x = BasicXmlDocument.GetFloatAttribute(child, "x", 0);
                float y = BasicXmlDocument.GetFloatAttribute(child, "y", 0);
                Points.Add(new PointF(x, y));
            }
        }
    }

    

    public class LineGraphKey : VisualPanel
    {
	    class KeyItem
        {
            public string Name;
            public Color Colour;

            public KeyItem(XmlNode xml)
            {
                Debug.Assert(xml.Attributes != null);

                if (xml.Attributes != null)
                {
                    foreach (XmlAttribute attr in xml.Attributes)
                    {
                        switch(attr.Name)
                        {
                            case "name":
                                Name = attr.Value;
                                break;
                            case "colour":
                                Colour = CONVERT.ParseComponentColor(attr.Value);
                                break;
                        }
                    }
                }
            }
        }

        public enum Orientation
        {
            Horizontal,
            Vertical
        }

        public Orientation KeyOrientation;

        public Color KeyTextColour;
        public Color KeyBackColour;
        public Font KeyTextFont;

        List<KeyItem> keyItems;

        bool useLine;


        public LineGraphKey(XmlNode xml)
        {
            Debug.Assert(xml.Attributes != null);

            if (xml.Attributes != null)
            {
                foreach (XmlAttribute attr in xml.Attributes)
                {
                    switch (attr.Name)
                    {
                        case "layout":
                            KeyOrientation = ((attr.Value == "horizontal")
                                ? Orientation.Horizontal
                                : Orientation.Vertical);
                            break;
                        case "use_line":
                            useLine = CONVERT.ParseBool(attr.Value, false);
                            break;
                            //TODO: Add text and background colour attributes, and font
                    }
                }
            }

            KeyBackColour = Color.Transparent;
            KeyTextColour = Color.Black;

            KeyTextFont = SkinningDefs.TheInstance.GetFont(10);

            keyItems = new List<KeyItem>();

            foreach (XmlNode child in xml.ChildNodes)
            {
                if (child.Name == "keyItem")
                {
                    keyItems.Add(new KeyItem(child));
                }
            }

        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);

            Invalidate();
        }


        protected override void OnPaint(PaintEventArgs e)
        {
            using (Brush backBrush = new SolidBrush(KeyBackColour))
            {
                e.Graphics.FillRectangle(backBrush, new Rectangle(0, 0, Width, Height));
            }
            // TODO: have an option for a border?? 

            if (keyItems.Count > 0)
            {
                int keyItemWidth = ((KeyOrientation == Orientation.Horizontal) ? Width / keyItems.Count : Width);
                int keyItemHeight = ((KeyOrientation == Orientation.Horizontal) ? Height : Height / keyItems.Count);

                int dx = ((KeyOrientation == Orientation.Horizontal) ? keyItemWidth : 0);
                int dy = ((KeyOrientation == Orientation.Horizontal) ? 0 : keyItemHeight);

                int startX = 0;
                int startY = 0;

                using (Brush textBrush = new SolidBrush(KeyTextColour))
                {
                    for (int i = 0; i < keyItems.Count; i++)
                    {
                        KeyItem keyItem = keyItems[i];
                        int x = startX + (i * dx);
                        int y = startY + (i * dy);

                        //using (Brush tempBack = new SolidBrush(keyItem.Colour))
                        //{
                        //    e.Graphics.FillRectangle(tempBack, new Rectangle(x, y, keyItemWidth, keyItemHeight));
                        //}

                        if (useLine)
                        {
                            int padding = 2;
                            int lineLength = 10;

                            int lineX = x + padding;
                            int lineY = y + (keyItemHeight / 2);

                            using (Pen linePen = new Pen(keyItem.Colour, 2))
                            {
                                e.Graphics.DrawLine(linePen, new Point(lineX, lineY),
                                    new Point(lineX + lineLength, lineY));
                            }

                            x += lineLength + (2 * padding);
                            keyItemWidth -= lineLength + (2 * padding);
                        }

                        e.Graphics.DrawString(keyItem.Name, KeyTextFont, textBrush,
                            new Rectangle(x, y, keyItemWidth, keyItemHeight), 
                            new StringFormat
                            {
                                LineAlignment = StringAlignment.Center,
                                Alignment = StringAlignment.Center
                            });
                    }
                }
                
            }
            
        }
    }


    public class EsmLineGraph : Chart
    {
	    class Axis
        {
            public enum Orientation
            {
                Error,
                Horizontal,
                Vertical
            }
            
            public Orientation AxisOrientation;

            public enum GraphSide
            {
                Error,
                Left,
                Right,
                Top,
                Bottom
            }

            public GraphSide Side;

            public string Legend;

            public double Min;
            public double Max;
            public double Step;

            public bool ShowLegend;
            public bool ShowTicks;
            public bool ShowSteps;

            public Axis(XmlNode xml)
            {
                Debug.Assert(xml.Attributes != null);

                if (xml.Attributes != null)
                {
                    foreach (XmlAttribute attr in xml.Attributes)
                    {
                        switch (attr.Name.ToLower())
                        {
                            case "legend":
                                Legend = attr.Value;
                                break;
                            case "min":
                                Min = CONVERT.ParseDouble(attr.Value);
                                break;
                            case "max":
                                Max = CONVERT.ParseDouble(attr.Value);
                                break;
                            case "step":
                                Step = CONVERT.ParseDouble(attr.Value);
                                break;
                            case "show_steps":
                                ShowSteps = CONVERT.ParseBool(attr.Value, false);
                                break;
                            case "show_tick_lines":
                                ShowTicks = CONVERT.ParseBool(attr.Value, false);
                                break;
                            case "show_legend":
                                ShowLegend = CONVERT.ParseBool(attr.Value, false);
                                break;
                            case "orientation":
                                AxisOrientation = ParseOrientation(attr.Value);
                                break;
                            case "side":
                                Side = ParseGraphSide(attr.Value);
                                break;
                        }
                    }

                    Debug.Assert(Max > Min);
                    Debug.Assert(Max - Min > 0);
                    Debug.Assert(Step < Max);
                    Debug.Assert(Step > Min && Step > 0);
                }

            }

            Orientation ParseOrientation(string value)
            {
                switch(value.ToLower())
                {
                    case "horizontal":
                        return Orientation.Horizontal;
                    case "vertical":
                        return Orientation.Vertical;
                    default :
                        Debug.Assert(false);
                        return Orientation.Error;
                }
            }

            GraphSide ParseGraphSide(string value)
            {
                switch(value.ToLower())
                {
                    case "left":
                        return GraphSide.Left;
                    case "right":
                        return GraphSide.Right;
                    case "top":
                        return GraphSide.Top;
                    case "bottom":
                        return GraphSide.Bottom;
                    default:
                        Debug.Assert(false);
                        return GraphSide.Error;
                }
            }

            public double MapValueWithBounds(double value, double lowerBound, double upperBound)
            {
                return lowerBound + ((value - Min) * (upperBound - lowerBound) / (Max - Min));
            }

            public void RenderAxis(Graphics g, Rectangle absoluteBounds, Rectangle graphBounds, Font font, bool showAxisLine, Color textColour, Color lineColour, int legendHeight, int stepTextWidth)
            {

                if (showAxisLine)
                {
                    RenderAxisLine(g, graphBounds, lineColour);
                }

                if (ShowLegend)
                {

                    RenderLegend(g, graphBounds, font, textColour, legendHeight, stepTextWidth);
                }

                if (ShowSteps)
                {
                    RenderSteps(g, graphBounds, font, lineColour, stepTextWidth);
                }
            }

            void RenderAxisLine(Graphics g, Rectangle graphBounds, Color lineColour)
            {
                using (Pen linePen = new Pen(lineColour, 2))
                {
                    Point start = new Point();
                    Point end = new Point();

                    switch (Side)
                    {
                        case GraphSide.Left:
                            start.X = graphBounds.Left;
                            start.Y = graphBounds.Bottom;
                            end.X = graphBounds.Left;
                            end.Y = graphBounds.Top;
                            break;
                        case GraphSide.Right:
                            start.X = graphBounds.Right;
                            start.Y = graphBounds.Bottom;
                            end.X = graphBounds.Right;
                            end.Y = graphBounds.Top;
                            break;
                        case GraphSide.Top:
                            start.X = graphBounds.Left;
                            start.Y = graphBounds.Top;
                            end.X = graphBounds.Right;
                            end.Y = graphBounds.Top;
                            break;
                        case GraphSide.Bottom:
                            start.X = graphBounds.Left;
                            start.Y = graphBounds.Bottom;
                            end.X = graphBounds.Right;
                            end.Y = graphBounds.Bottom;
                            break;
                    }

                    g.DrawLine(linePen, start, end);
                }
            }

            void RenderLegend(Graphics g, Rectangle graphBounds, Font font, Color textColour, int legendHeight, int stepTextWidth)
            {
                int legendWidth = (Side == GraphSide.Left || Side == GraphSide.Right)
                        ? graphBounds.Height
                        : graphBounds.Width;

                int x = 0;
                int y = 0;

                StringAlignment verticalAlignment = StringAlignment.Center;

                switch (Side)
                {
                    case GraphSide.Left:
                        x = graphBounds.Left - legendHeight - stepTextWidth;
                        y = graphBounds.Bottom;
                        break;
                    case GraphSide.Right:
                        x = graphBounds.Right + legendHeight + stepTextWidth;
                        y = graphBounds.Top;
                        break;
                    case GraphSide.Top:
                        x = graphBounds.Left;
                        y = graphBounds.Top - legendHeight;
                        break;
                    case GraphSide.Bottom:
                        x = graphBounds.Left;
                        y = graphBounds.Bottom;
                        break;
                }
                GraphicsState state = g.Save();

                if (AxisOrientation == Orientation.Vertical)
                {
                    Debug.Assert(Side == GraphSide.Right || Side == GraphSide.Left, "Side should be either Left or Right if the axis is vertical.");


                    float angle = (Side == GraphSide.Left) ? -90 : 90;
                    g.ResetTransform();
                    g.RotateTransform(angle);
                    
                    g.TranslateTransform(x, y, MatrixOrder.Append);
                    
                    x = y = 0;
                }
                using (Brush textBrush = new SolidBrush(textColour))
                {
                    g.DrawString(Legend, font, textBrush, new Rectangle(x, y, legendWidth, legendHeight),
                        new StringFormat
                        {
                            LineAlignment = verticalAlignment,
                            Alignment = StringAlignment.Center
                        });
                }
                

                g.Restore(state);
            }

            void RenderSteps(Graphics g, Rectangle graphBounds, Font textFont, Color stepColour, int stepTextWidth)
            {
                using (Brush stepBrush = new SolidBrush(stepColour))
                {
                    int tickBarLength = 2;
                    
                    int textX = 0;
                    int textY = 0;

                    int textWidth = stepTextWidth;
                    int textHeight = 15;


                    double step = Min;

                    double lowerBound = (Side == GraphSide.Left || Side == GraphSide.Right) ? graphBounds.Bottom : graphBounds.Left;
                    double upperBound = (Side == GraphSide.Left || Side == GraphSide.Right) ? graphBounds.Top : graphBounds.Right;

                    StringAlignment horizontalAlignment = StringAlignment.Center;
                    

                    while (step <= Max)
                    {
                        double stepPoint = MapValueWithBounds(step, lowerBound, upperBound);

                        switch (Side)
                        {
                            case GraphSide.Left:
                                textX = graphBounds.Left - tickBarLength - textWidth;
                                textY = (int)stepPoint - (textHeight / 2);
                                horizontalAlignment = StringAlignment.Far;
                                
                                break;
                            case GraphSide.Right:
                                textX = graphBounds.Right + tickBarLength;
                                textY = (int)stepPoint - (textHeight / 2);
                                horizontalAlignment = StringAlignment.Near;
                                
                                break;
                            case GraphSide.Top:
                                textX = (int)stepPoint - (textWidth / 2);
                                textY = graphBounds.Top - tickBarLength - textHeight;
                                
                                break;
                            case GraphSide.Bottom:
                                textX = (int)stepPoint - (textWidth / 2);
                                textY = graphBounds.Bottom + tickBarLength;
                                
                                break;
                            default:
                                Debug.Assert(false, "Shouldn't hit here, Side is not set correctly.");
                                break;
                        }

                        g.DrawString(CONVERT.Format("{0}", step), textFont, stepBrush,
                            new Rectangle(textX, textY, textWidth, textHeight),
                            new StringFormat
                            {
                                LineAlignment = StringAlignment.Center,
                                Alignment = horizontalAlignment
                            });

                        step += Step;
                    }
                }
            }

        }

        public LineGraphKey KeyPanel;

        Axis xAxis;
        Axis yAxis;

        Font textFont;

        List<Data> dataSets;

        Color rowColour;
        Color altRowColour;

        string title;

        int lastKnownGameTime;
        
        public override void LoadData (string xmldata)
        {
            BasicXmlDocument xml = BasicXmlDocument.Create(xmldata);
            XmlNode root = xml.DocumentElement;

            if (root != null)
            {
                dataSets = new List<Data>();

                foreach (XmlNode child in root.ChildNodes)
                {
                    switch(child.Name.ToLower())
                    {
                        case "xaxis":
                            xAxis = new Axis(child);
                            break;
                        case "yaxis":
                            yAxis = new Axis(child);
                            break;
                        case "data":
                            dataSets.Add(new Data(child));
                            break;
                        case "key":
                            KeyPanel = new LineGraphKey(child);
                            break;
                        default:
                            Debug.Assert(false, "What?! " + child.Name);
                            break;
                    }
                }

                Debug.Assert(root.Attributes != null);

                if (root.Attributes != null)
                {
                    foreach (XmlAttribute attr in root.Attributes)
                    {
                        switch(attr.Name)
                        {
                            case "row_colour":
                                rowColour = CONVERT.ParseComponentColor(attr.Value);
                                break;
                            case "row_colour_alternate":
                                altRowColour = CONVERT.ParseComponentColor(attr.Value);
                                break;
                            case "title":
                                title = attr.Value;
                                break;
                            case "last_known_time":
                                lastKnownGameTime = CONVERT.ParseInt(attr.Value);
                                break;
                        }
                    }
                }
            }
        }
        
        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);

            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            g.SmoothingMode = SmoothingMode.HighQuality;

            Color textColour = SkinningDefs.TheInstance.GetColorData("line_graph_text_colour", Color.Black);
            Color lineColour = SkinningDefs.TheInstance.GetColorData("line_graph_line_colour", Color.Black);
            textFont = SkinningDefs.TheInstance.GetFont(
                                    SkinningDefs.TheInstance.GetFloatData("line_graph_font_size",10),
                                    SkinningDefs.TheInstance.GetBoolData("line_graph_bold_font", false) ? FontStyle.Bold : FontStyle.Regular);

            using (Pen tickPen = new Pen(lineColour, 1))
            using (Brush textBrush = new SolidBrush(textColour))
            using (Brush rowBrush = new SolidBrush(rowColour))
            using (Brush altRowBrush = new SolidBrush(altRowColour))
            {
                g.FillRectangle(rowBrush, new Rectangle(0, 0, Width, Height));

                int legendHeight = 30;
                int yAxisStepWidth = 40;
                int rightPadding = 15;
                int titleHeight = 40;
                Rectangle graphBounds = new Rectangle(legendHeight + yAxisStepWidth, titleHeight, Width - legendHeight - yAxisStepWidth - rightPadding,
                    Height - (titleHeight + legendHeight));
                
                // Render alternating row back colours
                int numRows = (int)((Math.Abs(yAxis.Max - yAxis.Min)) / yAxis.Step);
                double rowHeight = yAxis.Step * graphBounds.Height / Math.Abs(yAxis.Max - yAxis.Min);
                
                
                for (int i = 1; i < numRows; i++)
                {
                    Debug.Assert(i * yAxis.Step <= yAxis.Max);

                    double rowY = yAxis.MapValueWithBounds(i * yAxis.Step, graphBounds.Bottom, graphBounds.Top);

                    if (i % 2 == 1)
                    {
                        g.FillRectangle(altRowBrush, new Rectangle(graphBounds.Left, (int)rowY, graphBounds.Width, (int)rowHeight));
                    }

                    if (yAxis.ShowTicks)
                    {
                        g.DrawLine(tickPen, new PointF(graphBounds.Left, (float)rowY),
                            new PointF(graphBounds.Right, (float) rowY));
                    }
                    
                    
                }


                yAxis.RenderAxis(g, new Rectangle(0, 0, Width, Height), graphBounds,
                    textFont, false, textColour, lineColour, legendHeight, yAxisStepWidth);

                xAxis.RenderAxis(g, new Rectangle(0, 0, Width, Height), graphBounds,
                    textFont, true, textColour, lineColour, legendHeight, yAxisStepWidth);

                // Render title
                int titleX = graphBounds.Left;
                int titleWidth = graphBounds.Width;
                int titleY = 0;

                Font titleFont = SkinningDefs.TheInstance.GetFont(12, FontStyle.Bold);
                g.DrawString(title, titleFont, textBrush, new Rectangle(titleX, titleY, titleWidth, titleHeight),
                    new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    });

                foreach (Data data in dataSets)
                {
                    using (Pen linePen = new Pen(data.Colour, data.Thickness))
                    {
                        for (int min = 1; min <= lastKnownGameTime; min++)
                        {
                            PointF startPoint = data.Points[min - 1];
                            PointF endPoint = data.Points[min];

                            PointF transformedStart =
                                new PointF((float)xAxis.MapValueWithBounds(startPoint.X, graphBounds.Left, graphBounds.Right),
                                    (float)yAxis.MapValueWithBounds(startPoint.Y, graphBounds.Bottom, graphBounds.Top));

                            PointF transformedEnd =
                                new PointF((float)xAxis.MapValueWithBounds(endPoint.X, graphBounds.Left, graphBounds.Right),
                                    (float)yAxis.MapValueWithBounds(endPoint.Y, graphBounds.Bottom, graphBounds.Top));

                            g.DrawLine(linePen, transformedStart, transformedEnd);
                        }
                    }
                }

            }
        }

    }
}
