using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Algorithms;
using CommonGUI;
using CoreUtils;
using LibCore;
using Network;

namespace DevOps.OpsScreen
{
	public class AwtPanel : FlickerFreePanel
	{
		NodeTree model;
	    Node timeNode;
		Node awtNode;

	    Timer timer;
		Timer tooltipPopupTimer;

		IWatermarker watermarker;

		DevOpsLozengePopupMenu menu;
		DevOpsLozengePopupMenu tooltip;

		Dictionary<Interval, Node> xRangeToAwtEntry;

		public IWatermarker Watermarker
		{
			get => watermarker;

			set
			{
				watermarker = value;
				Invalidate();
			}
		}

		void awtNode_ChildAdded (Node parent, Node child)
		{
			child.AttributesChanged += entry_AttributesChanged;
			Invalidate();
		}

		void awtNode_ChildRemoved (Node parent, Node child)
		{
			child.AttributesChanged -= entry_AttributesChanged;
			Invalidate();
		}

		void entry_AttributesChanged (Node sender, ArrayList attributes)
		{
			Invalidate();
		}

		public AwtPanel (NodeTree model)
		{
			this.model = model;

			awtNode = model.GetNamedNode("Awt");
			awtNode.ChildAdded += awtNode_ChildAdded;
			awtNode.ChildRemoved += awtNode_ChildRemoved;

			xRangeToAwtEntry = new Dictionary<Interval, Node> ();

			foreach (Node entry in awtNode.getChildren())
			{
				entry.AttributesChanged += entry_AttributesChanged;
			}

            timeNode = model.GetNamedNode("CurrentTime");

			timer = new Timer
			{
				Interval = 1000
			};
            timer.Tick += timer_Tick;
			timer.Start();

			tooltipPopupTimer = new Timer
			{
				Interval = 250
			};
			tooltipPopupTimer.Tick += TooltipPopupTimerTick;
		}

		void timer_Tick(object sender, EventArgs e)
        {
            Invalidate();
        }
        
		protected override void Dispose (bool disposing)
		{
			if (disposing)
			{
				foreach (Node entry in awtNode.getChildren())
				{
					entry.AttributesChanged += entry_AttributesChanged;
				}

				awtNode.ChildAdded += awtNode_ChildAdded;
				awtNode.ChildRemoved += awtNode_ChildRemoved;
			}

			base.Dispose(disposing);
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);

			Invalidate();
		}

		protected override void OnPaint (PaintEventArgs e)
		{
			using (var brush = new SolidBrush (Color.FromArgb(239, 239, 239)))
			{
				e.Graphics.FillRectangle(brush, 0, 0, Width, Height);
			}

			watermarker?.Draw(this, e.Graphics);

			int sideMargin = 20;
			int topMargin = 20;
			int bottomMargin = 30;
			int columnGap = 4;
			int rowGap = 4;

			Color [] colours = {
				Color.FromArgb(140, 198, 63), Color.FromArgb(217, 224, 33), Color.FromArgb(252, 238, 33),
				Color.FromArgb(251, 176, 59), Color.FromArgb(241, 90, 36), Color.FromArgb(237, 28, 36)
			};
            Color emptyColour = Color.FromArgb(215, 221, 226);

			int currentTime = timeNode.GetIntAttribute("seconds", 0);

			var entries = awtNode.GetChildrenAsList();
			entries.Sort((a, b) => a.GetIntAttribute("index", 0).CompareTo(b.GetIntAttribute("index", 0)));

			int columnWidth = (Width - (2 * sideMargin) - ((entries.Count - 1) * columnGap)) / entries.Count;
			int rowHeight = (Height - topMargin - bottomMargin - ((colours.Length - 1) * rowGap)) / colours.Length;

			xRangeToAwtEntry.Clear();

			int x = sideMargin;
			for (int j = 0; j < entries.Count; j++)
			{
				int blocksLit = 0;
				int incidentAppliedTime = 0;
				Node businessService = model.GetNamedNode(entries[j].GetAttribute("service"));

				xRangeToAwtEntry.Add(new Interval(x, columnWidth), entries[j]);

				if (businessService != null)
				{
					blocksLit = Math.Max(1, businessService.GetIntAttribute("danger_level", 0) * colours.Length / 100);
					incidentAppliedTime = businessService.GetIntAttribute("incident_applied_time", 0);

					if (incidentAppliedTime == 0 && blocksLit == colours.Length)
					{
						businessService.SetAttribute("incident_applied_time", currentTime);
						KlaxonSingleton.TheInstance.PlayAudio(AppInfo.TheInstance.Location + @"\audio\IT.wav", false);
					}
					else if (incidentAppliedTime > 0 && blocksLit < colours.Length)
					{
						businessService.SetAttribute("incident_applied_time", 0);
					}
				}

				for (int i = 0; i < colours.Length; i++)
				{
                    Color colour = emptyColour;

                    if (i < blocksLit)
                    {
                        colour = colours[i];
                    }

				    if (blocksLit == colours.Length)
				    {
                        colour = Color.FromArgb(237, 28, 36);
				        if (currentTime - incidentAppliedTime < 10)
				        {
				            colour = currentTime % 2 == 1 ? colour : Color.FromArgb(139, 14, 18);
				        }
				    }

				    using (Brush brush = new SolidBrush (colour))
					{
						e.Graphics.FillRectangle(brush, new Rectangle (x, Height - bottomMargin - (i * (rowHeight + rowGap)) - rowHeight, columnWidth, rowHeight));
					}
				}

				e.Graphics.DrawString(CONVERT.Format("{0}", 1 + j),
				                      SkinningDefs.TheInstance.GetFont(9), (businessService != null) ? Brushes.Black : Brushes.LightGray,
				                      new Rectangle (x, Height - bottomMargin, columnWidth, bottomMargin),
				                      new StringFormat {Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near});
				x += columnWidth + columnGap;
			}
		}

		protected override void OnMouseEnter (EventArgs e)
		{
			base.OnMouseEnter(e);

			if (menu != null)
			{
				return;
			}

			var mousePosition = PointToClient(Cursor.Position);
			var column = GetColumn(mousePosition.X);

			if ((menu == null) && (column != null))
			{
				ShowTooltip(column.Value);
			}
		}

		protected override void OnMouseLeave (EventArgs e)
		{
			base.OnMouseLeave(e);
			CloseTooltip();
		}

		protected override void OnMouseMove (MouseEventArgs e)
		{
			base.OnMouseMove(e);

			if (menu != null)
			{
				return;
			}

			var column = GetColumn(e.X);
			if (column == null)
			{
				CloseTooltip();
			}
			else if ((tooltip == null) || (((Interval) tooltip.Tag) != column.Value))
			{
				ShowTooltip(column.Value);
			}
		}

		void CloseTooltip ()
		{
			tooltipPopupTimer.Stop();

			if (tooltip != null)
			{
				tooltip.Close();
				tooltip = null;
			}
		}

		void CloseMenu ()
		{
			if (menu != null)
			{
				menu.Close();
				menu = null;
			}
		}

		void ShowTooltip (Interval column)
		{
			tooltipPopupTimer.Start();
			tooltipPopupTimer.Tag = column;
		}

		Interval? GetColumn (int x)
		{
			foreach (var interval in xRangeToAwtEntry.Keys)
			{
				if (interval.Contains(x))
				{
					return interval;
				}
			}

			return null;
		}

		void TooltipPopupTimerTick (object sender, EventArgs args)
		{
			tooltipPopupTimer.Stop();

			if (menu != null)
			{
				return;
			}

			CloseTooltip();

			var column = (Interval) (tooltipPopupTimer.Tag);
			var entry = xRangeToAwtEntry[column];

			var businessService = model.GetNamedNode(entry.GetAttribute("service"));
			if (businessService == null)
			{
				return;
			}

			tooltip = new DevOpsLozengePopupMenu ();
			tooltip.BackColor = Color.FromArgb(247, 148, 51);
			tooltip.Tag = column;
			tooltip.AddHeading(businessService.GetAttribute("desc"), @"\icons\" + businessService.GetAttribute("icon") + "_default.png");

			tooltip.FormClosed += tooltip_Closed;

			tooltip.Show(TopLevelControl, this, PointToScreen(new Point (column.Min, 0)));
		}

		void tooltip_Closed (object sender, EventArgs args)
		{
			if (tooltip != null)
			{
				tooltip.Dispose();
				tooltip = null;
			}
		}

		void menu_Closed (object sender, EventArgs args)
		{
			if (menu != null)
			{
				menu.Dispose();
				menu = null;
			}
		}

		protected override void OnMouseClick (MouseEventArgs e)
		{
			base.OnMouseClick(e);

			var column = GetColumn(e.X);

			if (column == null)
			{
				CloseMenu();
			}
			else if (((menu == null) || (((Interval) (menu.Tag)) != column))
					&& ! xRangeToAwtEntry[column.Value].GetBooleanAttribute("fixed", false))
			{
				ShowMenu(column.Value);
			}
		}

		void ShowMenu (Interval column)
		{
			if (menu != null)
			{
				CloseTooltip();
				CloseMenu();
			}

			menu = new DevOpsLozengePopupMenu ();
			menu.BackColor = Color.FromArgb(247, 148, 51);
			menu.AddHeading($"Column {1 + xRangeToAwtEntry.Keys.ToList().IndexOf(column)}");
			menu.AddDivider();
			menu.Tag = column;

			var currentService = xRangeToAwtEntry[column].GetAttribute("service");

			var selectedColour = Color.LightGray;

			var noneItem = menu.AddItem("(none)");
			noneItem.Chosen += menuItem_Chosen;
			noneItem.Tag = "";
			noneItem.SetColours(string.IsNullOrEmpty(currentService) ? selectedColour : Color.White, Color.Black);
			foreach (Node businessService in model.GetNamedNode("Business Services Group").getChildren())
			{
				var item = menu.AddItem(businessService.GetAttribute("desc"), @"\icons\" + businessService.GetAttribute("icon") + "_default.png");
				item.Chosen += menuItem_Chosen;
				item.Tag = businessService.GetAttribute("name");
				item.SetColours((currentService == businessService.GetAttribute("name")) ? selectedColour : Color.White, Color.Black);
			}

			menu.FormClosed += menu_Closed;

			menu.Show(TopLevelControl, this, PointToScreen(new Point (column.Min, 0)));
		}

		void menuItem_Chosen (object sender, EventArgs args)
		{
			if (menu == null)
			{
				return;
			}

			var item = (DevOpsLozengePopupMenu.DevOpsLozengePopupMenuItem) sender;

			var column = (Interval) (menu.Tag);
			var columnIndex = 1 + xRangeToAwtEntry.Keys.ToList().IndexOf(column);

			foreach (Node entry in awtNode.getChildren())
			{
				if (entry.GetIntAttribute("index", 0) == columnIndex)
				{
					entry.SetAttribute("service", (string) (item.Tag));
					break;
				}
			}

			Invalidate();
			CloseMenu();
		}
	}
}