using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using Algorithms;
using BaseUtils;
using CommonGUI;
using CoreUtils;
using GameManagement;
using LibCore;
using Licensor;
using Media;
using zip = ICSharpCode.SharpZipLib;

namespace GameDetails
{
	public class GameSelectionScreen : BasePanel
	{
		protected IProductLicensor productLicensor;
		protected IProductLicence productLicence;

		protected ScrollableExpandHolder expandHolder;
		protected ScrollableExpandHolder rightExpandHolder;

		protected ExpandablePanel startNewGame;
		protected ExpandablePanel loadSavedGame;
		protected ExpandablePanel license;
		protected ExpandablePanel documents;
		ExpandablePanel options;
		protected ExpandablePanel DisplaySetting;

		protected Button facilitatorDocumentsButton;
		protected Button swimLanesButton;

		protected Panel logoSection;
		protected ImageBox logoBox;

		protected EntryBox resetPassword;
		protected Button resetPass_ok;
		protected Button resetPass_cancel;
		protected Label resetEnterPass;
		protected Label resetErrorMsg;

		Control gb_Chargeable;
		Control gb_NonChargeable;

		Label Tooltip;

		protected Panel gamesList;

		protected IGameLoader _gameLoader;

		protected string gameToLoad;
		protected Button loadGame;
		protected Button exportGames;
		protected Button importGames;

		Label licenceRefreshTimeLabel;
		Label licenceRefreshTime;

		public delegate bool PasswordCheckHandler (string password);

		public event PasswordCheckHandler PasswordCheck;

		protected VersionUpdatePanel versionPanel;

		protected Button licensedGame;
		protected Button unofficialGame;
		protected Button trainingGame;
		protected Button salesGame;

		protected Font MyDefaultSkinFontNormal8;
		protected Font MyDefaultSkinFontBold8;
		protected Font MyDefaultSkinFontBold9;
		protected Font MyDefaultSkinFontNormal9;
		protected Font MyDefaultSkinFontToolTip;

		protected string DirectPuchaseWebSite = "";

		Label dataImportLabel;

		RadioButton directShowButton;

		CheckBox useMultipleScreensBox;

		Button launchMaturityEditorButton;
		Form maturityEditor;
		FormCreator maturityFormCreator;

		CreateGamePanel createGamePanel;

		public delegate Form FormCreator ();

		public ScrollableExpandHolder LeftColumn => expandHolder;
		public ScrollableExpandHolder RightColumn => rightExpandHolder;

		public GameSelectionScreen (IGameLoader gameLoader, IProductLicence productLicence, IProductLicensor productLicensor)
		{
			this.productLicensor = productLicensor;
			this.productLicence = productLicence;

			string fontname = SkinningDefs.TheInstance.GetData("fontname");
			MyDefaultSkinFontNormal8 = ConstantSizeFont.NewFont(fontname, 8, FontStyle.Regular);
			MyDefaultSkinFontNormal9 = ConstantSizeFont.NewFont(fontname, 9, FontStyle.Regular);
			MyDefaultSkinFontBold8 = ConstantSizeFont.NewFont(fontname, 8, FontStyle.Bold);
			MyDefaultSkinFontBold9 = ConstantSizeFont.NewFont(fontname, 9, FontStyle.Bold);
			MyDefaultSkinFontToolTip =
				ConstantSizeFont.NewFont(SkinningDefs.TheInstance.GetData("font_name_tooltip", fontname),
					float.Parse(SkinningDefs.TheInstance.GetData("font_size_tooltip", "8")),
					FontStyle.Bold);

			_gameLoader = gameLoader;
			this.BackColor =
				CoreUtils.SkinningDefs.TheInstance.GetColorDataGivenDefault("game_selection_screen_background_colour", Color.White);

			CreateHolders();

			DoSize();

			AddSections();

			productLicence.RefreshStatusComplete += productLicence_RefreshStatusComplete;

			UpdateGameButtons();
			UpdateLicencePanel();
			UpdateVersionPanel();
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing)
			{
				productLicence.RefreshStatusComplete -= productLicence_RefreshStatusComplete;

				if (versionPanel != null)
				{
					versionPanel.Dispose();
				}
			}

			base.Dispose(disposing);
		}

		void productLicence_RefreshStatusComplete (object sender, EventArgs args)
		{
			if (InvokeRequired)
			{
				BeginInvoke(new MethodInvoker(UpdateLicencePanel));
				BeginInvoke(new MethodInvoker(UpdateVersionPanel));
				BeginInvoke(new MethodInvoker(UpdateGameButtons));
			}
			else
			{
				UpdateLicencePanel();
				UpdateVersionPanel();
				UpdateGameButtons();
			}
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);
			DoSize();
		}

		protected virtual void CreateHolders ()
		{
			expandHolder = new ScrollableExpandHolder();
			expandHolder.TopGap = SkinningDefs.TheInstance.GetIntData("left_expandable_panel_top_gap", 0);
			Controls.Add(expandHolder);

			rightExpandHolder = new ScrollableExpandHolder();
			Controls.Add(rightExpandHolder);
		}

		protected virtual void AddSections ()
		{
			AddLogoSection();

			AddNewGameSection();
			AddSavedGamesSection();
			AddLicenceSection();
			AddDocumentsSection();
			AddOptionsSection(SkinningDefs.TheInstance.GetBoolData("use_multiple_screens", false));
		}

		protected virtual void AddLogoSection ()
		{
			logoSection = new Panel();

			logoBox = new ImageBox();
			logoBox.Location = new Point(0, 0);
			logoBox.Size = new Size(400, 430);
			logoBox.Image =
				LibCore.Repository.TheInstance.GetImage(LibCore.AppInfo.TheInstance.Location + "\\images\\main_logo.png");
			logoBox.SizeMode = PictureBoxSizeMode.Zoom;
			logoSection.Controls.Add(logoBox);
			logoSection.Size = new Size(logoBox.Right, logoBox.Bottom + 50);

			rightExpandHolder.AddExpandablePanel(logoSection);
		}

		Control CreateGroupBox (string legend)
		{
			Control box;

			if (SkinningDefs.TheInstance.GetBoolData("game_selection_screen_use_boxes", true))
			{
				box = new GroupBox { Text = legend };
			}
			else
			{
				box = new Panel();
				var label = new Label
				{
					Text = legend,
					TextAlign = ContentAlignment.TopCenter,
					Font = MyDefaultSkinFontBold9
				};

				box.Controls.Add(label);
			}

			return box;
		}

		void FixGroupLabel (Control control)
		{
			foreach (var child in control.Controls)
			{
				var label = child as Label;
				if (label != null)
				{
					label.Bounds = new Rectangle(0, 0, control.Width, label.PreferredHeight);
					label.SendToBack();
				}
			}
		}

		protected virtual void AddNewGameSection ()
		{
			startNewGame = new ExpandablePanel();
			startNewGame.Location = new Point(5, 5);
			startNewGame.SetSize(500, 125);
			startNewGame.Collapsible = false;
			startNewGame.Title = "Start A New Game";
			expandHolder.AddExpandablePanel(startNewGame);

			gb_Chargeable = CreateGroupBox("Chargeable");
			gb_Chargeable.Location = new System.Drawing.Point(10, 0);
			gb_Chargeable.Width = (startNewGame.Width - 24) / 2;
			gb_Chargeable.Height = 75;
			gb_Chargeable.TabIndex = 1;
			gb_Chargeable.TabStop = false;
			gb_Chargeable.ForeColor = SkinningDefs.TheInstance.GetColorData("game_selection_screen_text_colour", Color.Black);
			startNewGame.ThePanel.Controls.Add(gb_Chargeable);

			gb_NonChargeable = CreateGroupBox("Non Chargeable");
			gb_NonChargeable.Location = new System.Drawing.Point(20 + gb_Chargeable.Width, 0);
			gb_NonChargeable.Width = (startNewGame.Width - 24) / 2;
			gb_NonChargeable.Height = 75;
			gb_NonChargeable.TabIndex = 1;
			gb_NonChargeable.TabStop = false;
			gb_NonChargeable.ForeColor = SkinningDefs.TheInstance.GetColorData("game_selection_screen_text_colour", Color.Black);
			startNewGame.ThePanel.Controls.Add(gb_NonChargeable);

			int button_width = gb_NonChargeable.Width - 20;

			licensedGame = NewButtonToGroupBox(gb_Chargeable, "Simulation Game", 10, 15, button_width, 22);
			licensedGame.Font = this.MyDefaultSkinFontBold9;
			licensedGame.MouseEnter += licensedGame_MouseEnter;
			licensedGame.MouseLeave += licensedGame_MouseLeave;
			licensedGame.Click += licensedGame_Click;
			licensedGame.Enabled = (productLicence.GameCreditsRemaining > 0);

			unofficialGame = NewButtonToGroupBox(gb_Chargeable, "Non-Billable Game", 10, 42, button_width, 22);
			unofficialGame.Font = this.MyDefaultSkinFontBold9;
			unofficialGame.MouseEnter += unofficialGame_MouseEnter;
			unofficialGame.MouseLeave += unofficialGame_MouseLeave;
			unofficialGame.Click += unofficialGame_Click;
			unofficialGame.Visible = productLicence.CanWeCreateGamesOffTheRecord;
			unofficialGame.Enabled = true;

			//Reset the button width to match the GroupBox 
			button_width = gb_NonChargeable.Width - 20;

			//trainingGame = NewButton(startNewGame, "Training Game", 5, 35, 205, 25);
			trainingGame = NewButtonToGroupBox(gb_NonChargeable, "Training Game", 10, 15, button_width, 22);
			trainingGame.Font = this.MyDefaultSkinFontBold9;
			trainingGame.MouseEnter += trainingGame_MouseEnter;
			trainingGame.MouseLeave += trainingGame_MouseLeave;
			trainingGame.Click += trainingGame_Click;

			//salesGame = NewButton(startNewGame, "Sales Game", 5, 65, 205, 25);
			salesGame = NewButtonToGroupBox(gb_NonChargeable, "Sales Game", 10, 42, button_width, 22);
			salesGame.Font = this.MyDefaultSkinFontBold9;
			salesGame.MouseEnter += salesGame_MouseEnter;
			salesGame.MouseLeave += salesGame_MouseLeave;
			salesGame.Click += salesGame_Click;

			FixGroupLabel(gb_Chargeable);
			FixGroupLabel(gb_NonChargeable);

			startNewGame.Panel.Leave += LicensedGamePanel_Leave;

			Tooltip = new Label();
			Tooltip.Font = MyDefaultSkinFontToolTip;
			Tooltip.Text = "";
			Tooltip.Visible = false;
			Tooltip.Size = new Size(startNewGame.Width - 20, 70);
			Tooltip.Location = new Point(10, this.gb_Chargeable.Top + this.gb_Chargeable.Height + 10);
			Tooltip.ForeColor = SkinningDefs.TheInstance.GetColorData("game_selection_screen_text_colour", Color.Black);
			startNewGame.ThePanel.Controls.Add(Tooltip);
		}

		protected virtual void AddSavedGamesSection ()
		{
			AddSavedGamesSection(expandHolder);
		}

		protected virtual void AddSavedGamesSection (ScrollableExpandHolder holder)
		{
			loadSavedGame = new ExpandablePanel();
			loadSavedGame.Expanded = true;
			loadSavedGame.Location = new Point(5, 150);
			loadSavedGame.SetSize(500, 190 - 5);
			loadSavedGame.Title = "Saved Games";
			loadSavedGame.Collapsible = false;
			holder.AddExpandablePanel(loadSavedGame);

			FillGamesList();

			loadGame = SkinningDefs.TheInstance.CreateWindowsButton(FontStyle.Bold);
			loadGame.Location = new Point(330, 180 - 20);
			loadGame.Size = new Size(80, 22);
			loadGame.Click += loadGame_Click;
			loadGame.Enabled = false;
			loadGame.Text = "Load";
			loadSavedGame.ThePanel.Controls.Add(loadGame);

			importGames = SkinningDefs.TheInstance.CreateWindowsButton(FontStyle.Bold);
			importGames.Location = new Point(330, 20);
			importGames.Size = new Size(80, 22);
			importGames.Click += importGames_Click;
			importGames.Text = "Import...";
			loadSavedGame.ThePanel.Controls.Add(importGames);

			exportGames = SkinningDefs.TheInstance.CreateWindowsButton(FontStyle.Bold);
			exportGames.Location = new Point(330, 60);
			exportGames.Size = new Size(80, 22);
			exportGames.Click += exportGames_Click;
			exportGames.Text = "Export...";
			loadSavedGame.ThePanel.Controls.Add(exportGames);

			bool showImportExport = SkinningDefs.TheInstance.GetBoolData("allow_import_and_export_of_games", true);
			importGames.Visible = showImportExport;
			exportGames.Visible = showImportExport;
		}

		protected virtual void AddOptionsSection (bool allowMultipleScreens = false)
		{
			options = new ExpandablePanel();
			options.Title = "Options";
			options.Expanded = true;

			var y = 10;

			directShowButton = new RadioButton
			{
				Text = "Use Windows Media Player",
				Bounds = new Rectangle(0, y, 200, 30),
				Font = MyDefaultSkinFontNormal8
			};
			directShowButton.CheckedChanged += directShowButton_CheckedChanged;

			options.ThePanel.Controls.Add(directShowButton);

			directShowButton.Checked = (PersistentGlobalOptions.MediaMode == MediaMode.Windows);
			y = directShowButton.Bottom + 20;

			useMultipleScreensBox = new CheckBox
			{
				Text = "Use multiple displays",
				Bounds = new Rectangle(0, y, 200, 30),
				Font = MyDefaultSkinFontNormal8,
				Checked = PersistentGlobalOptions.UseMultipleScreens
			};
			useMultipleScreensBox.CheckedChanged += useMultipleScreensBox_CheckedChanged;
			useMultipleScreensBox.Checked = PersistentGlobalOptions.UseMultipleScreens;

			if (allowMultipleScreens)
			{
				options.ThePanel.Controls.Add(useMultipleScreensBox);
				y = useMultipleScreensBox.Bottom + 20;
			}

			launchMaturityEditorButton = SkinningDefs.TheInstance.CreateWindowsButton();
			launchMaturityEditorButton.Text = "Launch Maturity Criteria Editor";
			launchMaturityEditorButton.Bounds = new Rectangle (0, y, 200, 30);
			launchMaturityEditorButton.Font = MyDefaultSkinFontNormal8;
			launchMaturityEditorButton.Click += launchMaturityEditorButton_Click;
			options.ThePanel.Controls.Add(launchMaturityEditorButton);
			y = launchMaturityEditorButton.Bottom + 20;

			UpdateMaturityEditorButton();

			options.SetSize(500, y);

			rightExpandHolder.AddExpandablePanel(options);
		}

		void launchMaturityEditorButton_Click (object sender, EventArgs args)
		{
			maturityEditor = maturityFormCreator();
			maturityEditor.Closed += maturityEditor_Closed;
			maturityEditor.Show();

			UpdateMaturityEditorButton();
		}

		void UpdateMaturityEditorButton ()
		{
			launchMaturityEditorButton.Visible = (maturityFormCreator != null);
			launchMaturityEditorButton.Enabled = (maturityEditor == null);
		}

		void maturityEditor_Closed (object sender, EventArgs args)
		{
			maturityEditor = null;
			UpdateMaturityEditorButton();
		}

		public FormCreator MaturityEditorCreator
		{
			set
			{
				maturityFormCreator = value;
				UpdateMaturityEditorButton();
			}
		}

		void directShowButton_CheckedChanged (object sender, EventArgs args)
		{
			if (directShowButton.Checked)
			{
				PersistentGlobalOptions.MediaMode = MediaMode.Windows;
				SoundPlayer.SetUseVlc(false);
			}
		}

		void useMultipleScreensBox_CheckedChanged (object sender, EventArgs args)
		{
			PersistentGlobalOptions.UseMultipleScreens = useMultipleScreensBox.Checked;
		}

		protected virtual void AddDisplaySection ()
		{
			DisplaySetting = new ExpandablePanel();
			DisplaySetting.Title = "Language";
			DisplaySetting.Expanded = true;
			DisplaySetting.Location = new Point(0, 656 - DisplaySetting.GetSize().Height);
			rightExpandHolder.AddExpandablePanel(DisplaySetting);
		}

		protected virtual void AddLicenceSection ()
		{
			AddLicenceSection(expandHolder);
		}

		protected virtual void AddLicenceSection (ScrollableExpandHolder holder)
		{
			int leftColumnWidth = ((holder == expandHolder) ? 220 : 190);
			int rightColumnStart = leftColumnWidth + 10;

			int passwordEntryWidth = ((holder == expandHolder) ? 80 : 60);

			license = new ExpandablePanel();
			license.Location = new Point(5, 515 - 25);
			license.Title = "License";
			license.Expanded = true;
			license.Collapsible = false;
			holder.AddExpandablePanel(license);

			int label_height = 30;

			Color textColour = SkinningDefs.TheInstance.GetColorData("game_selection_screen_text_colour", Color.Black);

			licenceRefreshTimeLabel = new Label
			{
				Bounds = new Rectangle(10, 5, 100, label_height),
				Font = MyDefaultSkinFontBold8,
				Text = "License Updated:"
			};
			license.Panel.Controls.Add(licenceRefreshTimeLabel);

			licenceRefreshTime = new Label
			{
				Bounds = new Rectangle(licenceRefreshTimeLabel.Right, 5, rightColumnStart - 10 - licenceRefreshTimeLabel.Right,
					label_height),
				Font = MyDefaultSkinFontNormal8
			};
			license.Panel.Controls.Add(licenceRefreshTime);

			Label appVersionLabel = new Label();
			appVersionLabel.Location = new Point(rightColumnStart, 5 + (0 * label_height));
			appVersionLabel.Font = MyDefaultSkinFontBold8;
			appVersionLabel.Text = "Software Version:";
			appVersionLabel.Size = new Size(120, label_height);
			appVersionLabel.ForeColor = textColour;
			license.Panel.Controls.Add(appVersionLabel);

			Label appVersionContent = new Label();
			appVersionContent.Location = new Point(appVersionLabel.Right, 5 + (0 * label_height));
			appVersionContent.Font = MyDefaultSkinFontNormal8;
			appVersionContent.Text = Application.ProductVersion;
			appVersionContent.Size = new Size(150, label_height);
			appVersionContent.ForeColor = textColour;
			license.Panel.Controls.Add(appVersionContent);

			Label appBuildNotesContent = new Label();
			appBuildNotesContent.Location = new Point(appVersionLabel.Right, 5 + (1 * label_height));
			appBuildNotesContent.Font = MyDefaultSkinFontNormal8;
			appBuildNotesContent.Text = Assembly.GetEntryAssembly()
				.GetCustomAttributes(typeof (AssemblyDescriptionAttribute), false).OfType<AssemblyDescriptionAttribute>()
				.FirstOrDefault().Description;
			appBuildNotesContent.Size = new Size(150, label_height);
			appBuildNotesContent.ForeColor = textColour;
			license.Panel.Controls.Add(appBuildNotesContent);

			versionPanel = new VersionUpdatePanel();
			license.Panel.Controls.Add(versionPanel);
			versionPanel.Location = new Point(rightColumnStart, 5 + (2 * label_height));
			versionPanel.Size = new Size(270, 50);
			versionPanel.ShowFull = true;

			int startPointY = 95;

			Button resetButton = SkinningDefs.TheInstance.CreateWindowsButton(FontStyle.Bold);
			resetButton.Text = "Reset License";
			resetButton.Size = new Size(130, 22);
			resetButton.Location = new Point(10, startPointY);
			resetButton.Click += resetButton_Click;
			license.Panel.Controls.Add(resetButton);
			resetButton.BringToFront();

			resetPassword = new EntryBox();
			resetPassword.Font = this.MyDefaultSkinFontNormal8;
			resetPassword.PasswordChar = '*';
			resetPassword.Visible = false;
			resetPassword.Location = new Point(220 + 40, startPointY);
			resetPassword.Width = passwordEntryWidth;
			license.Panel.Controls.Add(resetPassword);
			resetPassword.KeyDown += this.resetPassword_KeyDown;

			resetPass_ok = SkinningDefs.TheInstance.CreateWindowsButton(FontStyle.Bold);
			resetPass_ok.Text = "OK";
			resetPass_ok.Visible = false;
			resetPass_ok.Size = new Size(60, 22);
			resetPass_ok.Location = new Point(resetPassword.Right + 10, startPointY);
			resetPass_ok.Click += resetPass_ok_Click;
			license.Panel.Controls.Add(resetPass_ok);
			resetPass_ok.BringToFront();

			resetPass_cancel = SkinningDefs.TheInstance.CreateWindowsButton(FontStyle.Bold);
			resetPass_cancel.Font = this.MyDefaultSkinFontBold8;
			resetPass_cancel.Text = "Cancel";
			resetPass_cancel.Visible = false;
			resetPass_cancel.Size = new Size(60, 22);
			resetPass_cancel.Location = new Point(resetPass_ok.Right + 10, startPointY);
			resetPass_cancel.Click += resetPass_cancel_Click;
			license.Panel.Controls.Add(resetPass_cancel);

			resetEnterPass = new Label();
			resetEnterPass.Font = this.MyDefaultSkinFontBold8;
			resetEnterPass.Text = "Enter Password:";
			resetEnterPass.Location = new Point(150, startPointY + 3);
			resetEnterPass.Size = new Size(140, 22);
			resetEnterPass.Visible = false;
			license.Panel.Controls.Add(resetEnterPass);
			resetEnterPass.BringToFront();
			resetPassword.BringToFront();

			resetErrorMsg = new Label();
			resetErrorMsg.Font = this.MyDefaultSkinFontBold8;
			resetErrorMsg.Text = "";
			resetErrorMsg.Location = new Point(220, startPointY + resetEnterPass.Height + 5);
			resetErrorMsg.ForeColor = Color.Red;
			resetErrorMsg.Size = new Size(140, 22);
			resetErrorMsg.Visible = false;
			license.Panel.Controls.Add(resetErrorMsg);
			license.Panel.Leave += ResetEntryPanel_Leave;

			license.SetSize(500, resetButton.Bottom + SkinningDefs.TheInstance.GetIntData("license_box_bottom_pad", 6));

			UpdateLicencePanel();
			UpdateVersionPanel();
		}

		void UpdateLicencePanel ()
		{
			if (license != null)
			{
				licenceRefreshTime.Text = productLicence.TimeLastUpdatedUtc.ToString();
			}
		}

		void UpdateVersionPanel ()
		{
			if (versionPanel != null)
			{
				versionPanel.SetInfo(! productLicence.UpgradeIsAvailable, productLicence.UpgradeMessage,
					productLicence.UpgradeDownloadUrl, productLicence.UpgradeDownloadUserName, productLicence.UpgradeDownloadPassword);
			}
		}

		protected virtual void AddDocumentsSection (ScrollableExpandHolder holder)
		{
			bool allowFacSheet = SkinningDefs.TheInstance.GetBoolData("allow_fac_sheet", true);
			bool allowSwimLanesSheet = SkinningDefs.TheInstance.GetBoolData("allow_swim_lanes_sheet", true);
			bool hideFacSheet = SkinningDefs.TheInstance.GetBoolData("hide_fac_sheet", false);

			if (! hideFacSheet)
			{
				documents = new ExpandablePanel();
				documents.Location = new Point(5, 570);
				documents.Title = "Documents";
				documents.Expanded = true;
				documents.Collapsible = false;

				facilitatorDocumentsButton = SkinningDefs.TheInstance.CreateWindowsButton(FontStyle.Bold);
				facilitatorDocumentsButton.Text = "Reprint Documents";
				facilitatorDocumentsButton.Size = new Size(200, 22);
				facilitatorDocumentsButton.Location = new Point(10, 10 - 5);
				facilitatorDocumentsButton.Enabled = allowFacSheet;
				facilitatorDocumentsButton.Visible = true;
				facilitatorDocumentsButton.Click += docButton_Click;
				documents.Panel.Controls.Add(facilitatorDocumentsButton);
			}

			if (allowSwimLanesSheet
			    && (facilitatorDocumentsButton != null)
			    && System.IO.File.Exists(LibCore.AppInfo.TheInstance.Location + "\\data\\SwimLanes_Sheet.pdf"))
			{
				swimLanesButton = SkinningDefs.TheInstance.CreateWindowsButton(FontStyle.Bold);
				swimLanesButton.Text = "Quick Guide";
				swimLanesButton.Size = new Size(200, 22);
				swimLanesButton.Location = new Point(facilitatorDocumentsButton.Left + facilitatorDocumentsButton.Width + 5, 5);
				swimLanesButton.Visible = true;
				swimLanesButton.Click += docButton_Click;
				documents.Panel.Controls.Add(swimLanesButton);
			}

			if (facilitatorDocumentsButton != null)
			{
				documents.SetSize(500, facilitatorDocumentsButton.Bottom);
			}

			if (documents != null)
			{
				holder.AddExpandablePanel(documents);
			}
		}

		protected virtual void AddDocumentsSection ()
		{
			AddDocumentsSection(expandHolder);
		}

		void UpdateGameButtons ()
		{
			bool chargableAllowed = (productLicence.GameCreditsRemaining > 0);
			bool showGameButtons = true;

			bool salesAllowed = SkinningDefs.TheInstance.GetBoolData("allow_sales_game", true);
			bool trainingAllowed = SkinningDefs.TheInstance.GetBoolData("allow_training_game", true);

			gb_Chargeable.Visible = showGameButtons;
			gb_NonChargeable.Visible = ((showGameButtons && salesAllowed) || (showGameButtons && trainingAllowed));
			licensedGame.Visible = showGameButtons;
			licensedGame.Enabled = chargableAllowed;

			trainingGame.Visible = (showGameButtons && trainingAllowed);
			trainingGame.Enabled = true;

			salesGame.Visible = (showGameButtons && salesAllowed);
			salesGame.Enabled = true;

			gamesList.Enabled = showGameButtons;

			if (documents != null)
			{
				documents.Visible = true;
			}

			loadGame.Enabled = (gameToLoad != null);
			importGames.Enabled = true;

			int games = 0;
			foreach (Control control in gamesList.Controls)
			{
				if (control is GameDetailRow)
				{
					games++;
				}
			}
			exportGames.Enabled = (games > 0);
		}

		protected virtual void DoSize ()
		{
			expandHolder.Bounds = new Rectangle(0, 0, 530, Height);

			var width = Math.Min(expandHolder.Width, Width - expandHolder.Width);
			rightExpandHolder.Bounds = new Rectangle(Width - width, 0, width, Height);

			if (createGamePanel != null)
			{
				createGamePanel.Bounds = new Rectangle(0, 0, Width, Height);
			}
		}

		void EnsurePathExists (string path)
		{
			if (! Directory.Exists(path))
			{
				string parent = Path.GetDirectoryName(path);
				if ((parent != "") && (parent != null) && (parent != path) &&
				    ((parent.IndexOf('\\') != -1) || (parent.IndexOf("/") != -1)))
				{
					EnsurePathExists(parent);
				}

				Directory.CreateDirectory(path);
			}
		}

		protected void FillGamesList ()
		{
			if (gamesList != null)
			{
				gamesList.Dispose();
			}

			gamesList = new Panel();

			gamesList.BorderStyle = BorderStyle.FixedSingle;
			gamesList.AutoScroll = true;
			gamesList.Location = new Point(10, 5 - 5);
			gamesList.Size = new Size(300, 200 - 20);

			int offset = 0;
			bool flipColour = false;

			Color color1 = CONVERT.ParseComponentColor(SkinningDefs.TheInstance.GetData("games_list_row_colour", "186,224,255"));
			Color color2 = CONVERT.ParseComponentColor(SkinningDefs.TheInstance.GetData("games_list_row_colour_alternate", "155,210,255"));

			string gamesPath = LibCore.AppInfo.TheInstance.Location + "\\games";
			EnsurePathExists(gamesPath);

			// Transfer any old games files to the new location.
			string oldGamesPath = LibCore.AppInfo.TheInstance.InstallLocation + "\\games";
#if ! DEBUG
			try
			{
				if (Directory.Exists(oldGamesPath))
				{
					foreach (string file in Directory.GetFiles(oldGamesPath))
					{
						string destination = gamesPath + "\\" + Path.GetFileName(file);

						if (! File.Exists(destination))
						{
							File.Move(file, destination);
						}
					}
				}
			}
			catch
			{
				// Maybe we didn't have permission to do the move.
			}
#endif

			DirectoryInfo dir = new DirectoryInfo(gamesPath);
			FileInfo [] filesInDir = dir.GetFiles("*.gmz");

			DateTime [] creationTimes = new DateTime[filesInDir.Length];

			for (int i = 0; i < filesInDir.Length; i++)
			{
				creationTimes[i] = filesInDir[i].CreationTime;
			}
			Array.Sort(creationTimes, filesInDir);

			if (SkinningDefs.TheInstance.GetBoolData("sort_game_files_most_recent_first", true))
			{
				Array.Reverse(filesInDir);
			}

			int count = 0;

			foreach (FileInfo file in filesInDir)
			{
				try
				{
					++count;

					string fileName = file.Name;
					string firstPart, lastPart;
					DateTime date = GameUtils.FileNameToCreationDate(fileName);
					string shortname = GameUtils.FileNameToGameName(fileName, out firstPart, out lastPart);

					// Actually display the date on the screen in local format to help the user.
					GameDetailRow gdr = new GameDetailRow(shortname, date.ToShortDateString(), file.FullName);
					gdr.Location = new Point(0, offset);
					gdr.Size = new Size(281, 20);
					gdr.ForeColor = SkinningDefs.TheInstance.GetColorData("games_list_fore_colour", Color.Black);

					if (flipColour)
					{
						gdr.SetBaseColor(color1);
					}
					else
					{
						gdr.SetBaseColor(color2);
					}
					gdr.GameSelected += gdr_GameSelected;
					gdr.GameChosen += gdr_GameChosen;

					gamesList.Controls.Add(gdr);
					flipColour = ! flipColour;
					offset += 20;
				}
				catch
				{
				}
			}
			loadSavedGame.ThePanel.Controls.Add(gamesList);
		}

		protected Button NewButton (ExpandablePanel p, string text, int x, int y, int w, int h)
		{
			Button tmpBtn = SkinningDefs.TheInstance.CreateWindowsButton();
			tmpBtn.Location = new Point(x, y);
			tmpBtn.Size = new Size(w, h);
			tmpBtn.Text = text;
			p.ThePanel.Controls.Add(tmpBtn);
			return tmpBtn;
		}

		protected Button NewButtonToGroupBox (Control p, string text, int x, int y, int w, int h)
		{
			Button tmpBtn = SkinningDefs.TheInstance.CreateWindowsButton();
			tmpBtn.Location = new Point(x, y);
			tmpBtn.Size = new Size(w, h);
			tmpBtn.Text = text;
			p.Controls.Add(tmpBtn);
			return tmpBtn;
		}

		void licensedGame_Click (object sender, EventArgs e)
		{
			if (CheckForDownload())
			{
				Tooltip.Visible = false;

				createGamePanel = new CreateGamePanel(productLicensor, productLicence, null);
				createGamePanel.CancelClicked += createGamePanel_CancelClicked;
				createGamePanel.OkClicked += createGamePanel_OkClicked;
				Controls.Add(createGamePanel);
				createGamePanel.BringToFront();
				createGamePanel.Select();
				createGamePanel.Focus();

				DoSize();
			}
		}

		void createGamePanel_CancelClicked (object sender, EventArgs args)
		{
			createGamePanel.Dispose();
		}

		void createGamePanel_OkClicked (object sender, EventArgs args)
		{
			try
			{
				using (var cursor = new WaitCursor (this))
				{
					IGameLicence gameLicence;

					if (createGamePanel.IsUnbillableMode)
					{
						gameLicence = productLicence.CreateUnofficialGameLicence(createGamePanel.GameDetails);
					}
					else
					{
						gameLicence = productLicence.CreateChargeableGameLicence(createGamePanel.GameDetails);
						gameLicence.Upload();
					}

					CreateAndLoadGame(gameLicence);

					createGamePanel.Dispose();
				}
			}
			catch (Exception e)
			{
				var message = "Unable to register game online.  Please ensure you are connected to the Internet and try again.  " + e.Message;

				MessageBox.Show(this, message, "Licensor Error");
			}
		}

		public void CreateAndLoadGame (IGameLicence gameLicence)
		{
			string leafName;
			string error;

			GameUtils.EstablishNewFileName(out leafName, gameLicence.GameDetails.Title, out error);
			string fullPath = LibCore.AppInfo.TheInstance.Location + @"\games\" + leafName;

			var networkFolder = LibCore.AppInfo.TheInstance.Location + @"\data\round1";
			var gameFile = CreateGameFile(fullPath, networkFolder, true, true, gameLicence);
			gameFile.SetDetails(gameLicence.GameDetails);

			var oldStyleLicense = OldGameLicense.CreateNewLicense(productLicence.UserDetails.UserName, fullPath);
			oldStyleLicense.Save(gameFile);

			if (productLicence.MustBeOnlineToPlay(gameLicence))
			{
				MessageBox.Show(this,
					"Because you are running the software in a Virtual Machine, please remember that you must connect to the internet to start each round.",
					"Licensor Reminder");
			}

			_gameLoader.LoadGame(gameFile, false);
		}

		protected virtual GameManagement.NetworkProgressionGameFile CreateGameFile (
			string filename, string directory, bool allowSave, bool allowWriteToDisk, IGameLicence licence)
		{
			return GameManagement.NetworkProgressionGameFile.CreateNew(filename, directory, allowSave, allowWriteToDisk,
				licence);
		}

		protected virtual GameManagement.NetworkProgressionGameFile OpenExistingGameFile (
			string filename, string roundOneFilesDir, bool allowSave, bool allowWriteToDisk)
		{
			return GameManagement.NetworkProgressionGameFile.OpenExisting(filename, roundOneFilesDir, allowSave,
				allowWriteToDisk);
		}

		void trainingGame_Click (object sender, EventArgs e)
		{
			if (CheckForDownload())
			{
				using (WaitCursor cursor = new WaitCursor(this))
				{
					string _fileName;
					string errMsg;

					GameManagement.GameUtils.EstablishNewFileName(out _fileName, "", out errMsg);
					string fileName = LibCore.AppInfo.TheInstance.Location + "\\games\\" + _fileName;
					string dir = LibCore.AppInfo.TheInstance.Location + "\\data\\round1";

					GameManagement.NetworkProgressionGameFile gameFile =
						CreateGameFile(fileName, dir, false, true, productLicence.CreateTrainingGameLicence(null));

					// Copy over the details.xml so that we have our details ready filled in.
					File.Copy(LibCore.AppInfo.TheInstance.Location + "\\data\\details.xml", gameFile.Dir + "\\global\\details.xml",
						true);

					_gameLoader.LoadGame(gameFile, true);
				}
			}
		}

		bool AreWeDownloading ()
		{
			return (versionPanel != null) && versionPanel.Downloading;
		}

		bool CheckForDownload ()
		{
			bool continueGame = true;
			if (AreWeDownloading())
			{
				Invoke(new MethodInvoker(delegate ()
				{
					if (DialogResult.Yes != MessageBox.Show(TopLevelControl,
						    "You are currently downloading the latest version of the software. Starting a game will cancel the download. \n\n Are you sure you wish to continue?",
						    "Warning",
						    MessageBoxButtons.YesNoCancel))
					{
						continueGame = false;
					}
				}));
			}

			return continueGame;
		}

		void salesGame_Click (object sender, EventArgs e)
		{
			bool continueGame = CheckForDownload();
			if (continueGame)
			{
				using (WaitCursor cursor = new WaitCursor(this))
				{
					string _fileName = NetworkProgressionGameFile.SalesGameFilename;

					string dir = LibCore.AppInfo.TheInstance.Location + "\\data\\round1";
					GameManagement.NetworkProgressionGameFile gameFile = OpenExistingGameFile(_fileName, dir, false, false);
					gameFile.SetSalesGameLicence();
					if (SkinningDefs.TheInstance.GetIntData("sales_round_1_ops_only", 0) == 1)
					{
						gameFile.SetCurrentRound(1, GameFile.GamePhase.OPERATIONS, false);
					}
					else
					{
						gameFile.SetCurrentRound(2, GameFile.GamePhase.OPERATIONS, false);
					}
					_gameLoader.LoadGame(gameFile, false);
				}
			}
		}

		void gdr_GameSelected (GameDetailRow sender)
		{
			gameToLoad = sender.FullFileName;
			UpdateGameButtons();
		}

		void gdr_GameChosen (GameDetailRow sender)
		{
			bool continueGame = CheckForDownload();
			if (continueGame)
			{
				using (WaitCursor cursor = new WaitCursor(this))
				{
					string dir = LibCore.AppInfo.TheInstance.Location + "\\data\\round1";
					GameManagement.NetworkProgressionGameFile gameFile = OpenExistingGameFile(sender.FullFileName, dir, true, true);

					_gameLoader.LoadGame(gameFile, false);
				}
			}
		}

		void loadGame_Click (object sender, EventArgs e)
		{
			bool continueGame = CheckForDownload();
			if (continueGame)
			{
				using (WaitCursor cursor = new WaitCursor(this))
				{
					string dir = LibCore.AppInfo.TheInstance.Location + "\\data\\round1";
					GameManagement.NetworkProgressionGameFile gameFile = OpenExistingGameFile(gameToLoad, dir, true, true);
					_gameLoader.LoadGame(gameFile, false);
				}
			}
		}

		void licensedGame_MouseEnter (object sender, EventArgs e)
		{
			Tooltip.Text = CoreUtils.SkinningDefs.TheInstance.GetData("licensed_game_text");
			Tooltip.Visible = true;
		}

		void licensedGame_MouseLeave (object sender, EventArgs e)
		{
			Tooltip.Text = "";
			Tooltip.Visible = false;
		}

		void unofficialGame_Click (object sender, EventArgs e)
		{
			if (CheckForDownload())
			{
				Tooltip.Visible = false;

				createGamePanel = new CreateGamePanel(productLicensor, productLicence, null, CreateGamePanelMode.Unbilled);
				createGamePanel.CancelClicked += createGamePanel_CancelClicked;
				createGamePanel.OkClicked += createGamePanel_OkClicked;
				Controls.Add(createGamePanel);
				createGamePanel.BringToFront();
				createGamePanel.Select();
				createGamePanel.Focus();

				DoSize();
			}
		}
		void unofficialGame_MouseEnter (object sender, EventArgs e)
		{
			Tooltip.Text = "(Only for Company Facilitators.)  Create a normal game which will not appear on any billing reports.";
			Tooltip.Visible = true;
		}

		void unofficialGame_MouseLeave (object sender, EventArgs e)
		{
			Tooltip.Text = "";
			Tooltip.Visible = false;
		}

		void trainingGame_MouseEnter (object sender, EventArgs e)
		{
			Tooltip.Text =
				"The Training Game is free to use for facilitator training and demonstration purposes. Use of the Training Game for any other purpose is strictly prohibited.";
			Tooltip.Visible = true;
		}

		void trainingGame_MouseLeave (object sender, EventArgs e)
		{
			Tooltip.Text = "";
			Tooltip.Visible = false;
		}

		void salesGame_MouseEnter (object sender, EventArgs e)
		{
			Tooltip.Text = CoreUtils.SkinningDefs.TheInstance.GetData("sales_game");
			Tooltip.Visible = true;
		}

		void salesGame_MouseLeave (object sender, EventArgs e)
		{
			Tooltip.Text = "";
			Tooltip.Visible = false;
		}

		protected virtual void docButton_Click (object sender, EventArgs e)
		{
			List<string> filenames = new List<string>();

			if (sender == facilitatorDocumentsButton)
			{
				filenames.Add(LibCore.AppInfo.TheInstance.Location + "\\data\\Reprints.zip");
				filenames.Add(LibCore.AppInfo.TheInstance.Location + "\\data\\Facilitator_Sheet.pdf");
			}
			else if (sender == swimLanesButton)
			{
				filenames.Add(LibCore.AppInfo.TheInstance.Location + "\\data\\SwimLanes_Sheet.pdf");
			}

			bool found = false;
			foreach (string filename in filenames)
			{
				if (File.Exists(filename))
				{
					try
					{
						System.Diagnostics.Process.Start(filename);
						found = true;
						break;
					}
					catch (Exception evc)
					{
						string error;

						if (evc.Message.IndexOf("No Application") > -1)
						{
							error = "No PDF Reader Application Installed";
						}
						else
						{
							error = "Failed to Start PDF Reader";
						}

						MessageBox.Show(TopLevelControl, "Cannot open documentation", error, MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			}

			if (! found)
			{
				MessageBox.Show(TopLevelControl, "Cannot open documentation", "File Not Found", MessageBoxButtons.OK,
					MessageBoxIcon.Error);
			}
		}


		void runGame ()
		{
			if (productLicence.CanCreateChargeableGame())
			{
				var gameDetails = new Licensor.GameDetails(null, null, null, null, null, null, null, null, null, 0);
				var licence = productLicence.CreateChargeableGameLicence(gameDetails);

			}
			else
			{
				List<CustomMessageBoxItem> items = new List<CustomMessageBoxItem>();

				CustomMessageBoxItem okItem = new CustomMessageBoxItem("OK", StringAlignment.Center);
				items.Add(okItem);

				using (CustomMessageBox box = new CustomMessageBox("Unable to create a Chargeable Game.",
					"Error Creating Game",
					items.ToArray()))
				{
					box.ShowDialog(TopLevelControl);
				}
			}
		}

		protected void JumpBuyButton_Click (object sender, EventArgs e)
		{
			DirectPuchaseWebSite = "";

			DirectPuchaseWebSite = SkinningDefs.TheInstance.GetData("dpw", DirectPuchaseWebSite);

			try
			{
				if (string.IsNullOrEmpty(DirectPuchaseWebSite) == false)
				{
					System.Diagnostics.Process.Start(DirectPuchaseWebSite);
				}
				else
				{
					MessageBox.Show(TopLevelControl, "No web site defined", "Jump to Web Site");
				}
			}
			catch (System.ComponentModel.Win32Exception noBrowser)
			{
				if (noBrowser.ErrorCode == -2147467259)
				{
					MessageBox.Show(TopLevelControl, "Unable to launch Web Browser. Please open site  " + DirectPuchaseWebSite,
						"Jump to Web Site");
				}
			}
			catch (System.Exception)
			{
				MessageBox.Show(TopLevelControl, "Unable to launch Web Browser. Please open site  " + DirectPuchaseWebSite,
					"Jump to Web Site");
			}
		}

		bool isInternetConnected ()
		{
			bool connected = false;

			InternetDetector id = new InternetDetector();
			connected = id.IsNetworkConnected();

			return connected;
		}

		protected void RefreshLicButton_Click (object sender, EventArgs e)
		{
			if (isInternetConnected() == false)
			{
				MessageBox.Show(TopLevelControl, "Internet was not detected, please reconnect", "Internet not available");
			}
			else
			{
				RefreshActivation();
			}
		}

		protected void resetButton_Click (object sender, EventArgs e)
		{
			if (isInternetConnected() == false)
			{
				MessageBox.Show(TopLevelControl, "Internet was not detected, please reconnect", "Internet not available");
			}
			else
			{
				resetEnterPass.Visible = true;
				resetPassword.Visible = true;
				resetPass_ok.Visible = true;
				resetEnterPass.BringToFront();
				resetPass_cancel.Visible = true;
				resetEnterPass.SendToBack();
				resetPass_cancel.BringToFront();
				resetPass_ok.BringToFront();

				versionPanel.ShowFull = false;

				resetPassword.Clear();
				resetPassword.Focus();
			}
		}

		protected void resetPass_ok_Click (object sender, EventArgs e)
		{
			if (isInternetConnected() == false)
			{
				MessageBox.Show(TopLevelControl, "Internet was not detected, please reconnect", "Internet not available");
			}
			else
			{
				if ((PasswordCheck != null) && PasswordCheck(resetPassword.Text))
				{
					ResetActivation(resetPassword.Text.Trim());
				}
				else
				{
					resetErrorMsg.Text = "Invalid Password";
					resetErrorMsg.Visible = true;
				}
			}
		}

		void ShowPasswordHelp ()
		{
			MessageBox.Show((Form) TopLevelControl,
				"Please check that you are typing your password correctly.\n\nIf problems persist, please quit and re-load the application.\n\nFor further help, contact support.",
				"Password Error",
				MessageBoxButtons.OK);
		}

		protected void resetPassword_KeyDown (object sender, System.Windows.Forms.KeyEventArgs e)
		{
			if (resetErrorMsg.Visible)
			{
				resetErrorMsg.Text = "";
				resetErrorMsg.Visible = false;
			}
		}

		protected void resetPass_cancel_Click (object sender, EventArgs e)
		{
			ClearResetEntry();
		}

		void ClearResetEntry ()
		{
			versionPanel.ShowFull = true;
			license.Panel.SuspendLayout();
			resetPassword.Text = "";
			resetPassword.Visible = false;
			resetPass_ok.Visible = false;
			resetPass_cancel.Visible = false;
			resetEnterPass.Visible = false;
			resetErrorMsg.Visible = false;
			license.Panel.ResumeLayout();
		}

		bool ZipGameFiles (string destination, bool suppressUi)
		{
			string gamePath = LibCore.AppInfo.TheInstance.Location + @"\games\";

			bool approvalToLockGames = suppressUi;

			try
			{
				ConfPack zipper = new ConfPack();

				List<string> gameFiles = new List<string>();

				string tempParentFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
				Directory.CreateDirectory(tempParentFolder);

				// Build a list of games to zip into the archive.
				foreach (string sourceGameFile in GetGames())
				{
					string copiedGameFileName = Path.Combine(tempParentFolder, Path.GetFileName(sourceGameFile));
					File.Copy(sourceGameFile, copiedGameFileName);

					using (var copiedGame = NetworkProgressionGameFile.OpenExisting(copiedGameFileName, null, true, true))
					{
						// Lock the gamefile so we can't use this system to duplicate still-playable games.
						if (! (copiedGame.Licence?.GetPhasePlayability(copiedGame.LastPhaseNumberPlayed + 1)?.IsPermanentlyUnplayable ??
						       true))
						{
							if (! approvalToLockGames)
							{
								if (MessageBox.Show(this,
									    "All exported game files will be locked. Further rounds cannot be played. \r\nDo you want to continue?",
									    "Export games",
									    MessageBoxButtons.YesNo)
								    != DialogResult.Yes)
								{
									return false;
								}

								approvalToLockGames = true;
							}

							copiedGame.Licence.MakeUnplayable();
						}
					}

					gameFiles.Add(copiedGameFileName);
				}

				// Zip the games.
				zipper.CreateZip(destination, gameFiles, "");

				Directory.Delete(tempParentFolder, true);

				if (! suppressUi)
				{
					MessageBox.Show(this,
						CONVERT.Format("{0} successfully exported.",
							Plurals.Format(gameFiles.Count, "game was", "games were")),
						"Export games",
						MessageBoxButtons.OK);
				}
			}
			catch
			{
				if (! suppressUi)
				{
					MessageBox.Show(TopLevelControl,
						"Error copying the saved game files to '" + Path.GetFileName(destination) + "' on the desktop!", "Export games");
				}
				return false;
			}

			return true;
		}

		bool IsGameLocked (string gameFile)
		{
			using (var game = NetworkProgressionGameFile.OpenExisting(gameFile, null, true, true))
			{
				return ! game.Licence.IsPlayable;
			}
		}

		void LockGame (string gameFile)
		{
			using (var game = NetworkProgressionGameFile.OpenExisting(gameFile, null, true, true))
			{
				game.Licence.MakeUnplayable();
			}
		}

		bool LockGames ()
		{
			bool error = false;
			foreach (string gameFile in GetGames())
			{
				try
				{
					LockGame(gameFile);
				}
				catch
				{
					error = true;
				}
			}

			return ! error;
		}

		string [] GetGames ()
		{
			return Directory.GetFiles(LibCore.AppInfo.TheInstance.Location + @"\games\", "*.gmz");
		}

		int CountGames ()
		{
			return GetGames().Length;
		}

		int CountUnlockedGames ()
		{
			int unlocked = 0;
			foreach (string gameFile in GetGames())
			{
				if (! IsGameLocked(gameFile))
				{
					unlocked++;
				}
			}

			return unlocked;
		}

		string GenerateUniqueFilenameForOldGames ()
		{
			string folder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.DesktopDirectory);
			string appName = LibCore.AppInfo.TheInstance.GetApplicationName().Replace(" ", "_");
			string date = DateTime.Now.ToString("yyyy-MM-dd");

			string filename;

			int serial = 1;
			bool foundUnique;

			do
			{
				if (serial == 1)
				{
					filename = CONVERT.Format(@"{0}\old_games_{1}_{2}.zip", folder, appName, date);
				}
				else
				{
					filename = CONVERT.Format(@"{0}\old_games_{1}_{2} ({3}).zip", folder, appName, date, serial);
				}

				foundUnique = ! File.Exists(filename);

				if (! foundUnique)
				{
					serial++;
				}
			} while (! foundUnique);

			return filename;
		}

		void ResetActivation (string password)
		{
			ClearResetEntry();

			if (MessageBox.Show(TopLevelControl, "Are you sure you want to reset your account?",
				    "License Reset",
				    MessageBoxButtons.OKCancel)
			    == DialogResult.OK)
			{
				int games;
				using (WaitCursor cursor = new WaitCursor(this))
				{
					games = CountGames();
				}

				string zipFilename = null;
				bool gamesZippedIfNecessary = true;
				if (games > 0)
				{
					gamesZippedIfNecessary = false;

					using (WaitCursor cursor = new WaitCursor(this))
					{
						zipFilename = GenerateUniqueFilenameForOldGames();
						gamesZippedIfNecessary = ZipGameFiles(zipFilename, true);
					}

					if (! gamesZippedIfNecessary)
					{
						if (MessageBox.Show(TopLevelControl,
							    "Your game files could not be successfully exported. Do you want to continue with your TAC Reset?",
							    "License Reset",
							    MessageBoxButtons.YesNo)
						    == DialogResult.Yes)
						{
							gamesZippedIfNecessary = true;
						}
					}
				}

				if (gamesZippedIfNecessary)
				{
					bool resetSuccessful;

					using (WaitCursor cursor = new WaitCursor(this))
					{
						resetSuccessful = _gameLoader.ResetActivation(password);
					}

					if (resetSuccessful)
					{
						StringBuilder builder = new StringBuilder();

						if (gamesZippedIfNecessary
						    && (games > 0))
						{
							builder.Append("Activation reset successful. \n\n");
							builder.Append("Your saved game files have been copied to the desktop.\n\n");
						}

						builder.Append("You can now activate the simulation on another machine.\n\n");

						MessageBox.Show(TopLevelControl, builder.ToString(),
							"Activation Reset Successful",
							MessageBoxButtons.OK);
						Invalidate();

						System.Environment.Exit(0);
					}
					else
					{
						MessageBox.Show(TopLevelControl, "Please ensure you are connected to the Internet and try again.",
							"Failed to Reset Activation",
							MessageBoxButtons.OK);
					}
				}
			}

			Invalidate();
		}

		void RefreshActivation ()
		{
			if (_gameLoader.RefreshActivation())
			{
				MessageBox.Show(TopLevelControl, "System Refresh was successful. You can now play your game. ",
					"System Refresh Successful", MessageBoxButtons.OK);
			}
		}

		void LicensedGamePanel_Leave (object sender, EventArgs e)
		{
		}

		protected void ResetEntryPanel_Leave (object sender, EventArgs e)
		{
			ClearResetEntry();
		}

		public virtual void QuickStartGame ()
		{
			var gameLicence = productLicence.CreateChargeableGameLicence(new Licensor.GameDetails("Test", "", "", "", "APJ", "ANTARCTICA", "", "Test game", "Other", 1));
			CreateAndLoadGame(gameLicence);
		}

		void importGames_Click (object sender, EventArgs args)
		{
			ImportGames();
		}

		void exportGames_Click (object sender, EventArgs args)
		{
			ExportGames();
		}

		void ImportGames ()
		{
			using (OpenFileDialog dialog = new OpenFileDialog())
			{
				dialog.Title = "Import archive of game files";
				dialog.InitialDirectory = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
				dialog.Filter = "Saved Game or Zip files (*.gmz, *.zip)|*.gmz;*.zip";

				if (dialog.ShowDialog(TopLevelControl) == DialogResult.OK)
				{
					ImportGameFiles(dialog.FileName);
				}
			}
		}

		void ExportGames ()
		{
			using (SaveFileDialog dialog = new SaveFileDialog())
			{
				dialog.Title = "Save archive of existing game files";
				dialog.InitialDirectory = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
				dialog.Filter = "Zip files (*.zip)|*.zip";
				dialog.FileName = CONVERT.Format("old_games_{0}.zip",
					LibCore.AppInfo.TheInstance.GetApplicationName().Replace(" ", "_"));
				if (dialog.ShowDialog(this) == DialogResult.OK)
				{
					using (WaitCursor cursor = new WaitCursor(this))
					{
						ZipGameFiles(dialog.FileName, false);
					}
				}
			}
		}

		bool IsFileAValidGameForUs (string gameFilename)
		{
			ConfPack zipper = new ConfPack();

			string tempFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
			Directory.CreateDirectory(tempFolder);

			bool isValid = false;

			try
			{
				zipper.ExtractAllFilesFromZip(gameFilename, tempFolder, "");
				isValid = IsUnzippedFileAValidGameForUs(tempFolder);
			}
			finally
			{
				Directory.Delete(tempFolder, true);
			}

			return isValid;
		}

		bool IsUnzippedFileAValidGameForUs (string unzippedFolder)
		{
			string detailsFilename = Path.Combine(unzippedFolder, @"global\details.xml");

			if (File.Exists(detailsFilename))
			{
				BasicXmlDocument xml = BasicXmlDocument.CreateFromFile(detailsFilename);
				XmlAttribute type = xml.DocumentElement.Attributes["type"];

				if ((type == null)
				    || (type.Value == SkinningDefs.TheInstance.GetData("gametype")))
				{
					return true;
				}
			}

			return false;
		}

		void ImportGameFiles (string filename)
		{
			using (WaitCursor cursor = new WaitCursor(this))
			{
				ConfPack zipper = new ConfPack();
				int validGames = 0;

				if (IsFileAValidGameForUs(filename))
				{
					using (var gameFile = NetworkProgressionGameFile.OpenExistingRespectingType(filename, null, true, true))
					{
						gameFile.Licence.MakeUnplayable();
						gameFile.Save(true);
					}
				}
				else
				{
					// Unzip the archive.
					foreach (string gameFilename in zipper.ExtractAllFilesFromZip(filename,
						LibCore.AppInfo.TheInstance.Location + @"\games\", ""))
					{
						if (IsFileAValidGameForUs(gameFilename))
						{
							using (var gameFile = NetworkProgressionGameFile.OpenExistingRespectingType(gameFilename, null, true, true))
							{
								gameFile.Licence?.MakeUnplayable();
								gameFile.Save(true);
								validGames++;
							}
						}
						else
						{
							File.Delete(gameFilename);
						}
					}
				}

				string message = CONVERT.Format("{0} been successfully imported.",
					Plurals.Format(validGames, "game has", "games have"));
				if (validGames == 0)
				{
					message = CONVERT.Format("There are no {0} games in the selected file.",
						LibCore.AppInfo.TheInstance.GetApplicationName());
				}

				MessageBox.Show(this, message, "Import games", MessageBoxButtons.OK);

				FillGamesList();
				UpdateGameButtons();
			}
		}

		public void SetLatestVersionInfo (VersionCheckResults args)
		{
			if ((args != null)
			    && args.Success)
			{
				versionPanel.SetInfo(args.UpToDate, args.Message, args.Url, args.Username, args.Password);
			}
			else
			{
				versionPanel.ShowError();
			}
		}

		public void ShowDevTools ()
		{
			var loadSpreadsheetButton = SkinningDefs.TheInstance.CreateWindowsButton(FontStyle.Bold);
			loadSpreadsheetButton.Text = "Load spreadsheet";
			loadSpreadsheetButton.Bounds = new Rectangle(gb_Chargeable.Left, gb_Chargeable.Bottom + 10, gb_Chargeable.Width, 20);
			loadSpreadsheetButton.BackColor = Color.Red;
			startNewGame.ThePanel.Controls.Add(loadSpreadsheetButton);
			loadSpreadsheetButton.Click += loadSpreadsheetButton_Click;

			var setDataFolderButton = SkinningDefs.TheInstance.CreateWindowsButton(FontStyle.Bold);
			setDataFolderButton.Text = "Set data export folder";
			setDataFolderButton.Bounds = new Rectangle(gb_NonChargeable.Left, loadSpreadsheetButton.Top, gb_NonChargeable.Width,
				loadSpreadsheetButton.Height);
			setDataFolderButton.BackColor = Color.Red;
			startNewGame.ThePanel.Controls.Add(setDataFolderButton);
			setDataFolderButton.Click += setDataFolderButton_Click;

			dataImportLabel = new Label();
			dataImportLabel.Bounds = new Rectangle(loadSpreadsheetButton.Left, loadSpreadsheetButton.Bottom + 3,
				setDataFolderButton.Right - loadSpreadsheetButton.Left, 30);
			dataImportLabel.ForeColor = Color.Red;
			dataImportLabel.Font = MyDefaultSkinFontBold9;
			startNewGame.ThePanel.Controls.Add(dataImportLabel);
			dataImportLabel.BringToFront();
		}

		void loadSpreadsheetButton_Click (object sender, EventArgs args)
		{
			GetSpreadsheetFilename();
		}

		void GetSpreadsheetFilename ()
		{
			using (var openDialog =
				new OpenFileDialog { Filter = "Spreadsheet files (*.xlsx)|*.xlsx", Title = "Load spreadsheet" })
			{
				if (! string.IsNullOrEmpty(PersistentGlobalOptions.SpreadsheetFilename))
				{
					openDialog.InitialDirectory = Path.GetDirectoryName(PersistentGlobalOptions.SpreadsheetFilename);
					openDialog.FileName = Path.GetFileName(PersistentGlobalOptions.SpreadsheetFilename);
				}

				if (openDialog.ShowDialog(TopLevelControl) == DialogResult.OK)
				{
					PersistentGlobalOptions.SpreadsheetFilename = openDialog.FileName;

					if (string.IsNullOrEmpty(PersistentGlobalOptions.DataExportFolder))
					{
						if (! GetDataFolder())
						{
							return;
						}
					}

					OnSpreadsheetLoading(PersistentGlobalOptions.SpreadsheetFilename, PersistentGlobalOptions.DataExportFolder);

					if (! string.IsNullOrEmpty(PersistentGlobalOptions.DataExportFolder))
					{
						dataImportLabel.Text = CONVERT.Format("Spreadsheet imported at {0}", DateTime.Now);
					}
				}
			}
		}

		public event SpreadsheetLoadingEventHandler SpreadsheetLoading;

		public delegate void SpreadsheetLoadingEventHandler (object sender, SpreadsheetLoadingEventArgs args);

		public class SpreadsheetLoadingEventArgs : EventArgs
		{
			public string SpreadsheetFilename;
			public string DataExportFolder;
		}

		protected virtual void OnSpreadsheetLoading (string spreadsheetFilename, string dataExportFolder)
		{
			if (SpreadsheetLoading != null)
			{
				SpreadsheetLoading(this,
					new SpreadsheetLoadingEventArgs
					{
						SpreadsheetFilename = spreadsheetFilename,
						DataExportFolder = dataExportFolder
					});
				DirectoryCopier.Copy(dataExportFolder, LibCore.AppInfo.TheInstance.Location + @"\data");
			}
		}

		void setDataFolderButton_Click (object sender, EventArgs args)
		{
			GetDataFolder();
		}

		bool GetDataFolder ()
		{
			using (var openDialog = new FolderBrowserDialog
			{
				Description = "Select data folder for Subversion",
				SelectedPath = PersistentGlobalOptions.DataExportFolder
			})
			{
				if (openDialog.ShowDialog(TopLevelControl) == DialogResult.OK)
				{
					PersistentGlobalOptions.DataExportFolder = openDialog.SelectedPath;
					return true;
				}
			}

			return false;
		}

		void emergencyButton_Click (object sender, EventArgs args)
		{
			if (CheckForDownload())
			{
				Tooltip.Visible = false;

				createGamePanel = new CreateGamePanel(productLicensor, productLicence, null);
				createGamePanel.CancelClicked += createGamePanel_CancelClicked;
				Controls.Add(createGamePanel);
				createGamePanel.BringToFront();
				createGamePanel.Select();
				createGamePanel.Focus();

				DoSize();
			}
		}
	}
}