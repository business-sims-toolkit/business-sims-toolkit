using System;
using System.IO;

using System.Windows.Forms;
using System.Drawing;

using LibCore;
using CommonGUI;

namespace maturity_check
{
	public class MaturityEditControlPanel : Panel
	{
		ImageButton iinfo;
		ImageButton idone;
		ImageButton ireport;
		ImageButton iedit;
		ImageButton ichart;
		ImageButton iexport;
		ImageButton ipdf;

		ImageBox logo;

		IMaturityEditSession session;

		public bool Allow_CSV_Export
		{
			set
			{
				iexport.Visible = value;
			}
		}

		public MaturityEditControlPanel (IMaturityEditSession session, bool hasCustomerInfo)
		{
			this.session = session;

			this.BackColor = CoreUtils.SkinningDefs.TheInstance.GetColorData("maturity_editor_top_bar_colour");

			int ButtonImageWidth = 31;
			int ButtonImageHeight = 31;

			int x = 0;

			idone = new ImageButton(0);
			idone.Size = new Size(ButtonImageWidth,ButtonImageHeight);
			idone.Location = new Point(5 + 34*x , 5);
			idone.SetToolTipText = "Create/Open Template";
			idone.SetVariants("\\images\\maturity_tool\\save");
			idone.ButtonPressed += idone_ButtonPressed;
			idone.Name = "Create/Open Template";
			this.Controls.Add(idone);
			x++;

			if (hasCustomerInfo)
			{
				iinfo = new ImageButton(6);
				iinfo.Size = new Size(ButtonImageWidth, ButtonImageHeight);
				iinfo.Location = new Point(5 + 34 * x, 5);
				iinfo.SetToolTipText = "View Customer Info";
				iinfo.SetVariants("\\images\\maturity_tool\\info");
				iinfo.ButtonPressed += idone_ButtonPressed;
				iinfo.Name = "View Customer Info";
				this.Controls.Add(iinfo);
				x++;
			}

			ireport = new ImageButton(1);
			ireport.Size = new Size(ButtonImageWidth,ButtonImageHeight);
			ireport.Location = new Point(5 + 34*x , 5);
			ireport.SetToolTipText = "Maturity Scores";
			ireport.SetVariants("\\images\\maturity_tool\\doc");
			ireport.ButtonPressed += idone_ButtonPressed;
			ireport.Name = "Maturity Scores";
			this.Controls.Add(ireport);
			x++;

			iedit = new ImageButton(2);
			iedit.Size = new Size(ButtonImageWidth,ButtonImageHeight);
			iedit.Location = new Point(5 + 34*x , 5);
			iedit.SetToolTipText = "Edit Questions";
			iedit.SetVariants("\\images\\maturity_tool\\edit");
			iedit.ButtonPressed += idone_ButtonPressed;
			iedit.Name = "Edit Questions";
			this.Controls.Add(iedit);
			x++;

			ichart = new ImageButton(3);
			ichart.Size = new Size(ButtonImageWidth,ButtonImageHeight);
			ichart.Location = new Point(5 + 34*x , 5);
			ichart.SetToolTipText = "Show Chart";
			ichart.SetVariants("\\images\\maturity_tool\\chart");
			ichart.ButtonPressed += idone_ButtonPressed;
			ichart.Name = "Show Chart";
			this.Controls.Add(ichart);
			x++;

			if (hasCustomerInfo)
			{
				iexport = new ImageButton(4);
				iexport.Size = new Size(ButtonImageWidth, ButtonImageHeight);
				iexport.Location = new Point(5 + 34 * x, 5);
				iexport.SetToolTipText = "Export CSV";
				iexport.SetVariants("\\images\\maturity_tool\\csv");
				iexport.ButtonPressed += idone_ButtonPressed;
				iexport.Name = "Export CSV";
				this.Controls.Add(iexport);
				x++;

				ipdf = new ImageButton(5);
				ipdf.Size = new Size(ButtonImageWidth, ButtonImageHeight);
				ipdf.Location = new Point(5 + 34 * x, 5);
				ipdf.SetToolTipText = "Export PDF";
				ipdf.SetVariants("\\images\\maturity_tool\\pdf");
				ipdf.ButtonPressed += idone_ButtonPressed;
				ipdf.Name = "Export PDF";
				this.Controls.Add(ipdf);
				x++;
			}

			string filename = AppInfo.TheInstance.Location + "/images/maturity_tool/top_logo.png";
			if (File.Exists(filename))
			{
				logo = new ImageBox();
				this.Controls.Add(logo);
				logo.Image = Repository.TheInstance.GetImage(filename);
				logo.Size = logo.Image.Size;
			}
		}

		void idone_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			switch(args.Code)
			{
				case 0:
				{
					session.Done();
				}
					break;

				case 1:
				{
					session.ViewScoreEditor();
				}
					break;

				case 2:
				{
					session.ViewSectionsEditor();
				}
					break;

				case 3:
				{
					session.ViewChart(true);
				}
					break;

				case 4:
				{
					SaveFileDialog dialog = new SaveFileDialog ();
					dialog.Filter = "CSV files (*.CSV)|*.csv";

					if (dialog.ShowDialog(TopLevelControl) == DialogResult.OK)
					{
						session.ExportCSV(dialog.FileName);
					}
				}
					break;

				case 5:
				{
					SaveFileDialog dialog = new SaveFileDialog ();
					dialog.Filter = "PDF files (*.PDF)|*.pdf";

					if (dialog.ShowDialog(TopLevelControl) == DialogResult.OK)
					{
						session.ExportPDF(dialog.FileName);
					}
				}
				break;

				case 6:
				{
					session.ViewCustomerInfo(false);
				}
					break;

			}
		}

		void exportCSV_Click (object sender, EventArgs e)
		{
			SaveFileDialog dialog = new SaveFileDialog ();
			dialog.Filter = "CSV files (*.CSV)|*.csv";

			if (dialog.ShowDialog(TopLevelControl) == DialogResult.OK)
			{
				session.ExportCSV(dialog.FileName);
			}
		}

		public void EnableAll (bool enable)
		{
			if (iinfo != null)
			{
				iinfo.Enabled = enable;
			}

			ireport.Enabled = enable;
			iedit.Enabled = enable;
			ichart.Enabled = enable;

			if (iexport != null)
			{
				iexport.Enabled = enable;
			}

			if (ipdf != null)
			{
				ipdf.Enabled = enable;
			}
		}

		public void Done ()
		{
			session.Done();
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);

			if (logo != null)
			{
				logo.Location = new Point (this.ClientSize.Width - logo.Width - 20, (this.ClientSize.Height - logo.Height) / 2);
			}
		}
	}
}