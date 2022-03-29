using System;
using System.Drawing;
using System.Windows.Forms;

using LibCore;
using CoreUtils;


namespace CommonGUI
{
	/// <summary>
	/// Summary description for DialMonitor.
	/// </summary>
	public class HalfMoonDialMonitor : BasePanel, ITimedClass
	{
		StopControlledTimer smoothTimer;

		Image dial = Repository.TheInstance.GetImage(
			AppInfo.TheInstance.Location + "\\images\\dials\\dial.png") ;

		Label DialLabel;
		Label DialTitle;

		int _realVal;
		int _dialVal;
		int _targetVal;
		Point needleFixedPoint = new Point(100,40);
		int needle_originX = 100;
		int needle_originY = 40;

		int needle_x1=0;
		int needle_y1=0;
		int needle_x2=0;
		int needle_y2=0;
		int needle_radiusInner = 27;
		int needle_radiusOuter = 50;
		Pen needle_pen;

		Boolean ScalingOn = true;
		int MaxDeflectionValue = 100;

		public int DialVal 
		{
			get { return _realVal; }
			set 
			{ 
				_realVal = value;
				if (ScalingOn)
				{
					if (value > MaxDeflectionValue)
					{
						_targetVal = 100;
						//System.Diagnostics.Debug.WriteLine("HM A val:"+value.ToString() + "  tar:"+_targetVal.ToString());
					}
					else
					{
						_targetVal = ((value) * 100)/MaxDeflectionValue; 
						//System.Diagnostics.Debug.WriteLine("HM B val:"+value.ToString() + "  tar:"+_targetVal.ToString());
					}
				}
				else
				{	
					_targetVal = value; 
					//System.Diagnostics.Debug.WriteLine("HM C val:"+value.ToString() + "  tar:"+_targetVal.ToString());
				}
			}
		}

		public HalfMoonDialMonitor(Random _random, int newMaxDeflectionValue, int StartValue,
			string title,  Image BackImage, Color needleColor)
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.DoubleBuffer,true);
			SetStyle(ControlStyles.UserPaint, true);

			needle_pen = new Pen(needleColor, 3);

			//this.Size = new Size(78,78);
			BackColor = Color.Black;
			if (BackImage != null)
			{
				BackgroundImage = BackImage;
			}

			MaxDeflectionValue = newMaxDeflectionValue;
			_dialVal = StartValue;

			DialTitle = new Label();
			DialTitle.Size = new Size(100,18);
			DialTitle.TextAlign = ContentAlignment.MiddleLeft;
			DialTitle.Location = new Point(0,85-18);
			//DialTitle.BackColor = Color.Gray;
			DialTitle.ForeColor = Color.White;
			DialTitle.Text = title;
			Controls.Add(DialTitle);

			DialLabel = new Label();
			DialLabel.Size = new Size(25,18);
			DialLabel.TextAlign = ContentAlignment.MiddleCenter;
			DialLabel.Location = new Point(200-25,85-18);
			DialLabel.BackColor = Color.White;
			DialLabel.ForeColor = Color.Black;
			Controls.Add(DialLabel);

			smoothTimer = new StopControlledTimer();
			smoothTimer.Interval = 100;
			smoothTimer.Tick +=	smoothTimer_Tick;

			TimeManager.TheInstance.ManageClass(this);
		}

		/// <summary>
		/// Dispose...
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose(bool disposing)
		{
			if(disposing)
			{
				TimeManager.TheInstance.UnmanageClass(this);
				if (smoothTimer != null)
				{
					smoothTimer.Tick -=	smoothTimer_Tick;
					smoothTimer.Dispose();
				}
				if (needle_pen != null)
				{
					needle_pen.Dispose();
				}
			}

			base.Dispose(disposing);
		}

		public void ChangeTitleVisibility(Boolean visible)
		{
			DialTitle.Visible = visible;
		}

		public void ChangeDisplayValueVisibility(Boolean visible)
		{
			DialLabel.Visible = visible;
		}

		public void ChangeDisplayValueBackColor(Color NewColor)
		{
			DialLabel.BackColor = NewColor;
		}

		public void ChangeDisplayValuePosition (int top, int bottom) 
		{
			Point pt = new Point(top,bottom);
			DialLabel.Location = pt;
		}

//		public void SetScaleFactorAndDirection(int Scale, Boolean EnableScaling)
//		{
//			divide_scale = EnableScaling;
//			scalefactor = Scale;
//		}

		public void ChangeDialFixedEndPosition (int x, int y) 
		{
			needleFixedPoint = new Point(x,y);
			needle_originX = x;
			needle_originY = y;
		}

		void GenerateLinePoints(int inval, int originX, int originY,
			int radiusInner, int radiusOuter, out int x1, out int y1, out int x2, out int y2)
		{
			double angle = 0.0f;
			double RadInner = (double) radiusInner;
			double RadOuter = (double) radiusOuter;
			double orgX = (double) originX;
			double orgY = (double) originY; 

			x1=0;
			y1=0;
			x2=0;
			y2=0;

			if (inval>=50)
			{
				angle = (((double)100-(double)inval)*(double)180)/(double)100;
				x1 = (int)((orgX)+(RadInner*Math.Cos(angle*Math.PI/180)));
				y1 = (int)((orgY)-(RadInner*Math.Sin(angle*Math.PI/180)));

				x2 = (int)((orgX)+(RadOuter*Math.Cos(angle*Math.PI/180)));
				y2 = (int)((orgY)-(RadOuter*Math.Sin(angle*Math.PI/180)));
			}
			else
			{
				angle = ((((double)100-(double)inval)*(double)180)/(double)100)-(double)90;
				x1 = (int)((orgX)-(RadInner*Math.Sin(angle*Math.PI/180)));
				y1 = (int)((orgY)-(RadInner*Math.Cos(angle*Math.PI/180)));

				x2 = (int)((orgX)-(RadOuter*Math.Sin(angle*Math.PI/180)));
				y2 = (int)((orgY)-(RadOuter*Math.Cos(angle*Math.PI/180)));
			}
		}



		protected override void OnPaint(PaintEventArgs e)
		{
			e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
			//e.Graphics.FillRectangle(Brushes.Black,0,0,this.Width, this.Height);
			if (BackgroundImage != null)
			{
				e.Graphics.DrawImage(BackgroundImage,0,0,BackgroundImage.Width,BackgroundImage.Height);
				//e.Graphics.DrawImageUnscaled(BackgroundImage,0,0,BackgroundImage.Width,BackgroundImage.Height);
			}

			GenerateLinePoints(_dialVal, needle_originX, needle_originY, needle_radiusInner, 
				needle_radiusOuter, out needle_x1, out needle_y1, out needle_x2, out needle_y2);

			e.Graphics.DrawLine(needle_pen, needle_x1, needle_y1, needle_x2, needle_y2);

			//e.Graphics.ResetTransform();
			//e.Graphics.TranslateTransform(needleFixedPoint.X, needleFixedPoint.Y);
			//e.Graphics.RotateTransform((float)(Math.Round(val * 1.8) - 90));
			//e.Graphics.TranslateTransform(-4,-44);
			//e.Graphics.DrawImage(dial,0,0,5,44);
		}

		void smoothTimer_Tick(object sender, EventArgs e)
		{
			int diff = Math.Abs(_targetVal - _dialVal);
			int delta = 1;

			if (diff>1)
			{
				delta = 3;
			}

			if(_targetVal < _dialVal)
			{
				_dialVal = _dialVal - delta;
			}
			else 
			{
				if(_targetVal > _dialVal)
				{
					_dialVal = _dialVal + delta;
				}
			}
			DialLabel.Text = CONVERT.ToStr(_realVal);

//			if(this.divide_scale)
//			{
//				DialLabel.Text = CONVERT.ToStr(_dialVal / 5);
//			}
//			else 
//			{
//				DialLabel.Text = CONVERT.ToStr(_dialVal);
//			}

			Invalidate();
		}

		#region ITimedClass Members

		public void Start()
		{
			smoothTimer.Start();
		}

		public void FastForward(double timesRealTime)
		{
			// TODO:  Add BouncyNodeMonitor.FastForward implementation
		}

		public void Reset()
		{
			// TODO:  Add BouncyNodeMonitor.Reset implementation
		}

		public void Stop()
		{
			smoothTimer.Stop();
		}

		#endregion
	}
}
