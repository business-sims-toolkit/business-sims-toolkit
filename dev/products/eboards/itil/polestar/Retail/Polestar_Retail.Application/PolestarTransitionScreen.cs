using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;
using CommonGUI;
using CoreUtils;
using LibCore;
using Network;
using ResizingUi;
using TransitionScreens;
using Algorithms;
using GameEngine;

namespace Polestar_Retail.Application
{
	public class PolestarTransitionScreen : TransitionScreen, ITimedClass
	{
		ControlBar controlBar;

		ImageTextButton addSipButton;
		ImageTextButton cancelSipButton;
		ImageTextButton installSipButton;
		ImageTextButton setMtrsButton;
		ImageTextButton upgradeAppButton;
		ImageTextButton upgradeServerButton;
		ImageTextButton mirrorButton;

		Control popup;
		IWatermarker watermarker;

		RoundTimeViewPanel clockPanel;

		Node projectsNode;
		Node calendarNode;

		RectangleF calendarTitle;

		ResizingServiceCatalogueViewer serviceCatalogue;

		CompleteGamePanel parent;
		
		public PolestarTransitionScreen (GameManagement.NetworkProgressionGameFile gameFile, string dataDirectory, ControlBar controlBar, CompleteGamePanel parent)
			: base (gameFile, dataDirectory)
		{
			this.parent = parent;
			this.controlBar = controlBar;

			if (gameFile.IsTrainingGame)
			{
				watermarker = new Watermarker (Color.FromArgb(255, 153, 0), Color.White, new Point(0, 0), Math.PI / 4, 80, 200, 750, "TRAINING MODE: NOT FOR COMMERCIAL USE", "For facilitator's personal use only");
			}
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing)
			{
				projectsNode.ChildAdded -= projectsNode_ChildAdded;
				projectsNode.ChildRemoved -= projectsNode_ChildRemoved;
				foreach (Node project in projectsNode.getChildren())
				{
					project.AttributesChanged -= project_AttributesChanged;
				}

				calendarNode.ChildAdded -= calendarNode_ChildAdded;
				calendarNode.ChildRemoved -= calendarNode_ChildRemoved;

				serviceCatalogue.Dispose();

				clockPanel.Dispose();
			}

			base.Dispose(disposing);
		}

		void BuildControlBar ()
		{
			controlBar.IncidentApplier = MyTransPhaseEngine.TheIncidentApplier;

			addSipButton = controlBar.AddButton("Add Product",
				() => ShowPopup(new StartSIP (controlBar, MyNodeTreeRoot, gameFile.CurrentRound, Color.White) { BackColor = Color.White }));

			cancelSipButton = controlBar.AddButton("Cancel",
				() => ShowPopup(new CancelSIP (controlBar, MyNodeTreeRoot, gameFile.CurrentRound, Color.White) { BackColor = Color.White }));

			installSipButton = controlBar.AddButton("Install",
				() => ShowPopup(new InstallSIP (controlBar, MyNodeTreeRoot, gameFile.CurrentRound, Color.White)
					{ BackColor = Color.White }));

			setMtrsButton = controlBar.AddButton("MTRS",
				() => ShowPopup(new DefineImpactBasedSLA (controlBar, MyNodeTreeRoot, Color.White, gameFile.CurrentRound)
					{ BackColor = Color.White }));

			upgradeAppButton = controlBar.AddButton("Upgrade App",
				() => ShowPopup(new TransUpgradeAppControl (controlBar, MyTransPhaseEngine.TheIncidentApplier, MyNodeTreeRoot, false, Color.White, Color.White)
					{ BackColor = Color.White }));

			upgradeServerButton = controlBar.AddButton("Upgrade Server",
				() => ShowPopup(new TransUpgradeMemDiskControl (controlBar, MyNodeTreeRoot, false, MyTransPhaseEngine.TheIncidentApplier, Color.White, Color.White)
					{ BackColor = Color.White }));

			mirrorButton = controlBar.AddButton("Mirror",
				() => ShowPopup(new TransAddOrRemoveMirrorControl (controlBar, MyNodeTreeRoot, MyTransPhaseEngine.TheMirrorApplier, Color.White, Color.White)
					{ BackColor = Color.White }));
		}

		protected override ServicePortfolioViewer CreateServicePortfolioViewer()
		{
			ServicePortfolioViewer spv = new ServicePortfolioViewer(MyNodeTreeRoot, Color.Transparent);
			spv.EnableSelfDrawTitle(true);
			return spv;
		}

		public override bool BuildObjects(int round, bool logData)
		{
			Color baseBackColor = SkinningDefs.TheInstance.GetColorData("transition_basebackcolor", Color.White);
			BackColor = baseBackColor;

			MyTransPhaseEngine = new TransitionPhaseEngine (MyNodeTreeRoot, RndDataDir, AppDataDir + "\\SIPS\\", round, logData, TrainingGameFlag);
			MyTransPhaseEngine.PhaseFinished += MyTransPhaseEngine_PhaseFinished;

			projectsNode = MyNodeTreeRoot.GetNamedNode("projects");
			projectsNode.ChildAdded += projectsNode_ChildAdded;
			projectsNode.ChildRemoved += projectsNode_ChildRemoved;
			foreach (Node project in projectsNode.getChildren())
			{
				project.AttributesChanged += project_AttributesChanged;
			}

			clockPanel = new RoundTimeViewPanel (gameFile);
			parent.AddGameScreenControl(clockPanel);

			calendarNode = MyNodeTreeRoot.GetNamedNode("Calendar");
			calendarNode.ChildAdded += calendarNode_ChildAdded;
			calendarNode.ChildRemoved += calendarNode_ChildRemoved;

			BuildControlBar();
			UpdateControlPanelButtonsState();

			projects = new ResizingProjectsViewer (MyNodeTreeRoot, round, 2);
			Controls.Add(projects);

			serviceCatalogue = new ResizingServiceCatalogueViewer (MyNodeTreeRoot.GetNamedNode("Business Services Group"), MyNodeTreeRoot.GetNamedNode("Retired Business Services"));
			Controls.Add(serviceCatalogue);

			BusinessNewsViewer = new BusinessNewsViewer(MyNodeTreeRoot);
		    BusinessNewsViewer.SetTrainingMode(TrainingGameFlag);
		    BusinessNewsViewer.SetFlashPos(0, 0, BusinessNewsViewer.Width, BusinessNewsViewer.Height);
		    BusinessNewsViewer.EnableSelfDrawTitle(true);
		    BusinessNewsViewer.HideTitle();
		    Controls.Add(BusinessNewsViewer);

			WorkScheduleView = new WorkScheduleViewer(MyNodeTreeRoot) { Watermarker = watermarker };
			Controls.Add(WorkScheduleView);
			WorkScheduleView.SetCalendarRowsAndCols(5, 5);
			WorkScheduleView.CalendarOffsetTopY = 25;
			WorkScheduleView.CalendarOffsetBottomY = 5;

			messenger = new FacilitatorErrorMessenger (this, MyNodeTreeRoot);

			return true;
		}

		protected override void BuildProjectsViewer (int round)
		{
		}

		void calendarNode_ChildRemoved (Node sender, Node child)
		{
			UpdateControlPanelButtonsState();
		}

		void calendarNode_ChildAdded (Node sender, Node child)
		{
			UpdateControlPanelButtonsState();
		}

		void projectsNode_ChildRemoved (Node sender, Node child)
		{
			child.AttributesChanged -= project_AttributesChanged;
			UpdateControlPanelButtonsState();
		}

		void projectsNode_ChildAdded (Node sender, Node child)
		{
			child.AttributesChanged += project_AttributesChanged;
			UpdateControlPanelButtonsState();
		}

		void project_AttributesChanged (Node sender, ArrayList attributes)
		{
			UpdateControlPanelButtonsState();
		}

		Control ShowPopup (Control popup)
		{
			controlBar.DisposeEntryPanel();

			this.popup = popup;
			Controls.Add(popup);
			popup.BringToFront();
			DoSize();

			return popup;
		}

		protected override void DoSize ()
		{
			projects.Bounds = new RectangleFromBounds
			{
				Left = 0,
				Top = 0,
				Right = Width / 2,
				Height = Height / 3
			}.ToRectangle();

			serviceCatalogue.Bounds = new RectangleFromBounds
			{
				Left = 0,
				Top = projects.Bottom + (Height / 40),
				Right = projects.Right,
				Bottom = Height
			}.ToRectangle();

			BusinessNewsViewer.Bounds = new RectangleFromBounds
			{
				Left = projects.Right,
				Top = 0,
				Right = Width,
				Height = Height / 2
			}.ToRectangle();

			calendarTitle = new RectangleFFromBounds
			{
				Left = BusinessNewsViewer.Left,
				Top = BusinessNewsViewer.Bottom,
				Right = BusinessNewsViewer.Right,
				Height = (Height - BusinessNewsViewer.Bottom) / 12
			}.ToRectangleF();

			WorkScheduleView.Bounds = new RectangleFromBounds
			{
				Left = BusinessNewsViewer.Left,
				Right = BusinessNewsViewer.Right,
				Top = (int) calendarTitle.Bottom,
				Bottom = Height
			}.ToRectangle();

			if (popup != null)
			{
				popup.Bounds = new RectangleFromBounds
				{
					Left = 0,
					Top = WorkScheduleView.Top,
					Right = WorkScheduleView.Left,
					Bottom = Height
				}.ToRectangle();
			}

			var clockWidth = Width / 3;
			var topBarHeight = Top;
			var clockBounds = new Rectangle ((Width - clockWidth) / 2, -topBarHeight, clockWidth, topBarHeight);
			clockPanel.Bounds = clockBounds.Map(this, Parent);
		}

		protected override void OnPaint (PaintEventArgs e)
		{
			base.OnPaint(e);

			watermarker?.Draw(this, e.Graphics);

			var calendarTitleText = "Change Schedule";
			using (Font font = this.GetFontToFit(FontStyle.Regular, calendarTitleText, calendarTitle.Size))
			{
				e.Graphics.DrawString(calendarTitleText, font, Brushes.LightGray, calendarTitle, new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Near });
			}
		}

		public override void Start ()
		{
			base.Start();
			UpdateControlPanelButtonsState();
		}

		public override void Stop ()
		{
			base.Stop();
			UpdateControlPanelButtonsState();
		}

		public override void Reset ()
		{
			base.Reset();
			UpdateControlPanelButtonsState();
		}

		void UpdateControlPanelButtonsState ()
		{
			if (controlBar == null)
			{
				return;
			}

			bool running = TimeManager.TheInstance.TimeIsRunning;

			int totalProjects = 0;
			int projectsNotInstalled = 0;
			foreach (Node project in projectsNode.getChildren())
			{
				if (project.GetIntAttribute("createdinround", 0) == gameFile.CurrentRound)
				{
					totalProjects++;

					switch (project.GetAttribute("stage"))
					{
						case "installed_ok":
							break;

						default:
							projectsNotInstalled++;
							break;
					}
				}
			}

			int pendingUpgrades = 0;
			int pendingInstalls = 0;
			int currentDay = MyNodeTreeRoot.GetNamedNode("CurrentDay").GetIntAttribute("day", 0);
			foreach (Node day in calendarNode.getChildren())
			{
				if (day.GetIntAttribute("day", 0) > currentDay)
				{
					switch (day.GetAttribute("type"))
					{
						case "Install":
							pendingInstalls++;
							break;

						case "external":
							break;

						default:
							pendingUpgrades++;
							break;
					}
				}
			}

			addSipButton.Enabled = running && (totalProjects < SkinningDefs.TheInstance.GetIntData("transition_projects_count", 4));
			cancelSipButton.Enabled = running && ((projectsNotInstalled + pendingInstalls) > 0);
			installSipButton.Enabled = running && (projectsNotInstalled > pendingInstalls);
			setMtrsButton.Enabled = true;
			upgradeAppButton.Enabled = running;
			upgradeServerButton.Enabled = running;
			mirrorButton.Enabled = running;
		}
	}
}