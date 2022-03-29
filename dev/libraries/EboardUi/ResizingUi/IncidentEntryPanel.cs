using System;
using System.Drawing;
using System.Windows.Forms;

using CommonGUI;
using CoreUtils;
using IncidentManagement;
using Network;

using StyledDynamicButton = ResizingUi.Button.StyledDynamicButton;

namespace ResizingUi
{
	public class IncidentEntryPanel : Panel
	{
	    readonly NodeTree model;
		IncidentApplier incidentApplier;

	    readonly FilteredTextBox textBox;
	    readonly ImageTextButton add;
	    readonly ImageTextButton remove;

		int textBoxWidth;
		int buttonWidth;
		int verticalMargin;
		int horizontalMargin;
		int horizontalPadding;

		bool allowLetters;
		int maxDigits;

		public IncidentEntryPanel (NodeTree model, bool useStyledButtons = false)
		{
			this.model = model;

			textBoxWidth = 50;
			buttonWidth = 75;
			verticalMargin = 2;
			horizontalMargin = 2;
			horizontalPadding = 5;

			allowLetters = false;
			maxDigits = 2;

			textBox = new FilteredTextBox (TextBoxFilterType.Unfiltered) { TextAlign = HorizontalAlignment.Left };
			textBox.KeyPress += textBox_KeyPress;
			textBox.TextChanged += textBox_TextChanged;
			Controls.Add(textBox);

			if (useStyledButtons)
			{
				add = new StyledDynamicButton ("standard", "Enter Incident");
				remove = new StyledDynamicButton ("standard", "Remove Incident");
			}
			else
			{
				add = ImageTextButton.CreateButton("Enter Incident");
				remove = ImageTextButton.CreateButton("Remove Incident");
			}

			add.ButtonPressed += add_ButtonPressed;
			Controls.Add(add);

			remove.ButtonPressed += remove_ButtonPressed;
			Controls.Add(remove);

			UpdateState();
			DoSize();
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing)
			{
				textBox.Dispose();
				add.Dispose();
				remove.Dispose();
			}

			base.Dispose(disposing);
		}

		protected override void OnFontChanged (EventArgs e)
		{
			base.OnFontChanged(e);

			add.Font = Font;
			remove.Font = Font;

			Invalidate();
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);
			DoSize();
		}

		protected override void OnGotFocus (EventArgs e)
		{
			base.OnGotFocus(e);
			textBox.Focus();
		}

		protected override void OnEnabledChanged (EventArgs e)
		{
			base.OnEnabledChanged(e);

			UpdateState();

			if (Enabled)
			{
				Select();
				Focus();

				textBox.Select();
				textBox.Focus();
			}
		}

		void DoSize ()
		{
			int x = horizontalMargin;

			textBox.Bounds = new Rectangle (x, verticalMargin, textBoxWidth, Height - (2 * verticalMargin));
			x = textBox.Right + horizontalPadding;

			add.Bounds = new Rectangle (x, verticalMargin, buttonWidth, Height - (2 * verticalMargin));
			x = add.Right + horizontalPadding;

			remove.Bounds = new Rectangle (x, verticalMargin, buttonWidth, Height - (2 * verticalMargin));
			x = remove.Right + horizontalPadding;

			textBox.Font = SkinningDefs.TheInstance.GetPixelSizedFont((Height - (2 * verticalMargin)) * 3 / 5, FontStyle.Bold);
		}

		void UpdateState ()
		{
			textBox.MaxLength = maxDigits;
			textBox.FilterType = (allowLetters ? TextBoxFilterType.Alphanumeric : TextBoxFilterType.Digits);
			textBox.Enabled = Enabled;

			bool incidentExists = ((! string.IsNullOrEmpty(textBox.Text))
			                       && (incidentApplier?.GetIncident(textBox.Text) != null));

			add.Enabled = Enabled && incidentExists;
			remove.Enabled = Enabled && incidentExists;
		}

		public int TextBoxWidth
		{
			get => textBoxWidth;

			set
			{
				textBoxWidth = value;
				DoSize();
			}
		}

		public int ButtonWidth
		{
			get => buttonWidth;

			set
			{
				buttonWidth = value;
				DoSize();
			}
		}

		public int VerticalMargin
		{
			get => verticalMargin;

			set
			{
				verticalMargin = value;
				DoSize();
			}
		}

		public int HorizontalMargin
		{
			get => horizontalMargin;

			set
			{
				horizontalMargin = value;
				DoSize();
			}
		}

		public int HorizontalPadding
		{
			get => horizontalPadding;

			set
			{
				horizontalPadding = value;
				DoSize();
			}
		}

		public bool AllowLetters
		{
			get => allowLetters;

			set
			{
				allowLetters = value;
				UpdateState();
				DoSize();
			}
		}

		public int MaxDigits
		{
			get => maxDigits;

			set
			{
				maxDigits = value;
				UpdateState();
				DoSize();
			}
		}

		public override Size GetPreferredSize (Size proposedSize)
		{
			return new Size (horizontalMargin + textBoxWidth + horizontalPadding + buttonWidth + horizontalPadding + buttonWidth + horizontalMargin, proposedSize.Height);
		}

		void textBox_KeyPress (object sender, KeyPressEventArgs args)
		{
			if (args.KeyChar == 13)
			{
				EnterIncident();
				args.Handled = true;
			}
		}

		void textBox_TextChanged (object sender, EventArgs args)
		{
			UpdateState();
		}

		void add_ButtonPressed (object sender, ImageButtonEventArgs args)
		{
			EnterIncident();
		}

		void remove_ButtonPressed (object sender, ImageButtonEventArgs args)
		{
			RemoveIncident();
		}

		void EnterIncident ()
		{
			if (add.Enabled)
			{
				var incidentEntryQueue = model.GetNamedNode("enteredIncidents");
				new Node (incidentEntryQueue, "IncidentNumber", "", new AttributeValuePair("id", textBox.Text.ToUpper()));

				textBox.Clear();
			}
		}

		void RemoveIncident ()
		{
			if (remove.Enabled)
			{
				var fixItQueue = model.GetNamedNode("FixItQueue");
				new Node (fixItQueue, "entrypanel_fix", "", new AttributeValuePair("incident_id", textBox.Text.ToUpper()));

				textBox.Clear();
			}
		}

		public void SetIncidentApplier (IncidentApplier incidentApplier)
		{
			this.incidentApplier = incidentApplier;
		}
	}
}