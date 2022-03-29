using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Collections;
using CommonGUI;
using CoreUtils;
using LibCore;

namespace Charts
{
	public class PieChartControl : FlickerFreePanel
	{
		protected class Sector
		{
			public double radius;
			public double angle0, angle1;

			public Sector (double radius, double angle0, double angle1)
			{
				this.radius = radius;
				this.angle0 = angle0;
				this.angle1 = angle1;
			}
		}

		protected ArrayList sectors;

		protected int px;
		protected int py;
		protected int prad;
		int psegs;
		int pbands;
		int pieAngleOffset;
        //draw the key or not
		bool keyrequired = true;

		protected Color[] pastelTones = new Color[10];
		ArrayList points1;
		ArrayList points1To;
		ArrayList points2;
		ArrayList labels;
		protected ArrayList segments;
		protected ArrayList segmentColours;
		Image CoreImage = null;
		bool DrawCoreImage = false;
		int CoreOffset = 0;
		public bool use_drop_shadow = true;

		Image BackImage = null;
		bool DrawBackImage = false;

		public int keyYOffset = 45;//25;
		public bool auto_translate = true;

		public PieChartControl()
		{
			this.px = 0;
			this.py = 0;
			this.prad = 100;
			this.psegs = 4;
			this.pbands = 0;
			pieAngleOffset = 0;

			Initialise();
		}

		public void SetBackColorOverride(Color newColor)
		{
			 this.BackColor = newColor;
		}

		void Initialise()
		{
			// Pastel colours
			pastelTones[0] = SkinningDefs.TheInstance.GetColorDataGivenDefault("pie_colour_0", Color.FromArgb(204, 160, 202));
			pastelTones[1] = SkinningDefs.TheInstance.GetColorDataGivenDefault("pie_colour_1", Color.FromArgb(242, 175, 201));
			pastelTones[2] = SkinningDefs.TheInstance.GetColorDataGivenDefault("pie_colour_2", Color.FromArgb(244, 241, 158));
			pastelTones[3] = SkinningDefs.TheInstance.GetColorDataGivenDefault("pie_colour_3", Color.FromArgb(114, 198, 219));
			pastelTones[4] = SkinningDefs.TheInstance.GetColorDataGivenDefault("pie_colour_4", Color.FromArgb(166, 217, 106));
			pastelTones[5] = SkinningDefs.TheInstance.GetColorDataGivenDefault("pie_colour_5", Color.FromArgb(189, 153, 121));
			pastelTones[6] = SkinningDefs.TheInstance.GetColorDataGivenDefault("pie_colour_6", Color.FromArgb(210, 129, 120));
			pastelTones[7] = SkinningDefs.TheInstance.GetColorDataGivenDefault("pie_colour_7", Color.FromArgb(102, 152, 121));
			pastelTones[8] = SkinningDefs.TheInstance.GetColorDataGivenDefault("pie_colour_8", Color.FromArgb(162, 153, 140));
			pastelTones[9] = SkinningDefs.TheInstance.GetColorDataGivenDefault("pie_colour_9", Color.FromArgb( 96, 149, 193));

			ClearData();
		}

		public void ClearData()
		{
			points1 = new ArrayList();
			points1To = new ArrayList();
			points2 = new ArrayList();
			labels = new ArrayList();
			segments = new ArrayList();
			segmentColours = new ArrayList();

			sectors = new ArrayList();
		}

		public void SetCoreImage(Image CoreImg, int OffsetSize)
		{
			CoreImage = CoreImg;
			DrawCoreImage = true;
			CoreOffset = OffsetSize;
		}

		public void SetBackImage(Image BackImg)
		{
			BackImage = BackImg;
			DrawBackImage = true;
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			e.Graphics.Clear(BackColor);
			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
			e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

			if (DrawBackImage)
			{
				e.Graphics.DrawImage(this.BackImage, 0, 0, this.BackImage.Width, this.BackImage.Height);
			}

			DrawPieSegments(e.Graphics, psegs);

			if (pbands > 0)
			{
				int brad = (prad - (1*CoreOffset)) / pbands ;
				for (int i = 0; i < pbands; i++)
				{
					//inline code
					int cx = px + prad - brad - CoreOffset;
					int cy = py + prad - brad - CoreOffset;
					e.Graphics.DrawEllipse(new Pen(Brushes.White, 1f), cx, cy, 2 * (brad + CoreOffset), 2 * (brad + CoreOffset));
					//old code
					//DrawBand(e.Graphics, brad);
					//brad += prad / pbands;
					brad += (prad - (1*CoreOffset)) / pbands;
				}
			}

			PointF[] p2 = (PointF[])points2.ToArray(typeof(PointF));

			if (p2.Length > 0)
			{
				Pen dpen = new Pen(Color.Black, 3f);
				dpen.DashStyle = DashStyle.Dash;

				//need at least 3 points to draw a polygon
				if (p2.Length > 2)
				{
					GraphicsPath path = new GraphicsPath();
					path.AddPolygon(p2);
					e.Graphics.DrawPath(dpen, path);
				}
			}

			PointF[] p1 = (PointF[])points1.ToArray(typeof(PointF));
			PointF[] p1to = (PointF[])points1To.ToArray(typeof(PointF));

			if (p1.Length > 0)
			{
				//need at least 3 points to make a polygon
				if (p1.Length > 2)
				{
					GraphicsPath path = new GraphicsPath();
					path.AddPolygon(p1);
					e.Graphics.DrawPath(new Pen(Color.Black, 1f), path);
					e.Graphics.FillPath(new SolidBrush(Color.FromArgb(150, 255, 255, 255)), path);
				}
				for (int i = 0; i < p1.Length; i++)
				{
					e.Graphics.DrawLine(new Pen(Color.Black, 1f), p1[i], p1to[i]);

				    var fudge = 1.02;
				    var x = (float) (p1[i].X + ((p1to[i].X - p1[i].X) * fudge));
				    var y = (float) (p1[i].Y + ((p1to[i].Y - p1[i].Y) * fudge));
					DrawTextBox(e.Graphics, x, y, px + prad, labels[i].ToString());
				}
			}

			//Drawing the Core Image (Should be last thing)
			if (DrawCoreImage)
			{
				if (CoreImage != null)
				{
					e.Graphics.DrawImage(this.CoreImage,503-80+42-220,318-80+42-220-2,515,515);
				}
			}

			//need more than one segments to require a key
			if (psegs > 1 && keyrequired)
			{
				DrawKey(e.Graphics);
			}

			base.OnPaint (e);
		}

		void DrawTextBox(Graphics g, float x, float y, float flipX, string text)
		{
            float fontSize = SkinningDefs.TheInstance.GetFloatData("maturity_label_font_size", 8);

            Font font;
            string newText = text;
            if (auto_translate)
            {
                font = ConstantSizeFont.NewFont(TextTranslator.TheInstance.GetTranslateFont("Tahoma"), fontSize);
                newText = TextTranslator.TheInstance.Translate(text);
            }
            else
            {
                font = SkinningDefs.TheInstance.GetFont(fontSize);
            }

            SizeF stringSize = g.MeasureString(newText, font, SizeF.Empty, StringFormat.GenericTypographic);
            float padding = 3;
            SizeF rectangleSize = new SizeF (stringSize.Width + (2 * padding), stringSize.Height + (2 * padding));

            if (x < flipX)
            {
                x -= stringSize.Width;
            }

            x = (float) Algorithms.Maths.Clamp(x, 0, Width - stringSize.Width - 2);
			y = (float) Algorithms.Maths.Clamp(y, rectangleSize.Height / 2, Height - (rectangleSize.Height / 2));

            Rectangle rectangle = new Rectangle ((int) (x - padding), (int) (y - (rectangleSize.Height / 2)), (int) rectangleSize.Width, (int) rectangleSize.Height);

		    if (SkinningDefs.TheInstance.GetBoolData("maturity_show_boxes_round_labels", true))
		    {
		        GraphicsContainer gc = g.BeginContainer();
		        g.SmoothingMode = SmoothingMode.None;
		        g.FillRectangle(Brushes.LightYellow, rectangle);
		        g.DrawRectangle(Pens.Black, rectangle);
		        g.EndContainer(gc);
		    }

		    g.DrawString(newText, font, Brushes.Black, (int) x, (int) (y + padding - (rectangleSize.Height / 2)));
		}

		void ReCalcPoints()
		{
			//work out where the points go based on new offset
		}

		public void AddPoint(int seg, int segs, int pt, int totalPts, int val, string text)
		{
			points1.Add(CalcPoint(seg, segs, pt, totalPts, val));
			points1To.Add(CalcPoint(seg, segs, pt, totalPts, (prad - this.CoreOffset) + (prad / 5)));
			labels.Add(text);
		}

		public void AddPoint2(int seg, int segs, int pt, int totalPts, int val)
		{
			points2.Add(CalcPoint(seg, segs, pt, totalPts, val));
		}

		public void AddSegment(string text)
		{
			AddSegment(text, Color.Transparent);
		}

		public void AddSegment (string text, Color colour)
		{
			segments.Add(text);
			psegs = segments.Count;

			if (colour == Color.Transparent)
			{
				segmentColours.Add(pastelTones[(psegs - 1) % pastelTones.Length]);
			}
			else
			{
				segmentColours.Add(colour);
			}
		}

		void DrawPieSegments(Graphics g, int segs)
		{
			if (prad > 0)
			{
				sectors.Clear();

				if (use_drop_shadow)
				{
					EllipseDropShadow(g, px, py, prad + (prad / 20));
				}

				float segInc = 360f / segs;

				for (int i = 0; i < segs; i++)
				{
					float angle = 270 + (i * segInc);

					sectors.Add(new Sector(prad, angle, angle + segInc));

					Color colour = GetSectorColour(i);

					g.FillPie(new SolidBrush(colour), px, py, 2 * prad, 2 * prad, angle, segInc);
				}
			}
		}

		protected virtual Color GetSectorColour (int sectorIndex)
		{
			if ((segmentColours != null) && (segmentColours.Count > sectorIndex))
			{
				return (Color) segmentColours[sectorIndex];
			}

			return pastelTones[sectorIndex % pastelTones.Length];
		}

		//Determine the Number of Pixels that a score relates to (10 is a max)
		//This relates a score value into a number of pixels 
		//This class should own this calculation rather than piechart
		public int CalcPieScore(int score)
		{
			int usable_size = this.PieRadius - (1*this.CoreOffset);
			return ((score * usable_size)/ 10);
		}

		PointF CalcPoint(int seg, int segs, int pt, int totalPts, int val)
		{
			int cx = px + prad;
			int cy = py + prad;
			float segInc = 360f / segs;
			float pointInc = segInc / totalPts;
			float angle;

			if (totalPts == 1)
				angle = 270f + ((seg - 1) * segInc) + (segInc / 2);
			else
				angle = 270f + ((seg - 1) * segInc) + (pt * pointInc) - (pointInc / 2);

			angle += pieAngleOffset;

			float nx = (float)(cx + Math.Cos(angle * (Math.PI / 180)) * (val + this.CoreOffset));
			float ny = (float)(cy + Math.Sin(angle * (Math.PI / 180)) * (val + this.CoreOffset));

			return new PointF(nx, ny);
		}

		void EllipseDropShadow(Graphics g, int x, int y, int radius)
		{
			if (radius > 0)
			{
				Color darkShadow = Color.FromArgb(255, Color.Black);
				Color lightShadow = Color.FromArgb(0, Color.Black);

				GraphicsPath gp = new GraphicsPath();
				gp.AddEllipse(x, y, 2 * radius, 2 * radius);

				PathGradientBrush pgb = new PathGradientBrush(gp);
				pgb.CenterColor = darkShadow;
				pgb.SurroundColors = new Color[] { lightShadow };

				g.FillEllipse(pgb, x, y, 2 * radius, 2 * radius);
			}
		}

		void DrawKey(Graphics g)
		{
			if (segments.Count > 0)
			{
				Font font = SkinningDefs.TheInstance.GetFont(8, FontStyle.Bold);
				if (auto_translate)
				{
					font.Dispose();
					font = ConstantSizeFont.NewFont(TextTranslator.TheInstance.GetTranslateFont("Tahoma"), 8f, FontStyle.Bold);
				}

				int rowHeight = 16;
				int maxWidth = 0;

				for (int i = 0; i < segments.Count; i++)
				{
					SizeF size = g.MeasureString(segments[i].ToString(), font);
					if (size.Width > maxWidth)
						maxWidth = (int)size.Width;
				}

				float width = 18 + maxWidth + 5;
				float height = 5 + (segments.Count * rowHeight);
				
				int x = 5;
				int y = keyYOffset;

				Rectangle borderRect = new Rectangle(x, y, (int)width + 8, (int)height + 1);

			    if (SkinningDefs.TheInstance.GetBoolData("maturity_show_box_round_key", true))
			    {
				    g.FillRectangle(new SolidBrush(Color.FromArgb(200, 255, 255, 255)), borderRect);
			        g.DrawRectangle(new Pen(Color.Black, 1f), borderRect);
			    }

                for (int i = 0; i < segments.Count; i++)
				{
					Rectangle rect = new Rectangle(x + 5, y + 5, 12, 12);
					Brush b = new SolidBrush(GetSectorColour(i));

					g.FillRectangle(b, rect);

				    if (SkinningDefs.TheInstance.GetBoolData("maturity_show_boxes_round_key_swatches", true))
				    {
				        g.DrawRectangle(new Pen(Color.Black, 1f), rect);
				    }

				    string text = segments[i].ToString();
					string newText = text;
					if (auto_translate)
					{
						newText = TextTranslator.TheInstance.Translate(text);
					}
					g.DrawString(newText, font, Brushes.Black, x + 23, y + 5);
					y += rowHeight;
				}
			}
		}

        public bool KeyRequired
        {
            get { return keyrequired; }
            set { keyrequired = value; }
        }

		public int PieX
		{
			get { return px; }
			set { px = value; Invalidate(); ReCalcPoints();}
		}

		public int PieY
		{
			get { return py; }
			set { py = value; Invalidate(); ReCalcPoints(); }
		}

		public int PieRadius
		{
			get { return prad; }
			set { prad = value; Invalidate(); }
		}

		public int PieBands
		{
			get { return pbands; }
			set { pbands = value; Invalidate(); }
		}

		public int PieAngleOffset
		{
			get
			{
				return pieAngleOffset;
			}

			set
			{
				pieAngleOffset = value;
				Invalidate();
			}
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);
			Invalidate();
		}
	}
}