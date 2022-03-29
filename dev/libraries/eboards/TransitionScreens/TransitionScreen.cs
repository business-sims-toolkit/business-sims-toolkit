using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;
using LibCore;
using Network;
using GameEngine;
using TransitionObjects;
using GameManagement;

using CoreUtils;
using CommonGUI;

namespace TransitionScreens
{
	/// <summary>
	/// The TransitionScreen is the screen that is shown for the transition phase between
	/// the races / ops phases.
	/// </summary>
	public class TransitionScreen : FlickerFreePanel, ITimedClass
	{
		public delegate void ActionHandler(object sender);

		bool phaseFinished;

		//Environment Variables 
		protected string AppExeDir = string.Empty;
		protected string AppDataDir = string.Empty;
		protected string ImgDir = string.Empty;
		protected string RndDataDir = string.Empty;
		protected int Round = 1;
		protected bool started = false;

		public enum ViewScreen 
		{ 
			ACTIVE,
			INACTIVE
		}

		protected ViewScreen _CurrentView;
		public ViewScreen CurrentView
		{
			get { return _CurrentView; }
			set 
			{ 
				CurrentServiceView.DoLayoutBoth();	

				//REFACTOR CurrentServiceView has it's own view button 
				if (value == ViewScreen.ACTIVE) 
				{
					CurrentServiceView.SetShowRetiredServices(false);
				}
				else
				{
					CurrentServiceView.SetShowRetiredServices(true);
				}
				_CurrentView = value;
				
				if (tcp?.definesla != null)
				{
					tcp.definesla.LoadDataDisplay();
					tcp.definesla.DoLayout();
				}
			}
		}

		public void ImportIncidents(string incidentsFile)
		{
			//EventDelayer.TheInstance.AddModelCounter( MyNodeTreeRoot, "CurrentTime", "seconds" );
			//
			//EventDelayer.TheInstance.SetCurrentExtendedModelCounter( this.MyNodeTreeRoot.GetNamedNode("CurrentTime") );
			System.IO.StreamReader file = new System.IO.StreamReader(incidentsFile);
			string xmldata = file.ReadToEnd();
			file.Close();
			file = null;
			MyTransPhaseEngine.TheIncidentApplier.SetIncidentDefinitions(xmldata,MyNodeTreeRoot);
			//EventDelayer.TheInstance.SetCurrentExtendedModelCounter(null);
		}

		//Engine components 
		protected TransitionPhaseEngine MyTransPhaseEngine = null;
		protected NodeTree MyNodeTreeRoot = null;
		protected FacilitatorErrorMessenger messenger;

		public event PhaseFinishedHandler PhaseFinished;

		//UI components 
		protected ProjectsViewer projects = null;
		protected BusinessNewsViewer BusinessNewsViewer = null;
		protected ServicePortfolioViewer CurrentServiceView = null;
		protected WorkScheduleViewer WorkScheduleView = null;
		protected LogoPanelBase LogoPanel = null;
		public TransitionControlPanel tcp = null;
		protected bool TrainingGameFlag;
		
		//better handling of focus 
		protected FocusJumper focusJumper;

		protected NetworkProgressionGameFile gameFile;

		/// <summary>
		/// Constructor 
		/// </summary>
		public TransitionScreen (NetworkProgressionGameFile gameFile, string DataDirectory)
		{
			this.gameFile = gameFile;
			SetStyle(ControlStyles.Selectable, true);
			TrainingGameFlag = gameFile.IsTrainingGame;
			SetObjects(gameFile.NetworkModel, gameFile.CurrentRound, DataDirectory, gameFile.CurrentRoundDir, gameFile.Dir);

			TimeManager.TheInstance.ManageClass(this);

			phaseFinished = false;

			GotFocus += TransitionScreen_GotFocus;
		}

		public void SetupIncidents()
		{
			MyTransPhaseEngine.SetupIncidents();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="rt"></param>
		/// <param name="tmpRound"></param>
		/// <param name="DataDirectory"></param>
		/// <param name="RoundDirectory"></param>
		protected void SetObjects(NodeTree rt, int tmpRound, string DataDirectory, string RoundDirectory, string ImgDir)
		{
			MyNodeTreeRoot = rt;
			Round = tmpRound;
			this.ImgDir = ImgDir;
			AppExeDir = AppInfo.TheInstance.Location;
			AppDataDir = DataDirectory;
			RndDataDir = RoundDirectory;
		}

		protected virtual void BuildLogoPanel(Color baseBackColor)
		{
			//there is a better method than this but time is very short
			switch (SkinningDefs.TheInstance.GetData("skinname"))
			{
				case "HP_OBI_SM":
					LogoPanel = new LogoPanel_PS();
					break;

				case "HP_RTR_SM":
					LogoPanel = new LogoPanel_HP();
					break;

				case "PoleStar":
					LogoPanel = new LogoPanel_PS();
					break;

				case "Reckitt":
					LogoPanel = new LogoPanel_RB();
					break;

				default:
					//Create a null transparent version 
					LogoPanel = new LogoPanelBase();
					LogoPanel.BackColor = Color.Transparent;
					break;
			}
			LogoPanel.Location = new Point(12-7,574);
			LogoPanel.Size = new Size(560,100);
			LogoPanel.SetImageDir(ImgDir);
			LogoPanel.SetTrainingMode(TrainingGameFlag);
			LogoPanel.BackColor = baseBackColor;
			Controls.Add(LogoPanel);
		}

		protected virtual void BuildProjectsViewer(int round)
		{
			projects = new ProjectsViewer(MyNodeTreeRoot, round);
			projects.Location = new Point(10-5,10-5);
			projects.SetTrainingMode(TrainingGameFlag);
			Controls.Add(projects);
		}

		protected virtual ServicePortfolioViewer CreateServicePortfolioViewer()
		{
			return new ServicePortfolioViewer(MyNodeTreeRoot, Color.Transparent);
		}

		/// <summary>
		/// Build all the objects needs for the Transition Screen 
		/// </summary>
		/// <returns></returns>
		public virtual bool BuildObjects(int round, bool logData)
		{
			SuspendLayout();

			//Build the Transition Colors
			Color OperationsBackColor = SkinningDefs.TheInstance.GetColorData("transition_basebackgroundcolor");
			Color GroupPanelBackColor = SkinningDefs.TheInstance.GetColorData("transition_groupbackcolor");
			Color baseBackColor = SkinningDefs.TheInstance.GetColorData("transition_basebackcolor");

			ArrayList testAttrs = new ArrayList();
			string strInstallAction = string.Empty;

			//Build the Repository Objects 
			ProjectSIP_Repository MySIPRepository = ProjectSIP_Repository.TheInstance;

			MyTransPhaseEngine = CreateEngine(MyNodeTreeRoot, RndDataDir, AppDataDir + "\\SIPS\\", round, logData, TrainingGameFlag);

			BusinessNewsViewer = new BusinessNewsViewer(MyNodeTreeRoot);
			BusinessNewsViewer.Location = new Point(578,10-7);
			BusinessNewsViewer.SetTrainingMode(TrainingGameFlag);
			Controls.Add(BusinessNewsViewer);

			BuildProjectsViewer(round);

			//CurrentServiceView = new ServicePortfolioViewer(MyNodeTreeRoot, panelBackColor);
			CurrentServiceView = CreateServicePortfolioViewer();

			CurrentServiceView.Location = new Point(10-5,285);
			CurrentServiceView.SetTrainingMode(TrainingGameFlag);
			CurrentServiceView.ReBuildAll();
			//CurrentServiceView.BackColor = Color.Thistle;
			CurrentServiceView.SendToBack();
			Controls.Add(CurrentServiceView);

			BuildTransitionControlPanel(GroupPanelBackColor, OperationsBackColor, round);

			BuildWorkScheduleView(MyNodeTreeRoot, TrainingGameFlag);

			BuildLogoPanel(baseBackColor);

			ResumeLayout(false);

			focusJumper = new FocusJumper();
			focusJumper.Add(tcp.startSIPButton);
			focusJumper.Add(tcp.cancelSIPButton);
			focusJumper.Add(tcp.installSIPButton);
			focusJumper.Add(tcp.slaButton);
			focusJumper.Add(tcp.upgradeAppButton);
			focusJumper.Add(tcp.upgradeMemDiskButton);
			focusJumper.Add(tcp.addMirrorButton);
	
			messenger = new FacilitatorErrorMessenger(this, MyNodeTreeRoot);

			BackColor = baseBackColor;

			CurrentServiceView.BringToFront();

			//this attually does stuff 
			CurrentView = ViewScreen.ACTIVE;

			return true;
		}
		
		/// <summary>
		/// Dispose ...
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose(bool disposing)
		{
			if(disposing)
			{
				TimeManager.TheInstance.Stop();
				TimeManager.TheInstance.UnmanageClass(this);

				if(focusJumper != null)
				{
					focusJumper.Dispose();
				}
				if (messenger != null)
				{
					messenger.Dispose();
				}
				if (tcp != null)
				{
					tcp.Dispose();
				}
				if (MyTransPhaseEngine != null)
				{
					MyTransPhaseEngine.Dispose();
				}
				if (LogoPanel != null)
				{
					LogoPanel.Dispose();
				}
				if (BusinessNewsViewer != null)
				{
					BusinessNewsViewer.Dispose();
				}
				if (projects != null)
				{
					projects.Dispose();
				}
				if (CurrentServiceView != null)
				{
					CurrentServiceView.Dispose();
				}
			}
			base.Dispose (disposing);
		}

		protected void MyTransPhaseEngine_PhaseFinished(object sender)
		{
			phaseFinished = true;
            KlaxonSingleton.TheInstance.PlayAudio(AppInfo.TheInstance.Location + "\\audio\\end.wav", false);
			// Phase has finished so move on...
			if(null != PhaseFinished)
			{
				PhaseFinished(this);
			}
		}

		protected void MyTransPhaseEngine_PhaseStarted ()
		{
			// Fix for 3877 (play CA bell on round start as well as end).
			string name = SkinningDefs.TheInstance.GetData("sound_on_round_start");

			if (name != "")
			{
                KlaxonSingleton.TheInstance.PlayAudio( AppInfo.TheInstance.Location + "\\audio\\" + name, false);
			}
		}


		#region ITimedClass Members

		/// <summary>
		/// Game fast forward
		/// </summary>
		/// <param name="timesRealTime"></param>
		public virtual void FastForward(double timesRealTime)
		{
			// TODO:  Add TransitionScreen.CoreUtils.ITimedClass.FastForward implementation
		}

		/// <summary>
		/// Game round reset
		/// </summary>
		public virtual void Reset()
		{
			// TODO:  Add TransitionScreen.Reset implementation
		}

		/// <summary>
		/// Game start
		/// </summary>
		public virtual void Start()
		{
			if(null != tcp)
			{
				tcp.startSIPButton.Focus();
			}

			if (! started)
			{
				MyTransPhaseEngine_PhaseStarted();
				started = true;
			}
		}

		/// <summary>
		/// Game stop
		/// </summary>
		public virtual void Stop()
		{
		}

		#endregion

		void TransitionScreen_GotFocus(object sender, EventArgs e)
		{
			if(tcp != null)
			{
				tcp.startSIPButton.Focus();
			}
		}


		protected void tcp_PanelStatusChange(Boolean IsOpsPanelChange)
		{
			CurrentServiceView.SetNewSMIVisibility(IsOpsPanelChange);
		}

		protected virtual void BuildWorkScheduleView (NodeTree tree, bool TrainingGameFlag)
		{
			WorkScheduleView = new WorkScheduleViewer(MyNodeTreeRoot);
			WorkScheduleView.Location = new Point(578,245-7);
			WorkScheduleView.Size = new Size(429,435);
			Controls.Add(WorkScheduleView);
			WorkScheduleView.SetTrainingMode(TrainingGameFlag);
		}

		protected virtual void BuildTransitionControlPanel (Color GroupPanelBackColor, Color OperationsBackColor, int round)
		{
			tcp = new TransitionControlPanel(this,MyNodeTreeRoot,MyTransPhaseEngine.TheIncidentApplier,
				MyTransPhaseEngine.TheMirrorApplier, round, OperationsBackColor, GroupPanelBackColor);
			tcp.Location = new Point(5,0+240);
			tcp.Size = new Size(540,278);
			tcp.PanelStatusChange +=tcp_PanelStatusChange;
			tcp.Name = "Transition Facilitator Control Panel";
			CurrentServiceView.Controls.Add(tcp);
		}

		protected virtual TransitionPhaseEngine CreateEngine (NodeTree model, string roundDir, string sipDir, int round, bool logResults, bool isTrainingGame)
		{
			TransitionPhaseEngine engine = new TransitionPhaseEngine (model, roundDir, sipDir, round, logResults, isTrainingGame);
			engine.PhaseFinished += MyTransPhaseEngine_PhaseFinished;
			return engine;
		}

		public virtual void Play ()
		{
			if (MyNodeTreeRoot.GetNamedNode("CurrentTime").GetIntAttribute("seconds", -1) == 0)
			{
				gameFile.PlayNow(gameFile.CurrentRound, gameFile.CurrentPhase);
			}

			TimeManager.TheInstance.Start();
		}

		public bool IsGameFinished ()
		{
			return phaseFinished;
		}

	    public static void DrawSectionTitle (Control control, Graphics graphics, string text, Font font, Brush brush, Point defaultPosition)
	    {
	        var lozengeColour = SkinningDefs.TheInstance.GetColorDataGivenDefault("transition_title_lozenge_colour", Color.Empty);
	        if (lozengeColour != Color.Empty)
	        {
                var size = graphics.MeasureString(text, font);
                var rectangle = new RectangleF((float) ((control.Width - size.Width) / 2), 0, size.Width, (float) (size.Height * 1.3));

	            if (SkinningDefs.TheInstance.GetBoolData("transition_title_lozenges_full_width", false))
	            {
	                rectangle = new RectangleF (0, 0, control.Width, (float) (size.Height * 1.3));
	            }

	            using (var backgroundBrush = new SolidBrush(lozengeColour))
	            {
                    graphics.FillRectangle(backgroundBrush, rectangle);

	                if (SkinningDefs.TheInstance.GetBoolData("transition_title_rounded", true))
	                {
	                    graphics.FillEllipse(backgroundBrush, rectangle.Left - (rectangle.Height / 2), rectangle.Top, rectangle.Height, rectangle.Height);
	                    graphics.FillEllipse(backgroundBrush, rectangle.Right - (rectangle.Height / 2), rectangle.Top, rectangle.Height, rectangle.Height);
	                }
	                else
	                {
	                    graphics.FillRectangle(backgroundBrush, rectangle.Left - (rectangle.Height / 2), rectangle.Top, rectangle.Height, rectangle.Height);
                        graphics.FillRectangle(backgroundBrush, rectangle.Right - (rectangle.Height / 2), rectangle.Top, rectangle.Height, rectangle.Height);	                    
	                }
	            }

	            int verticalFudge = 2;
	            rectangle.Offset(0, verticalFudge);

	            graphics.DrawString(text, font, brush, rectangle,
	                         new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
	        }
	        else
	        {
	            graphics.DrawString(text, font, brush, defaultPosition);
	        }
	    }

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);

			DoSize();
			Invalidate();
		}

		protected virtual void DoSize ()
		{ 
			BusinessNewsViewer.Location = new Point (Width - BusinessNewsViewer.Width, BusinessNewsViewer.Top);
			WorkScheduleView.Location = new Point (Width - WorkScheduleView.Width, WorkScheduleView.Top);
			LogoPanel.Location = new Point (Width - LogoPanel.Width, LogoPanel.Top);
		}

		public void DisableUserInteraction ()
		{
			messenger.IsInteractionDisabled = true;
		}
	}
}