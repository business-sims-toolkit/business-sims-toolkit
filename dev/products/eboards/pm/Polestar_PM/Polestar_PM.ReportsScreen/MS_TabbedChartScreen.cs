using System;
using System.Windows.Forms;
using System.Drawing;

using GameManagement;

using CommonGUI;

using ChartScreens;
using ReportBuilder;

using LibCore;
using Network;

using TransitionScreens;
using Polestar_PM.TransScreen;

using Charts;
using System.Collections;
using Logging;

namespace Polestar_PM.ReportsScreen
{
	public class MS_TabbedChartScreen : PerformanceTabbedChartScreen
	{
		private Font titleFont = CoreUtils.SkinningDefs.TheInstance.GetFont(7.5f, FontStyle.Bold);
		private ImageBox MOFSymbolDisplay = null;
		private int SymbolMOF = 0;
		private MS_PipelineDisplay Pipeline = null;
		private MS_PipelineKeyDisplay PipeKeyDisplay;

		public MS_TabbedChartScreen (NetworkProgressionGameFile gameFile, SupportSpendOverrides _spend_overrides)
			: base(gameFile, _spend_overrides)
		{
			tabBar.SetTabTitle(5,"Preparation");
			tabBar.SetTabTitle(1, "Gap Analysis");
			tabBar.SetTabTitle(2, "Process Maps");

			show_maturity_drop_shadow = false;
			key_y_offset = 35;
		}

		protected override void ShowMaturity(int round)
		{
#if !PASSEXCEPTIONS
			try
			{
#endif
				if(null != Costs) Costs.Flush();
				OpsMaturityReport omr = new OpsMaturityReport();
				string maturityXmlFile = omr.BuildReport(_gameFile, round, Scores);

				System.IO.StreamReader file = new System.IO.StreamReader(maturityXmlFile);
				string xmldata = file.ReadToEnd();
				file.Close();

				if (MOFSymbolDisplay == null)
				{
					MOFSymbolDisplay = new ImageBox();
					MOFSymbolDisplay.Load(AppInfo.TheInstance.Location + "\\Images\\mof_symbol.png");
					MOFSymbolDisplay.Size = new Size(160,160);
					//Display at Top Right Corner
					//MOFSymbolDisplay.Location = new Point(this.Width - 183,5);
					//Display at Bottom Right Corner
					MOFSymbolDisplay.Location = new Point(this.Width - 183,this.Height - 183-30);
					pnlMaturity.Controls.Add(MOFSymbolDisplay);
					MOFSymbolDisplay.Click += new System.EventHandler(this.MOFSymbol_Click);
					SymbolMOF=0;
				}
				PieChart old_maturity = maturity;

				maturity = new PieChart();
				maturity.ShowDropShadow = show_maturity_drop_shadow;
				maturity.KeyYOffset = key_y_offset;
				
				if (maturity != null)
				{
					maturity.Location = new Point(5,0);
					maturity.Size = new Size(this.Width - 10,this.Height - 0);
				}
				maturity.LoadData(xmldata);

				if (maturity != null)
				{
					if(null != old_maturity)
					{
						pnlMaturity.SuspendLayout();
						pnlMaturity.Controls.Remove(old_maturity);
						old_maturity.Dispose();
						pnlMaturity.ResumeLayout(false);
					}

					pnlMaturity.SuspendLayout();
					this.pnlMaturity.Controls.Add(maturity);
					MOFSymbolDisplay.BringToFront();
					pnlMaturity.ResumeLayout(false);

					this.RedrawMaturity = false;
				}
#if !PASSEXCEPTIONS
			}
			catch(Exception ex)
			{
				AppLogger.TheInstance.WriteLine("Timer Level Exception : " + ex.Message + ":\r\n" + ex.StackTrace);
			}
#endif
		}


		private void MOFSymbol_Click(object sender, System.EventArgs e)
		{
			if (SymbolMOF==0)
			{
				MOFSymbolDisplay.Load(AppInfo.TheInstance.Location + "\\Images\\mof_symbol.png");
				SymbolMOF=1;
			}
			else
			{
				MOFSymbolDisplay.Load(AppInfo.TheInstance.Location + "\\Images\\mof_lifecycle.png");
				SymbolMOF=0;
			}
		}

		
		/// <summary>
		/// In this case, we have different controls to build so we do all the work
		/// There is no calling of the base class method, we detect data and build controls
		/// </summary>
		/// <param name="round"></param>
		/// <returns>whether we have allocated controls</returns>
		protected override bool ShowTransition (int round)
		{
			NodeTree model = GetModelForTransitionRound(round);
			if (model == null)
			{
				// Not played this round yet, so nothing to show.
				return false; //no controls allocated 
			}
			RemoveTransitionControls();
			this.RedrawTransition = false;

			//Move the Underlying controls 

			int w = pnlTransition.Width;
			int h2 = pnlTransition.Height;

			int Days = 20;
			int DayWidth = w / Days;
			int pipeline_size = 645; 

			Pipeline = new MS_PipelineDisplay(model, round, false);
			Pipeline.Location = new Point(pnlTransition.Width - pipeline_size, 0);
			Pipeline.Size = new Size(pipeline_size, pipeline_size);

			pnlTransition.SuspendLayout();
			TransitionSelector.Location = new Point(5,10);

			transitionControlsDisplayPanel.Location = new Point(0,0);

			transitionControlsDisplayPanel.Width = pnlTransition.Width;	
			transitionControlsDisplayPanel.Height = pnlTransition.Height;
			transitionControlsDisplayPanel.BackColor = Color.FromArgb(255, 255, 255);
			transitionControlsDisplayPanel.Controls.Add(Pipeline);
			//transitionControlsDisplayPanel.BackColor = Color.Thistle;
			//transitionControlsDisplayPanel.Controls.Add(projectsViewer);

			TransitionSelector.BringToFront();

			if (PipeKeyDisplay == null)
			{
				PipeKeyDisplay = new MS_PipelineKeyDisplay(false);
				PipeKeyDisplay.Location = new Point(10,430);
				PipeKeyDisplay.Size = new Size(360,230);
				transitionControlsDisplayPanel.Controls.Add(PipeKeyDisplay);
			}

			PipeKeyDisplay.BringToFront();
			pnlTransition.ResumeLayout(false);

			// ...and the logo panel.
			LogoPanelBase LogoPanel = CreateLogoPanel();
			if (LogoPanel != null)
			{
				pnlTransition.SuspendLayout();
				LogoPanel.Location = new Point(7,330);
				LogoPanel.Size = new Size(348,90);
				LogoPanel.SetImageDir(_gameFile.Dir);				
				LogoPanel.SetTrainingMode(false);
				LogoPanel.BackColor = CoreUtils.SkinningDefs.TheInstance.GetColorDataGivenDefault("transition_basebackcolor", Color.White);

				LogoPanel.Visible = false;
				
				transitionControlsDisplayPanel.Controls.Add(LogoPanel);
				TransitionSelector.BringToFront();
				PipeKeyDisplay.BringToFront();
				pnlTransition.ResumeLayout(false);
			}

//			if (PortfolioView == null)
//			{
//				PortfolioView = new MS_PortfolioViewer(model);
//				PortfolioView.SetNewSMISize(117,34);			
//				PortfolioView.SetNewLayoutOffsetsAndSeperations(7,36+9,3,7,10);
//				PortfolioView.Location = new Point(0,0);
//				PortfolioView.Size = new Size(1024,726);
//				PortfolioView.SetTrainingMode(false);
//				transitionControlsDisplayPanel.Controls.Add(PortfolioView);
//			}
//			PortfolioView.ReBuildAll();

			transitionControlsDisplayPanel.Visible = true;
			return true; //allocated controls 
		}
	}
}