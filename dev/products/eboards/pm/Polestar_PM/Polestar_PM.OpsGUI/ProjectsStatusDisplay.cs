using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Data;
using System.Windows.Forms;
using System.Xml;
using System.Globalization;
using System.Drawing.Text;
using System.IO;
using System.Runtime.InteropServices;

using LibCore;
using Network;
using CoreUtils;
using CommonGUI;

namespace Polestar_PM.OpsGUI
{
	public class ProjectsStatusDisplay: FlickerFreePanel
	{
		protected Image mainImage = null;
		protected Image projectbackImage = null;
		protected NodeTree MyNodeTree;
		protected Node projectsNode;
		protected Hashtable ProjectNodes = new Hashtable();
		protected Hashtable ProjectDisplays = new Hashtable();
		int round;

		protected Font MyDefaultSkinFontNormal8 = null;
		protected Font MyDefaultSkinFontNormal10 = null;
		protected Font MyDefaultSkinFontNormal12 = null;
		protected Font MyDefaultSkinFontBold8 = null;
		protected Font MyDefaultSkinFontBold10 = null;
		protected Font MyDefaultSkinFontBold12 = null;
		protected int project_seperation_y_offset = 45;
		protected int project_seperation_y_step = 10;
		protected bool display_seven_projects = false;

		bool hideITDetails;

		/// <summary>
		/// Show Availability / Revenue
		/// </summary>
		/// <param name="model"></param>
		public ProjectsStatusDisplay(NodeTree model, int round, bool isTrainingGame, bool hideITDetails)
		{
			this.round = round;
			this.hideITDetails = hideITDetails;

			string fontname =  SkinningDefs.TheInstance.GetData("fontname");
			MyDefaultSkinFontNormal8 = ConstantSizeFont.NewFont(fontname,8);
			MyDefaultSkinFontNormal10 = ConstantSizeFont.NewFont(fontname,10);
			MyDefaultSkinFontNormal12 = ConstantSizeFont.NewFont(fontname,12);
			MyDefaultSkinFontBold8 = ConstantSizeFont.NewFont(fontname,8,FontStyle.Bold);
			MyDefaultSkinFontBold10 = ConstantSizeFont.NewFont(fontname,10,FontStyle.Bold);
			MyDefaultSkinFontBold12 = ConstantSizeFont.NewFont(fontname,12,FontStyle.Bold);

			if (isTrainingGame)
			{
				mainImage = loadImage("t_projects_status_back.png");
			}
			else
			{
				mainImage = loadImage("projects_status_back.png");
			}
			projectbackImage = loadImage("projectback.png");

			MyNodeTree = model;
			projectsNode = MyNodeTree.GetNamedNode("pm_projects_running");
			projectsNode.ChildAdded +=new Network.Node.NodeChildAddedEventHandler(projectsNode_ChildAdded);
			projectsNode.ChildRemoved +=new Network.Node.NodeChildRemovedEventHandler(projectsNode_ChildRemoved);
			projectsNode.AttributesChanged += new Node.AttributesChangedEventHandler(projectsNode_AttributesChanged);

			display_seven_projects = projectsNode.GetBooleanAttribute("display_seven_projects", false);
			if (display_seven_projects)
			{
				project_seperation_y_step = 2;
			}			
			foreach (Node existing_prj_node in projectsNode.getChildren())
			{
				add_New_Project(existing_prj_node);
			}
		}

		public void projectsNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			foreach (AttributeValuePair avp in attrs)
			{
				if (avp.Attribute == "display_seven_projects")
				{
					bool display_seven_projects = sender.GetBooleanAttribute("display_seven_projects",false);
					if (display_seven_projects)
					{
						//rebuilding the positioning 
						//we already swapped the slot number, this code is all about adjusting the positioning 
						project_seperation_y_step = 2;
						foreach(ProjectLinearDisplay p1 in ProjectDisplays.Values)
						{
							int project_slot_id = p1.getSlotNumber();
							int upper_pt = (52 + project_seperation_y_step) * project_slot_id + project_seperation_y_offset; 
							p1.Location = new Point(10+2-4,upper_pt);
						}
					}
				}
			}
		}

		new public void Dispose()
		{
			if (MyDefaultSkinFontNormal8 != null)
			{
				MyDefaultSkinFontNormal8.Dispose();
				MyDefaultSkinFontNormal8 = null;
			}
			if (MyDefaultSkinFontNormal10 != null)
			{
				MyDefaultSkinFontNormal10.Dispose();
				MyDefaultSkinFontNormal10 = null;
			}
			if (MyDefaultSkinFontNormal12 != null)
			{
				MyDefaultSkinFontNormal12.Dispose();
				MyDefaultSkinFontNormal12 = null;
			}
			if (MyDefaultSkinFontBold8 != null)
			{
				MyDefaultSkinFontBold8.Dispose();
				MyDefaultSkinFontBold8 = null;
			}
			if (MyDefaultSkinFontBold10 != null)
			{
				MyDefaultSkinFontBold10.Dispose();
				MyDefaultSkinFontBold10 = null;
			}
			if (MyDefaultSkinFontBold12 != null)
			{
				MyDefaultSkinFontBold12.Dispose();
				MyDefaultSkinFontBold12 = null;
			}

			if (projectsNode != null)
			{
				projectsNode.ChildAdded -=new Network.Node.NodeChildAddedEventHandler(projectsNode_ChildAdded);
				projectsNode.ChildRemoved -=new Network.Node.NodeChildRemovedEventHandler(projectsNode_ChildRemoved);
				projectsNode.AttributesChanged -= new Node.AttributesChangedEventHandler(projectsNode_AttributesChanged);
				projectsNode = null;
			}

			foreach (ProjectLinearDisplay p1 in ProjectDisplays.Values)
			{
				this.Controls.Remove(p1);
				p1.Dispose();
			}
			ProjectDisplays.Clear();

			MyNodeTree = null;
			projectsNode = null;			

			base.Dispose();
		}

		private Image loadImage(string imagename)
		{
			return Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images\\panels\\"+imagename);
		}

		private void add_New_Project(Node child)
		{
			if (child != null)
			{
				string project_id = child.GetAttribute("project_id");
				int project_slot_id = child.GetIntAttribute("slot",0);

				if (ProjectNodes.ContainsKey(project_id)==false)
				{
					//This code used to extract a image from the main background image

					//int upper_pt = 62 * project_slot_id + 50; 

					int upper_pt = (52 + project_seperation_y_step) * project_slot_id + project_seperation_y_offset; 
					//Point[] destPts1 = {new Point(0, 0),new Point(1014,0),new Point(0, 60)};
					//Rectangle srcRect = new Rectangle(10,upper_pt,1014,60);
					//Bitmap bmp = new Bitmap(1014,60);
					//Graphics g = Graphics.FromImage(bmp);
					//g.DrawImage(this.mainImage,destPts1,srcRect,System.Drawing.GraphicsUnit.Pixel);
					//g.Dispose();
					ProjectNodes.Add(project_id,child);
					ProjectLinearDisplay p1 = new ProjectLinearDisplay(MyNodeTree, child, this.round, hideITDetails);
					//p1.setBackImage(bmp);
					p1.setBackImage(projectbackImage);
					p1.Location = new Point(10+2-4,upper_pt);
					p1.Size = new Size(1010,52);
					this.Controls.Add(p1);
					ProjectDisplays.Add(project_id,p1);
				}
			}		
		}


		private void projectsNode_ChildAdded(Node sender, Node child)
		{
			add_New_Project(child);
		}

		private void projectsNode_ChildRemoved(Node sender, Node child)
		{
			if (child != null)
			{
				string project_id = child.GetAttribute("project_id");
				int project_slot_id = child.GetIntAttribute("slot",0);

				if (ProjectNodes.ContainsKey(project_id))
				{
					ProjectNodes.Remove(project_id);
					if (ProjectDisplays.ContainsKey(project_id))
					{
						ProjectLinearDisplay p1 = (ProjectLinearDisplay) ProjectDisplays[project_id];
						this.Controls.Remove(p1);
						p1.Dispose();
					}
					ProjectDisplays.Remove(project_id);
				}
			}
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			//Setup the Quality and Rendering Options 
			e.Graphics.SmoothingMode = SmoothingMode.None;
			e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
			e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

			e.Graphics.FillRectangle(Brushes.Black,0,0,this.Width, this.Height);
			if (mainImage != null)
			{
				e.Graphics.DrawImage(mainImage,0,0,this.Width, this.Height);
			}

			Brush textBrush_Title = new SolidBrush(Color.FromArgb(255, 255, 255));
			Brush textBrush = new SolidBrush(Color.FromArgb(255, 255, 255));  //dark color deep Gray #333333

			//First Column
			e.Graphics.DrawString(SkinningDefs.TheInstance.GetData("project_term_round_" + LibCore.CONVERT.ToStr(round), "Project"), MyDefaultSkinFontBold10, textBrush_Title, 10, 5);

			//Second Column
			e.Graphics.DrawString(" Spec", MyDefaultSkinFontBold10, textBrush_Title, 118-9, 5);
			e.Graphics.DrawString(" Product", MyDefaultSkinFontBold8, textBrush, 85-9, 26);
			e.Graphics.DrawString(" Platform", MyDefaultSkinFontBold8, textBrush, 141 - 9, 26);

			//Third Column
			e.Graphics.DrawString("Dev:Design", MyDefaultSkinFontBold10, textBrush_Title, 235 + 11-9, 5);
			e.Graphics.DrawString("A", MyDefaultSkinFontBold8, textBrush, 220 + 3 + 26 - 9, 26);
			e.Graphics.DrawString("B", MyDefaultSkinFontBold8, textBrush, 270 + 3 + 17 + 8 - 9, 26);
			e.Graphics.DrawString("C", MyDefaultSkinFontBold8, textBrush, 320 + 3 + 10 + 13 - 9, 26);

			//Fourth Column
			e.Graphics.DrawString("Dev:Build", MyDefaultSkinFontBold10, textBrush_Title, 375 + 15 - 9, 5);
			e.Graphics.DrawString("D", MyDefaultSkinFontBold8, textBrush, 380 + 1 + 15 - 9, 26);
			e.Graphics.DrawString("E", MyDefaultSkinFontBold8, textBrush, 440 - 9 + 14 - 9, 26);
			
			//Fifth Column
			e.Graphics.DrawString("Test", MyDefaultSkinFontBold10, textBrush_Title, 530 - 9, 5);
			e.Graphics.DrawString("F", MyDefaultSkinFontBold8, textBrush, 220 + 270 + 3 + 2 - 9, 26);
			e.Graphics.DrawString("G", MyDefaultSkinFontBold8, textBrush, 270 + 270 + 3 + 2 - 9, 26);
			e.Graphics.DrawString("H", MyDefaultSkinFontBold8, textBrush, 330 + 270 - 8 + 2 - 9, 26);

			//Sixth Column
			e.Graphics.DrawString(" Handover", MyDefaultSkinFontBold10, textBrush_Title, 638-9, 5);
			//Seventh Column
			e.Graphics.DrawString(" Install", MyDefaultSkinFontBold10, textBrush_Title, 735, 5);
			//Eigth Column
			e.Graphics.DrawString(" Status", MyDefaultSkinFontBold10, textBrush_Title, 895, 5);
			e.Graphics.DrawString("   Money ($)", MyDefaultSkinFontBold8, textBrush, 895 - 81 + 2, 26);
			e.Graphics.DrawString("   Scope", MyDefaultSkinFontBold8, textBrush, 895 + 3 - 6, 26);
			e.Graphics.DrawString("   Go Live Day", MyDefaultSkinFontBold8, textBrush, 895 + 44, 26);

			textBrush.Dispose();
			textBrush_Title.Dispose();
		}


	}
}
