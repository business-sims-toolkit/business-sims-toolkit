using System.IO;

using LibCore;
using Licensor;
using zip =ICSharpCode.SharpZipLib;

namespace GameManagement
{
	public class PMNetworkProgressionGameFile : NetworkProgressionGameFile
	{
		internal PMNetworkProgressionGameFile (string filename, string roundOneFilesDir, bool isNew,
			bool allowSave, bool allowWriteToDisk, IGameLicence licence)
			: base (filename, roundOneFilesDir, isNew, allowSave, allowWriteToDisk, licence)
		{
		}

		public override void PhaseToRound(int phase, out int round, out GamePhase gamePhase)
		{
			round = 1;
			gamePhase = GamePhase.OPERATIONS;

			switch (phase)
			{
				case 1:
					round = 2;
					gamePhase = GamePhase.OPERATIONS;
					break;

				case 2:
					round = 3;
					gamePhase = GamePhase.OPERATIONS;
					break;
			}
		}

		protected override void Setup(string filename, string roundOneFilesDir, bool isNew, 
			bool allowSave, bool allowWriteToDisk, IGameLicence licence)
		{
			_allowWriteToDisk = allowWriteToDisk;
			_allowSave = allowSave;
			theRoundOneFilesDir = roundOneFilesDir;
			fileName = filename;
			tempDirName = Path.GetTempFileName();
			File.Delete(tempDirName);
			DirectoryInfo di = Directory.CreateDirectory(tempDirName);
			//
			if (isNew)
			{
				CopyDirContents(roundOneFilesDir, tempDirName + "\\round1");
				CreateBaseDirs();
			}
			else
			{
				// Unzip the game file to our temp dir.
				ConfPack cp = new ConfPack();
				cp.ExtractAllFilesFromZip(filename, tempDirName, "");//"password");
				SetEvaluationTypeFromDetails();
			}
		}

		public string GetNetworkFileAllowingSalesGameOverride (int round, GamePhase phase)
		{
			// If it doesn't exist then do a rewind to get it.
			string filename = tempDirName + "\\round" + CONVERT.ToStr(round) + "\\Network_ran.xml";
			if (! File.Exists(filename))
			{
				filename = tempDirName + "\\round" + CONVERT.ToStr(round) + "\\Network.xml";
				if (! File.Exists(filename))
				{
					// Copy round from previous...
					CopyPreviousNetwork(round, phase);
				}
			}
			return filename;
		}

		public override int RoundToPhase(int round, GamePhase gamePhase)
		{
			return round - 1;
		}

		public override string CurrentRoundDir
		{
			get
			{
				return tempDirName + "\\round" + CONVERT.ToStr(currentRound);
			}
		}


        public override void CreateTrainingNetworkFile(int round, GamePhase gamePhase)
        {

            string dir = "";

            if (round < this.LastRoundPlayed)
            {
                dir = FindNetworkDirectory(round);
                CopyDirContents(dir, tempDirName + "\\round" + CONVERT.ToStr(round));
            }
           
            if (!Directory.Exists(tempDirName + "\\round" + CONVERT.ToStr(round - 1)) && round != 1)
            {
                dir = FindNetworkDirectory(round - 1);
                CopyDirContents(dir, tempDirName + "\\round" + CONVERT.ToStr(round - 1));
            }


            Network.NodeTree TrainingGameNetwork;
            if (dir != "")
            {
                using (StreamReader reader = new StreamReader(LibCore.AppInfo.TheInstance.Location + "\\data\\round1\\network.xml"))
                {
                    TrainingGameNetwork = new Network.NodeTree(reader.ReadToEnd());
                }


               


                if (!((LastRoundPlayed == round) && (LastPhasePlayed == GamePhase.TRANSITION)))
                {
                    for (int skipsToUse = 1; skipsToUse < round; skipsToUse++)
                    {
                        IncidentManagement.IncidentApplier iApplier = new IncidentManagement.IncidentApplier(TrainingGameNetwork);

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
                        System.IO.StreamReader file = new System.IO.StreamReader(skipDefsFile);
                        string xmldata = file.ReadToEnd();
                        file.Close();
                        file = null;

                        iApplier.SetIncidentDefinitions(xmldata, TrainingGameNetwork);

                        //mind to close and dispose
                        iApplier.Dispose();
                    }
                }

                _NetworkModel = TrainingGameNetwork;

            }

        }

		string FindNetworkDirectory(int round)
        {
            bool foundDirectory = false;
            int roundToUse = round;
            while (!foundDirectory)
            {
                if (Directory.Exists(LibCore.AppInfo.TheInstance.Location + "\\data\\round" + roundToUse))
                {
                    foundDirectory = true;
                }
                else
                {
                    roundToUse--;
                }

                if (roundToUse == 1)
                {
                    return LibCore.AppInfo.TheInstance.Location + "\\data\\round1";
                }
            }


            return LibCore.AppInfo.TheInstance.Location + "\\data\\round" + roundToUse;
        }

		protected override void CopyPreviousNetwork (int round, GamePhase phase)
		{
			//
			// PM has no transitions!
			string newDir = tempDirName + "\\round" + CONVERT.ToStr(round);
			//

			//
			// For round one and round three we cpoy from the original data dir.
			// For round two we copy from the round one dir.
			//
			string copy_network_from_dir;
			if (round > 1)
			{
				copy_network_from_dir = tempDirName + "\\round" + CONVERT.ToStr(round - 1);
			}
			else
			{
				copy_network_from_dir = theRoundOneFilesDir;
			}

			if (Directory.Exists(newDir))
			{
				Directory.Delete(newDir, true);
			}
			Directory.CreateDirectory(newDir);

			// Copy the network...
			CopyDirContents(copy_network_from_dir, newDir);

			// Ditch any log files that may have been copied.
			string logFile = newDir + "\\NetworkIncidents.log";
			if (File.Exists(logFile))
			{
				File.Delete(logFile);
			}

			CopyInitialMaturityFiles(round);

			string oldAssessmentsName = tempDirName + @"\round" + CONVERT.ToStr(round-1) + @"\assessment_selection.xml";
			if (File.Exists(oldAssessmentsName))
			{
				File.Copy(oldAssessmentsName, tempDirName + @"\round" + CONVERT.ToStr(round) + @"\assessment_selection.xml", true);
			}
		}

		public override string GetRoundFile (int round, string filename, GameManagement.GameFile.GamePhase phase)
		{
			string fullName = tempDirName + "\\round" + CONVERT.ToStr(round) + "\\" + filename;

			// Timesheets get handled specially in the sales game.
			if ((filename == "past_timesheet.xml") || (filename == "future_timesheet.xml"))
			{
				string ranName = tempDirName + "\\round" + CONVERT.ToStr(round) + "\\" + Path.GetFileNameWithoutExtension(filename) + "_ran.xml";

				if (File.Exists(ranName) && (round != CurrentRound))
				{
					return ranName;
				}
			}

			return fullName;
		}

		public override void SetCurrentRound (int round, GamePhase phase, bool reset)
		{
			base.SetCurrentRound(round, phase, reset);

			// If this is a sales game, don't overwrite anything, but do replace the network in-memory with
			// a blank one!
			if ((! this._allowSave) && (! this._allowWriteToDisk))
			{
				string originalNetworkFile = AppInfo.TheInstance.Location + "\\data\\round1\\network.xml";
				using (StreamReader reader = new StreamReader (originalNetworkFile))
				{
					_NetworkModel = new Network.NodeTree (reader.ReadToEnd());
				}
			}
		}

		public override Network.NodeTree GetNetworkModel (int round, GamePhase phase)
		{
			// If it's a sales game, always fetch from disk rather than the current state.
			if ((! _allowSave) && (! _allowWriteToDisk))
			{
				string roundFilename = GetNetworkFileAllowingSalesGameOverride(round, phase);

				using (StreamReader reader = new StreamReader(roundFilename))
				{
					return new Network.NodeTree(reader.ReadToEnd());
				}
			}

			return base.GetNetworkModel(round, phase);
		}

		public override bool GameTypeUsesTransitions ()
		{
			return false;
		}

		public override int OpsRoundToBePlayedInSalesGame ()
		{
			return 1;
		}
	}
}