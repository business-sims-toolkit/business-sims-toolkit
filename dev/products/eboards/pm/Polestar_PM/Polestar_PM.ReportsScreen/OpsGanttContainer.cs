using System;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

using CommonGUI;
using ChartScreens;
using ReportBuilder;
using LibCore;
using Network;
using Charts;
using System.Collections;
using Logging;
using GameManagement;
using CoreUtils;
using Polestar_PM.OpsGUI;
using Polestar_PM.DataLookup;

namespace Polestar_PM.ReportsScreen
{
	/// <summary>
	/// This class is used as a wrapper around the old GanntControl 
	/// This display the calender and What Ops actions were undertaken 
	/// This includes FSC, Change Cards, Upgrades and Project SIP installs
	/// </summary>
	public class OpsGanttContainer :FlickerFreePanel
	{
		NodeTree MyNodeTree = null;
		NetworkProgressionGameFile gameFile;
		Node projectsNode = null;
		OpsGanttControl myOpsGanttControl = null;

		int round = 1;

		public OpsGanttContainer(NetworkProgressionGameFile gameFile, NodeTree model, int tmpRound)
		{
			this.gameFile = gameFile;
			this.BackColor = Color.White;
			MyNodeTree = model;
			round = tmpRound;
			projectsNode = MyNodeTree.GetNamedNode("pm_projects_running");
			BuildControls();
			Fill_ProjectData();
		}

		new public void Dispose()
		{
			MyNodeTree = null;
			projectsNode = null;
			DisposeControls();
		}

		public void BuildControls()
		{
			myOpsGanttControl = new OpsGanttControl(gameFile, MyNodeTree, round);
			//myOpsGanttControl.SetRoundNumber(round);
			//myOpsGanttControl.SetProjectNumber(1);
			myOpsGanttControl.Size = new Size(Width,584);
			myOpsGanttControl.Location = new Point(0,20);
			this.Controls.Add(myOpsGanttControl);

		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);

			myOpsGanttControl.Width = this.Width;
		}

		public void DisposeControls()
		{
			if (myOpsGanttControl != null)
			{
				this.Controls.Remove(myOpsGanttControl);
				myOpsGanttControl.Dispose();
			}
		}

		public void Fill_ProjectData()
		{

		}
	
		public void RefreshScreen()
		{
			//this.myOpsGanttControl.SetRoundNumber(1);
			//this.myOpsGanttControl.SetProjectNumber(projectslot_id);
			//this.myOpsGanttControl.setChartType(RequiredChartType);
			//this.myOpsGanttControl.Refresh();
		}



	}
}
