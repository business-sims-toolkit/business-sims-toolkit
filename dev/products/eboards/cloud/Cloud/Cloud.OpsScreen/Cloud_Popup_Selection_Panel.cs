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
	public class Cloud_Popup_Selection_Panel : PopupPanel
	{
		protected const string OPTION_PRIVATE = "private";
		protected const string OPTION_IAAS = "iaas";
		protected const string OPTION_SAAS = "saas";

		protected const string OPTION_NONE_SELECT = "----";

		protected Hashtable ChargeModel_Lookup;
		protected Hashtable BuildTypes_Lookup;
		protected ArrayList BuildTypes_DisplayLookup;

		protected int WIDTH_Selected = 120;
		protected int WIDTH_Central = 470 + 230;
		protected int HEIGHT_Title = 25;
		protected int HEIGHT_Panel = 255 - 60;
		protected int HEIGHT_OK = 30;
		protected int FeedBackWidth = 300;

		protected NodeTree model;
		protected Node roundVariables;
		protected Node plannedOrders;
		protected Node orderQueue;
		protected OrderPlanner orderPlanner;

		//Current Selection information 
		protected Node selectedBusinessNode = null;
		protected Node selectedServiceNode = null;

		protected string Selected_ExchangeName = string.Empty;
		protected string Selected_ServiceName = string.Empty;
		protected string selected_DeploymentType = string.Empty;

		protected string SelectedOption_DataCentre = "";
		protected Node SelectedOption_DataCentreNode = null;

		protected string SelectedOption_BuildType = "";
		protected string SelectedOption_ChargeModel = "";

		protected string SelectedOption_Vendor = "";
		protected Node SelectedOption_Vendor_Node = null;

		protected bool isAlreadyInPrivateCloud = false;
		protected Panel pnl_Selection;

		//Selection of Exchange 
		protected Hashtable ExchangeNodes = new Hashtable();
		protected ArrayList ExchangeNodeNameList = new ArrayList();
		protected Hashtable ExchangeButtons = new Hashtable();
		protected Hashtable ExchangeOtherCtrls = new Hashtable();

		protected Panel pnl_ExchangeChoice;
		//protected Label lblSelectExchangeTitle;

		//Selection of Service
		protected Hashtable ServiceNodes = new Hashtable();
		protected ArrayList ServiceNodeNameList = new ArrayList();
		protected Hashtable Service_Private_Buttons = new Hashtable();
		protected Hashtable Service_IAAS_Buttons = new Hashtable();
		protected Hashtable Service_SAAS_Buttons = new Hashtable();
		protected Hashtable ServiceOtherCtrls = new Hashtable();
		protected Hashtable ServiceLabels = new Hashtable();
		protected Hashtable ServiceOtherLabels = new Hashtable();
		protected Hashtable ServiceBackColorLookup = new Hashtable();

		protected Hashtable Service_Title_Buttons = new Hashtable();

		protected Hashtable DeploymentOtherCtrls = new Hashtable();

		protected Hashtable ChoiceCtrls_Labels = new Hashtable();
		protected Hashtable ChoiceCtrls_OtherButtons = new Hashtable();
		protected Hashtable ChoiceCtrls_ItemButtons = new Hashtable();

		protected bool ShowFeedBack = true;
		protected bool ShowClearButtons = false;
		protected bool showSelectedOptionsText = false;

		protected TextBox tb_Selected_Feedback_handle;

		//Selection of DC
		protected Label lblSelectDCTitle_Main;
		protected Label lblSelectDCTitle_Select;
		protected Label lblSelectDC_Aspect_1_Title;
		protected Label lblSelectDC_Aspect_1_Selected;
		protected Label lblSelectDC_FeedbackTitle;
		protected TextBox	tb_SelectDC_FeedbackValue;

		protected Hashtable DCOtherCtrls = new Hashtable();
		protected Hashtable DCButtons = new Hashtable();

		//Selection of IAAS Options
		protected Label lblSelect_IAAS_Title_Main;
		protected Label lblSelect_IAAS_Title_Select;
		protected Label lblSelect_IAAS_Aspect_BuildType_Title;
		protected Label lblSelect_IAAS_Aspect_BuildType_Selected;
		protected Label lblSelect_IAAS_Aspect_ChargeModel_Title;
		protected Label lblSelect_IAAS_Aspect_ChargeModel_Selected;
		protected Label lblSelect_IAAS_Aspect_Vendor_Title;
		protected Label lblSelect_IAAS_Aspect_Vendor_Selected;
		protected Label lblSelect_IAAS_FeedbackTitle;
		protected TextBox tb_Select_IAAS_FeedbackValue;

		protected Hashtable IAAS_OtherCtrls = new Hashtable();
		protected Hashtable IAAS_Aspect_BuildType_Buttons = new Hashtable();
		protected Hashtable IAAS_Aspect_ChargeModel_Buttons = new Hashtable();
		protected Hashtable IAAS_Aspect_Vendor_Buttons = new Hashtable();

		//Selection of SAAS Options
		protected Label lblSelect_SAAS_Title_Main;
		protected Label lblSelect_SAAS_Title_Select;
		protected Label lblSelect_SAAS_Aspect_Vendor_Title;
		protected Label lblSelect_SAAS_Aspect_Vendor_Selected;
		protected Label lblSelect_SAAS_FeedbackTitle;
		protected TextBox tb_Select_SAAS_FeedbackValue;

		protected Hashtable SAAS_OtherCtrls = new Hashtable();
		protected Hashtable SAAS_Aspect_Vendor_Buttons = new Hashtable();

		//General Controls
		protected Label lblPanelTitle;
		protected ImageTextButton ok;
		protected ImageTextButton cancel;
		protected ImageTextButton close;

		protected Font Font_Title;
		protected Font Font_SubTitle;
		protected Font Font_BodyBold;
		protected Font Font_Body;

		protected Font Font_ServiceName_Selected;
		protected Font Font_ServiceName_UnSelected;

		protected Color TitleForeColour = Color.FromArgb(64, 64, 64);
		protected Color SelectedValueForeColour = Color.FromArgb(255, 237, 210);

		protected Color ServiceNameForeColor_Select = Color.Black;
		protected Color ServiceNameForeColor_Unselect = Color.Black;
		protected Color ServiceNameBackColor_Odd = Color.GhostWhite;
		protected Color ServiceNameBackColor_Even = Color.Silver;
		protected Color ServiceNameBackColor_Select = Color.FromArgb(122, 163, 101);

		protected Bitmap ControlBackgroundImage;
		protected Bitmap OptionsBackImage;

		protected bool allowDevelopmentWorkReuse;
		protected bool poolDevelopmentResourcesGlobally;
		protected bool poolProductionResourcesWithinDatacenter;

		protected Panel pnl_Exchanges;
		protected Panel pnl_Services;
		protected Panel pnl_Options;
		protected ImagePanel option_back;

		protected bool showNameBack = true;

		public Cloud_Popup_Selection_Panel(NodeTree model, OrderPlanner orderPlanner)
		{
			this.model = model;
			this.orderPlanner = orderPlanner;

			plannedOrders = model.GetNamedNode("PlannedOrders");
			orderQueue = model.GetNamedNode("IncomingOrders");

			ControlBackgroundImage = (Bitmap)Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images\\panels\\PopupBack_Cloud.png");
			OptionsBackImage = (Bitmap)Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images\\panels\\Popup_Cloud_options.png");

			string font = SkinningDefs.TheInstance.GetData("fontname");
			Font_Title = FontRepository.GetFont(font, 16, FontStyle.Bold);
			Font_SubTitle = FontRepository.GetFont(font, 12, FontStyle.Bold);
			Font_BodyBold = FontRepository.GetFont(font, 10, FontStyle.Bold);
			Font_Body = FontRepository.GetFont(font, 10, FontStyle.Regular);

			BackColor = Color.Transparent;

			Font_ServiceName_Selected = FontRepository.GetFont(font, 12, FontStyle.Bold);
			Font_ServiceName_UnSelected = FontRepository.GetFont(font, 12, FontStyle.Regular);

			roundVariables = model.GetNamedNode("RoundVariables");
			allowDevelopmentWorkReuse = roundVariables.GetBooleanAttribute("shared_development", false);
			poolDevelopmentResourcesGlobally = roundVariables.GetBooleanAttribute("global_private_cloud_deployment_allowed", false);
			poolProductionResourcesWithinDatacenter = roundVariables.GetBooleanAttribute("online_and_floor_can_share_racks", false);

			BuildBaseData();
			BuildMainControls();

			Build_Exchange_Selection_Panel();
			Build_Service_Selection_Panel();
			Build_Options_Selection_Panel();
			Fill2_ExchangeControls();
			Fill_Big_Service_display();
		}

		#region Utility Methods

		protected void RefreshPlannedOrders()
		{
			StringBuilder devTextBuilder = new StringBuilder();
			StringBuilder productionTextBuilder = new StringBuilder();
			plannedOrders.DeleteChildren();

			if (selectedServiceNode != null)
			{
				Node selectedProductionDeploymentLocation = null;
				string selectedVmSpecNodeName = "";

				ok.Enabled = orderPlanner.AddServiceCommissionPlanToQueue(plannedOrders, selectedServiceNode,
					selectedVmSpecNodeName, selectedProductionDeploymentLocation);

				foreach (Node entry in plannedOrders.getChildren())
				{
					if (entry.GetAttribute("type") != "order")
					{
						StringBuilder builder = (entry.GetAttribute("stage") == "dev") ? devTextBuilder : productionTextBuilder;
						builder.AppendLine(entry.GetAttribute("message"));
					}
				}
			}
			else
			{
				ok.Enabled = false;
			}

			string data = string.Empty;

			if ((devTextBuilder.Length > 0) && (productionTextBuilder.Length > 0))
			{
				data += devTextBuilder.ToString();
				data += System.Environment.NewLine;
				data += productionTextBuilder.ToString();
			}
			else
			{
				if ((devTextBuilder.Length > 0))
				{
					data = devTextBuilder.ToString();
				}
				if ((productionTextBuilder.Length > 0))
				{
					data = productionTextBuilder.ToString();
				}
			}
		}

		#endregion  Utility Methods

		#region Data Methods

		protected void BuildBaseData()
		{
			ChargeModel_Lookup = new Hashtable();
			ChargeModel_Lookup.Add("on_demand", "On demand");
			ChargeModel_Lookup.Add("reserved", "Reserved");

			BuildTypes_Lookup = new Hashtable();
			BuildTypes_Lookup.Add(1, "6.4.2");
			BuildTypes_Lookup.Add(2, "6.4.3");
			BuildTypes_Lookup.Add(3, "6.9.10");
			BuildTypes_Lookup.Add(4, "7.2.0");
			BuildTypes_Lookup.Add(5, "8.4.3");
			BuildTypes_Lookup.Add(6, "9.6.2");

			BuildTypes_DisplayLookup = new ArrayList();
			foreach (int step in BuildTypes_Lookup.Keys)
			{
				BuildTypes_DisplayLookup.Add(step);
			}
			BuildTypes_DisplayLookup.Sort();


			//Get the Regions
			foreach (Node business in orderPlanner.GetBusinesses())
			{
				string display_str = business.GetAttribute("desc");
				if (ExchangeNodes.ContainsKey(display_str) == false)
				{
					ExchangeNodes.Add(display_str, business);
					ExchangeNodeNameList.Add(display_str);
				}
			}
		}

		#endregion Data Methods

		#region New Methods

		protected void Build_Exchange_Selection_Panel()
		{
			pnl_Exchanges = new Panel();
			pnl_Exchanges.Location = new Point(10, 35);
			pnl_Exchanges.Size = new Size(1000, 45);
			pnl_Exchanges.BackColor = Color.Transparent;
			Controls.Add(pnl_Exchanges);
		}

		protected void Build_Service_Selection_Panel()
		{
			pnl_Services = new Panel();
			pnl_Services.Location = new Point(10, 80);
			pnl_Services.Size = new Size(1000, 380);
			pnl_Services.BackColor = Color.Transparent;
			Controls.Add(pnl_Services);

			Label lbl_temp = new Label();
			lbl_temp.ForeColor = TitleForeColour;
			lbl_temp.BackColor = Color.Transparent;
			lbl_temp.Font = Font_SubTitle;
			lbl_temp.Size = new Size(150, 20);
			lbl_temp.Location = new Point(5, 0);
			lbl_temp.Text = "Services";
			pnl_Services.Controls.Add(lbl_temp);

			Label lbl_temp2 = new Label();
			lbl_temp2.ForeColor = TitleForeColour;
			lbl_temp2.BackColor = Color.Transparent;
			lbl_temp2.Font = Font_SubTitle;
			lbl_temp2.Size = new Size(250, 20);
			lbl_temp2.Location = new Point(260, 0);
			lbl_temp2.Text = "Current State";
			pnl_Services.Controls.Add(lbl_temp2);
		}

		protected void Build_Options_Selection_Panel()
		{
			pnl_Options = new Panel();
			pnl_Options.Location = new Point(10, 485 - 10);
			pnl_Options.Size = new Size(1000 - 75, 175);
			pnl_Options.Visible = false;
			pnl_Options.BackColor = Color.Transparent;
			Controls.Add(pnl_Options);
		}

		protected void Reset_Options_Back()
		{
		}

		protected void Fill2_ExchangeControls()
		{
			int startx = 10;
			int starty = 25;
			int posx = startx;
			int posy = starty;
			int count = 0;

			ArrayList al = new ArrayList();
			foreach (string ex in ExchangeNodeNameList)
			{
				Node tmpBusinessNode = (Node)ExchangeNodes[ex];

				al.Add(new ToggleButtonBarItem(count, ex, tmpBusinessNode));
				count++;
			}

			Label lbl_temp = new Label();
			lbl_temp.ForeColor = TitleForeColour;
			lbl_temp.BackColor = Color.Transparent;
			lbl_temp.Font = Font_SubTitle;
			lbl_temp.Size = new Size(150, 20);
			lbl_temp.Location = new Point(5, 14);
			lbl_temp.Text = "Exchange";
			pnl_Exchanges.Controls.Add(lbl_temp);

			ToggleButtonBar tbb = new ToggleButtonBar(false);
			tbb.SetControlAlignment(emToggleButtonBarAlignment.HORIZONTAL);
			tbb.SetAllowNoneSelected(true);
			//tbb.BackColor = Color.GreenYellow;
			//tbb.BackColor = Color.Transparent;
			tbb.SetOptions(al, 120, 32, 4, 4, "images/buttons/button_70x32_on.png", "images/buttons/button_70x32_active.png",
				"images/buttons/button_70x32_disabled.png", "images/buttons/button_70x32_hover.png", Color.White, "");
			tbb.Size = new Size(800, 300);
			tbb.Location = new Point(250, 5);
			tbb.sendItemSelected += new ToggleButtonBar.ItemSelectedHandler(tbb_sendItemSelected);

			pnl_Exchanges.Controls.Add(tbb);
		}

		protected void rebuildServiceListDisplay()
		{
			showNameBack = false;
			pnl_Services.Visible = false;
			Fill_Big_Service_display();
			pnl_Services.Visible = true;
			showNameBack = true;

			//Extract out the selected Item 
			Selected_ServiceName = string.Empty;
			selectedServiceNode = null;
			pnl_Options.Visible = false;

			Refresh();
		}

		protected void tbb_sendItemSelected(object sender, string item_name_selected, object selected_object)
		{
			selectedBusinessNode = (Node)selected_object;
			rebuildServiceListDisplay();
		}

		protected void DetermineServiceCurrentProvision(Node businessServiceOrDefinitionNode,
			out bool IsInPrivateCloud, out bool IsInPublicCloud, out bool IsInIAAS, out bool IsInSAAS, out bool IsInTradServer, out bool isDeployed)
		{
			IsInTradServer = false;
			IsInPrivateCloud = false;
			IsInPublicCloud = false;
			IsInIAAS = false;
			IsInSAAS = false;
			isDeployed = false;

			Node vmInstance = null;
			if (businessServiceOrDefinitionNode != null)
			{
				if (businessServiceOrDefinitionNode.GetAttribute("type") == "business_service")
				{
					isDeployed = true;
					vmInstance = model.GetNamedNode(businessServiceOrDefinitionNode.GetAttribute("vm_instance"));
				}
			}
			if (vmInstance != null)
			{
				List<Node> servers = new List<Node> ();
				foreach (Node vmInstanceLinkToServer in vmInstance.GetChildrenOfType("vm_instance_link_to_server"))
				{
					servers.Add(model.GetNamedNode(vmInstanceLinkToServer.GetAttribute("server")));
				}
				if (servers.Count > 0)
				{
					Node server = servers[0];
					Node rack = server.Parent;

					IsInTradServer = (rack.GetAttribute("owner") == "traditional");
				}
				else
				{
					IsInTradServer = false;
				}
			}

			Node cloud = model.GetNamedNode(businessServiceOrDefinitionNode.GetAttribute("cloud"));
			if (cloud != null)
			{
				foreach (Node locationReference in cloud.GetChildrenOfType("cloud_location"))
				{
					Node location = model.GetNamedNode(locationReference.GetAttribute("location"));
					foreach (Node server in location.GetChildrenOfType("server"))
					{
						if (server.GetBooleanAttribute("is_cloud_server", false))
						{
							IsInPublicCloud = true;

							if (server.GetBooleanAttribute("iaas", false))
							{
								IsInIAAS = true;
								break;
							}
							else if (server.GetBooleanAttribute("saas", false))
							{
								IsInSAAS = true;
								break;
							}
						}
					}
				}
			}

			IsInPrivateCloud = isDeployed && (!IsInPublicCloud) && !IsInTradServer;
		}

		private void ClearServiceChoiceControls()
		{
			foreach (Label lbl in ServiceLabels.Values)
			{
				if (lbl != null)
				{
					pnl_Services.Controls.Remove(lbl);
					lbl.Dispose();
				}
			}
			ServiceLabels.Clear();

			foreach (Label lbl in ServiceOtherLabels.Values)
			{
				if (lbl != null)
				{
					pnl_Services.Controls.Remove(lbl);
					lbl.Dispose();
				}
			}
			ServiceOtherLabels.Clear();

			foreach (ImageTextButton itb in Service_Title_Buttons.Values)
			{
				if (itb != null)
				{
					pnl_Services.Controls.Remove(itb);
					itb.ButtonPressed += new ImageButton.ImageButtonEventArgsHandler(BtnServiceTitle_ButtonPressed);
					itb.DoubleClick += new EventHandler(BtnServiceTitle_DoubleClick);
					itb.DoubleClick += new EventHandler(privateButton_DoubleClick);
					itb.Dispose();
				}
			}

			foreach (ImageTextButton itb in Service_Private_Buttons.Values)
			{
				if (itb != null)
				{
					pnl_Services.Controls.Remove(itb);
					itb.ButtonPressed += new ImageButton.ImageButtonEventArgsHandler(privateButton_ButtonPressed);
					itb.DoubleClick += new EventHandler(privateButton_DoubleClick);
					itb.Dispose();
				}
			}
			foreach (ImageTextButton itb in Service_IAAS_Buttons.Values)
			{
				if (itb != null)
				{
					pnl_Services.Controls.Remove(itb);
					itb.ButtonPressed += new ImageButton.ImageButtonEventArgsHandler(iaasButton_ButtonPressed);
					itb.DoubleClick += new EventHandler(iaasButton_DoubleClick);
					itb.Dispose();
				}
			}
			foreach (ImageTextButton itb in Service_SAAS_Buttons.Values)
			{
				if (itb != null)
				{
					pnl_Services.Controls.Remove(itb);
					itb.ButtonPressed += new ImageButton.ImageButtonEventArgsHandler(saasButton_ButtonPressed);
					itb.DoubleClick += new EventHandler(saasButton_DoubleClick);
					itb.Dispose();
				}
			}
		}

		protected void Fill_Big_Service_display()
		{
			int startx = 5;
			int starty = 25;
			int posx = startx;
			int posy = starty;
			int gapx = 5;
			int gapy = 5;
			int serviceButtonWidth = 120;
			int serviceButtonHeight = 25;

			int BtnServiceTitleWidth = 240;
			int BtnServiceTitleHeight = 25;
			int serviceStatusLabelWidth = 120;
			int serviceStatusLabelHeight = 25;

			int serviceRowHeight = 35;
			int serviceCounter = 0;

			bool IsPrivate = false;
			bool IsPublic = false;
			bool IsIAAS = false;
			bool IsSAAS = false;
			bool IsTrad = false;
			bool isDeployed = false;

			ClearServiceChoiceControls();
			Service_Title_Buttons.Clear();
			Service_Private_Buttons.Clear();
			Service_IAAS_Buttons.Clear();
			Service_SAAS_Buttons.Clear();
			ServiceBackColorLookup.Clear();

			if (selectedBusinessNode != null)
			{
				string attributeName = CONVERT.Format("available_in_cloud_panel_round_{0}", roundVariables.GetIntAttribute("current_round", 0));

				foreach (Node businessServiceOrDefinition in orderPlanner.GetBusinessServicesOrDefinitionsSuitableForCloudDeployment(selectedBusinessNode))
				{
					if (string.IsNullOrEmpty(businessServiceOrDefinition.GetAttribute("demand_name"))
						&& (businessServiceOrDefinition.GetBooleanAttribute(attributeName, false)
								|| (businessServiceOrDefinition.GetAttribute("common_service_name") == "Automated Search and Match")))
					{
						serviceCounter++;
						//only show the "low" security items 
						string security_level = businessServiceOrDefinition.GetAttribute("security");
						string name = businessServiceOrDefinition.GetAttribute("name");

						posx = startx;

						Debug.WriteLine(name + "  " + security_level);
						//CLD_SEC_CHK
						//if (security_level.Equals("low", StringComparison.InvariantCultureIgnoreCase))
						if (true)
						{
							//servicePanel.AddItem(businessServiceOrDefinition, businessServiceOrDefinition.GetAttribute("desc"), buttonImageName);
							string display_code = businessServiceOrDefinition.GetAttribute("desc");

							DetermineServiceCurrentProvision(businessServiceOrDefinition, out IsPrivate, out IsPublic, out IsIAAS, out IsSAAS, out IsTrad, out isDeployed);

							//==================================================
							//==Build the service Name Title Label
							//==================================================
							//Label lbl_ServiceTitle = new Label();
							//lbl_ServiceTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
							//lbl_ServiceTitle.ForeColor = ServiceNameForeColor_Unselect;
							//lbl_ServiceTitle.BackColor = Color.Transparent;
							//lbl_ServiceTitle.BackColor = ServiceNameBackColor_Odd;
							//if (serviceCounter % 2 == 0)
							//{
							//  lbl_ServiceTitle.BackColor = ServiceNameBackColor_Even;
							//}
							//ServiceBackColorLookup.Add(lbl_ServiceTitle, lbl_ServiceTitle.BackColor);

							//lbl_ServiceTitle.Font = this.Font_ServiceName_UnSelected;
							//lbl_ServiceTitle.Size = new System.Drawing.Size(serviceTitleLabelWidth, serviceTitleLabelHeight);
							//lbl_ServiceTitle.Location = new Point(posx, posy);
							//lbl_ServiceTitle.Tag = businessServiceOrDefinition;
							//lbl_ServiceTitle.Text = display_code;
							//lbl_ServiceTitle.Click += new EventHandler(lbl_ServiceTitle_Click);
							//lbl_ServiceTitle.DoubleClick += new EventHandler(lbl_ServiceTitle_DoubleClick);
							//pnl_Services.Controls.Add(lbl_ServiceTitle);
							//ServiceLabels.Add(businessServiceOrDefinition, lbl_ServiceTitle);
							//posx += lbl_ServiceTitle.Width + gapx;

							ImageTextButton BtnServiceTitle = new ImageTextButton("images/buttons/button_240x25.png");
							BtnServiceTitle.Size = new Size(BtnServiceTitleWidth, BtnServiceTitleHeight);
							BtnServiceTitle.Location = new Point(posx, posy);
							BtnServiceTitle.SetButtonText(display_code, Color.White, Color.White, Color.White, Color.DimGray);
							BtnServiceTitle.ButtonPressed += new ImageButton.ImageButtonEventArgsHandler(BtnServiceTitle_ButtonPressed);
							BtnServiceTitle.DoubleClick += new EventHandler(BtnServiceTitle_DoubleClick);
							BtnServiceTitle.Tag = businessServiceOrDefinition;
							BtnServiceTitle.Visible = true;
							pnl_Services.Controls.Add(BtnServiceTitle);
							Service_Title_Buttons.Add("serviceBtn" + CONVERT.ToStr(serviceCounter), BtnServiceTitle);
							posx += (BtnServiceTitle.Width + gapx);

							//==================================================
							//==Build the service Name Status Label
							//==================================================
							Label lbl_ServiceStatus = new Label();
							lbl_ServiceStatus.ForeColor = ServiceNameForeColor_Unselect;
							lbl_ServiceStatus.BackColor = Color.Transparent;
							ServiceBackColorLookup.Add(lbl_ServiceStatus, lbl_ServiceStatus.BackColor);

							lbl_ServiceStatus.Font = Font_ServiceName_UnSelected;
							lbl_ServiceStatus.Size = new Size(serviceStatusLabelWidth, serviceStatusLabelHeight);
							lbl_ServiceStatus.Location = new Point(posx, posy);
							lbl_ServiceStatus.Tag = businessServiceOrDefinition;
							lbl_ServiceStatus.Text = "";
							lbl_ServiceStatus.Click += new EventHandler(lbl_ServiceStatus_Click);
							lbl_ServiceStatus.DoubleClick += new EventHandler(lbl_ServiceStatus_DoubleClick);

							pnl_Services.Controls.Add(lbl_ServiceStatus);
							ServiceLabels.Add("Status"+CONVERT.ToStr(serviceCounter), lbl_ServiceStatus);
							posx += lbl_ServiceStatus.Width + gapx;
							lbl_ServiceStatus.BringToFront();

							//======================================================================
							//==Build the service Buttons (Private IAAS and SAAS) 
							//======================================================================
							ImageTextButton privateButton = new ImageTextButton("images/buttons/button_85x25.png");
							//privateButton.SetAutoSize();
							privateButton.Size = new Size(serviceButtonWidth, serviceButtonHeight);
							privateButton.Location = new Point(posx, posy);
							privateButton.SetButtonText("Private", Color.White, Color.White, Color.White, Color.DimGray);
							privateButton.ButtonPressed += new ImageButton.ImageButtonEventArgsHandler(privateButton_ButtonPressed);
							privateButton.DoubleClick += new EventHandler(privateButton_DoubleClick);
							privateButton.Tag = businessServiceOrDefinition;
							privateButton.Visible = false;
							pnl_Services.Controls.Add(privateButton);
							Service_Private_Buttons.Add("privateBtn" + CONVERT.ToStr(serviceCounter), privateButton);
							posx += (privateButton.Width + gapx);

							ImageTextButton iaasButton = new ImageTextButton("images/buttons/button_85x25.png");
							//iaasButton.SetAutoSize();
							iaasButton.Size = new Size(serviceButtonWidth, serviceButtonHeight);
							iaasButton.Location = new Point(posx, posy);
							iaasButton.SetButtonText("IaaS", Color.White, Color.White, Color.White, Color.DimGray);
							iaasButton.ButtonPressed += new ImageButton.ImageButtonEventArgsHandler(iaasButton_ButtonPressed);
							iaasButton.DoubleClick += new EventHandler(iaasButton_DoubleClick);
							iaasButton.Tag = businessServiceOrDefinition;
							iaasButton.Visible = false;
							pnl_Services.Controls.Add(iaasButton);
							Service_IAAS_Buttons.Add("iaasBtn" + CONVERT.ToStr(serviceCounter), iaasButton);
							posx += (iaasButton.Width + gapx);

							ImageTextButton saasButton = new ImageTextButton("images/buttons/button_85x25.png");
							//saasButton.SetAutoSize();
							saasButton.Size = new Size(serviceButtonWidth, serviceButtonHeight);
							saasButton.Location = new Point(posx, posy);
							saasButton.SetButtonText("SaaS", Color.White, Color.White, Color.White, Color.DimGray);
							saasButton.ButtonPressed += new ImageButton.ImageButtonEventArgsHandler(saasButton_ButtonPressed);
							saasButton.DoubleClick += new EventHandler(saasButton_DoubleClick);
							saasButton.Tag = businessServiceOrDefinition;
							saasButton.Visible = false;
							pnl_Services.Controls.Add(saasButton);
							Service_SAAS_Buttons.Add("saasBtn" + CONVERT.ToStr(serviceCounter), saasButton);
							posx += (saasButton.Width + gapx);

							posx = startx;
							posy += serviceRowHeight + gapy;

							//======================================================================
							//==Dtermine whether this are switched ON
							//======================================================================
							bool canDeployPrivate = businessServiceOrDefinition.GetBooleanAttribute("can_deploy_private", false);
							bool canDeployIaaS = businessServiceOrDefinition.GetBooleanAttribute("can_deploy_iaas", false);
							bool canDeploySaaS = businessServiceOrDefinition.GetBooleanAttribute("can_deploy_saas", false);

							isDeployed = false;
							bool hasBeenDeveloped = businessServiceOrDefinition.GetBooleanAttribute("has_been_developed", false);
							bool isInPublicCloud = false;
							bool isInPrivateCloud = false;
							bool isInIaaS = false;
							bool isInSaaS = false;
							bool isOnTraditionalServer = false;

							Node businessService = null;
							string businessServiceName = "";
							switch (businessServiceOrDefinition.GetAttribute("type"))
							{
								case "business_service":
									businessServiceName = businessServiceOrDefinition.GetAttribute("name");
									businessService = businessServiceOrDefinition;
									break;

								case "new_service":
									businessServiceName = businessServiceOrDefinition.GetAttribute("service_name");
									businessService = model.GetNamedNode(businessServiceName);
									break;

								default:
									Debug.Assert(false);
									break;
							}

							DetermineServiceCurrentProvision(businessServiceOrDefinition, out isInPrivateCloud, out isInPublicCloud, out isInIaaS, out isInSaaS, out isOnTraditionalServer, out isDeployed);

							if (isOnTraditionalServer)
							{
								lbl_ServiceStatus.Text = "Traditional";
							}
							else if (isInIaaS)
							{
								lbl_ServiceStatus.Text = "IaaS (" + orderPlanner.GetSelectedCloudProvider(businessServiceOrDefinition).GetAttribute("short_desc") + ")";
							}
							else if (isInSaaS)
							{
								lbl_ServiceStatus.Text = "SaaS (" + orderPlanner.GetSelectedCloudProvider(businessServiceOrDefinition).GetAttribute("short_desc") + ")";
							}
							else if (isInPrivateCloud)
							{
								lbl_ServiceStatus.Text = "Private";
							}
							else
							{
								lbl_ServiceStatus.Text = "";
							}

							isAlreadyInPrivateCloud = isInPrivateCloud;

							privateButton.Enabled = canDeployPrivate && isDeployed && hasBeenDeveloped;
							iaasButton.Enabled = canDeployIaaS && isDeployed && hasBeenDeveloped;
							saasButton.Enabled = canDeploySaaS;

							if (isInPrivateCloud)
							{
								privateButton.Enabled = false;
							}
							//privateButton.Active = isInPrivateCloud;
							//iaasButton.Active = isInIaaS;
							//saasButton.Active = isInSaaS;
						}
					}
				}
			}
		}

		protected void ClearDisplayedOptions()
		{
			pnl_Options.Visible = false;
			Clear_ChoiceControls();
			//Extract out the selected Item 
			Selected_ServiceName = string.Empty;
			selectedServiceNode = null;
		}

		protected void Handle_ServiceTitle_Button(object sender)
		{
			if (sender is ImageTextButton)
			{
				ImageTextButton tmpITB = (ImageTextButton)sender;
				Node ServiceNodeObj = (Node)tmpITB.Tag;
				tmpITB.Active = true;

				setHighlightServiceLabel(ServiceNodeObj);

				foreach (ImageTextButton btn in Service_Private_Buttons.Values)
				{
					if (btn.Tag == ServiceNodeObj)
					{
						btn.Visible = true;
					}
					else
					{
						btn.Visible = false;
					}

					btn.Active = false;
				}

				foreach (ImageTextButton btn in Service_IAAS_Buttons.Values)
				{
					if (btn.Tag == ServiceNodeObj)
					{
						btn.Visible = true;
					}
					else
					{
						btn.Visible = false;
					}
					btn.Active = false;
				}

				foreach (ImageTextButton btn in Service_SAAS_Buttons.Values)
				{
					if (btn.Tag == ServiceNodeObj)
					{
						btn.Visible = true;
					}
					else
					{
						btn.Visible = false;
					}
					btn.Active = false;
				}
				ClearDisplayedOptions();
			}
		}

		protected void BtnServiceTitle_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			Handle_ServiceTitle_Button(sender);
		}
		protected void BtnServiceTitle_DoubleClick(object sender, EventArgs e)
		{
			Handle_ServiceTitle_Button(sender);
		}

		protected void Handle_Service_Selection(object sender)
		{
			if (sender is Label)
			{
				Label tmpLabel = (Label)sender;
				Node ServiceNodeObj = (Node)tmpLabel.Tag;

				setHighlightServiceLabel(ServiceNodeObj);

				foreach (ImageTextButton btn in Service_Private_Buttons.Values)
				{
					if (btn.Tag == ServiceNodeObj)
					{
						btn.Visible = true;
					}
					else
					{
						btn.Visible = false;
					}
				}

				foreach (ImageTextButton btn in Service_IAAS_Buttons.Values)
				{
					if (btn.Tag == ServiceNodeObj)
					{
						btn.Visible = true;
					}
					else
					{
						btn.Visible = false;
					}
				}

				foreach (ImageTextButton btn in Service_SAAS_Buttons.Values)
				{
					if (btn.Tag == ServiceNodeObj)
					{
						btn.Visible = true;
					}
					else
					{
						btn.Visible = false;
					}
				}
				ClearDisplayedOptions();
			}
		}

		protected void lbl_ServiceStatus_DoubleClick(object sender, EventArgs e)
		{
			Handle_Service_Selection(sender);
		}

		protected void lbl_ServiceStatus_Click(object sender, EventArgs e)
		{
			Handle_Service_Selection(sender);
		}

		protected void lbl_ServiceTitle_DoubleClick(object sender, EventArgs e)
		{
			Handle_Service_Selection(sender);
		}

		protected void lbl_ServiceTitle_Click(object sender, EventArgs e)
		{
			Handle_Service_Selection(sender);
		}

		protected void Clear_DC_ChoiceControls()
		{
			foreach (Object obj in DCOtherCtrls.Values)
			{
				if (obj is Label)
				{
					Label lbl = (Label)obj;
					pnl_Options.Controls.Remove(lbl);
					lbl.Dispose();
				}
				if (obj is TextBox)
				{
					TextBox tb = (TextBox)obj;
					pnl_Options.Controls.Remove(tb);
					tb.Dispose();
				}

				if (obj is ImageTextButton)
				{
					ImageTextButton itb = (ImageTextButton)obj;
					pnl_Options.Controls.Remove(itb);
					itb.Dispose();
				}
				if (obj is ImageToggleButton)
				{
					ImageToggleButton itb = (ImageToggleButton)obj;
					pnl_Options.Controls.Remove(itb);
					itb.Dispose();
				}
			}
			DCOtherCtrls.Clear();

			foreach (Object obj in DCButtons.Values)
			{
				if (obj is Label)
				{
					Label lbl = (Label)obj;
					pnl_Options.Controls.Remove(lbl);
					lbl.Dispose();
				}
				if (obj is ImageTextButton)
				{
					ImageTextButton itb = (ImageTextButton)obj;
					pnl_Options.Controls.Remove(itb);
					itb.Dispose();
				}
				if (obj is ImageToggleButton)
				{
					ImageToggleButton itb = (ImageToggleButton)obj;
					pnl_Options.Controls.Remove(itb);
					itb.Dispose();
				}
			}
			DCButtons.Clear();
		}

		protected void Clear_IAAS_ChoiceControls()
		{
			foreach (Object obj in IAAS_OtherCtrls.Values)
			{
				if (obj is Label)
				{
					Label lbl = (Label)obj;
					pnl_Options.Controls.Remove(lbl);
					lbl.Dispose();
				}
				if (obj is TextBox)
				{
					TextBox lbl = (TextBox)obj;
					pnl_Options.Controls.Remove(lbl);
					lbl.Dispose();
				}
				if (obj is ImageTextButton)
				{
					ImageTextButton itb = (ImageTextButton)obj;
					pnl_Options.Controls.Remove(itb);
					itb.Dispose();
				}
			}
			IAAS_OtherCtrls.Clear();

			foreach (Object obj in IAAS_Aspect_BuildType_Buttons.Values)
			{
				if (obj is Label)
				{
					Label lbl = (Label)obj;
					pnl_Options.Controls.Remove(lbl);
					lbl.Dispose();
				}
				if (obj is ImageTextButton)
				{
					ImageTextButton itb = (ImageTextButton)obj;
					pnl_Options.Controls.Remove(itb);
					itb.Dispose();
				}
				if (obj is ImageToggleButton)
				{
					ImageToggleButton itb = (ImageToggleButton)obj;
					pnl_Options.Controls.Remove(itb);
					itb.Dispose();
				}
			}
			IAAS_Aspect_BuildType_Buttons.Clear();

			foreach (Object obj in IAAS_Aspect_ChargeModel_Buttons.Values)
			{
				if (obj is Label)
				{
					Label lbl = (Label)obj;
					pnl_Options.Controls.Remove(lbl);
					lbl.Dispose();
				}
				if (obj is ImageTextButton)
				{
					ImageTextButton itb = (ImageTextButton)obj;
					pnl_Options.Controls.Remove(itb);
					itb.Dispose();
				}
				if (obj is ImageToggleButton)
				{
					ImageToggleButton itb = (ImageToggleButton)obj;
					pnl_Options.Controls.Remove(itb);
					itb.Dispose();
				}
			}
			IAAS_Aspect_ChargeModel_Buttons.Clear();

			foreach (Object obj in IAAS_Aspect_Vendor_Buttons.Values)
			{
				if (obj is Label)
				{
					Label lbl = (Label)obj;
					pnl_Options.Controls.Remove(lbl);
					lbl.Dispose();
				}
				if (obj is ImageTextButton)
				{
					ImageTextButton itb = (ImageTextButton)obj;
					pnl_Options.Controls.Remove(itb);
					itb.Dispose();
				}
				if (obj is ImageToggleButton)
				{
					ImageToggleButton itb = (ImageToggleButton)obj;
					pnl_Options.Controls.Remove(itb);
					itb.Dispose();
				}
			}
			IAAS_Aspect_Vendor_Buttons.Clear();
		}

		protected void Clear_SAAS_ChoiceControls()
		{
			foreach (Object obj in SAAS_OtherCtrls.Values)
			{
				if (obj is Label)
				{
					Label lbl = (Label)obj;
					pnl_Options.Controls.Remove(lbl);
					lbl.Dispose();
				}
				if (obj is TextBox)
				{
					TextBox lbl = (TextBox)obj;
					pnl_Options.Controls.Remove(lbl);
					lbl.Dispose();
				}

				if (obj is ImageTextButton)
				{
					ImageTextButton itb = (ImageTextButton)obj;
					pnl_Options.Controls.Remove(itb);
					itb.Dispose();
				}
			}
			SAAS_OtherCtrls.Clear();

			foreach (Object obj in SAAS_Aspect_Vendor_Buttons.Values)
			{
				if (obj is Label)
				{
					Label lbl = (Label)obj;
					pnl_Options.Controls.Remove(lbl);
					lbl.Dispose();
				}
				if (obj is ImageTextButton)
				{
					ImageTextButton itb = (ImageTextButton)obj;
					pnl_Options.Controls.Remove(itb);
					itb.Dispose();
				}
				if (obj is ImageToggleButton)
				{
					ImageToggleButton itb = (ImageToggleButton)obj;
					pnl_Options.Controls.Remove(itb);
					itb.Dispose();
				}
			}
			SAAS_Aspect_Vendor_Buttons.Clear();
		}

		private void Clear_OKCancel_Buttons()
		{
			if (ok != null)
			{
				pnl_Options.Controls.Remove(ok);
				ok.ButtonPressed -= new ImageButton.ImageButtonEventArgsHandler(ok_ButtonPressed);
				ok.Dispose();
				ok = null;
			}
			if (cancel != null)
			{
				pnl_Options.Controls.Remove(ok);
				cancel.ButtonPressed -= new ImageButton.ImageButtonEventArgsHandler(cancel_ButtonPressed);
				cancel.Dispose();
				cancel = null;
			}
		}

		private void Clear_ChoiceControls()
		{
			Clear_OKCancel_Buttons();
			Clear_DC_ChoiceControls();
			Clear_IAAS_ChoiceControls();
			Clear_SAAS_ChoiceControls();
		}

		protected void Clear_SelectedServices()
		{
			Selected_ServiceName = string.Empty;
			selectedServiceNode = null;

			SelectedOption_BuildType = string.Empty;
			SelectedOption_ChargeModel = string.Empty;
			SelectedOption_DataCentre = string.Empty;
			SelectedOption_DataCentreNode = null;
			SelectedOption_Vendor = string.Empty;
			SelectedOption_Vendor_Node = null;
		}

		protected void setHighlightServiceLabel(Node highlit_Node)
		{
			foreach (ImageTextButton itb in Service_Title_Buttons.Values)
			{
				Node n1 = (Node) itb.Tag;
				if (n1 == highlit_Node)
				{
					itb.Active = true;
				}
				else
				{
					itb.Active = false;
				}
			}
			//we only change the Font to show Select and not the Colors 
			foreach (Label lbl in ServiceLabels.Values)
			{
				Node n1 = (Node)lbl.Tag;
				if (n1 == highlit_Node)
				{
					//lbl.BackColor = ServiceNameBackColor_Select;
					//lbl.ForeColor = ServiceNameForeColor_Select;
					lbl.Font = Font_ServiceName_Selected;
					lbl.Refresh();
				}
				else
				{
					Color tmpBack = ServiceNameBackColor_Odd;
					if (ServiceBackColorLookup.ContainsKey(lbl))
					{
						tmpBack = (Color)ServiceBackColorLookup[lbl];
					}
					//lbl.BackColor = tmpBack;
					//lbl.ForeColor = ServiceNameForeColor_Unselect;
					lbl.Font = Font_ServiceName_UnSelected;
					lbl.Refresh();
				}
			}
		}

		protected void Handle_Private_Button(object sender)
		{
			pnl_Options.Visible = false;
			Clear_SelectedServices();

			//Extract out the selected Item 
			ImageTextButton itb = (ImageTextButton)sender;
			Selected_ServiceName = (itb).GetButtonText();
			selectedServiceNode = (Node)(itb).Tag;
			//setHighlightServiceLabel(selectedServiceNode);

			selected_DeploymentType = OPTION_PRIVATE;
			Fill_Private_ChoiceControls();
			pnl_Options.Visible = true;

			((ImageTextButton) sender).Active = true;
		}
		protected void privateButton_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			Handle_Private_Button(sender);
		}
		protected void privateButton_DoubleClick(object sender, EventArgs e)
		{
			Handle_Private_Button(sender);
		}

		protected void Handle_IAAS_Button(object sender)
		{
			pnl_Options.Visible = false;
			Clear_SelectedServices();
			//Clear_ChoiceControls();

			//Extract out the selected Item 
			ImageTextButton itb = (ImageTextButton)sender;
			Selected_ServiceName = (itb).GetButtonText();
			selectedServiceNode = (Node)(itb).Tag;
			//setHighlightServiceLabel(selectedServiceNode);

			selected_DeploymentType = OPTION_IAAS;
			SelectedOption_DataCentre = "Public IaaS";
			SelectedOption_DataCentreNode = model.GetNamedNode("Public IaaS Cloud Server");

			Fill_IAAS_ChoiceControls();
			pnl_Options.Visible = true;
			((ImageTextButton) sender).Active = true;
		}
		protected void iaasButton_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			Handle_IAAS_Button(sender);
		}
		protected void iaasButton_DoubleClick(object sender, EventArgs e)
		{
			Handle_IAAS_Button(sender);
		}

		protected void Handle_SAAS_Button(object sender)
		{
			pnl_Options.Visible = false;
			Clear_SelectedServices();
			//Clear_ChoiceControls();

			//Extract out the selected Item 
			ImageTextButton itb = (ImageTextButton)sender;
			Selected_ServiceName = (itb).GetButtonText();
			selectedServiceNode = (Node)(itb).Tag;
			//setHighlightServiceLabel(selectedServiceNode);

			selected_DeploymentType = OPTION_SAAS;
			SelectedOption_DataCentre = "Public SaaS";
			SelectedOption_DataCentreNode = model.GetNamedNode("Public SaaS Cloud Server");

			Fill_SAAS_ChoiceControls();
			pnl_Options.Visible = true;
			((ImageTextButton) sender).Active = true;
		}
		protected void saasButton_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			Handle_SAAS_Button(sender);
		}
		protected void saasButton_DoubleClick(object sender, EventArgs e)
		{
			Handle_SAAS_Button(sender);
		}

		#region Private Options System

		private void Fill_Private_ChoiceControls()
		{
			int startx = 10;
			int starty = 35;
			int posx = startx;
			int posy = starty;
			int gapx = 5;
			int option_buttonWidth = 85;
			int posx_clear_button = 520;

			Node currentDatacenter = null;

			if (isAlreadyInPrivateCloud)
			{
				if (selectedServiceNode.GetAttribute("type") == "business_service")
				{
					Node vmInstance = model.GetNamedNode(selectedServiceNode.GetAttribute("vm_instance"));
					if (vmInstance != null)
					{
						foreach (Node vmInstanceLinkToServer in vmInstance.GetChildrenOfType("vm_instance_link_to_server"))
						{
							Node server = model.GetNamedNode(vmInstanceLinkToServer.GetAttribute("server"));
							Node rack = server.Parent;
							Node datacenter = rack.Parent;
							if (datacenter.GetAttribute("type") == "datacenter")
							{
								currentDatacenter = datacenter;
								break;
							}
						}
					}
				}
			}

			//Now clear any old controls 
			Clear_ChoiceControls();

			int effectiveWidth = pnl_Options.Width-10;
			if (ShowFeedBack)
			{
				effectiveWidth = effectiveWidth - FeedBackWidth;
			}

			ok = new ImageTextButton(@"images\buttons\button_85x25.png");
			ok.SetAutoSize();
			ok.Location = new Point(effectiveWidth - ((ok.Width + 10) * 2), pnl_Options.Height - (ok.Height + 10));
			ok.SetButtonText("OK", Color.White, Color.White, Color.White, Color.DimGray);
			ok.ButtonPressed += new ImageButton.ImageButtonEventArgsHandler(ok_ButtonPressed);
			pnl_Options.Controls.Add(ok);

			cancel = new ImageTextButton(@"images\buttons\button_85x25.png");
			cancel.SetAutoSize();
			cancel.Location = new Point(effectiveWidth - ((cancel.Width + 10) * 1), pnl_Options.Height - (ok.Height + 10));
			cancel.SetButtonText("Cancel", Color.White, Color.White, Color.White, Color.DimGray);
			cancel.ButtonPressed += new ImageButton.ImageButtonEventArgsHandler(cancel_ButtonPressed);
			pnl_Options.Controls.Add(cancel);

			string display_service_Name = selectedServiceNode.GetAttribute("short_name");

			//Rebuild New Controls
			lblSelectDCTitle_Main = new Label();
			lblSelectDCTitle_Main.ForeColor = TitleForeColour;
			lblSelectDCTitle_Main.Location = new Point(10, 5);
			lblSelectDCTitle_Main.Size = new Size(480, 30);
			lblSelectDCTitle_Main.Text = "Select Options for " + display_service_Name;
			lblSelectDCTitle_Main.Font = Font_SubTitle;
			lblSelectDCTitle_Main.Visible = true;
			pnl_Options.Controls.Add(lblSelectDCTitle_Main);
			DCOtherCtrls.Add("label1", lblSelectDCTitle_Main);

			lblSelectDC_Aspect_1_Title = new Label();
			lblSelectDC_Aspect_1_Title.ForeColor = TitleForeColour;
			lblSelectDC_Aspect_1_Title.Location = new Point(posx, posy);
			lblSelectDC_Aspect_1_Title.Size = new Size(120, 30);
			lblSelectDC_Aspect_1_Title.Text = "Exchange";
			lblSelectDC_Aspect_1_Title.Font = Font_SubTitle;
			lblSelectDC_Aspect_1_Title.Visible = true;
			pnl_Options.Controls.Add(lblSelectDC_Aspect_1_Title);
			DCOtherCtrls.Add("label2", lblSelectDC_Aspect_1_Title);
			posx += lblSelectDC_Aspect_1_Title.Width + 10;

			//data used to select the existing one or the correct one 
			//Still waiting on Model code to extract the Current Data Centre 
			ImageToggleButton itb_dc_first_option = null;
			bool dc_selected = false;

			int ctl_count = 0;
			foreach (Node datacenter in orderPlanner.GetDatacenters())
			{
				string display_name = datacenter.GetAttribute("desc");

				ImageToggleButton itb_temp = new ImageToggleButton(0, "images/buttons/button_65x25_on.png", "images/buttons/button_65x25_active.png", display_name, display_name);
				itb_temp.Name = "ITB VM " + datacenter.GetAttribute("desc");
				itb_temp.setTextForeColor(Color.White);
				itb_temp.Size = new Size(option_buttonWidth, 25);
				itb_temp.Location = new Point(posx, posy);
				itb_temp.Font = Font_Body;
				itb_temp.Tag = datacenter;

				if (currentDatacenter == datacenter)
				{
					itb_temp.State = 1;
					Node tmpNode = (Node)itb_temp.Tag;
					SelectedOption_DataCentre = tmpNode.GetAttribute("desc");
					SelectedOption_DataCentreNode = tmpNode;
					dc_selected = true;
				}
				else
				{
					itb_temp.State = 0;
				}
				
				if (itb_dc_first_option == null)
				{
					itb_dc_first_option = itb_temp;
				}

				itb_temp.ButtonPressed += new ImageButton.ImageButtonEventArgsHandler(itb_DCButton_ButtonPressed);
				itb_temp.DoubleClick += new EventHandler(itb_DCButton_DoubleClick);
				pnl_Options.Controls.Add(itb_temp);
				DCButtons.Add("ITB VM " + CONVERT.ToStr(ctl_count) + datacenter.GetAttribute("desc"), itb_temp);
				posx += (itb_temp.Width + gapx);

				itb_temp.Active = (datacenter == currentDatacenter);
				ctl_count++;
			}

			if (dc_selected==false)
			{
				itb_dc_first_option.State = 1;
				Node tmpNode = (Node)itb_dc_first_option.Tag;
				SelectedOption_DataCentre = tmpNode.GetAttribute("desc");
				SelectedOption_DataCentreNode = tmpNode;
			}

			if (ShowClearButtons)
			{
				posx = posx_clear_button;
				ImageTextButton itb_DCButton_Clear = new ImageTextButton("images/buttons/button_65x25.png");
				itb_DCButton_Clear.Size = new Size(65, 25);
				itb_DCButton_Clear.Location = new Point(posx, posy);
				itb_DCButton_Clear.Tag = null;
				itb_DCButton_Clear.SetButtonText("Clear", Color.White, Color.White, Color.White, Color.DimGray);
				itb_DCButton_Clear.ButtonPressed += new ImageButton.ImageButtonEventArgsHandler(itb_DCButton_Clear_ButtonPressed);
				itb_DCButton_Clear.DoubleClick += new EventHandler(itb_DCButton_Clear_DoubleClick);
				pnl_Options.Controls.Add(itb_DCButton_Clear);
				DCOtherCtrls.Add("privateBtn" + CONVERT.ToStr(ctl_count), itb_DCButton_Clear);
				posx += (itb_DCButton_Clear.Width + gapx);
			}

			//Rebuild New Controls
			if (showSelectedOptionsText)
			{
				lblSelectDCTitle_Select = new Label();
				lblSelectDCTitle_Select.ForeColor = TitleForeColour;
				lblSelectDCTitle_Select.Location = new Point(posx, 5);
				lblSelectDCTitle_Select.Size = new Size(100, 30);
				lblSelectDCTitle_Select.Text = "Selected Option";
				lblSelectDCTitle_Select.Font = Font_BodyBold;
				lblSelectDCTitle_Select.Visible = true;
				pnl_Options.Controls.Add(lblSelectDCTitle_Select);
				DCOtherCtrls.Add("label1S", lblSelectDCTitle_Select);
			}

			if (showSelectedOptionsText)
			{
				lblSelectDC_Aspect_1_Selected = new Label();
				lblSelectDC_Aspect_1_Selected.ForeColor = TitleForeColour;
				lblSelectDC_Aspect_1_Selected.Location = new Point(posx, posy);
				lblSelectDC_Aspect_1_Selected.Size = new Size(100, 30);
				lblSelectDC_Aspect_1_Selected.Text = OPTION_NONE_SELECT;
				lblSelectDC_Aspect_1_Selected.Font = Font_Body;
				lblSelectDC_Aspect_1_Selected.Visible = true;
				pnl_Options.Controls.Add(lblSelectDC_Aspect_1_Selected);
				DCOtherCtrls.Add("label3", lblSelectDC_Aspect_1_Selected);
				posx += lblSelectDC_Aspect_1_Selected.Width + 10;
			}

			if (ShowFeedBack)
			{
				lblSelectDC_FeedbackTitle = new Label();
				lblSelectDC_FeedbackTitle.ForeColor = TitleForeColour;
				lblSelectDC_FeedbackTitle.Location = new Point(pnl_Options.Width - FeedBackWidth - 10, 5);
				lblSelectDC_FeedbackTitle.Size = new Size(220, 25);
				lblSelectDC_FeedbackTitle.Text = "Feedback";
				lblSelectDC_FeedbackTitle.Font = Font_BodyBold;
				lblSelectDC_FeedbackTitle.Visible = true;
				pnl_Options.Controls.Add(lblSelectDC_FeedbackTitle);
				DCOtherCtrls.Add("label1FT", lblSelectDC_FeedbackTitle);

				tb_SelectDC_FeedbackValue = new TextBox();
				tb_SelectDC_FeedbackValue.Multiline = true;
				tb_SelectDC_FeedbackValue.ReadOnly = true;
				tb_SelectDC_FeedbackValue.Visible = true;
				tb_SelectDC_FeedbackValue.Size = new Size(FeedBackWidth, pnl_Options.Height - 40);
				tb_SelectDC_FeedbackValue.Location = new Point(pnl_Options.Width - FeedBackWidth - 10, 30);
				tb_SelectDC_FeedbackValue.ForeColor = Color.White;
				tb_SelectDC_FeedbackValue.BackColor = Color.FromArgb(32, 32, 32);
				tb_SelectDC_FeedbackValue.BorderStyle = BorderStyle.FixedSingle;
				pnl_Options.Controls.Add(tb_SelectDC_FeedbackValue);
				DCOtherCtrls.Add("label1FV", tb_SelectDC_FeedbackValue);

				tb_Selected_Feedback_handle = tb_SelectDC_FeedbackValue;

				UpdateFeedback();
			}
			Reset_Options_Back();
		}

		protected void Handle_DC_Clear_Button()
		{
			if (showSelectedOptionsText)
			{
				lblSelectDC_Aspect_1_Selected.Text = OPTION_NONE_SELECT;
			}
			foreach (ImageToggleButton itb in DCButtons.Values)
			{
				itb.State = 0;
			}
			SelectedOption_DataCentre = "";
			SelectedOption_DataCentreNode = null;
		}

		protected void itb_DCButton_Clear_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			Handle_DC_Clear_Button();
		}
		protected void itb_DCButton_Clear_DoubleClick(object sender, EventArgs e)
		{
			Handle_DC_Clear_Button();
		}

		protected void Handle_DC_Button(object sender)
		{
			ImageToggleButton itb_sender = (ImageToggleButton)sender;
			if (itb_sender != null)
			{
				itb_sender.State = 1 - itb_sender.State;
				itb_sender.Refresh();

				Node tmpNode = (Node)itb_sender.Tag;
				if (showSelectedOptionsText)
				{
					lblSelectDC_Aspect_1_Selected.Text = tmpNode.GetAttribute("desc");
				}

				SelectedOption_DataCentre = tmpNode.GetAttribute("desc");
				SelectedOption_DataCentreNode = tmpNode;
			}

			foreach (ImageToggleButton itb in DCButtons.Values)
			{
				if (itb != itb_sender)
				{
					itb.State = 0;
				}
			}

			bool noControlSelected = true;
			foreach (ImageToggleButton itb in DCButtons.Values)
			{
				if (itb.State == 1)
				{
					noControlSelected = false;
				}
			}
			if (noControlSelected)
			{
				if (showSelectedOptionsText)
				{
					lblSelectDC_Aspect_1_Selected.Text = OPTION_NONE_SELECT;
				}
				SelectedOption_DataCentre = "";
				SelectedOption_DataCentreNode = null;
			}
			UpdateFeedback();
		}

		void UpdateFeedback ()
		{
			string feedback = "";

			if (SelectedOption_DataCentreNode != null)
			{
				Node service = selectedServiceNode;
				int cpus = service.GetIntAttribute("cpus_required", 0);

				foreach (Node usagePoint in service.GetChildrenOfType("service_cpu_usage_data_point"))
				{
					cpus = Math.Max(cpus, usagePoint.GetIntAttribute("cpus_used", 0));
				}

				Node business = model.GetNamedNode(SelectedOption_DataCentreNode.GetAttribute("business"));
				string region = business.GetAttribute("region");
				Node cloud = model.GetNamedNode(CONVERT.Format("{0} Production Cloud", region));

				feedback = CONVERT.Format("Use {0} CPU from {1}",
										  cpus,
										  cloud.GetAttribute("desc"));
			}

			tb_Selected_Feedback_handle.Text = feedback;
		}

		protected void itb_DCButton_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			Handle_DC_Button(sender);
		}
		protected void itb_DCButton_DoubleClick(object sender, EventArgs e)
		{
			Handle_DC_Button(sender);
		}

		#endregion Private Options System

		#region IAAS

		protected void Fill_IAAS_ChoiceControls()
		{
			int startx = 10;
			int starty = 35;
			int posx = startx;
			int posy = starty;
			int gapx = 5;
			int posx_clear_button = 520;
			int option_buttonWidth_BT = 60;
			int option_buttonWidth_CM = 120;
			int option_buttonWidth_V = 120;

			Clear_ChoiceControls();

			int effectiveWidth = pnl_Options.Width - 10;
			if (ShowFeedBack)
			{
				effectiveWidth = effectiveWidth - FeedBackWidth;
			}

			ok = new ImageTextButton(@"images\buttons\button_85x25.png");
			ok.SetAutoSize();
			ok.Location = new Point(effectiveWidth - ((ok.Width + 10) * 2), pnl_Options.Height - (ok.Height + 10));
			ok.SetButtonText("OK", Color.White, Color.White, Color.White, Color.DimGray);
			ok.ButtonPressed += new ImageButton.ImageButtonEventArgsHandler(ok_ButtonPressed);
			pnl_Options.Controls.Add(ok);

			cancel = new ImageTextButton(@"images\buttons\button_85x25.png");
			cancel.SetAutoSize();
			cancel.Location = new Point(effectiveWidth - ((cancel.Width + 10) * 1), pnl_Options.Height - (ok.Height + 10));
			cancel.SetButtonText("Cancel", Color.White, Color.White, Color.White, Color.DimGray);
			cancel.ButtonPressed += new ImageButton.ImageButtonEventArgsHandler(cancel_ButtonPressed);
			pnl_Options.Controls.Add(cancel);

			string display_service_Name = selectedServiceNode.GetAttribute("short_name");

			//Rebuild New Controls
			lblSelect_IAAS_Title_Main = new Label();
			lblSelect_IAAS_Title_Main.ForeColor = TitleForeColour;
			lblSelect_IAAS_Title_Main.Location = new Point(10, 5);
			lblSelect_IAAS_Title_Main.Size = new Size(480, 30);
			lblSelect_IAAS_Title_Main.Text = "Select Options for " + display_service_Name;
			lblSelect_IAAS_Title_Main.Font = Font_SubTitle;
			lblSelect_IAAS_Title_Main.Visible = true;
			pnl_Options.Controls.Add(lblSelect_IAAS_Title_Main);
			IAAS_OtherCtrls.Add("label1M", lblSelect_IAAS_Title_Main);

			//Rebuild New Controls
			if (showSelectedOptionsText)
			{
				lblSelect_IAAS_Title_Select = new Label();
				lblSelect_IAAS_Title_Select.ForeColor = TitleForeColour;
				lblSelect_IAAS_Title_Select.Location = new Point(posx_clear_button + 55, 5);
				lblSelect_IAAS_Title_Select.Size = new Size(100, 30);
				lblSelect_IAAS_Title_Select.Text = "Selected Option";
				lblSelect_IAAS_Title_Select.Font = Font_BodyBold;
				lblSelect_IAAS_Title_Select.Visible = true;
				pnl_Options.Controls.Add(lblSelect_IAAS_Title_Select);
				IAAS_OtherCtrls.Add("label1S", lblSelect_IAAS_Title_Select);
			}

			//=============================================================
			//==Build Type=================================================
			//=============================================================
			lblSelect_IAAS_Aspect_BuildType_Title = new Label();
			lblSelect_IAAS_Aspect_BuildType_Title.ForeColor = TitleForeColour;
			//lblSelect_IAAS_Aspect_BuildType_Title.BackColor = Color.Purple;
			lblSelect_IAAS_Aspect_BuildType_Title.Location = new Point(posx, posy);
			lblSelect_IAAS_Aspect_BuildType_Title.Size = new Size(120, 30);
			lblSelect_IAAS_Aspect_BuildType_Title.Text = "Build Type";
			lblSelect_IAAS_Aspect_BuildType_Title.Font = Font_SubTitle;
			lblSelect_IAAS_Aspect_BuildType_Title.Visible = true;
			pnl_Options.Controls.Add(lblSelect_IAAS_Aspect_BuildType_Title);
			IAAS_OtherCtrls.Add("label2", lblSelect_IAAS_Aspect_BuildType_Title);
			posx += lblSelect_IAAS_Aspect_BuildType_Title.Width + 0;

			int ctl_count = 0;

			//data used to select the existing one or the correct one 
			string display_service_NameCC = selectedServiceNode.GetAttribute("short_name");
			string existing_build_type = selectedServiceNode.GetAttribute("cloud_build_type");
			string preferred_build_type = selectedServiceNode.GetAttribute("correct_build_code");
			ImageToggleButton itb_correct_option = null;
			ImageToggleButton itb_bt_first_option = null;
			bool build_type_selected = false;

			foreach (int bt_displaycode in BuildTypes_DisplayLookup)
			{
				if (BuildTypes_Lookup.ContainsKey(bt_displaycode))
				{
					string display_name = (string)BuildTypes_Lookup[bt_displaycode];

					ImageToggleButton itb_temp = new ImageToggleButton(0, "images/buttons/button_65x25_on.png", "images/buttons/button_65x25_active.png", display_name, display_name);
					itb_temp.Name = "ITB BT " + display_name;
					itb_temp.setTextForeColor(Color.White);
					itb_temp.Size = new Size(option_buttonWidth_BT, 25);
					itb_temp.Location = new Point(posx, posy);
					itb_temp.Font = Font_Body;
					itb_temp.Tag = display_name;

					if (existing_build_type.Equals(display_name, StringComparison.InvariantCultureIgnoreCase) == true)
					{
						itb_temp.State = 1;
						SelectedOption_BuildType = display_name;
						build_type_selected = true;
					}
					else
					{
						itb_temp.State = 0;
					}
					if (preferred_build_type.Equals(display_name, StringComparison.InvariantCultureIgnoreCase) == true)
					{
						itb_correct_option = itb_temp;
					}
					if (itb_bt_first_option == null)
					{
						itb_bt_first_option = itb_temp;
					}

					itb_temp.ButtonPressed += new ImageButton.ImageButtonEventArgsHandler(itb_IAAS_BuildType_Button_ButtonPressed);
					itb_temp.DoubleClick += new EventHandler(itb_IAAS_BuildType_Button_DoubleClick);
					pnl_Options.Controls.Add(itb_temp);
					IAAS_Aspect_BuildType_Buttons.Add("ITB BT " + CONVERT.ToStr(ctl_count) + display_name, itb_temp);
					posx += (itb_temp.Width + gapx);

					ctl_count++;
				}
			}

			if (build_type_selected == false)
			{
				if (itb_correct_option != null)
				{
					//use correct option as the pre-select
					itb_correct_option.State = 1;
					SelectedOption_BuildType = (string)itb_correct_option.Tag;
				}
				else
				{ 
					//no correct option so use the first one
					itb_bt_first_option.State = 1;
					SelectedOption_BuildType = (string)itb_bt_first_option.Tag;
				}
			}

			posx = posx_clear_button;
			if (ShowClearButtons)
			{
				ImageTextButton itb_IAAS_Aspect_BuildType_Button_Clear = new ImageTextButton("images/buttons/button_65x25.png");
				itb_IAAS_Aspect_BuildType_Button_Clear.Size = new Size(65, 25);
				itb_IAAS_Aspect_BuildType_Button_Clear.Location = new Point(posx, posy);
				itb_IAAS_Aspect_BuildType_Button_Clear.Tag = null;
				itb_IAAS_Aspect_BuildType_Button_Clear.SetButtonText("Clear", Color.White, Color.White, Color.White, Color.DimGray);
				itb_IAAS_Aspect_BuildType_Button_Clear.ButtonPressed += new ImageButton.ImageButtonEventArgsHandler(itb_IAAS_BuildType_Button_Clear_ButtonPressed);
				itb_IAAS_Aspect_BuildType_Button_Clear.DoubleClick += new EventHandler(itb_IAAS_BuildType_Button_Clear_DoubleClick);
				pnl_Options.Controls.Add(itb_IAAS_Aspect_BuildType_Button_Clear);
				IAAS_OtherCtrls.Add("privateBtn_BT" + CONVERT.ToStr(ctl_count), itb_IAAS_Aspect_BuildType_Button_Clear);
				posx += (itb_IAAS_Aspect_BuildType_Button_Clear.Width + gapx);
			}

			if (showSelectedOptionsText)
			{
				lblSelect_IAAS_Aspect_BuildType_Selected = new Label();
				lblSelect_IAAS_Aspect_BuildType_Selected.ForeColor = TitleForeColour;
				lblSelect_IAAS_Aspect_BuildType_Selected.Location = new Point(posx, posy);
				lblSelect_IAAS_Aspect_BuildType_Selected.Size = new Size(100, 30);
				lblSelect_IAAS_Aspect_BuildType_Selected.Text = OPTION_NONE_SELECT;
				lblSelect_IAAS_Aspect_BuildType_Selected.Font = Font_Body;
				lblSelect_IAAS_Aspect_BuildType_Selected.Visible = true;
				pnl_Options.Controls.Add(lblSelect_IAAS_Aspect_BuildType_Selected);
				IAAS_OtherCtrls.Add("label3", lblSelect_IAAS_Aspect_BuildType_Selected);
				posx += lblSelect_IAAS_Aspect_BuildType_Selected.Width + 10;
			}

			posx = startx;
			posy += 35;
			//=============================================================
			//==Charge Model===============================================
			//=============================================================
			lblSelect_IAAS_Aspect_ChargeModel_Title = new Label();
			lblSelect_IAAS_Aspect_ChargeModel_Title.ForeColor = TitleForeColour;
			lblSelect_IAAS_Aspect_ChargeModel_Title.Location = new Point(posx, posy);
			lblSelect_IAAS_Aspect_ChargeModel_Title.Size = new Size(120, 30);
			lblSelect_IAAS_Aspect_ChargeModel_Title.Text = "Charge Model";
			lblSelect_IAAS_Aspect_ChargeModel_Title.Font = Font_SubTitle;
			lblSelect_IAAS_Aspect_ChargeModel_Title.Visible = true;
			pnl_Options.Controls.Add(lblSelect_IAAS_Aspect_ChargeModel_Title);
			IAAS_OtherCtrls.Add("label4", lblSelect_IAAS_Aspect_ChargeModel_Title);
			posx += lblSelect_IAAS_Aspect_ChargeModel_Title.Width + 0;

			ImageToggleButton itb_cm_first_option = null;
			bool charge_model_selected = false;
			string prior_cloud_model = selectedServiceNode.GetAttribute("cloud_charge_model");
			string default_cloud_model = "reserved";

			foreach (string charge_model_option in ChargeModel_Lookup.Keys)
			{
				string display_name = (string)ChargeModel_Lookup[charge_model_option];

				ImageToggleButton itb_temp = new ImageToggleButton(0, "images/buttons/button_65x25_on.png", "images/buttons/button_65x25_active.png", display_name, display_name);
				itb_temp.Name = "ITB BT " + charge_model_option;
				itb_temp.setTextForeColor(Color.White);

				if (prior_cloud_model.Equals(charge_model_option, StringComparison.InvariantCultureIgnoreCase) == true)
				{
					itb_temp.State = 1;
					SelectedOption_ChargeModel = charge_model_option;
					charge_model_selected = true;
				}
				else
				{
					itb_temp.State = 0;
				}

				if (charge_model_selected == false)
				{
					if (default_cloud_model.Equals(charge_model_option, StringComparison.InvariantCultureIgnoreCase) == true)
					{
						itb_temp.State = 1;
						SelectedOption_ChargeModel = charge_model_option;
						charge_model_selected = true;
					}
					else
					{
						itb_temp.State = 0;
					}
				}

				itb_temp.Size = new Size(option_buttonWidth_CM, 25);
				itb_temp.Location = new Point(posx, posy);
				itb_temp.Font = Font_Body;
				itb_temp.Tag = charge_model_option;
				itb_temp.ButtonPressed += new ImageButton.ImageButtonEventArgsHandler(itb_IAAS_ChargeModel_Button_ButtonPressed);
				itb_temp.DoubleClick += new EventHandler(itb_IAAS_ChargeModel_Button_DoubleClick);
				pnl_Options.Controls.Add(itb_temp);
				IAAS_Aspect_ChargeModel_Buttons.Add("ITB CM " + CONVERT.ToStr(ctl_count) + display_name, itb_temp);
				posx += (itb_temp.Width + gapx);

				if (itb_cm_first_option == null)
				{
					itb_cm_first_option = itb_temp;
				}
				ctl_count++;
			}

			if (charge_model_selected == false)
			{
				itb_cm_first_option.State = 1;
				SelectedOption_ChargeModel = (string)(itb_cm_first_option.Tag);
			}

			posx = posx_clear_button;
			if (ShowClearButtons)
			{
				ImageTextButton itb_IAAS_Aspect_ChargeModel_Button_Clear = new ImageTextButton("images/buttons/button_65x25.png");
				itb_IAAS_Aspect_ChargeModel_Button_Clear.Size = new Size(50, 25);
				itb_IAAS_Aspect_ChargeModel_Button_Clear.Location = new Point(posx, posy);
				itb_IAAS_Aspect_ChargeModel_Button_Clear.Tag = null;
				itb_IAAS_Aspect_ChargeModel_Button_Clear.SetButtonText("Clear", Color.White, Color.White, Color.White, Color.DimGray);
				itb_IAAS_Aspect_ChargeModel_Button_Clear.ButtonPressed += new ImageButton.ImageButtonEventArgsHandler(itb_IAAS_ChargeModel_Button_Clear_ButtonPressed);
				itb_IAAS_Aspect_ChargeModel_Button_Clear.DoubleClick += new EventHandler(itb_IAAS_ChargeModel_Button_Clear_DoubleClick);
				pnl_Options.Controls.Add(itb_IAAS_Aspect_ChargeModel_Button_Clear);
				IAAS_OtherCtrls.Add("privateBtn_CM" + CONVERT.ToStr(ctl_count), itb_IAAS_Aspect_ChargeModel_Button_Clear);
				posx += (itb_IAAS_Aspect_ChargeModel_Button_Clear.Width + gapx);
			}

			if (showSelectedOptionsText)
			{
				lblSelect_IAAS_Aspect_ChargeModel_Selected = new Label();
				lblSelect_IAAS_Aspect_ChargeModel_Selected.ForeColor = TitleForeColour;
				lblSelect_IAAS_Aspect_ChargeModel_Selected.Location = new Point(posx, posy);
				lblSelect_IAAS_Aspect_ChargeModel_Selected.Size = new Size(100, 30);
				lblSelect_IAAS_Aspect_ChargeModel_Selected.Text = OPTION_NONE_SELECT;
				lblSelect_IAAS_Aspect_ChargeModel_Selected.Font = Font_Body;
				lblSelect_IAAS_Aspect_ChargeModel_Selected.Visible = true;
				pnl_Options.Controls.Add(lblSelect_IAAS_Aspect_ChargeModel_Selected);
				IAAS_OtherCtrls.Add("label5", lblSelect_IAAS_Aspect_ChargeModel_Selected);
				posx += lblSelect_IAAS_Aspect_ChargeModel_Selected.Width + 10;
			}

			posx = startx;
			posy += 35;
			//=============================================================
			//==Vendor=====================================================
			//=============================================================
			lblSelect_IAAS_Aspect_Vendor_Title = new Label();
			lblSelect_IAAS_Aspect_Vendor_Title.ForeColor = TitleForeColour;
			lblSelect_IAAS_Aspect_Vendor_Title.Location = new Point(posx, posy);
			lblSelect_IAAS_Aspect_Vendor_Title.Size = new Size(120, 30);
			lblSelect_IAAS_Aspect_Vendor_Title.Text = "Vendor";
			lblSelect_IAAS_Aspect_Vendor_Title.Font = Font_SubTitle;
			lblSelect_IAAS_Aspect_Vendor_Title.Visible = true;
			pnl_Options.Controls.Add(lblSelect_IAAS_Aspect_Vendor_Title);
			IAAS_OtherCtrls.Add("label6", lblSelect_IAAS_Aspect_Vendor_Title);
			posx += lblSelect_IAAS_Aspect_Vendor_Title.Width + 0;

			bool saas_flag = false; // as we are in the IAAS section  
			Node LastV_Node = null;
			int v_count = 0;

			//data used to select the existing one or the correct one 
			string existing_vendor = selectedServiceNode.GetAttribute("cloud_provider");
			ImageToggleButton itb_vdr_first_option = null;
			bool vendor_selected = false;

			foreach (Node cloudProvider in orderPlanner.GetSuitableCloudProviders(selectedServiceNode, saas_flag))
			{
				string display_name = cloudProvider.GetAttribute("desc");
				bool vendor_active = cloudProvider.GetBooleanAttribute("available",false);

				LastV_Node = cloudProvider;

				ImageToggleButton itb_temp = new ImageToggleButton(0, "images/buttons/button_65x25_on.png", "images/buttons/button_65x25_active.png", display_name, display_name);
				itb_temp.Name = "ITB V " + display_name;
				itb_temp.setTextForeColor(Color.White);
				itb_temp.Size = new Size(option_buttonWidth_V, 25);
				itb_temp.Location = new Point(posx, posy);
				itb_temp.Font = Font_Body;
				itb_temp.Tag = cloudProvider;

				if ((selectedServiceNode.GetAttribute("cloud_provider") == cloudProvider.GetAttribute("name")))
				{
					itb_temp.State = 1;
					SelectedOption_Vendor = cloudProvider.GetAttribute("desc");
					SelectedOption_Vendor_Node = cloudProvider;
					vendor_selected = true;
				}
				else
				{
					itb_temp.State = 0;
				}

				if ((itb_vdr_first_option == null) && (vendor_active))
				{
					itb_vdr_first_option = itb_temp;
				}
				itb_temp.ButtonPressed += new ImageButton.ImageButtonEventArgsHandler(itb_IAAS_Vendor_ButtonPressed);
				itb_temp.DoubleClick += new EventHandler(itb_IAAS_Vendor_Button_DoubleClick);
				pnl_Options.Controls.Add(itb_temp);
				IAAS_Aspect_Vendor_Buttons.Add("ITB V " + CONVERT.ToStr(ctl_count) + display_name, itb_temp);
				posx += (itb_temp.Width + gapx);

				itb_temp.Refresh();

				ctl_count++;
				v_count++;
			}

			if (vendor_selected == false)
			{
				itb_vdr_first_option.State = 1;
				Node tmpNode = (Node)itb_vdr_first_option.Tag;
				SelectedOption_Vendor = tmpNode.GetAttribute("desc");
				SelectedOption_Vendor_Node = tmpNode;
			}

			posx = posx_clear_button;
			if (ShowClearButtons)
			{
				ImageTextButton itb_IAAS_Aspect_Vendor_Button_Clear = new ImageTextButton("images/buttons/button_65x25.png");
				itb_IAAS_Aspect_Vendor_Button_Clear.Size = new Size(50, 25);
				itb_IAAS_Aspect_Vendor_Button_Clear.Location = new Point(posx, posy);
				itb_IAAS_Aspect_Vendor_Button_Clear.Tag = null;
				itb_IAAS_Aspect_Vendor_Button_Clear.SetButtonText("Clear", Color.White, Color.White, Color.White, Color.DimGray);
				itb_IAAS_Aspect_Vendor_Button_Clear.ButtonPressed += new ImageButton.ImageButtonEventArgsHandler(itb_IAAS_Vendor_Button_Clear_ButtonPressed);
				itb_IAAS_Aspect_Vendor_Button_Clear.DoubleClick += new EventHandler(itb_IAAS_Vendor_Button_Clear_DoubleClick);
				pnl_Options.Controls.Add(itb_IAAS_Aspect_Vendor_Button_Clear);
				IAAS_OtherCtrls.Add("privateBtn_V" + CONVERT.ToStr(ctl_count), itb_IAAS_Aspect_Vendor_Button_Clear);
				posx += (itb_IAAS_Aspect_Vendor_Button_Clear.Width + gapx);
			}

			if (showSelectedOptionsText)
			{
				lblSelect_IAAS_Aspect_Vendor_Selected = new Label();
				lblSelect_IAAS_Aspect_Vendor_Selected.ForeColor = TitleForeColour;
				lblSelect_IAAS_Aspect_Vendor_Selected.Location = new Point(posx, posy);
				lblSelect_IAAS_Aspect_Vendor_Selected.Size = new Size(100, 30);
				lblSelect_IAAS_Aspect_Vendor_Selected.Text = OPTION_NONE_SELECT;
				lblSelect_IAAS_Aspect_Vendor_Selected.Font = Font_Body;
				lblSelect_IAAS_Aspect_Vendor_Selected.Visible = true;
				pnl_Options.Controls.Add(lblSelect_IAAS_Aspect_Vendor_Selected);
				IAAS_OtherCtrls.Add("label7", lblSelect_IAAS_Aspect_Vendor_Selected);
				posx += lblSelect_IAAS_Aspect_Vendor_Selected.Width + 10;
			}

			if (ShowFeedBack)
			{
				lblSelect_IAAS_FeedbackTitle = new Label();
				lblSelect_IAAS_FeedbackTitle.ForeColor = TitleForeColour;
				lblSelect_IAAS_FeedbackTitle.Location = new Point(pnl_Options.Width - FeedBackWidth - 10, 5);
				lblSelect_IAAS_FeedbackTitle.Size = new Size(220, 25);
				lblSelect_IAAS_FeedbackTitle.Text = "Feedback";
				lblSelect_IAAS_FeedbackTitle.Font = Font_BodyBold;
				lblSelect_IAAS_FeedbackTitle.Visible = true;
				pnl_Options.Controls.Add(lblSelect_IAAS_FeedbackTitle);
				IAAS_OtherCtrls.Add("label1FT", lblSelect_IAAS_FeedbackTitle);

				tb_Select_IAAS_FeedbackValue = new TextBox();
				tb_Select_IAAS_FeedbackValue.Multiline = true;
				tb_Select_IAAS_FeedbackValue.ReadOnly = true;
				tb_Select_IAAS_FeedbackValue.Visible = true;
				tb_Select_IAAS_FeedbackValue.Size = new Size(FeedBackWidth, pnl_Options.Height - 40);
				tb_Select_IAAS_FeedbackValue.Location = new Point(pnl_Options.Width - FeedBackWidth - 10, 30);
				tb_Select_IAAS_FeedbackValue.ForeColor = Color.White;
				tb_Select_IAAS_FeedbackValue.BackColor = Color.FromArgb(32, 32, 32);
				tb_Select_IAAS_FeedbackValue.BorderStyle = BorderStyle.FixedSingle;
				pnl_Options.Controls.Add(tb_Select_IAAS_FeedbackValue);
				IAAS_OtherCtrls.Add("label1FV", tb_Select_IAAS_FeedbackValue);

				tb_Selected_Feedback_handle = tb_Select_IAAS_FeedbackValue;
			}
			Reset_Options_Back();
		}

		protected void Handle_IAAS_BuildType_Clear_Button()
		{
			if (showSelectedOptionsText)
			{
				lblSelect_IAAS_Aspect_BuildType_Selected.Text = OPTION_NONE_SELECT;
			}
			foreach (ImageToggleButton itb in IAAS_Aspect_BuildType_Buttons.Values)
			{
				itb.State = 0;
			}
			SelectedOption_BuildType = "";
		}

		protected void itb_IAAS_BuildType_Button_Clear_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			Handle_IAAS_BuildType_Clear_Button();
		}
		protected void itb_IAAS_BuildType_Button_Clear_DoubleClick(object sender, EventArgs e)
		{
			Handle_IAAS_BuildType_Clear_Button();
		}

		protected void Handle_IAAS_BuildType_Button(object sender)
		{
			ImageToggleButton itb_sender = (ImageToggleButton)sender;
			if (itb_sender != null)
			{
				itb_sender.State = 1 - itb_sender.State;
				itb_sender.Refresh();

				string item = (string)itb_sender.Tag;
				if (showSelectedOptionsText)
				{
					lblSelect_IAAS_Aspect_BuildType_Selected.Text = item;
				}
				SelectedOption_BuildType = item;
			}

			foreach (ImageToggleButton itb in IAAS_Aspect_BuildType_Buttons.Values)
			{
				if (itb != itb_sender)
				{
					itb.State = 0;
				}
			}

			bool noControlSelected = true;
			foreach (ImageToggleButton itb in IAAS_Aspect_BuildType_Buttons.Values)
			{
				if (itb.State == 1)
				{
					noControlSelected = false;
				}
			}
			if (noControlSelected)
			{
				if (showSelectedOptionsText)
				{
					lblSelect_IAAS_Aspect_BuildType_Selected.Text = OPTION_NONE_SELECT;
				}
				SelectedOption_BuildType = "";
			}
		}

		protected void itb_IAAS_BuildType_Button_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			Handle_IAAS_BuildType_Button(sender);
		}
		protected void itb_IAAS_BuildType_Button_DoubleClick(object sender, EventArgs e)
		{
			Handle_IAAS_BuildType_Button(sender);
		}

		protected void Handle_IAAS_ChargeModel_Clear_Button()
		{
			if (showSelectedOptionsText)
			{
				lblSelect_IAAS_Aspect_ChargeModel_Selected.Text = OPTION_NONE_SELECT;
			}
			foreach (ImageToggleButton itb in IAAS_Aspect_ChargeModel_Buttons.Values)
			{
				itb.State = 0;
			}
			SelectedOption_ChargeModel = "";
		}

		protected void itb_IAAS_ChargeModel_Button_Clear_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			Handle_IAAS_ChargeModel_Clear_Button();
		}
		protected void itb_IAAS_ChargeModel_Button_Clear_DoubleClick(object sender, EventArgs e)
		{
			Handle_IAAS_ChargeModel_Clear_Button();
		}

		protected void Handle_IAAS_ChargeModel_Button(object sender)
		{
			ImageToggleButton itb_sender = (ImageToggleButton)sender;
			if (itb_sender != null)
			{
				itb_sender.State = 1 - itb_sender.State;
				itb_sender.Refresh();

				string lookup = (string)(itb_sender.Tag);
				string display_name = (string)ChargeModel_Lookup[lookup];

				if (showSelectedOptionsText)
				{
					lblSelect_IAAS_Aspect_ChargeModel_Selected.Text = display_name;
				}
				SelectedOption_ChargeModel = lookup;
			}
			foreach (ImageToggleButton itb in IAAS_Aspect_ChargeModel_Buttons.Values)
			{
				if (itb != itb_sender)
				{
					itb.State = 0;
				}
			}

			bool noControlSelected = true;
			foreach (ImageToggleButton itb in IAAS_Aspect_ChargeModel_Buttons.Values)
			{
				if (itb.State == 1)
				{
					noControlSelected = false;
				}
			}
			if (noControlSelected)
			{
				if (showSelectedOptionsText)
				{
					lblSelect_IAAS_Aspect_ChargeModel_Selected.Text = OPTION_NONE_SELECT;
				}
				SelectedOption_ChargeModel = "";
			}
		}

		protected void itb_IAAS_ChargeModel_Button_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			Handle_IAAS_ChargeModel_Button(sender);
		}
		protected void itb_IAAS_ChargeModel_Button_DoubleClick(object sender, EventArgs e)
		{
			Handle_IAAS_ChargeModel_Button(sender);
		}

		protected void Handle_IAAS_Vendor_Clear_Button()
		{
			if (showSelectedOptionsText)
			{
				lblSelect_IAAS_Aspect_Vendor_Selected.Text = OPTION_NONE_SELECT;
			}
			foreach (ImageToggleButton itb in IAAS_Aspect_Vendor_Buttons.Values)
			{
				itb.State = 0;
			}
			SelectedOption_Vendor = "";
			SelectedOption_Vendor_Node = null;
		}

		protected void itb_IAAS_Vendor_Button_Clear_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			Handle_IAAS_Vendor_Clear_Button();
		}
		protected void itb_IAAS_Vendor_Button_Clear_DoubleClick(object sender, EventArgs e)
		{
			Handle_IAAS_Vendor_Clear_Button();
		}

		protected void Handle_IAAS_Vendor_Button(object sender)
		{
			ImageToggleButton itb_sender = (ImageToggleButton)sender;
			if (itb_sender != null)
			{
				itb_sender.State = 1 - itb_sender.State;
				itb_sender.Refresh();

				Node tmpNode = (Node)itb_sender.Tag;
				if (showSelectedOptionsText)
				{
					lblSelect_IAAS_Aspect_Vendor_Selected.Text = tmpNode.GetAttribute("desc");
				}
				SelectedOption_Vendor = tmpNode.GetAttribute("desc");
				SelectedOption_Vendor_Node = tmpNode;
			}

			foreach (ImageToggleButton itb in IAAS_Aspect_Vendor_Buttons.Values)
			{
				if (itb != itb_sender)
				{
					itb.State = 0;
				}
			}

			bool noControlSelected = true;
			foreach (ImageToggleButton itb in IAAS_Aspect_Vendor_Buttons.Values)
			{
				if (itb.State == 1)
				{
					noControlSelected = false;
				}
			}
			if (noControlSelected)
			{
				if (showSelectedOptionsText)
				{
					lblSelect_IAAS_Aspect_Vendor_Selected.Text = OPTION_NONE_SELECT;
				}
				SelectedOption_Vendor = "";
				SelectedOption_Vendor_Node = null;
			}
		}

		protected void itb_IAAS_Vendor_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			Handle_IAAS_Vendor_Button(sender);
		}
		protected void itb_IAAS_Vendor_Button_DoubleClick(object sender, EventArgs e)
		{
			Handle_IAAS_Vendor_Button(sender);
		}

		#endregion IAAS

		#region SAAS

		protected void Fill_SAAS_ChoiceControls()
		{
			int startx = 10;
			int starty = 35;
			int posx = startx;
			int posy = starty;
			int gapx = 5;
			int posx_clear_button = 520;
			int option_buttonWidth_V = 120;

			//Now clear any old controls 
			Clear_ChoiceControls();

			int effectiveWidth = pnl_Options.Width - 10;
			if (ShowFeedBack)
			{
				effectiveWidth = effectiveWidth - FeedBackWidth;
			}

			ok = new ImageTextButton(@"images\buttons\button_85x25.png");
			ok.SetAutoSize();
			ok.Location = new Point(effectiveWidth - ((ok.Width + 10) * 2), pnl_Options.Height - (ok.Height + 10));
			ok.SetButtonText("OK", Color.White, Color.White, Color.White, Color.DimGray);
			ok.ButtonPressed += new ImageButton.ImageButtonEventArgsHandler(ok_ButtonPressed);
			pnl_Options.Controls.Add(ok);

			cancel = new ImageTextButton(@"images\buttons\button_85x25.png");
			cancel.SetAutoSize();
			cancel.Location = new Point(effectiveWidth - ((cancel.Width + 10) * 1), pnl_Options.Height - (ok.Height + 10));
			cancel.SetButtonText("Cancel", Color.White, Color.White, Color.White, Color.DimGray);
			cancel.ButtonPressed += new ImageButton.ImageButtonEventArgsHandler(cancel_ButtonPressed);
			pnl_Options.Controls.Add(cancel);

			string display_service_Name = selectedServiceNode.GetAttribute("short_name");

			//Rebuild New Controls
			lblSelect_SAAS_Title_Main = new Label();
			lblSelect_SAAS_Title_Main.ForeColor = TitleForeColour;
			lblSelect_SAAS_Title_Main.Location = new Point(10, 5);
			lblSelect_SAAS_Title_Main.Size = new Size(480, 30);
			lblSelect_SAAS_Title_Main.Text = "Select Options for " + display_service_Name;
			lblSelect_SAAS_Title_Main.Font = Font_SubTitle;
			lblSelect_SAAS_Title_Main.Visible = true;
			pnl_Options.Controls.Add(lblSelect_SAAS_Title_Main);
			SAAS_OtherCtrls.Add("label1M", lblSelect_SAAS_Title_Main);

			//Rebuild New Controls
			if (showSelectedOptionsText)
			{
				lblSelect_SAAS_Title_Select = new Label();
				lblSelect_SAAS_Title_Select.ForeColor = TitleForeColour;
				lblSelect_SAAS_Title_Select.Location = new Point(posx_clear_button + 55, 5);
				lblSelect_SAAS_Title_Select.Size = new Size(100, 30);
				lblSelect_SAAS_Title_Select.Text = "Selected Option";
				lblSelect_SAAS_Title_Select.Font = Font_BodyBold;
				lblSelect_SAAS_Title_Select.Visible = true;
				pnl_Options.Controls.Add(lblSelect_SAAS_Title_Select);
				SAAS_OtherCtrls.Add("label1S", lblSelect_SAAS_Title_Select);
			}

			//=============================================================
			//==Vendor=====================================================
			//=============================================================
			lblSelect_SAAS_Aspect_Vendor_Title = new Label();
			lblSelect_SAAS_Aspect_Vendor_Title.ForeColor = TitleForeColour;
			lblSelect_SAAS_Aspect_Vendor_Title.Location = new Point(posx, posy);
			lblSelect_SAAS_Aspect_Vendor_Title.Size = new Size(120, 30);
			lblSelect_SAAS_Aspect_Vendor_Title.Text = "Vendor";
			lblSelect_SAAS_Aspect_Vendor_Title.Font = Font_SubTitle;
			lblSelect_SAAS_Aspect_Vendor_Title.Visible = true;
			pnl_Options.Controls.Add(lblSelect_SAAS_Aspect_Vendor_Title);
			SAAS_OtherCtrls.Add("label6", lblSelect_IAAS_Aspect_Vendor_Title);
			posx += lblSelect_SAAS_Aspect_Vendor_Title.Width + 0;

			int ctl_count = 0;
			bool saas_flag = true; // As we are in The SAAS section 

			//data used to select the existing one or the correct one 
			string existing_vendor = selectedServiceNode.GetAttribute("cloud_provider");
			ImageToggleButton itb_vdr_first_option = null;
			bool vendor_selected = false;

			foreach (Node cloudProvider in orderPlanner.GetSuitableCloudProviders(selectedServiceNode, saas_flag))
			{
				string display_name = cloudProvider.GetAttribute("desc");
				bool vendor_active = cloudProvider.GetBooleanAttribute("available", false);

				ImageToggleButton itb_temp = new ImageToggleButton(0, "images/buttons/button_65x25_on.png", "images/buttons/button_65x25_active.png", display_name, display_name);
				itb_temp.Name = "ITB V " + display_name;
				itb_temp.setTextForeColor(Color.White);
				itb_temp.Size = new Size(option_buttonWidth_V, 25);
				itb_temp.Location = new Point(posx, posy);
				itb_temp.Font = Font_Body;
				itb_temp.Tag = cloudProvider;

				if ((selectedServiceNode.GetAttribute("cloud_provider") == cloudProvider.GetAttribute("name")))
				{
					itb_temp.State = 1;
					SelectedOption_Vendor = cloudProvider.GetAttribute("desc");
					SelectedOption_Vendor_Node = cloudProvider;
					vendor_selected = true;
				}
				else
				{
					itb_temp.State = 0;
				}

				if ((itb_vdr_first_option == null) && (vendor_active))
				{
					itb_vdr_first_option = itb_temp;
				}
				
				itb_temp.ButtonPressed += new ImageButton.ImageButtonEventArgsHandler(itb_SAAS_Vendor_Button_ButtonPressed);
				itb_temp.DoubleClick += new EventHandler(itb_SAAS_Vendor_Button_DoubleClick);
				pnl_Options.Controls.Add(itb_temp);
				SAAS_Aspect_Vendor_Buttons.Add("ITB V " + CONVERT.ToStr(ctl_count) + display_name, itb_temp);
				posx += (itb_temp.Width + gapx);
				itb_temp.Refresh();
				ctl_count++;
			}

			if (vendor_selected == false)
			{
				itb_vdr_first_option.State = 1;
				Node tmpNode = (Node)itb_vdr_first_option.Tag;
				SelectedOption_Vendor = tmpNode.GetAttribute("desc");
				SelectedOption_Vendor_Node = tmpNode;
			}

			posx = posx_clear_button;
			if (ShowClearButtons)
			{
				ImageTextButton itb_SAAS_Aspect_Vendor_Button_Clear = new ImageTextButton("images/buttons/button_65x25.png");
				itb_SAAS_Aspect_Vendor_Button_Clear.Size = new Size(65, 25);
				itb_SAAS_Aspect_Vendor_Button_Clear.Location = new Point(posx, posy);
				itb_SAAS_Aspect_Vendor_Button_Clear.Tag = null;
				itb_SAAS_Aspect_Vendor_Button_Clear.SetButtonText("Clear", Color.White, Color.White, Color.White, Color.DimGray);
				itb_SAAS_Aspect_Vendor_Button_Clear.ButtonPressed += new ImageButton.ImageButtonEventArgsHandler(itb_SAAS_Vendor_Button_Clear_ButtonPressed);
				itb_SAAS_Aspect_Vendor_Button_Clear.DoubleClick += new EventHandler(itb_SAAS_Vendor_Button_Clear_DoubleClick);
				pnl_Options.Controls.Add(itb_SAAS_Aspect_Vendor_Button_Clear);
				SAAS_OtherCtrls.Add("privateBtn_V" + CONVERT.ToStr(ctl_count), itb_SAAS_Aspect_Vendor_Button_Clear);
				posx += (itb_SAAS_Aspect_Vendor_Button_Clear.Width + gapx);
			}

			if (showSelectedOptionsText)
			{
				lblSelect_SAAS_Aspect_Vendor_Selected = new Label();
				lblSelect_SAAS_Aspect_Vendor_Selected.ForeColor = TitleForeColour;
				lblSelect_SAAS_Aspect_Vendor_Selected.Location = new Point(posx, posy);
				lblSelect_SAAS_Aspect_Vendor_Selected.Size = new Size(100, 30);
				lblSelect_SAAS_Aspect_Vendor_Selected.Text = OPTION_NONE_SELECT;
				lblSelect_SAAS_Aspect_Vendor_Selected.Font = Font_Body;
				lblSelect_SAAS_Aspect_Vendor_Selected.Visible = true;
				pnl_Options.Controls.Add(lblSelect_SAAS_Aspect_Vendor_Selected);
				SAAS_OtherCtrls.Add("label7", lblSelect_SAAS_Aspect_Vendor_Selected);
				posx += lblSelect_SAAS_Aspect_Vendor_Selected.Width + 10;
			}

			if (ShowFeedBack)
			{
				int pt = pnl_Options.Width - FeedBackWidth - 10;

				lblSelect_SAAS_FeedbackTitle = new Label();
				lblSelect_SAAS_FeedbackTitle.ForeColor = TitleForeColour;
				lblSelect_SAAS_FeedbackTitle.Location = new Point(pt , 5);
				lblSelect_SAAS_FeedbackTitle.Size = new Size(220, 25);
				lblSelect_SAAS_FeedbackTitle.Text = "Feedback";
				lblSelect_SAAS_FeedbackTitle.Font = Font_BodyBold;
				lblSelect_SAAS_FeedbackTitle.Visible = true;
				pnl_Options.Controls.Add(lblSelect_SAAS_FeedbackTitle);
				SAAS_OtherCtrls.Add("label1FT", lblSelect_SAAS_FeedbackTitle);

				tb_Select_SAAS_FeedbackValue = new TextBox();
				tb_Select_SAAS_FeedbackValue.Multiline = true;
				tb_Select_SAAS_FeedbackValue.ReadOnly = true;
				tb_Select_SAAS_FeedbackValue.Visible = true;
				tb_Select_SAAS_FeedbackValue.Size = new Size(FeedBackWidth, pnl_Options.Height - 40);
				tb_Select_SAAS_FeedbackValue.Location = new Point(pt, 30);
				tb_Select_SAAS_FeedbackValue.ForeColor = Color.White;
				tb_Select_SAAS_FeedbackValue.BackColor = Color.FromArgb(32, 32, 32);
				tb_Select_SAAS_FeedbackValue.BorderStyle = BorderStyle.FixedSingle;
				pnl_Options.Controls.Add(tb_Select_SAAS_FeedbackValue);
				SAAS_OtherCtrls.Add("label1FV", tb_Select_SAAS_FeedbackValue);

				tb_Selected_Feedback_handle = tb_Select_SAAS_FeedbackValue;
			}
			Reset_Options_Back();
		}

		protected void Handle_SAAS_Vendor_Clear_Button()
		{
			if (showSelectedOptionsText)
			{
				lblSelect_SAAS_Aspect_Vendor_Selected.Text = OPTION_NONE_SELECT;
			}
			foreach (ImageToggleButton itb in SAAS_Aspect_Vendor_Buttons.Values)
			{
				itb.State = 0;
			}
			SelectedOption_Vendor = "";
			SelectedOption_Vendor_Node = null;
		}

		protected void itb_SAAS_Vendor_Button_Clear_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			Handle_SAAS_Vendor_Clear_Button();
		}
		protected void itb_SAAS_Vendor_Button_Clear_DoubleClick(object sender, EventArgs e)
		{
			Handle_SAAS_Vendor_Clear_Button();
		}

		protected void Handle_SAAS_Vendor_Button(object sender)
		{
			ImageToggleButton itb_sender = (ImageToggleButton)sender;
			if (itb_sender != null)
			{
				itb_sender.State = 1 - itb_sender.State;
				itb_sender.Refresh();

				Node tmpNode = (Node)itb_sender.Tag;
				if (showSelectedOptionsText)
				{
					lblSelect_SAAS_Aspect_Vendor_Selected.Text = tmpNode.GetAttribute("desc");
				}
				SelectedOption_Vendor = tmpNode.GetAttribute("desc");
				SelectedOption_Vendor_Node = tmpNode;
			}

			foreach (ImageToggleButton itb in SAAS_Aspect_Vendor_Buttons.Values)
			{
				if (itb != itb_sender)
				{
					itb.State = 0;
				}
			}

			bool noControlSelected = true;
			foreach (ImageToggleButton itb in SAAS_Aspect_Vendor_Buttons.Values)
			{
				if (itb.State == 1)
				{
					noControlSelected = false;
				}
			}
			if (noControlSelected)
			{
				if (showSelectedOptionsText)
				{
					lblSelect_SAAS_Aspect_Vendor_Selected.Text = OPTION_NONE_SELECT;
				}
				SelectedOption_Vendor = "";
				SelectedOption_Vendor_Node = null;
			}
		}

		protected void itb_SAAS_Vendor_Button_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			Handle_SAAS_Vendor_Button(sender);
		}
		protected void itb_SAAS_Vendor_Button_DoubleClick(object sender, EventArgs e)
		{
			Handle_SAAS_Vendor_Button(sender);
		}

		#endregion SAAS

		#endregion New Methods

		#region Main Methods

		protected void BuildMainControls()
		{
			lblPanelTitle = new Label();
			lblPanelTitle.Location = new Point(10, -4);
			lblPanelTitle.Size = new Size(400, 25);
			lblPanelTitle.ForeColor = Color.White;
			lblPanelTitle.BackColor = Color.Transparent;
			lblPanelTitle.Text = "Create New Cloud Request";
			lblPanelTitle.Font = Font_Title;
			lblPanelTitle.Visible = true;
			Controls.Add(lblPanelTitle);

			close = new ImageTextButton(@"images\buttons\button_85x25.png");
			close.SetAutoSize();
			close.SetButtonText("Close", Color.White, Color.White, Color.White, Color.DimGray);
			close.ButtonPressed += new ImageButton.ImageButtonEventArgsHandler(close_ButtonPressed);
			Controls.Add(close);
		}

		#endregion Main Methods

		protected void HandleSelection()
		{
		}

		protected string GetErrorFromPlans(Node plannedOrders)
		{
			StringBuilder builder = new StringBuilder();

			foreach (Node order in plannedOrders.getChildren())
			{
				string message = order.GetAttribute("message");

				if (!string.IsNullOrEmpty(message))
				{
					builder.AppendLine(message);
				}
			}
			return builder.ToString();
		}

		protected void TryDeployment()
		{
			plannedOrders.DeleteChildren();

			Node business = model.GetNamedNode(SelectedOption_DataCentreNode.GetAttribute("business"));
			Node selectedDeploymentLocationNode = SelectedOption_DataCentreNode;
			if (SelectedOption_DataCentreNode.GetAttribute("type") == "datacenter")
			{
				selectedDeploymentLocationNode = model.GetNamedNode(CONVERT.Format("{0} Production Cloud", business.GetAttribute("region")));
			}

			Node selectedCloudProviderNode = SelectedOption_Vendor_Node;

			if (orderPlanner.AddCloudDeploymentPlanToQueue(plannedOrders, selectedServiceNode, selectedDeploymentLocationNode,
																									 SelectedOption_BuildType, SelectedOption_ChargeModel, selectedCloudProviderNode))
			{
				while (plannedOrders.GetChildrenOfType("order").Count > 0)
				{
					model.MoveNode((Node)plannedOrders.GetChildrenOfType("order")[0], orderQueue);
					//OnClosed();
				}
				pnl_Options.Visible = false;
				setHighlightServiceLabel(null);
				Clear_ChoiceControls();

				rebuildServiceListDisplay();
			}
			else
			{
				//MessageBox.Show(GetErrorFromPlans(plannedOrders));
				tb_Selected_Feedback_handle.Text = GetErrorFromPlans(plannedOrders);
			}
		}

		protected void ok_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			switch (selected_DeploymentType)
			{
				case OPTION_PRIVATE:
					if (string.IsNullOrEmpty(SelectedOption_DataCentre) == false)
					{
						TryDeployment();
					}
					else
					{
						//MessageBox.Show("No Datacenter Selected", "Select Private Deployment Option");
						tb_Selected_Feedback_handle.Text = "No Exchange Selected";
					}
					break;
				case OPTION_IAAS:
					bool BT_Good = string.IsNullOrEmpty(SelectedOption_BuildType) == false;
					bool CM_Good = string.IsNullOrEmpty(SelectedOption_ChargeModel) == false;
					bool V_Good = string.IsNullOrEmpty(SelectedOption_Vendor) == false;

					bool BT_match_failure = false;

					//Need to check that the Correct Build type has been selected
					if (BT_Good == true)
					{
						string serviceBuildType = selectedServiceNode.GetAttribute("correct_build_code");
						if (serviceBuildType != SelectedOption_BuildType)
						{
							BT_match_failure = true;
							BT_Good = false;
						}
					}

					if ((BT_Good) & (CM_Good) & (V_Good))
					{
						TryDeployment();
					}
					else
					{
						string err_message = "";

						if (BT_match_failure)
						{
							err_message = "Invalid build type for selected service, please reselect";
						}
						else
						{
							err_message = "Missing fields, Please enter missing fields(";
							if (BT_Good == false)
							{
								err_message += "Build Type,";
							}
							if (CM_Good == false)
							{
								err_message += "Charge Model,";
							}
							if (V_Good == false)
							{
								err_message += "Vendor";
							}
							err_message += ")";
						}
						//MessageBox.Show(err_message, err_title);
						tb_Selected_Feedback_handle.Text = err_message;
					}

					break;
				case OPTION_SAAS:
					if (string.IsNullOrEmpty(SelectedOption_Vendor) == false)
					{
						TryDeployment();
					}
					else
					{
						//MessageBox.Show("No Vendor Selected", "Select SAAS Deployment Option");
						tb_Selected_Feedback_handle.Text = "No Vendor Selected "+" Select SAAS Deployment Option";
					}
					break;
				default:
					//MessageBox.Show("No Item Selected", "No Item Selected");
					tb_Selected_Feedback_handle.Text = "No Item Selected"; 
					break;
			}
		}

		protected void cancel_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			//OnClosed();
			SuspendLayout();
			pnl_Options.Visible = false;
			pnl_Services.Visible = false;
			setHighlightServiceLabel(null);
			Clear_ChoiceControls();
			rebuildServiceListDisplay();
			pnl_Services.Visible = true;
			ResumeLayout();
		}

		protected void close_ButtonPressed(object sender, ImageButtonEventArgs args)
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
			close.Location = new Point(Width - (close.Width + 10), Height - (close.Height + 8));
		}

		public override Size getPreferredSize()
		{
			return new Size(1020, 685);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			e.Graphics.FillRectangle(Brushes.Silver, 0, 0, Width, Height);
			if (ControlBackgroundImage != null)
			{
				e.Graphics.DrawImage(ControlBackgroundImage, 0, 0, ControlBackgroundImage.Width, ControlBackgroundImage.Height);
			}
		}

		public override bool IsFullScreen => true;
	}
}