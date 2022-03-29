using System;
using System.Xml;

using LibCore;

namespace CoreUtils
{
	/// <summary>
	/// Summary description for ITimedClass.
	/// </summary>
	public class XMLUtils
	{
		/// <summary>
		/// Create Element from a string
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="name"></param>
		/// <param name="contents"></param>
		/// <returns></returns>
		public static XmlElement CreateElementString(XmlNode parent, string name, string contents)
		{
			XmlDocument xdoc = parent.OwnerDocument;
			XmlElement element = CreateElement(parent, name);
			element.AppendChild(xdoc.CreateTextNode(contents));
			return element;
		}

		/// <summary>
		/// Create Element
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		public static XmlElement CreateElement(XmlNode parent, string name)
		{
			XmlDocument xdoc = parent.OwnerDocument;
			XmlElement element = xdoc.CreateElement(name);
			parent.AppendChild(element);
			return element;
		}

		public static XmlAttribute SetAttribute(XmlNode parent, string name, string val)
		{
			XmlAttribute xatt = (XmlAttribute) parent.Attributes[name];
			if(xatt == null)
			{
				XmlDocument xdoc = parent.OwnerDocument;
				xatt = xdoc.CreateAttribute(name);
				parent.Attributes.Append(xatt);
			}
			//
			xatt.Value = val;
			return xatt;
		}

		/// <summary>
		/// Create Element with bool content
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="name"></param>
		/// <param name="contents"></param>
		/// <returns></returns>
		public static XmlElement CreateElementBool(XmlNode parent, string name, bool contents)
		{
			return CreateElementString(parent, name, CONVERT.ToStr(contents) );
		}

		/// <summary>
		/// Create Element with int contents
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="name"></param>
		/// <param name="contents"></param>
		/// <returns></returns>
		public static XmlElement CreateElementInt(XmlNode parent, string name, int contents)
		{
			return CreateElementString(parent, name, CONVERT.ToStr(contents) );
		}

		/// <summary>
		/// Create Element with date time
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="name"></param>
		/// <param name="contents"></param>
		/// <returns></returns>
		public static XmlElement CreateElementDateTime(XmlNode parent, string name, DateTime contents)
		{
			return CreateElementString(parent, name, CONVERT.ToStr(contents) );
		}

		public static XmlElement CreateElementGuid (XmlNode parent, string name, Guid contents)
		{
			return CreateElementString(parent, name, contents.ToString());
		}

		public static string GetElementString(XmlNode parent, string name)
		{
			string str = parent.SelectSingleNode(name).InnerText;
			str = str.Replace("&amp;", "&");
			str = str.Replace("&lt;", "<");
			str = str.Replace("&gt;", ">");
			str = str.Replace("&apos;", "'");
			str = str.Replace("&quot;", "\"");

			return str;
		}

		public static string GetElementStringWithDefault (XmlNode parent, string name, string defaultValue)
		{
			string str = defaultValue;

			XmlNode found_node = parent.SelectSingleNode(name);
			if (found_node != null)
			{
				str = found_node.InnerText;
				str = str.Replace("&amp;", "&");
				str = str.Replace("&lt;", "<");
				str = str.Replace("&gt;", ">");
				str = str.Replace("&apos;", "'");
				str = str.Replace("&quot;", "\"");
			}
			return str;
		}

		public static string GetElementStringWithCheck(XmlNode parent, string name, out bool node_exists)
		{
			string str = "";
			node_exists = false;

			XmlNode found_node = parent.SelectSingleNode(name);
			if (found_node != null)
			{
				str = found_node.InnerText;
				str = str.Replace("&amp;", "&");
				str = str.Replace("&lt;", "<");
				str = str.Replace("&gt;", ">");
				str = str.Replace("&apos;", "'");
				str = str.Replace("&quot;", "\"");
				node_exists = true;
			}
			return str;
		}

		public static bool GetElementBool (XmlNode parent, string name)
		{
			return CONVERT.ParseBool(GetElementString(parent, name), false);
		}

		public static bool GetElementBool (XmlNode parent, string name, bool defaultValue)
		{
			bool? value = GetElementNullableBool(parent, name);

			if (value.HasValue)
			{
				return value.Value;
			}
			else
			{
				return defaultValue;
			}
		}

		public static bool? GetElementNullableBool (XmlNode parent, string name)
		{
			bool gotValue;
			string element = GetElementStringWithCheck(parent, name, out gotValue);
			if (gotValue)
			{
				return CONVERT.ParseBool(element);
			}
			else
			{
				return null;
			}
		}

		public static int GetElementInt(XmlNode parent, string name)
		{
			return CONVERT.ParseInt(GetElementString(parent, name));
		}

		public static DateTime GetElementDateTime(XmlNode parent, string name)
		{
			return CONVERT.ParseDateTime(GetElementString(parent, name));
		}

		public static Guid GetElementGuid (XmlNode parent, string name)
		{
			return new Guid (GetElementString(parent, name));
		}

		public static Guid GetElementGuid (XmlNode parent, string name, Guid defaultValue)
		{
			string element = GetElementStringWithDefault(parent, name, null);

			if (string.IsNullOrEmpty(element))
			{
				return defaultValue;
			}
			else
			{
				return new Guid (element);
			}
		}

		public static XmlElement GetOrCreateElement (XmlElement parent, string name)
		{
			XmlElement element = (XmlElement) parent.SelectSingleNode(name);

			if (element == null)
			{
				element = parent.OwnerDocument.CreateElement(name);
				parent.AppendChild(element);
			}

			return element;
		}

		public static XmlElement GetOrCreateElement (XmlDocument xml, string name)
		{
			if (xml.DocumentElement == null)
			{
				XmlElement element = xml.CreateElement(name);
				xml.AppendChild(element);
				return element;
			}
			else if (xml.DocumentElement.Name == name)
			{
				return xml.DocumentElement;
			}

			return GetOrCreateElement(xml.DocumentElement, name);
		}
	}
}