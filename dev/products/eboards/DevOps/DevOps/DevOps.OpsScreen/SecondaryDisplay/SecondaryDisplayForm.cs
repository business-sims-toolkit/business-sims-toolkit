using System;
using System.Windows.Forms;

using DevOps.ReportsScreen;
using GameManagement;
using LibCore;

namespace DevOps.OpsScreen.SecondaryDisplay
{
    internal partial class SecondaryDisplayForm : Form
    {
        public SecondaryDisplayForm(NetworkProgressionGameFile gameFile)
        {
            FormBorderStyle = FormBorderStyle.None;

            displayPanel = new SecondaryDisplayPanel(gameFile);
            Controls.Add(displayPanel);
            
            InitializeComponent();
        }

	    public NetworkProgressionGameFile GameFile
	    {
		    set => displayPanel.GameFile = value;
	    }

        public void ShowGameScreen (GameScreenPanel newGameScreen)
        {
            displayPanel.ShowGameScreen(newGameScreen);
            
        }

        public void ShowReportScreen (ReportsScreenPanel newReportsScreen)
        {
            displayPanel.ShowReportsScreen(newReportsScreen);

        }

        protected override CreateParams CreateParams
        {
            get
            {
                var createParams = base.CreateParams;

                createParams.Style &= ~0x00C0000; // remove WS_CAPTION
                createParams.Style |= 0x00040000; // include WS_SIZEBOX

                return createParams;
            }
        }

        protected override void OnVisibleChanged (EventArgs e)
        {
            base.OnVisibleChanged(e);

            if (Visible)
            {
                DoSize();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.Button == MouseButtons.Left)
            {
                ((Form)TopLevelControl).DragMove();
            }
        }

        protected override void OnSizeChanged (EventArgs e)
        {
            DoSize();
        }

        void DoSize ()
        {
            displayPanel.Size = ClientSize;
        }
        
        readonly SecondaryDisplayPanel displayPanel;
    }
}
