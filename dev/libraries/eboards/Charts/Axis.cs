using System;
using System.Drawing;
using System.Windows.Forms;

using CoreUtils;
using LibCore;

namespace Charts
{
	/// <summary>
	/// Summary description for Axis.
	/// </summary>
	public abstract class Axis : VisualPanel
	{
		public Control axisTitle;
		protected Font _font;
		protected int _min, _max, _steps;
		public bool showGrid = true;
		protected ContentAlignment align = ContentAlignment.MiddleCenter;
		protected PrintLabel[] labels = new PrintLabel[0];

		protected string LabelAlignment;
		protected bool marksVisible = true;

		protected bool auto_translate = true;

		protected Color textColour = Color.Black;

		bool omitTop = false;

		public bool Marked
		{
			set
			{
				foreach(Control c in labels)
				{
					c.Visible = value;
				}
				marksVisible = value;
			}
		}

		public int Min { get { return _min; } }
		public int Max
		{
			get
			{
				//if(_max > 0) return _max-1;
				return _max;
			}
		}
		public int Steps { get { return _steps; } }

		public Axis()
		{
			Resize += Axis_Resize;
		}

		public int NumLabels
		{
			get
			{
				return labels.Length;
			}
		}

		public override Font Font
		{
			set
			{
				if(value != null)
				{
					if(auto_translate)
					{
						string fstr = value.FontFamily.GetName(409);
						_font = ConstantSizeFont.NewFont( TextTranslator.TheInstance.GetTranslateFont(fstr), SkinningDefs.TheInstance.GetFloatData("gantt_y_axis_font_size", TextTranslator.TheInstance.GetTranslateFontSize(fstr,(int)value.Size)), value.Style);
					}
					else
					{
						_font = value;
					}	
					axisTitle.Font = _font;
					//
					foreach(Label l in labels)
					{
						l.Font = _font;
					}
				}
			}
		}

		protected Color [] stripeColours = null;
        protected bool stripedText = false;

		public void SetStriped (params Color [] colours)
		{
			stripeColours = colours;
			DoSize();
		}

		public void SetUnStriped ()
		{
			stripeColours = null;
			DoSize();
		}

        public void SetStripedText()
        {
            stripedText = true;
            DoSize();
        }

        public void SetUnStripedText()
        {
            stripedText = false;
            DoSize();
        }

		public Font TitleFont
		{
			set
			{
				if(value != null)
				{
					axisTitle.Font = value;
				}
			}
		}

		public void SetColour(Color c)
		{
			this.SetStroke(2, c);
		}

		public void SetTextColour (Color c)
		{
			textColour = c;
		    axisTitle.ForeColor = textColour;
			DoSize();
		}

		public bool SetLabel(int i, string text)
		{
			if( (i < 0) || (i>labels.Length-1)) return false;

			PrintLabel pl = labels[i] as PrintLabel;
			pl.Text = text;
			return true;
		}

		public void SetLabelAlignment(string alignment)
		{
			LabelAlignment = alignment;
			DoSize();
		}

		public void OmitTop (bool omit)
		{
			omitTop = omit;
			UpdateOmittedTopLabel();
		}

		public virtual void SetRange(int min, int max, int step)
		{
			this.SuspendLayout();

			foreach(Label l in labels)
			{
				Controls.Remove(l);
				l.Dispose();
			}
			//
			_min = min;
			_max = max;
//			_steps = steps;
			//
			_steps = (max-min)/step;
			//
			if(_steps > 0)
			{
				labels = new PrintLabel[_steps];//-1];
				//
				for(int i=1; i<=_steps; ++i)
				{
					int num = min + step*i;
					labels[i-1] = new PrintLabel();
					labels[i-1].Text = CONVERT.ToStr(num);
					if(null != _font)
					{
						labels[i-1].Font = _font;
					}
					labels[i-1].TextAlign = align;
					//
					labels[i-1].Visible = marksVisible;
					//
					Controls.Add(labels[i-1]);
				}
			}
			//
			this.ResumeLayout(false);
			//
			axisTitle.BringToFront();

			UpdateOmittedTopLabel();
			//
			DoSize();
			this.Invalidate();
		}

		protected void UpdateOmittedTopLabel ()
		{
			if (labels.Length > 0)
			{
				labels[labels.Length - 1].Visible = ! omitTop;
			}
		}

		protected abstract void DoSize();

		void Axis_Resize(object sender, EventArgs e)
		{
			Invalidate();
			DoSize();
		}
	}
}