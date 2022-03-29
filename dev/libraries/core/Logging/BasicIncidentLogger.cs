using System;
using System.IO;
using System.Collections;

using LibCore;
using Network;
using CoreUtils;

namespace Logging
{
	/// <summary>
	/// A BasicLogger logs all changes to a Network Tree
	/// into a log file that can be reloaded into an event
	/// delayer and replayed as required.
	/// </summary>
	public class BasicIncidentLogger : BaseClass, ITimedClass, IPostNotifiedTimedClass, IDisposable
	{
		NodeTree _tree = null;
		string logFileName = "";

		Hashtable ignoreTypes = new Hashtable();
		Hashtable ignoreAttributes = new Hashtable();
		Hashtable ignoreTargets = new Hashtable();
		Hashtable ignoreMovingItemsByName = new Hashtable();

		DateTime started = DateTime.Now;

		bool writeLogLinesImmediately;

		Node currentTimeNode;
		int currentSeconds = 0;
		int? timeOverride;

		bool ignoreNamedMovingItems = false;
		bool ignoreUnNamedDeletions = false;

		public bool KeepLogOpen
		{
			get
			{
				return false;
			}

			set
			{
			}
		}

		public void Dispose()
		{
			CoreUtils.TimeManager.TheInstance.UnmanageClass(this);

			if(null != _tree)
			{
				_tree.TreeChanged -= _tree_TreeChanged;
				_tree.NodeAdded -= _tree_NodeAdded;
				_tree.NodeMoved -= _tree_NodeMoved;
				_tree = null;
			}
		}

		public void LogTreeToFile(NodeTree t, string file, bool writeLogLinesImmediately = false)
		{
			logFileName = file;

			if(_tree != null)
			{
				if (this.writeLogLinesImmediately)
				{
					_tree.EarlyTreeChanged -= _tree_TreeChanged;
				}
				else
				{
					_tree.TreeChanged -= _tree_TreeChanged;
				}

				_tree.NodeAdded -= _tree_NodeAdded;
				_tree.NodeMoved -= _tree_NodeMoved;

				if(currentTimeNode != null)
				{
					currentTimeNode.AttributesChanged -= currentTimeNode_AttributesChanged;
					currentTimeNode = null;
				}
			}

			_tree = t;
			this.writeLogLinesImmediately = writeLogLinesImmediately;

			if (writeLogLinesImmediately)
			{
				_tree.EarlyTreeChanged += _tree_TreeChanged;
			}
			else
			{
				_tree.TreeChanged += _tree_TreeChanged;
			}
			_tree.NodeAdded += _tree_NodeAdded;
			_tree.NodeMoved += _tree_NodeMoved;

			currentTimeNode = _tree.GetNamedNode("CurrentTime");
			currentTimeNode.AttributesChanged += currentTimeNode_AttributesChanged;
			currentSeconds = currentTimeNode.GetIntAttribute("seconds",0);
			//
			started = DateTime.Now;
			//
			WriteLine("<logStarted time=\"" + CONVERT.ToStr(started) + "\"/>");
		}

		public void SetIgnoreUnameDeletions(bool new_value_IgnoreUnNamedDeletions)
		{
			this.ignoreUnNamedDeletions = new_value_IgnoreUnNamedDeletions;
		}

		public BasicIncidentLogger(bool IgnoreMoving)
		{
			CoreUtils.TimeManager.TheInstance.ManageClass(this);
			handleBaseIgnores();

			string dataDir = LibCore.AppInfo.TheInstance.Location + "\\data\\";
			ignoreNamedMovingItems = true;
			LoadIgnores(ignoreMovingItemsByName, dataDir + "ignore_movingtargets.txt");
		}

		public BasicIncidentLogger()
		{
			CoreUtils.TimeManager.TheInstance.ManageClass(this);
			handleBaseIgnores();
		}

		void handleBaseIgnores()
		{
			// Do we have any ignore definitions?
			string dataDir = LibCore.AppInfo.TheInstance.Location + "\\data\\";
			//
			LoadIgnores(ignoreTypes, dataDir + "ignore_types.txt");
			LoadIgnores(ignoreAttributes, dataDir + "ignore_atts.txt");
			LoadIgnores(ignoreTargets, dataDir + "ignore_targets.txt");
		}

		void LoadIgnores(Hashtable ignoreTable, string file)
		{
			if(File.Exists(file))
			{
				using (StreamReader SR = new StreamReader(file))
				{
					string S;
					S = SR.ReadLine();
					while (S != null)
					{
						string str = S.Trim();
						if (str.Length > 0)
						{
							if (str.IndexOf(",") > 0)
							{
								char[] comma = { ',' };
								string[] sep = str.Split(comma);
								ignoreTable.Add(sep[0], sep[1]);
							}
							else
							{
								ignoreTable.Add(str, "_");
							}
						}
						S = SR.ReadLine();
					}
				}
			}
		}

		void _tree_TreeChanged(object sender, NodeTreeEventArgs args)
		{
			currentSeconds = currentTimeNode.GetIntAttribute("seconds", 0);
			int time = timeOverride ?? currentSeconds;

			DateTime now = DateTime.Now;
			TimeSpan ts = now-started;

			string affected_node_type = "";
			if (args.NodeAffected != null)
			{
				affected_node_type = args.NodeAffected.GetAttribute("type");
				if (affected_node_type != "")
				{
					if (this.ignoreTypes.ContainsKey(affected_node_type))
					{
						// We ignore these.
						return;
					}
				}
			}

			switch(args.TypeOfEvent)
			{
				case NodeTreeEventArgs.EventType.ArgsChanged:
				{
					string name = args.NodeAffected.GetAttribute("name");
					// Is this a target to be ignored?...
					if(ignoreTargets.ContainsKey(name))
					{
						// We ignore these.
						return;
					}
					// See if we can chuch any attributes away...
					ArrayList allowedAttrs;
					if(ignoreAttributes.Count > 0)
					{
						allowedAttrs = new ArrayList();
						foreach(AttributeValuePair avp in args.AttributesChanged)
						{
							if(!ignoreAttributes.ContainsKey(avp.Attribute))
							{
								// We don't ignore this one...
								allowedAttrs.Add(avp);
							}
							else
							{
								string _val = (string) ignoreAttributes[avp.Attribute];
								if(_val != "_")
								{
									// We shouldn't ignore this attribute if it matches the value defined...
									if( (_val == avp.Value) || (avp.Value == "") || (avp.Value == "0") )
									{
										allowedAttrs.Add(avp);
									}
								}
							}
						}
						//
						if(allowedAttrs.Count == 0)
						{
							return;
						}
					}
					else
					{
						allowedAttrs = args.AttributesChanged;
					}

					allowedAttrs = RemoveDuplicates(allowedAttrs);

					string val = "<i id=\"AtStart\"><apply i_name=\"" + BaseUtils.xml_utils.TranslateToEscapedXMLChars(name) + "\" i_doAfterSecs=\"" + CONVERT.ToStr(time) + "\"";
					foreach(AttributeValuePair avp in allowedAttrs)
					{
						val += " " + avp.Attribute + "=\"" + BaseUtils.xml_utils.TranslateToEscapedXMLChars(avp.Value.Replace("\r\n","\\r\\n")) + "\"";
					}
					val += "/></i>\r\n";
					WriteLine(val);

					string val2 = "<i id=\"AtStart\"><apply i_name=\"" + BaseUtils.xml_utils.TranslateToEscapedXMLChars(name) + "\" i_doAfterSecs=\"" + CONVERT.ToStr(time) + "\"";
					foreach(AttributeValuePair avp in args.AttributesChanged)
					{
						val2 += " " + avp.Attribute + "=\"" + BaseUtils.xml_utils.TranslateToEscapedXMLChars(avp.Value.Replace("\r\n","\\r\\n")) + "\"";
					}
					val2 += "/></i>\r\n";
					AppLogger.TheInstance.GWrite(val2);
				}
					break;

				case NodeTreeEventArgs.EventType.Deleted:
				{
					// If the node being deleted has no name then we have to log its ID instead.
					string name = args.NodeAffected.GetAttribute("name");
					if("" != name)
					{
						string str = "<i id=\"AtStart\"><delete i_name=\"" + BaseUtils.xml_utils.TranslateToEscapedXMLChars(name) + "\" i_doAfterSecs=\"" + CONVERT.ToStr(time) + "\"/></i>\r\n";
						WriteLine(str);
						AppLogger.TheInstance.GWrite(str);
					}
					else
					{
						if (ignoreUnNamedDeletions == false)
						{
							string str = "<i id=\"AtStart\"><delete i_name=\"UUID_" + CONVERT.ToStr(args.NodeAffected.ID) + "\" i_doAfterSecs=\"" + CONVERT.ToStr(time) + "\"/></i>\r\n";
							WriteLine(str);
							AppLogger.TheInstance.GWrite(str);
						}
					}
				}
					break;
			}
		}

		ArrayList RemoveDuplicates(ArrayList allowedAttrs)
		{
			ArrayList AVPList = new ArrayList();
			ArrayList AttributeList = new ArrayList();


			foreach (AttributeValuePair AVP1 in allowedAttrs)
			{
				if (!AttributeList.Contains(AVP1.Attribute))
				{

					AVPList.Add(AVP1);
					AttributeList.Add(AVP1.Attribute);

				}
			}

			//foreach (int removal in AVPList)
			//{

			//    allowedAttrs.RemoveAt(removal);
			//}


			
			return AVPList;

		}
		// TODO : This gets called even if a new node has just been moved in the tree! (Does it? Check!)
		// This should be changed so that it only gets called if a new node is added to
		// the tree. A new event "moved node" should be created and called.
		void _tree_NodeAdded(NodeTree sender, Node newNode)
		{
			currentSeconds = currentTimeNode.GetIntAttribute("seconds", 0);
			int time = timeOverride ?? currentSeconds;

			// TODO : Need a test that creates a new node in the tree!
			DateTime now = DateTime.Now;
			TimeSpan ts = now-started;
			string pname = newNode.Parent.GetAttribute("name");

			//
			string str = "<i id=\"AtStart\"><createNodes i_to=\"" + BaseUtils.xml_utils.TranslateToEscapedXMLChars(pname) + "\" i_doAfterSecs=\"" + CONVERT.ToStr(time) + "\">";
			str += "<" + BaseUtils.xml_utils.TranslateToEscapedXMLChars(string.IsNullOrEmpty(newNode.Type) ? "node" : newNode.Type.Replace(" ", "_"));

			//
			// If the node doesn't have a name store its UUID in the file as this will help track it...
			//
			bool named = false;
			//
			foreach(string key in newNode.AttributesAsStringDictionary.Keys)
			{
				string val = newNode.GetAttribute(key);
				str += " " + key + "=\"" + BaseUtils.xml_utils.TranslateToEscapedXMLChars(val.Replace("\r\n","\\r\\n")) + "\"";
				if( (key == "name") && (val != "") ) named = true;
			}

			if(!named)
			{
				str += " uuid=\"" + CONVERT.ToStr(newNode.ID) + "\"";
			}
			str += "/></createNodes></i>\r\n";
			WriteLine(str);

			AppLogger.TheInstance.GWrite(str);
		}

		void _tree_NodeMoved(NodeTree sender, Node oldParent, Node movedNode)
		{
			currentSeconds = currentTimeNode.GetIntAttribute("seconds", 0);
			int time = timeOverride ?? currentSeconds;

			bool process = true;

			if (ignoreNamedMovingItems)
			{
				//we can ignore 
				string name = movedNode.GetAttribute("name");
				if (ignoreMovingItemsByName.Contains(name))
				{
					process = false;
				}
			}

			if (process)
			{
				Node parentNode = movedNode.Parent;
				DateTime now = DateTime.Now;
				TimeSpan ts = now-started;
				string str;

				if (parentNode.GetBooleanAttribute("log_nodes_moved_in_as_creation", false))
				{
					str = "<i id=\"AtStart\"><createNodes i_to=\"" + BaseUtils.xml_utils.TranslateToEscapedXMLChars(parentNode.GetAttribute("name")) + "\" i_doAfterSecs=\"" + CONVERT.ToStr(time) + "\">" + movedNode.toXmlString(true) + "</createNodes></i>\r\n";
				}
				else
				{
					str = "<i id=\"AtStart\"><addNodes i_to=\"" + BaseUtils.xml_utils.TranslateToEscapedXMLChars(parentNode.GetAttribute("name")) + "\" i_doAfterSecs=\"" + CONVERT.ToStr(time) + "\"><node i_name=\"" + BaseUtils.xml_utils.TranslateToEscapedXMLChars(movedNode.GetAttribute("name")) + "\"/></addNodes></i>\r\n";
				}
				WriteLine(str);

				AppLogger.TheInstance.GWrite(str);
			}
		}

		public bool OpenLog()
		{
			string [] dirs = Path.GetDirectoryName(logFileName).Split('\\');
			string dir = "";
			if (dirs.Length > 1)
			{
				dir = dirs[dirs.Length - 1];
			}

			string file = Path.GetFileName(logFileName);

			AppLogger.TheInstance.CreateLog(dir + "_" + file);

			return true;
		}

		public void CloseLog ()
		{
			AppLogger.TheInstance.Close();
		}

		#region ITimedClass Members
		/// <summary>
		/// Starting the logger should re-attach it to the log file if we are currently detatched.
		/// </summary>
		public void Start()
		{
		}

		public void BeforeStart()
		{
			WriteLine("<logStarted time=\"" + CONVERT.ToStr(DateTime.Now) + "\"/>");
		}

		/// <summary>
		/// Stopping the logger detaches it from the log file.
		/// </summary>
		public void Stop()
		{
		}

		public void AfterStop()
		{
			WriteLine("<logStopped time=\"" + CONVERT.ToStr(DateTime.Now) + "\"/>");
		}

		public void Reset()
		{
			lock(this)
			{
				started = DateTime.Now;

				File.WriteAllText(logFileName, string.Empty);
			}
		}

		public void FastForward(double timesRealTime)
		{
		}

		#endregion

		void currentTimeNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			foreach (AttributeValuePair avp in attrs)
			{
				if (avp.Attribute == "seconds")
				{
					currentSeconds = CONVERT.ParseInt(avp.Value);
				}
			}
		}

		public void OverrideTime (int currentTime)
		{
			timeOverride = currentTime;
		}

		public void RemoveTimeOverride ()
		{
			timeOverride = null;
		}

		void WriteLine (string format, params object [] arguments)
		{
			using (StreamWriter writer = File.AppendText(logFileName))
			{
				writer.Write(format, arguments);
				writer.Flush();
			}
		}
	}
}