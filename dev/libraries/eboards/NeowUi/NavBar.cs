using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace NeowUi
{
	public class NavBar : Panel
	{
		TabBar tabBar;
		ITabBarItem gameSelectionScreenTab;
		ITabBarItem gameDetailsScreenTab;
		ITabBarItem gameScreenTab;
		ITabBarItem reportsTab;
		Dictionary<ITabBarItem, TabItemSelectedEventHandler> tabItemToHandler;

		bool reportsAreAvailable;

		public NavBar ()
		{
			tabBar = new TabBar ();
			tabBar.ItemSelected += tabBar_SelectedItem;
			tabItemToHandler = new Dictionary<ITabBarItem, TabItemSelectedEventHandler> ();
			tabBar.BackColor = BackColor;
			tabBar.ForeColor = ForeColor;
			Controls.Add(tabBar);

			gameSelectionScreenTab = AddTab("Home", tabBar_SelectedGameSelectionScreen);
			gameDetailsScreenTab = AddTab("Details", tabBar_SelectedGameDetailsScreen);
			gameScreenTab = AddTab("Game", tabBar_SelectedGameScreen);
			reportsTab = AddTab("Reports", tabBar_SelectedReportsScreen);

			DoSize();
		}

		public int PreferredWidth
		{
			get
			{
				if (tabBar != null)
				{
					return tabBar.PreferredWidth;
				}
				else
				{
					return 0;
				}
			}
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);
			DoSize();
		}

		void DoSize ()
		{
			tabBar.Bounds = new Rectangle (0, 0, Width, Height);
		}

		ITabBarItem AddTab (string text, TabItemSelectedEventHandler handler)
		{
			TabBarItem item = new TabBarItem (text);
			tabBar.AddTab(item);
			tabItemToHandler.Add(item, handler);

			return item;
		}

		void tabBar_SelectedItem (object sender, TabItemSelectedEventArgs args)
		{
			if (tabItemToHandler.ContainsKey(args.Item))
			{
				tabItemToHandler[args.Item](this, args);
			}
		}

		void tabBar_SelectedGameSelectionScreen (object sender, TabItemSelectedEventArgs args)
		{
			OnSelectGameSelectionScreen();
		}

		void tabBar_SelectedGameDetailsScreen (object sender, TabItemSelectedEventArgs args)
		{
			OnSelectGameDetailsScreen();
		}

		void tabBar_SelectedGameScreen (object sender, TabItemSelectedEventArgs args)
		{
			OnSelectGameScreen();
		}

		void tabBar_SelectedReportsScreen (object sender, TabItemSelectedEventArgs args)
		{
			OnSelectReportsScreen();
		}

		void OnSelectGameSelectionScreen ()
		{
			if (SelectGameSelectionScreen != null)
			{
				SelectGameSelectionScreen(this, EventArgs.Empty);
			}
		}

		void OnSelectGameDetailsScreen ()
		{
			if (SelectGameDetailsScreen != null)
			{
				SelectGameDetailsScreen(this, EventArgs.Empty);
			}
		}

		void OnSelectGameScreen ()
		{
			if (SelectGameScreen != null)
			{
				SelectGameScreen(this, EventArgs.Empty);
			}
		}

		void OnSelectReportsScreen ()
		{
			if (SelectReportsScreen != null)
			{
				SelectReportsScreen(this, EventArgs.Empty);
			}
		}

		public event EventHandler SelectGameSelectionScreen;
		public event EventHandler SelectGameDetailsScreen;
		public event EventHandler SelectGameScreen;
		public event EventHandler SelectReportsScreen;

		public void SwitchToGameSelectionScreen ()
		{
			gameSelectionScreenTab.Enabled = true;
			gameDetailsScreenTab.Enabled = false;
			gameScreenTab.Enabled = false;
			reportsTab.Enabled = false;

			tabBar.SelectedItem = gameSelectionScreenTab;
		}

		public void SwitchToGameDetailsScreen (bool detailsComplete)
		{
			gameSelectionScreenTab.Enabled = detailsComplete;
			gameDetailsScreenTab.Enabled = true;
			gameScreenTab.Enabled = false;
			reportsTab.Enabled = detailsComplete && reportsAreAvailable;

			tabBar.SelectedItem = gameDetailsScreenTab;
		}

		public void SwitchToGameScreen (bool lockedIntoGame)
		{
			gameSelectionScreenTab.Enabled = ! lockedIntoGame;
			gameDetailsScreenTab.Enabled = ! lockedIntoGame;
			gameScreenTab.Enabled = true;
			reportsTab.Enabled = reportsAreAvailable;

			tabBar.SelectedItem = gameScreenTab;
		}

		public void SwitchToReportsScreen (bool lockedIntoGame)
		{
			gameSelectionScreenTab.Enabled = ! lockedIntoGame;
			gameDetailsScreenTab.Enabled = ! lockedIntoGame;
			gameScreenTab.Enabled = lockedIntoGame;
			reportsTab.Enabled = reportsAreAvailable;

			tabBar.SelectedItem = reportsTab;
		}

		protected override void OnBackColorChanged (EventArgs e)
		{			
			base.OnBackColorChanged(e);
			tabBar.BackColor = BackColor;
		}

		protected override void OnForeColorChanged (EventArgs e)
		{
			base.OnForeColorChanged(e);
			tabBar.ForeColor = ForeColor;
		}

		public bool ReportsAreAvailable
		{
			get
			{
				return reportsAreAvailable;
			}

			set
			{
				reportsAreAvailable = value;

				if (! reportsAreAvailable)
				{
					reportsTab.Enabled = false;
				}
			}
		}
	}
}