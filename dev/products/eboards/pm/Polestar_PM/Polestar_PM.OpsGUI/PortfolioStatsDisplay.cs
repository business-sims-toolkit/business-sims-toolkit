using System;
using System.Collections;
using System.Collections.Generic;
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
using System.Text;

using LibCore;
using Network;
using CoreUtils;
using CommonGUI;

namespace Polestar_PM.OpsGUI
{
	public class PortfolioStatsDisplay : GameStatsDisplay
	{
		static System.Globalization.CultureInfo myCI_enGB = new System.Globalization.CultureInfo("en-GB", false);

		private Node BudgetNode = null;

		private NumberFormatInfo nfi = null;

		Node portfoliosNode;
		Node portfolioNode;
		List<Node> programNodes;
		bool BudgetNegative = false;
		bool StaffNegative = false;

		string staffString;

		/// <summary>
		/// This shows the Availability of Staff and the Budget 
		/// There are 2 modes based on the Question (Do we have a single pool of people)
		/// </summary>
		/// <param name="model"></param>
		public PortfolioStatsDisplay (NodeTree model, bool IsTrainingGame)
			: base (model, IsTrainingGame)
		{
			//Just display in GB number format for the time being 
			nfi = new CultureInfo("en-GB", false).NumberFormat;

			BudgetNode = model.GetNamedNode("pmo_budget");
			BudgetNode.AttributesChanged += new Network.Node.AttributesChangedEventHandler(BudgetNode_AttributesChanged);

			programNodes = new List<Node> ();

			portfoliosNode = model.GetNamedNode("Portfolios");
			portfoliosNode.ChildAdded += new Node.NodeChildAddedEventHandler (portfoliosNode_ChildAdded);
			portfoliosNode.ChildRemoved += new Node.NodeChildRemovedEventHandler (portfoliosNode_ChildRemoved);
			portfoliosNode.AttributesChanged += new Node.AttributesChangedEventHandler (portfoliosNode_AttributesChanged);

			portfolioNode = portfoliosNode.GetFirstChildOfType("Portfolio");
			portfolioNode.ChildAdded += new Node.NodeChildAddedEventHandler (portfolioNode_ChildAdded);
			portfolioNode.ChildRemoved += new Node.NodeChildRemovedEventHandler (portfolioNode_ChildRemoved);
			portfolioNode.AttributesChanged += new Node.AttributesChangedEventHandler (portfolioNode_AttributesChanged);

			foreach (Node program in portfolioNode.GetChildrenOfType("Program"))
			{
				AddProgram(program);
			}

			UpdateFigures();
		}

		void portfoliosNode_AttributesChanged (Node sender, ArrayList attrs)
		{
			UpdateFigures();
		}

		void portfoliosNode_ChildAdded (Node sender, Node child)
		{
			UpdateFigures();
		}

		void portfoliosNode_ChildRemoved (Node sender, Node child)
		{
			UpdateFigures();
		}

		void portfolioNode_AttributesChanged (Node sender, ArrayList attrs)
		{
			UpdateFigures();
		}

		void portfolioNode_ChildAdded (Node sender, Node child)
		{
			UpdateFigures();
		}

		void portfolioNode_ChildRemoved (Node sender, Node child)
		{
			UpdateFigures();
		}

		void AddProgram (Node program)
		{
			programNodes.Add(program);
			program.AttributesChanged += new Node.AttributesChangedEventHandler (program_AttributesChanged);
			UpdateFigures();
		}

		void program_AttributesChanged (Node sender, ArrayList attrs)
		{
			UpdateFigures();
		}

		void RemoveProgram (Node program)
		{
			programNodes.Remove(program);
			program.AttributesChanged -= new Node.AttributesChangedEventHandler (program_AttributesChanged);
			UpdateFigures();
		}

		/// <summary>
		/// Dispose ...
		/// </summary>
		public override void Dispose ()
		{
			portfoliosNode.ChildAdded -= new Node.NodeChildAddedEventHandler (portfoliosNode_ChildAdded);
			portfoliosNode.ChildRemoved -= new Node.NodeChildRemovedEventHandler (portfoliosNode_ChildRemoved);
			portfoliosNode.AttributesChanged -= new Node.AttributesChangedEventHandler (portfoliosNode_AttributesChanged);

			portfolioNode.ChildAdded -= new Node.NodeChildAddedEventHandler (portfolioNode_ChildAdded);
			portfolioNode.ChildRemoved -= new Node.NodeChildRemovedEventHandler (portfolioNode_ChildRemoved);
			portfolioNode.AttributesChanged -= new Node.AttributesChangedEventHandler (portfolioNode_AttributesChanged);

			foreach (Node program in new List<Node> (programNodes))
			{
				RemoveProgram(program);
			}

			base.Dispose();
		}

		protected override void UpdateFigures ()
		{
			Budget_Str = "0";

			if (BudgetNode != null)
			{
				int budget_value = BudgetNode.GetIntAttribute("budget_left", 0);

				BudgetNegative = false;
				if (budget_value < 0)
				{
					BudgetNegative = true;
				}
				//budget_value = budget_value / 1000;
				string tempstr = (Math.Abs(budget_value)).ToString("N", nfi);
				tempstr = tempstr.Replace(".00", "");
				Budget_Str = "$" + tempstr;
				if (BudgetNegative)
				{
					Budget_Str = "-" + Budget_Str; 
				}
			}

			int staff = 0;
			if (portfoliosNode != null)
			{
				staff += portfoliosNode.GetIntAttribute("free_resources", 0);

				if (portfolioNode != null)
				{
					staff += portfolioNode.GetIntAttribute("free_resources", 0);
				}
			}

			StaffNegative = false;
			if (staff < 0)
			{
				StaffNegative = true;
			}
			staffString = FormatThousands(staff);

			Invalidate();
		}

		protected override void DrawSingleSectionMode (PaintEventArgs e)
		{
			//Draw the background 
			if (backImage != null)
			{
				e.Graphics.DrawImage(this.backImage, 0, 0, 210, 141);
			}

			//PMO
			Brush textBrush = new SolidBrush(Color.FromArgb(255, 255, 255));  //dark color deep Gray #333333
			e.Graphics.DrawString(" PMO", MyDefaultSkinFontBold10, textBrush, 5, 5 - 2);
			textBrush.Dispose();

			SizeF sf;

			//Draw the Workforce area 
			e.Graphics.DrawString(" Resources: Staff", MyDefaultSkinFontBold10, Brushes.Black, 5, 25);

			if (StaffNegative == false)
			{
				e.Graphics.DrawString(" Total", MyDefaultSkinFontNormal10, Brushes.Black, 5, 44 + 2);
				sf = e.Graphics.MeasureString(staffString, MyDefaultSkinFontNormal10);
				e.Graphics.DrawString(staffString, MyDefaultSkinFontNormal10, Brushes.Black, 205 - sf.Width, 44 + 2);
			}
			else
			{
				e.Graphics.FillRectangle(Brushes.Red, 5 - 1, 44-2 , 203, 29+11);
				e.Graphics.DrawString(" Total", MyDefaultSkinFontNormal10, Brushes.White, 5, 44 + 2);
				sf = e.Graphics.MeasureString(staffString, MyDefaultSkinFontNormal10);
				e.Graphics.DrawString(staffString, MyDefaultSkinFontNormal10, Brushes.White, 205 - sf.Width, 44 + 2);
			}

			//Draw the Budget area 
			e.Graphics.DrawString(" Resources: Financial", MyDefaultSkinFontBold10, Brushes.Black, 5, 80 + 4);

			if (this.BudgetNegative == false)
			{
				e.Graphics.DrawString(" PMO Budget", MyDefaultSkinFontNormal10, Brushes.Black, 5, 110 - 1);
				sf = e.Graphics.MeasureString(Budget_Str, MyDefaultSkinFontNormal10);
				e.Graphics.DrawString(Budget_Str, MyDefaultSkinFontNormal10, Brushes.Black, 205 - sf.Width, 110 - 1);
			}
			else
			{
				e.Graphics.FillRectangle(Brushes.Red, 5-1, 110-8, 203, 29);
				e.Graphics.DrawString(" PMO Budget", MyDefaultSkinFontNormal10, Brushes.White, 5, 110 - 1);
				sf = e.Graphics.MeasureString(Budget_Str, MyDefaultSkinFontNormal10);
				e.Graphics.DrawString(Budget_Str, MyDefaultSkinFontNormal10, Brushes.White, 205 - sf.Width, 110 - 1);
			}
		}

		string FormatThousands (int a)
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
	}
}