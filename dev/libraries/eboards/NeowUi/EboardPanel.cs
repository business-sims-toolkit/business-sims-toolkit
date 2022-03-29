using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace NeowUi
{
	public abstract class EboardPanel : Panel
	{
		WindowControlPanel controlPanel;
		EboardTopBar topBar;
		Control bottomBar;

#if ! DEBUG
		Rectangle? unmaximisedBounds;
#endif

		protected EboardPanel ()
		{
			controlPanel = CreateWindowControlPanel();
			SetTopBar(CreateTopBar());
			SetBottomBar(CreateBottomBar());
		}

		protected virtual WindowControlPanel CreateWindowControlPanel ()
		{
			WindowControlPanel controlPanel = new WindowControlPanel ();
			Controls.Add(controlPanel);
			controlPanel.MinimisePressed += controlPanel_MinimisePressed;
			controlPanel.ToggleSizePressed += controlPanel_ToggleSizePressed;
			controlPanel.ClosePressed += controlPanel_ClosePressed;

			return controlPanel;
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);
			DoSize();

			Invalidate();
		}

		protected virtual void DoSize ()
		{
			controlPanel.SetAutoSize();
			controlPanel.Location = new Point (Width - controlPanel.Width, 0);

			if (topBar != null)
			{
				topBar.Bounds = new Rectangle (0, 0, Width, topBar.Height);
			}

			if (bottomBar != null)
			{
				bottomBar.Bounds = new Rectangle (0, Height - bottomBar.Height, Width, bottomBar.Height);
			}
		}

		protected abstract EboardTopBar CreateTopBar ();

		protected virtual Control CreateBottomBar ()
		{
			CompanyBrandingBottomBar bottomBar = new CompanyBrandingBottomBar ();
			bottomBar.Height = 40;

			return bottomBar;
		}

		void SetTopBar (EboardTopBar topBar)
		{
			if (this.topBar != topBar)
			{
				if (this.topBar != null)
				{
					Controls.Remove(this.topBar);
					this.topBar = null;
				}

				if (topBar != null)
				{
					this.topBar = topBar;
					Controls.Add(topBar);
				}
			}

			controlPanel.BringToFront();

			DoSize();
		}

		public void SetBottomBar (Control bottomBar)
		{
			if (this.bottomBar != bottomBar)
			{
				if (this.bottomBar != null)
				{
					Controls.Remove(this.bottomBar);
					this.bottomBar = null;
				}

				if (bottomBar != null)
				{
					this.bottomBar = bottomBar;
					Controls.Add(bottomBar);
				}
			}

			DoSize();
		}

		public Control BottomBar
		{
			get
			{
				return bottomBar;
			}
		}

		void controlPanel_MinimisePressed (object sender, EventArgs args)
		{
			((Form) TopLevelControl).WindowState = FormWindowState.Minimized;
		}

		void controlPanel_ToggleSizePressed (object sender, EventArgs args)
		{
			Form form = (Form) TopLevelControl;

#if DEBUG
			ContextMenu menu = new ContextMenu ();
			foreach (Size size in new [] { new Size (1024, 600),
			                               new Size (1024, 768),
										   new Size (1280, 1024),
										   new Size (1600, 900),
										   new Size (1600, 1200),
										   new Size (1920, 1080) })
			{
				MenuItem item = new MenuItem (LibCore.CONVERT.Format("{0} x {1}", size.Width, size.Height),
				                              sizeItem_Click);
				item.RadioCheck = true;
				item.Tag = size;
				item.Checked = (form.Size == size);
				menu.MenuItems.Add(item);
			}

			menu.Show(controlPanel.ToggleSizeButton, new Point (0, 0));
#else
			if (unmaximisedBounds.HasValue)
			{
				form.Bounds = unmaximisedBounds.Value;
				unmaximisedBounds = null;
			}
			else
			{
				unmaximisedBounds = form.Bounds;
				form.Bounds = Screen.FromPoint(form.Location).WorkingArea;
			}
#endif
		}

		void sizeItem_Click (object sender, EventArgs args)
		{
			MenuItem item = (MenuItem) sender;
			Form form = (Form) TopLevelControl;

			if (form.WindowState == FormWindowState.Maximized)
			{
				form.WindowState = FormWindowState.Normal;
			}

			form.Size = (Size) (item.Tag);
		}

		void controlPanel_ClosePressed (object sender, EventArgs args)
		{
			if (ConfirmOkToExitGame())
			{
				((Form) TopLevelControl).Close();
			}
		}

		public abstract bool ConfirmOkToExitGame ();

		public abstract void OpenNetworkModel ();

		public abstract void ImportIncidents (string filename);

		public abstract void ExportIncidents ();

		public abstract void QuickStartGame ();
	}
}