using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Xml;
using System.Xml.Serialization;

using GameManagement;
using LibCore;
using Logging;
using Network;

using CoreUtils;

namespace ReportBuilder
{

	public class pt
	{
		public float X;
		public float Y;
			
		public pt()
		{
		}

		public pt(float x, float y)
		{
			X=x;
			Y=y;
		}
	}

	public class CarInfo
	{
		public string driver;
		public string team;
		public string lagd;
		public string Catch;
		public string car;
		public string pos;
	}

	/// <summary>
	/// Summary description for RoundScores.
	/// </summary>
	public class RoundScores
	{
        public int Round
        {
            get
            {
                return _round;
            }
        }

		//make protected and get functions, but no time just now
		public double IncidentDownTimeIncludingFacilities;
		public int NumAppConsultancyFixes;
		public int NumServerConsultancyFixes;
		public int NumFixedCosts;
		public int NumWorkarounds;
		public int NumServerUpgrades;
		public int NumAppUpgrades;
		//
		public int Mirror1;
		public int Mirror2;
		//
		public int SupportCostsTotal;
		public int SupportFinesTotal;
		public int Points;
		public int Revenue;
		public int FixedCosts;
		public int Profit;
		public int AOSE_Profit;
		public int ProjectSpend;
		public int Gain;
		public double Availability;
		public int Incidents;
		public int ComplianceIncidents;
		public double MTTR;
		public float IndicatorScore = 0;
		public int PreventedIncidents;
		public int IncidentsPreventedByUpgrades;
		public int RecurringIncidents;
		public int SupportProfit;
		public int SupportBudget;
		public int NumMemUpgrades;
		public int NumStorageUpgrades;
		public Hashtable cars;
		public bool AdvancedWarningEnabled;
		public ArrayList MaturityHashOrder;
		public Hashtable SectionOrder;
		public int NumSLAbreaches;
		public int RegulationFines;
		public int ComplianceFines;
        public int ContinuousFines;
	    public int StockLevelFines
        {
	        get
            {
                return stockLevelsBreached ? _rep.GetCost("stock_level_fine", _round) : 0;
            }
	    }
	    bool stockLevelsBreached;

        public int NumServices;
		public int NumServicesBeforeRace;
		public int NumNewServices;
		public double FinalTime;
		public Hashtable outer_sections;
		public Hashtable inner_sections;
		public Dictionary<string, Color> sectionNameToColour;
        public int ConsultancyCosts;

		System.Collections.Generic.List<string> failedAuditNames;
		public int FailedAudits
		{
			get
			{
				return failedAuditNames.Count;
			}
		}

		public int FradulentTransactionsApproved;

		//POLESTAR
		public int TargetTransactions;
		public int NumTransactions;
		public int MaxTransactions;
		public int MaxRevenue;

	    public struct Transaction
	    {
	        public string Channel;
	        public int PotentialValue;
	        public int ActualValue;

	        public Transaction (string channel, int potentialValue, int actualValue)
	        {
	            Channel = channel;
	            PotentialValue = potentialValue;
	            ActualValue = actualValue;
	        }
	    }

	    public IList<Transaction> Transactions;

	    //SERVICENOW
        public int FirstLineFixes;

		public int AutomatedFixes;

		//POLESTAR NPO
		public int ApplicationsHandled;
		public int ApplicationsDelayed;
		public int ApplicationsMax;
		public int RoundBudget;

		public int AverageAppCostNumerator;
		public int AverageAppCostDenominator;
		public int AverageAppCost;

		public int CustomerComplaints;

		//CA
		public ArrayList AllIncidents;
		public ArrayList FixedIncidents;

		//IBM CLOUD
		public int num_active_servers;
		public double server_utilization;
		public double system_DCIE;
		public double system_OPEX;
		public double system_OPEX_ADD;
		public double system_CAPEX_NEW;
		public int lost_opportunity_cost;
		public int ibm_Profit;
		public int ibm_Gain;
		
		//CA Security
		public int total_vulnode_count = 0;
		public int open_vulnodes_count = 0;
		public int protected_vulnodes_count = 0;
		public int upgrade_vulnodes_count = 0;
		public int starting_vuln_count = 0;
		public int identified_vuln_count = 0;
		public int prevented_vuln_count = 0;
		public int fixed_vuln_count = 0;
		public int product_penalties = 0;

		//protected variables
		protected int firstspend;
		protected int lastspend;
		public double IncidentDownTime;
		protected int prevProfit;
		protected bool firsttime;
		protected Hashtable downedIncidents = new Hashtable();
		protected Hashtable incidentDurations = new Hashtable ();
		protected bool InFixItQueue;
		protected int eventsfound;
		protected int downed;
		protected bool InTransition;

		//global to class
		protected NetworkProgressionGameFile _gameFile;
		protected int _round;
		protected ReportUtils _rep;

		//
		// Two servers can be mirrored so we need to know their names and the names of their
		// mirrors.
		//
		protected string serverOneToMirror = "";
		protected string serverOneMirror = "";
		protected string serverTwoToMirror = "";
		protected string serverTwoMirror = "";

		public SupportSpendOverrides ov;

		public int FixedCostsSaaS;
		public int NumFixedCostsSaaS;

	    List<string> mirrorableServerNames;

	    public IList<string> MirrorableServerNames
        {
	        get
            {
                return mirrorableServerNames;
            }
	    }

	    void LoadMirrorDefs ()
	    {
            mirrorableServerNames = new List<string> ();

	        var mirrorFile = AppInfo.TheInstance.Location + @"\data\mirror_defs.xml";
	        if (File.Exists(mirrorFile))
	        {
	            var xml = BasicXmlDocument.CreateFromFile(mirrorFile);
	            foreach (var child in xml.DocumentElement.ChildNodes)
	            {
	                var element = child as XmlElement;
	                if (element != null)
	                {
                        var targetNode = element.SelectSingleNode("target").InnerText;
	                    mirrorableServerNames.Add(targetNode);
	                }
	            }

                serverOneToMirror = mirrorableServerNames[0];
	            serverOneMirror = serverOneToMirror + "(M)";
                serverTwoToMirror = mirrorableServerNames[1];
	            serverTwoMirror = serverTwoToMirror + "(M)";
	        }
	    }

	    public RoundScores(NetworkProgressionGameFile gameFile, int round, int prevprofit, int NewServices, SupportSpendOverrides _ov)
		{
			ov = _ov;
			_gameFile = gameFile;
			_round = round;

		    LoadMirrorDefs();

			_rep = new ReportUtils(_gameFile);

		    Transactions = new List<Transaction> ();

			failedAuditNames = new System.Collections.Generic.List<string> ();

			//DateTime DT_A1 = DateTime.Now;
			ResetScores();
			//DateTime DT_A2 = DateTime.Now;

			prevProfit = prevprofit;
			NumNewServices = NewServices;

			//DateTime DT_B1 = DateTime.Now;
			ReadData();
			//DateTime DT_B2 = DateTime.Now;
			GetSupportCostOverrides();
			CalcSupportCosts();
			
			//DebugTime("RS ResetScores ("+CONVERT.ToStr(round)+")", DT_A1, DT_A2);
			//DebugTime("RS ReadData    ("+CONVERT.ToStr(round)+")", DT_B1, DT_B2);
		}

		public RoundScores()
		{
		}

		void DebugTime(string desc, DateTime First, DateTime Last)
		{
			long diff = Last.Ticks - First.Ticks;
			TimeSpan ts = new TimeSpan(diff);
			double tt = ts.TotalMilliseconds;
			System.Diagnostics.Debug.WriteLine(desc + "   Milli  " + CONVERT.ToPaddedStr(tt, 3));
		}

		protected virtual void ResetScores()
		{
			Points = 0;
			Revenue = 0;
			Incidents = 0;
			Profit = 0;
			ProjectSpend = 0;
			firsttime = true;
			firstspend = 0;
			lastspend = 0;
			FixedCosts = 0;
			Gain = 0;
			Availability = 0;
			MTTR = 0;
			PreventedIncidents = 0;
			IncidentsPreventedByUpgrades = 0;
			num_active_servers = 0;
			server_utilization = 0;
			system_DCIE = 0;
			system_OPEX = 0;
			system_OPEX_ADD = 0;
			system_CAPEX_NEW = 0;
			lost_opportunity_cost = 0;
			ibm_Profit = 0;
			ibm_Gain = 0;
            FirstLineFixes = 0;
			AutomatedFixes = 0;

			ApplicationsHandled = 0;
			ApplicationsDelayed = 0;
			ApplicationsMax = 0;
			RoundBudget = 0;

			outer_sections = new Hashtable();
			inner_sections = new Hashtable();
			MaturityHashOrder = new ArrayList();
			SectionOrder = new Hashtable();

			//support costs
			NumWorkarounds = 0;
			AdvancedWarningEnabled = false;
			SupportCostsTotal = 0;
			NumServerUpgrades = 0;
			NumAppUpgrades = 0;
			InFixItQueue = false;
			eventsfound = 0;
			NumAppConsultancyFixes = 0;
			NumServerConsultancyFixes = 0;
			NumFixedCosts = 0;
			NumWorkarounds = 0;
			Mirror2 = 0;
			Mirror1 = 0;
			NumStorageUpgrades = 0;
			NumMemUpgrades = 0;
			RecurringIncidents = 0;
			NumSLAbreaches = 0;
			RegulationFines = 0;
			ComplianceFines = 0;
            ContinuousFines = 0;

		    stockLevelsBreached = false;

			downed = 0;
			//
			SupportProfit = 0;
			SupportBudget = 0;
			product_penalties = 0;

			//
			cars = new Hashtable();

			NumServices = 0;
			NumServicesBeforeRace = 0;

			InTransition = false;

			AllIncidents = new ArrayList();
			FixedIncidents = new ArrayList();
		}

		protected virtual void GetSupportCostOverrides()
		{
			string val = "";

			//mirror 1 (Suzuka)
			string mirror1 = "Server " + CoreUtils.SkinningDefs.TheInstance.GetData("mirrored_server1");
			if (ov.GetOverride(_round, mirror1, out val))
			{
				ov.SetOriginalValue(_round, mirror1, CONVERT.ToStr(Mirror1) );

				//need to ensure this value can only be 0 or 1
				int tmp = CONVERT.ParseInt(val);
				if (tmp <= 0)
				{
					Mirror1 = 0;
				}
				else
				{
					Mirror1 = 1;
				}
			}
			//mirror 2 (Monaco)
			string mirror2 = "Server " + CoreUtils.SkinningDefs.TheInstance.GetData("mirrored_server2");
			if (ov.GetOverride(_round, mirror2, out val))
			{
				ov.SetOriginalValue(_round, mirror2, CONVERT.ToStr(Mirror2) );

				//need to ensure this value can only be 0 or 1
				int tmp = CONVERT.ParseInt(val);
				if (tmp <= 0)
				{
					Mirror2 = 0;
				}
				else
				{
					Mirror2 = 1;
				}
			}

			//app upgrades
			if (ov.GetOverride(_round, "Application Upgrades", out val))
			{
				ov.SetOriginalValue(_round, "Application Upgrades", CONVERT.ToStr(NumAppUpgrades) );
				NumAppUpgrades = CONVERT.ParseInt(val);
			}

			//server upgrades
			if (ov.GetOverride(_round, "Server Upgrades", out val))
			{
				ov.SetOriginalValue(_round, "Server Upgrades", CONVERT.ToStr(NumServerUpgrades) );
				NumServerUpgrades = CONVERT.ParseInt(val);
			}
			//memory upgrades
			if (ov.GetOverride(_round, "Server Memory Upgrades", out val))
			{
				ov.SetOriginalValue(_round, "Server Memory Upgrades", CONVERT.ToStr(NumMemUpgrades) );
				NumMemUpgrades = CONVERT.ParseInt(val);
			}
			//storage upgrades
			if (ov.GetOverride(_round, "Server Storage Upgrades", out val))
			{
				ov.SetOriginalValue(_round, "Server Storage Upgrades", CONVERT.ToStr(NumStorageUpgrades) );
				NumStorageUpgrades = CONVERT.ParseInt(val);
			}
			//hardware consultancy
			if (ov.GetOverride(_round, "Consultancy - Hardware", out val))
			{
				ov.SetOriginalValue(_round, "Consultancy - Hardware", CONVERT.ToStr(NumServerConsultancyFixes) );
				NumServerConsultancyFixes = CONVERT.ParseInt(val);
			}
			//software consultancy
			if (ov.GetOverride(_round, "Consultancy - Software", out val))
			{
				ov.SetOriginalValue(_round, "Consultancy - Software", CONVERT.ToStr(NumAppConsultancyFixes) );
				NumAppConsultancyFixes = CONVERT.ParseInt(val);
			}
			//infrastructure
			if (ov.GetOverride(_round, "Infrastructure & Manpower", out val))
			{
				ov.SetOriginalValue(_round, "Infrastructure & Manpower", CONVERT.ToStr(NumFixedCosts) );
				NumFixedCosts = CONVERT.ParseInt(val);
			}
			//new services
			if (ov.GetOverride(_round, "New Service Support Costs", out val))
			{
				ov.SetOriginalValue(_round, "New Service Support Costs", CONVERT.ToStr(NumNewServices) );
				NumNewServices = CONVERT.ParseInt(val);
			}
			//workarounds
			if (ov.GetOverride(_round, "Workaround", out val))
			{
				ov.SetOriginalValue(_round, "Workaround", CONVERT.ToStr(NumWorkarounds) );
				NumWorkarounds = CONVERT.ParseInt(val);
			}
			//awt
			if (ov.GetOverride(_round, "Event Monitoring", out val))
			{
				if(AdvancedWarningEnabled)
				{
					ov.SetOriginalValue(_round, "Event Monitoring", "1" );
				}
				else
				{
					ov.SetOriginalValue(_round, "Event Monitoring", "0" );
				}

				int tmp = CONVERT.ParseInt(val);

				if (tmp == 1)
				{
					AdvancedWarningEnabled = true;
				}
				else
				{
					AdvancedWarningEnabled = false;
				}
			}
		}

		protected virtual void CalcSupportCosts()
		{
			//new services
			int cost = _rep.GetCost("newservice",_round);
			SupportCostsTotal += (cost * NumNewServices);

			//infrastructure costs
			cost = _rep.GetCost("infrastructure",_round);
			SupportCostsTotal += (cost * NumFixedCosts);

			//Advanced Warning tool
			if (AdvancedWarningEnabled == true)
			{
				cost = _rep.GetCost("eventmonitoring",_round);
				SupportCostsTotal += cost;
				//system_OPEX_ADD += (double)cost;
			}

			if (Mirror2 == 1)
			{
				cost = _rep.GetCost("mirror_"+serverTwoToMirror,_round);
				SupportCostsTotal += cost;
			}
			if (Mirror1 == 1)
			{
				cost = _rep.GetCost("mirror_"+serverOneToMirror,_round);
				SupportCostsTotal += cost;
			}

			//workarounds
			cost = _rep.GetCost("workaround",_round);
			SupportCostsTotal += (cost * NumWorkarounds);
			system_OPEX_ADD += (double)(cost * NumWorkarounds);

			//software consultancy
			cost = _rep.GetCost("consultancy_software",_round);
			SupportCostsTotal += (cost * NumAppConsultancyFixes);
			system_OPEX_ADD += (double)(cost * NumAppConsultancyFixes);
            ConsultancyCosts += (cost * NumAppConsultancyFixes);

			//hardware consultancy
			cost = _rep.GetCost("consultancy_hardware",_round);
			SupportCostsTotal += (cost * NumServerConsultancyFixes);
			system_OPEX_ADD += (double)(cost * NumServerConsultancyFixes);
            ConsultancyCosts += (cost * NumServerConsultancyFixes);

			//app upgrades
			cost = _rep.GetCost("appupgrade",_round);
			SupportCostsTotal += (cost * NumAppUpgrades);

			//server upgrades
			cost = _rep.GetCost("upgradeserver",_round);
			SupportCostsTotal += (cost * NumServerUpgrades);

			//memory upgrades
			cost = _rep.GetCost("upgrade_mem",_round);
			SupportCostsTotal += (cost * NumMemUpgrades);

			//storage upgrades
			cost = _rep.GetCost("upgrade_storage",_round);
			SupportCostsTotal += (cost * NumStorageUpgrades);

			SupportFinesTotal = _rep.GetCost("support_fines", _round);

			//finally work out support profit/loss
			SupportProfit = SupportBudget - SupportCostsTotal - SupportFinesTotal;

		}

		public virtual void SerializeNow()
		{
			string gfile = _gameFile.Dir + "\\RoundScores_" + CONVERT.ToStr(_round) + ".xml";
			TextWriter f = new StreamWriter(gfile);//,FileMode.Create);
			StringWriter sw = new StringWriter();
			XmlSerializer serializer = new XmlSerializer(typeof(RoundScores));
			serializer.Serialize(sw, this);

			//Stream s = f.Open(FileMode.Create);
			string str = sw.ToString();
			f.WriteLine(str);//Write(str.ToCharArray,0,str.Length);
			//s.Close();
			f.Close();
		}

//		public virtual static RoundScores DeSerializeNow(NetworkProgressionGameFile gameFile, int round)
//		{
//			RoundScores rs = null;
//
//			try
//			{
//				string gfile = gameFile.Dir + "\\RoundScores_" + CONVERT.ToStr(round) + ".xml";
//				Stream s = new FileStream(gfile,FileMode.Open);
//				XmlSerializer serializer = new XmlSerializer(typeof(RoundScores));
//				rs = (RoundScores)serializer.Deserialize(s);
//				s.Close();
//			}
//			catch
//			{
//				// NOP, but we must catch for trashed files.
//			}
//
//			return rs;
//		}

		protected virtual void ReadData()
		{
			incidentOccurrences = new List<IncidentOccurrence> ();

            string TransactionNodeName;
            ArrayList TransactionNode = _gameFile.NetworkModel.GetNodesWithAttributeValue("use_for_transactions", "true");
            if (TransactionNode.Count > 1)
            {
                throw new Exception("Multiple Nodes being used as transaction node");
            }
            else if (TransactionNode.Count == 1)
            {
                Node transactionNode = (Node)TransactionNode[0];
                TransactionNodeName = transactionNode.GetAttribute("name");
            }
            else
            {
                TransactionNodeName = "Transactions";
            }
			string fined_rounds = SkinningDefs.TheInstance.GetData("rounds_with_regulation_fines");
			if(fined_rounds == "")
			{
				//assume we are going to have a regulation project fine until we know any different
				if (_round > 1)
				{
					RegulationFines = _rep.GetCost("no_regulation_project",_round);
				}
			}
			else
			{
				// Split comma delimted list...
				char[] comma = { ',' };
				string[] rounds_to_fine = fined_rounds.Split(comma);
				string rstr = LibCore.CONVERT.ToStr(_round);
				foreach(string _round_str in rounds_to_fine)
				{
					if(_round_str == rstr)
					{
						RegulationFines = _rep.GetCost("no_regulation_project",_round);
					}
				}
			}

			// Pull the logfile to get data from.
			string logFile = _gameFile.GetRoundFile(_round,"NetworkIncidents.log", GameManagement.GameFile.GamePhase.OPERATIONS);
			if (_gameFile.LastRoundPlayed == _round && _gameFile.LastPhasePlayed == GameManagement.GameFile.GamePhase.TRANSITION)
			{
				//no ops phase yet so don't set ops log file
				logFile = "";
			}

			if (File.Exists(logFile))
			{
				//add watches for different events
				BasicIncidentLogReader biLogReader = new BasicIncidentLogReader(logFile);
				//watch for costed events
				biLogReader.WatchCreatedNodes("CostedEvents", this.biLogReader_CostedEventFound );

				//watch for event monitoring being switched on/off
				biLogReader.WatchApplyAttributes("AdvancedWarningTechnology", this.biLogReader_MonitoringEventFound );
				//watch for fixitqueue - need this to check for fix by consultancy
				biLogReader.WatchApplyAttributes("FixItQueue", this.biLogReader_FixItQueueEventFound );
				//watch for sla breaches
				biLogReader.WatchApplyAttributes("sla_breach", this.biLogReader_SLAbreachEventFound );
				//watch the project spend
				biLogReader.WatchApplyAttributes("DevelopmentSpend", this.biLogReader_SpendFound );

				//POLESTAR - watch for transactions count

                

				biLogReader.WatchApplyAttributes(TransactionNodeName, this.biLogReader_TransactionsFound );
				biLogReader.WatchApplyAttributes("Revenue", this.biLogReader_RevenueFound );

				
				//POLESTAR NPO - watch for ApplicationsProcessed count
				biLogReader.WatchApplyAttributes("ApplicationsProcessed", this.biLogReader_ApplicationsProcessedFound);
				biLogReader.WatchApplyAttributes("Budget", this.biLogReader_RoundBudgetFound);

				//IBM CLOUD - watch for ApplicationsProcessed count
				biLogReader.WatchApplyAttributes("ServerUtil", this.biLogReader_ServerUtilChangesFound);
				biLogReader.WatchApplyAttributes("Expenses", this.biLogReader_ExpensesChangesFound);

				Node tt_node = _gameFile.NetworkModel.GetNamedNode("TransactionsTarget");
				if (tt_node != null)
				{
					TargetTransactions = tt_node.GetIntAttribute(string.Format("round_{0}_target", _round), 0);
				}

				Node audits = _gameFile.NetworkModel.GetNamedNode("Audits");
				if (audits != null)
				{
					foreach (Node audit in audits.getChildren())
					{
						biLogReader.WatchApplyAttributes(audit.GetAttribute("name"), biLogReader_AuditFound);
					}
				}

                Node stores = _gameFile.NetworkModel.GetNamedNode("Stores");
                if (stores != null)
                {
			        foreach (Node store in stores.GetChildrenOfType("Store"))
			        {
                        biLogReader.WatchApplyAttributes(store.GetAttribute("name"), biLogReader_StoreAttributesChanged);
			        }
                }

			    biLogReader.Run();

				ReadNetworkInfo();
			}

			//only try transition files if in round 2 or higher
			if (_gameFile.GameTypeUsesTransitions() && _round > 1)
			{
				// get the transition log file
				string logFile2 = _gameFile.GetRoundFile(_round,"NetworkIncidents.log", GameManagement.GameFile.GamePhase.TRANSITION);
				if (File.Exists(logFile2))
				{
					InTransition = true;

					//add watches for different events
					BasicIncidentLogReader biLogReader = new BasicIncidentLogReader(logFile2);
					//watch the project spend
					biLogReader.WatchApplyAttributes("DevelopmentSpend", this.biLogReader_SpendFound );
					//watch for costed events
					biLogReader.WatchCreatedNodes("CostedEvents", this.biLogReader_CostedEventFound );

					
					//watch for event monitoring being switched on/off
					biLogReader.WatchApplyAttributes("AdvancedWarningTechnology", this.biLogReader_MonitoringEventFound );
					//watch for fixitqueue - need this to check for fix by consultancy
					biLogReader.WatchApplyAttributes("FixItQueue", this.biLogReader_FixItQueueEventFound );
					//watch for sla breaches
					biLogReader.WatchApplyAttributes("sla_breach", this.biLogReader_SLAbreachEventFound );
					//watch the project spend
					biLogReader.WatchApplyAttributes(TransactionNodeName, this.biLogReader_TransactionsFound );
					biLogReader.WatchApplyAttributes("Revenue", this.biLogReader_RevenueFound );

					biLogReader.Run();
					// Don't read network info from the transition phase!
					//ReadNetworkInfo();
				}
			}

			//get fixed costs
			NumFixedCosts = 1;

			//work out points & revenue
			CalculatePoints();

			//calc project spend
			ProjectSpend = firstspend - lastspend;

			//work out overall profit/loss
			FixedCosts = _rep.GetCost("fixedcosts",_round);
			SupportBudget = _rep.GetCost("supportbudget",_round);

			ComplianceFines += _rep.GetCost("compliance_fine",_round);
			ComplianceFines += (ComplianceIncidents * _rep.GetCost("compliance_fine_per_incident", _round));


            Profit = Revenue - FixedCosts - SupportBudget - ProjectSpend - RegulationFines - ComplianceFines - ContinuousFines;

            AOSE_Profit = Revenue - FixedCosts - SupportBudget - ProjectSpend - RegulationFines;// -ComplianceFines;
			ibm_Profit = Revenue - FixedCosts - SupportBudget;

			AverageAppCostNumerator = SupportBudget + FixedCosts + ProjectSpend + RegulationFines + ComplianceFines;
			AverageAppCostDenominator = Math.Max(1400, ApplicationsHandled);
			AverageAppCost = AverageAppCostNumerator / AverageAppCostDenominator;

			NodeTree model = _gameFile.GetNetworkModel(_round, GameFile.GamePhase.OPERATIONS);
			Node appsProcessedNode = model.GetNamedNode("ApplicationsProcessed");
			if (appsProcessedNode != null)
			{
				CustomerComplaints = appsProcessedNode.GetIntAttribute("complaints", 0);
			}

			//gain/loss
			if (_round == 1)
			{
				Gain = 0;
				ibm_Gain = 0;
			}
			else
			{
				Gain = Profit - prevProfit;
				ibm_Gain = ibm_Profit - prevProfit;
			}

			// Bug 2434 : MTTR calculation to include incidents open at end of race.
			CountRemainingIncidentsMTTR((int)FinalTime);

			//calc mean time to recovery
			if ( (Incidents == 0) || (downed==0) ) MTTR = 0;
			else MTTR = IncidentDownTime / downed;

			//read the maturity scores
			sectionNameToColour = new Dictionary<string, Color> ();
			ReportUtils.TheInstance.GetMaturityScores(_gameFile, _round, inner_sections, outer_sections, MaturityHashOrder, SectionOrder, sectionNameToColour);

			//add maturity values to list to work out indicator score
			ArrayList plots = new ArrayList();
			foreach(string section in outer_sections.Keys)
			{
				ArrayList points = (ArrayList)outer_sections[section];

				foreach (string pt in points)
				{
					string[] vals = pt.Split(':');

					//processing rate needs to be scaled 0 to 5
					if (vals[0] == "Processing Rate")
					{
						int prScore = 0;
						int prate = CONVERT.ParseInt(vals[1]);

						if (prate > 4)
						{
							prScore = 0;
						}
						else
						{
							prScore = (5 - prate) * 2;
						}

						plots.Add(prScore);
					}
					else
					{
						plots.Add(CONVERT.ParseInt(vals[1]));
					}
				}
			}
			
			CalcIndicatorScore(plots);
		}

	    void biLogReader_StoreAttributesChanged (object sender, string key, string line, double time)
	    {
	        var stockLevel = BasicIncidentLogReader.ExtractIntValue(line, "stock_level");
	        var storeName = BasicIncidentLogReader.ExtractValue(line, "i_name");
	        if (stockLevel.HasValue)
	        {
	            Node stores = _gameFile.NetworkModel.GetNamedNode("Stores");
	            Node store = _gameFile.NetworkModel.GetNamedNode(storeName);

	            if (store.GetBooleanAttribute("show_stock_level", true)
                    && ((stockLevel.Value < stores.GetIntAttribute("min_stock_level", 0))
	                || (stockLevel.Value > stores.GetIntAttribute("max_stock_level", 0))))
	            {
	                stockLevelsBreached = true;
	            }
	        }
	    }

	    public virtual Hashtable GetRoundPoints()
		{
			Hashtable teams = new Hashtable();

			//now work out how many points each team got
			foreach( CarInfo car in cars.Values)
			{
				string team = car.team;

				if (teams.ContainsKey(team))
				{
					int pos = CONVERT.ParseInt(car.pos);
					int pts = (int)teams[team] + _rep.GetCost("pos " + car.pos,_round);
					teams[team] = pts;
				}
				else
				{
					int pos = CONVERT.ParseInt(car.pos);
					int pts = _rep.GetCost("pos " + car.pos,_round);
					teams.Add(team,pts);
				}
			}
			return teams;
		}

		protected virtual void ReadNetworkInfo()
		{
			string NetworkFile;

			// If we are in the sales game then do not use the current network model values but
			// just use the ones from disk.
			bool isSalesGame = _gameFile.IsSalesGame;

			if (_gameFile.LastRoundPlayed == _round && (!isSalesGame))
			{
				//read the car info from the current or latest operations network model
				NodeTree model;

				// If we are in the ops phase then load the current model.
				if (_gameFile.CurrentPhase == GameFile.GamePhase.OPERATIONS)
				{
					model = _gameFile.NetworkModel;
				}
				else
				{
					// Load the model from file.
					NetworkFile = _gameFile.GetRoundFile(_round, "Network.xml", GameFile.GamePhase.OPERATIONS);
					if (File.Exists(NetworkFile))
					{
						System.IO.StreamReader file = new System.IO.StreamReader(NetworkFile);
						string xmldata = file.ReadToEnd();
						file.Close();
						file = null;
						model = new NodeTree(xmldata);
					}
					else
					{
						// Problem, so no data
						return;
					}
					// : Set final time to 25 mins!
					//FinalTime = 25*60;
				}

				FinalTime = model.GetNamedNode("CurrentTime").GetIntAttribute("seconds", 0);
				string AvailabilityStr = model.GetNamedNode("Availability").GetAttribute("availability");
				Availability = LibCore.CONVERT.ParseDoubleSafe(AvailabilityStr, 0);

				//check if awt on
				Node awt = model.GetNamedNode("AdvancedWarningTechnology");
				string enabled = (awt == null) ? "" : awt.GetAttribute("enabled");
				if (enabled == "true") this.AdvancedWarningEnabled = true;

				ArrayList types = new ArrayList();
				types.Add("Car");
				Hashtable servers = model.GetNodesOfAttribTypes(types);

				foreach (Node service in servers.Keys)
				{
					string name = service.GetAttribute("name");

					if (service.GetAttribute("type") == "Car")
					{
						//get the driver and team names for this car from the current network model
						string driver = service.GetAttribute("driver");
						string team = service.GetAttribute("team");
						string pos = service.GetAttribute("pos");
						string Catch = service.GetAttribute("catch");
						string lagd = service.GetAttribute("lagd");
						string car = service.GetAttribute("name");

						if (cars.ContainsKey(pos))
						{
							((CarInfo) cars[pos]).car = car;
							((CarInfo) cars[pos]).Catch = Catch;
							((CarInfo) cars[pos]).lagd = lagd;
							((CarInfo) cars[pos]).pos = pos;
							((CarInfo) cars[pos]).driver = driver;
							((CarInfo) cars[pos]).team = team;
						}
						else
						{
							CarInfo carinfo = new CarInfo();
							carinfo.car = car;
							carinfo.Catch = Catch;
							carinfo.lagd = lagd;
							carinfo.pos = pos;
							carinfo.driver = driver;
							carinfo.team = team;

							cars.Add(pos, carinfo);
						}
					}
				}
			}

			{
				NodeTree model = _gameFile.GetNetworkModel(_round, GameFile.GamePhase.OPERATIONS);

				Node vulnstats = model.GetNamedNode("VulnerabilityStats");
				if (vulnstats != null)
				{
					total_vulnode_count = vulnstats.GetIntAttribute("total_nodes", 0);
					open_vulnodes_count = vulnstats.GetIntAttribute("open_nodes", 0);
					protected_vulnodes_count = vulnstats.GetIntAttribute("protected_nodes", 0);
					upgrade_vulnodes_count = vulnstats.GetIntAttribute("upgraded_nodes", 0);

					prevented_vuln_count = vulnstats.GetIntAttribute("prevented_nodes", 0);
					fixed_vuln_count = vulnstats.GetIntAttribute("fixed_nodes", 0);

					//calculate 
					starting_vuln_count = vulnstats.GetIntAttribute("open_nodes_at_start_of_round", 0);
					identified_vuln_count = upgrade_vulnodes_count;
				}
			}

			//need to charge for mirrors/installs in every round, not just one where it was added
			//open network file and check for existance of Monaco(M) and Suzuka(M) or new biz services
			// 14-04-2008 : If we are in a sales game then try to use a special backuped version of round 2 Ops network
			// that was stored in the sales game creator before it rewound round 2 ops. If it's not there then just use the one
			// on disk.
			NetworkFile = _gameFile.GetRoundFile(_round, "Network.xml", GameFile.GamePhase.OPERATIONS);//_gameFile.CurrentPhase);
			//
			if (isSalesGame)
			{
				string ran_net_file = _gameFile.GetRoundFile(_round, "network_ran.xml", GameFile.GamePhase.OPERATIONS);
				if (File.Exists(ran_net_file))
				{
					NetworkFile = ran_net_file;
				}
			}
			if (File.Exists(NetworkFile))
			{
				System.IO.StreamReader file = new System.IO.StreamReader(NetworkFile);
				string xmldata = file.ReadToEnd();
				file.Close();
				file = null;

				NodeTree model = new NodeTree(xmldata);

				if (FinalTime == 0)
				{
					FinalTime = model.GetNamedNode("CurrentTime").GetIntAttribute("seconds", 0);
				}

				if (Availability == 0)
				{
					string AvailabilityStr = model.GetNamedNode("Availability").GetAttribute("availability");
					Availability = LibCore.CONVERT.ParseDoubleSafe(AvailabilityStr, 0);
				}

				//check if awt on
				Node awt = model.GetNamedNode("AdvancedWarningTechnology");
				string enabled = (awt == null) ? "" : awt.GetAttribute("enabled");
				if (enabled == "true") this.AdvancedWarningEnabled = true;

				ArrayList types = new ArrayList();
				types.Add("Server");
				types.Add("biz_service");
				types.Add("Car");
				Hashtable servers = model.GetNodesOfAttribTypes(types);

				foreach (Node service in servers.Keys)
				{
					string name = service.GetAttribute("name");
					string type = service.GetAttribute("mirror");

					//if (service.GetAttribute("type") == "Car" && (_gameFile.LastRoundPlayed != _round || inx >=0) )
					if (service.GetAttribute("type") == "Car" && (_gameFile.LastRoundPlayed != _round || isSalesGame))
					{
						//get the driver and team names for this car from the network file
						string driver = service.GetAttribute("driver");
						string team = service.GetAttribute("team");
						string pos = service.GetAttribute("pos");
						string Catch = service.GetAttribute("catch");
						string lagd = service.GetAttribute("lagd");
						string car = service.GetAttribute("name");

						if (cars.ContainsKey(pos))
						{
							((CarInfo) cars[pos]).car = car;
							((CarInfo) cars[pos]).Catch = Catch;
							((CarInfo) cars[pos]).lagd = lagd;
							((CarInfo) cars[pos]).pos = pos;
							((CarInfo) cars[pos]).driver = driver;
							((CarInfo) cars[pos]).team = team;
						}
						else
						{
							CarInfo carinfo = new CarInfo();
							carinfo.car = car;
							carinfo.Catch = Catch;
							carinfo.lagd = lagd;
							carinfo.pos = pos;
							carinfo.driver = driver;
							carinfo.team = team;

							cars.Add(pos, carinfo);
						}
					}
					if (name == serverTwoMirror)
					{
						this.Mirror2 = 1;
					}
					if (name == serverOneMirror)
					{
						this.Mirror1 = 1;
					}
				}
			}
		}

		protected virtual string GetCarPosition(string name)
		{
			for (int i=1; i<= 20; i++)
			{
				string key = CONVERT.ToStr(i);
				if (cars.ContainsKey(key))
				{
					if (((CarInfo)cars[key]).car == name)
					{
						return key;
					}
				}
			}
			return "";
		}

		protected virtual void biLogReader_FixItQueueEventFound(object sender, string key, string line, double time)
		{
			//check if entering or leaving fixitqueue
			string val = BasicIncidentLogReader.ExtractValue(line, "fixing");

			if (val == "true")
			{
				InFixItQueue = true;
				eventsfound = 0;
			}
			if (val == "false")
			{
				InFixItQueue = false;
			}
		}

		protected virtual void biLogReader_AuditFound (object sender, string key, string line, double time)
		{
			string failedString = BasicIncidentLogReader.ExtractValue(line, "failed");
			if (failedString != "")
			{
				if (CONVERT.ParseBool(failedString, false))
				{
					string auditName = BasicIncidentLogReader.ExtractValue(line, "i_name");
					if (! failedAuditNames.Contains(auditName))
					{
						failedAuditNames.Add(auditName);
					}
				}
			}
		}

		protected virtual void biLogReader_MonitoringEventFound(object sender, string key, string line, double time)
		{
			string enabledStr = BasicIncidentLogReader.ExtractValue(line,"enabled");

			if (enabledStr == "true")
			{
				AdvancedWarningEnabled = true;
			}
			if (enabledStr == "false")
			{
				AdvancedWarningEnabled = false;
			}
		}

		protected virtual void biLogReader_SLAbreachEventFound(object sender, string key, string line, double time)
		{
			string count = BasicIncidentLogReader.ExtractValue(line,"biz_serv_count");

			if (count != string.Empty)
			{
				this.NumSLAbreaches = CONVERT.ParseInt(count);
			}
		}

		protected virtual void biLogReader_CarPosFound(object sender, string key, string line, double time)
		{
			string pos = BasicIncidentLogReader.ExtractValue(line, "Pos");
			string Catch = BasicIncidentLogReader.ExtractValue(line, "Catch");
			string lagd = BasicIncidentLogReader.ExtractValue(line, "Lagd");

			//check if the car exists in the hash table
			if (pos != string.Empty)
			{
				if (cars.ContainsKey(pos))
				{
					((CarInfo)cars[pos]).car = key;
					if (Catch != string.Empty) ((CarInfo)cars[pos]).Catch = Catch;
					if (lagd != string.Empty) ((CarInfo)cars[pos]).lagd = lagd;
					((CarInfo)cars[pos]).pos = pos;
				}
				else
				{
					CarInfo carinfo = new CarInfo();
					carinfo.car = key;
					if (Catch != string.Empty) carinfo.Catch = Catch;
					if (lagd != string.Empty) carinfo.lagd = lagd;
					if (pos != string.Empty) carinfo.pos = pos;

					cars.Add(pos, carinfo);
				}
			}
			else if (lagd != string.Empty)
			{
				pos = GetCarPosition(key);

				if (cars.ContainsKey(pos))
				{
					((CarInfo)cars[pos]).lagd = lagd;
				}
			}
		}

		// Bug 2434 : 
		protected virtual void CountRemainingIncidentsMTTR(int endTime)
		{
			foreach (string id in downedIncidents.Keys)
			{
				string timeWentDown = (string) downedIncidents[id];
				int timeLeft = endTime - (int)(CONVERT.ParseDouble(timeWentDown));
				IncidentDownTime += timeLeft;
				incidentDurations[id] = ((double) incidentDurations[id]) + (double) timeLeft;
				downed++;
				IncidentDownTimeIncludingFacilities += timeLeft;

			}
		}

		protected virtual void biLogReader_CostedEventFound(object sender, string key, string line, double time)
		{
			string id = BasicIncidentLogReader.ExtractValue(line, "incident_id");
			string val = BasicIncidentLogReader.ExtractValue(line,"type");

			if (val.StartsWith("entity fix") && id != string.Empty)
			{
				FixedIncidents.Add(id);

				//we have an incident fix
				if (downedIncidents.ContainsKey(id))
				{
					string curtime = BasicIncidentLogReader.ExtractValue(line, "i_doAfterSecs");
					string downedtime = (string)downedIncidents[id];

					double start = CONVERT.ParseDouble(downedtime);
					double end = CONVERT.ParseDouble(curtime);

					double timedown = end - start;

					IncidentDownTime += timedown;
					downed++;

					IncidentDownTimeIncludingFacilities += timedown;

					incidentDurations[id] = ((double) incidentDurations[id]) + timedown;

					// 03-05-2007 : We now remove this incident from being down in case it
					// gets applied again!
					downedIncidents.Remove(id);

					var incident = incidentOccurrences.FindLast(io => io.Id == id);
					incident.EndTime = (int) time;
				}
			}
			else if (val == "prevented_incident")
			{
				PreventedIncidents++;

				if (BasicIncidentLogReader.ExtractValue(line, "caused_by_upgrade") == "true")
				{
					IncidentsPreventedByUpgrades++;
				}

				incidentOccurrences.Add(new IncidentOccurrence
				{
					Description = BasicIncidentLogReader.ExtractValue(line, "desc"),
					ServiceNames = BasicIncidentLogReader.ExtractValue(line, "service_name").Split(','),
					StartTime = (int) time,
					EndTime = (int) time,
					WasPrevented = true
				});
			}
			else if (val == "recurring_incident")
			{
				RecurringIncidents++;

				if (BasicIncidentLogReader.ExtractValue(line, "compliance") == "true")
				{
					ComplianceIncidents++;
				}

				var incident = incidentOccurrences.FindLast(io => io.Id == id);

				if (incident == null)
				{
					incident = new IncidentOccurrence { Id = id, StartTime = (int) time };
				}

				incident.EndTime = (int) time;
				incident.IsRecurring = true;
			}
			else if (val == "incident")
			{
				Incidents++;

				if (BasicIncidentLogReader.ExtractValue(line, "compliance") == "true")
				{
					ComplianceIncidents++;
				}
	
				//work out the down time for each incident so can get MTTR
				//get the incident id
				id = BasicIncidentLogReader.ExtractValue(line, "incident_id");
				if (id != string.Empty)
				{
					string curtime = BasicIncidentLogReader.ExtractValue(line, "i_doAfterSecs");

					//add to hashtable with downtime of 0
					if (!downedIncidents.ContainsKey(id))
					{
						downedIncidents.Add(id,curtime);
					}

					if (! incidentDurations.ContainsKey(id))
					{
						incidentDurations.Add(id, (double) 0);
					}

					AllIncidents.Add(id + ";" + curtime);
				}

				incidentOccurrences.Add(new IncidentOccurrence
				{
					Id = id,
					Description = BasicIncidentLogReader.ExtractValue(line, "desc"),
					StartTime = (int) time,
					ServiceNames = BasicIncidentLogReader.ExtractValue(line, "service_name").Split(',')
				});
			}
            //handle continuous fine
            else if (val == "continuous_fine")
            {
                //ContinuousFines += CONVERT.ParseInt(BasicIncidentLogReader.ExtractValue(line, "cost"));
                ComplianceFines += CONVERT.ParseInt(BasicIncidentLogReader.ExtractValue(line, "cost")); 
            }
            //support cost
            else if (val.ToLower() == "workaround")
            {
                NumWorkarounds++;
            }
            //support cost
            else if (val == "mirror")
            {
                //get which mirror has been added
                string refStr = BasicIncidentLogReader.ExtractValue(line, "ref");
                if (refStr == CoreUtils.SkinningDefs.TheInstance.GetData("mirrored_server2"))
                {
                    Mirror2 = 1;
                }
                if (refStr == CoreUtils.SkinningDefs.TheInstance.GetData("mirrored_server1"))
                {
                    Mirror1 = 1;
                }
            }
            else if ((val == "entity_fix_by_consultancy") || (val == "entity fix by consultancy"))
            {

                //look for fix by consultancy
                if (this.InFixItQueue == true)
                {
                    //got a fix by consultancy ENTITY event, need the entity to get the type of node that was fixed
                    string ID = BasicIncidentLogReader.ExtractValue(line, "incident_id");

                    if (!FixedIncidents.Contains(ID))
                    {
                        FixedIncidents.Add(ID);
                    }

                    if (eventsfound == 0)
                    {
                        //only charge for one fix
                        string node_type = BasicIncidentLogReader.ExtractValue(line, "node_category");
                        if (node_type == "App" || node_type == "Connection")
                        {
                            //charge for app
                            NumAppConsultancyFixes++;
                        }
                        else
                        {
                            //charge for server
                            NumServerConsultancyFixes++;
                        }
                    }
                    eventsfound++;
                }

                //also need to mark fix for mttr calculation - bug #3137
                //we have an incident fix
                if (downedIncidents.ContainsKey(id))
                {
                    string curtime = BasicIncidentLogReader.ExtractValue(line, "i_doAfterSecs");
                    string downedtime = (string)downedIncidents[id];

                    double start = CONVERT.ParseDouble(downedtime);
                    double end = CONVERT.ParseDouble(curtime);

                    double timedown = end - start;

                    IncidentDownTime += timedown;
                    downed++;

                    IncidentDownTimeIncludingFacilities += timedown;

                    incidentDurations[id] = ((double)incidentDurations[id]) + timedown;

                    // 03-05-2007 : We now remove this incident from being down in case it
                    // gets applied again!
                    downedIncidents.Remove(id);
                }

	            var incident = incidentOccurrences.FindLast(io => io.Id == id);
	            if (incident != null)
	            {
		            incident.EndTime = (int) time;
	            }
            }
			else if (val == "appupgrade")
            {
                NumAppUpgrades++;
            }
            else if (val == "upgradeserver")
            {
                NumServerUpgrades++;
            }
            else if (val == "memoryupgrade")
            {
                NumMemUpgrades++;
            }
            else if (val == "storageupgrade")
            {
                NumStorageUpgrades++;
            }
            else if (val == "upgradeclearincident")
            {
                string incident = BasicIncidentLogReader.ExtractValue(line, "incident_id");
                ClearIncident(incident, time);
            }
            else if (val == "project")
            {
                //check if a regulation project has been installed
                string reg = BasicIncidentLogReader.ExtractValue(line, "regulation");
                if (reg == "true")
                {
                    this.RegulationFines = 0;
                }
            }
            else if (val == "Install")
            {
                NumServices++;
                if (InTransition == true)
                {
                    NumServicesBeforeRace++;
                }

                NumNewServices++;

            }
            else if (val == "sipupgrade")
            {
                NumServices++;
                if (InTransition == true)
                {
                    NumServicesBeforeRace++;
                }
            }
            else if (val == "fraudulent_transaction_fine")
            {
                FradulentTransactionsApproved++;
            }
            else if (val == "first_line_fix")
            {
                FirstLineFixes++;

                if (downedIncidents.ContainsKey(id)
                    && incidentDurations.ContainsKey(id))
                {
                    string curtime = BasicIncidentLogReader.ExtractValue(line, "i_doAfterSecs");
                    string downedtime = (string)downedIncidents[id];

                    double start = CONVERT.ParseDoubleSafe(downedtime, 0);
                    double end = CONVERT.ParseDoubleSafe(curtime, 0);

                    double timedown = end - start;

                    IncidentDownTime += timedown;
                    downed++;

                    IncidentDownTimeIncludingFacilities += timedown;

                    incidentDurations[id] = ((double)incidentDurations[id]) + timedown;

                    // 03-05-2007 : We now remove this incident from being down in case it
                    // gets applied again!
                    downedIncidents.Remove(id);
                }
            }
			else if (val == "automated_fix")
			{
				AutomatedFixes++;

				if (downedIncidents.ContainsKey(id)
					&& incidentDurations.ContainsKey(id))
				{
					string curtime = BasicIncidentLogReader.ExtractValue(line, "i_doAfterSecs");
					string downedtime = (string) downedIncidents[id];

					double start = CONVERT.ParseDoubleSafe(downedtime, 0);
					double end = CONVERT.ParseDoubleSafe(curtime, 0);

					double timedown = end - start;

					IncidentDownTime += timedown;
					downed++;

					IncidentDownTimeIncludingFacilities += timedown;

					incidentDurations[id] = ((double) incidentDurations[id]) + timedown;

					// 03-05-2007 : We now remove this incident from being down in case it
					// gets applied again!
					downedIncidents.Remove(id);
				}
			}
		}

		protected virtual void biLogReader_ServerUtilChangesFound(object sender, string key, string line, double time)
		{
			string numActSvrCount = BasicIncidentLogReader.ExtractValue(line, "active_server_count");
			string numSvrUtil = BasicIncidentLogReader.ExtractValue(line, "overall_server_util_level");
			string numSysDCIE = BasicIncidentLogReader.ExtractValue(line, "system_dcie");

			if (numActSvrCount != string.Empty)
			{
				this.num_active_servers = CONVERT.ParseInt(numActSvrCount);
			}
			if (numSvrUtil != string.Empty)
			{
				server_utilization = CONVERT.ParseDoubleSafe(numSvrUtil,0);
			}
			if (numSysDCIE != string.Empty)
			{
				int tmpValue = CONVERT.ParseIntSafe(numSysDCIE, 0);
				system_DCIE = ((double)tmpValue);
			}
		}

		protected virtual void biLogReader_ExpensesChangesFound(object sender, string key, string line, double time)
		{
			string num_opex = BasicIncidentLogReader.ExtractValue(line, "opex");
			string num_opex_add = BasicIncidentLogReader.ExtractValue(line, "opex_additional");
			string num_capex_new = BasicIncidentLogReader.ExtractValue(line, "capex_new_services");

			if (num_opex != string.Empty)
			{
				this.system_OPEX = CONVERT.ParseInt(num_opex);
			}
			if (num_opex_add != string.Empty)
			{
				this.system_OPEX_ADD = CONVERT.ParseInt(num_opex_add);
			}
			if (num_capex_new != string.Empty)
			{
				this.system_CAPEX_NEW = CONVERT.ParseInt(num_capex_new);
			}
		}

		protected virtual void biLogReader_RoundBudgetFound(object sender, string key, string line, double time)
		{
			string numBudget = BasicIncidentLogReader.ExtractValue(line, "overall_budget");
			if (numBudget != string.Empty)
			{
				this.RoundBudget = CONVERT.ParseInt(numBudget);
			}
		}

		protected virtual void biLogReader_ApplicationsProcessedFound(object sender, string key, string line, double time)
		{
			string numMaxAppsProcessed = BasicIncidentLogReader.ExtractValue(line, "max_apps_processed");
			string numAppsProcessed = BasicIncidentLogReader.ExtractValue(line, "apps_processed");
			string numAppsDelayed = BasicIncidentLogReader.ExtractValue(line, "apps_lost");

			if (numMaxAppsProcessed != string.Empty)
			{
				ApplicationsMax = CONVERT.ParseInt(numMaxAppsProcessed);
			}
			if (numAppsProcessed != string.Empty)
			{
				ApplicationsHandled = CONVERT.ParseInt(numAppsProcessed);
			}
			if (numAppsDelayed != string.Empty)
			{
				ApplicationsDelayed = CONVERT.ParseInt(numAppsDelayed);
			}
		}

		protected virtual void biLogReader_TransactionsFound(object sender, string key, string line, double time)
		{
			string numtrans = BasicIncidentLogReader.ExtractValue(line,"count_good");
			string maxtrans = BasicIncidentLogReader.ExtractValue(line,"count_max");

			if (numtrans != string.Empty)
			{
				NumTransactions = CONVERT.ParseInt(numtrans);
			}
			if (maxtrans != string.Empty)
			{
				MaxTransactions = CONVERT.ParseInt(maxtrans);
			}
		}

		protected virtual void biLogReader_RevenueFound(object sender, string key, string line, double time)
		{
			string revenue = BasicIncidentLogReader.ExtractValue(line,"revenue");
			string maxrev = BasicIncidentLogReader.ExtractValue(line,"max_revenue");
			string lost_op_cost = BasicIncidentLogReader.ExtractValue(line,"lost_opportunity");
			string product_penalties_str = BasicIncidentLogReader.ExtractValue(line, "product_penalties");

			if (revenue != string.Empty)
			{
				this.Revenue = CONVERT.ParseInt(revenue);
			}
			if (maxrev != string.Empty)
			{
				MaxRevenue = CONVERT.ParseInt(maxrev);
			}
			if (lost_op_cost != string.Empty)
			{
				lost_opportunity_cost = CONVERT.ParseInt(lost_op_cost);
			}
			if (product_penalties_str != string.Empty)
			{
				this.product_penalties = CONVERT.ParseInt(product_penalties_str);
			}
		}

		protected virtual void biLogReader_SpendFound(object sender, string key, string line, double time)
		{
			string spend = BasicIncidentLogReader.ExtractValue(line,"RoundBudgetLeft");

			if (spend != string.Empty)
			{
				int val = CONVERT.ParseInt(spend);

				if (firsttime)
				{
					firstspend = val;
					lastspend = val;
					firsttime = false;
				}
				else
				{
					lastspend = val;
				}
			}
		}

		protected virtual int PlotScore(int score)
		{
			if (score < 0) score = 0;
			if (score > 10) score = 10;

			float norm = score / 2;
			return (int)Math.Round(norm, 0);
		}

		protected virtual void CalcIndicatorScore(ArrayList plots)
		{
			int angle = 0;
			pt[] pts = new pt[plots.Count];
			float totalArea = 0;

			// calculate the coordinates for each point
			for (int i = 0; i < plots.Count; i++)
			{
				int val = PlotScore((int)plots[i]);

				float x = (float)Math.Round((Math.Cos((angle - 90) * (3.1415 / 180)) * val), 4);
				float y = (float)Math.Round((Math.Sin((angle - 90) * (3.1415 / 180)) * val), 4);
				pts[i] = new pt(x,y);
				angle += 360 / plots.Count;
			}

			// calculate the total area of the graph
			for (int i = 0; i < plots.Count - 1; i++)
			{
				pt p = new pt(0,0);
				totalArea += AreaOfTriangle(p, pts[i], pts[i+1]);
			}

			// adjust range to between 0 and 5 (58.91 is the maximum area)
			IndicatorScore = (float)Math.Round(totalArea, 2);
			if (IndicatorScore <= 0.1f) IndicatorScore = 0;
		}

		protected virtual float AreaOfTriangle(pt a, pt b, pt c)
		{
			float sidea = (float)Math.Sqrt(Math.Pow((b.X - c.X), 2) + Math.Pow((b.Y - c.Y), 2));
			float sideb = (float)Math.Sqrt(Math.Pow((a.X - c.X), 2) + Math.Pow((a.Y - c.Y), 2));
			float sidec = (float)Math.Sqrt(Math.Pow((b.X - a.X), 2) + Math.Pow((b.Y - a.Y), 2));
			return (sidea + sideb + sidec) / 2;
		}

		protected virtual void CalculatePoints()
		{
			//calculate the number of points we got from all of our cars

			int moneyPerPoint = _rep.GetCost("point",_round);

			//check if we have any data
			if (cars.Count > 0)
			{
				foreach (CarInfo car in cars.Values)
				{
                    if (car.team == SkinningDefs.TheInstance.GetData("team_name", "HP"))
					{
						int carpoints = _rep.GetCost("pos " + car.pos,_round);
						int carrevenue = carpoints * moneyPerPoint;

						Points += carpoints;
						Revenue += carrevenue;	
					}
				}
			}
		}

		protected virtual void ClearIncident (string incident, double time)
		{
			if (downedIncidents.ContainsKey(incident))
			{
				string downedtime = (string)downedIncidents[incident];

				double start = CONVERT.ParseDouble(downedtime);
				double end = time;

				double timedown = end - start;
				IncidentDownTime += timedown;
				downed++;

				IncidentDownTimeIncludingFacilities += timedown;

				incidentDurations[incident] = ((double) incidentDurations[incident]) + timedown;

				// 03-05-2007 : We now remove this incident from being down in case it
				// gets applied again!
				downedIncidents.Remove(incident);
			}

			var occurrence = incidentOccurrences.FindLast(io => io.Id == incident);
			occurrence.EndTime = (int) time;
		}

		public class IncidentOccurrence
		{
			public string Id;
			public int StartTime;
			public int? EndTime;
			public string [] ServiceNames;
			public string Description;
			public bool IsRecurring;
			public bool WasPrevented;
			public bool WasFirstLineFixed;
		}

		public List<IncidentOccurrence> incidentOccurrences;
	}
}