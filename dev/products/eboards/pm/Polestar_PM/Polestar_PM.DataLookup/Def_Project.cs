using System;
using System.IO;
using System.Xml;
using System.Collections;

using BaseUtils;
using CoreUtils;
using LibCore;

namespace Polestar_PM.DataLookup
{
	/// <summary>
	/// Summary description for Project.
	/// </summary>
	public class Def_Project 
	{
		public int projectid = 0;
		public string appname = "";
		public int budget = 0;
		public int cost_reduction = 0;
		public bool visible = true;
		public bool isRegulation = false; 
		public Boolean allowedrecycle = false;
		public ArrayList MyProducts = new ArrayList();
		public Hashtable MyProductsByID = new Hashtable();
		public Hashtable RequiredSkills = new Hashtable();

		//autostart
		public int autostart_budget = 0;
		public int autostart_product = 0;
		public int autostart_platform = 0;
		public string autostart_install_location = "";
		public int autostart_install_day = 0;
		public bool auto_start_install_day_auto_update = false;

		#region Constructor

		public Def_Project()
		{
		}

		public Def_Project(int projectid, string appname, int budget, 
			int cost_reduction, bool isRegulation, bool isVisible , bool allowedrecycle,
			int autostart_budget, int autostart_product, int autostart_platform,
			string autostart_install_location, int autostart_install_day, 
			bool auto_start_install_day_auto_update)
		{
			this.projectid = projectid;
			this.appname = appname;
			this.budget = budget;
			this.cost_reduction = cost_reduction;
			this.isRegulation = isRegulation;
			this.visible = isVisible;
			this.allowedrecycle = allowedrecycle;

			this.autostart_budget = autostart_budget;
			this.autostart_product =  autostart_product;
			this.autostart_platform = autostart_platform;
			this.autostart_install_location =  autostart_install_location;
			this.autostart_install_day = autostart_install_day;
			this.auto_start_install_day_auto_update = auto_start_install_day_auto_update;
		}

		#endregion Accessors

		public bool LoadFromXMLNode(XmlNode xn)
		{
			Boolean OpSuccess = false;
			int ErrCount =0;
			string tmpstr = string.Empty;
			string ErrMsg="";
			
			RequiredSkills.Clear();
			
			projectid = int.Parse(xml_utils.extractStr("projectID", ref xn, string.Empty, ref ErrCount, out ErrMsg));
			appname = xml_utils.extractStr("AppName", ref xn, string.Empty,  ref ErrCount, out ErrMsg);
			budget = xml_utils.extractInt("ProposedBudget", ref xn, 0, ref ErrCount, out ErrMsg);
			cost_reduction = xml_utils.extractInt("CostReduction", ref xn, -1, ref ErrCount, out ErrMsg);
			tmpstr =  xml_utils.extractStr("Type", ref xn, string.Empty, ref ErrCount, out ErrMsg);  
			if (tmpstr.ToLower().IndexOf("regulation")>-1)
			{
				isRegulation =true;
			}

			//ProjectType = (emProjectType) Enum.Parse(typeof(emProjectType),tmpstr.ToUpper());
			visible = xml_utils.extractBoolean("Visible", ref xn, false, ref ErrCount, out ErrMsg);

			//ProjectType = (emProjectType) Enum.Parse(typeof(emProjectType),tmpstr.ToUpper());
			allowedrecycle = xml_utils.extractBoolean("AllowedRecycle", ref xn, false, ref ErrCount, out ErrMsg);

			XmlNode xn2 = xn.SelectSingleNode("autostart");
			if (xn2 != null)
			{
				autostart_budget = xml_utils.extractInt("auto_start_budget", ref xn2, 0, ref ErrCount, out ErrMsg);
				autostart_product = xml_utils.extractInt("auto_start_product", ref xn2, 0, ref ErrCount, out ErrMsg);
				autostart_platform = xml_utils.extractInt("auto_start_platform", ref xn2, 0, ref ErrCount, out ErrMsg);
				autostart_install_location = xml_utils.extractStr("auto_start_install_location", ref xn2, "", ref ErrCount, out ErrMsg);
				autostart_install_day = xml_utils.extractInt("auto_start_install_day", ref xn2, 0, ref ErrCount, out ErrMsg);
				auto_start_install_day_auto_update = xml_utils.extractBoolean("auto_start_install_day_auto_update", ref xn2, false, ref ErrCount, out ErrMsg);
			}

//			System.Diagnostics.Debug.WriteLine("PROJECT "+CONVERT.ToStr(projectid)+"["+appname+"]");
			XmlNode xn3 = xn.SelectSingleNode("SkillsRequired");
			if (xn3 != null)
			{
				foreach (XmlNode xcn3 in xn3.ChildNodes)
				{
					XmlNode xcn3a = xcn3;
					//string s1 = xcn3.InnerText;
					//string s2 = xcn3.InnerXml;
					string skillname = xml_utils.extractStr("SkillType", ref xcn3a, string.Empty, ref ErrCount, out ErrMsg);
					int skilllevel = xml_utils.extractInt("SkillLevel", ref xcn3a, 0, ref ErrCount, out ErrMsg);
					RequiredSkills.Add(skillname, skilllevel);
				}
			}

			XmlNode xn4 = xn.SelectSingleNode("products");
			if (xn4 != null)
			{
				foreach (XmlNode xcn4 in xn4.ChildNodes)
				{
					//string s1 = xcn4.InnerText;
					//string s2 = xcn4.InnerXml;
					Def_Product p1 = new Def_Product();
					p1.LoadFromXMLNode(xcn4);
					MyProducts.Add(p1);
					MyProductsByID.Add(p1.productid,p1);
					//s1 = s1+ "";
				}
			}
			return OpSuccess;
		}

		public Def_Product getProduct(int product_id)
		{
			Def_Product prd = null;
			if (MyProductsByID.ContainsKey(product_id))
			{
				prd = (Def_Product) MyProductsByID[product_id];
			}
			return prd;
		}

	}
}
