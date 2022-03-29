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
	public class TabTextBox : TextBox
	{
		ScoreCardWizardBase	_parent;

		public TabTextBox(ScoreCardWizardBase parent)
		{
			_parent = parent;
		}

		protected override bool ProcessDialogKey(Keys keyData)
		{
			if(keyData == Keys.Tab)
			{
				_parent.NextFocus();
				return true;
			}

			return false;
		}
	}

	public enum WizardStyle
	{
		CHECKBOX,			//expands to show factors using checkbox (multiple selection allowed)
		RADIOBUTTON,	//expands to show factors using radio buttons (multiple selection not allowed)
		DESC_ONLY,			//expands to show description
        None
	}

	///	<summary>
	/// The question can operate in 3 modes (wizard style), dependant on constructor called 
	///  CHECKBOX -- STANDARD STYLE used in most skins
	///    you have multiple check boxes and each box is weighted and the total value is sum of all checked box
	///  RADIOBUTTON --
	///    you have a number of radiobutton which are linked together (only one can be checked at any time)
	///    The check boxes are weighted and produce the overall score for the question  
	///  DESC ONLY --
	///    The facilitor needs to enter the number but can click to expand the question and show the description
	///	</summary>
	public class WizardQuestion	: BasePanel
	{
		string xmlFilenameBase;

		WizardStyle currentStyle = WizardStyle.CHECKBOX;

		public struct Aspect
		{
			public string question;
			public double weight;

			public Aspect(string _q , double _w)
			{ 
				question = _q;
				weight = _w;
			}
		}

		int	_round = 1;
		public virtual int	Round
		{
			get	{ return _round; }
			set	
			{ 
				_round = value;	
				setRound();
			}
		}

		bool _collapsed	= false;
		public	virtual bool Collapsed
		{
			get	{ return _collapsed; }
			set	
			{
			    Height = (value) ? 20 : full_height;

			    if (SkinningDefs.TheInstance.GetBoolData("wizard_allow_expand", true))
			    {
			        _collapsed = value;
			    }
			    setCollapsedArrow();
			}
		}

		NetworkProgressionGameFile gameFile;
		string tag_name;
		Panel p	= new Panel();
		int	full_height;
		int current_round;
		ScoreCardWizardBase	parent;
		Font f = SkinningDefs.TheInstance.GetFont(10);
		ArrayList factors;
		ArrayList factors_check	= new ArrayList();
		ArrayList score_text = new ArrayList();
		TextBox	r1_text;
		TextBox	r2_text;
		TextBox	r3_text;
		TextBox	r4_text;
		TextBox	r5_text;
		ArrayList r1_factors = new ArrayList();
		ArrayList r2_factors = new ArrayList();
		ArrayList r3_factors = new ArrayList();
		ArrayList r4_factors = new ArrayList();
		ArrayList r5_factors = new ArrayList();
	    protected int rounds;

		CheckBox state = new CheckBox();

		string _question;
		bool _enabled;

		bool done = false;
		bool auto_translate = true;

	    int digitLimit;

		public void SetXmlFilename (string xmlFilename)
		{
			xmlFilenameBase = xmlFilename;
		}

        public WizardQuestion()
        {
        }

		public WizardQuestion(ScoreCardWizardBase _p, string question, ArrayList _factors,
			ArrayList round_scores, ArrayList round_ticks, string tag_name, int round,
			NetworkProgressionGameFile gameFile, bool enabled)
            : this (_p, question, "", 
			_factors,	round_scores, round_ticks, tag_name, round, gameFile, enabled, 
			WizardStyle.CHECKBOX, 10, Color.White)
		{
		}

		public WizardQuestion (ScoreCardWizardBase _p, string question, string desc, 
			ArrayList _factors,	ArrayList round_scores, ArrayList round_ticks, 
			string tag_name, int round,	NetworkProgressionGameFile gameFile, bool enabled, 
			WizardStyle newStyle, int digitLimit, Color backColour)
		{
			_question = question;
			_enabled = enabled;
			currentStyle = newStyle;

		    this.digitLimit = digitLimit;

		    BackColor = backColour;

			xmlFilenameBase = "";

			this.tag_name = tag_name;
			Width = 970;
			parent	= _p;
			factors = _factors;
			current_round = round;
			this.gameFile = gameFile;

			if(gameFile.CurrentPhase == GameFile.GamePhase.TRANSITION)
			{
				current_round -= 1;
			}

			string rawtext_question = question;
			rawtext_question = TextTranslator.TheInstance.Translate(rawtext_question);

			string rawtext_desc = desc;
			rawtext_desc = TextTranslator.TheInstance.Translate(rawtext_desc);			

			Font f2 = null;
			string displayFontName = SkinningDefs.TheInstance.GetData("fontname");
			if (auto_translate)
			{
				displayFontName = TextTranslator.TheInstance.GetTranslateFont(displayFontName);
			}
			f2 = ConstantSizeFont.NewFont(displayFontName,10);

			Label name = new Label();
			name.Text = rawtext_question;
			name.Location =	new	Point(40,1);
			name.Size =	new	Size(500,18);
			name.Font =	f2;
			name.Enabled = enabled;
			Controls.Add(name);

			name.Click += name_Click;

			if (currentStyle == WizardStyle.DESC_ONLY)
			{
				Label desc_text = new Label();
				desc_text.Text = rawtext_desc;
				desc_text.Location = new Point(50, 20);
				desc_text.Size = new Size(560, 20);
				desc_text.Font = f2;
				Controls.Add(desc_text);
			}

            if (question == "Processing Rate")
            {
                r1_text = createTwoDigitTextBox(620, 0, 1);
                r1_text.Name = "1";
                r2_text = createTwoDigitTextBox(690, 0, 2);
                r2_text.Name = "2";
                r3_text = createTwoDigitTextBox(760, 0, 3);
                r3_text.Name = "3";
                r4_text = createTwoDigitTextBox(830, 0, 4);
                r4_text.Name = "4";
                r5_text = createTwoDigitTextBox(900, 0, 5);
                r5_text.Name = "5";
            }
            else
            {
                r1_text = createTextBox(620, 0, 1);
                r1_text.Name = "1";
                r2_text = createTextBox(690, 0, 2);
                r2_text.Name = "2";
                r3_text = createTextBox(760, 0, 3);
                r3_text.Name = "3";
                r4_text = createTextBox(830, 0, 4);
                r4_text.Name = "4";
                r5_text = createTextBox(900, 0, 5);
                r5_text.Name = "5";
            
            }

			score_text.Add(r1_text);
			score_text.Add(r2_text);
			score_text.Add(r3_text);
			score_text.Add(r4_text);
			score_text.Add(r5_text);
	
			rounds = SkinningDefs.TheInstance.GetIntData("roundcount", 5);

			if (rounds >= 1)
			{
				Controls.Add(r1_text);
			}

			if (rounds >= 2)
			{
				Controls.Add(r2_text);
			}

			if (rounds >= 3)
			{
				Controls.Add(r3_text);
			}

			if (rounds >= 4)
			{
				Controls.Add(r4_text);
			}

			if (rounds >= 5)
			{
				Controls.Add(r5_text);
			}

			// Set Initial Scores
			// Fix for Case 5494:   MS: crash viewing eval scores with too many sections.
			// Bad XML can have more than 5 rounds! So limit to 5 rounds max.
			int num_rounds = round_scores.Count;
			if(num_rounds > 5) num_rounds = 5;
			//
			for(int x = 0; x < num_rounds; x++)
			{
				TextBox tb = score_text[x] as TextBox;
				tb.Text = (string) round_scores[x];
				//tb.Text = CONVERT.ToStr( round_scores[x] );
			}

			int i = 1;
			if (currentStyle != WizardStyle.DESC_ONLY)
			{
                if (gameFile.Game_Eval_Type != em_GameEvalType.ISO_20K)
                {
                    foreach (Aspect aspect in factors)
                    {
                        Label tmp = new Label();
                        tmp.Text = aspect.question;
                        tmp.Size = new Size(600, 20);
                        tmp.Location = new Point(50, i * 20);

                        ArrayList[] factorsByRound = new ArrayList[] { r1_factors, r2_factors, r3_factors, r4_factors, r5_factors };

                        for (int j = 0; j < SkinningDefs.TheInstance.GetIntData("roundcount", 5); j++)
                        {
                            Control r_fac = null;
                            if (currentStyle == WizardStyle.CHECKBOX)
                            {
                                r_fac = createCheckBox(628 + (70 * j), i * 20 + 2);
                            }
                            if (currentStyle == WizardStyle.RADIOBUTTON)
                            {
                                r_fac = createRadioButton(628 + (70 * j), i * 20 + 2);
                            }
                            if (r_fac != null)
                            {
                                Controls.Add(r_fac);
                                factorsByRound[j].Add(r_fac);
                            }
                        }

                        Controls.Add(tmp);
                        i++;
                    }
                }
                else
                {
                    foreach (Aspect aspect in factors)
                    {
                        Label tmp = new Label();
                        tmp.Text = aspect.question;
                        tmp.Size = new Size(600, 20);
                        tmp.Location = new Point(50, i * 20);

                        ArrayList[] factorsByRound = new ArrayList[] { r1_factors, r2_factors, r3_factors, r4_factors, r5_factors };

                        for (int j = 0; j < SkinningDefs.TheInstance.GetIntData("roundcount", 5) - 1; j++)
                        {
                            for (int boxes = 0; boxes < 4; boxes++)
                            {
                                Control r_fac = null;
                                if (currentStyle == WizardStyle.CHECKBOX)
                                {

                                    r_fac = createCheckBox(628 + (70 * j) + 10*boxes, i * 20 + 2);

                                }
                                if (currentStyle == WizardStyle.RADIOBUTTON)
                                {
                                    r_fac = createRadioButton(628 + (70 * j), i * 20 + 2);
                                }
                                if (r_fac != null)
                                {
                                    Controls.Add(r_fac);

                                    factorsByRound[j].Add(r_fac);
                                }
                            }
                        }

                        Controls.Add(tmp);
                        i++;
                    }
                }

				factors_check.Add(r1_factors);
				factors_check.Add(r2_factors);
				factors_check.Add(r3_factors);
				factors_check.Add(r4_factors);
				factors_check.Add(r5_factors);

				for (int y = 0; y < round_ticks.Count; y++)
				{
					for (int x = 0; x < ((ArrayList)round_ticks[y]).Count; x++)
					{
						string tmp = (string)((ArrayList)round_ticks[y])[x];
						if (tmp == "True")
						{
							if (currentStyle == WizardStyle.RADIOBUTTON)
							{
								((RadioButton)((ArrayList)factors_check[y])[x]).Checked = true;
							}
							if (currentStyle == WizardStyle.CHECKBOX)
							{
								((CheckBox)((ArrayList)factors_check[y])[x]).CheckState = CheckState.Checked;
							}
						}
					}
				}
			}
			else
			{
				i = 2;
			}

			state.Checked = _enabled;
			state.Size = new Size(15,15);
			state.Location = new Point(10,4);
			state.CheckStateChanged += state_CheckStateChanged;

			Controls.Add(state);

			if (currentStyle != WizardStyle.DESC_ONLY)
			{
				if (factors.Count > 0)
				{
					p.Size = new Size(9, 9);
					p.Location = new Point(30, 4);
					p.Enabled = _enabled;
					p.Click += p_Click;
					Controls.Add(p);
				}
			}
			else
			{
				p.Size = new Size(9, 9);
				p.Location = new Point(30, 4);
				p.Enabled = _enabled;
				p.Click += p_Click;
				Controls.Add(p);
			}

		    full_height = i * 20;
            Collapsed = true;
			done = true;
		}

		protected virtual void setCollapsedArrow()
		{
		    if (SkinningDefs.TheInstance.GetBoolData("wizard_allow_expand", true))
		    {
		        string img_path = (Collapsed) ? "arrow_collapsed.png" : "arrow_expanded.png";
		        Image bg = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\wizard\\" + img_path);
		        p.BackgroundImage = bg;
		    }
		}

		public void disableQuestion()
        {
            state.Checked = false;
        }

		protected virtual void setRound()
		{
			for(int	i =	0; i < 5; i++)
			{
				if (currentStyle == WizardStyle.RADIOBUTTON)
				{
					foreach (RadioButton box in ((ArrayList)factors_check[i]))
					{
						box.Enabled = (i == _round - 1);
					}
				}
				if (currentStyle == WizardStyle.CHECKBOX)
				{
					foreach (CheckBox box in ((ArrayList)factors_check[i]))
					{
						box.Enabled = (i == _round - 1);
					}
				}
			}
		}

        public TextBox createTextBox(int x, int y, int round)
        {
            TabTextBox tmp = new TabTextBox(parent);

            tmp.Size = new Size(30, 20);
            tmp.TextAlign = HorizontalAlignment.Center;
            tmp.BorderStyle = BorderStyle.Fixed3D;
            tmp.KeyPress += tmp_KeyPress;
            tmp.GotFocus += TextBox_GotFocus;
            if (_enabled == false) tmp.Enabled = false;
            else tmp.Enabled = (current_round >= round);
            tmp.Location = new Point(x, y);
            return tmp;
        }

        public TextBox createTwoDigitTextBox(int x, int y, int round)
        {
            TabTextBox tmp = new TabTextBox(parent);

            tmp.Size = new Size(30, 20);
            tmp.TextAlign = HorizontalAlignment.Center;
            tmp.BorderStyle = BorderStyle.Fixed3D;
            tmp.KeyPress += tmp_KeyPress_multi;
            tmp.GotFocus += TextBox_GotFocus;
            if (_enabled == false) tmp.Enabled = false;
            else tmp.Enabled = (current_round >= round);
            tmp.Location = new Point(x, y);
            return tmp;
        }

        public CheckBox createCheckBox(int x, int y)
        {
            return createCheckBox(x, y, 15);
        }

        public CheckBox createCheckBox(int x, int y, int size)
        {
            CheckBox tmp = new CheckBox();
            tmp.Enabled = false;
            tmp.CheckStateChanged += CheckStateChanged;
            tmp.Size = new Size(size, size);
            tmp.Location = new Point(x, y);
            return tmp;
        }

        public RadioButton createRadioButton(int x, int y)
		{
			RadioButton tmp = new RadioButton();
			tmp.Enabled = false;
			tmp.CheckedChanged += CheckStateChanged_Radio;
			tmp.Size = new Size(12, 12);
			tmp.Location = new Point(x, y);
			return tmp;
		}

        public void name_Click(object sender, EventArgs e)
		{
			Collapsed = !Collapsed;
			parent.DoLayout();
			parent.ScrollControlIntoView(this);
		}

		protected virtual void state_CheckStateChanged(object sender, EventArgs e)
		{

			if (state.Checked == true)
			{
				//enable all controls
				foreach (Control c in Controls)
				{
					//if textbox, only enable if we have got to that round
					if (c.GetType().ToString() == "CommonGUI.TabTextBox")
					{
						//get which round this textbox is for
						int rnd = CONVERT.ParseInt(c.Name);

						c.Enabled = (current_round >= rnd);
					}
					else
					{
						c.Enabled = true;
					}
				}
			}
			else
			{
				//disable all controls, apart from checkbox obviously
				foreach (Control c in Controls)
				{
					if (c != state || c != sender)
					{
						c.Enabled = false;
					}
				}
			}
			SaveState();
		}

		protected virtual void SaveState()
		{
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
			}
			else
			{
				xml = BasicXmlDocument.Create();
				root = (XmlNode) xml.CreateElement("ignore_list");
				xml.AppendChild(root);
			}
			
			//only store the list of questions to be switched off in file, so if not in file then switched on
			if (state.Checked == true)
			{
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

								if (question == _question)
								{
									root.RemoveChild(node);
								}
							}
						}
					}
				}
			}
			else
			{
				//switch question off - add to off file
				XmlNode cost = (XmlNode) xml.CreateElement("ignore");
				((XmlElement)cost).SetAttribute( "question",_question);
				root.AppendChild(cost);
			}

			xml.SaveToURL(null, gameFile.Dir + "\\global\\Eval_States.xml");
		}

		void CheckStateChanged(object sender, EventArgs	e)
		{
			if(done)
			{
				int	i =	0;
				double score = 0;
				foreach(CheckBox box in	((ArrayList)factors_check[(_round-1)]))
				{
					if(box.CheckState == CheckState.Checked)
					{
						score += 1 * ((Aspect)factors[i]).weight;
					}
	
					i++;
				}

				((TextBox)score_text[(Round-1)]).Text =	CONVERT.ToStr(Math.Round(score));
		
				setScore(((TextBox)score_text[(Round-1)]) , (int)Math.Round(score));
			}
		}

		void CheckStateChanged_Radio(object sender, EventArgs e)
		{
			if (done)
			{
				int i = 0;
				double score = 0;
				foreach (RadioButton box in ((ArrayList)factors_check[(_round - 1)]))
				{
					if (box.Checked)
					{
						score += 1 * ((Aspect)factors[i]).weight;
					}
					i++;
				}
				((TextBox)score_text[(Round - 1)]).Text = CONVERT.ToStr(Math.Round(score));
				setScore(((TextBox)score_text[(Round - 1)]), (int)Math.Round(score));
			}
		}

		protected void setScore (TextBox display, int score)
		{
			display.Text = CONVERT.ToStr(score);

			string filenameBase = xmlFilenameBase;

			if (filenameBase == "")
			{
				filenameBase = gameFile.GetMaturityFilename();
			}

			string NetworkFile = gameFile.GetRoundFile(Round, filenameBase, GameFile.GamePhase.OPERATIONS);

			System.IO.StreamReader file = new System.IO.StreamReader(NetworkFile);
			BasicXmlDocument xml = BasicXmlDocument.Create(file.ReadToEnd());
			file.Close();
			file = null;

			foreach (XmlNode section in xml.DocumentElement.ChildNodes)
			{
				XmlNode section_name = section.SelectSingleNode("section_name");
				XmlNode aspects = section.SelectSingleNode("aspects");

				foreach (XmlNode aspect in aspects.ChildNodes)
				{
					XmlNode aspect_name = aspect.SelectSingleNode("aspect_name");

					string dest_tag_name = aspect.SelectSingleNode("dest_tag_name").InnerText;

					if (dest_tag_name == tag_name)
					{
						XmlNode dest_tag_data = aspect.SelectSingleNode("dest_tag_data");

						dest_tag_data.InnerText = CONVERT.ToStr(score);

						XmlNode factorsNode = aspect.SelectSingleNode("factors");

						// which factors are ticked 
						int i = 0;
						foreach (XmlNode factor in factorsNode.ChildNodes)
						{
							XmlNode factor_data = factor.SelectSingleNode("factor_data");

							if (currentStyle == WizardStyle.CHECKBOX)
							{
								factor_data.InnerText =
									CONVERT.ToStr(((CheckBox)((ArrayList)factors_check[Round - 1])[i]).Checked);
							}
							if (currentStyle == WizardStyle.RADIOBUTTON)
							{
								factor_data.InnerText =
									CONVERT.ToStr(((RadioButton)((ArrayList)factors_check[Round - 1])[i]).Checked);
							}

							i++;
						}
					}
				}
			}

			xml.SaveToURL(null, NetworkFile);
		}

		void TextBox_GotFocus(object sender, EventArgs e)
		{
			if(done)
			{
				int	round =	1;

				foreach(TextBox	text in	((ArrayList)score_text))
				{
					if(text	== ((TextBox)sender))
					{
						text.SelectAll();
						break;
					}
	
					round++;
				}	
	
				Round =	round;

				parent.setQuestion(this);
				parent.setRounds(round);
			}
		}

		void tmp_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBox s = (TextBox)sender;

            if (Char.IsDigit(e.KeyChar))
            {
                if (s.SelectionLength != 0)
                {
                    s.Text = string.Empty;
                }

                int val = CONVERT.ParseInt(s.Text + e.KeyChar);

                if ((tag_name == "People_SelectServiceRequirements" && val < 26) || 
                    val <= digitLimit)
                {
                    setScore(s, val);

                    s.SelectionStart = s.Text.Length;
                }
                
            }
            else if (e.KeyChar == (char)Keys.Back || e.KeyChar == (char)Keys.Delete)
            {
                int firstStringLength = s.SelectionStart;
                int selectionEnd = s.SelectionStart + s.SelectionLength;
                int startSecondString = selectionEnd;

                if (s.SelectionLength == 0)
                {
                    if (e.KeyChar == (char) Keys.Back)
                    {
                        firstStringLength--;
                    }
                    else if (e.KeyChar == (char) Keys.Delete)
                    {
                        startSecondString++;
                    }
                }

                firstStringLength = Math.Max(firstStringLength, 0);
                startSecondString = Math.Min(startSecondString, s.Text.Length);

                int secondStringLength = s.Text.Length - startSecondString;

                s.Text = s.Text.Substring(0, firstStringLength) +
                         s.Text.Substring(startSecondString, secondStringLength);
                
                s.SelectionStart = firstStringLength;

                if (s.Text == "")
                {
                    setScore(s, 0);
                }
            }
            e.Handled = true;
        }

		void tmp_KeyPress_multi(object sender, KeyPressEventArgs e)
        {
            TextBox s = (TextBox)sender;

            if (Char.IsDigit(e.KeyChar))
            {
                if (tag_name != "People_SelectServiceRequirements")
                {
                    int val;
                    if (s.Text == "1")

                        val = Convert.ToInt32("1" + ((int)(e.KeyChar) - 48));
                    else if(s.Text == "2" && (e.KeyChar == '0' || e.KeyChar == '1' || e.KeyChar == '2' || e.KeyChar == '3' || e.KeyChar == '4' || e.KeyChar == '5'))
                        val = Convert.ToInt32("2" + ((int)(e.KeyChar) - 48));
                    else
                        val = (int)e.KeyChar - 48;

                    setScore(s, val);
                }
                else
                {
                    if (s.SelectionLength != 0)
                        s.Text = "";

                    int val = CONVERT.ParseInt(s.Text + CONVERT.ToStr((int)e.KeyChar - 48));

                    if (val < 26)
                    {
                        setScore(s, val);

                        s.SelectionStart = s.Text.Length;
                    }
                }
            }
            e.Handled = true;
        }

		public virtual void SetFocus()
		{
			((TextBox)score_text[Round-1]).Focus();
		}

		protected virtual void p_Click(object sender, EventArgs e)
		{
			Collapsed = ! Collapsed;
			parent.DoLayout();
			parent.ScrollControlIntoView(this);
		}

	    protected override void OnSizeChanged (EventArgs e)
	    {
	        base.OnSizeChanged(e);
	        DoSize();
		    Invalidate();
	    }

	    protected virtual void DoSize ()
	    {
	        int leftColumnWidth = 628;
	        var textBoxes = new List<Control> (new[] { r1_text, r2_text, r3_text, r4_text, r5_text });
	        var checkBoxes = new List<ArrayList> (new [] { r1_factors, r2_factors, r3_factors, r4_factors, r5_factors });

		    var columnWidth = ((Width - 10 - leftColumnWidth) / Math.Max(1, rounds));

			for (int round = 1; round <= rounds; round++)
	        {
		        if (SkinningDefs.TheInstance.GetBoolData("tools_screen_cells_full_size", false))
		        {
			        var textBox = textBoxes[round - 1];

			        textBox.AutoSize = false;

			        var midX = leftColumnWidth + ((round - 1) * columnWidth) + 15;
			        var width = columnWidth / 3;

					textBox.Bounds = new Rectangle (midX - (width / 2), textBox.Top, width, textBox.Height);
		        }
				else
		        {
			        textBoxes[round - 1].Location = new Point(leftColumnWidth + (columnWidth * (round - 1)), textBoxes[round - 1].Location.Y);
		        }

		        foreach (Control checkBox in checkBoxes[round - 1])
	            {
	                checkBox.Location = new Point(8 + leftColumnWidth + (columnWidth * (round - 1)), checkBox.Location.Y);
	            }
	        }
        }

		protected override void OnPaint (PaintEventArgs e)
		{
			base.OnPaint(e);

			var borderColour = SkinningDefs.TheInstance.GetColorData("tools_screen_cell_border_colour", Color.Transparent);
			if (borderColour != Color.Transparent)
			{
				using (var pen = new Pen(borderColour, 1))
				{
					e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
				}
			}
		}
	}
}