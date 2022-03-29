using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using CommonGUI;

using LibCore;
using CoreUtils;

using Network;

using Cloud.OpsEngine;

namespace Cloud.OpsScreen
{
	public class ToolDeploymentPanel : PopupPanel
	{
		protected int WIDTH_Selected = 120;
		protected int WIDTH_Central = 470;
		protected int WIDTH_FeedBack = 230;
		protected int HEIGHT_Title = 25;
		protected int HEIGHT_Panel = 255 - 60;
		protected int HEIGHT_OK = 30;

		protected NodeTree model;
		protected Node roundVariables;

		protected Label lblPanelTitle;
		protected OrderPlanner orderPlanner;

		Node timeNode;
		
		protected ImageTextButton cancel;
		protected ImageTextButton itb_VMM;
		protected ImageTextButton itb_SingleSignON;

		protected Font Font_Title;
		protected Font Font_SubTitle;
		protected Font Font_Body;

		protected Color TitleForeColour = Color.FromArgb(64, 64, 64);
		protected Color SelectedValueForeColour = Color.FromArgb(255, 237, 210);

		protected Bitmap FullBackgroundImage;

		protected Node VMM_Node = null;
		protected Node SSO_Node = null;

		protected bool VMM_Visible = false;
		protected bool SSO_Visible = false;
		protected bool VMM_Enabled = false;
		protected bool SSO_Enabled = false;

		List<ImageTextButton> demandButtons;

		public ToolDeploymentPanel(NodeTree model, OrderPlanner orderPlanner)
		{
			this.model = model;
			this.orderPlanner = orderPlanner;

			FullBackgroundImage = (Bitmap)Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images\\panels\\PopupBack_Requests.png");
			BackgroundImage = FullBackgroundImage;

			string font = SkinningDefs.TheInstance.GetData("fontname");
			Font_Title = FontRepository.GetFont(font, 16, FontStyle.Bold);
			Font_SubTitle = FontRepository.GetFont(font, 12, FontStyle.Bold);
			Font_Body = FontRepository.GetFont(font, 10, FontStyle.Bold);

			roundVariables = model.GetNamedNode("RoundVariables");
			int current_round = roundVariables.GetIntAttribute("current_round", 1);

			VMM_Node = model.GetNamedNode("tool_vmm_node");
			SSO_Node = model.GetNamedNode("tool_sso_node");

			VMM_Visible = false;
			SSO_Visible = false;
			VMM_Enabled = false;
			SSO_Enabled = false;


			//bool vmm_used = VMM_Node.GetBooleanAttribute("status",false);
			//bool sso_used = SSO_Node.GetBooleanAttribute("status", false);
			//int vmm_round = VMM_Node.GetIntAttribute("round",1);
			//int sso_round = SSO_Node.GetIntAttribute("round", 1);

			//bool VMM_Visible = (current_round >= vmm_round);
			//bool SSO_Visible = (current_round >= sso_round);

			//bool VMM_Enabled = (VMM_Visible) & (vmm_used == false);
			//bool SSO_Enabled = (SSO_Visible) & (sso_used == false);

			getButtonStatusDetails(VMM_Node, out VMM_Visible, out VMM_Enabled);
			getButtonStatusDetails(SSO_Node, out SSO_Visible, out SSO_Enabled);

			lblPanelTitle = new Label();
			lblPanelTitle.Location = new Point(0, 0);
			lblPanelTitle.Size = new Size(230, 30);
			lblPanelTitle.ForeColor = Color.White;
			lblPanelTitle.BackColor = Color.Transparent;
			lblPanelTitle.Text = "Optional Demands ";
			lblPanelTitle.Font = Font_Title;
			lblPanelTitle.Visible = true;
			Controls.Add(lblPanelTitle);

			//itb_VMM = new ImageTextButton(@"images\buttons\button_162x40.png");
			//itb_VMM.SetAutoSize();
			////itb_VMM.Size = new System.Drawing.Size(WIDTH_Selected - 10, 40);
			//itb_VMM.Location = new Point(10, 40);
			//itb_VMM.SetButtonText("VMM and Automation", Color.White, Color.White, Color.White, Color.DimGray);
			//itb_VMM.Font = Font_Body;
			//itb_VMM.ButtonFont = Font_Body;
			//itb_VMM.Visible = VMM_Visible;
			//itb_VMM.Enabled = VMM_Enabled;
			//itb_VMM.ButtonPressed += new ImageButton.ImageButtonEventArgsHandler(itb_VMM_ButtonPressed);
			//itb_VMM.DoubleClick += new EventHandler(itb_VMM_DoubleClick);
			//Controls.Add(itb_VMM);

			//itb_SingleSignON = new ImageTextButton(@"images\buttons\button_162x40.png");
			//itb_SingleSignON.SetAutoSize();
			////itb_SingleSignON.Size = new System.Drawing.Size(WIDTH_Selected - 10, 40);
			//itb_SingleSignON.Location = new Point(10, 40 + 10 + itb_VMM.Height);
			//itb_SingleSignON.SetButtonText("Single Sign On ", Color.White, Color.White, Color.White, Color.DimGray);
			//itb_SingleSignON.Font = Font_Body;
			//itb_SingleSignON.ButtonFont = Font_Body;
			//itb_SingleSignON.Visible = SSO_Visible;
			//itb_SingleSignON.Enabled = SSO_Enabled;
			//itb_SingleSignON.ButtonPressed += new ImageButton.ImageButtonEventArgsHandler(itb_SingleSignON_ButtonPressed);
			//itb_SingleSignON.DoubleClick += new EventHandler(itb_SingleSignON_DoubleClick);
			//Controls.Add(itb_SingleSignON);

			Dictionary<int, List<Node>> demandNumberToDemands = new Dictionary<int, List<Node>> ();
			foreach (Node demand in model.GetNodesWithAttributeValue("type", "demand"))
			{
				if (demand.GetBooleanAttribute("optional", false))
				{
					int demandNumber = demand.GetIntAttribute("demand_number", 0);

					if (! demandNumberToDemands.ContainsKey(demandNumber))
					{
						demandNumberToDemands.Add(demandNumber, new List<Node> ());
					}

					demandNumberToDemands[demandNumber].Add(demand);
				}
			}

			timeNode = model.GetNamedNode("CurrentTime");
			int currentTime = timeNode.GetIntAttribute("seconds", 0);

			Label demandLabel = new Label ();
			demandLabel.Text = "Optional Demands";
			demandLabel.Font = Font_Body;
			demandLabel.BackColor = Color.Transparent;
			demandLabel.TextAlign = ContentAlignment.MiddleLeft;
			demandLabel.Size = demandLabel.GetPreferredSize(new Size (1000, 25));
			demandLabel.Location = new Point (10, 100);
			Controls.Add(demandLabel);

			Label demand_tp_Label = new Label();
			demand_tp_Label.Text = "Activate by Period";
			demand_tp_Label.Font = Font_Body;
			demand_tp_Label.BackColor = Color.Transparent;
			demand_tp_Label.TextAlign = ContentAlignment.MiddleLeft;
			demand_tp_Label.Size = demandLabel.GetPreferredSize(new Size(1000, 25));
			demand_tp_Label.Location = new Point(10, 130);
			Controls.Add(demand_tp_Label);

			string current_round_tag = "round_" + CONVERT.ToStr(current_round) + "_Delay";

			demandButtons = new List<ImageTextButton> ();
			List<int> sortedDemandNumbers = new List<int>(demandNumberToDemands.Keys);
			sortedDemandNumbers.Sort();
			int x = 130;
			foreach (int demandNumber in sortedDemandNumbers)
			{
				ImageTextButton button = new ImageTextButton (@"images\buttons\button_65x25.png");
				button.SetButtonText(CONVERT.ToStr(demandNumber), Color.White, Color.White, Color.White, Color.DimGray);
				button.Tag = demandNumberToDemands[demandNumber];
				Controls.Add(button);
				button.SetAutoSize();
				button.Location = new Point (x, 100);
				button.ButtonPressed += new ImageButton.ImageButtonEventArgsHandler (button_ButtonPressed);
				
				//Determine the Delay from dstart of the round 
				int min = 3600;
				foreach (Node demNode in demandNumberToDemands[demandNumber])
				{
					int delay = demNode.GetIntAttribute(current_round_tag, 0);
					min = Math.Min(min, delay);
				}
				min = min / 60;
				min = Math.Max(min - 1, 0); // we want 2 mins before 

				demandButtons.Add(button);

				Node demand = demandNumberToDemands[demandNumber][0];
				button.Active = demand.GetBooleanAttribute("active", false);

				Node sip = null;
				foreach (Node trySip in model.GetNodesWithAttributeValue("type", "new_service"))
				{
					if (trySip.GetAttribute("service_name") == demand.GetAttribute("business_service"))
					{
						sip = trySip;
						break;
					}
				}

				button.Enabled = (demand.GetIntAttribute("delay", 0) > currentTime)
								 && sip.GetBooleanAttribute(CONVERT.Format("available_in_round_{0}", current_round), false);

				//add a label
				Label tmp_lbl = new Label();
				tmp_lbl.Location = new Point(x, 130);
				tmp_lbl.Size = new Size(button.Width, 20);
				tmp_lbl.ForeColor = TitleForeColour;
				tmp_lbl.BackColor = Color.Transparent;
				tmp_lbl.Text = CONVERT.ToStr(min);
				tmp_lbl.Font = Font_Body;
				tmp_lbl.TextAlign = ContentAlignment.MiddleCenter;
				tmp_lbl.Visible = true;
				Controls.Add(tmp_lbl);

				x = button.Right + 10;
			}

			//cancel = new ImageTextButton(@"images\buttons\button_small.png");
			//cancel.SetAutoSize();
			cancel = new ImageTextButton(@"images\buttons\button_85x25.png");
			cancel.SetAutoSize();
			cancel.SetButtonText("Close", Color.White, Color.White, Color.White, Color.DimGray);
			cancel.ButtonPressed += new ImageButton.ImageButtonEventArgsHandler(cancel_ButtonPressed);
			Controls.Add(cancel);
		}

		void button_ButtonPressed (object sender, ImageButtonEventArgs args)
		{
			ImageTextButton button = (ImageTextButton) sender;

			button.Active = ! button.Active;

			Node plannedOrders = model.GetNamedNode("PlannedOrders");
			plannedOrders.DeleteChildren();

			Dictionary<Node, bool> demandToActive = new Dictionary<Node, bool> ();
			foreach (ImageTextButton demandButton in demandButtons)
			{
				foreach (Node demand in (List<Node>) demandButton.Tag)
				{
					demandToActive.Add(demand, demandButton.Active);
				}
			}
			orderPlanner.AddOptionalDemandsPlanToQueue(plannedOrders, demandToActive);

			Node orderQueue = model.GetNamedNode("IncomingOrders");
			foreach (Node order in plannedOrders.GetChildrenOfType("order"))
			{
				model.MoveNode(order, orderQueue);
			}
			plannedOrders.DeleteChildren();
		}

		public void getButtonStatusDetails(Node whichNode, out bool visible, out bool enabled)
		{ 
			visible = false;
			enabled = false;

			bool node_used = whichNode.GetBooleanAttribute("status", false);
			int node_round = whichNode.GetIntAttribute("round", 1);

			int current_round = roundVariables.GetIntAttribute("current_round", 1);

			visible = (current_round >= node_round);
			enabled = (visible) & (node_used == false);
		}

		public void Handle_VMM_Action()
		{
			VMM_Node.SetAttribute("status", "true");
			getButtonStatusDetails(VMM_Node, out VMM_Visible, out VMM_Enabled);

			itb_VMM.Visible = VMM_Visible;
			itb_VMM.Enabled = VMM_Enabled;

			OnClosed();
		}

		protected void itb_VMM_DoubleClick(object sender, EventArgs e)
		{
			Handle_VMM_Action();
		}

		protected void itb_VMM_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			Handle_VMM_Action();
		}

		public void Handle_SingleSignON_Action()
		{
			SSO_Node.SetAttribute("status", "true");
			getButtonStatusDetails(SSO_Node, out SSO_Visible, out SSO_Enabled);

			itb_SingleSignON.Visible = SSO_Visible;
			itb_SingleSignON.Enabled = SSO_Enabled;

			OnClosed();
		}

		protected void itb_SingleSignON_DoubleClick(object sender, EventArgs e)
		{
			Handle_SingleSignON_Action();
		}

		protected void itb_SingleSignON_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			Handle_SingleSignON_Action();
		}

		protected void cancel_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			OnClosed();
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);
			DoSize();
		}

		protected void DoSize()
		{
			cancel.Location = new Point(Width - (cancel.Width + 10), Height - (cancel.Height + 2));
		}

		public override Size getPreferredSize()
		{
			return new Size(800, 255);
		}

		public override bool IsFullScreen => false;
	}
}