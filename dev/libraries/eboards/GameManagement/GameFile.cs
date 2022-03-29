using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Linq;
using CoreUtils;
using zip = ICSharpCode.SharpZipLib;
using LibCore;
using Licensor;

namespace GameManagement
{
	/// <summary>
	/// The GameFile class loads a game file that is a zipped repository of information
	/// that is used to play a game. Each round is in a sub directory in the zip. This
	/// zip is opened into a temporary folder and files are read from there. When the
	/// game is saved it is recompressed and saved back into the correct place.
	/// 
	/// Each round directiry is "round1", "round2", "round3", etc.
	/// </summary>
	public class GameFile : BaseClass, IDisposable
	{
		/// <summary>
		/// The Phase of the current game
		/// </summary>
		public enum GamePhase
		{
			/// <summary>
			/// Racing Screen
			/// </summary>
			OPERATIONS,
			/// <summary>
			/// Transition Screen
			/// </summary>
			TRANSITION
		}

		public Guid Id
		{
			get => GetGuidGlobalOption("id").Value;

			private set
			{
				SetGlobalOption("id", value);
			}
		}

		protected IGameLicence licence;

		public IGameLicence Licence => licence;

		protected bool currentPhaseHasStarted;

		public bool CurrentPhaseHasStarted => currentPhaseHasStarted;

		protected List<int> phaseNumbersPlayed;

		public bool PlayNow(int round, GamePhase gamePhase)
		{
			bool canPlayNow = false;

			// If this is a sales game always say yes.
			if (! this._allowSave && ! this._allowWriteToDisk)
			{
				canPlayNow = true;
			}
			else if (licence == null)
			{
				canPlayNow = false;
			}
			else
			{
				canPlayNow = licence.GetPhasePlayability(RoundToPhase(round, gamePhase)).IsPlayable;
				if (canPlayNow)
				{
					currentPhaseHasStarted = true;
					Save(false);
				}
			}

			if (canPlayNow)
			{
				int phase = RoundToPhase(round, gamePhase);
				if (! phaseNumbersPlayed.Contains(phase))
				{
					phaseNumbersPlayed.Add(phase);
				}
				licence.PlayPhase(phase);
			}

			return canPlayNow;
		}

		public bool CanPlayNow ()
		{
			return CanPlayNow(CurrentRound, CurrentPhase);
		}

		public bool CanPlayNow(int round, GamePhase gamePhase)
		{
			// If this is a sales or training game always say yes.
			if(!this._allowSave) return true;

			if (licence == null)
			{
				return false;
			}

			return licence.GetPhasePlayability(RoundToPhase(round, gamePhase)).IsPlayable;
		}

		public virtual int RoundToPhase(int round, GamePhase gamePhase)
		{
			if (CoreUtils.SkinningDefs.TheInstance.GetBoolData("uses_transition_rounds", true))
			{
				if (round == 1) return 0;
				int phase = (round - 2) * 2 + 1;
				if (gamePhase == GamePhase.OPERATIONS) ++phase;

				return phase;
			}
			else
			{
				return round - 1;
			}
		}

		protected GamePhase currentPhase = GamePhase.OPERATIONS;

		protected string fileName;
		public string FileName
		{
			get
			{
				return fileName;
			}
		}

		protected string tempDirName;
		protected string theRoundOneFilesDir;

		protected bool _allowSave = true;
		protected bool _allowWriteToDisk = true;

		protected em_GameEvalType _evaltype = em_GameEvalType.ITIL;

		public bool IsSalesGame
		{
			get
			{
				return ! _allowWriteToDisk;
			}
		}

		public bool IsTrainingGame
		{
			get
			{
				return (! _allowSave) && _allowWriteToDisk;
			}
		}

		public bool IsSalesOrTrainingGame
		{
			get
			{
				return IsSalesGame || IsTrainingGame;
			}
		}

		public bool IsNormalGame
		{
			get
			{
				return !IsSalesOrTrainingGame;
			}
		}

		public em_GameEvalType Game_Eval_Type
		{
			get
			{
				return _evaltype;
			}
			set
			{
				_evaltype = value;
			}
		}

		protected int currentRound = 0;

		Hashtable isaveClasses = new Hashtable();

		public virtual void Dispose()
		{
			if(Directory.Exists(tempDirName))
			{
				Directory.Delete(tempDirName, true);
			}
		}

		public void Delete ()
		{
			if (Directory.Exists(tempDirName))
			{
				Directory.Delete(tempDirName, true);
			}

			if (File.Exists(fileName))
			{
				File.Delete(fileName);
			}

			_allowSave = false;
			_allowWriteToDisk = false;
		}

		/// <summary>
		/// get the current round of the game
		/// </summary>
		public int CurrentRound
		{
			get
			{
				return currentRound;
			}
		}

		/// <summary>
		/// get the current phase of the game
		/// return values are OPERATIONS or TRANSITION
		/// </summary>
		public GamePhase CurrentPhase
		{
			get
			{
				return currentPhase;
			}
		}

		public int CurrentPhaseNumber
		{
			get
			{
				return RoundToPhase(CurrentRound, CurrentPhase);
			}
		}

		public string Dir
		{
			get
			{
				return tempDirName;
			}
		}
		/// <summary>
		/// Get the temp directory where the round data is being written to
		/// </summary>
		public virtual string CurrentRoundDir
		{
			get
			{
                int round = currentRound;
                if (round == 0)
                {
                    round++;
                }
                
				if (CoreUtils.SkinningDefs.TheInstance.GetBoolData("uses_transition_rounds", true))
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

		/// <summary>
		/// Create a new game file with specific files defining initial state.
		/// </summary>
		/// <param name="filename"></param>
		/// <param name="roundOneFilesDir"></param>
		/// <param name="isNew"></param>
		public GameFile(string filename, string roundOneFilesDir, bool isNew,
			bool allowSave, bool allowWriteToDisk, IGameLicence licence)
		{
			Setup(filename, roundOneFilesDir, isNew, allowSave, allowWriteToDisk, licence);
			Id = Guid.NewGuid();
		}

		protected virtual void Setup (string filename, string roundOneFilesDir, bool isNew, 
			bool allowSave, bool allowWriteToDisk, IGameLicence licence)
		{
			this.licence = licence;

			phaseNumbersPlayed = new List<int> ();
			_allowWriteToDisk = allowWriteToDisk;
			_allowSave = allowSave;
			theRoundOneFilesDir = roundOneFilesDir;
			fileName = filename;
			tempDirName = Path.GetTempFileName();
			File.Delete(tempDirName);
			DirectoryInfo di = Directory.CreateDirectory(tempDirName);
			//
			if(isNew)
			{
                CopyDirContents(roundOneFilesDir, this.CurrentRoundDir);
				CreateBaseDirs();
			}
			else
			{
				// Unzip the game file to our temp dir.
				ConfPack cp = new ConfPack();
				cp.ExtractAllFilesFromZip(filename,tempDirName, "");

				SetEvaluationTypeFromDetails();
			}
		}

		protected virtual void SetEvaluationTypeFromDetails ()
		{
			BasicXmlDocument xml = BasicXmlDocument.CreateFromFile(tempDirName + @"\global\details.xml");

			bool existed;
			string gameType = CoreUtils.XMLUtils.GetElementStringWithCheck(xml.DocumentElement, "evaltype", out existed);

			if (existed)
			{
				Game_Eval_Type = (em_GameEvalType) Enum.Parse(typeof (em_GameEvalType), gameType, true);
			}
			else
			{
				Game_Eval_Type = (em_GameEvalType) Enum.GetValues(typeof(em_GameEvalType)).GetValue(0);
			}
		}

		public string Name
		{
			get
			{
				return fileName;
			}
		}

		/// <summary>
		/// Open an exisitng game file.
		/// </summary>
		/// <param name="filename"></param>
		public GameFile(string filename)
		{
			fileName = filename;

			try
			{
				tempDirName = Path.GetTempFileName();
				File.Delete(tempDirName);
				DirectoryInfo di = Directory.CreateDirectory(tempDirName);
				//
				ConfPack cp = new ConfPack();
				cp.ExtractAllFilesFromZip(fileName,this.tempDirName, "");
			}
			catch { }
		}

		protected void CreateBaseDirs()
		{
			string global = this.tempDirName + "\\global";

			if(!Directory.Exists(global))
			{
				Directory.CreateDirectory(global);
			}
		}

		protected void CopyDirContents(string srcdir, string destdirName)
		{
			// if destination directory exists then wipe it.
			if(Directory.Exists(destdirName))
			{
				Directory.Delete(destdirName,true);
			}
			//
			Directory.CreateDirectory(destdirName);
			CopyDir(srcdir,destdirName,false);
		}
		/// <summary>
		/// CopyDir only copies the file contents of a directory, not any sub-directories
		/// unless recursive is set.
		/// </summary>
		/// <param name="src">Source Directory.</param>
		/// <param name="dest">Destination Directory.</param>
		/// <param name="recursive">Sets whether to recursively sopy sub-directories.</param>
		protected void CopyDir(string src, string dest, bool recursive)
		{
			string[] files = Directory.GetFiles(src);
			foreach(string f in files)
			{
				File.Copy(f,dest + "\\" + Path.GetFileName(f));
			}
			//
			if(recursive)
			{
				string[] dirs = Directory.GetDirectories(src);
				char[] dsep = { '\\' };
				//
				foreach(string d in dirs)
				{
					string[] dirTree = d.Split(dsep);
					string newDir = dest + "\\" + dirTree[ dirTree.Length - 1 ];
					Directory.CreateDirectory(newDir);
					CopyDir(d,newDir,true);
				}
			}
		}

		/// <summary>
		/// Get the full path of a given file in the game temp directory, based on a game phase
		/// </summary>
		/// <param name="round">the round</param>
		/// <param name="filename">filename to get path for</param>
		/// <param name="phase">a game phase (OPERATIONS or TRANSITION)</param>
		/// <returns></returns>
		public virtual string GetRoundFile(int round, string filename, GamePhase phase)
		{
            if (CoreUtils.SkinningDefs.TheInstance.GetBoolData("uses_transition_rounds", true))
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
                return tempDirName + @"\round" + CONVERT.ToStr(currentRound);
            }
		}

		public int GetNumRounds()
		{
			SortedList sl = new SortedList();
			string[] dirs = Directory.GetDirectories(tempDirName);
			foreach(string s in dirs)
			{
				if(s.StartsWith("round"))
				{
					sl.Add(s,1);
				}
			}
			//
			return sl.Count;
		}

		public void AddDirNames(ArrayList array, string dir)
		{
			string[] dirs = Directory.GetDirectories(dir);
			foreach(string d in dirs)
			{
				array.Add(d);
				AddDirNames(array,d);
			}
		}

		public virtual void Rename(string newName)
		{
			if(this._allowSave)
			{
				Save(true);
				File.Move(fileName, newName);
				fileName = newName;
			}
		}

		public virtual string GetGlobalFile (string filename)
		{
			return tempDirName + @"\global\" + filename;
		}

		public string LicenceGuidFilename => GetGlobalFile("licence.txt");

		protected string PhaseLogFilename => GetGlobalFile("phases_played.xml");

		void SavePhasePlayLog ()
		{
			var xml = BasicXmlDocument.Create();
			var root = xml.AppendNewChild("Phases");
			root.AppendAttribute("current_phase", CurrentPhaseNumber);

			var played = root.AppendNewChild("PhasesPlayed");
			foreach (var playedPhase in phaseNumbersPlayed)
			{
				var entry = played.AppendNewChild("PhasePlayed");
				entry.AppendAttribute("number", playedPhase);
			}

			xml.Save(PhaseLogFilename);
		}

		public virtual void Save (bool fullSave, bool markAsUploaded = false)
		{
			if(_allowWriteToDisk)
			{
				SetGlobalOption("file_uploaded", markAsUploaded);

				if (licence != null)
				{
					File.WriteAllText(LicenceGuidFilename, licence.Id.ToString());
				}

				SavePhasePlayLog();

				foreach(ISave c in isaveClasses.Keys)
				{
					string filename = (string) isaveClasses[c];
					c.SaveToURL("", filename);
				}

				if(fullSave && _allowSave)
				{
					ArrayList aDirs = new ArrayList();
					//aDirs.Add(tempDirName);
					AddDirNames(aDirs,tempDirName);
					//
					string[] dirs = (string[]) aDirs.ToArray( typeof(string) );
					ConfPack cp = new ConfPack();
					cp.CreateZip(fileName, dirs, false, "");
				}
			}
		}

		public void AddISaver(ISave isClass, string filename)
		{
			isaveClasses.Add(isClass,filename);
		}

		public void RemoveISaver(ISave isClass)
		{
			isaveClasses.Remove(isClass);
		}

		public bool DoesGlobalOptionExist(string optionName)
		{
			return (GetStringGlobalOption(optionName) != null);
		}

		public bool GetBoolGlobalOption (string optionName, bool defaultValue)
		{
			return GetBoolGlobalOption(optionName) ?? defaultValue;
		}

		public Guid? GetGuidGlobalOption (string optionName)
		{
			var option = GetStringGlobalOption(optionName);
			if (Guid.TryParse(option, out var guid))
			{
				return guid;
			}
			else
			{
				return null;
			}
		}

		public Guid GetGuidGlobalOption (string optionName, Guid defaultValue)
		{
			return GetGuidGlobalOption(optionName) ?? defaultValue;
		}

		public bool? GetBoolGlobalOption (string optionName)
		{
			return CONVERT.ParseBool(GetStringGlobalOption(optionName));
		}

		public string GetStringGlobalOption (string optionName)
		{
			if (File.Exists(GetGlobalOptionsFileName()))
			{
				BasicXmlDocument xml = BasicXmlDocument.CreateFromFile(GetGlobalOptionsFileName());

				foreach (XmlNode child in xml.DocumentElement.ChildNodes)
				{
					if ((child is XmlElement) && (child.Name == optionName))
					{
						string attributeValue = BasicXmlDocument.GetStringAttribute(child, "value");
						if (attributeValue != null)
						{
							return attributeValue;
						}
					}
				}
			}

			return null;
		}

		public void SetGlobalOption (string optionName, bool value)
		{
			SetGlobalOption(optionName, CONVERT.ToStr(value));
		}

		public void SetGlobalOption (string optionName, Guid value)
		{
			SetGlobalOption(optionName, value.ToString());
		}

		public void SetGlobalOption (string optionName, string value)
		{
			BasicXmlDocument xml;

			if (File.Exists(GetGlobalOptionsFileName()))
			{
				xml = BasicXmlDocument.CreateFromFile(GetGlobalOptionsFileName());
			}
			else
			{
				xml = BasicXmlDocument.Create();
				xml.AppendNewChild("options");
			}

			XmlElement optionElement = null;
			foreach (XmlNode child in xml.DocumentElement.ChildNodes)
			{
				if ((child is XmlElement) && (child.Name == optionName))
				{
					optionElement = (XmlElement) child;
					break;
				}
			}

			if (optionElement == null)
			{
				optionElement = xml.AppendNewChild(xml.DocumentElement, optionName);
			}

			if (optionElement.Attributes["value"] == null)
			{
				BasicXmlDocument.AppendAttribute(optionElement, "value", value);
			}
			else
			{
				optionElement.Attributes["value"].Value = value;
			}

			xml.Save(GetGlobalOptionsFileName());
		}

		string GetGlobalOptionsFileName ()
		{
			return Dir + @"\global\options.xml";
		}

		public int GetTotalRounds ()
		{
			return CoreUtils.SkinningDefs.TheInstance.GetIntData("roundcount", 5);
		}

        public int GetRaceRoundLengthMins (int defaultRoundLenght)
        {
            bool? isShortGame = GetBoolGlobalOption("isShortGame");

            if (isShortGame == null)
            {
                return defaultRoundLenght;
            }

            if ((bool)isShortGame)
            {
                return 10;
            }
            else
            {
                return 20;
            }
        }

		public override string ToString ()
		{
			return CONVERT.Format("{0} ({1})", FileName, tempDirName);
		}

		public virtual string GetPurpose ()
		{
			if (File.Exists(Dir + @"\global\details.xml"))
			{
				BasicXmlDocument xml = BasicXmlDocument.CreateFromFile(Dir + @"\global\details.xml");
				return XMLUtils.GetOrCreateElement(xml.DocumentElement, "Purpose").InnerText;
			}
			return "";
		}

		public int GetPlayers ()
		{
			if (File.Exists(Dir + @"\global\details.xml"))
			{
				BasicXmlDocument xml = BasicXmlDocument.CreateFromFile(Dir + @"\global\details.xml");
				return CONVERT.ParseIntSafe(XMLUtils.GetOrCreateElement(xml.DocumentElement, "Players").InnerText, 0);
			}
			return 0;
		}

		public virtual string GetTitle ()
		{
			if (File.Exists(Dir + @"\global\details.xml"))
			{
				BasicXmlDocument xml = BasicXmlDocument.CreateFromFile(Dir + @"\global\details.xml");
				return XMLUtils.GetOrCreateElement(xml.DocumentElement, "Title").InnerText;
			}
			return "";
		}

		public virtual string GetVenue ()
		{
			if (File.Exists(Dir + @"\global\details.xml"))
			{
				BasicXmlDocument xml = BasicXmlDocument.CreateFromFile(Dir + @"\global\details.xml");

				return XMLUtils.GetOrCreateElement(xml.DocumentElement, "Venue").InnerText;
			}
			return "";
		}

		public virtual string GetClient ()
		{
			if (File.Exists(Dir + @"\global\details.xml"))
			{
				var xml = BasicXmlDocument.CreateFromFile(Dir + @"\global\details.xml");

				return XMLUtils.GetOrCreateElement(xml.DocumentElement, "Client").InnerText;
			}

			return "";
		}

		public virtual void GetTeamMembersAndRoles (string[] members, string[] roles)
		{
			string filename = Dir + @"\global\team.xml";

			if (File.Exists(filename))
			{
				BasicXmlDocument xml = BasicXmlDocument.CreateFromFile(filename);

				int i = 0;

				XmlElement membersNode = (XmlElement) xml.DocumentElement.SelectSingleNode("members");
				foreach (XmlElement child in membersNode.ChildNodes)
				{
					if (child.Name == "member")
					{
						if (i < Math.Min(members.Length, roles.Length))
						{
							members[i] = XMLUtils.GetElementString(child, "name");
							roles[i] = XMLUtils.GetElementString(child, "role");
						}

						i++;
					}
				}
			}
		}

		public string GetLocation ()
		{
			if (File.Exists(Dir + @"\global\details.xml"))
			{
				var xml = BasicXmlDocument.CreateFromFile(Dir + @"\global\details.xml");

				return XMLUtils.GetOrCreateElement(xml.DocumentElement, "Location").InnerText;
			}

			return "";
		}

		public string GetRegion ()
		{
			if (File.Exists(Dir + @"\global\details.xml"))
			{
				var xml = BasicXmlDocument.CreateFromFile(Dir + @"\global\details.xml");

				return XMLUtils.GetOrCreateElement(xml.DocumentElement, "GeoRegion").InnerText;
			}

			return "";
		}

		public string GetCountry ()
		{
			if (File.Exists(Dir + @"\global\details.xml"))
			{
				var xml = BasicXmlDocument.CreateFromFile(Dir + @"\global\details.xml");

				return XMLUtils.GetOrCreateElement(xml.DocumentElement, "Country").InnerText;
			}

			return "";
		}

		public string GetChargeCompany ()
		{
			if (File.Exists(Dir + @"\global\details.xml"))
			{
				var xml = BasicXmlDocument.CreateFromFile(Dir + @"\global\details.xml");

				return XMLUtils.GetOrCreateElement(xml.DocumentElement, "ChargeCompany").InnerText;
			}

			return "";
		}

		public string GetNotes ()
		{
			if (File.Exists(Dir + @"\global\details.xml"))
			{
				var xml = BasicXmlDocument.CreateFromFile(Dir + @"\global\details.xml");

				return XMLUtils.GetOrCreateElement(xml.DocumentElement, "Notes").InnerText;
			}

			return "";
		}

		public int LastPhaseNumberPlayed
		{
			get
			{
				int lastPhasePlayed = -1;
				if (phaseNumbersPlayed.Count > 0)
				{
					lastPhasePlayed = phaseNumbersPlayed.Max();
				}

				return lastPhasePlayed;
			}
		}

		public GamePhase LastPhasePlayed
		{
			get
			{
				int round;
				GamePhase gamePhase;
				PhaseToRound(LastPhaseNumberPlayed, out round, out gamePhase);
				return gamePhase;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public int LastRoundPlayed
		{
			get
			{
				int round;
				GamePhase gamePhase;
				PhaseToRound(LastPhaseNumberPlayed, out round, out gamePhase);
				return round;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="phase"></param>
		/// <param name="round"></param>
		/// <param name="gamePhase"></param>
		public virtual void PhaseToRound (int phase, out int round, out GamePhase gamePhase)
		{
			round = 1;
			gamePhase = GamePhase.OPERATIONS;

			if (GameTypeUsesTransitions())
			{
				switch (phase)
				{
					case 0:
						round = 1;
						gamePhase = GamePhase.OPERATIONS;
						break;

					case 1:
						round = 2;
						gamePhase = GamePhase.TRANSITION;
						break;

					case 2:
						round = 2;
						gamePhase = GamePhase.OPERATIONS;
						break;

					case 3:
						round = 3;
						gamePhase = GamePhase.TRANSITION;
						break;

					case 4:
						round = 3;
						gamePhase = GamePhase.OPERATIONS;
						break;

					case 5:
						round = 4;
						gamePhase = GamePhase.TRANSITION;
						break;

					case 6:
						round = 4;
						gamePhase = GamePhase.OPERATIONS;
						break;

					case 7:
						round = 5;
						gamePhase = GamePhase.TRANSITION;
						break;

					case 8:
						round = 5;
						gamePhase = GamePhase.OPERATIONS;
						break;
				}
			}
			else
			{
				round = 1 + phase;
				gamePhase = GamePhase.OPERATIONS;
			}
		}

		public virtual bool GameTypeUsesTransitions ()
		{
			return SkinningDefs.TheInstance.GetBoolData("uses_transition_rounds", true);
		}
	}
}