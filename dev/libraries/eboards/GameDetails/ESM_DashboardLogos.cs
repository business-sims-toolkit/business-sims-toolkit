using System.Collections.Generic;
using System.Drawing;
using GameManagement;

namespace GameDetails
{
    public class ESM_DashboardLogos: GameDetailsSection
    {
        protected NetworkProgressionGameFile gameFile;
        protected List<LogoPanel> logoPanels;

        public ESM_DashboardLogos(NetworkProgressionGameFile gameFile,string sectionTitle)
        {
            this.gameFile = gameFile;

            logoPanels = new List<LogoPanel>();

            Title = sectionTitle;

            BuildControls();

            LoadData();
          
        }
        void BuildControls()
        {
            logoPanels.Add(new LogoPanel(gameFile, "Round 1", "dashboard_round1_logo.png", "DefDashboardBlankLogo.png", new Size(100, 100)));
            logoPanels.Add(new LogoPanel(gameFile, "Round 2", "dashboard_round2_logo.png", "DefDashboardBlankLogo.png", new Size(100, 100)));
            logoPanels.Add(new LogoPanel(gameFile, "Round 3", "dashboard_round3_logo.png", "DefDashboardBlankLogo.png", new Size(100, 100)));

            int y = 10;
            foreach (LogoPanel panel in logoPanels)
            {
                this.panel.Controls.Add(panel);
                panel.Size = new Size(500, 100);
                panel.Location = new Point(0, y);
                y = panel.Bottom + 10;
            }

            SetSize(500, y);
        }

        public override void LoadData()
        {
            foreach(LogoPanel lp in logoPanels)
            {
                lp.LoadData();
            }
        }

        public override bool SaveData()
        {
            bool isSaveSuccessful = false;
            foreach(LogoPanel lp in logoPanels)
            {
                lp.SaveData();
                isSaveSuccessful = true;
            }

            return isSaveSuccessful;
        }
    }
}
