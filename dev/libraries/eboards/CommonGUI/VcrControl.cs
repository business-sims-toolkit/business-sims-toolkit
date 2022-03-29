using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using GameManagement;
using Network;

namespace CommonGUI
{
	public enum VcrButton
	{
		Pause,
		Play,
		Rewind,
		FastForward
	}

	public delegate void VcrButtonPressedHandler (VcrControl sender, VcrButton button);

	public class VcrControl : FlickerFreePanel
	{
		NetworkProgressionGameFile gameFile;
		NodeTree model;
		Node prePlayControl;
		Node prePlayStatus;

		List<VcrButton> buttonTypes;
		Dictionary<VcrButton, ImageTextButton> buttonTypeToButton;

		public event VcrButtonPressedHandler ButtonPressed;

		public VcrControl (NetworkProgressionGameFile gameFile)
		{
			this.gameFile = gameFile;
			model = gameFile.NetworkModel;
			prePlayControl = model.GetNamedNode("preplay_control");
			prePlayStatus = model.GetNamedNode("preplay_status");

			buttonTypes = new List<VcrButton> ();
			buttonTypeToButton = new Dictionary<VcrButton, ImageTextButton> ();

			foreach (var buttonSpec in new [] { new Tuple<VcrButton, string> (VcrButton.Rewind, @"buttons\rewind.png"),
			                                    new Tuple<VcrButton, string> (VcrButton.Pause, @"buttons\rewind.png"),
			                                    new Tuple<VcrButton, string> (VcrButton.Play, @"buttons\rewind.png"),
			                                    new Tuple<VcrButton, string> (VcrButton.FastForward, @"buttons\rewind.png") })
			{
				buttonTypes.Add(buttonSpec.Item1);
				ImageTextButton button = new ImageTextButton (buttonSpec.Item2);
				button.SetAutoSize();
				Controls.Add(button);
				buttonTypeToButton.Add(buttonSpec.Item1, button);					 
			}

			UpdateButtons();
			DoSize();
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);

			DoSize();
		}

		public override Size GetPreferredSize (Size proposedSize)
		{
			return new Size (buttonTypeToButton.Values.Sum(b => b.Width), buttonTypeToButton.Values.Max(b => b.Height));
		}

		void DoSize ()
		{
			int x = 0;
			int gap = (Width - buttonTypeToButton.Values.Sum(b => b.Width)) / buttonTypeToButton.Values.Count;

			foreach (VcrButton buttonType in buttonTypes)
			{
				buttonTypeToButton[buttonType].Bounds = new Rectangle (x, (Height - buttonTypeToButton[buttonType].Height) / 2,
																	   buttonTypeToButton[buttonType].Width, buttonTypeToButton[buttonType].Height);
				x = buttonTypeToButton[buttonType].Right + gap;
			}
		}

		void OnButtonPressed (VcrButton button)
		{
			ButtonPressed?.Invoke(this, button);
		}

		void UpdateButtons ()
		{
		}

		public IDictionary<VcrButton, ImageTextButton> Buttons
		{
			get
			{
				return new Dictionary<VcrButton, ImageTextButton> (buttonTypeToButton);
			}
		}

		public void SetAutoSize ()
		{
			Size = GetPreferredSize(Size.Empty);
		}
	}
}