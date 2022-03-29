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
	public class DialMonitor : BasePanel, ITimedClass
	{
		StopControlledTimer smoothTimer;

		Image dial = Repository.TheInstance.GetImage(
			AppInfo.TheInstance.Location + "\\images\\dials\\dial.png") ;

		Label DialLabel;

		bool divide_scale;

		int _dialVal;
		int _targetVal;
		public  int DialVal 
		{
			get { return _dialVal;    }
			set { _targetVal = value; }
		}

		public DialMonitor(Random _random, bool divide_scale)
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.DoubleBuffer,true);
			SetStyle(ControlStyles.UserPaint, true);

			this.divide_scale = divide_scale;

			Size = new Size(78,78);
			BackColor = Color.Black;

			if(divide_scale)
			{
				BackgroundImage = Repository.TheInstance.GetImage(
					AppInfo.TheInstance.Location + "\\images\\dials\\dial_small_20.png") ;
			}
			else
			{
				BackgroundImage = Repository.TheInstance.GetImage(
					AppInfo.TheInstance.Location + "\\images\\dials\\dial_small_100.png") ;

				_dialVal = 100;
			}

			DialLabel = new Label();
			DialLabel.Size = new Size(23,12);
			DialLabel.TextAlign = ContentAlignment.MiddleCenter;
			DialLabel.Location = new Point(27,59);
			DialLabel.BackColor = Color.White;
			Controls.Add(DialLabel);

			smoothTimer = new StopControlledTimer();
			smoothTimer.Interval = 100;
			smoothTimer.Tick +=	smoothTimer_Tick;

			TimeManager.TheInstance.ManageClass(this);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			e.Graphics.FillRectangle(Brushes.Black,0,0,Width, Height);
			e.Graphics.DrawImageUnscaled(BackgroundImage,0,0,BackgroundImage.Width,BackgroundImage.Height);

			int val = _dialVal;
			if(!divide_scale)
			{
				// Reverse dial...
				val = 100 - val;
			}

			e.Graphics.ResetTransform();
			e.Graphics.TranslateTransform(40,39);
			e.Graphics.RotateTransform((float)(Math.Round(val * 2.7) - 130));
			e.Graphics.TranslateTransform(-2,-14);
			e.Graphics.DrawImage(dial,0,0,3,16);
		}

		void smoothTimer_Tick(object sender, EventArgs e)
		{
			if(_targetVal < _dialVal)
				--_dialVal;
			else if(_targetVal > _dialVal)
				++_dialVal;

			if(divide_scale)
				DialLabel.Text = CONVERT.ToStr(_dialVal / 5);
			else 
				DialLabel.Text = CONVERT.ToStr(_dialVal);

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
