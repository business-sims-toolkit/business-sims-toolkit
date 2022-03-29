using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Linq;
using System.Reflection;

using CoreUtils;
using LibCore;

namespace GameDetails
{
	public class EditGamePanel : BasePanel
	{
		protected GameDeliveryDetailsSection gameDetailsSection;

		protected Button test;
		protected GameManagement.NetworkProgressionGameFile _gameFile;

		protected PhaseSelector selectRaceSection;
		protected TeamMemberDetails teamMembersSection;
		protected TeamPhotoDetails teamPhotoSection;

		protected TeamNameAndLogoDetails teamNameAndLogoSection;
		protected FacilitatorLogoDetails facilLogoSection;
		protected CoursewareDelivererLogoDetails clientLogoSection;

		protected OptionsDetails optionsSection;

		protected PDFSummary pdfSection;

        protected ScrollableExpandHolder leftExpandHolder;
		protected ScrollableExpandHolder rightExpandHolder;

	    ExpandablePanel facilitatorDocumentsPanel;
	    Button facilitatorDocumentsButton;

		public delegate void PlayPressedHandler(int phase);
		public event PlayPressedHandler PlayPressed;
		public event PlayPressedHandler SkipPressed;

		protected IGameLoader gamePanel;
		protected int FirstColumnWidth = 500;
		protected int SecondColumnWidth = 500;
		protected int ColumnGap = 5;

		public event GenerateReportHandler GenerateReport;

		GameManagement.em_GameEvalType lastKnownEvalType;

		protected virtual void OnGenerateReport (ReportType reportType, string filename, DateTime date)
		{
			GenerateReport?.Invoke(this, new GenerateReportEventArgs(reportType, filename, date));
		}

		public virtual void OverridePDF(string file)
		{
			if (pdfSection != null)
			{
				pdfSection.OveridePDF(file);
			}
		}

		public EditGamePanel (GameManagement.NetworkProgressionGameFile gameFile, IGameLoader gamePanel)
		{
			this.gamePanel = gamePanel;
			_gameFile = gameFile;
			lastKnownEvalType = gameFile.Game_Eval_Type;

			BackColor = CoreUtils.SkinningDefs.TheInstance.GetColorDataGivenDefault("game_selection_screen_background_colour", Color.White);

			setColumnSizes();

			leftExpandHolder = new ScrollableExpandHolder ();
			Controls.Add(leftExpandHolder);
			BuildLeftHandPanel();

			rightExpandHolder = new ScrollableExpandHolder();
			Controls.Add(rightExpandHolder);
			BuildRightHandPanel();

			selectRaceSection.PlayPressed += selectRaceSection_PlayPressed;
			selectRaceSection.SkipTransitionPressed += selectRaceSection_SkipTransitionPressed;

			this.Resize += EditGamePanel_Resize;
			DoSize();

			UpdateParts();
		}

		public virtual void setColumnSizes()
		{
			FirstColumnWidth = 500;
			SecondColumnWidth = 500;
			ColumnGap = 5;
		}

		public virtual void UpdateParts ()
		{
			ShowSpecificParts(true);

			RearrangeLeftPanel();
			RearrangeRightPanel();
		}

		protected virtual void RearrangeLeftPanel ()
		{
			leftExpandHolder.DoSize();
		}

		protected virtual void RearrangeRightPanel ()
		{
			rightExpandHolder.DoSize();
		}

		protected virtual void ShowSpecificParts (bool showGameSpecificParts)
		{
			if (gameDetailsSection != null)
			{
				gameDetailsSection.ShowSpecificParts(showGameSpecificParts);
			}

			if (selectRaceSection != null)
			{
				selectRaceSection.Visible = showGameSpecificParts;
			}

			if (teamMembersSection != null)
			{
				teamMembersSection.Visible = showGameSpecificParts;
			}

			if (teamPhotoSection != null)
			{
				teamPhotoSection.Visible = showGameSpecificParts;
			}

			if (teamNameAndLogoSection != null)
			{
				teamNameAndLogoSection.Visible = showGameSpecificParts;
			}

			if (facilLogoSection != null)
			{
				facilLogoSection.Visible = showGameSpecificParts;
			}

			if (pdfSection != null)
			{
				pdfSection.Visible = showGameSpecificParts;
			}
		}

		protected virtual void BuildLeftHandPanel()
		{
			Add_Game_Details_Section();
			Add_Race_Selection();
        }

        protected virtual void Add_Game_Details_Section()
		{
			throw new Exception();
		}

		protected virtual void Add_Race_Selection()
		{
			selectRaceSection = new PhaseSelector (_gameFile);
			selectRaceSection.SetSize(500, 175);
			leftExpandHolder.AddExpandablePanel(selectRaceSection);
		}

		protected virtual void BuildRightHandPanel()
		{
			Add_Team_Members_Section();
			Add_Team_Photo_Section();
			Add_Team_And_Logo_Section();
			Add_Facilitator_Logo_Section();
			Add_Client_Logo_Section();
			Add_PDF_Section();
			Add_Options_Section();
		}

        protected virtual void Add_Team_Members_Section()
		{
			teamMembersSection = new TeamMemberDetails(_gameFile);
			teamMembersSection.Title = "Team Members";
			teamMembersSection.Location = new Point(FirstColumnWidth + ColumnGap, 0);
			teamMembersSection.SetSize(SecondColumnWidth, 275);
			rightExpandHolder.AddExpandablePanel(teamMembersSection);
		}

		protected virtual void Add_Team_Photo_Section()
		{
			teamPhotoSection = new TeamPhotoDetails(_gameFile);
			teamPhotoSection.Location = new Point(FirstColumnWidth + ColumnGap, 270);
			teamPhotoSection.SetSize(SecondColumnWidth, 230);
			rightExpandHolder.AddExpandablePanel(teamPhotoSection);
		}

		protected virtual void Add_Team_And_Logo_Section()
		{
			teamNameAndLogoSection = new TeamNameAndLogoDetails(_gameFile);
			teamNameAndLogoSection.Location = new Point(FirstColumnWidth + ColumnGap, 370);
			teamNameAndLogoSection.SetSize(SecondColumnWidth, 100);
			rightExpandHolder.AddExpandablePanel(teamNameAndLogoSection);
		}

		protected virtual void Add_Facilitator_Logo_Section()
		{
			facilLogoSection = new FacilitatorLogoDetails(_gameFile);
			facilLogoSection.Location = new Point(FirstColumnWidth + ColumnGap, 420);
			facilLogoSection.SetSize(SecondColumnWidth, 100);
			rightExpandHolder.AddExpandablePanel(facilLogoSection);
		}

		protected virtual void Add_Client_Logo_Section ()
		{
			clientLogoSection = new CoursewareDelivererLogoDetails (_gameFile);
			clientLogoSection.Location = new Point(FirstColumnWidth + ColumnGap, 570);
			clientLogoSection.SetSize(SecondColumnWidth, 150);
			rightExpandHolder.AddExpandablePanel(clientLogoSection);
		}

		protected virtual void Add_Options_Section ()
		{
			if (File.Exists(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles) + @"\config.xml"))
			{
				optionsSection = new OptionsDetails ();
				optionsSection.Location = new Point(FirstColumnWidth + ColumnGap, 820);
				optionsSection.SetSize(SecondColumnWidth, 90);
				rightExpandHolder.AddExpandablePanel(optionsSection);
			}
		}

		protected virtual void Add_PDF_Section()
		{
			pdfSection = new PDFSummary(_gameFile, this, false);
			pdfSection.Location = new Point(FirstColumnWidth + ColumnGap, 720);
			pdfSection.SetSize(SecondColumnWidth, 50);
			rightExpandHolder.AddExpandablePanel(pdfSection);
		}

	    protected void AddDocumentsSection (ScrollableExpandHolder expandHolder)
	    {
	        if (SkinningDefs.TheInstance.GetBoolData("game_details_show_facilitator_docs", false))
	        {
                facilitatorDocumentsPanel = new ExpandablePanel
                {
                    Title = "Documents",
                    Expanded = true,
                    Collapsible = false
                };

	            facilitatorDocumentsButton = SkinningDefs.TheInstance.CreateWindowsButton(FontStyle.Bold);
	            facilitatorDocumentsButton.Text = "Reprint Documents";
	            facilitatorDocumentsButton.Size = new Size(200, 22);
	            facilitatorDocumentsButton.Location = new Point(10, 5);
                facilitatorDocumentsButton.Click += facilitatorDocumentsButton_Click;
                facilitatorDocumentsPanel.Panel.Controls.Add(facilitatorDocumentsButton);

                facilitatorDocumentsPanel.SetSize(500, 30);

	            expandHolder.AddExpandablePanel(facilitatorDocumentsPanel);
            }
	    }

        IEnumerable<string> AddFooterToPdfs()
        {
            const string facilitatorSheetFilename = "Facilitator_Sheet.pdf";
            var originalFacilitatorSheetFilename = AppInfo.TheInstance.Location + $@"\data\{facilitatorSheetFilename}";
            var originalReprintsFilename = AppInfo.TheInstance.Location + @"\data\Reprints.zip";

            if (!SkinningDefs.TheInstance.GetBoolData("show_watermark_text", false))
            {
                return new List<string>
                {
                    originalFacilitatorSheetFilename,
                    originalFacilitatorSheetFilename
                };
            }

            var watermarkFormat = SkinningDefs.TheInstance.GetData("watermark_text_format");

	        var client = _gameFile.GetClient();
			client = client.Substring(0, Math.Min(client.Length, 30));

			var footerText = watermarkFormat.Replace("{clientName}", client)
                .Replace("{licenseDate}", _gameFile.Licence.ValidFromUtc?.ToString(SkinningDefs.TheInstance.GetData("watermark_text_date_format", "MMMM dd yyyy")) ?? "");
            
            var tempFilenames = new List<string>();

            var baseTempPath = Path.GetTempPath() + @"\facilitatorDocs\";

            var productName = Application.ProductName;
            var licensedGameFolder = baseTempPath + $@"{productName}\{_gameFile.Licence.ValidFromUtc:yyyy-MM-ddTHH_mm_ssZ}\";

            Directory.CreateDirectory(licensedGameFolder);

          
            const string reprintsFilename = "Reprints.zip";
            
            

            if (File.Exists(originalReprintsFilename))
            {
                var zipOutputDir = licensedGameFolder + @"zip\";
                Directory.CreateDirectory(zipOutputDir);

                var confPack = new ConfPack();
                var unzippedFilenames = confPack.ExtractAllFilesFromZip(originalReprintsFilename, zipOutputDir, string.Empty);

                var pdfEditorDir = licensedGameFolder + @"pdfEditor\";
                Directory.CreateDirectory(pdfEditorDir);

                var modifiedReprintsFilename = licensedGameFolder + reprintsFilename;
                confPack.CreateZip(modifiedReprintsFilename, unzippedFilenames, string.Empty);

                tempFilenames.Add(modifiedReprintsFilename);
            }
            
            return tempFilenames;
        }

        protected virtual void OpenFacilitatorDocuments(IEnumerable<string> filenames)
        {
            var found = false;
            foreach (var filename in filenames)
            {
                if (File.Exists(filename))
                {
                    try
                    {
                        System.Diagnostics.Process.Start(filename);
                        found = true;

                        break;
                    }
                    catch (Exception e)
                    {
                        string error;
                        if (e.Message.IndexOf("No Application") > -1)
                        {
                            error = "No PDF Reader Application installed";
                        }
                        else
                        {
                            error = "Failed to start PDF Reader";
                        }

                        MessageBox.Show(TopLevelControl, "Cannot open documentation", error,
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                }
            }

            if (!found)
            {
                MessageBox.Show(TopLevelControl, "Cannot open documentation", "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

		void facilitatorDocumentsButton_Click (object sender, EventArgs e)
		{
			OpenFacilitatorDocuments();
		}

		protected virtual void OpenFacilitatorDocuments ()
		{
            if (AllGameDetailsSections.Any(s => !s.SaveData()))
            {
                return;
            }

            OpenFacilitatorDocuments(AddFooterToPdfs());
        }

        public virtual void ShowTeamName(Boolean show)
		{
			if (teamNameAndLogoSection != null)
			{
				teamNameAndLogoSection.ChangeTeamNameVisibility(show);
			}
		}

		public void ShowPDFSection(Boolean show)
		{
			if (pdfSection != null)
			{
				pdfSection.Visible = show;
			}
		}

		public void ShowTeamMembersSection(Boolean show)
		{
			if (teamMembersSection != null)
			{
				teamMembersSection.Visible = show;
			}
		}

		public void ShowTeamNameAndLogoSection(Boolean show)
		{
			if (teamNameAndLogoSection != null)
			{
				teamNameAndLogoSection.Visible = show;
			}
		}

		public void ShowFacilitatorSection(Boolean show)
		{
			if (facilLogoSection != null)
			{
				facilLogoSection.Visible = show;
			}
		}

		public void ShowTeamPhotoSection(Boolean show)
		{
			if (teamPhotoSection != null)
			{
				teamPhotoSection.Visible = show;
			}
		}

		public delegate bool GameSetupHandler(GameManagement.NetworkProgressionGameFile game);
		public event GameSetupHandler GameSetup;

		public virtual bool ValidateFields (bool reportErrors = true)
		{
			foreach (GameDetailsSection section in AllGameDetailsSections)
			{
				if (! section.ValidateFields(reportErrors))
				{
					return false;
				}
			}

			return true;
		}

		protected virtual IList<GameDetailsSection> AllGameDetailsSections
		{
			get
			{
				List<GameDetailsSection> list = new List<GameDetailsSection> ();

				foreach (Control control in Controls)
				{
					if (control is GameDetailsSection)
					{
						list.Add((GameDetailsSection) control);
					}

					if (control is ScrollableExpandHolder)
					{
						foreach (Control panel in ((ScrollableExpandHolder) control).Panels)
						{
							if (panel is GameDetailsSection)
							{
								list.Add((GameDetailsSection) panel);
							}
						}
					}
				}

				return list.AsReadOnly();
			}
		}

		public virtual bool SetupGame ()
		{
			if (! ValidateFields())
			{
				return false;
			}

			foreach (GameDetailsSection section in AllGameDetailsSections)
			{
				if (! section.SaveData())
				{
					return false;
				}
			}

			_gameFile.Licence?.UpdateDetails(new Licensor.GameDetails (gameDetailsSection.GameTitle, gameDetailsSection.GameVenue,
				gameDetailsSection.GameLocation, gameDetailsSection.GameClient, gameDetailsSection.GameRegion,
				gameDetailsSection.GameCountry, gameDetailsSection.GameChargeCompany, gameDetailsSection.GameNotes, gameDetailsSection.GamePurpose, gameDetailsSection.GamePlayers));

			_gameFile.Save(true);

			if (_gameFile.Game_Eval_Type != lastKnownEvalType)
			{
				lastKnownEvalType = _gameFile.Game_Eval_Type;

				OnGameEvalTypeChanged();
			}

			if (GameSetup != null)
			{
				return GameSetup(_gameFile);
			}

			return false;
		}

		public event EventHandler GameEvalTypeChanged;

		void OnGameEvalTypeChanged ()
		{
			GameEvalTypeChanged?.Invoke(this, EventArgs.Empty);
		}

		protected virtual void OnPlayPressed (int phase)
		{
			PlayPressed?.Invoke(phase);
		}

		protected virtual void selectRaceSection_PlayPressed(object sender, EventArgs args)
		{
			int phase = selectRaceSection.SelectedPhase;

			if(_gameFile.IsSalesGame)
			{
				// This is a sales game so always just allow it....
				OnPlayPressed(phase);
			}
			else if (ValidateFields())
			{
				if (SetupGame())
				{
					if (phase == _gameFile.LastPhaseNumberPlayed)
					{
						if (_gameFile.CurrentPhaseHasStarted)
						{
                            using (CustomDialogBox dialog = new CustomDialogBox(CustomDialogBox.MessageType.Warning))
                            {
                                dialog.setText("Replaying this round will reset the results.\r\nDo you wish to continue?");
                                dialog.setOKButtonText("Rewind");
                                dialog.ShowDialog(TopLevelControl);

                                if (dialog.DialogResult != DialogResult.OK)
                                {
                                    return;
                                }
                            }

    					}
					}

					OnPlayPressed(phase);
				}
			}
		}

		void EditGamePanel_Resize(object sender, EventArgs e)
		{
			DoSize();
		}

		void DoSize ()
		{
			leftExpandHolder.Size = new Size(this.FirstColumnWidth , Height);
			rightExpandHolder.Size = new Size(this.SecondColumnWidth + 2 * ColumnGap, Height);

			leftExpandHolder.Location = new Point (0, 0);
			rightExpandHolder.Location = new Point (leftExpandHolder.Right, 0);
		}
	
		protected override void Dispose(bool disposing)
		{
			if(disposing)
			{
				gameDetailsSection.Dispose();
				selectRaceSection.Dispose();
				leftExpandHolder.Dispose();
				rightExpandHolder.Dispose();

				if (teamMembersSection != null)
				{
					teamMembersSection.Dispose();
				}

				if (teamPhotoSection != null)
				{
					teamPhotoSection.Dispose();
				}

				//mind that some sections may be missing intentionally  
				if (teamNameAndLogoSection != null)
				{
					teamNameAndLogoSection.Dispose();
				}
				if (facilLogoSection != null) 
				{
					facilLogoSection.Dispose();
				}

				if (pdfSection != null)
				{
					pdfSection.Dispose();
				}

			    facilitatorDocumentsPanel?.Dispose();
			}
			base.Dispose (disposing);
		}

		void selectRaceSection_SkipTransitionPressed(object sender, EventArgs args)
		{
			int phase = selectRaceSection.SelectedPhase;

			if(_gameFile.IsSalesGame)
			{
				// This is a sales game so always just allow it....
				SkipPressed(phase);
			}
			else if (ValidateFields())
			{
				if (SetupGame())
				{
					if ((phase == _gameFile.LastPhaseNumberPlayed)
						&& _gameFile.CurrentPhaseHasStarted)
						{

                        using (CustomDialogBox dialog = new CustomDialogBox(CustomDialogBox.MessageType.Warning))
                        {
                            dialog.setText("Replaying this round will reset the results.\r\nDo you wish to continue?");
                            dialog.setOKButtonText("Rewind");
                            dialog.ShowDialog(TopLevelControl);

                            if (dialog.DialogResult != DialogResult.OK)
                            {
                                return;
                            }
						}
					}
					SkipPressed(phase);
				}
			}
		}

		public virtual void QuickStartGame ()
		{
			selectRaceSection_PlayPressed(this, EventArgs.Empty);
		}

		public event EventHandler Changed;

		protected void OnChanged ()
		{
			Changed?.Invoke(this, EventArgs.Empty);
		}

		void gameDetailsSection_Changed (object sender, EventArgs args)
		{
			OnChanged();
		}

		public PhaseSelector PhaseSelector
		{
			get
			{
				return selectRaceSection;
			}
		}

		public event EventHandler ReportsInvalidated;

		protected virtual void OnReportsInvalidated ()
		{
			ReportsInvalidated?.Invoke(this, EventArgs.Empty);
		}

		public event GenerateReportHandler ExportReportImages;

		protected virtual void OnExportReportImages (GenerateReportEventArgs args)
		{
			ExportReportImages?.Invoke(this, args);
		}
	}
}