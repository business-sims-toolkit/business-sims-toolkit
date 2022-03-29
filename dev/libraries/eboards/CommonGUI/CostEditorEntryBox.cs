using System;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using GameManagement;
using LibCore;

namespace CommonGUI
{
	/// <summary>
	/// Summary description for CostEditorEntryBox.
	/// </summary>
	public class CostEditorEntryBox : EntryBox
	{
		NetworkProgressionGameFile gameFile;
		int round;
		string costsxml;
		int index;
		string nodeName;
		string attributeName;
		bool allRoundValuesPresentInNetwork;

		public CostEditorEntryBox(NetworkProgressionGameFile gameFile, int round, int index, string text, string modelName)
		{
			this.gameFile = gameFile;
			this.round = round;
			costsxml = gameFile.GetGlobalFile($"costs_r{round}.xml");
			this.index = index;
			MaxLength = 9;
			DigitsOnly = true;
			KeyUp += CostEditorEntryBox_KeyUp;

			if (modelName != null)
			{
				allRoundValuesPresentInNetwork = modelName.Contains("{round}");

				modelName = modelName.Replace("{round}", CONVERT.ToStr(round));
				nodeName = modelName.Substring(0, modelName.IndexOf("."));
				attributeName = modelName.Substring(modelName.IndexOf(".") + 1);
				Text = gameFile.GetNetworkModel(round).GetNamedNode(nodeName).GetAttribute(attributeName);
			}
			else if (text != null)
			{
				Text = text;
			}
		}

		void CostEditorEntryBox_KeyUp(object sender, KeyEventArgs e)
		{
			var xml = BasicXmlDocument.CreateFromFile(costsxml);

			XmlElement costnode = (XmlElement) xml.DocumentElement.ChildNodes[index];
			string val = Text;
			if (val == string.Empty) val = "0";

			if (nodeName != null)
			{
				var rounds = new [] { round };
				if (allRoundValuesPresentInNetwork)
				{
					rounds = Enumerable.Range(1, gameFile.GetTotalRounds()).ToArray();
				}

				foreach (var round in rounds)
				{
					var model = gameFile.GetNetworkModel(round);
					model.GetNamedNode(nodeName).SetAttribute(attributeName, val);
					model.SaveToURL("", gameFile.GetNetworkFile(round));
				}
			}

			var attribute = costnode.Attributes["cost"];
			if (attribute == null)
			{
				attribute = costnode.AppendAttribute("cost", val);
			}
			attribute.Value = val;

			xml.SaveToURL(null, costsxml);
		}
	}
}
