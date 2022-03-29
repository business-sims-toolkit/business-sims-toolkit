using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using LibCore;
using Network;

namespace GameEngine
{
	public class AnalyticsLogger : IDisposable
	{
		NodeTree model;
		int round;
		int frequency;
		Node analyticsNode;
		Node timeNode;

		class Analytic
		{
			public string Name { get; }
			public string NodeName { get; }
			public string AttributeName { get; }

			public Analytic (string name, string nodeName, string attributeName)
			{
				Name = name;
				NodeName = nodeName;
				AttributeName = attributeName;
			}
		}

		List<Analytic> analytics;

		public AnalyticsLogger (NodeTree model, int round)
		{
			this.model = model;
			this.round = round;

			timeNode = model.GetNamedNode("CurrentTime");
			timeNode.AttributesChanged += timeNode_AttributesChanged;

			var rootAnalyticsNode = model.GetNamedNode("Analytics");
			if (rootAnalyticsNode == null)
			{
				rootAnalyticsNode = new Node (model.Root, "Analytics", "Analytics", new AttributeValuePair ("type", "analytics"));
				AddSystemAnalytics(rootAnalyticsNode);
			}

			analyticsNode = model.GetNamedNode($"Analytics_Round{round}");
			if (analyticsNode == null)
			{
				analyticsNode = new Node (rootAnalyticsNode, "Analytics", $"Analytics_Round{round}", new AttributeValuePair ("type", "analytics"));
			}
			analyticsNode.DeleteChildren();

			analytics = new List<Analytic> ();

			var xml = BasicXmlDocument.CreateFromFile(AppInfo.TheInstance.Location + @"\data\analytics.xml");
			frequency = xml.DocumentElement.GetIntAttribute("frequency_secs", 60);

			foreach (XmlElement entry in xml.DocumentElement.ChildNodes)
			{
				Watch(entry.GetStringAttribute("name", null), entry.GetStringAttribute("node", null), entry.GetStringAttribute("attribute", null));
			}
		}

		void AddSystemAnalytics (Node rootAnalyticsNode)
		{
			var generalAnalyticsNode = new Node (rootAnalyticsNode, "Analytics", "Analytics_General", new AttributeValuePair ("type", "analytics"));

			AddEntry(generalAnalyticsNode, "OS", System.Environment.OSVersion.ToString());
		}

		void AddEntry (Node parent, string description, string value)
		{
			new Node (parent, "AnalyticsEntry", "", new List<AttributeValuePair>
			{
				new AttributeValuePair ("type", "analytic_entry"),
				new AttributeValuePair ("entry_name", description),
				new AttributeValuePair ("value", value)
			});
		}

		public void Dispose ()
		{
			timeNode.AttributesChanged -= timeNode_AttributesChanged;
		}

		public void Watch (string name, string nodeName, string attributeName)
		{
			nodeName = nodeName.Replace("{round}", CONVERT.ToStr(round));
			attributeName = attributeName.Replace("{round}", CONVERT.ToStr(round));

			analytics.Add(new Analytic (name, nodeName, attributeName));
		}

		void timeNode_AttributesChanged (Node sender, ArrayList attributes)
		{
			if ((timeNode.GetIntAttribute("seconds", 0) % frequency) == 0)
			{
				LogAnalytics();
			}
		}

		public void LogAnalytics ()
		{
			var time = timeNode.GetIntAttribute("seconds", 0);

			var entryName = $"AnalyticsTimeEntry_{time}";
			var entry = model.GetNamedNode(entryName);
			if (entry != null)
			{
				entry.Parent.DeleteChildTree(entry);
			}

			entry = new Node(analyticsNode, "AnalyticsTimeEntry", entryName, new List<AttributeValuePair>
			{
				new AttributeValuePair ("type", "analytics_time_entry"),
				new AttributeValuePair ("time", time)
			});

			foreach (var analytic in analytics)
			{
				new Node (entry, "AnalyticsEntry", "", new List<AttributeValuePair>
				{
					new AttributeValuePair ("type", "analytics_entry"),
					new AttributeValuePair ("entry_name", analytic.Name),
					new AttributeValuePair ("value", model.GetNamedNode(analytic.NodeName).GetDoubleAttribute(analytic.AttributeName, 0))
				});
			}
		}
	}
}