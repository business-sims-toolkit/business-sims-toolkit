using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Algorithms;
using CoreUtils;
using GameManagement;
using LibCore;
using LibCore.Enums;
using Logging;
using Network;

namespace ReportBuilder
{
	public class CustomerValueReportBuilder
	{
		readonly NetworkProgressionGameFile gameFile;
		readonly int [] rounds;
		readonly int? currentRound;
		readonly int maxMetrics;
		readonly Dictionary<int, RoundResults> roundToResults;
		int revenue;
		int potentialRevenue;

		class RoundResults
		{
			public List<string> LiveFeatureNames { get; set; }
			public TimeLog<List<MetricChanges>> TimeToMetricChanges { get; set; }
		}

		class MetricChange
		{
			public double Expected { get; set; }
			public double Actual { get; set; }
		}

		class MetricChanges
		{
			public bool IsRedevelopment { get; set; }
			public string NodeName { get; set; }
			public int ProblemStatementNumber { get; set; }
			public string Title { get; set; }
			public string FeatureName { get; set; }
			public string ServiceName { get; set; }
			public Dictionary<int, MetricChange> MetricNumberToChange { get; set; }
		}

		public CustomerValueReportBuilder (NetworkProgressionGameFile gameFile, int round, int maxMetrics)
		{
			this.gameFile = gameFile;

			if (round == (gameFile.GetTotalRounds() + 1))
			{
				rounds = Enumerable.Range(1, gameFile.LastRoundPlayed).Reverse().ToArray();
			}
			else
			{
				rounds = new [] { round };
			}

			this.maxMetrics = maxMetrics;

			roundToResults = new Dictionary<int, RoundResults> ();

			foreach (var i in rounds)
			{
				currentRound = i;
				revenue = 0;
				roundToResults.Add(i, new RoundResults { TimeToMetricChanges = new TimeLog<List<MetricChanges>>(), LiveFeatureNames = new List<string> ()});
				var logReader = new BasicIncidentLogReader(gameFile.GetRoundFile(i, "NetworkIncidents.log", GameFile.GamePhase.OPERATIONS));
				logReader.WatchCreatedNodes("BeginNewServicesInstall", beginNewServicesInstall_CreatedNode);
				logReader.WatchCreatedNodes($"Round {i} Completed New Services", completedNodes_NodeCreated);
				logReader.WatchApplyAttributes("Revenue", revenue_ApplyAttributes);
				logReader.Run();
			}
			currentRound = null;
		}

		string GetDeltaImageName (double delta)
		{
			if (delta > 0)
			{
				return "positive_delta";
			}
			else if (delta < 0)
			{
				return "negative_delta";
			}
			else
			{
				return null;
			}
		}

		public string BuildReport (ComboBoxOption [] extraFilters)
		{
			var metricNumber = ((int?) extraFilters[0]?.Tag) ?? 0;
			var yScale = SkinningDefs.TheInstance.GetIntData($"metric_{metricNumber}_sign", 1);

			var sumVerticalValues = metricNumber != 0;
			var showControlPoints = metricNumber != 0;
			var showDeltaImage = metricNumber != 0;
			var showTitles = metricNumber != 0;

			var expectedTitle = metricNumber == 0 ? "Target" : "Expected";

			var verticalValueFormatting = metricNumber == 2 ? NumberFormatting.None : NumberFormatting.PaddedThousands;

			string service = null;
			if (extraFilters.Length > 1)
			{
				service = ((Node) extraFilters[1].Tag)?.GetAttribute("name");
			}

			var stepped = true;
			if (extraFilters.Length > 2)
			{
				stepped = (bool) extraFilters[2].Tag;
			}

			var xml = BasicXmlDocument.Create();
			var root = xml.AppendNewChild("linegraph");
			var category = root.AppendNewChild("category");

			var model = gameFile.NetworkModel;

			var preplayNode = model.GetNamedNode("preplay_control");
			var timeNode = model.GetNamedNode("CurrentTime");
			
			var gameplayInPreplay = preplayNode.GetBooleanAttribute("allow_gameplay", false);

			var timeMin = gameplayInPreplay ? -(preplayNode.GetIntAttribute("time_ref", 0) / 60) : 0;
			var timeMax = timeNode.GetIntAttribute("round_duration", 1500) / 60;

			var xAxis = category.AppendNewChild("x_axis");
			xAxis.AppendAttribute("title", "Time (Minutes)");
			
			xAxis.AppendAttribute("max", timeMax);
			xAxis.AppendAttribute("interval", "5");

			var yAxis = category.AppendNewChild("y_axis");
			yAxis.AppendAttribute("title", "Value");
			yAxis.AppendAttribute("min", 0);
			yAxis.AppendAttribute("number_formatting", verticalValueFormatting.ToString());

			var content = category.AppendNewChild("content");

			double xMin = 0;
			foreach (var round in rounds)
			{
				if (! roundToResults[round].TimeToMetricChanges.IsEmpty)
				{
					xMin = Math.Floor(Math.Min(xMin, roundToResults[round].TimeToMetricChanges.FirstTime / 60));
				}
			}

			// Don't show negative time if there's nothing that has started before 0
			if (xMin >= 0)
			{
				timeMin = 0;
			}

			xAxis.AppendAttribute("min", timeMin);

			double? yMax = null;

			var actualColours = new [] { Color.FromArgb(224, 63, 33), Color.FromArgb(47, 192, 159), Color.FromArgb(64, 158, 236) };
			var expectedColours = actualColours.Select(a => Maths.Lerp(0.25f, a, Color.White)).ToList();

			var order = 1;
			foreach (var round in rounds)
			{
				var finalTime = gameFile.GetNetworkModel(round).GetNamedNode("CurrentTime").GetIntAttribute("seconds", 0) / 60.0f;

				var roundText = "";
				if (rounds.Length > 1)
				{
					roundText = $"Round {round} ";
				}

				var colourIndex = 0;
				if (rounds.Length > 1)
				{
					colourIndex = round % actualColours.Length;
				}

				var actualSeries = content.AppendNewChild("series");
				actualSeries.AppendAttribute("title", $"{roundText}Actual");
				actualSeries.AppendAttribute("order", order);
				actualSeries.AppendAttribute("colour", actualColours[colourIndex]);
				actualSeries.AppendAttribute("line_style", "Solid");
				actualSeries.AppendAttribute("line_width", 1);
				actualSeries.AppendAttribute("fill_area", true);
				actualSeries.AppendAttribute("include_steps", stepped);
				actualSeries.AppendAttribute("step_coordinate_order", "YThenX");
				actualSeries.AppendAttribute("sum_vertical_values", sumVerticalValues);
				order++;

				var expectedSeries = content.AppendNewChild("series");
				expectedSeries.AppendAttribute("title", $"{roundText}{expectedTitle}");
				expectedSeries.AppendAttribute("order", order);
				expectedSeries.AppendAttribute("colour", expectedColours[colourIndex]);
				expectedSeries.AppendAttribute("line_style", "Dash");
				expectedSeries.AppendAttribute("line_width", 1);
				expectedSeries.AppendAttribute("fill_area", false);
				expectedSeries.AppendAttribute("include_steps", stepped);
				expectedSeries.AppendAttribute("step_coordinate_order", "YThenX");
				expectedSeries.AppendAttribute("sum_vertical_values", sumVerticalValues);
				order++;

				// Start the lines off.
				{
					var expectedEntry = expectedSeries.AppendNewChild("entry");
					expectedEntry.AppendAttribute("show_control_point", false);
					expectedEntry.AppendAttribute("line_end_point", true);
					expectedEntry.AppendAttribute("horizontal_value", xMin);
					expectedEntry.AppendAttribute("vertical_value", 0);

					var actualEntry = actualSeries.AppendNewChild("entry");
					actualEntry.AppendAttribute("show_control_point", false);
					actualEntry.AppendAttribute("line_end_point", true);
					actualEntry.AppendAttribute("horizontal_value", xMin);
					actualEntry.AppendAttribute("vertical_value", 0);
				}

				var timeToMetricChanges = roundToResults[round].TimeToMetricChanges;
				var liveFeatureNames = roundToResults[round].LiveFeatureNames;

				double lastActualValue = 0;

				foreach (var t in timeToMetricChanges.Times)
				{
					foreach (var changes in timeToMetricChanges[t])
					{
						if ((! string.IsNullOrEmpty(changes.FeatureName))
							&& (! liveFeatureNames.Contains(changes.FeatureName)))
						{
							continue;
						}

						if ((metricNumber != 0) && (! changes.MetricNumberToChange.ContainsKey(metricNumber)))
						{
							continue;
						}

						if (changes.IsRedevelopment)
						{
							continue;
						}

						if ((service != null)
						    && (service != changes.ServiceName)
							&& ! string.IsNullOrEmpty(changes.ServiceName))
						{
							continue;
						}

						var actualValue = lastActualValue;
						if (changes.MetricNumberToChange.ContainsKey(metricNumber))
						{
							actualValue = changes.MetricNumberToChange[metricNumber].Actual;
							lastActualValue = actualValue;
						}

						var actualEntry = actualSeries.AppendNewChild("entry");
						bool pointIsRelevantToOtherMetric = ((metricNumber == 0)
						                                     && changes.MetricNumberToChange.Keys.Any(mn => mn != 0));
						actualEntry.AppendAttribute("title", changes.Title);
						actualEntry.AppendAttribute("show_title", showTitles || pointIsRelevantToOtherMetric);
						actualEntry.AppendAttribute("show_control_point", showControlPoints || pointIsRelevantToOtherMetric);
						actualEntry.AppendAttribute("horizontal_value", t / 60.0f);
						actualEntry.AppendAttribute("vertical_value", actualValue * yScale);

						var entryMax = actualValue;

						if (changes.MetricNumberToChange.ContainsKey(metricNumber))
						{
							var expectedEntry = expectedSeries.AppendNewChild("entry");
							expectedEntry.AppendAttribute("title", changes.Title);
							expectedEntry.AppendAttribute("show_control_point", showControlPoints);
							expectedEntry.AppendAttribute("horizontal_value", t / 60.0f);
							expectedEntry.AppendAttribute("vertical_value", changes.MetricNumberToChange[metricNumber].Expected * yScale);

							var deltaImage = GetDeltaImageName(yScale * (changes.MetricNumberToChange[metricNumber].Actual -
							                                             changes.MetricNumberToChange[metricNumber].Expected));
							actualEntry.AppendAttribute("show_delta_image", showDeltaImage && ! string.IsNullOrEmpty(deltaImage));

							if (! string.IsNullOrEmpty(deltaImage))
							{
								actualEntry.AppendAttribute("delta_image", deltaImage);
							}

							entryMax = Math.Max(entryMax, changes.MetricNumberToChange[metricNumber].Expected);
						}

						entryMax *= yScale;
						if (yMax.HasValue)
						{
							yMax = Math.Max(yMax.Value, entryMax);
						}
						else
						{
							yMax = entryMax;
						}
					}
				}

				// Finish the lines off.
				{
					var expectedEntry = expectedSeries.AppendNewChild("entry");
					expectedEntry.AppendAttribute("show_control_point", false);
					expectedEntry.AppendAttribute("line_end_point", true);
					expectedEntry.AppendAttribute("horizontal_value", finalTime);
					expectedEntry.AppendAttribute("vertical_value", 0);

					var actualEntry = actualSeries.AppendNewChild("entry");
					actualEntry.AppendAttribute("show_control_point", false);
					actualEntry.AppendAttribute("line_end_point", true);
					actualEntry.AppendAttribute("horizontal_value", finalTime);
					actualEntry.AppendAttribute("vertical_value", 0);
				}
			}

			if (yMax == null)
			{
				yMax = 10;
			}

			yMax = Maths.RoundToNiceInterval(yMax.Value);
			yAxis.AppendAttribute("max", yMax.Value);
			yAxis.AppendAttribute("interval", yMax.Value / 10);

			string filePath;
			if (rounds.Length == 1)
			{
				filePath = gameFile.GetRoundFile(rounds[0], "CustomerValueReport.xml", GameFile.GamePhase.OPERATIONS);
			}
			else
			{
				filePath = gameFile.GetGlobalFile("CustomerValueReport.xml");
			}

			xml.Save(filePath);
			return filePath;
		}

		void beginNewServicesInstall_CreatedNode (object sender, string key, string line, double time)
		{
			if (BasicIncidentLogReader.ExtractBoolValue(line, "is_hidden_in_reports") ?? false)
			{
				return;
			}

			var beginName = BasicIncidentLogReader.ExtractValue(line, "name");
			var serviceName = BasicIncidentLogReader.ExtractValue(line, "biz_service_function");
			var serviceId = BasicIncidentLogReader.ExtractValue(line, "service_id");
			var problemStatementNumber = BasicIncidentLogReader.ExtractIntValue(line, "problem_statement").Value;

			var metricChanges = new MetricChanges
			{
				NodeName = beginName.Substring("Begin ".Length),
				ProblemStatementNumber = problemStatementNumber,
				FeatureName = serviceId,
				Title = serviceId,
				ServiceName = serviceName,
				MetricNumberToChange = new Dictionary<int, MetricChange> ()
			};
			for (int i = 1; i <= maxMetrics; i++)
			{
				var expectedChange = BasicIncidentLogReader.ExtractDoubleValue(line, $"metric_{i}_change_expected");
				var actualChange = BasicIncidentLogReader.ExtractDoubleValue(line, $"metric_{i}_change_actual");

				if (expectedChange.HasValue || actualChange.HasValue)
				{
					metricChanges.MetricNumberToChange.Add(i, new MetricChange { Actual = actualChange.Value, Expected = expectedChange.Value });
				}
			}

			var timeToMetricChanges = roundToResults[currentRound.Value].TimeToMetricChanges;

			if (! timeToMetricChanges.ContainsTime(time))
			{
				timeToMetricChanges.Add(time, new List<MetricChanges>());
			}
			timeToMetricChanges[time].Add(metricChanges);

			var reader = (BasicIncidentLogReader) sender;
			reader.WatchApplyAttributes(beginName, beginNewService_ApplyAttributes);
		}

		void completedNodes_NodeCreated (object sender, string key, string line, double time)
		{
			var featureName = BasicIncidentLogReader.ExtractValue(line, "service_id");

			var liveFeatureNames = roundToResults[currentRound.Value].LiveFeatureNames;
			if (! liveFeatureNames.Contains(featureName))
			{
				liveFeatureNames.Add(featureName);
			}
		}

		void revenue_ApplyAttributes (object sender, string key, string line, double time)
		{
			var newRevenue = BasicIncidentLogReader.ExtractIntValue(line, "revenue");

			if (newRevenue == null)
			{
				return;
			}
			var newPotentialRevenue = BasicIncidentLogReader.ExtractIntValue(line, "revenue_potential") ?? 0;

			var metricChanges = new MetricChanges
			{
				NodeName = "Revenue",
				ProblemStatementNumber = 1,
				FeatureName = "",
				Title = "",
				ServiceName = "",
				MetricNumberToChange = new Dictionary<int, MetricChange> ()
			};
			metricChanges.MetricNumberToChange.Add(0, new MetricChange { Actual = newRevenue.Value - revenue, Expected = newPotentialRevenue - potentialRevenue });

			revenue = newRevenue.Value;
			potentialRevenue = newPotentialRevenue;

			var timeToMetricChanges = roundToResults[currentRound.Value].TimeToMetricChanges;
			if (! timeToMetricChanges.ContainsTime(time))
			{
				timeToMetricChanges.Add(time, new List<MetricChanges>());
			}
			timeToMetricChanges[time].Add(metricChanges);
		}

		void ChangeMetricChangesTime (MetricChanges changes, double newTime)
		{
			var timeToMetricChanges = roundToResults[currentRound.Value].TimeToMetricChanges;

			double? oldTime = null;
			foreach (var t in timeToMetricChanges.Times)
			{
				if (timeToMetricChanges[t].Contains(changes))
				{
					oldTime = t;
					break;
				}
			}

			if (oldTime.HasValue)
			{
				timeToMetricChanges[oldTime.Value].Remove(changes);
			}

			if (! timeToMetricChanges.ContainsTime(newTime))
			{
				timeToMetricChanges.Add(newTime, new List<MetricChanges> ());
			}

			timeToMetricChanges[newTime].Add(changes);
		}

		void beginNewService_ApplyAttributes (object sender, string key, string line, double time)
		{
			if (BasicIncidentLogReader.ExtractBoolValue(line, "is_hidden_in_reports") ?? false)
			{
				return;
			}

			var beginName = BasicIncidentLogReader.ExtractValue(line, "i_name");
			var nodeName = beginName.Substring("Begin ".Length);

			var timeToMetricChanges = roundToResults[currentRound.Value].TimeToMetricChanges;
			MetricChanges metricChanges = null;

			if (BasicIncidentLogReader.ExtractValue(line, "status") == "redevelop")
			{
				MetricChanges prototypeMetricChanges = null;
				foreach (var changes in timeToMetricChanges.Values)
				{
					foreach (var change in changes)
					{
						if (change.NodeName == nodeName)
						{
							prototypeMetricChanges = change;
						}
					}
				}

				metricChanges = new MetricChanges
				{
					IsRedevelopment = true,
					NodeName = nodeName,
					ProblemStatementNumber = prototypeMetricChanges.ProblemStatementNumber,
					FeatureName = prototypeMetricChanges.FeatureName,
					Title = prototypeMetricChanges.Title,
					ServiceName = prototypeMetricChanges.ServiceName,
					MetricNumberToChange = new Dictionary<int, MetricChange> ()
				};

				foreach (var metricNumber in prototypeMetricChanges.MetricNumberToChange.Keys)
				{
					metricChanges.MetricNumberToChange.Add(metricNumber,
						new MetricChange
						{
							Actual = 0,
							Expected = 0
						});
				}
			}
			else
			{
				foreach (var changeList in timeToMetricChanges.Values)
				{
					foreach (var changes in changeList)
					{
						if (changes.NodeName == nodeName)
						{
							metricChanges = changes;
							break;
						}
					}
				}
			}

			for (int i = 1; i <= maxMetrics; i++)
			{
				var actualChange = BasicIncidentLogReader.ExtractDoubleValue(line, $"metric_{i}_change_actual");
				if (actualChange.HasValue)
				{
					metricChanges.MetricNumberToChange[i].Actual = actualChange.Value;
				}
			}

			ChangeMetricChangesTime(metricChanges, time);
		}
	}
}