using System;
using System.Drawing;
using System.Windows.Forms;
using GameManagement;
using LibCore;
using ReportBuilder;

namespace NetworkScreens
{
	/// <summary>
	/// Summary description for FlashToolsScreen.
	/// </summary>
	public class FlashToolsScreen : ToolsScreen
	{
		bool suppressnetworkIcons = false;

	    bool useBoardView;

		protected override void BuildBoardView()
		{
            if (useBoardView)
            {
                board = new GameBoardView.GameBoardViewWithController(_gameFile.NetworkModel);
                board.BackColor = Color.Black;
                this.SuspendLayout();
                this.Controls.Add(board);
                this.ResumeLayout(false);
            }
        }

		public FlashToolsScreen(NetworkProgressionGameFile gameFile, Control gamePanel, bool enableOptions, SupportSpendOverrides spendOverrides, bool useBoardView = true)
			: base (gameFile, gamePanel, enableOptions, spendOverrides)
		{
		    this.useBoardView = useBoardView;
            BuildBoardView();
            RefreshScreen();
			this.VisibleChanged += FlashToolsScreen_VisibleChanged;
		}

		void FlashToolsScreen_VisibleChanged(object sender, EventArgs e)
		{
			// : workaround for bug 3549: on becoming visible, reset the zoom
			// on the board view, so that the flash icons don't get repositioned crazily.
			ResetView();

			if (assessmentsPanel != null)
			{
				RefreshAssessments();
			}
		}

		public override void ResetView ()
		{
			if (board != null)
			{
				board.ResetView();
			}
		}

		public void SuppressNetworkIcons (bool suppress)
		{
			suppressnetworkIcons = suppress;
			readNetwork();
		}

		public override void readNetwork ()
		{
			if (board != null)
			{
				board.ReadNetwork(_gameFile.NetworkModel);
			}
		}
	}
}