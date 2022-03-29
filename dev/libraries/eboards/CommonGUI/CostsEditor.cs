using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

using GameManagement;
using LibCore;
using CoreUtils;

namespace CommonGUI
{
	/// <summary>
	/// Summary description for CostsEditor
	/// </summary>
	public class CostsEditor : BasePanel
	{
		Color text_colour;
		Font f = SkinningDefs.TheInstance.GetFont(12);
		Font displayFont;

		Hashtable panels = new Hashtable();

		Hashtable file_scores = new Hashtable();
		Hashtable factor_ticks = new Hashtable();

		Panel p = new Panel();

		NetworkProgressionGameFile gameFile;

		int rounds = SkinningDefs.TheInstance.GetIntData("roundcount", 5);

		bool auto_translate = true;

		public CostsEditor(NetworkProgressionGameFile gameFile)
		{
		    text_colour = SkinningDefs.TheInstance.GetColorData("table_text_colour", Color.Black);

			SuspendLayout();
			p.SuspendLayout();

			p.AutoScroll = true;
			Controls.Add(p);
			this.gameFile = gameFile;

            roundToColumnHeading = new Dictionary<int, Label> ();
			for (int round = 1; round <= rounds; round++)
			{
			    var label = createRoundLabel(round);
                roundToColumnHeading.Add(round, label);
                p.Controls.Add(label);
			}

			p.ResumeLayout(false);
			ResumeLayout(false);

			setupCostLabels();

			setupCostInputs();

			DoLayout();

			Resize += CostsEditor_Resize;
		}

		void setupCostInputs()
		{
			p.SuspendLayout();

            roundToRowToEntryBox = new Dictionary<int, List<CostEditorEntryBox>> ();

			for (int i = 0; i < rounds; i++)
			{
                roundToRowToEntryBox.Add(1 + i, new List<CostEditorEntryBox> ());

				BasicXmlDocument xml = BasicXmlDocument.CreateFromFile(gameFile.GetGlobalFile($"costs_r{i + 1}.xml"));
				if (xml == null)
				{
					continue;
				}

				int row = 0;
				int actual_row = 0;

				foreach (System.Xml.XmlNode cost in xml.DocumentElement.ChildNodes)
				{
					if (cost.Name == "header")
					{
						row++;
					}
					else if (cost.Attributes["visible"] != null && cost.Attributes["visible"].Value == "true")
					{
						CostEditorEntryBox e = new CostEditorEntryBox(gameFile, i + 1, actual_row, cost.Attributes["cost"]?.Value, cost.Attributes["model_value"]?.Value);

						roundToRowToEntryBox[i + 1].Add(e);

						e.Enabled = cost.Attributes["editable"] != null
							&& cost.Attributes["editable"].Value == "true";

						p.Controls.Add(e);
                        e.BringToFront();

						row++;
					}
					actual_row++;
				}
			}

			p.ResumeLayout(false);
		}

	    Dictionary<int, List<CostEditorEntryBox>> roundToRowToEntryBox;
	    Dictionary<int, Label> roundToColumnHeading;
	    List<Label> rowStripes;

		void setupCostLabels()
		{
			p.SuspendLayout();

            rowStripes = new List<Label> ();

			string NetworkFile = gameFile.Dir + "\\global\\costs_r1.xml";
			if (File.Exists(NetworkFile))
			{
				StreamReader file = new StreamReader(NetworkFile);
				BasicXmlDocument xml = BasicXmlDocument.Create(file.ReadToEnd());
				file.Close();
				file = null;

			    var rowColours = new []
			    {
			        SkinningDefs.TheInstance.GetColorDataGivenDefault("table_row_colour", Color.White),
			        SkinningDefs.TheInstance.GetColorDataGivenDefault("table_row_colour_alternate", Color.White)
			    };

				displayFont = null;
				string displayFontName = SkinningDefs.TheInstance.GetData("fontname");
				if (auto_translate)
				{
					displayFontName = TextTranslator.TheInstance.GetTranslateFont(displayFontName);
				}
				displayFont = ConstantSizeFont.NewFont(displayFontName, 10);

				int rows = 0;
				var borderColour = SkinningDefs.TheInstance.GetColorData("tools_screen_cell_border_colour", Color.Transparent);
				foreach (System.Xml.XmlNode costs in xml.DocumentElement.ChildNodes)
				{
					if (costs.Name == "header")
					{
						Label label = OnCreateSectionHeader();
						label.Text = BasicXmlDocument.GetStringAttribute(costs, "desc", "");
						label.Location = new Point(20, 60 + ((rows * 25)));
						label.Size = new Size(960, 20);
						p.Controls.Add(label);

						if (borderColour != Color.Transparent)
						{
							label.Paint += control_Paint;
						}

						rows++;
					}
					else if (costs.Attributes["visible"] != null && costs.Attributes["visible"].Value == "true")
					{
						Label l = OnCreateCostLabel();

						string rawtext = costs.Attributes["desc"].Value;
						rawtext = TextTranslator.TheInstance.Translate(rawtext);
						l.Text = rawtext;

						l.Location = new Point(20, 60 + ((rows * 25)));
                        l.ForeColor = text_colour;
					    l.BackColor = rowColours[rows % rowColours.Length];
                        l.TextAlign = ContentAlignment.MiddleLeft;
						p.Controls.Add(l);
                        rowStripes.Add(l);

						if (borderColour != Color.Transparent)
						{
							l.Paint += control_Paint;
						}

						rows++;
					}
				}
			}

			p.ResumeLayout(false);
		}

		void control_Paint (object sender, PaintEventArgs args)
		{
			var control = (Control) sender;

			using (var pen = new Pen (SkinningDefs.TheInstance.GetColorData("tools_screen_cell_border_colour", Color.Transparent), 1))
			{
				args.Graphics.DrawRectangle(pen, 0, 0, control.Width - 1, control.Height - 1);
			}
		}

		protected virtual Label OnCreateSectionHeader ()
		{
			Label label = new Label ();
			label.Font = SkinningDefs.TheInstance.GetFont(10, FontStyle.Bold);

			return label;
		}

		protected virtual Label OnCreateCostLabel ()
		{
			Label label = new Label ();
			label.Font = displayFont;

			return label;
		}

		public void DoLayout()
		{
			p.Size = new Size(p.Parent.Width - p.Left, Math.Max(600, p.Parent.Height - p.Top));

		    var columnWidth = (p.Width - 500) / gameFile.GetTotalRounds();

		    for (var round = 1; round <= roundToColumnHeading.Count; round++)
		    {
		        var label = roundToColumnHeading[round];
                label.Bounds = new Rectangle (515 + ((round - 1) * columnWidth) - (columnWidth / 2), label.Location.Y, columnWidth, label.Height);
            }

		    foreach (var stripe in rowStripes)
		    {
		        stripe.Size = new Size (p.Width - 25 - stripe.Left, 25);
		    }

			foreach (var round in roundToRowToEntryBox.Keys)
			{
				for (var row = 0; row < roundToRowToEntryBox[round].Count; row++)
				{
					var entryBox = roundToRowToEntryBox[round][row];

					if (SkinningDefs.TheInstance.GetBoolData("tools_screen_cells_full_size", false))
					{
						entryBox.AutoSize = false;

						var midX = ((roundToColumnHeading[round].Left + roundToColumnHeading[round].Right) / 2);
						var width = columnWidth / 3;
						entryBox.Bounds = new Rectangle(midX - (width / 2), rowStripes[row].Top + 1, width, rowStripes[row].Height - 2);
					}
					else
					{
						entryBox.Size = new Size(60, 15);
						entryBox.Location = new Point(500 + ((round - 1) * columnWidth), 62 + ((row * 25)));
					}
				}
			}
		}

		protected virtual Label createRoundLabel(int round)
		{
			Label tmp = OnCreateRoundLabel();

			tmp.Text = GetRoundLabelText(round);
			tmp.Location = new Point (0, 20);
			tmp.TextAlign = ContentAlignment.MiddleCenter;
			tmp.Size = new Size (30, 30);

			Font f2 = SkinningDefs.TheInstance.GetFont(12, FontStyle.Bold);
			tmp.Font = f2;

			if (SkinningDefs.TheInstance.GetBoolData("tools_screen_cells_full_size", false))
			{
				tmp.BackColor = SkinningDefs.TheInstance.GetColorData("scorecard_column_heading_back_colour", Color.Transparent);
				tmp.ForeColor = SkinningDefs.TheInstance.GetColorData("scorecard_column_heading_text_colour", Color.Black);
				tmp.Resize += ((sender, args) => ((Control) sender).Invalidate());
				tmp.Paint += control_Paint;
			}

			return tmp;
		}

		string GetRoundLabelText (int round)
		{
			if (SkinningDefs.TheInstance.GetBoolData("tools_screen_cells_full_size", false))
			{
				var text = $"Round {round}";
				if (SkinningDefs.TheInstance.GetBoolData("scorecard_headings_uppercase", false))
				{
					text = text.ToUpper();
				}

				return text;
			}
			else
			{
				return CONVERT.ToStr(round);
			}
		}

		public delegate Label CreateRoundLabelHandler ();
		public event CreateRoundLabelHandler CreateRoundLabel;

		protected virtual Label OnCreateRoundLabel ()
		{
			if (CreateRoundLabel != null)
			{
				return CreateRoundLabel();
			}

			Label label = new Label ();
			label.Font = f;
			return label;
		}

		void CostsEditor_Resize (object sender, EventArgs e)
		{
			DoLayout();
		}
	}
}