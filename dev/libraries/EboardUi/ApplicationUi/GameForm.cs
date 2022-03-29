using CoreScreens;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Algorithms;
using CoreUtils;
using GameManagement;
using LibCore;
using Licensor;
using Media;

namespace ApplicationUi
{
	public class GameForm : Form
	{
		BaseCompleteGamePanel mainPanel;

		protected IProductLicence productLicence;

		public BaseCompleteGamePanel MainPanel
		{
			get => mainPanel;

			set
			{
				mainPanel = value;
			}
		}

		public GameForm (IProductLicence productLicence)
		{
			this.productLicence = productLicence;
			productLicence.RefreshStatusComplete += productLicence_RefreshStatusComplete;

			ControlBox = false;
			Icon = Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetEntryAssembly().Location);
			Text = BaseUtils.AssemblyExtensions.MainAssemblyTitle;
			FormBorderStyle = FormBorderStyle.None;

			MinimumSize = new Size (1024, 768);
			StartPosition = FormStartPosition.Manual;
			Size = new Size (Screen.PrimaryScreen.Bounds.Width, Math.Min(768, Screen.PrimaryScreen.Bounds.Height));
			Location = Screen.PrimaryScreen.Bounds.Location;

			SoundPlayer.SetContainer(this, false);

			WindowDraggingExtensions.EnableDragging();
		}

		protected override CreateParams CreateParams
		{
			get
			{
				CreateParams createParams = base.CreateParams;

				createParams.Style &= ~0x00C00000; // remove WS_CAPTION
				createParams.Style |= 0x00040000;  // include WS_SIZEBOX

				return createParams;
			}
		}

		protected override void Dispose (bool disposing)
		{
			productLicence.RefreshStatusComplete -= productLicence_RefreshStatusComplete;
			mainPanel.Dispose();
			base.Dispose(disposing);
		}

		protected bool mainPanel_PasswordCheck (string password)
		{
			return productLicence.VerifyPassword(password);
		}

		void productLicence_RefreshStatusComplete (object sender, EventArgs args)
		{
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);

			if (WindowState != FormWindowState.Minimized)
			{
				DoSize();
			}
		}

		protected virtual void DoSize ()
		{
			if (mainPanel != null)
			{
				mainPanel.Size = ClientSize;
			}
		}

		protected override void OnMouseDown (MouseEventArgs e)
		{
			base.OnMouseDown(e);

			if (e.Button == MouseButtons.Left)
			{
				((Form) TopLevelControl).DragMove();
			}
		}

		public void RunRace (int round, bool rewind)
		{
			mainPanel.RunRace(round, rewind, false);
		}

		public void RunTransition (int round, bool rewind)
		{
			mainPanel.RunTransition(round, rewind);
		}

		public void ImportIncidents (string incidentsFile)
		{
			mainPanel.ImportIncidents(incidentsFile);
		}

		public void ExportIncidents ()
		{
			mainPanel.ExportIncidents();
		}

		public void QuickStartGame ()
		{
			mainPanel.QuickStartGame();
		}

		public void ExportAllRoundIncidents ()
		{
			mainPanel.ExportAllRoundIncidents();
		}

		public void ImportAllRoundIncidents ()
		{
			mainPanel.ImportAllRoundIncidents();
		}

		public void ExportAnalytics ()
		{
			mainPanel.ExportAnalytics();
		}

		static T Max<T> (params T [] args)
		{
			return args.Max();
		}

		public void RunScript (IList<string> arguments)
		{
			try
			{
				MediaPanel.SuppressFileErrors = true;

				var gameLicence = productLicence.CreateChargeableGameLicence(new Licensor.GameDetails("Scripted game", "", "", "", "APJ", "ANTARCTICA", "", "Scripted game", "Other", 1));
				mainPanel.CreateAndLoadGame(gameLicence);
				mainPanel.IsUserInteractionDisabled = true;

				string copyFilename = null;
				string reportsFolder = null;
				string screenGrabsFolder = null;

				var incidentsFilenames = new List<string>();
				var costsFilenames = new List<string>();
				var maturityFilenames = new List<string>();
				var isoMaturityFilenames = new List<string>();
				var leanMaturityFilenames = new List<string>();
				var pathfinderFilenames = new List<string>();
				var npsSurveyFilenames = new List<string>();

				var unlockGame = false;

				var scriptArgs = new List<string>(arguments);

				var useFilenameReferencePath = false;
				string filenameReferencePath = null;

				Console.WriteLine($"Executing command line '{string.Join(" ", arguments)}'");

				var index = 0;
				while (index < scriptArgs.Count)
				{
					var flag = scriptArgs[index];

					if (flag.StartsWith("-"))
					{
						var firstArgumentIndex = index + 1;
						var nextFlagIndex = firstArgumentIndex + 1;
						while ((nextFlagIndex < scriptArgs.Count) && (! scriptArgs[nextFlagIndex].StartsWith("-")))
						{
							nextFlagIndex++;
						}

						var lastArgumentIndex = nextFlagIndex - 1;
						index = nextFlagIndex;

						Console.WriteLine($"Parsing command line flag '{flag}'");

						switch (flag.ToLower())
						{
							case "-paths-are-relative-to-command-file":
								useFilenameReferencePath = true;
								index = firstArgumentIndex;
								break;

							case "-commands":
							{
								var commandFile = GetOneArgument(flag, scriptArgs, firstArgumentIndex, lastArgumentIndex);
								filenameReferencePath = Path.GetDirectoryName(commandFile);

								scriptArgs.AddRange(File.ReadAllText(commandFile).SplitOnWhitespace(true));
								break;
							}

							case "-filename":
								copyFilename = MapFilename(filenameReferencePath, useFilenameReferencePath,
									GetOneArgument(flag, scriptArgs, firstArgumentIndex, lastArgumentIndex));
								break;

							case "-fully-unlock-game":
								unlockGame = true;
								break;

							case "-reportsfolder":
								reportsFolder = MapFilename(filenameReferencePath, useFilenameReferencePath,
									GetOneArgument(flag, scriptArgs, firstArgumentIndex, lastArgumentIndex));
								break;

							case "-screengrabsfolder":
								screenGrabsFolder = MapFilename(filenameReferencePath, useFilenameReferencePath,
									GetOneArgument(flag, scriptArgs, firstArgumentIndex, lastArgumentIndex));
								break;

							case "-details":
								mainPanel.LoadGameDetails(MapFilename(filenameReferencePath, useFilenameReferencePath,
									GetOneArgument(flag, scriptArgs, firstArgumentIndex, lastArgumentIndex)));
								break;

							case "-team":
								mainPanel.LoadTeamDetails(MapFilename(filenameReferencePath, useFilenameReferencePath,
									GetOneArgument(flag, scriptArgs, firstArgumentIndex, lastArgumentIndex)));
								break;

							case "-facilitator-logo":
								mainPanel.LoadFacilitatorLogo(MapFilename(filenameReferencePath, useFilenameReferencePath,
									GetOneArgument(flag, scriptArgs, firstArgumentIndex, lastArgumentIndex)));
								break;

							case "-client-logo":
								mainPanel.LoadClientLogo(MapFilename(filenameReferencePath, useFilenameReferencePath,
									GetOneArgument(flag, scriptArgs, firstArgumentIndex, lastArgumentIndex)));
								break;

							case "-maturity-options":
								File.Copy(
									MapFilename(filenameReferencePath, useFilenameReferencePath,
										GetOneArgument(flag, scriptArgs, firstArgumentIndex, lastArgumentIndex)),
									mainPanel.TheGameFile.GetGlobalFile("Eval_States.xml"), true);
								break;

							case "-incidents-files":
								incidentsFilenames.AddRange(GetArguments(flag, scriptArgs, firstArgumentIndex, lastArgumentIndex)
									.Select(a => MapFilename(filenameReferencePath, useFilenameReferencePath, a)));
								break;

							case "-costs-files":
								costsFilenames.AddRange(GetArguments(flag, scriptArgs, firstArgumentIndex, lastArgumentIndex)
									.Select(a => MapFilename(filenameReferencePath, useFilenameReferencePath, a)));
								break;

							case "-maturity-files":
								maturityFilenames.AddRange(GetArguments(flag, scriptArgs, firstArgumentIndex, lastArgumentIndex)
									.Select(a => MapFilename(filenameReferencePath, useFilenameReferencePath, a)));
								break;

							case "-iso-maturity-files":
								isoMaturityFilenames.AddRange(GetArguments(flag, scriptArgs, firstArgumentIndex, lastArgumentIndex)
									.Select(a => MapFilename(filenameReferencePath, useFilenameReferencePath, a)));
								break;

							case "-lean-maturity-files":
								leanMaturityFilenames.AddRange(GetArguments(flag, scriptArgs, firstArgumentIndex, lastArgumentIndex)
									.Select(a => MapFilename(filenameReferencePath, useFilenameReferencePath, a)));
								break;

							case "-pathfinder-files":
								pathfinderFilenames.AddRange(GetArguments(flag, scriptArgs, firstArgumentIndex, lastArgumentIndex)
									.Select(a => MapFilename(filenameReferencePath, useFilenameReferencePath, a)));
								break;

							case "-nps-files":
								npsSurveyFilenames.AddRange(GetArguments(flag, scriptArgs, firstArgumentIndex, lastArgumentIndex)
									.Select(a => MapFilename(filenameReferencePath, useFilenameReferencePath, a)));
								break;

							default:
								throw new Exception($"Invalid command line argument {flag}");
						}
					}
					else
					{
						throw new Exception($"Command line argument {flag} not understood");
					}
				}

				if (unlockGame)
				{
					mainPanel.TheGameFile.Licence.FullyUnlock();
				}

				for (int round = 0; round < costsFilenames.Count; round++)
				{
					File.Copy(costsFilenames[round], mainPanel.TheGameFile.GetGlobalFile($"costs_r{round + 1}.xml"), true);
				}

				for (int phase = 0; phase < incidentsFilenames.Count; phase++)
				{
					int round;
					GameFile.GamePhase gamePhase;

					mainPanel.TheGameFile.PhaseToRound(phase, out round, out gamePhase);

					Console.WriteLine($"Playing round {round} {gamePhase}");
					mainPanel.TheGameFile.SetCurrentRound(round, gamePhase);
					Console.WriteLine($"Now gamefile is in round {mainPanel.TheGameFile.CurrentRound} {mainPanel.TheGameFile.CurrentPhase}");

					if ((mainPanel.TheGameFile.CurrentRound != round)
						|| (mainPanel.TheGameFile.CurrentPhase != gamePhase))
					{
						Console.Error.WriteLine($"Error: gamefile actually thinks we're in round {mainPanel.TheGameFile.CurrentRound} {mainPanel.TheGameFile.CurrentPhase}");
						return;
					}

					mainPanel.PlayCurrentPhase();

					if (gamePhase == GameFile.GamePhase.OPERATIONS)
					{
						if (round < maturityFilenames.Count)
						{
							File.Copy(maturityFilenames[round], mainPanel.TheGameFile.GetRoundFile(round, "eval_wizard.xml", gamePhase),
								true);
						}

						if (round < isoMaturityFilenames.Count)
						{
							File.Copy(isoMaturityFilenames[round],
								mainPanel.TheGameFile.GetRoundFile(round, "eval_wizard_iso.xml", gamePhase), true);
						}

						if (round < leanMaturityFilenames.Count)
						{
							File.Copy(leanMaturityFilenames[round],
								mainPanel.TheGameFile.GetRoundFile(round, "eval_wizard_lean.xml", gamePhase), true);
						}

						if (round < pathfinderFilenames.Count)
						{
							File.Copy(pathfinderFilenames[round],
								mainPanel.TheGameFile.GetRoundFile(round, "pathfinder_survey_wizard.xml", gamePhase), true);
						}

						if (round < npsSurveyFilenames.Count)
						{
							File.Copy(npsSurveyFilenames[round],
								mainPanel.TheGameFile.GetRoundFile(round, "nps_survey_wizard.xml", gamePhase), true);
						}
					}

					if (phase < incidentsFilenames.Count)
					{
						mainPanel.ImportIncidents(incidentsFilenames[phase]);
					}

					mainPanel.RunToEndOfPhase();
					Console.WriteLine($"Phase finished, now gamefile is in round {mainPanel.TheGameFile.CurrentRound} {mainPanel.TheGameFile.CurrentPhase}");
					Console.WriteLine();
				}

				if (reportsFolder != null)
				{
					GlobalAccess.EnsurePathExists(reportsFolder);
					mainPanel.GenerateAllReports(reportsFolder);
				}

				if (screenGrabsFolder != null)
				{
					GlobalAccess.EnsurePathExists(screenGrabsFolder);
					mainPanel.ScreenGrabAllReports(screenGrabsFolder);
				}

				if (copyFilename != null)
				{
					mainPanel.CopyGameFile(copyFilename);
				}

				Console.WriteLine("Script finished");
			}
			catch (Exception e)
			{
				Console.Error.WriteLine(e);
			}
		}

		string MapFilename (string filenameReferencePath, bool useFilenameReferencePath, string filename)
		{
			if (useFilenameReferencePath)
			{
				return Path.Combine(filenameReferencePath, filename);
			}
			else
			{
				return filename;
			}
		}

		string GetOneArgument (string flag, IList<string> arguments, int firstArgumentIndex, int lastArgumentIndex)
		{
			int argumentCount = lastArgumentIndex + 1 - firstArgumentIndex;
			if (argumentCount != 1)
			{
				throw new Exception ($"Expected 1 argument for flag {flag}, got {argumentCount}");
			}

			return arguments[firstArgumentIndex];
		}

		List<string> GetArguments (string flag, IList<string> arguments, int firstArgumentIndex, int lastArgumentIndex)
		{
			return arguments.Skip(firstArgumentIndex).Take(lastArgumentIndex + 1 - firstArgumentIndex).ToList();
		}

		public void CreateAndLoadGame (IGameLicence gameLicence)
		{
			mainPanel.CreateAndLoadGame(gameLicence);
		}
	}
}