using System;
using System.Collections;

using System.Drawing;
using System.Windows.Forms;

using Network;

using CommonGUI;

namespace Polestar_PM.OpsGUI
{
	public class ProgramView : FlickerFreePanel
	{
		ProgramLabelPanel labelPanel;
		ProgramProgressPanel progressPanel;
		ProgramStatusPanel statusPanel;

		int [] tabs;

		int margin = 2;

		public ProgramView (Node programNode)
		{
			labelPanel = new ProgramLabelPanel (programNode);
			progressPanel = new ProgramProgressPanel (programNode);
			statusPanel = new ProgramStatusPanel (programNode);

			DoSize();

			this.Controls.Add(statusPanel);
			this.Controls.Add(progressPanel);
			this.Controls.Add(labelPanel);
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);

			DoSize();
		}

		void DoSize ()
		{
			if (tabs != null)
			{
				labelPanel.Location = new Point (margin, 0);
				labelPanel.Size = new Size (tabs[0] - margin - labelPanel.Left, Height);

				progressPanel.Location = new Point (tabs[0] + margin, 0);
				progressPanel.Size = new Size (tabs[8] - margin - progressPanel.Left, Height);

				statusPanel.Location = new Point (tabs[8] + margin, 0);
				statusPanel.Size = new Size (Width - margin - statusPanel.Left, Height);
			}
		}

		public void SetTabs (params int [] tabs)
		{
			this.tabs = tabs;

			int [] subTabs = new int [tabs.Length - 2];
			for (int i = 0; i < subTabs.Length; i++)
			{
				subTabs[i] = tabs[1 + i] - (tabs[0] + margin);
			}
			progressPanel.SetTabs(subTabs);

			DoSize();
		}
	}
}