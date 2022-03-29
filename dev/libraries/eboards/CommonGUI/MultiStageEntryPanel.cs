using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace CommonGUI
{
	public class MultiStageEntryPanel : FlickerFreePanel
	{
		List<Control> stages;
		Control currentStage;

		Panel buttonPanel;
		ImageTextButton back;
		ImageTextButton next;
		ImageTextButton cancel;
		ImageTextButton ok;

		public delegate void CompletedHandler (MultiStageEntryPanel sender, EventArgs args);
		public delegate void CancelledHandler (MultiStageEntryPanel sender, EventArgs args);

		public event CompletedHandler Completed;
		public event CancelledHandler Cancelled;

		public MultiStageEntryPanel ()
		{
			stages = new List<Control> ();
			currentStage = null;

			buttonPanel = new Panel ();
			Controls.Add(buttonPanel);

			back = new ImageTextButton (@"\images\buttons\blank_med.png");
			back.SetButtonText("< Back");
			back.ButtonPressed += back_ButtonPressed;
			buttonPanel.Controls.Add(back);

			next = new ImageTextButton (@"\images\buttons\blank_med.png");
			next.SetButtonText("> Next");
			next.ButtonPressed += next_ButtonPressed;
			buttonPanel.Controls.Add(next);

			cancel = new ImageTextButton (@"\images\buttons\blank_med.png");
			cancel.SetButtonText("Cancel");
			cancel.ButtonPressed += cancel_ButtonPressed;
			buttonPanel.Controls.Add(cancel);

			ok = new ImageTextButton (@"\images\buttons\blank_med.png");
			ok.SetButtonText("OK");
			ok.ButtonPressed += ok_ButtonPressed;
			buttonPanel.Controls.Add(ok);

			DoSize();
			DoButtons();
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing)
			{
			}

			base.Dispose(disposing);
		}

		public void AddStagePanel (Control stage)
		{
			Controls.Add(stage);
			stages.Add(stage);
			if (stages.Count == 1)
			{
				ShowStage(stage);
			}

			DoSize();
			DoButtons();
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);

			DoSize();
		}

		void DoSize ()
		{
			Size buttonSize = new Size (75, 30);
			int gutter = 10;
			int horizontalGap = (Width - (buttonPanel.Controls.Count * buttonSize.Width)) / (buttonPanel.Controls.Count + 1);

			buttonPanel.Size = new Size (Width, (2 * gutter) + buttonSize.Height);
			buttonPanel.Location = new Point (0, Height - buttonPanel.Height);

			back.Location = new Point (horizontalGap, gutter);
			back.Size = buttonSize;

			next.Location = new Point ((2 * horizontalGap) + buttonSize.Width, gutter);
			next.Size = buttonSize;

			cancel.Location = new Point ((3 * horizontalGap) + (2 * buttonSize.Width), gutter);
			cancel.Size = buttonSize;

			ok.Location = new Point ((4 * horizontalGap) + (3 * buttonSize.Width), gutter);
			ok.Size = buttonSize;

			if (currentStage != null)
			{
				currentStage.Location = new Point (0, 0);
				currentStage.Size = new Size (Width, buttonPanel.Top);
			}
		}

		void DoButtons ()
		{
			int index = stages.IndexOf(currentStage);

			back.Enabled = (index > 0);
			next.Enabled = (index < (stages.Count - 1));

			ok.Enabled = (index == (stages.Count - 1));
			cancel.Enabled = true;
		}

		public void ShowStage (Control stage)
		{
			if (currentStage != stage)
			{
				if (currentStage != null)
				{
					currentStage.Hide();
				}

				if (stage != null)
				{
					stage.Show();
					stage.Select();
				}

				currentStage = stage;
			}

			DoSize();
			DoButtons();
		}

		void back_ButtonPressed (object sender, ImageButtonEventArgs args)
		{
			ShowStage(stages[stages.IndexOf(currentStage) - 1]);
		}

		void next_ButtonPressed (object sender, ImageButtonEventArgs args)
		{
			ShowStage(stages[stages.IndexOf(currentStage) + 1]);
		}

		void cancel_ButtonPressed (object sender, ImageButtonEventArgs args)
		{
			OnCancelled();
		}

		void ok_ButtonPressed (object sender, ImageButtonEventArgs args)
		{
			OnCompleted();
		}

		void OnCancelled ()
		{
			Cancelled?.Invoke(this, EventArgs.Empty);
		}

		void OnCompleted ()
		{
			Completed?.Invoke(this, EventArgs.Empty);
		}

		public IEnumerable<Control> Stages
		{
			get
			{
				return stages;
			}
		}
	}

	internal class Test
	{
		void Testy ()
		{
			MultiStageEntryPanel panel = new MultiStageEntryPanel ();

			Panel firstStage = new Panel ();
			panel.AddStagePanel(firstStage);

			Panel secondStage = new Panel();
			panel.AddStagePanel(secondStage);

			panel.Completed += panel_Completed;
			panel.Cancelled += panel_Cancelled;

			panel.ShowStage(firstStage);
		}

		void panel_Cancelled (MultiStageEntryPanel sender, EventArgs args)
		{
		}

		void panel_Completed (MultiStageEntryPanel sender, EventArgs args)
		{
		}
	}
}