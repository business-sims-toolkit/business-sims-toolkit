using System;
using System.Drawing;
using System.Collections;

using LibCore;
using Network;
using Logging;
using GameEngine;
using IncidentManagement;
using TransitionObjects;
using TransitionScreens;
using CoreUtils;
using CommonGUI;
using Media;

namespace Polestar_PM.TransScreen
{
	public class MSTransitionScreen : TransitionScreen
	{
		MS_PipelineDisplay pipelineDisp = null;
		TitlePanel ServiceCatTitle = null;
		TitlePanel ServicePipelineTitle = null;
		MS_PipelineKeyDisplay PipeKeyDisplay = null;

		public MSTransitionScreen(GameManagement.NetworkProgressionGameFile gameFile, string DataDirectory)
			: base(gameFile, DataDirectory)
		{
			this.Height = 728;
		}

		protected override void Dispose(bool disposing) 
		{
			if(disposing)
			{
				if (pipelineDisp != null)
				{
					pipelineDisp.Dispose();
					pipelineDisp = null;
				}
				if (ServiceCatTitle != null)
				{
					ServiceCatTitle.Dispose();
					ServiceCatTitle =null;
				}
				if (ServicePipelineTitle != null)
				{
					ServicePipelineTitle.Dispose();
					ServicePipelineTitle =null;
				}
				if (PipeKeyDisplay != null)
				{
					PipeKeyDisplay.Dispose();
					PipeKeyDisplay = null;
				}
					//				if (PoweredByImagePanel != null)
					//				{
					//					this.Parent.Controls.Remove(PoweredByImagePanel);
					//					PoweredByImagePanel.Dispose();
					//					PoweredByImagePanel = null;
					//				}
				}
			base.Dispose(disposing);
		}

		public override bool BuildObjects(int round, bool logData)
		{
			bool ret = true;
			//TOTAL Overide of the 
			//bool ret = base.BuildObjects(round, logData);

			this.SuspendLayout();

			//Build the Transition Colors
			Color OperationsBackColor = CoreUtils.SkinningDefs.TheInstance.GetColorData("transition_operationsbackcolor");
			Color GroupPanelBackColor = CoreUtils.SkinningDefs.TheInstance.GetColorData("transition_groupbackcolor");
			Color baseBackColor = SkinningDefs.TheInstance.GetColorData("transition_basebackcolor");

			ArrayList testAttrs = new ArrayList();
			string strInstallAction = string.Empty;

			//Build the Repository Objects 
			ProjectSIP_Repository MySIPRepository = ProjectSIP_Repository.TheInstance;

			MyTransPhaseEngine = new TransitionPhaseEngine(MyNodeTreeRoot,RndDataDir,AppDataDir+"\\SIPS\\", round, logData, this.TrainingGameFlag);
			MyTransPhaseEngine.PhaseFinished += MyTransPhaseEngine_PhaseFinished;

			pipelineDisp = new MS_PipelineDisplay(MyNodeTreeRoot, round, TrainingGameFlag);
			pipelineDisp.Size = new Size(650,650);
			pipelineDisp.Location = new Point(1023-650,0+40);
			this.Controls.Add(pipelineDisp);
			
			//BuildProjectsViewer(round);
			//CurrentServiceView = new ServicePortfolioViewer(MyNodeTreeRoot, panelBackColor);
			CurrentServiceView = new MS_PortfolioViewer(MyNodeTreeRoot);
			CurrentServiceView.SetNewSMISize(117,34);			
			CurrentServiceView.SetNewLayoutOffsetsAndSeperations(7,36+9,3,7,10);
			CurrentServiceView.Location = new Point(0,0);
			CurrentServiceView.Size = new Size(1024,726);
			CurrentServiceView.SetTrainingMode(TrainingGameFlag);
			//CurrentServiceView.SetNewColumns(3);
			CurrentServiceView.ReBuildAll();
			//CurrentServiceView.BackColor = Color.Thistle;
			this.Controls.Add(CurrentServiceView);

			tcp = new MS_TransitionControlPanel(this,MyNodeTreeRoot,MyTransPhaseEngine.TheIncidentApplier,
				MyTransPhaseEngine.TheMirrorApplier, round, OperationsBackColor, GroupPanelBackColor);
			tcp.Location = new Point(0,0+650+40+4);
			tcp.Size = new Size(540,278);
			tcp.RePositionButtons (66, 27, 7);
			tcp.SetPopUpPosition(0,280+65);
			tcp.SetPopUpSize(372,336);
			tcp.PanelStatusChange +=new TransitionScreens.TransitionControlPanel.OperationPanelStatusHandler(tcp_PanelStatusChange);
			this.CurrentServiceView.Controls.Add(tcp);

			PipeKeyDisplay = new MS_PipelineKeyDisplay(false);
			PipeKeyDisplay.Location = new Point(10,450);
			PipeKeyDisplay.Size = new Size(360,230);
			this.CurrentServiceView.Controls.Add(PipeKeyDisplay);

			this.BuildLogoPanel(baseBackColor);

			ServiceCatTitle = new TitlePanel("SERVICE PORTFOLIO SERVICE CATALOG");
			ServiceCatTitle.Location = new Point(8,4);
			ServiceCatTitle.Size = new Size(350,22);
			this.Controls.Add (ServiceCatTitle);

			ServicePipelineTitle =  new TitlePanel("SERVICE PORTFOLIO SERVICE PIPELINE");
			ServicePipelineTitle.Location = new Point(500+10,4);
			ServicePipelineTitle.Size = new Size(355,22);
			this.Controls.Add (ServicePipelineTitle);


			this.ResumeLayout(false);

			focusJumper = new FocusJumper();
			focusJumper.Add(tcp.startSIPButton);
			focusJumper.Add(tcp.cancelSIPButton);
			focusJumper.Add(tcp.installSIPButton);
			focusJumper.Add(tcp.slaButton);
			focusJumper.Add(tcp.upgradeAppButton);
			focusJumper.Add(tcp.upgradeMemDiskButton);
			focusJumper.Add(tcp.addMirrorButton);
	
			messenger = new FacilitatorErrorMessenger (this, this.MyNodeTreeRoot);

			this.BackColor = baseBackColor;

			//this attually does stuff 
			CurrentView = ViewScreen.ACTIVE;
			//
			//BusinessNewsViewer.Visible = true;
			//CurrentServiceView.Visible = true;
			//projects.Visible = false;
			//CurrentServiceView.Location = new Point(2500,0);
			//LogoPanel.Visible = true;
			//

			if (LogoPanel != null)
			{
				LogoPanel.BringToFront();
			}
			if (ServiceCatTitle != null)
			{
				ServiceCatTitle.BringToFront();
			}
			if (ServicePipelineTitle  != null)
			{
				ServicePipelineTitle.BringToFront();
			}


			return ret;
		}

		protected override void BuildLogoPanel(Color baseBackColor)
		{
			LogoPanel = new LogoPanel_MS();
			LogoPanel.Location = new Point(370-364+1,500-152+15-13);
			LogoPanel.Size = new Size(348,90);
			LogoPanel.SetImageDir(ImgDir);
			LogoPanel.SetTrainingMode(TrainingGameFlag);
			LogoPanel.BackColor = Color.Transparent;
			//LogoPanel.BackColor = Color.Plum;

			if (CurrentServiceView != null)
			{
				CurrentServiceView.Controls.Add(LogoPanel);
			}
			//this.Controls.Add(LogoPanel);
			LogoPanel.BringToFront();
		}

		protected override void BuildProjectsViewer(int round)
		{
			base.BuildProjectsViewer(round);
			//This needs a new Cirlaur 
			//projects = (ProjectsViewer) new PlannerProjectsViewer(MyNodeTreeRoot, round, 2);
			//projects.Location = new Point(10-5,10-5);
			//projects.SetTrainingMode(TrainingGameFlag);
			//this.Controls.Add(projects);
		}
	}
}
