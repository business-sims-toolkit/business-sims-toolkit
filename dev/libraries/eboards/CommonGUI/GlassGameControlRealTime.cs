using System.Drawing;

namespace CommonGUI
{
	public class GlassGameControlRealTime : GlassGameControl
	{
		protected ImageButton slow;
		protected ImageButton skipHours;

		protected double slowSpeed = 1.0 / 60;
		public double SlowSpeed
		{
			get
			{
				return slowSpeed;
			}
		}

		public GlassGameControlRealTime ()
		{
		}

		protected override void CreateButtons ()
		{
			SuspendLayout();
			//
			rewind = new ImageButton ((int) ButtonAction.Rewind);
			rewind.SetVariants("/images/buttons/rewind.png");
			rewind.Size = new Size(40,40);
			rewind.Location = new Point(0,0);
			rewind.ButtonPressed += _ButtonPressed;
			rewind.Name = "Rewind Button";
			Controls.Add(rewind);
			//
			play = new ImageButton((int) ButtonAction.Play);
			play.SetVariants("/images/buttons/play.png");
			play.Size = new Size(40,40);
			play.Location = new Point(45,0);
			play.ButtonPressed += _ButtonPressed;
			play.Name = "Play Button";
			Controls.Add(play);
			//
			pause = new ImageButton((int) ButtonAction.Pause);
			pause.SetVariants("/images/buttons/pause.png");
			pause.Size = new Size(40,40);
			pause.Location = new Point(90,0);
			pause.ButtonPressed += _ButtonPressed;
			pause.Name = "Pause Button";
			Controls.Add(pause);
			//
			fastForward = new ImageButton((int) ButtonAction.FastForward);
			fastForward.SetVariants("/images/buttons/fastforward.png");
			fastForward.Size = new Size(40,40);
			fastForward.Location = new Point(135,0);
			fastForward.ButtonPressed += _ButtonPressed;
			fastForward.Name = "FastForward Button";
			Controls.Add(fastForward);
			//
			slow = new ImageButton((int) ButtonAction.Slow);
			slow.SetVariants("/images/buttons/slow.png");
			slow.Size = new Size(40,40);
			slow.Location = new Point(180,0);
			slow.ButtonPressed += _ButtonPressed;
			slow.Name = "SuperForward Button";
			Controls.Add(slow);
			//
			skipHours = new ImageButton((int) ButtonAction.SkipHours);
			skipHours.SetVariants("/images/buttons/skip2hrs.png");
			skipHours.Size = new Size(40,40);
			skipHours.Location = new Point(225,0);
			skipHours.ButtonPressed += _ButtonPressed;
			skipHours.Name = "Skip 2 Hours Button";
			Controls.Add(skipHours);
			//
			ResumeLayout(false);
		}

		protected bool _slowOn;
		protected bool _skipHoursOn;

		/// <summary>
		/// allows the child Completed Game Panel to adjust the positioning of buttons
		/// </summary>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="seperation"></param>
		public override void AdjustPosition(int btnWidth, int btnHeight, int btnSeperation)
		{
			int xoffset = 0;

			//Position Rewind as First Button 
			rewind.Size = new Size(btnWidth, btnHeight);
			rewind.Location = new Point(0,0);

			xoffset = btnWidth + btnSeperation;

			//Position Pause as Second Button 
			pause.Size = new Size(btnWidth, btnHeight);
			pause.Location = new Point(xoffset,0);
			
			xoffset = xoffset + btnWidth + btnSeperation;

			//Position Slow as Third Button 
			slow.Size = new Size(btnWidth, btnHeight);
			slow.Location = new Point(xoffset,0);
			
			xoffset = xoffset + btnWidth + btnSeperation;
			
			//Position Play as Fourth Button 
			play.Size = new Size(btnWidth, btnHeight);
			play.Location = new Point(xoffset,0);

			xoffset = xoffset + btnWidth + btnSeperation;

			//Position FastForward as Fifth Button 
			fastForward.Size = new Size(btnWidth, btnHeight);
			fastForward.Location = new Point(xoffset,0);
			
			xoffset = xoffset + btnWidth + btnSeperation;

			//Position Skip as Sixth Button 
			skipHours.Size = new Size(btnWidth, btnHeight);
			skipHours.Location = new Point(xoffset,0);
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
			Width = skipHours.Right +5;
			Height = skipHours.Bottom;
		}

		protected override void _ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			ButtonAction action = (ButtonAction) args.Code;

			switch(action)
			{
				case ButtonAction.Slow:
					if(!_fastForwardOn) return;
					break;

				case ButtonAction.SkipHours:
					if(!_fastForwardOn) return;
					break;
			}

			base._ButtonPressed(sender, args);
		}

		public override void SetState (bool rewindOn, bool playOn, bool pauseOn, bool fastForwardOn, bool preplayOn)
		{
			base.SetState(rewindOn, playOn, pauseOn, fastForwardOn, preplayOn);

			slow.Enabled = fastForwardOn;
			skipHours.Enabled = fastForwardOn;
		}
	}
}