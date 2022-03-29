using System.Drawing;
using GameManagement;

namespace GameDetails
{
	public class Cloud_Choice_EditGamePanel : EditGamePanel
	{
		protected ReportSection teamDetails;
		string overriddenPdfFilename;

		protected override void Add_PDF_Section ()
		{
		}

		public Cloud_Choice_EditGamePanel (NetworkProgressionGameFile game, IGameLoader gamePanel)
			: base(game, gamePanel)
		{
		}

		protected override void BuildRightHandPanel ()
		{
			AddTeamDetailsSection();
			Add_Options_Section();
		}

		protected virtual void AddTeamDetailsSection ()
		{
			teamDetails = new ReportSection(this, _gameFile, false, true, true, false);
			teamDetails.GenerateReport += teamDetails_GenerateReport;
			rightExpandHolder.AddExpandablePanel(teamDetails);
		}

		public override void setColumnSizes ()
		{
			this.FirstColumnWidth = 550;
			this.SecondColumnWidth = 450;
			ColumnGap = 5;
		}

		protected override void Add_Game_Details_Section ()
		{
			gameDetailsSection = new PSGameDetails(_gameFile, gamePanel);
			gameDetailsSection.Location = new Point(0, 0);
			gameDetailsSection.Collapsible = false;
			gameDetailsSection.SetSize(this.FirstColumnWidth, 300);
			leftExpandHolder.AddExpandablePanel(gameDetailsSection);
		}


		public override void OverridePDF (string file)
		{
			base.OverridePDF(file);
			overriddenPdfFilename = file;
		}

		void teamDetails_GenerateReport (object sender, GenerateReportEventArgs args)
		{
			string file;

			if (!string.IsNullOrEmpty(overriddenPdfFilename))
			{
				file = overriddenPdfFilename;
			}
			else
			{
				OnGenerateReport(args.Type, args.Filename, args.Date);
				file = args.Filename;
			}
		}
	}
}