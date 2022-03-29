using System;

using System.Drawing;

using GameManagement;

namespace GameDetails
{
	public class PM_EditGamePanel : PS_EditGamePanel
	{

        string overriddenPdfFilename;
		public PM_EditGamePanel (NetworkProgressionGameFile gameFile, IGameLoader gamePanel)
			: base (gameFile, gamePanel)
		{
		}

        protected override void BuildRightHandPanel()
        {
            AddLogoDetailsSection();
            AddTeamDetailsSection();
            Add_Options_Section();
        }

        protected virtual void AddLogoDetailsSection()
        {
            logoDetails = new LogoDetails(_gameFile, false, true);
            rightExpandHolder.AddExpandablePanel(logoDetails);
        }

		protected override void Add_PDF_Section ()
		{
		}
		
		protected override void Add_Game_Details_Section()
		{
			gameDetailsSection = new CAPMGameDetails(_gameFile);
			gameDetailsSection.Location = new Point(0, 0);
			gameDetailsSection.Collapsible = false;
			gameDetailsSection.SetSize(500, 235);
			leftExpandHolder.AddExpandablePanel(gameDetailsSection);
		}


        public override void OverridePDF(string file)
        {
            base.OverridePDF(file);

            overriddenPdfFilename = file;
        }

		protected override void Add_Facilitator_Logo_Section ()
		{
		}

        protected virtual void teamDetails_GenerateReport (object sender, GenerateReportEventArgs args)
        {
            string file;

            if (!string.IsNullOrEmpty(overriddenPdfFilename))
            {
                file = overriddenPdfFilename;
            }
            else
            {
                CAPM_EditGamePanel_GenerateReport(args.Type, args.Filename, args.Date);
                file = args.Filename;
            }
        }


        protected void CAPM_EditGamePanel_GenerateReport (ReportType reportType, string filename, DateTime date)
        {
            switch (reportType)
            {
                case ReportType.Csv:
                    break;

                case ReportType.Pdf:
                    ReportBuilder.CAPM_SummaryReportData data = new ReportBuilder.CAPM_SummaryReportData((PMNetworkProgressionGameFile)_gameFile, date);

                    break;
            }
        }

	}
}