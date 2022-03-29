using System.Collections.Generic;
using System.IO;

using LibCore;
using Licensor;
using zip = ICSharpCode.SharpZipLib;

namespace GameManagement
{
	public class Cloud_NetworkProgressionGameFile : NetworkProgressionGameFile
	{
		internal Cloud_NetworkProgressionGameFile (string filename, string roundOneFilesDir, bool isNew,
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

				case 3:
					round = 4;
					gamePhase = GamePhase.OPERATIONS;
					break;
			}
		}

		protected override void Setup(string filename, string roundOneFilesDir, bool isNew, 
			bool allowSave, bool allowWriteToDisk, IGameLicence licence)
		{
			_allowWriteToDisk = allowWriteToDisk;
			_allowSave = allowSave;
			phaseNumbersPlayed = new List<int>();
			this.licence = licence;
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

			//
			// Then copy the rest of the files definitely from the previous dir...
			string gameGlobalDataDir = tempDirName + "\\global";

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
			return 3;
		}

		string ChangeFilename (string filename, string newLastSection)
		{
			return Path.Combine(Path.GetDirectoryName(filename), newLastSection);
		}
	}
}