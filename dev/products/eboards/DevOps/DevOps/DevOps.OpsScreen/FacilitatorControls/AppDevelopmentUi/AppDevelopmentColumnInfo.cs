using System.Collections.Generic;

namespace DevOps.OpsScreen.FacilitatorControls.AppDevelopmentUi
{
    internal enum AppDevelopmentColumn
    {
        ServiceId,
        ServiceIcon,
        ProductChoice,
        DevOneChoice,
        DevTwoChoice,
        TestChoice,
        TestTimeRemaining,
        ReleaseChoice,
        EnclosureChoice,
        Status,
        Undo,
        Abort
    }
    internal class AppDevelopmentColumnInfo
    {
        public static IReadOnlyList<AppDevelopmentColumn> ColumnOrder => new List<AppDevelopmentColumn>
        {
            AppDevelopmentColumn.ServiceId,
            AppDevelopmentColumn.ServiceIcon,
            AppDevelopmentColumn.ProductChoice,
            AppDevelopmentColumn.DevOneChoice,
            AppDevelopmentColumn.DevTwoChoice,
            AppDevelopmentColumn.TestChoice,
            AppDevelopmentColumn.TestTimeRemaining,
            AppDevelopmentColumn.ReleaseChoice,
            AppDevelopmentColumn.EnclosureChoice,
            AppDevelopmentColumn.Status,
            //AppDevelopmentColumn.Undo,
            //AppDevelopmentColumn.Abort
        };

        public static IReadOnlyDictionary<AppDevelopmentColumn, string> ColumnToTitle { get; } =
            new Dictionary<AppDevelopmentColumn, string>
            {
                {AppDevelopmentColumn.ServiceId, "ID" },
                {AppDevelopmentColumn.ServiceIcon, " "},
                {AppDevelopmentColumn.ProductChoice, "Product" },
                {AppDevelopmentColumn.DevOneChoice, "Dev One"},
                {AppDevelopmentColumn.DevTwoChoice, "Dev Two"},
                {AppDevelopmentColumn.TestChoice, "Test"},
                {AppDevelopmentColumn.TestTimeRemaining, "Test Time"},
                {AppDevelopmentColumn.ReleaseChoice, "Release"},
                {AppDevelopmentColumn.EnclosureChoice, "Enclosure"},
                {AppDevelopmentColumn.Status, "Status"}
            };

        // TODO column width fractions and alignments ?? 

    }
}
