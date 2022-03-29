using System.Collections.Generic;
using System.IO;
using System.Xml;
using Network;
using GameManagement;
using LibCore;
using Logging;

namespace GenericExtractIncidents
{
	public class IncidentExtractor
	{
		BasicIncidentLogReader reader;

		FileStream fileStream;
		TextWriter textWriter;

		NetworkProgressionGameFile gameFile;

		public IncidentExtractor (NetworkProgressionGameFile gameFile)
		{
			this.gameFile = gameFile;
		}

		public void ExtractRoundIncidents (Network.NodeTree model, int round, string outfile)
		{
			ExtractRoundIncidents(model, gameFile.GetRoundFile(round, "NetworkIncidents.log", GameFile.GamePhase.OPERATIONS), outfile);
		}

		public void ExtractRoundIncidents (Network.NodeTree model, string logFile, string outfile)
		{
			fileStream = new FileStream (outfile, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
			textWriter = new StreamWriter (fileStream);

			textWriter.WriteLine("<incidents>");

			//This is used in AOSE where we only have redirected incidents in R2 and R3
			//so we need to ignore the normal incidents (which are used as secondary consequences of the redirected ones)
			//The redirected ones cause the hidden normal ones as consequence (and the cant be extract and applied)
			bool ExtractNormalIncidentRequests = true;

			Node extract_normal_incidents_node = model.GetNamedNode("extract_normal_incidents");
			if (extract_normal_incidents_node != null)
			{
				ExtractNormalIncidentRequests = extract_normal_incidents_node.GetBooleanAttribute("extract", true);
			}

			reader = new BasicIncidentLogReader (logFile);

			if (ExtractNormalIncidentRequests)
			{
				reader.WatchCreatedNodes("enteredIncidents", this.biLogReader_EnteredIncidentsLineFound);
			}
			reader.WatchCreatedNodes("FixItQueue", this.biLogReader_FixItQueueEventFound);
			reader.WatchCreatedNodes("ProjectsIncomingRequests", this.biLogReader_GeneralLineFound);
			reader.WatchCreatedNodes("MirrorCommandQueue", this.biLogReader_GeneralLineFound);
			reader.WatchCreatedNodes("AppUpgradeQueue", this.biLogReader_GeneralLineFound);
			reader.WatchCreatedNodes("MachineUpgradeQueue", this.biLogReader_GeneralLineFound);
			reader.WatchCreatedNodes("TaskManager", this.biLogReader_GeneralLineFound);
			reader.WatchCreatedNodes("ZoneActivationQueue", this.biLogReader_GeneralLineFound);

			// Watch every attribute change on every node, and every creation.
			reader.WatchApplyAttributes("", this.biLogReader_LogLineFound);
			reader.WatchCreatedNodes("", this.biLogReader_CreatedNodeEventFound);

			// And watch everything that's tagged as being watchable.
			WatchAllTaggedNodes(reader, model.Root, false);

			reader.Run();

			textWriter.WriteLine("</incidents>");

			textWriter.Close();
			fileStream.Close();
			textWriter = null;
			fileStream = null;
		}

		void WatchAllTaggedNodes (BasicIncidentLogReader reader, Network.Node node, bool watchEvenIfNotTagged)
		{
			string name = node.GetAttribute("name");
			bool tagged = node.GetBooleanAttribute("extract_incidents", watchEvenIfNotTagged);

			if (tagged && (name != ""))
			{
				reader.WatchApplyAttributes(name, this.biLogReader_GeneralLineFound);
				reader.WatchCreatedNodes(name, this.biLogReader_GeneralLineFound);
				reader.WatchMovedNodes(name, this.biLogReader_GeneralLineFound);
				reader.WatchDeletedNodes(name, this.biLogReader_GeneralLineFound);
			}

			foreach (Network.Node child in node.getChildren())
			{
				WatchAllTaggedNodes(reader, child, tagged);
			}
		}

		string StripUnwantedAttributes (string input)
		{
			var xml = BasicXmlDocument.Create(input);
			StripUnwantedAttributes(xml.DocumentElement);
			
			return xml.OuterXml;
		}

		string [] GetUnwantedAttributeNames ()
		{
			return new string [0];
		}

		void StripUnwantedAttributes (XmlElement xml)
		{
			foreach (var attribute in GetUnwantedAttributeNames())
			{
				if (xml.Attributes[attribute] != null)
				{
					xml.RemoveAttribute(attribute);
				}
			}

			foreach (var child in xml.ChildNodes)
			{
				var element = child as XmlElement;
				if (element != null)
				{
					StripUnwantedAttributes(element);
				}
			}
		}

		void biLogReader_FixItQueueEventFound (object sender, string key, string line, double time)
		{
			// There was bad XML in an old version so fix...
			line = line.Replace("fix by consultancy", "fix_by_consultancy");
			textWriter.WriteLine(StripUnwantedAttributes(line));
		}

		void biLogReader_GeneralLineFound (object sender, string key, string line, double time)
		{
			textWriter.WriteLine(StripUnwantedAttributes(line));
		}

		void biLogReader_EnteredIncidentsLineFound (object sender, string key, string line, double time)
		{
			if (LibCore.CONVERT.ParseBool(BasicIncidentLogReader.ExtractValue(line, "extract_incidents"), true))
			{
				textWriter.WriteLine(StripUnwantedAttributes(line));
			}
		}

		void biLogReader_CreatedNodeEventFound (object sender, string key, string line, double time)
		{
			biLogReader_PossiblyInterestingLineFound(sender, key, StripUnwantedAttributes(line), time);
		}

		int id = 0;

		void biLogReader_PossiblyInterestingLineFound (object sender, string key, string line, double time)
		{
			string extract = BasicIncidentLogReader.ExtractValue(line, "extract_incidents");
			if (LibCore.CONVERT.ParseBool(extract, false))
			{
				if (line.Contains("StartService"))
				{
					var xml = BasicXmlDocument.Create(line);
					var element = (XmlElement) (((XmlElement) xml.DocumentElement.ChildNodes[0]).ChildNodes[0]);
					if (string.IsNullOrEmpty(element.GetAttribute("unique_id")))
					{
						element.AppendAttribute("unique_id", id);
						id++;
						line = xml.OuterXml;
					}
				}

				textWriter.WriteLine(line);

				string name = BasicIncidentLogReader.ExtractValue(line, "i_name");

				if (name != "")
				{
					BasicIncidentLogReader reader = sender as BasicIncidentLogReader;
					reader.WatchApplyAttributes(name, biLogReader_LogLineFound);
					reader.WatchCreatedNodes(name, biLogReader_GeneralLineFound);
					reader.WatchMovedNodes(name, biLogReader_GeneralLineFound);
					reader.WatchDeletedNodes(name, biLogReader_GeneralLineFound);
				}
			}
		}

		void biLogReader_LogLineFound (object sender, string key, string line, double time)
		{
			// Log SLA changes.
			string slalimit;
			if (BasicIncidentLogReader.ExtractValue(line, "slalimit", out slalimit))
			{
				string name;
				if (BasicIncidentLogReader.ExtractValue(line, "i_name", out name))
				{
					string do_after;
					if (BasicIncidentLogReader.ExtractValue(line, "i_doAfterSecs", out do_after))
					{
						// Order not correct so delay for one second.
						int i = int.Parse(do_after) + 1;
						textWriter.WriteLine("<i id=\"AtStart\"><apply i_name=\"" + name + "\" i_doAfterSecs=\"" + i.ToString() + "\" slalimit=\"" + slalimit + "\"/></i>");
					}
				}
			}
			// Anything else might still be of note.
			else
			{
				biLogReader_PossiblyInterestingLineFound(sender, key, line, time);
			}
		}

		void biLogReader_nodeMovedEventFound (object sender, string key, string line, double time)
		{
			textWriter.WriteLine(StripUnwantedAttributes(line));
		}
	}
}