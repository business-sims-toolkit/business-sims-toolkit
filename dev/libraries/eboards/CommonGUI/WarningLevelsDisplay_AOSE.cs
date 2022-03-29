using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;

using System.Xml;

using LibCore;
using Network;

using CoreUtils;

namespace CommonGUI
{
	/// <summary>
	/// This has 5 Groups on the top and 4 on the bottom 
	///  Top Row (1 * Routers and 4 * Servers) Bottom Row (4 * Apps)
	///  
	/// 
	/// </summary>
	public class WarningLevelsDisplay_AOSE : FlickerFreePanel, ITimedClass
	{
		protected Random rng = new Random ();
		protected bool running = false;

		protected Random random;
		protected ArrayList monitors_activity;
		protected int xoffset = 0;
		protected int yoffset = 2;
		protected int numInGroup = 0;
		protected int numTotal = 0;
		protected int numGroups = 0;
		protected Size size = new Size(8,50);
		protected Size numSize = new Size(8,12);
		protected int numInDisplayRows = 3;

		protected int GroupVerticalSep = 90;
		protected int GroupTextVerticalSep = 20;

		protected Font font = SkinningDefs.TheInstance.GetFont(8);
		protected Font sFont = SkinningDefs.TheInstance.GetFont(12);
		protected Font powerFont = SkinningDefs.TheInstance.GetFont(9.5f, FontStyle.Bold);

		protected NodeTree model;
		protected Node MyAWT_SystemNode = null;
		protected Boolean MyAWT_SystemFailure = false;
		protected Hashtable KnownMirrorSlots = new Hashtable();
		protected Hashtable LocatToName = new Hashtable();

		protected Hashtable nameToActivityMonitor = new Hashtable();
		protected Hashtable nameToTempMonitor = new Hashtable();
		protected Color BackgroundColor = Color.Black;
		protected Color TitleForeColor = Color.White;
		protected Color TitleBackColor = Color.White;
		protected Color EmptyStatusColor = Color.DarkGray;
		protected int current_round = 1;

		protected double [] randomZoneTemperatureFluctuation = new double [7];

		#region Constructor and Dispose

		// Old constructor, for including all monitor groups.
		public WarningLevelsDisplay_AOSE(NodeTree _model, int round, Boolean UseCompactMode, 
			Color newBackColor, Color newTitleForeColor, Color newTitleBackColor, Color newEmptyStatusColor)
			: this(_model, round, UseCompactMode, newBackColor, newTitleForeColor,
			newTitleBackColor, newEmptyStatusColor, -1, -1, -1)
		{
		}

		// Only include the groups in the range [startGroupIndex, startGroupIndex + groupCount],
		// starting from index 0 (the first one specified in the XML).
		// If startGroupIndex is -1, include all.  If groupCount is -1, include all after startGroupIndex.
		public WarningLevelsDisplay_AOSE(NodeTree _model, int round, Boolean UseCompactMode, 
			Color newBackColor, Color newTitleForeColor, Color newTitleBackColor, Color newEmptyStatusColor, 
			int startGroupIndex, int groupCount, int monitorHeight)
		{
			model = _model;
			current_round = round;

			monitors_activity = new ArrayList();
			random = new Random(1);
			BackgroundColor = newBackColor;
			TitleForeColor = newTitleForeColor;
			TitleBackColor = newTitleBackColor;
			BackColor = BackgroundColor;
			EmptyStatusColor = newEmptyStatusColor;

			if (UseCompactMode)
			{
				yoffset = 2;					
				GroupVerticalSep = 90;
				GroupTextVerticalSep = 15;
			}
			else
			{
				yoffset = 10;
				GroupVerticalSep = 90;
				GroupTextVerticalSep = 20;
			}

			if (monitorHeight != -1)
			{
				size.Height = monitorHeight;
			}


			string def_file_name = "\\data\\monitor_items_r" + CONVERT.ToStr(current_round) + ".xml";

			//Load the monitor_items.xml file and connect the displays to the correct audio style monitors.
			XmlDocument xdoc = new XmlDocument();
			xdoc.Load( AppInfo.TheInstance.Location + def_file_name);

			//Connect up to app that provides the AWT service
			MyAWT_SystemNode = _model.GetNamedNode("AdvancedWarningTechnology");
			MyAWT_SystemNode.AttributesChanged +=MyAWT_SystemNode_AttributesChanged;
			MyAWT_SystemFailure = ! (MyAWT_SystemNode.GetBooleanAttribute("up",false));

			//Connect up to incoming Requests
			int groupIndex = 0;
			foreach(XmlNode xnode in xdoc.DocumentElement.ChildNodes)
			{
				if(xnode.NodeType == XmlNodeType.Element)
				{
					if(xnode.Name == "group")
					{
						// Is this a group we're interested in?
						// (startGroupIndex == -1) => include all groups
						if ((startGroupIndex == -1)
							// or are we on or after the start of the interesting range...
							|| ((groupIndex >= startGroupIndex) &&
							// and are we on or before the end of the interesting range
							// (groupCount == -1) => include all groups after the start
							((groupCount == -1) || (groupIndex < (startGroupIndex + groupCount)))))
						{
							// Add this group to the display...
							AddGroup(xnode);
						}
						groupIndex++;
					}
				}
			}

			//need to monitor empty slots that change into Monitored Servers, This only occurs for the 2 Mirror Servers 
			//we assume that all mirror nodes have a location code MXXX rather than monitor all the slots 
			//there is only 2 mirror available
			ArrayList types = new ArrayList();
			Hashtable ht = new Hashtable();
			types.Clear();
			types.Add("Slot");
			ht = _model.GetNodesOfAttribTypes(types);
			foreach(Node MirrorSlotNode in ht.Keys)
			{
				string namestr = MirrorSlotNode.GetAttribute("name");
				string locationstr = MirrorSlotNode.GetAttribute("location");
				//System.Diagnostics.Debug.WriteLine(" ## LC : "+locationstr + "   " + namestr);
				if (locationstr.ToLower().IndexOf("m")>-1)
				{
					//System.Diagnostics.Debug.WriteLine(" ###### LC : "+locationstr + "   " + namestr);
					KnownMirrorSlots.Add(MirrorSlotNode, locationstr);
					MirrorSlotNode.AttributesChanged +=MirrorSlotNode_AttributesChanged;
				}
			}
			//check for any node being attached to the network 
			model.NodeAdded += model_NodeAdded;
			TimeManager.TheInstance.ManageClass(this);
		}

		protected override void Dispose(bool disposing)
		{
			if(disposing)
			{
				if (MyAWT_SystemNode != null)
				{
					MyAWT_SystemNode.AttributesChanged -=MyAWT_SystemNode_AttributesChanged;
				}

				foreach(BouncyNodeMonitor monitor in monitors_activity)
				{
					monitor.Dispose();
				}
			
				foreach(Node kms in KnownMirrorSlots.Keys)
				{
					kms.AttributesChanged -= MirrorSlotNode_AttributesChanged;
				}
				KnownMirrorSlots.Clear();

				model.NodeAdded -= model_NodeAdded;
				monitors_activity.Clear();
				TimeManager.TheInstance.UnmanageClass(this);
			}
			base.Dispose (disposing);
		}

		#endregion Constructor and Dispose
				
		#region Misc Methods
		
		public Random GetRandom()
		{
			return random;
		}

		protected void CreateLabel(int _x, int _y, string text, Font sFont)
		{
			Label servers1 = new Label();
			servers1.Font = sFont;
			servers1.ForeColor = TitleForeColor;
			servers1.BackColor = TitleBackColor;
			servers1.Text = text;
			servers1.TextAlign = ContentAlignment.MiddleCenter;
			servers1.Size = new Size(72,14);
			servers1.Location = new Point(_x,_y);
			Controls.Add(servers1);
		}

		protected void AddGroup(XmlNode xnode)
		{
			numInGroup = 0;
			bool istype_activity = false;

			foreach(XmlNode n in xnode.ChildNodes)
			{
				if(n.NodeType == XmlNodeType.Element)
				{
					if(n.Name == "type")
					{
						istype_activity = false;
						if (n.InnerText.ToLower()=="activity")
						{
							istype_activity = true;
						}
					}

					if(n.Name == "name")
					{
						xoffset += 5;
						//
						if ((numGroups != 0) && (numGroups % numInDisplayRows == 0))
						{
							// Move onto a new row...
							xoffset = 5;
							yoffset += GroupVerticalSep;
							numInGroup = 0;
						}
						//
						CreateLabel(xoffset,yoffset,n.InnerXml, sFont);
						//
						++numGroups;
					}
					else if(n.Name == "monitor")
					{
						++numInGroup;
						//
						Node modelNode = model.GetNamedNode( n.InnerXml );

						if (istype_activity)
						{
							//We need a BouncyNodeMonitor for displaying server activity 
							BouncyNodeMonitor monitor = new BouncyNodeMonitor(modelNode, random);
							nameToActivityMonitor.Add(n.InnerXml, monitor);
							monitor.Location = new Point(xoffset,yoffset+GroupTextVerticalSep);
							monitor.Size = size;
							Controls.Add(monitor);
							monitors_activity.Add(monitor);

							monitor.SetOverrideFailure(MyAWT_SystemFailure);
							monitor.SetEmptyColor(EmptyStatusColor);

							Label number  = new Label();
							number.Text = CONVERT.ToStr(numInGroup);
							number.Font = font;
							number.Size = numSize;
							number.ForeColor = Color.White;
							number.ForeColor = TitleForeColor;
							number.BackColor = TitleBackColor;
							number.Location = new Point(xoffset,yoffset + GroupTextVerticalSep + size.Height);
							Controls.Add(number);
							xoffset += size.Width + 2;
						}
					}
				}
			}
		}

		#endregion Misc Methods 

		#region Handling Node added

		/// <summary>
		/// Handling a changes to a node that we are monitoring 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="newNode"></param>
		void model_NodeAdded(NodeTree sender, Node newNode)
		{
			// This may be a node that we want to be monitoring...
			string name = newNode.GetAttribute("name");
			
			// Check for Activity Monitoring Nodes
			if(nameToActivityMonitor.ContainsKey(name))
			{
				BouncyNodeMonitor oldMonitormodel = (BouncyNodeMonitor) nameToActivityMonitor[name];
				nameToActivityMonitor.Remove(name);
				monitors_activity.Remove(oldMonitormodel);

				BouncyNodeMonitor monitor = new BouncyNodeMonitor(newNode, random);
				
				nameToActivityMonitor.Add(name, monitor );
				monitor.Location = oldMonitormodel.Location;
				monitor.Size = oldMonitormodel.Size;

				Controls.Add(monitor);
				monitors_activity.Add(monitor);
				monitor.SetOverrideFailure(MyAWT_SystemFailure);
				monitor.SetEmptyColor(EmptyStatusColor);
				oldMonitormodel.Dispose();
			}
			
		}

		#endregion Handling Node added

		protected void OverideMonitors(Boolean Flag)
		{
			if (monitors_activity.Count>0)
			{
				foreach (BouncyNodeMonitor bnm in monitors_activity)
				{
					bnm.SetOverrideFailure(Flag);
				}
			}
		}

		void MyAWT_SystemNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			foreach(AttributeValuePair avp in attrs)
			{
				if(avp.Attribute == "up")
				{
					//If we are up then the there is no failure
					MyAWT_SystemFailure = ! (MyAWT_SystemNode.GetBooleanAttribute("up",false));
					OverideMonitors(MyAWT_SystemFailure);
				}
			}
		}

		void MirrorSlotNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			foreach(AttributeValuePair avp in attrs)
			{
				if(avp.Attribute == "name")
				{
					string namestr = sender.GetAttribute("name");
					string locationstr = sender.GetAttribute("location");

					//process for activity monitors 
					if (nameToActivityMonitor.ContainsKey(namestr))
					{
						//adding in a new Mirror 
						LocatToName.Add(locationstr,namestr);
						BouncyNodeMonitor oldMonitormodel = (BouncyNodeMonitor) nameToActivityMonitor[namestr];
						nameToActivityMonitor.Remove(namestr);
						monitors_activity.Remove(oldMonitormodel);

						BouncyNodeMonitor monitor = new BouncyNodeMonitor(sender, random);
				
						nameToActivityMonitor.Add(namestr, monitor );
						monitor.Location = oldMonitormodel.Location;
						monitor.Size = oldMonitormodel.Size;
						Controls.Add(monitor);
						monitors_activity.Add(monitor);
						monitor.SetOverrideFailure(MyAWT_SystemFailure);
						monitor.SetEmptyColor(EmptyStatusColor);
						oldMonitormodel.Dispose();
					}
					else
					{
						//Removing a monitor 
						if (LocatToName.ContainsKey(locationstr))
						{
							//extract the name of the vanishing Mirror
							string name = (string)LocatToName[locationstr];

							//extract and remove the Monitor for that name
							BouncyNodeMonitor oldMonitormodel = (BouncyNodeMonitor) nameToActivityMonitor[name];
							nameToActivityMonitor.Remove(name);
							monitors_activity.Remove(oldMonitormodel);

							//adding a new null monitor since the mirror is gone
							Node newNode = null;
							BouncyNodeMonitor monitor = new BouncyNodeMonitor(newNode, random);
				
							nameToActivityMonitor.Add(name, monitor );
							monitor.Location = oldMonitormodel.Location;
							monitor.Size = oldMonitormodel.Size;
							Controls.Add(monitor);
							monitors_activity.Add(monitor);

							monitor.SetOverrideFailure(MyAWT_SystemFailure);
							monitor.SetEmptyColor(EmptyStatusColor);

							oldMonitormodel.Dispose();
							LocatToName.Remove(name);
						}
					}
				}
			}
		}

		public void AddDaftVerticalDividers (int count)
		{
			//			foreach (BouncyNodeMonitor bnm in monitors)
			//			{
			//				bnm.AddDaftVerticalDividers(count);
			//			}
		}

		protected override void OnPaint (PaintEventArgs e)
		{
			base.OnPaint(e);
		}

		protected override void OnClick (EventArgs e)
		{
		}

	
		#region ITimedClass Members
		public void Start ()
		{
			running = true;
		}

		public void FastForward (double timesRealTime)
		{
			running = true;
		}

		public void Reset ()
		{
			running = false;
		}

		public void Stop ()
		{
			running = false;
		}
		#endregion

	}
}