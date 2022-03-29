using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Algorithms;
using LibCore;
using CoreUtils;
using GameManagement;
using CommonGUI;
using Licensor;

namespace GameDetails
{
	public class PhaseSelector : GameDetailsSection
	{
		NetworkProgressionGameFile gameFile;

		int minPhase;
		int maxPhase;

		int selectedPhase;

		ImageTextButton left;
		ImageTextButton right;

		Button play;
		Button skip;
		List<Button> buttons;

		ImageScroller<int> scroller;
		Label text;

		Timer updatePlayabilityTimer;

		public PhaseSelector (NetworkProgressionGameFile gameFile)
		{
			this.gameFile = gameFile;

			minPhase = gameFile.MinPhaseNumber;
			maxPhase = gameFile.MaxPhaseNumber;

			scroller = new ImageScroller<int> ();
			panel.Controls.Add(scroller);

			buttons = new List<Button> ();

			var colourKey = SkinningDefs.TheInstance.GetColourData("phase_selector_colour_key");
			var replacementColour = SkinningDefs.TheInstance.GetColourData("phase_selector_replacement_colour");

			for (int i = minPhase; i <= maxPhase; i++)
			{
				var image = Repository.TheInstance.GetImage(CONVERT.Format(@"{0}\images\phases\phase_image_{1}.png", AppInfo.TheInstance.Location, i));

				if (colourKey != null && replacementColour != null)
				{
					image = new Bitmap(image).ConvertColours(new Dictionary<Color, Color> { { colourKey.Value, replacementColour.Value } });
				}

				scroller.AddItem(i, image, 1, 0);
			}
			scroller.AddItem(-1, Repository.TheInstance.GetImage(CONVERT.Format(@"{0}\images\phases\game_locked.png", AppInfo.TheInstance.Location)), 1, 0);
			scroller.Size = scroller.GetPreferredSize(Size.Empty);

			text = new Label { Font = SkinningDefs.TheInstance.GetFont(10), ForeColor = Color.Red };
			panel.Controls.Add(text);

			left = new ImageTextButton (@"\images\buttons\left_arrow.png");
			left.ButtonPressed += left_ButtonPressed;
			left.SetAutoSize();
			panel.Controls.Add(left);

			right = new ImageTextButton (@"\images\buttons\right_arrow.png");
			right.ButtonPressed += right_ButtonPressed;
			right.SetAutoSize();
			panel.Controls.Add(right);

			int leftMargin = 50;
			int topMargin = 30;
			int horizontalGap = 30;
			scroller.Location = new Point (leftMargin + horizontalGap + left.Width, topMargin);
			left.Location = new Point (leftMargin, topMargin + ((scroller.Height - left.Height) / 2));
			right.Location = new Point (scroller.Right + horizontalGap, topMargin + ((scroller.Height - right.Height) / 2));
			text.Bounds = new Rectangle (left.Left, scroller.Bottom + 10, right.Right - left.Left, 50);

			play = AddButton("Play", play_Click);

			if (gameFile.GameTypeUsesTransitions())
			{
				skip = AddButton("Skip Transition", skip_Click);
			}

			int phaseToPlay = gameFile.CurrentPhaseNumber;
			if (! gameFile.CanPlayPhase(phaseToPlay))
			{
				// Find the last phase we can play, if any, and select that.
				phaseToPlay = -1;
				for (int i = gameFile.MaxPhaseNumber; i >= gameFile.MinPhaseNumber; i--)
				{
					if (gameFile.CanPlayPhase(i, true))
					{
						phaseToPlay = i;
						break;
					}
				}
			}
			SelectedPhase = phaseToPlay;
			OnSelectedPhaseChanged(true);

			Title = CONVERT.Format("Select {0}", (gameFile.GameTypeUsesTransitions() ? "Phase" : "Round"));
			Collapsible = false;
			Expanded = true;
			SetAutoSize();

			updatePlayabilityTimer = new Timer { Interval = 30 * 1000 };
			updatePlayabilityTimer.Tick += updatePlayabilityTimer_Tick;
			updatePlayabilityTimer.Start();
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing)
			{
				updatePlayabilityTimer.Dispose();
			}

			base.Dispose(disposing);
		}

		public int SelectedPhase
		{
			get
			{
				return selectedPhase;
			}

			set
			{
				int clampedPhase = value;
				if (clampedPhase != -1)
				{
					clampedPhase = Algorithms.Maths.Clamp(value, minPhase, maxPhase);
				}

				if (clampedPhase != selectedPhase)
				{
					selectedPhase = clampedPhase;

					OnSelectedPhaseChanged(false);
				}
			}
		}

		public event EventHandler SelectedPhaseChanged;

		protected virtual void OnSelectedPhaseChanged (bool instant = false)
		{
			if (instant)
			{
				scroller.JumpToItem(selectedPhase);
			}
			else
			{
				scroller.ScrollToItem(selectedPhase, 0.25);
			}

			left.Enabled = gameFile.CanPlayPhase(selectedPhase - 1);
			left.Visible = selectedPhase - 1 >= gameFile.MinPhaseNumber;

			right.Enabled = gameFile.CanPlayPhase(selectedPhase + 1);
			right.Visible = selectedPhase + 1 <= gameFile.MaxPhaseNumber;

			UpdatePlayability();

			SelectedPhaseChanged?.Invoke(this, EventArgs.Empty);
		}

		void updatePlayabilityTimer_Tick (object sender, EventArgs args)
		{
			UpdatePlayability();
		}

		void UpdatePlayability ()
		{
			play.Enabled = gameFile.CanPlayPhase(selectedPhase);
			var playability = gameFile.Licence?.GetPhasePlayability(selectedPhase);
			if (playability != null)
			{
				text.Text = "";
			}
			else
			{
				text.Text = "Error";
			}

			if (skip != null)
			{
				int round;
				GameFile.GamePhase phaseType;
				gameFile.PhaseToRound(selectedPhase, out round, out phaseType);
				skip.Visible = (phaseType == GameFile.GamePhase.TRANSITION)
				               && gameFile.CanPlayPhase(selectedPhase)
				               && gameFile.CouldPlayPhaseAfterSkip(selectedPhase + 1);
			}
		}

		void left_ButtonPressed (object sender, ImageButtonEventArgs args)
		{
			SelectedPhase--;
		}

		void right_ButtonPressed (object sender, ImageButtonEventArgs args)
		{
			SelectedPhase++;
		}

		public Button AddButton (string text, EventHandler handler)
		{
			Button button = SkinningDefs.TheInstance.CreateWindowsButton(FontStyle.Bold);
			button.Text = text;

            button.Click += handler;
			panel.Controls.Add(button);
			buttons.Add(button);

			Size buttonSize = new Size (150, 25);
			int verticalGap = 10;

			int buttonsHeight = (buttons.Count * buttonSize.Height) + (Math.Max(0, (buttons.Count - 1)) * verticalGap);
			int y;
			if (buttonsHeight > scroller.Height)
			{
				y = 40;
			}
			else
			{
				y = scroller.Top + ((scroller.Height - buttonsHeight) / 2);
			}

			foreach (Button positionButton in buttons)
			{
				positionButton.Bounds = new Rectangle (right.Right + 30, y, buttonSize.Width, buttonSize.Height);
				y = positionButton.Bottom + verticalGap;
			}

			SetAutoSize();

			return button;
		}

		public event EventHandler PlayPressed;

		void OnPlayPressed ()
		{
			PlayPressed?.Invoke(this, EventArgs.Empty);
		}

		public event EventHandler SkipTransitionPressed;

		void OnSkipTransitionPressed ()
		{
			SkipTransitionPressed?.Invoke(this, EventArgs.Empty);
		}

		void play_Click (object sender, EventArgs args)
		{
			OnPlayPressed();
		}

		void skip_Click (object sender, EventArgs args)
		{
			OnSkipTransitionPressed();
		}

		public void Play ()
		{
			OnPlayPressed();
		}

		public void HideSkip (bool hide)
		{
			if (skip != null)
			{
				skip.Visible = ! hide;
			}
		}
	}
}