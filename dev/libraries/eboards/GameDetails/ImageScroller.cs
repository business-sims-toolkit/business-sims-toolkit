using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using Algorithms;

using CommonGUI;
using CoreUtils;

namespace GameDetails
{
	public class ImageScroller<T> : FlickerFreePanel
	{
		Dictionary<T, Rectangle> itemToBounds;
		Dictionary<T, Image> itemToImage;

		Rectangle currentBounds;

		bool isMoving;
		Rectangle startBounds;
		Rectangle endBounds;
		double moveDuration;
		double moveTimer;

		Timer timer;

		public ImageScroller ()
		{
			itemToBounds = new Dictionary<T, Rectangle> ();
			itemToImage = new Dictionary<T, Image> ();

			isMoving = false;

			timer = new Timer ();
			timer.Interval = 20;
			timer.Tick += timer_Tick;
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing)
			{
				timer.Dispose();
			}

			base.Dispose(disposing);
		}

		void timer_Tick (object sender, EventArgs args)
		{
			if (isMoving)
			{
				moveTimer += (timer.Interval / 1000.0f);
				if (moveTimer >= moveDuration)
				{
					StopMoving();
				}

				double t = Maths.SmoothStep(Maths.Clamp(Maths.MapBetweenRanges(moveTimer, 0, moveDuration, 0, 1), 0, 1));
				currentBounds = Maths.Lerp(t, startBounds, endBounds);

				Invalidate();
			}
		}

		public void AddItem (T item, Image image, int xDirection, int yDirection)
		{
			Rectangle bounds = PositionItem(image.Size, xDirection, yDirection);

			itemToBounds.Add(item, bounds);
			itemToImage.Add(item, image);

			if (itemToBounds.Keys.Count == 1)
			{
				JumpToItem(item);
			}
		}

		int PositionItem (int itemSize, int overallMin, int overallMax, int alignment)
		{
			switch (Math.Sign(alignment))
			{
				case -1:
					return overallMin - itemSize;

				case 1:
					return overallMax;

				case 0:
				default:
					return (overallMin + overallMax - itemSize) / 2;
			}
		}

		Rectangle PositionItem (Size itemSize, int xDirection, int yDirection)
		{
			Rectangle? overallBounds = null;
			foreach (Rectangle itemBounds in itemToBounds.Values)
			{
				if (overallBounds.HasValue)
				{
					overallBounds = new Rectangle (Math.Min(itemBounds.Left, overallBounds.Value.Left),
												   Math.Min(itemBounds.Top, overallBounds.Value.Top),
												   Math.Max(itemBounds.Right, overallBounds.Value.Right) - Math.Min(itemBounds.Left, overallBounds.Value.Left),
												   Math.Max(itemBounds.Bottom, overallBounds.Value.Bottom) - Math.Min(itemBounds.Top, overallBounds.Value.Top));
				}
				else
				{
					overallBounds = itemBounds;
				}
			}

			if (overallBounds.HasValue)
			{
				return new Rectangle (PositionItem(itemSize.Width, overallBounds.Value.Left, overallBounds.Value.Right, xDirection),
									  PositionItem(itemSize.Height, overallBounds.Value.Top, overallBounds.Value.Bottom, yDirection),
									  itemSize.Width, itemSize.Height);
			}
			else
			{
				return new Rectangle (0, 0, itemSize.Width, itemSize.Height);
			}
		}

		void StopMoving ()
		{
			isMoving = false;
			timer.Stop();
		}

		public void JumpToItem (T item)
		{
			StopMoving();

			currentBounds = itemToBounds[item];
			Invalidate();
		}

		public void ScrollToItem (T item, double time)
		{
			moveTimer = 0;
			moveDuration = time;

			startBounds = currentBounds;
			endBounds = itemToBounds[item];

			isMoving = true;
			timer.Start();
		}

		protected override void OnPaint (PaintEventArgs e)
		{
			base.OnPaint(e);

			foreach (T item in itemToBounds.Keys)
			{
				Rectangle itemBounds = itemToBounds[item];

				RectangleF destination = new RectangleFFromBounds
				{
					Left = (float) Maths.MapBetweenRanges(itemBounds.Left, currentBounds.Left, currentBounds.Right, 0, Width),
					Top = (float) Maths.MapBetweenRanges(itemBounds.Top, currentBounds.Top, currentBounds.Bottom, 0, Height),
					Right = (float) Maths.MapBetweenRanges(itemBounds.Right, currentBounds.Left, currentBounds.Right, 0, Width),
					Bottom = (float) Maths.MapBetweenRanges(itemBounds.Bottom, currentBounds.Top, currentBounds.Bottom, 0, Height)
				}.ToRectangleF();

				e.Graphics.DrawImage(itemToImage[item], destination);
			}
		}

		public override Size GetPreferredSize (Size proposedSize)
		{
			Size size = new Size (0, 0);
			foreach (Image image in itemToImage.Values)
			{
				size = new Size (Math.Max(size.Width, image.Width), Math.Max(size.Height, image.Height));
			}

			return size;
		}
	}
}