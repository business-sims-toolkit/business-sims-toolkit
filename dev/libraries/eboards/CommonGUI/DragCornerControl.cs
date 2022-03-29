using System;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using System.Reflection;
using System.IO;
using System.Data;

using LibCore;

namespace CommonGUI
{
	public class DragCornerControl : FlickerFreePanel
	{

		public delegate void newSizeRequestHandler(int newWidth, int newHeight);
		public event newSizeRequestHandler newSizeRequest;


		private ImageBox resize;
		private bool MouseActive = false;
		private bool DragEngaged = false;
		private Point dragStart = new Point(0,0);
		private Point dragOffset = new Point(0,0);
		private Size oldSize = new Size(0, 0);

		public DragCornerControl()
		{
			this.BackColor = Color.FromArgb(218, 218, 203);

			resize = new ImageBox();
			resize.Size = new System.Drawing.Size(30, 30);
			resize.Location = new Point(0,0);
			resize.Image = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images\\resize.png");
			this.Controls.Add(resize);

			resize.MouseDown += new MouseEventHandler(handleMouseDown);
			resize.MouseMove += new MouseEventHandler(handleMouseMove);
			resize.MouseUp += new MouseEventHandler(handleMouseUp);
			resize.MouseLeave += new EventHandler(handleMouseLeave);

		}


		protected void handleMouseUp(object sender, MouseEventArgs e)
		{
			DoMouseUp();
		}
		protected void handleMouseLeave(object sender, EventArgs e)
		{
			DoMouseUp();
		}

		protected void DoMouseUp()
		{
			MouseActive = false;
		}

		private Point getNewPoint(int x, int y)
		{
			if (true)
			{
				return this.PointToScreen(new Point(x,y));
			}
			else
			{
				return new Point(x,y);
			}
		}


		protected void handleMouseDown(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right)
			{
				DragEngaged = false;
			}
			else
			{
				DragEngaged = true;
			}
			dragStart = getNewPoint(e.X, e.Y);
			dragOffset = getNewPoint(e.X, e.Y);

			oldSize = new Size(Parent.Width, Parent.Height);
			//oldSize = new Size(this.Width, this.Height);
			MouseActive = true;
		}

		private void DoMouseDown(int x, int y)
		{
			//dragOffset = new Point(x, y);
			dragOffset = getNewPoint(x,y);
		}


		protected void handleMouseMove(object sender, MouseEventArgs e)
		{
			DoMouseMove(e);
		}

		private void DoMouseMove(System.Windows.Forms.MouseEventArgs e)
		{
			if (MouseActive)
			{
				//dragOffset = new Point(e.X, e.Y);
				dragOffset = getNewPoint(e.X, e.Y);
				//System.Diagnostics.Debug.WriteLine("DCC move pt  " + e.X.ToString() + " : " + e.Y.ToString());

				if (DragEngaged)
				{
					Point mousePos = Control.MousePosition;
					mousePos.Offset(-1 * dragStart.X, -1 * dragStart.Y);
					this.Location = mousePos;
				}
				else
				{
					Point mousePos = Control.MousePosition;
					//System.Diagnostics.Debug.WriteLine("DCC mouse " + mousePos.X.ToString() + " : " + mousePos.Y.ToString());


					////mousePos.Offset(dragOffset.X, dragOffset.Y);
					//int old_width = this.Size.Width;
					//int old_height = this.Size.Height;
					int old_width = oldSize.Width;
					int old_height = oldSize.Height;

					//latest
					int new_width = old_width + (dragOffset.X - dragStart.X);
					int new_height = old_height + (dragOffset.Y - dragStart.Y);
					//int new_width = old_width + (mousePos.X - dragStart.X);
					//int new_height = old_height + (mousePos.Y - dragStart.Y);

					//this.Size = new Size(new_width, new_height);
					//Parent.Size = new Size(new_width, new_height);

					if (newSizeRequest != null)
					{
						newSizeRequest(new_width, new_height);
					}

				}
			}

			//if (e.Button == MouseButtons.Left)
			//{
			//  if (e.Y < 50)
			//  {
			//    Point mousePos = Control.MousePosition;
			//    mousePos.Offset(dragOffset.X, dragOffset.Y);
			//    this.Location = mousePos;
			//  }
			//}
		}

	}
}
