using System;
using System.Xml;

namespace LibCore
{
	public static class XmlDocumentExtensions
	{
		public static XmlElement AppendNewChild (this XmlDocument parent, string name)
		{
			if (parent.DocumentElement == null)
			{
				XmlElement root = parent.CreateElement(name);
				parent.AppendChild(root);

				return root;
			}
			else
			{
				return parent.DocumentElement.AppendNewChild(name);
			}
		}

		public static XmlElement AppendNewChild (this XmlElement parent, string name)
		{
			string nameSpace = "";

			if (parent.ParentNode != null)
			{
				nameSpace = parent.ParentNode.NamespaceURI;
			}
			else if (parent.OwnerDocument.DocumentElement != null)
			{
				nameSpace = parent.OwnerDocument.DocumentElement.NamespaceURI;
			}

			XmlElement child = parent.OwnerDocument.CreateElement(name, nameSpace);
			parent.AppendChild(child);

			return child;
		}

		public static XmlAttribute AppendAttribute (this XmlDocument parent, string attributeName, string attributeValue)
		{
			return parent.DocumentElement.AppendAttribute(attributeName, attributeValue);
		}

		public static XmlAttribute AppendAttribute (this XmlElement parent, string attributeName, string attributeValue)
		{
			XmlAttribute attribute = null;

			foreach (XmlAttribute existingAttribute in parent.Attributes)
			{
				if (existingAttribute.Name == attributeName)
				{
					attribute = existingAttribute;
					break;
				}
			}

			if (attribute == null)
			{
				attribute = parent.OwnerDocument.CreateAttribute(attributeName);
				parent.Attributes.Append(attribute);
			}

			attribute.Value = attributeValue;

			return attribute;
		}

		public static XmlAttribute AppendAttribute (this XmlElement parent, string name, bool value)
		{
			return parent.AppendAttribute(name, CONVERT.ToStr(value));
		}

		public static XmlAttribute AppendAttribute (this XmlElement parent, string name, double value)
		{
			return parent.AppendAttribute(name, CONVERT.ToStr(value));
		}

		public static XmlAttribute AppendAttribute (this XmlElement parent, string name, int value)
		{
			return parent.AppendAttribute(name, CONVERT.ToStr(value));
		}

		public static XmlAttribute AppendAttribute (this XmlElement parent, string name, decimal value)
		{
			return parent.AppendAttribute(name, CONVERT.ToStr(value));
		}

		public static XmlAttribute AppendAttribute (this XmlElement parent, string name, Guid value)
		{
			return parent.AppendAttribute(name, value.ToString());
		}

		public static XmlAttribute AppendAttribute (this XmlElement parent, string name, DateTime value)
		{
			return parent.AppendAttribute(name, CONVERT.ToStr(value));
		}

		public static XmlAttribute AppendAttribute (this XmlElement parent, string name, System.Drawing.Color value)
		{
			return parent.AppendAttribute(name, CONVERT.ToComponentStr(value));
		}

		public static XmlAttribute AppendDateAttribute (this XmlElement parent, string name, DateTime value)
		{
			return parent.AppendAttribute(name, CONVERT.ToDateStr(value));
		}

		public static XmlAttribute AppendTimeAttribute (this XmlElement parent, string name, TimeSpan value)
		{
			return parent.AppendAttribute(name, CONVERT.ToHmsFromSeconds((int) value.TotalSeconds));
		}

		public static int GetIntAttribute (this XmlElement parent, string name, int defaultValue)
		{
			return BasicXmlDocument.GetIntAttribute(parent, name, defaultValue);
		}

		public static int? GetIntAttribute(this XmlElement parent, string name)
		{
			return BasicXmlDocument.GetIntAttribute(parent, name);
		}

		public static double GetDoubleAttribute (this XmlElement parent, string name, double defaultValue)
		{
			return BasicXmlDocument.GetDoubleAttribute(parent, name, defaultValue);
		}

		public static double? GetDoubleAttribute (this XmlElement parent, string name)
		{
			return BasicXmlDocument.GetDoubleAttribute(parent, name);
		}

		public static DateTime GetDateTimeAttribute (this XmlElement parent, string name, DateTime defaultValue)
		{
			return BasicXmlDocument.GetDateTimeAttribute(parent, name, defaultValue);
		}

		public static DateTime GetDateAttribute (this XmlElement parent, string name, DateTime defaultValue)
		{
			return BasicXmlDocument.GetDateAttribute(parent, name, defaultValue);
		}

		public static TimeSpan GetTimeAttribute (this XmlElement parent, string name, TimeSpan defaultValue)
		{
			return BasicXmlDocument.GetTimeAttribute(parent, name, defaultValue);
		}

		public static TimeSpan? GetTimeAttribute (this XmlElement parent, string name)
		{
			return BasicXmlDocument.GetNullableTimeAttribute(parent, name);
		}

		public static bool GetBooleanAttribute (this XmlElement parent, string name, bool defaultValue)
		{
			return BasicXmlDocument.GetBoolAttribute(parent, name, defaultValue);
		}
		public static bool? GetBooleanAttribute (this XmlElement parent, string name)
		{
			return BasicXmlDocument.GetBoolAttribute(parent, name);
		}

		public static string GetStringAttribute (this XmlElement parent, string name, string defaultValue)
		{
			return BasicXmlDocument.GetStringAttribute(parent, name, defaultValue);
		}

		static void RemoveNamespaceRecursively (XmlElement element)
		{
            element.RemoveAttribute("xmlns");

			foreach (XmlNode child in element.ChildNodes)
			{
				XmlElement childElement = child as XmlElement;
				if (childElement != null)
                {
                    RemoveNamespaceRecursively(childElement);
				}
			}
		}

		public static XmlElement AppendNewChildGivenXml (this XmlElement parent, string xml)
		{
			XmlDocument childDoc = new XmlDocument ();
			childDoc.LoadXml(xml);

            RemoveNamespaceRecursively(childDoc.DocumentElement);

			return (XmlElement) parent.AppendChild(parent.OwnerDocument.ImportNode(childDoc.DocumentElement, true));
		}
	}
}