using System;
using System.Windows.Forms;
using System.Drawing;
using CoreUtils;
using LibCore;

namespace CommonGUI
{
	/// <summary>
	/// Summary description for GlassGameControl.
	/// </summary>
	public class GlassGameControl : BasePanel
	{
		public enum ButtonAction
		{
			Rewind = 0,
			Play,
			Pause,
			FastForward,
			PrePlay,
			Slow,
			SkipHours,
			EndRoundAction
		}

		GradientControlBackground background;

		public delegate void ButtonActionHandler(object sender, ButtonAction action);
		public event ButtonActionHandler ButtonPressed;

		protected ImageButton play;
		protected ImageButton pause;
		protected ImageButton rewind;
		protected ImageButton fastForward;

		protected int fastForwardSpeed = 60;

		bool properPlayStarted;
		//
		public int FastForwardSpeed
		{
			get
			{
				return fastForwardSpeed;
			}
		}
		//
		public GlassGameControl()
		{
			CreateButtons();
		}

		protected virtual void CreateButtons ()
		{
			SuspendLayout();

			var useStyledImageButton = SkinningDefs.TheInstance.GetBoolData("use_styled_image_button", false);
			//
			rewind = useStyledImageButton ? new StyledImageButtonCommon("standard_image_button") : new ImageButton (0);
			rewind.SetVariants("/images/buttons/rewind.png");
			rewind.Size = new Size(40,40);
			rewind.Location = new Point(0,0);
			rewind.ButtonPressed += _ButtonPressed;
			rewind.Name = "Rewind Button";
			Controls.Add(rewind);
			//
			play = useStyledImageButton ? new StyledImageButtonCommon("standard_image_button", 1) : new ImageButton(1);
			play.SetVariants("/images/buttons/play.png");
			play.Size = new Size(40,40);
			play.Location = new Point(45,0);
			play.ButtonPressed += _ButtonPressed;
			play.Name = "Play Button";
			Controls.Add(play);
			//
			pause = useStyledImageButton ? new StyledImageButtonCommon("standard_image_button", 2) : new ImageButton(2);
			pause.SetVariants("/images/buttons/pause.png");
			pause.Size = new Size(40,40);
			pause.Location = new Point(90,0);
			pause.ButtonPressed += _ButtonPressed;
			pause.Name = "Pause Button";
			Controls.Add(pause);
			//
			fastForward = useStyledImageButton ? new StyledImageButtonCommon("standard_image_button", 3) : new ImageButton(3);
			fastForward.SetVariants("/images/buttons/fastforward.png");
			fastForward.Size = new Size(40,40);
			fastForward.Location = new Point(135,0);
			fastForward.ButtonPressed += _ButtonPressed;
			fastForward.Name = "FastForward Button";
			Controls.Add(fastForward);
			//
			ResumeLayout(false);
		}

		protected bool _playOn;
		protected bool _pauseOn;
		protected bool _rewindOn;
		protected bool _fastForwardOn;
		protected bool _preplayOn;
		protected bool _preplayIgnore = true;

		public void SuspendButtonsForModal(bool entering)
		{
			if (entering)
			{
				//record what state we were in 
				rewind.Tag = rewind.Enabled;
				play.Tag = play.Enabled;
				pause.Tag = pause.Enabled;
				fastForward.Tag = fastForward.Enabled;
				//Disable the buttons for the modal state 
				rewind.Enabled = false;
				play.Enabled = false;
				pause.Enabled = false;
				fastForward.Enabled = false;
			}
			else
			{
				//Exiting the Modal, Recover the State from the Tags
				if (rewind.Tag != null)
				{
					rewind.Enabled = (bool)rewind.Tag;
				}
				if (play.Tag != null)
				{
					play.Enabled = (bool)play.Tag;
				}
				if (pause.Tag != null)
				{
					pause.Enabled = (bool)pause.Tag;
				}
				if (fastForward.Tag != null)
				{
					fastForward.Enabled = (bool)fastForward.Tag;
				}
			}
		}

		/// <summary>
		/// allows the child Completed Game Panel to adjust the positioning of buttons
		/// </summary>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="seperation"></param>
		public virtual void AdjustPosition(int btnWidth, int btnHeight, int btnSeperation)
		{
			int xoffset = 0;

			//Position Rewind as First Button 
			rewind.Size = new Size(btnWidth, btnHeight);
			rewind.Location = new Point(0,0);

			xoffset = btnWidth + btnSeperation;
			
			//Position Rewind as Second Button 
			play.Size = new Size(btnWidth, btnHeight);
			play.Location = new Point(xoffset,0);

			xoffset = xoffset + btnWidth + btnSeperation;

			//Position Rewind as Third Button 
			pause.Size = new Size(btnWidth, btnHeight);
			pause.Location = new Point(xoffset,0);
			
			xoffset = xoffset + btnWidth + btnSeperation;

			//Position Rewind as Fourth Button 
			fastForward.Size = new Size(btnWidth, btnHeight);
			fastForward.Location = new Point(xoffset,0);
		}

		/// <summary>
		/// allows the child Completed Game Panel to adjust the positioning of buttons
		/// </summary>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="seperation"></param>
		public virtual void AdjustPositionAndWidthResize(int btnWidth, int btnHeight, int btnSeperation)
		{
			AdjustPosition(btnWidth, btnHeight, btnSeperation);
			Width = fastForward.Left + fastForward.Width +5;
		}

		public void SetPrePlayIgnore(Boolean newValue)
		{
			_preplayIgnore = newValue;
		}

		public void ClearPrePlay()
		{
			_preplayOn = false;
		}

		public Boolean GetPrePlayStatus()
		{
			return _preplayOn;
		}

		bool disablePauseAndEnableRewindInstead;
		public void DisablePauseAndEnableRewindInstead (bool disablePause)
		{
			disablePauseAndEnableRewindInstead = disablePause;

			if (disablePauseAndEnableRewindInstead)
			{
				SetState(_rewindOn, _playOn && !prePlayCannotBeSkipped, _pauseOn, _fastForwardOn, _preplayOn);
			}
			else
			{
				SetState(false, true, true, play.Active, false);
			}
		}

		public virtual void SetState(bool rewindOn, bool playOn, bool pauseOn, bool fastForwardOn, bool preplayOn)
		{
			_playOn = playOn;
			_pauseOn = pauseOn;
			_rewindOn = rewindOn;
			_fastForwardOn = fastForwardOn;
			_preplayOn = preplayOn;

			if (playOn && ! preplayOn)
			{
				properPlayStarted = true;
			}

			rewind.Enabled = rewindOn;
			play.Enabled = playOn || play.Active;
			pause.Enabled = pauseOn || pause.Active;
			fastForward.Enabled = fastForwardOn || fastForward.Active;

			if (disablePauseAndEnableRewindInstead && pause.Enabled)
			{
				pause.Enabled = false;
				_pauseOn = false;
				rewind.Enabled = true;
				_rewindOn = true;
			}
		}

		bool playWasActive;

		public bool PlayWasActive
		{
			get
			{
				return playWasActive;
			}
		}
	
		protected virtual void _ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			playWasActive = play.Active;

			using (WaitCursor cursor = new WaitCursor (this))
			{
				ButtonAction action = (ButtonAction) args.Code;

				switch (action)
				{
					case ButtonAction.Play:
						if ((_preplayOn) & (_preplayIgnore))
						{
							action = ButtonAction.PrePlay;
						}
						else if (prePlayCannotBeSkipped && ! properPlayStarted)
						{
							return;
						}
						else
						{
							action = ButtonAction.Play;
							properPlayStarted = true;
						}
						if (!_playOn) return;
						break;

					case ButtonAction.Pause:
						if (!_pauseOn) return;
						break;

					case ButtonAction.Rewind:
						if (!_rewindOn) return;
						break;

					case ButtonAction.FastForward:
						if (!_fastForwardOn) return;
						break;
				}
				SelectButton(action);

				InvokeButtonPressed(this, action);
			}
		}

		public void ResetButtons (bool atEndOfRace)
		{
			play.Active = false;
			pause.Active = false;
			rewind.Active = false;
			fastForward.Active = false;

			play.Enabled = ! atEndOfRace;
			pause.Enabled = false;
			rewind.Enabled = false;
			fastForward.Enabled = false;

			properPlayStarted = false;
		}

		public void InternalPauseRequest()
		{
			ImageButtonEventArgs args = new ImageButtonEventArgs((int)ButtonAction.Pause);
			_ButtonPressed(this,args);
		}

		public void InternalPlayRequest()
		{
			ImageButtonEventArgs args = new ImageButtonEventArgs((int)ButtonAction.Play);
			_ButtonPressed(this,args);
		}

		protected void InvokeButtonPressed (object sender, ButtonAction action)
		{
			ButtonPressed?.Invoke(sender, action);
		}

		public void SelectButton (ButtonAction action)
		{
			play.Active = (action == ButtonAction.Play);
			pause.Active = (action == ButtonAction.Pause);
			rewind.Active = (action == ButtonAction.Rewind);
			fastForward.Active = (action == ButtonAction.FastForward);
		}

		public bool IsButtonActive (ButtonAction action)
		{
			switch (action)
			{
				case ButtonAction.Play:
				case ButtonAction.PrePlay:
					return play.Active;

				case ButtonAction.Pause:
					return pause.Active;

				case ButtonAction.Rewind:
					return rewind.Active;

				case ButtonAction.FastForward:
					return fastForward.Active;
			}

			return false;
		}

		bool prePlayCannotBeSkipped;
		public bool PrePlayCannotBeSkipped
		{
			get
			{
				return prePlayCannotBeSkipped;
			}

			set
			{
				prePlayCannotBeSkipped = value;
			}
		}

		public void SetAutoSize (Size? buttonSize = null)
		{
			if (buttonSize == null)
			{
				buttonSize = play.ActiveImage.Size;
			}

			AdjustPosition(buttonSize.Value.Width, buttonSize.Value.Height, 0);
			Size = new Size (fastForward.Right, fastForward.Bottom);
		}

		public void SetBackgroundFill (GradientControlBackground background)
		{
			this.background = background;
			Invalidate();
		}

		protected override void OnPaint (PaintEventArgs e)
		{
			base.OnPaint(e);

			if (background != null)
			{
				background.Draw(this, e.Graphics);
			}
		}
	}
}