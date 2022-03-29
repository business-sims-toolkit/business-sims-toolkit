using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;

using GameManagement;
using LibCore;
using CoreUtils;

namespace CommonGUI
{
	/// <summary>
	/// Summary description for ScoreCardWizard.
	/// </summary>
	public class ScoreCardWizard : ScoreCardWizardBase
	{
	    protected Font f = SkinningDefs.TheInstance.GetFont(12);
        protected Color text_colour;

        protected Hashtable panels = new Hashtable();

        protected Hashtable file_scores = new Hashtable();
        protected Hashtable factor_ticks = new Hashtable();
        protected ArrayList disabled_questions = new ArrayList();

        protected BasicXmlDocument doc;
        protected Panel p = new Panel();

        protected ArrayList questions;
        protected int selected = 1;

        protected NetworkProgressionGameFile gameFile;

        protected int rounds = SkinningDefs.TheInstance.GetIntData("roundcount", 5);

        protected ArrayList ignoreList = new ArrayList();

        protected bool auto_translate = true;
        protected int labels;
        public ArrayList scores;

	    Dictionary<int, Control> roundToColumnHeader;
	    List<Control> stripes;

		public ScoreCardWizard(NetworkProgressionGameFile gameFile)
		{
            text_colour = SkinningDefs.TheInstance.GetColorData("table_text_colour", Color.Black);

			scores = new ArrayList();
			int round = gameFile.CurrentRound;
            
			if(gameFile.CurrentPhase == GameFile.GamePhase.TRANSITION)
				round -= 1;

			AutoScroll = true;
			Controls.Add(p);
			this.gameFile = gameFile;
			questions = new ArrayList();

			readScores();


			ReadIgnoreList(gameFile);

            labels = rounds;
		    if (gameFile.Game_Eval_Type == em_GameEvalType.ISO_20K)
		    {
		        labels--; // In ISO20k we only show 4 columns and not all 5, this is reducing the number of labels.
		    }

            roundToColumnHeader = new Dictionary<int, Control> ();
		    for (int i = 1; i <= labels; i++)
		    {
		        var label = createRoundLabel(i);
                p.Controls.Add(label);
                roundToColumnHeader.Add(i, label);
            }

		    var rowColours = new []
		    {
		        SkinningDefs.TheInstance.GetColorDataGivenDefault("table_row_colour", Color.White),
		        SkinningDefs.TheInstance.GetColorDataGivenDefault("table_row_colour_alternate", Color.White)
		    };
		    var row = 0;

			doc = BasicXmlDocument.CreateFromFile(gameFile.GetMaturityRoundFile(1));

			bool first = true;
			bool suppressSections = (SkinningDefs.TheInstance.GetIntData("maturity_suppress_sections", 0) == 1);

			bool? documentUseCheckBoxes = BasicXmlDocument.GetNullableBoolAttribute((XmlElement) doc.DocumentElement, "use_check_boxes");

            stripes = new List<Control> ();
			foreach(XmlNode section in doc.DocumentElement.ChildNodes)
			{
				bool? sectionUseCheckBoxes = BasicXmlDocument.GetNullableBoolAttribute((XmlElement) section, "use_check_boxes") ?? documentUseCheckBoxes;

				if (first || !suppressSections)
				{
					XmlNode section_name = section.SelectSingleNode("section_name");

					if (! suppressSections)
					{
						Label head = new Label();

						string rawtext = section_name.InnerText;
						if (auto_translate)
						{
							rawtext = TextTranslator.TheInstance.Translate(rawtext);
						}

						Font f2 = null;
						string displayFontName = SkinningDefs.TheInstance.GetData("fontname");
						if (auto_translate)
						{
							displayFontName = TextTranslator.TheInstance.GetTranslateFont(displayFontName);
						}
						f2 = ConstantSizeFont.NewFont(displayFontName, 10, FontStyle.Bold);

						head.Text = rawtext;
						head.Font = f2;
					    head.BackColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("table_header_colour", Color.White);
                        head.ForeColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("table_header_text_colour", Color.Black);
					    head.Tag = section;

						if (SkinningDefs.TheInstance.GetColorData("tools_screen_cell_border_colour", Color.Transparent) != Color.Transparent)
						{
							head.SizeChanged += (sender, args) => ((Control) sender).Invalidate();
							head.Paint += control_Paint;
						}

						stripes.Add(head);
					}
				}

				first = false;

				XmlNode aspects = section.SelectSingleNode("aspects");
				foreach(XmlNode aspect in aspects.ChildNodes)
				{
					bool? aspectUseCheckBoxes = BasicXmlDocument.GetNullableBoolAttribute((XmlElement) aspect, "use_check_boxes") ?? sectionUseCheckBoxes;

					XmlNode aspect_name = aspect.SelectSingleNode("aspect_name");
					XmlNode aspect_desc = aspect.SelectSingleNode("aspect_desc");

					// Since we now have user editable XML we have a problem if the dest_tag_name is not unique.
					// Therefore, we make it up instead from the section_name and the aspect_name.
					//
					// Hmmm, this fails when we consider the "Eval_States.xml" file.

					string dest_tag_name = aspect.SelectSingleNode("dest_tag_name").InnerText;

					ArrayList factors = new ArrayList();
					
					ArrayList values = (ArrayList)file_scores[dest_tag_name];
					ArrayList ticks  = (ArrayList)factor_ticks[dest_tag_name];

					XmlNode factorsNode = aspect.SelectSingleNode("factors");

                    if (gameFile.Game_Eval_Type == em_GameEvalType.ISO_20K)
                    {

                        foreach (XmlNode factor in factorsNode.ChildNodes)
                        {
                            XmlNode factorAspect = factor.SelectSingleNode("aspect");
                            XmlNode weighting = factor.SelectSingleNode("weight");

                            factors.Add(new WizardQuestion.Aspect(
                                factorAspect.InnerText,
                                CONVERT.ParseDouble(weighting.InnerText)));
                        }
                    }
                    else
                    {
                        foreach (XmlNode factor in factorsNode.ChildNodes)
                        {
                            XmlNode factorAspect = factor.SelectSingleNode("aspect");
                            XmlNode weighting = factor.SelectSingleNode("weight");

                            factors.Add(new WizardQuestion.Aspect(
                                factorAspect.InnerText,
                                CONVERT.ParseDouble(weighting.InnerText)));
                        }
                    }

					bool enabled = ! Ignore(aspect_name.InnerText);

                    WizardQuestion question;
                    if (gameFile.Game_Eval_Type != em_GameEvalType.ISO_20K)
                    {

                        question = new WizardQuestion (this, aspect_name.InnerText, "", factors, values, ticks,
						                               dest_tag_name, gameFile.CurrentRound, gameFile, enabled, WizardStyle.CHECKBOX, 10, rowColours[row % rowColours.Length]);
                    }
                    else
                    {
						WizardStyle style = WizardStyle.DESC_ONLY;
						if (aspectUseCheckBoxes.HasValue
							&& aspectUseCheckBoxes.Value)
						{
							style = WizardStyle.CHECKBOX;
						}

						question = new ISO20kWizardQuestion (this, aspect_name.InnerText, "", factors, values, ticks,
						                                     dest_tag_name, gameFile.CurrentRound, gameFile, enabled, style, rowColours[row % rowColours.Length]);
                    }

                    if(disabled_questions.Contains(aspect_name.InnerXml))
                    {
                        question.disableQuestion();
                    }

                    p.Controls.Add(question);
                    questions.Add(question);

                    question.Round = round;
				    row++;
				}
			}

			DoLayout();

            if (gameFile.Game_Eval_Type != em_GameEvalType.ISO_20K)
            {
                ((WizardQuestion)questions[0]).SetFocus();
            }
		}

		void control_Paint (object sender, PaintEventArgs args)
		{
			var control = (Control) sender;

			using (var pen = new Pen(SkinningDefs.TheInstance.GetColorData("tools_screen_cell_border_colour", Color.Transparent), 1))
			{
				args.Graphics.DrawRectangle(pen, 0, 0, control.Width - 1, control.Height - 1);
			}
		}

		void ReadIgnoreList (NetworkProgressionGameFile gameFile)
		{
			ignoreList.Clear();
			//if file already exists, read into xml
			string StatesFile = gameFile.Dir + "\\global\\Eval_States.xml";

			BasicXmlDocument xml;
			XmlNode root;

			if (System.IO.File.Exists(StatesFile))
			{

				System.IO.StreamReader file = new System.IO.StreamReader(StatesFile);
				xml = BasicXmlDocument.Create(file.ReadToEnd());
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
						}
					}
				}
			}
		}

		//private bool Ignore(string name, string data_tag, ArrayList ignoreList, ArrayList ignore_tag_List)
		bool Ignore(string name)
		{
			foreach( string s in ignoreList)
			{
				if (s == name)
					return true;
			}
			return false;
		}

		public override void DoLayout()
		{
			int i = 0;
			int y_offset = 30;

		    int rounds = roundToColumnHeader.Keys.Count;
		    int width = Width - 20 - p.Left;
		    int leftColumnWidth = 638;
		    int spaceAvailable = width - leftColumnWidth;

			var columnWidth = spaceAvailable / rounds;

			foreach (var round in roundToColumnHeader.Keys)
            {
                roundToColumnHeader[round].Bounds = new Rectangle (leftColumnWidth + ((round - 1) * columnWidth) - (columnWidth / 2) + 15, roundToColumnHeader[round].Location.Y, columnWidth, roundToColumnHeader[round].Height);
            }

		    foreach (var stripe in stripes)
		    {
                stripe.Width = width;
            }

            foreach (Control question in questions)
		    {
		        question.Width = width;
		    }

            foreach (Control c in p.Controls)
			{
				if(++i > rounds)
				{
					if (c.GetType().ToString() == "System.Windows.Forms.Label")
					{
					    if (c.Tag != null)
					    {
					        c.Location = new Point(10, y_offset + 30);
					        y_offset = c.Bottom - 30;
					    }
                        else
					    {
					        c.Location = new Point(10, y_offset);
					    }
					}
					else
					{
					    c.Location = new Point(10, y_offset + 30);
					    y_offset += c.Height;
					}
				}
			}
			p.Size = new Size (width ,y_offset+50);
		}

		protected virtual void readScores()
		{
			int round = gameFile.CurrentRound;
            int labels = rounds - 1;
			if(gameFile.CurrentPhase == GameFile.GamePhase.TRANSITION)
				round -= 1;

			for(int i = 1; i <= round; i++)
			{
                
				BasicXmlDocument xml = BasicXmlDocument.CreateFromFile(gameFile.GetMaturityRoundFile(i));

				foreach(XmlNode section in xml.DocumentElement.ChildNodes)
				{
					XmlNode section_name = section.SelectSingleNode("section_name");
					XmlNode aspects = section.SelectSingleNode("aspects");

					foreach(XmlNode aspect in aspects)
					{
						XmlNode aspect_name = aspect.SelectSingleNode("aspect_name");

						string dest_tag_name = aspect.SelectSingleNode("dest_tag_name").InnerText;

						string name = dest_tag_name;
						
						// creates a 5 * #factors array list
						if(factor_ticks[name] == null)
						{
							ArrayList tmp = new ArrayList();

							for (int j = 0; j < labels; j++)
							{
								tmp.Add(new ArrayList());
							}
							factor_ticks[name] = tmp;
						}

                        XmlNode aspect_off = aspect.SelectSingleNode("aspect_off");

						XmlNode dest_tag_data = aspect.SelectSingleNode("dest_tag_data");
						
						if(file_scores[name] != null)
						{
							((ArrayList)file_scores[name]).Add(dest_tag_data.InnerText);
						}
						else
						{
							ArrayList val = new ArrayList();
							val.Add(dest_tag_data.InnerText);
							file_scores[name] = val;
						}

                        if (aspect_off != null)
                        {
                            bool disableQuestion = CONVERT.ParseBool(aspect_off.InnerText, false);
                            XmlNode QuestionToDisable = aspect.SelectSingleNode("aspect_name");
                            if (disableQuestion)
                            {
                                disabled_questions.Add(QuestionToDisable.InnerXml);
                            }
                        }

						// which factors are ticked 
						XmlNode factors = aspect.SelectSingleNode("factors");

						foreach(XmlNode factor in factors)
						{
							XmlNode factor_data = factor.SelectSingleNode("factor_data");

                            if (((ArrayList)factor_ticks[name]).Count >= i)
                            {
                                ((ArrayList)((ArrayList)factor_ticks[name])[(i - 1)]).Add(
                                    factor_data.InnerText);
                            }
						}
					}
				}
               
			}
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

		Label createRoundLabel (int round)
		{
			Label tmp = new Label();

			tmp.Text = GetRoundLabelText(round);
			tmp.Location = new Point(0,20);
			tmp.TextAlign = ContentAlignment.MiddleCenter;
			tmp.Size = new Size(30,30);

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

		public override void setQuestion(WizardQuestion question)
		{
			int i = 1;
			foreach(WizardQuestion q in questions)
			{
				if(q == question)
					selected = i;

				i++;
			}
		}

		public override void setRounds(int round)
		{
			foreach(WizardQuestion question in questions)
			{
				question.Round = round;
			}
		}

		public override void NextFocus()
		{
			if(selected < questions.Count)
				((WizardQuestion)questions[selected++]).SetFocus();
		}

	    protected override void OnSizeChanged (EventArgs e)
	    {
	        base.OnSizeChanged(e);
            DoLayout();
	    }
	}
}