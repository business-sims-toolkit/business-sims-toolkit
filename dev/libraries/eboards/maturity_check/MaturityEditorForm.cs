using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using CoreUtils;
using LibCore;
using Logging;

namespace maturity_check
{
	public class MaturityEditorForm : Form
	{
		Container components = null;

		public OpenOrNewScoresPanel mainPanel;

		public MaturityEditorForm ()
		{
			Icon = Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetEntryAssembly().Location);
			InitializeComponent();
			//
			SuspendLayout();

			mainPanel = new OpenOrNewScoresPanel();
			mainPanel.Size = ClientSize;
			mainPanel.Location = new Point(0, 0);
			Controls.Add(mainPanel);

			StartPosition = FormStartPosition.CenterScreen;

			Resize += new EventHandler(MainForm_Resize);

			ResumeLayout(false);

			Closing += new CancelEventHandler(MainForm_Closing);

			MinimumSize = new Size(650, 600);
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing)
			{
				Controls.Remove(mainPanel);
				mainPanel.Dispose();
				mainPanel = null;
			}

			if (disposing)
			{
				if (components != null)
				{
					components.Dispose();
				}
			}

			base.Dispose(disposing);
		}

		void InitializeComponent ()
		{
			BackColor = Color.White;
			ClientSize = new Size(800, 550);
			FormBorderStyle = FormBorderStyle.Sizable;//.None;
			Name = "MaturityForm";
			StartPosition = FormStartPosition.Manual;
			Text = "Maturity Template Editor";
		}

		void MainForm_Resize (object sender, EventArgs e)
		{
			mainPanel.Size = ClientSize;
		}

		void MainForm_Closing (object sender, CancelEventArgs e)
		{
			mainPanel.SaveToXML();
		}

		public void LoadFile (string filename)
		{
			mainPanel.LoadFile(filename);
		}
	}
}