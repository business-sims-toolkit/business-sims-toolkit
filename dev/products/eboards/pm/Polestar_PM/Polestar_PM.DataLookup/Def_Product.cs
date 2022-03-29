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
	/// Summary description for Product.
	/// </summary>
	public class Def_Product
	{
		public int productid;
		public int productcost;
		public int estimatedgains;
		public int cost_reduction = 0;
		public bool pick;
		public Boolean productpick = false;
		public int productpriority = 0;
		public Hashtable MyPlatformsByID = new Hashtable();

		public Def_Product()
		{
		}

		public Def_Product(int productid, int productcost, int estimatedgains, int cost_reduction, bool pick)
		{
			this.productid = productid;
			this.productcost = productcost;
			this.estimatedgains = estimatedgains;
			this.cost_reduction = cost_reduction;
			this.pick = pick;

		}

		public Boolean LoadFromXMLNode(XmlNode xn)
		{
			XmlNode selectednode = null;
			int ErrCount =0;
			string ErrMsg = string.Empty;
			string tmpstr = string.Empty;
			
			//====================================================================
			//==extract the single Attributes=====================================
			//====================================================================
			productid = xml_utils.extractInt("productID", ref xn, 0,  ref ErrCount, out ErrMsg);
			productcost = xml_utils.extractInt("cost", ref xn, 0, ref ErrCount, out ErrMsg);
			estimatedgains = xml_utils.extractInt("gain", ref xn, 0, ref ErrCount, out ErrMsg);
			cost_reduction = xml_utils.extractInt("CostReduction", ref xn, -1, ref ErrCount, out ErrMsg);
			productpick = xml_utils.extractBoolean("pick", ref xn, false, ref ErrCount, out ErrMsg);
			System.Diagnostics.Debug.WriteLine("  PRODUCT "+CONVERT.ToStr(productid));

			selectednode = xn.SelectSingleNode("Platforms");

			if (selectednode != null)
			{
				foreach (XmlNode xcn3 in selectednode.ChildNodes)
				{
					if (xcn3 != null)
					{	
						Def_Platform test = new Def_Platform();
						test.LoadFromXMLNode(xcn3);
						MyPlatformsByID.Add(test.platformid,test);

//						if ((test.ImpTechID > 0)&(test.ImpTechID <= IMP_TECH_COUNT))
//						{
//							DVD[test.ImpTechID-1] = test;
//						}
					}
				}
			}
			return true;
		}


		public Def_Platform getPlatform(int platform_id)
		{
			Def_Platform plt = null;
			if (MyPlatformsByID.ContainsKey(platform_id))
			{
				plt = (Def_Platform) MyPlatformsByID[platform_id];
			}
			return plt;
		}


	}
}
