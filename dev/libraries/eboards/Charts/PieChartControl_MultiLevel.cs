using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Collections;

using CoreUtils;
using LibCore;

namespace Charts
{
	/// <summary>
	/// This is a seperate development of the standard Pie chart to show multiple levels at once
	/// It is currently built to all recorded levels as overlapping shaded areas
	/// </summary>
	public class PieChartControl_MultiLevel : Control
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
		ArrayList[] points = new ArrayList[5];
		ArrayList[] pointsTo = new ArrayList[5];
		ArrayList labels;
		protected ArrayList segments;
		protected ArrayList segmentColours;
		Image CoreImage = null;
		bool DrawCoreImage = false;
		int CoreOffset = 0;
		public bool use_drop_shadow = true;

		public int keyYOffset = 45;//25;
		public bool auto_translate = true;
		int TopRound = 1;

		public PieChartControl_MultiLevel()
		{
			this.px = 0;
			this.py = 0;
			this.prad = 100;
			this.psegs = 4;
			this.pbands = 0;
			pieAngleOffset = 0;
			Initialise();
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
			// Set control styles
			SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.DoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
		}

		public void ClearData()
		{
			TopRound = 1;

			for (int step = 0; step < 5; step++)
			{ 
				points[step] = new ArrayList();
				pointsTo[step] = new ArrayList();
			}
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

		protected override void OnPaint(PaintEventArgs e)
		{
			e.Graphics.Clear(this.BackColor);
			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
			e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

			DrawPieSegments(e.Graphics, psegs);

			if (pbands > 0)
			{
				int brad = (prad - (1*CoreOffset)) / pbands ;
				for (int i = 0; i < pbands; i++)
				{
					int cx = px + prad - brad - CoreOffset;
					int cy = py + prad - brad - CoreOffset;
					e.Graphics.DrawEllipse(new Pen(Brushes.White, 1f), cx, cy, 2 * (brad + CoreOffset), 2 * (brad + CoreOffset));
					brad += (prad - (1*CoreOffset)) / pbands;
				}
			}

			int PenStyleIndex = 0;
			int DrawingIndex = 0;
			for (int step = 0; step < (TopRound); step++)
			{
				PointF[] p = (PointF[])points[step].ToArray(typeof(PointF));
				PointF[] pTo = (PointF[])pointsTo[step].ToArray(typeof(PointF));

				if (p.Length > 0)
				{
					Color fillColor = Color.FromArgb(150, 255, 255, 255);
					Pen dpen = new Pen(Color.Black, 3f);
					dpen.DashStyle = DashStyle.Dash;

					DrawingIndex = (TopRound-1) - step;
					switch (DrawingIndex)
					{
						case 0://Most Recent Round 
							dpen.DashStyle = DashStyle.Solid;
							fillColor = Color.FromArgb(160, 255, 255, 255);
							break;
						case 1://Second Most Recent Round 
							dpen.DashStyle = DashStyle.Dash;
							fillColor = Color.FromArgb(120, 255, 255, 255);
							break;
						case 2://Third Most Recent Round 
							dpen.DashStyle = DashStyle.DashDot;
							fillColor = Color.FromArgb(80, 255, 255, 255);
							break;
						case 3://Fourth Most Recent Round 
							dpen.DashStyle = DashStyle.DashDotDot;
							fillColor = Color.FromArgb(40, 255, 255, 255);
							break;
						case 4://fifth Most Recent Round 
							dpen.DashStyle = DashStyle.Dot;
							fillColor = Color.FromArgb(0, 255, 255, 255);
							break;
					}
					//need at least 3 points to draw a polygon
					if (p.Length > 2)
					{
						GraphicsPath path = new GraphicsPath();
						path.AddPolygon(p);
						e.Graphics.DrawPath(dpen, path);
						e.Graphics.FillPath(new SolidBrush(fillColor), path);
					}
					if (DrawingIndex == 0)
					{
						for (int i = 0; i < p.Length; i++)
						{
							e.Graphics.DrawLine(new Pen(Color.Black, 1f), p[i], pTo[i]);
							DrawTextBox(e.Graphics, pTo[i].X, pTo[i].Y, px + prad, labels[i].ToString());
						}
					}
					dpen.Dispose();
					PenStyleIndex++;
					DrawingIndex++;
				}
			}
			//Drawing the Core Image (Should be last thing, if we need it)
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
			Font font = ConstantSizeFont.NewFont("Tahoma", 8f);
			if (auto_translate)
			{
				font.Dispose();
				font = ConstantSizeFont.NewFont(TextTranslator.TheInstance.GetTranslateFont("Tahoma"), 8f);
			}
			string newText = text;
			if (auto_translate)
			{
				newText = TextTranslator.TheInstance.Translate(text);
			}

			SizeF ss = g.MeasureString(newText, font);

			if (x < flipX) x -= ss.Width;
			float cy = y - ((ss.Height + 4) / 2);

			GraphicsContainer gc = g.BeginContainer();
			g.SmoothingMode = SmoothingMode.None;
			g.FillRectangle(Brushes.LightYellow, x, cy, ss.Width + 3, ss.Height + 4);
			g.DrawRectangle(new Pen(Color.Black, 1f), x, cy, ss.Width + 3, ss.Height + 4);
			g.EndContainer(gc);
			g.DrawString(newText, font, Brushes.Black, x + 2, cy + 2);
		}

		void ReCalcPoints()
		{
			//work out where the points go based on new offset
		}

		public void AddPoint(int round, int seg, int segs, int pt, int totalPts, int val, string text)
		{
			if (round > TopRound)
			{
				TopRound = round;
			}

			if (points[round - 1] != null)
			{
				points[round - 1].Add(CalcPoint(seg, segs, pt, totalPts, val));
				pointsTo[round - 1].Add(CalcPoint(seg, segs, pt, totalPts, (prad - this.CoreOffset) + (prad / 5)));
				labels.Add(text);
			}
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
			sectors.Clear();

			if (use_drop_shadow)
			{
				EllipseDropShadow(g, px, py, prad + (prad / 20));
			}

			float segInc = 360f / segs;

			for (int i=0; i<segs; i++)
			{
				float angle = 270 + (i * segInc);

				sectors.Add(new Sector (prad, angle, angle + segInc));

				Color colour = GetSectorColour(i);

				g.FillPie(new SolidBrush(colour), px, py, 2 * prad, 2 * prad, angle, segInc);
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
	//		int segs = this.segments.Count;
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
				
				int x = 5;//(int)(this.Size.Width - maxWidth - 60);
				int y = keyYOffset;

				Rectangle borderRect = new Rectangle(x, y, (int)width + 8, (int)height + 1);
				g.FillRectangle(new SolidBrush(Color.FromArgb(200, 255, 255, 255)), borderRect);
				g.DrawRectangle(new Pen(Color.Black, 1f), borderRect);

				for (int i = 0; i < segments.Count; i++)
				{
					Rectangle rect = new Rectangle(x + 5, y + 5, 12, 12);
					Brush b = new SolidBrush(GetSectorColour(i));

					g.FillRectangle(b, rect);
					g.DrawRectangle(new Pen(Color.Black, 1f), rect);
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
	}
}