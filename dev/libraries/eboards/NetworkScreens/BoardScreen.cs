using System;
using System.Windows.Forms;
using System.Drawing;

using RacingGUI;
using GameManagement;
using LibCore;


namespace NetworkScreens
{
	/// <summary>
	/// Summary description for BoardScreen.
	/// </summary>
	public class BoardScreen : Panel
	{
		protected BoardView board;

		public BoardScreen(NetworkProgressionGameFile gameFile)
		{
			board = new BoardView(gameFile.NetworkModel, AppInfo.TheInstance.Location + "\\data\\board.xml",
				AppInfo.TheInstance.Location + "\\data\\visual.xml");
			this.Controls.Add(board);

			this.Resize += new EventHandler(BoardScreen_Resize);
		}

		public void readNetwork()
		{
			board.readNetwork();
		}

		private void BoardScreen_Resize(object sender, EventArgs e)
		{
			DoSize();
		}

		protected void DoSize()
		{
			board.Size = new Size(this.Width,this.Height);
			board.Location = new Point(0,0);
		}
	}
}
