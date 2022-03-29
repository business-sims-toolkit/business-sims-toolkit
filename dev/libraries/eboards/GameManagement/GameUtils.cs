using System;
using LibCore;

namespace GameManagement
{
	/// <summary>
	/// 
	/// </summary>
	public enum em_GameEvalType
	{
		UNDEFINED,
		CUSTOM,
		ITIL,
		ISO_20K,
		MOF,
		LEAN,
		PMBOK,
		PRINCE2,
        ESM
	}

	/// <summary>
	/// Summary description for GameUtils.
	/// </summary>
	public class GameUtils
	{
		public GameUtils()
		{
		}

		/// <summary>
		/// Method for getting reasonably readable file name which doesn't already exist
		/// YearMonthDay_GameName_UUID
		/// </summary>
		/// <returns></returns>
		public static void EstablishNewFileName(out string NewFileName, string GameName, out string ErrMsg) 
		{
			DateTime dt = DateTime.Now;
			EstablishNewFileName(out NewFileName, GameName, out ErrMsg, dt.Year, dt.Month, dt.Day);
		}

		/// <summary>
		/// Method for getting reasonably readable file name which doesn't already exist
		/// YearMonthDay_GameName_UUID
		/// </summary>
		/// <returns></returns>
		public static void EstablishNewFileName(out string NewFileName, string GameName, out string ErrMsg, int Year, int Month, int Day) 
		{
			ErrMsg = "";
			NewFileName = EstablishNewFileName(GameName, new DateTime (Year, Month, Day), Guid.NewGuid());
		}

		public static string EstablishNewFileName (string title, DateTime createdDate, Guid guid)
		{
			return CONVERT.Format("{0}_{1}_{2}-#{3}#-{4}.gmz", createdDate.Year, createdDate.Month, createdDate.Day, EncodeGameName(title), guid);
		}

		static public string EncodeGameName(string tname)
		{
			// \ / : * ? " < > |
			string newName = "";
			foreach(char c in tname)
			{
				switch(c)
				{
					case '\\':
					case '/':
					case ':':
					case '*':
					case '?':
					case '"':
					case '<':
					case '>':
					case '|':
						newName += EscapeChar(c);
						break;

					case '_':
						newName += "__";
						break;

					default:
						newName += c;
						break;
				}
			}

			return newName;
		}

		static public string EscapeChar(char c)
		{
			int i = (int) c;
			if(i>255) return "X";
			byte b = (byte) c;

			string ec = "_";
			ec += b.ToString("X2");
			return ec;
		}

		static public string DecodeGameName(string gname)
		{
			string name = "";

			for(int i=0; i<gname.Length; ++i)
			{
				if(gname[i] == '_')
				{
					// Escaped char...
					if(i<gname.Length-1)
					{
						if(gname[i+1] == '_')
						{
							++i;
							name += "_";
						}
						else
						{
							// Encoded as HEX.
							if(i<gname.Length-2)
							{
								// Two chars needed for HEX byte (char).
								string temp = gname.Substring(i+1,2);
								int discarded;
								byte[] b = CoreUtils.HexEncoding.GetBytes(temp,out discarded);
								if(b.Length > 0)
								{
									name += (char) b[0];
								}
								//
								i+=2;
							}
						}
					}
				}
				else
				{
					name += gname[i];
				}
			}

			//if(name.Length == 0) name = "X";
			return name;
		}

		static public string FileNameToGameName(string fileName, out string firstPart, out string lastPart)
		{
			int firstCut = fileName.IndexOf("-#");
			int lastCut = fileName.LastIndexOf("#-");
			//
			if(firstCut < 0)
			{
				firstPart = "";
				firstCut = 0;
			}
			else
			{
				firstCut += 2;
				firstPart = fileName.Substring(0,firstCut);
			}
			//
			if(lastCut < 0)
			{
				lastPart = "";
				lastCut = fileName.Length;
			}
			else
			{
				lastPart = fileName.Substring(lastCut);
			}
			//
			string gname = fileName.Substring(firstCut,lastCut-firstCut);
			//
			return DecodeGameName(gname);
		}

		static public DateTime FileNameToCreationDate (string FileName)
		{
			// Default values to return if we have a problem parsing the filename.
			DateTime now = DateTime.Now;
			int year = now.Year;
			int month = now.Month;
			int day = now.Day;

			try
			{
				string firstPart;
				string lastPart;

				string shortname = FileNameToGameName(FileName, out firstPart, out lastPart);
				// Remove spurious end tags...
				firstPart = firstPart.Replace("-#","");
				char[] dash = { '_' };
				string[] dateParts = firstPart.Split(dash);
				year = CONVERT.ParseIntSafe(dateParts[0], year);
				month = CONVERT.ParseIntSafe(dateParts[1], month);
				day = CONVERT.ParseIntSafe(dateParts[2], day);
			}
			catch
			{
			}

			return new DateTime(year, month, day);
		}
	}
}
