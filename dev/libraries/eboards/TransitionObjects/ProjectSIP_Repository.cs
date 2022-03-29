using System;
using System.IO;
using System.Xml;
using System.Collections;

using LibCore;
using Network;

namespace TransitionObjects
{
	/// <summary>
	/// Class for Handling the Project SIP 
	/// wanted a pass through based model, rather that the load at start system
	/// So this is a skeleton, the only important method is getSIP_Data 
	/// </summary>
	public sealed class ProjectSIP_Repository
	{
		ArrayList LoadedSIPs = new ArrayList();

		/// <summary>
		/// 
		/// </summary>
		public static readonly ProjectSIP_Repository TheInstance = new ProjectSIP_Repository();

		/// <summary>
		/// Private Constructor for the Singleton
		/// </summary>
		ProjectSIP_Repository()
		{
			appBasePath = LibCore.AppInfo.TheInstance.Location;
		}

		public Boolean getSIP_DataForFixedLocation(int ProjectNumber, out string FixedLocation, out string FixedZone)
		{
			Boolean OpSuccess = false;
			string filename;
			string filedata;
			FixedLocation = string.Empty;
			FixedZone = string.Empty;

			//Load the SIP file
			filename = GetSipFilename(CONVERT.ToStr(ProjectNumber));

			Boolean File_Exists = System.IO.File.Exists(filename);
			if (File_Exists)
			{
				//Extract the data 
				using (FileStream fs = File.OpenRead(filename))
				{
					StreamReader reader = new StreamReader(fs);
					filedata = reader.ReadToEnd();
					reader.Close();
					reader = null;
				}

				//Build the XML Document 
				BasicXmlDocument xdoc = LibCore.BasicXmlDocument.Create(filedata);
				XmlNode n1 = xdoc.DocumentElement;

				//Extract out the FixedLocation and FixedZone (if present)
				XmlAttribute x1 = n1.Attributes["fixedlocation"];
				if (x1 != null)
				{
					FixedLocation = x1.InnerText;
				}
				XmlAttribute x2 = n1.Attributes["fixedzone"];
				if (x2 != null)
				{
					FixedZone =  x2.InnerText;
				}
			}
			return OpSuccess;
		}


		public Boolean getSIP_CheckforLookupPlatform(int ProjectNumber, out string PreviousAppName)
		{
			Boolean OpSuccess = false;
			string filename;
			string filedata;
			PreviousAppName = string.Empty;

			//Load the SIP file
			filename = GetSipFilename(CONVERT.ToStr(ProjectNumber));

			Boolean File_Exists = System.IO.File.Exists(filename);
			if (File_Exists)
			{
				//Extract the data 
				using (FileStream fs = File.OpenRead(filename))
				{
					StreamReader reader = new StreamReader(fs);
					filedata = reader.ReadToEnd();
					reader.Close();
					reader = null;
				}

				//Build the XML Document 
				BasicXmlDocument xdoc = LibCore.BasicXmlDocument.Create(filedata);
				XmlNode n1 = xdoc.DocumentElement;

				//Extract out the FixedLocation and FixedZone (if present)
				XmlAttribute x1 = n1.Attributes["lookupplatform"];
				if (x1 != null)
				{
					OpSuccess =true;
				}
				XmlAttribute x2 = n1.Attributes["upgradename"];
				if (x2 != null)
				{
					PreviousAppName =  x2.InnerText;
				}
			}
			return OpSuccess;
		}

		string appBasePath;

		public void SetAppBasePath (string path)
		{
			appBasePath = path;
		}

		string GetSipFilename (string projectNumber)
		{
			return appBasePath + CONVERT.Format(@"\data\sips\SIP{0}.xml", projectNumber);
		}

		/// <summary>
		/// Pass through verion for getting the Project Information 
		/// It returns the various attributes that are required for the Project Node in the network
		/// It returns the Install Action XML String that details the changes to the network when it is installed
		/// </summary>
		/// <param name="ProjectNumber"></param>
		/// <param name="ProductNumber"></param>
		/// <param name="Platform"></param>
		/// <param name="PrjAttrs"></param>
		/// <param name="InstallAction"></param>
		/// <returns></returns>
		public Boolean getSIP_Data(string ProjectNumber, string ProductNumber,string Platform, 
			out ArrayList PrjAttrs, out string InstallAction,
			out Boolean SIPNotFound, out Boolean ProductNotFound, out Boolean PlatformNotFound)
		{
			Boolean OpSuccess = false;
			string filename;
			string filedata;

			SIPNotFound = true;
			ProductNotFound = true;
			PlatformNotFound = true;

			//Set the output parameters 
			PrjAttrs = new ArrayList();
			InstallAction = "";

			//Load the SIP file
			filename = GetSipFilename(ProjectNumber);

			if(File.Exists(filename))
			{
				//Extract the data 
				using (FileStream fs = File.OpenRead(filename))
				{
					StreamReader reader = new StreamReader(fs);
					filedata = reader.ReadToEnd();
					reader.Close();
					reader = null;
				}
			
				//Build the XML Document 
				BasicXmlDocument xdoc = LibCore.BasicXmlDocument.Create(filedata);
				XmlNode n1 = xdoc.DocumentElement;
				SIPNotFound = false; //we have a readable SIP document  

				OpSuccess = StripAttrsFromXML(n1, ProductNumber, Platform, out PrjAttrs, 
					out InstallAction, out ProductNotFound, out PlatformNotFound);
			}

			return OpSuccess;
		}

		/// <summary>
		/// The method which runs down through the Project, Product and Platform
		/// return a list of all the attrs and the install script
		/// </summary>
		/// <param name="n1"></param>
		/// <param name="ProductNumber"></param>
		/// <param name="PlatformStr"></param>
		/// <param name="PrjAttrs"></param>
		/// <param name="InstallAction"></param>
		/// <returns></returns>
		Boolean StripAttrsFromXML(XmlNode n1, string ProductNumber, string PlatformStr, 
			out ArrayList PrjAttrs, out string InstallAction, out Boolean ProductNotFound, 
			out Boolean PlatformNotFound)
		{
			Boolean OpSuccess = false;
			ProductNotFound = true;
			PlatformNotFound = true;
			//Set the output parameters 
			PrjAttrs = new ArrayList();
			InstallAction = "";

			//get the Project Level Attributes 
			foreach(System.Xml.XmlAttribute xa in n1.Attributes)
			{
				PrjAttrs.Add( new AttributeValuePair( xa.Name, xa.InnerText ) );
			}
			//Interate into the Children Nodes to find the Product
			foreach (XmlNode nn in n1.ChildNodes)
			{
				XmlNode namednode1 = nn.Attributes.GetNamedItem("productid");

				int tmpProductID = CONVERT.ParseInt(namednode1.InnerText);
				if (ProductNumber == CONVERT.ToStr(tmpProductID))
				{
					ProductNotFound = false;
					//Add the Attributes from the Product Definition
					foreach(System.Xml.XmlAttribute xa in nn.Attributes)
					{
						PrjAttrs.Add( new AttributeValuePair( xa.Name, xa.InnerText.Replace("\\r\\n","\r\n") ) );
					}
					//Interate into the Children Nodes to find the Product
					foreach (XmlNode np in nn.ChildNodes)
					{
						XmlNode namednode2 = np.Attributes.GetNamedItem("platformid");
						string tmpPlatformStr = namednode2.InnerText;

						if (tmpPlatformStr == PlatformStr)
						{
							PlatformNotFound = false;
							//Add the Attributes from the PlatForm Definition
							foreach(System.Xml.XmlAttribute xa in np.Attributes)
							{
								PrjAttrs.Add( new AttributeValuePair( xa.Name, xa.InnerText.Replace("\\r\\n","\r\n") ) );
							}
							//Extract the Install String Xml Script
							XmlNode ns = np.FirstChild;
							if (ns != null)
							{
								InstallAction = ns.OuterXml;
								OpSuccess = true;
							}
						}
					}
				}
			}
			return OpSuccess;
		}
	}
}

//=============================================================================
//=============================================================================
//==Code Commented out (Reserved for future Use)===============================
//=============================================================================
//=============================================================================

//
//		public void Reset()
//		{
//			LoadedSIPs.Clear();
//		}

		/// <summary>
		/// Scan through the SIP data directory and load all the SIPs 
		/// </summary>
		/// <returns></returns>
//		public Boolean LoadData(Boolean ResetData)
//		{
//			Boolean OpSuccess = false;
//			Boolean loadedOK = false;
//			string filename = string.Empty;
//			string filedata = string.Empty;
//
//			if (ResetData)
//			{
//				Reset();
//			}
//
//			if (data_directory != null)
//			{
//				//Get the list of possibles files 
//				string[] files = Directory.GetFiles(data_directory,"*.xml");
//				if (files.Length >0)
//				{
//					for (int step=0; step < files.Length; step++)
//					{
//						filename = files[step];
//
//						try
//						{
//							//Extract the data 
//							using (FileStream fs = File.OpenRead(filename))
//							{
//								StreamReader reader = new StreamReader(fs);
//								filedata = reader.ReadToEnd();
//							}
//							//Build the XML Document 
//							XmlDocument xd = new XmlDocument();
//							xd.LoadXml(filedata);
//							//Get the First Node
//							XmlNode n1 = xd.FirstChild;
//							ProjectSIP tmpSIP = new ProjectSIP();
//							loadedOK = tmpSIP.LoadFromXML(n1);
//							if (loadedOK)
//							{
//								LoadedSIPs.Add(tmpSIP);
//							}
//						}
//						catch (Exception evc)
//						{
//							string st = "scdsc";
//						}
//					}
//				}
//			}
//			if (LoadedSIPs.Count>0)
//			{
//				OpSuccess = true;
//			}
//			return OpSuccess;
//		}

//		/// <summary>
//		/// Extract out a numbered SIP
//		/// </summary>
//		/// <param name="SIPnumber"></param>
//		/// <returns></returns>
//		public ProjectSIP getSIPByNumber(int SIPnumber)
//		{
//			ProjectSIP foundSIP = null;
//
//			foreach(ProjectSIP tmpSIP in LoadedSIPs)
//			{
//				if (tmpSIP.SIP_number == SIPnumber)
//				{
//					foundSIP = tmpSIP;
//				}
//			}
//			return foundSIP;
//		}

		/// <summary>
		/// Scan through the SIPs and pick out the required SIPs
		/// </summary>
		/// <param name="round">Which round do we want the selection for</param>
		/// <param name="IncludePrevious">Should we include previous round SIPs</param>
		/// <returns></returns>
//		public ArrayList getSIPSForRound(int round, Boolean IncludePrevious)
//		{
//			ArrayList al = null;
//
//			if (round ==-1)
//			{
//				al = (ArrayList) LoadedSIPs.Clone();
//			}
//			else
//			{
//				al = new ArrayList();
//				foreach(ProjectSIP tmpSIP in  LoadedSIPs)
//				{
//					if (IncludePrevious)
//					{
//						if (round > tmpSIP.round)
//						{
//							al.Add(tmpSIP);
//						}
//					}
//					else
//					{
//						if (round == tmpSIP.round)
//						{
//							al.Add(tmpSIP);
//						}
//					}
//				}
//			}
//			return al;
//		}

