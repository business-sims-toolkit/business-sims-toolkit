using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CommonGUI;
using LibCore;
using ReportGenFunc = System.Func<int, string, LibCore.ComboBoxOption [], string>;

namespace DevOpsReportBuilders
{
	public enum Report
	{
		BusinessScorecard,
		ProblemStatementsScorecard,
		NewAppsGantt,
		Complaints,
		ProductQuality,
		OperationsScorecard,
		IncidentsGantt,
		CpuUsageReport,
		ServerUsageReport,
		Network,
		Maturity,
		ProcessScorecard,
		CustomerSatisfactionReport,
		CustomerValueReport,
		WorkInProgress
	}

	public class ReportBuilderCache
	{
		public ReportBuilderCache ()
		{
			reportIdToFilePath = new Dictionary<ReportId, string>();
			reportToReportGenerators = new Dictionary<Report, ReportGenFunc>();
		}

		public void RegisterReportGenerator (Report report, ReportGenFunc reportGen)
		{
			reportToReportGenerators[report] = reportGen;
		}

		public void MarkRoundAsOutOfDate (int round)
		{
			var outOfDateReports = reportIdToFilePath.Where(kvp => kvp.Key.Round == round).Select(kvp => kvp.Key).ToList();

			foreach (var reportId in outOfDateReports)
			{
				reportIdToFilePath.Remove(reportId);
			}
		}

		public bool GenerateReport (Report report, int round, string business, out string reportFilepath, params ComboBoxOption [] additionalFilters)
		{
			Debug.Assert(reportToReportGenerators.ContainsKey(report));

			if (!reportToReportGenerators.ContainsKey(report))
			{
				reportFilepath = null;
				return false;
			}

			var reportId = new ReportId(report, round, business, additionalFilters);

			if (!reportIdToFilePath.ContainsKey(reportId))
			{
				reportIdToFilePath[reportId] = reportToReportGenerators[report].Invoke(round, business, additionalFilters);
			}

			var reportFile = reportIdToFilePath[reportId];

			if (! File.Exists(reportFile))
			{
				reportFilepath = null;
				return false;
			}

			reportFilepath = reportFile;

			return true;
		}

		readonly Dictionary<ReportId, string> reportIdToFilePath;
		readonly Dictionary<Report, ReportGenFunc> reportToReportGenerators;

		struct ReportId : IEquatable<ReportId>
		{
			public Report Report { get; }
			public int Round { get; }
			public string Business { get; }
			public List<ComboBoxOption> AdditionalFilters { get; }

			public ReportId (Report report, int round, string business, ComboBoxOption [] additionalFilters = null)
			{
				Report = report;
				Round = round;
				Business = business;
				AdditionalFilters = (additionalFilters != null) ? (new List<ComboBoxOption> (additionalFilters)) : null;
			}

			public bool Equals (ReportId other)
			{
				return (Report == other.Report)
				       && (Round == other.Round)
				       && string.Equals(Business, other.Business)
				       && (((AdditionalFilters == null) && (other.AdditionalFilters == null))
				           || ((AdditionalFilters != null) && (other.AdditionalFilters != null) &&
				               AdditionalFilters.Zip(other.AdditionalFilters, (a, b) => a == b).All(a => a)));
			}

			public override bool Equals (object obj)
			{
				if (ReferenceEquals(null, obj)) return false;
				return obj is ReportId id && Equals(id);
			}

			public override int GetHashCode ()
			{
				unchecked
				{
					var hashCode = (int) Report;
					hashCode = (hashCode * 397) ^ Round.GetHashCode();
					hashCode = (hashCode * 397) ^ (Business != null ? Business.GetHashCode() : 0);
					hashCode = (hashCode * 397) ^ (AdditionalFilters != null ? AdditionalFilters.GetHashCode() : 0);
					return hashCode;
				}
			}
		}
	}
}

