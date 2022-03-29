using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Xml;
using System.IO;

using GameManagement;
using LibCore;
using Logging;

namespace ReportBuilder
{
	/// <summary>
	/// Summary description for ReportUtils.
	/// </summary>
	public class ReportUtils
	{
		ArrayList costs;
		public Hashtable Maturity_Names = new Hashtable();

		ArrayList ignoreList = new ArrayList();
		ArrayList ignoreList_tag_names = new ArrayList();

		public ReportUtils(NetworkProgressionGameFile gameFile)
		{
			costs = new ArrayList();
			for (int i = 0; i < CoreUtils.SkinningDefs.TheInstance.GetIntData("roundcount", 5); i++)
			{
				Hashtable tmp = new Hashtable();
				costs.Add(tmp);
				ReadCostsFromFile(gameFile.Dir + "\\global\\costs_r"+((i+1).ToString())+".xml",tmp);
			}
		}

		ReportUtils()
		{
		}

		public static readonly ReportUtils TheInstance = new ReportUtils();

		public int GetCost(string costType, int round)
		{
			if ((round > 0) && (round <= costs.Count))
			{
				if (((Hashtable) costs[(round - 1)]).ContainsKey(costType))
				{
					return (int) ((Hashtable) costs[(round - 1)])[costType];
				}
			}
			return 0;
		}

		void ReadCostsFromFile(string CostsFile,Hashtable costtable)
		{
			//System.Diagnostics.Debug.WriteLine("=========================================");

			if (File.Exists(CostsFile))
			{
				FileStream fs = new FileStream(CostsFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				//
				StreamReader sr = new StreamReader(fs);
				string line = sr.ReadLine();

				while (null != line)
				{
					string cost = BasicIncidentLogReader.ExtractValue(line, "type");
					string val = BasicIncidentLogReader.ExtractValue(line, "cost");
					string reference = BasicIncidentLogReader.ExtractValue(line, "ref");

					if (reference != string.Empty)
					{
						cost += "_" + reference;
					}

					if (!costtable.ContainsKey(cost)
						&& ! string.IsNullOrEmpty(cost)
						&& ! string.IsNullOrEmpty(val))
					{
						costtable.Add(cost, CONVERT.ParseInt(val));
					}
					line = sr.ReadLine();
				}
				//
				sr.Close();
				fs.Close();
			}
		}

		public void GetMaturityScores(NetworkProgressionGameFile gameFile, int round, Hashtable inner_sections, Hashtable outer_sections, ArrayList HashOrder)
		{
			GetMaturityScores(gameFile, round, inner_sections, outer_sections, HashOrder, null);
		}

		public void GetMaturityScores(NetworkProgressionGameFile gameFile, int round, Hashtable inner_sections, Hashtable outer_sections, ArrayList HashOrder, Hashtable SectionOrder, Dictionary<string, Color> sectionNameToColour = null)
		{
			ReadIgnoreList(gameFile);

			//read the xml file to get the segment and score names as well as the data
			string xmlfile = "";
			System.IO.StreamReader file;
			string xmldata = "";
			if (round > 1)
			{
				xmlfile = gameFile.GetMaturityRoundFile(round - 1);

				if (File.Exists(xmlfile))
				{
					file = new System.IO.StreamReader(xmlfile);
					xmldata = file.ReadToEnd();
					file.Close();
					file = null;

					ReadMaturityScores(xmldata, false, inner_sections, outer_sections, HashOrder, SectionOrder, Maturity_Names, ignoreList, ignoreList_tag_names, sectionNameToColour);
				}
			}

			xmlfile = gameFile.GetMaturityRoundFile(round);
			if (File.Exists(xmlfile))
			{
				file = new System.IO.StreamReader(xmlfile);
				xmldata = file.ReadToEnd();
				file.Close();
				file = null;

				ReadMaturityScores(xmldata, true, inner_sections, outer_sections, HashOrder, SectionOrder, Maturity_Names, ignoreList, ignoreList_tag_names, sectionNameToColour);
			}
		}

		void ReadIgnoreList(NetworkProgressionGameFile gameFile)
		{
			ignoreList.Clear();
			ignoreList_tag_names.Clear();
			//if file already exists, read into xml
			string StatesFile = gameFile.Dir + "\\global\\Eval_States.xml";

			LibCore.BasicXmlDocument xml;
			XmlNode root;

			if (System.IO.File.Exists(StatesFile))
			{

				System.IO.StreamReader file = new System.IO.StreamReader(StatesFile);
				xml = LibCore.BasicXmlDocument.Create(file.ReadToEnd());
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
							else if (att.Name == "dest_tag_name")
							{
								ignoreList_tag_names.Add(att.Value);
							}
						}
					}
				}
			}
		}

		//private static bool Ignore(ArrayList ignoreList, string name)
		static bool Ignore(ArrayList ignoreList, ArrayList ignore_tag_List, string question, string data_tag_name)
		{
			// If we have a valid non-empty ignore_tag_List then use it instead of just the names.
			if(ignore_tag_List != null)
			{
				if(ignore_tag_List.Count > 0)
				{
					foreach(string s in ignore_tag_List)
					{
						if(s == data_tag_name)
						{
							return true;
						}
					}

					return false;
				}
			}

			foreach( string s in ignoreList)
			{
				if(s == question)
				{
					return true;
				}
			}
			return false;
		}

		static public void ReadMaturityScoresWithIgnores (string xmldata, Hashtable outer_sections, ArrayList HashOrder, Hashtable SectionOrder, ArrayList ignore, ArrayList tag_ignore)
		{
			ReadMaturityScores(xmldata, true, new Hashtable (), outer_sections, HashOrder, SectionOrder, new Hashtable (), ignore, tag_ignore);
		}

		static public void ReadMaturityScores (string xmldata, Hashtable outer_sections, ArrayList HashOrder, Hashtable SectionOrder)
		{
			ReadMaturityScores(xmldata, true, new Hashtable (), outer_sections, HashOrder, SectionOrder, new Hashtable (), new ArrayList (), new ArrayList());
		}

		void ReadMaturityScores(string xmldata, bool outer, Hashtable inner_sections, Hashtable outer_sections, ArrayList HashOrder)
		{
			ReadMaturityScores(xmldata, outer, inner_sections, outer_sections, HashOrder, null);
		}

		void ReadMaturityScores(string xmldata, bool outer, Hashtable inner_sections, Hashtable outer_sections, ArrayList HashOrder, Hashtable OrderForPieChart)
		{
			ReadMaturityScores(xmldata, outer, inner_sections, outer_sections, HashOrder, OrderForPieChart, this.Maturity_Names, this.ignoreList, this.ignoreList_tag_names);
		}

		static void ReadMaturityScores(string xmldata, bool outer, Hashtable inner_sections, Hashtable outer_sections, ArrayList HashOrder, Hashtable OrderForPieChart, Hashtable Maturity_Names, ArrayList ignoreList, ArrayList ignore_tag_List, Dictionary<string, Color> sectionNameToColour = null)
		{
			BasicXmlDocument xdoc = LibCore.BasicXmlDocument.Create(xmldata);
			XmlNode rootNode = xdoc.DocumentElement;

			HashOrder.Clear();
			Maturity_Names.Clear();

			if (OrderForPieChart != null)
			{
				OrderForPieChart.Clear();
			}

			foreach(XmlNode section in rootNode.ChildNodes)
			{
				if(section.Name == "section")
				{
					var colour = BasicXmlDocument.GetColourAttribute(section, "colour", Color.Transparent);

					string section_name = "";
					//get the section name
					foreach(XmlNode child in section.ChildNodes)
					{
						if (child.Name == "section_name")
						{
							section_name = child.InnerText;
							HashOrder.Add(section_name);

							if (OrderForPieChart != null)
							{
								OrderForPieChart[section_name] = -1;
							}

							if ((sectionNameToColour != null)
								&& (colour != Color.Transparent))
							{
								sectionNameToColour[section_name] = colour;
							}
						}
						else if (child.Name == "section_order")
						{
							if (OrderForPieChart != null)
							{
								OrderForPieChart[section_name] = CONVERT.ParseInt(child.InnerText);
							}
						}
						else if (child.Name == "aspects")
						{
							foreach(XmlNode aspect in child.ChildNodes)
							{
								string aspect_name = "";
								string aspect_val = "";
								string aspect_tag_name = "";
                                bool addToSections = true;
								//
								foreach (XmlNode att in aspect.ChildNodes)
								{
									if (att.Name == "aspect_name")
									{
										aspect_name = att.InnerText;
									}
									else if (att.Name == "dest_tag_data")
									{
										aspect_val = att.InnerText;
									}
									else if(att.Name == "dest_tag_name")
									{
										aspect_tag_name = att.InnerText;
									}

                                    if (att.Name == "aspect_off" && att.InnerText == "true")
                                    {
                                        addToSections = false;
                                    }
								}

                                if (!addToSections)
                                {
                                    continue;
                                }

								//if (Ignore(ignoreList, aspect_name))
								if (Ignore(ignoreList, ignore_tag_List, aspect_name, aspect_tag_name))
								{
									continue;
								}

								if (outer == true)
								{
									if (outer_sections.ContainsKey(section_name))
									{
										((ArrayList)outer_sections[section_name]).Add(aspect_name + ":" + aspect_val);
									}
									else
									{
										ArrayList arr = new ArrayList();
										arr.Add(aspect_name + ":" + aspect_val);
										outer_sections.Add(section_name,arr);
									}
								}
								else
								{
									if (inner_sections.ContainsKey(section_name))
									{
										((ArrayList)inner_sections[section_name]).Add(aspect_name + ":" + aspect_val);
									}
									else
									{
										ArrayList arr = new ArrayList();
										arr.Add(aspect_name + ":" + aspect_val);
										inner_sections.Add(section_name,arr);
									}
								}
								if (Maturity_Names.ContainsKey(section_name))
								{
									int ind = ((ArrayList)Maturity_Names[section_name]).IndexOf(aspect_name);
									if (ind < 0)
									{	
										((ArrayList)Maturity_Names[section_name]).Add(aspect_name);
									}
								}
								else
								{
									ArrayList arr = new ArrayList();
									arr.Add(aspect_name);
									Maturity_Names.Add(section_name,arr);
								}
							}
						}
					}
				}
			}
		}
	}
}