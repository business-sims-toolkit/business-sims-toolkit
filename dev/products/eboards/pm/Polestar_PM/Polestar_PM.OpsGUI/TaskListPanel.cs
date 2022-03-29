using System;
using System.Drawing;
using System.Windows.Forms;

using Network;

using LibCore;
using CoreUtils;

using BusinessServiceRules;

namespace Polestar_PM.OpsGUI
{
	public class TaskListPanel : Form
	{
		protected NodeTree tree;
		protected Node queueNode;

		protected Font font;

		protected Button ok;

		public TaskListPanel (NodeTree tree)
		{
			this.ShowInTaskbar = false;

			this.tree = tree;
			queueNode = tree.GetNamedNode("TaskManager");
			queueNode.ChildAdded += new Network.Node.NodeChildAddedEventHandler (queueNode_ChildAdded);			
			queueNode.ChildRemoved += new Network.Node.NodeChildRemovedEventHandler (queueNode_ChildRemoved);

			foreach (Node task in queueNode.getChildren())
			{
				task.AttributesChanged += new Network.Node.AttributesChangedEventHandler (task_AttributesChanged);
			}

			this.Text = "Scheduled tasks";

			ok = new Button ();
			ok.Text = "OK";
			ok.Click += new EventHandler(ok_Click);
            ok.Name = "TLP OK2 Button";
			this.Controls.Add(ok);

			font = ConstantSizeFont.NewFont("Times New Roman", 10);

			this.FormBorderStyle = FormBorderStyle.None;
			this.Opacity = 0.75;
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			ok.Location = new Point (Width - ok.Width - 10, Height - ok.Height - 10);
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing)
			{
				foreach (Node task in queueNode.getChildren())
				{
					task.AttributesChanged -= new Network.Node.AttributesChangedEventHandler (task_AttributesChanged);
				}

				queueNode.ChildAdded -= new Network.Node.NodeChildAddedEventHandler (queueNode_ChildRemoved);
				queueNode.ChildRemoved -= new Network.Node.NodeChildRemovedEventHandler (queueNode_ChildRemoved);
			}

			base.Dispose(disposing);
		}

		private void queueNode_ChildAdded (Node sender, Node child)
		{
			sender.AttributesChanged += new Network.Node.AttributesChangedEventHandler(task_AttributesChanged);

			Update();
		}

		private void queueNode_ChildRemoved (Node sender, Node child)
		{
			sender.AttributesChanged -= new Network.Node.AttributesChangedEventHandler(task_AttributesChanged);

			Update();
		}

		private void task_AttributesChanged(Node sender, System.Collections.ArrayList attrs)
		{
			Update();
		}

		new protected void Update ()
		{
			this.Invalidate();
		}

		protected override void OnPaint (PaintEventArgs e)
		{
			base.OnPaint (e);

			int y = 20;
			foreach (Node task in queueNode.getChildren())
			{
				string s = task.GetAttribute("id");

				string args = "";
				for (int i = 0; i < 10; i++)
				{
					string arg = task.GetAttribute("arg" + CONVERT.ToStr(i));

					if (arg.Length > 0)
					{
						if (args.Length > 0)
						{
							args += " ";
						}

						args += arg;
					}
				}

				s += " '" + args + "' " + ModelTimeManager.TimeToStringWithDay(task.GetIntAttribute("start_time", 0)) + " " + task.GetAttribute("duration_total") + " " + task.GetAttribute("completed");
				e.Graphics.DrawString(s, font, Brushes.Black, 10, y);
				y += 20;
			}
		}

		private void ok_Click(object sender, EventArgs e)
		{
			Close();
		}
	}
}