using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using LibCore;
using Network;
using CoreUtils;
using CommonGUI;
using IncidentManagement;
using GameManagement;

using Cloud.OpsEngine;

namespace Cloud.OpsScreen
{
	public class CloudOpsControlPanel : FlickerFreePanel, ITimedClass, IDataEntryControlHolder
	{
		List<ImageButton> allButtons = new List<ImageButton> ();

		ImageTextButton requestButton;
		ImageTextButton consolidateButton;
		ImageTextButton cloudButton;
		ImageTextButton toolsButton;
		ImageTextButton plannerButton;
		ImageTextButton moveButton;

		OrderPlanner orderPlanner;
		VirtualMachineManager vmManager;

		protected NetworkProgressionGameFile gameFile;
		protected NodeTree model;
		protected CloudOpsEngine opsEngine;
		TradingOpsScreen gameScreen;

		Node timeNode;

		Font Font_Button;
		
		public CloudOpsControlPanel (NetworkProgressionGameFile gameFile, NodeTree model, CloudOpsEngine opsEngine,
									 TradingOpsScreen gameScreen, OrderPlanner orderPlanner, VirtualMachineManager vmManager)
		{
			this.gameFile = gameFile;
			this.model = model;
			this.opsEngine = opsEngine;
			this.gameScreen = gameScreen;
			this.orderPlanner = orderPlanner;
			this.vmManager = vmManager;

			timeNode = model.GetNamedNode("CurrentTime");
			timeNode.AttributesChanged += new Node.AttributesChangedEventHandler (timeNode_AttributesChanged);

			string font = SkinningDefs.TheInstance.GetData("fontname");
			Font_Button = FontRepository.GetFont(font, 10, FontStyle.Bold);

			TimeManager.TheInstance.ManageClass(this);

			BuildControls();

			cloudButton.Enabled = (gameFile.CurrentRound >= 3);
			toolsButton.Enabled = (gameFile.CurrentRound >= 3);
			moveButton.Enabled = (gameFile.CurrentRound >= 3);

			DoSize();
		}

		void timeNode_AttributesChanged (Node sender, ArrayList attrs)
		{
            consolidateButton.Enabled = true;
		}

		protected virtual void BuildControls ()
		{
			BackColor = Color.Transparent;
			string btn_filename = "images/buttons/button_bar_85x25.png";


			requestButton = new ImageTextButton(btn_filename);
			requestButton.SetButtonText("Requests", Color.White, Color.White, Color.White, Color.DimGray);
			requestButton.ButtonFont = Font_Button;
			requestButton.ButtonPressed += new ImageButton.ImageButtonEventArgsHandler (requestButton_ButtonPressed);
			Controls.Add(requestButton);
			allButtons.Add(requestButton);

			consolidateButton = new ImageTextButton(btn_filename);
			consolidateButton.SetButtonText("Consolidate", Color.White, Color.White, Color.White, Color.DimGray);
			consolidateButton.ButtonFont = Font_Button;
			consolidateButton.ButtonPressed += new ImageButton.ImageButtonEventArgsHandler (consolidateButton_ButtonPressed);
			Controls.Add(consolidateButton);
			allButtons.Add(consolidateButton);

			cloudButton = new ImageTextButton(btn_filename);
			cloudButton.SetButtonText("Cloud", Color.White, Color.White, Color.White, Color.DimGray);
			cloudButton.ButtonFont = Font_Button;
			cloudButton.ButtonPressed += new ImageButton.ImageButtonEventArgsHandler (cloudButton_ButtonPressed);
			Controls.Add(cloudButton);
			allButtons.Add(cloudButton);

			toolsButton = new ImageTextButton(btn_filename);
			toolsButton.SetButtonText("Options", Color.White, Color.White, Color.White, Color.DimGray);
			toolsButton.ButtonFont = Font_Button;
			toolsButton.ButtonPressed += new ImageButton.ImageButtonEventArgsHandler(toolsButton_ButtonPressed);
			Controls.Add(toolsButton);
			allButtons.Add(toolsButton);

			plannerButton = new ImageTextButton(btn_filename);
			plannerButton.SetButtonText("CPU", Color.White, Color.White, Color.White, Color.DimGray);
			plannerButton.ButtonFont = Font_Button;
			plannerButton.ButtonPressed += new ImageButton.ImageButtonEventArgsHandler(plannerButton_ButtonPressed);
			Controls.Add(plannerButton);
			allButtons.Add(plannerButton);

			moveButton = new ImageTextButton(btn_filename);
			moveButton.SetButtonText("Vendor", Color.White, Color.White, Color.White, Color.DimGray);
			moveButton.ButtonFont = Font_Button;
			moveButton.ButtonPressed += new ImageButton.ImageButtonEventArgsHandler(moveButton_ButtonPressed);
			Controls.Add(moveButton);
			allButtons.Add(moveButton);
		}

		void requestButton_ButtonPressed (object sender, ImageButtonEventArgs args)
		{
			if (gameScreen.CanClosePopup())
			{
				gameScreen.ShowPopup(new RequestPanel_NewStyle (model, orderPlanner, vmManager));
				SelectButton(requestButton);
			}
		}

		void consolidateButton_ButtonPressed (object sender, ImageButtonEventArgs args)
		{
			if (gameScreen.CanClosePopup())
			{
				gameScreen.ShowPopup(new ConsolidationPanel (gameFile, model, orderPlanner, vmManager));
				SelectButton(consolidateButton);
			}
		}

		void cloudButton_ButtonPressed (object sender, ImageButtonEventArgs args)
		{
			if (gameScreen.CanClosePopup())
			{
				gameScreen.ShowPopup(new Cloud_Popup_Selection_Panel(model, orderPlanner));
				SelectButton(cloudButton);
			}
		}

		void toolsButton_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			if (gameScreen.CanClosePopup())
			{
				gameScreen.ShowPopup(new ToolDeploymentPanel(model, orderPlanner));
				SelectButton(toolsButton);
			}
		}

		void moveButton_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			if (gameScreen.CanClosePopup())
			{
				gameScreen.ShowPopup(new MoveProviderPanel(model, orderPlanner, opsEngine.CloudVendorManager));
				SelectButton(moveButton);
			}
		}

		void plannerButton_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			if (gameScreen.CanClosePopup())
			{
				gameScreen.ShowPopup(new PlannerDisplayPanel(gameFile, model, orderPlanner));
				SelectButton(plannerButton);
			}
		}

		public virtual void Start()
		{
			if (plannerButton != null)
			{
				plannerButton.Enabled = false;
			}
		}

		public virtual void FastForward (double timesRealTime)
		{
		}

		public virtual void Reset ()
		{
			if (plannerButton != null)
			{
				plannerButton.Enabled = true;
			}
		}

		public virtual void Stop ()
		{
			if (plannerButton != null)
			{
				plannerButton.Enabled = true;
			}
			DisposeEntryPanel();
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);
			DoSize();
		}

		void DoSize ()
		{
			int xGap = 10;
			
			requestButton.SetAutoSize();
			consolidateButton.Size = requestButton.Size;
			cloudButton.Size = requestButton.Size;
			toolsButton.Size = requestButton.Size;
			plannerButton.Size = requestButton.Size;
			moveButton.Size = requestButton.Size;

			requestButton.Location = new Point (10, (Height - requestButton.Height) / 2);
			consolidateButton.Location = new Point (requestButton.Right + xGap, requestButton.Top);
			cloudButton.Location = new Point (consolidateButton.Right + xGap, requestButton.Top);
			toolsButton.Location = new Point(cloudButton.Right + xGap, requestButton.Top);
			plannerButton.Location = new Point(toolsButton.Right + xGap, requestButton.Top);
			moveButton.Location = new Point(plannerButton.Right + xGap, requestButton.Top);
		}

		public virtual void DisableButtons ()
		{
			DisposeEntryPanel();

			foreach (ImageTextButton button in allButtons)
			{
				button.Enabled = false;
			}
		}

		public virtual void ResetButtons ()
		{
			SelectButton(null);
		}

		void SelectButton (ImageTextButton selectedButton)
		{
			foreach (ImageTextButton button in allButtons)
			{
				button.Active = (button == selectedButton);
			}
		}

		public virtual void DisposeEntryPanel ()
		{
			ResetButtons();
		}

		public void DisposeEntryPanel_indirect (int which)
		{
			DisposeEntryPanel();
			SwapToOtherPanel(which);
		}

		public void SwapToOtherPanel (int which)
		{
		}

		public IncidentApplier IncidentApplier => opsEngine.IncidentApplier;

		protected override void Dispose (bool disposing)
		{
			if (disposing)
			{
				DisposeEntryPanel();
				TimeManager.TheInstance.UnmanageClass(this);
				timeNode.AttributesChanged -= new Node.AttributesChangedEventHandler (timeNode_AttributesChanged);
			}
			base.Dispose(disposing);
		}

		public override Size GetPreferredSize (Size proposedSize)
		{
			return new Size (moveButton.Right, 40);
		}
	}
}