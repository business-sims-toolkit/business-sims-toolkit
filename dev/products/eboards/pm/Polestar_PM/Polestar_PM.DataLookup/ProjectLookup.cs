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
	/// This class handles all lookups in the Project Information 
	/// We are keeping the same data files (but cut into different rounds)
	/// and both the UI and The Engine will need information from the data file 
	/// </summary>
	public sealed class ProjectLookup
	{
		//private string project_Data_filename = "";
		private int round = 1;

		private Hashtable projectDataByID = new Hashtable();
		
		public static readonly ProjectLookup TheInstance = new ProjectLookup();

		public ProjectLookup()
		{
		}

		public void lookupData(int round_value)
		{
			round = round_value;
			string project_Data_filename = LibCore.AppInfo.TheInstance.Location + "data\\SIPS\\round_sips.xml";
			XmlDocument xdoc = new XmlDocument();
			xdoc.Load(project_Data_filename);
			//
			XmlElement xe = (XmlElement) xdoc.DocumentElement.SelectSingleNode("round" + CONVERT.ToStr(round_value));
			if (xe.InnerText.Length > 0)
			{
				string[] filebases = xe.InnerText.Split(',');
				lookupData(filebases);
			}
		}

		private void lookupData(string[] filebases)
		{
			projectDataByID.Clear();

			foreach (string basefile in filebases)
			{
				string xmlfile = basefile + ".xml";
				XmlDocument xdoc = new XmlDocument();
				string project_Data_filename = LibCore.AppInfo.TheInstance.Location + "data\\SIPS\\" + xmlfile;
				xdoc.Load(project_Data_filename);
				//
				XmlNode projectNode = xdoc.DocumentElement;
				//Project Level 
				string s4 = projectNode.InnerText;
				string s5 = projectNode.InnerXml;
				string s6 = projectNode.InnerXml;

				XmlNode xn = projectNode;

				Def_Project prj_obj = new Def_Project();
				prj_obj.LoadFromXMLNode(xn);
				projectDataByID.Add(prj_obj.projectid, prj_obj);
			}
		}

		public Def_Project getProjectObj(int prj_id)
		{
			Def_Project dp = null;
			if (projectDataByID.ContainsKey(prj_id))
			{
				dp = (Def_Project)projectDataByID[prj_id];
			}
			return dp;
		}

		/// <summary>
		/// Return the list of required projects for this Round
		/// </summary>
		/// <returns></returns>
		public ArrayList getRegulationProjectList()
		{
			ArrayList regulation_projects_names = new ArrayList();

			foreach(int project_id in projectDataByID.Keys)
			{
				Def_Project prj_obj = (Def_Project) projectDataByID[project_id];
				if (prj_obj != null)
				{
					if (prj_obj.isRegulation)
					{
						regulation_projects_names.Add(project_id);
					}
				}
			}
			return regulation_projects_names;
		}

		/// <summary>
		/// Return the list of all projects for this Round
		/// </summary>
		/// <returns></returns>
		public ArrayList getProjectList(bool includeHidden)
		{
			ArrayList project_id_list = new ArrayList();
			foreach(int project_id in projectDataByID.Keys)
			{
				Def_Project tmpPrjObj = (Def_Project)(projectDataByID[project_id]);
				if (((includeHidden) & (tmpPrjObj.visible == false)) | (tmpPrjObj.visible == true))
				{
					project_id_list.Add(project_id);
				}
			}
			return project_id_list;
		}

		public ArrayList getProductList(int project_id)
		{
			ArrayList product_id_list = new ArrayList();

			if (projectDataByID.ContainsKey(project_id))
			{
				Def_Project prj = (Def_Project) projectDataByID[project_id];
				if (prj != null)
				{
					foreach(int product_id in prj.MyProductsByID.Keys)
					{
						product_id_list.Add(product_id);
					}
				}
			}
			return product_id_list;
		}

		public ArrayList getPlatformList(int project_id, int product_id)
		{
			ArrayList platform_id_list = new ArrayList();

			if (projectDataByID.ContainsKey(project_id))
			{
				Def_Project prj = (Def_Project)projectDataByID[project_id];
				if (prj != null)
				{
					if (prj.MyProductsByID.Contains(product_id))
					{
						Def_Product prd = (Def_Product)	prj.MyProductsByID[product_id];
						if (prd != null)
						{
							foreach (int platform_id in prd.MyPlatformsByID.Keys)
							{
								platform_id_list.Add(platform_id);
							}
						}
					}
				}
			}
			return platform_id_list;
		}

		public string TranslatePlatformToStr(int platform_id)
		{
			string platform_display_str = "X";
			switch(platform_id)
			{
				case 1:
					platform_display_str = "X";
					break;
				case 2:
					platform_display_str = "Y";
					break;
				case 3:
					platform_display_str = "Z";
					break;
			}
			return platform_display_str;
		}

		/// <summary>
		/// Extracts all the definition information for a requested Prj,Prd,Plt combination
		/// The hashtable contain each of the stages and an arrylist of TaskBlocks
		/// </summary>
		/// <param name="project_id"></param>
		/// <param name="product_id"></param>
		/// <param name="platform_id"></param>
		/// <param name="project_data"></param>
		/// <param name="product_data"></param>
		/// <param name="platform_data"></param>
		/// <param name="stage_data"></param>
		/// <returns></returns>
		public bool getProjectData(int project_id, int product_id, int platform_id, 
			out Def_Project project_data, out Def_Product product_data, out Def_Platform platform_data)
		{
			bool data_built = false;

			project_data = null;
			product_data = null;
			platform_data = null;
		
			if (projectDataByID.ContainsKey(project_id))
			{
				project_data = (Def_Project) projectDataByID[project_id];
				if (project_data != null)
				{
					product_data = project_data.getProduct(product_id);
					if (product_data != null)
					{
						platform_data = product_data.getPlatform(platform_id);
						if (platform_data != null)
						{
							data_built = true;
						}
					}
				}
			}
			return data_built;
		}

	}
}
