using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Collections;

using LibCore;

namespace BaseUtils
{
	/// <summary>
	/// A control for displaying data as
	/// an area chart.
	/// </summary>
	public class PieChartControl : Control
	{
		int px;
		int py;
		int prad;
		int psegs;
		int pbands;

		Color[] pastelTones = new Color[10];
		ArrayList points1;
		ArrayList points1To;
		ArrayList points2;
		ArrayList labels;
		ArrayList segments;

		public PieChartControl()
		{
			this.px = 0;
			this.py = 0;
			this.prad = 100;
			this.psegs = 4;
			this.pbands = 0;

			Initialise();
		}

		void Initialise()
		{
			// Pastel colours
			pastelTones[0] = Color.FromArgb(244, 241, 158);
			pastelTones[1] = Color.FromArgb(242, 175, 201);
			pastelTones[2] = Color.FromArgb(204, 160, 202);
			pastelTones[3] = Color.FromArgb(114, 198, 219);
			pastelTones[4] = Color.FromArgb(166, 217, 106);
			pastelTones[5] = Color.FromArgb(189, 153, 121);
			pastelTones[6] = Color.FromArgb(210, 129, 120);
			pastelTones[7] = Color.FromArgb(102, 152, 121);
			pastelTones[8] = Color.FromArgb(162, 153, 140);
			pastelTones[9] = Color.FromArgb(96, 149, 193);

			// Initialise arrays
			segments = new ArrayList();
			ClearData();

			// Set control styles
			SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.DoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
		}

		public void ClearData()
		{
			// Initialise arrays
			points1 = new ArrayList();
			points1To = new ArrayList();
			points2 = new ArrayList();
			labels = new ArrayList();
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			e.Graphics.Clear(this.BackColor);
			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
			e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
	
			DrawPieSegments(e.Graphics, psegs);

			if (pbands > 0)
			{
				int brad = prad / pbands;
				for (int i = 0; i < pbands; i++)
				{
					DrawBand(e.Graphics, brad);
					brad += prad / pbands;
				}
			}

			PointF[] p2 = (PointF[])points2.ToArray(typeof(PointF));

			if (p2.Length > 0)
			{
				Pen dpen = new Pen(Color.Black, 3f);
				dpen.DashStyle = DashStyle.Dash;

				GraphicsPath path = new GraphicsPath();
				path.AddPolygon(p2);
				e.Graphics.DrawPath(dpen, path);
			}

			PointF[] p1 = (PointF[])points1.ToArray(typeof(PointF));
			PointF[] p1to = (PointF[])points1To.ToArray(typeof(PointF));

			if (p1.Length > 0)
			{
				GraphicsPath path = new GraphicsPath();
				path.AddPolygon(p1);
				e.Graphics.DrawPath(new Pen(Color.Black, 1f), path);
				e.Graphics.FillPath(new SolidBrush(Color.FromArgb(150, 255, 255, 255)), path);

				for (int i = 0; i < p1.Length; i++)
				{
					e.Graphics.DrawLine(new Pen(Color.Black, 1f), p1[i], p1to[i]);
					DrawTextBox(e.Graphics, p1to[i].X, p1to[i].Y, px + prad, labels[i].ToString());
				}
			}

			DrawKey(e.Graphics);

			base.OnPaint (e);
		}

		void DrawTextBox(Graphics g, float x, float y, float flipX, string text)
		{
			Font font = ConstantSizeFont.NewFont("Tahoma", 8f);
			SizeF ss = g.MeasureString(text, font);

			if (x < flipX) x -= ss.Width;
			float cy = y - ((ss.Height + 4) / 2);

			GraphicsContainer gc = g.BeginContainer();
			g.SmoothingMode = SmoothingMode.None;
			g.FillRectangle(Brushes.LightYellow, x, cy, ss.Width + 3, ss.Height + 4);
			g.DrawRectangle(new Pen(Color.Black, 1f), x, cy, ss.Width + 3, ss.Height + 4);
			g.EndContainer(gc);
			g.DrawString(text, font, Brushes.Black, x + 2, cy + 2);
		}

		public void AddPoint(int seg, int segs, int pt, int totalPts, int val, string text)
		{
			points1.Add(CalcPoint(seg, segs, pt, totalPts, val));
			points1To.Add(CalcPoint(seg, segs, pt, totalPts, prad + (prad / 5)));
			labels.Add(text);
		}

		public void AddPoint2(int seg, int segs, int pt, int totalPts, int val)
		{
			points2.Add(CalcPoint(seg, segs, pt, totalPts, val));
		}

		public void AddSegment(string text)
		{
			segments.Add(text);
			psegs = segments.Count;
		}

		void DrawPieSegments(Graphics g, int segs)
		{
			EllipseDropShadow(g, px, py, prad + (prad / 20));
			int currSeg = 0;
			float segInc = 360f / segs;

			for (int i=0; i<segs; i++)
			{
				g.FillPie(new SolidBrush(pastelTones[currSeg]), px, py, 2 * prad, 2 * prad, 270f + (i * segInc), segInc);
				if (++currSeg > pastelTones.Length - 1)
					currSeg = 0;
			}
		}

		void DrawBand(Graphics g, int brad)
		{
			int cx = px + prad - brad;
			int cy = py + prad - brad;
			g.DrawEllipse(new Pen(Brushes.White, 1f), cx, cy, 2 * brad, 2 * brad);
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

			float nx = (float)(cx + Math.Cos(angle * (Math.PI / 180)) * val);
			float ny = (float)(cy + Math.Sin(angle * (Math.PI / 180)) * val);

			return new PointF(nx, ny);
		}

		void EllipseDropShadow(Graphics g, int x, int y, int radius)
		{
			Color darkShadow = Color.FromArgb(255, Color.Black);
			Color lightShadow = Color.FromArgb(0, Color.Black);

			GraphicsPath gp = new GraphicsPath();
			gp.AddEllipse(x, y, 2 * radius, 2 * radius);

			PathGradientBrush pgb = new PathGradientBrush(gp);
			pgb.CenterColor = darkShadow;
			pgb.SurroundColors = new Color[] {lightShadow};

			g.FillEllipse(pgb, x, y, 2 * radius, 2 * radius);
		}

		void DrawKey(Graphics g)
		{
			if (segments.Count > 0)
			{
				Font font = ConstantSizeFont.NewFont("Tahoma", 8f, FontStyle.Bold);
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
				
				int currSeg = 0;
				int x = (int)(this.Size.Width - maxWidth - 60);
				int y = 25;

				Rectangle borderRect = new Rectangle(x, y, (int)width + 8, (int)height + 1);
				g.FillRectangle(new SolidBrush(Color.FromArgb(200, 255, 255, 255)), borderRect);
				g.DrawRectangle(new Pen(Color.Black, 1f), borderRect);

				for (int i = 0; i < segments.Count; i++)
				{
					Rectangle rect = new Rectangle(x + 5, y + 5, 12, 12);
					Brush b = new SolidBrush(pastelTones[currSeg]);

					g.FillRectangle(b, rect);
					g.DrawRectangle(new Pen(Color.Black, 1f), rect);
					g.DrawString(segments[i].ToString(), font, Brushes.Black, x + 23, y + 5);
					y += rowHeight;

					if (++currSeg > pastelTones.Length - 1)
						currSeg = 0;
				}
			}
		}

		public int PieX
		{
			get { return px; }
			set { px = value; Invalidate(); }
		}

		public int PieY
		{
			get { return py; }
			set { py = value; Invalidate(); }
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
	}

}
