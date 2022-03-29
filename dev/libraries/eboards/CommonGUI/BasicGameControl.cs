using System;
using System.Windows.Forms;
using System.Drawing;

using CoreUtils;
using LibCore;

namespace CommonGUI
{
	/// <summary>
	/// Summary description for BasicGameControl.
	/// </summary>
	public class BasicGameControl : BasePanel, ITimedClass
	{
		public enum ButtonAction
		{
			Play,
			Pause,
			Rewind,
			FastForward
		}

		public delegate void ButtonActionHandler(object sender, ButtonAction action);
		public event ButtonActionHandler ButtonPressed;

		protected Button play;
		protected Button pause;
		protected Button rewind;
		protected ComboBox fastForward;

		protected int fastForwardSpeed = 1;
		//
		public int FastForwardSpeed
		{
			get
			{
				return fastForwardSpeed;
			}
		}
		//
		public BasicGameControl()
		{
			SuspendLayout();
			BackColor = Color.LightGray;
			//
			play = new Button();
			play.Text = "Play";
			play.Size = new Size(50,30);
			play.Location = new Point(0,0);
			play.Click += play_Click;
            play.Name = "BGC play Button";
			Controls.Add(play);
			//
			pause = new Button();
			pause.Text = "Pause";
			pause.Size = new Size(50,30);
			pause.Location = new Point(50,0);
			pause.Click += pause_Click;
            pause.Name = "pause button";
			Controls.Add(pause);
			//
			rewind = new Button();
			rewind.Text = "Rewind";
			rewind.Size = new Size(50,30);
			rewind.Location = new Point(100,0);
			rewind.Click += rewind_Click;
            rewind.Name = "rewind Button";
			Controls.Add(rewind);
			//
			fastForward = new ComboBox();
			//fastForward.Text = "Fast Forward";
			fastForward.DropDownStyle = ComboBoxStyle.DropDownList;
			fastForward.Items.Add(1);
			fastForward.Items.Add(2);
			fastForward.Items.Add(4);
			fastForward.Items.Add(10);
			fastForward.Items.Add(30);
			fastForward.Items.Add(60);
			fastForward.Size = new Size(50,30);
			fastForward.Location = new Point(150,0);
			//fastForward.Click += new EventHandler(fastForward_Click);
			fastForward.SelectedIndex = 0;
			fastForward.SelectedIndexChanged += fastForward_SelectedIndexChanged;
			Controls.Add(fastForward);
			//
			TimeManager.TheInstance.ManageClass(this);
			//
			ResumeLayout(false);
		}

		public void SetState(bool playOn, bool pauseOn, bool rewindOn, bool fastForwardOn)
		{
			play.Enabled = playOn;
			pause.Enabled = pauseOn;
			rewind.Enabled = rewindOn;
			fastForward.Enabled = fastForwardOn;
		}

		void play_Click(object sender, EventArgs e)
		{
			ButtonPressed?.Invoke(this, ButtonAction.Play);
		}

		void pause_Click(object sender, EventArgs e)
		{
			ButtonPressed?.Invoke(this, ButtonAction.Pause);
		}

		void rewind_Click(object sender, EventArgs e)
		{
			ButtonPressed?.Invoke(this, ButtonAction.Rewind);
		}

		void fastForward_Click(object sender, EventArgs e)
		{
			ButtonPressed?.Invoke(this, ButtonAction.FastForward);
		}

		void fastForward_SelectedIndexChanged(object sender, EventArgs e)
		{
			if(null != ButtonPressed)
			{
				fastForwardSpeed = (int)fastForward.Items[ fastForward.SelectedIndex ];
				ButtonPressed(this, ButtonAction.FastForward);
				//TimeManager.TheInstance.FastForward((int)fastForward.Items[ fastForward.SelectedIndex ] );
			}
		}
		#region ITimedClass Members

		public void Start()
		{
		}

		public void FastForward(double timesRealTime)
		{
			// We don't change our state for this case.
		}

		public void Reset()
		{
		}

		public void Stop()
		{
		}

		#endregion
	
		protected override void Dispose(bool disposing)
		{
			if(disposing)
			{
				TimeManager.TheInstance.UnmanageClass(this);
			}
			base.Dispose (disposing);
		}
	}
}
