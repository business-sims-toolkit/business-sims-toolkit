using System;
using System.Collections;
using System.Xml;

using System.Windows.Forms;
using System.Drawing;
using ReportBuilder;
using Charts;

namespace maturity_check
{
	public interface IMaturityEditSession
	{
		void Done ();
		void ExportCSV (string filename);
		void ExportPDF (string filename);
		void ViewChart (bool view);

		void ViewCustomerInfo (bool editable);
		void ViewSectionsEditor ();
		void ViewScoreEditor ();

		void SetEditable(bool e);
	}

	public class MaturityEditSessionPanel : Panel, IMaturityEditSession, ICustomerInfo
	{
		public event EventHandler CloseSession;

		string filename;
		MaturityCard editor;
		Panel editorPanel;
		MaturityEditControlPanel controls;
		DraggablePieChart chart;
		CustomerInfoPanel info;
		ICustomerInfo _ci;
		bool _is_new = true;

		protected Panel chart_panel;

		protected GameManagement.MaturityInfoFile report_file;
		string customerDetailsFilename;

		protected ArrayList ignoreList = new ArrayList();
		protected ArrayList ignoreList_tag_names = new ArrayList();
		protected Hashtable sectionOrderForPie;

		string chartXml;

		public void SetEditable(bool e)
		{
			editor.SetEditable(e);
		}

		public bool ShowNotes
		{
			set
			{
				editor.ShowNotes = value;
			}
		}

		public MaturityEditSessionPanel (GameManagement.MaturityInfoFile _report_file, ICustomerInfo ci, bool is_new, bool has_customer_info)
		{
			_is_new = is_new;
			_ci = ci;
			report_file = _report_file;
			this.filename = report_file.GetFile("eval_wizard_custom.xml");
			customerDetailsFilename = report_file.GetFile("customer_info.xml");

			//
			editor = new MaturityCard (filename, this);
			editor.Changed += editor_Changed;

			this.BackColor = CoreUtils.SkinningDefs.TheInstance.GetColorData("maturity_editor_background_colour");

			this.SuspendLayout();

			if (has_customer_info)
			{
				info = new CustomerInfoPanel(this);
				this.Controls.Add(info);
				if (!is_new)
				{
					info.LoadXml(customerDetailsFilename);
				}
			}

			controls = new MaturityEditControlPanel (this, has_customer_info);

			chart_panel = new Panel();
			chart_panel.Visible = false;
			this.Controls.Add(chart_panel);

			editorPanel = new Panel ();
			editorPanel.Controls.Add(editor);
			editorPanel.AutoScroll = true;

			this.Controls.Add(controls);
			this.Controls.Add(editorPanel);

			this.ResumeLayout(false);

			if (has_customer_info)
			{
				if (is_new)
				{
					ViewCustomerInfo(true);
					controls.EnableAll(false);
				}
				else
				{
					ViewCustomerInfo(false);
					controls.EnableAll(true);
				}
			}
			else
			{
				ViewScoreEditor();
				controls.EnableAll(true);
			}

			SetEditable(false);
			DoSize();
			this.SizeChanged += MaturityEditSessionPanel_SizeChanged;
		}

		protected override void Dispose (bool disposing)
		{
			base.Dispose(disposing);
		}

		void DoSize ()
		{
			controls.Size = new Size (this.ClientSize.Width, 40);
			controls.Location = new Point (0, 0);//this.ClientSize.Height - controls.Height);

			editorPanel.Location = new Point (0, controls.Height);//0);
			editorPanel.Size = new Size (this.ClientSize.Width, ClientSize.Height-controls.Height);//controls.Top - editorPanel.Top);

			if(info != null)
			{
				info.Location = new Point((ClientSize.Width-info.Width)/2, controls.Bottom );
			}

			if (chart != null)
			{
				chart.Visible = false;
				chart_panel.SuspendLayout();

				chart.Location = new Point (0, 0);

				double ratio = 1.5333;
				double hh = ClientSize.Height - controls.Height;
				double ww = ClientSize.Width;
				double nratio = ww/hh;

				if(nratio > ratio)
				{
					// clip by height.
					ww = ratio * hh;
				}
				else
				{
					// clip by width.
					hh = ww / ratio;
				}

				chart_panel.Size = new Size(this.ClientSize.Width, ClientSize.Height-controls.Height);
				chart_panel.Location = new Point(0, controls.Height);

				//chart.Size = new Size (this.ClientSize.Width, controls.Top - chart.Top);
				chart.Size = new Size( (int)ww, (int)hh);
				chart.Location = new Point((ClientSize.Width-chart.Width)/2, (ClientSize.Height-chart.Height)/2);

				// This is manky but works around the way that the pie chart can't cope with being resized.
				chart.LoadData(chartXml);

				chart_panel.ResumeLayout(false);
				chart.Visible = true;
			}
		}

		void MaturityEditSessionPanel_SizeChanged (object sender, EventArgs e)
		{
			DoSize();
		}

		public void Save ()
		{
			SaveToXML();
			SaveCustomerDetails();
			report_file.Save(true);
		}

		public void Done ()
		{
			Save();

			controls.Dispose();
			controls = null;

			editor.Dispose();
			editor = null;

			_ci.Cancelled();
			//Dispose();

			OnCloseSession();
		}

		void OnCloseSession ()
		{
			if (CloseSession != null)
			{
				CloseSession(this, new EventArgs());
			}
		}

		public void SaveToXML()
		{
			editor.SaveToXML(filename);
			string ignore_file = report_file.GetFile("Eval_States.xml");
			editor.SaveIgnoreList(ignore_file);
		}

		public void SaveCustomerDetails ()
		{
			if (info != null)
			{
				info.SaveToXml(customerDetailsFilename);
			}
		}

		public void ExportCSV (string filename)
		{
			editor.ExportCSVFile(filename);
		}

		public void ExportPDF (string filename)
		{
			editor.ExportPDFFile(filename);
		}

		protected void ReadIgnoreList()
		{
			ignoreList.Clear();
			ignoreList_tag_names.Clear();

			string StatesFile = report_file.GetFile("Eval_States.xml");

			LibCore.BasicXmlDocument xml;
			XmlNode root;

			if (System.IO.File.Exists(StatesFile))
			{
				System.IO.StreamReader file = new System.IO.StreamReader(StatesFile);
				xml = LibCore.BasicXmlDocument.Create(file.ReadToEnd());
				file.Close();
				file = null;

				root = xml.DocumentElement;

				//check if question already switched off and if so switch back on (remove from file)
				foreach (XmlNode node in root.ChildNodes)
				{
					if (node.Name == "ignore")
					{
						foreach(XmlAttribute att in node.Attributes)
						{
							if (att.Name == "question")
							{
								string question = att.Value;

								ignoreList.Add(question);
							}
							else if (att.Name == "dest_tag_name")
							{
								ignoreList_tag_names.Add(att.Value);
							}
						}
					}
				}
			}
		}

		public void ViewChart (bool view)
		{
			if (chart != null)
			{
				this.chart_panel.Controls.Remove(chart);
				chart.Dispose();
				chart = null;
			}

			if (view)
			{
				chart = new DraggablePieChart ();
				chart.ShowDropShadow = true;
				chart.DragFinished += chart_DragFinished;
				chart.ColourChanged += chart_ColourChanged;

				chart.KeyYOffset = 20;

				chart_panel.Controls.Add(chart);
				chart.BringToFront();

				RefreshChart();
			}

			chart_panel.Visible = view;

			if (info != null)
			{
				info.Hide();
			}

			controls.Show();
			editorPanel.Hide();
		}

		void RefreshChart ()
		{
			chartXml = GenerateMaturityPieXml();
			DoSize();
		}

		void chart_ColourChanged (string sectorName, Color colour)
		{
			editor.ChangeSectionColour(sectorName, colour);
			RefreshChart();
		}

		void chart_DragFinished (int sourceSectorIndex, int destSectorIndex)
		{
			// Find the names of the two sectors in question.
			string sourceSectionName = "";
			string destSectionName = "";
			foreach (string sectionName in sectionOrderForPie.Keys)
			{
				int orderInPie = ((int) sectionOrderForPie[sectionName]) - 1; // because the hashtable stores their orders starting at 1 not 0.

				if (orderInPie == sourceSectorIndex)
				{
					sourceSectionName = sectionName;
				}

				if (orderInPie == destSectorIndex)
				{
					destSectionName = sectionName;
				}
			}

			editor.SwapSectionsGivenPieChartOrder(sourceSectionName, destSectionName);
			ViewChart(true);
		}

		public string GenerateMaturityPieXml ()
		{
			OpsMaturityReport omr = new OpsMaturityReport();

			string report_data = editor.SaveToXML(filename).OuterXml;
			string ignore_file = report_file.GetFile("Eval_States.xml");
			editor.SaveIgnoreList(ignore_file);

			ReadIgnoreList();

			string maturityXmlFile = omr.BuildReport(report_data, ignoreList, ignoreList_tag_names, editor.GetSectionColours());
			sectionOrderForPie = omr.GetSectionOrder(report_data, ignoreList, ignoreList_tag_names);

			using (System.IO.StreamReader file = new System.IO.StreamReader(maturityXmlFile))
			{
				return file.ReadToEnd();
			}
		}

		public void ViewCustomerInfo (bool editable)
		{
			ViewChart(false);

			info.Show();
			controls.Show();
			editorPanel.Hide();

			info.SetEditable(editable);
		}

		public void ViewSectionsEditor ()
		{
			SetEditable(true);
			ViewChart(false);

			if (info != null)
			{
				info.Hide();
			}

			controls.Show();
			editorPanel.Show();
		}

		public void ViewScoreEditor ()
		{
			SetEditable(false);
			ViewChart(false);

			if (info != null)
			{
				info.Hide();
			}

			controls.Show();
			editorPanel.Show();
		}

		void editor_Changed (object sender)
		{
			Save();
		}
		#region ICustomerInfo Members

		public void Cancelled()
		{
			// TODO:  Add MaturityEditSessionPanel.Cancelled implementation
			// The user has cancelled creating a new maturity report.
			_ci.Cancelled();
		}

		public void Accepted ()
		{
			_ci.Accepted();

			SaveCustomerDetails();

			controls.EnableAll(true);
			ViewScoreEditor();
		}

		#endregion

		public string ExportCustomerInfoCSV ()
		{
			return info.ExportCSV();
		}

		public void BuildMaturityPDF (string filename, string maturityXml)
		{
		}
	}
}