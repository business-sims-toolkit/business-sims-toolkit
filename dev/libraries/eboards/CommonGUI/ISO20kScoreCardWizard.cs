using System.Collections;
using GameManagement;

namespace CommonGUI
{

    public class ISO20kScoreCardWizard : ScoreCardWizard
    {

        public ISO20kScoreCardWizard(NetworkProgressionGameFile gameFile) : base(gameFile)
		{
        }

        protected override void readScores()
        {
            int round = gameFile.CurrentRound;
            int labels = rounds - 1;
            if (gameFile.CurrentPhase == GameFile.GamePhase.TRANSITION)
                round -= 1;
            ArrayList roundScores = new ArrayList();

            for (int i = 1; i <= round; i++)
            {
                roundScores = new ArrayList();   
                LibCore.BasicXmlDocument xml = LibCore.BasicXmlDocument.CreateFromFile(gameFile.GetMaturityRoundFile(i));

                foreach (System.Xml.XmlNode section in xml.DocumentElement.ChildNodes)
                {
                    System.Xml.XmlNode section_name = section.SelectSingleNode("section_name");
                    System.Xml.XmlNode aspects = section.SelectSingleNode("aspects");

                    foreach (System.Xml.XmlNode aspect in aspects)
                    {
                        System.Xml.XmlNode aspect_name = aspect.SelectSingleNode("aspect_name");

                        string dest_tag_name = aspect.SelectSingleNode("dest_tag_name").InnerText;

                        string name = dest_tag_name;

                        // creates a 5 * #factors array list
                        if (factor_ticks[name] == null)
                        {
                            ArrayList tmp = new ArrayList();

                            for (int j = 0; j < labels; j++)
                            {
                                tmp.Add(new ArrayList());
                            }
                            factor_ticks[name] = tmp;
                        }

                        System.Xml.XmlNode dest_tag_data = aspect.SelectSingleNode("dest_tag_data");

                        if (file_scores[name] != null)
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
                        System.Xml.XmlNode factors = aspect.SelectSingleNode("factors");

                        foreach (System.Xml.XmlNode factor in factors)
                        {
                            System.Xml.XmlNode factor_data = factor.SelectSingleNode("factor_data");

                            if (((ArrayList)factor_ticks[name]).Count >= i)
                            {
                                ((ArrayList)((ArrayList)factor_ticks[name])[(i - 1)]).Add(
                                    factor_data.InnerText);
                            }
                        }
                    }
                }
            }

            foreach (DictionaryEntry value in file_scores)
            {
                roundScores.Add(value);
            }

            scores.Add(roundScores);
        }
    }
}