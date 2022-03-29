using System;
using System.Xml;
using System.IO;

namespace LibCore
{
	public class BasicXmlDocument : XmlDocument
	{
		protected BasicXmlDocument()
		{
		}

		public static BasicXmlDocument Create()
		{
			BasicXmlDocument xdoc = new BasicXmlDocument ();
			return xdoc;
		}

		public static BasicXmlDocument Create(string data)
		{
			BasicXmlDocument xdoc = new BasicXmlDocument ();
			xdoc.LoadXml(data);
			return xdoc;
		}

		public static BasicXmlDocument CreateFromFile (string filename)
		{
			BasicXmlDocument xdoc = new BasicXmlDocument ();

			if (File.Exists(filename))
			{
				var xml = BasicXmlDocument.Create();
				xml.LoadXml(File.ReadAllText(filename));
				return xml;
			}
			else
			{
				return null;
			}
		}

		public static BasicXmlDocument LoadOrCreate (string filename)
		{
			if (File.Exists(filename))
			{
				return CreateFromFile(filename);
			}

			return Create();
		}

		public static XmlAttribute GetAttribute (XmlNode node, string attributeName)
		{
		    if (node.Attributes != null)
		    {
		        foreach (XmlAttribute attribute in node.Attributes)
		        {
		            if (attribute.Name == attributeName)
		            {
		                return attribute;
		            }
		        }
		    }

		    return null;
		}

	    public static string GetStringAttribute (XmlNode node, string attributeName)
		{
			return GetStringAttribute(node, attributeName, "");
		}

		public static string GetStringAttribute (XmlNode node, string attributeName, string defaultValue)
		{
			XmlAttribute attribute = GetAttribute(node, attributeName);

			if (attribute != null)
			{
				return attribute.Value;
			}

			return defaultValue;
		}

		public static int? GetIntAttribute (XmlNode node, string attributeName)
		{
			XmlAttribute attribute = GetAttribute(node, attributeName);

			if (attribute != null)
			{
				return CONVERT.ParseIntSafe(attribute.Value, default (int));
			}

			return null;
		}

		public static int GetIntAttribute (XmlNode node, string attributeName, int defaultValue)
		{
			XmlAttribute attribute = GetAttribute(node, attributeName);

			if (attribute != null)
			{
				return CONVERT.ParseIntSafe(attribute.Value, defaultValue);
			}

			return defaultValue;
		}

		public static double GetDoubleAttribute (XmlNode node, string attributeName, double defaultValue)
		{
			XmlAttribute attribute = GetAttribute(node, attributeName);

			if (attribute != null)
			{
				return CONVERT.ParseDoubleSafe(attribute.Value, defaultValue);
			}

			return defaultValue;
		}

		public static double? GetDoubleAttribute (XmlNode node, string attributeName)
		{
			XmlAttribute attribute = GetAttribute(node, attributeName);

			if (attribute != null)
			{
				return CONVERT.ParseDouble(attribute.Value);
			}

			return null;
		}

		public static float GetFloatAttribute (XmlNode node, string attributeName, float defaultValue)
        {
            XmlAttribute attribute = GetAttribute(node, attributeName);

            if (attribute != null)
            {
                return CONVERT.ParseFloatSafe(attribute.Value, defaultValue);
            }
            return defaultValue;
        }

	    public static float? GetFloatAttribute (XmlNode node, string attributeName)
	    {
	        var attribute = GetAttribute(node, attributeName);

	        if (attribute != null)
	        {
	            return CONVERT.ParseFloatSafe(attribute.Value, default (float));
	        }

	        return null;
	    }

		public static DateTime? GetNullableDateTimeAttribute (XmlElement node, string attributeName)
		{
			XmlAttribute attribute = GetAttribute(node, attributeName);

			if (attribute != null)
			{
				return CONVERT.ParseDateTime(attribute.Value);
			}

			return null;
		}

		public static DateTime GetDateTimeAttribute (XmlElement node, string attributeName, DateTime defaultValue)
		{
			XmlAttribute attribute = GetAttribute(node, attributeName);

			if (attribute != null)
			{
				return CONVERT.ParseDateTime(attribute.Value);
			}

			return defaultValue;
		}

		public static DateTime? GetNullableDateAttribute (XmlElement node, string attributeName)
		{
			XmlAttribute attribute = GetAttribute(node, attributeName);

			if (attribute != null)
			{
				return CONVERT.ParseDate(attribute.Value);
			}

			return null;
		}

		public static DateTime GetDateAttribute (XmlElement node, string attributeName, DateTime defaultValue)
		{
			XmlAttribute attribute = GetAttribute(node, attributeName);

			if (attribute != null)
			{
				return CONVERT.ParseDate(attribute.Value);
			}

			return defaultValue;
		}

		public static TimeSpan? GetNullableTimeAttribute (XmlElement node, string attributeName)
		{
			XmlAttribute attribute = GetAttribute(node, attributeName);

			if (attribute != null)
			{
				return new TimeSpan (0, 0, CONVERT.ParseHmsToSeconds(attribute.Value));
			}

			return null;
		}

		public static TimeSpan GetTimeAttribute (XmlElement node, string attributeName, TimeSpan defaultValue)
		{
			XmlAttribute attribute = GetAttribute(node, attributeName);

			if (attribute != null)
			{
				return new TimeSpan (0, 0, CONVERT.ParseHmsToSeconds(attribute.Value));
			}

			return defaultValue;
		}

		public static bool? GetNullableBoolAttribute (XmlElement node, string attributeName)
		{
			XmlAttribute attribute = GetAttribute(node, attributeName);

			if (attribute != null)
			{
				return CONVERT.ParseBool(attribute.Value);
			}

			return null;
		}

		public static bool GetBoolAttribute (XmlElement node, string attributeName, bool defaultValue)
		{
			XmlAttribute attribute = GetAttribute(node, attributeName);

			if (attribute != null)
			{
				return CONVERT.ParseBool(attribute.Value, defaultValue);
			}

			return defaultValue;
		}

		public static bool? GetBoolAttribute (XmlElement node, string attributeName)
		{
			XmlAttribute attribute = GetAttribute(node, attributeName);

			if (attribute != null)
			{
				return CONVERT.ParseBool(attribute.Value);
			}

			return null;
		}

		public static System.Drawing.Color GetColourAttribute (XmlNode node, string attributeName, System.Drawing.Color defaultValue)
		{
			XmlAttribute attribute = GetAttribute(node, attributeName);

			if (attribute != null)
			{
				return CONVERT.ParseColor(attribute.Value, defaultValue);
			}

			return defaultValue;
		}

		public static Guid GetGuidAttribute (XmlElement node, string attributeName)
		{
			return new Guid (GetStringAttribute(node, attributeName));
		}

		public static XmlNode GetNamedChild (XmlNode parent, string elementName)
		{
			foreach (XmlNode child in parent.ChildNodes)
			{
				if (child.Name == elementName)
				{
					return child;
				}
			}

			return null;
		}

		public static T GetEnumAttribute<T> (XmlNode node, string name, T defaultValue) where T : struct, IConvertible
		{
			if (! typeof (T).IsEnum)
			{
				throw new ArgumentException("T must be an enumerated type");
			}

			var valueStr = GetStringAttribute(node, name);

			return Enum.TryParse(valueStr, true, out T value) ? value : defaultValue;
		}

		public void SaveToURL(string url, string filename)
		{
			using (XmlTextWriter writer = new XmlTextWriter(filename, null))
			{
				writer.Formatting = Formatting.Indented;
				Save(writer);
			}
		}

		public static string GetElementContent (XmlNode node)
		{
			return node.InnerText;
		}	

		public static void AppendAttribute (XmlNode n, XmlAttribute att)
		{
			n.Attributes.Append(att);
		}

		public static void AppendAttribute (XmlNode n, string name, string value)
		{
			XmlAttribute attribute = null;

			foreach (XmlAttribute existingAttribute in n.Attributes)
			{
				if (existingAttribute.Name == name)
				{
					attribute = existingAttribute;
					break;
				}
			}

			if (attribute == null)
			{
				attribute = n.OwnerDocument.CreateAttribute(name);
				AppendAttribute(n, attribute);
			}

			attribute.Value = value;
		}

		public static void AppendAttribute (XmlNode n, string name, bool value)
		{
			AppendAttribute(n, name, CONVERT.ToStr(value));
		}

		public static void AppendAttribute (XmlNode n, string name, System.Drawing.Color value)
		{
			AppendAttribute(n, name, CONVERT.ToComponentStr(value));
		}

		public static void AppendAttribute (XmlNode n, string name, double value)
		{
			AppendAttribute(n, name, CONVERT.ToStr(value));
		}

		public static void AppendAttribute (XmlNode n, string name, int value)
		{
			AppendAttribute(n, name, CONVERT.ToStr(value));
		}

		public static void AppendAttribute (XmlNode n, string name, Guid value)
		{
			AppendAttribute(n, name, value.ToString());
		}
		
		public XmlElement AppendNewChild (string name)
		{
			if (DocumentElement == null)
			{
				XmlElement child = CreateElement(name, NamespaceURI);
				AppendChild(child);

				return child;
			}
			else
			{
				return DocumentElement.AppendNewChild(name);
			}
		}

		public XmlElement AppendNewChild (XmlNode parent, string name)
		{
			XmlDocument xmlDocument = parent.OwnerDocument;
			if (parent is XmlDocument)
			{
				xmlDocument = (XmlDocument) parent;
			}

			XmlElement child = xmlDocument.CreateElement(name, parent.NamespaceURI);
			parent.AppendChild(child);

			return child;
		}

		
	}
}