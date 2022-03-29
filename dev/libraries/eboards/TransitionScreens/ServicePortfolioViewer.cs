using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;

using LibCore;
using Network;
using CoreUtils;
using CommonGUI;

namespace TransitionScreens
{
	/// <summary>
	/// Summary description for ServicePortfolioViewer.
	/// </summary>
	public class ServicePortfolioViewer : FlickerFreePanel
	{
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		protected System.ComponentModel.Container components = null;

		//images and background graphic
		protected Image csd = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\Images\\panels\\csd.png");
		protected Image t_csd = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\Images\\panels\\t_csd.png");
		protected Color MyCommonBackColor;
		protected int columns = 4;
		protected int column_seperation = 2;	//The horizontal gap between columns
		protected int row_seperation = 2;			//The vertical gap between rows
		protected int DisplayOffsetX = 15;		//The origin X for the layout of service controls 
		protected int DisplayOffsetY = SkinningDefs.TheInstance.GetIntData("service_portfolio_viewer_yOffset",30);		//The origin Y for the layout of service controls 

		//Service Monitoring Variables
		protected NodeTree _tree;											//The model tree
		protected Node ActiveBusinessServicesGroup;		//the Root node for all Active Services
		protected Node RetiredBusinessServicesGroup;	//the Root node for all Retired Services

		protected ArrayList ExistingFunctionNames = new ArrayList();
		protected Hashtable ExistingSMIs = new Hashtable();
		protected Hashtable RetiredSMIs = new Hashtable();
		protected Boolean showRetiredServices = false;
		protected ImageToggleButton toggleView;
		protected string strbuttonImage_ShowRetired = "/images/buttons/show_retired.png";
		protected string strbuttonImage_ShowCatalog = "/images/buttons/show_catalog.png";
		protected Hashtable UpgradeNodeMonitors = new Hashtable();
		protected Hashtable RetiredNodeNodeHandlers = new Hashtable();
		protected Boolean IsPopUpOpsWindowDisplayed = false;
		protected ArrayList HiddenSMI = new ArrayList();

		protected int catalog_button_width = 105;
		protected int catalog_button_height = 26;
		protected int smi_width = 132;
		protected int smi_height = 31;
		protected string panelTitle = "Service Portfolio - Service Catalog";
		protected bool auto_translate = true;
		protected bool SelfDrawTranslatedTitle = false;
		protected Font titleFont = null;

		public Brush text_brush = new SolidBrush (SkinningDefs.TheInstance.GetColorDataGivenDefault("transition_title_colour", SkinningDefs.TheInstance.GetColorDataGivenDefault("transition_text_colour", Color.DarkBlue)));

		public void SetCatalogButtonSizeAndPosition(int x, int y, int w, int h)
		{
			catalog_button_width = w;
			catalog_button_height = h;
			if(toggleView != null)
			{
				toggleView.Size = new Size(catalog_button_width, catalog_button_height);
				toggleView.Location = new Point(x,y);
			}
		}

		public void EnableSelfDrawTitle(bool newState)
		{
			SelfDrawTranslatedTitle = newState;
		}

		public void SetCatalogButtonSize(int w, int h)
		{
			catalog_button_width = w;
			catalog_button_height = h;
			if(toggleView != null)
			{
				toggleView.Size = new Size(catalog_button_width, catalog_button_height);
			}
		}
	
		#region Constructor and Dispose
		/// <summary>
		/// Constructor for class
		/// </summary>
		/// <param name="tree"></param>
		/// <param name="CommonBackColor"></param>
		public ServicePortfolioViewer(NodeTree tree, Color CommonBackColor)
		{
			//presentation stuff
			Name = "ServicePortfolioViewer";
			Size = new Size(561, 285);
			BackColor = Color.Transparent;
			BackgroundImage = csd;
			MyCommonBackColor = CommonBackColor;

			string fontname =  SkinningDefs.TheInstance.GetData("fontname");
		    float size = (float) SkinningDefs.TheInstance.GetDoubleData("transition_title_font_size", 12);
            titleFont = ConstantSizeFont.NewFont(fontname, size);
			if (auto_translate)
			{
				titleFont.Dispose();
                titleFont = ConstantSizeFont.NewFont(TextTranslator.TheInstance.GetTranslateFont(fontname), size, FontStyle.Bold);
			}

			BuildToggleButton();

			//Connect up to the tree and Group Node
			_tree = tree;

			//Connect up to the Business service Group and watch for New Services being Added
			ActiveBusinessServicesGroup = _tree.GetNamedNode("Business Services Group");
			ActiveBusinessServicesGroup.ChildAdded += businessServicesGroup_ChildAdded;
			ActiveBusinessServicesGroup.ChildRemoved += businessServicesGroup_ChildRemoved;

			//Connect up to the Retired Business Services Node and watch for New Retired Services being Added
			RetiredBusinessServicesGroup = _tree.GetNamedNode("Retired Business Services");
			RetiredBusinessServicesGroup.ChildAdded +=RetiredBusinessServicesGroup_ChildAdded;
		}

		public virtual void BuildToggleButton ()
		{
			if (SkinningDefs.TheInstance.GetIntData("transition_use_code_drawn_catalog_button", 0) == 1)
			{
				toggleView = new ImageToggleButton(0, "\\images\\buttons\\show_toggle", "\\images\\buttons\\show_toggle","Show Retired", "Show Catalog");
			}
			else
			{
				toggleView = new ImageToggleButton(0, "\\images\\buttons\\show_retired", "\\images\\buttons\\show_catalog");
			}
			toggleView.Size = new Size(catalog_button_width, catalog_button_height);

		    if (SkinningDefs.TheInstance.GetBoolData("transition_auto_size_buttons", false))
		    {
                toggleView.SetAutoSize();
		    }

		    toggleView.Location = new Point(51+390,0);
			toggleView.SetTransparent();
			toggleView.ButtonPressed += toggleView_ButtonPressed;
			toggleView.Name = "Toggle Retired Service Portfolio View";
			Controls.Add(toggleView);
		}

		public void SetButtonsTop(int top)
		{
		}

		protected void disposeSMIs(Hashtable ht)
		{
			ArrayList killList2 = new ArrayList();
			foreach(ServiceMonitorItem m in ht.Values)
			{
				killList2.Add(m);
			}
			foreach(ServiceMonitorItem m in killList2)
			{
				Controls.Remove(m);
				m.Dispose();
			}		
			ht.Clear();
		}


		protected void disposeRetiredNodeHandlers(Hashtable ht)
		{
			ArrayList killList2 = new ArrayList();
			foreach(Node m in ht.Values)
			{
				killList2.Add(m);
			}
			foreach(Node m in killList2)
			{
				m.AttributesChanged -=RetiredNode_AttributesChanged;
			}		
			ht.Clear();
		}

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				//need to iterate through and kill off the upgrade Monitors
				ArrayList killList = new ArrayList();
				foreach(Node n in UpgradeNodeMonitors.Values)
				{
					killList.Add(n);
				}
				foreach(Node MonitorUpgradeNode in killList)
				{
					MonitorUpgradeNode.ChildAdded -=MonitorUpgradeNode_ChildAdded;
				}		
				UpgradeNodeMonitors.Clear();

				//need to iterate through and kill off the smi controls
				disposeSMIs(ExistingSMIs);
				disposeSMIs(RetiredSMIs);
				disposeRetiredNodeHandlers(RetiredNodeNodeHandlers);

				//disconnect from Monitored Node
				ActiveBusinessServicesGroup.ChildAdded -= businessServicesGroup_ChildAdded;
				ActiveBusinessServicesGroup.ChildRemoved -= businessServicesGroup_ChildRemoved;
				RetiredBusinessServicesGroup.ChildAdded -=RetiredBusinessServicesGroup_ChildAdded;
				//Closing out

				if (titleFont != null)
				{
					titleFont.Dispose();
				}

				foreach(Control c in Controls)
				{
					if(c != null) c.Dispose();
				}
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#endregion Constructor and Dispose

		#region Utils

		public void SetNewSMISize(int new_smi_width, int new_smi_height)
		{
			smi_width = new_smi_width;
			smi_height = new_smi_height;
		}

		public void SetNewColumns(int new_columns)
		{
			columns = new_columns;
			DoLayoutBoth();
		}

		public void SetNewLayoutOffsetsAndSeperations(int newDisplayOffsetX, int newDisplayOffsetY,
			int new_columns, int new_column_seperation, int new_row_seperation)
		{
			DisplayOffsetX = newDisplayOffsetX;
			DisplayOffsetY = newDisplayOffsetY;
			columns = new_columns;
			column_seperation = new_column_seperation;
			row_seperation = new_row_seperation;
			DoLayoutBoth();
		}

		public void SetNewSMIVisibility(Boolean IsVisible)
		{
			IsPopUpOpsWindowDisplayed = IsVisible;
			//System.Diagnostics.Debug.WriteLine("##### VISIBLE" + IsVisible);

			if (IsPopUpOpsWindowDisplayed == false)
			{
				if (HiddenSMI.Count >0)
				{
					foreach (ServiceMonitorItem smi in HiddenSMI)
					{
						smi.Visible = true;
					}
					HiddenSMI.Clear();
				}
			}
		}

		/// <summary>
		/// Whether we show the Training game background
		/// </summary>
		/// <param name="showRetiredFlag"></param>
		public void SetTrainingMode(Boolean Tr)
		{
			if (Tr)
			{
				BackgroundImage = t_csd;
			}
			else
			{
				BackgroundImage = csd;
			}
		}

		#endregion Utils

		#region Handling Retired Services 

		protected void RetiredNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			//no longer interested in the Retired node changes 
		}

		protected virtual ServiceMonitorItem CreateServiceMonitorItem(NodeTree tree, Node n, string functional_name)
		{
			return new ServiceMonitorItem(tree, n, functional_name);
		}

		protected void AddRetiredService(Node RetiredNode, Boolean IsDynamicAddition) 
		{
			if (RetiredNode != null)
			{
				//Connect up the Retired node to the Service Monitor Item
				string function_name = RetiredNode.GetAttribute("biz_service_function");
				string node_name = RetiredNode.GetAttribute("name");
				ServiceMonitorItem smi = CreateServiceMonitorItem(_tree, RetiredNode, function_name);
				//smi.StatusChanged +=new CommonGUI.ServiceMonitorItem.StatusChangedHandler(smi_StatusChanged);
				smi.Size = new Size(smi_width,smi_height);
				smi.BackColor = MyCommonBackColor;
				smi.BringToFront();
				smi.SetRetiredBackground(true);
				if (showRetiredServices == true)
				{
					smi.Visible = true;
				}
				else
				{
					smi.Visible = false;
				}
				Controls.Add(smi);
				RetiredSMIs.Add(node_name, smi);
			}
		}

		protected void BuildRetiredServices()
		{
			if (RetiredBusinessServicesGroup != null)
			{
				foreach (Node RetiredNode in RetiredBusinessServicesGroup.getChildren())
				{
					string function_name = RetiredNode.GetAttribute("biz_service_function");
					string node_name = RetiredNode.GetAttribute("name");
					string node_type = RetiredNode.GetAttribute("type");
					if (RetiredNodeNodeHandlers.ContainsKey(node_name)==false)
					{
						AddRetiredService(RetiredNode, false); 
						RetiredNodeNodeHandlers.Add(node_name, RetiredNode);
						RetiredNode.AttributesChanged +=RetiredNode_AttributesChanged;
					}
				}
			}
		}

		protected void RetiredBusinessServicesGroup_ChildAdded(Node sender, Node child)
		{
			string function_name = child.GetAttribute("biz_service_function");
			string node_name = child.GetAttribute("name");
			if (RetiredNodeNodeHandlers.ContainsKey(node_name)==false)
			{
				AddRetiredService(child, true); 
				RetiredNodeNodeHandlers.Add(node_name, child);
				child.AttributesChanged +=RetiredNode_AttributesChanged;
				DoLayoutRetired(true);
			}
		}

		#endregion Handling Retired Services 

		#region Handling Active Services 

		protected void AddService(string function_name, Node tmpBusinessService, string serviceName)
		{
			//Now there is only one active BS with a funational name 	
			if (ExistingFunctionNames.Contains(function_name)==false)
			{
				//add to local List of functional Names
				ExistingFunctionNames.Add(function_name);
				//add the control 
				SuspendLayout();
				ServiceMonitorItem smi = CreateServiceMonitorItem(_tree, tmpBusinessService, function_name);

				smi.Name = "Portfolio Service Monitored Label";

				if (IsPopUpOpsWindowDisplayed)
				{
					smi.Visible = false;
					HiddenSMI.Add(smi);
				}

				//smi.StatusChanged +=new CommonGUI.ServiceMonitorItem.StatusChangedHandler(smi_StatusChanged);
				smi.Size = new Size(smi_width,smi_height);
				smi.BackColor = MyCommonBackColor;

				// : fix for 4203 (newly-added services are initially visible even if
				// you are viewing retired services).
				if (showRetiredServices)
				{
					smi.Visible = false;
				}

				Controls.Add(smi);
				smi.BringToFront();

				ResumeLayout(false);

				//determine whether we have an update for the 

				//smi.SetUpdateAvailable(false);
				ExistingSMIs.Add(function_name, smi);
				//System.Diagnostics.Debug.WriteLine("==Created SMI "+ serviceName);
				//smi.StatusChanged+=new CommonGUI.ServiceMonitorItem.StatusChangedHandler(smi_StatusChanged);
			}
		}

		protected void BuildActiveServices()
		{
			//build up a list of functional names 
			ArrayList bsg_kids = ActiveBusinessServicesGroup.getChildren();
			foreach(Node tmpNode in bsg_kids)
			{
				string node_type = tmpNode.GetAttribute("type");
				string function_name = tmpNode.GetAttribute("biz_service_function");
				string name = tmpNode.GetAttribute("name");

				//in future there might other things in here, we only want "biz_service"
				if (node_type.ToLower() == "biz_service")
				{
					AddService(function_name, tmpNode , name);
				}				
			}		
		}

		#endregion Handling Active Services 

		#region Show Method
		
		/// <summary>
		/// Whether we show retired or active services
		/// As each control contains both Active and Retired Services and they can be in different places 
		/// We need to tell the controls to present usint the correct internal information
		/// </summary>
		/// <param name="showRetiredFlag"></param>
		public void SetShowRetiredServices(Boolean showRetiredFlag)
		{
			showRetiredServices = showRetiredFlag;

			if (showRetiredFlag)
			{
				toggleView.State = 1;
			}
			else
			{
				toggleView.State = 0;
			}

			if (showRetiredServices)
			{
				//iterate over the Active making then Visible
				foreach (ServiceMonitorItem m in ExistingSMIs.Values)
				{
					//m.SwapToActiveLocation();
					m.Visible = false;
				}
				//iterate over the Retired SMI making then Visible
				foreach (ServiceMonitorItem m in RetiredSMIs.Values)
				{
					//m.SwapToActiveLocation();
					m.Visible = true;
				}
			}
			else
			{
				//iterate over the Retired SMI making then InVisible
				foreach (ServiceMonitorItem m in RetiredSMIs.Values)
				{
					//m.SwapToActiveLocation();
					m.Visible = false;
				}
				//iterate over the Active making then Visible
				foreach (ServiceMonitorItem m in ExistingSMIs.Values)
				{
					//m.SwapToActiveLocation();
					m.Visible = true;
				}
			}

//			foreach(ServiceMonitorItem m in ExistingSMIs.Values)
//			{
//				//we need to show using the retired information 
//				if (showRetiredFlag)
//				{
//					//Only need if the Control has a retired service
//					if (m.ContainsRetiredNode())
//					{
//						m.SwapToRetiredLocation();
//						m.SwapToRetiredText();
//						m.SwapToRetiredBackground();
//						m.Visible = true;
//						m.Refresh();
//					}
//					else
//					{
//						//No Retired, no need to show
//						m.Visible = false;
//					}
//				}
//				else
//				{
//					//Show the Active Information 
//					m.SwapToActiveLocation();
//					m.SwapToActiveText();
//					m.SwapToActiveBackground();
//					m.Visible = true;
//					m.Refresh();
//				}
//			}
		}

		#endregion Show Method

		/// <summary>
		/// Scan through the Business Servivc group and extract the Services 
		/// </summary>
		public void LoadData()
		{
			//Always Consider the active before the retired.
			//we don't support the full retirement of a functional service
			//only it's replacement by an upgrade
			//so there is always a functional name for all services (both active and retired)
			BuildActiveServices();
			BuildRetiredServices();
			DoLayoutBoth();
		}

		protected void AddUpgradeMonitor(Node BusinessService)
		{
			if (BusinessService != null)
			{
				string nodename = BusinessService.GetAttribute("name");
				if (UpgradeNodeMonitors.ContainsKey(nodename)==false)
				{
					foreach (Node n in BusinessService.getChildren())
					{
						string nodetype = n.GetAttribute("type");
						if (nodetype.ToLower() == "upgrades")
						{
							Node MonitorUpgradeNode = n;
							MonitorUpgradeNode.ChildAdded +=MonitorUpgradeNode_ChildAdded;
							UpgradeNodeMonitors.Add(nodename,MonitorUpgradeNode);
						}
					}
				}
			}
		}

		/// <summary>
		/// A helper method that is called to rebuild everthing 
		/// </summary>
		public void ReBuildAll()
		{
			LoadData();
		}

		class CompareBizServiceNamesByDescriptions : IComparer
		{
			Hashtable nameToServiceMonitorItem;

			public CompareBizServiceNamesByDescriptions (Hashtable nameToServiceMonitorItem)
			{
				this.nameToServiceMonitorItem = nameToServiceMonitorItem;
			}

			public int Compare (object x, object y)
			{
				ServiceMonitorItem itemX = ((ServiceMonitorItem) nameToServiceMonitorItem[(string) x]);
				ServiceMonitorItem itemY = ((ServiceMonitorItem) nameToServiceMonitorItem[(string) y]);

				return itemX.getMonitoredItem().GetAttribute("desc").CompareTo(itemY.getMonitoredItem().GetAttribute("desc"));
			}
		}

		/// <summary>
		/// Layout the controls in a Grid according to the insertion order of the functional name
		/// Uses the Display Offset Location to start with and the numbers of formatted columns 
		/// </summary>
		/// <param name="IsActive"></param>
		protected void DoLayoutNew(Boolean IsActive)
		{
			//Need to know how many per column
			double numPerColumn = ((double)ExistingSMIs.Values.Count) / ((double)columns);
			numPerColumn = Math.Ceiling(numPerColumn);
			//
			int xoffset = DisplayOffsetX;
			int yoffset = DisplayOffsetY;
			int rowCount = 0;
			Point npt = new Point(0,0);

			ExistingFunctionNames.Sort(new CompareBizServiceNamesByDescriptions (ExistingSMIs));
			
			//Iterate through using the insertion of the functional names
			foreach(string functionalname in ExistingFunctionNames)
			{
				if (ExistingFunctionNames.Contains(functionalname))
				{
					ServiceMonitorItem smi = (ServiceMonitorItem) ExistingSMIs[functionalname];

					Boolean IncludeThisItem = true;
					//layout the required Item 
					if (IncludeThisItem)
					{
						string s = smi.getFunctionName();
						npt.X = xoffset;
						npt.Y = yoffset;
						//System.Diagnostics.Debug.WriteLine("Name: "+s + "  X:"+npt.X.ToString()+ " Y:"+npt.Y.ToString());

						smi.setDisplayLocation(npt);
						if (smi.BackColor == MyCommonBackColor)
						{
							smi.BackColor = MyCommonBackColor;
						}

						//move onto the next point 
						++rowCount;
						if(rowCount == numPerColumn)
						{
							yoffset = DisplayOffsetY;
							xoffset += smi.Width + column_seperation;
							rowCount = 0;
						}
						else
						{
							yoffset += smi.Height + row_seperation;
						}
					}
				}
			}
		}

		/// <summary>
		/// Layout the controls in a Grid according to the insertion order of the functional name
		/// Uses the Display Offset Location to start with and the numbers of formatted columns 
		/// </summary>
		/// <param name="IsActive"></param>
		protected void DoLayoutRetired(Boolean IsActive)
		{
			//Need to know how many per column
			double numPerColumn = ((double)ExistingSMIs.Values.Count) / ((double)columns);
			numPerColumn = Math.Ceiling(numPerColumn);
			//
			int xoffset = DisplayOffsetX;
			int yoffset = DisplayOffsetY;
			int rowCount = 0;
			Point npt = new Point(0,0);
			
			//Iterate through using the insertion of the functional names
			foreach(ServiceMonitorItem smi in RetiredSMIs.Values)
			{
				//If we are laying out the retired, ignore any control that has no retired service
				Boolean IncludeThisItem = true;
				//layout the required Item 
				if (IncludeThisItem)
				{
					string s = smi.getFunctionName();
					npt.X = xoffset;
					npt.Y = yoffset;
					//System.Diagnostics.Debug.WriteLine("Name: "+s + "  X:"+npt.X.ToString()+ " Y:"+npt.Y.ToString());

					smi.setDisplayLocation(npt);
					if (smi.BackColor == MyCommonBackColor)
					{
						smi.BackColor = MyCommonBackColor;
					}

					//move onto the next point 
					++rowCount;
					if(rowCount == numPerColumn)
					{
						yoffset = DisplayOffsetY;
						xoffset += smi.Width + column_seperation;
						rowCount = 0;
					}
					else
					{
						yoffset += smi.Height + row_seperation;
					}
				}
			}
		}

		/// <summary>
		/// Layout for both Active and Retired Services
		/// </summary>
		public void DoLayoutBoth()
		{
			DoLayoutNew(true);
			DoLayoutRetired(true);
		}

		/// <summary>
		/// New Business Service being added to Group
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="child"></param>
		protected void businessServicesGroup_ChildAdded(Node sender, Node child)
		{
			//When we are adding a new service, there are 2 situations
			//A, it's a upgrade to an existing service  
			//B, it's a new service with a new function 
			string node_type = child.GetAttribute("type");
			string function_name = child.GetAttribute("biz_service_function");
			string name = child.GetAttribute("name");
			//System.Diagnostics.Debug.WriteLine("SPV Created Child name:"+name + " function_name:"+function_name + " node_type:" +node_type);

			//in future there might other things in here, we only want "biz_service"
			if (node_type.ToLower() == "biz_service")
			{
				AddService(function_name, child, name);
				DoLayoutBoth();
				if (ExistingFunctionNames.Contains(function_name))
				{
					ServiceMonitorItem smi = (ServiceMonitorItem) ExistingSMIs[function_name];
					//smi.SwapToActiveLocation();
				}
			}
		}

		/// <summary>
		/// Business Service being removed from Group
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="child"></param>
		protected void businessServicesGroup_ChildRemoved(Node sender, Node child)
		{
			//We don't remove services at this point
			//string node_type = child.GetAttribute("type");
			//string function_name = child.GetAttribute("biz_service_function");
			//string name = child.GetAttribute("name");
			//System.Diagnostics.Debug.WriteLine("SPV Removed Child name:"+name + " function_name:"+function_name + " node_type:" +node_type);
		}

		protected void toggleView_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			showRetiredServices = ! showRetiredServices;
			SetShowRetiredServices(showRetiredServices);
		}

		protected void MonitorUpgradeNode_ChildAdded(Node sender, Node child)
		{
			//use the sender to identify the which service name has a new upgrade available
			//string node_type = child.GetAttribute("type");
			//string function_name = child.GetAttribute("biz_service_function");
			//string name = child.GetAttribute("name");
			//System.Diagnostics.Debug.WriteLine("MUN Added Child name:"+name + " function_name:"+function_name + " node_type:" +node_type);
		}


		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			//g.DrawRectangle(Pens.Cyan,0,0,this.Width-1,this.Height-1);
			if (SelfDrawTranslatedTitle)
			{
				string title_text = panelTitle;
				if (auto_translate)
				{
					title_text = TextTranslator.TheInstance.Translate(title_text);
				    TransitionScreen.DrawSectionTitle(this, g, title_text, titleFont, text_brush, new Point (10, 0));
				}
			}
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);
		}
	}
}