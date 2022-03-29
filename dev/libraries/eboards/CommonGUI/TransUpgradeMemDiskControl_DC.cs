using System;
using System.Drawing;
using IncidentManagement;
using Network;
using CoreUtils;

namespace CommonGUI
{
	public class TransUpgradeMemDiskControl_DC : TransUpgradeMemDiskControl
	{
		public TransUpgradeMemDiskControl_DC (IDataEntryControlHolder mainPanel, NodeTree model,
			bool usingmins, IncidentApplier _iApplier,
			Color OperationsBackColor, Color GroupPanelBackColor)
			: base (mainPanel, model, usingmins, _iApplier, OperationsBackColor, GroupPanelBackColor)
		{
			Color upColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_text_colour", Color.Black);
			Color downColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_down_text_colour", Color.White);
			Color hoverColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_hover_text_colour", Color.Green);
			Color disabledColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_disabled_text_colour", Color.DarkGray);
		}

		void PendingButton_Click (object sender, EventArgs e)
		{
			Node n1 = (Node) ((ImageTextButton)sender).Tag; //Extract the Queue Node
			RemovingPendingActionNode(n1);
		}
	}
}