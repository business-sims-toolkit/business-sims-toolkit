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
	/// This class is used as a wrapper around the Resource Levels Control
	/// This display is only useful in Round 2
	/// It is also used as a pop up in Round 2 and so it stored in OpsGUI
	/// </summary>
	public class ResourceLevelsContainer :FlickerFreePanel
	{
		//NodeTree MyNodeTree = null;
		//Node projectsNode = null;
		GamePlanPanel MyGamePlanPanel = null;
		int round = 1;
		bool AllowResourcePlaninRound1 = false;
		bool isReport = false;

		NetworkProgressionGameFile _game_file;

		public ResourceLevelsContainer(NetworkProgressionGameFile game_file, int tmpRound, bool isReportFlag)
		{
			_game_file = game_file;
			isReport = isReportFlag;

			//string AllowResourcePlaninRound1_str = SkinningDefs.TheInstance.GetData("show_resource_plan_in_round1","false");
			//if (AllowResourcePlaninRound1_str.IndexOf("true")>-1)
			//{
			//  AllowResourcePlaninRound1 = true;
			//}

			AllowResourcePlaninRound1 = true;

			this.BackColor = Color.White;
			//MyNodeTree = model;
			round = tmpRound;
			//projectsNode = MyNodeTree.GetNamedNode("pm_projects_running");
			BuildControls();
		}

		new public void Dispose()
		{
			//MyNodeTree = null;
			//projectsNode = null;
			DisposeControls();
		}

		public void BuildControls()
		{
			MyGamePlanPanel = new GamePlanPanel(this.round, isReport);
			//MyGamePlanPanel.setModel(MyNodeTree);
			MyGamePlanPanel.LoadData(_game_file);
			MyGamePlanPanel.Size = new Size(Width - 60,Height);
			MyGamePlanPanel.Location = new Point(30,0);
			MyGamePlanPanel.SetBackColor(Color.White);
			this.Controls.Add(MyGamePlanPanel);
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			 base.OnSizeChanged(e);
			 MyGamePlanPanel.Size = new Size(Width - 60, Height);
		}			

		public void DisposeControls()
		{
			if (MyGamePlanPanel != null)
			{
				this.Controls.Remove(MyGamePlanPanel);
				MyGamePlanPanel.Dispose();
			}
		}

		public void ShowRound(int round)
		{
			bool display_panel = false;

			if ((AllowResourcePlaninRound1) & (round == 1))
			{
				display_panel = true;
			}
			if (round == 2)
			{
				display_panel = true;
			}
			if (round == 3)
			{
				display_panel = true;
			}
			MyGamePlanPanel.Visible = display_panel;
		}
	
		public void RefreshScreen()
		{
		}

	}
}





