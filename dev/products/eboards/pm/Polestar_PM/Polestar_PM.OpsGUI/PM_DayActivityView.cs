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
	public class PM_DayActivityView : FlickerFreePanel
	{
		protected Font MyDefaultSkinFontNormal12 = null;
		protected Font MyDefaultSkinFontBold12 = null;
		protected Font MyDefaultSkinFontNormal10 = null;
		protected Font MyDefaultSkinFontBold10 = null;
		protected Font MyDefaultSkinFontBold9 = null;

		protected NodeTree model;
		protected int roundLength;
		protected bool isTrainingMode;
		protected Image backImage = null;

		protected Node workList;
		protected List<Node> opsItems;

		protected Node projectsNode;
		protected List<Node> prjNodes;

		protected Node currentDayNode;

		protected Node messageNode;
		protected Node messageNode1;
		protected Node messageNode2;
		protected Node messageNode3;
		protected Node messageNode4;
		protected Node predictedMarketInfoNode;
		protected bool display_predict = false;


		protected int round;

		public PM_DayActivityView (NodeTree model, bool isTrainingMode, int round, int roundLength)
		{
			this.model = model;
			this.isTrainingMode = isTrainingMode;
			this.round = round;
			this.roundLength = roundLength;

			if (isTrainingMode)
			{
				backImage = loadImage("t_DayActivityPanel.png");
			}
			else
			{
				backImage = loadImage("DayActivityPanel.png");
			}

			//Just display in GB number format for the time being 
			string fontname = SkinningDefs.TheInstance.GetData("fontname");
			MyDefaultSkinFontNormal12 = ConstantSizeFont.NewFont(fontname, 12f);
			MyDefaultSkinFontBold12 = ConstantSizeFont.NewFont(fontname, 12f, FontStyle.Bold);
			MyDefaultSkinFontNormal10 = ConstantSizeFont.NewFont(fontname, 10f);
			MyDefaultSkinFontBold10 = ConstantSizeFont.NewFont(fontname, 10f, FontStyle.Bold);
			MyDefaultSkinFontBold9 = ConstantSizeFont.NewFont(fontname, 9f, FontStyle.Bold);
			
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

			messageNode = model.GetNamedNode("day_activity_messages");
			if (messageNode != null)
			{
				messageNode.AttributesChanged += new Node.AttributesChangedEventHandler (messageNode_AttributesChanged);
			}
			messageNode1 = model.GetNamedNode("day_activity_messages1");
			if (messageNode1 != null)
			{
				messageNode1.AttributesChanged += new Node.AttributesChangedEventHandler(messageNode_AttributesChanged);
			}
			messageNode2 = model.GetNamedNode("day_activity_messages2");
			if (messageNode2 != null)
			{
				messageNode2.AttributesChanged += new Node.AttributesChangedEventHandler(messageNode_AttributesChanged);
			}
			messageNode3 = model.GetNamedNode("day_activity_messages3");
			if (messageNode3 != null)
			{
				messageNode3.AttributesChanged += new Node.AttributesChangedEventHandler(messageNode_AttributesChanged);
			}
			messageNode4 = model.GetNamedNode("day_activity_messages4");
			if (messageNode4 != null)
			{
				messageNode4.AttributesChanged += new Node.AttributesChangedEventHandler(messageNode_AttributesChanged);
			}

			//Catch and handle the change of Day
			currentDayNode = model.GetNamedNode("CurrentDay");
			currentDayNode.AttributesChanged += new Node.AttributesChangedEventHandler (currentDay_AttributesChanged);

			predictedMarketInfoNode = model.GetNamedNode("predicted_market_info");
			predictedMarketInfoNode.AttributesChanged += new Node.AttributesChangedEventHandler(predictedMarketInfoNode_AttributesChanged);
		}

		protected void predictedMarketInfoNode_AttributesChanged(Node sender, System.Collections.ArrayList attrs)
		{
			display_predict = predictedMarketInfoNode.GetBooleanAttribute("displaytext",false);
			Invalidate();
		}

		protected void messageNode_AttributesChanged (Node sender, System.Collections.ArrayList attrs)
		{
			Invalidate();
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing)
			{
				currentDayNode.AttributesChanged -= new Node.AttributesChangedEventHandler (currentDay_AttributesChanged);

				if (messageNode != null)
				{
					messageNode.AttributesChanged -= new Node.AttributesChangedEventHandler (messageNode_AttributesChanged);
					messageNode = null;
				}
				if (messageNode1 != null)
				{
					messageNode1.AttributesChanged -= new Node.AttributesChangedEventHandler(messageNode_AttributesChanged);
					messageNode1 = null;
				}
				if (messageNode2 != null)
				{
					messageNode2.AttributesChanged -= new Node.AttributesChangedEventHandler(messageNode_AttributesChanged);
					messageNode2 = null;
				}
				if (messageNode3 != null)
				{
					messageNode3.AttributesChanged -= new Node.AttributesChangedEventHandler(messageNode_AttributesChanged);
					messageNode3 = null;
				}
				if (messageNode4 != null)
				{
					messageNode4.AttributesChanged -= new Node.AttributesChangedEventHandler(messageNode_AttributesChanged);
					messageNode4 = null;
				}

				if (predictedMarketInfoNode != null)
				{
					predictedMarketInfoNode.AttributesChanged -= new Node.AttributesChangedEventHandler(predictedMarketInfoNode_AttributesChanged);
					predictedMarketInfoNode = null;
				}

				//get rid of the Font
				if (MyDefaultSkinFontNormal12 != null)
				{
					MyDefaultSkinFontNormal12.Dispose();
					MyDefaultSkinFontNormal12 = null;
				}
				if (MyDefaultSkinFontBold12 != null)
				{
					MyDefaultSkinFontBold12.Dispose();
					MyDefaultSkinFontBold12 = null;
				}
				if (MyDefaultSkinFontNormal10 != null)
				{
					MyDefaultSkinFontNormal10.Dispose();
					MyDefaultSkinFontNormal10 = null;
				}
				if (MyDefaultSkinFontBold10 != null)
				{
					MyDefaultSkinFontBold10.Dispose();
					MyDefaultSkinFontBold10 = null;
				}

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

		public Image loadImage(string imagename)
		{
			return Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images\\panels\\" + imagename);
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

			//Perhaps we should cath the deletion of the sub node and detach the event handler then 

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

		private string FormatThousands(int a)
		{
			if (a < 0)
			{
				return "-" + FormatThousands(Math.Abs(a));
			}

			string raw = LibCore.CONVERT.ToStr(a);

			StringBuilder builder = new StringBuilder("");
			int digits = 0;
			for (int character = raw.Length - 1; character >= 0; character--)
			{
				builder.Insert(0, raw[character]);
				digits++;

				if (((digits % 3) == 0) && (character > 0))
				{
					builder.Insert(0, ",");
				}
			}

			return builder.ToString();
		}

		private string FormatMoney(int a)
		{
			return "$" + FormatThousands(a);
		}

		protected void HandleDisplayNormal(PaintEventArgs e)
		{
			Brush textBrush = new SolidBrush(Color.FromArgb(255, 255, 255));  //dark color deep Gray #333333
			string title = " Day Activity";
			e.Graphics.DrawString(title, MyDefaultSkinFontBold10, textBrush, 5, 5 - 2);
			textBrush.Dispose();

			int currentDay = this.currentDayNode.GetIntAttribute("day", 0);

			string message1 = "";
			string message2 = "";
			//Check through the ops items (is there anything happening for the current day)
			foreach (Node opsItem in opsItems)
			{
				int day = opsItem.GetIntAttribute("day", 0);
				int duration = opsItem.GetIntAttribute("duration", 1);
				string display = opsItem.GetAttribute("display");
				string job_action = opsItem.GetAttribute("action");

				if (day == currentDay)
				{
					message1 = display;
					switch (job_action.ToLower())
					{
						case "upgrade_fsc_app":
						case "rebuild_fsc_app":
							message1 = "FSC";
							break;
						case "upgrade_memory":
							message1 = "Memory Upgrade on " + display;
							break;
						case "upgrade_disk":
							message1 = "Disk Upgrade on " + display;
							break;
						case "upgrade_both":
							message1 = " Upgrade on ";
							break;
						case "install_cc_app":
							//Extract the extra parameters 
							int job_cardChangeID = opsItem.GetIntAttribute("cc_card_change_id", 0);
							message1 = "Change " + CONVERT.ToStr(job_cardChangeID) + " installing";
							break;
						case "blockday": //no work need required 
							message1 = "No Change possible";
							break;
					}
				}
			}
			//Check through the projects (is there anything happening for the current days) 
			if (message1 == "")
			{
				int number_of_project_installing = 0;
				List<int> prjDays = new List<int>();
				foreach (Node prjItem in this.prjNodes)
				{
					ProjectReader pr = new ProjectReader(prjItem);
					int iday = pr.getInstallDay();
					string project_id_str = CONVERT.ToStr(pr.getProjectID());
					bool prj_installing = pr.isCurrentState(emProjectOperationalState.PROJECT_STATE_INSTALLING);

					if (iday > 0)
					{
						if (iday == currentDay)
						{
							if (prj_installing)
							{
								//add this project to the message 
								if (number_of_project_installing > 0)
								{
									message1 += ", ";
								}
								message1 += project_id_str;
								number_of_project_installing++;
							}
						}
					}
				}
				if (message1 != "")
				{
					message1 = " Projects " + message1 + " Installing";
				}
			}

			Font font = MyDefaultSkinFontBold10;

			// We can be overridden by a message in the network.
			if (messageNode != null)
			{
				string nodeMessage = messageNode.GetAttribute("message");
				if (nodeMessage != "")
				{
					message1 = nodeMessage;
					font = MyDefaultSkinFontBold9;
				}
			}

			if (message1 != "")
			{
				Brush textBrush2 = new SolidBrush(Color.FromArgb(64, 64, 64));  //dark color deep Gray #333333
				int y_offset = 32;
				if (message1 != "")
				{
					e.Graphics.DrawString(message1, font, textBrush2, 10, y_offset);
					y_offset += 20;
				}
				if (message2 != "")
				{
					e.Graphics.DrawString(message1, font, textBrush2, 10, y_offset);
					y_offset += 20;
				}
				textBrush2.Dispose();
			}
		}

		//protected void HandleDisplayRound3(PaintEventArgs e)
		//{
		//  Brush textBrush = new SolidBrush(Color.FromArgb(255, 255, 255));  //dark color deep Gray #333333
		//  string title = "Day Activity";
		//  if (round == 3)
		//  {
		//    title = "Comms";
		//  }
		//  e.Graphics.DrawString(title, MyDefaultSkinFontBold10, textBrush, 5, 5 - 2);
		//  textBrush.Dispose();

		//  int currentDay = this.currentDayNode.GetIntAttribute("day", 0);

		//  string OPsItem_message = "";
		//  string Projects_message = "";
		//  string activitymessage1 = "";
		//  string activitymessage2 = "";
		//  string activitymessage3 = "";
		//  string activitymessage4 = "";

			////Check through the ops items (is there anything happening for the current day)
			//foreach (Node opsItem in opsItems)
			//{
			//  int day = opsItem.GetIntAttribute("day", 0);
			//  int duration = opsItem.GetIntAttribute("duration", 1);
			//  string display = opsItem.GetAttribute("display");
			//  string job_action = opsItem.GetAttribute("action");

			//  if (day == currentDay)
			//  {
			//    OPsItem_message = display;
			//    switch (job_action.ToLower())
			//    {
			//      case "upgrade_fsc_app":
			//      case "rebuild_fsc_app":
			//        OPsItem_message = "FSC";
			//        break;
			//      case "upgrade_memory":
			//        OPsItem_message = "Memory Upgrade on " + display;
			//        break;
			//      case "upgrade_disk":
			//        OPsItem_message = "Disk Upgrade on " + display;
			//        break;
			//      case "upgrade_both":
			//        OPsItem_message = " Upgrade on ";
			//        break;
			//      case "install_cc_app":
			//        //Extract the extra parameters 
			//        int job_cardChangeID = opsItem.GetIntAttribute("cc_card_change_id", 0);
			//        OPsItem_message = "Change " + CONVERT.ToStr(job_cardChangeID) + " installing";
			//        break;
			//      case "blockday": //no work need required 
			//        OPsItem_message = "No Change possible";
			//        break;
			//    }
			//  }
			//}
			////Check through the projects (is there anything happening for the current days) 
			////if (message == "")
			////{
			//  int number_of_project_installing = 0;
			//  List<int> prjDays = new List<int>();
			//  foreach (Node prjItem in this.prjNodes)
			//  {
				//  ProjectReader pr = new ProjectReader(prjItem);
				//  int iday = pr.getInstallDay();
				//  string project_id_str = CONVERT.ToStr(pr.getProjectID());
				//  bool prj_installing = pr.isCurrentState(emProjectOperationalState.PROJECT_STATE_INSTALLING);

				//  if (iday > 0)
				//  {
				//    if (iday == currentDay)
				//    {
				//      if (prj_installing)
				//      {
				//        //add this project to the message 
				//        if (number_of_project_installing > 0)
				//        {
				//          Projects_message += ", ";
				//        }
				//        Projects_message += project_id_str;
				//        number_of_project_installing++;
				//      }
				//    }
				//  }
				//}
				//if (Projects_message != "")
				//{
				//  Projects_message = " Projects " + Projects_message + " Installing";
			//  }
			////}

			//Font font = MyDefaultSkinFontBold9;

			//// We can be overridden by a message in the network.
			//if (messageNode1 != null)
			//{
			//  activitymessage1 = messageNode1.GetAttribute("message");
			//}
			//if (messageNode2 != null)
			//{
			//  activitymessage2 = messageNode2.GetAttribute("message");
			//}
			//if (messageNode3 != null)
			//{
			//  activitymessage3 = messageNode3.GetAttribute("message");
			//}
			//if (messageNode4 != null)
			//{
			//  activitymessage4 = messageNode4.GetAttribute("message");
			//}

			//int lineCount = 0;
			//Brush textBrush2 = new SolidBrush(Color.FromArgb(64, 64, 64));  //dark color deep Gray #333333
			//int position_y = 25;
			//if (Projects_message != "")
			//{
			//  e.Graphics.DrawString(Projects_message, font, textBrush2, 10, position_y);
			//  position_y += 20;
			//  lineCount++;
			//}
			//else
			//{
			//  if (OPsItem_message != "")
			//  {
			//    e.Graphics.DrawString(OPsItem_message, font, textBrush2, 10, position_y);
			//    position_y += 20;
			//    lineCount++;
			//  }
			//}

			//if (display_predict)
			//{ 
			//  int transactions_gain_value =  predictedMarketInfoNode.GetIntAttribute("transactions_gain",0);
			//  int cost_reduction_value =  predictedMarketInfoNode.GetIntAttribute("cost_reduction",0);

			//  string predictedTG = "Predicted Transaction Gain " + FormatThousands(transactions_gain_value);
			//  string predictedCR = "Predicted Cost Reduction "+ FormatMoney(cost_reduction_value);

			//  e.Graphics.DrawString(predictedTG, font, textBrush2, 10, position_y);
			//  position_y += 20;
			//  e.Graphics.DrawString(predictedCR, font, textBrush2, 10, position_y);
			//  position_y += 20;
			//  lineCount++;
			//  lineCount++;
			//}

			//if ((activitymessage1 != "") & (lineCount < 4))
			//{
			//  e.Graphics.DrawString(activitymessage1, font, textBrush2, 10, position_y);
			//  position_y += 20;
			//  lineCount++;
			//}
			//if ((activitymessage2 != "") & (lineCount < 4))
			//{
			//  e.Graphics.DrawString(activitymessage2, font, textBrush2, 10, position_y);
			//  position_y += 20;
			//  lineCount++;
			//}
			//if ((activitymessage3 != "") & (lineCount < 4))
			//{
			//  e.Graphics.DrawString(activitymessage3, font, textBrush2, 10, position_y);
			//  position_y += 20;
			//  lineCount++;
			//}
			//if ((activitymessage4 != "")&(lineCount<4))
			//{
			//  e.Graphics.DrawString(activitymessage4, font, textBrush2, 10, position_y);
			//  position_y += 20;
			//  lineCount++;
		//  }
		//  textBrush2.Dispose();
		//}

		protected override void OnPaint (PaintEventArgs e)
		{
			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
			e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
			e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

			base.OnPaint(e);
			if (backImage != null)
			{
				e.Graphics.DrawImage(backImage, 0, 0, this.Width, this.Height);
			}
			this.HandleDisplayNormal(e);
		}
	}
}