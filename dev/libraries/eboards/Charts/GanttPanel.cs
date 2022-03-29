using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

using LibCore;


namespace Charts
{
    public class GanttPanel : VisualPanel
    {
        public bool GraduateBars = true;

        public Color BorderColor = Color.Transparent;
        public Color LegendColor = Color.Transparent;
        public string Legend = "";

        protected override void OnPaint(PaintEventArgs e)
        {
            if (this.BackgroundImage == null)
            {
                if ((this.Bounds.Width > 0) && (this.Bounds.Height > 0))
                {
                    if (GraduateBars)
                    {
                        //fill with gradient fill
                        RectangleF gradRect = new RectangleF(0, 0, this.Width, this.Height);
                        gradRect.Inflate(0, 10);
                        LinearGradientBrush lBrush = new LinearGradientBrush(gradRect, this.BackColor, ChartUtils.DarkerColor(this.BackColor), LinearGradientMode.Vertical);
                        e.Graphics.FillRectangle(lBrush, 0, 0, this.Width, this.Height);// vp.Bounds);
                    }
                    else
                    {
                        SolidBrush sb = new SolidBrush(this.BackColor);
                        e.Graphics.FillRectangle(sb, 0, 0, this.Width, this.Height);
                    }
                }
            }

            if (BorderColor != Color.Transparent)
            {
                int thickness = 2;
                Pen sb = new Pen(this.BorderColor, thickness);
                e.Graphics.DrawRectangle(sb, 0, 0, this.Width, this.Height);
            }

            if ((Legend != "") && (LegendColor != Color.Transparent))
            {
                StringFormat format = new StringFormat();
                format.Alignment = StringAlignment.Far;
                format.LineAlignment = StringAlignment.Center;

                RectangleF rect = new RectangleF(0, 0, Width, Height);
                
                Font font = CoreUtils.SkinningDefs.TheInstance.GetFont(9, FontStyle.Bold);

                e.Graphics.DrawString(Legend, font, new SolidBrush(LegendColor), rect, format);

            }
        }
    }
}
