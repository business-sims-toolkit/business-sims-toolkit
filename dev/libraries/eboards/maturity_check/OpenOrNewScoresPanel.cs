using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using GameManagement;

using LibCore;
using GameDetails;

namespace maturity_check
{
	/// <summary>
	/// Summary description for OpenOrNewReportPanel.
	/// </summary>
	public class OpenOrNewScoresPanel : OpenOrNewTemplatePanel
	{
		protected ScrollableExpandHolder expandHolder;

		protected ExpandablePanel startNewReport;
		protected ExpandablePanel loadSavedReport;
		protected ExpandablePanel license;

		protected Label errorText;
		protected Button load;
		protected TextBox passwordBox;

		protected Button based_on_mof;
		protected Button based_on_itil;
		protected Button based_on_iso;
		protected Button based_on_lean;

		protected MaturityInfoFile report_file;

		protected MaturityEditSessionPanel sessionPanel;

		protected PictureBox logo;

		public OpenOrNewScoresPanel()
		{
			this.SuspendLayout();
			expandHolder = new ScrollableExpandHolder();
			expandHolder.Location = new Point(0,0);
			expandHolder.Name = "GameSelectionScreen Expand Holder";
			this.Controls.Add(expandHolder);

			this.ResumeLayout(false);

			expandHolder.SuspendLayout();

			startNewReport = new ExpandablePanel();
			startNewReport.Location = new Point(5,5);
			startNewReport.SetSize(500,150);
			startNewReport.Collapsible = false;
			startNewReport.Title = "Create a new template";
			startNewReport.Name = "Create a new template";
			startNewReport.Expanded = true;
			expandHolder.AddExpandablePanel(startNewReport);

			based_on_mof = new Button();
			based_on_mof.BackColor = Color.LightSteelBlue;
			based_on_mof.ForeColor = Color.Black;
			based_on_mof.Text = "Create a template based on MOF";
			based_on_mof.Location = new Point (10, 10);
			based_on_mof.Size = new Size (200, 20);
			based_on_mof.Click += based_on_mof_Click;
			startNewReport.ThePanel.Controls.Add(based_on_mof);
			based_on_mof.Visible = File.Exists(AppInfo.TheInstance.Location + @"\data\eval_wizard_mof.xml");

			based_on_itil = new Button();
			based_on_itil.BackColor = Color.LightSteelBlue;
			based_on_itil.ForeColor = Color.Black;
			based_on_itil.Text = "Create a template based on ITIL";
			based_on_itil.Location = new Point (10, 40);
			based_on_itil.Size = new Size (200, 20);
			based_on_itil.Click += based_on_itil_Click;
			based_on_itil.Visible = File.Exists(AppInfo.TheInstance.Location + @"\data\eval_wizard.xml");
			startNewReport.ThePanel.Controls.Add(based_on_itil);

			based_on_iso = new Button();
			based_on_iso.BackColor = Color.LightSteelBlue;
			based_on_iso.ForeColor = Color.Black;
			based_on_iso.Text = "Create a template based on ISO";
			based_on_iso.Location = new Point (10, 70);
			based_on_iso.Size = new Size (200, 20);
			based_on_iso.Click += based_on_iso_Click;
			based_on_iso.Visible = File.Exists(AppInfo.TheInstance.Location + @"\data\eval_wizard_iso.xml");
			startNewReport.ThePanel.Controls.Add(based_on_iso);

			based_on_lean = new Button();
			based_on_lean.BackColor = Color.LightSteelBlue;
			based_on_lean.ForeColor = Color.Black;
			based_on_lean.Text = "Create a template based on Lean";
			based_on_lean.Location = new Point(10, 100);
			based_on_lean.Size = new Size(200, 20);
			based_on_lean.Click += based_on_lean_Click;
			based_on_lean.Visible = File.Exists(AppInfo.TheInstance.Location + @"\data\eval_wizard_lean.xml");
			startNewReport.ThePanel.Controls.Add(based_on_lean);

			errorText = new Label ();
			errorText.Location = new Point (5, based_on_lean.Bottom + 10);
			errorText.Size = new Size (500, 20);
			errorText.ForeColor = Color.Red;
			startNewReport.ThePanel.Controls.Add(errorText);

			startNewReport.Size = new Size (500, 130);

			loadSavedReport = new ExpandablePanel();
			loadSavedReport.Location = new Point(5,105);
			loadSavedReport.SetSize(500,100);
			loadSavedReport.Collapsible = false;
			loadSavedReport.Expanded = true;
			loadSavedReport.Title = "Edit an existing template";
			loadSavedReport.Name = "Edit an existing template";
			expandHolder.AddExpandablePanel(loadSavedReport);

			load = new Button ();
			load.BackColor = Color.LightSteelBlue;
			load.ForeColor = Color.Black;
			load.Text = "Load";
			load.Size = new Size (50, 20);
			load.Location = new Point (10, 40);
			loadSavedReport.ThePanel.Controls.Add(load);
			load.Click += load_Click;

			loadSavedReport.Size = new Size (500, 100);

			logo = new PictureBox();
            logo.Image = Repository.TheInstance.GetImage(AppInfo.TheInstance.InstallLocation + "\\images\\maturity_template.png");
            logo.Size = new Size(logo.Image.Width, logo.Image.Height);
            this.Controls.Add(logo);

			expandHolder.ResumeLayout(false);

            DoSize();
			this.Resize += OpenOrNewReportPanel_Resize;
        }

		public override void SaveToXML()
		{
			if(sessionPanel != null)
			{
				sessionPanel.SaveToXML();
				report_file.Save(true);
			}
		}

		void OpenOrNewReportPanel_Resize(object sender, EventArgs e)
		{
			DoSize();
		}

		void DoSize ()
		{
			expandHolder.Size = new Size (350, ClientSize.Height);

			startNewReport.SetSize(320, 200);
			startNewReport.Size = new Size (320, 200);

			loadSavedReport.SetSize(320, 255);
			loadSavedReport.Size = new Size (320, 255);

			if (sessionPanel != null)
			{
				sessionPanel.Location = new Point (0, 0);
				sessionPanel.Size = this.ClientSize;
			}

			logo.Location = new Point(expandHolder.Width + (this.ClientSize.Width-expandHolder.Width-logo.Image.Width)/2, (this.ClientSize.Height-logo.Image.Height)/2);
		}

		void load_Click (object sender, EventArgs e)
		{
			OpenFileDialog dialog = new OpenFileDialog();
			dialog.Filter = "Maturity Templates (*.mrt)|*.mrt";
			if (dialog.ShowDialog(TopLevelControl) == DialogResult.OK)
			{
				LoadFile(dialog.FileName);
			}
		}

		public void LoadFile (string filename)
		{
			report_file = new MaturityInfoFile (filename);
			OpenSession(report_file, false);
		}

		void OpenSession (MaturityInfoFile _report_file, bool is_new)
		{
			sessionPanel = new MaturityEditSessionPanel (_report_file, this, is_new, false);
			sessionPanel.BackColor = Color.White;
			sessionPanel.ShowNotes = false;
			sessionPanel.Location = new Point (0, 0);
			sessionPanel.Size = this.ClientSize;
			this.Controls.Add(sessionPanel);
			sessionPanel.BringToFront();
			sessionPanel.Disposed += sessionPanel_Disposed;
		}

		void sessionPanel_Disposed (object sender, EventArgs e)
		{
			sessionPanel = null;
			report_file = null;
		}
		#region ICustomerInfo Members

		public override void Cancelled()
		{
			this.Controls.Remove(sessionPanel);
			sessionPanel = null;
			report_file = null;
		}

		public override void Accepted()
		{
		}

		public override void SaveCustomerDetails ()
		{
		}

		#endregion

		void CreateNewTemplate(string template)
		{
			errorText.Visible = false;

			SaveFileDialog dialog = new SaveFileDialog ();
			//dialog.Filter = "Maturity XML reports (*.XML)|*.XML";
			dialog.Title = "Please provide a filename for your new Maturity Template";
			dialog.Filter = "Maturity Templates (*.mrt)|*.mrt";
			if (dialog.ShowDialog(TopLevelControl) == DialogResult.OK)
			{
				report_file = new MaturityTemplateFile(template, dialog.FileName, true, true, true);
				OpenSession(report_file, true);
			}
		}

		void based_on_mof_Click(object sender, EventArgs e)
		{
			CreateNewTemplate("eval_wizard_mof.xml");
		}

		void based_on_itil_Click(object sender, EventArgs e)
		{
			CreateNewTemplate("eval_wizard.xml");
		}

		void based_on_iso_Click(object sender, EventArgs e)
		{
			CreateNewTemplate("eval_wizard_iso.xml");
		}

		void based_on_lean_Click (object sender, EventArgs e)
		{
			CreateNewTemplate("eval_wizard_lean.xml");
		}

		public void Done ()
		{
			sessionPanel.Done();
		}
	}
}