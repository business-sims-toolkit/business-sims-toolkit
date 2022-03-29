using System.Collections;
using LibCore;
using Network;

using CommonGUI;
using System;

namespace TransitionScreens
{
	/// <summary>
	/// Common, display-agnostic, functionality for all project panels.
	/// </summary>
	public class ProjectProgressPanelBase : FlickerFreePanel
	{
		protected const string AttrName_Stage_DEFINITION = "";
		protected const string AttrName_Stage_DESIGN = "design";
		protected const string AttrName_Stage_BUILD = "build";
		protected const string AttrName_Stage_TEST = "test";
		protected const string AttrName_Stage_HANDOVER = "handover";
		protected const string AttrName_Stage_READY = "ready";
		protected const string AttrName_Stage_INSTALL_OK= "installed_ok";
		protected const string AttrName_Stage_INSTALL_FAIL= "installed_fail";

		protected const string AttrName_FirstDay = "firstday";
		protected const string AttrName_DesignDays = "designdays";
		protected const string AttrName_BuildDays = "builddays";
		protected const string AttrName_TestDays = "testdays";

		protected const string AttrName_Stage = "stage";
		protected const string AttrName_WRequest = "wrequest";
		protected const string AttrName_StageDaystoGo = "stagedaystogo";
		protected const string AttrName_wCount = "wcount";
		protected const string AttrName_OverTime = "overtime";
		protected const string AttrName_HandoverValue = "handovervalue";
		protected const string AttrName_HandoverDisplay = "handoverdisplay";
		protected const string AttrName_ReadyForDeployment = "readyfordeployment";
		protected const string AttrName_ProjectDisplayName = "projectid";
		protected const string AttrName_ProductDisplayName = "productid";
		protected const string AttrName_PlatformDisplayName = "platformid";
		protected const string AttrName_ReadyDayValue = "readydayvalue";
		protected const string AttrName_ActualCost = "actual_cost";
		protected const string AttrName_CurrentSpend = "currentspend";
		protected const string AttrName_Location = "location";
		protected const string AttrName_FailReason = "failreason";

		protected string stage;
		protected string location = string.Empty;
		
		protected int wrequest = 0;
		protected int wcount = 0;
		protected int wdays = 0;
		protected bool OvertimeFlag = false;
		protected string Handover;
		protected string GoLive;
		protected int ReadyDays;
		protected string DisplayNameProject;
		protected string DisplayNameProduct;
		protected string DisplayNamePlatform;
		protected int ActualCost = 0;
		protected int CurrentSpend = 0;
		protected string reason = string.Empty;
		protected int effectiveness = 0;

		protected int dayStarted = 0;
		protected int daysToDesign = 0;
		protected int daysToBuild = 0;
		protected int daysToTest = 0;
		protected int dayToInstall = 0;

		protected Node monitoredProject;

		public ProjectProgressPanelBase (Node project)
		{
			//if we have a good node then get the data from it 
			//if not, just reset and display empty 
			if (project != null)
			{
				string xt = project.toDataString(true);

				stage = project.GetAttribute(AttrName_Stage);
				wrequest = project.GetIntAttribute(AttrName_WRequest,0);
				wcount = project.GetIntAttribute(AttrName_wCount,0);
				wdays = project.GetIntAttribute(AttrName_StageDaystoGo,0);
				OvertimeFlag = (project.GetAttribute(AttrName_OverTime)) == "true";
				Handover = project.GetAttribute(AttrName_HandoverDisplay);
				GoLive = project.GetAttribute(AttrName_ReadyForDeployment);
				DisplayNameProject = project.GetAttribute(AttrName_ProjectDisplayName);
				DisplayNameProduct = project.GetAttribute(AttrName_ProductDisplayName);
				DisplayNamePlatform = project.GetAttribute(AttrName_PlatformDisplayName);
				ActualCost = CONVERT.ParseInt(project.GetAttribute(AttrName_ActualCost)); 
				CurrentSpend = CONVERT.ParseInt(project.GetAttribute(AttrName_CurrentSpend));
				effectiveness = CONVERT.ParseInt(project.GetAttribute(AttrName_HandoverValue));
				location = project.GetAttribute(AttrName_Location);
				reason = project.GetAttribute(AttrName_FailReason);

				// The following attribute isn't present in old saved games, so leave reading it
				// to our specialised subclasses.
				dayStarted = 0;

				daysToDesign = CONVERT.ParseInt(project.GetAttribute(AttrName_DesignDays));
				daysToBuild = CONVERT.ParseInt(project.GetAttribute(AttrName_BuildDays));
				daysToTest = CONVERT.ParseInt(project.GetAttribute(AttrName_TestDays));
			}
			else
			{
				Reset();
			}

			monitoredProject = project;
			if (project != null)
			{
				project.AttributesChanged += NodeAttributesChanged;
			}
		}

		public virtual void Reset()
		{
			if(null != monitoredProject)
			{
				monitoredProject.AttributesChanged -= NodeAttributesChanged;
				monitoredProject = null;
			}
			//
			stage = "";
			wrequest = 0;
			wcount = 0;
			wdays = 0;
			OvertimeFlag = false;
			Handover = string.Empty;
			GoLive = string.Empty;		
			DisplayNameProject = string.Empty;
			DisplayNameProduct = string.Empty; 
			DisplayNamePlatform = string.Empty;
			ActualCost = 0;
			CurrentSpend = 0;
			effectiveness = 0;
		}

		protected override void Dispose(bool disposing)
		{
			if(disposing)
			{
				Reset();
			}

			base.Dispose(disposing);
		}

		public Node getMonitoredProjectNode()
		{
			return monitoredProject;
		}

		public event EventHandler ProjectStatusChanged;

		protected virtual void OnProjectStatusChanged ()
		{
			ProjectStatusChanged?.Invoke(this, EventArgs.Empty);
		}

		void NodeAttributesChanged(Node sender, ArrayList attrs)
		{
			if (attrs != null && attrs.Count > 0)
			{
				foreach(AttributeValuePair avp in attrs)
				{
					string attribute = avp.Attribute;
					string newValue = avp.Value;

					switch (attribute.ToLower())
					{
						case AttrName_Stage:	
							stage = newValue;
							break;

						case AttrName_WRequest:
							wrequest = CONVERT.ParseInt(newValue);
							break;

						case AttrName_wCount:	
							wcount = CONVERT.ParseInt(newValue);
							break;

						case AttrName_StageDaystoGo:
							if (newValue != "")
							{
								wdays = CONVERT.ParseInt(newValue);
							}
							break;

						case AttrName_OverTime:
							//Don't Care
							break;

						case AttrName_HandoverDisplay:
							Handover = newValue;
							break;

						case AttrName_ReadyForDeployment:
							GoLive = newValue;
							break;

						case AttrName_ProjectDisplayName:
							DisplayNameProject = newValue;
							break;

						case AttrName_ProductDisplayName:
							DisplayNameProduct = newValue;
							break;

						case AttrName_PlatformDisplayName:
							DisplayNamePlatform = newValue;
							break;

						case AttrName_ReadyDayValue:
							ReadyDays = CONVERT.ParseInt(newValue);
							break;

						case AttrName_ActualCost:
							ActualCost = CONVERT.ParseInt(newValue);
							break;

						case AttrName_CurrentSpend:
							CurrentSpend = CONVERT.ParseInt(newValue);
							break;

						case AttrName_HandoverValue:
							effectiveness = CONVERT.ParseInt(newValue);
							break;

						case AttrName_Location:
							location = newValue;
							break;

						case AttrName_FailReason:
							reason = newValue;
							break;

						case AttrName_FirstDay:
							dayStarted = CONVERT.ParseInt(newValue);
							break;

						case AttrName_DesignDays:
							daysToDesign = CONVERT.ParseInt(newValue);
							break;

						case AttrName_BuildDays:
							daysToBuild = CONVERT.ParseInt(newValue);
							break;

						case AttrName_TestDays:
							daysToTest = CONVERT.ParseInt(newValue);
							break;
					}
				}
			}

			OnProjectStatusChanged();
		}
	}
}