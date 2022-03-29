using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

using LibCore;
using CoreUtils;
using Network;
using CommonGUI;
using GameManagement;
using Cloud.OpsEngine;

namespace Cloud.OpsScreen
{
	public class ConsolidationPanel : PopupPanel
	{
		NodeTree model;
		Node datacenter;
		Node roundVariables;
		Node time;
		Node consolidations;

		ImageTextButton ok;
		ImageTextButton close;

		ImageTextButton move;
		ImageTextButton reset;

		ToggleButtonBar dataCenterSelectionBar;

		OrderPlanner orderPlanner;
		VirtualMachineManager vmManager;

		Dictionary<Node, ClickableRack> rackToClickableRack;

		NetworkProgressionGameFile gameFile;

		Bitmap FullBackgroundImage;
		Dictionary<int, Point> RackPositions = new Dictionary<int, Point> ();

		int rack_image_6_width = 152;
		int rack_image_6_height = 315;
		int rack_image_4_width = 152;
		int rack_image_4_height = 224;

		int offsetx = 2;
		int offsety = 38;
		int rack_gap_x = 40;
		int rack_gap_y = 10;
		Font Font_Title;

		Dictionary<string, Node> businessDescriptionToNode = new Dictionary<string, Node> ();
		List<string> businessDescriptions = new List<string> ();
		List<ClickableRack> racks = new List<ClickableRack> ();

		public ConsolidationPanel (NetworkProgressionGameFile gameFile, NodeTree model, OrderPlanner orderPlanner, VirtualMachineManager vmManager)
		{
			this.gameFile = gameFile;
			this.model = model;
			this.orderPlanner = orderPlanner;
			this.vmManager = vmManager;

			FullBackgroundImage = (Bitmap)Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\consolidate\\general.png");
		
			string font = SkinningDefs.TheInstance.GetData("fontname");
			Font_Title = FontRepository.GetFont(font, 20, FontStyle.Bold);

			roundVariables = model.GetNamedNode("RoundVariables");

			time = model.GetNamedNode("CurrentTime");
			time.AttributesChanged += new Node.AttributesChangedEventHandler (time_AttributesChanged);

			consolidations = model.GetNamedNode("Server Consolidations");

			AutoScroll = true;

			BuildMainControls();
			Build_Exchange_Selection_Controls();

			DoSize();

			UpdateButtons();
		}

		void UpdateButtons ()
		{
			List<ClickableServer> selectedServers = GetSelectedServers();

			bool anySelected = (selectedServers.Count > 0);
			move.Enabled = true;

			reset.Enabled = (consolidations.GetChildrenOfType("server_consolidation_record").Count > 0);
			ok.Enabled = anySelected && ! move.Active;

			foreach (ClickableServer server in selectedServers)
			{
				if (move.Active)
				{
					if (! orderPlanner.CanServerBeMoved(server.Server))
					{
						server.Selected = false;
					}
				}
				else
				{
					if (! orderPlanner.CanServerBeRetired(server.Server))
					{
						server.Selected = false;
					}
				}
			}

			UpdateRackFreeSpace();
		}

		void UpdateRackFreeSpace ()
		{
			Dictionary<Node, int> rackToFreeCpus = vmManager.GetMinFreeCpusOverTimeForRacks();
			foreach (Node rack in rackToClickableRack.Keys)
			{
				rackToClickableRack[rack].SetFreeCpus(rackToFreeCpus[rack]);
			}
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing)
			{
				time.AttributesChanged -= new Node.AttributesChangedEventHandler (time_AttributesChanged);

				foreach (ClickableRack rack in racks)
				{
					rack.Dispose();
				}
			}

			base.Dispose(disposing);
		}

		void BuildMainControls()
		{
			close = new ImageTextButton (@"images\buttons\button_85x25.png");
			close.SetAutoSize();
			close.SetButtonText("Close", Color.White, Color.White, Color.White, Color.DimGray);
			close.ButtonPressed += new ImageButton.ImageButtonEventArgsHandler (close_ButtonPressed);
			Controls.Add(close);

			ok = new ImageTextButton (@"images\buttons\button_85x25.png");
			ok.SetAutoSize();
			ok.SetButtonText("Retire", Color.White, Color.White, Color.White, Color.DimGray);
			ok.ButtonPressed += new ImageButton.ImageButtonEventArgsHandler (ok_ButtonPressed);
			Controls.Add(ok);

			move = new ImageTextButton (@"images\buttons\button_85x25.png");
			move.SetAutoSize();
			move.SetButtonText("Move", Color.White, Color.White, Color.White, Color.DimGray);
			move.ButtonPressed += new ImageButton.ImageButtonEventArgsHandler (move_ButtonPressed);
			Controls.Add(move);

			reset = new ImageTextButton (@"images\buttons\button_85x25.png");
			reset.SetAutoSize();
			reset.SetButtonText("Reset", Color.White, Color.White, Color.White, Color.DimGray);
			reset.ButtonPressed += new ImageButton.ImageButtonEventArgsHandler (reset_ButtonPressed);
			Controls.Add(reset);
		}

		void Build_Exchange_Selection_Controls()
		{
			int startx = 10;
			int starty = 50;
			int posx = startx;
			int posy = starty;
			int count = 0;

			//Get the Regions
			foreach (Node business in orderPlanner.GetBusinesses())
			{
				string display_str = business.GetAttribute("desc");
				if (businessDescriptionToNode.ContainsKey(display_str) == false)
				{
					businessDescriptionToNode.Add(display_str, business);
					businessDescriptions.Add(display_str);
				}
			}

			ArrayList al = new ArrayList();
			foreach (string ex in businessDescriptions)
			{
				Node tmpBusinessNode = (Node)businessDescriptionToNode[ex];

				al.Add(new ToggleButtonBarItem(count, ex, tmpBusinessNode));
				count++;
			}

			dataCenterSelectionBar = new ToggleButtonBar(false);
			dataCenterSelectionBar.SetAllowNoneSelected(false);
			dataCenterSelectionBar.BackColor = Color.Purple;
			dataCenterSelectionBar.BackColor = Color.Transparent;
			dataCenterSelectionBar.SetOptions(al, 65, 32, 5, 4, "images/buttons/button_70x32_on.png", "images/buttons/button_70x32_active.png",
				"images/buttons/button_70x32_disabled.png", "images/buttons/button_70x32_hover.png", Color.White, "");
			dataCenterSelectionBar.Location = new Point(0, 10);
			dataCenterSelectionBar.sendItemSelected += new ToggleButtonBar.ItemSelectedHandler(tbb_sendItemSelected);

			Controls.Add(dataCenterSelectionBar);

			ShowRacks();
			Refresh();
		}

		void tbb_sendItemSelected(object sender, string item_name_selected, object selected_object)
		{
			string hh = item_name_selected;

			Node BusinessNode = (Node)selected_object;

			datacenter = model.GetNamedNode(BusinessNode.GetAttribute("datacenter"));

			DisposeRacks();
			ShowRacks();
			Refresh();
		}

		void BuildRackPositions()
		{
			rack_gap_y = 0;

			RackPositions.Clear();
			RackPositions.Add(1, new Point(offsetx + (0 * (rack_image_6_width + rack_gap_x)), offsety + (0 * (rack_image_6_height + rack_gap_y))));
			RackPositions.Add(2, new Point(offsetx + (1 * (rack_image_6_width + rack_gap_x)), offsety + (0 * (rack_image_6_height + rack_gap_y))));
			RackPositions.Add(3, new Point(offsetx + (2 * (rack_image_6_width + rack_gap_x)), offsety + (0 * (rack_image_6_height + rack_gap_y))));
			RackPositions.Add(4, new Point(offsetx + (3 * (rack_image_6_width + rack_gap_x)), offsety + (0 * (rack_image_6_height + rack_gap_y))));
			RackPositions.Add(5, new Point(offsetx + (0 * (rack_image_6_width + rack_gap_x)), offsety + (1 * (rack_image_6_height + rack_gap_y))));
			RackPositions.Add(6, new Point(offsetx + (1 * (rack_image_6_width + rack_gap_x)), offsety + (1 * (rack_image_6_height + rack_gap_y))));
			RackPositions.Add(7, new Point(offsetx + (2 * (rack_image_6_width + rack_gap_x)), offsety + (1 * (rack_image_6_height + rack_gap_y))));
			RackPositions.Add(8, new Point(offsetx + (3 * (rack_image_6_width + rack_gap_x)), offsety + (1 * (rack_image_6_height + rack_gap_y))));
			RackPositions.Add(9, new Point(offsetx + (4 * (rack_image_6_width + rack_gap_x)), offsety + (0 * (rack_image_6_height + rack_gap_y))+91));
		}

		Bitmap BuildRackBack(Node Rack, Bitmap FullBackgroundImage, bool selected)
		{ 
			bool is6Rack = (Rack.GetIntAttribute("max_height_u", 6)) == 6;

			Bitmap bp = null;
			Bitmap back_template = null;

			if (is6Rack)
			{
				bp = new Bitmap(rack_image_6_width, rack_image_6_height);

				string image = AppInfo.TheInstance.Location + "\\images\\consolidate\\rack_back6";
				if (selected)
				{
					image += "_selected";
				}
				back_template = (Bitmap)Repository.TheInstance.GetImage(image + ".png");
			}
			else
			{
				bp = new Bitmap(rack_image_4_width, rack_image_4_height);
				string image = AppInfo.TheInstance.Location + "\\images\\consolidate\\rack_back4";
				if (selected)
				{
					image += "_selected";
				}
				back_template = (Bitmap) Repository.TheInstance.GetImage(image + ".png");
			}

			int rack_position_code = Rack.GetIntAttribute("monitor_index", 1);
			//extract Correct Code from Rack object 
			Point pt = new Point(offsetx + (0 * (rack_image_6_width + rack_gap_x)), offsety + (0 * (rack_image_6_height + rack_gap_y)));

			if (RackPositions.ContainsKey(rack_position_code))
			{
				pt = (Point) RackPositions[rack_position_code];
			}

			Graphics g = Graphics.FromImage(bp);

			g.SmoothingMode = SmoothingMode.AntiAlias;
			g.CompositingQuality = CompositingQuality.HighQuality;
			g.PixelOffsetMode = PixelOffsetMode.HighQuality;
			
			Rectangle destRect = new Rectangle(0,0,bp.Width,bp.Height);
			Rectangle srcRect = new Rectangle(pt.X, pt.Y, bp.Width,bp.Height);

			//draw the extracted Background 
			g.DrawImage(FullBackgroundImage, destRect, srcRect, GraphicsUnit.Pixel);

			//draw the Rack Back 
			g.DrawImage(back_template, destRect, destRect, GraphicsUnit.Pixel);
			g.Dispose();

			return bp;
		}

		void time_AttributesChanged (Node sender, ArrayList attrs)
		{
			if (time.GetIntAttribute("seconds", 0) > 0)
			{
				OnClosed();
			}
		}

		void DisposeRacks()
		{
			foreach (ClickableRack cr in racks)
			{
				Controls.Remove(cr);
				cr.ServerClick -= new EventHandler(clickableRack_ServerClick);
				cr.Dispose();
			}
			racks.Clear();
		}

		void ShowRacks ()
		{
			rackToClickableRack = new Dictionary<Node, ClickableRack> ();

			if (datacenter != null)
			{
				foreach (Node rack in datacenter.GetChildrenOfType("rack"))
				{
					ClickableRack clickableRack = new ClickableRack (rack, orderPlanner,
																	BuildRackBack(rack, FullBackgroundImage, false),
																	BuildRackBack(rack, FullBackgroundImage, true));

					clickableRack.ServerClick += new EventHandler (clickableRack_ServerClick);
					clickableRack.MouseEnter += new EventHandler (clickableRack_MouseEnter);
					clickableRack.MouseLeave += new EventHandler (clickableRack_MouseLeave);
					clickableRack.Click += new EventHandler (clickableRack_Click);

					rackToClickableRack.Add(rack, clickableRack);
					Controls.Add(clickableRack);
					racks.Add(clickableRack);
				}
			}

			UpdateRackFreeSpace();
			DoSize();
		}

		void clickableRack_Click (object sender, EventArgs e)
		{
			if (move.Active)
			{
				MoveServers((ClickableRack) sender);
			}
		}

		void MoveServers (ClickableRack rack)
		{
			// If we're moving any servers between clouds, check that we don't reduce any cloud's capacity below
			// what it needs.
			Dictionary<Node, int> rackToFreeCpus = vmManager.GetMinFreeCpusOverTimeForRacks();
			foreach (ClickableServer server in GetSelectedServers())
			{
				if (vmManager.GetCloudControllingLocation(server.Server) != vmManager.GetCloudControllingLocation(rack.Rack))
				{
					int cpus = server.Server.GetIntAttribute("cpus", 0);

					if (rackToFreeCpus[server.Server.Parent] >= cpus)
					{
						rackToFreeCpus[server.Server.Parent] -= cpus;
					}
					else
					{
						MessageBox.Show(CONVERT.Format("Insufficient free capacity in {0} to remove {1}",
						                               server.Server.Parent.GetAttribute("name"),
													   server.Server.GetAttribute("name")));
						return;
					}
				}
			}

			Node plannedOrders = model.GetNamedNode("PlannedOrders");
			plannedOrders.DeleteChildren();

			foreach (ClickableServer server in GetSelectedServers())
			{
				orderPlanner.AddServerMovePlanToQueue(plannedOrders, server.Server, rack.Rack);
			}

			StringBuilder builder = new StringBuilder ();
			foreach (Node error in plannedOrders.GetChildrenOfType("error"))
			{
				builder.AppendLine(error.GetAttribute("message"));
			}

			if (builder.Length > 0)
			{
				MessageBox.Show(builder.ToString());
			}
			else
			{
				Node orderQueue = model.GetNamedNode("IncomingOrders");
				foreach (Node order in plannedOrders.GetChildrenOfType("order"))
				{
					model.MoveNode(order, orderQueue);
				}
			}

			plannedOrders.DeleteChildren();

			UpdateButtons();
		}

		void clickableRack_MouseLeave (object sender, EventArgs e)
		{
			ClickableRack rack = (ClickableRack) sender;

			if (rack.Selected)
			{
				rack.Selected = false;
			}
		}

		List<ClickableServer> GetSelectedServers ()
		{
			List<ClickableServer> servers = new List<ClickableServer> ();

			foreach (ClickableRack clickableRack in rackToClickableRack.Values)
			{
				foreach (ClickableServer clickableServer in clickableRack.Servers)
				{
					if (clickableServer.Selected)
					{
						servers.Add(clickableServer);
					}
				}
			}

			return servers;
		}

		void clickableRack_MouseEnter (object sender, EventArgs e)
		{
			ClickableRack rack = (ClickableRack) sender;

			if (move.Active && (GetSelectedServers().Count > 0))
			{
				if (! rack.Selected)
				{
					rack.Selected = true;
				}
			}
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);
			DoSize();
		}

		void DoSize()
		{
			dataCenterSelectionBar.Size = new Size(285, 35);
			dataCenterSelectionBar.Location = new Point (Width - dataCenterSelectionBar.Width, 0);
			offsety = 0;

			BuildRackPositions();
			close.Location = new Point (Width - 8 - close.Width, Height - 8 - close.Height);
			ok.Location = new Point (close.Left - 8 - ok.Width, close.Top - 8 - ok.Height);
			reset.Location = new Point(ok.Left, ok.Top - 8 - reset.Height);
			move.Location = new Point (reset.Left, reset.Top - 8 - move.Height);

			int rack_count = 1;
			if (datacenter != null)
			{
				Point position = new Point(5, 5);
				foreach (Node rack in datacenter.GetChildrenOfType("rack"))
				{
					ClickableRack clickableRack = rackToClickableRack[rack];

					if (clickableRack.getHeightInU() == 6)
					{
						clickableRack.Size = new Size(rack_image_6_width, rack_image_6_height);
					}
					else
					{
						clickableRack.Size = new Size(rack_image_6_width, rack_image_4_height);
					}
					
					Point p1 = new Point(0,0);
					if (RackPositions.ContainsKey(rack_count))
					{
						p1 = (Point)RackPositions[rack_count];
					}
					clickableRack.Location = p1;
					position = new Point(clickableRack.Right + 5, position.Y);
					rack_count++;
				}
			}
		}

		void clickableRack_ServerClick (object sender, EventArgs e)
		{
			ClickableServer clickableServer = (ClickableServer) sender;

			if (clickableServer.Selected)
			{
				clickableServer.Selected = false;
			}
			else
			{
				if (move.Active || orderPlanner.CanServerBeRetired(clickableServer.Server))
				{
					clickableServer.Selected = true;
				}
			}

			UpdateButtons();
		}

		void ok_ButtonPressed (object sender, ImageButtonEventArgs args)
		{
			Node plannedOrders = model.GetNamedNode("PlannedOrders");
			plannedOrders.DeleteChildren();

			bool canProceed = true;

			List<Node> servers = new List<Node> ();
			foreach (ClickableServer clickableServer in GetSelectedServers())
			{
				servers.Add(clickableServer.Server);
			}
			canProceed = canProceed && orderPlanner.AddServerRetirementPlanToQueue(plannedOrders, servers);

			if (canProceed)
			{
				Node orderQueue = model.GetNamedNode("IncomingOrders");
				foreach (Node order in plannedOrders.GetChildrenOfType("order"))
				{
					model.MoveNode(order, orderQueue);
				}
				plannedOrders.DeleteChildren();
			}
			else
			{
				StringBuilder builder = new StringBuilder ();
				foreach (Node entry in plannedOrders.getChildren())
				{
					if (entry.GetAttribute("type") == "error")
					{
						builder.AppendLine(entry.GetAttribute("message"));
					}
				}

				MessageBox.Show(builder.ToString());
			}

			UpdateButtons();
		}

		void close_ButtonPressed (object sender, ImageButtonEventArgs args)
		{
			OnClosed();
		}

		void move_ButtonPressed (object sender, ImageButtonEventArgs args)
		{
			move.Active = ! move.Active;
			UpdateButtons();
		}

		void reset_ButtonPressed (object sender, ImageButtonEventArgs args)
		{
			Node plannedOrders = model.GetNamedNode("PlannedOrders");
			plannedOrders.DeleteChildren();

			bool canProceed = true;

			List<Node> recordsProcessed = new List<Node> ();
			foreach (Node record in consolidations.GetChildrenOfType("server_consolidation_record"))
			{
				Node rack = model.GetNamedNode(record.GetAttribute("rack"));
				if (rack.Parent == datacenter)
				{
					Node business = model.GetNamedNode(datacenter.GetAttribute("business"));
					Node serverSpec = model.GetNamedNode(record.GetAttribute("server_spec"));

					if (orderPlanner.AddCommissionFreeReplacementServerPlanToQueue(plannedOrders,
					                                                               serverSpec,
																				   business,
																				   rack,
																				   orderPlanner.GetNewServerName(plannedOrders, rack),
																				   record.GetAttribute("server_group"),
																				   record.GetAttribute("owner")))
					{
						recordsProcessed.Add(record);
					}
					else
					{
						canProceed = false;
						break;
					}
				}
			}

			if (canProceed)
			{
				foreach (Node record in recordsProcessed)
				{
					record.Parent.DeleteChildTree(record);
				}

				Node orderQueue = model.GetNamedNode("IncomingOrders");
				foreach (Node order in plannedOrders.GetChildrenOfType("order"))
				{
					model.MoveNode(order, orderQueue);
				}
			}
			else
			{
				StringBuilder builder = new StringBuilder ();
				foreach (Node message in plannedOrders.GetChildrenOfType("error"))
				{
					builder.AppendLine(message.GetAttribute("message"));
				}

				MessageBox.Show(builder.ToString());
			}

			plannedOrders.DeleteChildren();

			UpdateButtons();
		}

		public override Size getPreferredSize()
		{
			return new Size(1020, 680);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
			e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
			e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

			e.Graphics.FillRectangle(Brushes.Purple, 0, 0, Width, Height);
			if (FullBackgroundImage != null)
			{
				//Draw the back
				e.Graphics.DrawImage(FullBackgroundImage, 0, 0, Width, Height);
			}
		}

		public override bool IsFullScreen => true;
	}

	public class ClickableRack : FlickerFreePanel
	{
		string rackTitleStr = string.Empty;
		int title_height = 20;
		int server_1u_height = 47;
		int server_start_y = 29;
		int server_width_border = 12;

		int rack_height_in_u = 6;
		int freeCpus;

		Node rack;
		OrderPlanner orderPlanner;
		bool selected;
		Bitmap RackImageBack = null;
		Bitmap selectedRackImageBack;
		Font Font_TitleBold;

		Dictionary<Node, ClickableServer> serverToClickable;

		public ClickableRack (Node rack, OrderPlanner orderPlanner, Bitmap NewBack, Bitmap selectedBack)
		{
			this.rack = rack;
			this.orderPlanner = orderPlanner;

			string font = SkinningDefs.TheInstance.GetData("fontname");
			Font_TitleBold = FontRepository.GetFont(font, 12, FontStyle.Bold);

			rack_height_in_u = rack.GetIntAttribute("max_height_u", 6);

			RackImageBack = NewBack;
			selectedRackImageBack = selectedBack;

			rack.AttributesChanged += new Node.AttributesChangedEventHandler (rack_AttributesChanged);
			rack.ChildAdded += new Node.NodeChildAddedEventHandler (rack_ChildAdded);
			rack.ChildRemoved += new Node.NodeChildRemovedEventHandler (rack_ChildRemoved);

			UpdateStatus();
		}

		public void SetFreeCpus (int freeCpus)
		{
			this.freeCpus = freeCpus;
			UpdateTitle();
		}

		void rack_AttributesChanged (Node sender, ArrayList attrs)
		{
			UpdateStatus();
		}

		void rack_ChildRemoved (Node sender, Node child)
		{
			UpdateStatus();
		}

		void rack_ChildAdded (Node sender, Node child)
		{
			UpdateStatus();
		}

		void UpdateTitle ()
		{
			string owner = rack.GetAttribute("owner");
			string display_owner = string.Empty;
			switch (owner)
			{
				case "floor":
				case "online":
				case "traditional":
					display_owner = "PD";
					break;
				case "dev&test":
					display_owner = "DT";
					break;
			}

			rackTitleStr = CONVERT.Format("{0} {1} ({2} free)",
										  rack.GetAttribute("desc"),
										  display_owner,
										  freeCpus);
			Invalidate();
		}

		void UpdateStatus ()
		{
			List<Control> controlsToRemove = new List<Control> ();
			foreach (Control control in Controls)
			{
				controlsToRemove.Add(control);
			}
			foreach (Control control in controlsToRemove)
			{
				Controls.Remove(control);
			}

			UpdateTitle();

			if (serverToClickable != null)
			{
				foreach (ClickableServer server in serverToClickable.Values)
				{
					server.Dispose();
				}
			}

			serverToClickable = new Dictionary<Node, ClickableServer> ();

			foreach (Node server in rack.GetChildrenOfType("server"))
			{
				ClickableServer clickableServer = new ClickableServer (server, orderPlanner);
				clickableServer.Click += new EventHandler (clickableServer_Click);
				serverToClickable.Add(server, clickableServer);
				Controls.Add(clickableServer);
			}

			Invalidate();

			Selected = false;
			DoSize();
		}

		public int getHeightInU()
		{
			return rack_height_in_u;
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);
			DoSize();
		}

		void DoSize ()
		{
			int height = Height - title_height;
			int heightPerU = server_1u_height;
			int bay = 0;

			foreach (Node server in serverToClickable.Keys)
			{
				ClickableServer clickable = serverToClickable[server];
				int heightInU = server.GetIntAttribute("height_u", 0);
				clickable.Location = new Point(server_width_border, server_start_y + ((heightPerU-2) * bay));
				clickable.Size = clickable.getRequiredSize();
				bay += (heightInU);
			}
		}

		void clickableServer_Click (object sender, EventArgs e)
		{
			OnServerClick(((ClickableServer) sender).Server);
		}

		public Node Rack
		{
			get
			{
				return rack;
			}
		}

		public List<ClickableServer> Servers
		{
			get
			{
				return new List<ClickableServer> (serverToClickable.Values);
			}
		}

		public event EventHandler ServerClick;

		void OnServerClick (Node server)
		{
			if (ServerClick != null)
			{
				ServerClick(serverToClickable[server], EventArgs.Empty);
			}
		}

		public bool Selected
		{
			get
			{
				return selected;
			}

			set
			{
				selected = value;
				Refresh();
			}
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
			e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
			e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

			e.Graphics.FillRectangle(Brushes.Black, 0, 0, Width, Height);

			Bitmap backImage = (Selected ? selectedRackImageBack : RackImageBack);
			e.Graphics.DrawImage(backImage, 0, 0, backImage.Width, backImage.Height);

			e.Graphics.DrawString(rackTitleStr, Font_TitleBold, Brushes.White, 14, 3);
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing)
			{
				rack.ChildAdded -= new Node.NodeChildAddedEventHandler (rack_ChildAdded);
				rack.ChildRemoved -= new Node.NodeChildRemovedEventHandler (rack_ChildRemoved);
				rack.AttributesChanged -= new Node.AttributesChangedEventHandler (rack_AttributesChanged);

				foreach (ClickableServer server in serverToClickable.Values)
				{
					server.Dispose();
				}
			}

			base.Dispose(disposing);
		}
	}

	public class ClickableServer : FlickerFreePanel
	{
		Node server;
		OrderPlanner orderPlanner;
		Bitmap ServerImageBack = null;
		Bitmap ServerImageBack_Selected = null;
		Bitmap ServerImageBack_Retired = null;
		string DisplayLegend = String.Empty;

		bool selected = false;

		public ClickableServer (Node server, OrderPlanner orderPlanner)
		{
			this.server = server;
			this.orderPlanner = orderPlanner;

			server.AttributesChanged += new Node.AttributesChangedEventHandler (server_AttributesChanged);
			server.ChildAdded += new Node.NodeChildAddedEventHandler (server_ChildAdded);
			server.ChildRemoved += new Node.NodeChildRemovedEventHandler (server_ChildRemoved);
			foreach (Node serverLinkToVmInstance in server.GetChildrenOfType("server_link_to_vm_instance"))
			{
				serverLinkToVmInstance.AttributesChanged += new Node.AttributesChangedEventHandler (serverLinkToVmInstance_AttributesChanged);
			}

			int server_height_in_U = 1;
			server_height_in_U = server.GetIntAttribute("height_u", 1);

			string image_name = "\\images\\consolidate\\Server_"+CONVERT.ToStr(server_height_in_U)+"U.png";
			ServerImageBack = (Bitmap)Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + image_name);

			string image_name_invert = "\\images\\consolidate\\Server_" + CONVERT.ToStr(server_height_in_U) + "U_selected.png";
			ServerImageBack_Selected = (Bitmap)Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + image_name_invert);

			string image_name_retired = "\\images\\consolidate\\Server_" + CONVERT.ToStr(server_height_in_U) + "U_retired.png";
			ServerImageBack_Retired = (Bitmap)Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + image_name_invert);

			Selected = false;

			UpdateLegend();
		}

		void server_ChildAdded (Node sender, Node child)
		{
			UpdateLegend();
		}

		void server_ChildRemoved (Node sender, Node child)
		{
			UpdateLegend();
		}

		void serverLinkToVmInstance_AttributesChanged (Node sender, ArrayList attrs)
		{
			UpdateLegend();
		}

		void UpdateLegend ()
		{
			DisplayLegend = CONVERT.Format("{0} ({1}) {2}",
									server.GetAttribute("name"),
									server.GetAttribute("server_group"),
									server.GetIntAttribute("cpus", 0));

			Invalidate();
		}

		void server_AttributesChanged (Node sender, ArrayList attrs)
		{
			UpdateLegend();
		}

		public bool Selected
		{
			get
			{
				return selected;
			}

			set
			{
				if (selected != value)
				{
					selected = value;
					Refresh();
				}
			}
		}

		public Node Server
		{
			get
			{
				return server;
			}
		}
		
		public Size getRequiredSize()
		{
			return new Size(ServerImageBack.Width, ServerImageBack.Height);
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing)
			{
				server.AttributesChanged -= new Node.AttributesChangedEventHandler (server_AttributesChanged);
				server.ChildAdded -= new Node.NodeChildAddedEventHandler (server_ChildAdded);
				server.ChildRemoved -= new Node.NodeChildRemovedEventHandler (server_ChildRemoved);

				foreach (Node serverLinkToVmInstance in server.GetChildrenOfType("server_link_to_vm_instance"))
				{
					serverLinkToVmInstance.AttributesChanged -= new Node.AttributesChangedEventHandler (serverLinkToVmInstance_AttributesChanged);
				}
			}

			base.Dispose(disposing);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
			e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
			e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

			e.Graphics.FillRectangle(Brushes.Black, 0, 0, Width, Height);
			if (ServerImageBack != null)
			{
				if (selected)
				{
					e.Graphics.DrawImage(ServerImageBack_Selected, 0, 0, ServerImageBack.Width, ServerImageBack.Height);
					e.Graphics.DrawString(DisplayLegend, Font, Brushes.White, 0, 18);
				}
				else
				{
					e.Graphics.DrawImage(ServerImageBack, 0, 0, ServerImageBack.Width, ServerImageBack.Height);
					e.Graphics.DrawString(DisplayLegend, Font, Brushes.Black, 0, 18);
				}
			}
		}
	}
}