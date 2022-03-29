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
	public class AuditPanel : Panel
	{
		class AuditControls
		{
			public Label Label;
			public ImageTextButton Start;
			public ImageTextButton Stop;
			public ImageBox Tick;

			public AuditControls (Label label, ImageTextButton start, ImageTextButton stop, ImageBox tick)
			{
				Label = label;
				Start = start;
				Stop = stop;
				Tick = tick;
			}
		}

		NodeTree model;
		Dictionary<Node, AuditControls> auditNodeToButtons;

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
		int current_round = 1;

		IDataEntryControlHolder controlPanel;

		FocusJumper focusJumper;

		Dictionary<Node, bool> auditNodeToAssignedStartedState;

		public AuditPanel (IDataEntryControlHolder controlPanel, NodeTree model, bool training, int round)
		{
			this.controlPanel = controlPanel;
			this.training = training;
			current_round = round;
			string background = "race_panel_back_normal.png";
			if (training)
			{
				background = "race_panel_back_training.png";
			}
			BackgroundImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\panels\\" + background);

			string fontName = SkinningDefs.TheInstance.GetData("fontname");
			bigFont = ConstantSizeFont.NewFont(fontName, 10, FontStyle.Bold);
			buttonFont = ConstantSizeFont.NewFont(fontName, 9);

			focusJumper = new FocusJumper ();

			title = new Label();
			title.BackColor = Color.Transparent;
			title.ForeColor = Color.White;
			title.Text = "Audits";
			title.Font = bigFont;
			Controls.Add(title);

			ok = new ImageTextButton(0);
			ok.ButtonFont = buttonFont;
			ok.SetVariants("/images/buttons/blank_big.png");
			ok.SetButtonText("OK", upColour, upColour, hoverColour, disabledColour);
			ok.Click += ok_Click;
			Controls.Add(ok);
			focusJumper.Add(ok);

			cancel = new ImageTextButton (0);
			cancel.ButtonFont = buttonFont;
			cancel.SetVariants("/images/buttons/blank_big.png");
			cancel.SetButtonText("Cancel", upColour, upColour, hoverColour, disabledColour);
			cancel.Click += cancel_Click;
			Controls.Add(cancel);
			focusJumper.Add(cancel);

			this.model = model;

			auditNodeToButtons = new Dictionary<Node, AuditControls> ();
			auditNodeToAssignedStartedState = new Dictionary<Node, bool> ();

			Node audits = model.GetNamedNode("Audits");
			foreach (Node audit in audits.getChildren())
			{
				AddAudit(audit);
			}
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing)
			{
				List<Node> auditsToRemove = new List<Node> (auditNodeToButtons.Keys);
				foreach (Node audit in auditsToRemove)
				{
					RemoveAudit(audit);
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

		void AddAudit(Node audit)
		{
			Label label = new Label();
			label.Text = audit.GetAttribute("name");
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

			ImageBox tick = new ImageBox ();
			tick.Image = ImageUtils.LoadImage(AppInfo.TheInstance.Location + @"\images\icons\tick.png");
			Controls.Add(tick);

			AuditControls controls = new AuditControls (label, start, stop, tick);
			auditNodeToButtons.Add(audit, controls);

			audit.AttributesChanged += audit_AttributesChanged;

			focusJumper.Add(start);
			focusJumper.Add(stop);

			auditNodeToAssignedStartedState[audit] = audit.GetBooleanAttribute("active", false);

			UpdateButtons();
		}

		void audit_AttributesChanged (Node sender, ArrayList attrs)
		{
			UpdateButtons();
		}

		void RemoveAudit (Node audit)
		{
			Controls.Remove(auditNodeToButtons[audit].Label);
			Controls.Remove(auditNodeToButtons[audit].Start);
			Controls.Remove(auditNodeToButtons[audit].Stop);
			Controls.Remove(auditNodeToButtons[audit].Tick);

			focusJumper.Remove(auditNodeToButtons[audit].Start);
			focusJumper.Remove(auditNodeToButtons[audit].Stop);

			audit.AttributesChanged -= audit_AttributesChanged;

			auditNodeToButtons.Remove(audit);
		}

		void button_Click (object sender, EventArgs e)
		{
			foreach (Node audit in auditNodeToButtons.Keys)
			{
				if (auditNodeToButtons[audit].Start == sender)
				{
					auditNodeToAssignedStartedState[audit] = true;
				}
				else if (auditNodeToButtons[audit].Stop == sender)
				{
					auditNodeToAssignedStartedState[audit] = false;
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
			foreach (Node audit in auditNodeToAssignedStartedState.Keys)
			{
				if (audit.GetBooleanAttribute("active", false) != auditNodeToAssignedStartedState[audit])
				{
					ArrayList attributes = new ArrayList ();
					attributes.Add(new AttributeValuePair ("active", auditNodeToAssignedStartedState[audit]));
					attributes.Add(new AttributeValuePair ("timer", 0));
					audit.SetAttributes(attributes);
				}
			}

			controlPanel.DisposeEntryPanel();
		}

		void UpdateButtons ()
		{
			bool someDiffer = false;

			foreach (Node audit in auditNodeToButtons.Keys)
			{
				bool enabledInThisRound = current_round >= audit.GetIntAttribute("enableround", 1);

				someDiffer = someDiffer || (audit.GetBooleanAttribute("active", false) != auditNodeToAssignedStartedState[audit]);

				auditNodeToButtons[audit].Stop.Enabled = auditNodeToAssignedStartedState[audit] && enabledInThisRound;
				auditNodeToButtons[audit].Start.Enabled = (! auditNodeToButtons[audit].Stop.Enabled)
														  && (audit.GetIntAttribute("timer", 0) < audit.GetIntAttribute("duration", 0))
														  && enabledInThisRound;
				auditNodeToButtons[audit].Start.Active = enabledInThisRound && ! auditNodeToButtons[audit].Start.Enabled;

				auditNodeToButtons[audit].Tick.Visible = audit.GetBooleanAttribute("has_started", false);
			}

			ok.Enabled = someDiffer;
		}

		void DoSize ()
		{
			int labelWidth = 45;
			int labelHeight = 20;
			int buttonWidth = 55;
			int buttonHeight = 20;
			int xGap = 4;
			int yGap = 15;
			int numberInColumn = 2;

			title.Location = new Point(10, 1);
			title.Size = new Size (Width - title.Left, 30);

			cancel.Size = new Size (80, 20);
			cancel.Location = new Point(Width - cancel.Width - 5, Height - cancel.Height - 5);

			ok.Size = new Size(80, 20);
			ok.Location = new Point(cancel.Left - ok.Width - 10, Height - ok.Height - 5);

			int top = title.Bottom + 30;
			int offset_y = top;
			int offset_x = 10;

			if (auditNodeToButtons.Keys.Count > 6)
			{
				numberInColumn = 4;
			}

			int item_counter=0;
			foreach (Node audit in auditNodeToButtons.Keys)
			{
				Label label = auditNodeToButtons[audit].Label;
				ImageBox tick = auditNodeToButtons[audit].Tick;
				Control start = auditNodeToButtons[audit].Start;
				Control stop = auditNodeToButtons[audit].Stop;

				label.Size = new Size (labelWidth, labelHeight);
				label.Location = new Point (offset_x, offset_y);

				tick.Size = new Size (buttonHeight, buttonHeight);
				tick.SizeMode = PictureBoxSizeMode.StretchImage;
				tick.Location = new Point (label.Right + xGap, label.Top + ((label.Height - tick.Height) / 2));
				tick.BringToFront();

				start.Size = new Size (buttonWidth, buttonHeight);
				start.Location = new Point (tick.Right + xGap, label.Top);

				stop.Size = new Size (buttonWidth, buttonHeight);
				stop.Location = new Point (start.Right + xGap, start.Top);

				offset_y += labelHeight + yGap;
				if (item_counter >= numberInColumn)
				{
					offset_y = top;
					offset_x = 230;
					item_counter = 0;
				}
				else
				{
					item_counter++;
				}
			}
		}

		public void SetFocus ()
		{
			Focus();
			cancel.Focus();
		}
	}
}