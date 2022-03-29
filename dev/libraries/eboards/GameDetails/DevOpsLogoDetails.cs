using System.Drawing;
using GameManagement;

namespace GameDetails
{
    public class DevOpsLogoDetails : LogoDetails
    {
        public DevOpsLogoDetails(NetworkProgressionGameFile gameFile, bool showFacilitatorLogo, bool showTeamLogo)
			: base(gameFile, showFacilitatorLogo, showTeamLogo)
        {
        }

        protected override void BuildControls(bool showFacilitatorLogo, bool showTeamLogo)
        {
            if (showFacilitatorLogo)
            {
                logoPanels.Add(new LogoPanel(gameFile, "Client Logo", "logo.png", "DefCustLogo.png", new Size (300, 300)));
            }

			DoLayout();
        }
    }
}