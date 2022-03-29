using System;
using System.Drawing;
using Network;

using TransitionObjects;

namespace CommonGUI
{
	public class InstallBusinessServiceControlHPDC : InstallBusinessServiceControl
	{
		int round;
		Color backColour;
		Color groupColour;

		public InstallBusinessServiceControlHPDC (IDataEntryControlHolder mainPanel,
			NodeTree model, int round, ProjectManager prjmanager, Boolean IsTrainingMode,
			Color OperationsBackColor, Color GroupPanelBackColor)
			: base (mainPanel, model, round, prjmanager, IsTrainingMode, OperationsBackColor, GroupPanelBackColor)
		{
			this.round = round;

			backColour = OperationsBackColor;
			groupColour = GroupPanelBackColor;
		}
	}
}