using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

using System.Collections;
using System.Collections.Generic;

using CoreUtils;
using LibCore;
using Network;

namespace Cloud.OpsScreen
{
	class LeaderboardEntryDisplay
	{
		public Node LeaderboardEntry;
		public double CurrentValue;
		public double PreviousValue;
		public int ChangeSign;
		public double ChangeFraction;

		public LeaderboardEntryDisplay (Node leaderboardEntry)
		{
			LeaderboardEntry = leaderboardEntry;

			Update();
			ChangeSign = 0;
		}

		public void Update ()
		{
			PreviousValue = CurrentValue;
			CurrentValue = LeaderboardEntry.GetDoubleAttribute("current_value", 0);

			ChangeSign = Math.Sign(CurrentValue - PreviousValue);

			if ((ChangeSign == 0) || (PreviousValue == 0))
			{
				ChangeFraction = 0;
			}
			else
			{
				ChangeFraction = (CurrentValue - PreviousValue) / PreviousValue;
			}
		}
	}

	public class ShadedViewPanel_Leaderboard : ShadedViewPanel_Base
	{
		Font Font_Symbol;
		Font Font_Name;
		Font Font_PriceDelta;
		Font Font_PriceTime;

		List<Node> leaderboardEntries;
		Dictionary<Node, LeaderboardEntryDisplay> leaderboardEntryToDisplay;

		public ShadedViewPanel_Leaderboard (NodeTree model, bool isTraining)
			: base (model, isTraining)
		{
			showTitleBack = false;

			string font = SkinningDefs.TheInstance.GetData("fontname");
			Font_Symbol = FontRepository.GetFont(font, 10, FontStyle.Bold);
			Font_PriceDelta = FontRepository.GetFont(font, 8, FontStyle.Regular);
			Font_Name = FontRepository.GetFont(font, 8, FontStyle.Regular);
			Font_PriceTime = FontRepository.GetFont(font, 8, FontStyle.Regular);

			leaderboardEntries = new List<Node> ();
			leaderboardEntryToDisplay = new Dictionary<Node, LeaderboardEntryDisplay> ();
			Node leaderboard = model.GetNamedNode("Leaderboard");
			foreach (Node leaderboardEntry in leaderboard.GetChildrenOfType("leaderboard_entry"))
			{
				leaderboardEntries.Add(leaderboardEntry);
				leaderboardEntry.AttributesChanged += new Node.AttributesChangedEventHandler (leaderboardEntry_AttributesChanged);
				leaderboardEntryToDisplay.Add(leaderboardEntry, new LeaderboardEntryDisplay (leaderboardEntry));
			}
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing)
			{
				foreach (Node leaderboardEntry in leaderboardEntries)
				{
					leaderboardEntry.AttributesChanged -= new Node.AttributesChangedEventHandler (leaderboardEntry_AttributesChanged);
				}
			}

			base.Dispose(disposing);
		}

		void leaderboardEntry_AttributesChanged (Node sender, ArrayList attributes)
		{
			Invalidate();
		}

		protected override void OnPaint (PaintEventArgs e)
		{
			e.Graphics.FillRectangle(Brushes.Black, 0, 0, Width, Height);
			if (string.IsNullOrEmpty(TitleText) == false)
			{
				e.Graphics.DrawString(TitleText, Font_Title, brush_Title, 0, 0);
			}

			int pos_x = 10;
			int pos_y = 1;
			int diff_y = 15;
			int slot_gap = 70;
			int slot_width = (Width - (2 * pos_x) - (slot_gap * (leaderboardEntries.Count - 1))) / leaderboardEntries.Count;

			foreach (Node leaderboardEntry in leaderboardEntries)
			{
				string symbol = leaderboardEntry.GetAttribute("short_desc");

				LeaderboardEntryDisplay display = leaderboardEntryToDisplay[leaderboardEntry];
				display.Update();

				StringFormat format = new StringFormat();
				format.Alignment = StringAlignment.Far;

				e.Graphics.DrawString(symbol, Font_Symbol, Brushes.White, pos_x, pos_y);
				e.Graphics.DrawString(leaderboardEntry.GetAttribute("desc"), Font_Name, Brushes.White, pos_x, pos_y + diff_y);

				string valueText = "$" + CONVERT.ToPaddedStrWithThousands(display.CurrentValue, 0);
				e.Graphics.DrawString(valueText, Font_Symbol, Brushes.White, pos_x + slot_width, pos_y, format);
				SizeF valueSize = e.Graphics.MeasureString(valueText, Font_Symbol);

				if (display.ChangeSign != 0)
				{
					string price_delta = CONVERT.Format("{0:+00.00;-00.00; 00.00}%", 100 * display.ChangeFraction);
					e.Graphics.DrawString(price_delta, Font_PriceDelta, Brushes.White, pos_x + slot_width, pos_y + diff_y, format);

					double radius = 6;
					double baseAngle = Math.PI / 6;
					double yOffset = radius + 2;

					if (display.ChangeSign > 0)
					{
						FillTriangle(e.Graphics, Brushes.Green, pos_x + slot_width - 10 - valueSize.Width, pos_y + yOffset, radius, baseAngle);
					}
					else
					{
						FillTriangle(e.Graphics, Brushes.Red, pos_x + slot_width - 10 - valueSize.Width, pos_y + yOffset, radius, baseAngle - Math.PI);
					}
				}

				pos_x += (slot_width + slot_gap);
			}
		}

		void FillTriangle (Graphics graphics, Brush brush, double x, double y, double radius, double angle)
		{
			SmoothingMode oldMode = graphics.SmoothingMode;
			graphics.SmoothingMode = SmoothingMode.HighQuality;

			List<PointF> points = new List<PointF> ();

			for (int i = 0; i < 4; i++)
			{
				points.Add(new PointF ((float) (x + (radius * Math.Cos(angle + (i * (2 * Math.PI / 3))))),
									   (float) (y + (radius * Math.Sin(angle + (i * (2 * Math.PI / 3)))))));
			}

			graphics.FillPolygon(brush, points.ToArray());
			using (Pen pen = new Pen (brush))
			{
				graphics.DrawPolygon(pen, points.ToArray());
			}

			graphics.SmoothingMode = oldMode;
		}
	}
}