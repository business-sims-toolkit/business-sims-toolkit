using System.Collections.Specialized;
using System.IO;
using System.Xml;
//
using LibCore;

namespace ReportBuilder
{
	/// <summary>
	/// SupportSpendOverrides loads and saves a support costs override file that allows Facilitators
	/// to override support costs in a "horse trading" session after each round.
	/// </summary>
	public class SupportSpendOverrides
	{
		protected string overrideFile;
		protected System.Collections.Specialized.StringDictionary cell_name_to_override_value;

		protected System.Collections.Specialized.StringDictionary cell_name_to_original_value;

		public SupportSpendOverrides(GameManagement.NetworkProgressionGameFile gameFile)
		{
			cell_name_to_override_value = new  System.Collections.Specialized.StringDictionary();
			cell_name_to_original_value = new StringDictionary();

			overrideFile = gameFile.Dir + "\\global\\support_costs_override.xml";
			if(File.Exists(overrideFile))
			{
				XmlDocument xdoc = new XmlDocument();
				xdoc.Load(overrideFile);
				//
				foreach(XmlNode cellOverride in xdoc.DocumentElement.ChildNodes)
				{
					if(cellOverride.NodeType == XmlNodeType.Element)
					{
						string cellname = cellOverride.Attributes["cellname"].Value;
						string overrideValue = cellOverride.InnerText;

						cell_name_to_override_value[cellname] = overrideValue;
					}
				}
			}
		}

		public static string CreateCellName(int round, string row_name)
		{
			string cellname = row_name + "|" + CONVERT.ToStr(round);
			return cellname;
		}

		public bool GetOverride(int round, string row_name, out string override_val)
		{
			override_val = "";
			string cellname = CreateCellName(round,row_name);
			if(!cell_name_to_override_value.ContainsKey(cellname)) return false;
			override_val = cell_name_to_override_value[cellname];
			return true;
		}

		public bool GetOriginalValue(int round, string row_name, out string original_val)
		{
			original_val = "";
			string cellname = CreateCellName(round,row_name);
			if(!cell_name_to_original_value.ContainsKey(cellname)) return false;
			original_val = cell_name_to_original_value[cellname];
			return true;
		}

		public bool GetOverride(string cellname, out string override_val)
		{
			override_val = "";
			if(!cell_name_to_override_value.ContainsKey(cellname)) return false;
			override_val = cell_name_to_override_value[cellname];
			return true;
		}

		public bool GetOriginalValue(string cellname, out string original_val)
		{
			original_val = "";
			if(!cell_name_to_original_value.ContainsKey(cellname)) return false;
			original_val = cell_name_to_original_value[cellname];
			return true;
		}

		public void SetOverride(int round, string row_name, string override_val)
		{
			string cellname = CreateCellName(round,row_name);
			cell_name_to_override_value[cellname] = override_val;
		}

		public void SetOriginalValue(int round, string row_name, string original_val)
		{
			string cellname = CreateCellName(round,row_name);
			cell_name_to_original_value[cellname] = original_val;
		}

		public void SetOverride(string cellname, string override_val)
		{
			cell_name_to_override_value[cellname] = override_val;
		}

		public void SetOriginalValue(string cellname, string original_val)
		{
			cell_name_to_original_value[cellname] = original_val;
		}

		public void Save()
		{
			XmlDocument xdoc = new XmlDocument();
			XmlElement root = xdoc.CreateElement("overrides");
			xdoc.AppendChild(root);

			foreach(string cellname in cell_name_to_override_value.Keys)
			{
				XmlElement node = CoreUtils.XMLUtils.CreateElementString(root, "celloverride", cell_name_to_override_value[cellname]);
				node.SetAttribute("cellname",cellname);
				//CoreUtils.XMLUtils.CreateElementString(root, cellname, cell_name_to_override_value[cellname]);
			}

			xdoc.Save(overrideFile);
		}
	}
}
