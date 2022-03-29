using System;
using System.Collections.Generic;

namespace Polestar_PM.DataLookup
{
	public enum emWorkTimeMode
	{
		NORMAL = 8,
		OVERTIME = 12
	}	

	public enum emProjectRunningState
	{
		NOT_PAUSED = 0,			//Normal Project Running
		PAUSE_FIREALL = 1,	//Starting Pause, need to fire all people 
		PAUSED = 2,					//Paused 
		PAUSE_RESTART = 3,	//Leaving Pause Restarting, need to hire all people 
	}	
	
	public enum emPHASE
	{
		DEV  = 0,
		TEST = 11 
	}

	public enum emPHASE_STAGE
	{
		STAGE_A = 0,  //Normal Path 
		STAGE_B = 1,  //Normal Path 
		STAGE_C = 2,  //Normal Path 
		STAGE_D = 3,  //Normal Path 
		STAGE_E = 4,  //Normal Path 
		STAGE_F = 5,  //Normal Path 
		STAGE_G = 6,  //Normal Path 
		STAGE_H = 7,	//Normal Path 

		STAGE_I = 8,	//Recycle Path 1
		STAGE_J = 9,	//Recycle Path 1
		STAGE_K = 10,	//Recycle Path 2
		STAGE_L = 11,	//Recycle Path 2
		STAGE_M = 12,	//Recycle Path 3
		STAGE_N = 13,	//Recycle Path 3
		STAGE_P = 14,	//Recycle Path 4
		STAGE_Q = 15,	//Recycle Path 4
		STAGE_R = 16,	//Recycle Path 5
		STAGE_T = 17,	//Recycle Path 5
	}

	public enum emProjectType
	{
		OPTIONAL,
		REGULATION
	}

	/// <summary>
	/// Used to provide a Set of Selectors for Retrieving Data
	/// </summary>
	public enum emProjectOperationalState
	{
		PROJECT_STATE_EMPTY = 0,
		PROJECT_STATE_PROJECTSELECTED = 1,
		PROJECT_STATE_PRODUCTSELECTED = 2,
		PROJECT_STATE_A = 3,		//DELEVOPING_A
		PROJECT_STATE_B = 4,		//DEVELOPING_B		 
		PROJECT_STATE_C = 5,		//DEVELOPING_C
		PROJECT_STATE_D = 6,		//DEVELOPING_D
		PROJECT_STATE_E = 7,		//DEVELOPING_E
		PROJECT_STATE_F = 8,		//TESTING_A
		PROJECT_STATE_G = 9,		//TESTING_B
		PROJECT_STATE_H = 10,		//TESTING_C
		PROJECT_STATE_IN_HANDOVER = 11,

		PROJECT_STATE_I = 12,		//DEVELOPING_I  REBUILD 1
		PROJECT_STATE_J = 13,		//TESTING_J     RETEST 1
		PROJECT_STATE_K = 14,		//DEVELOPING_K  REBUILD 2
		PROJECT_STATE_L = 15,		//TESTING_L     RETEST 2
		PROJECT_STATE_M = 16,		//DEVELOPING_M  REBUILD 3
		PROJECT_STATE_N = 17,		//TESTING_N     RETEST 3
		PROJECT_STATE_P = 18,		//DEVELOPING_P  REBUILD 4
		PROJECT_STATE_Q = 19,		//TESTING_Q     RETEST 4
		PROJECT_STATE_R = 20,		//DEVELOPING_R  REBUILD 5
		PROJECT_STATE_T = 21,		//TESTING_T     RETEST 5

		PROJECT_STATE_HANDOVER_COMPLETED = 22,
		PROJECT_STATE_INSTALL_NO_LOCATION = 23,
		PROJECT_STATE_PRE_INSTALLING = 24,
		PROJECT_STATE_INSTALLING = 25,
		PROJECT_STATE_INSTALLED_OK = 26,
		PROJECT_STATE_INSTALLED_FAIL = 27,
		PROJECT_STATE_COMPLETED = 28,
		PROJECT_STATE_CANCELLED = 29,
		PROJECT_STATE_UNKNOWN = 30,

		PROJECT_STATE_STALLED_NO_MONEY = 31,
		PROJECT_STATE_PRE_RECYCLE = 32
	}	

	/// <summary>
	/// Just a centralised Holder for all the constants in the 
	/// </summary>
	public static class GameConstants
	{
		public const int MAX_NUMBER_SLOTS = 7;												//Displayed Projects
		public const int MAX_NUMBER_CANCELLED_SLOTS = 20;							//Maximum of 20 cancelled projects
		public const int MAX_NUMBER_DAYS = 25;												//Number of Days in the round
		public const int MAX_ROUNDS = 3;															//Maximum Number of Rounds
		public const int MAX_RECYCLE = 5;															//Maximum Number of ReCycles

		public const int GAME_TICK = 1;															  //Tick at 1 per second
		public const int GAME_MAX_MIN = 25;														//Maximum Day 
		public const int GAME_MAX_DAY = 25;														//Maximum Day 
		public const int GAME_LIMIT_TIME = GAME_MAX_MIN*60*GAME_TICK;	//Game limit in Secs
		public const int GAME_LIMIT_TICK = GAME_MAX_MIN*60*GAME_TICK;	//Game limit in Ticks
		public const int GAME_TICK_MIN = 60*GAME_TICK;								//Ticks Per min

		public const int RACE_LEADERBOARD = 20;												//Number of Names in the LeaderBoard
		public const int USE_TEAM_EFFECT_ROUND = 3;										//Invoke the team effect in this round and later rounds
		public const int MAX_SERVER_SLOTS = 22;												//NUMBER of SERVERS

		public const string ACTIVITY_PROJECT_SELECT = "1";
		public const string ACTIVITY_PRODUCT_SELECT = "2";
		public const string ACTIVITY_DEV = "3";
		public const string ACTIVITY_DEV_A_WORK = "3A";
		public const string ACTIVITY_DEV_B_WORK = "3B";
		public const string ACTIVITY_DEV_C_WORK = "3C";
		public const string ACTIVITY_DEV_D_WORK = "3D";
		public const string ACTIVITY_DEV_E_WORK = "3E";
		public const string ACTIVITY_TEST = "4";
		public const string ACTIVITY_TEST_A_WORK = "4F";
		public const string ACTIVITY_TEST_B_WORK = "4G";
		public const string ACTIVITY_TEST_C_WORK = "4H";
		public const string ACTIVITY_TEST_D_WORK = "4G";
		public const string ACTIVITY_TEST_E_WORK = "4H";
		//public const string ACTIVITY_ACCEPT = "5";
		public const string ACTIVITY_HANDOVER = "5";
		public const string ACTIVITY_INSTALLING = "6";
		public const string ACTIVITY_INSTALL_FAIL = "7";
		public const string ACTIVITY_INSTALLED = "8";
		public const string ACTIVITY_COMPLETE = "9";
		public const string ACTIVITY_BUYIN = "P";			//P for purchase rather than 
		public const string ACTIVITY_RECYCLE = "I";		
		public const string ACTIVITY_NOMONEY = "$";	
	
		public const string SAVE_DT_STR = "20000101";

		private static Dictionary<emProjectOperationalState, string> BuildStateMappings ()
		{
			Dictionary<emProjectOperationalState, string> stateToShortCode = new Dictionary<emProjectOperationalState, string>();

			stateToShortCode.Add(emProjectOperationalState.PROJECT_STATE_UNKNOWN, "");
			stateToShortCode.Add(emProjectOperationalState.PROJECT_STATE_PROJECTSELECTED, ACTIVITY_PROJECT_SELECT);
			stateToShortCode.Add(emProjectOperationalState.PROJECT_STATE_PRODUCTSELECTED, ACTIVITY_PRODUCT_SELECT);
			stateToShortCode.Add(emProjectOperationalState.PROJECT_STATE_A, ACTIVITY_DEV_A_WORK);
			stateToShortCode.Add(emProjectOperationalState.PROJECT_STATE_B, ACTIVITY_DEV_B_WORK);
			stateToShortCode.Add(emProjectOperationalState.PROJECT_STATE_C, ACTIVITY_DEV_C_WORK);
			stateToShortCode.Add(emProjectOperationalState.PROJECT_STATE_D, ACTIVITY_DEV_D_WORK);
			stateToShortCode.Add(emProjectOperationalState.PROJECT_STATE_E, ACTIVITY_DEV_E_WORK);
			stateToShortCode.Add(emProjectOperationalState.PROJECT_STATE_F, ACTIVITY_TEST_A_WORK);
			stateToShortCode.Add(emProjectOperationalState.PROJECT_STATE_G, ACTIVITY_TEST_B_WORK);
			stateToShortCode.Add(emProjectOperationalState.PROJECT_STATE_H, ACTIVITY_TEST_C_WORK);
			stateToShortCode.Add(emProjectOperationalState.PROJECT_STATE_I, ACTIVITY_DEV_E_WORK);
			stateToShortCode.Add(emProjectOperationalState.PROJECT_STATE_J, ACTIVITY_TEST_C_WORK);
			stateToShortCode.Add(emProjectOperationalState.PROJECT_STATE_K, ACTIVITY_DEV_E_WORK);
			stateToShortCode.Add(emProjectOperationalState.PROJECT_STATE_L, ACTIVITY_TEST_C_WORK);
			stateToShortCode.Add(emProjectOperationalState.PROJECT_STATE_M, ACTIVITY_DEV_E_WORK);
			stateToShortCode.Add(emProjectOperationalState.PROJECT_STATE_N, ACTIVITY_TEST_C_WORK);
			stateToShortCode.Add(emProjectOperationalState.PROJECT_STATE_P, ACTIVITY_DEV_E_WORK);
			stateToShortCode.Add(emProjectOperationalState.PROJECT_STATE_Q, ACTIVITY_TEST_C_WORK);
			stateToShortCode.Add(emProjectOperationalState.PROJECT_STATE_R, ACTIVITY_DEV_E_WORK);
			stateToShortCode.Add(emProjectOperationalState.PROJECT_STATE_T, ACTIVITY_TEST_C_WORK);
			stateToShortCode.Add(emProjectOperationalState.PROJECT_STATE_PRE_INSTALLING, ACTIVITY_HANDOVER);
			stateToShortCode.Add(emProjectOperationalState.PROJECT_STATE_IN_HANDOVER, ACTIVITY_HANDOVER);
			stateToShortCode.Add(emProjectOperationalState.PROJECT_STATE_HANDOVER_COMPLETED, ACTIVITY_HANDOVER);
			stateToShortCode.Add(emProjectOperationalState.PROJECT_STATE_INSTALLING, ACTIVITY_INSTALLING);
			stateToShortCode.Add(emProjectOperationalState.PROJECT_STATE_INSTALLED_FAIL, ACTIVITY_INSTALL_FAIL);
			stateToShortCode.Add(emProjectOperationalState.PROJECT_STATE_INSTALLED_OK, ACTIVITY_INSTALLED);
			stateToShortCode.Add(emProjectOperationalState.PROJECT_STATE_STALLED_NO_MONEY, ACTIVITY_NOMONEY);
			stateToShortCode.Add(emProjectOperationalState.PROJECT_STATE_COMPLETED, ACTIVITY_COMPLETE);

			return stateToShortCode;
		}

		public static emProjectOperationalState ProjectStateFromShortCode (string code)
		{
			Dictionary<emProjectOperationalState, string> stateToShortCode = BuildStateMappings();

			foreach (emProjectOperationalState key in stateToShortCode.Keys)
			{
				if (stateToShortCode[key] == code)
				{
					return key;
				}
			}

			throw new ApplicationException ("Unable to find a mapping from project activity code '" + code + "' to a member of emProjectOperationalState.");
		}

		public static string ProjectShortCodeFromState (emProjectOperationalState state)
		{
			Dictionary<emProjectOperationalState, string> stateToShortCode = BuildStateMappings();

			if (stateToShortCode.ContainsKey(state))
			{
				return stateToShortCode[state];
			}

			return "";
		}

		public static emProjectOperationalState ProjectStateFromStageName (string stageString)
		{
			return (emProjectOperationalState) Enum.Parse(typeof(emProjectOperationalState), "PROJECT_STATE_" + stageString.ToUpper());
		}

		public static string [] GetAllStageNames()
		{
			return new string [] { "A", "B", "C", "D", "E", "F", "G", "H" };
		}
	}
}