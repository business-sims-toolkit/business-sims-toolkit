using System;
using System.IO;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;

using LibCore;
using Network;
using Polestar_PM.OpsGUI;
using Polestar_PM.OpsEngine;

using Logging;
using Polestar_PM.DataLookup;

using GameEngine;
using CoreUtils;

using GameManagement;
using CommonGUI;
using DiscreteSimGUI;

using Media;

namespace Polestar_PM.OpsScreen
{
	/// <summary>
	/// Summary description for RaceEndEventScreen.
	/// </summary>
	public class RaceEndEventScreen : FlickerFreePanel
	{
		public delegate void EndEventCompletedEvent();
		public event EndEventCompletedEvent endeventcompleted;
		//private System.Windows.Forms.Button btnPrj1Clear;

		protected NetworkProgressionGameFile _gameFile;
		protected NodeTree MyNetworkNodeTree = null;
		protected bool _isTrainingGame = false;
		protected int currentTick;
		protected int currentRound;
		protected Panel m_parent;
		protected GameRaceControl grc = null;
		protected string race_team_name = "Test Team";
		
		public RaceEndEventScreen(Panel parent, NetworkProgressionGameFile gameFile, 
			bool isTrainingGame, string ImgDir)
		{
			m_parent = parent;

			race_team_name = SkinningDefs.TheInstance.GetData("race_team_name");
			this.SuspendLayout();

			this.BackColor = Color.FromArgb(102,102,102);
			//Determine the main images and colors 
			_isTrainingGame = isTrainingGame;
			_gameFile = gameFile;
//			MyNetworkNodeTree = _gameFile.NetworkModel;
			
			grc = new GameRaceControl(gameFile);
			grc.Location = new Point(8,22);
			grc.Size = new Size(1008,685); //720);
			this.Controls.Add(grc);

			grc.RaceFinished += new GameRaceControl.RaceEventArgsHandler(grc_RaceFinished);

			this.ResumeLayout();
		}

		void grc_RaceFinished(object sender)
		{
			if (null != endeventcompleted)
			{
				endeventcompleted(); // TODO - step into this and don't hide the flash view.
				// then make sure the flash view is hidden if you click on any of the screen buttons.
			}
			//throw new Exception("The method or operation is not implemented.");
		}

		public void LoadData()
		{
			MyNetworkNodeTree = _gameFile.NetworkModel;

			//Extract the Gain which has been already calculated 
			int total_gain = 0;
			Node operational_results_node = MyNetworkNodeTree.GetNamedNode("operational_results");
			total_gain = operational_results_node.GetIntAttribute("total_gain",0);

			//end point is just a debug node
			Node ep = MyNetworkNodeTree.GetNamedNode("endpoint");
			int epd = ep.GetIntAttribute("hits",-1);
			
			grc.SetRound(_gameFile.CurrentRound);
			grc.SetDriverName(race_team_name);
			grc.SetSecondsGained(total_gain);
			grc.setDisplayModeHP(false,"Company");
		}
		/*
		private void handle_Click(object sender, System.EventArgs e)
		{
			if(null != endeventcompleted)
			{
				endeventcompleted();
			}
		}*/
	}
}
