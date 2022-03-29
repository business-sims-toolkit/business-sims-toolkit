using System;
using System.Drawing;

using System.Windows.Forms;
using ResizingUi.Button;

namespace SecondaryDisplay
{
	internal class WindowControlBar : Panel
	{
		// TODO add optional close button too??
		public WindowControlBar ()
		{
			var buttonSize = new Size(26, 24);

			// TODO should setup a style in the skin for these
			minimiseButton = new StyledImageButton(0, true)
			{
				Size = buttonSize
			};
			minimiseButton.SetVariants(@"images\buttons\minimise.png");
			Controls.Add(minimiseButton);
			minimiseButton.Click += minimiseButton_Click;

			maximiseButton = new StyledImageButton(0, true)
			{
				Size = buttonSize
			};
			maximiseButton.SetVariants(@"images\buttons\maximise.png");
			Controls.Add(maximiseButton);
			maximiseButton.Click += maximiseButton_Click;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (Parent != null)
				{
					Parent.ParentChanged -= parent_ParentChanged;
				}

				if (parentForm != null)
				{
					parentForm.ClientSizeChanged -= parentForm_ClientSizeChanged;
				}
			}

			base.Dispose(disposing);
		}

		protected override void OnParentChanged(EventArgs e)
		{
			if (Parent == null)
			{
				return;
			}

			Parent.ParentChanged += parent_ParentChanged;
		}

		void parent_ParentChanged(object sender, EventArgs e)
		{
			if (TopLevelControl != null)
			{
				parentForm = (Form)TopLevelControl;
				parentForm.ClientSizeChanged += parentForm_ClientSizeChanged;
			}
		}

		void parentForm_ClientSizeChanged(object sender, EventArgs e)
		{
			OnParentFormWindowStateChanged();

		}

		protected override void OnSizeChanged(EventArgs e)
		{
			DoSize();
		}

		void DoSize()
		{
			const int padding = 10;

			var buttonLeft = Width - padding;
			const int buttonTop = padding / 2;

			var buttons = new[]
			{
                //closeButton,
                maximiseButton, minimiseButton
			};

			foreach (var button in buttons)
			{
				buttonLeft -= button.Width;
				button.Location = new Point(buttonLeft, buttonTop);
				buttonLeft -= padding / 2;
			}
		}

		void OnParentFormWindowStateChanged()
		{
			var isMaximised = parentForm?.WindowState == FormWindowState.Maximized;
			maximiseButton.SetVariants(
				$@"images\buttons\{(isMaximised ? "restore" : "maximise")}.png");
		}

		void minimiseButton_Click(object sender, EventArgs e)
		{
			if (parentForm == null)
			{
				return;
			}
			parentForm.WindowState = FormWindowState.Minimized;
		}

		void maximiseButton_Click(object sender, EventArgs e)
		{
			if (parentForm == null)
			{
				return;
			}
			var isMaximised = parentForm.WindowState == FormWindowState.Maximized;
			parentForm.WindowState = isMaximised ? FormWindowState.Normal : FormWindowState.Maximized;

		}


		Form parentForm;

		readonly StyledImageButton minimiseButton;
		readonly StyledImageButton maximiseButton;
	}
}
