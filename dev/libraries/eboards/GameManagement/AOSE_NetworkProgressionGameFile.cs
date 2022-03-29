using System;
using System.IO;
using LibCore;
using CoreUtils;
using Licensor;
using Logging;

namespace GameManagement
{
	/// <summary>
	/// We did have a odd round progression but changed it back to standard
	/// This is insurance against going back to an odd round order 
	/// Code tidy up will remove this class if not needed 
	/// </summary>
	public class AOSE_NetworkProgressionGameFile : NetworkProgressionGameFile
	{
		bool UseReplayRound2inRound3 = true;

		internal AOSE_NetworkProgressionGameFile (string filename, string roundOneFilesDir, bool isNew,
				bool allowSave, bool allowWriteToDisk, IGameLicence licence)
			: base (filename, roundOneFilesDir, isNew, allowSave, allowWriteToDisk, licence)
		{
			UseReplayRound2inRound3 = SkinningDefs.TheInstance.GetBoolData("ReplayR2asR3", false);
		}

		public override bool GameTypeUsesTransitions ()
		{
			return false;
		}

		public override int RoundToPhase (int round, GameFile.GamePhase gamePhase)
		{
			return round - 1;
		}

		public override void PhaseToRound (int phase, out int round, out GameFile.GamePhase gamePhase)
		{
			gamePhase = GamePhase.OPERATIONS;
			round = phase + 1;
		}

		/// <summary>
		/// Copies the model from the previous round so the next round (used when a round is started or rewound).
		/// In AOSE, in R3, we replay from from the end of R1 (effectivly replaying r2 with different settings)
		///  so we just replace the filename that we are copying from so we get teh network file from end round 1
		/// </summary>
		/// <param name="round">The round to copy the previous model to.</param>
		/// <param name="phase">The phase to copy the model to.</param>
		/// 
		protected override void CopyPreviousNetwork(int round, GamePhase phase)
		{
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
							string newDir = tempDirName + "\\round1";
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
						}
						else
						{
							string newDir = tempDirName + "\\round" + CONVERT.ToStr(round);
							string newGameDataDir = tempDirName + "\\global";

							if (Directory.Exists(newDir))
							{
								Directory.Delete(newDir, true);
							}

							Directory.CreateDirectory(newDir);

							// If our current phase is operations then we step back to the transition phase network.
							//
							string pathname = "";
							string filename = "";
							//
							if (phase == GamePhase.OPERATIONS)
							{
								pathname = tempDirName + "\\round" + CONVERT.ToStr(round - 1) + "\\";
								filename = pathname + "Network.xml";
								if (!File.Exists(filename))
								{
									CopyPreviousNetwork(round, GamePhase.TRANSITION);
								}

								CopyInitialMaturityFiles(round);

								string oldAssessmentsName = tempDirName + @"\round" + CONVERT.ToStr(round - 1) + @"\assessment_selection.xml";
								if (File.Exists(oldAssessmentsName))
								{
									File.Copy(oldAssessmentsName, tempDirName + @"\round" + CONVERT.ToStr(round) + @"\assessment_selection.xml", true);
								}
							}
							else
							{
								// We are in the transition phase so we grab the network from the previous round's ops phase.
								int prevRound = round - 1;
								pathname = tempDirName + "\\round" + CONVERT.ToStr(prevRound) + "\\";
								filename = pathname + "Network.xml";

								if (!File.Exists(filename))
								{
									// Copy round from previous...
									CopyPreviousNetwork(round - 1, GamePhase.OPERATIONS);
								}

								// Also copy the upcoming assessment requests from the previous ops phase.
								string oldAssessmentsName = tempDirName + @"\round" + CONVERT.ToStr(prevRound) + @"\assessment_selection_for_next_round.xml";
								if (File.Exists(oldAssessmentsName))
								{
									File.Copy(oldAssessmentsName, tempDirName + @"\round" + CONVERT.ToStr(round) + @"\assessment_selection.xml", true);
								}
							}

							//
							if (UseReplayRound2inRound3)
							{
								string src_filename = filename;
								if (src_filename.IndexOf("round2") > -1)
								{
									src_filename = src_filename.Replace("round2", "round1");
								}
								//System.Diagnostics.Debug.WriteLine("##" + recursion.ToString() + " Copy Network file from " + filename);
								//System.Diagnostics.Debug.WriteLine("##" + recursion.ToString() + " Copy Network file from " + src_filename);
								//System.Diagnostics.Debug.WriteLine("##" + recursion.ToString() + " Copy Network file to " + newDir + "\\Network.xml");
								File.Copy(src_filename, newDir + "\\Network.xml");
							}
							else
							{
								//System.Diagnostics.Debug.WriteLine("##" + recursion.ToString() + " Copy Network file from " + filename);
								//System.Diagnostics.Debug.WriteLine("##" + recursion.ToString() + " Copy Network file to " + newDir + "\\Network.xml");
								File.Copy(filename, newDir + "\\Network.xml");
							}
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
	}
}