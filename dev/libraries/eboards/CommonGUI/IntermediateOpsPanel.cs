using System;
using System.Windows.Forms;
using System.Drawing;
using System.Collections;

using LibCore;
using CoreUtils;

namespace CommonGUI
{
	/// <summary>
	/// An ops entry panel with a series of buttons, each of which opens another ops entry panel.
	/// </summary>
	public class IntermediateOpsPanel : FlickerFreePanel
	{
		FocusJumper focusJumper;
		OpsControlPanel tcp;

		ImageTextButton cancelButton;
		ArrayList otherButtons;

		public IntermediateOpsPanel (OpsControlPanel tcp, bool trainingMode, Color backColour, int buttonWidth,
		                             string title, string [] buttons, EventHandler [] eventHandlers)
		{
			string mediumButtonImage = "/images/buttons/blank_med.png";
			string smallButtonImage = "/images/buttons/blank_small.png";

			ForeColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("race_paneltitleforecolor", Color.Black);
			BackColor = backColour;
			string fontName = TextTranslator.TheInstance.GetTranslateFont(SkinningDefs.TheInstance.GetData("fontname"));

            Font fontBold9 = ConstantSizeFont.NewFont(fontName, 9, FontStyle.Bold);
            Font fontBold10 = ConstantSizeFont.NewFont(fontName, 10, FontStyle.Bold);
			Font fontBold12 = ConstantSizeFont.NewFont(fontName, 12, FontStyle.Bold);

			if (trainingMode)
			{
				BackgroundImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location +
				                                                  "\\images\\panels\\race_panel_back_training.png");
			}
			else
			{
				BackgroundImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location +
				                                                  "\\images\\panels\\race_panel_back_normal.png");
			}

			SuspendLayout();
			SetStyle(ControlStyles.Selectable, true);
			focusJumper = new FocusJumper ();

			this.tcp = tcp;

			Label header = new Label ();
			header.Text = TextTranslator.TheInstance.Translate(title);
			header.Size = new Size (500, 20);
			header.Font = fontBold12;
			header.Location = new Point (0,0);
			header.BackColor = Color.Transparent;
			header.ForeColor = ForeColor;
			Controls.Add(header);

			Color upColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_text_colour", Color.Black);
			Color downColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_down_text_colour", Color.White);
			Color hoverColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_hover_text_colour", Color.Green);
			Color disabledColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_disabled_text_colour", Color.DarkGray);

			cancelButton = new ImageTextButton (0);
			cancelButton.ButtonFont = fontBold10;
			cancelButton.SetVariants(smallButtonImage);
			cancelButton.Size = new Size (80, 20);
			cancelButton.SetButtonText("Close", upColor, upColor, hoverColor, disabledColor);
			cancelButton.Click += cancelButton_Click;
			Controls.Add(cancelButton);

			focusJumper.Add(cancelButton);

			ResumeLayout(false);

			GotFocus += IntermediateOpsPanel_GotFocus;
			Resize += IntermediateOpsPanel_Resize;

			otherButtons = new ArrayList ();
			for (int i = 0; i < buttons.Length; i++)
			{
				string buttonText = (string) buttons[i];
				ImageTextButton button = new ImageTextButton (0);
				otherButtons.Add(button);
				button.ButtonFont = fontBold9;
				button.SetVariants(mediumButtonImage);
				button.Size = new Size (buttonWidth, 20);
				button.SetButtonText(buttonText, upColor, upColor, hoverColor, disabledColor);

				if (i < eventHandlers.Length)
				{
					button.Click += eventHandlers[i];
				}

				Controls.Add(button);
				focusJumper.Add(button);
			}

			DoLayout();
		}

		void cancelButton_Click (object sender, EventArgs args)
		{
			tcp.DisposeEntryPanel();
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing)
			{
				focusJumper.Dispose();
			}

			base.Dispose(disposing);
		}

		protected void IntermediateOpsPanel_GotFocus (object sender, EventArgs args)
		{
			cancelButton.Focus();
		}

		protected void IntermediateOpsPanel_Resize (object sender, EventArgs e)
		{
			DoLayout();
		}

		void DoLayout ()
		{
            cancelButton.Location = new Point(520, 185);
			int xGap = 20;
			int yGap = 10;
			int left = 30;

			int x = left;
			int y = 60;

			foreach (Control button in otherButtons)
			{
				button.Location = new Point (x, y);
				x += button.Width + xGap;

				if ((x + button.Width) >= Width)
				{
					x = left;
					y += button.Height + yGap;
				}
			}
		}
	}
}