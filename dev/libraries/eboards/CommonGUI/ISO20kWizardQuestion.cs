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
    public class ISO20kWizardQuestion : WizardQuestion
    {

        string xmlFilenameBase;

        WizardStyle currentStyle = WizardStyle.CHECKBOX;

	    int _round = 1;
        ArrayList [] factorsByRound;

        public override int Round
        {
            get { return _round; }
            set
            {
                _round = value;
                setRound();
            }
        }

        public override void SetFocus()
        {
            ((TextBox)score_text[Round - 1]).Focus();
        }

	    bool _collapsed = false;
        public override bool Collapsed
        {
            get { return _collapsed; }
            set
            {
                Height = (value) ? 29 : full_height;
                _collapsed = value;
                setCollapsedArrow();
            }
        }

	    Hashtable file_scores = new Hashtable();
	    Hashtable factor_ticks = new Hashtable();

	    NetworkProgressionGameFile gameFile;
	    string tag_name;
	    Panel p = new Panel();
	    int full_height;
	    int current_round;
	    ScoreCardWizardBase parent;
	    Font f = SkinningDefs.TheInstance.GetFont(10);
	    Font f8 = SkinningDefs.TheInstance.GetFont(8);
	    ArrayList factors;
	    ArrayList factors_check = new ArrayList();
	    ArrayList score_text = new ArrayList();
	    TextBox r1_text;
	    TextBox r2_text;
	    TextBox r3_text;
	    TextBox r4_text;

	    ArrayList r1_factors = new ArrayList();
	    ArrayList r2_factors = new ArrayList();
	    ArrayList r3_factors = new ArrayList();
	    ArrayList r4_factors = new ArrayList();
	    ArrayList r5_factors = new ArrayList();
        protected double questionScore = 0;
	    CheckBox state = new CheckBox();

	    string _question;
	    bool _enabled;

	    bool done = false;
	    bool auto_translate = true;

        enum BoxChoice { Yes, No, InProgress, NotApplicable }


        public ISO20kWizardQuestion(ScoreCardWizardBase _p, string question, ArrayList _factors,
			ArrayList round_scores, ArrayList round_ticks, string tag_name, int round,
			NetworkProgressionGameFile gameFile, bool enabled): this (_p, question, "", 
			_factors,	round_scores, round_ticks, tag_name, round, gameFile, enabled, 
			WizardStyle.CHECKBOX, Color.White)
		{
		}


        public ISO20kWizardQuestion(ScoreCardWizardBase _p, string question, string desc,
            ArrayList _factors, ArrayList round_scores, ArrayList round_ticks,
            string tag_name, int round, NetworkProgressionGameFile gameFile, bool enabled,
            WizardStyle newStyle, Color backColour)
        {
           
            _question = question;
            _enabled = enabled;
            currentStyle = newStyle;

            xmlFilenameBase = "";

            BackColor = backColour;

            allBoxes = new ArrayList();
            this.tag_name = tag_name;
            Width = 970;
            parent = _p;
            factors = _factors;
            current_round = round;
            this.gameFile = gameFile;

            if (gameFile.CurrentPhase == GameFile.GamePhase.TRANSITION)
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
            f2 = ConstantSizeFont.NewFont(displayFontName, 10);

            Label name = new Label();
            name.Text = rawtext_question;
            name.Location = new Point(40, 0);
            name.Size = new Size(560, 20);
            name.Font = f2;
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

			r1_text = createTextBox(638, 0, 1);
            r1_text.Name = "r1";
			r2_text = createTextBox(708, 0, 2);
            r2_text.Name = "r2";
			r3_text = createTextBox(778, 0, 3);
            r3_text.Name = "r3";
			r4_text = createTextBox(848, 0, 4);
            r4_text.Name = "r4";
          
            score_text.Add(r1_text);
            score_text.Add(r2_text);
            score_text.Add(r3_text);
            score_text.Add(r4_text);

            AddLabels();
            
            colourBoxes(round_scores);

            rounds = Math.Min(4, SkinningDefs.TheInstance.GetIntData("roundcount", 5));

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


            // Set Initial Scores
            // Fix for Case 5494:   MS: crash viewing eval scores with too many sections.
            // Bad XML can have more than 5 rounds! So limit to 5 rounds max.
            int num_rounds = round_scores.Count;
            if (num_rounds > 5) num_rounds = 5;
            int labels = num_rounds - 1;
            //
            for (int x = 0; x < labels; x++)
            {
                TextBox tb = score_text[x] as TextBox;
                tb.Text = (string)round_scores[x];
            }

            int i = 1;
            if (currentStyle != WizardStyle.DESC_ONLY)
            {
                if (gameFile.Game_Eval_Type != em_GameEvalType.ISO_20K)
                {
                    foreach (Aspect aspect in factors)
                    {
                        Label questionLabel = new Label();
                        questionLabel.Text = aspect.question;
                        questionLabel.Size = new Size(600, 30);
                        questionLabel.Location = new Point(50, i * 30);

                        factorsByRound = new ArrayList[] { r1_factors, r2_factors, r3_factors, r4_factors, r5_factors };

                        for (int j = 0; j < SkinningDefs.TheInstance.GetIntData("roundcount", 5); j++)
                        {
                            Control r_fac = null;
                            if (currentStyle == WizardStyle.CHECKBOX)
                            {
                                r_fac = createCheckBox(628 + (70 * j), i * 30 + 2);
                            }
                            if (r_fac != null)
                            {
                                Controls.Add(r_fac);
                                factorsByRound[j].Add(r_fac);
                            }
                        }

                        Controls.Add(questionLabel);
                        i++;
                    }
                }
                else
                {
                    rowToRoundToCheckBoxes = new List<Dictionary<int, List<Control>>> ();

                    foreach (Aspect aspect in factors)
                    {
                        var roundToCheckBoxes = new Dictionary<int, List<Control>> ();
                        rowToRoundToCheckBoxes.Add(roundToCheckBoxes);

                        Label questionLabel = new Label();
                        questionLabel.Text = aspect.question;
                        questionLabel.Size = new Size(575, 30);
                        questionLabel.Location = new Point(50, i * 30 + 12);

                        factorsByRound = new ArrayList[] { r1_factors, r2_factors, r3_factors, r4_factors, r5_factors };

                        for (int j = 0; j < SkinningDefs.TheInstance.GetIntData("roundcount", 5) - 1; j++)
                        {
                            var checkBoxes = new List<Control> ();
                            roundToCheckBoxes.Add(1 + j, checkBoxes);
                            if (currentStyle == WizardStyle.CHECKBOX)
                            {
                                ArrayList boxList = createCheckBoxes(628 + (70 * j), i * 30 + 14);
                                foreach (CheckBox box in boxList)
                                {
                                    Controls.Add(box);
                                    factorsByRound[j].Add(box);

                                    checkBoxes.Add(box);
                                }
                            }
                        }

                        Controls.Add(questionLabel);
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
                        {
                            if (currentStyle == WizardStyle.CHECKBOX)
                            {
                                if (tmp == "Yes")
                                {
                                    if (4*x < ((ArrayList)factors_check[y]).Count)
                                    {
                                        int xx = 4 * x;
                                        ((CheckBox)((ArrayList)factors_check[y])[xx]).CheckState = CheckState.Checked;
                                    }
                                }
                                else if (tmp == "NotApplicable")
                                {
                                    if (4 * x + 3 < ((ArrayList)factors_check[y]).Count)
                                    {
                                        int xx = 4 * x + 3;
                                        ((CheckBox)((ArrayList)factors_check[y])[xx]).CheckState = CheckState.Checked;
                                    }
                                }
                                else if (tmp == "No")
                                {
                                    if (4 * x + 1 < ((ArrayList)factors_check[y]).Count)
                                    {
                                        int xx = 4 * x + 1;
                                        ((CheckBox)((ArrayList)factors_check[y])[xx]).CheckState = CheckState.Checked;
                                    }
                                }
                                else if (tmp == "InProgress")
                                {
                                    if (4 * x + 2 < ((ArrayList)factors_check[y]).Count)
                                    {
                                        int xx = 4 * x + 2;
                                        ((CheckBox)((ArrayList)factors_check[y])[xx]).CheckState = CheckState.Checked;
                                    }
                                }
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
            state.Size = new Size(15, 15);
            state.Location = new Point(10, 4);
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

            full_height = i * 32;
            Collapsed = true;
            done = true;
          
            if (gameFile.LastPhaseNumberPlayed == -1)
            {
                foreach (Control c in Controls)
                {
                       c.Enabled = false;
                }
            }
        }

        protected override void state_CheckStateChanged(object sender, EventArgs e)
        {
            if (state.Checked)
            {
                //enable all controls
                foreach (Control c in Controls)
                {
                    //if textbox, only enable if we have got to that round
                    if (c.GetType().ToString() == "CommonGUI.TabTextBox")
                    {
                        c.Enabled = false; 
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

        protected override void SaveState()
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
                root = (XmlNode)xml.CreateElement("ignore_list");
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
                        foreach (XmlAttribute att in node.Attributes)
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
                XmlNode cost = (XmlNode)xml.CreateElement("ignore");
                ((XmlElement)cost).SetAttribute("question", _question);
                root.AppendChild(cost);
            }

            xml.SaveToURL(null, gameFile.Dir + "\\global\\Eval_States.xml");
        }

        Dictionary<int, List<Control>> roundToCheckBoxColumnHeaders;
        List<Dictionary<int, List<Control>>> rowToRoundToCheckBoxes;

	    void AddLabels()
        {
            if (!Collapsed)
            {
                roundToCheckBoxColumnHeaders = new Dictionary<int, List<Control>> ();

                foreach (TextBox textBox in score_text)
                {
                    var roundList = new List<Control> ();
                    roundToCheckBoxColumnHeaders.Add(roundToCheckBoxColumnHeaders.Keys.Count + 1, roundList);

                    var yes_text = new Label { TextAlign = ContentAlignment.TopCenter, Size = new Size (24, 12) };
                    var no_text = new Label { TextAlign = ContentAlignment.TopCenter, Size = new Size (24, 12) };
                    var IP_text = new Label { TextAlign = ContentAlignment.TopCenter, Size = new Size (24, 12) };
                    var NA_text = new Label { TextAlign = ContentAlignment.TopCenter, Size = new Size(24, 12) };

                    yes_text.Text = "Y";
                    yes_text.Font = f8;
                    Controls.Add(yes_text);

                    no_text.Text = "N";
                    no_text.Font = f8;
                    Controls.Add(no_text);

                    IP_text.Text = "IP";
                    IP_text.Font = f8;
                    Controls.Add(IP_text);

                    NA_text.Text = "NA";
                    NA_text.Font = f8;
                    Controls.Add(NA_text);

                    roundList.Add(yes_text);
                    roundList.Add(no_text);
                    roundList.Add(IP_text);
                    roundList.Add(NA_text);
                }
            }
        }

        public new void name_Click(object sender, EventArgs e)
        {
            Collapsed = !Collapsed;
            parent.DoLayout();
            parent.ScrollControlIntoView(this);
        }

        protected new void setScore(TextBox display, int score)
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
                                factor_data.InnerText = whichBoxChecked(i, factorsNode);
                            }

                            i++;
                        }
                    }
                }
            }

            xml.SaveToURL(null, NetworkFile);
        }

	    string whichBoxChecked(int i, XmlNode factorsNode)
        {
            ArrayList relevantBoxes = allBoxes.GetRange(i * 16 + (4 * (Round - 1)), 4);

            foreach (CheckBox box in relevantBoxes)
            {
                if (box.Checked)
                {
                    return BoxNumberToEnum(Convert.ToInt16(box.Name)).ToString();
                }
            }

            return "False";
        }

	    BoxChoice BoxNumberToEnum(int boxNumber)
        {
            int box = boxNumber % 4;
            switch (box)
            {
                case 0:
                    return BoxChoice.Yes;
                case 1:
                    return BoxChoice.No;
                case 2:
                    return BoxChoice.InProgress;
                case 3:
                    return BoxChoice.NotApplicable;
                default:
                    return BoxChoice.NotApplicable;
            }
        }

        public ArrayList createCheckBoxes(int x, int y)
        {
            return createCheckBoxes(x, y, 12);
        }

        ArrayList allBoxes;

        public ArrayList createCheckBoxes(int x, int y, int size)
        {
            CheckBox newBox;
            ArrayList boxList = new ArrayList();
            for (int boxes = 0; boxes < 4; boxes++) // < 4 as we are creating 4 checkboxes (Yes, No, IP, NA)
            {
                newBox = new CheckBox();
                newBox.Enabled = false;
                newBox.Size = new Size(size, size);
                newBox.Location = new Point(x + ((size + 3) * boxes), y);
                newBox.Name = allBoxes.Count.ToString();
                boxList.Add(newBox);
                allBoxes.Add(newBox);
                
                switch (boxes)
                {
                    case 0:
                        newBox.CheckStateChanged += CheckStateChangedYesBoxes;
                        break;
                    case 1:
                        newBox.CheckStateChanged += CheckStateChangedNoBoxes;
                        break;
                    case 2:
                        newBox.CheckStateChanged += CheckStateChangedInProgressBoxes;
                        break;
                    case 3:
                        newBox.CheckStateChanged += CheckStateChangedNABoxes;
                        break;
                }
            }

            return boxList;
        }

        protected override void p_Click(object sender, EventArgs e)
        {
            Collapsed = !Collapsed;
            parent.DoLayout();
            parent.ScrollControlIntoView(this);
        }

        protected override void setCollapsedArrow()
        {
            string img_path = (Collapsed) ? "arrow_collapsed.png" : "arrow_expanded.png";
            Image bg = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\wizard\\" + img_path);
            p.BackgroundImage = bg;
        }

	    bool lockBoxChanging = false;

	    void UncheckOtherBoxes(object sender)
        {
            lockBoxChanging = true;
            CheckBox sendingBox = (CheckBox)sender;
            string a = sendingBox.Name;
            int BoxNumber = CONVERT.ParseInt(a);
            int[] boxesToUncheck;
            int boxtype = BoxNumber % 4;
            switch (boxtype)
            {
                case 0:
                    boxesToUncheck = new int[3]{BoxNumber +1, BoxNumber + 2, BoxNumber + 3};
                    break;
                case 1:
                    boxesToUncheck = new int[3] { BoxNumber - 1, BoxNumber + 1, BoxNumber + 2 };
                    break;
                case 2:
                    boxesToUncheck = new int[3] { BoxNumber -2, BoxNumber -1, BoxNumber + 1 };
                    break;
                case 3:
                    boxesToUncheck = new int[3] { BoxNumber -3, BoxNumber - 2, BoxNumber -1 };
                    break;
                default:
                    boxesToUncheck = new int[3];
                    break;
            }

            foreach (int box in boxesToUncheck)
            {
                Control[] toChange = Controls.Find(box.ToString(), true);
                if (toChange.Length == 1)
                {
                    CheckBox temp = (CheckBox)toChange[0];
                    temp.Checked = false;
                }
            }
            lockBoxChanging = false;
        }

	    void CheckStateChangedYesBoxes(object sender, EventArgs e)
        {

            if (!lockBoxChanging)
            {
                UncheckOtherBoxes(sender);
            }
            CheckBox sendingBox = (CheckBox)sender;
            string a = sendingBox.Name;
            if (done)
            {
                ArrayList tmp = (ArrayList)factors_check[_round - 1];
                ArrayList factorsToCheck = new ArrayList();
                for (int check = 0; check < tmp.Count; check += 4)
                {
                    factorsToCheck.Add(tmp[check]);

                }
                for (int check = 3; check < tmp.Count; check += 4)
                {
                    factorsToCheck.Add(tmp[check]);

                }

                int i = 0;
                double score = 0;
                foreach (CheckBox box in factorsToCheck)
                {
                    if (box.CheckState == CheckState.Checked)
                    {
                        score += 1;
                    }

                    i++;
                }

                ((TextBox)score_text[(Round - 1)]).Text = CONVERT.ToStr(Math.Round(score));
                colourBoxes(score);

                setScore(((TextBox)score_text[(Round - 1)]), (int)Math.Round(score));
            }
        }

	    void CheckStateChangedNoBoxes(object sender, EventArgs e)
        {
            if (!lockBoxChanging)
            {
                UncheckOtherBoxes(sender);
            }
            if (done)
            {
                ArrayList tmp = (ArrayList)factors_check[_round - 1];
                ArrayList factorsToCheck = new ArrayList();
                for (int check = 0; check < tmp.Count; check += 4)
                {
                    factorsToCheck.Add(tmp[check]);

                }
                for (int check = 3; check < tmp.Count; check += 4)
                {
                    factorsToCheck.Add(tmp[check]);

                }

                int i = 0;
                double score = 0;
                foreach (CheckBox box in factorsToCheck)
                {
                    if (box.CheckState == CheckState.Checked)
                    {
                        score += 1; 
                    }

                    i++;
                }
                colourBoxes(score);

                setScore(((TextBox)score_text[(Round - 1)]), (int)Math.Round(score));
            }
        }

	    void CheckStateChangedInProgressBoxes(object sender, EventArgs e)
        {
            if (!lockBoxChanging)
            {
                UncheckOtherBoxes(sender);
            }
            CheckBox sendingBox = (CheckBox)sender;
            string a = sendingBox.Name;
            if (done)
            {
                ArrayList tmp = (ArrayList)factors_check[_round - 1];
                ArrayList factorsToCheck = new ArrayList();
                for (int check = 0; check < tmp.Count; check += 4)
                {
                    factorsToCheck.Add(tmp[check]);

                }
                for (int check = 3; check < tmp.Count; check += 4)
                {
                    factorsToCheck.Add(tmp[check]);

                }

                int i = 0;
                double score = 0;
                foreach (CheckBox box in factorsToCheck)
                {
                    if (box.CheckState == CheckState.Checked)
                    {
                        score += 1;
                    }

                    i++;
                }

                colourBoxes(score);
             

                setScore(((TextBox)score_text[(Round - 1)]), (int)Math.Round(score));
            }
        }

	    void CheckStateChangedNABoxes(object sender, EventArgs e)
        {
            if (!lockBoxChanging)
            {
                UncheckOtherBoxes(sender);
            }
            CheckBox sendingBox = (CheckBox)sender;
            string a = sendingBox.Name;
            if (done)
            {
                ArrayList tmp = (ArrayList)factors_check[_round - 1];
                ArrayList factorsToCheck = new ArrayList();
                for (int check = 0; check < tmp.Count; check += 4)
                {
                    factorsToCheck.Add(tmp[check]);

                }
                for (int check = 3; check < tmp.Count; check += 4)
                {
                    factorsToCheck.Add(tmp[check]);

                }


                int i = 0;
                double score = 0;
                foreach (CheckBox box in factorsToCheck)
                {
                    if (box.CheckState == CheckState.Checked)
                    {
                        score += 1;
                    }

                    i++;
                }

                ((TextBox)score_text[(Round - 1)]).Text = CONVERT.ToStr(Math.Round(score));

                colourBoxes(score);

                setScore(((TextBox)score_text[(Round - 1)]), (int)Math.Round(score));
            }
        }

	    void colourBoxes(double score)
        {

            if (score == 5)
            {
                ((TextBox)score_text[(Round - 1)]).BackColor = Color.LightGreen;
            }
            else if (score > 2)
            {
                ((TextBox)score_text[(Round - 1)]).BackColor = Color.Yellow;
            }
            else
            {
                ((TextBox)score_text[(Round - 1)]).BackColor = Color.Red;
            }
        }

	    void colourBoxes(ArrayList scores)
        {
            int countLimit = Math.Min(scores.Count, score_text.Count);
            for(int i = 0; i<countLimit; i++)
            {
                int tmp = Convert.ToInt32(scores[i]);

                if (tmp == 5)
                {
                    ((TextBox)score_text[i]).BackColor = Color.LightGreen;
                    ((TextBox)score_text[i]).Text = tmp.ToString();
                }
                else if (tmp >2)
                {
                    ((TextBox)score_text[i]).BackColor = Color.Yellow;
                    ((TextBox)score_text[i]).Text = tmp.ToString();
                }
                else
                {
                    ((TextBox)score_text[i]).BackColor = Color.Red;
                    ((TextBox)score_text[i]).Text = tmp.ToString();
                }
            }

            
        }

        protected override void setRound()
        {
            for (int i = 0; i < 5; i++)
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

        protected override void DoSize ()
        {
            int leftColumnWidth = 628;
            var textBoxes = new List<Control> (new [] { r1_text, r2_text, r3_text, r4_text });

            for (int round = 1; round <= rounds; round++)
            {
                textBoxes[round - 1].Location = new Point(leftColumnWidth + (((Width - 10 - leftColumnWidth) * (round - 1)) / rounds), textBoxes[round - 1].Location.Y);

                for (int i = 0; i < 4; i++)
                {
                    int spread = 80;
                    int centreX = textBoxes[round - 1].Left + (textBoxes[round - 1].Width / 2) - (spread / 2) + (spread * i / 3);

                    var columnHeader = roundToCheckBoxColumnHeaders[round][i];

                    columnHeader.Location = new Point (centreX - (columnHeader.Width / 2), textBoxes[round - 1].Bottom + 6);

                    foreach (var roundToCheckBoxes in rowToRoundToCheckBoxes)
                    {
                        var checkBox = roundToCheckBoxes[round][i];
                        checkBox.Location = new Point (centreX - (checkBox.Width / 2), checkBox.Top);
                    }
                }
            }
        }
    }
}