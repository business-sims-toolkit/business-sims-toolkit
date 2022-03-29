using System.Collections.Generic;
using System.Drawing;
using GameManagement;

namespace GameDetails
{
	public class LogoDetails : GameDetailsSection
	{
	    protected NetworkProgressionGameFile gameFile;
	    protected List<LogoPanel> logoPanels;

		public LogoDetails (NetworkProgressionGameFile gameFile, bool showFacilitatorLogo, bool showTeamLogo)
		{
			this.gameFile = gameFile;

			logoPanels = new List<LogoPanel> ();

			Title = "Logos";

			BuildControls(showFacilitatorLogo, showTeamLogo);

			LoadData();
		}

		protected virtual void BuildControls (bool showFacilitatorLogo, bool showTeamLogo)
		{
			if (showFacilitatorLogo)
			{
				logoPanels.Add(new LogoPanel (gameFile, "Facilitator Logo", "facil_logo.png", "DefFacLogo.png", new Size (140, 70)));
			}

			if (showTeamLogo)
			{
				logoPanels.Add(new LogoPanel (gameFile, "Team Logo", "logo.png", "DefCustLogo.png", new Size (140, 70)));
			}

			DoLayout();
		}

		protected virtual void DoLayout ()
		{
			int y = 10;
			foreach (LogoPanel panel in logoPanels)
			{
				this.panel.Controls.Add(panel);
				panel.Size = new Size(500, panel.ShownImageSize.Height);
				panel.Location = new Point(0, y);
				y = panel.Bottom + 10;
			}

			SetSize(500, y);
		}

		public override void LoadData ()
		{
			foreach (LogoPanel panel in logoPanels)
			{
				panel.LoadData();
			}
		}
	}
}