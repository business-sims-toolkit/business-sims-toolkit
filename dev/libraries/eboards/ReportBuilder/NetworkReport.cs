using System.Collections;
using System.Xml;
using System.IO;

using GameManagement;
using Network;
using LibCore;

namespace ReportBuilder
{
	/// <summary>
	/// Summary description for NetworkReport.
	/// </summary>
	public class NetworkReport
	{
		Hashtable Servers;
		ArrayList Mirrors;

		public NetworkReport()
		{
			Servers = new Hashtable();
			Mirrors = new ArrayList();
		}

		void ReadNetworkData(NetworkProgressionGameFile gameFile, int round)
		{
			// Pull the network file so that we can get all the servers/apps
			NodeTree model;
			if (gameFile.CurrentRound == round)
			{
				//get the current network from game file
				model = gameFile.NetworkModel;
			}
			else
			{
				//read the network.xml file from the correct round
				GameFile.GamePhase phase = gameFile.CurrentPhase;
				if (round == 1) phase = GameFile.GamePhase.OPERATIONS;

				string NetworkFile = gameFile.GetRoundFile(round, "Network.xml", phase);
				if (File.Exists(NetworkFile))
				{
					System.IO.StreamReader file = new System.IO.StreamReader(NetworkFile);
					string xmldata = file.ReadToEnd();
					file.Close();
					file = null;
					model = new NodeTree(xmldata);
				}
				else
				{
					//this round not been played yet, so no data
					return;
				}
			}
			
			ArrayList types = new ArrayList();
			types.Add("Server");
			Servers = model.GetNodesOfAttribTypes(types);
		}

		void GetMirroredServers()
		{
			foreach(Node server in Servers.Keys)
			{
				string name = server.GetAttribute("name");
				int inx = name.IndexOf("(M)");

				if (inx >= 0)
				{
					//add to list of mirrors
					string mirror_name = name.Substring(0,inx);
					Mirrors.Add(mirror_name);
				}
			}
		}

		int StripNonNumber(string location)
		{
			string numberpart = location.Remove(0,1);
			int locvalue = CONVERT.ParseInt(numberpart);
			return locvalue;
		}

		public string BuildReport(NetworkProgressionGameFile gameFile, int round)
		{
			ReadNetworkData(gameFile, round);

			string reportFile = gameFile.GetRoundFile(gameFile.CurrentRound,"NetworkReport_Round" + CONVERT.ToStr(round) + ".xml" , gameFile.CurrentPhase);

			LibCore.BasicXmlDocument xdoc = LibCore.BasicXmlDocument.Create();
			XmlNode root = (XmlNode) xdoc.CreateElement("network");
			xdoc.AppendChild(root);

			GetMirroredServers();

			// Get all the biz_services that we are interested in...
			foreach(Node server in Servers.Keys)
			{
				string name = server.GetAttribute("name");
				string loc = server.GetAttribute("location");
				string mem = server.GetAttribute("mem_left");
				string disk = server.GetAttribute("disk_left");
				string proc = server.GetAttribute("proccap");
                string affected_by = server.GetAttribute("affectedby");

				//Check for Upgrades, if no tag then no upgrades
				int count_mem_upgrades = server.GetIntAttribute("count_mem_upgrades",-1);
				string mem_upgraded = "true";
				if (count_mem_upgrades == -1) mem_upgraded = "false";

				int count_disk_upgrades = server.GetIntAttribute("count_disk_upgrades",-1);
				string disk_upgraded = "true";
				if (count_disk_upgrades == -1) disk_upgraded = "false";

				double m = 0;

				if(mem != "")
				{
					m = CONVERT.ParseDouble(mem) / 1000.0;
				}

				//Need to calculate the remaining processor level for the server
				//Should have a monitor calculating this as a ongoing dynamic proc_left value
				//need to calculate it before we 
				int tmpServerProc_Left = CONVERT.ParseInt(proc);
				foreach( Node child in server.getChildren())
				{
					if (! child.GetBooleanAttribute("is_saas", false))
					{
						string tmpName = child.GetAttribute("name");
						string tmpType = child.GetAttribute("type");
						string tmpSWProc_str = child.GetAttribute("proccap");
						if (tmpSWProc_str != "")
						{
							if ((tmpType.ToLower() == "app") || ((tmpType.ToLower() == "database")))
							{
								tmpServerProc_Left -= CONVERT.ParseInt(tmpSWProc_str);
							}
						}
					}
				}

				//Build the server xml information 
				XmlNode serv = (XmlNode) xdoc.CreateElement("server");
				((XmlElement)serv).SetAttribute( "name", name);
				((XmlElement)serv).SetAttribute( "location",loc);
				((XmlElement)serv).SetAttribute( "memory", CONVERT.ToPaddedStr(m,1) );
				((XmlElement)serv).SetAttribute( "storage",disk );
				((XmlElement)serv).SetAttribute( "memory_upgraded", mem_upgraded);
				((XmlElement)serv).SetAttribute( "storage_upgraded", disk_upgraded);
				//((XmlElement)serv).SetAttribute( "proccap", proc);
				((XmlElement)serv).SetAttribute( "proccap", CONVERT.ToStr(tmpServerProc_Left));
                ((XmlElement)serv).SetAttribute("affected_by", affected_by);
				if (Mirrors.Contains((string)name))
				{
					((XmlElement)serv).SetAttribute( "mirror", "true");
				}
				root.AppendChild(serv);

				ArrayList AddressList = new ArrayList();
				Hashtable NodeByAddress = new Hashtable();
				foreach( Node child in server.getChildren())
				{
					loc = child.GetAttribute("location");

					if (! string.IsNullOrEmpty(loc))
					{
						NodeByAddress.Add(loc, child);
						AddressList.Add(loc);
					}
				}
				AddressList.Sort();

				//now get all apps/DBs/empty slots on this server
				//and place the information as a child node on the server
				//foreach( Node child in server.getChildren())
				foreach(string nodeloc in AddressList)
				{
					if (NodeByAddress.Contains(nodeloc))
					{
						Node child = (Node)NodeByAddress[nodeloc];
						if (! child.GetBooleanAttribute("is_saas", false))
						{

							name = child.GetAttribute("name");
							loc = child.GetAttribute("location");
							string type = child.GetAttribute("type");
							string proj = child.GetAttribute("created_by_sip");
							string upgrade = child.GetAttribute("upgraded_by_sip");
							proc = child.GetAttribute("proccap");

							if (type != "Router")
							{
								XmlNode app = (XmlNode) xdoc.CreateElement("slot");
								((XmlElement) app).SetAttribute("name", name);
								((XmlElement) app).SetAttribute("location", loc);
								((XmlElement) app).SetAttribute("type", type);
								((XmlElement) app).SetAttribute("project", proj);
								((XmlElement) app).SetAttribute("upgrade", upgrade);
								((XmlElement) app).SetAttribute("proccap", proc);
								serv.AppendChild(app);
							}
						}
					}
				}
			}

			xdoc.SaveToURL("",reportFile);
			return reportFile;
		}
	}
}
