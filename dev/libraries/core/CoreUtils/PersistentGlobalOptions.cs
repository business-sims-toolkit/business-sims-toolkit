using System;
using System.Diagnostics;
using System.IO;
using System.Xml;
using LibCore;
using Microsoft.SqlServer.Server;

namespace CoreUtils
{
    public static class PersistentGlobalOptions
    {
        public static string SpreadsheetFilename
        {
            get
            {
                return GetStringSetting("SpreadsheetFilename", null);
            }

            set
            {
                SetStringSetting("SpreadsheetFilename", value);
            }
        }

        public static string DataExportFolder
        {
            get
            {
                return GetStringSetting("DataExportFolder", null);
            }

            set
            {
                SetStringSetting("DataExportFolder", value);
            }
        }

        static string GetStringSetting (string keyName, string defaultValue)
        {
	        XmlElement element = (XmlElement) Xml.DocumentElement.SelectSingleNode(keyName);
	        if (element != null)
	        {
				return element.InnerText;
	        }
	        else
	        {
		        return defaultValue;
	        }
        }

        static void SetStringSetting (string keyName, string value)
        {
			XmlElement element = (XmlElement) Xml.DocumentElement.SelectSingleNode(keyName);
	        if (element == null)
	        {
		        element = Xml.DocumentElement.AppendNewChild(keyName);
	        }

	        element.InnerText = value;
			Save();
        }

	    static string MediaModeSettingName => "MediaMode";

	    public static MediaMode MediaMode
	    {
		    get
		    {
			    MediaMode setting = MediaMode.Windows;
			    if (! Enum.TryParse(GetStringSetting(MediaModeSettingName, ""), true, out setting))
			    {
				    setting = MediaMode.Windows;
			    }

			    return setting;
		    }

		    set
		    {
				SetStringSetting(MediaModeSettingName, value.ToString());
		    }
	    }

	    static string MultipleScreenSettingName => "MultipleDisplays";

	    public static bool UseMultipleScreens
	    {
		    get
		    {
			    bool setting = false;
			    if (! Boolean.TryParse(GetStringSetting(MultipleScreenSettingName, ""), out setting))
			    {
				    setting = false;
			    }

			    return setting;
			}

			set
		    {
			    SetStringSetting(MultipleScreenSettingName, value.ToString());
		    }
	    }

	    static string OptionsFilename
        {
            get
            {
                return AppInfo.TheInstance.Location + @"\options.xml";
            }
        }

	    static XmlDocument xml;

	    static XmlDocument Xml
	    {
		    get
		    {
			    if (xml == null)
			    {
				    if (File.Exists(OptionsFilename))
				    {
					    xml = BasicXmlDocument.CreateFromFile(OptionsFilename);
				    }
				    else
				    {
					    xml = BasicXmlDocument.Create();
					    xml.AppendNewChild("options");
				    }
			    }

			    return xml;
		    }
	    }

	    static void Save ()
	    {
		    Xml.Save(OptionsFilename);
	    }

	    static PersistentGlobalOptions ()
	    {
		    xml = null;
	    }
    }
}