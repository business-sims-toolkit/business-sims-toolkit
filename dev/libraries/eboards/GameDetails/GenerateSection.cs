using System;
using System.Windows.Forms;
using System.Drawing;
using System.IO;

using GameManagement;
using CoreUtils;

namespace GameDetails
{
	internal class GenerateSection : Panel
	{
		NetworkProgressionGameFile gameFile;

		EditGamePanel editGamePanel;

		bool showCsv;

		Label dateLabel;
		DateTimePicker datePicker;
		Button generatePdf;
		Button generateCsv;
		Button exportReports;

		public event GenerateReportHandler GenerateReport;

		public GenerateSection (NetworkProgressionGameFile gameFile, EditGamePanel editGamePanel, bool showCsv)
		{
			this.gameFile = gameFile;
			this.editGamePanel = editGamePanel;
			this.showCsv = showCsv;

			dateLabel = new Label();
			dateLabel.Font = SkinningDefs.TheInstance.GetFont(10);
			dateLabel.Text = "Date for report";
            dateLabel.ForeColor = SkinningDefs.TheInstance.GetColorData("game_details_screen_text_colour", Color.Black);
            dateLabel.TextAlign = ContentAlignment.MiddleLeft;
			Controls.Add(dateLabel);

			datePicker = new DateTimePicker();
			if (gameFile.IsSalesGame)
			{
				datePicker.Value = File.GetCreationTime(gameFile.FileName);
			}
			else
			{
				datePicker.Value = GameUtils.FileNameToCreationDate(Path.GetFileName(gameFile.FileName));
			}
			datePicker.Format = DateTimePickerFormat.Custom;
			datePicker.Font = SkinningDefs.TheInstance.GetFont(9);
			datePicker.CustomFormat = "d MMMM yyyy";
			Controls.Add(datePicker);

			generateCsv = SkinningDefs.TheInstance.CreateWindowsButton(FontStyle.Bold);
			generateCsv.Text = "Generate CSV...";
			generateCsv.Click += generateCsv_Click;
			Controls.Add(generateCsv);

			var canGenerateReports = gameFile.LastPhaseNumberPlayed > -1;

			generatePdf = SkinningDefs.TheInstance.CreateWindowsButton(FontStyle.Bold);
			generatePdf.Text = "Generate PDF...";
			generatePdf.Click += generatePdf_Click;
			generatePdf.Enabled = canGenerateReports;
			Controls.Add(generatePdf);

			exportReports = SkinningDefs.TheInstance.CreateWindowsButton(FontStyle.Bold);
			exportReports.Text = "Export reports...";
			exportReports.Click += exportReports_Click;
			exportReports.Enabled = canGenerateReports;
			Controls.Add(exportReports);

			DoLayout();
		}

	    public void SetGenerateButtonEnabledState(bool enabled)
	    {
	        generatePdf.Enabled = enabled;
	    }

		void DoLayout ()
		{
			int labelToBoxGap = 0;
			Size labelSize = new Size(120, 25);
			Size boxSize = new Size(160, labelSize.Height);

			dateLabel.Location = new Point(0, 10);
			dateLabel.Size = labelSize;

			datePicker.Location = new Point(dateLabel.Right + labelToBoxGap, dateLabel.Top);
			datePicker.Size = boxSize;

			generatePdf.Size = boxSize;
			generatePdf.Location = new Point(dateLabel.Right + labelToBoxGap, datePicker.Bottom + 10);

			generateCsv.Size = boxSize;
			generateCsv.Location = new Point(generatePdf.Left, generatePdf.Bottom + 10);
			generateCsv.Visible = showCsv;

			exportReports.Size = boxSize;
			exportReports.Location = new Point (generatePdf.Left, (showCsv ? generateCsv : generatePdf).Bottom + 10);

			Size = new Size (500, exportReports.Bottom + 10);
		}

		void OnGenerateReport (ReportType type, string filename, DateTime date)
		{
			GenerateReport?.Invoke(this, new GenerateReportEventArgs(type, filename, date));
		}

		void generatePdf_Click (object sender, EventArgs e)
		{
			if (editGamePanel.ValidateFields())
			{
				OnGenerateReport(ReportType.Pdf, Path.GetDirectoryName(gameFile.FileName)
												   + @"\"
												   + Path.GetFileNameWithoutExtension(gameFile.FileName)
												   + ".pdf",
									 datePicker.Value);
			}
		}

		void generateCsv_Click (object sender, EventArgs e)
		{
			if (editGamePanel.ValidateFields())
			{
				OnGenerateReport(ReportType.Csv, Path.GetDirectoryName(gameFile.FileName)
												  + @"\"
												  + Path.GetFileNameWithoutExtension(gameFile.FileName)
												  + ".csv",
									 datePicker.Value);
			}
		}

		void exportReports_Click (object sender, EventArgs e)
		{
			OnGenerateReport(ReportType.Images, Path.GetDirectoryName(gameFile.FileName)
			                                 + @"\"
			                                 + Path.GetFileNameWithoutExtension(gameFile.FileName)
			                                 + ".zip",
							datePicker.Value);
		}
	}
}