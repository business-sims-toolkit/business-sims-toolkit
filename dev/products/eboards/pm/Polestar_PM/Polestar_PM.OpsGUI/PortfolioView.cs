using System;
using System.Collections;

using System.Drawing;
using System.Windows.Forms;

using Network;

using CommonGUI;

using LibCore;

namespace Polestar_PM.OpsGUI
{
	public class PortfolioView : FlickerFreePanel
	{
		Node portfolioNode;
		Hashtable programNodeToView;
		ArrayList programNodes;

		Image backgroundImage;

		int programIndent = 6;

		Font bigFont, smallFont;

		int [] tabs;

		public PortfolioView (Node portfolioNode,  bool isTrainingMode)
		{

			if (isTrainingMode)
			{
				backgroundImage = LibCore.Repository.TheInstance.GetImage(
					LibCore.AppInfo.TheInstance.Location + "images/panels/t_projects_status_back_r3.png");
			}
			else
			{
				backgroundImage = LibCore.Repository.TheInstance.GetImage(
					LibCore.AppInfo.TheInstance.Location + "images/panels/projects_status_back_r3.png");
			}

			this.portfolioNode = portfolioNode;

			tabs = new int [] { 83, 174, 265, 356, 447, 538, 629, 720, 811 };

			programNodeToView = new Hashtable ();
			programNodes = new ArrayList ();
			foreach (Node programNode in portfolioNode.GetSortedChildrenOfType("Program"))
			{
				CreateProgramView(programNode);
			}

			DoSize();

			portfolioNode.ChildAdded += new Network.Node.NodeChildAddedEventHandler (portfolioNode_ChildAdded);
			portfolioNode.ChildRemoved += new Network.Node.NodeChildRemovedEventHandler (portfolioNode_ChildRemoved);

			bigFont = ConstantSizeFont.NewFont(CoreUtils.SkinningDefs.TheInstance.GetData("fontname"), 10, FontStyle.Bold);
			smallFont = ConstantSizeFont.NewFont(CoreUtils.SkinningDefs.TheInstance.GetData("fontname"), 8, FontStyle.Bold);
		}

		void CreateProgramView (Node programNode)
		{
			if (! programNodeToView.ContainsKey(programNode))
			{
				ProgramView view = new ProgramView (programNode);

				int [] offsetTabs = new int [tabs.Length];
				for (int i = 0; i < offsetTabs.Length; i++)
				{
					offsetTabs[i] = tabs[i] - programIndent;
				}
				view.SetTabs(offsetTabs);
				this.Controls.Add(view);
				programNodeToView.Add(programNode, view);
				programNodes.Add(programNode);
				DoSize();
			}
		}

		void DeleteProgramView (Node programNode)
		{
			if (programNodeToView.ContainsKey(programNode))
			{
				ProgramView view = programNodeToView[programNode] as ProgramView;
				this.Controls.Remove(view);
				programNodeToView.Remove(programNode);
				programNodes.Remove(programNode);
				DoSize();
			}
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing)
			{
				foreach (Node programNode in new ArrayList (programNodeToView.Keys))
				{
					DeleteProgramView(programNode);
				}
			}

			portfolioNode.ChildAdded -= new Network.Node.NodeChildAddedEventHandler (portfolioNode_ChildAdded);
			portfolioNode.ChildRemoved -= new Network.Node.NodeChildRemovedEventHandler (portfolioNode_ChildRemoved);

			bigFont.Dispose();
			smallFont.Dispose();

			base.Dispose(disposing);
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);

			DoSize();
		}

		void DoSize ()
		{
			int yGap = 2;

			int top = 42;
			int bottom = 420;
			int y = top;
			int height = (bottom - top) / 6;
			foreach (Node programNode in programNodes)
			{
				ProgramView view = programNodeToView[programNode] as ProgramView;
				view.Location = new Point (programIndent, y + yGap);
				view.Size = new Size (this.Width - (2 * programIndent), height - (yGap * 2));
				y = view.Bottom + yGap;
			}
		}

		private void portfolioNode_ChildAdded (Node sender, Node child)
		{
			if (child.GetAttribute("type") == "Program")
			{
				CreateProgramView(child);
			}
		}

		private void portfolioNode_ChildRemoved (Node sender, Node child)
		{
			DeleteProgramView(child);
		}

		protected override void OnPaint (PaintEventArgs e)
		{
			base.OnPaint(e);

			e.Graphics.DrawImageUnscaled(backgroundImage, 0, 0);

			RenderText(e.Graphics, bigFont, Color.White, "Program", 0, tabs[0], 5, 22);

			for (char stage = 'A'; stage <= 'H'; stage++)
			{
				int column = 1 + (stage - 'A');
				RenderText(e.Graphics, smallFont, Color.Black, stage.ToString(), tabs[column - 1], tabs[column], 24, 38);
			}

			RenderText(e.Graphics, bigFont, Color.White, "Status", tabs[8], Width - 3, 5, 22);
			RenderText(e.Graphics, smallFont, Color.Black, "Money ($)", tabs[8], (tabs[8] + Width - 3) / 2, 24, 38);
			RenderText(e.Graphics, smallFont, Color.Black, "Projection", (tabs[8] + Width - 3) / 2, Width - 3, 24, 38);
		}

		void RenderText (Graphics graphics, Font font, Color colour, string text, int left, int right, int top, int bottom)
		{
			RectangleF rectangle = new RectangleF (left, top, right - left, bottom - top);
			using (StringFormat format = new StringFormat ())
			{
				format.Alignment = StringAlignment.Center;
				format.LineAlignment = StringAlignment.Center;

				using (Brush brush = new SolidBrush (colour))
				{
					graphics.DrawString(text, font, brush, rectangle, format);
				}
			}
		}
	}
}