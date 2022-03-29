using System;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Drawing;
using System.Linq;
using LibCore;
using Network;
using CoreUtils;
using Licensor;
using Logging;
using Parsing;

namespace GameManagement
{
	public enum CustomContentSource
	{
		None,
		GameFile
	}

	/// <summary>
	/// A NetworkProgressionGameFile is used for a game whereby the network is improved
	/// upon in each round and the network for round N is the state of the network at the
	/// end of round N-1.
	/// 
	/// Rewinding a round forces it back to its initial state.
	/// </summary>
	public class NetworkProgressionGameFile : GameFile, ITimedClass
	{
		protected int _version = 2;

		static IProductLicence productLicence;

		public static void SetProductLicence (IProductLicence productLicence)
		{
			NetworkProgressionGameFile.productLicence = productLicence;
		}

		Dictionary<int, Dictionary<GamePhase, NodeTree>> roundToPhaseToModel;

		public int Version
		{
			get { return _version; }
		}
		/// <summary>
		/// The current model for the current phase and round.
		/// </summary>
		protected NodeTree _NetworkModel;
		/// <summary>
		/// Accessor to retrieve the current model being used/played.
		/// </summary>
		/// <summary>
		/// 
		/// </summary>
		public NodeTree NetworkModel
		{
			get { return _NetworkModel; }
		}

		/// <summary>
		/// Creates a new game file.
		/// </summary>
		/// <param name="filename"></param>
		/// <param name="roundOneFilesDir"></param>
		/// <param name="isNew"></param>
		/// <param name="allowSave"></param>
		/// <param name="allowWriteToDisk"></param>
		/// <param name="selectedContentOption"></param>
		protected NetworkProgressionGameFile (string filename, string roundOneFilesDir, bool isNew,
			bool allowSave, bool allowWriteToDisk, IGameLicence licence)
			: this(filename, false, roundOneFilesDir, isNew, allowSave, allowWriteToDisk, licence)
		{
		}

		protected NetworkProgressionGameFile (string filename, bool isOnlyForDeterminingType, string roundOneFilesDir, bool isNew,
			bool allowSave, bool allowWriteToDisk, IGameLicence licence)
			: base(filename, roundOneFilesDir, isNew, allowSave, allowWriteToDisk, licence)
		{
			roundToPhaseToModel = new Dictionary<int, Dictionary<GamePhase, NodeTree>> ();

			if (!isOnlyForDeterminingType)
			{
				currentRound = FirstRoundNumber - 1;

				if (isNew)
				{
					SetCurrentRound(FirstRoundNumber, GamePhase.OPERATIONS);
				}
				else
				{
					RestoreCurrentPhase();
				}
			}
		}

		public virtual int FirstRoundNumber
		{
			get
			{
				return 1;
			}
		}

		/// <summary>
		/// Brand new game...
		/// </summary>
		/// <param name="filename"></param>
		/// <param name="roundOneFilesDir"></param>
		/// <param name="allowSave"></param>
		/// <param name="allowWriteToDisk"></param>
		/// <param name="selectedContentOption"></param>
		/// <returns></returns>
		public static NetworkProgressionGameFile CreateNew (string filename, string roundOneFilesDir, bool allowSave,
			bool allowWriteToDisk, IGameLicence licence)
		{
			NetworkProgressionGameFile npg = new NetworkProgressionGameFile(filename, roundOneFilesDir, true, allowSave, allowWriteToDisk, licence);
			if (allowWriteToDisk)
			{
				npg.CopyInitialMaturityFiles();

				// Create a new version ID in the file...
				StreamWriter fs = File.CreateText(npg.Dir + "\\global\\version");
				fs.WriteLine("2");
				fs.Close();
				//
				File.Copy(AppInfo.TheInstance.Location + "\\data\\costs.xml", npg.Dir + "\\global\\costs_r1.xml", true);
				File.Copy(AppInfo.TheInstance.Location + "\\data\\costs.xml", npg.Dir + "\\global\\costs_r2.xml", true);
				File.Copy(AppInfo.TheInstance.Location + "\\data\\costs.xml", npg.Dir + "\\global\\costs_r3.xml", true);
				File.Copy(AppInfo.TheInstance.Location + "\\data\\costs.xml", npg.Dir + "\\global\\costs_r4.xml", true);
				File.Copy(AppInfo.TheInstance.Location + "\\data\\costs.xml", npg.Dir + "\\global\\costs_r5.xml", true);
			}
			//
			npg.SetCurrentPhase(0);
			//
			return npg;
		}

		public static NetworkProgressionGameFile CreateNew_Cloud (string filename, string roundOneFilesDir,
			bool allowSave, bool allowWriteToDisk, IGameLicence licence)
		{
			NetworkProgressionGameFile npg = new Cloud_NetworkProgressionGameFile(filename, roundOneFilesDir, true, allowSave, allowWriteToDisk, licence);
			if (allowWriteToDisk)
			{
				npg.CopyInitialMaturityFiles();

				// Create a new version ID in the file...
				StreamWriter fs = File.CreateText(npg.Dir + "\\global\\version");
				fs.WriteLine("2");
				fs.Close();
			}
			npg.SetCurrentPhase(0);
			return npg;
		}
		

		/// <summary>
		/// 
		/// </summary>
		/// <param name="filename"></param>
		/// <param name="roundOneFilesDir"></param>
		/// <param name="allowSave"></param>
		/// <param name="allowWriteToDisk"></param>
		/// <param name="selectedContentOption"></param>
		/// <returns></returns>
		public static NetworkProgressionGameFile CreateNewPM (string filename, string roundOneFilesDir,
			bool allowSave, bool allowWriteToDisk, IGameLicence licence)
		{
			NetworkProgressionGameFile npg = new PMNetworkProgressionGameFile(filename, roundOneFilesDir, true, allowSave, allowWriteToDisk, licence);
			if (allowWriteToDisk)
			{
				npg.CopyInitialMaturityFiles();

				// Create a new version ID in the file...
				StreamWriter fs = File.CreateText(npg.Dir + "\\global\\version");
				fs.WriteLine("2");
				fs.Close();
				//
				File.Copy(AppInfo.TheInstance.Location + "\\data\\costs.xml", npg.Dir + "\\global\\costs_r1.xml", true);
				File.Copy(AppInfo.TheInstance.Location + "\\data\\costs.xml", npg.Dir + "\\global\\costs_r2.xml", true);
				File.Copy(AppInfo.TheInstance.Location + "\\data\\costs.xml", npg.Dir + "\\global\\costs_r3.xml", true);
				File.Copy(AppInfo.TheInstance.Location + "\\data\\costs.xml", npg.Dir + "\\global\\costs_r4.xml", true);
				File.Copy(AppInfo.TheInstance.Location + "\\data\\costs.xml", npg.Dir + "\\global\\costs_r5.xml", true);
			}
			//
			npg.SetCurrentPhase(0);
			//
			return npg;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="filename"></param>
		/// <param name="roundOneFilesDir"></param>
		/// <param name="allowSave"></param>
		/// <param name="allowWriteToDisk"></param>
		/// <param name="ContentOption"></param>
		/// <returns></returns>
		public static NetworkProgressionGameFile OpenExisting (string filename, string roundOneFilesDir,
			bool allowSave, bool allowWriteToDisk)
		{
			NetworkProgressionGameFile npg = new NetworkProgressionGameFile(filename, roundOneFilesDir, false,
				allowSave, allowWriteToDisk, null);

			npg.LoadLicence();
			npg.RestoreCurrentPhase();

			if (File.Exists(npg.Dir + "\\global\\version"))
			{
				StreamReader fs = File.OpenText(npg.Dir + "\\global\\version");
				string v = fs.ReadLine();
				npg._version = CONVERT.ParseInt(v);
				fs.Close();
			}
			return npg;
		}

		public void RestoreCurrentPhase ()
		{
			if (File.Exists(PhaseLogFilename))
			{
				var xml = BasicXmlDocument.CreateFromFile(PhaseLogFilename);

				phaseNumbersPlayed = new List<int> ();
				foreach (XmlElement child in xml.DocumentElement.SelectNodes("PhasesPlayed/PhasePlayed"))
				{
					phaseNumbersPlayed.Add(child.GetIntAttribute("number").Value);
				}

				SetCurrentPhase(xml.DocumentElement.GetIntAttribute("current_phase").Value);
			}
			else
			{
				phaseNumbersPlayed = new List<int> ();
				for (int phase = 0; Directory.Exists(GetPhasePath(phase)); phase++)
				{
					phaseNumbersPlayed.Add(phase);
				}

				if (phaseNumbersPlayed.Any())
				{
					SetCurrentPhase(phaseNumbersPlayed.Max());
				}
			}
		}

		static NetworkProgressionGameFile OpenExistingOnlyForDeterminingType (string filename, string roundOneFilesDir,
			bool allowSave, bool allowWriteToDisk)
		{
			NetworkProgressionGameFile npg = new NetworkProgressionGameFile(filename, true, roundOneFilesDir, false,
				allowSave, allowWriteToDisk, null);

			if (File.Exists(npg.Dir + "\\global\\version"))
			{
				StreamReader fs = File.OpenText(npg.Dir + "\\global\\version");
				string v = fs.ReadLine();
				npg._version = CONVERT.ParseInt(v);
				fs.Close();
			}
			return npg;
		}

		public static Cloud_NetworkProgressionGameFile OpenExisting_Cloud (string filename, string roundOneFilesDir,
			bool allowSave, bool allowWriteToDisk)
		{
			Cloud_NetworkProgressionGameFile npg = new Cloud_NetworkProgressionGameFile(filename, roundOneFilesDir, false,
				allowSave, allowWriteToDisk, null);

			npg.LoadLicence();
			npg.RestoreCurrentPhase();

			if (File.Exists(npg.Dir + "\\global\\version"))
			{
				StreamReader fs = File.OpenText(npg.Dir + "\\global\\version");
				string v = fs.ReadLine();
				npg._version = CONVERT.ParseInt(v);
				fs.Close();
			}
			return npg;
		}

		public static DevOpsNetworkProgressionGameFile OpenExistingDevOps (string filename, string roundOneFilesDir,
			bool allowSave, bool allowWriteToDisk)
		{
			DevOpsNetworkProgressionGameFile npg = new DevOpsNetworkProgressionGameFile(filename, roundOneFilesDir, false,
				allowSave, allowWriteToDisk, null);

			npg.LoadLicence();
			npg.RestoreCurrentPhase();

			if (File.Exists(npg.Dir + "\\global\\version"))
			{
				StreamReader fs = File.OpenText(npg.Dir + "\\global\\version");
				string v = fs.ReadLine();
				npg._version = CONVERT.ParseInt(v);
				fs.Close();
			}
			return npg;
		}


		public static PMNetworkProgressionGameFile OpenExistingPM (string filename, string roundOneFilesDir,
			bool allowSave, bool allowWriteToDisk)
		{
			PMNetworkProgressionGameFile npg = new PMNetworkProgressionGameFile(filename, roundOneFilesDir, false,
				allowSave, allowWriteToDisk, null);

			npg.LoadLicence();
			npg.RestoreCurrentPhase();

			if (File.Exists(npg.Dir + "\\global\\version"))
			{
				StreamReader fs = File.OpenText(npg.Dir + "\\global\\version");
				string v = fs.ReadLine();
				npg._version = CONVERT.ParseInt(v);
				fs.Close();
			}
			return npg;
		}

		public static AOSE_NetworkProgressionGameFile OpenExistingAOSE (string filename, string roundOneFilesDir,
			bool allowSave, bool allowWriteToDisk)
		{
			AOSE_NetworkProgressionGameFile npg = new AOSE_NetworkProgressionGameFile(filename, roundOneFilesDir, false,
				allowSave, allowWriteToDisk, null);

			npg.LoadLicence();
			npg.RestoreCurrentPhase();

			if (File.Exists(npg.Dir + "\\global\\version"))
			{
				StreamReader fs = File.OpenText(npg.Dir + "\\global\\version");
				string v = fs.ReadLine();
				npg._version = CONVERT.ParseInt(v);
				fs.Close();
			}
			return npg;
		}

		/// <summary>
		/// 
		/// </summary>
		public override void Dispose ()
		{
			if (_NetworkModel != null)
			{
				_NetworkModel.Dispose();
			}
			_NetworkModel = null;
			//TimeManager.TheInstance.UnmanageClass(this);
			base.Dispose();
		}

		/// <summary>
		/// Copies the model from the previous round so the next round 
		/// (used when a round is started or rewound).
		/// </summary>
		/// <param name="round">The round to copy the previous model to.</param>
		/// <param name="phase">The phase to copy the model to.</param>
		protected virtual void CopyPreviousNetwork (int round, GamePhase phase)
		{
		    if (round < 0)
		    {
		        throw new Exception();
		    }

			// : Dodgy hack for bug 4779 (mysterious file exceptions here).
			int tries = 0;
			bool success = false;

			while ((!success) && (tries < 5))
			{
				tries++;

				try
				{
					if (_allowWriteToDisk)
					{
						// This by definition has to be a clean start so wipe the current
						// directory and copy over again.
						// TODO : Log that this is being done!
						// Round One Operations Phase is different. We have to build it from the original.
						if ((1 == round) && (phase == GamePhase.OPERATIONS))
						{
							string newDir = GetPhasePath(1, phase);
							string newGameDataDir = tempDirName + "\\global";
							//try
							//{
							if (Directory.Exists(newDir))
							{
								Directory.Delete(newDir, true);
							}

							//Copy the previous round files into the new directory
							CopyDirContents(theRoundOneFilesDir, newDir);

							//Overwrite with Required Wizard files
							CopyInitialMaturityFiles();
							OverwriteAttributesFromCostsFiles();
						}
						else
						{
							string newDir = GetPhasePath(round, phase);
							string newGameDataDir = tempDirName + "\\global";

							if (Directory.Exists(newDir))
							{
								Directory.Delete(newDir, true);
							}
							//
							Directory.CreateDirectory(newDir);
							//
							// If our current phase is operations then we step back to the transition phase network.
							//
							string pathname = "";
							string filename = "";

							if (GameTypeUsesTransitions())
							{
								if (phase == GamePhase.OPERATIONS)
								{
									pathname = GetPhasePath(round, GamePhase.TRANSITION);
									filename = pathname + "Network.xml";
									if (!File.Exists(filename))
									{
										CopyPreviousNetwork(round, GamePhase.TRANSITION);
									}

									CopyInitialMaturityFiles(round);

									string oldAssessmentsName = tempDirName + @"\round" + CONVERT.ToStr(round) +
																@"_A_transition\assessment_selection.xml";
									if (File.Exists(oldAssessmentsName))
									{
										File.Copy(oldAssessmentsName,
											tempDirName + @"\round" + CONVERT.ToStr(round) + @"_B_operations\assessment_selection.xml", true);
									}
								}
								else
								{
									// We are in the transition phase so we grab the network from the previous round's ops phase.
									int prevRound = round - 1;
									filename =
										GetPhasePath(prevRound, (phase == GamePhase.OPERATIONS) ? GamePhase.TRANSITION : GamePhase.OPERATIONS) +
										"Network.xml";

									if (!File.Exists(filename))
									{
										// Copy round from previous...
										CopyPreviousNetwork(round - 1, GamePhase.OPERATIONS);
									}

									// Also copy the upcoming assessment requests from the previous ops phase.
									string oldAssessmentsName = tempDirName + @"\round" + CONVERT.ToStr(prevRound) +
																@"_B_operations\assessment_selection_for_next_round.xml";
									if (File.Exists(oldAssessmentsName))
									{
										File.Copy(oldAssessmentsName,
											tempDirName + @"\round" + CONVERT.ToStr(round) + @"_A_transition\assessment_selection.xml", true);
									}
								}
							}
							else
							{
								pathname = GetPhasePath(round - 1, GamePhase.OPERATIONS);
								filename = pathname + "Network.xml";
								if (!File.Exists(filename))
								{
									CopyPreviousNetwork(round - 1, GamePhase.OPERATIONS);
								}

								CopyInitialMaturityFiles(round);
							}
							File.Copy(filename, newDir + "\\Network.xml", true);
						}
					}

					success = true;
				}
				catch (Exception e)
				{
					AppLogger.TheInstance.WriteLine("Exception in CopyPreviousNetwork:");
					AppLogger.TheInstance.WriteLine(e.ToString());
					if (e.InnerException != null)
					{
						AppLogger.TheInstance.WriteLine(e.InnerException.ToString());
					}

					// Wait a bit before retrying.
					System.Threading.Thread.Sleep(2000);
				}
			}
		}

		void OverwriteAttributesFromCostsFiles ()
		{
			var costsFilename = GetGlobalFile("costs_r1.xml");
			if (File.Exists(costsFilename))
			{
				var modelFilename = GetNetworkFile(1, GamePhase.OPERATIONS);
				var model = new NodeTree(File.ReadAllText(modelFilename));

				var xml = BasicXmlDocument.CreateFromFile(costsFilename);
				foreach (XmlElement node in xml.DocumentElement.ChildNodes)
				{
					var cost = node.GetAttribute("cost");
					if (! string.IsNullOrEmpty(cost))
					{
						var modelName = node.GetStringAttribute("model_value", null)?.Replace("{round}", "1");
						if (modelName != null)
						{
							var nodeName = modelName.Substring(0, modelName.IndexOf("."));
							var attributeName = modelName.Substring(modelName.IndexOf(".") + 1);

							model.GetNamedNode(nodeName).SetAttribute(attributeName, cost);
						}
					}
				}

				model.SaveToURL("", modelFilename);
			}
		}

		public override string GetRoundFile (int round, string filename, GamePhase phase)
		{
			if (GameTypeUsesTransitions() || GameTypeUsesTransitionFolderNamesButTheyAreSkipped())
			{
				if (GamePhase.OPERATIONS == phase)
				{
					return tempDirName + "\\round" + CONVERT.ToStr(round) + "_B_operations\\" + filename;
				}
				else
				{
					return tempDirName + "\\round" + CONVERT.ToStr(round) + "_A_transition\\" + filename;
				}
			}
			else
			{
				return tempDirName + CONVERT.Format(@"\round{0}\{1}", round, filename);
			}
		}

		public string GetDefaultMaturityFile (int round, string subDir, GamePhase phase)
		{
			if (GamePhase.OPERATIONS == phase)
			{
				return AppInfo.TheInstance.Location + "\\data\\" + subDir + "\\eval_wizard_default_" + CONVERT.ToStr(round) + ".xml";
			}
			else
			{
				return AppInfo.TheInstance.Location + "\\data\\" + subDir + "\\eval_wizard_default_" + CONVERT.ToStr(round) + ".xml";
			}
		}

		public string GetDefaultProcessFile (int round, string maturityFilename, GamePhase phase)
		{
			if (phase == GamePhase.OPERATIONS)
			{
				return AppInfo.TheInstance.Location + "\\data\\MaturityDefaults\\" + Path.GetFileNameWithoutExtension(maturityFilename) + "_" + CONVERT.ToStr(round) + ".xml";
			}
			else
			{
				return AppInfo.TheInstance.Location + "\\data\\maturityDefaults\\" + Path.GetFileNameWithoutExtension(maturityFilename) + "_" + CONVERT.ToStr(round) + ".xml";
			}
		}

		protected virtual string GetPhasePath (int phaseNumber)
		{
			PhaseToRound(phaseNumber, out var round, out var phase);
			return GetPhasePath(round, phase);
		}

		protected virtual string GetPhasePath (int round, GameFile.GamePhase phase)
		{
			if (GameTypeUsesTransitions() || GameTypeUsesTransitionFolderNamesButTheyAreSkipped())
			{
				string phaseName = (phase == GamePhase.OPERATIONS) ? "b_operations" : "a_transition";
				return tempDirName + CONVERT.Format(@"\round{0}_{1}\", round, phaseName);
			}
			else
			{
				return tempDirName + CONVERT.Format(@"\round{0}\", round);
			}
		}

		public virtual string GetNetworkFile (int round)
		{
			if (!GameTypeUsesTransitions())
			{
				return GetNetworkFile(round, GamePhase.OPERATIONS);
			}

			return null;
		}

		/// <summary>
		/// Retrieves the path to the network file for the current round/phase.
		/// </summary>
		/// <param name="round">The round the caller is interested in (1-X).</param>
		/// <param name="phase">The GamePhase that the caller is interested in.</param>
		/// <returns></returns>
		public virtual string GetNetworkFile (int round, GamePhase phase)
		{
			return GetNetworkFile(round, phase, true);
		}

		public virtual string GetNetworkFile (int round, GamePhase phase, bool useSalesIfPresent)
		{
			string filename;

			if (useSalesIfPresent)
			{
				filename = GetPhasePath(round, phase) + "network_ran.xml";
				if (File.Exists(filename))
				{
					return filename;
				}
			}

			filename = GetPhasePath(round, phase) + "network.xml";
			// If it doesn't exist then do a rewind to get it.
			if (!File.Exists(filename))
			{
				// Copy round from previous...
				CopyPreviousNetwork(round, phase);
			}

			return filename;
		}

		/// <summary>
		/// Rewinds / Resets the current round/phase so it is in its default state.
		/// </summary>
		public void ResetRoundPhase ()
		{
			SetCurrentRound(currentRound, currentPhase, true);
		}

		/// <summary>
		/// Sets the current round (1-X) and the current GamePhase.
		/// </summary>
		/// <param name="round">The current round (1-X).</param>
		/// <param name="phase">The current GamePhase.</param>
		public void SetCurrentRound (int round, GamePhase phase)
		{
			SetCurrentRound(round, phase, false);
		}

		public void SetCurrentPhase (int phaseNumber, bool reset = false)
		{
			int round;
			GamePhase phase;
			PhaseToRound(Math.Max(MinPhaseNumber, Math.Min(MaxPhaseNumber, phaseNumber)), out round, out phase);
			SetCurrentRound(round, phase, reset);
		}

		public virtual void SetCurrentRound (int round, GamePhase phase, bool reset)
		{
			roundToPhaseToModel.Clear();

			if (!reset && (round == this.currentRound) && (phase == this.currentPhase))
			{
				return;
			}
			Save(false);
			if (null != _NetworkModel)
			{
				this.RemoveISaver(_NetworkModel);
			}
			currentPhase = phase;
			currentRound = round;
			currentPhaseHasStarted = false;
			if ((GamePhase.TRANSITION == phase) && (round == 1))
			{
				throw (new Exception("No Transition Phase Exists For Round 1. Attempting to set game to round 1 transition phase."));
			}

			if (reset && _allowWriteToDisk)
			{
				CopyPreviousNetwork(currentRound, currentPhase);
			}

			if (!IsTrainingGame || _NetworkModel == null || !SkinningDefs.TheInstance.GetBoolData("access_all_training_rounds", false))
			{
				string xmlFileName = GetNetworkFile(round, currentPhase, false);

				try
				{
					System.IO.StreamReader file = new System.IO.StreamReader(xmlFileName);
					string xmldata = file.ReadToEnd();
					file.Close();
					file = null;
					_NetworkModel = new NodeTree(xmldata);
					this.AddISaver(_NetworkModel, xmlFileName);
				}
				catch (FileNotFoundException)
				{
				}
			}
			Save(false);
		}
		#region ITimedClass Members

		/// <summary>
		/// 
		/// </summary>
		public void Start ()
		{
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="timesRealTime"></param>
		public void FastForward (double timesRealTime)
		{
		}

		/// <summary>
		/// 
		/// </summary>
		public void Reset ()
		{
			this.ResetRoundPhase();
		}

		/// <summary>
		/// 
		/// </summary>
		public void Stop ()
		{
		}

		#endregion

		public virtual NodeTree GetNetworkModel (int round)
		{
			if (!GameTypeUsesTransitions())
			{
				return GetNetworkModel(round, GamePhase.OPERATIONS);
			}

			return null;
		}


		public virtual void CreateTrainingNetworkFile (int round, GamePhase gamePhase)
		{
			int lastRound = LastRoundPlayed;
			GameFile.GamePhase lastPhase = LastPhasePlayed;
			bool applyskip = true;
			int skipRound = 1;


			NodeTree TrainingGameNetwork;
			string originalNetworkFile = LibCore.AppInfo.TheInstance.Location + "\\data\\round1\\network.xml";

			if (round == lastRound + 1)
			{
				TrainingGameNetwork = NetworkModel;
				if (gamePhase == GamePhase.TRANSITION)
				{
					applyskip = false;

				}
				else
				{
					skipRound = lastRound;
				}

			}
			else if (lastRound == round && LastPhasePlayed == GamePhase.TRANSITION && gamePhase == GamePhase.OPERATIONS)
			{
				TrainingGameNetwork = NetworkModel;
				applyskip = false;
			}
			else if ((round > lastRound) && lastRound != 1)
			{
				TrainingGameNetwork = NetworkModel;
				skipRound = lastRound;
			}
			else
			{
				using (StreamReader reader = new StreamReader(originalNetworkFile))
				{
					TrainingGameNetwork = new Network.NodeTree(reader.ReadToEnd());
				}
			}


			if (gamePhase == GamePhase.TRANSITION)
			{
				round--;
			}

			if (!((LastRoundPlayed == round) && (LastPhasePlayed == GamePhase.TRANSITION)) && applyskip)
			{
				for (int skipsToUse = skipRound; skipsToUse < round; skipsToUse++)
				{
					//Read the Required skip incidents xml file
					string skipDefsFile;

					if (File.Exists(LibCore.AppInfo.TheInstance.Location + "data\\training_skip" + LibCore.CONVERT.ToStr(skipsToUse + 1) + ".xml"))
					{
						skipDefsFile = LibCore.AppInfo.TheInstance.Location + "data\\training_skip" + LibCore.CONVERT.ToStr(skipsToUse + 1) + ".xml";
					}
					else
					{
						skipDefsFile = LibCore.AppInfo.TheInstance.Location + "data\\skip" + LibCore.CONVERT.ToStr(skipsToUse + 1) + ".xml";
					}

					if (System.IO.File.Exists(skipDefsFile))
					{
						using (IncidentManagement.IncidentApplier iApplier = new IncidentManagement.IncidentApplier(TrainingGameNetwork))
						{
							iApplier.SetIncidentDefinitions(System.IO.File.ReadAllText(skipDefsFile), TrainingGameNetwork);
						}
					}
				}
			}

			_NetworkModel = TrainingGameNetwork;
		}

		public virtual NodeTree GetNetworkModel (int round, GamePhase phase)
		{
			// If we're currently playing, and not a sales game, then return the current model.
			if ((round == currentRound) && (phase == currentPhase) && (_allowSave || _allowWriteToDisk))
			{
				return NetworkModel;
			}

			if (!roundToPhaseToModel.ContainsKey(round))
			{
				roundToPhaseToModel.Add(round, new Dictionary<GamePhase, NodeTree>());
			}


			if (!roundToPhaseToModel[round].ContainsKey(phase))
			{
				roundToPhaseToModel[round].Add(phase, new NodeTree(File.ReadAllText(GetNetworkFile(round, phase))));
			}

			return roundToPhaseToModel[round][phase];
		}

		public string GetGameType ()
		{
			string gameDetailsFile = Dir + @"\global\details.xml";
			LibCore.BasicXmlDocument xml = LibCore.BasicXmlDocument.CreateFromFile(gameDetailsFile);
			return LibCore.BasicXmlDocument.GetStringAttribute(xml.SelectSingleNode("details"), "type", "");
		}

		void LoadLicence ()
		{
			if ((productLicence != null)
				&& File.Exists(LicenceGuidFilename)
				&& Guid.TryParse(File.ReadAllText(LicenceGuidFilename), out var guid))
			{
				licence = productLicence.GetGameLicence(guid);
			}
			else
			{
				licence = null;
			}
		}

		public static NetworkProgressionGameFile OpenExistingRespectingType (string filename, string roundOneFilesDir,
																			 bool allowSave, bool allowWriteToDisk)
		{
			string type = "";
			using (NetworkProgressionGameFile untypedGame = NetworkProgressionGameFile.OpenExistingOnlyForDeterminingType(filename, roundOneFilesDir, allowSave, allowWriteToDisk))
			{
				// All games created by recent-enough code will tag their game details file with the type.
				type = untypedGame.GetGameType();
			}

			switch (type)
			{
				case "PoleStar":
				case "Polestar":
					return NetworkProgressionGameFile.OpenExisting(filename, roundOneFilesDir, allowSave, allowWriteToDisk);

				case "AOSE":
					return AOSE_NetworkProgressionGameFile.OpenExistingAOSE(filename, roundOneFilesDir, allowSave, allowWriteToDisk);

				case "DevOps":
					return DevOpsNetworkProgressionGameFile.OpenExistingDevOps(filename, roundOneFilesDir, allowSave, allowWriteToDisk);

				case "Polestar_PM":
					return PMNetworkProgressionGameFile.OpenExistingPM(filename, roundOneFilesDir, allowSave, allowWriteToDisk);

				case "Cloud":
					return Cloud_NetworkProgressionGameFile.OpenExisting_Cloud(filename, roundOneFilesDir, allowSave, allowWriteToDisk);

				default:
					throw new UnableToDetermineGameTypeException();
			}
		}

		public class UnableToDetermineGameTypeException : Exception
		{
		}

		public virtual bool GameTypeUsesTransitionFolderNamesButTheyAreSkipped ()
		{
			return false;
		}

		public virtual int OpsRoundToBePlayedInSalesGame ()
		{
			int salesRound = SkinningDefs.TheInstance.GetIntData("sales_round", 0);
			if (salesRound == 0)
			{
				if (SkinningDefs.TheInstance.GetIntData("sales_round_1_ops_only", 0) == 1)
				{
					salesRound = 1;
				}
				else if (SkinningDefs.TheInstance.GetIntData("sales_round_2_ops_only", 0) == 1)
				{
					salesRound = 2;
				}
				else
				{
					salesRound = 2;
				}
			}

			return salesRound;
		}

		public void CopyNewCustomMaturityFiles (string file)
		{
			MaturityInfoFile maturityFile = new MaturityInfoFile(file);
			string evalFile = maturityFile.GetFile(GetMaturityFilenameByType(em_GameEvalType.CUSTOM));
			string ignoreFile = maturityFile.GetFile("Eval_States.xml");

			File.Copy(evalFile, Dir + @"\global\" + GetMaturityFilenameByType(em_GameEvalType.CUSTOM), true);
			File.Copy(evalFile, Dir + @"\global\Eval_States.xml", true);

			for (int round = 1; round <= 5; round++)
			{
				string destination = GetRoundFile(round, GetMaturityFilenameByType(em_GameEvalType.CUSTOM), GamePhase.OPERATIONS);

				if (Directory.Exists(Path.GetDirectoryName(destination)))
				{
					File.Copy(evalFile, destination, true);
				}
			}
		}

		protected virtual void CopyInitialMaturityFiles ()
		{
			CopyInitialMaturityFiles(FirstRoundNumber);
		}

		protected virtual void CopyInitialMaturityFiles (int round)
		{
			foreach (string maturityFile in GetAllMaturityFilenames())
			{
				if (maturityFile != "eval_wizard_iso.xml")
				{
					if (SkinningDefs.TheInstance.GetBoolData("use_maturity_defaults", false))
					{
						if (File.Exists(this.GetDefaultProcessFile(round, maturityFile, GameManagement.GameFile.GamePhase.OPERATIONS)))
						{
							File.Copy(this.GetDefaultProcessFile(round, maturityFile, GameManagement.GameFile.GamePhase.OPERATIONS),
									 GetPhasePath(round, GamePhase.OPERATIONS) + @"\" + maturityFile, true);
						}
						else
						{
							if (File.Exists(AppInfo.TheInstance.Location + @"\data\" + maturityFile))
							{
								File.Copy(AppInfo.TheInstance.Location + @"\data\" + maturityFile,
										  GetPhasePath(round, GamePhase.OPERATIONS) + @"\" + maturityFile, true);
							}
							else if (File.Exists(tempDirName + @"\global\" + maturityFile))
							{
								File.Copy(tempDirName + @"\global\" + maturityFile,
										  GetPhasePath(round, GamePhase.OPERATIONS) + @"\" + maturityFile, true);
							}
						}
					}
					else if (SkinningDefs.TheInstance.GetBoolData("setMaturityDefaults", false) && maturityFile == "eval_wizard.xml")
					{
						if (File.Exists(this.GetDefaultMaturityFile(round, "ITILDefaults", GameManagement.GameFile.GamePhase.OPERATIONS)))
						{
							File.Copy(this.GetDefaultMaturityFile(round, "ITILDefaults", GameManagement.GameFile.GamePhase.OPERATIONS),
									 GetPhasePath(round, GamePhase.OPERATIONS) + @"\" + maturityFile, true);
						}
						else
						{
							if (File.Exists(AppInfo.TheInstance.Location + @"\data\" + maturityFile))
							{
								File.Copy(AppInfo.TheInstance.Location + @"\data\" + maturityFile,
										  GetPhasePath(round, GamePhase.OPERATIONS) + @"\" + maturityFile, true);
							}
							else if (File.Exists(tempDirName + @"\global\" + maturityFile))
							{
								File.Copy(tempDirName + @"\global\" + maturityFile,
										  GetPhasePath(round, GamePhase.OPERATIONS) + @"\" + maturityFile, true);
							}
						}
					}
					else
					{
						if (File.Exists(AppInfo.TheInstance.Location + @"\data\" + maturityFile))
						{
							File.Copy(AppInfo.TheInstance.Location + @"\data\" + maturityFile,
									  GetPhasePath(round, GamePhase.OPERATIONS) + @"\" + maturityFile, true);
						}
						else if (File.Exists(tempDirName + @"\global\" + maturityFile))
						{
							File.Copy(tempDirName + @"\global\" + maturityFile,
									  GetPhasePath(round, GamePhase.OPERATIONS) + @"\" + maturityFile, true);
						}
					}
				}
				else
				{
					if (round != 1 && File.Exists(this.GetRoundFile(round - 1, maturityFile, GameManagement.GameFile.GamePhase.OPERATIONS)))
					{
						File.Copy(this.GetRoundFile(round - 1, maturityFile, GameManagement.GameFile.GamePhase.OPERATIONS),
							 GetPhasePath(round, GamePhase.OPERATIONS) + @"\" + maturityFile, true);
					}
					else if (round == 1 && File.Exists(AppInfo.TheInstance.Location + @"\data\" + maturityFile))
					{
						File.Copy(AppInfo.TheInstance.Location + @"\data\" + maturityFile,
								  GetPhasePath(round, GamePhase.OPERATIONS) + @"\" + maturityFile, true);
					}
					else if (File.Exists(tempDirName + @"\global\" + maturityFile))
					{
						File.Copy(tempDirName + @"\global\" + maturityFile,
								  GetPhasePath(round, GamePhase.OPERATIONS) + @"\" + maturityFile, true);
					}
				}
			}

			if (SkinningDefs.TheInstance.GetBoolData("use_default_ignore_list", false) && round == 1)
			{
				if (File.Exists(AppInfo.TheInstance.Location + @"\data\Eval_States.xml"))
				{
					File.Copy(AppInfo.TheInstance.Location + @"\data\Eval_States.xml",
						Dir + @"\global\Eval_States.xml", true);
				}
			}

		}

		protected virtual string GetRoundMaturityFile (int round)
		{
			return GetRoundFile(round, GetMaturityFilename(), GamePhase.OPERATIONS);
		}

		protected virtual string[] GetAllMaturityFilenames ()
		{
			List<string> all = new List<string>();
			foreach (em_GameEvalType type in Enum.GetValues(typeof(em_GameEvalType)))
			{
				all.Add(GetMaturityFilenameByType(type));
			}

			all.Add("pathfinder_survey_wizard.xml");
			all.Add("cloud_score_wizard.xml");
			all.Add("nps_survey_wizard.xml");

			return all.ToArray();
		}

		public virtual string GetMaturityFilename ()
		{
			return GetMaturityFilenameByType(Game_Eval_Type);
		}

		public virtual string GetMaturityFilenameByType (em_GameEvalType type)
		{
			switch (type)
			{
				case em_GameEvalType.ISO_20K:
					return "eval_wizard_iso.xml";

				case em_GameEvalType.MOF:
					return "eval_wizard_mof.xml";

				case em_GameEvalType.LEAN:
					return "eval_wizard_lean.xml";

				case em_GameEvalType.PMBOK:
					return "eval_wizard_pmbok.xml";

				case em_GameEvalType.PRINCE2:
					return "eval_wizard_prince2.xml";

				case em_GameEvalType.CUSTOM:
					return "eval_wizard_custom.xml";

				case em_GameEvalType.ESM:
					return "eval_wizard_esm.xml";

				case em_GameEvalType.UNDEFINED:
				case em_GameEvalType.ITIL:
				default:
					return "eval_wizard.xml";
			}
		}

		public string GetMaturityRoundFile (int round)
		{
			string correctFile = GetRoundFile(round, GetMaturityFilename(), GamePhase.OPERATIONS);
			if (File.Exists(correctFile))
			{
				return correctFile;
			}
			else if (GetMaturityFilename() == "eval_wizard_custom.xml")
			{
				if (File.Exists(Dir + @"\global\eval_custom.xml"))
				{
					File.Copy(Dir + @"\global\eval_custom.xml", Dir + @"\round" + round + @"_B_operations\eval_custom.xml", true);
				}
				return GetRoundFile(round, "eval_custom.xml", GamePhase.OPERATIONS);
			}

			// Fall back on anything we can find -- so that old gamefiles (eg a PM game file with ITIL
			// eval files) can still work.
			foreach (em_GameEvalType type in Enum.GetValues(typeof(em_GameEvalType)))
			{
				string tryFile = GetRoundFile(round, GetMaturityFilenameByType(type), GamePhase.OPERATIONS);
				if (File.Exists(tryFile))
				{
					return tryFile;
				}
			}

			return null;
		}

		public virtual bool IsPdfTeamPhotoOverriddenFromDefault ()
		{
			return File.Exists(Dir + @"\global\team_photo.png");
		}

		public virtual Image GetPdfTeamPhoto ()
		{
			string teamPhoto = Dir + @"\global\team_photo.png";
			string placeHolder = AppInfo.TheInstance.Location + @"\images\PDF_Placeholder.png";
			string loadingScreen = AppInfo.TheInstance.Location + @"\images\v3SplashScreen.png";
			string file;

			if (File.Exists(teamPhoto))
			{
				file = teamPhoto;
			}
			else if (File.Exists(placeHolder))
			{
				file = placeHolder;
			}
			else
			{
				file = loadingScreen;
			}

			using (Image image = Image.FromFile(file))
			{
				return ImageUtils.ScaleImageToAspectRatio(image, 1.45);
			}
		}

		public virtual Image GetFacilitatorLogo ()
		{
			string gameLogo = Dir + @"\global\facil_logo.png";
			string defaultLogo = AppInfo.TheInstance.Location + @"\images\DefFacLogo.png";
			string file;

			if (File.Exists(gameLogo))
			{
				file = gameLogo;
			}
			else
			{
				file = defaultLogo;
			}

			if (File.Exists(file))
			{
				using (Image image = Image.FromFile(file))
				{
					return ImageUtils.ScaleImageToAspectRatio(image, SkinningDefs.TheInstance.GetIntData("image_scale_ratio", 2), SkinningDefs.TheInstance.GetBoolData("logo_panel_transparency", false) ? Color.Transparent : Color.White);
				}
			}

			return null;
		}

		public virtual Image GetClientLogo ()
		{
			string gameLogo = Dir + @"\global\logo.png";
			string defaultLogo = AppInfo.TheInstance.Location + @"\images\DefCustLogo.png";
			string file;

			if (File.Exists(gameLogo))
			{
				file = gameLogo;
			}
			else
			{
				file = defaultLogo;
			}

			if (File.Exists(file))
			{
				using (Image image = Image.FromFile(file))
				{
					return ImageUtils.ScaleImageToAspectRatio(image, SkinningDefs.TheInstance.GetIntData("image_scale_ratio", 2), SkinningDefs.TheInstance.GetBoolData("logo_panel_transparency", false) ? Color.Transparent : Color.White);
				}
			}

			return null;
		}
		string CustomContentFolder => Dir + @"\custom_content";
		string customContentSourceOptionName = "custom_content_source";
		string customContentFilenameOptionBaseName = "custom_content_filename_";

		public CustomContentSource CustomContentSource
		{
			get
			{
				var option = GetOption(customContentSourceOptionName);
				if (option != null)
				{
					CustomContentSource value;
					if (Enum.TryParse<CustomContentSource>(option, out value))
					{
						return value;
					}
				}

				return CustomContentSource.None;
			}

			private set
			{
				SetOption(customContentSourceOptionName, value.ToString());
			}
		}

		public string GetCustomContentFilename (string tag)
		{
			var option = GetOption(customContentFilenameOptionBaseName + tag);
			if (! string.IsNullOrEmpty(option))
			{
				return Path.Combine(CustomContentFolder, option);
			}
			else
			{
				return null;
			}
		}

		public void ImportCustomContent (string tag, string sourceFilename)
		{
			if (! Directory.Exists(CustomContentFolder))
			{
				Directory.CreateDirectory(CustomContentFolder);
			}

			string copiedFilename = tag + Path.GetExtension(sourceFilename);

			File.Copy(sourceFilename, Path.Combine(CustomContentFolder, copiedFilename), true);
			SetOption(customContentFilenameOptionBaseName + tag, copiedFilename);

			Repository.TheInstance.RemoveImage(Path.Combine(CustomContentFolder, copiedFilename));

			CustomContentSource = CustomContentSource.GameFile;
		}

		public void DeleteAllCustomContent ()
		{
			if (Directory.Exists(CustomContentFolder))
			{
				foreach (var filename in Directory.GetFiles(CustomContentFolder))
				{
					Repository.TheInstance.RemoveImage(filename);
				}

				Directory.Delete(CustomContentFolder, true);
			}

			CustomContentSource = CustomContentSource.None;

			var options = GetAllOptions(customContentFilenameOptionBaseName);
			foreach (var option in options.Keys)
			{
				RemoveOption(option);
			}
		}

		public void DeleteCustomContentByTag (string tag)
		{
			string filename = GetCustomContentFilename(tag);

			if (! string.IsNullOrEmpty(filename))
			{
				File.Delete(filename);
				Repository.TheInstance.RemoveImage(filename);
			}

			RemoveOption(customContentFilenameOptionBaseName + tag);
		}

		public virtual void SetOption (string key, string value)
		{
			var filename = GetGlobalFile("options.xml");
			BasicXmlDocument xml;
			if (File.Exists(filename))
			{
				xml = BasicXmlDocument.CreateFromFile(filename);
			}
			else
			{
				xml = BasicXmlDocument.Create();
				xml.AppendNewChild("options");
			}

			var element = (XmlElement) xml.DocumentElement.SelectSingleNode(key);
			if (element == null)
			{
				element = xml.DocumentElement.AppendNewChild(key);
			}

			element.InnerText = value;

			xml.Save(filename);
		}

		public virtual void RemoveOption (string key)
		{
			var filename = GetGlobalFile("options.xml");
			BasicXmlDocument xml;
			if (File.Exists(filename))
			{
				xml = BasicXmlDocument.CreateFromFile(filename);

				var element = (XmlElement) xml.DocumentElement.SelectSingleNode(key);
				if (element != null)
				{
					element.ParentNode.RemoveChild(element);
					xml.Save(filename);
				}
			}
		}

		public virtual string GetOption (string key, string defaultValue = null)
		{
			var options = GetAllOptions(key);

			if (options.ContainsKey(key))
			{
				return options[key];
			}

			return defaultValue;
		}

		public Dictionary<string, string> GetAllOptions (string baseName = "")
		{
			var options = new Dictionary<string, string> ();

			var filename = GetGlobalFile("options.xml");
			if (File.Exists(filename))
			{
				var xml = BasicXmlDocument.CreateFromFile(filename);

				foreach (XmlElement element in xml.DocumentElement.ChildNodes)
				{
					if (element.Name.StartsWith(baseName))
					{
						options.Add(element.Name, element.InnerText);
					}
				}
			}

			return options;
		}

		public virtual void RevertSalesChanges()
        {
        }

        public int LastOpsRoundPlayed
        {
            get
            {
                if (GameTypeUsesTransitions()
                    && (LastPhasePlayed == GamePhase.TRANSITION))
                {
                    return Math.Max(0, LastRoundPlayed - 1);
                }
                else
                {
                    return LastRoundPlayed;
                }
            }
        }

        public virtual string GetNetworkModelFilenameAtStartOfRound(int round)
        {
            string filename = GetRoundFile(round, "network_at_start.xml", GamePhase.OPERATIONS);
            if (!File.Exists(filename))
            {
                if (round == 1)
                {
                    filename = LibCore.AppInfo.TheInstance.Location + "\\data\\round1\\network.xml";
                }
                else
                {
                    filename = GetRoundFile(round - 1, "network.xml", GamePhase.OPERATIONS);
                }
            }

            return filename;
        }

        public virtual NodeTree GetNetworkModelAtStartOfRound(int round)
        {
            return new NodeTree(File.ReadAllText(GetNetworkModelFilenameAtStartOfRound(round)));
        }

        public virtual double DefaultSpeed
        {
            get
            {
                if (IsTrainingGame)
                {
                    return 3;
                }
                else if (IsSalesGame)
                {
                    if (CurrentPhase == GamePhase.OPERATIONS)
                    {
                        return 1;
                    }
                    else
                    {
                        return 4;
                    }
                }
                else
                {
                    return 1;
                }
            }
        }

        public virtual bool CanSkipPhase(int phase)
        {
            return false;
        }

        public int RoundToReportOn
        {
            get
            {
                return LastRoundPlayed;
            }
        }

        public static string SalesGameFilename
        {
            get
            {
                return LibCore.AppInfo.TheInstance.Location + "\\data\\2007_6_13-#Sales Game#-9d8cf9f4-c5ad-4316-8b65-d969f0329412.gmz";
            }
        }

        public virtual int MinPhaseNumber
        {
            get
            {
                return RoundToPhase(FirstRoundNumber, GamePhase.OPERATIONS);
            }
        }

        public virtual int MaxPhaseNumber
        {
            get
            {
				return RoundToPhase(CoreUtils.SkinningDefs.TheInstance.GetIntData("roundcount", 5), GamePhase.OPERATIONS);
			}
        }

        /// <summary>
        /// Like CanPlayPhase(), but also returns true for a phase that, although not playable right now, would be playable
        /// after we select to skip, eg, a transition phase.
        /// </summary>
        /// <param name="phase"></param>
        /// <returns></returns>
        public virtual bool CouldPlayPhaseAfterSkip(int phase)
        {
            if (GameTypeUsesTransitions())
            {
                int round;
                GamePhase phaseType;
                PhaseToRound(phase, out round, out phaseType);

                if (IsSalesGame)
                {
                    if ((phaseType == GamePhase.OPERATIONS) && (round != OpsRoundToBePlayedInSalesGame()))
                    {
                        return false;
                    }
                    else if ((phaseType == GamePhase.TRANSITION) && (round != (OpsRoundToBePlayedInSalesGame() + 1)))
                    {
                        return false;
                    }
                }

                if ((phaseType == GamePhase.OPERATIONS)
                    && ((round - CurrentRound) <= 1))
                {
                    return true;
                }
            }

            return CanPlayPhase(phase);
        }

        public virtual bool CanPlayPhase (int phase, bool ignoreTemporaryUnplayability = false)
        {
            if ((phase < MinPhaseNumber)
                || (phase > MaxPhaseNumber))
            {
                return false;
            }

            if (IsSalesGame)
            {
                return ((phase == RoundToPhase(OpsRoundToBePlayedInSalesGame(), GamePhase.OPERATIONS))
                        || (GameTypeUsesTransitions() && (phase == RoundToPhase(OpsRoundToBePlayedInSalesGame() + 1, GamePhase.TRANSITION))));
            }
            else if (IsTrainingGame
                     && SkinningDefs.TheInstance.GetBoolData("access_all_training_rounds", false))
            {
                return true;
            }
            else
            {
                // Can't replay rounds before the last one played.
                if (phase < LastPhaseNumberPlayed)
                {
                    return false;
                }

                // We can skip forwards if we're allowed to.
                for (int phaseToSkip = (LastPhaseNumberPlayed + 1); phaseToSkip < phase; phaseToSkip++)
                {
                    if (!CanSkipPhase(phaseToSkip))
                    {
                        return false;
                    }
                }

	            if (Licence == null)
	            {
		            return false;
	            }

                // Otherwise, the licence will handle the maximum number of plays allowed etc.
	            if (ignoreTemporaryUnplayability)
	            {
		            return ! Licence.GetPhasePlayability(phase).IsPermanentlyUnplayable;
	            }
				else
	            {
		            return Licence.GetPhasePlayability(phase).IsPlayable;
	            }
            }
        }

        public void AdvanceRound()
        {
            if (CurrentPhaseNumber < MaxPhaseNumber)
            {
                SetCurrentPhase(CurrentPhaseNumber + 1, true);
            }
        }

        public override string CurrentRoundDir
        {
            get
            {
                int round = currentRound;
                if (round == 0)
                {
                    round++;
                }

                if (GameTypeUsesTransitions() || GameTypeUsesTransitionFolderNamesButTheyAreSkipped())
                {
                    if (GamePhase.OPERATIONS == currentPhase)
                    {
                        return tempDirName + "\\round" + CONVERT.ToStr(round) + "_B_operations";
                    }
                    else
                    {
                        return tempDirName + "\\round" + CONVERT.ToStr(round) + "_A_transition";
                    }
                }
                else
                {
                    return tempDirName + @"\round" + CONVERT.ToStr(round);
                }
            }
        }

		string DetailsFilename => GetGlobalFile("details.xml");

		void SetDetailsElement (XmlElement root, string name, string value)
		{
			var element = root.SelectSingleNode(name) ?? root.AppendNewChild(name);
			element.InnerText = value;
		}

		public void SetDetails (GameDetails details)
		{
			var xml = BasicXmlDocument.LoadOrCreate(DetailsFilename);

			var detailsElement = (XmlElement) xml.SelectSingleNode("details") ?? xml.AppendNewChild("details");
			var typeAttribute = detailsElement.Attributes["type"] ?? detailsElement.AppendAttribute("type", "");
			typeAttribute.Value = SkinningDefs.TheInstance.GetData("gametype");

			SetDetailsElement(detailsElement, "Title", details.Title);
			SetDetailsElement(detailsElement, "Venue", details.Venue);
			SetDetailsElement(detailsElement, "Location", details.Location);
			SetDetailsElement(detailsElement, "Client", details.Client);
			SetDetailsElement(detailsElement, "Players", CONVERT.ToStr(details.Players));
			SetDetailsElement(detailsElement, "GeoRegion", details.Region);
			SetDetailsElement(detailsElement, "Country", details.Country);
			SetDetailsElement(detailsElement, "Purpose", details.Purpose);
			SetDetailsElement(detailsElement, "Notes", details.Notes);
			SetDetailsElement(detailsElement, "ChargeCompany", details.ChargeCompany);
			SetDetailsElement(detailsElement, "evaltype", "ITIL");
			SetDetailsElement(detailsElement, "GameType", "ITIL");

			xml.Save(DetailsFilename);
		}

		public void SetSalesGameLicence ()
		{
			licence = productLicence.CreateSalesGameLicence(new GameDetails (GetTitle(), GetVenue(), GetLocation(), GetClient(),
				GetRegion(), GetCountry(), GetChargeCompany(), GetNotes(), GetPurpose(), GetPlayers()));
		}

		public event EventHandler StateChanged;

		public void OnStateChanged ()
		{
			StateChanged?.Invoke(this, EventArgs.Empty);
		}
	}
}