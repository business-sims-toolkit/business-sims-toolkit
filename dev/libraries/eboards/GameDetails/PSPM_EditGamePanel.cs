using System.Drawing;
using GameManagement;
using ReportBuilder;

namespace GameDetails
{
	public class PSPM_EditGamePanel : PM_EditGamePanel
	{
		PM_ITStyleSelection itStyleSelection;

		public PSPM_EditGamePanel (NetworkProgressionGameFile gameFile, IGameLoader gamePanel)
			: base (gameFile, gamePanel)
		{
			GenerateReport += PSPM_EditGamePanel_GenerateReport;
		}

		protected override void BuildLeftHandPanel ()
		{
			Add_Game_Details_Section();
			AddChargeCompanyDetailsSection();
			Add_IT_Style_Selection();
			AddProcessMaturitySection();
			Add_Race_Selection();
		}

		void AddChargeCompanyDetailsSection ()
		{
			chargeCompanyDetails = new ChargeCompanyDetails (_gameFile);
			leftExpandHolder.AddExpandablePanel(chargeCompanyDetails);
		}

		protected override void Add_Game_Details_Section ()
		{
			gameDetailsSection = new PolestarGameDetailsSection (_gameFile);
			gameDetailsSection.Collapsible = false;
			leftExpandHolder.AddExpandablePanel(gameDetailsSection);
		}

		protected virtual void Add_IT_Style_Selection ()
		{
			// We don't provide this option for the sales game.
			if (! _gameFile.IsSalesGame)
			{
				itStyleSelection = new PM_ITStyleSelection (_gameFile, gamePanel, true, true);
				itStyleSelection.Collapsible = true;
				itStyleSelection.Expanded = false;
				leftExpandHolder.AddExpandablePanel(itStyleSelection);
			}
		}

		protected override void Add_Race_Selection ()
		{
			selectRaceSection = new PhaseSelector (_gameFile);
			leftExpandHolder.AddExpandablePanel(selectRaceSection);
		}

		protected override void BuildRightHandPanel ()
		{
			AddLogoDetails();
			Add_PDF_Section();
		}

		void AddLogoDetails ()
		{
			logoDetails = new LogoDetails (_gameFile, false, true);
			rightExpandHolder.AddExpandablePanel(logoDetails);
		}

		protected override void  Add_PDF_Section()
		{
			teamDetails = new ReportSection (this, _gameFile, false, true, true, false);
			teamDetails.GenerateReport += teamDetails_GenerateReport;
			rightExpandHolder.AddExpandablePanel(teamDetails);
		}

		protected override void teamDetails_GenerateReport(object sender, GenerateReportEventArgs args)
		{
			PSPM_EditGamePanel_GenerateReport(sender, args);
		}

		protected virtual void PSPM_EditGamePanel_GenerateReport (object sender, GenerateReportEventArgs args)
		{
			switch (args.Type)
			{
				case ReportType.Csv:
					break;

				case ReportType.Pdf:
					CAPM_SummaryReportData data = new CAPM_SummaryReportData(
						(PMNetworkProgressionGameFile)_gameFile, args.Date);
					break;
			}
		}



		protected override void AddProcessMaturitySection ()
		{
			maturityDetails = new ProcessMaturityDetails (_gameFile, gamePanel);
			leftExpandHolder.AddExpandablePanel(maturityDetails);

			maturityDetails.AddType("PMBoK", "PMBOK", "PMBOK", em_GameEvalType.PMBOK);
			maturityDetails.AddType("Prince 2", "PRINCE2", "PRINCE2", em_GameEvalType.PRINCE2);

			maturityDetails.LoadData();
		}
	}
}