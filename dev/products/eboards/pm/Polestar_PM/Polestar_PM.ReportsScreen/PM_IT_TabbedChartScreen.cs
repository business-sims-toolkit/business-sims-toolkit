using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;

using GameManagement;
using ReportBuilder;
using CoreUtils;
using Charts;
using Logging;

using TransitionScreens;
using CommonGUI;
using ChartScreens;
using Network;

namespace Polestar_PM.ReportsScreen
{
	/// <summary>
	/// Summary description for PM_IT_TabbedChartScreen.
	/// </summary>
	public class PM_IT_TabbedChartScreen : BaseTabbedChartScreen
	{
		protected GameBoardView.GameBoardViewWithController infrastructureView;
		protected new NetworkControl network;

		public PM_IT_TabbedChartScreen(NetworkProgressionGameFile gameFile, SupportSpendOverrides _spend_overrides) : base(gameFile, _spend_overrides)
		{
			this.SuspendLayout();

			//Mostly the Network Text report is called "Network" but some companies have thier own commerical products 
			//so we can over it in the skin file. (CA is the first example)
			string networktab_displayname = "Network";
			networktab_displayname = CoreUtils.SkinningDefs.TheInstance.GetData("networktab_displayname","Network");

			//add the tabs
			tabBar.AddTab(networktab_displayname, 0, true);
			tabBar.AddTab("Infrastructure", 2, true);

			//add the new tab bar and the sub tab bar (invisible)
			tabBar.Location = new Point(5,0);

			this.Controls.Add(tabBar);

			this.ResumeLayout(false);

			tabBar.TabPressed += new CommonGUI.TabBar.TabBarEventArgsHandler(tabBar_TabPressed);

			InitialisePanels();
			//
			//
			HidePanels();

			this.Resize += new EventHandler(TabbedChartScreen_Resize);

			this.BackColor = Color.White;

		}

		protected override LogoPanelBase CreateLogoPanel()
		{
			return new LogoPanel_PS();
		}

		protected /*override*/ void InitialisePanels()
		{
			string transactionName =  SkinningDefs.TheInstance.GetData("transactionname");

			pnlMain = new Panel();
			//pnlGantt = new Panel();
			pnlNetwork = new Panel();
			pnlIncidents = new Panel();
			pnlIncidents.AutoScroll = true;
			// 
			// pnlMain
			// 
			pnlMain.BackColor = System.Drawing.Color.White;

			//GanttCarSelector = new ComboBox();
			//GanttRoundSelector = new ComboBox();
			NetworkRoundSelector = new ComboBox();

			IncidentsRoundSelector = new ComboBox();

			this.SuspendLayout();
			pnlMain.SuspendLayout();
			//pnlGantt.SuspendLayout();
			pnlNetwork.SuspendLayout();
			pnlIncidents.SuspendLayout();

			//this.pnlMain.Controls.Add(pnlGantt);
			this.pnlMain.Controls.Add(pnlNetwork);
			this.pnlMain.Controls.Add(pnlIncidents);
			this.pnlMain.DockPadding.All = 4;
			this.pnlMain.Name = "pnlMain";
			this.pnlMain.TabIndex = 6;
			this.Controls.Add(this.pnlMain);
			//
			// pnlGantt
			//
//			this.pnlGantt.BackColor = System.Drawing.Color.White;
//			this.pnlGantt.Location = new System.Drawing.Point(0, 0);
//			this.pnlGantt.Name = "pnlGantt";
//			this.pnlGantt.TabIndex = 0;

//			//set up the combo box for the gantt chart car selections
//			GanttCarSelector.DropDownStyle = ComboBoxStyle.DropDownList;
//			string allbiz = CoreUtils.SkinningDefs.TheInstance.GetData("allbiz");
//			GanttCarSelector.Items.Add(allbiz);
//			string biz = CoreUtils.SkinningDefs.TheInstance.GetData("biz");
//			GanttCarSelector.Items.Add(biz + " 1");
//			GanttCarSelector.Items.Add(biz + " 2");
//			GanttCarSelector.Items.Add(biz + " 3");
//			GanttCarSelector.Items.Add(biz + " 4");
//			GanttCarSelector.Text = allbiz;
//			GanttCarSelector.Location = new Point(450,10);
//			GanttCarSelector.Size = new Size(100,100);
//			GanttCarSelector.SelectedIndexChanged += new EventHandler(GanttCarSelector_SelectedIndexChanged);
//
//			pnlGantt.Controls.Add(GanttCarSelector);
//
//			GanttRoundSelector.DropDownStyle = ComboBoxStyle.DropDownList;
//			for (int i = 1; i <= SkinningDefs.TheInstance.GetIntData("roundcount", 5); i++)
//			{
//				GanttRoundSelector.Items.Add("Round " + LibCore.CONVERT.ToStr(i));
//			}
//			GanttRoundSelector.Location = new Point(250,10);
//			GanttRoundSelector.Size = new Size(100,100);
//			GanttRoundSelector.SelectedIndexChanged += new EventHandler(GanttRoundSelector_SelectedIndexChanged);
//			pnlGantt.Controls.Add(GanttRoundSelector);

			//pnlIncidents
			this.pnlIncidents.BackColor = System.Drawing.Color.White;
			this.pnlIncidents.Location = new System.Drawing.Point(0, 0);
			this.pnlIncidents.Name = "pnlIncidents";
			this.pnlIncidents.TabIndex = 0;

			IncidentsRoundSelector = new ComboBox();
			IncidentsRoundSelector.DropDownStyle = ComboBoxStyle.DropDownList;
			for (int i = 1; i <= SkinningDefs.TheInstance.GetIntData("roundcount", 3); i++)
			{
				IncidentsRoundSelector.Items.Add("Round " + LibCore.CONVERT.ToStr(i));
			}
			IncidentsRoundSelector.Location = new Point(10,10);
			IncidentsRoundSelector.Size = new Size(100,100);
			IncidentsRoundSelector.SelectedIndexChanged += new EventHandler(IncidentsRoundSelector_SelectedIndexChanged);

			pnlIncidents.SuspendLayout();
			pnlIncidents.Controls.Add(IncidentsRoundSelector);
			pnlIncidents.ResumeLayout(false);

			//pnlNetwork
			pnlNetwork.BackColor = System.Drawing.Color.White;
			pnlNetwork.Location = new System.Drawing.Point(0, 0);
			pnlNetwork.Name = "pnlNetwork";
			pnlNetwork.TabIndex = 0;

			NetworkRoundSelector.DropDownStyle = ComboBoxStyle.DropDownList;
			for (int i = 1; i <= SkinningDefs.TheInstance.GetIntData("roundcount", 2); i++)
			{
				NetworkRoundSelector.Items.Add("Round " + LibCore.CONVERT.ToStr(i));
			}
			NetworkRoundSelector.Location = new Point(5,10);
			NetworkRoundSelector.Size = new Size(100,100);
			NetworkRoundSelector.SelectedIndexChanged += new EventHandler(NetworkRoundSelector_SelectedIndexChanged);
			pnlNetwork.Controls.Add(NetworkRoundSelector);

			infrastructureView = new  GameBoardView.GameBoardViewWithController (this._gameFile.NetworkModel);
			infrastructureView.Location = new Point(0,0);
			infrastructureView.Size = new Size(this.Width - 80,552);
			infrastructureView.Visible = false;
			pnlMain.Controls.Add(infrastructureView);

			pnlNetwork.ResumeLayout(false);
			//pnlGantt.ResumeLayout(false);
			pnlIncidents.ResumeLayout(false);
			pnlMain.ResumeLayout(false);
			this.ResumeLayout(false);
		}

		protected virtual void IncidentsRoundSelector_SelectedIndexChanged(object sender, EventArgs e)
		{
			this.ShowIncidents(IncidentsRoundSelector.SelectedIndex+1);
		}

		protected override void ShowPanel(int panel)
		{
			if(null != Costs) Costs.Flush();
			this.scorecardTabs.Visible = false;

			switch(panel)
			{
				case 0:
					ShowNetworkPanel();
					break;
				case 1:
					ShowIncidentsPanel();
					break;
				case 2:
					ShowInfrastrucurePanel();
					break;
			}
		}

		protected override void ShowNetwork(int round)
		{
#if !PASSEXCEPTIONS
			try
			{
#endif
				if(null != Costs) Costs.Flush();
				NetworkReport nr = new NetworkReport();
				string ganttXmlFile = nr.BuildReport(_gameFile, round);

				//
				System.IO.StreamReader file = new System.IO.StreamReader(ganttXmlFile);
				string xmldata = file.ReadToEnd();
				file.Close();

				// : Fix for 3754 (among others).  Rather than scan all our controls for
				// one whose type name matches what we expect (which breaks when we subclass),
				// we just keep an object reference.
				if (network != null)
				{
					pnlNetwork.SuspendLayout();
					pnlNetwork.Controls.Remove(network);
					network.Dispose();
					pnlNetwork.ResumeLayout(false);
				}

				network = new NetworkControl ();
				network.SetProcessingFlagVisible(false);
				network.LoadData(xmldata);

				network.Location = new Point(5,40);
				network.Size = new Size(this.Width - 10,this.Height - 30);

				pnlNetwork.SuspendLayout();
				this.pnlNetwork.Controls.Add(network);
				pnlNetwork.ResumeLayout(false);

				this.RedrawNetwork = false;
#if !PASSEXCEPTIONS
			}
			catch(Exception ex)
			{
				AppLogger.TheInstance.WriteLine("Timer Level Exception : " + ex.Message + ":\r\n" + ex.StackTrace);
			}
#endif
		}



		protected override void HidePanels()
		{
			pnlIncidents.Visible = false;
			if(infrastructureView != null)
			{
				try
				{
					infrastructureView.Visible = false;
				}
				catch { }
			}
			base.HidePanels();
		}

		private void ShowInfrastrucurePanel()
		{
			infrastructureView.ReadNetwork(_gameFile.NetworkModel);
			infrastructureView.Visible = true;
			infrastructureView.ResetView();
		}

		private void ShowIncidentsPanel()
		{
			this.pnlIncidents.Visible = true;
			if(null != Costs) Costs.Flush();

			int round = _gameFile.LastRoundPlayed;

			IncidentsRoundSelector.SelectedIndex = round-1;

			if (RedrawIncidents == true)
			{
				ShowIncidents(round);
			}
		}

		private void ShowIncidents(int round)
		{
#if !PASSEXCEPTIONS
			try
			{
#endif
				if(null != Costs) Costs.Flush();
				OpsIncidentsReport oir = new OpsIncidentsReport();
				string XmlFile = "";
				if (round <= Scores.Count)
				{
					XmlFile = oir.BuildReport(_gameFile, round, Scores);
				}
				else
				{
					XmlFile = oir.BuildReport(_gameFile, round, null);
				}

				Table incidents = new Table();
				//
				System.IO.StreamReader file = new System.IO.StreamReader(XmlFile);
				string xmldata = file.ReadToEnd();
				file.Close();
				file = null;
				//

				incidents.LoadData(xmldata);

				if (incidents != null)
				{
					ArrayList tokill = new ArrayList();
					foreach (Control c in this.pnlIncidents.Controls)
					{
						if (c.GetType().ToString() == "Charts.Table")
						{
							tokill.Add(c);
						}
					}

					pnlIncidents.SuspendLayout();
					foreach(Control c in tokill)
					{
						this.pnlIncidents.Controls.Remove(c);
						c.Dispose();
					}
					pnlIncidents.ResumeLayout(false);
				}

				if (incidents != null)
				{
					pnlIncidents.AutoScroll = false;
					pnlIncidents.SuspendLayout();
					this.pnlIncidents.Controls.Add(incidents);
					pnlIncidents.ResumeLayout(false);
					pnlIncidents.AutoScroll = true;
					pnlIncidents.AutoScrollPosition = new Point (0, 0);

					incidents.Location = new Point(5,70);

					// : Fix for 3822 (incidents report needs a scrollbar).
					incidents.Size = new Size(this.Width - 100, incidents.TableHeight);
					incidents.AutoScroll = true;

					this.RedrawIncidents = false;
				}
#if !PASSEXCEPTIONS
			}
			catch(Exception ex)
			{
				AppLogger.TheInstance.WriteLine("Timer Level Exception : " + ex.Message + ":\r\n" + ex.StackTrace);
			}
#endif
		}

		public override void Init(ChartPanel screen)
		{
			this.RedrawScorecard = true;
			this.RedrawSupportcosts = true;
			this.RedrawProcessScores = true;
			this.RedrawGantt = true;
			this.RedrawKB = true;
			
			ShowPanel((int)screen);
		}

		protected override void DoSize()
		{
			tabBar.Size = new Size(this.Width-21, 29);

			pnlMain.Location = new Point(10, this.tabBar.Bottom + 5);
			pnlMain.Size = new Size(this.Width - 20,this.Height-40);

			//pnlGantt.Size = pnlMain.Size;
			pnlNetwork.Size = pnlMain.Size;
			pnlIncidents.Size = pnlMain.Size;

			this.infrastructureView.Size = pnlMain.Size;
		}	

		public override void ReloadDataAndShow(bool reload)
		{
			this.GetRoundScores();

			//RedrawGantt = true;
			RedrawNetwork = true;
			RedrawIncidents = true;

			ShowPanel(this.tabBar.SelectedTab);
		}
	
		protected override void Dispose(bool disposing)
		{
			if(disposing)
			{
				if(null != infrastructureView) infrastructureView.Dispose();
				infrastructureView = null;
			}
			base.Dispose (disposing);
		}

		public override void ShowReport (ChartScreenTabOption report)
		{
			throw new NotImplementedException ();
		}

		public override IList<ChartScreenTabOption> GetAllAvailableReports ()
		{
			throw new NotImplementedException ();
		}
	}
}