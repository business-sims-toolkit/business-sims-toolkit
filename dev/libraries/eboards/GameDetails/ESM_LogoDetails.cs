using System.Drawing;
using GameManagement;

namespace GameDetails
{
    public class ESM_LogoDetails : LogoDetails
    {
        public ESM_LogoDetails (NetworkProgressionGameFile gameFile, bool showFacilitatorLogo, bool showTeamLogo)
			: base(gameFile, showFacilitatorLogo, showTeamLogo)
        {
        }

        protected override void BuildControls(bool showFacilitatorLogo, bool showTeamLogo)
        {
            if (showFacilitatorLogo)
            {
                logoPanels.Add(new LogoPanel(gameFile, "Facilitator Logo", "facil_logo.png", "DefFacLogo.png", new Size(100, 100)));
            }

            if (showTeamLogo)
            {
                logoPanels.Add(new LogoPanel(gameFile, "Team Logo", "logo.png", "DefCustLogo.png", new Size(100, 100)));
            }

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
    }
}
