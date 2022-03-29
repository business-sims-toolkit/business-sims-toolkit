using System.Collections;
using System.IO;
using System.Xml;

using LibCore;
using Network;

using GameManagement;

namespace BusinessServiceRules
{
	public class AssessmentAction
	{
		public string name;

		public AssessmentAction (string name)
		{
			this.name = name;
		}
	}

	public class Assessment
	{
		public string name;
		public int cost;
		public Hashtable actionListsByRound;

		public Assessment (string name, int cost)
		{
			this.name = name;
			this.cost = cost;

			actionListsByRound = new Hashtable ();
		}

		public ArrayList ActionListForRound (int round)
		{
			if (actionListsByRound.ContainsKey(round))
			{
				return (ArrayList) actionListsByRound[round];
			}

			return new ArrayList ();
		}
	}

	/// <summary>
	/// The Assessments biller simply levies a charge at the start of each ops phase,
	/// for every assessment that has been requested during transition.
	/// </summary>
	public class AssessmentsBiller
	{
		public ArrayList assessments;
		GameFile gameFile;

		public AssessmentsBiller (GameFile gameFile)
		{
			this.gameFile = gameFile;

			assessments = new ArrayList ();

			string xmlData;
			using (StreamReader reader = File.OpenText(AppInfo.TheInstance.Location + "data/assessments.xml"))
			{
				xmlData = reader.ReadToEnd();
			}
			BasicXmlDocument xml = BasicXmlDocument.Create(xmlData);
			XmlNode root = xml.DocumentElement;

			foreach (XmlNode assessmentNode in root.ChildNodes)
			{
				Assessment assessment = new Assessment (assessmentNode.Attributes["name"].Value, CONVERT.ParseInt(assessmentNode.Attributes["cost"].Value));
				assessments.Add(assessment);

				foreach (XmlNode roundNode in assessmentNode.ChildNodes)
				{
					int round = CONVERT.ParseInt(roundNode.Attributes["number"].Value);

					foreach (XmlNode actionNode in roundNode.ChildNodes)
					{
						if (! assessment.actionListsByRound.ContainsKey(round))
						{
							assessment.actionListsByRound.Add(round, new ArrayList ());
						}
						(assessment.actionListsByRound[round] as ArrayList).Add(new AssessmentAction (actionNode.Attributes["name"].Value));
					}
				}
			}
		}

		string GetSelectionFileName (int round, bool loadingInGame)
		{
			if (! loadingInGame)
			{
				round++;
			}

			string filename = gameFile.GetRoundFile(round, "assessment_selection.xml", GameFile.GamePhase.TRANSITION);

			// If we haven't started the transition phase yet, then we'll save the file under a different name for the prior ops phase.
			if (! Directory.Exists(Path.GetDirectoryName(filename)))
			{
				filename = gameFile.GetRoundFile(round - 1, "assessment_selection_for_next_round.xml", GameFile.GamePhase.OPERATIONS);
			}

			return filename;
		}

		public void OutputSelectionFile (int round, ArrayList selection)
		{
			BasicXmlDocument xml = BasicXmlDocument.Create();

			XmlNode root = xml.CreateElement("assessments");
			xml.AppendChild(root);

			foreach (string name in selection)
			{
				XmlNode child = xml.CreateElement("assessment");
				XmlAttribute attribute = xml.CreateAttribute("name");
				attribute.Value = name;
				child.Attributes.Append(attribute);
				root.AppendChild(child);
			}

			string filename = GetSelectionFileName(round, false);
			xml.Save(filename);
		}

		public ArrayList LoadSelectionFile (int round, bool inGame)
		{
			ArrayList selection = new ArrayList ();

			string filename = GetSelectionFileName(round, inGame);

			if (File.Exists(filename))
			{
				XmlDocument xml = new XmlDocument ();
				xml.Load(filename);
				XmlNode root = xml.DocumentElement;
				foreach (XmlNode child in root.ChildNodes)
				{
					selection.Add(child.Attributes["name"].Value);
				}
			}

			return selection;
		}

		public void PerformBilling (int round, NodeTree network)
		{
			ArrayList selectedAssessments = new ArrayList ();
			ArrayList selectedAssessmentNames = LoadSelectionFile(round, true);
			foreach (string name in selectedAssessmentNames)
			{
				foreach (Assessment assessment in assessments)
				{
					if (assessment.name == name)
					{
						selectedAssessments.Add(assessment);
						break;
					}
				}
			}

			Node costedEventNode = network.GetNamedNode("CostedEvents");
			foreach (Assessment assessment in selectedAssessments)
			{
				ArrayList attrs = new ArrayList ();
				attrs.Add(new AttributeValuePair ("type", "assessment_fee"));
				attrs.Add(new AttributeValuePair ("desc", "Fee for " + assessment.name));
				attrs.Add(new AttributeValuePair ("ref", "Assessments Biller"));
				attrs.Add(new AttributeValuePair ("fee", assessment.cost));
				new Node (costedEventNode, "CostedEvents", "", attrs);
			}
		}
	}
}