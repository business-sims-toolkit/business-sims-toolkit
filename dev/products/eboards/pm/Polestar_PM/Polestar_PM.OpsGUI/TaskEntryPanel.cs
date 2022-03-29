using System;
using System.Collections;

using System.Drawing;
using System.Windows.Forms;

using Network;

using LibCore;
using CoreUtils;

using BusinessServiceRules;

namespace Polestar_PM.OpsGUI
{
	public class TaskEntryPanel : Form
	{
		protected TextBox taskID;
		protected TextBox time;
		protected TextBox extension;

		protected TextBox location;

		protected TextBox server;

		protected TextBox app;

		protected TextBox days;

		protected Button ok;

		protected NodeTree tree;
		protected Node queueNode;
		protected Node timeNode;

		protected void AddLabel (Control item, string text)
		{
			Label label = new Label ();
			label.Text = text;
			label.Location = new Point (item.Left, item.Top - 20);
			this.Controls.Add(label);
		}

		public TaskEntryPanel (NodeTree tree)
		{
			this.ShowInTaskbar = false;

			this.tree = tree;
			queueNode = tree.GetNamedNode("TaskManager");
			timeNode = tree.GetNamedNode("CurrentTime");

			this.Text = "New task";

			taskID = new TextBox ();
			time = new TextBox ();
			extension = new TextBox ();
			location = new TextBox ();
			server = new TextBox ();
			app = new TextBox ();
			days = new TextBox ();

			ok = new Button ();
			ok.Text = "OK";
			ok.Click += new EventHandler(ok_Click);
            ok.Name = "TEP OK Button";

			this.Controls.Add(taskID);
			this.Controls.Add(time);
			this.Controls.Add(extension);
			this.Controls.Add(location);
			this.Controls.Add(server);
			this.Controls.Add(app);
			this.Controls.Add(days);
			this.Controls.Add(ok);

			taskID.Location = new Point (10, 30);
			taskID.Size = new Size (100, taskID.Height);

			time.Location = new Point (taskID.Right + 20, taskID.Top);
			time.Size = new Size (100, taskID.Height);
			extension.Location = new Point (time.Right + 20, taskID.Top);
			extension.Size = new Size (100, taskID.Height);
			location.Location = new Point (extension.Right + 20, taskID.Top);
			location.Size = new Size (200, taskID.Height);

			server.Location = new Point (time.Left, time.Bottom + 40);
			server.Size = new Size (200, taskID.Height);
			app.Location = new Point (server.Right + 20, server.Top);
			app.Size = new Size (200, taskID.Height);

			days.Location = new Point (app.Left, server.Bottom + 40);
			days.Size = new Size (200, taskID.Height);
			ok.Location = new Point (days.Right + 20, days.Top);
			ok.Size = new Size (100, ok.Height);

			AddLabel(taskID, "Opcode");
			AddLabel(time, "Time");
			AddLabel(extension, "Phone #");
			AddLabel(location, "Location");
			AddLabel(server, "Server");
			AddLabel(app, "App/DB");
			AddLabel(days, "# days");

			this.ClientSize = new Size (ok.Right + 10, ok.Bottom + 10);

			this.FormBorderStyle = FormBorderStyle.None;
			this.Opacity = 0.75;
		}

		protected int CountOccurrences (string s, char c)
		{
			int occurrences = 0;
			int start = 0;

			while (start < s.Length)
			{
				int next = s.IndexOf(c, start);

				if (next != -1)
				{
					occurrences++;
					start = 1 + next;
				}
				else
				{
					start = s.Length;
				}
			}

			return occurrences;
		}

		protected int TimeFromString (string s)
		{
			switch (CountOccurrences(s, ':'))
			{
				case 0:
					// Treat it as military-type format (eg 1100 for 11:00).
					return TimeFromString(s.Substring(0, s.Length - 2) + ":" + s.Substring(s.Length - 2, 2));

				case 1:
				case 2:
				{
					int time = ModelTimeManager.ParseTime(s,true);
					int currentTime = timeNode.GetIntAttribute("seconds", 0);

					// Advance the time by however many days needed to make it be in the future.
					while (time < currentTime)
					{
						time += (24 * 60);
					}

					return time;
				}
			}

			return 0;
		}

		private void ok_Click (object sender, EventArgs e)
		{
			if (taskID.Text != "")
			{
				ArrayList attrs = new ArrayList ();
				string timeString = time.Text;
				if (timeString == "")
				{
					timeString = "0";
				}
				attrs.Add(new AttributeValuePair ("start_time", TimeFromString(timeString)));
				attrs.Add(new AttributeValuePair ("taskid", taskID.Text));

				attrs.Add(new AttributeValuePair ("argPHONE", extension.Text));
				attrs.Add(new AttributeValuePair ("argXX_LOCNAME", location.Text));
				attrs.Add(new AttributeValuePair ("argXX_NEWLOC", location.Text));
				attrs.Add(new AttributeValuePair ("argXX_SVRNAME", server.Text));
				attrs.Add(new AttributeValuePair ("argXX_NEWSVRNAME", server.Text));
				attrs.Add(new AttributeValuePair ("argXX_APPNAME", app.Text));
				attrs.Add(new AttributeValuePair ("argXX_OFFNAME", location.Text));
				attrs.Add(new AttributeValuePair ("argXX_DAYS", days.Text));

				new Node (queueNode, "task", "", attrs);
			}

			Close();
		}
	}
}