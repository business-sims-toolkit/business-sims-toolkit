using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Xml;

using GameManagement;
using LibCore;
using CoreUtils;

namespace CommonGUI
{
	/// <summary>
	/// Summary description for PathFinderSurveyCardWizard.
	/// </summary>
	public class PathFinderSurveyCardWizard : ScoreCardWizardBase
	{
		Font f = SkinningDefs.TheInstance.GetFont(10);

		Hashtable panels = new Hashtable();

		Hashtable file_scores = new Hashtable();
		Hashtable factor_ticks = new Hashtable();

		BasicXmlDocument doc;
		Panel p = new Panel();

		ArrayList questions;
		int selected = 1;

		NetworkProgressionGameFile gameFile;

		int rounds = SkinningDefs.TheInstance.GetIntData("roundcount", 5);

		ArrayList ignoreList = new ArrayList();

		bool auto_translate = true;

	    string surveyFilename;

		Dictionary<int, Control> roundToColumnHeader;
		List<Control> stripes;

		public PathFinderSurveyCardWizard (NetworkProgressionGameFile gameFile, string surveyFilename = "pathfinder_survey_wizard.xml")
		{
			int round = gameFile.CurrentRound;

			if(gameFile.CurrentPhase == GameFile.GamePhase.TRANSITION)
				round -= 1;

			AutoScroll = true;
			Controls.Add(p);
			this.gameFile = gameFile;
			questions = new ArrayList();

            this.surveyFilename = surveyFilename;
            
			readScores();
			ReadIgnoreList(gameFile);

			roundToColumnHeader = new Dictionary<int, Control>();
			for (int i = 1; i <= rounds; i++)
			{
				var label = createRoundLabel(CONVERT.ToStr(i));
				p.Controls.Add(label);
				roundToColumnHeader.Add(i, label);
			}

			stripes = new List<Control> ();

            string xmlfile = gameFile.GetRoundFile(1, surveyFilename, GameFile.GamePhase.OPERATIONS);

			if (File.Exists(xmlfile))
			{
				StreamReader file = new StreamReader(xmlfile);
				doc = BasicXmlDocument.Create(file.ReadToEnd());
				file.Close();
				file = null;

				bool first = true;
				//bool suppressSections = (CoreUtils.SkinningDefs.TheInstance.GetIntData("maturity_suppress_sections", 0) == 1);
				bool suppressSections = false;

				foreach (XmlNode section in doc.DocumentElement.ChildNodes)
				{
					if (first || !suppressSections)
					{
						XmlNode section_name = section.SelectSingleNode("section_name");

						if (!suppressSections)
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
							f2 = ConstantSizeFont.NewFont(displayFontName, 10);

							head.Text = rawtext;
							head.Size = new Size(600, 20);
							head.Font = f2;

							Panel hr = new Panel();
							hr.BackColor = Color.DarkGray;
							hr.Size = new Size(960, 1);
							stripes.Add(hr);

							p.Controls.Add(hr);
							p.Controls.Add(head);
						}
					}

					first = false;

					XmlNode aspects = section.SelectSingleNode("aspects");
					foreach (XmlNode aspect in aspects.ChildNodes)
					{
						XmlNode aspect_name = aspect.SelectSingleNode("aspect_name");
						XmlNode aspect_desc = aspect.SelectSingleNode("aspect_desc");
					    XmlNode aspectMaxLimit = aspect.SelectSingleNode("max_limit");
					    int digitLimit = 10;

                        if (aspectMaxLimit != null)
                        {
                            digitLimit = CONVERT.ParseInt(aspectMaxLimit.InnerText);
                        }
						// Since we now have user editable XML we have a problem if the dest_tag_name is not unique.
						// Therefore, we make it up instead from the section_name and the aspect_name.
						//
						// Hmmm, this fails when we consider the "Eval_States.xml" file.

						//System.Xml.XmlNode dest_tag_name = aspect.SelectSingleNode("dest_tag_name");
						//string dest_tag_name = section_name.InnerText + "_" + aspect_name.InnerText;
						//dest_tag_name = dest_tag_name.Replace(" ", "_");

						string dest_tag_name = aspect.SelectSingleNode("dest_tag_name").InnerText;

						ArrayList factors = new ArrayList();

						ArrayList values = (ArrayList) file_scores[dest_tag_name];//dest_tag_name.InnerText];
						ArrayList ticks = (ArrayList) factor_ticks[dest_tag_name];//dest_tag_name.InnerText];

						XmlNode factorsNode = aspect.SelectSingleNode("factors");

						foreach (XmlNode factor in factorsNode.ChildNodes)
						{
							XmlNode factorAspect = factor.SelectSingleNode("aspect");
							XmlNode weighting = factor.SelectSingleNode("weight");

							factors.Add(new WizardQuestion.Aspect(
								factorAspect.InnerText,
								CONVERT.ParseDouble(weighting.InnerText)));
						}

						bool enabled = true;
						if (Ignore(aspect_name.InnerText)) enabled = false;

                        WizardQuestion question =
                                new WizardQuestion(this, aspect_name.InnerText, aspect_desc.InnerText, factors, values, ticks,
                                    dest_tag_name/*.InnerText*/, gameFile.CurrentRound, gameFile, enabled, WizardStyle.None, digitLimit, Color.White);
                        question.SetXmlFilename(this.surveyFilename);

						p.Controls.Add(question);
						questions.Add(question);

						question.Round = round;
					}
				}
				DoLayout();


               // if (gameFile.Game_Eval_Type != em_GameEvalType.ISO_20K)
                {
                    ((WizardQuestion)questions[0]).SetFocus();
                }
               // else
                {
                  //  ((ISO20kWizardQuestion)questions[0]).SetFocus();
                }
			}
		}

		void ReadIgnoreList(NetworkProgressionGameFile gameFile)
		{
			ignoreList.Clear();
			//if file already exists, read into xml
			string StatesFile = gameFile.Dir + "\\global\\Eval_States.xml";

			BasicXmlDocument xml;
			XmlNode root;

			if (File.Exists(StatesFile))
			{

				StreamReader file = new StreamReader(StatesFile);
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
			int y_offset = 20;

			int rounds = roundToColumnHeader.Keys.Count;
			int width = Width - 20 - p.Left;
			int leftColumnWidth = 638;
			int spaceAvailable = width - leftColumnWidth;
			foreach (var round in roundToColumnHeader.Keys)
			{
				roundToColumnHeader[round].Location = new Point(leftColumnWidth + ((round - 1) * (spaceAvailable / rounds)), roundToColumnHeader[round].Location.Y);
			}

			foreach (var stripe in stripes)
			{
				stripe.Width = width - stripe.Left;
			}

			foreach (Control question in questions)
			{
				question.Width = width - question.Left;
			}

			foreach (Control c in p.Controls)
			{
				if(++i > rounds)
				{
					if(c.GetType().ToString() == "System.Windows.Forms.Panel")
					{
						y_offset += 40;
						c.Location = new Point(10,y_offset+21);
					}
					else if (c.GetType().ToString() == "System.Windows.Forms.Label")
					{
						c.Location = new Point(10,y_offset);
					}
					else
					{
						c.Location = new Point(10,y_offset+30);
						y_offset  += c.Height;
					}
				}
			}
			p.Size = new Size(width, y_offset+50);
		}

		void readScores()
		{
			int round = gameFile.CurrentRound;

			if(gameFile.CurrentPhase == GameFile.GamePhase.TRANSITION)
				round -= 1;

			for(int i = 1; i <= round; i++)
			{
				string NetworkFile = "";

                NetworkFile = gameFile.GetRoundFile(i, surveyFilename, GameFile.GamePhase.OPERATIONS);

				if (File.Exists(NetworkFile))
				{
					StreamReader file = new StreamReader(NetworkFile);
					BasicXmlDocument xml = BasicXmlDocument.Create(file.ReadToEnd());
					file.Close();
					file = null;

					foreach(XmlNode section in xml.DocumentElement.ChildNodes)
					{
						XmlNode section_name = section.SelectSingleNode("section_name");
						XmlNode aspects = section.SelectSingleNode("aspects");

						foreach(XmlNode aspect in aspects)
						{
							//System.Xml.XmlNode dest_tag_name = aspect.SelectSingleNode("dest_tag_name");

							XmlNode aspect_name = aspect.SelectSingleNode("aspect_name");

							//string dest_tag_name = section_name.InnerText + "_" + aspect_name.InnerText;
							//dest_tag_name = dest_tag_name.Replace(" ", "_");

							string dest_tag_name = aspect.SelectSingleNode("dest_tag_name").InnerText;

							string name = dest_tag_name;//dest_tag_name.InnerText;
							
							// creates a 5 * #factors array list
							if(factor_ticks[name] == null)
							{
								ArrayList tmp = new ArrayList();

								for (int j = 0; j < rounds; j++)
								{
									tmp.Add(new ArrayList());
								}
								factor_ticks[name] = tmp;
							}

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

							// which factors are ticked 
							XmlNode factors = aspect.SelectSingleNode("factors");

							foreach(XmlNode factor in factors)
							{
								XmlNode factor_data = factor.SelectSingleNode("factor_data");

								((ArrayList)((ArrayList)factor_ticks[name])[(i-1)]).Add(
									factor_data.InnerText);
							}
						}
					}
				}
			}
		}

		Label createRoundLabel(string text)
		{
			Label tmp = new Label();

			tmp.Text = text;
			tmp.Location = new Point(0, 20);
			tmp.BackColor = Color.White;
			tmp.TextAlign = ContentAlignment.MiddleCenter;
			tmp.Size = new Size(30,30);
			tmp.BorderStyle = BorderStyle.None;

			Font f2 = SkinningDefs.TheInstance.GetFont(10, FontStyle.Bold);
			tmp.Font = f2;

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
			DoSize();
		}

		void DoSize ()
		{
			DoLayout();
		}
	}
}