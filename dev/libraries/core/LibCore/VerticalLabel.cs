using System;
using System.Drawing;
using System.Windows.Forms;

namespace LibCore
{
	public class VerticalLabel : VisualPanel
	{
		protected string text;
		protected SizeF sf;
		protected Font font = ConstantSizeFont.NewFont("Arial", 12);
		protected Color colour = Color.Black;
		protected Brush drawingBrush = new SolidBrush(Color.Black);

        public bool UseAlternatePaintMethod = false;

		public override string Text
		{
			get
			{
				return text;
			}
			
			set
			{
				text = value;
				if(null != font)
				{
					sf = MeasureString( font, text );
				}
				this.Invalidate();
			}
		}

		public void setDrawingBrushColor(Color newColor)
		{
			if (drawingBrush != null)
			{
				drawingBrush.Dispose();
			}
			drawingBrush = new SolidBrush(newColor);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			//e.Graphics.Transform.RotateAt(45, new PointF(Width/2,Height/2));
			e.Graphics.RotateTransform(-90);
			//
            
            if (UseAlternatePaintMethod)
            {
                if (sf.Width < Height)
                {
                    float diff = Height - sf.Width;
                    e.Graphics.DrawString(text, this.Font, drawingBrush, (int)((diff / 2) - Height), Width / 8f);
                }
                else
                {
                    e.Graphics.DrawString(text, this.Font, drawingBrush, -Height, 0);
                }
            }
            else
            {
                if (sf.Width < Height)
                {
                    float diff = Height - sf.Width;
                    e.Graphics.DrawString(text, this.Font, drawingBrush, (int)((diff / 2) - Height), 0);
                }
                else
                {
                    e.Graphics.DrawString(text, this.Font, drawingBrush, -Height, 0);
                }
            }

			
		}
		
		public override Font Font
		{
			get
			{
				return font;
			}
			
			set
			{
				font = value;
				if(null != font)
				{
					sf = MeasureString( font, text );
				}
				this.Invalidate();
			}
		}
		
		public VerticalLabel()
		{
			sf = new SizeF();
			sf.Width = 0;
			sf.Height = 0;
			Resize += VerticalLabel_Resize;
			//
			this.BackColor = Color.Transparent;
		}

		void VerticalLabel_Resize(object sender, EventArgs e)
		{
			this.Invalidate();
		}
	
	}
}
