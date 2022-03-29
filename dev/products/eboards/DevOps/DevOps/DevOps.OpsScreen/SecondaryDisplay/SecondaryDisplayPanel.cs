using System;
using System.Drawing;
using System.Windows.Forms;

using CommonGUI;
using Algorithms;
using CoreUtils;
using DevOps.ReportsScreen;
using GameManagement;
using LibCore;
// ReSharper disable LocalizableElement

namespace DevOps.OpsScreen.SecondaryDisplay
{
    internal class SecondaryDisplayPanel : FlickerFreePanel
    {
        public SecondaryDisplayPanel (NetworkProgressionGameFile gameFile)
        {
			this.gameFile = gameFile;

            windowControl = new WindowControlBar
            {
                Size = new Size(67, 32),
                BackColor = Color.Transparent
			};
            Controls.Add(windowControl);

            screenLabel = new Label
            {
                Font = SkinningDefs.TheInstance.GetFont(14),
                Size = new Size(150, 40),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            Controls.Add(screenLabel);
			screenLabel.MouseDown += screenLabel_MouseDown;
        }

	    public NetworkProgressionGameFile GameFile
	    {
		    set => gameFile = value;
	    }

		public void ShowGameScreen (GameScreenPanel newGameScreen)
        {
            reportsScreen?.Hide();

            if (gameScreen != newGameScreen)
            {
                gameScreen?.Dispose();
                gameScreen = newGameScreen;
                Controls.Add(gameScreen);
            }

			gameScreen.Show();
            gameScreen.BringToFront();
            inGame = true;
            UpdateTopBar();
            DoSize();
        }

        public void ShowReportsScreen (ReportsScreenPanel newReportsScreen)
        {
            gameScreen?.Hide();

            if (reportsScreen != newReportsScreen)
            {
                reportsScreen?.Dispose();
                reportsScreen = newReportsScreen;
                Controls.Add(reportsScreen);
            }
            
			reportsScreen.Show();
            reportsScreen.BringToFront();
            inGame = false;
            UpdateTopBar();
            DoSize();
        }

        protected override void OnSizeChanged (EventArgs e)
        {
            DoSize();
        }

        protected override void OnPaint (PaintEventArgs e)
        {
            using (var barBrush =
                new SolidBrush(SkinningDefs.TheInstance.GetColorData("game_screen_top_bar_back_colour")))
            {
                e.Graphics.FillRectangle(barBrush, topBarBounds);
                e.Graphics.FillRectangle(barBrush, bottomBarBounds);
            }


            var poweredByImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + @"\images\panels\low_poweredby_logo.png");
            e.Graphics.DrawImage(poweredByImage, new Rectangle(bottomBarBounds.Left + 150, bottomBarBounds.Top + (bottomBarBounds.Height - poweredByImage.Height) / 2, poweredByImage.Width, poweredByImage.Height));

            var topBarContentSize = new Size(Width / 3, topBarBounds.Height);

            var imageBounds = topBarBounds.AlignRectangle(topBarContentSize, StringAlignment.Far, StringAlignment.Center);

            var logoImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + @"\images\panels\top_devops_logo.png");
            e.Graphics.DrawImage(logoImage, imageBounds.CentreSubRectangle(logoImage.Width, logoImage.Height));

        }

	    protected override void OnMouseDown(MouseEventArgs e)
	    {
		    base.OnMouseDown(e);

		    if (e.Button == MouseButtons.Left)
		    {
			    ((Form)TopLevelControl).DragMove();
		    }
	    }

		void UpdateTopBar ()
        {
            gameTimeLine?.Dispose();
            gameTimeLine = null;

            gameTimer?.Dispose();
            gameTimer = null;

            if (inGame)
            {
                screenLabel.Text = $"Round {gameFile.CurrentRound}";

                gameTimeLine = new TimeLine(gameFile.NetworkModel);
                Controls.Add(gameTimeLine);
                gameTimer = new TimerViewer(gameFile.NetworkModel)
                {
                    BackColor = SkinningDefs.TheInstance.GetColorData("game_screen_top_bar_back_colour")
                };
                Controls.Add(gameTimer);
            }
            else
            {
                screenLabel.Text = "Reports Screen";
            }
                
        }
        
        void DoSize ()
        {
            var bounds = new Rectangle(new Point(0,0), ClientSize);

            const int barHeight = 40;

            topBarBounds = bounds.AlignRectangle(Width, barHeight, StringAlignment.Center, StringAlignment.Near);

            windowControl.Bounds =
                topBarBounds.AlignRectangle(windowControl.Size, StringAlignment.Far, StringAlignment.Center);

            if (inGame)
            {
                gameTimer.Bounds = topBarBounds.AlignRectangle(Width / 3, barHeight, StringAlignment.Center, StringAlignment.Center);
                gameTimeLine.Bounds = new Rectangle(0, topBarBounds.Bottom, Width, SkinningDefs.TheInstance.GetSizeData("time_line_panel_size", Width, 5).Height);

                screenLabel.Location = new Point(gameTimer.Left - screenLabel.Width, 0);
            }
            else
            {
                screenLabel.Bounds =
                    topBarBounds.AlignRectangle(screenLabel.Size, StringAlignment.Center, StringAlignment.Center);
            }

            bottomBarBounds = bounds.AlignRectangle(Width, barHeight, StringAlignment.Center, StringAlignment.Far);

            var subPanelBounds = new RectangleFromBounds
            {
                Left = 0,
                Right = Width,
                Top = gameTimeLine?.Bottom ?? topBarBounds.Bottom,
                Bottom = bottomBarBounds.Top
            }.ToRectangle();

            if (gameScreen != null)
            {
                gameScreen.Bounds = subPanelBounds;
            }

            if (reportsScreen != null)
            {
                reportsScreen.Bounds = subPanelBounds;
            }

            Invalidate();
        }

	    void screenLabel_MouseDown(object sender, MouseEventArgs e)
	    {
		    OnMouseDown(e);
	    }

		GameScreenPanel gameScreen;
        ReportsScreenPanel reportsScreen;

        bool inGame;

        Rectangle topBarBounds;
        Rectangle bottomBarBounds;

        TimeLine gameTimeLine;
        TimerViewer gameTimer;
        readonly Label screenLabel;

        readonly WindowControlBar windowControl;
        NetworkProgressionGameFile gameFile;
    }
}
