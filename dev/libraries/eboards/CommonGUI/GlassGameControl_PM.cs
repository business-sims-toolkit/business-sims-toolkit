using System.Drawing;

namespace CommonGUI
{
	/// <summary>
	/// Summary description for GlassGameControl.
	/// </summary>
	public class GlassGameControl_PM : GlassGameControl
	{
		protected ImageTextButton endround;
		protected bool _endRoundOn;

		//
		public GlassGameControl_PM()
		{
			//No need to do anything
		}

		protected override void CreateButtons ()
		{
			SuspendLayout();
			//
			rewind = new ImageButton ((int) ButtonAction.Rewind);
			rewind.SetVariants("/images/buttons/rewind.png");
			rewind.Size = new Size(40,33);
			rewind.Location = new Point(0,0);
			rewind.ButtonPressed += _ButtonPressed;
			rewind.Name = "Rewind Button";
			Controls.Add(rewind);
			//
			play = new ImageButton((int) ButtonAction.Play);
			play.SetVariants("/images/buttons/play.png");
			play.Size = new Size(40,33);
			play.Location = new Point(45,0);
			play.ButtonPressed += _ButtonPressed;
			play.Name = "Play Button";
			Controls.Add(play);
			//
			pause = new ImageButton((int) ButtonAction.Pause);
			pause.SetVariants("/images/buttons/pause.png");
			pause.Size = new Size(40,33);
			pause.Location = new Point(90,0);
			pause.ButtonPressed += _ButtonPressed;
			pause.Name = "Pause Button";
			Controls.Add(pause);
			//
			fastForward = new ImageButton((int) ButtonAction.FastForward);
			fastForward.SetVariants("/images/buttons/fastforward.png");
			fastForward.Size = new Size(40,33);
			fastForward.Location = new Point(135,0);
			fastForward.ButtonPressed += _ButtonPressed;
			fastForward.Name = "FastForward Button";
			Controls.Add(fastForward);
			

			endround = new ImageTextButton((int)ButtonAction.EndRoundAction);
			endround.SetVariants("/images/buttons/blank.png");
			endround.ButtonPressed += _ButtonPressed;
			endround.SetButtonText("Results", Color.Black, Color.Black, Color.White, Color.LightGray);
			//endround.ButtonFont = this.MyDefaultSkinFontBold8;
			endround.Size = new Size(60,33);
			endround.Location = new Point(186, 0);
			Controls.Add(endround);
			
			//
			ResumeLayout(false);
		}

		public void setEndRoundButtonText(string end_round_text)
		{ 
			if (endround != null)
			{
				endround.SetButtonText(end_round_text, Color.Black, Color.Black, Color.White, Color.LightGray);
				endround.Refresh();
			}
		}

		/// <summary>
		/// allows the child Completed Game Panel to adjust the positioning of buttons
		/// </summary>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="seperation"></param>
		public override void AdjustPosition(int btnWidth, int btnHeight, int btnSeperation)
		{
			int xoffset = 0;

			int h = Height;

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

			xoffset = xoffset + btnWidth + btnSeperation+6;

			endround.Size = new Size(60, 27);
			endround.Location = new Point(xoffset, (play.Height - endround.Height) / 2);
		}
		
		/// <summary>
		/// allows the child Completed Game Panel to adjust the positioning of buttons
		/// </summary>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="seperation"></param>
		public override void AdjustPositionAndWidthResize(int btnWidth, int btnHeight, int btnSeperation)
		{
			AdjustPosition(btnWidth, btnHeight, btnSeperation);
			Width = endround.Left + endround.Width + 7;
		}

		public override void SetState(bool rewindOn, bool playOn, bool pauseOn, bool fastForwardOn, bool preplayOn)
		{
			SetState(rewindOn, playOn, pauseOn, fastForwardOn, preplayOn, false);
		}

		public void SetState(bool rewindOn, bool playOn, bool pauseOn, bool fastForwardOn, bool preplayOn, bool endRoundOn)
		{
			base.SetState(rewindOn, playOn, pauseOn, fastForwardOn, preplayOn);

			_endRoundOn = endRoundOn;
			endround.Enabled = endRoundOn;
		}
	
		protected override void Dispose(bool disposing)
		{
			if( disposing )
			{
				/*
				if(components != null)
				{
					components.Dispose();
				}*/
			}
			base.Dispose (disposing);
		}

		protected override void _ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			ButtonAction action = (ButtonAction) args.Code;

			switch(action)
			{
				case ButtonAction.Play:
					if ((_preplayOn)&(_preplayIgnore))
					{
						action = ButtonAction.PrePlay;
					}
					else
					{
						action = ButtonAction.Play;
					}
					if(!_playOn) return;
					break;

				case ButtonAction.Pause:
					if(!_pauseOn) return;
					break;

				case ButtonAction.Rewind:
					if(!_rewindOn) return;
					break;

				case ButtonAction.FastForward:
					if(!_fastForwardOn) return;
					break;

				case ButtonAction.EndRoundAction:
					if(!_endRoundOn) return;
					break;
			}

			SelectButton(action);
			InvokeButtonPressed(this, action);
		}



	}
}