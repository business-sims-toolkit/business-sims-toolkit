using System;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Text;

namespace BaseUtils
{
	/// <summary>
	/// A label-style control for displaying text using the highest
	/// possible rendering quality.
	/// </summary>
	public class HiQualityLabel : Control
	{
		StringAlignment valign;
		StringAlignment halign;

		public HiQualityLabel()
		{
			this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.DoubleBuffer | ControlStyles.SupportsTransparentBackColor | ControlStyles.UserPaint, true);
			this.BackColor = Color.Transparent;
			this.valign = StringAlignment.Center;
			this.halign = StringAlignment.Near;
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			RenderLabel(e.Graphics);
			base.OnPaint (e);
		}

		void RenderLabel(Graphics g)
		{
			g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
			StringFormat sf = new StringFormat();
			sf.Alignment = halign;
			sf.LineAlignment = valign;
			g.DrawString(base.Text, base.Font, new SolidBrush(base.ForeColor), base.ClientRectangle, sf);
		}

		public StringAlignment VerticalAlignment
		{
			get { return valign; }
			set 
			{ 
				valign = value; 
				Invalidate();
			}
		}

		public StringAlignment HorizontalAlignment
		{
			get { return halign; }
			set 
			{ 
				halign = value; 
				Invalidate();
			}
		}

		protected override void OnFontChanged(EventArgs e)
		{
			Invalidate();
			base.OnFontChanged (e);
		}

		protected override void OnForeColorChanged(EventArgs e)
		{
			Invalidate();
			base.OnForeColorChanged (e);
		}

		protected override void OnTextChanged(EventArgs e)
		{
			Invalidate();
			base.OnTextChanged (e);
		}

		protected override void OnResize(EventArgs e)
		{
			Invalidate();
			base.OnResize (e);
		}

	}
}
