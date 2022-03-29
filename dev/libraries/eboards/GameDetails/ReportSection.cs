using System.Drawing;
using CoreUtils;
using GameManagement;

namespace GameDetails
{
	public class ReportSection : GameDetailsSection
	{
		NetworkProgressionGameFile gameFile;

		TeamNameSection nameSection;
		TeamPhotoSection photoSection;
		TeamMembersSection membersSection;
		GenerateSection generateSection;

		public event GenerateReportHandler GenerateReport;

		public ReportSection (EditGamePanel editGamePanel, NetworkProgressionGameFile gameFile, bool showName, bool showMembers, bool showPhoto, bool showCsv)
		{
			this.gameFile = gameFile;

			Title = "Summary Report";

			BuildControls(showName, showMembers, showPhoto, editGamePanel, showCsv);
		}

		public override void LoadData ()
		{
			if (nameSection != null)
			{
				nameSection.LoadData();
			}

			if (photoSection != null)
			{
				photoSection.LoadData();
			}

			if (membersSection != null)
			{
				membersSection.LoadData();
			}
		}

		public override bool SaveData ()
		{
			if (nameSection != null)
			{
				nameSection.SaveData();
			}

			if (photoSection != null)
			{
				photoSection.SaveData();
			}

			if (membersSection != null)
			{
				membersSection.SaveData();
			}

			return true;
		}

	    public void SetGeneratePdfButtonEnabledState(bool enabled)
	    {
	        if (generateSection != null)
	        {
	            generateSection.SetGenerateButtonEnabledState(enabled);
	        }
	    }

		void BuildControls (bool showName, bool showMembers, bool showPhoto, EditGamePanel editGamePanel, bool showCsv)
		{
			if (showName)
			{
				nameSection = new TeamNameSection (gameFile);
				panel.Controls.Add(nameSection);
			}

			if (showPhoto)
			{
				photoSection = new TeamPhotoSection (gameFile);
				panel.Controls.Add(photoSection);
			}

			if (showMembers)
			{
				membersSection = new TeamMembersSection (gameFile);
				panel.Controls.Add(membersSection);
			}

			if (SkinningDefs.TheInstance.GetBoolData("allow_pdf_report", true))
			{
				generateSection = new GenerateSection (gameFile, editGamePanel, showCsv);
				generateSection.GenerateReport += generateSection_GenerateReport;
				panel.Controls.Add(generateSection);
			}

			DoLayout();
			LoadData();
		}

		void generateSection_GenerateReport (object sender, GenerateReportEventArgs args)
		{
			OnGenerateReport(sender, args);
		}

		void DoLayout ()
		{
			int y = 10;

			if (nameSection != null)
			{
				nameSection.Location = new Point (0, y);
				y = nameSection.Bottom;
			}

			if (membersSection != null)
			{
				membersSection.Location = new Point (0, y);
				y = membersSection.Bottom;
			}

			if (photoSection != null)
			{
				photoSection.Location = new Point (0, y);
				y = photoSection.Bottom;
			}

            if (generateSection != null)
            {
                generateSection.Location = new Point(0, y);
                y = generateSection.Bottom;
            }

			SetSize(500, y);
		}

		void OnGenerateReport (object sender, GenerateReportEventArgs args)
		{
			GenerateReport?.Invoke(sender, args);
		}
	}
}