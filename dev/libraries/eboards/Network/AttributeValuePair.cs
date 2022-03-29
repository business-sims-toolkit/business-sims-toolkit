using System;
using System.Collections;
using System.Collections.Generic;
using LibCore;

namespace Network
{
	/// <summary>
	/// Summary description for AttributeValuePair.
	/// </summary>
	public class AttributeValuePair : BaseClass, ICloneable
	{
		public AttributeValuePair() { }

		public AttributeValuePair (AttributeValuePair avp)
		{
			Attribute = avp.Attribute;
			Value = avp.Value;
		}

		public static void AddIfNotEqual(Node n, ArrayList array, string attr, int val)
		{
			AddIfNotEqual(n,array,attr, CONVERT.ToStr(val));
		}

		public static void AddIfNotEqual (Node n, ArrayList array, string attr, double val)
		{
			AddIfNotEqual(n, array, attr, CONVERT.ToStr(val));
		}

		public static void AddIfNotEqual (Node n, ArrayList array, string attr, bool val)
		{
			AddIfNotEqual(n, array, attr, CONVERT.ToStr(val));
		}

		public static void AddIfNotEqual (Node n, ArrayList array, string attr, DateTime val)
		{
			AddIfNotEqual(n, array, attr, CONVERT.ToStr(val));
		}

		public static void AddIfNotEqual (Node n, ArrayList array, string attr, string val)
		{
			if ((n == null) || (n.GetAttribute(attr) != val))
			{
				array.Add(new AttributeValuePair(attr, val));
			}
		}

		public static void AddIfNotEqual (Node n, List<AttributeValuePair> array, string attr, int val)
		{
			AddIfNotEqual(n, array, attr, CONVERT.ToStr(val));
		}

		public static void AddIfNotEqual (Node n, List<AttributeValuePair> array, string attr, double val)
		{
			AddIfNotEqual(n, array, attr, CONVERT.ToStr(val));
		}

		public static void AddIfNotEqual (Node n, List<AttributeValuePair> array, string attr, bool val)
		{
			AddIfNotEqual(n, array, attr, CONVERT.ToStr(val));
		}

		public static void AddIfNotEqual (Node n, List<AttributeValuePair> array, string attr, DateTime val)
		{
			AddIfNotEqual(n, array, attr, CONVERT.ToStr(val));
		}

		public static void AddIfNotEqual (Node n, List<AttributeValuePair> array, string attr, string val)
		{
			if ((n == null) || (n.GetAttribute(attr) != val))
			{
				array.Add(new AttributeValuePair(attr, val));
			}
		}

		public AttributeValuePair (string att, int v)
		{
			Attribute = att;
			Value = CONVERT.ToStr(v);
		}

		public AttributeValuePair(string att, double v)
		{
			Attribute = att;
			Value = CONVERT.ToStr(v);
		}

		public AttributeValuePair(string att, string v)
		{
			Attribute = att;
			Value = v;
		}

		public AttributeValuePair (string att, bool v)
		{
			Attribute = att;
			Value = CONVERT.ToStr(v).ToLower();
		}

		public AttributeValuePair (string att, DateTime v)
		{
			Attribute = att;
			Value = CONVERT.ToStr(v);
		}

		public AttributeValuePair (string att, decimal v)
		{
			Attribute = att;
			Value = CONVERT.ToStr(v);
		}

		public string Attribute;
		public string Value;
		
		public string toDataString()
		{
			string st = string.Empty;
			st = "[Attr:(" + Attribute + ")  Val:("+Value+")]";		
			return st;
		}

		public static string GetValue(string attribute, ArrayList attributes)
		{
			foreach(AttributeValuePair avp in attributes)
			{
				if(avp.Attribute == attribute)
				{
					return avp.Value;
				}
			}
			//throw(new Exception("No Such Attribute " + attribute));
			return "";
		}

		public static void RemoveAttribute(string attribute, ref ArrayList attributes)
		{
			AttributeValuePair avp = null;
			bool found = false;
			//
			for(int index=0; !found && (index < attributes.Count); ++index)
			{
				avp = (AttributeValuePair) attributes[index];
				//
				if(avp.Attribute == attribute)
				{
					found = true;
				}
			}
			//
			if(found)
			{
				attributes.Remove(avp);
			}
		}

		public override string ToString ()
		{
			return Attribute + "=\"" + Value + "\"";
		}

		public string AsString
		{
			get
			{
				return ToString();
			}
		}


		#region ICloneable Members

		public object Clone()
		{
			return new AttributeValuePair(Attribute, Value);
		}

		#endregion

		public static void AddOrReplaceInList (System.Collections.Generic.List<AttributeValuePair> attributes, AttributeValuePair attributeValuePair)
		{
			attributes.RemoveAll(avp => avp.Attribute == attributeValuePair.Attribute);
			attributes.Add(attributeValuePair);
		}
	}
}