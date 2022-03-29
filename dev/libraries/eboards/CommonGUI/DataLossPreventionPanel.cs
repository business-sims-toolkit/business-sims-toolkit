using System;
using System.Collections;
using System.Collections.Generic;

using System.Drawing;
using System.Windows.Forms;
using Network;

using LibCore;
using CoreUtils;

namespace CommonGUI
{
	public class DataLossPreventionPanel : Panel
	{
		class DlpControls
		{
			public Label Label;
			public Control Start;
			public Control Stop;

			public DlpControls (Label label, Control start, Control stop)
			{
				Label = label;
				Start = start;
				Stop = stop;
			}
		}

		NodeTree model;
		Dictionary<Node, DlpControls> regionNodeToButtons;

		Font bigFont;
		Font buttonFont;

		Label title;

		Color upColour = Color.White;
		Color downColour = Color.White;
		Color hoverColour = Color.White;
		Color disabledColour = Color.FromArgb(102, 102, 102);

		ImageTextButton ok;
		ImageTextButton cancel;

		bool training;

		IDataEntryControlHolder controlPanel;

		FocusJumper focusJumper;

		Dictionary<Node, bool> regionNodeToAssignedStartedState;

		public DataLossPreventionPanel (IDataEntryControlHolder controlPanel, NodeTree model, bool training)
		{
			this.controlPanel = controlPanel;
			this.training = training;
			string background = "race_panel_back_normal.png";
			if (training)
			{
				background = "race_panel_back_training.png";
			}
			BackgroundImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\panels\\" + background);

			string fontName = SkinningDefs.TheInstance.GetData("fontname");
			bigFont = ConstantSizeFont.NewFont(fontName, 10, FontStyle.Bold);
			buttonFont = ConstantSizeFont.NewFont(fontName, 9);

			focusJumper = new FocusJumper();

			title = new Label();
			title.BackColor = Color.Transparent;
			title.ForeColor = Color.White;
			title.Text = "Data Loss Prevention";
			title.Font = bigFont;
			Controls.Add(title);

			ok = new ImageTextButton(0);
			ok.ButtonFont = buttonFont;
			ok.SetVariants("/images/buttons/blank_big.png");
			ok.SetButtonText("OK", upColour, upColour, hoverColour, disabledColour);
			ok.Click += ok_Click;
			Controls.Add(ok);
			focusJumper.Add(ok);

			cancel = new ImageTextButton(0);
			cancel.ButtonFont = buttonFont;
			cancel.SetVariants("/images/buttons/blank_big.png");
			cancel.SetButtonText("Cancel", upColour, upColour, hoverColour, disabledColour);
			cancel.Click += cancel_Click;
			Controls.Add(cancel);
			focusJumper.Add(cancel);

			this.model = model;

			regionNodeToButtons = new Dictionary<Node, DlpControls> ();
			regionNodeToAssignedStartedState = new Dictionary<Node, bool>();

			Node regions = model.GetNamedNode("Regions");
			foreach (Node region in regions.getChildren())
			{
				AddRegion(region);
			}
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing)
			{
				List<Node> regionsToRemove = new List<Node> (regionNodeToButtons.Keys);
				foreach (Node region in regionsToRemove)
				{
					RemoveRegion(region);
				}
				bigFont.Dispose();
				buttonFont.Dispose();
				focusJumper.Dispose();
			}
			base.Dispose(disposing);
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);
			DoSize();
		}

		void AddRegion (Node region)
		{
			Label label = new Label();
			label.Text = region.GetAttribute("desc");
			label.TextAlign = ContentAlignment.MiddleLeft;
			label.Font = buttonFont;
			Controls.Add(label);

			ImageTextButton start = new ImageTextButton(0);
			start.SetVariants("/images/buttons/blank_small.png");
			start.ButtonFont = buttonFont;
			start.Click += button_Click;
			start.SetButtonText("Start", upColour, upColour, hoverColour, disabledColour);
			Controls.Add(start);

			ImageTextButton stop = new ImageTextButton(0);
			stop.SetVariants("/images/buttons/blank_small.png");
			stop.ButtonFont = buttonFont;
			stop.Click += button_Click;
			stop.SetButtonText("Stop", upColour, upColour, hoverColour, disabledColour);
			Controls.Add(stop);

			DlpControls controls = new DlpControls (label, start, stop);
			regionNodeToButtons.Add(region, controls);

			Node securityNode = GetSecurityNodeByRegionNode(region);

			securityNode.AttributesChanged += securityNode_AttributesChanged;

			focusJumper.Add(start);
			focusJumper.Add(stop);

			regionNodeToAssignedStartedState[region] = securityNode.GetBooleanAttribute("on", false);

			UpdateButtons();
		}

		Node GetSecurityNodeByRegionNode (Node region)
		{
			return model.GetNamedNode("Security_Protection_" + region.GetAttribute("name"));
		}

		void securityNode_AttributesChanged (Node sender, ArrayList attrs)
		{
			UpdateButtons();
		}

		void RemoveRegion (Node region)
		{
			Controls.Remove(regionNodeToButtons[region].Label);
			Controls.Remove(regionNodeToButtons[region].Start);
			Controls.Remove(regionNodeToButtons[region].Stop);

			focusJumper.Remove(regionNodeToButtons[region].Start);
			focusJumper.Remove(regionNodeToButtons[region].Stop);

			Node securityNode = GetSecurityNodeByRegionNode(region);
			securityNode.AttributesChanged -= securityNode_AttributesChanged;

			regionNodeToButtons.Remove(region);
		}

		void button_Click (object sender, EventArgs e)
		{
			foreach (Node region in regionNodeToButtons.Keys)
			{
				if (regionNodeToButtons[region].Start == sender)
				{
					regionNodeToAssignedStartedState[region] = true;
				}
				else if (regionNodeToButtons[region].Stop == sender)
				{
					regionNodeToAssignedStartedState[region] = false;
				}
			}
			UpdateButtons();
		}

		void cancel_Click (object sender, EventArgs e)
		{
			controlPanel.DisposeEntryPanel();
		}

		void ok_Click (object sender, EventArgs e)
		{
			foreach (Node region in regionNodeToAssignedStartedState.Keys)
			{
				Node securityNode = GetSecurityNodeByRegionNode(region);
				if (securityNode.GetBooleanAttribute("on", false) != regionNodeToAssignedStartedState[region])
				{
					securityNode.SetAttribute("on", regionNodeToAssignedStartedState[region]);
				}
			}
			controlPanel.DisposeEntryPanel();
		}

		void UpdateButtons ()
		{
			bool someDiffer = false;

			foreach (Node region in regionNodeToButtons.Keys)
			{
				Node securityNode = GetSecurityNodeByRegionNode(region);

				someDiffer = someDiffer || (securityNode.GetBooleanAttribute("on", false) != regionNodeToAssignedStartedState[region]);

				regionNodeToButtons[region].Stop.Enabled = regionNodeToAssignedStartedState[region];
				regionNodeToButtons[region].Start.Enabled = (!regionNodeToButtons[region].Stop.Enabled);
			}

			ok.Enabled = someDiffer;
		}

		void DoSize ()
		{
			int buttonWidth = 100;
			int height = 20;
			int xGap = 10;
			int yGap = 20;

			title.Location = new Point (10, 1);
			title.Size = new Size(Width - title.Left, 30);

			cancel.Size = new Size (80, height);
			cancel.Location = new Point(Width - cancel.Width - 5, Height - cancel.Height - 5);

			ok.Size = new Size (80, height);
			ok.Location = new Point(cancel.Left - ok.Width - xGap, Height - ok.Height - 5);

			int y = title.Bottom + 10;
			foreach (Node region in regionNodeToButtons.Keys)
			{
				Label label = regionNodeToButtons[region].Label;
				Control start = regionNodeToButtons[region].Start;
				Control stop = regionNodeToButtons[region].Stop;

				label.Size = new Size (75, height);
				label.Location = new Point (10, y);

				start.Size = new Size (buttonWidth, height);
				start.Location = new Point (label.Right + xGap, label.Top);

				stop.Size = new Size (buttonWidth, height);
				stop.Location = new Point (start.Right + xGap, start.Top);

				y += height + yGap;
			}
		}

		public void SetFocus ()
		{
			Focus();
			cancel.Focus();
		}
	}
}