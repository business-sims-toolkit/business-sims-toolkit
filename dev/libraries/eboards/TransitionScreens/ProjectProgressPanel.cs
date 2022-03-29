using System.Collections;
using System.Drawing;
using LibCore;
using CoreUtils;
using Network;

namespace TransitionScreens
{
	/// <summary>
	/// An old-style panel with a load of boxes.
	/// </summary>
	public class ProjectProgressPanel : ProjectProgressPanelBase
	{
		Font NormalDisplayFont = null;
		Font SmallDisplayFont = null;

		int Border = 2;

		StatusDualBox title;
		StatusDualBox design;
		StatusDualBox build;
		StatusDualBox testing;
		StatusDualBox handover;
		StatusDualBox golive;

		StatusDualBox install;
		StatusDualBox spend;
		StatusDualBox cost;

		bool auto_translate = true;
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="n"></param>
		/// <param name="c"></param>
		public ProjectProgressPanel(Node n, Color c)
			: base(n)
		{

			string displayFontName = SkinningDefs.TheInstance.GetData("fontname");
			if (auto_translate)
			{
				displayFontName = TextTranslator.TheInstance.GetTranslateFont(displayFontName);
			}
			NormalDisplayFont = ConstantSizeFont.NewFont(displayFontName,9f,FontStyle.Bold);
			SmallDisplayFont = ConstantSizeFont.NewFont(displayFontName,8f,FontStyle.Regular);

			Location = new Point(10,Border);
			BackColor = Color.Transparent;
			Size = new Size(550,52);

			title = new StatusDualBox(TransitionStatus.TITLE);
			title.ForeColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("project_progress_panel_title_text_colour", Color.Black);
			title.Location = new Point(10,Border);

			handover = new StatusDualBox(TransitionStatus.HANDOVER_TODO);
			//handover.DisplayText = Handover;
			handover.setDisplayText("");
			handover.Location = new Point(250,Border);

			golive = new StatusDualBox(TransitionStatus.NOTREADY);
			golive.setDisplayText(GoLive);
			golive.Location = new Point(310,Border);

			design = new StatusDualBox(wrequest,wcount,wdays,OvertimeFlag);
			build = new StatusDualBox(wrequest,wcount,wdays,OvertimeFlag);
			testing = new StatusDualBox(wrequest,wcount,wdays,OvertimeFlag);

			design.Location = new Point(70,Border);
			build.Location = new Point(130,Border);
			testing.Location = new Point(190,Border);

			install = new StatusDualBox(TransitionStatus.INSTALL_TODO);
			install.Location = new Point(370,Border);
			spend = new StatusDualBox(TransitionStatus.MONEY);
			spend.Location = new Point(430,Border);
			cost = new StatusDualBox(TransitionStatus.MONEY);
			cost.Location = new Point(490,Border);

			spend.Actual = CurrentSpend;
			cost.Actual = ActualCost;
			install.setDisplayText(location);
			install.ForeColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("project_progress_panel_install_location_colour", Color.Black);

			SuspendLayout();
			//Adding the Status Box Controls into the Panel
			Controls.Add(title);
			Controls.Add(design);
			Controls.Add(build);
			Controls.Add(testing);
			Controls.Add(handover);
			Controls.Add(golive);

			Controls.Add(install);
			Controls.Add(spend);
			Controls.Add(cost);
			ResumeLayout(false);

			HandleStageWork();
			updateProjectTitle();

			if (stage != AttrName_Stage_INSTALL_FAIL)
			{
				if (location.Length>2)
				{
					install.setDisplayText(location.ToUpper());
					install.ForeColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("project_progress_panel_install_location_colour", Color.Black);
				}
			}
			else
			{
				install.setDisplayFont(SmallDisplayFont);
			}
			
			//Refresh();
			Invalidate();

			if (n != null)
			{
				n.AttributesChanged += NodeAttributesChanged;
			}
		}

		void updateProjectTitle()
		{
			//title.DisplayText = DisplayNameProject + "\n" + DisplayNameProduct + "\n" + DisplayNamePlatform;
			string newtext = DisplayNameProduct + "\n" + DisplayNamePlatform+ "\n";
			title.setDisplayText(newtext);
		}

		/// <summary>
		/// 
		/// </summary>
		public override void Reset ()
		{
			if (null != monitoredProject)
			{
				monitoredProject.AttributesChanged -= NodeAttributesChanged;
			}

			base.Reset();
		}

		/// <summary>
		/// Common Handler for handling which stage we want to display 
		/// </summary>
		void HandleStageWork()
		{
			switch (stage)
			{
				case AttrName_Stage_DEFINITION:
					golive.Status = TransitionStatus.NOTREADY;
					design.Status = TransitionStatus.TO_DO;
					build.Status = TransitionStatus.TO_DO;
					testing.Status = TransitionStatus.TO_DO;
					break;
				case AttrName_Stage_DESIGN:
					golive.Status = TransitionStatus.NOTREADY;
					design.Status = TransitionStatus.IN_STAGE;
					build.Status = TransitionStatus.TO_DO;
					testing.Status = TransitionStatus.TO_DO;
					break;
				case AttrName_Stage_BUILD:
					golive.Status = TransitionStatus.NOTREADY;
					design.Status = TransitionStatus.WORKING;
					build.Status = TransitionStatus.IN_STAGE;
					testing.Status = TransitionStatus.TO_DO;
					break;
				case AttrName_Stage_TEST :
					golive.Status = TransitionStatus.NOTREADY;
					design.Status = TransitionStatus.WORKING;
					build.Status = TransitionStatus.WORKING;
					testing.Status = TransitionStatus.IN_STAGE;
					break;
				case AttrName_Stage_HANDOVER:
					golive.Status = TransitionStatus.NOTREADY;
					design.Status = TransitionStatus.WORKING;
					build.Status = TransitionStatus.WORKING;
					testing.Status = TransitionStatus.WORKING;
					handover.Status = TransitionStatus.HANDOVER_DONE;
					handover.setDisplayText(Handover);
					break;
				case AttrName_Stage_INSTALL_OK:
					design.Status = TransitionStatus.WORKING;
					build.Status = TransitionStatus.WORKING;
					testing.Status = TransitionStatus.WORKING;
					handover.Status = TransitionStatus.HANDOVER_DONE;
					golive.Status = TransitionStatus.READY;
					install.Status = TransitionStatus.INSTALL_DONE;
					handover.setDisplayText(Handover);
					break;
				case AttrName_Stage_INSTALL_FAIL:
					design.Status = TransitionStatus.WORKING;
					build.Status = TransitionStatus.WORKING;
					testing.Status = TransitionStatus.WORKING;
					handover.Status = TransitionStatus.HANDOVER_DONE;
					golive.Status = TransitionStatus.READY;
					install.Status = TransitionStatus.INSTALL_FAILED;
					install.setDisplayText(reason);
					install.ForeColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("project_progress_panel_install_fail_colour", Color.Black);
					handover.setDisplayText(Handover);
					break;
				case AttrName_Stage_READY:
					design.Status = TransitionStatus.WORKING;
					build.Status = TransitionStatus.WORKING;
					testing.Status = TransitionStatus.WORKING;
					handover.Status = TransitionStatus.HANDOVER_DONE;
					golive.Status = TransitionStatus.READY;
					handover.setDisplayText(Handover);
					break;
				default:
					golive.Status = TransitionStatus.NOTREADY;
					break;
			}
		}

		//Common Handler for handling which stage we want to display 
		void HandleStageDaysToGo(int Daycount)
		{
			switch (stage)
			{
				case AttrName_Stage_DEFINITION:
					break;
				case AttrName_Stage_DESIGN:
					design.Days = Daycount;
					break;
				case AttrName_Stage_BUILD:
					build.Days = Daycount;
					break;
				case AttrName_Stage_TEST :
					testing.Days = Daycount;
					break;
				case AttrName_Stage_HANDOVER:
					break;
			}
		}

		void NodeAttributesChanged(Node sender, ArrayList attrs)
		{
			bool refreshRequired = false;

			if (attrs != null && attrs.Count > 0)
			{
				foreach(AttributeValuePair avp in attrs)
				{
					//Extraction of the data attribute
					string attribute = avp.Attribute;
					string newValue = avp.Value;
					//Do the work
					switch (attribute.ToLower())
					{
						case AttrName_Stage:	
							HandleStageWork();
							refreshRequired = true;
							break;
						case AttrName_WRequest:
							refreshRequired = true;
							break;
						case AttrName_wCount:	
							refreshRequired = true;
							break;
						case AttrName_StageDaystoGo:
							if (newValue != "")
							{
								HandleStageDaysToGo(wdays);
								refreshRequired = true;
							}
							break;
						case AttrName_OverTime:
							//Don't Care
							break;
						case AttrName_HandoverDisplay:
							handover.setDisplayText(Handover);
							refreshRequired = true;
							break;
						case AttrName_ReadyForDeployment:
							golive.setDisplayText(GoLive);
							refreshRequired = true;
							break;
						case AttrName_ProjectDisplayName:
							updateProjectTitle();
							refreshRequired = true;
							break;
						case AttrName_ProductDisplayName:
							updateProjectTitle();
							refreshRequired = true;
							break;
						case AttrName_PlatformDisplayName:
							updateProjectTitle();
							refreshRequired = true;
							break;
						case AttrName_ReadyDayValue:
							golive.Days = ReadyDays;
							refreshRequired = true;
							break;
						case AttrName_ActualCost:
							cost.Actual = ActualCost;
							refreshRequired = true;
							break;
						case AttrName_CurrentSpend:
							spend.Actual = CurrentSpend;
							refreshRequired = true;
							break;
						case AttrName_Location:
							install.setDisplayFont(NormalDisplayFont);
							install.setDisplayText(location.ToUpper());
							install.ForeColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("project_progress_panel_install_location_colour", Color.Black);
							refreshRequired = true;
							break;
						case AttrName_FailReason:
							install.setDisplayFont(SmallDisplayFont);
							install.setDisplayText(reason);
							install.ForeColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("project_progress_panel_install_fail_colour", Color.Black);
							refreshRequired = true;
							break;
					}
				}
			}

			if (refreshRequired)
			{
				//Refresh();
				Invalidate();
			}
		}	
	}
}