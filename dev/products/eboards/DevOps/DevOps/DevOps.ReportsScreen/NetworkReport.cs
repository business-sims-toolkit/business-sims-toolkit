using System.Collections.Generic;
using System.Drawing;
using System.Xml;
using LibCore;

using GameManagement;
using Network;


namespace DevOps.ReportsScreen
{
	public class NetworkReport
    {
        NetworkProgressionGameFile gameFile;
        NodeTree model;

        int round;

        public NetworkReport(NetworkProgressionGameFile gameFile, NodeTree network, int round)
        {
            this.gameFile = gameFile;
            model = network;
            this.round = round;
        }

        public string BuildReport()
        {
            BasicXmlDocument xml = BasicXmlDocument.Create();

            XmlElement root = xml.AppendNewChild("network_report");

            XmlElement enclosures = xml.AppendNewChild("Enclosures");
            enclosures.AppendAttribute("back_colour", Color.White);
            Node hub = model.GetNamedNode("Hub");
            
            Dictionary<string, Dictionary<string, List<Node>>> platformToEnclosures =
                new Dictionary<string, Dictionary<string, List<Node>>>();

            foreach (Node router in hub.GetChildrenWithAttributeValue("type", "Router"))
            {
                foreach (Node server in router.GetChildrenWithAttributeValue("type", "Server"))
                {
                    if (server.GetAttribute("name").Equals("Deimos") || server.GetBooleanAttribute("hidden", false))
                    {
                        continue;
                    }
                    
                    List<Node> apps = server.GetChildrenWithAttributeValue("type", "App");

                    apps.Sort((a, b) => a.GetIntAttribute("round", 0).CompareTo(b.GetIntAttribute("round", 0)));
                    
                    string enclosureName = server.GetAttribute("name");
                    //enclosureNameToApps[enclosureName] = apps;

                    string platform = server.GetAttribute("platform");

                    if (!platformToEnclosures.ContainsKey(platform))
                    {
                        platformToEnclosures.Add(platform, new Dictionary<string, List<Node>>());
                    }

                    platformToEnclosures[platform][enclosureName] = apps;
                    
                }
            }

            foreach (string platformName in platformToEnclosures.Keys)
            {
                foreach (string enclosureName in platformToEnclosures[platformName].Keys)
                {
                    XmlElement enclosure = xml.AppendNewChild(enclosures, "Enclosure");
                    BasicXmlDocument.AppendAttribute(enclosure, "name", CONVERT.Format("{0}({1})", enclosureName, platformName));

                    XmlElement preexistingSection = null;
                    XmlElement newServicesSection = null;

                    foreach (Node app in platformToEnclosures[platformName][enclosureName])
                    {
                        int roundInstalled = app.GetIntAttribute("round", 0);

                        Node serviceLink = app.GetFirstChildOfType("Connection");

                        Node bsu = model.GetNamedNode(serviceLink.GetAttribute("to"));

                        Node bizService = model.GetNamedNode(bsu.GetAttribute("biz_service_function"));
                        
                        if (roundInstalled == 0)
                        {
                            if (preexistingSection == null)
                            {
                                preexistingSection = xml.AppendNewChild(enclosure, "Section");
                                BasicXmlDocument.AppendAttribute(preexistingSection, "name", "Pre-Existing");
                            }

                            AddServiceToSection(xml, preexistingSection, bizService, roundInstalled);
                        }
                        else
                        {
                            if (newServicesSection == null)
                            {
                                newServicesSection = xml.AppendNewChild(enclosure, "Section");
                                BasicXmlDocument.AppendAttribute(newServicesSection, "name", "New Apps");
                            }

                            AddServiceToSection(xml, newServicesSection, bizService, roundInstalled);
                        }
                        
                    }

                }
            }
            

            string reportFile = gameFile.GetRoundFile(round, "NetworkReport.xml", GameFile.GamePhase.OPERATIONS);
            xml.Save(reportFile);

            return reportFile;
        }

        void AddServiceToSection(BasicXmlDocument xml, XmlElement section, Node bizService, int round = 0)
        {

            string serviceName = bizService.GetAttribute("biz_service_function", "Not found");
            
            string shortDesc = bizService.GetAttribute("shortdesc");

            XmlElement service = xml.AppendNewChild(section, "Service");

            BasicXmlDocument.AppendAttribute(service, "name", serviceName);
            BasicXmlDocument.AppendAttribute(service, "short_desc", shortDesc);
            string displayText = shortDesc;
            if (round > 0)
            {
                displayText += " (" + round + ")";
            }
            BasicXmlDocument.AppendAttribute(service, "display_text", displayText);
            BasicXmlDocument.AppendAttribute(service, "round", round);

        }

    }
}
