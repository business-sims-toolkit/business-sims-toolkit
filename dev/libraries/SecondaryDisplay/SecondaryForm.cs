using System;
using System.Drawing;
using System.Windows.Forms;

using GameManagement;
using LibCore;

namespace SecondaryDisplay
{
	public partial class SecondaryForm : Form
	{
		public SecondaryForm(NetworkProgressionGameFile gameFile)
		{
			FormBorderStyle = FormBorderStyle.None;

			displayPanel = new SecondaryPanel(gameFile);
			Controls.Add(displayPanel);

			InitializeComponent();
		}
		
		public NetworkProgressionGameFile GameFile
		{
			set => displayPanel.GameFile = value;
		}

		public void ShowGameScreen(Control newGameScreen)
		{
			displayPanel.ShowGameScreen(newGameScreen);

		}

		public void ShowReportScreen(Control newReportsScreen)
		{
			displayPanel.ShowReportsScreen(newReportsScreen);
		}

		public void ShowTransitionScreen (Control transitionScreen)
		{
			displayPanel.ShowTransitionScreen(transitionScreen);
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

		protected override void OnVisibleChanged(EventArgs e)
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

		protected override void OnSizeChanged(EventArgs e)
		{
			DoSize();
		}

		void DoSize()
		{
			displayPanel.Size = ClientSize;
		}

		readonly SecondaryPanel displayPanel;

		public static SecondaryForm CreateSecondaryForm(NetworkProgressionGameFile gameFile)
		{
			var secondaryScreen = Screen.AllScreens[0];

			if (CcdWrapper.GetDisplayConfigBufferSizes(CcdWrapper.QueryDisplayFlags.OnlyActivePaths,
				    out var numPathArrayElements, out var numModeInfoArrayElements) == 0)
			{
				var pathInfoArray = new CcdWrapper.DisplayConfigPathInfo[numPathArrayElements];
				var modeInfoArray = new CcdWrapper.DisplayConfigModeInfo[numModeInfoArrayElements];

				CcdWrapper.QueryDisplayConfig(CcdWrapper.QueryDisplayFlags.DatabaseCurrent,
					ref numPathArrayElements, pathInfoArray, ref numModeInfoArrayElements, modeInfoArray,
					out var currentTopologyId);

				if (Screen.AllScreens.Length > 1 && currentTopologyId == CcdWrapper.DisplayConfigTopologyId.Extend)
				{
					secondaryScreen = Screen.AllScreens[1];
				}
			}

			var secondaryDisplay = new SecondaryForm(gameFile)
			{
				StartPosition = FormStartPosition.Manual,
				ShowInTaskbar = false,
				MinimumSize = new Size(1024, 768),
				Size = new Size(secondaryScreen.Bounds.Width, Math.Min(768, secondaryScreen.Bounds.Height)),
				Location = secondaryScreen.Bounds.Location,
				Icon = Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetEntryAssembly()?.Location),
				Text = Application.ProductName + " (Public)"
			};
			secondaryDisplay.Hide();

			return secondaryDisplay;
		}
	}
}
