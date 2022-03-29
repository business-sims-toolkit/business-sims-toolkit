using System;
using System.Collections.Generic;
using System.Diagnostics;
using GameManagement;
using LibCore;
using CoreUtils;
using Logging;
using Network;

namespace ReportBuilder
{
	public class EsmRoundScores : RoundScores
	{
		List<string> functionNames;
		List<string> functionAttributeNames;
		Dictionary<string, string> functionNameToDescription;
		double syntheticMTFRAverage;

        public class BusinessCase
		{
			public string Name;
			public string Description;
			public double StartTime;
			public double? InstallTimeStart;
			public double? InstallTimeEnd;
			public double? PenaltyTime;
            // Only set when cancelled
            public double? EndTime; 
			public string Option;

            public bool WasSatisfied
			{
				get
				{
					return (InstallTimeEnd != null);
				}
			}

			public double Cost;
			public double Benefit;

			public Dictionary<string, double> FunctionAttributeNameToSatisfiedTime;

			public BusinessCase (string name, string description, double startTime)
			{
				Name = name;
				Description = description;
				StartTime = startTime;
				InstallTimeStart = null;
				InstallTimeEnd = null;
				PenaltyTime = null;
                EndTime = null;

				FunctionAttributeNameToSatisfiedTime = new Dictionary<string, double> ();
			}

			public double TimeToValue
			{
				get
				{
					return Cost / Benefit;
				}
			}
		}

		int position;
		public int Requests;

		public Dictionary<string, BusinessCase> NameToBusinessCase;

		Dictionary<string, int> functionNameToConsultancyFixes;
		Dictionary<string, double> functionNameToConsultancySpend;
		Dictionary<string, int> functionNameToIncidents;
		double businessCaseCosts;
		double businessCasePenalties;
		double consultancyCosts;
		double opex;
        public double Opex
        {
            get
            {
                return opex;
            }
        }
		NodeTree model;

		public enum BusinessServiceState
		{
			Down,
			Up
		}

		Dictionary<string, TimeLog<BusinessServiceState>> businessServiceNameToTimeToState;

		int CompareCompetitors (Node a, Node b)
		{
			int revenueA = a.GetIntAttribute("revenue", 0);
			int revenueB = b.GetIntAttribute("revenue", 0);

			if (revenueA != revenueB)
			{
				return revenueB.CompareTo(revenueA);
			}
			else
			{
				return a.GetAttribute("name").CompareTo(b.GetAttribute("name"));
			}
		}

		public EsmRoundScores (NetworkProgressionGameFile gameFile, int round, int previousProfit, int newServices, SupportSpendOverrides spendOverrides)
			: base (gameFile, round, previousProfit, newServices, spendOverrides)
		{
            syntheticMTFRAverage = SkinningDefs.TheInstance.GetDoubleData("synthetic_mtfr_average", 60);

        }

		protected override void ReadData ()
		{
			functionAttributeNames = new List<string>();
			functionNames = new List<string>();
			functionNameToBenefit = new Dictionary<string, double>();
			functionNameToSpend = new Dictionary<string, double>();
			functionNameToDescription = new Dictionary<string, string>();
			functionNameToConsultancyFixes = new Dictionary<string, int> ();
			functionNameToConsultancySpend = new Dictionary<string, double> ();
			functionNameToIncidents = new Dictionary<string, int> ();
			foreach (Node function in _gameFile.NetworkModel.GetNamedNode("Functions").GetChildrenOfType("function"))
			{
				functionAttributeNames.Add(function.GetAttribute("attribute_name"));
				functionNames.Add(function.GetAttribute("name"));
				functionNameToBenefit.Add(function.GetAttribute("name"), function.GetDoubleAttribute("benefit_gained", 0));
				functionNameToSpend.Add(function.GetAttribute("name"), function.GetDoubleAttribute("spend", 0) + function.GetDoubleAttribute("sla_fines", 0));
				functionNameToDescription.Add(function.GetAttribute("name").ToLower(), function.GetAttribute("desc"));
				functionNameToConsultancyFixes.Add(function.GetAttribute("attribute_name"), 0);
                functionNameToIncidents.Add(function.GetAttribute("attribute_name"), 0);
			}

			Node revenueNode = _gameFile.GetNetworkModel(_round).GetNamedNode("Revenue");
			profitTarget = _rep.GetCost("profit_target", _round);
			benefit = revenueNode.GetDoubleAttribute("benefit_gained", 0);
			spend = 0;

			businessCaseCosts = 0;
			businessCasePenalties = 0;

			base.ReadData();

			NameToBusinessCase = new Dictionary<string, BusinessCase> ();
			businessServiceNameToTimeToState = new Dictionary<string, TimeLog<BusinessServiceState>> ();
			using (BasicIncidentLogReader reader = new BasicIncidentLogReader (_gameFile.GetRoundFile(_round, "NetworkIncidents.log", GameFile.GamePhase.OPERATIONS)))
			{
				NodeTree initialModel = _gameFile.GetNetworkModelAtStartOfRound(_round);
				foreach (Node businessService in initialModel.GetNamedNode("Business Services Group").GetChildrenOfType("biz_service"))
				{
					businessServiceNameToTimeToState.Add(businessService.GetAttribute("name"), new TimeLog<BusinessServiceState> ());
					businessServiceNameToTimeToState[businessService.GetAttribute("name")].Add(0, businessService.GetBooleanAttribute("up", false) ? BusinessServiceState.Up : BusinessServiceState.Down);

					reader.WatchApplyAttributes(businessService.GetAttribute("name"), reader_BusinessServiceApplyAttributesFound);
				}

				model = _gameFile.GetNetworkModel(_round);

				reader.WatchApplyAttributes("Revenue", reader_RevenueApplyAttributesFound);

				reader.WatchCreatedNodes("BusinessCases", reader_CreatedNodesFound);
			    //reader.WatchDeletedNodes("BusinessCases", reader_DeletedNodesFound);
                reader.WatchMovedNodes("BusinessCasesCompleted", reader_MoveNodesFound);
				reader.Run();

				List<Node> competitors = model.GetNamedNode("Competitors").GetChildrenAsList();
				competitors.Sort(CompareCompetitors);

				foreach (Node competitor in competitors)
				{
					if (competitor.GetAttribute("type") == "player")
					{
						position = 1 + competitors.IndexOf(competitor);
					}
				}
			}

			consultancyCosts = 0;
			double slaFines = 0;
			Dictionary<string, double> functionNameToSlaFines = new Dictionary<string, double> ();
			foreach (string functionName in functionNameToConsultancyFixes.Keys)
			{
				functionNameToConsultancySpend[functionName] = functionNameToConsultancyFixes[functionName] * _rep.GetCost("consultancy", _round);
				consultancyCosts += functionNameToConsultancySpend[functionName];

				functionNameToSlaFines[functionName] = _gameFile.NetworkModel.GetNamedNode(functionName).GetDoubleAttribute("sla_fines", 0);
				slaFines += functionNameToSlaFines[functionName];
			}

			opex = _rep.GetCost("opex_fm", _round)
			       + _rep.GetCost("opex_hr", _round)
			       + _rep.GetCost("opex_it", _round)
			       + _rep.GetCost("opex_other", _round);

			spend = businessCaseCosts
			        + businessCasePenalties
			        + consultancyCosts
			        + slaFines
			        + opex;

			double spendCalculatedOtherWay = revenueNode.GetDoubleAttribute("spend_opex", 0) +
			                                 revenueNode.GetDoubleAttribute("spend_fines", 0) +
			                                 revenueNode.GetDoubleAttribute("spend_business_cases", 0) +
											 revenueNode.GetDoubleAttribute("spend_hr_consultancy", 0) +
											 revenueNode.GetDoubleAttribute("spend_fm_consultancy", 0) +
											 revenueNode.GetDoubleAttribute("spend_it_consultancy", 0) +
											 revenueNode.GetDoubleAttribute("spend_other_consultancy", 0);

			Debug.Assert(Math.Abs(spend - spendCalculatedOtherWay) < 0.1f);

			Profit = (int) (Revenue - spend);

			double timeToValueAccumulator = 0;
			int timeToValueCount = 0;

			foreach (BusinessCase businessCase in NameToBusinessCase.Values)
			{
				timeToValueAccumulator += businessCase.TimeToValue;
				timeToValueCount++;
			}

			if (timeToValueCount > 0)
			{
				timeToValue = timeToValueAccumulator / timeToValueCount;
			}
			else
			{
				timeToValue = 0;
			}
		}

		void reader_RevenueApplyAttributesFound (object sender, string key, string line, double time)
		{
			string revenueString = BasicIncidentLogReader.ExtractValue(line, "revenue_per_second");
			if (! string.IsNullOrEmpty(revenueString))
			{
				double revenue = CONVERT.ParseDouble(revenueString);

				if (MinProductivity != null)
				{
					MinProductivity = Math.Min(MinProductivity.Value, revenue);
				}
				else
				{
					MinProductivity = revenue;
				}

				if (MaxProductivity != null)
				{
					MaxProductivity = Math.Max(MinProductivity.Value, revenue);
				}
				else
				{
					MaxProductivity = revenue;
				}
			}
		}

		void reader_BusinessServiceApplyAttributesFound (object sender, string key, string line, double time)
		{
			BasicIncidentLogReader reader = (BasicIncidentLogReader) sender;

			string upString = BasicIncidentLogReader.ExtractValue(line, "up");
			if (! string.IsNullOrEmpty(upString))
			{
				bool up = CONVERT.ParseBool(upString, false);
				BusinessServiceState state = up ? BusinessServiceState.Up : BusinessServiceState.Down;

				TimeLog<BusinessServiceState> log = businessServiceNameToTimeToState[key];

				if (log.GetLastValueOnOrBefore(time) != state)
				{
					log.Add(time, state);
				}
			}
		}

		void reader_CreatedNodesFound (object sender, string key, string line, double time)
		{
			BasicIncidentLogReader reader = (BasicIncidentLogReader) sender;

			string name = BasicIncidentLogReader.ExtractValue(line, "name");
			

            if (!NameToBusinessCase.ContainsKey(name))
            {
                string desc = BasicIncidentLogReader.ExtractValue(line, "desc");
                NameToBusinessCase[name] = new BusinessCase(name, desc, time);

                reader.WatchApplyAttributes(name, reader_ApplyAttributesFound);
                reader.WatchDeletedNodes(name, reader_DeletedNodesFound);
            }
			
		}

        void reader_DeletedNodesFound(object sender, string key, string line, double time)
        {
            // Sanity check
            string name = BasicIncidentLogReader.ExtractValue(line, "i_name");
            Debug.Assert(NameToBusinessCase.ContainsKey(name), "Deleted Business Case isn't in dictionary.");

            NameToBusinessCase[name].EndTime = time;
        }

		void reader_MoveNodesFound (object sender, string key, string line, double time)
		{
			BasicIncidentLogReader reader = (BasicIncidentLogReader) sender;

			string name = BasicIncidentLogReader.ExtractValue(line, "i_name");
			BusinessCase businessCase = NameToBusinessCase[name];

			businessCase.Benefit = (int) time;

			if (businessCase.InstallTimeEnd == null)
			{
				businessCase.PenaltyTime = time;
			}
		}

		void reader_ApplyAttributesFound (object sender, string key, string line, double time)
		{
			BusinessCase businessCase = NameToBusinessCase[key];

			foreach (string functionName in functionAttributeNames)
			{
				string status = BasicIncidentLogReader.ExtractValue(line, functionName + "_task_complete");
				if ((! string.IsNullOrEmpty(status))
					&& CONVERT.ParseBool(status, false))
				{
					businessCase.FunctionAttributeNameToSatisfiedTime[functionName] = time;
				}
			}

			string option = BasicIncidentLogReader.ExtractValue(line, "selected_option");
			if (! string.IsNullOrEmpty(option))
			{
				Node optionNode = _gameFile.GetNetworkModel(_round).GetNamedNode(option);
				businessCase.Benefit = optionNode.GetIntAttribute("benefit", 0);
				businessCase.Cost = optionNode.GetIntAttribute("cost", 0);
				businessCase.Option = option;
			}

			string installTimeLeftString = BasicIncidentLogReader.ExtractValue(line, "install_time_left");
			if (! string.IsNullOrEmpty(installTimeLeftString))
			{
				int installTimeLeft = CONVERT.ParseInt(installTimeLeftString);

				if (businessCase.InstallTimeStart == null)
				{
					businessCase.InstallTimeStart = (int) time;
				}

				if (installTimeLeft <= 0)
				{
					businessCase.InstallTimeEnd = (int) time;
				}
			}
		}

		double benefit;
		public double Benefit
		{
			get
			{
				return benefit;
			}
		}

		Dictionary<string, double> functionNameToBenefit; 
		public double GetBenefitForFunction (string functionName)
		{
			return functionNameToBenefit[functionName];
		}

		public string GetFunctionDescription (string functionName)
		{
			return functionNameToDescription[functionName.ToLower()];
		}

		public List<string> FunctionNames
		{
			get
			{
				return functionNames;
			}
		}

		double spend;
		public double Spend
		{
			get
			{
				return spend;
			}
		}

		Dictionary<string, double> functionNameToSpend;
		public double GetSpendForFunction (string functionName)
		{
			return functionNameToSpend[functionName] + functionNameToConsultancySpend[functionName.ToLower()] + _rep.GetCost("opex_" + functionName.ToLower(), _round);
		}

		double profitTarget;
		public double ProfitTarget
		{
			get
			{
				return profitTarget;
			}
		}

        double percentTargetProfit;
        public double PercentTargetProfit
        {
            get
            {
                if (profitTarget > 0)
                {
                    percentTargetProfit = (Profit * 100) / ProfitTarget;
                }
                else
                {
                    percentTargetProfit = profitTarget;
                }

                return percentTargetProfit;
            }
        }


        public double GetProfitForFunction (string functionName)
		{
			return functionNameToBenefit[functionName] - functionNameToSpend[functionName];
		}

		double timeToValue;
		public double TimeToValue
		{
			get
			{
				return timeToValue;
			}
		}

		double mtrs;
        public double Mtrs
		{
			get
			{
                mtrs = GetMtrsForFunction(null);
                return mtrs;
			}
		}

        public double HrMtrs
        {
            get
            {
                return GetMtrsForFunction("HR");
            }
        }

        public double FacMtrs
        {
            get
            {
                return GetMtrsForFunction("FM");
            }
        }

        public double ItMtrs
        {
            get
            {
                return GetMtrsForFunction("IT");
            }
        }

        public double FinLegMtrs
        {
            get
            {
                return GetMtrsForFunction("Other");
            }
        }

		public double GetMtrsForFunction (string functionName)
		{
			double downTimeAccumulator = 0;
			int numberOfOutages = 0;

			foreach (string businessServiceName in businessServiceNameToTimeToState.Keys)
			{
				if (string.IsNullOrEmpty(functionName)
				    || (_gameFile.GetNetworkModel(_round).GetNamedNode(businessServiceName).GetAttribute("function").ToLower() == functionName.ToLower()))
				{
					TimeLog<BusinessServiceState> log = businessServiceNameToTimeToState[businessServiceName];

					double? downTimeStart = null;
                    int count = 0;
					foreach (double time in log.Times)
					{
						if (log[time] == BusinessServiceState.Down)
						{
							if (downTimeStart == null)
							{
								downTimeStart = time;
							}

                            if(count == log.Times.Count - 1) // if last time has business down then treat as closed
                            {
                                
                                int currentTime = _gameFile.GetNetworkModel(_round).GetNamedNode("CurrentTime").GetIntAttribute("seconds", 0);
                                double downTime = currentTime - time;
                                downTimeAccumulator += downTime;
                                numberOfOutages++;
                            }
						}
						else
						{
							if (downTimeStart != null)
							{
								double downTime = time - downTimeStart.Value;
								downTimeAccumulator += downTime;
								numberOfOutages++;
								downTimeStart = null;
							}
						}
                        count++;
					}
				}
			}

			if (numberOfOutages > 0)
			{
				return downTimeAccumulator / numberOfOutages;
			}
			else
			{
				return 0;
			}
		}

		public double GetRevenueForFunction (string functionName)
		{
			return _gameFile.GetNetworkModel(_round).GetNamedNode(functionName).GetDoubleAttribute("revenue", 0);
		}

		public double GetMaxRevenueForFunction (string functionName)
		{
			return _gameFile.GetNetworkModel(_round).GetNamedNode(functionName).GetDoubleAttribute("max_revenue", 0);
		}

		public int GetIncidentsForFunction (string functionName)
		{
			return functionNameToIncidents[functionName.ToLower()];
		}

        public double HrRequests
        {
            get
            {
                return GetIncidentsForFunction("HR");
            }
        }

        public double FacRequests
        {
            get
            {
                return GetIncidentsForFunction("FM");
            }
        }

        public double ItRequests
        {
            get
            {
                return GetIncidentsForFunction("IT");
            }
        }

        public double FinLegRequests
        {
            get
            {
                return GetIncidentsForFunction("Other");
            }
        }

		protected override void biLogReader_CostedEventFound (object sender, string key, string line, double time)
		{
			string id = BasicIncidentLogReader.ExtractValue(line, "incident_id");
			string val = BasicIncidentLogReader.ExtractValue(line, "type");

			if (val.StartsWith("entity fix") && id != string.Empty)
			{
				FixedIncidents.Add(id);

				//we have an incident fix
				if (downedIncidents.ContainsKey(id))
				{
					string curtime = BasicIncidentLogReader.ExtractValue(line, "i_doAfterSecs");
					string downedtime = (string) downedIncidents[id];

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
				}
			}
			else if (val == "prevented_incident")
			{
				PreventedIncidents++;

				if (BasicIncidentLogReader.ExtractValue(line, "caused_by_upgrade") == "true")
				{
					IncidentsPreventedByUpgrades++;
				}
			}
			else if (val == "recurring_incident")
			{
				RecurringIncidents++;

				if (BasicIncidentLogReader.ExtractValue(line, "compliance") == "true")
				{
					ComplianceIncidents++;
				}
			}
			else if (val == "incident")
			{
				if (BasicIncidentLogReader.ExtractValue(line, "compliance") == "true")
				{
					ComplianceIncidents++;
				}

				string incidentType = BasicIncidentLogReader.ExtractValue(line, "incident_type");
				if (incidentType == "1")
				{
					Incidents++;
				}
				else
				{
					Requests++;
				}

				string nodeName = BasicIncidentLogReader.ExtractValue(line, "desc");
				string tag = " Down.";
				nodeName = nodeName.Substring(0, nodeName.IndexOf(tag));
				Node businessService = _gameFile.GetNetworkModel(_round).GetNamedNode(nodeName);
				string functionName = businessService.GetAttribute("function");
				functionNameToIncidents[functionName.ToLower()]++;

				//work out the down time for each incident so can get MTTR
				//get the incident id
				id = BasicIncidentLogReader.ExtractValue(line, "incident_id");
				if (id != string.Empty)
				{
					string curtime = BasicIncidentLogReader.ExtractValue(line, "i_doAfterSecs");

					//add to hashtable with downtime of 0
					if (!downedIncidents.ContainsKey(id))
					{
						downedIncidents.Add(id, curtime);
					}

					if (! incidentDurations.ContainsKey(id))
					{
						incidentDurations.Add(id, (double) 0);
					}

					AllIncidents.Add(id + ";" + curtime);
				}
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
				// Only track the first node touched per fix operation.
				if (eventsfound == 0)
				{
					string nodeName = BasicIncidentLogReader.ExtractValue(line, "desc");
					string tag = " To ";
					nodeName = nodeName.Substring(nodeName.IndexOf(tag) + tag.Length);
					nodeName = nodeName.Substring(0, nodeName.Length - 1);
					LinkNode connection = _gameFile.GetNetworkModel(_round).GetNamedNode(nodeName).GetFirstChildOfType("Connection") as LinkNode;
					if (connection != null)
					{
						Node businessService = connection.From;
						string functionName = businessService.GetAttribute("function");
						functionNameToConsultancyFixes[functionName.ToLower()]++;
					}
				}
				eventsfound++;

				//also need to mark fix for mttr calculation - bug #3137
				//we have an incident fix
				if (downedIncidents.ContainsKey(id))
				{
					string curtime = BasicIncidentLogReader.ExtractValue(line, "i_doAfterSecs");
					string downedtime = (string) downedIncidents[id];

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
			else if (val == "business_case_met")
			{
				businessCaseCosts += CONVERT.ParseDouble(BasicIncidentLogReader.ExtractValue(line, "amount"));
			}
			else if (val == "business_case_penalty")
			{
				businessCasePenalties += CONVERT.ParseDouble(BasicIncidentLogReader.ExtractValue(line, "amount"));
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

		public double Costs
		{
			get
			{
				return Spend;
			}
		}

		public double Position
		{
			get
			{
				return position;
			}
		}

		public double RecurringRequests
		{
			get
			{
				return RecurringIncidents;
			}
		}

		public double SelfFulfilledRequests
		{
			get
			{
				return FirstLineFixes;
			}
		}

		public double AutomatedRequests
		{
			get
			{
				return AutomatedFixes;
			}
		}

		public double? MaxProductivity;
		public double? MinProductivity;

		public double? MeanProductivity
		{
			get
			{
				if (FinalTime > 0)
				{
					return Revenue / FinalTime;
				}
				else
				{
					return null;
				}
			}
		}

		public double ProductivityDrain
		{
			get
			{
                double mtfr = Mtrs;
                if (mtfr == 0)
                {
                    return mtfr;
                }
                else
                {
                    return ((mtfr - syntheticMTFRAverage)*100) / mtfr;
                }
            }
		}

        
    }
}