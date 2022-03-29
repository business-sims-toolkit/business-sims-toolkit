using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Xml;

using System.Windows.Forms;
using System.Drawing;

using LibCore;
using CoreUtils;
using GameManagement;
using Network;
using CommonGUI;
using Logging;

namespace Polestar_PM.OpsGUI
{
	public class ActivityLog : FlickerFreePanel
	{
		IDataEntryControlHolder controlPanel;

		NetworkProgressionGameFile gameFile;

		Image backImage;

		Font normalFont;
		Font bigBoldFont;
		Font boldFont;

		ImageTextButton closeButton;
		Label title;
		Panel containerPanel;

		int timeX;
		int eventX;
		int y;

		Color [] lineColours;
		int lineCounter;

		public ActivityLog (IDataEntryControlHolder controlPanel, NetworkProgressionGameFile gameFile)
		{
			this.controlPanel = controlPanel;
			this.gameFile = gameFile;

			backImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + @"\images\panels\PM_opsback.png");

			string fontName = SkinningDefs.TheInstance.GetData("fontname");
			normalFont = ConstantSizeFont.NewFont(fontName, 10);
			boldFont = ConstantSizeFont.NewFont(fontName, 10, FontStyle.Bold);
			bigBoldFont = ConstantSizeFont.NewFont(fontName, 12, FontStyle.Bold);

			closeButton = new ImageTextButton (0);
			closeButton.SetVariants(@"images\buttons\button_70x25.png");
			closeButton.Location = new System.Drawing.Point (400, 220);
			closeButton.Size = new System.Drawing.Size (70, 25);
			closeButton.TabIndex = 22;
			closeButton.ButtonFont = boldFont;
			closeButton.SetButtonText("Close",
									  System.Drawing.Color.Black, System.Drawing.Color.Black,
									  System.Drawing.Color.White, System.Drawing.Color.Gray);
			closeButton.Click += new System.EventHandler (closeButton_Click);
			this.Controls.Add(closeButton);

			title = new Label();
			title.Font = bigBoldFont;
			title.BackColor = Color.Transparent;
			title.ForeColor = Color.White;
			title.Location = new Point (85, 8);
			title.Size = new Size (200, 18);
			title.Text = "Activity Log";
			title.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			Controls.Add(title);

			timeX = 0;
			eventX = 90;

			containerPanel = new Panel();
			containerPanel.BackColor = Color.White;
			containerPanel.AutoScroll = true;
			containerPanel.Location = new Point (85, 50);
			containerPanel.Size = new Size (390, closeButton.Top - containerPanel.Top - 5);
			Controls.Add(containerPanel);

			Label timeHeader = new Label();
			timeHeader.Font = normalFont;
			timeHeader.BackColor = Color.Transparent;
			timeHeader.ForeColor = Color.White;
			timeHeader.Location = new Point (timeX + containerPanel.Left, 29);
			timeHeader.Size = new Size (50, 18);
			timeHeader.Text = "Time";
			timeHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			Controls.Add(timeHeader);

			Label eventHeader = new Label();
			eventHeader.Font = normalFont;
			eventHeader.BackColor = Color.Transparent;
			eventHeader.ForeColor = Color.White;
			eventHeader.Location = new Point (eventX + containerPanel.Left, 29);
			eventHeader.Size = new Size (200, 18);
			eventHeader.Text = "Event";
			eventHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			Controls.Add(eventHeader);

			lineColours = new Color [] { Color.FromArgb(255, 255, 255), Color.FromArgb(200, 200, 200) };
			lineCounter = 0;

			BuildActivityList();
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing)
			{
				normalFont.Dispose();
				boldFont.Dispose();
				bigBoldFont.Dispose();
			}

			base.Dispose(disposing);
		}

		public void SetFocus ()
		{
			Focus();
		}

		protected override void OnPaint (PaintEventArgs e)
		{
			base.OnPaint(e);

			e.Graphics.DrawImage(backImage, 0, 0, Width, Height);
		}

		void closeButton_Click (object sender, EventArgs e)
		{
			controlPanel.DisposeEntryPanel();
		}

		void BuildActivityList ()
		{
			NodeTree model = gameFile.NetworkModel;

			Node queueNode = model.GetNamedNode("TaskManager");
			Node incidentsNode = model.GetNamedNode("enteredIncidents");

			string logFile = gameFile.GetRoundFile(gameFile.CurrentRound, "NetworkIncidents.log", GameFile.GamePhase.OPERATIONS);

			BasicIncidentLogReader logReader = new BasicIncidentLogReader(logFile);
			logReader.WatchCreatedNodes(queueNode.GetAttribute("name"), new LogLineFoundDef.LineFoundHandler (queueNode_CreatedNode));
			logReader.WatchCreatedNodes(incidentsNode.GetAttribute("name"), new LogLineFoundDef.LineFoundHandler (incidentsNode_CreatedNode));

			y = 0;
			logReader.Run();
		}

		string GetPlatformNameByNumber (string number)
		{
			switch (number)
			{
				case "1":
					return "X";

				case "2":
					return "Y";

				case "3":
					return "Z";

				default:
					return "?";
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

		string FormatMoney (int a)
		{
			return "$" + FormatThousands(a);
		}

		void queueNode_CreatedNode (object sender, string key, string line, double time)
		{
			string task = "";

			string command = BasicIncidentLogReader.ExtractValue(line, "cmd_type");
			switch (command)
			{
				case "request_new_project":
					task = CONVERT.Format("Set up project {0} {1}",
					                      BasicIncidentLogReader.ExtractValue(line, "prdid"),
					                      GetPlatformNameByNumber(BasicIncidentLogReader.ExtractValue(line, "pltid")));
					break;

				case "cancel_project":
					//task = CONVERT.Format("Cancel project {0}",
					//            BasicIncidentLogReader.ExtractValue(line, "projectnodename").Replace("project", ""));
					task = CONVERT.Format("Cancel project {0}",
											BasicIncidentLogReader.ExtractValue(line, "nodedesc"));
					break;

				case "restaff_project":
					//task = CONVERT.Format("Change resources for project {0}",
					//            BasicIncidentLogReader.ExtractValue(line, "project_node_name").Replace("project", ""));
					task = CONVERT.Format("Change resources for project {0}",
											BasicIncidentLogReader.ExtractValue(line, "nodedesc"));
					break;

				case "droptasks_project":
					task = CONVERT.Format("Drop tasks on project {0} to {1}%",
											BasicIncidentLogReader.ExtractValue(line, "nodedesc"),
											BasicIncidentLogReader.ExtractValue(line, "droppercent"));
					break;

				case "dropcritpath_project":
					task = CONVERT.Format("Drop critical tasks on project {0}",
											BasicIncidentLogReader.ExtractValue(line, "nodedesc"));
					break;


				case "change_pmo":
					task = CONVERT.Format("Change PMO budget to {0}",
										  FormatMoney(CONVERT.ParseInt(BasicIncidentLogReader.ExtractValue(line, "pmo_newvalue"))));
					break;

				case "install_project":
					task = CONVERT.Format("Schedule install of project {0} to {1} on day {2}",
										  BasicIncidentLogReader.ExtractValue(line, "nodedesc"),
										  BasicIncidentLogReader.ExtractValue(line, "install_location"),
										  BasicIncidentLogReader.ExtractValue(line, "install_day"));
					break;

				case "clear_project_install":
					//task = CONVERT.Format("Cancel install of project {0}",
					//            BasicIncidentLogReader.ExtractValue(line, "projectnodename").Replace("project", ""));
					task = CONVERT.Format("Cancel install of project {0}",
											BasicIncidentLogReader.ExtractValue(line, "nodedesc"));
					break;

				case "pause_project":
					//task = CONVERT.Format("Pause project {0}",
					//            BasicIncidentLogReader.ExtractValue(line, "projectnodename").Replace("project", ""));
					task = CONVERT.Format("Pause project {0}",
											BasicIncidentLogReader.ExtractValue(line, "nodedesc"));
					break;

				case "resume_project":
					//task = CONVERT.Format("Resume project {0}",
					//            BasicIncidentLogReader.ExtractValue(line, "projectnodename").Replace("project", ""));
					task = CONVERT.Format("Resume project {0}",
											BasicIncidentLogReader.ExtractValue(line, "nodedesc"));
					break;

				case "request_cc":
					task = CONVERT.Format("Schedule change {0} for installation to {1} on day {2}",
										  BasicIncidentLogReader.ExtractValue(line, "cc_id"),
										  BasicIncidentLogReader.ExtractValue(line, "cc_location"),
										  BasicIncidentLogReader.ExtractValue(line, "cc_day"));
					break;
			}

			AddLine(time, task);
		}

		void incidentsNode_CreatedNode (object sender, string key, string line, double time)
		{
			AddLine(time, "Incident " + BasicIncidentLogReader.ExtractLastValue(line, "id"));
		}

		void AddLine (double time, string message)
		{
			Label timeField = new Label ();
			timeField.Text = FormatTime(time);
			timeField.Font = normalFont;
			timeField.Location = new Point (timeX, y);
			containerPanel.Controls.Add(timeField);

			Label messageField = new Label ();
			messageField.Text = message;
			messageField.Font = normalFont;
			messageField.Location = new Point(eventX, y);
			messageField.Width = containerPanel.Width - 20 - messageField.Left;
			using (Graphics graphics = messageField.CreateGraphics())
			{
				SizeF size = graphics.MeasureString(messageField.Text, messageField.Font, messageField.Width);
				messageField.Height = 5 + (int) Math.Ceiling(size.Height);
			}

			messageField.BackColor = lineColours[lineCounter % lineColours.Length];
			timeField.Size = new Size (messageField.Left - timeX, messageField.Height);
			timeField.BackColor = messageField.BackColor;
			
			lineCounter++;
			containerPanel.Controls.Add(messageField);
			y = messageField.Bottom;
		}

		string FormatTime (double time)
		{
			return CONVERT.Format("{0}:{1:00}", (int) (time / 60), ((int) time) % 60);
		}
	}
}