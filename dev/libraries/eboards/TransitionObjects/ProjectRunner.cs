using System;
using System.Collections;
using System.Xml;

using CoreUtils;
using LibCore;
using Network;
using IncidentManagement;

namespace TransitionObjects
{
	/// <summary>
	/// Project Runner processes projects 
	/// It handles Project Creation Requests, Project Cancellation Requests and Project Insert Requests.
	/// It will process any running projects (using an internal timer) 
	/// </summary>
	public class ProjectRunner
	{
		//Attributes (marking the project)
		const string AttrName_ReadyForDeployment = "ReadyForDeployment";
		//Stage Attributes (used in processing the project)
		const string AttrName_Stage = "stage";

		const string AttrName_Stage_DEFINITION = "";
		const string AttrName_Stage_DESIGN = "design";
		const string AttrName_Stage_BUILD = "build";
		const string AttrName_Stage_TEST = "test";
		const string AttrName_Stage_HANDOVER = "handover";
		const string AttrName_Stage_READY= "ready";
		const string AttrName_Stage_INSTALL_OK= "installed_ok";
		const string AttrName_Stage_INSTALL_FAIL= "installed_fail";

		const string AttrName_StageDays_Design = "designdays";		//Number of design days required
		const string AttrName_StageDays_Build = "builddays";			//Number of build days required
		const string AttrName_StageDays_Test = "testdays";				//Number of test days required
		const string AttrName_StageDaystoGo = "StageDaystoGo";		//Number of days left in this stage
		const string AttrName_HandoverValue = "handovervalue";		//Internal Handover Value 
		const string AttrName_HandoverDisplay = "handoverdisplay";//Displayed handover figure 
		const string AttrName_ReadyDayValue = "readydayvalue";		//Which day will we be ready on 
		const string AttrName_ActualCost = "actual_cost";					//Estimated Cost
		const string AttrName_CurrentSpend = "currentspend";			//how much we have spent
		const string AttrName_FailReason = "failreason";					//why did it fail
		const string AttrName_FixedLocation = "fixedlocation";		//The location, only if fixed
		const string AttrName_FixedZone = "fixedzone";						//The zone, only if fixed
		const string AttrName_CompletedDays = "completed_days";		//
		const string AttrName_ScheduledDays = "scheduled_days";		//
		const string AttrName_Location = "location";							//
		const string AttrName_DeployHardware = "deploy_hardware";	//wether this deploys hardware as well as the App

		const string AttrName_usePrefer = "use_prefer_interaction";//

		Hashtable dayToBookedOpsEvents = new Hashtable();

		NodeTree MyNodeTree;

		//Support Varibles 
		string dataDir = string.Empty;

		Boolean MyTransitionMode = true;
		Boolean Cancelled = false;

		Node projectNode;
		Node currentDayNode;
		Node currentTimeNode;

		int lastKnownCurrentDay = 0;
		int lastKnownCurrentSecond = 0;

		string _createActionsStr = string.Empty;
		string _installActionsStr = string.Empty;

		string requirements = "";
		int WhenToInstall = -1;
		string install_title_name = "Install";
		string install_name = "install";

		public bool InstallScheduled
		{
			get
			{
				return (WhenToInstall >= 0);
			}
		}
		public int InstallDueTime
		{
			get
			{
				return WhenToInstall;
			}
		}

		public bool ProjectVisible
		{
			get
			{
				return (projectNode != null) && (projectNode.GetBooleanAttribute("visible", true));
			}
		}

		#region Constructor, Connections and Dispose

		/// <summary>
		/// 
		/// </summary>
		public ProjectRunner(Node n, Boolean TransitionMode)
		{
			MyNodeTree = n.Tree;
			projectNode = n;

			install_title_name = SkinningDefs.TheInstance.GetData("transition_install_title","Install");
			install_name = SkinningDefs.TheInstance.GetData("transition_install_name","install");

			MyTransitionMode = TransitionMode;
			if (MyTransitionMode)
			{
				//In transition we count in Days
				currentDayNode = MyNodeTree.GetNamedNode("CurrentDay");
				lastKnownCurrentDay = currentDayNode.GetIntAttribute("day",0);
				//
				currentDayNode.AttributesChanged += TimeNode_AttributesChanged;
			}
			else
			{
				//In operation we count in Seconds
				currentTimeNode = MyNodeTree.GetNamedNode("CurrentTime");
				lastKnownCurrentSecond = currentTimeNode.GetIntAttribute("seconds",0);
				currentTimeNode.AttributesChanged += TimeNode_AttributesChanged;
			}
			projectNode.Deleting += projectNode_Deleting;
		}

		/// <summary>
		/// Dispose ...
		/// </summary>
		public void Dispose()
		{
			Cancelled = true;
			if (this.MyTransitionMode)
			{
				currentDayNode.AttributesChanged -= TimeNode_AttributesChanged;
			}
			else
			{
				currentTimeNode.AttributesChanged -= TimeNode_AttributesChanged;
			}
			projectNode.Deleting -= projectNode_Deleting;
		}

		void projectNode_Deleting(Node sender)
		{
			// This project is being removed.
			// TODO : check that we do get disposed!
			this.Dispose();
		}

		#endregion Constructor, Connections and Dispose

		#region Misc Utils

		void OutputError(string errorText)
		{
			Node errorsNode = MyNodeTree.GetNamedNode("FacilitatorNotifiedErrors");
			Node error = new Node(errorsNode, "error", "", new AttributeValuePair( "text", errorText ) );
		}

		public Node getProjectNode()
		{ 
			return projectNode;
		}

		#endregion Misc Utils

		#region ProjectsRunning Methods

		/// <summary>
		/// This allows the create script to be set up 
		/// As we need to apply the creation scrtipt before we know the install location 
		/// </summary>
		public void Set_CreateScriptActionXML(string xmlDef)
		{
			string processed_XmlDef = xmlDef;
			XmlDocument xdoc = new XmlDocument();
			xdoc.LoadXml(processed_XmlDef);
			XmlNode isnode = xdoc.DocumentElement.SelectSingleNode("i");
			XmlNode createnode = isnode.SelectSingleNode("oncreate");
			_createActionsStr = createnode.OuterXml;
		}

		/// <summary>
		/// This builds the create incident 
		/// Mind to replace the SLA limit based on what the Project has
		/// </summary>
		/// <returns></returns>
		IncidentDefinition BuildCreateActions_Incident()
		{
			XmlDocument xdoc = new XmlDocument();

			string processed_XmlDef = _createActionsStr;

			if (processed_XmlDef.IndexOf("YYY")>-1)
			{
				//TODO  Replacement by SLA Manager Access Routines to get the value 
				string defined_SlaLimit = SLAManager.get_SLA_string(projectNode);
				if (defined_SlaLimit != "YYY")
				{
					_createActionsStr = processed_XmlDef.Replace("YYY", defined_SlaLimit);
				}
				else
				{
					_createActionsStr = processed_XmlDef.Replace("YYY", "360");
				}
			}
			xdoc.LoadXml(this._createActionsStr);
			XmlNode createnode = xdoc.FirstChild;
			string s1 = createnode.OuterXml;
			string s2 = createnode.InnerXml;
			string s3 = createnode.InnerText;
			return (new IncidentDefinition(createnode,MyNodeTree));
		}

		/// <summary>
		/// This builds the install incident 
		/// The location replacement has already been performed
		/// </summary>
		/// <returns></returns>
		IncidentDefinition BuildInstallActions_Incident()
		{
			XmlDocument xdoc = new XmlDocument();

			string processed_XmlDef = this._installActionsStr;

			if (processed_XmlDef.IndexOf("YYY")>-1)
			{
				//TODO  Replacement by SLA Manager Access Routines to get the value 
				string defined_SlaLimit = SLAManager.get_SLA_string(projectNode);
				if (defined_SlaLimit != "YYY")
				{
					processed_XmlDef = processed_XmlDef.Replace("YYY", defined_SlaLimit);
				}
				else
				{
					processed_XmlDef = processed_XmlDef.Replace("YYY", "360");
				}
			}

			xdoc.LoadXml(processed_XmlDef);
			XmlNode installnode = xdoc.FirstChild;
			string s1 = installnode.OuterXml;
			string s2 = installnode.InnerXml;
			string s3 = installnode.InnerText;
			return (new IncidentDefinition(installnode,MyNodeTree));
		}

		public void UpdateNodeData(string request_Location, int request_SLA )
		{
			this.projectNode.SetAttribute("location",request_Location);
			this.projectNode.SetAttribute("slalimit",CONVERT.ToStr(request_SLA*60));
		}

		public void ApplyActionOnTime(string xmlDef, int when, string ReplaceLocation, int RequestSLA)
		{
			string RequestSLA_SecondsString = CONVERT.ToStr(RequestSLA*60);
			//Need to extract the actions scripts for create and install 
			requirements = "";
			XmlDocument xdoc = new XmlDocument();
			xdoc.LoadXml( xmlDef);
			XmlNode isnode = xdoc.DocumentElement.SelectSingleNode("i");
			XmlNode createnode = isnode.SelectSingleNode("oncreate");
			XmlNode installnode = isnode.SelectSingleNode("oninstall");

			_createActionsStr = createnode.OuterXml;
			_installActionsStr = installnode.OuterXml;

			string processed_XmlDef = _installActionsStr;

			//process the installActionsStr for SLA replacements 
			if (processed_XmlDef.IndexOf("YYY")>-1)
			{
				//Just set to the requested value, it beats the sla manager
				processed_XmlDef = processed_XmlDef.Replace("YYY", RequestSLA_SecondsString);
			}

			//process the installActionsStr for Location replacements 
			if (processed_XmlDef.IndexOf("XXXX")>-1)
			{
				if (ReplaceLocation != string.Empty)
				{
					_installActionsStr = processed_XmlDef.Replace("XXXX", ReplaceLocation.ToUpper());
				}
			}

			WhenToInstall = when;

			// Do we have any requirements as well?
			requirements = "";
			XmlNode rnode = xdoc.DocumentElement.SelectSingleNode("requirements");
			if(null != rnode)
			{
				requirements = rnode.OuterXml;

				if (requirements.IndexOf("XXXX")>-1)
				{
					if (requirements != string.Empty)
					{
						requirements = requirements.Replace("XXXX", ReplaceLocation.ToUpper());
					}
				}
			}
		}

		public void ApplyActionOnTimeWithInstallData(string xmlDef, int when, string ReplaceLocation, int RequestSLA, string install_data)
		{
			string RequestSLA_SecondsString = CONVERT.ToStr(RequestSLA * 60);
			//Need to extract the actions scripts for create and install 
			requirements = "";
			XmlDocument xdoc = new XmlDocument();
			xdoc.LoadXml(xmlDef);
			XmlNode isnode = xdoc.DocumentElement.SelectSingleNode("i");
			XmlNode createnode = isnode.SelectSingleNode("oncreate");
			XmlNode installnode = isnode.SelectSingleNode("oninstall");

			_createActionsStr = createnode.OuterXml;
			_installActionsStr = installnode.OuterXml;

			string processed_XmlDef = _installActionsStr;

			//process the installActionsStr for SLA replacements 
			if (processed_XmlDef.IndexOf("YYY") > -1)
			{
				//Just set to the requested value, it beats the sla manager
				processed_XmlDef = processed_XmlDef.Replace("YYY", RequestSLA_SecondsString);
			}

			//process the installActionsStr for SLA replacements 
			if (processed_XmlDef.IndexOf("VVVV") > -1)
			{
				//Just set to the requested value, it beats the sla manager
				processed_XmlDef = processed_XmlDef.Replace("VVVV", install_data);
			}

			//process the installActionsStr for Location replacements 
			if (processed_XmlDef.IndexOf("XXXX") > -1)
			{
				if (ReplaceLocation != string.Empty)
				{
					_installActionsStr = processed_XmlDef.Replace("XXXX", ReplaceLocation.ToUpper());
				}
			}

			WhenToInstall = when;

			// Do we have any requirements as well?
			requirements = "";
			XmlNode rnode = xdoc.DocumentElement.SelectSingleNode("requirements");
			if (null != rnode)
			{
				requirements = rnode.OuterXml;

				if (requirements.IndexOf("XXXX") > -1)
				{
					if (requirements != string.Empty)
					{
						requirements = requirements.Replace("XXXX", ReplaceLocation.ToUpper());
					}
				}
			}




		}

		void UpdateCompletedDays()
		{
			int completedDayCount = projectNode.GetIntAttribute(AttrName_CompletedDays,0);
			completedDayCount += 1;
			projectNode.SetAttribute(AttrName_CompletedDays, CONVERT.ToStr(completedDayCount));
		}

		void HandlePercentageSpent()
		{
			//extract the values
			int completedDayCount = projectNode.GetIntAttribute(AttrName_CompletedDays,0);
			int scheduledDayCount = projectNode.GetIntAttribute(AttrName_ScheduledDays,1);
			int actual_cost = projectNode.GetIntAttribute(AttrName_ActualCost,0);

			//Calculate the money spent from a percentage of the time elapsed
			int current_spend = ((completedDayCount * actual_cost) / scheduledDayCount);
			projectNode.SetAttribute(AttrName_CurrentSpend,current_spend);

			//System.Diagnostics.Debug.WriteLine("completedDayCount: "+completedDayCount.ToString());
			//System.Diagnostics.Debug.WriteLine("scheduledDayCount: "+scheduledDayCount.ToString());
			//System.Diagnostics.Debug.WriteLine("actual_cost: "+actual_cost.ToString());
			//System.Diagnostics.Debug.WriteLine("current_spend: "+current_spend.ToString());
		}

		/// <summary>
		/// Actually does the work of moving the active projects through the states 
		/// </summary>
		void ProgressProject(int CurrDay)
		{
			int GoLive = 0;
			string tmpStageValue;
			string tmpDaysValue;

			int count = 0;

			string st2 = projectNode.GetAttribute(AttrName_ReadyForDeployment);
			string sd1 = projectNode.GetAttribute(AttrName_StageDays_Design);
			string sd2 = projectNode.GetAttribute(AttrName_StageDays_Build);
			string sd3 = projectNode.GetAttribute(AttrName_StageDays_Test);
			string sd4 = projectNode.GetAttribute(AttrName_ActualCost);
			string sd5 = projectNode.GetAttribute(AttrName_CurrentSpend);

			int handover_value = projectNode.GetIntAttribute(AttrName_HandoverValue,0);

			int DesignDays = CONVERT.ParseInt(sd1);
			int BuildDays = CONVERT.ParseInt(sd2);
			int TestDays = CONVERT.ParseInt(sd3);
			int ActualCost =  CONVERT.ParseInt(sd4);
			int CurrentSpend =  CONVERT.ParseInt(sd5);
			
			GoLive = CurrDay;

			//get the stage 
			tmpStageValue = projectNode.GetAttribute(AttrName_Stage);

			switch (tmpStageValue)
			{
				case AttrName_Stage_DEFINITION:
					//We are in Definition, we move to Design and set the required Design Days 
					projectNode.SetAttribute(AttrName_Stage,AttrName_Stage_DESIGN);
					//Handle the Completing Days and the money spent
					UpdateCompletedDays();
					HandlePercentageSpent();

					tmpDaysValue = projectNode.GetAttribute(AttrName_StageDays_Design);
					projectNode.SetAttribute(AttrName_StageDaystoGo,tmpDaysValue);
					GoLive += DesignDays + BuildDays + TestDays + 1;
					projectNode.SetAttribute(AttrName_ReadyForDeployment,GoLive);
					break;

				case AttrName_Stage_DESIGN:
					//Handle the Completing Days and the money spent
					UpdateCompletedDays();
					HandlePercentageSpent();
					//We are in Design, decrement or move to build
					tmpDaysValue = projectNode.GetAttribute(AttrName_StageDaystoGo);
					count = CONVERT.ParseInt(tmpDaysValue);
					if (count>1)
					{
						count--;
						projectNode.SetAttribute(AttrName_StageDaystoGo,count.ToString());
						GoLive += count + BuildDays + TestDays + 1;
						projectNode.SetAttribute(AttrName_ReadyForDeployment,GoLive.ToString());
					}
					else
					{
						projectNode.SetAttribute(AttrName_Stage,AttrName_Stage_BUILD);
						tmpDaysValue = projectNode.GetAttribute(AttrName_StageDays_Build);
						projectNode.SetAttribute(AttrName_StageDaystoGo,tmpDaysValue);
						GoLive += BuildDays + TestDays + 1; 
						projectNode.SetAttribute(AttrName_ReadyForDeployment,GoLive);
					}
					break;

				case AttrName_Stage_BUILD:
					//Handle the Completing Days and the money spent
					UpdateCompletedDays();
					HandlePercentageSpent();

					//We are in Build, decrement or move to build
					tmpDaysValue = projectNode.GetAttribute(AttrName_StageDaystoGo);
					count = CONVERT.ParseInt(tmpDaysValue);
					if (count>1)
					{
						count--;
						projectNode.SetAttribute(AttrName_StageDaystoGo,count);
						GoLive += count + TestDays + 1;
						projectNode.SetAttribute(AttrName_ReadyForDeployment,GoLive);

					}
					else
					{
						projectNode.SetAttribute(AttrName_Stage,AttrName_Stage_TEST);
						tmpDaysValue = projectNode.GetAttribute(AttrName_StageDays_Test);
						projectNode.SetAttribute(AttrName_StageDaystoGo,tmpDaysValue);
						GoLive += TestDays + 1;
						projectNode.SetAttribute(AttrName_ReadyForDeployment,GoLive.ToString());
					}
					break;

				case AttrName_Stage_TEST:
					//We are in TEST, decrement or move to HANDOVER
					//apply costs only when counting down through the days 
					tmpDaysValue = projectNode.GetAttribute(AttrName_StageDaystoGo);
					count = CONVERT.ParseInt(tmpDaysValue);
					if (count>1)
					{
						//Handle the Completing Days and the money spent
						UpdateCompletedDays();
						HandlePercentageSpent();
						count--;
						projectNode.SetAttribute(AttrName_StageDaystoGo,count);
						GoLive += count + 1;
						projectNode.SetAttribute(AttrName_ReadyForDeployment,GoLive);
					}
					else
					{
						//No costs since we are moving into handover
						projectNode.SetAttribute(AttrName_Stage,AttrName_Stage_HANDOVER);
						//projectNode.SetAttribute(AttrName_HandoverValue,CONVERT.ToStr(handover_value));
						projectNode.SetAttribute(AttrName_HandoverDisplay,CONVERT.ToStr(handover_value));
						projectNode.SetAttribute(AttrName_StageDaystoGo, "0");
						GoLive += 1;
						projectNode.SetAttribute(AttrName_ReadyForDeployment,GoLive);
					}
					break;

				case AttrName_Stage_HANDOVER:
					//We are in HANDOVER, decrement or move to INSTALL
					projectNode.SetAttribute(AttrName_Stage,AttrName_Stage_READY);
					projectNode.SetAttribute(AttrName_StageDaystoGo, "0");
					projectNode.SetAttribute(AttrName_ReadyForDeployment,GoLive);
					//projectNode.SetAttribute(AttrName_HandoverValue,CONVERT.ToStr(handover_value));
					projectNode.SetAttribute(AttrName_HandoverDisplay,CONVERT.ToStr(handover_value));
					RunCreateScript();
					break;
			}
		}
		
		public void RunCreateScript()
		{
			//Need to rebuild the create script to take account of changes to the SlaLimit
			//process the createActionsStr for replacements 
			IncidentDefinition createActions = BuildCreateActions_Incident();
			createActions.ApplyActionNow(this.MyNodeTree);
			createActions = null;
		}

		#endregion ProjectsRunning Methods

		#region Day Change Handler

		void MarkCalenderEventSuccess(Boolean SuccessFlag)
		{
			Node MyCalendarNode = MyNodeTree.GetNamedNode("Calendar");
			string projectID = this.projectNode.GetAttribute("projectid");
			
			ArrayList al = new ArrayList();
			MyCalendarNode.GetNodesWithAttributeValue("projectid", projectID, ref al);
			if (al.Count>0)
			{
				foreach (Node n1 in al)
				{
					string currentState = n1.GetAttribute("status");
					//process if this node is not already marked
					if ((currentState.ToLower() != "completed_ok")&&((currentState.ToLower() != "completed_fail")))
					{
						//we only want to affect calender nodes 
						if (SuccessFlag)
						{
							n1.SetAttribute("status","completed_ok");
						}
						else
						{
							n1.SetAttribute("status","completed_fail");
						}
					}
				}
			}
		}

		void HandleDayChanged(Node sender, ArrayList attrs)
		{
			IncidentDefinition installActions = null;
			
			// Day has probably ticked over.
			foreach(AttributeValuePair avp in attrs)
			{
				if("day" == avp.Attribute)
				{
					// Day has changed.
					int newDay = CONVERT.ParseInt(avp.Value);
					int diffDays = newDay - lastKnownCurrentDay;
					if(diffDays <= 1)
					{
						this.ProgressProject(newDay);
						lastKnownCurrentDay = newDay;
						//
						if(newDay == WhenToInstall)
						{
							installActions = BuildInstallActions_Incident();
							if(installActions != null)
							{
								// Actually only install if the project is ready! or we failed a previous attempt
								string projectStage = projectNode.GetAttribute(AttrName_Stage);

								if ((projectStage == AttrName_Stage_INSTALL_FAIL)|(projectStage == AttrName_Stage_READY))
								{
									// Do we have any requirements and if so are they currently satisfied?
									Boolean requirementsOK = false;
									if("" != requirements)
									{
										string reason;
										string short_reason;

										if(false == RequirementsChecker.AreRequirementsMet(requirements, sender.Tree, out reason, out short_reason))
										{
											//Mark the Project as Failed install 
											projectNode.SetAttribute(AttrName_Stage,AttrName_Stage_INSTALL_FAIL);
											projectNode.SetAttribute(AttrName_FailReason, short_reason); 
											// output an error and prevent the install script running
											OutputError(install_title_name + " failed: Cannot " + install_title_name.ToLower() + " product " + projectNode.GetAttribute("name") + " because " + reason);
											requirementsOK = false; //There are requirements and we failed them 
										}
										else
										{
											requirementsOK = true; //There are requirements and we met them
										}
									}
									else
									{
										requirementsOK = true; //There are no requirements
									}

									if (requirementsOK)
									{
										// Mark us as installed.
										projectNode.SetAttribute(AttrName_Stage,AttrName_Stage_INSTALL_OK);
										installActions.ApplyActionNow(this.MyNodeTree);
										installActions = null;
										MarkCalenderEventSuccess(true); 
									}
									else
									{
										MarkCalenderEventSuccess(false); 
									}
								}
								else
								{
									//Calender Event to Red
									MarkCalenderEventSuccess(false);

									OutputError(install_title_name + " Failed. Product " + projectNode.GetAttribute("name") + " has not completed development.");
								}
							}
						}
					}
					else
					{
						// Throw because we don't support advancing at more than one day at a time.
						throw( new Exception("ProjectRunner only supports day increments. CurrentDay has been moved on by " + CONVERT.ToStr(diffDays) ) );
					}
				}
			}
		}

		void HandleTimeChanged(Node sender, ArrayList attrs)
		{
			IncidentDefinition installActions = null;

			// Day has probably ticked over.
			foreach(AttributeValuePair avp in attrs)
			{
				if("seconds" == avp.Attribute)
				{
					// Day has changed.
					int newSecond = CONVERT.ParseInt(avp.Value);
					//System.Diagnostics.Debug.WriteLine("Project Runner Seconds "+newSecond.ToString());

					//this.ProgressProject(newDay);  No Need 
					if (WhenToInstall != -1)
					{
						if(newSecond > WhenToInstall)
						{
							WhenToInstall = -1;

							installActions = BuildInstallActions_Incident();
							if(installActions != null)
							{
								// Actually only install if the project is ready! or we failed a previous attempt
								string projectStage = projectNode.GetAttribute(AttrName_Stage);
								if ((projectStage == AttrName_Stage_INSTALL_FAIL)|(projectStage == AttrName_Stage_READY))
								{
									// Do we have any requirements and if so are they currently satisfied?
									Boolean requirementsOK = false;
									if("" != requirements)
									{
										string reason;
										string short_reason;

										if(false == RequirementsChecker.AreRequirementsMet(requirements, sender.Tree, out reason, out short_reason))
										{
											//Mark the Project as Failed install 
											projectNode.SetAttribute(AttrName_Stage,AttrName_Stage_INSTALL_FAIL);
											projectNode.SetAttribute(AttrName_FailReason, short_reason); 
											//No Need to update the pending job as we just don't show any past item

											OutputError(install_title_name+" failed for product " + projectNode.GetAttribute("name") + ". " + reason);
											requirementsOK = false; //There are requirements and we failed them 
										}
										else
										{
											requirementsOK = true; //There are requirements and we met them
										}
									}
									else
									{
										requirementsOK = true; //There are no requirements
									}

									if (requirementsOK)
									{
										// Mark us as installed.
										projectNode.SetAttribute(AttrName_Stage,AttrName_Stage_INSTALL_OK);
										installActions.ApplyActionNow(this.MyNodeTree);
										installActions = null;
									}
								}
								else
								{
									OutputError(install_title_name+" failed for product " + projectNode.GetAttribute("name") + ". Product not ready.");
								}
							}
						}
					}
				}
			}
		}

		void TimeNode_AttributesChanged(Node sender, ArrayList attrs)
		{
            //if (sender.GetIntAttribute("day", 0) != 1)
            {
                if (Cancelled)
                {
                    return;
                }

                if (MyTransitionMode)
                {
                    //working in Days 
                    HandleDayChanged(sender, attrs);
                }
                else
                {
                    //working in Seconds
                    HandleTimeChanged(sender, attrs);
                }
            }
		}

		public void ClearBookingState()
		{
			//projectNode.SetAttribute(AttrName_Stage,AttrName_Stage_READY);
			this.WhenToInstall = -1;
			projectNode.SetAttribute(AttrName_Location, "");

			if (isState_AfterHandoverStarted()==false)
			{
				//projectNode.SetAttribute(AttrName_HandoverValue, "");
				projectNode.SetAttribute(AttrName_HandoverDisplay,"");
			}
		}

		public Boolean isState_Ready()
		{
			Boolean state = false;
			string projectStage = projectNode.GetAttribute(AttrName_Stage);
			if (projectStage == AttrName_Stage_READY)
			{
				state = true;
			}
			return state;
		}

		public Boolean isState_InstallOK()
		{
			Boolean state = false;
			string projectStage = projectNode.GetAttribute(AttrName_Stage);
			if (projectStage == AttrName_Stage_INSTALL_OK)
			{
				state = true;
			}
			return state;
		}


		public Boolean isState_AfterHandoverStarted()
		{
			Boolean state = false;
			string projectStage = projectNode.GetAttribute(AttrName_Stage);
			if (projectStage ==  AttrName_Stage_HANDOVER)
			{
				state = true;
			}
			if (projectStage == AttrName_Stage_READY)
			{
				state = true;
			}
			if (projectStage == AttrName_Stage_INSTALL_OK)
			{
				state = true;
			}
			if (projectStage == AttrName_Stage_INSTALL_FAIL)
			{
				state = true;
			}
			return state;
		}


		public Boolean isState_InstallFail()
		{
			Boolean state = false;
			string projectStage = projectNode.GetAttribute(AttrName_Stage);
			if (projectStage == AttrName_Stage_INSTALL_FAIL)
			{
				state = true;
			}
			return state;
		}

		public void setStateReady()
		{
			projectNode.SetAttribute(AttrName_Stage, AttrName_Stage_READY);
		}

		public string getSipIdentifier()
		{
			return projectNode.GetAttribute("projectid");
		}

		public string getProductIdentifier()
		{
			return projectNode.GetAttribute("productid");
		}

		public string getInstallName()
		{
			return projectNode.GetAttribute("installname");
		}

		public string getUpgradeName()
		{
			return projectNode.GetAttribute("upgradename");
		}

		public bool isDeploy_AppWithHardware()
		{
			return projectNode.GetBooleanAttribute(AttrName_DeployHardware,false);
		}

		public Boolean isState_NotCompleted()
		{
			Boolean state = true;
			string projectStage = projectNode.GetAttribute(AttrName_Stage);
			if (projectStage == AttrName_Stage_READY)
			{
				state = false;
			}
			if (projectStage == AttrName_Stage_INSTALL_OK)
			{
				state = false;
			}
			if (projectStage == AttrName_Stage_INSTALL_FAIL)
			{
				state = false;
			}
			return state;
		}

		public void getFixedInformation(out string fixedlocation, out string fixedzone)
		{
			fixedlocation = projectNode.GetAttribute(AttrName_FixedLocation);
			fixedzone = projectNode.GetAttribute(AttrName_FixedZone);
		}

		public void getPreferedInformation(out bool usePrefer)
		{
			usePrefer = projectNode.GetBooleanAttribute(AttrName_usePrefer,false);
		}

		public int getWhentoInstall()
		{
			return WhenToInstall;
		}

		#endregion Day Change Handler

	}
}