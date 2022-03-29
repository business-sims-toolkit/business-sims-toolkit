using System;
using System.IO;
using System.Collections.Generic;
using System.Xml;

using LibCore;

namespace Logging
{
	public class LogLineFoundDef : BaseClass
	{
		public delegate void LineFoundHandler(object sender, string key, string line, double time);
		public event LineFoundHandler LogLineFound;

		// List used to optimise event management performance
		public List<LineFoundHandler> handlers = new List<LineFoundHandler>();

		public void CallLogLineFound(object sender, string key, string line, double time)
		{
			LogLineFound(sender,key,line,time);
		}
	}

	/// <summary>
	/// The BasicIncidentLogReader reads a BasicIncidentLogger output file.
	/// </summary>
	public class BasicIncidentLogReader : IDisposable
	{
		Dictionary<string, LogLineFoundDef> CreatedNodesWatchers = new Dictionary<string, LogLineFoundDef> ();
		Dictionary<string, LogLineFoundDef> ApplyWatchers = new Dictionary<string, LogLineFoundDef> ();
		Dictionary<string, LogLineFoundDef> MovedNodesWatchers = new Dictionary<string, LogLineFoundDef> ();
		Dictionary<string, LogLineFoundDef> DeletedNodesWatchers = new Dictionary<string, LogLineFoundDef> ();

		string logFile;

		double lastTimeFound = 0;

		public double LastTimeFound
		{
			get
			{
				return lastTimeFound;
			}
		}

		public BasicIncidentLogReader(string fileName)
		{
			logFile = fileName;
		}

		public void WatchCreatedNodes (string parentNode, LogLineFoundDef.LineFoundHandler handler)
		{
			LogLineFoundDef llfd = null;
			if (!CreatedNodesWatchers.TryGetValue(parentNode, out llfd))
			{
                llfd = new LogLineFoundDef();
                CreatedNodesWatchers[parentNode] = llfd;
            }
			if (!llfd.handlers.Contains(handler))
			{
				llfd.LogLineFound += handler;
				llfd.handlers.Add(handler);
			}
		}

		public void WatchMovedNodes(string newParentNode, LogLineFoundDef.LineFoundHandler handler)
		{
			LogLineFoundDef llfd = null;
			if (!MovedNodesWatchers.TryGetValue(newParentNode, out llfd))
			{
				llfd = new LogLineFoundDef();
				MovedNodesWatchers[newParentNode] = llfd;
			}
			if (!llfd.handlers.Contains(handler))
			{
				llfd.LogLineFound += handler;
				llfd.handlers.Add(handler);
			}
		}

		public void WatchDeletedNodes(string deletedNode, LogLineFoundDef.LineFoundHandler handler)
		{
			LogLineFoundDef llfd = null;
			if (!DeletedNodesWatchers.TryGetValue(deletedNode, out llfd))
			{
				llfd = new LogLineFoundDef ();
				DeletedNodesWatchers[deletedNode] = llfd;
			}
			if (!llfd.handlers.Contains(handler))
			{
				llfd.LogLineFound += handler;
				llfd.handlers.Add(handler);
			}
		}

		public void WatchApplyAttributes(string parentNode, LogLineFoundDef.LineFoundHandler handler)
		{
			LogLineFoundDef llfd = null;
			if (!ApplyWatchers.TryGetValue(parentNode, out llfd))
		    {
			    llfd = new LogLineFoundDef();
			    ApplyWatchers[parentNode] = llfd;
            }
			if (!llfd.handlers.Contains(handler))
			{
				llfd.LogLineFound += handler;
				llfd.handlers.Add(handler);
			}
        }

		public void Run ()
		{
			if (File.Exists(logFile))
			{
				using (FileStream stream = new FileStream (logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				{
					using (StreamReader reader = new StreamReader (stream))
					{
						try
						{
							BasicXmlDocument xml = BasicXmlDocument.Create("<log>" + reader.ReadToEnd() + "</log>");

							foreach (XmlElement logLine in xml.DocumentElement.ChildNodes)
							{
								ProcessLogLine(logLine);
							}
						}
						catch (XmlException)
						{
							// Reset the stream reader.
							reader.DiscardBufferedData();
							reader.BaseStream.Seek(0, SeekOrigin.Begin);
							reader.BaseStream.Position = 0;

							string line;

							do
							{
								line = reader.ReadLine();

								if (line != null)
								{
									ProcessLine(line);
								}
							}
							while (! reader.EndOfStream);
						}
					}
				}
			}
		}

        struct WatchParameters
        {
            public string ParentNode;
            public LogLineFoundDef.LineFoundHandler Handler;

            public WatchParameters (string parentNode, LogLineFoundDef.LineFoundHandler handler)
            {
                ParentNode = parentNode;
                Handler = handler;
            }
        }

		void ProcessLogLine (XmlElement element)
		{
			currentLogElement = element;
			currentLine = element.OuterXml;

			DateTime watchStart = DateTime.Now;
			
			switch (element.Name)
			{
				case "logStarted":
					break;

				case "i":
					currentCommand = (XmlElement)element.ChildNodes[0];
					string line = element.OuterXml;

					double time = BasicXmlDocument.GetIntAttribute(currentCommand, "i_doAfterSecs", 0);
					Dictionary<string, LogLineFoundDef> handlers = null;
					string nodeName = "";

					lastTimeFound = time;

					switch (currentCommand.Name)
					{
						case "apply":
							nodeName = BasicXmlDocument.GetStringAttribute(currentCommand, "i_name");
							handlers = ApplyWatchers;
							break;

						case "createNodes":
							nodeName = BasicXmlDocument.GetStringAttribute(currentCommand, "i_to");
							handlers = CreatedNodesWatchers;
							break;

						case "delete":
						case "deleteNode":
						case "deleteNodes":
							nodeName = BasicXmlDocument.GetStringAttribute(currentCommand, "i_name");
							handlers = DeletedNodesWatchers;
							break;

						case "addNodes":
							nodeName = BasicXmlDocument.GetStringAttribute(currentCommand, "i_to");
							handlers = MovedNodesWatchers;
							break;

						default:
							System.Diagnostics.Debug.Assert(false);
							break;
					}

					if (!string.IsNullOrEmpty(nodeName))
					{
						if (handlers.ContainsKey(nodeName))
						{
							handlers[nodeName].CallLogLineFound(this, nodeName, line, time);
						}
					}
					if (handlers.ContainsKey(""))
					{
						handlers[""].CallLogLineFound(this, "", line, time);
					}
					break;
			}
		}

		void ProcessLine(string line)
		{
			currentLogElement = null;
			currentLine = line;

			string val = ExtractValue(line, "i_doAfterSecs");
			if ("" == val) return;
			double seconds = CONVERT.ParseDouble(val);
			lastTimeFound = seconds;

			string name = ExtractValue(line, "i_name");
			char[] charLine= line.ToCharArray();
			string firstchar = charLine[1].ToString();
			switch (firstchar)
			{
				case "L":
					break;

				case "i":
					//XmlElement command = (XmlElement)element.ChildNodes[0];
					//string line = element.OuterXml;

					double time = CONVERT.ParseDouble(ExtractValue(line, "i_doAfterSecs"));
					Dictionary<string, LogLineFoundDef> handlers = null;
					string nodeName = "";
					string when = ExtractValue(line, "id");

					int secondOpen = line.IndexOf("><");
					int counter = 2;
					string commandName = "";
					while (charLine[secondOpen + counter] != ' ')
					{
						commandName += charLine[secondOpen + counter];
						counter++;
					}


					lastTimeFound = time;



					switch (commandName)
					{
						case "apply":
							nodeName = ExtractValue(line, "i_name");
							handlers = ApplyWatchers;
							break;

						case "createNodes":
							nodeName = ExtractValue(line, "i_to");
							handlers = CreatedNodesWatchers;
							break;

						case "delete":
						case "deleteNode":
						case "deleteNodes":
							nodeName = ExtractValue(line, "i_name");
							handlers = DeletedNodesWatchers;
							break;

						case "addNodes":
							nodeName = ExtractValue(line, "i_to");
							handlers = MovedNodesWatchers;
							break;

						default:
							System.Diagnostics.Debug.Assert(false);
							break;
					}

					if (!string.IsNullOrEmpty(nodeName))
					{
						if (handlers.ContainsKey(nodeName))
						{
							handlers[nodeName].CallLogLineFound(this, nodeName, line, time);
						}
					}
					if (handlers.ContainsKey(""))
					{
						handlers[""].CallLogLineFound(this, "", line, time);
					}
					break;
			}
		}


		public static bool ExtractValue(string line, string element, string key, out string ret)
		{
			return ExtractValue(line, element, key, false, out ret);
		}

		public static bool ExtractValue(string line, string element, string key, bool skip_start, out string ret)
		{
			// <i id="AtStart"> is 16 chars long so skip past these.
			int off = 0;
			if(skip_start) off = GetSkippableBit(line);

			ret = "";

			// Bind search to just the element we are interested in...
			int startElementPos = line.IndexOf("<" + element, off);
			if(-1 != startElementPos)
			{
				int endElementPos = line.IndexOf(">",startElementPos);
				if(-1 != endElementPos)
				{
					string subLine = line.Substring(startElementPos, endElementPos - startElementPos);
					ret = ExtractValue(subLine, key);
					return true;
				}
			}

			return false;
		}

		public static string ExtractValue(string line, string element, string key)
		{
			return ExtractValue(line, element, key, false);
		}

		public static string ExtractValue(string line, string element, string key, bool skip_start)
		{
			// <i id="AtStart"> is 16 chars long so skip past these.
			int off = 0;
			if(skip_start) off = GetSkippableBit(line);
			// Bind search to just the element we are interested in...
			int startElementPos = line.IndexOf("<" + element, off);
			if(-1 != startElementPos)
			{
				int endElementPos = line.IndexOf(">",startElementPos);
				if(-1 != endElementPos)
				{
					string subLine = line.Substring(startElementPos, endElementPos - startElementPos);
					return ExtractValue(subLine, key);
				}
			}

			return "";
		}

		public static bool ExtractValue(string line, string nkey, out string val)
		{
			string key = " " + nkey;

			val = "";
			int secsSplitPoint = line.IndexOf(key+"=\"");
			if(-1 == secsSplitPoint) return false;
			secsSplitPoint += key.Length+2;
			int secsStrPoint = line.IndexOf("\"",secsSplitPoint);
			if(-1 == secsStrPoint) return false;
			val = line.Substring( secsSplitPoint, secsStrPoint-secsSplitPoint);
			return true;
		}

		public static string ExtractValue(string line, string nkey)
		{
			return ExtractValue(line, nkey, false);
		}

        public static int? ExtractIntValue(string line, string nkey)
        {
            string value = ExtractValue(line, nkey);

            if (!string.IsNullOrEmpty(value))
            {
                return CONVERT.ParseInt(value);
            }

            return null;
        }

        public static bool? ExtractBoolValue(string line, string nkey)
        {
            string value = ExtractValue(line, nkey);

            if (!string.IsNullOrEmpty(value))
            {
                return CONVERT.ParseBool(value);
            }

            return null;
        }

        public static double? ExtractDoubleValue(string line, string nkey)
        {
            string value = ExtractValue(line, nkey);

            if (!string.IsNullOrEmpty(value))
            {
                return CONVERT.ParseDouble(value);
            }

            return null;
        }

		public static string ExtractValue(string line, string nkey, bool skip_start)
		{
			int off = 0;
			if(skip_start) off = GetSkippableBit(line);
			string key = " " + nkey;

			string val = "";
			int secsSplitPoint = line.IndexOf(key+"=\"", off);
			if(-1 == secsSplitPoint) return "";
			secsSplitPoint += key.Length+2;
			int secsStrPoint = line.IndexOf("\"",secsSplitPoint);
			if(-1 == secsStrPoint) return "";
			val = line.Substring( secsSplitPoint, secsStrPoint-secsSplitPoint);
			return BaseUtils.xml_utils.TranslateFromEscapedXMLChars(val);
		}

		public static string ExtractValueGivenDefault (string line, string nkey, string defaultValue)
		{
			int off = 0;
			string key = " " + nkey;

			string val = "";
			int secsSplitPoint = line.IndexOf(key + "=\"", off);
			if (-1 == secsSplitPoint) return defaultValue;
			secsSplitPoint += key.Length + 2;
			int secsStrPoint = line.IndexOf("\"", secsSplitPoint);
			if (-1 == secsStrPoint) return defaultValue;
			val = line.Substring(secsSplitPoint, secsStrPoint - secsSplitPoint);
			return val;
		}

		public static string ExtractLastValue (string line, string nkey)
		{
			return ExtractLastValue(line, nkey, false);
		}

		public static string ExtractLastValue (string line, string nkey, bool skip_start)
		{
			string key = " " + nkey;

			string val = "";
			int secsSplitPoint = line.LastIndexOf(key + "=\"");
			if (-1 == secsSplitPoint) return "";
			secsSplitPoint += key.Length + 2;
			int secsStrPoint = line.IndexOf("\"", secsSplitPoint);
			if (-1 == secsStrPoint) return "";
			val = line.Substring(secsSplitPoint, secsStrPoint - secsSplitPoint);
			return val;
		}

		// : Hardcoding the length of <i id="AtStart"> didn't work because some lines
		// had other guff on them first.
		static int GetSkippableBit (string line)
		{
			string skippableBit = "\"AtStart\">";
			if (line.IndexOf(skippableBit) != -1)
			{
				return line.IndexOf(skippableBit) + skippableBit.Length;
			}

			return 0;
		}

		public XmlElement currentLogElement;
		public XmlElement currentCommand;
		public int currentCPUs = 0;

		string currentLine;

		public void Dispose ()
		{
			ApplyWatchers.Clear();
			CreatedNodesWatchers.Clear();
			DeletedNodesWatchers.Clear();
			MovedNodesWatchers.Clear();
		}
	}
}