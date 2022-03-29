using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

using LibCore;
using GameManagement;
using Network;

using Cloud.OpsEngine;

namespace Cloud.ReportsScreen
{
	public class RackReport : Panel
	{
		Label nameLabel;
		List<Label> newServerLabels;

		public RackReport (Node rack, Dictionary<Node, int> rackToMinFreeCpusOverTime, int round)
		{
			BorderStyle = BorderStyle.FixedSingle;

			newServerLabels = new List<Label> ();

			int cpuTotal = 0;
			foreach (Node server in rack.GetChildrenOfType("server"))
			{
				cpuTotal += server.GetIntAttribute("cpus", 0);

				if (server.GetIntAttribute("created_in_round", 0) == rack.Tree.GetNamedNode("RoundVariables").GetIntAttribute("current_round", 0))
				{
					Label serverLabel = new Label ();
					serverLabel.Text = CONVERT.Format("New: {0}", server.GetAttribute("name"));
					Controls.Add(serverLabel);
					newServerLabels.Add(serverLabel);
				}
			}
			int cpuAvailable = rackToMinFreeCpusOverTime[rack];

			StringBuilder detailsBuilder = new StringBuilder ();
			string owner = rack.GetAttribute("owner");
			if (1 < round)
			{
				switch (owner)
				{
					case "floor":
					case "online":
					case "traditional":
						owner = "production";
						break;
				}
			}

			if (!string.IsNullOrEmpty(owner))
			{
				if (detailsBuilder.Length > 0)
				{
					detailsBuilder.Append(", ");
				}
				detailsBuilder.Append(owner.Replace("&", "&&"));
			}
			if (cpuTotal > 0)
			{
				if (detailsBuilder.Length > 0)
				{
					detailsBuilder.Append(", ");
				}
				detailsBuilder.Append(CONVERT.Format("{0} CPU, {1} free", cpuTotal, cpuAvailable));
			}

			StringBuilder mainBuilder = new StringBuilder ();
			mainBuilder.Append(rack.GetAttribute("name"));
			if (detailsBuilder.Length > 0)
			{
				mainBuilder.AppendFormat(" ({0})", detailsBuilder.ToString());
			}

			nameLabel = new Label ();
			nameLabel.Text = mainBuilder.ToString();
			Controls.Add(nameLabel);

			DoSize();
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);

			DoSize();
		}

		void DoSize ()
		{
			int y = 0;

			nameLabel.Location = new Point (0, y);
			nameLabel.Size = new Size (Width, 20);
			nameLabel.TextAlign = ContentAlignment.MiddleCenter;
			y = nameLabel.Bottom;

			foreach (Label server in newServerLabels)
			{
				server.Location = new Point (0, y);
				server.Size = new Size (Width, 20);
				y = server.Bottom;
			}
		}
	}

	public class DatacenterReport : Panel
	{
		Label label;

		List<RackReport> rackReports;

		public DatacenterReport (Node datacenter, Dictionary<Node, int> rackToMinFreeCpusOverTime, int round)
		{
			BorderStyle = BorderStyle.FixedSingle;

			label = new Label ();
			label.Text = datacenter.GetAttribute("desc");
			Controls.Add(label);

			rackReports = new List<RackReport> ();
			foreach (Node rack in datacenter.GetChildrenOfType("rack"))
			{
				RackReport report = new RackReport (rack, rackToMinFreeCpusOverTime, round);
				rackReports.Add(report);
				Controls.Add(report);
			}

			DoSize();
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);

			DoSize();
		}

		void DoSize ()
		{
			label.Location = new Point (0, 0);
			label.Size = new Size (Width, 20);
			label.TextAlign = ContentAlignment.MiddleCenter;

			int y = label.Bottom;
			Size rackSize = new Size (Width, (Height - y) / rackReports.Count);

			foreach (RackReport rack in rackReports)
			{
				rack.Size = rackSize;
				rack.Location = new Point (0, y);
				y = rack.Bottom;
			}
		}
	}

	public class NetworkReport : Panel
	{
		NetworkProgressionGameFile gameFile;
		int round;

		List<DatacenterReport> datacenterReports;

		public NetworkReport (NetworkProgressionGameFile gameFile, int round)
		{
			this.gameFile = gameFile;
			this.round = round;

			NodeTree model = new NodeTree (gameFile.GetNetworkModel(round));
			model.GetNamedNode("CurrentTime").SetAttribute("seconds", 0);
			BauManager bauManager = new BauManager (model);
			VirtualMachineManager vmManager = new VirtualMachineManager (model, bauManager);

			List<Node> datacenters = new List<Node> ();
			foreach (Node datacenter in model.GetNodesWithAttributeValue("type", "datacenter"))
			{
				if (! datacenter.GetBooleanAttribute("hidden", false))
				{
					datacenters.Add(datacenter);
				}
			}
			datacenters.Sort(delegate (Node a, Node b)
			                 {
								 Node businessA = model.GetNamedNode(a.GetAttribute("business"));
								 Node businessB = model.GetNamedNode(b.GetAttribute("business"));

								 return businessA.GetIntAttribute("order", 0).CompareTo(businessB.GetIntAttribute("order", 0));
							 });

			Dictionary<Node, int> rackToMinFreeCpusOverTime = vmManager.GetMinFreeCpusOverTimeForRacks();

			datacenterReports = new List<DatacenterReport> ();
			foreach (Node datacenter in datacenters)
			{
				DatacenterReport report = new DatacenterReport (datacenter, rackToMinFreeCpusOverTime, round);
				datacenterReports.Add(report);
				Controls.Add(report);
			}

			DoSize();
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);

			DoSize();
		}

		void DoSize ()
		{
			int x = 0;
			Size datacenterSize = new Size (Width / datacenterReports.Count, Height);

			foreach (DatacenterReport report in datacenterReports)
			{
				report.Size = datacenterSize;
				report.Location = new Point (x, 0);
				x = report.Right;
			}
		}
	}
}