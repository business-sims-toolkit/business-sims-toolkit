using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections;
using System.Windows.Forms;
using System.Drawing.Text;
using LibCore;
using Network;

using CoreUtils;

namespace DiscreteSimGUI
{
	/// <summary>
	/// A StatusMonitor shows the business service states on a particular car.
	/// </summary>
	public class StatusMonitor : BasePanel, ITimedClass
	{
		StopControlledTimer _timer = new StopControlledTimer();
		protected int flashCount = 0;

		NodeTree _Network;

		string _carName;
		//
		public Size ItemSize;
		protected ArrayList items = new ArrayList();
		protected int maxRows = 0;
		protected int titleHeight = 25;

		protected int titleSpan = 2;

		public int TitleSpan
		{
			set
			{
				titleSpan = value;
			}
		}
		//
		/// <summary>
		/// Required designer variable.
		/// </summary>
		System.ComponentModel.Container components = null;

		protected Node car;

		protected StatusMonitorItem lastItem;
		protected StatusMonitorItem mouseOverItem;

		public void AttachToNewNode(NodeTree nt, string carName)
		{
			if(null != _Network)
			{
			}
			//
			_Network = nt;
			_carName = carName;
			this.AttachToCars();
		}

		public StatusMonitor(NodeTree nt, string carName)
		{
			_Network = nt;
			_carName = carName;
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
			this.ItemSize = new Size(95, 40);
			this.SetStyle(ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.DoubleBuffer, true);

			this.BackColor = Color.Transparent;
			//this.BackColor = Color.Black;
			this.Resize += StatusMonitor_Resize;

			AttachToCars();

			LayoutItems();
			Invalidate();

			_timer.Interval = 1000;
			_timer.Tick += _timer_Tick;
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			StatusMonitorItem found = null;

			foreach (StatusMonitorItem item in items)
			{
				if (item.Bounds.Contains(e.X, e.Y))
				{
					found = item;
					break;
				}
			}
			
			if (found != null)
			{
				if (lastItem != found)
				{
					mouseOverItem = found;
					Invalidate();
				}
			}

			lastItem = found;

			base.OnMouseMove (e);
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			if (lastItem != null)
				lastItem.MouseDown(e.X, e.Y);
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			if (lastItem != null)
				lastItem.MouseUp(e.X, e.Y);
		}

		protected override void OnMouseLeave(EventArgs e)
		{
			if (lastItem != null)
			{
				lastItem.MouseLeave();
			}

			mouseOverItem = null;
			Invalidate();

			base.OnMouseLeave (e);
		}

		protected void AttachToCars()
		{
			foreach(StatusMonitorItem smi in items)
			{
				smi.Dispose();
			}
			items.Clear();
			//
			car = _Network.GetNamedNode(_carName);
			// Run through all child nodes adding watches onto any attributes being changed...
			foreach(Node bussinessService in car)
			{
				items.Add( new StatusMonitorItem(this, bussinessService) );
			}
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
			e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
			e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
			e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
			RenderBorder(e.Graphics);
			RenderItems(e.Graphics);
			RenderDesc(e.Graphics);
			//doneRedraw = false;
		}

		static int titleOff = 5;

		/// <summary>
		/// Draw the border of the screen.
		/// </summary>
		/// <param name="g"></param>
		protected void RenderBorder(Graphics g)
		{
			if (car != null)
			{
				Color hpBlue = Color.FromArgb(24, 54, 126);
				Font f = ConstantSizeFont.NewFont("Tahoma", 10f, FontStyle.Bold);

				int w = 90*titleSpan - 4 - titleHeight/2;

				Brush hpBlueBrush = new SolidBrush(hpBlue);

				g.FillEllipse(hpBlueBrush, 2, titleOff, titleHeight, titleHeight);
				g.FillRectangle(hpBlueBrush, 2 + (titleHeight / 2), titleOff,w , titleHeight);
				g.FillEllipse(hpBlueBrush, 2 + w, titleOff, titleHeight, titleHeight);

				g.DrawString(car.GetAttribute("Driver"), f, Brushes.White, 8, titleOff+4);

				f.Dispose();
			}
		}

		/// <summary>
		/// Refresh the screen.
		/// </summary>
		/// <param name="g"></param>
		protected void RenderItems(Graphics g)
		{
			for(int i=0; i<items.Count; ++i)
			{
				StatusMonitorItem item = (StatusMonitorItem) items[i];
				item.Render(g);
			}
		}

		/// <summary>
		/// Render the tool-tip.
		/// </summary>
		/// <param name="g"></param>
		void RenderDesc(Graphics g)
		{
			if (mouseOverItem != null)
			{
				GraphicsContainer gc = g.BeginContainer();
				g.TextRenderingHint = TextRenderingHint.SystemDefault;
				g.SmoothingMode = SmoothingMode.None;

				string text = mouseOverItem.Text;
				string up = mouseOverItem.MonitoredBusinessService.GetAttribute("up");
				if("false" == up.ToLower())
				{
					string reason = mouseOverItem.MonitoredBusinessService.GetAttribute("reasondown");
					if("" != reason)
					{
						reason = "\r\n" + reason.Replace("|","\r\n").Trim();
						text += reason;
					}
				}

				Font f = ConstantSizeFont.NewFont("Tahoma", 8f, FontStyle.Regular);
				SizeF s = g.MeasureString(text, f);
				//
				int rX = mouseOverItem.Bounds.X;
				int rY = mouseOverItem.Bounds.Y + 24;
				int rWidth = (int)s.Width + 2;
				int rHeight = (int)s.Height + 2;
				//
				int xOverShoot = rX+rWidth-this.Width;
				int yOverShoot = rY+rHeight-this.Height;
				//
				if(xOverShoot > 0) rX -= xOverShoot;
				if(yOverShoot > 0) rY -= yOverShoot;
				//
				Rectangle rect = new Rectangle(rX,rY,rWidth,rHeight);
				// Alpha was 120.
				g.FillRectangle(new SolidBrush(Color.FromArgb(200, 255, 255, 255)), rect);
				g.DrawRectangle(new Pen(Brushes.Black, 1f), rect);
				g.DrawString(text, f, Brushes.Black, rect.X + 2, rect.Y);
				g.EndContainer(gc);
			}
		}

		/// <summary>
		/// Layout the individual application monitors
		/// on the screen.
		/// </summary>
		protected void LayoutItems()
		{
			maxRows = 0;

			if (items != null && items.Count > 0)
			{
				int x = 4;
				int y = titleHeight + 4;

				// start two losenges down
				// y += (ItemSize.Height * 2);
				y = ItemSize.Height;

				int column = 0;

				for (int i = 0; i < items.Count; i++)
				{
					StatusMonitorItem smi = (StatusMonitorItem) items[i];
					//
					smi.Size = ItemSize;
					smi.Location = new Point(x, y);

					y += ItemSize.Height;

					if (y > (this.Height - ItemSize.Height)) //(titleHeight + 6)))
					{
						++column;

						if (maxRows == 0)
						{
							maxRows = i + 1;
						}

						y = titleHeight + 4;
						if(column >= 2)
						{
							y = 0;
						}
						else
						{
							y = ItemSize.Height;
						}

						x += ItemSize.Width + 2;
					}
				}
			}
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if( components != null )
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			// 
			// UserControl1
			// 
			this.Size = new System.Drawing.Size(460, 400);

		}
		#endregion

		void StatusMonitor_Resize(object sender, EventArgs e)
		{
			this.LayoutItems();
		}

		void _timer_Tick(object sender, EventArgs e)
		{
				if(this.flashCount <= -2) _timer.Stop();
				else
				{
					--flashCount;
					//
					for(int i=0; i<items.Count; ++i)
					{
						StatusMonitorItem item = (StatusMonitorItem) items[i];
						item.DecrementFlashCounter();
					}
					//
					this.Invalidate();
				}
		}

		public bool OnFlash
		{
			get
			{
				if(flashCount%2 == 0) return false;
				return true;
			}
		}

		public void SetFlashCount(int c)
		{
			flashCount = c;
			_timer.Start();
		}
		#region ITimedClass Members

		public void Start()
		{
			// TODO:  Add StatusMonitor.Start implementation
		}

		public void Stop()
		{
			// TODO:  Add StatusMonitor.Stop implementation
		}

		public void Reset()
		{
		}

		public void FastForward(double timesRealTime)
		{
		}

		#endregion
	}
}
