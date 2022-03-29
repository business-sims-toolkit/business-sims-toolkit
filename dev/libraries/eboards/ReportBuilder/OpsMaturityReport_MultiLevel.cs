using System;
using System.Xml;
using System.Collections;
using LibCore;

namespace ReportBuilder
{
	/// <summary>
	/// This a refactor of the OpsMaturityReport.
	/// In this we are allowed multiple levels of previous rounds (rather than just the Inner and Outer)
	/// 
	/// 
	/// </summary>
	public class OpsMaturityReport_MultiLevel
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

		public OpsMaturityReport_MultiLevel()
		{
			NumBands = 5;
		}

		public string BuildReport(string reportFile, Hashtable sections, Hashtable SectionOrder, Hashtable sectionColours, bool DrawKey)
		{
			LibCore.BasicXmlDocument xdoc = produceXmlDoc(sections, SectionOrder, sectionColours, DrawKey);

			xdoc.SaveToURL("", reportFile);

			string datacontents = xdoc.InnerXml;

			return reportFile;
		}

		public LibCore.BasicXmlDocument produceXmlDoc (Hashtable sections, Hashtable SectionOrder, Hashtable sectionColours, bool drawkey)
		{
			LibCore.BasicXmlDocument xdoc = LibCore.BasicXmlDocument.Create();
			XmlElement root = xdoc.CreateElement("piechart");
			root.SetAttribute("bands", CONVERT.ToStr(NumBands));
			root.SetAttribute("angle_offset", CONVERT.ToStr(CoreUtils.SkinningDefs.TheInstance.GetIntData("maturity_angle_offset", 0)));
			root.SetAttribute("drawkey", CONVERT.ToStr(drawkey));
			xdoc.AppendChild(root);

			bool suppressSections = (CoreUtils.SkinningDefs.TheInstance.GetIntData("maturity_suppress_sections", 0) == 1);

			ArrayList names = new ArrayList ();

			if (sections.Count > 0)
			{
				foreach (string section in ((Hashtable)sections[1]).Keys)
				{
					int order = -1;
					if (SectionOrder.ContainsKey(section))
					{
						order = (int)SectionOrder[section];
					}

					names.Add(new NumberedSection(section, order));
				}
				names.Sort();
			}

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

			for (int step = 1; step <= sections.Count; step++)
			{
				Hashtable sectionlist = (Hashtable)sections[step];
				if (sectionlist != null)
				{
					XmlNode inner = (XmlNode) xdoc.CreateElement("level");
					((XmlElement)inner).SetAttribute("round", CONVERT.ToStr(step));
					((XmlElement)inner).SetAttribute("colour", "");
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

						//ArrayList points = (ArrayList)inner_sections[section];
						ArrayList points = (ArrayList)sectionlist[section];

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
			}
			string gg = xdoc.OuterXml;
			//xdoc.SaveToURL("",reportFile);
			return xdoc;
		}

	}
}
