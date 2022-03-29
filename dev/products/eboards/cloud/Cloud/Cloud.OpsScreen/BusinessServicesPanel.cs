using System.Drawing;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;

using System.Collections;
using System.Collections.Generic;
using CoreUtils;
using LibCore;
using Network;
using CommonGUI;

namespace Cloud.OpsScreen
{
	public class BusinessServicesPanel : FlickerFreePanel
	{
		protected Node BusinessServiceListNode = null;
		protected Node timeNode = null;
		protected Hashtable BS_Nodes = new Hashtable();
		protected ArrayList BS_names = new ArrayList();

		protected ArrayList BS_Items_alreadyDisplayed = new ArrayList();
		protected ArrayList BS_Items_DEV_Candidates = new ArrayList();

		protected Bitmap Indicator_Red;
		protected Bitmap Indicator_Amber;
		protected Bitmap Indicator_Green;
		protected Bitmap Indicator_White;
		protected Bitmap Indicator_Blue;
		protected Font Font_BSName;
		protected int Icon_Display_Width = 12;
		protected int Icon_Display_Height = 12;

	    NodeTree model;
	    int currentRound;
	    bool isTraining;

	    string businessTitle;
	    Color titleBackColour;
	    Color backColour;
        
		public BusinessServicesPanel(NodeTree nt, Node businessNode, bool isTraining)
		{
            model = nt;
		    this.isTraining = isTraining;
		    currentRound = model.GetNamedNode("RoundVariables").GetIntAttribute("current_round", 0);
            BusinessServiceListNode = businessNode;

		    SetTitle();
            SetBusinessNodeName();

		    Debug.Assert(currentRound > 0);

			Indicator_Red = (Bitmap)Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images\\buttons\\light_red.png");
			Indicator_Amber = (Bitmap)Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images\\buttons\\light_amber.png");
			Indicator_Green = (Bitmap)Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images\\buttons\\light_green.png");
			Indicator_White = (Bitmap)Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images\\buttons\\light_white.png");
			Indicator_Blue = (Bitmap)Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images\\buttons\\light_blue.png");

			string font = SkinningDefs.TheInstance.GetData("fontname");
			Font_BSName = FontRepository.GetFont(font, 10, FontStyle.Regular);

			timeNode = model.GetNamedNode("CurrentTime");
			timeNode.AttributesChanged += new Node.AttributesChangedEventHandler(timeNode_AttributesChanged);

		    Setup();
		}

        void Setup()
        {
            DoubleBuffered = true;
        }

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (timeNode != null)
				{
					timeNode.AttributesChanged -= new Node.AttributesChangedEventHandler(timeNode_AttributesChanged);
					timeNode = null;
				}
			}
			base.Dispose(disposing);
		}

		protected void timeNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			int seconds = sender.GetIntAttribute("seconds", 0);
			if (seconds % 60 == 0)
			{
				Invalidate();
			}
		}

        void SetTitle()
        {
            businessTitle = BusinessServiceListNode.GetAttribute("desc");

            titleBackColour = SkinningDefs.TheInstance.GetColorDataGivenDefault(businessTitle + "_title_back_colour", Color.Orange);
            backColour = SkinningDefs.TheInstance.GetColorDataGivenDefault(businessTitle + "_back_colour", Color.HotPink);
        }

        void SetBusinessNodeName()
        {
            BusinessServiceListNode.ChildAdded += new Node.NodeChildAddedEventHandler(BusinessServiceListNode_ChildAdded);
            BusinessServiceListNode.ChildRemoved += new Node.NodeChildRemovedEventHandler(BusinessServiceListNode_ChildRemoved);

            foreach (Node child in BusinessServiceListNode.getChildren())
            {
                AddChild(child);
            }
        }
        
		protected void AddChild(Node child)
		{
			if (BS_Nodes.ContainsValue(child) == false)
			{
				string name = child.GetAttribute("name");
				string type = child.GetAttribute("type");
				if (type.ToLower() == "business_service")
				{
					BS_Nodes.Add(name, child);
					BS_names.Add(name);
					BS_names.Sort();
					child.AttributesChanged += new Node.AttributesChangedEventHandler(child_AttributesChanged);
					Invalidate();
				}
			}
		}

		protected void BusinessServiceListNode_ChildAdded(Node sender, Node child)
		{
			AddChild(child);
		}

		protected void BusinessServiceListNode_ChildRemoved(Node sender, Node child)
		{
			if (BS_Nodes.ContainsValue(child))
			{ 
				string name = child.GetAttribute("name");
				BS_Nodes.Remove(name);
				BS_names.Remove(name);
				BS_names.Sort();
				child.AttributesChanged -= new Node.AttributesChangedEventHandler(child_AttributesChanged);
				Invalidate();
			}
		}

		protected void child_AttributesChanged(Node sender, ArrayList attrs)
		{
			Invalidate();
		}

		private void getDisplayAssests(Node businessService, string status_value, bool isNOTDemandBased, bool isDemandRunning, out Bitmap displayicon)
		{
			
			switch (status_value)
			{
				case "no_vm_instance":
					if (businessService.GetBooleanAttribute("is_dev", false)
							&& (businessService.GetIntAttribute("dev_countdown", 0) == 0))
					{
						displayicon = Indicator_Green;
					}
					else
					{
						displayicon = Indicator_Blue;
					}
					break;

				case "up":
					if (isNOTDemandBased == false)
					{
						if (isDemandRunning)
						{
							displayicon = Indicator_Green;
						}
						else
						{
							displayicon = Indicator_White;
						}
					}
					else
					{
						//New service
						if (businessService.GetBooleanAttribute("is_dev", false)
							&& (businessService.GetIntAttribute("dev_countdown", 0) > 0))
						{
							displayicon = Indicator_Blue;
						}
						else
						{
							displayicon = Indicator_Green;
						}
					}
					break;
				case "waiting_on_storage":
				case "waiting_on_server":
				case "waiting_on_handover":
				case "waiting_on_start_of_handover":
					displayicon = Indicator_Amber;
					break;
				case "":
				case "waiting_on_dev":
				case "waiting_on_dev_storage":
				case "waiting_on_dev_server":
				default:
					//displayicon = Indicator_Red;
					displayicon = Indicator_Blue;
					break;
			}
		}

		private void getDemandAspects(string Demand_nodename, string display_name, out bool isDemandActive, 
			out bool isDemandBased, out bool isDemandPast, out bool isDemandRunning, out string new_display_name)
		{
			new_display_name = display_name;
			isDemandBased = false;
			isDemandPast = false;
			isDemandActive = false;
			isDemandRunning = false;

			Node demNode = model.GetNamedNode(Demand_nodename);
			if (demNode != null)
			{
				isDemandBased = true;

				isDemandActive = demNode.GetBooleanAttribute("active", false);
				int duration_count_down = demNode.GetIntAttribute("duration_countdown", 0);
				int delay_count_down = demNode.GetIntAttribute("delay_countdown", 0);
				int linger_count_down = demNode.GetIntAttribute("linger_countdown", 0);

				if ((delay_count_down == 0) & (duration_count_down == 0))
				{
					isDemandPast = true;
				}
				if ((delay_count_down == 0) && (duration_count_down > 0))
				{
					isDemandRunning = true;
				}

				//need to add the Demand Prefix onto the Display Name
				string demand_display_prefix = CONVERT.Format("({0}) ", demNode.GetAttribute(CONVERT.Format("short_desc_round_{0}", currentRound)));
				new_display_name = demand_display_prefix;
			}
		}

		enum CloudType
		{
			Private,
			IaaS,
			SaaS
		}

		CloudType GetServiceLocation (Node businessService)
		{
			Node cloud = model.GetNamedNode(businessService.GetAttribute("cloud"));
			if (cloud != null)
			{
				foreach (Node locationReference in cloud.GetChildrenOfType("cloud_location"))
				{
					Node location = model.GetNamedNode(locationReference.GetAttribute("location"));
					foreach (Node server in location.GetChildrenOfType("server"))
					{
						if (server.GetBooleanAttribute("iaas", false))
						{
							return CloudType.IaaS;
						}
						else if (server.GetBooleanAttribute("saas", false))
						{
							return CloudType.SaaS;
						}
					}
				}
			}

			return CloudType.Private;
		}

		protected override void OnPaint(PaintEventArgs e)
		{
		    int titleHeight = 25;
		    Font titleFont = SkinningDefs.TheInstance.GetFont(12, FontStyle.Bold);
            Color titleTextColour = Color.White;

            using (Brush titleBack = new SolidBrush(titleBackColour))
            {
                Rectangle titleRect = new Rectangle(0, 0, Width, titleHeight);
                e.Graphics.FillRectangle(titleBack,titleRect);

                titleRect.X = 5;

                e.Graphics.DrawString(businessTitle, titleFont, Brushes.White, titleRect,
                    new StringFormat { LineAlignment = StringAlignment.Far, Alignment = StringAlignment.Near });
            }

            using (Brush servicesBack = new SolidBrush(backColour))
            {
                e.Graphics.FillRectangle(servicesBack, new Rectangle(0, titleHeight, Width, Height - titleHeight));
            }

            

			BS_Items_alreadyDisplayed.Clear();
			BS_Items_DEV_Candidates.Clear();

		    const int maxNumberOfServices = 7;
			//base.OnPaint(e);
            int y_offset = titleHeight + 5;
			int y_row_step = (Height - titleHeight) / maxNumberOfServices;
			int y_pos = y_offset;
			string display_name = string.Empty;
			string common_service_name = string.Empty;
			string short_name = string.Empty;

			Bitmap StatusIcon = Indicator_Red;

			List<string> filteredNames = new List<string> ();
			List<string> iaasProviderNames = new List<string> ();
			foreach (Node businessService in BS_Nodes.Values)
			{
				bool includeServiceInList = false;
				CloudType deploymentLocation = GetServiceLocation(businessService);

				if (deploymentLocation == CloudType.IaaS)
				{
					includeServiceInList = ((businessService.GetIntAttribute("created_in_round", 0) == currentRound)
										   && ((timeNode.GetIntAttribute("seconds", 0) == 0)
											   || ! string.IsNullOrEmpty(businessService.GetAttribute("demand_name"))));

					string providerName = businessService.GetAttribute("cloud_provider");
					if (! includeServiceInList)
					{
						if (! iaasProviderNames.Contains(providerName))
						{
							iaasProviderNames.Add(providerName);
						}
					}
				}
				else if (deploymentLocation == CloudType.SaaS)
				{
					includeServiceInList = ! businessService.GetBooleanAttribute("is_preexisting", false); 
				}
				else if ((deploymentLocation == CloudType.Private)
						 && (businessService.GetIntAttribute("created_in_round", 0) == currentRound)
						 && ((currentRound <= 2) || (businessService.GetAttribute("status") != "up") || ! string.IsNullOrEmpty(businessService.GetAttribute("demand_name")))
					     && ((! businessService.GetBooleanAttribute("is_dev", false))
				             || (model.GetNamedNode(businessService.GetAttribute("production_service_name")) == null)))
				{
					includeServiceInList = true;
				}

				if (includeServiceInList)
				{
					filteredNames.Add(businessService.GetAttribute("name"));
				}
			}

			List<string> sortedNames = new List<string> (filteredNames);
			sortedNames.Sort(delegate (string a, string b)
							{
								string demandPrefix = "(DMD";

								if (a.StartsWith(demandPrefix) && ! b.StartsWith(demandPrefix))
								{
									return -1;
								}
								else if ((! a.StartsWith(demandPrefix)) && b.StartsWith(demandPrefix))
								{
									return 1;
								}

								return a.CompareTo(b);
							});

			if (iaasProviderNames.Count > 0)
			{
				StringBuilder builder = new StringBuilder();
				foreach (string iaasProviderName in iaasProviderNames)
				{
					if (builder.Length > 0)
					{
						builder.Append(", ");
					}

					builder.Append(model.GetNamedNode(iaasProviderName).GetAttribute("short_desc"));
				}

				sortedNames.Insert(0, builder.ToString());
			}

            using (Brush serviceNameBrush = new SolidBrush(Color.FromArgb(244,244,244)))
            {
                foreach (string name in sortedNames)
                {
                    display_name = name;

                    Node bsnode = (Node)BS_Nodes[name];
                    if (bsnode != null)
                    {
                        string status_value = bsnode.GetAttribute("status");
                        string service_code = bsnode.GetAttribute("service_code");
                        bool is_dev = bsnode.GetBooleanAttribute("is_dev", false);

                        //get the shortened service name
                        common_service_name = bsnode.GetAttribute("short_name");
                        short_name = bsnode.GetAttribute("short_name");
                        display_name = short_name;
                        bool isnotDEV = (is_dev == false); // PRODUCTION
                        bool isDemandBased = false;
                        bool isDemandActive = false;
                        bool isDemandRunning = false;
                        bool isDemandPast = false;

                        //Extract out the demand aspects 
                        string Demand_nodename = bsnode.GetAttribute("demand_name");
                        getDemandAspects(Demand_nodename, display_name, out isDemandActive,
                            out isDemandBased, out isDemandPast, out isDemandRunning, out display_name);

                        //whether to show it (Not DemandBased) OR (Demandbased and Active) 
                        bool isNOTDemandBased = isDemandBased == false;
                        bool isDemandBased_and_Active = (isDemandBased) && (isDemandActive) && (isDemandPast == false);

                        CloudType location = GetServiceLocation(bsnode);
                        if (location != CloudType.Private)
                        {
                            Node vendor = model.GetNamedNode(bsnode.GetAttribute("cloud_provider"));
                            display_name += CONVERT.Format(" ({0})", vendor.GetAttribute("short_desc"));
                        }

                        if ((isnotDEV))
                        {
                            if (BS_Items_alreadyDisplayed.Contains(common_service_name) == false)
                            {
                                BS_Items_alreadyDisplayed.Add(common_service_name);
                            }

                            //This is PRODUCTION
                            if ((isNOTDemandBased) | (isDemandBased_and_Active))
                            {
                                getDisplayAssests(bsnode, status_value, isNOTDemandBased, isDemandRunning, out StatusIcon);
                                if (StatusIcon != null)
                                {
                                    e.Graphics.DrawImage(StatusIcon, 2, y_pos, Icon_Display_Width, Icon_Display_Height);
                                }
                                if (string.IsNullOrEmpty(display_name) == false)
                                {
                                    //TODO
                                    e.Graphics.DrawString(display_name, Font_BSName, serviceNameBrush, 15, y_pos - 4);
                                }
                                y_pos += y_row_step;
                            }
                        }
                        else
                        {
                            //This is DEV and worthy of further consideration 
                            if (BS_Items_DEV_Candidates.Contains(name) == false)
                            {
                                BS_Items_DEV_Candidates.Add(name);
                            }
                        }
                    }
                    else
                    {
                        // TODO
                        // Must be an IaaS provider we want to list.
                        e.Graphics.DrawString(CONVERT.Format("IaaS services ({0})", name),
                                              Font_BSName, serviceNameBrush, 15, y_pos - 4);
                        y_pos += y_row_step;
                    }
                }
                //Now go through the Dev candidates,
                //these are dev items whose common service name has not already been displayed 
                foreach (string name in sortedNames)
                {
                    string dev_display_name = string.Empty;
                    if (BS_Items_DEV_Candidates.Contains(name))
                    {
                        Node bsnode = (Node)BS_Nodes[name];
                        if (bsnode != null)
                        {
                            string status_value = bsnode.GetAttribute("status");
                            string service_code = bsnode.GetAttribute("service_code");
                            bool is_dev = bsnode.GetBooleanAttribute("is_dev", false);
                            string common_service_name2 = bsnode.GetAttribute("short_name");
                            dev_display_name = common_service_name2;
                            //Have we already display this (there is a PRODUCTION thing already displayed)
                            if (BS_Items_alreadyDisplayed.Contains(common_service_name2) == false)
                            {
                                //display the dev service as we haven't display this before 

                                bool isnotDEV = (is_dev == false); // PRODUCTION
                                bool isDemandBased = false;
                                bool isDemandActive = false;
                                bool isDemandRunning = false;
                                bool isDemandPast = false;

                                //Extract out the demand aspects 
                                string Demand_nodename = bsnode.GetAttribute("demand_name");
                                getDemandAspects(Demand_nodename, dev_display_name, out isDemandActive,
                                    out isDemandBased, out isDemandPast, out isDemandRunning, out display_name);

                                //whether to show it (Not DemandBased) OR (Demandbased and Active) 
                                bool isNOTDemandBased = isDemandBased == false;
                                bool isDemandBased_and_Active = (isDemandBased) && (isDemandActive) && (isDemandPast == false);

                                if ((isnotDEV == false))
                                {
                                    //This is DEV 
                                    if ((isNOTDemandBased) | (isDemandBased_and_Active))
                                    {
                                        getDisplayAssests(bsnode, status_value, isNOTDemandBased, isDemandRunning, out StatusIcon);
                                        if (StatusIcon != null)
                                        {
                                            e.Graphics.DrawImage(StatusIcon, 2, y_pos, Icon_Display_Width, Icon_Display_Height);
                                        }
                                        if (string.IsNullOrEmpty(display_name) == false)
                                        {
                                            //TODO
                                            e.Graphics.DrawString("DV "+ display_name, Font_BSName, serviceNameBrush, 15, y_pos-4);
                                        }
                                        y_pos += y_row_step;
                                    }
                                }
                            }
                        }
                    }
                }
            }

			
		}
	}
}