namespace GameDetails
{
	/// <summary>
	/// Summary description for HP_EditGameDetails.
	/// </summary>
	public class HP_EditGameDetails : EditGamePanel
	{
		protected LogoDetails logoSection;
		protected DriversDetails driversSection;
		protected ReportSection reportSection;

		public HP_EditGameDetails (GameManagement.NetworkProgressionGameFile gameFile, IGameLoader gamePanel)
			: base(gameFile, gamePanel)
		{
		}

		protected override void BuildLeftHandPanel()
		{
			Add_Game_Details_Section();
			Add_Race_Selection();
		}

		protected override void Add_Game_Details_Section()
		{
			gameDetailsSection = new HPGameDetails (_gameFile);
			leftExpandHolder.AddExpandablePanel(gameDetailsSection);
		}

		protected override void Add_Race_Selection()
		{
			selectRaceSection = new PhaseSelector (_gameFile);
			leftExpandHolder.AddExpandablePanel(selectRaceSection);
		}

		protected override void BuildRightHandPanel()
		{
			Add_Logo_Section();
			Add_Drivers_Section();
			Add_PDF_Section();
		}

		protected virtual void Add_Logo_Section ()
		{
			logoSection = new LogoDetails (_gameFile, false, true);
			rightExpandHolder.AddExpandablePanel(logoSection);
		}

		protected override void Add_PDF_Section()
		{
			reportSection = new ReportSection(this, _gameFile, false, true, true, false);
			reportSection.GenerateReport += reportSection_GenerateReport;
			rightExpandHolder.AddExpandablePanel(reportSection);
		}

		protected virtual void reportSection_GenerateReport (object sender, GenerateReportEventArgs args)
		{
			OnGenerateReport(args.Type, args.Filename, args.Date);
		}

		protected void Add_Drivers_Section()
		{
			driversSection = new DriversDetails (_gameFile);
			rightExpandHolder.AddExpandablePanel(driversSection);
		}

		protected override void Dispose(bool disposing)
		{
			if(disposing)
			{
				if (driversSection != null)
				{
					driversSection.Dispose();
				}
			}

			base.Dispose(disposing);
		}
	}
}