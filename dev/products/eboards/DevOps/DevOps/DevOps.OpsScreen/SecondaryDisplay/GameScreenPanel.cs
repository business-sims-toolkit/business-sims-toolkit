using System;
using System.Drawing;
using System.Windows.Forms;

using Algorithms;
using CommonGUI;
using Events;
using DevOps.OpsEngine;
using GameManagement;
using LibCore;
using Network;
using ResizingUi;

namespace DevOps.OpsScreen.SecondaryDisplay
{
    internal class GameScreenPanel : FlickerFreePanel
    {
        public GameScreenPanel (NetworkProgressionGameFile gameFile, DevOpsQuadStatusLozengeGroup incidentPanel, TradingOpsEngine opsEngine, bool isTrainingGame)
        {
            this.incidentPanel = incidentPanel;
            this.opsEngine = opsEngine;

            opsEngine.PhaseFinished += opsEngine_PhaseFinished;

            if (isTrainingGame)
            {
                watermarker = new Watermarker(Color.FromArgb(255, 153, 0), Color.White, new Point(0, 0),
                    Math.PI / 4, 60, 240, 750,
                    "TRAINING MODE: NOT FOR COMMERCIAL USE", "For facilitator's personal use only");
            }

            newServicesDisplay = new NewServicesDisplay(gameFile.NetworkModel, opsEngine.TheRequestsApplier,
                gameFile.CurrentRound)
            {
                NumServicesHorizontally = 6,
                Watermarker = watermarker
            };
            Controls.Add(newServicesDisplay);
            newServicesDisplay.ServiceIconClicked += newServicesDisplay_ServiceIconClicked;
			newServicesDisplay.NumberOfServicesChanged += newServicesDisplay_NumberOfServicesChanged;

            devMetricView = new DevMetricView(gameFile.NetworkModel, gameFile.CurrentRound);
            Controls.Add(devMetricView);
            devMetricView.BringToFront();


            transactionsViewPanel = new TransactionViewPanel(gameFile.NetworkModel) { BackColor = Color.White, Watermarker = watermarker };
            Controls.Add(transactionsViewPanel);

            Controls.Add(incidentPanel);

            metricView = new DevOpsMetricView(gameFile.NetworkModel);
            Controls.Add(metricView);

            awtPanel = new AwtPanel(gameFile.NetworkModel)
            {
                Watermarker = watermarker
            };
            Controls.Add(awtPanel);

            cityScapePicturePanel = new PicturePanel();
            Controls.Add(cityScapePicturePanel);

            cityScapeImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + @"\images\panels\CityScape.png");
			
            appDevelopmentFailedDisplay = new TimedMessageDisplay();
            Controls.Add(appDevelopmentFailedDisplay);

            appDevelopmentFailedDisplay.AddNodeToWatch(gameFile.NetworkModel.GetNamedNode("AppDevelopmentStageFailures"), "error_title", "error_text");
            appDevelopmentFailedDisplay.AddNodeToWatch(gameFile.NetworkModel.GetNamedNode("FacilitatorNotifiedErrors"), "title", "text");
            
            DoSize();
        }

		void newServicesDisplay_NumberOfServicesChanged(object sender, EventArgs e)
		{
			NumberOfAvailableLocationsChanged?.Invoke(this, NumberOfAvailableLocationsChanged.CreateArgs(newServicesDisplay.NumberOfRemainingLocations));
		}

	    public event EventHandler<EventArgs<int>> NumberOfAvailableLocationsChanged;
		public event EventHandler<EventArgs<Node>> AppIconClicked;
        
        void OnAppIconClicked (EventArgs<Node> e)
        {
            AppIconClicked?.Invoke(this, e);
        }

        protected override void Dispose (bool disposing)
        {
            if (disposing)
            {
                opsEngine.PhaseFinished -= opsEngine_PhaseFinished;
            }

            base.Dispose(disposing);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.Button == MouseButtons.Left)
            {
                ((Form)TopLevelControl).DragMove();
            }
        }

        protected override void OnSizeChanged (EventArgs e)
        {
            DoSize();
        }

        void DoSize ()
        {
            //roundLabel.BackColor = Color.Transparent;
            //const int barHeight = 40;
            //topBarBounds = new Rectangle(0, 0, Width, barHeight);
            //bottomBarBounds = new Rectangle(0, Height - barHeight, Width, barHeight);
            
            //timerViewer.Bounds = topBarBounds.CentreSubRectangle(Width / 3, barHeight);
            //timeLine.Bounds = new Rectangle(0, topBarBounds.Bottom, Width, SkinningDefs.TheInstance.GetSizeData("time_line_panel_size", Width, 5).Height);

            //roundLabel.Location = new Point(timerViewer.Left - roundLabel.Width, 0);

            var subBounds = new Rectangle(0, 0, Width, Height);
            
            newServicesDisplay.Bounds = new Rectangle(subBounds.Left, subBounds.Top, subBounds.Width / 2, subBounds.Height * 3 / 7);
            devMetricView.Bounds = new Rectangle(subBounds.Left, newServicesDisplay.Bottom, newServicesDisplay.Width, subBounds.Height / 7);

            transactionsViewPanel.Bounds = new Rectangle(newServicesDisplay.Right, newServicesDisplay.Top, newServicesDisplay.Width, newServicesDisplay.Height);

            metricView.Bounds = new Rectangle(transactionsViewPanel.Left, transactionsViewPanel.Bottom, transactionsViewPanel.Width, devMetricView.Height);

            incidentPanel.Bounds = new Rectangle(metricView.Left, metricView.Bottom, subBounds.Width - metricView.Left, (subBounds.Height - metricView.Bottom) * 3 / 5);

            awtPanel.Bounds = new RectangleFromBounds
            {
                Left = incidentPanel.Left,
                Right = incidentPanel.Right,
                Top = incidentPanel.Bottom,
                Bottom = Height
            }.ToRectangle();

            var lowerLeftQuadrantBounds = new RectangleFromBounds
            {
                Left = devMetricView.Left,
                Right = devMetricView.Right,
                Top = devMetricView.Bottom,
                Bottom = Height
            }.ToRectangle();

            appDevelopmentFailedDisplay.Bounds = lowerLeftQuadrantBounds;

            cityScapePicturePanel.Bounds = lowerLeftQuadrantBounds;

            if (appDevelopmentFailedDisplay.Visible)
            {
                appDevelopmentFailedDisplay.BringToFront();
            }
            else
            {
                cityScapePicturePanel.BringToFront();
            }

            

            cityScapePicturePanel.ZoomWithCropping(cityScapeImage);
            

            Invalidate();
        }
		
        void opsEngine_PhaseFinished (object sender)
        {
            newServicesDisplay.DisableButtons();
        }

        void newServicesDisplay_ServiceIconClicked(object sender, EventArgs<Node> e)
        {
            OnAppIconClicked(e);
        }
		
        readonly NewServicesDisplay newServicesDisplay;
        readonly DevMetricView devMetricView;

        readonly TransactionViewPanel transactionsViewPanel;
        readonly DevOpsMetricView metricView;
        readonly DevOpsQuadStatusLozengeGroup incidentPanel;
        readonly AwtPanel awtPanel;

        readonly PicturePanel cityScapePicturePanel;
        readonly Image cityScapeImage;

        readonly TimedMessageDisplay appDevelopmentFailedDisplay;

        readonly TradingOpsEngine opsEngine;

        readonly IWatermarker watermarker;
    }
}
