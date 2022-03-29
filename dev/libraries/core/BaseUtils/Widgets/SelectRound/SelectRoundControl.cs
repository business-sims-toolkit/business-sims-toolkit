using System;
using System.Windows.Forms;
using System.Drawing;
using System.Threading;

namespace BaseUtils
{
	/// <summary>
	/// Encapsulates the functionality required to change between
	/// rounds.
	/// </summary>
	public class SelectRoundControl : Control
	{
		const int SleepDelay = 3;
		ImageList imageList;
		int current;
		int direction;
		int offset;
		Bitmap bmp;
		bool focus;
		bool scrolling;

		delegate void VoidDelegate();
		public event System.EventHandler ScrollDone;

		/// <summary>
		/// Creates an instance of SelectRoundControl.
		/// </summary>
		public SelectRoundControl()
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.DoubleBuffer | ControlStyles.UserPaint, true);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			if (bmp != null)
			{
				e.Graphics.DrawImageUnscaled(bmp, offset, 0);

				if (focus && scrolling == false)
					ControlPaint.DrawFocusRectangle(e.Graphics, new Rectangle(5, 5, this.Width-10, this.Height-10), Color.White, Color.Black);
			}
		}

		protected override void OnClick(EventArgs e)
		{
			//this.Focus();
			base.OnClick (e);
		}

		protected override void OnEnter(EventArgs e)
		{
			focus = true;
			Invalidate();
			base.OnEnter (e);
		}

		protected override void OnLeave(EventArgs e)
		{
			focus = false;
			Invalidate();
			base.OnLeave (e);
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			if (focus)
			{
				if (keyData == Keys.Left)
				{
					Previous();
					return true;
				}
				else if (keyData == Keys.Right)
				{
					Next();
					return true;
				}
			}

			return base.ProcessCmdKey (ref msg, keyData);
		}

		public ImageList ImageList
		{
			get { return imageList; }
			set 
			{ 
				imageList = value; 
				Size = imageList.ImageSize;
				LoadCurrentImage();
			}
		}

		public int CurrentImage
		{
			get { return current; }
			set 
			{ 
				current = value; 
				LoadCurrentImage(); 
			}
		}

		public void Next()
		{
			if (!scrolling && (current < imageList.Images.Count - 1))
			{
				direction = 1;
				StartSlide();
			}
			System.Diagnostics.Debug.WriteLine("current"+current.ToString());
		}

		public void Previous()
		{
			if (!scrolling && (current > 0))
			{
				direction = -1;
				StartSlide();
			}
			System.Diagnostics.Debug.WriteLine("current"+current.ToString());
		}

		protected void LoadCurrentImage()
		{
			if (imageList != null && imageList.Images.Count > 0 && current >= 0 && current < imageList.Images.Count)
			{
				offset = 0;
				bmp = new Bitmap(imageList.ImageSize.Width * 2, imageList.ImageSize.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
				Graphics g = Graphics.FromImage(bmp);
				g.DrawImageUnscaled(imageList.Images[current], 0, 0);
				g.Dispose();
				Invalidate();
			}
		}

		protected void StartSlide()
		{
			bmp = new Bitmap(imageList.ImageSize.Width * 2, imageList.ImageSize.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

			if (direction == 1)
			{
				Graphics g = Graphics.FromImage(bmp);
				g.DrawImageUnscaled(imageList.Images[current], 0, 0);
				g.DrawImageUnscaled(imageList.Images[current+1], imageList.ImageSize.Width, 0);
				g.Dispose();

				scrolling = true;
				ThreadPool.QueueUserWorkItem(DoScrollNext);
			}
			else
			{
				Graphics g = Graphics.FromImage(bmp);
				g.DrawImageUnscaled(imageList.Images[current-1], 0, 0);
				g.DrawImageUnscaled(imageList.Images[current], imageList.ImageSize.Width, 0);
				g.Dispose();

				scrolling = true;
				ThreadPool.QueueUserWorkItem(DoScrollPrevious);
			}
		}

		void DoScrollNext(object state)
		{
			try
			{
				offset = 1;
				while (-offset < imageList.ImageSize.Width)
				{
					Thread.Sleep(SleepDelay);
					offset -= 2;
				
					if (-offset >= imageList.ImageSize.Width)
						scrolling = false;

					if (this.InvokeRequired)
						this.Invoke(new VoidDelegate(Invalidate));
				}
			}
			catch (ThreadAbortException)
			{
			}
			finally
			{
				current++;

				if (this.InvokeRequired)
					this.Invoke(new VoidDelegate(OnScrollDone));
			}
			System.Diagnostics.Debug.WriteLine("After Slide Scroll Next current"+current.ToString());
		}

		void DoScrollPrevious(object state)
		{
			try
			{
				offset = -(imageList.ImageSize.Width + 1);
				while (offset < 0)
				{
					Thread.Sleep(SleepDelay);
					offset += 2;

					if (offset >= 0)
						scrolling = false;

					if (this.InvokeRequired)
						this.Invoke(new VoidDelegate(Invalidate));
				}
			}
			catch (ThreadAbortException)
			{
			}
			finally
			{
				current--;

				if (this.InvokeRequired)
					this.Invoke(new VoidDelegate(OnScrollDone));
			}
			System.Diagnostics.Debug.WriteLine("After Slide Scroll Prev current"+current.ToString());
		}

		void OnScrollDone()
		{
			scrolling = false;
			if (ScrollDone != null)
				ScrollDone(this, EventArgs.Empty);
		}
	}
}
