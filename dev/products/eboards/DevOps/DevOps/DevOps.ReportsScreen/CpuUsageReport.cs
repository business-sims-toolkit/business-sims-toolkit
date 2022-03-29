using System;
using System.Collections.Generic;
using System.Drawing;
using System.Xml;

using Algorithms;
using CoreUtils;
using GameManagement;
using LibCore;
using Network;


namespace DevOps.ReportsScreen
{
	public class CpuUsageReport
    {
        NetworkProgressionGameFile gameFile;
        NodeTree model;

        int round;
        
        BasicXmlDocument xml;

        public CpuUsageReport(NetworkProgressionGameFile gameFile, int round)
        {
            this.gameFile = gameFile;
            this.round = round;
            model = gameFile.GetNetworkModel(round);

            xml = BasicXmlDocument.Create();
        }

        public string BuildReport()
        {
            XmlElement root = xml.AppendNewChild("grouped_bar_chart");
            BasicXmlDocument.AppendAttribute(root, "bars_stacked", true);
            BasicXmlDocument.AppendAttribute(root, "bold_legend", true);
            BasicXmlDocument.AppendAttribute(root, "draw_tick_lines", false);
            BasicXmlDocument.AppendAttribute(root, "row_colour_one", SkinningDefs.TheInstance.GetColorData("scorecard_row_two_colour", Color.White));
            BasicXmlDocument.AppendAttribute(root, "row_colour_two", SkinningDefs.TheInstance.GetColorData("scorecard_row_one_colour", Color.White));

            XmlElement categories = xml.AppendNewChild(root, "bar_categories");

            Color usedCpuColour = SkinningDefs.TheInstance.GetColorData("used_cpu_colour");
            Color freeCpuColour = SkinningDefs.TheInstance.GetColorData("free_cpu_colour");

            AddCategory(categories, "Free CPU", freeCpuColour);
            AddCategory(categories, "Used CPU", usedCpuColour);
            
            XmlElement groupsElement = xml.AppendNewChild(root, "groups");
            groupsElement.AppendAttribute("legend", "Enclosures");
            groupsElement.AppendAttribute("use_gradient", false);
            
            XmlElement yAxisElement = xml.AppendNewChild(root, "y_axis");
            yAxisElement.AppendAttribute("legend", "CPUs");

            Node hub = model.GetNamedNode("Hub");

            int maxCpu = 0;
            
            List<string> serverNames = new List<string>();

            foreach(Node router in hub.GetChildrenWithAttributeValue("type", "Router"))
            {
                foreach (Node server in router.GetChildrenWithAttributeValue("type", "Server"))
                {
                    if (!server.GetBooleanAttribute("hidden", false) 
                        && !server.GetAttribute("name").Equals("Deimos"))
                    {
                        serverNames.Add(server.GetAttribute("name"));
                    }
                }
            }

            // Alphabetise them in case they're wanted in that order,
            // if not then just delete the sort.
            serverNames.Sort();

            foreach (string serverName in serverNames)
            {
                Node server = model.GetNamedNode(serverName);
                
                int freeCpu = server.GetIntAttribute("free_cpu", 0);
                int usedCpu = server.GetIntAttribute("used_cpu", 0);
                
                maxCpu = Math.Max(maxCpu, (freeCpu + usedCpu));

                AddGroup(groupsElement, serverName, usedCpu, freeCpu);

            }
            
            BasicXmlDocument.AppendAttribute(yAxisElement, "min", 0);

            maxCpu = (int)Maths.RoundToNiceInterval(maxCpu);
            BasicXmlDocument.AppendAttribute(yAxisElement, "max", maxCpu);

            int interval = (int) Maths.RoundToNiceInterval(maxCpu / 10.0);
            BasicXmlDocument.AppendAttribute(yAxisElement, "interval", interval);

            BasicXmlDocument.AppendAttribute(yAxisElement, "legend", "CPUs");
            BasicXmlDocument.AppendAttribute(yAxisElement, "tick_colour", Color.FromArgb(125, 238, 238, 238));

            string reportFile = gameFile.GetRoundFile(round, "CpuUsageReport.xml", GameFile.GamePhase.OPERATIONS);
            xml.Save(reportFile);


            return reportFile;
        }

        void AddCategory(XmlElement parentElement, string name, Color colour)
        {
            XmlElement category = xml.AppendNewChild(parentElement, "bar_category");

            BasicXmlDocument.AppendAttribute(category, "name", name);
            BasicXmlDocument.AppendAttribute(category, "colour", colour);
            BasicXmlDocument.AppendAttribute(category, "border_colour", Color.Black);
            BasicXmlDocument.AppendAttribute(category, "border_thickness", 1);
            BasicXmlDocument.AppendAttribute(category, "border_inset", 2);
        }

        void AddGroup(XmlElement groupsElement, string name, int usedCpuHeight, int freeCpuHeight)
        {
            XmlElement group = xml.AppendNewChild(groupsElement, "group");

            BasicXmlDocument.AppendAttribute(group, "name", name);
            BasicXmlDocument.AppendAttribute(group, "stacked", true);
            int total = usedCpuHeight + freeCpuHeight;
            AddBar(group, usedCpuHeight, "Used CPU", true, total);
            AddBar(group, freeCpuHeight, "Free CPU", true, total);
        }

        void AddBar(XmlElement group, int height, string category, bool displayValue, int total)
        {
            XmlElement bar = xml.AppendNewChild(group, "bar");
            BasicXmlDocument.AppendAttribute(bar, "category", category);
            BasicXmlDocument.AppendAttribute(bar, "height", height);
            BasicXmlDocument.AppendAttribute(bar, "display_height", displayValue);
            BasicXmlDocument.AppendAttribute(bar, "display_text", CONVERT.Format("{0}", height));
        }

    }
}
