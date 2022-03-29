using System;
using System.Drawing;
using Network;
using TransitionObjects;

using IncidentManagement;

namespace CommonGUI
{
	/// <summary>
	/// Summary description for standard Operations Control panel 
	/// </summary>
	public class OpsControlPanel_CA_SE2 : OpsControlPanel
	{
		public OpsControlPanel_CA_SE2(NodeTree nt, IncidentApplier iApplier, MirrorApplier mirrorApplier, 
			ProjectManager prjmanager, int round, int round_length_mins, Boolean IsTrainingFlag, 
			Color OperationsBackColor, Color GroupPanelBackColor,
			GameManagement.NetworkProgressionGameFile gameFile)
			: base (nt, iApplier, mirrorApplier, prjmanager, round, round_length_mins, IsTrainingFlag, 
					OperationsBackColor,  GroupPanelBackColor, gameFile)
		{
		}

		protected override void addRemoveMirrorsButton_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			if(playing)
			{
				AddOrRemoveRedundancyControl addOrRemoveMirrorControl = new AddOrRemoveRedundancyControl(this, _network,
					_mirrorApplier,MyIsTrainingFlag, MyOperationsBackColor, MyGroupPanelBackColor);
				addOrRemoveMirrorControl.Size = new Size(popup_width,popup_height);
				addOrRemoveMirrorControl.Location = new Point(popup_xposition,popup_yposition);
				Parent.Controls.Add(addOrRemoveMirrorControl);
				addOrRemoveMirrorControl.BringToFront();
				DisposeEntryPanel();
				shownControl = addOrRemoveMirrorControl;
				addOrRemoveMirrorControl.Focus();
				// : Fix for 2661: make buttons stay active
				((ImageButton) sender).Active = true;
			}
		}

		protected override void GenerateOperationPanel_MTRS()
		{
			Race_SLA_Editor pac = new Race_SLA_Editor(this, _network, MyIsTrainingFlag, MyOperationsBackColor, _round);
			pac.Size = new Size(Parent.Width, popup_height);
			pac.Location = new Point(popup_xposition, popup_yposition);
			Parent.Controls.Add(pac);
			pac.BringToFront();
			DisposeEntryPanel();
			shownControl = pac;
			pac.Focus();

		}

	}
}