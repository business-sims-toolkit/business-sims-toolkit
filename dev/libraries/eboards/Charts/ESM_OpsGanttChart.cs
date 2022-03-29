using System;
using System.Drawing;
using System.Windows.Forms;

using CoreUtils;
using LibCore;

namespace Charts
{
    public class ESM_OpsGanttChart : OpsGanttChart
    {
        protected override void OnPaint(PaintEventArgs e)
        {
            int xl = xAxis.Left;
            int yl = xAxis.Top;

            base.OnPaint(e);

            //Draw the Grid
            if (fillColour)
            {
                e.Graphics.FillRectangle(fill, xl, TopOffset, xAxis.Width, yl - TopOffset);
            }

            ChartUtils.DrawGrid(e, this, gridColour, xl, TopOffset, xAxis.Width, yl - TopOffset, xAxis.Max, leftAxis.Steps, hwidth, vwidth);

            foreach (Strip strip in strips)
            {
                double startX = (strip.start - xAxis.Min) * (xAxis.Width * 1.0 / (xAxis.Max - xAxis.Min));
                double endX = (strip.end - xAxis.Min) * (xAxis.Width * 1.0 / (xAxis.Max - xAxis.Min));

                RectangleF rectangle = new RectangleF((float)startX, TopOffset, (float)(endX - startX), yl - TopOffset);

                StringFormat format = new StringFormat
                                      {
                                          Alignment = StringAlignment.Far,
                                          LineAlignment = StringAlignment.Center
                                      };

                if (strip.colour.HasValue)
                {
                    using (Brush brush = new SolidBrush(strip.colour.Value))
                    {
                        e.Graphics.FillRectangle(brush, rectangle);
                    }
                }

                if (strip.borderColour.HasValue)
                {
                    using (Pen pen = new Pen(strip.borderColour.Value))
                    {
                        e.Graphics.DrawRectangle(pen, new Rectangle((int)rectangle.Left, (int)rectangle.Top, (int)rectangle.Width, (int)rectangle.Height));
                    }
                }

                if (!String.IsNullOrEmpty(strip.legend))
                {
                    using (Font font = ConstantSizeFont.NewFont(SkinningDefs.TheInstance.GetData("fontname"), 12))
                    {
                        e.Graphics.DrawString(strip.legend.Replace(" ", "\n\n"), font, Brushes.Black, rectangle, format);
                    }
                }
            }
        }

        protected override string GetFilenameFromPatternName(string patternName)
        {
            if (patternName == "orange_hatch")
            {
                return AppInfo.TheInstance.Location + "\\images\\chart\\hatch.png";
            }
            else if (patternName == "yellow_hatch")
            {
                return AppInfo.TheInstance.Location + "\\images\\chart\\yellow_hatch.png";
            }
            else if (patternName == "magenta_hatch")
            {
                return AppInfo.TheInstance.Location + "\\images\\chart\\magenta_hatch.png";
            }
            if (patternName == "hatch_it")
            {
                return AppInfo.TheInstance.Location + "\\images\\chart\\hatch_it.png";
            }
            else if (patternName == "hatch_hr")
            {
                return AppInfo.TheInstance.Location + "\\images\\chart\\hatch_hr.png";
            }
            if (patternName == "hatch_fm")
            {
                return AppInfo.TheInstance.Location + "\\images\\chart\\hatch_fm.png";
            }
            else if (patternName == "hatch_fin")
            {
                return AppInfo.TheInstance.Location + "\\images\\chart\\hatch_fin.png";
            }
            else
            {
                return "";
            }
        }

        protected override void AddBarControls()
        {
            SuspendLayout();

            foreach (BarData bar in BarArray)
            {
                GanttPanel vp = new GanttPanel
                                {
                                    GraduateBars = _GraduateBars,
                                    BorderStyle = BorderStyle.None,
                                    BorderColor = bar.borderColor,
                                    LegendColor = bar.legendColour,
                                    Legend = bar.legend
                                };

                string filename = GetFilenameFromPatternName(bar.fill);
                
                if (filename != "")
                {
                    vp.BackgroundImage = Repository.TheInstance.GetImage(filename);
                }
                else
                {
                    vp.BackColor = bar.color;
                }

                if (bar.description != "")
                {
                    string desc = bar.description;
                    desc = desc.Replace("\\r\\n", "\r\n");
                    controlToTitle[vp] = desc;
                }

                BarPanels.Add(vp);
                vp.MouseMove += vp_MouseMove;

                Controls.Add(vp);
            }

            ResumeLayout(false);
        }
    }
}
