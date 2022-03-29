using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Algorithms;
using CoreUtils;
using LibCore;
using Network;

namespace TransitionScreens
{
	public class ResizingProjectsViewer : ProjectsViewer
	{
		List<ResizingProjectProgressPanel> progressPanels;
		List<Interval> columns;
		List<string> headings;
		int titleHeight;
		int columnHeaderHeight;
		int leading;
		int totalsHeight;
		bool drawTitle;
		bool drawHeaders;

		public ResizingProjectsViewer (NodeTree model, int round)
			: base(model, round)
		{
			drawTitle = true;
			drawHeaders = true;

			BackgroundImage = null;

			RemoveLabels();
		}

		public ResizingProjectsViewer (NodeTree model, int round, int projects)
			: base(model, round, projects)
		{
			drawTitle = true;
			drawHeaders = true;

			BackgroundImage = null;

			RemoveLabels();
		}

		void RemoveLabels ()
		{
			lblCurrentSpendAmount_Value.Hide();
			lblActualCostAmount_Value.Hide();
			lblDevBudgetLeft_Value.Hide();
			lblCurrentSpendAmount_Title.Hide();
			lblActualCostAmount_Title.Hide();
			lblDevBudgetLeft_Title.Hide();
		}

		public bool DrawTitle
		{
			get => drawTitle;

			set
			{
				drawTitle = value;
				DoSize();
			}
		}

		public bool DrawHeaders
		{
			get => drawHeaders;

			set
			{
				drawHeaders = value;
				DoSize();
			}
		}

		protected override ProjectProgressPanelBase CreateProgressPanel (NodeTree nt, Node n, Color c)
		{
			if (progressPanels == null)
			{
				progressPanels = new List<ResizingProjectProgressPanel> ();
			}

			var panel = new ResizingProjectProgressPanel(n);
			progressPanels.Add(panel);
			return panel;
		}

		protected override void AddProjectNode (Node child)
		{
			if (child.GetIntAttribute("createdinround", 0) != CurrentRound)
			{
				return;
			}

			int index = 0;
			foreach (ProjectProgressPanelBase panel in ProjectPanels)
			{
				if (panel.getMonitoredProjectNode() == null)
				{
					var pNew = (ResizingProjectProgressPanel) CreateProgressPanel(child.Tree, child, MyCommonBackColor);
					pNew.Location = panel.Location;
					Controls.Add(pNew);

					ProjectPanels.RemoveAt(index);
					ProjectPanels.Insert(index, pNew);

					var oldIndex = progressPanels.IndexOf((ResizingProjectProgressPanel) panel);
					progressPanels.Remove(pNew);
					progressPanels[oldIndex] = pNew;

					panel.Dispose();

					break;
				}
				index++;
			}

			DoSize();
		}

		protected override void DoSize ()
		{
			columns = new List<Interval> ();
			headings = new List<string>(new [] { "Product", "Design", "Build", "Test", "Handover", "Ready", "Install", "Spend", "Budget" });

			int lozenges = headings.Count;
			int margin = Width / 60;
			float horizontalGapAsFractionOfLozengeSize = 0.05f;
			var lozengeWidth = ((Width - (2 * margin) - ((lozenges - 1) * (int) (Height * horizontalGapAsFractionOfLozengeSize))) / lozenges);
			int lozengeHorizontalGap = (Width - (2 * margin) - (lozenges * lozengeWidth)) / (lozenges - 1);

			int x = margin;
			for (int i = 0; i < lozenges; i++)
			{
				var column = new Interval (x, lozengeWidth);
				columns.Add(column);
				x = column.Max + lozengeHorizontalGap;
			}

			titleHeight = Height / 8;
			var titleBottom = titleHeight + leading;
			if (! drawTitle)
			{
				titleHeight = 0;
				titleBottom = 0;
			}

			columnHeaderHeight = Height / 10;
			leading = Height / 16;

			totalsHeight = columnHeaderHeight;
			int rows = 2;

			int headerBottom = titleBottom;
			if (drawHeaders)
			{
				headerBottom += columnHeaderHeight + leading;
			}

			var y = headerBottom;
			var rowHeight = ((Height - leading - totalsHeight) - y - ((rows - 1) * leading)) / rows;

			if (progressPanels != null)
			{
				foreach (var panel in progressPanels)
				{
					panel.Bounds = new Rectangle (0, y, Width, rowHeight);
					panel.SetColumnSizes(columns.ToArray());
					y = panel.Bottom + leading;
				}
			}

			Invalidate();
		}

		protected override void OnPaint (PaintEventArgs e)
		{
			var titleBottom = 0;

			if (drawTitle)
			{
				string title = "Service Pipeline";
				var titleBox = new RectangleF (0, 0, Width, titleHeight);
				float titleSize = ResizingUi.FontScalerExtensions.GetFontSizeInPixelsToFit(this, FontStyle.Regular, title, titleBox.Size);
				using (var font = SkinningDefs.TheInstance.GetPixelSizedFont(titleSize, FontStyle.Regular))
				{
					e.Graphics.DrawString(title, font, Brushes.LightGray, titleBox, new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Near });
				}

				titleBottom = (int) (titleBox.Bottom + leading);
			}

			float headingSize = ResizingUi.FontScalerExtensions.GetFontSizeInPixelsToFit(this, FontStyle.Bold, headings, columns.Select(c => new SizeF (c.Size, columnHeaderHeight)).ToList());
			using (var font = SkinningDefs.TheInstance.GetPixelSizedFont(headingSize, FontStyle.Bold))
			{
				if (drawHeaders)
				{
					for (int i = 0; i < headings.Count; i++)
					{
						e.Graphics.DrawString(headings[i], font, Brushes.White, new RectangleF (columns[i].Min, titleBottom, columns[i].Size, columnHeaderHeight), new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Center });
					}
				}

				var initialBudgetSection = new RectangleF(columns[0].Min, Height - totalsHeight, columns[1].Max - columns[0].Min, totalsHeight);
				e.Graphics.DrawString("Initial Budget: " + CONVERT.FormatMoney(totalBudget, 0, CONVERT.MoneyFormatting.Thousands), font, Brushes.White, initialBudgetSection, new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Near });

				var budgetLeftSection = new RectangleF(columns[2].Min, Height - totalsHeight, columns[3].Max - columns[2].Min, totalsHeight);
				e.Graphics.DrawString("Budget Left: " + CONVERT.FormatMoney(budgetLeft, 0, CONVERT.MoneyFormatting.Thousands), font, Brushes.White, budgetLeftSection, new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Near });

				e.Graphics.DrawString(CONVERT.FormatMoney(totalSpend, 0, CONVERT.MoneyFormatting.Thousands), font, Brushes.White, new RectangleF(columns[7].Min, Height - totalsHeight, columns[7].Size, totalsHeight), new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Center });
				e.Graphics.DrawString(CONVERT.FormatMoney(budgetAllocated, 0, CONVERT.MoneyFormatting.Thousands), font, Brushes.White, new RectangleF(columns[8].Min, Height - totalsHeight, columns[8].Size, totalsHeight), new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Center });
			}
		}
	}
}