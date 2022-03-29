using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;

using System.Xml;

using LibCore;
using Network;

namespace CommonGUI
{
	/// <summary>
	/// Summary description for WarningLevelsDisplay.
	/// </summary>
	public class VariableHeightWarningLevelsDisplay : FlickerFreePanel
	{
		protected Random random;
		protected ArrayList monitors;
		protected int xoffset = 0;
		protected int yoffset = 10;
		protected int numInGroup = 0;
		protected int numTotal = 0;
		protected int numGroups = 0;
		protected Size size = new Size(10,50);
		protected Size numSize = new Size(10,12);

		protected int GroupVerticalSep = 90;
		protected int GroupTextVerticalSep = 20;

		protected Font font = CoreUtils.SkinningDefs.TheInstance.GetFont(8);
		protected Font sFont = CoreUtils.SkinningDefs.TheInstance.GetFont(12);

		protected NodeTree model;
		protected Node MyAWT_SystemNode = null;
		protected Boolean MyAWT_SystemFailure = false;
		protected Hashtable KnownMirrorSlots = new Hashtable();
		protected Hashtable LocatToName = new Hashtable();

		protected Hashtable nameToMonitor = new Hashtable();
		protected Color BackgroundColor = Color.Black;
		protected Color TitleForeColor = Color.White;
		protected Color TitleBackColor = Color.White;
		protected Color EmptyStatusColor = Color.DarkGray;

		//		//Providing App System 
		//		protected Node applicationAWTProvider;
		//		protected ArrayList applicationAWTProvider_RequiredNodes;
		//		protected Hashtable applicationAWTProvider_RequiredNodesStatus;
		//		protected bool providingAppUsing = true;	//Whether we are using the Providing App Sub System 
		//		protected bool providingAppStatus = true; //Whether the Providing App is functionally up or down

		#region Constructor and Dispose

		// Default constructor, for including all monitor groups.
		public VariableHeightWarningLevelsDisplay(NodeTree _model, Boolean UseCompactMode, 
			Color newBackColor, Color newTitleForeColor, Color newTitleBackColor,
			Color newEmptyStatusColor)
			: this(_model, UseCompactMode,
			newBackColor, newTitleForeColor,
			newTitleBackColor, newEmptyStatusColor, -1, -1, -1)
		{
		}

		// Only include the groups in the range [startGroupIndex, startGroupIndex + groupCount],
		// starting from index 0 (the first one specified in the XML).
		// If startGroupIndex is -1, include all.  If groupCount is -1, include all after startGroupIndex.
		public VariableHeightWarningLevelsDisplay(NodeTree _model, Boolean UseCompactMode, 
			Color newBackColor, Color newTitleForeColor, Color newTitleBackColor,
			Color newEmptyStatusColor,
			int startGroupIndex, int groupCount,
			int monitorHeight)
		{
			model = _model;
			monitors = new ArrayList();
			random = new Random(1);
			BackgroundColor = newBackColor;
			TitleForeColor = newTitleForeColor;
			TitleBackColor = newTitleBackColor;
			BackColor = BackgroundColor;
			EmptyStatusColor = newEmptyStatusColor;

			if (UseCompactMode)
			{
				yoffset = 0;					
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

			// Load the monitor_items.xml file and connect up the displays to the correct audio
			// style monitors.
			XmlDocument xdoc = new XmlDocument();
			xdoc.Load( AppInfo.TheInstance.Location + "\\data\\monitor_items.xml" );

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

			//need to monitor empty slots that change into Monitored Servers
			//This only occurs for the 2 Mirror Servers 
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
		}

		protected override void Dispose(bool disposing)
		{
			if(disposing)
			{
				if (MyAWT_SystemNode != null)
				{
					MyAWT_SystemNode.AttributesChanged -=MyAWT_SystemNode_AttributesChanged;
				}

				foreach(VariableHeightBouncyNodeMonitor monitor in monitors)
				{
					monitor.Dispose();
				}

				foreach(Node kms in KnownMirrorSlots.Keys)
				{
					kms.AttributesChanged -= MirrorSlotNode_AttributesChanged;
				}
				KnownMirrorSlots.Clear();

				model.NodeAdded -= model_NodeAdded;
				monitors.Clear();
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

			foreach(XmlNode n in xnode.ChildNodes)
			{
				if(n.NodeType == XmlNodeType.Element)
				{
					if(n.Name == "name")
					{
						xoffset += 5;
						//
						if( (numGroups != 0) && (numGroups%4 == 0))
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
						//
						VariableHeightBouncyNodeMonitor monitor = new VariableHeightBouncyNodeMonitor(modelNode, random);
						nameToMonitor.Add(n.InnerXml, monitor );
						monitor.Location = new Point(xoffset,yoffset+GroupTextVerticalSep);
						monitor.Size = size;
						Controls.Add(monitor);
						monitors.Add(monitor);

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
			if(nameToMonitor.ContainsKey(name))
			{
				VariableHeightBouncyNodeMonitor oldMonitormodel = (VariableHeightBouncyNodeMonitor) nameToMonitor[name];
				nameToMonitor.Remove(name);
				monitors.Remove(oldMonitormodel);

				VariableHeightBouncyNodeMonitor monitor = new VariableHeightBouncyNodeMonitor(newNode, random);
				
				nameToMonitor.Add(name, monitor );
				monitor.Location = oldMonitormodel.Location;
				monitor.Size = oldMonitormodel.Size;
				Controls.Add(monitor);
				monitors.Add(monitor);
				monitor.SetOverrideFailure(MyAWT_SystemFailure);
				monitor.SetEmptyColor(EmptyStatusColor);
				oldMonitormodel.Dispose();
			}
		}

		#endregion Handling Node added

		protected void OverideMonitors(Boolean Flag)
		{
			if (monitors.Count>0)
			{
				foreach (VariableHeightBouncyNodeMonitor bnm in monitors)
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
					if (nameToMonitor.ContainsKey(namestr))
					{
						//adding in a new Mirror 
						LocatToName.Add(locationstr,namestr);
						VariableHeightBouncyNodeMonitor oldMonitormodel = (VariableHeightBouncyNodeMonitor) nameToMonitor[namestr];
						nameToMonitor.Remove(namestr);
						monitors.Remove(oldMonitormodel);

						VariableHeightBouncyNodeMonitor monitor = new VariableHeightBouncyNodeMonitor(sender, random);
				
						nameToMonitor.Add(namestr, monitor );
						monitor.Location = oldMonitormodel.Location;
						monitor.Size = oldMonitormodel.Size;
						Controls.Add(monitor);
						monitors.Add(monitor);

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
							VariableHeightBouncyNodeMonitor oldMonitormodel = (VariableHeightBouncyNodeMonitor) nameToMonitor[name];
							nameToMonitor.Remove(name);
							monitors.Remove(oldMonitormodel);

							//adding a new null monitor since the mirror is gone
							Node newNode = null;
							VariableHeightBouncyNodeMonitor monitor = new VariableHeightBouncyNodeMonitor(newNode, random);
				
							nameToMonitor.Add(name, monitor );
							monitor.Location = oldMonitormodel.Location;
							monitor.Size = oldMonitormodel.Size;
							Controls.Add(monitor);
							monitors.Add(monitor);

							monitor.SetOverrideFailure(MyAWT_SystemFailure);
							monitor.SetEmptyColor(EmptyStatusColor);

							oldMonitormodel.Dispose();
							LocatToName.Remove(name);
						}
					}
				}
			}
		}
	}
}