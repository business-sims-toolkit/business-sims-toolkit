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
	/// <summary>
	/// Summary description for ProjectedRevenueView.
	/// </summary>
	public class GameStatsDisplay : FlickerFreePanel
	{
		static System.Globalization.CultureInfo myCI_enGB = new System.Globalization.CultureInfo( "en-GB", false );

		protected Font MyDefaultSkinFontBold10 = null;
		protected Font MyDefaultSkinFontBold12 = null;
		protected Font MyDefaultSkinFontNormal10 = null;
		protected Font MyDefaultSkinFontNormal12 = null;
		protected Image backImage = null;
		protected Image blockImage_red = null;
		protected Image blockImage_yellow = null;

		private Node node_Staff_Int_Dev = null;
		private Node node_Staff_Ext_Dev = null;
		private Node node_Staff_Int_Test = null;
		private Node node_Staff_Ext_Test = null;
		private Node BudgetNode = null;
		private Node ops_worklist_node = null;

		private NumberFormatInfo nfi = null;
		protected string Staff_Dev_Int_Str = "";
		protected string Staff_Dev_Ext_Str = "";
		protected string Staff_Test_Int_Str = "";
		protected string Staff_Test_Ext_Str = "";
		protected string Budget_Str = "";

		protected bool ops_block = false;
		protected bool ops_install = false;
		protected string ops_name = "";
		protected bool use_single_staff_section = false;
		
		/// <summary>
		/// This shows the Availability of Staff and the Budget 
		/// There are 2 modes based on the Question (Do we have a single pool of people)
		/// </summary>
		/// <param name="model"></param>
		public GameStatsDisplay(NodeTree model, bool IsTrainingGame)
		{
			if (IsTrainingGame)
			{
				backImage = loadImage("t_gamestats.png");
			}
			else
			{
				backImage = loadImage("gamestats.png");
			}
			blockImage_red = loadImage("game_stats_block_red.png");
			blockImage_yellow = loadImage("game_stats_block_yellow.png");

			string UseSingleSection_Str = SkinningDefs.TheInstance.GetData("use_single_staff_section", "false");
			if (UseSingleSection_Str.IndexOf("true") > -1)
			{
				use_single_staff_section = true;
			}

			//Just display in GB number format for the time being 
			nfi = new CultureInfo( "en-GB", false ).NumberFormat;

			string fontname =  SkinningDefs.TheInstance.GetData("fontname");
			MyDefaultSkinFontNormal10 = ConstantSizeFont.NewFont(fontname,10f,FontStyle.Regular);
			MyDefaultSkinFontNormal12 = ConstantSizeFont.NewFont(fontname,12f,FontStyle.Regular);
			MyDefaultSkinFontBold10 = ConstantSizeFont.NewFont(fontname,10f,FontStyle.Bold);
			MyDefaultSkinFontBold12 = ConstantSizeFont.NewFont(fontname,12f);//,FontStyle.Bold);
			
			ops_worklist_node = model.GetNamedNode("ops_worklist"); 
			ops_worklist_node.AttributesChanged +=new Network.Node.AttributesChangedEventHandler(ops_worklist_node_AttributesChanged);

			BudgetNode = model.GetNamedNode("pmo_budget"); 
			BudgetNode.AttributesChanged +=new Network.Node.AttributesChangedEventHandler(BudgetNode_AttributesChanged);

			node_Staff_Int_Dev = model.GetNamedNode("dev_staff"); 
			node_Staff_Int_Dev.AttributesChanged +=new Network.Node.AttributesChangedEventHandler(node_Staff_Int_Dev_AttributesChanged);
			node_Staff_Int_Dev.ChildAdded +=new Network.Node.NodeChildAddedEventHandler(node_Staff_Int_Dev_ChildAdded);
			node_Staff_Int_Dev.ChildRemoved +=new Network.Node.NodeChildRemovedEventHandler(node_Staff_Int_Dev_ChildRemoved);

			node_Staff_Ext_Dev = model.GetNamedNode("dev_contractor"); 
			node_Staff_Ext_Dev.AttributesChanged +=new Network.Node.AttributesChangedEventHandler(node_Staff_Ext_Dev_AttributesChanged);
			node_Staff_Ext_Dev.ChildAdded +=new Network.Node.NodeChildAddedEventHandler(node_Staff_Ext_Dev_ChildAdded);
			node_Staff_Ext_Dev.ChildRemoved +=new Network.Node.NodeChildRemovedEventHandler(node_Staff_Ext_Dev_ChildRemoved);

			node_Staff_Int_Test = model.GetNamedNode("test_staff"); 
			node_Staff_Int_Test.AttributesChanged +=new Network.Node.AttributesChangedEventHandler(node_Staff_Int_Test_AttributesChanged);
			node_Staff_Int_Test.ChildAdded +=new Network.Node.NodeChildAddedEventHandler(node_Staff_Int_Test_ChildAdded);
			node_Staff_Int_Test.ChildRemoved +=new Network.Node.NodeChildRemovedEventHandler(node_Staff_Int_Test_ChildRemoved);

			node_Staff_Ext_Test = model.GetNamedNode("test_contractor"); 
			node_Staff_Ext_Test.AttributesChanged +=new Network.Node.AttributesChangedEventHandler(node_Staff_Ext_Test_AttributesChanged);
			node_Staff_Ext_Test.ChildAdded +=new Network.Node.NodeChildAddedEventHandler(node_Staff_Ext_Test_ChildAdded);
			node_Staff_Ext_Test.ChildRemoved +=new Network.Node.NodeChildRemovedEventHandler(node_Staff_Ext_Test_ChildRemoved);

			UpdateFigures();
			Refresh();
		}

		/// <summary>
		/// Dispose ...
		/// </summary>
		new public virtual void Dispose()
		{
			//get rid of the Font
			if (MyDefaultSkinFontNormal10 != null)
			{
				MyDefaultSkinFontNormal10.Dispose();
				MyDefaultSkinFontNormal10 = null;
			}
			//get rid of the Font
			if (MyDefaultSkinFontNormal12 != null)
			{
				MyDefaultSkinFontNormal12.Dispose();
				MyDefaultSkinFontNormal12 = null;
			}
			//get rid of the Font
			if (MyDefaultSkinFontBold10 != null)
			{
				MyDefaultSkinFontBold10.Dispose();
				MyDefaultSkinFontBold10 = null;
			}
			//get rid of the Font
			if (MyDefaultSkinFontBold12 != null)
			{
				MyDefaultSkinFontBold12.Dispose();
				MyDefaultSkinFontBold12 = null;
			}
			//Disconnect from the nodes
			if (ops_worklist_node != null)
			{
				ops_worklist_node.AttributesChanged -=new Network.Node.AttributesChangedEventHandler(ops_worklist_node_AttributesChanged);
				ops_worklist_node = null;
			}
			
			if (BudgetNode != null)
			{
				BudgetNode.AttributesChanged -=new Network.Node.AttributesChangedEventHandler(BudgetNode_AttributesChanged);
				BudgetNode = null;
			}

			if (node_Staff_Int_Dev != null)
			{
				node_Staff_Int_Dev.AttributesChanged -=new Network.Node.AttributesChangedEventHandler(node_Staff_Int_Dev_AttributesChanged);
				node_Staff_Int_Dev.ChildAdded -=new Network.Node.NodeChildAddedEventHandler(node_Staff_Int_Dev_ChildAdded);
				node_Staff_Int_Dev.ChildRemoved -=new Network.Node.NodeChildRemovedEventHandler(node_Staff_Int_Dev_ChildRemoved);
				node_Staff_Int_Dev = null;
			}
			if (node_Staff_Ext_Dev != null)
			{
				node_Staff_Ext_Dev.AttributesChanged -=new Network.Node.AttributesChangedEventHandler(node_Staff_Ext_Dev_AttributesChanged);
				node_Staff_Ext_Dev.ChildAdded -=new Network.Node.NodeChildAddedEventHandler(node_Staff_Ext_Dev_ChildAdded);
				node_Staff_Ext_Dev.ChildRemoved -=new Network.Node.NodeChildRemovedEventHandler(node_Staff_Ext_Dev_ChildRemoved);
				node_Staff_Ext_Dev = null;
			}

			if (node_Staff_Int_Test != null)
			{
				node_Staff_Int_Test.AttributesChanged -=new Network.Node.AttributesChangedEventHandler(node_Staff_Int_Test_AttributesChanged);
				node_Staff_Int_Test.ChildAdded -=new Network.Node.NodeChildAddedEventHandler(node_Staff_Int_Test_ChildAdded);
				node_Staff_Int_Test.ChildRemoved -=new Network.Node.NodeChildRemovedEventHandler(node_Staff_Int_Test_ChildRemoved);
				node_Staff_Int_Test = null;
			}
			if (node_Staff_Ext_Test != null)
			{
				node_Staff_Ext_Test.AttributesChanged -=new Network.Node.AttributesChangedEventHandler(node_Staff_Ext_Test_AttributesChanged);
				node_Staff_Ext_Test.ChildAdded -=new Network.Node.NodeChildAddedEventHandler(node_Staff_Ext_Test_ChildAdded);
				node_Staff_Ext_Test.ChildRemoved -=new Network.Node.NodeChildRemovedEventHandler(node_Staff_Ext_Test_ChildRemoved);
				node_Staff_Ext_Test = null;
			}
		}

		public Image loadImage(string imagename)
		{
			return Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images\\panels\\"+imagename);
		}

		protected virtual void UpdateFigures ()
		{
			Staff_Dev_Int_Str = "0";
			Staff_Dev_Ext_Str = "0";
			Staff_Test_Int_Str = "0";
			Staff_Test_Ext_Str = "0";
			Budget_Str = "0";

			if (node_Staff_Int_Dev != null)
			{		
				Staff_Dev_Int_Str = CONVERT.ToStr(node_Staff_Int_Dev.getChildren().Count);
			}
			if (node_Staff_Ext_Dev != null)
			{	
				Staff_Dev_Ext_Str = CONVERT.ToStr(node_Staff_Ext_Dev.getChildren().Count);
			}
			if (node_Staff_Int_Test != null)
			{
				Staff_Test_Int_Str = CONVERT.ToStr(node_Staff_Int_Test.getChildren().Count);
			}
			if (node_Staff_Ext_Test != null)
			{	
				Staff_Test_Ext_Str = CONVERT.ToStr(node_Staff_Ext_Test.getChildren().Count);
			}
			if (BudgetNode != null)
			{
				int budget_value = BudgetNode.GetIntAttribute("budget_left",0);
				//budget_value = budget_value / 1000;
				string tempstr = budget_value.ToString( "N", nfi );
				tempstr = tempstr.Replace(".00","");
				Budget_Str = "$" + tempstr;	
			}

			if (ops_worklist_node != null)
			{
				ops_block = ops_worklist_node.GetBooleanAttribute("block",false);
				ops_install = ops_worklist_node.GetBooleanAttribute("install",false);
				ops_name = ops_worklist_node.GetAttribute("jobname");
			}

			this.Refresh();
		}

		private void RevenueNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			UpdateFigures();
		}

		private void node_Staff_Int_Dev_ChildRemoved(Node sender, Node child)
		{
			UpdateFigures();
		}

		private void node_Staff_Ext_Dev_AttributesChanged(Node sender, ArrayList attrs)
		{
			UpdateFigures();
		}

		private void node_Staff_Ext_Dev_ChildAdded(Node sender, Node child)
		{
			UpdateFigures();
		}

		private void node_Staff_Ext_Dev_ChildRemoved(Node sender, Node child)
		{
			UpdateFigures();
		}

		private void node_Staff_Int_Test_AttributesChanged(Node sender, ArrayList attrs)
		{
			UpdateFigures();
		}

		private void node_Staff_Int_Test_ChildAdded(Node sender, Node child)
		{
			UpdateFigures();
		}

		private void node_Staff_Int_Test_ChildRemoved(Node sender, Node child)
		{
			UpdateFigures();
		}

		private void node_Staff_Ext_Test_AttributesChanged(Node sender, ArrayList attrs)
		{
			UpdateFigures();
		}

		private void node_Staff_Ext_Test_ChildAdded(Node sender, Node child)
		{
			UpdateFigures();
		}

		private void node_Staff_Ext_Test_ChildRemoved(Node sender, Node child)
		{
			UpdateFigures();
		}

		protected void BudgetNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			UpdateFigures();
		}

		private void ops_worklist_node_AttributesChanged(Node sender, ArrayList attrs)
		{
			UpdateFigures();
		}


		private void node_Staff_Int_Dev_AttributesChanged(Node sender, ArrayList attrs)
		{
			UpdateFigures();
		}

		private void node_Staff_Int_Dev_ChildAdded(Node sender, Node child)
		{
			UpdateFigures();
		}

		protected virtual void DrawSingleSectionMode(PaintEventArgs e)
		{
			//Draw the background 
			if (backImage != null)
			{
				e.Graphics.DrawImage(this.backImage, 0, 0, 220, 141);
			}

			SizeF sf; 


			//PMO
			//Brush textBrush = new SolidBrush(Color.FromArgb(51, 51, 51));  //dark color deep Gray #333333
			Brush textBrush = new SolidBrush(Color.FromArgb(255, 255, 255));  //dark color deep Gray #333333
			e.Graphics.DrawString(" PMO", MyDefaultSkinFontBold10, textBrush, 5, 5 - 2);
			textBrush.Dispose();

			//Draw the Workforce area 
			e.Graphics.DrawString(" Resources: Staff", MyDefaultSkinFontBold10, Brushes.Black, 5, 25);
			e.Graphics.DrawString(" Internal", MyDefaultSkinFontNormal10, Brushes.Black, 5, 44 + 2);
			sf = e.Graphics.MeasureString(Staff_Dev_Int_Str, MyDefaultSkinFontNormal10);
			e.Graphics.DrawString(Staff_Dev_Int_Str, MyDefaultSkinFontNormal10, Brushes.Black, 205 - sf.Width, 44 + 2);

			e.Graphics.DrawString(" Contractors", MyDefaultSkinFontNormal10, Brushes.Black, 5, 58 + 4);
			sf = e.Graphics.MeasureString(Staff_Dev_Ext_Str, MyDefaultSkinFontNormal10);
			e.Graphics.DrawString(Staff_Dev_Ext_Str, MyDefaultSkinFontNormal10, Brushes.Black, 205 - sf.Width, 58 + 4);

			//Draw the Budget area 
			e.Graphics.DrawString(" Resources: Financial", MyDefaultSkinFontBold10, Brushes.Black, 5, 80+4);
			e.Graphics.DrawString(" PMO Budget", MyDefaultSkinFontNormal10, Brushes.Black, 5, 110 - 1);
			sf = e.Graphics.MeasureString(Budget_Str, MyDefaultSkinFontNormal10);
			e.Graphics.DrawString(Budget_Str, MyDefaultSkinFontNormal10, Brushes.Black, 210 - sf.Width, 110 - 1);

		}
		protected void DrawDualSectionMode(PaintEventArgs e)
		{
			//Draw the background 
			if (backImage != null)
			{
				e.Graphics.DrawImage(this.backImage, 0, 0, 210, 250);
			}

			//PMO
			Brush textBrush_title = new SolidBrush(Color.FromArgb(255, 255, 255));  //dark color deep Gray #333333
			//Brush textBrush = new SolidBrush(Color.FromArgb(51, 51, 51));  //dark color deep Gray #333333
			e.Graphics.DrawString(" PMO", MyDefaultSkinFontBold10, textBrush_title, 5, 5 - 2);
			textBrush_title.Dispose();

			//Draw the Workforce area 
			e.Graphics.DrawString(" Resources: Internal", MyDefaultSkinFontBold10, Brushes.Black, 5, 25);

			e.Graphics.DrawString(" Development", MyDefaultSkinFontNormal10, Brushes.Black, 5, 44 + 2);
			e.Graphics.DrawString(" Test", MyDefaultSkinFontNormal10, Brushes.Black, 5, 70 - 5 + 2);
			e.Graphics.DrawString(Staff_Dev_Int_Str, MyDefaultSkinFontNormal10, Brushes.Black, 160 + 25, 44 + 2);
			e.Graphics.DrawString(Staff_Test_Int_Str, MyDefaultSkinFontNormal10, Brushes.Black, 160 + 25, 70 - 5 + 2);

			e.Graphics.DrawString(" Resources: Contractors", MyDefaultSkinFontBold10, Brushes.Black, 5, 90);

			e.Graphics.DrawString(" Development", MyDefaultSkinFontNormal10, Brushes.Black, 5, 108 + 4);
			e.Graphics.DrawString(" Test", MyDefaultSkinFontNormal10, Brushes.Black, 5, 130 + 4);
			e.Graphics.DrawString(Staff_Dev_Ext_Str, MyDefaultSkinFontNormal10, Brushes.Black, 160 + 25, 108 + 4);
			e.Graphics.DrawString(Staff_Test_Ext_Str, MyDefaultSkinFontNormal10, Brushes.Black, 160 + 25, 130 + 4);

			//Draw the Budget area 
			e.Graphics.DrawString(" Resources: Financial", MyDefaultSkinFontBold10, Brushes.Black, 5, 163 - 3);

			e.Graphics.DrawString(" Budget", MyDefaultSkinFontNormal10, Brushes.Black, 5, 190 - 86 + 10);
			SizeF sf = e.Graphics.MeasureString(Budget_Str, MyDefaultSkinFontBold10);
			e.Graphics.DrawString(Budget_Str, MyDefaultSkinFontBold10, Brushes.Black, 210 - sf.Width, 190 - 6 + 10);		


		}

		protected override void OnPaint(PaintEventArgs e)
		{
			//Setup the Quality and Rendering Options 
			e.Graphics.SmoothingMode = SmoothingMode.None;
			e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
			e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
			SizeF textsize = new SizeF(0,0);

			if (use_single_staff_section)
			{
				DrawSingleSectionMode(e);
			}
			else
			{
				DrawDualSectionMode(e);
			}
		}

	}
}
