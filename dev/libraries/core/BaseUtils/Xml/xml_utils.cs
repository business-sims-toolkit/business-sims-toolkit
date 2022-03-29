using System;
using System.Xml;

namespace BaseUtils
{
	/// <summary>
	/// Summary description for xml_utils.
	/// </summary>
	public class xml_utils
	{
		static System.Globalization.CultureInfo myCI_enGB = new System.Globalization.CultureInfo( "en-GB", false );
		
		public xml_utils()
		{
		}

		public static Boolean DoesFieldNameExist(string FieldName, ref XmlNode xn)	
		{
			XmlNode selectednode = null;
			selectednode = xn.SelectSingleNode(FieldName);
			if (selectednode != null)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		public static string TranslateToEscapedXMLChars(string oldstr)
		{
			string newstr = string.Empty;

			if (oldstr != null)
			{
				newstr = oldstr.Replace(">","&gt;");
				newstr = newstr.Replace("<","&lt;");
				newstr = newstr.Replace("&","&amp;");
				newstr = newstr.Replace("%","&#37;");
			}
			return newstr;
		}

		public static string TranslateFromEscapedXMLChars(string oldstr)
		{
			string newstr = string.Empty;

			if (oldstr != null)
			{
				newstr = oldstr.Replace("&gt;",">");
				newstr = newstr.Replace("&lt;","<");
				newstr = newstr.Replace("&amp;","&");
				newstr = newstr.Replace("&#37;","%");
			}
			return newstr;
		}

		public static Boolean extractBoolean(string FieldName, ref XmlNode xn, Boolean FailValue, 
			ref int ErrCount, out string ErrMsg)	
		{
			string datavaluestr = string.Empty;
			Boolean datavalue = FailValue;
			XmlNode selectednode = null;
			ErrMsg = string.Empty;

			selectednode = xn.SelectSingleNode(FieldName);
			if (selectednode != null)
			{
				datavaluestr = selectednode.InnerText;
				if (datavaluestr.ToLower() == "true")
				{
					datavalue = true;
				}
				else
				{
					datavalue = false;
				}
			}
			else
			{
				ErrCount++;
				ErrMsg+="[Missing Parameter ("+FieldName+")]";
			}
			return datavalue;
		}





		public static string extractStr(string FieldName, ref XmlNode xn, string FailValue, 
			ref int ErrCount, out string ErrMsg)	
		{
			string datavalue = FailValue;
			XmlNode selectednode = null;
			ErrMsg = string.Empty;

			selectednode = xn.SelectSingleNode(FieldName);
			if (selectednode != null)
			{
				datavalue = selectednode.InnerText;
			}
			else
			{
				ErrCount++;
				ErrMsg+="[Missing Parameter ("+FieldName+")]";
			}
			return datavalue;
		}

		public static string extractXMLStr(string FieldName, ref XmlNode xn, string FailValue, 
			ref int ErrCount, out string ErrMsg)	
		{
			string datavalue = FailValue;
			XmlNode selectednode = null;
			ErrMsg = string.Empty;

			selectednode = xn.SelectSingleNode(FieldName);
			if (selectednode != null)
			{
				datavalue = selectednode.InnerXml;
			}
			else
			{
				ErrCount++;
				ErrMsg+="[Missing Parameter ("+FieldName+")]";
			}
			return datavalue;
		}


		public static float extractFloat(string FieldName, ref XmlNode xn, float FailValue, 
			ref int ErrCount, out string ErrMsg)	
		{
			float datavalue = FailValue;
			XmlNode selectednode = null;
			ErrMsg = string.Empty;

			try
			{
				selectednode = xn.SelectSingleNode(FieldName);
				if (selectednode != null)
				{
					datavalue = float.Parse(selectednode.InnerText,myCI_enGB.NumberFormat);
				}
				else
				{
					ErrCount++;
					ErrMsg="[Missing Parameter ("+FieldName+")]";
				}
			}
			catch (Exception evc)
			{
				ErrCount++;
				ErrMsg="[Exception ("+FieldName+")" + evc.Message + "]";
			}
			return datavalue;
		}

		public static double extractDouble(string FieldName, ref XmlNode xn, double FailValue, 
			ref int ErrCount, out string ErrMsg)	
		{
			double datavalue = FailValue;
			XmlNode selectednode = null;
			ErrMsg = string.Empty;

			try
			{
				selectednode = xn.SelectSingleNode(FieldName);
				if (selectednode != null)
				{
					datavalue = double.Parse(selectednode.InnerText,myCI_enGB.NumberFormat);
				}
				else
				{
					ErrCount++;
					ErrMsg="[Missing Parameter ("+FieldName+")]";
				}
			}
			catch (Exception evc)
			{
				ErrCount++;
				ErrMsg="[Exception ("+FieldName+")" + evc.Message + "]";
			}
			return datavalue;
		}

		public static int extractInt(string FieldName, ref XmlNode xn, int FailValue, 
			ref int ErrCount, out string ErrMsg)	
		{
			int datavalue = FailValue;
			XmlNode selectednode = null;
			ErrMsg = string.Empty;

			try
			{
				selectednode = xn.SelectSingleNode(FieldName);
				if (selectednode != null)
				{
					datavalue = int.Parse(selectednode.InnerText,myCI_enGB.NumberFormat);
				}
				else
				{
					ErrCount++;
					ErrMsg="[Missing Parameter ("+FieldName+")]";
				}
			}
			catch (Exception evc)
			{
				ErrCount++;
				ErrMsg="[Exception ("+FieldName+")" + evc.Message + "]";
			}
			return datavalue;
		}

		public static long extractLong(string FieldName, ref XmlNode xn, long FailValue, 
			ref int ErrCount, out string ErrMsg)	
		{
			long datavalue = FailValue;
			XmlNode selectednode = null;
			ErrMsg = string.Empty;

			try
			{
				selectednode = xn.SelectSingleNode(FieldName);
				if (selectednode != null)
				{
					datavalue = long.Parse(selectednode.InnerText,myCI_enGB.NumberFormat);
				}
				else
				{
					ErrCount++;
					ErrMsg="[Missing Parameter ("+FieldName+")]";
				}
			}
			catch (Exception evc)
			{
				ErrCount++;
				ErrMsg="[Exception ("+FieldName+")" + evc.Message + "]";
			}
			return datavalue;
		}
	}
}
