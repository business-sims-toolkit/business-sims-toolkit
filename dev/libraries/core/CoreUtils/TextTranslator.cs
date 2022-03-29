using System.Collections;
using System.Xml;
using System.IO;
using LibCore;

namespace CoreUtils
{
	/// <summary>
	/// Summary description for TextTranslator.
	/// </summary>
	public sealed class TextTranslator
	{
		/// <summary>
		/// Singleton
		/// </summary>
		public static readonly TextTranslator TheInstance = new TextTranslator();

		System.Collections.Hashtable translations = new Hashtable();
		System.Collections.Hashtable sizeAdjusts = new Hashtable();
		bool LanguageSystemAvailable = false;
		Hashtable Allowed_Languages = new Hashtable();
		string current_language_name = "english";
		string appname = ""; 

		/// <summary>
		/// 
		/// </summary>
		TextTranslator()
		{
		}

		/// <summary>
		/// Helper method to see if the 
		/// </summary>
		/// <returns></returns>
		public bool isLanguageSystemAvailable()
		{
			return LanguageSystemAvailable;
		}

		/// <summary>
		/// Used then the game selection wants to display the current choices
		/// Which may have chnaged since the application started 
		/// </summary>
		/// <returns></returns>
		public bool RefreshLanguageSystemStatus()
		{
			return DetermineLanguageSystemStatus(appname);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="applicationname"></param>
		/// <returns></returns>
		public bool DetermineLanguageSystemStatus(string applicationname)
		{
			appname = applicationname;

			string kname = "sims_lang_"+appname;
			string[] kns = null;
			bool LanguageSystemFound = false;
			LanguageSystemAvailable = false;

			//Default Situation , No Languages and "english"
			Allowed_Languages.Clear();
			current_language_name = "english";

			//Now check the existance of the registry keys 
			bool KeyExists = TheRegistry.GetRegistryLocalMachineSubKeyNames(kname, out kns);
			if (KeyExists)
			{
				//A language pack has been installed, so are there good keys
				if (kns != null)
				{
					LoadCurrentLanguage();

					//Extract out the list of languages (all languages have the form lang_xxxxxx)
					//Examples lang_german, lang_japanese etc
					bool current_language_found = false;
					foreach (string langstr in kns)
					{
						string[] names = langstr.Split('_');
						if (names.Length==2)
						{
							if (names[1]!="now")
							{
								string location = "";
								TheRegistry.GetRegistryLocalMachine_KeyValue(kname, langstr, out location);
								//only add if there is an actual location 
								if (location != null)
								{
									if (location != "")
									{
										if (File.Exists(location))
										{
											Allowed_Languages.Add(names[1], location);
											LanguageSystemFound = true; //Only true is there is a language choice
											LanguageSystemAvailable = true;

											if (names[1].ToLower() == current_language_name.ToLower())
											{
												current_language_found = true;
											}
										}
									}
								}
							}
						}
					}
					//handle the missing current language (happens when the first langpack has been installed)
					//the langpack only install the keys for thier language not the current language key
					if (current_language_found == false)
					{
						//We are missing the current language key that needs to be present. So create it 
						current_language_name = "english";
						saveCurrentLanguage(current_language_name);
					}
				}
				else
				{
					//we have a empty registry key with no defined language keys 
					//So lets add the default current language key 
					current_language_name = "english";
					saveCurrentLanguage(current_language_name);
				}
			}
			else
			{
				//No Key so no langpack installed
				current_language_name = "english";
			}
			return LanguageSystemFound;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public ArrayList getAllowed_Languages()
		{
			ArrayList al = new ArrayList();
			if (Allowed_Languages.Count>0)
			{
				foreach (string lang in Allowed_Languages.Keys)
				{
					al.Add(lang);
				}
				al.Sort();
			}
			return al;
		}

		static string GetRegistryKeyForLanguageChoice (string appName)
		{
			return @"SOFTWARE\" + appName;
		}

		void saveCurrentLanguage (string language)
		{
			using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(GetRegistryKeyForLanguageChoice(LibCore.AppInfo.TheInstance.GetApplicationName())))
			{
				key.SetValue("App_Language", language);
			}
		}

		string LoadCurrentLanguage ()
		{
			using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(GetRegistryKeyForLanguageChoice(LibCore.AppInfo.TheInstance.GetApplicationName())))
			{
				current_language_name = (string) key.GetValue("App_Language", "english");
			}

			return current_language_name;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public string getCurrentLanguage()
		{
			return current_language_name;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="current_language"></param>
		/// <returns></returns>
		public string getLanguageLocation(string current_language)
		{
			string location="";
			if (Allowed_Languages.ContainsKey(current_language))
			{
				location = (string) Allowed_Languages[current_language];
			}
			return location;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="current_language"></param>
		/// <returns></returns>
		public bool LoadLanguage(string current_language)
		{
			bool loaded_ok = false;
			if (current_language.ToLower()=="english")
			{
				ClearTranslations();
				saveCurrentLanguage("english");
				loaded_ok = true;
			}
			else
			{
				if (Allowed_Languages.ContainsKey(current_language))
				{
					string datafilename = (string) Allowed_Languages[current_language];
					if (File.Exists(datafilename))
					{
						if (LoadTranslations(datafilename))
						{
							loaded_ok = true;
						}
					}
				}
				if (loaded_ok)
				{
					saveCurrentLanguage(current_language);
				}			
			}
			return loaded_ok;
		}

		/// <summary>
		/// Have we any translations loaded 
		/// </summary>
		/// <returns></returns>
		public bool areTranslationsLoaded()
		{
			return (translations.Count>0);
		}

		/// <summary>
		/// Clear out any loaded translatiopns, back to english
		/// </summary>
		public void ClearTranslations()
		{
			translations.Clear();
			sizeAdjusts.Clear();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="node"></param>
		/// 
		void AddSizeAdjustment(XmlNode node)
		{
			XmlElement adjname = (XmlElement) node.SelectSingleNode("name");
			XmlElement adjsize = (XmlElement) node.SelectSingleNode("adjust");
			//
			if( (adjname != null) && (adjsize != null) )
			{
				string str_adjname = adjname.InnerText;
				string str_adjsize = adjsize.InnerText;
				double dbl_adjsize = CONVERT.ParseDouble(str_adjsize);
				//
				if(!sizeAdjusts.ContainsKey(str_adjname))
				{
					sizeAdjusts[str_adjname] = dbl_adjsize;
				}
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="node"></param>
		void AddTranslation(XmlNode node)
		{
			XmlElement from = (XmlElement) node.SelectSingleNode("from");
			XmlElement to = (XmlElement) node.SelectSingleNode("to");
			//
			if( (from != null) && (to != null) )
			{
				string str_from = from.InnerText.Replace("\\r\\n","\r\n");
				string str_to = to.InnerText.Replace("\\r\\n","\r\n");
				//
				if(!translations.ContainsKey(str_from))
				{
					translations[str_from] = str_to;
				}
			}
		}

		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="translations_file">Full path name including drive</param>
		/// <returns></returns>
		public bool LoadTranslations(string translations_file)
		{
			bool loaded = false; 
			translations.Clear();

			string xfile = translations_file;
			XmlDocument xdoc = new XmlDocument();
			System.IO.StreamReader file = new System.IO.StreamReader(xfile);
			string xmldata = file.ReadToEnd();
			file.Close();
			file = null;
			//
			xdoc.LoadXml(xmldata);
			//
			foreach(XmlNode xnode in xdoc.DocumentElement.ChildNodes)
			{
				//Handle all the translations
				if(xnode.Name == "size_adjustments")
				{
					foreach(XmlNode xnode2 in xnode.ChildNodes)
					{
						if(xnode2.Name == "size_adjustment")
						{
							AddSizeAdjustment(xnode2);
							loaded=true;
						}
					}
				}

				//Handle all the translations
				if(xnode.Name == "translations")
				{
					foreach(XmlNode xnode3 in xnode.ChildNodes)
					{
						if(xnode3.Name == "r")
						{
							AddTranslation(xnode3);
						}
					}
				}				
			}
			return loaded;
		}

		public string GetTranslateFont(string font)
		{
			if(translations.Keys.Count > 0)
			{
				return "Arial Unicode MS";
			}
			else
			{
				return font;
			}
		}

		public float GetTranslateFontSize(string font, float font_size)
		{
			if(translations.Keys.Count > 0)
			{
				//if(font == "Verdana") return font_size + 1;
				return font_size;
			}
			else
			{
				return font_size;
			}
		}

		public float GetTranslateFontSizeForName(string name, string font, float font_size)
		{
			float new_font_size = GetTranslateFontSize(font, font_size);

			if (sizeAdjusts.ContainsKey(name))
			{
				double adjust = (double)sizeAdjusts[name];
				new_font_size = new_font_size + (float)adjust;
			}
			return new_font_size;
		}


		public string Translate(string text)
		{
			string rtext = text;

            if (!string.IsNullOrEmpty(rtext))
            {
                //
                int max_size_found = 0;
                string replace_from = "";
                string replace_to = "";
                //
                foreach (string from in translations.Keys)
                {
                    if (text.IndexOf(from) > -1)
                    {
                        if (from.Length > max_size_found)
                        {
                            max_size_found = from.Length;
                            replace_from = from;
                            replace_to = (string)translations[replace_from];
                        }
                    }
                }
                //
                if (max_size_found > 0)
                {
                    rtext = text.Replace(replace_from, replace_to);
                }
                //
            }

			return rtext;
		}
	}
}
