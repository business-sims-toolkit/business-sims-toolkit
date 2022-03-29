using System;
using System.Xml;
using System.Collections;

using System.IO;

using GameManagement;
using LibCore;

namespace ReportBuilder
{
	/// <summary>
	/// Summary description for OpsMaturityReport.
	/// </summary>
	public class OpsMaturityReport
	{
		class NumberedSection : IComparable
		{
			public string name;
			public int order;

			public NumberedSection (string name)
				: this(name, -1)
			{
			}

			public NumberedSection (string name, int order)
			{
				this.name = name;
				this.order = order;
			}

			public int CompareTo (object obj)
			{
				NumberedSection that = obj as NumberedSection;

				if (this.order != -1)
				{
					if (that.order == -1)
					{
						return +1;
					}
					else
					{
						return this.order - that.order;
					}
				}
				else
				{
					if (that.order == -1)
					{
						return this.name.CompareTo(that.name);
					}
					else
					{
						return -1;
					}
				}
			}
		}

		int NumBands;		

		public OpsMaturityReport()
		{
			NumBands = 5;
		}

		public Hashtable GetSectionOrder (string xml, ArrayList ignore, ArrayList ignore_tags)
		{
			Hashtable outer_sections = new Hashtable();
			ArrayList HashOrder = new ArrayList();
			Hashtable SectionOrder = new Hashtable();

			ReportUtils.ReadMaturityScoresWithIgnores(xml, outer_sections, HashOrder, SectionOrder, ignore, ignore_tags);

			return SectionOrder;
		}

		public string BuildReport(string xml, ArrayList ignore, ArrayList ignore_tags)
		{
			Hashtable outer_sections = new Hashtable ();
			ArrayList HashOrder = new ArrayList ();
			Hashtable SectionOrder = new Hashtable ();

			ReportUtils.ReadMaturityScoresWithIgnores(xml, outer_sections, HashOrder, SectionOrder, ignore, ignore_tags);

			return BuildReport(Path.GetTempFileName(), outer_sections, null, SectionOrder);
		}

		public string BuildReport (string xml, ArrayList ignore, ArrayList ignore_tags, Hashtable sectionColours)
		{
			Hashtable outer_sections = new Hashtable();
			ArrayList HashOrder = new ArrayList();
			Hashtable SectionOrder = new Hashtable();

			ReportUtils.ReadMaturityScoresWithIgnores(xml, outer_sections, HashOrder, SectionOrder, ignore, ignore_tags);

			return BuildReport(Path.GetTempFileName(), outer_sections, null, SectionOrder, sectionColours);
		}

		public string BuildReport (string xml)
		{
			Hashtable outer_sections = new Hashtable ();
			ArrayList HashOrder = new ArrayList ();
			Hashtable SectionOrder = new Hashtable ();

			ReportUtils.ReadMaturityScores(xml, outer_sections, HashOrder, SectionOrder);

			return BuildReport(Path.GetTempFileName(), outer_sections, null, SectionOrder);
		}

		public string BuildReport(NetworkProgressionGameFile gameFile, int round, ArrayList Scores)
		{
            Hashtable outer_sections = new Hashtable();
            Hashtable inner_sections = new Hashtable();
            Hashtable SectionOrder = new Hashtable();
            if (round <= Scores.Count)
            {
	            var roundScores = (RoundScores) (Scores[round - 1]);
                outer_sections = roundScores.outer_sections;
                inner_sections = roundScores.inner_sections;
                SectionOrder = roundScores.SectionOrder;
                string reportFile = gameFile.GetRoundFile(gameFile.LastRoundPlayed, "OpsMaturityReport_Round" + round + ".xml", gameFile.LastPhasePlayed);

	            Hashtable sectionColours = null;
	            if (roundScores.sectionNameToColour != null)
	            {
		            sectionColours = new Hashtable (roundScores.sectionNameToColour);
	            }

				return BuildReport(reportFile, outer_sections, inner_sections, SectionOrder, sectionColours);
            }
            return "";
		}

		public string BuildReport (string reportFile, Hashtable outer_sections, Hashtable inner_sections, Hashtable SectionOrder)
		{
			return BuildReport(reportFile, outer_sections, inner_sections, SectionOrder, null);
		}

		public string BuildReport (string reportFile, Hashtable outer_sections, Hashtable inner_sections, Hashtable SectionOrder, Hashtable sectionColours)
		{
			LibCore.BasicXmlDocument xdoc = produceXmlDoc (outer_sections, inner_sections, SectionOrder, sectionColours, true);

			xdoc.SaveToURL("",reportFile);

			string datacontents = xdoc.InnerXml;

			return reportFile;
		}

		public string BuildReport(string reportFile, Hashtable outer_sections, Hashtable inner_sections, Hashtable SectionOrder, Hashtable sectionColours, bool DrawKey)
		{
			LibCore.BasicXmlDocument xdoc = produceXmlDoc(outer_sections, inner_sections, SectionOrder, sectionColours, DrawKey);

			xdoc.SaveToURL("", reportFile);

			string datacontents = xdoc.InnerXml;

			return reportFile;
		}

		public LibCore.BasicXmlDocument produceXmlDoc (Hashtable outer_sections, Hashtable inner_sections, Hashtable SectionOrder, Hashtable sectionColours, bool drawkey)
		{
			LibCore.BasicXmlDocument xdoc = LibCore.BasicXmlDocument.Create();
			XmlElement root = xdoc.CreateElement("piechart");
			root.SetAttribute("bands", CONVERT.ToStr(NumBands));
			root.SetAttribute("angle_offset", CONVERT.ToStr(CoreUtils.SkinningDefs.TheInstance.GetIntData("maturity_angle_offset", 0)));
            root.SetAttribute("drawkey", CONVERT.ToStr(drawkey));
			xdoc.AppendChild(root);

			bool suppressSections = (CoreUtils.SkinningDefs.TheInstance.GetIntData("maturity_suppress_sections", 0) == 1);

			ArrayList names = new ArrayList ();

			foreach (string section in outer_sections.Keys)
			{
				int order = -1;
				if (SectionOrder.ContainsKey(section))
				{
					order = (int) SectionOrder[section];
				}

				names.Add(new NumberedSection (section, order));
			}
			names.Sort();

			XmlNode firstSegment;
			bool first;

			//add the segments

			int sectionIndex = 0;
			foreach(NumberedSection ns in names)
			{
				if ((sectionIndex == 0) || !suppressSections)
				{
					string section = ns.name;
					XmlNode seg = (XmlNode) xdoc.CreateElement("segment");
					((XmlElement)seg).SetAttribute( "title", section );

					if ((sectionColours != null) && (sectionIndex < sectionColours.Count))
					{
						System.Drawing.Color colour = (System.Drawing.Color) sectionColours[section];

						if (colour != System.Drawing.Color.Transparent)
						{
							((XmlElement) seg).SetAttribute("colour", CONVERT.ToComponentStr(colour));
						}
					}
					root.AppendChild(seg);
				}

				sectionIndex++;
			}

			if ((inner_sections != null) && (inner_sections.Count > 0))
			{
				//show the previous round scores (inner circle)
				XmlNode inner = (XmlNode) xdoc.CreateElement("inner");
				((XmlElement)inner).SetAttribute( "colour","" );
				root.AppendChild(inner);

				int i=1;
				firstSegment = null;
				first = true;
				foreach (NumberedSection ns in names)
				{
					string section = ns.name;
					XmlNode seg;

					if (first || ! suppressSections)
					{
						seg = (XmlNode) xdoc.CreateElement("seg");
						((XmlElement)seg).SetAttribute( "val", CONVERT.ToStr(i) );
						inner.AppendChild(seg);

						if (first)
						{
							firstSegment = seg;
						}

						first = false;
					}
					else
					{
						seg = firstSegment;
					}

					ArrayList points = (ArrayList)inner_sections[section];

					foreach (string pt in points)
					{
						string[] vals = pt.Split(':');

						XmlNode point = (XmlNode) xdoc.CreateElement("point");
						((XmlElement)point).SetAttribute( "title",vals[0] );
						((XmlElement)point).SetAttribute( "val",vals[1] );
						seg.AppendChild(point);
					}
					i++;
				}
			}

			//now add the outer points (this round scores)
			XmlNode outer = (XmlNode) xdoc.CreateElement("outer");
			((XmlElement)outer).SetAttribute( "colour","" );
			root.AppendChild(outer);

			int j=1;
			firstSegment = null;
			first = true;
			foreach (NumberedSection ns in names)
			{
				string section = ns.name;
				XmlNode seg;
				
				if (first || ! suppressSections)
				{
					seg = (XmlNode) xdoc.CreateElement("seg");
					((XmlElement)seg).SetAttribute( "val", CONVERT.ToStr(j) );
					outer.AppendChild(seg);

					if (first)
					{
						firstSegment = seg;
					}

					first = false;
				}
				else
				{
					seg = firstSegment;
				}

				ArrayList points = (ArrayList)outer_sections[section];

				foreach (string pt in points)
				{
					string[] vals = pt.Split(':');

					XmlNode point = (XmlNode) xdoc.CreateElement("point");
					((XmlElement)point).SetAttribute( "title",vals[0] );
					((XmlElement)point).SetAttribute( "val", vals[1] );
					seg.AppendChild(point);
				}
				j++;
			}
			//xdoc.SaveToURL("",reportFile);
			return xdoc;
		}


	}
}
