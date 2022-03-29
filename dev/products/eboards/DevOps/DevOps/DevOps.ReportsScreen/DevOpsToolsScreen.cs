using System.Windows.Forms;
using CommonGUI;

using GameManagement;

using LibCore;

using NetworkScreens;

using ReportBuilder;

namespace DevOps.ReportsScreen
{
    public class DevOpsToolsScreen : FlashToolsScreen
    {
        public DevOpsToolsScreen (NetworkProgressionGameFile gameFile, Control gamePanel, bool enableOptions, 
            SupportSpendOverrides spendOverrides, bool useBoardView = true) : 
            base(gameFile, gamePanel, enableOptions, spendOverrides, useBoardView)
        {
            tabBar.AddTab("Customer Satisfaction Score", (int) PanelToShow.PathfinderSurvey, true);
        }

        protected override PathFinderSurveyCardWizard CreatePathFinderSurveyCardWizard()
        {
            return new PathFinderSurveyCardWizard(_gameFile, "nps_survey_wizard.xml");
        }
    }   
}