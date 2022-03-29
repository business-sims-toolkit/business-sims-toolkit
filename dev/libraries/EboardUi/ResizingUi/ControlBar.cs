using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using CommonGUI;
using CoreUtils;
using IncidentManagement;
using LibCore;
using Network;

using StyledDynamicButton = ResizingUi.Button.StyledDynamicButton;

namespace ResizingUi
{
	public class ControlBar : Panel, IDataEntryControlHolder
	{
		public delegate Control ButtonClickHandler ();

	    readonly NodeTree model;
		Control shownPopup;

		IncidentEntryPanel incidentEntryPanel;

	    readonly List<ImageTextButton> buttons;
	    readonly Dictionary<ImageTextButton, ButtonClickHandler> buttonToHandler;

		bool useStyledButtons;

		int textBoxWidth;
		int buttonWidth;
		int verticalMargin;
		int horizontalMargin;
		int horizontalPadding;

		IWatermarker watermarker;

		public IWatermarker Watermarker
		{
			get => watermarker;

			set
			{
				watermarker = value;
				Invalidate();
			}
		}

		public ControlBar (NodeTree model, bool useStyledButtons = false)
		{
			this.model = model;
			this.useStyledButtons = useStyledButtons;

			BackColor = Color.Transparent;

			textBoxWidth = 30;
			buttonWidth = 75;
			verticalMargin = 2;
			horizontalMargin = 2;
			horizontalPadding = 5;

			buttons = new List<ImageTextButton> ();
			buttonToHandler = new Dictionary<ImageTextButton, ButtonClickHandler>();
		}

		public void AddIncidentPanel (int maxIncidentDigits = 2, bool allowAlphabeticalIncidents = false)
		{
			incidentEntryPanel = new IncidentEntryPanel (model, useStyledButtons)
			{
				TextBoxWidth = textBoxWidth,
				ButtonWidth = buttonWidth,
				VerticalMargin = verticalMargin,
				HorizontalMargin = horizontalMargin,
				HorizontalPadding = horizontalPadding,
				AllowLetters = allowAlphabeticalIncidents,
				MaxDigits = maxIncidentDigits
			};
			Controls.Add(incidentEntryPanel);

			incidentEntryPanel.Font = SkinningDefs.TheInstance.GetFont(9, FontStyle.Bold);

			incidentEntryPanel.SetIncidentApplier(incidentApplier);
		}

		public ImageTextButton AddButton (string text, ButtonClickHandler clickHandler)
		{
			ImageTextButton button;

			if (useStyledButtons)
			{
				button = new StyledDynamicButton ("standard", text);
			}
			else
			{
				button = ImageTextButton.CreateButton(text);
			}
			button.Font = SkinningDefs.TheInstance.GetFont(9, FontStyle.Bold);

			button.ButtonPressed += button_ButtonPressed;

			buttons.Add(button);
			Controls.Add(button);

			buttonToHandler.Add(button, clickHandler);

			DoSize();

			return button;
		}

		void button_ButtonPressed (object sender, ImageButtonEventArgs args)
		{
			var button = (ImageTextButton) sender;

			var previousPopup = shownPopup;
			var newPopup = buttonToHandler[button]();
			if (newPopup != null)
			{
				if ((previousPopup != null)
					&& (previousPopup.GetType() == newPopup.GetType()))
				{
					newPopup.Dispose();

					previousPopup.Dispose();
					shownPopup = null;
					SelectButton(null);
				}
				else
				{
					previousPopup?.Dispose();
					shownPopup = newPopup;
					SelectButton(button);

					newPopup.Select();
					newPopup.Focus();
				}
			}
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);

			DoSize();
		}

		void DoSize ()
		{
			int x = 0;

			if (incidentEntryPanel != null)
			{
				incidentEntryPanel.Bounds = new Rectangle (x, 0, incidentEntryPanel.GetPreferredSize(Size).Width, Height);
				x = incidentEntryPanel.Right;
			}

			x += horizontalPadding;

			foreach (var button in buttons)
			{
				button.Bounds = new Rectangle (x, verticalMargin, buttonWidth, Height - (2 * verticalMargin));
				x = button.Right + horizontalPadding;
			}

			Invalidate();
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing)
			{
				incidentEntryPanel?.Dispose();
			}

			base.Dispose(disposing);
		}

		public void SetIncidentApplier (IncidentApplier incidentApplier)
		{
			incidentEntryPanel?.SetIncidentApplier(incidentApplier);
		}

		public void EnableIncidentPanel (bool enable)
		{
			incidentEntryPanel.Enabled = enable;
		}

		public int TextBoxWidth
		{
			get => textBoxWidth;

			set
			{
				textBoxWidth = value;

				if (incidentEntryPanel != null)
				{
					incidentEntryPanel.TextBoxWidth = textBoxWidth;
				}

				DoSize();
			}
		}

		public int ButtonWidth
		{
			get => buttonWidth;

			set
			{
				buttonWidth = value;

				if (incidentEntryPanel != null)
				{
					incidentEntryPanel.ButtonWidth = buttonWidth;
				}

				DoSize();
			}
		}

		public int VerticalMargin
		{
			get => verticalMargin;

			set
			{
				verticalMargin = value;

				if (incidentEntryPanel != null)
				{
					incidentEntryPanel.VerticalMargin = verticalMargin;
				}

				DoSize();
			}
		}

		public int HorizontalMargin
		{
			get => horizontalMargin;

			set
			{
				horizontalMargin = value;

				if (incidentEntryPanel != null)
				{
					incidentEntryPanel.HorizontalMargin = horizontalMargin;
				}

				DoSize();
			}
		}

		public int HorizontalPadding
		{
			get => horizontalPadding;

			set
			{
				horizontalPadding = value;

				if (incidentEntryPanel != null)
				{
					incidentEntryPanel.HorizontalPadding = horizontalPadding;
				}

				DoSize();
			}
		}

		public void DisposeEntryPanel ()
		{
			if (shownPopup != null)
			{
				shownPopup.Dispose();
				shownPopup = null;

				SelectButton(null);
			}

			incidentEntryPanel?.Select();
			incidentEntryPanel?.Focus();
		}

		void SelectButton (ImageTextButton button)
		{
			foreach (var otherButton in buttons)
			{
				otherButton.Active = (otherButton == button);
			}
		}

		public void DisposeEntryPanel_indirect (int which)
		{
			throw new NotImplementedException();
		}

		public void SwapToOtherPanel (int which_operation)
		{
			throw new NotImplementedException();
		}

		IncidentApplier incidentApplier;

		public IncidentApplier IncidentApplier
		{
			get => incidentApplier;

			set
			{
				incidentApplier = value;

				if (incidentEntryPanel != null)
				{
					incidentEntryPanel.SetIncidentApplier(value);
				}
			}
		}

		protected override void OnPaint (PaintEventArgs e)
		{
			base.OnPaint(e);

		    var poweredByImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + @"\images\panels\low_poweredby_logo.png");

			int x = 0;

			if (incidentEntryPanel != null)
			{
				x = incidentEntryPanel.Right;
			}

			if (buttons.Count > 0)
			{
				x = buttons[buttons.Count - 1].Right;
			}

			var destinationRectangle = new Rectangle (x + 10, (Height - poweredByImage.Height) / 2, poweredByImage.Width, poweredByImage.Height);
			if ((Width - destinationRectangle.Left) < poweredByImage.Width)
			{
				destinationRectangle.Width = Width - destinationRectangle.Left;
				destinationRectangle.Height = poweredByImage.Height * destinationRectangle.Width / poweredByImage.Width;
				destinationRectangle.Y = (Height - destinationRectangle.Height) / 2;
			}

			e.Graphics.DrawImage(poweredByImage, destinationRectangle);

			watermarker?.Draw(this, e.Graphics);
		}
	}
}