using System.Collections.Generic;

namespace DevOps.OpsScreen.FacilitatorControls
{
    internal enum IncidentColumns
    {
        Incident,
        Event,
        Service,
        Impact,
        Failure,
        Question,
        Answer
    }

    internal class IncidentColumnInfo
    {
        public static IReadOnlyList<IncidentColumns> ColumnOrder { get; } = new List<IncidentColumns>
        {
            IncidentColumns.Incident,
            IncidentColumns.Event,
            IncidentColumns.Service,
            IncidentColumns.Impact,
            IncidentColumns.Failure,
            IncidentColumns.Question,
            IncidentColumns.Answer
        };

        public static IReadOnlyDictionary<IncidentColumns, int> ColumnToWidthFactor { get; } = 
            new Dictionary<IncidentColumns, int>
            {
                { IncidentColumns.Incident, 1},
                { IncidentColumns.Event, 1},
                { IncidentColumns.Service, 3},
                { IncidentColumns.Impact, 2},
                { IncidentColumns.Failure, 3},
                { IncidentColumns.Question, 1},
                { IncidentColumns.Answer, 2},
            };

        public static IReadOnlyDictionary<IncidentColumns, string> ColumnToTitle { get; } =
            new Dictionary<IncidentColumns, string>
            {
                { IncidentColumns.Incident, "Incident"},
                { IncidentColumns.Event, "Event"},
                { IncidentColumns.Service, "Service"},
                { IncidentColumns.Impact, "Impact"},
                { IncidentColumns.Failure, "Failure"},
                { IncidentColumns.Question, "Question"},
                { IncidentColumns.Answer, "Answer"},
            };
        // TODO enum.ToString() could be used instead but this way allows for customisability ... or something
    }
}
