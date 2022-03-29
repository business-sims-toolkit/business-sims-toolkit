using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace Parsing
{
	public static class Xml
	{
		public static XmlDocument GetOwnerOrSelf (this XmlNode node)
		{
			if (node is XmlDocument)
			{
				return (XmlDocument) node;
			}
			else
			{
				return node.OwnerDocument;
			}
		}

		public static XmlElement AppendNewChild (this XmlNode parent, string name, string content = null)
		{
			var element = parent.GetOwnerOrSelf().CreateElement(name);
			parent.AppendChild(element);
			if (content != null)
			{
				element.InnerText = content;
			}

			return element;
		}

		public static XmlAttribute AppendAttribute (this XmlNode parent, string name, string value)
		{
			var attribute = parent.GetOwnerOrSelf().CreateAttribute(name);
			attribute.Value = value;
			parent.Attributes.Append(attribute);

			return attribute;
		}

		public static XmlAttribute AppendAttribute (this XmlNode parent, string name, int? value)
		{
			if (value != null)
			{
				return parent.AppendAttribute(name, Conversion.ToString(value.Value));
			}
			else
			{
				return null;
			}
		}
		public static XmlAttribute AppendAttribute (this XmlNode parent, string name, bool? value)
		{
			if (value != null)
			{
				return parent.AppendAttribute(name, Conversion.ToString(value.Value));
			}
			else
			{
				return null;
			}
		}

		public static XmlAttribute AppendAttribute (this XmlNode parent, string name, DateTime? value)
		{
			if (value != null)
			{
				return parent.AppendAttribute(name, Conversion.ToString(value.Value));
			}
			else
			{
				return null;
			}
		}

		public static XmlAttribute AppendAttribute (this XmlNode parent, string name, Guid? value)
		{
			if (value != null)
			{
				return parent.AppendAttribute(name, value.Value.ToString());
			}
			else
			{
				return null;
			}
		}

		public static string GetAttribute (this XmlNode parent, string name)
		{
			return parent.Attributes[name]?.Value;
		}

		public static int? GetIntAttribute (this XmlNode parent, string name)
		{
			var attribute = parent.Attributes[name];
			if (attribute != null)
			{
				return Conversion.ParseInt(attribute.Value);
			}

			return null;
		}

		public static int GetIntAttribute (this XmlNode parent, string name, int defaultValue)
		{
			return parent.GetIntAttribute(name) ?? defaultValue;
		}

		public static bool? GetBoolAttribute (this XmlNode parent, string name)
		{
			var attribute = parent.Attributes[name];
			if (attribute != null)
			{
				return Conversion.ParseBool(attribute.Value);
			}

			return null;
		}

		public static bool GetBoolAttribute (this XmlNode parent, string name, bool defaultValue)
		{
			return parent.GetBoolAttribute(name) ?? defaultValue;
		}

		public static DateTime? GetDateTimeAttribute (this XmlNode parent, string name)
		{
			var attribute = parent.Attributes[name];
			if (attribute != null)
			{
				return Conversion.ParseDateTime(attribute.Value);
			}

			return null;
		}

		public static DateTime GetDateTimeAttribute (this XmlNode parent, string name, DateTime defaultValue)
		{
			return parent.GetDateTimeAttribute(name) ?? defaultValue;
		}

		public static Guid? GetGuidAttribute (this XmlNode parent, string name)
		{
			var attribute = parent.Attributes[name];
			if (attribute != null)
			{
				return Guid.Parse(attribute.Value);
			}

			return null;
		}

		public static Guid GetGuidAttribute (this XmlNode parent, string name, Guid defaultValue)
		{
			return parent.GetGuidAttribute(name) ?? defaultValue;
		}

		public static XmlDocument LoadDocument (string filename)
		{
			var xml = new XmlDocument ();
			xml.Load(filename);

			return xml;
		}

		public static XmlDocument LoadDocumentFromString (string xmlString)
		{
			var xml = new XmlDocument ();
			xml.LoadXml(xmlString);

			return xml;
		}
	}
}