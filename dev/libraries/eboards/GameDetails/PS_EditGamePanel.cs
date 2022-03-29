using System;
using System.Collections;
using Network;
using CoreUtils;
using GameManagement;
using ReportBuilder;

namespace GameDetails
{
	public class PS_EditGamePanel : EditGamePanel
	{
		protected ChargeCompanyDetails chargeCompanyDetails;
		protected ProcessMaturityDetails maturityDetails;
		protected PathfinderDetails pathfinderDetails;

		protected LogoDetails logoDetails;
		protected ReportSection teamDetails;

		string overriddenPdfFilename;

		protected override void Add_PDF_Section ()
		{
		}

		public PS_EditGamePanel (NetworkProgressionGameFile game, IGameLoader gamePanel)
			: base (game, gamePanel)
		{
			overriddenPdfFilename = null;
			GenerateReport += PS_EditGamePanel_GenerateReport;
		}
		
		protected override void Add_Team_And_Logo_Section ()
		{
			teamNameAndLogoSection = new TeamNameAndLogoDetails (_gameFile);
			rightExpandHolder.AddExpandablePanel(teamNameAndLogoSection);
			teamNameAndLogoSection.ChangeTeamNameVisibility(false);
		}

		protected override void Add_Game_Details_Section ()
		{
			gameDetailsSection = new PolestarGameDetailsSection (_gameFile);
			gameDetailsSection.Collapsible = false;
			leftExpandHolder.AddExpandablePanel(gameDetailsSection);
		}

		protected override void BuildLeftHandPanel ()
		{
            bool isChargeCompany = SkinningDefs.TheInstance.GetBoolData("Left_Panel_ChargeCompany", true);
            bool isPathFinder = SkinningDefs.TheInstance.GetBoolData("Left_Panel_PathFinder", true);
            Add_Game_Details_Section();
            if (isChargeCompany)
            {
                AddChargeCompanySection();
            }
			AddProcessMaturitySection();
            if (isPathFinder)
            {
                AddPathfinderSection();
            }
			Add_Race_Selection();
		}

		protected override void BuildRightHandPanel ()
		{
			AddLogosSection();
			AddTeamDetailsSection();
			Add_PDF_Section();
			Add_Options_Section();
		}

		protected virtual void AddPathfinderSection ()
		{
			if (SkinningDefs.TheInstance.GetBoolData("show_pathfinder_options", false))
			{
				pathfinderDetails = new PathfinderDetails (_gameFile, gamePanel);
				leftExpandHolder.AddExpandablePanel(pathfinderDetails);
			}
		}

		protected virtual void AddTeamDetailsSection ()
		{
			teamDetails = new ReportSection (this, _gameFile, false, true, true, false);
			teamDetails.GenerateReport += teamDetails_GenerateReport;
			rightExpandHolder.AddExpandablePanel(teamDetails);
		}

		protected virtual void AddLogosSection ()
		{
			logoDetails = new LogoDetails (_gameFile, true, true);
			rightExpandHolder.AddExpandablePanel(logoDetails);
		}

		protected virtual void AddChargeCompanySection ()
		{
			chargeCompanyDetails = new ChargeCompanyDetails (_gameFile);
			leftExpandHolder.AddExpandablePanel(chargeCompanyDetails);
		}

		protected virtual void AddProcessMaturitySection ()
		{
			maturityDetails = new ProcessMaturityDetails(_gameFile, gamePanel);
			leftExpandHolder.AddExpandablePanel(maturityDetails);

			maturityDetails.AddType("ITIL", "ITIL", "ITIL", em_GameEvalType.ITIL);
			maturityDetails.AddType("ISO 20,000", "ISO_20K", "ISO", em_GameEvalType.ISO_20K);

			if (SkinningDefs.TheInstance.GetBoolData("game_type_allow_lean", true))
			{
				maturityDetails.AddType("Lean", "LEAN", "Lean", em_GameEvalType.LEAN);
			}

			if (SkinningDefs.TheInstance.GetBoolData("game_type_allow_custom", true))
			{
				maturityDetails.AddCustomType("Load custom file...", "CUSTOM", "Custom");
			}

			maturityDetails.LoadData();
		}

		public override void OverridePDF (string file)
		{
			base.OverridePDF(file);

			overriddenPdfFilename = file;
		}

		void teamDetails_GenerateReport (object sender, GenerateReportEventArgs args)
		{
			string file;

			if (! string.IsNullOrEmpty(overriddenPdfFilename))
			{
				file = overriddenPdfFilename;
			}
			else
			{
				PS_EditGamePanel_GenerateReport(sender, args);
				file = args.Filename;
			}
		}

		int GetTransactionNodeName()
        {
           
            ArrayList TransactionNode = _gameFile.NetworkModel.GetNodesWithAttributeValue("use_for_transactions", "true");
            if (TransactionNode.Count > 1)
            {
                throw new Exception("Multiple Nodes being used as transaction node");
            }
            else if (TransactionNode.Count == 1)
            {
                Node transactionNode = (Node)TransactionNode[0];
                return transactionNode.GetIntAttribute("count_max", 100);
            }
            else
            {
                return 0;
            }
        }

		protected virtual void PS_EditGamePanel_GenerateReport (object sender, GenerateReportEventArgs args)
		{
            int MaxTransValue = GetTransactionNodeName();
            
			switch (args.Type)
			{
				case ReportType.Images:
					OnExportReportImages(args);
					break;

				case ReportType.Csv:
					break;

				case ReportType.Pdf:
					ItilSummaryReportData data = new ItilSummaryReportData (_gameFile, args.Date);
                    // CHANGE THIS TO TAKE A LIST OF ATTRIBUTES, IT'S GETTING A LITTLE UNRULY IN HERE

					break;
			}
		}
	}
}