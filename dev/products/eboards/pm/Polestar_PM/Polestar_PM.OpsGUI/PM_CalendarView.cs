using System;
using System.Collections.Generic;

using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

using System.Xml;
using System.Text;

using GameManagement;
using Network;

using CommonGUI;

using LibCore;
using CoreUtils;
using Polestar_PM.DataLookup;

namespace Polestar_PM.OpsGUI
{
	public class PM_CalendarView : FlickerFreePanel
	{
		NodeTree model;
		int roundLength;
		bool isTrainingMode;

		Node workList;
		List<Node> opsItems;

		Node projectsNode;
		List<Node> prjNodes;

		Node currentDay;

		public PM_CalendarView (NodeTree model, bool isTrainingMode, int roundLength)
		{
			this.model = model;
			this.isTrainingMode = isTrainingMode;
			this.roundLength = roundLength;

			if (isTrainingMode)
			{
				this.BackgroundImage = LibCore.Repository.TheInstance.GetImage(LibCore.AppInfo.TheInstance.Location + "images/panels/t_Calendar_panel.png");
			}
			else
			{
				this.BackgroundImage = LibCore.Repository.TheInstance.GetImage(LibCore.AppInfo.TheInstance.Location + "images/panels/Calendar_panel.png");
			}
			//handling the Project Install Days 
			projectsNode = this.model.GetNamedNode("pm_projects_running");
			prjNodes = new List<Node> ();
			projectsNode.ChildAdded += new Node.NodeChildAddedEventHandler(projectsNode_ChildAdded);
			projectsNode.ChildRemoved += new Node.NodeChildRemovedEventHandler(projectsNode_ChildRemoved);
			foreach (Node prjItem in projectsNode.getChildren())
			{
				AddProjectNode(prjItem);
			}

			//handling the Operational Items 
			workList = model.GetNamedNode("ops_worklist");
			opsItems = new List<Node> ();
			workList.ChildAdded += new Node.NodeChildAddedEventHandler (workList_ChildAdded);
			workList.ChildRemoved += new Node.NodeChildRemovedEventHandler (workList_ChildRemoved);
			//Scan through any existing items 
			foreach (Node opsItem in workList.getChildren())
			{
				AddOpsItem(opsItem);
			}

			//Catch and handle the change of Day
			currentDay = model.GetNamedNode("CurrentDay");
			currentDay.AttributesChanged += new Node.AttributesChangedEventHandler (currentDay_AttributesChanged);
		}



		protected override void Dispose (bool disposing)
		{
			if (disposing)
			{
				currentDay.AttributesChanged -= new Node.AttributesChangedEventHandler (currentDay_AttributesChanged);

				projectsNode.ChildAdded -= new Node.NodeChildAddedEventHandler(projectsNode_ChildAdded);
				projectsNode.ChildRemoved -= new Node.NodeChildRemovedEventHandler(projectsNode_ChildRemoved);

				foreach (Node prjItem in projectsNode.getChildren())
				{
					RemoveProjectNode(prjItem);
				}

				workList.ChildAdded -= new Node.NodeChildAddedEventHandler(workList_ChildAdded);
				workList.ChildRemoved -= new Node.NodeChildRemovedEventHandler (workList_ChildRemoved);

				foreach (Node opsItem in workList.getChildren())
				{
					RemoveOpsItem(opsItem);
				}
			}

			base.Dispose(disposing);
		}

		void prj_subNode_AttributesChanged(Node sender, System.Collections.ArrayList attrs)
		{
			this.Invalidate();
		}

		protected void AddProjectNode(Node node)
		{
			this.prjNodes.Add(node);
			ProjectReader pr = new ProjectReader(node);
			Node prj_subNode = pr.getProjectSubNode();
			prj_subNode.AttributesChanged += new Node.AttributesChangedEventHandler(prj_subNode_AttributesChanged);
		}


		protected void RemoveProjectNode(Node node)
		{
			//Watch out from the possiblity that the sub node are killed before the main project node
			//the node1.Parent.DeleteChildTree(node1) kills all sub nodes before killing the main node
			//but we receive warning that main node has been killed (the sub node has already been killed)

			//Perhaps we should catch the deletion of the sub node and detach the event handler then 

			this.prjNodes.Remove(node);
			ProjectReader pr = new ProjectReader(node);
			Node prj_subNode = pr.getProjectSubNode();
			if (prj_subNode != null)
			{
				prj_subNode.AttributesChanged -= new Node.AttributesChangedEventHandler(prj_subNode_AttributesChanged);
			}
		}

		protected void projectsNode_ChildAdded(Node sender, Node child)
		{
			AddProjectNode(child);
		}

		protected void projectsNode_ChildRemoved(Node sender, Node child)
		{
			RemoveProjectNode(child);
		}

		protected void AddOpsItem (Node node)
		{
			opsItems.Add(node);
			node.AttributesChanged += new Node.AttributesChangedEventHandler (node_AttributesChanged);
		}

		protected void RemoveOpsItem(Node node)
		{
			opsItems.Remove(node);
			node.AttributesChanged -= new Node.AttributesChangedEventHandler(node_AttributesChanged);
		}

		protected void workList_ChildAdded(Node sender, Node child)
		{
			AddOpsItem(child);
		}

		protected void workList_ChildRemoved(Node sender, Node child)
		{
			RemoveOpsItem(child);
		}

		protected void node_AttributesChanged(Node sender, System.Collections.ArrayList attrs)
		{
			Invalidate();
		}

		protected void currentDay_AttributesChanged(Node sender, System.Collections.ArrayList attrs)
		{
			Invalidate();
		}

		public static void FillRoundedRectangle (Graphics graphics, Brush brush, Rectangle rectangle, int cornerRadius)
		{
			graphics.FillRectangle(brush, rectangle.Left + cornerRadius, rectangle.Top + cornerRadius, rectangle.Width - (2 * cornerRadius), rectangle.Height - (2 * cornerRadius));

			graphics.FillRectangle(brush, rectangle.Left + cornerRadius, rectangle.Top, rectangle.Width - (2 * cornerRadius), cornerRadius);
			graphics.FillRectangle(brush, rectangle.Left + cornerRadius, rectangle.Bottom - cornerRadius, rectangle.Width - (2 * cornerRadius), cornerRadius);
			graphics.FillRectangle(brush, rectangle.Left, rectangle.Top + cornerRadius, cornerRadius, rectangle.Height - (2 * cornerRadius));
			graphics.FillRectangle(brush, rectangle.Right - cornerRadius, rectangle.Top + cornerRadius, cornerRadius, rectangle.Height - (2 * cornerRadius));

			graphics.FillPie(brush, rectangle.Left, rectangle.Top, 2 * cornerRadius, 2 * cornerRadius, 180, 90);
			graphics.FillPie(brush, rectangle.Right - (2 * cornerRadius), rectangle.Top, 2 * cornerRadius, 2 * cornerRadius, 270, 90);
			graphics.FillPie(brush, rectangle.Left, rectangle.Bottom - (2 * cornerRadius), 2 * cornerRadius, 2 * cornerRadius, 90, 90);
			graphics.FillPie(brush, rectangle.Right - (2 * cornerRadius), rectangle.Bottom - (2 * cornerRadius), 2 * cornerRadius, 2 * cornerRadius, 0, 90);
		}

		public static void DrawRoundedRectangle (Graphics graphics, Pen pen, Rectangle rectangle, int cornerRadius)
		{
			graphics.DrawRectangle(pen, rectangle.Left + cornerRadius, rectangle.Top + cornerRadius, rectangle.Width - (2 * cornerRadius), rectangle.Height - (2 * cornerRadius));

			graphics.DrawRectangle(pen, rectangle.Left + cornerRadius, rectangle.Top, rectangle.Width - (2 * cornerRadius), cornerRadius);
			graphics.DrawRectangle(pen, rectangle.Left + cornerRadius, rectangle.Bottom - cornerRadius, rectangle.Width - (2 * cornerRadius), cornerRadius);
			graphics.DrawRectangle(pen, rectangle.Left, rectangle.Top + cornerRadius, cornerRadius, rectangle.Height - (2 * cornerRadius));
			graphics.DrawRectangle(pen, rectangle.Right - cornerRadius, rectangle.Top + cornerRadius, cornerRadius, rectangle.Height - (2 * cornerRadius));

			graphics.DrawPie(pen, rectangle.Left, rectangle.Top, 2 * cornerRadius, 2 * cornerRadius, 180, 90);
			graphics.DrawPie(pen, rectangle.Right - (2 * cornerRadius), rectangle.Top, 2 * cornerRadius, 2 * cornerRadius, 270, 90);
			graphics.DrawPie(pen, rectangle.Left, rectangle.Bottom - (2 * cornerRadius), 2 * cornerRadius, 2 * cornerRadius, 90, 90);
			graphics.DrawPie(pen, rectangle.Right - (2 * cornerRadius), rectangle.Bottom - (2 * cornerRadius), 2 * cornerRadius, 2 * cornerRadius, 0, 90);
		}

		protected override void OnPaint (PaintEventArgs e)
		{
			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
			e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
			e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

			base.OnPaint(e);

			//Build Up Operational Items by Day 
			Dictionary<int, Node> opsItemByDate = new Dictionary<int, Node> ();
			foreach (Node opsItem in opsItems)
			{
				int day = opsItem.GetIntAttribute("day", 0);
				int duration = opsItem.GetIntAttribute("duration", 1);

				for (int step = 0; step < duration; step++)
				{
					opsItemByDate[day+step] = opsItem;
				}
			}

			//Build Up the Booked days for projects (
			List<int> prjDays = new List<int>();
			foreach (Node prjItem in this.prjNodes)
			{
				ProjectReader pr = new ProjectReader(prjItem);
				int iday = pr.getInstallDay();
				if (iday > 0)
				{
					if (prjDays.Contains(iday)==false)
					{
						prjDays.Add(iday);
					}
				}
			}

			int columns = 7;
			int rows = 4;

			double horizontalGapAsFractionOfDayWidth = 0.125;
			double verticalGapAsFractionOfDayHeight = 0.125;

			int dayWidth = (int) (this.Width / (columns + ((1 + columns) * horizontalGapAsFractionOfDayWidth)));
			int dayHeight = (int) (this.Height / (rows + ((1 + rows) * verticalGapAsFractionOfDayHeight)));

			int horizontalGap = (int) (dayWidth * horizontalGapAsFractionOfDayWidth);
			int verticalGap = (int) (dayHeight * verticalGapAsFractionOfDayHeight);

			// Things won't divide in evenly, so distribute the error evenly at the two ends.
			int horizontalOffset = (Width - ((columns * dayWidth) + ((columns + 1) * horizontalGap))) / 2;
			int verticalOffset = (Height - ((rows * dayHeight) + ((rows + 1) * verticalGap))) / 2;

			string font = SkinningDefs.TheInstance.GetData("fontname", "Arial");
			int dateGutter = 2;

			int calendarLength = 1 + (roundLength / 60);

			int currentDate = currentDay.GetIntAttribute("day", 0);

			int date = 1;

			using (Font dateFont = ConstantSizeFont.NewFont(font, 10))
			{
				for (int row = 0; row < rows; row++)
				{
					for (int column = 0; column < columns; column++)
					{
						int x = ((column + 1) * horizontalGap) + (column * dayWidth) + horizontalOffset;
						int y = ((row + 1) * verticalGap) + (row * dayHeight) + verticalOffset;
						Rectangle rectangle = new Rectangle(x, y, dayWidth, dayHeight);
						Rectangle gradientRectangle = new Rectangle(x, y, dayWidth, dayHeight * 2 / 5);

						Color topColour = Color.FromArgb(202, 203, 204);
						Color bottomColour = Color.FromArgb(255, 255, 255);

						Color dateColour = Color.Black;

						bool pastCalendarEnd = (date > calendarLength);

						//Shading for Past, Current and Future Days 
						if (pastCalendarEnd)
						{
							bottomColour = Color.FromArgb(189, 194, 207);
						}
						else if (date == currentDate)
						{
							dateColour = Color.White;

							bottomColour = Color.FromArgb(79, 175, 0);
							topColour = Color.FromArgb(topColour.R * bottomColour.R / 255, topColour.G * bottomColour.G / 255, topColour.B * bottomColour.B / 255);
						}
						else if (date < currentDate)
						{
							int fraction = 200;
							bottomColour = Color.FromArgb(bottomColour.R * fraction / 255, bottomColour.G * fraction / 255, bottomColour.B * fraction / 255);
							topColour = Color.FromArgb(topColour.R * fraction / 255, topColour.G * fraction / 255, topColour.B * fraction / 255);
						}

						using (Brush dayBackBrush = new SolidBrush(bottomColour))
						{
							e.Graphics.FillRectangle(dayBackBrush, rectangle);
						}

						if (! pastCalendarEnd)
						{
							using (Brush dayBackBrush = new LinearGradientBrush(gradientRectangle, topColour, bottomColour, 90))
							{
								e.Graphics.FillRectangle(dayBackBrush, gradientRectangle);
							}

							//display any projects 
							if (prjDays.Contains(date))
							{
								string icon_name = "projectinstall.png";
								Image image = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images/calendar/" + icon_name);
								Rectangle imageRectangle = new Rectangle(rectangle.Left + ((rectangle.Width - image.Width) / 2),
																												rectangle.Top + ((rectangle.Height - image.Height) / 2)+6,
																				image.Width,image.Height);
								e.Graphics.DrawImage(image, imageRectangle);
							}

							//display any operational item  (Change, FSC, Upgrade etc) 
							if (opsItemByDate.ContainsKey(date))
							{
								Node opsItem = opsItemByDate[date];
								string icon_name = opsItem.GetAttribute("icon");

								if (icon_name != "")
								{
									Image image = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images/calendar/" + icon_name);
									Rectangle imageRectangle = new Rectangle (rectangle.Left + ((rectangle.Width - image.Width) / 2),
										                                      rectangle.Top + ((rectangle.Height - image.Height) / 2)+6,
																		      image.Width,image.Height);
									e.Graphics.DrawImage(image, imageRectangle);
								}
							}

							RectangleF rectangleF = new RectangleF(rectangle.Left + dateGutter, rectangle.Top + dateGutter, rectangle.Width - (2 * dateGutter), rectangle.Height - (2 * dateGutter));

							StringFormat format = new StringFormat();
							format.Alignment = StringAlignment.Far;
							format.LineAlignment = StringAlignment.Near;

							using (Brush dateBrush = new SolidBrush (dateColour))
							{
								e.Graphics.DrawString(CONVERT.ToStr(date), dateFont, dateBrush, rectangleF, format);
							}
						}

						if (date == currentDate)
						{
							using (Pen outlinePen = new Pen (Color.White))
							{
								e.Graphics.DrawRectangle(outlinePen, rectangle);
							}
						}

						date++;
					}
				}
			}
		}
	}
}