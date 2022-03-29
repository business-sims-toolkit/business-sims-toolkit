using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;
using Network;

using CoreUtils;
using IncidentManagement;

namespace CommonGUI
{
	/// <summary>
	/// Summary description for UpgradeMemDiskControl_DC.
	/// </summary>
	public class UpgradeMemDiskControl_DC : UpgradeMemDiskControl
	{

		#region Constructor and dispose

		public UpgradeMemDiskControl_DC(IDataEntryControlHolder mainPanel, NodeTree model, bool usingmins, 
			IncidentApplier _iApplier, Boolean IsTrainingMode, Color OperationsBackColor, Color GroupPanelBackColor, bool playing)
			:base( mainPanel, model, usingmins, _iApplier, IsTrainingMode, OperationsBackColor, GroupPanelBackColor, playing)
		{
		}

		protected override void Dispose(bool disposing)
		{
			if(disposing)
			{
				focusJumper.Dispose();

				foreach(Control c in disposeControls)
				{
					if(c!=null) c.Dispose();
				}
				disposeControls.Clear();
			}
			//
			base.Dispose (disposing);
		}

#endregion Constructor and dispose

		protected override void BuildServerPanel (NodeTree model)
		{
			Color upColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_text_colour", Color.Black);
			Color downColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_down_text_colour", Color.White);
			Color hoverColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_hover_text_colour", Color.Green);
			Color disabledColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_disabled_text_colour", Color.DarkGray);

			// Grab all the Servers
			ArrayList types = new ArrayList();
			types.Add("Server");
			Hashtable servers = _Network.GetNodesOfAttribTypes(types);
			// Alphabetically sort the servers...
			//System.Collections.Specialized.StringCollection serverArray = new StringCollection();
			ArrayList serverArray = new ArrayList();
			Hashtable serverNameToNode = new Hashtable();
			foreach(Node server in servers.Keys)
			{
				string name = server.GetAttribute("name");
				if(!name.EndsWith("(M)"))
				{
					serverArray.Add(name);
					serverNameToNode.Add(name,server);
				}
			}
			serverArray.Sort();
			// We can have 6 buttons wide before we have to go to a new line.
			int xoffset = 5;
			int yoffset = 30;
			int numOnRow = 0;
			int serverbuttonwidth = 102;
			int serverbuttonheight = 20;
			
			//
			foreach(string server in serverArray)
			{
				Node serverNode = model.GetNamedNode(server);

				bool enable = true;

				bool canUpMem = serverNode.GetBooleanAttribute("can_upgrade_mem", false);
				bool canUpDisk = serverNode.GetBooleanAttribute("can_upgrade_disk", false);
				bool canUpHware = serverNode.GetBooleanAttribute("can_upgrade_hardware", false);
				bool canUpFirmware = serverNode.GetBooleanAttribute("can_upgrade_firmware", false);

				enable = canUpMem || canUpDisk || canUpHware || canUpFirmware;
				if (enable)
				{
					ImageTextButton newBtnServer = new ImageTextButton(0);
					newBtnServer.ButtonFont = MyDefaultSkinFontBold9;
					newBtnServer.SetVariants(filename_mid);
					newBtnServer.Location = new Point(xoffset,yoffset);
					newBtnServer.Size = new Size(serverbuttonwidth, serverbuttonheight);
					newBtnServer.Tag = serverNameToNode[server];
					//newBtnServer.TabIndex = btn.TabIndex;
					//newBtnServer.ButtonFont = btn.Font;
					newBtnServer.Enabled = true;
					//newBtn.ForeColor = System.Drawing.Color.Black;
					newBtnServer.SetButtonText(server,
						upColor,upColor,
						hoverColor,disabledColor);
					//newBtn.ButtonPressed += new ProjectMngmntGamePanels.ImageButton.ImageButtonEventArgsHandler(playButton_ButtonPressed);
					newBtnServer.Click += HandleServerButton_Click;
					chooseServerPanel.Controls.Add(newBtnServer);
					disposeControls.Add(newBtnServer);

					xoffset += newBtnServer.Width+3;
					buttonArray.Add(newBtnServer);

					focusJumper.Add(newBtnServer);

					++numOnRow;
					if(numOnRow == 5)
					{
						numOnRow = 0;
						xoffset = 5;
						yoffset += serverbuttonheight + 5;
					}
				}
			}
		}
	}
}