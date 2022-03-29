using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;

using CommonGUI;

using LibCore;
using CoreUtils;

using Network;

using Cloud.OpsEngine;

namespace Cloud.OpsScreen
{
	public class RequestPanel_NewStyle : PopupPanel
	{
		protected int WIDTH_Selected = 120;
		protected int WIDTH_Central = 470;
		protected int WIDTH_FeedBack = 230;
		protected int HEIGHT_Title = 25;
		protected int HEIGHT_Panel = 255-60;
		protected int HEIGHT_OK = 30;

		protected OrderPlanner orderPlanner;
		VirtualMachineManager vmManager;

		protected NodeTree model;
		protected Node selectedBusinessNode = null;
		protected Node selectedServiceNode = null;
		protected Node selectedVmSpecNode = null;
		protected Node selectedRackNode = null;
		protected Node selectedDCNode = null;
		//protected Node selectedPVNode = null;

		protected Node selectedCloudProvider = null;
		protected string selectedCloudChargeModel;
		protected int currentRound = 1;

		protected Node roundVariables;
		protected Node plannedOrders;
		protected Node orderQueue;

		protected string Selected_ExchangeName = "";
		protected string Selected_ServiceName = "";

		protected Hashtable ExchangeNodes = new Hashtable();
		protected ArrayList ExchangeNodeNameList = new ArrayList();
		protected Hashtable ExchangeButtons = new Hashtable();
		protected Hashtable ExchangeOtherCtrls = new Hashtable();

		protected Hashtable ServiceNodes = new Hashtable();
		protected ArrayList ServiceNodeNameList = new ArrayList();
		protected Hashtable ServiceButtons = new Hashtable();
		protected Hashtable ServiceOtherCtrls = new Hashtable();

		protected Hashtable VMSpecNodes = new Hashtable();
		protected ArrayList VMSpecNodeNameList = new ArrayList();
		protected Hashtable VMSpecButtons = new Hashtable();
		protected Hashtable RackNodes = new Hashtable();
		protected ArrayList RackNodeNameList = new ArrayList();
		protected Hashtable RackButtons = new Hashtable();
		protected Hashtable VMRackOtherCtrls = new Hashtable();

		protected Hashtable DCButtons = new Hashtable();
		protected Hashtable PVButtons = new Hashtable();

		protected ImageTextButton ok;
		protected ImageTextButton cancel;

		protected Panel pnl_Selection;
		protected Panel pnl_ExchangeChoice;
		protected Panel pnl_ServiceChoice;
		protected Panel pnl_VMRackChoice;
		protected Panel pnl_FeedBack;

		ImageTextButton discardAllButton;

		protected Label lblPanelTitle;
		protected Label lblSelectionExchangeTitle;
		protected Label lblSelectExchangeTitle;
		protected Label lblSelectServiceTitle1;
		protected Label lblSelectServiceTitle2;
		protected Label lblSelectItemTitle1;
		protected Label lblSelectItemTitle2;
		protected Label lblSelectItemTitle3;

		protected Label lblFeedbackPanelTitle;
		FeedbackPanel feedbackPanel;
		protected ImageTextButton itb_SelectedExchange;

		protected Label lblSelectionServiceTitle;
		protected ImageTextButton itb_SelectedService;

		protected Font Font_Title;
		protected Font Font_SubTitle;
		protected Font Font_Body;
		protected Font Font_ServiceBtns;

		protected Color TitleForeColour = Color.FromArgb(64, 64, 64);
		protected Color SelectedValueForeColour = Color.FromArgb(255, 237, 210);
		protected Color FeedBackForeColour = Color.FromArgb(255, 255,255);

		protected Bitmap FullBackgroundImage;

		protected bool allowDevelopmentWorkReuse;
		protected bool poolDevelopmentResourcesGlobally;
		protected bool poolProductionResourcesWithinDatacenter;

		public RequestPanel_NewStyle (NodeTree model, OrderPlanner orderPlanner, VirtualMachineManager vmManager)
		{
			this.model = model;
			this.orderPlanner = orderPlanner;
			this.vmManager = vmManager;

			plannedOrders = model.GetNamedNode("PlannedOrders");
			orderQueue = model.GetNamedNode("IncomingOrders");

			FullBackgroundImage = (Bitmap)Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images\\panels\\PopupBack_Requests.png");
			BackgroundImage = FullBackgroundImage;

			string font = SkinningDefs.TheInstance.GetData("fontname");
			Font_Title = FontRepository.GetFont(font, 16, FontStyle.Bold);
			Font_SubTitle = FontRepository.GetFont(font, 12, FontStyle.Bold);
			Font_ServiceBtns = FontRepository.GetFont(font, 14, FontStyle.Bold);
			Font_Body = FontRepository.GetFont(font, 8, FontStyle.Bold);

			roundVariables = model.GetNamedNode("RoundVariables");
			allowDevelopmentWorkReuse = roundVariables.GetBooleanAttribute("shared_development", false);
			poolDevelopmentResourcesGlobally = roundVariables.GetBooleanAttribute("global_private_cloud_deployment_allowed", false);
			poolProductionResourcesWithinDatacenter = roundVariables.GetBooleanAttribute("online_and_floor_can_share_racks", false);

			currentRound = roundVariables.GetIntAttribute("current_round", 0);

			BuildBaseData();

			BuildMainControls();
			BuildSelectedControls();
			BuildExchangeChoiceControls();
			BuildServiceChoiceControls();
			BuildVMRackChoiceControls();

			BuildFeedBackControls();

			FillExchangeControls();
		}

		void discardAllButton_ButtonPressed (object sender, ImageButtonEventArgs args)
		{
			plannedOrders.DeleteChildren();

			Node developmentService = model.GetNamedNode(selectedServiceNode.GetAttribute("dev_service_name"));
			if (developmentService != null)
			{
				if (model.GetNamedNode(developmentService.GetAttribute("business")) == selectedBusinessNode)
				{
					orderPlanner.AddDiscardServicePlanToQueue(plannedOrders, developmentService);
				}
			}

			Node productionService = model.GetNamedNode(selectedServiceNode.GetAttribute("service_name"));
			if (productionService != null)
			{
				orderPlanner.AddDiscardServicePlanToQueue(plannedOrders, productionService);
			}

			foreach (Node order in plannedOrders.GetChildrenOfType("order"))
			{
				model.MoveNode(order, orderQueue);
			}
			plannedOrders.DeleteChildren();

			OnClosed();
		}

		#region Utility Methods 

		private string GetSipNameAndState(Node sip)
		{
			Node productionBusinessService = model.GetNamedNode(sip.GetAttribute("service_name"));
			Node devBusinessService = model.GetNamedNode(sip.GetAttribute("dev_service_name"));

			if ((productionBusinessService != null) && (productionBusinessService.GetAttribute("requires_dev") == ""))
			{
				if (productionBusinessService.GetIntAttribute("time_till_ready", 0) > 0)
				{
					return CONVERT.Format("{0} {1} (in production, waiting on storage {2})", sip.GetAttribute("sip_code"), sip.GetAttribute("service_code"), CONVERT.ToTimeStr(productionBusinessService.GetIntAttribute("time_till_ready", 0)));
				}
				else
				{
					return CONVERT.Format("{0} {1} (in production)", sip.GetAttribute("sip_code"), sip.GetAttribute("service_code"));
				}
			}
			else if (devBusinessService != null)
			{
				if (devBusinessService.GetIntAttribute("time_till_ready", 0) > 0)
				{
					return CONVERT.Format("{0} {1} (in development, waiting on storage {2})", sip.GetAttribute("sip_code"), sip.GetAttribute("service_code"), CONVERT.ToTimeStr(devBusinessService.GetIntAttribute("time_till_ready", 0)));
				}
				else if (devBusinessService.GetIntAttribute("dev_countdown", 0) > 0)
				{
					return CONVERT.Format("{0} {1} (in development {2})", sip.GetAttribute("sip_code"), sip.GetAttribute("service_code"), CONVERT.ToTimeStr(devBusinessService.GetIntAttribute("dev_countdown", 0)));
				}
				else if (devBusinessService.GetIntAttribute("handover_countdown", 0) > 0)
				{
					return CONVERT.Format("{0} {1} (in handover {2})", sip.GetAttribute("sip_code"), sip.GetAttribute("service_code"), CONVERT.ToTimeStr(devBusinessService.GetIntAttribute("handover_countdown", 0)));
				}
				else
				{
					return CONVERT.Format("{0} {1} (developed)", sip.GetAttribute("sip_code"), sip.GetAttribute("service_code"));
				}
			}
			else
			{
				return CONVERT.Format("{0} {1}", sip.GetAttribute("sip_code"), sip.GetAttribute("service_code"));
			}
		}

		void UpdateFeedbackText ()
		{
			feedbackPanel.ReflectPlannedOrders(plannedOrders);
		}

		protected void RefreshPlannedOrders()
		{
			plannedOrders.DeleteChildren();

			if (selectedServiceNode != null)
			{
				Node selectedProductionDeploymentLocation = null;

				if (selectedRackNode != null)
				{
					selectedProductionDeploymentLocation = vmManager.GetCloudControllingLocation(selectedRackNode);
				}
				else if (selectedDCNode != null)
				{
					if (selectedDCNode.GetAttribute("type") == "server")
					{
						Debug.Assert(selectedDCNode.GetBooleanAttribute("is_cloud_server", false));
						selectedProductionDeploymentLocation = vmManager.GetCloudControllingLocation(selectedDCNode);
					}
					else
					{
						Node business = model.GetNamedNode(selectedDCNode.GetAttribute("business"));
						string region = business.GetAttribute("region");
						selectedProductionDeploymentLocation = model.GetNamedNode(region + " Production Cloud");
					}
				}

				string selectedVmSpecNodeName = "";

				if (selectedVmSpecNode != null)
				{
					selectedVmSpecNodeName = selectedVmSpecNode.GetAttribute("name");
				}

				bool canProceed = orderPlanner.AddServiceCommissionPlanToQueue(plannedOrders, selectedServiceNode,
					selectedVmSpecNodeName, selectedProductionDeploymentLocation);

				if (canProceed
					&& (selectedDCNode != null)
					&& selectedDCNode.GetBooleanAttribute("is_cloud_server", false))
				{
					canProceed = orderPlanner.AddCloudDeploymentInfoToServiceCommission(plannedOrders, selectedServiceNode, selectedCloudProvider, selectedCloudChargeModel);
				}

				ok.Enabled = canProceed;
			}
			else
			{
				ok.Enabled = false;
			}

			UpdateFeedbackText();
		}

		#endregion  Utility Methods

		protected void BuildBaseData()
		{
			//Get the Regions
			foreach (Node business in orderPlanner.GetBusinesses())
			{
				string display_str = business.GetAttribute("desc");
				if (ExchangeNodes.ContainsKey(display_str)==false)
				{
					ExchangeNodes.Add(display_str, business);
					ExchangeNodeNameList.Add(display_str);
				}
			}
		}

		protected void BuildMainControls()
		{
			lblPanelTitle = new Label();
			lblPanelTitle.Location = new Point(0, 0);
			lblPanelTitle.Size = new Size(230, 30);
			lblPanelTitle.ForeColor = Color.White;
			lblPanelTitle.BackColor = Color.Transparent;
			lblPanelTitle.Text = "Create New Request";
			lblPanelTitle.Font = Font_Title;
			lblPanelTitle.Visible = true;
			Controls.Add(lblPanelTitle);

			ok = new ImageTextButton(@"images\buttons\button_85x25.png");
			ok.SetAutoSize();
			ok.SetButtonText("OK", Color.White, Color.White, Color.White, Color.DimGray);
			ok.SetFocusColours(Color.White);
			ok.ButtonFont = Font_SubTitle;
			ok.ButtonPressed += new ImageButton.ImageButtonEventArgsHandler(ok_ButtonPressed);
			ok.Visible = false;
			Controls.Add(ok);

			discardAllButton = new ImageTextButton (@"images\buttons\button_85x25.png");
			discardAllButton.SetButtonText("Discard", Color.White, Color.White, Color.White, Color.White);
			discardAllButton.SetAutoSize();
			discardAllButton.ButtonPressed += new ImageButton.ImageButtonEventArgsHandler (discardAllButton_ButtonPressed);
			Controls.Add(discardAllButton);
			discardAllButton.Visible = false;

			cancel = new ImageTextButton(@"images\buttons\button_85x25.png");
			cancel.SetAutoSize();
			cancel.SetButtonText("Cancel", Color.White, Color.White, Color.White, Color.DimGray);
			cancel.SetFocusColours(Color.White);
			cancel.ButtonFont = Font_SubTitle;
			cancel.ButtonPressed += new ImageButton.ImageButtonEventArgsHandler(cancel_ButtonPressed);
			Controls.Add(cancel);
		}

		protected void BuildSelectedControls()
		{
			pnl_Selection = new Panel();
			pnl_Selection.Location = new Point(0, 30);
			pnl_Selection.Size = new Size(WIDTH_Selected, HEIGHT_Panel);
			pnl_Selection.BackColor = Color.Transparent;
			//pnl_Selection.BackColor = Color.Pink;
			pnl_Selection.Visible = true;
			Controls.Add(pnl_Selection);

			lblSelectionExchangeTitle = new Label();
			lblSelectionExchangeTitle.ForeColor = TitleForeColour;
			lblSelectionExchangeTitle.Location = new Point(5, 10);
			lblSelectionExchangeTitle.Size = new Size(120, 30);
			lblSelectionExchangeTitle.Text = "Exchange";
			lblSelectionExchangeTitle.Font = Font_Title;
			lblSelectionExchangeTitle.Visible = false;
			pnl_Selection.Controls.Add(lblSelectionExchangeTitle);

			itb_SelectedExchange = new ImageTextButton(@"images\buttons\button_85x25.png");
			//itb_SelectedExchange.SetAutoSize();
			itb_SelectedExchange.Size = new Size(WIDTH_Selected-10,40);
			itb_SelectedExchange.Location = new Point(5, 40);
			itb_SelectedExchange.SetButtonText("Exchanges", Color.White, Color.White, Color.White, Color.DimGray);
			itb_SelectedExchange.ButtonFont = Font_SubTitle;
			itb_SelectedExchange.SetFocusColours(Color.White);
			itb_SelectedExchange.Visible = false;
			itb_SelectedExchange.ButtonPressed += new ImageButton.ImageButtonEventArgsHandler(itb_SelectedExchange_ButtonPressed);
			itb_SelectedExchange.DoubleClick += new EventHandler(itb_SelectedExchange_DoubleClick);
			pnl_Selection.Controls.Add(itb_SelectedExchange);

			lblSelectionServiceTitle = new Label();
			lblSelectionServiceTitle.ForeColor = TitleForeColour;
			lblSelectionServiceTitle.Location = new Point(5, 90+10);
			lblSelectionServiceTitle.Size = new Size(120, 30);
			lblSelectionServiceTitle.Text = "Service";
			lblSelectionServiceTitle.Font = Font_Title;
			lblSelectionServiceTitle.Visible = false;
			pnl_Selection.Controls.Add(lblSelectionServiceTitle);

			itb_SelectedService = new ImageTextButton(@"images\buttons\button_85x25.png");
			//itb_SelectedService.SetAutoSize();
			itb_SelectedService.Size = new Size(WIDTH_Selected - 10, 40);
			itb_SelectedService.Location = new Point(5, 120 + 10);
			itb_SelectedService.SetButtonText("Services", Color.White, Color.White, Color.White, Color.DimGray);
			itb_SelectedService.ButtonFont = Font_SubTitle;
			itb_SelectedService.SetFocusColours(Color.White);
			itb_SelectedService.Visible = false;
			itb_SelectedService.ButtonPressed += new ImageButton.ImageButtonEventArgsHandler(itb_SelectedService_ButtonPressed);
			itb_SelectedService.DoubleClick += new EventHandler(itb_SelectedService_DoubleClick);
			pnl_Selection.Controls.Add(itb_SelectedService);
		}

		protected void Handle_SelectedExchange_Action()
		{
			//Clear the Selected nodes 
			selectedBusinessNode = null;
			selectedServiceNode = null;

			//Handle the various panels 
			pnl_VMRackChoice.Visible = false;
			pnl_ServiceChoice.Visible = false;
			pnl_ExchangeChoice.Visible = true;

			//Clear The displed selected items
			lblSelectionExchangeTitle.Visible = false;
			itb_SelectedExchange.Visible = false;
			lblSelectionServiceTitle.Visible = false;
			itb_SelectedService.Visible = false;
			ok.Visible = false;
			discardAllButton.Visible = false;

			//clear the Service buttons 
			ClearServiceChoiceControls();
			//clear the Service buttons 
			ClearVMRacksChoiceControls();

			selectedVmSpecNode = null;
			selectedRackNode = null;
			selectedDCNode = null;
			HandleSelection();
		}

		protected void itb_SelectedExchange_DoubleClick(object sender, EventArgs e)
		{
			Handle_SelectedExchange_Action();
		}

		protected void itb_SelectedExchange_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			Handle_SelectedExchange_Action();
		}

		protected void Handle_SelectedService_Action()
		{
			//hide the selected Service
			lblSelectionServiceTitle.Visible = false;
			itb_SelectedService.Visible = false;

			Selected_ServiceName = "";
			selectedServiceNode = null;

			ClearVMRacksChoiceControls();

			ClearServiceChoiceControls();
			FillServiceChoiceControls();

			//Handle the various panels 
			pnl_ServiceChoice.Visible = true;
			pnl_VMRackChoice.Visible = false;
			pnl_ExchangeChoice.Visible = false;
			ok.Visible = false;
			discardAllButton.Visible = false;

			selectedVmSpecNode = null;
			selectedRackNode = null;
			selectedDCNode = null;
			HandleSelection();
		}
	
		protected void itb_SelectedService_DoubleClick(object sender, EventArgs e)
		{
			Handle_SelectedService_Action();
		}

		protected void itb_SelectedService_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			Handle_SelectedService_Action();
		}

		protected void BuildExchangeChoiceControls()
		{
			pnl_ExchangeChoice = new Panel();
			pnl_ExchangeChoice.Location = new Point(WIDTH_Selected, 30);
			pnl_ExchangeChoice.Size = new Size(WIDTH_Central, 235);
			pnl_ExchangeChoice.BackColor = Color.Transparent;
			//pnl_ExchangeChoice.BackColor = Color.Fuchsia;
			pnl_ExchangeChoice.Visible = true;
			Controls.Add(pnl_ExchangeChoice);
		}

		private void ClearExchangeControls()
		{
			foreach (Label lbl in ExchangeOtherCtrls.Values)
			{
				pnl_ExchangeChoice.Controls.Remove(lbl);
				lbl.Dispose();
			}
			ExchangeOtherCtrls.Clear();

			foreach (ImageTextButton itb in ExchangeButtons.Values)
			{
				if (itb != null)
				{
					pnl_ExchangeChoice.Controls.Remove(itb);
					itb.ButtonPressed -= new ImageButton.ImageButtonEventArgsHandler(Exchange_ButtonPressed);
					itb.DoubleClick -= new EventHandler(Exchange_DoubleClick);
					itb.Dispose();
				}
			}
			ExchangeButtons.Clear();
		}

		protected void FillExchangeControls()
		{
			int startx = 10;
			int starty = 25;
			int posx = startx;
			int posy = starty;
			int gapx = 10;
			int gapy = 10;
			int rowlimit = 2;
			int count = 0;
			bool show_Vertical_Stack = true;

			//Clear the Old Buttons 
			ClearExchangeControls();

			lblSelectExchangeTitle = new Label();
			lblSelectExchangeTitle.ForeColor = TitleForeColour;
			lblSelectExchangeTitle.Location = new Point(10, 0);
			lblSelectExchangeTitle.Size = new Size(220, 25);
			lblSelectExchangeTitle.Text = "Select Exchange";
			lblSelectExchangeTitle.Font = Font_SubTitle;
			lblSelectExchangeTitle.Visible = true;
			pnl_ExchangeChoice.Controls.Add(lblSelectExchangeTitle);
			ExchangeOtherCtrls.Add("label1", lblSelectExchangeTitle);

			foreach (string ex in ExchangeNodeNameList)
			{
				Node tmpBusinessNode = (Node)ExchangeNodes[ex];

				ImageTextButton itb_temp = new ImageTextButton(@"images\buttons\button_85x25.png");
				itb_temp.SetAutoSize();
				itb_temp.Location = new Point(posx, posy);
				itb_temp.SetButtonText(ex, Color.White, Color.White, Color.White, Color.DimGray);
				itb_temp.ButtonFont = Font_SubTitle;
				itb_temp.SetFocusColours(Color.White);
				itb_temp.ForeColor = Color.Black;				
				itb_temp.Tag = tmpBusinessNode;
				itb_temp.ButtonPressed += new ImageButton.ImageButtonEventArgsHandler(Exchange_ButtonPressed);
				itb_temp.DoubleClick += new EventHandler(Exchange_DoubleClick);
				pnl_ExchangeChoice.Controls.Add(itb_temp);

				if (show_Vertical_Stack)
				{
					posy = posy + itb_temp.Height + gapy;
				}
				else
				{
					if (count >= rowlimit)
					{
						posx = startx;
						posy = posy + itb_temp.Height + gapy;
						count = 0;
					}
					else
					{
						posx = posx + itb_temp.Width + gapx;
						count++;
					}
				}
				//Add button to Lookup
				ExchangeButtons.Add(ex, itb_temp);
			}
		}

		protected void Exchange_Btn_Action(object sender)
		{
			//Extract out the selected Item 
			Selected_ExchangeName = ((ImageTextButton)sender).GetButtonText();
			selectedBusinessNode = (Node)((ImageTextButton)sender).Tag;

			//show in the selected displays 
			lblSelectionExchangeTitle.Text = Selected_ExchangeName;
			lblSelectionExchangeTitle.Visible = true;
			itb_SelectedExchange.SetButtonText("Exchanges", Color.White, Color.White, Color.White, Color.DimGray);
			itb_SelectedExchange.Visible = true;

			//fill the service Choice Panel
			FillServiceChoiceControls();

			//hide the exchange panel 
			pnl_ExchangeChoice.Visible = false;
			//hide the service choice 
			pnl_ServiceChoice.Visible = true;
		}
		protected void Exchange_DoubleClick(object sender, EventArgs e)
		{
			Exchange_Btn_Action(sender);
		}
		protected void Exchange_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			Exchange_Btn_Action(sender);
		}

		protected	void BuildServiceChoiceControls()
		{
			pnl_ServiceChoice = new Panel();
			pnl_ServiceChoice.Location = new Point(WIDTH_Selected, 30);
			pnl_ServiceChoice.Size = new Size(WIDTH_Central, HEIGHT_Panel);
			pnl_ServiceChoice.BackColor = Color.Transparent;
			//pnl_ServiceChoice.BackColor = Color.Plum;
			pnl_ServiceChoice.Visible = true;
			Controls.Add(pnl_ServiceChoice);
		}

		private void ClearServiceChoiceControls()
		{
			foreach (Label lbl in ServiceOtherCtrls.Values)
			{
				pnl_ServiceChoice.Controls.Remove(lbl);
				lbl.Dispose();
			}
			ServiceOtherCtrls.Clear();

			foreach (ImageTextButton itb in ServiceButtons.Values)
			{
				if (itb != null)
				{
					pnl_ServiceChoice.Controls.Remove(itb);
					itb.ButtonPressed -= new ImageButton.ImageButtonEventArgsHandler(Service_ButtonPressed);
					itb.DoubleClick -= new EventHandler(Service_DoubleClick);
					itb.Dispose();
				}
			}
			ServiceButtons.Clear();
		}

		protected bool isDemandNode_OpenForOrders(Node DemandNode, out string errmsg)
		{
			bool isOpen = false;
			errmsg = string.Empty;

			string status = DemandNode.GetAttribute("status");

			switch (status.ToLower())
			{
				case "":
				case "waiting":
					errmsg = "Demand not ready yet";
					break;
				case "running":
					errmsg = "Demand already running";
					break;
				case "announcing":
					isOpen = true;
					errmsg = string.Empty;
					break;
				case "lingering":
				case "completed":
					errmsg = "Demand has completed";
					break;
			}
			return isOpen;
		}

		private void CheckShowButton(int CurrentRound, Node sipNode, out bool availableInThisRound, 
			out string sip_type, out bool is_demand, out bool is_demand_button_permitted,
			out bool is_button_enabled, out bool is_button_highlighted)
		{
			availableInThisRound = sipNode.GetBooleanAttribute(CONVERT.Format("available_in_round_{0}", CurrentRound), false);
			sip_type = "DMD";
			is_demand = false;
			is_demand_button_permitted = false;
			is_button_enabled = false;

			sip_type = "DMD";
			if (sipNode.GetBooleanAttribute("is_new_service", false))
			{
				Node devService = model.GetNamedNode(sipNode.GetAttribute("dev_service_name"));
				Node productionService = model.GetNamedNode(sipNode.GetAttribute("service_name"));

				sip_type = "NS";
				is_demand = false;
				is_demand_button_permitted = false;
				is_button_enabled = true;
				is_button_highlighted = ((devService != null)
				                        || (productionService != null));

				if (productionService != null)
				{
					Node location = orderPlanner.GetCurrentCloudDeploymentLocationIfAny(productionService);
					if ((location != null) && location.GetBooleanAttribute("saas", false))
					{
						is_button_enabled = false;
					}
				}
			}
			else
			{
				is_demand = true;
				is_demand_button_permitted = false;
				//Extract the demand node
				Node demand_node = model.GetNamedNode(sipNode.GetAttribute("demand_name"));

				availableInThisRound = availableInThisRound && (demand_node.GetBooleanAttribute("active", false));

				Debug.Assert(demand_node != null);

				//we need to determine which state the demand node in
				string errmsg = string.Empty;

				is_demand_button_permitted = true;

				is_button_enabled = isDemandNode_OpenForOrders(demand_node, out errmsg);
				is_button_highlighted = (model.GetNamedNode(sipNode.GetAttribute("service_name")) != null);										
			}
		}

		protected void FillServiceChoiceControls()
		{
			int startx = 10;
			int starty = 25;
			int startx_secondBox  = 260;

			int posx = startx;
			int posy = starty;
			int gapx = 5;
			int gapy = 5;
			int rowlimit = 1;
			int count = 0;

			ClearServiceChoiceControls();

			lblSelectServiceTitle1 = new Label();
			lblSelectServiceTitle1.ForeColor = TitleForeColour;
			lblSelectServiceTitle1.Location = new Point(10, 0);
			lblSelectServiceTitle1.Size = new Size(220, 25);
			lblSelectServiceTitle1.Text = "Select Service";
			lblSelectServiceTitle1.Font = Font_SubTitle;
			lblSelectServiceTitle1.Visible = true;
			pnl_ServiceChoice.Controls.Add(lblSelectServiceTitle1);
			ServiceOtherCtrls.Add("label1a", lblSelectServiceTitle1);

			lblSelectServiceTitle2 = new Label();
			lblSelectServiceTitle2.ForeColor = TitleForeColour;
			lblSelectServiceTitle2.Location = new Point(250, 0);
			lblSelectServiceTitle2.Size = new Size(220, 25);
			lblSelectServiceTitle2.Text = "Select Demand";
			lblSelectServiceTitle2.Font = Font_SubTitle;
			lblSelectServiceTitle2.Visible = true;
			pnl_ServiceChoice.Controls.Add(lblSelectServiceTitle2);
			ServiceOtherCtrls.Add("label1b", lblSelectServiceTitle2);

			ArrayList al = model.GetNamedNode("NewServiceDefinitions").GetChildrenOfType("new_service");

			//foreach (Node sip in model.GetNamedNode("NewServiceDefinitions").GetChildrenOfType("new_service"))
			foreach (Node sip in al)
			{
				bool availableInThisRound;
				bool is_demand;
				bool is_demand_button_permitted;
				bool is_button_enabled;
				bool is_button_highlighted;
				string sip_type;

				CheckShowButton(currentRound, sip, out availableInThisRound, out sip_type, out is_demand, 
					out is_demand_button_permitted, out is_button_enabled, out is_button_highlighted);

				bool NameCheck = (sip.GetAttribute("business") == selectedBusinessNode.GetAttribute("name"));
				bool RndAvailableCheck = availableInThisRound;
				bool is_SIP_Available = (sip.GetBooleanAttribute("available_in_request_panel", false));
				bool is_DMD_Available = ((is_demand == true)&&(is_demand_button_permitted));

				if (is_demand == false)
				{
					if ((NameCheck) && (RndAvailableCheck) && (is_SIP_Available || is_DMD_Available))
					{
						//businessServicePanel.AddItem(sip, CONVERT.Format("({0}) {1}", type, GetSipNameAndState(sip)));
						string display_code = CONVERT.Format("({0}) {1}", sip_type, GetSipNameAndState(sip));
						display_code = ""+sip.GetAttribute("sip_code") + " " + sip.GetAttribute("service_code");

						ImageTextButton itb_temp = new ImageTextButton(@"images\buttons\button_65x25.png");
						//itb_temp.Size = new System.Drawing.Size(ItemButton_width, 32);
						itb_temp.SetAutoSize();
						itb_temp.Location = new Point(posx, posy);
						itb_temp.SetButtonText(display_code, Color.White, Color.White, Color.White, Color.DimGray);
						itb_temp.Tag = sip;
						itb_temp.ButtonFont = Font_SubTitle;
						itb_temp.SetFocusColours(Color.White);
						itb_temp.ForeColor = Color.White;				
						itb_temp.ButtonPressed += new ImageButton.ImageButtonEventArgsHandler(Service_ButtonPressed);
						itb_temp.DoubleClick += new EventHandler(Service_DoubleClick);

						itb_temp.Enabled = is_button_enabled;
						itb_temp.Active = is_button_highlighted;

						pnl_ServiceChoice.Controls.Add(itb_temp);

						if (count > rowlimit)
						{
							posx = startx;
							posy = posy + itb_temp.Height + gapy;
							count = 0;
						}
						else
						{
							posx = posx + itb_temp.Width + gapx;
							count++;
						}
						//Add button to Lookup
						ServiceButtons.Add(display_code, itb_temp);
					}
				}
			}
			//====================================================================
			//==Now show the demands  (is not allowed then show but be disabled)
			//====================================================================
			posx = startx_secondBox;
			posy = starty;

			count = 0;
			rowlimit = 1;

			foreach (Node sip in al)
			{
				bool availableInThisRound;
				bool is_demand;
				bool is_demand_button_permitted;
				bool is_button_enabled;
				bool is_button_highlighted;
				string sip_type;
				
				CheckShowButton(currentRound, sip, out availableInThisRound, out sip_type, out is_demand,
					out is_demand_button_permitted, out is_button_enabled, out is_button_highlighted);

				bool NameCheck = (sip.GetAttribute("business") == selectedBusinessNode.GetAttribute("name"));
				bool RndAvailableCheck = availableInThisRound;
				bool is_SIP_Available = (sip.GetBooleanAttribute("available_in_request_panel", false));
				bool is_DMD_Available = ((is_demand == true) && (is_demand_button_permitted));

				if (is_demand == true)
				{
					if ((NameCheck) && (RndAvailableCheck) && (is_SIP_Available || is_DMD_Available))
					{
						Node demand = model.GetNamedNode(sip.GetAttribute("demand_name"));

						string display_code = CONVERT.Format("{0} {1}",
															 demand.GetAttribute(CONVERT.Format("short_desc_round_{0}", currentRound)).Replace("DMD ", ""),
															 sip.GetAttribute("service_code"));

						ImageTextButton itb_temp = new ImageTextButton(@"images\buttons\button_65x25.png");
						//itb_temp.Size = new System.Drawing.Size(ItemButton_width, 32);
						itb_temp.SetAutoSize();
						itb_temp.Location = new Point(posx, posy);
						itb_temp.SetButtonText(display_code, Color.White, Color.White, Color.White, Color.DimGray);
						itb_temp.Tag = sip;
						itb_temp.ButtonFont = Font_SubTitle;
						itb_temp.ButtonPressed += new ImageButton.ImageButtonEventArgsHandler(Service_ButtonPressed);
						itb_temp.DoubleClick += new EventHandler(Service_DoubleClick);

						itb_temp.Enabled = is_button_enabled;
						itb_temp.Active = is_button_highlighted;

						if (itb_temp.Enabled)
						{
							itb_temp.SetFocusColours(Color.White);
							itb_temp.ForeColor = Color.White;
							itb_temp.SetButtonText(display_code, Color.White, Color.White, Color.White, Color.DimGray);
						}

						pnl_ServiceChoice.Controls.Add(itb_temp);

						if (count >= rowlimit)
						{
							posx = startx_secondBox;
							posy = posy + itb_temp.Height + gapy;
							count = 0;
						}
						else
						{
							posx = posx + itb_temp.Width + gapx;
							count++;
						}
						//Add button to Lookup
						ServiceButtons.Add(display_code, itb_temp);
					}
				}
			}
		}

		protected void Handle_Service_Action(object sender)
		{
			//Extract out the selected Item 
			Selected_ServiceName = ((ImageTextButton)sender).GetButtonText();
			selectedServiceNode = (Node)((ImageTextButton)sender).Tag;

			//show in the selected displays 
			lblSelectionServiceTitle.Text = Selected_ServiceName;
			lblSelectionServiceTitle.Visible = true;
			itb_SelectedService.SetButtonText("Services", Color.White, Color.White, Color.White, Color.DimGray);
			itb_SelectedService.Visible = true;

			ok.Visible = true;

			//fill the service Choice Panel
			FillVMRackChoiceControls();
			HandleSelection();
			UpdateDiscardStatus();

			//hide the exchange panel 
			pnl_ExchangeChoice.Visible = false;
			pnl_ServiceChoice.Visible = false;
			pnl_VMRackChoice.Visible = true;
		}

		void UpdateDiscardStatus ()
		{
			StringBuilder builder = new StringBuilder ();

			bool developed = false;
			bool developmentUsedByOthers = false;
			bool inProduction = false;

			Node developmentService = model.GetNamedNode(selectedServiceNode.GetAttribute("dev_service_name"));

			// If the development service belongs to someone else, we can't delete it!
			if (developmentService != null)
			{
				if (model.GetNamedNode(developmentService.GetAttribute("business")) != selectedBusinessNode)
				{
					developmentService = null;
				}
			}

			Node productionService = model.GetNamedNode(selectedServiceNode.GetAttribute("service_name"));

			// Is our development service relied on by someone else?
			if (developmentService != null)
			{
				AddFeedbackForCurrentStatus(builder, developmentService);

				developed = true;

				foreach (Node service in model.GetNodesWithAttributeValue("type", "business_service"))
				{
					if (service.GetAttribute("requires_dev") == developmentService.GetAttribute("name"))
					{
						if (service != productionService)
						{
							Node business = model.GetNamedNode(service.GetAttribute("business"));
							string line = CONVERT.Format("Development in use by {0}", business.GetAttribute("desc"));
							builder.AppendLine(line);
							feedbackPanel.AddFeedbackItem("dev", line);
							developmentUsedByOthers = true;
						}
					}
				}
			}

			if (productionService != null)
			{
				AddFeedbackForCurrentStatus(builder, productionService);

				inProduction = true;
			}

			discardAllButton.Visible = (developed || inProduction) && ! developmentUsedByOthers;

			if (inProduction)
			{
				foreach (Control button in VMSpecButtons.Values)
				{
					button.Enabled = false;
				}

				foreach (Control button in RackButtons.Values)
				{
					button.Enabled = false;
				}

				foreach (Control button in DCButtons.Values)
				{
					button.Enabled = false;
				}

				foreach (Control button in PVButtons.Values)
				{
					button.Enabled = false;
				}
			}
		}

		void AddFeedbackForCurrentStatus (StringBuilder builder, Node devOrProductionService)
		{
			if (builder.Length > 0)
			{
				builder.AppendLine();
			}

			if (devOrProductionService.GetBooleanAttribute("is_dev", false))
			{
				builder.AppendLine("Development currently using:");
			}
			else
			{
				builder.AppendLine("Production currently using:");
			}

			Node vmInstance = model.GetNamedNode(devOrProductionService.GetAttribute("vm_instance"));
			Dictionary<Node, int> serverToCpus = new Dictionary<Node, int> ();
			if (vmInstance != null)
			{
				foreach (Node vmInstanceLinkToServer in vmInstance.GetChildrenOfType("vm_instance_link_to_server"))
				{
					Node server = model.GetNamedNode(vmInstanceLinkToServer.GetAttribute("server"));
					if (! serverToCpus.ContainsKey(server))
					{
						serverToCpus.Add(server, 0);
					}

					serverToCpus[server] += vmInstanceLinkToServer.GetIntAttribute("cpus", 0);
				}
			}

			List<Node> sortedServers = new List<Node> (serverToCpus.Keys);
			sortedServers.Sort(delegate (Node a, Node b)
			{
				return a.GetAttribute("name").CompareTo(b.GetAttribute("name"));
			});

			foreach (Node server in sortedServers)
			{
				builder.AppendLine(CONVERT.Format("{0} CPUs from {1}", serverToCpus[server], server.GetAttribute("name")));
			}
		}

		protected void Service_DoubleClick(object sender, EventArgs e)
		{
			Handle_Service_Action(sender);
		}

		protected void Service_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			Handle_Service_Action(sender);
		}

		protected	void BuildVMRackChoiceControls()
		{
			pnl_VMRackChoice = new Panel();
			pnl_VMRackChoice.Location = new Point(WIDTH_Selected, 30);
			pnl_VMRackChoice.Size = new Size(WIDTH_Central, HEIGHT_Panel);
			pnl_VMRackChoice.BackColor = Color.Transparent;
			//pnl_VMRackChoice.BackColor = Color.LightBlue;
			pnl_VMRackChoice.Visible = true;
			Controls.Add(pnl_VMRackChoice);
		}

		protected void ClearVMRacksChoiceControls()
		{
			foreach (Object obj in VMRackOtherCtrls.Values)
			{
				if (obj is Label)
				{
					Label lbl = (Label)obj;
					pnl_VMRackChoice.Controls.Remove(lbl);
					lbl.Dispose();
				}
				if (obj is ImageTextButton)
				{
					ImageTextButton itb = (ImageTextButton)obj;
					pnl_VMRackChoice.Controls.Remove(itb);
					itb.Dispose();
				}
			}
			VMRackOtherCtrls.Clear();

			foreach (ImageToggleButton itb in VMSpecButtons.Values)
			{
				if (itb != null)
				{
					pnl_VMRackChoice.Controls.Remove(itb);
					itb.ButtonPressed -= new ImageButton.ImageButtonEventArgsHandler(VM_Spec_ButtonPressed);
					itb.DoubleClick -= new EventHandler(VM_Spec_DoubleClick);
					itb.Dispose();
				}
			}
			VMSpecButtons.Clear();

			foreach (ImageToggleButton itb in RackButtons.Values)
			{
				if (itb != null)
				{
					pnl_VMRackChoice.Controls.Remove(itb);
					itb.ButtonPressed -= new ImageButton.ImageButtonEventArgsHandler(Rack_ButtonPressed);
					itb.DoubleClick -= new EventHandler(Rack_DoubleClick);
					itb.Dispose();
				}
			}
			RackButtons.Clear();

			foreach (ImageToggleButton itb in DCButtons.Values)
			{
				if (itb != null)
				{
					pnl_VMRackChoice.Controls.Remove(itb);
					itb.ButtonPressed -= new ImageButton.ImageButtonEventArgsHandler(DCNode_ButtonPressed);
					itb.DoubleClick -= new EventHandler(DCNode_DoubleClick);
					itb.Dispose();
				}
			}
			DCButtons.Clear();
		}

		protected void FillVMRackChoiceControls()
		{
			int startx = 10;
			int starty = 35;
			int posx = startx;
			int posy = starty;
			int gapx = 5;
			int gapy = 5;
			int rowlimit = 2;
			int count = 0;
			int startx2 = 360;

			string section_title1 = "VM";
			string section_title2 = "Rack";
			string section_title3 = "";

			bool vm_auto_selected = false;
			bool dc_auto_selected = false;

			selectedVmSpecNode = null;
			selectedRackNode = null;
			selectedDCNode = null;
			bool showPublicVendor = false;

			ClearVMRacksChoiceControls();
			PVButtons.Clear();

			if (roundVariables.GetAttribute("production_deploy_type") == "local")
			{
				section_title1 = "VM";
				section_title2 = "Exchange";
				section_title3 = "Vendor";
			}

			// If we've already deployed, pick the right datacenter.
			Node service = model.GetNamedNode(selectedServiceNode.GetAttribute("service_name"));
			Node deployedDC = null;
			if (service != null)
			{
				Node cloud = model.GetNamedNode(service.GetAttribute("cloud"));
				if (cloud != null)
				{
					foreach (Node locationReference in cloud.GetChildrenOfType("cloud_location"))
					{
						Node location = model.GetNamedNode(locationReference.GetAttribute("location"));
						if (location.GetAttribute("type") == "rack")
						{
							deployedDC = location.Parent;
							break;
						}
					}
				}
			}

			lblSelectItemTitle1 = new Label();
			lblSelectItemTitle1.ForeColor = TitleForeColour;
			//lblSelectItemTitle1.BackColor = Color.PaleGreen;
			lblSelectItemTitle1.Location = new Point(10, 5);
			lblSelectItemTitle1.Size = new Size(240, 30);
			lblSelectItemTitle1.Text = section_title1;
			lblSelectItemTitle1.Font = Font_SubTitle;
			lblSelectItemTitle1.Visible = true;
			pnl_VMRackChoice.Controls.Add(lblSelectItemTitle1);
			VMRackOtherCtrls.Add("label1a", lblSelectItemTitle1);

			lblSelectItemTitle2 = new Label();
			lblSelectItemTitle2.ForeColor = TitleForeColour;
			//lblSelectItemTitle2.BackColor = Color.LightSalmon;
			lblSelectItemTitle2.Location = new Point(250, 5);
			lblSelectItemTitle2.Size = new Size(100, 30);
			lblSelectItemTitle2.Text = section_title2;
			lblSelectItemTitle2.Font = Font_SubTitle;
			lblSelectItemTitle2.Visible = true;
			pnl_VMRackChoice.Controls.Add(lblSelectItemTitle2);
			VMRackOtherCtrls.Add("label1b", lblSelectItemTitle2);

			lblSelectItemTitle3 = new Label();
			lblSelectItemTitle3.ForeColor = TitleForeColour;
			//lblSelectItemTitle3.BackColor = Color.Maroon;
			lblSelectItemTitle3.Location = new Point(360, 5);
			lblSelectItemTitle3.Size = new Size(100, 30);
			lblSelectItemTitle3.Text = section_title3;
			lblSelectItemTitle3.Font = Font_SubTitle;
			lblSelectItemTitle3.Visible = true;
			pnl_VMRackChoice.Controls.Add(lblSelectItemTitle3);
			VMRackOtherCtrls.Add("label1c", lblSelectItemTitle3);

			Node Pref_VM_Node = null;
			if (currentRound>1)
			{
				Pref_VM_Node = orderPlanner.GetAVmSuitableForBusinessServiceOrDefinition(selectedServiceNode);
			}

			//Build the VM Spec Buttons 
			bool showVMs = true;
			if (showVMs)
			{ 
				Node data_center_node = model.GetNamedNode("VmSpecs");
				if (data_center_node != null)
				{
					foreach (Node vm_spec_node in data_center_node.getChildren())
					{
						string name = vm_spec_node.GetAttribute("name");
						string type = vm_spec_node.GetAttribute("type");
						bool hidden = vm_spec_node.GetBooleanAttribute("hidden", false);

						if (hidden == false)
						{
							ImageToggleButton itb_temp = new ImageToggleButton(0, "images/buttons/button_50x20_on.png", "images/buttons/button_50x20_active.png", name, name);
							itb_temp.Name = "ITB VM " + name;

							if ((Pref_VM_Node == vm_spec_node)&&(currentRound>1))
							{
								itb_temp.State = 1;
								vm_auto_selected = true;
								selectedVmSpecNode = vm_spec_node;
							}
							else
							{
								itb_temp.State = 0;
							}
							itb_temp.Size = new Size(50, 20);
							itb_temp.Location = new Point(posx, posy);
							itb_temp.Font = Font_Body;
							itb_temp.ForeColor = Color.White;
							itb_temp.setTextForeColor(Color.White);
							//itb_temp.ButtonFont = Font_Title;
							itb_temp.Tag = vm_spec_node;
							itb_temp.ButtonPressed += new ImageButton.ImageButtonEventArgsHandler(VM_Spec_ButtonPressed);
							itb_temp.DoubleClick += new EventHandler(VM_Spec_DoubleClick);
							pnl_VMRackChoice.Controls.Add(itb_temp);

							if (count > rowlimit)
							{
								posx = startx;
								posy = posy + itb_temp.Height + gapy;
								count = 0;
							}
							else
							{
								posx = posx + itb_temp.Width + gapx;
								count++;
							}
							//Add button to Lookup
							VMSpecButtons.Add(name, itb_temp);
						}
					}
				}
			}

			startx = 250;
			posx = startx;
			posy = starty;
			gapx = 5;
			gapy = 5;
			rowlimit = 1;
			count = 0;

			if (roundVariables.GetAttribute("production_deploy_type") == "local")
			{
				if (selectedBusinessNode != null)
				{
					foreach (Node dc_Node in orderPlanner.GetDeploymentTargets(selectedServiceNode))
					{
						string name = dc_Node.GetAttribute("desc");

						ImageToggleButton itb_temp = new ImageToggleButton(0, "images/buttons/button_50x20_on.png", "images/buttons/button_50x20_active.png", name, name);

						if (dc_Node == deployedDC)
						{
							itb_temp.State = 1;
							dc_auto_selected = true;
							selectedDCNode = dc_Node;
						}
						else if ((deployedDC == null)
						        && (name.Equals(Selected_ExchangeName,StringComparison.InvariantCultureIgnoreCase))
							    && (currentRound > 1))
						{
							itb_temp.State = 1;
							dc_auto_selected = true;
							selectedDCNode = dc_Node;
						}
						else if ((dc_Node.GetAttribute("type") == "server")
								 && (dc_Node.Parent != null)
								 && (dc_Node.Parent.Parent == deployedDC))
						{
							itb_temp.State = 1;
							dc_auto_selected = true;
						}
						else
						{
							itb_temp.State = 0;
						}						
						itb_temp.Name = "DC " + name;
						itb_temp.Size = new Size(65, 20);
						itb_temp.Location = new Point(posx, posy);
						itb_temp.Font = Font_Body;
						itb_temp.Tag = dc_Node;
						itb_temp.ForeColor = Color.White;
						itb_temp.setTextForeColor(Color.White);
						itb_temp.ButtonPressed += new ImageButton.ImageButtonEventArgsHandler(DCNode_ButtonPressed);
						itb_temp.DoubleClick += new EventHandler(DCNode_DoubleClick);
						pnl_VMRackChoice.Controls.Add(itb_temp);

						if (name.Equals("IAAS",StringComparison.InvariantCultureIgnoreCase))
						{
							showPublicVendor= true;
						}

						bool UseVert = true;
						if (UseVert)
						{
							posx = startx;
							posy += itb_temp.Height + gapy;
						}
						else
						{
							if (count > rowlimit)
							{
								posx = startx;
								posy = posy + itb_temp.Height + gapy;
								count = 0;
							}
							else
							{
								posx = posx + itb_temp.Width + gapx;
								count++;
							}
						}
						//Add button to Lookup
						DCButtons.Add(name, itb_temp);
					}
					//if Showing IAAS then we should show the Public Vendor)
					if (showPublicVendor)
					{
						int posx2 = startx2;
						int posy2 = starty;
						int count2 = 0;

						ImageToggleButton itb_last_control = null;
						ImageToggleButton itb_first_control = null;
						ImageToggleButton itb_first_control_enabled = null;
						bool auto_selected_PV = false;

						foreach (Node cloudProvider in orderPlanner.GetSuitableCloudProviders(selectedServiceNode, false))
						{
							string cp_nodename = cloudProvider.GetAttribute("name");
							string cp_desc = cloudProvider.GetAttribute("desc");
							bool cp_active = cloudProvider.GetBooleanAttribute("available",false)
							                 && (cloudProvider.GetAttribute("status") != "closing");

							ImageToggleButton itb_temp = new ImageToggleButton(0, "images/buttons/button_100x20_on.png", "images/buttons/button_100x20_active.png", cp_desc, cp_desc);
							itb_last_control = itb_temp;
							if (itb_first_control==null)
							{
								itb_first_control = itb_temp;
							}
							if ((itb_first_control_enabled==null)&&(cp_active))
							{
								itb_first_control_enabled = itb_temp;
							}
							itb_temp.Name = "PV " + cp_desc;
							itb_temp.Size = new Size(100, 20);
							itb_temp.Location = new Point(posx2, posy2);
							itb_temp.Font = Font_Body;
							itb_temp.Tag = cloudProvider;
							itb_temp.ForeColor = Color.White;
							itb_temp.Visible = false;
							itb_temp.setTextForeColor(Color.White);
							itb_temp.Enabled = cp_active;
							itb_temp.ButtonPressed += new ImageButton.ImageButtonEventArgsHandler(PVNode_ButtonPressed);
							itb_temp.DoubleClick += new EventHandler(PVNode_DoubleClick);
							pnl_VMRackChoice.Controls.Add(itb_temp);

							PVButtons.Add(cp_desc, itb_temp);

							bool UseVert = true;
							if (UseVert)
							{
								posx2 = startx2;
								posy2 += itb_temp.Height + gapy;
							}
							else
							{
								if (count2 > rowlimit)
								{
									posx2 = startx2;
									posy2 = posy2 + itb_temp.Height + gapy;
									count2 = 0;
								}
								else
								{
									posx2 = posx2 + itb_temp.Width + gapx;
									count2++;
								}
							}
						}
						//If we only have one Possible PV then select it 
						if (auto_selected_PV==false)
						{
							itb_first_control_enabled.Active = true;
							itb_first_control_enabled.State = 1;
							selectedCloudProvider = (Node)itb_first_control_enabled.Tag;
						}
					}
				}
			}
			else
			{
				//Build the Rack Buttons 
				if (selectedBusinessNode != null)
				{
					string datacenter_name = selectedBusinessNode.GetAttribute("datacenter");
					Node data_center_node = model.GetNamedNode(datacenter_name);
					if (data_center_node != null)
					{
						foreach (Node rackNode in orderPlanner.GetDeploymentTargets(selectedServiceNode))
						{
							string name = rackNode.GetAttribute("name");

							ImageToggleButton itb_temp = new ImageToggleButton(0, "images/buttons/button_50x20_on.png", "images/buttons/button_50x20_active.png", name, name);
							itb_temp.State = 0;
							itb_temp.Name = "ITB RK " + name;
							itb_temp.Size = new Size(65, 20);
							itb_temp.Location = new Point(posx, posy);
							itb_temp.Font = Font_Body;
							itb_temp.ForeColor = Color.White;
							itb_temp.setTextForeColor(Color.White);
							itb_temp.Tag = rackNode;
							itb_temp.ButtonPressed += new ImageButton.ImageButtonEventArgsHandler(Rack_ButtonPressed);
							itb_temp.DoubleClick += new EventHandler(Rack_DoubleClick);

							pnl_VMRackChoice.Controls.Add(itb_temp);

							if (count > rowlimit)
							{
								posx = startx;
								posy = posy + itb_temp.Height + gapy;
								count = 0;
							}
							else
							{
								posx = posx + itb_temp.Width + gapx;
								count++;
							}

							//Add button to Lookup
							RackButtons.Add(name, itb_temp);
						}
					}
				}
			}

			if (currentRound > 1)
			{ 
				//Have we auto selected VM and DC?
				if (vm_auto_selected && dc_auto_selected)
				{
					HandleSelection();
				}
			}
		}

		protected void itb_ClearVMRack_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			foreach (ImageToggleButton itb in VMSpecButtons.Values)
			{
				itb.State = 0;
			}
			foreach (ImageToggleButton itb in RackButtons.Values)
			{
				itb.State = 0;
			}
			foreach (ImageToggleButton itb in DCButtons.Values)
			{
				itb.State = 0;
			}
			selectedVmSpecNode = null;
			selectedRackNode = null;
			selectedDCNode = null;

			HandleSelection();
		}

		private void handle_VM_SpecButtonAction(object sender)
		{
			ImageToggleButton itb1 = (ImageToggleButton)sender;
			itb1.State = 1 - itb1.State;
			itb1.Refresh();

			if (itb1.State == 1)
			{
				selectedVmSpecNode = (Node)itb1.Tag;
			}
			else
			{
				selectedVmSpecNode = null;
			}

			foreach (ImageToggleButton itb in VMSpecButtons.Values)
			{
				if (itb != sender)
				{
					itb.State = 0;
				}
			}
			HandleSelection();
		}
		protected void VM_Spec_DoubleClick(object sender, EventArgs e)
		{
			handle_VM_SpecButtonAction(sender);
		}
		protected void VM_Spec_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			handle_VM_SpecButtonAction(sender);
		}

		private void handleRackButtonAction(object sender)
		{
			ImageToggleButton itb1 = (ImageToggleButton)sender;
			itb1.State = 1 - itb1.State;
			itb1.Refresh();

			if (itb1.State == 1)
			{
				selectedRackNode = (Node)itb1.Tag;
			}
			else
			{
				selectedRackNode = null;
			}

			foreach (ImageToggleButton itb in RackButtons.Values)
			{
				if (itb != sender)
				{
					itb.State = 0;
				}
			}
			HandleSelection();
		}
		protected void Rack_DoubleClick(object sender, EventArgs e)
		{
			handleRackButtonAction(sender);
		}
		protected void Rack_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			handleRackButtonAction(sender);
		}

		protected void Handle_DCNode_Action(object sender)
		{
			ImageToggleButton itb1 = (ImageToggleButton)sender;
			itb1.State = 1 - itb1.State;
			itb1.Refresh();

			if (itb1.State == 1)
			{
				selectedDCNode = (Node)itb1.Tag;
			}
			else
			{
				selectedDCNode = null;
			}

			if ((selectedDCNode != null)
				&& selectedDCNode.GetBooleanAttribute("iaas", false))
			{
				selectedCloudChargeModel = "on_demand";
			}
			else
			{
				selectedCloudChargeModel = "";
			}

			foreach (ImageToggleButton itb in DCButtons.Values)
			{
				if (itb != sender)
				{
					itb.State = 0;
				}
			}
			HandleSelection();
		}
		protected void DCNode_DoubleClick(object sender, EventArgs e)
		{
			Handle_DCNode_Action(sender);
		}
		protected void DCNode_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			Handle_DCNode_Action(sender);
		}

		protected void Handle_PVNode_Action(object sender)
		{
			ImageToggleButton itb1 = (ImageToggleButton)sender;
			itb1.State = 1 - itb1.State;
			itb1.Refresh();

			if (itb1.State == 1)
			{
				selectedCloudProvider = (Node)itb1.Tag;
			}
			else
			{
				selectedCloudProvider = null;
			}

			RefreshPlannedOrders();

			foreach (ImageToggleButton itb in PVButtons.Values)
			{
				if (itb != sender)
				{
					itb.State = 0;
				}
			}
			HandleSelection();
		}
		protected void PVNode_DoubleClick(object sender, EventArgs e)
		{
			Handle_PVNode_Action(sender);
		}
		protected void PVNode_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			Handle_PVNode_Action(sender);
		}

		protected void HandleSelection()
		{
			bool Selected_VM = (selectedVmSpecNode != null);
			bool Selected_Rack = (selectedRackNode != null);
			bool Selected_DC = (selectedDCNode != null);

			if (roundVariables.GetAttribute("production_deploy_type") == "rack")
			{
				RefreshPlannedOrders();
				//=====================================================================
				//==In Round 1, we are looking at VM and Rack 
				//=====================================================================
				if ((Selected_VM == false) && (Selected_Rack == false))
				{
					//Get the Feedback test for JUST DEV 
					//tbFeedbackPanelValue.Text = "JUST DEV";
				}
				else
				{
					if ((Selected_VM == true) && (Selected_Rack == true))
					{
						//Get the Feedback test for FULL SELECTION
						//tbFeedbackPanelValue.Text = "FULL SELECTION";
					}
					else
					{
						//one or the other selected, Not complete selection 
						//tbFeedbackPanelValue.Text = "";
					}
				}
			}
			else
			{
				if (currentRound == 2)
				{
					RefreshPlannedOrders();
					//=====================================================================
					//==In Round 2, we are looking at VM and Data Centre with No IAAS
					//=====================================================================
					if ((Selected_VM == false) && (Selected_DC == false))
					{
						//Get the Feedback test for JUST DEV 
						//tbFeedbackPanelValue.Text = "JUST DEV";
					}
					else
					{
						if ((Selected_VM == true) && (Selected_DC == true))
						{
							//Get the Feedback test for FULL SELECTION
							//tbFeedbackPanelValue.Text = "FULL SELECTION";
						}
						else
						{
							//one or the other selected, Not complete selection 
							//tbFeedbackPanelValue.Text = "";
						}
					}
				}
				else
				{
					RefreshPlannedOrders();
					//==========================================================================
					//==In Round 3 and 4, we could be looking at selecting a Public Cloud Vendor
					//==========================================================================

					Node selectedBusinessService = null;
					if (selectedServiceNode != null)
					{
						selectedBusinessService = model.GetNamedNode(selectedServiceNode.GetAttribute("service_name"));
					}

					bool PV_visible = (((selectedBusinessService != null)
										&& (orderPlanner.GetCurrentCloudDeploymentLocationIfAny(selectedBusinessService) != null))
									  || ((selectedDCNode != null)
										&& selectedDCNode.GetBooleanAttribute("is_cloud_server", false)));

					if (lblSelectItemTitle3 != null)
					{
						lblSelectItemTitle3.Visible = PV_visible;
					}

					ImageToggleButton tmp_itb = null;
					foreach (ImageToggleButton itb in PVButtons.Values)
					{
						itb.Visible = PV_visible;
						if (itb.Tag == selectedCloudProvider)
						{
							tmp_itb = itb;
						}
					}
					if(tmp_itb != null)
					{
						tmp_itb.State = 1;
					}
				}
			}
		}

		protected void BuildFeedBackControls()
		{
			pnl_FeedBack = new Panel();
			pnl_FeedBack.Location = new Point(WIDTH_Selected + WIDTH_Central, 30);
			pnl_FeedBack.Size = new Size(WIDTH_FeedBack, HEIGHT_Panel);
			pnl_FeedBack.BackColor = Color.Transparent;
			//pnl_FeedBack.BackColor = Color.PeachPuff;
			pnl_FeedBack.Visible = true;
			Controls.Add(pnl_FeedBack);

			lblFeedbackPanelTitle = new Label();
			lblFeedbackPanelTitle.ForeColor = TitleForeColour;
			lblFeedbackPanelTitle.Location = new Point(10, 0);
			lblFeedbackPanelTitle.Size = new Size(220, 25);
			lblFeedbackPanelTitle.Text = "Feedback";
			lblFeedbackPanelTitle.Font = Font_SubTitle;
			lblFeedbackPanelTitle.Visible = true;
			pnl_FeedBack.Controls.Add(lblFeedbackPanelTitle);

			feedbackPanel = new FeedbackPanel ();
			feedbackPanel.Location = new Point(10, 25);
			feedbackPanel.Size = new Size(190, HEIGHT_Panel - 40);
			feedbackPanel.ForeColor = FeedBackForeColour;
			feedbackPanel.BackColor = Color.FromArgb(32, 32, 32);
			feedbackPanel.BorderStyle = BorderStyle.FixedSingle;
			pnl_FeedBack.Controls.Add(feedbackPanel);
		}

		protected void ok_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			ExecuteOrders();	//Perform the defined action
			OnClosed();				//close this popup window
		}

		void ExecuteOrders ()
		{
			bool proceed = false;
			string errmsg = string.Empty;

			//check that if we are playing with a demand, we may have entred a bad time while the panel was open
			if (selectedServiceNode.GetBooleanAttribute("is_new_service", false) == false)
			{
				Node demand_node = model.GetNamedNode(selectedServiceNode.GetAttribute("demand_name"));

				if (isDemandNode_OpenForOrders(demand_node, out errmsg))
				{
					proceed = true;
				}
				else
				{
					MessageBox.Show(errmsg, "Create New Demand Request");
					proceed = false;
				}
			}
			else
			{
				proceed = true;
			}

			if (proceed)
			{
				//Looks weird but we need to copy to different arraylist 
				//so when we move nodes. we dont pollute our own iterator

				ArrayList al = new ArrayList();
				foreach (Node order in plannedOrders.getChildren())
				{
					al.Add(order);
				}
				foreach (Node order in al)
				{
					model.MoveNode(order, orderQueue);
				}
				plannedOrders.DeleteChildren();
			}
		}

		protected void cancel_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			OnClosed();
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);
			DoSize();
		}

		protected void DoSize()
		{
			cancel.Location = new Point (Width - (cancel.Width + 10), Height - (cancel.Height + 2));
			ok.Location = new Point (Width - 2 * (cancel.Width + 10), Height - (ok.Height + 2));
			discardAllButton.Location = new Point (ok.Left - 10 - discardAllButton.Width, Height - 2 - discardAllButton.Height);

			pnl_FeedBack.Size = new Size(Width - 20 - pnl_FeedBack.Left, ok.Top - 20 - pnl_FeedBack.Top);
			feedbackPanel.Size = new Size(pnl_FeedBack.Width - feedbackPanel.Left, pnl_FeedBack.Height - feedbackPanel.Top);
		}

		public override Size getPreferredSize()
		{
			return new Size(800, 255);
		}

		public override bool IsFullScreen => false;
	}
}