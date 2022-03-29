using System;
using System.Drawing;
using System.Windows.Forms;

using Algorithms;
using CommonGUI;
using CoreUtils;
using GameManagement;
using LibCore;
using ResizingUi.TimeDisplays;

namespace SecondaryDisplay
{
	public class SecondaryPanel : FlickerFreePanel
	{
		public SecondaryPanel (NetworkProgressionGameFile gameFile)
		{
			this.gameFile = gameFile;

			windowControl = new WindowControlBar
			{
				Size = new Size(67, 32),
				BackColor = Color.Transparent
			};
			Controls.Add(windowControl);

			screenTitleLabel = new Label
			{
				Font = SkinningDefs.TheInstance.GetFont(14),
				Size = new Size(150, 40),
				ForeColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("top_bar_label_fore_colour", Color.White),
				TextAlign = ContentAlignment.MiddleCenter,
				BackColor = Color.Transparent
			};
			Controls.Add(screenTitleLabel);
			screenTitleLabel.MouseDown += screenLabel_MouseDown;
		}

		public NetworkProgressionGameFile GameFile
		{
			set => gameFile = value;
		}

		public void ShowGameScreen(Control newGameScreen)
		{
			reportsScreen?.Hide();
			transitionScreen?.Hide();

			screen = Screen.Game;

			if (gameScreen != newGameScreen)
			{
				gameScreen?.Dispose();
				gameScreen = newGameScreen;
				Controls.Add(gameScreen);
			}

			gameScreen.Show();
			gameScreen.BringToFront();
			UpdateTopBar();
			DoSize();
		}

		public void ShowReportsScreen(Control newReportsScreen)
		{
			gameScreen?.Hide();
			transitionScreen?.Hide();

			screen = Screen.Reports;

			if (reportsScreen != newReportsScreen)
			{
				reportsScreen?.Dispose();
				reportsScreen = newReportsScreen;
				Controls.Add(reportsScreen);
			}

			reportsScreen.Show();
			reportsScreen.BringToFront();
			UpdateTopBar();
			DoSize();
		}

		public void ShowTransitionScreen(Control newTransitionScreen)
		{
			gameScreen?.Hide();
			reportsScreen?.Hide();

			screen = Screen.Transition;

			if (transitionScreen != newTransitionScreen)
			{
				transitionScreen?.Dispose();
				transitionScreen = newTransitionScreen;
				Controls.Add(transitionScreen);
			}

			transitionScreen.Show();
			transitionScreen.BringToFront();
			UpdateTopBar();
			DoSize();
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			DoSize();
		}

		protected override void OnPaint(PaintEventArgs e)
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

			var imageBounds = topBarBounds.AlignRectangle(topBarContentSize, StringAlignment.Far);

			var logoImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + @"\images\panels\top_devops_logo.png");
			e.Graphics.DrawImage(logoImage, imageBounds.CentreSubRectangle(logoImage.Width, logoImage.Height));
		}

		void DoSize()
		{
			var bounds = new Rectangle(new Point(0, 0), ClientSize);

			const int barHeight = 40;

			topBarBounds = bounds.AlignRectangle(Width, barHeight, StringAlignment.Center, StringAlignment.Near);

			windowControl.Bounds = topBarBounds.AlignRectangle(windowControl.Size, StringAlignment.Far);

			if (screen == Screen.Game)
			{
				gameTimer.Bounds = topBarBounds.AlignRectangle(Width / 3, barHeight);
				gameTimeLine.Bounds = new Rectangle(0, topBarBounds.Bottom, Width, SkinningDefs.TheInstance.GetSizeData("time_line_panel_size", Width, 5).Height);

				screenTitleLabel.Location = new Point(gameTimer.Left - screenTitleLabel.Width, 0);
			}
			else
			{
				screenTitleLabel.Bounds = topBarBounds.AlignRectangle(screenTitleLabel.Size);
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

		void UpdateTopBar()
		{
			gameTimeLine?.Dispose();
			gameTimeLine = null;

			gameTimer?.Dispose();
			gameTimer = null;

			switch (screen)
			{
				case Screen.Game:
					screenTitleLabel.Text = $"Round {gameFile.CurrentRound}";

					gameTimeLine = new TimeLine(gameFile.NetworkModel);
					Controls.Add(gameTimeLine);
					gameTimer = new TimerView(gameFile.NetworkModel)
					{
						BackColor = SkinningDefs.TheInstance.GetColorData("game_screen_top_bar_back_colour")
					};
					Controls.Add(gameTimer);
					break;
				case Screen.Reports:
					screenTitleLabel.Text = "Reports Screen";
					break;
				case Screen.Transition:
					// TODO
					break;
			}
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);

			if (e.Button == MouseButtons.Left)
			{
				((Form)TopLevelControl).DragMove();
			}
		}

		void screenLabel_MouseDown(object sender, MouseEventArgs e)
		{
			OnMouseDown(e);
		}

		enum Screen
		{
			Game,
			Reports,
			Transition
		}

		Screen screen;

		Control gameScreen;
		Control reportsScreen;
		Control transitionScreen;

		Rectangle topBarBounds;
		Rectangle bottomBarBounds;

		TimeLine gameTimeLine;
		TimerView gameTimer;
		readonly Label screenTitleLabel;

		readonly WindowControlBar windowControl;
		NetworkProgressionGameFile gameFile;

	}
}
