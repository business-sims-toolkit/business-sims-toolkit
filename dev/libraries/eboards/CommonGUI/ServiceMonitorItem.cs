using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

using System.Collections;
using LibCore;
using Network;

using CoreUtils;

namespace CommonGUI
{
	/// <summary>
	/// </summary>
	public class ServiceMonitorItem : MonitorItem
	{
		public const int GoneActive = 1;
		public const int GoneEmpty = 2;
		public const int GoneRetired = 3;

		protected Font font;
		Font infoFont;
		Image backGraphic;
		Node monitoredItem;
		protected int icon_offset = 0;
		int box_offset = 0;
		protected int text_offset = 0;
		Node ServiceNode = null;
		Node UpgradeNode = null;
		NodeTree _tree = null;

		float currentVersionNumber = -1;

		Point DisplayLocation = new Point(0,0);
		string function_name;
		int connectionChildrenCount = 0;

		bool auto_translate = true;

		#region Constructor
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="n"></param>
		/// <param name="upgrade"></param>
		/// <param name="functional_name"></param>
		public ServiceMonitorItem(NodeTree tree, Node n, string functional_name)
		{

			string name = n.GetAttribute("name");
			//System.Diagnostics.Debug.WriteLine("Created SMI "+name+"  "+functional_name);
			//Presentation Information
			padTop = SkinningDefs.TheInstance.GetIntData("smi_top_pad", 2);

            if (auto_translate)
			{
				font = ConstantSizeFont.NewFont(TextTranslator.TheInstance.GetTranslateFont("Verdana"),
						TextTranslator.TheInstance.GetTranslateFontSize("Verdana", 8), FontStyle.Bold);

				infoFont = ConstantSizeFont.NewFont(TextTranslator.TheInstance.GetTranslateFont("Verdana"),
					TextTranslator.TheInstance.GetTranslateFontSize("Verdana", 12), FontStyle.Bold);
			}
			else
			{
				font = SkinningDefs.TheInstance.GetFont(8f, FontStyle.Bold);
				infoFont = SkinningDefs.TheInstance.GetFont(12f, FontStyle.Bold);
			}

			//Locational Information
			string icon_offset_str = SkinningDefs.TheInstance.GetData("smi_icon_offset");
			string box_offset_str = SkinningDefs.TheInstance.GetData("smi_box_offset");
			string text_offset_str = SkinningDefs.TheInstance.GetData("smi_text_offset");

			icon_offset = CONVERT.ParseInt(icon_offset_str);
			box_offset =  CONVERT.ParseInt(box_offset_str);
			text_offset =  CONVERT.ParseInt(text_offset_str);

			//Functional Information
			_tree = tree;
			function_name = functional_name;
			AddNewService(n);


		}

		#endregion Constructor

		#region Dispose

		/// <summary>
		/// Custom dispose for the class (mind the event handlers)
		/// </summary>
		protected override void Dispose()
		{
			if (font != null)
			{
				font.Dispose();
			}
			if (infoFont != null)
			{
				infoFont.Dispose();
			}
			if (ServiceNode != null)
			{
				ServiceNode.AttributesChanged -=ServiceNode_AttributesChanged;
				ServiceNode.ChildAdded -=ServiceNode_ChildAdded;
				ServiceNode.ChildRemoved -=ServiceNode_ChildRemoved;
			}
			if (UpgradeNode != null)
			{
				UpgradeNode.ChildAdded -=UpgradeNode_ChildAdded;
				UpgradeNode.ChildRemoved -=UpgradeNode_ChildRemoved;
			}

			//			if (ServiceNodes.Count >0)
			//			{
			//				foreach (Node serviceNode in ServiceNodes)
			//				{
			//					serviceNode.AttributesChanged -= new Network.Node.AttributesChangedEventHandler(serviceNode_AttributesChanged);
			//					serviceNode.ChildAdded -= new Network.Node.NodeChildAddedEventHandler(serviceNode_ChildAdded);
			//					serviceNode.ChildRemoved -= new Network.Node.NodeChildRemovedEventHandler(serviceNode_ChildRemoved);
			//				}
			//			}
			base.Dispose();
		}

		#endregion Dispose

		#region Utils 

		Node getUpgradesNode(Node bs) 	
		{
			Node n1 = null;
			if (bs != null)
			{
				string nodename = bs.GetAttribute("name");
				string nodeupgradename = nodename + @"/upgrades";
				n1 = _tree.GetNamedNode(nodeupgradename);
			}
			return n1;
		}

		void getHighestUpgradeNumber(Node tmpBusinessServiceUpgradesNode, out float highnumber)
		{
			highnumber = -1;
			Hashtable upgrades = new Hashtable();
			foreach (Node n1 in tmpBusinessServiceUpgradesNode.getChildren())
			{
				string name = n1.GetAttribute("name");

				float versionnumber = -1;

				string vn = n1.GetAttribute("version");
				versionnumber = (float) CONVERT.ParseDouble(vn);//float.Parse(vn);

				//float versionnumber = n1.GetIntAttribute("version",-1);
				//int versionnumber = n1.GetIntAttribute("version",-1);

				if (versionnumber > highnumber)
				{
					highnumber = versionnumber;
				}
			}
		}

		public void setDisplayLocation(Point locationPt)
		{
			if ((DisplayLocation.X != locationPt.X)|(DisplayLocation.Y != locationPt.Y))
			{
				DisplayLocation.X = locationPt.X;
				DisplayLocation.Y = locationPt.Y;
			}
			if ((DisplayLocation.X != Left)|(DisplayLocation.Y != Top))
			{
				Left = DisplayLocation.X;
				Top = DisplayLocation.Y;
			}
		}

		/// <summary>
		/// helper method for other classes to get the current monitored node
		/// </summary>
		/// <returns></returns>
		public Node getMonitoredItem()
		{
			return monitoredItem;
		}

		/// <summary>
		/// helper method for other classes to get the function name
		/// </summary>
		/// <returns></returns>
		public string getFunctionName()
		{
			return function_name;
		}

		int DetermineConnectionChildCount(Node ServiceNode)
		{
			int numberOfConnectionChildren = 0;
			if (ServiceNode != null)
			{
				foreach (Node n in ServiceNode.getChildren())
				{
					string kidtype = n.GetAttribute("type");
					if (kidtype.ToLower() == "connection")
					{
						numberOfConnectionChildren++;
					}
				}
			}
			return numberOfConnectionChildren;
		}

		public virtual void SetNormalBackground(Boolean RefreshRequired)
		{
			//System.Diagnostics.Debug.WriteLine("SetNormal");
			ForeColor = Color.White;
			backGraphic = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\servicelozenges\\long_lozenge_green.png");
			if (RefreshRequired)
			{
				Invalidate(); //Refresh();
			}
		}

		public virtual void SetUpgradeBackground(Boolean RefreshRequired)
		{
			//System.Diagnostics.Debug.WriteLine("SetUpgrade");
			ForeColor = Color.White;
			backGraphic = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\servicelozenges\\long_lozenge_blue.png");
			if (RefreshRequired)
			{
				Invalidate(); //Refresh();
			}
		}

		public virtual void SetCreatedBackground(Boolean RefreshRequired)
		{
			//System.Diagnostics.Debug.WriteLine("SetCreated");
			ForeColor = Color.White;
			backGraphic = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\servicelozenges\\long_lozenge_dk_gray.png");
			if (RefreshRequired)
			{
				Invalidate(); //Refresh();
			}
		}		

		public virtual void SetRetiredBackground(Boolean RefreshRequired)
		{
			//System.Diagnostics.Debug.WriteLine("SetRetired");
			ForeColor = Color.White;
			backGraphic = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\servicelozenges\\long_lozenge_dk_gray.png");
			if (RefreshRequired)
			{
				Invalidate(); //Refresh();
			}
		}

		#endregion Utils 

		#region Build Data 

		void RemoveUpgradeNode(Node UpgradeNode)
		{
			if (UpgradeNode != null)
			{
				UpgradeNode.ChildAdded-=UpgradeNode_ChildAdded;
				UpgradeNode.ChildRemoved-=UpgradeNode_ChildRemoved;
				SetNormalBackground(true);
				//SetUpdateAvailable(false);
			}
		}

		void AddUpgradeNode(Node UpgradeNode)
		{
			if (UpgradeNode != null)
			{
				UpgradeNode.ChildAdded+=UpgradeNode_ChildAdded;
				UpgradeNode.ChildRemoved+=UpgradeNode_ChildRemoved;

				float highestAvailableUpgradeValue =-1;
				getHighestUpgradeNumber(UpgradeNode, out highestAvailableUpgradeValue);
				if (highestAvailableUpgradeValue > currentVersionNumber)
				{
					SetUpgradeBackground(true);
				}
			}
		}

		/// <summary>
		/// Adding a extra service which has the same functional name
		/// </summary>
		/// <param name="serviceNode"></param>
		public void AddNewService(Node businessServiceNode)
		{
			ServiceNode = businessServiceNode;
			monitoredItem = ServiceNode;
			UpgradeNode = getUpgradesNode(ServiceNode);
			//Connect up for information 
			if (ServiceNode != null)
			{
				ServiceNode.AttributesChanged +=ServiceNode_AttributesChanged;
				ServiceNode.ChildAdded+=ServiceNode_ChildAdded;
				ServiceNode.ChildRemoved+=ServiceNode_ChildRemoved;
				//extract data 
				desc = monitoredItem.GetAttribute("desc");
				shortdesc = monitoredItem.GetAttribute("shortdesc");


				//
				if(auto_translate)
				{
					desc = TextTranslator.TheInstance.Translate(desc);
					shortdesc = TextTranslator.TheInstance.Translate(shortdesc);
				}
				//
				string vn = monitoredItem.GetAttribute("version");
				currentVersionNumber = (float) CONVERT.ParseDouble(vn);//float.Parse(vn);
				
				iconname = monitoredItem.GetAttribute("icon");
				GetIcon();
				SetCreatedBackground(true);
				//SetUpdateAvailable(false);
				connectionChildrenCount = DetermineConnectionChildCount(monitoredItem);

				if (connectionChildrenCount > 0)
				{
					SetNormalBackground(true);
				}
				else
				{
					SetCreatedBackground(true);
				}
			}
			if (UpgradeNode != null)
			{
				AddUpgradeNode(UpgradeNode);
			}
		}

		#endregion Build Data 

		#region Handling Changing Node Data

		void ServiceNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			Boolean RefreshRequired = false;
			string child_name = sender.GetAttribute("name");
			string child_type = sender.GetAttribute("type");


			foreach(AttributeValuePair avp in attrs)
			{
				string avp1 = avp.Attribute;
				string avp2 = avp.Value;

				//System.Diagnostics.Debug.WriteLine("child_name:"+child_name+" child_type:"+child_type+"  avp1:"+avp1+ "  avp2:"+avp2);


				if ((avp.Attribute == "desc"))
				{
					desc = monitoredItem.GetAttribute("desc");
					if(auto_translate)
					{
						desc = TextTranslator.TheInstance.Translate(desc);
					}
					RefreshRequired = true;
				}
				if ((avp.Attribute == "shortdesc"))
				{
					shortdesc = monitoredItem.GetAttribute("shortdesc");
					if(auto_translate)
					{
						shortdesc = TextTranslator.TheInstance.Translate(shortdesc);
					}
					RefreshRequired = true;
				}
				if ((avp.Attribute == "icon"))
				{
					iconname = monitoredItem.GetAttribute("icon");
					GetIcon();
					RefreshRequired = true;
				}
			}
			if (RefreshRequired)
			{
				Invalidate(); //Refresh();
				//System.Diagnostics.Debug.WriteLine("REFRESH");
			}
		}

		void ServiceNode_ChildAdded(Node sender, Node child)
		{
			//System.Diagnostics.Debug.WriteLine("ServiceNodeKidAdded ");
			//catching the upgrade
			string child_name = sender.Parent.GetAttribute("name");
			string child_type = sender.Parent.GetAttribute("type");
			if (child_type.ToLower() == "upgrades")
			{
				AddUpgradeNode(child);
			}
			else
			{
				connectionChildrenCount = DetermineConnectionChildCount(monitoredItem);
				if (connectionChildrenCount>0)
				{
					SetNormalBackground(true);
				}
				else
				{
					SetCreatedBackground(true);
				}
			}
			//
			//System.Diagnostics.Debug.WriteLine("ServiceNodeKidAdded  child_name:"+child_name+" child_type:"+child_type + " CC "+connectionChildrenCount);
		}

		void ServiceNode_ChildRemoved(Node sender, Node child)
		{
			//System.Diagnostics.Debug.WriteLine("ServiceNodeKidRemoved ");

			//In case we are removing the Upgrades Node in the future
			string child_name = sender.Parent.GetAttribute("name");
			string child_type = sender.Parent.GetAttribute("type");
			if (child_type.ToLower() == "upgrades")
			{
				RemoveUpgradeNode(child);
			}
			else
			{
				connectionChildrenCount = DetermineConnectionChildCount(monitoredItem);
			}
			//
			//System.Diagnostics.Debug.WriteLine("ServiceNodeKidRemoved  child_name:"+child_name+" child_type:"+child_type + " CC "+connectionChildrenCount);
		}

		void UpgradeNode_ChildAdded(Node sender, Node child)
		{
			//System.Diagnostics.Debug.WriteLine("UpgradeNodeKidAdded ");
			//catching the upgrade
			string service_name = sender.Parent.GetAttribute("name");
			string child_name = child.GetAttribute("name");
			string child_type = child.GetAttribute("type");
			//there's no type check for the upgrade at this time 
			//just the existance of a node at this point is sufficient
			int upgradecount = sender.getChildren().Count;
			if (upgradecount > 0)
			{
				SetUpgradeBackground(true);
				//SetUpdateAvailable(true);
			}
			//System.Diagnostics.Debug.WriteLine("UpgradeNodeKidAdded service_name:"+service_name+" child_type:"+child_type + " UC "+upgradecount.ToString());
		}

		void UpgradeNode_ChildRemoved(Node sender, Node child)
		{
			//System.Diagnostics.Debug.WriteLine("UpgradeNodeKidRemoved ");
			string service_name = sender.Parent.GetAttribute("name");
			string child_name = child.GetAttribute("name");
			string child_type = child.GetAttribute("type");
			int upgradecount = sender.getChildren().Count;
			if (upgradecount == 0)
			{
				//SetUpdateAvailable(false);
				SetNormalBackground(true);
				Invalidate(); //Refresh();
			}
			//System.Diagnostics.Debug.WriteLine("UpgradeNodeKidRemoved service_name:"+service_name+" child_type:"+child_type + " UC "+upgradecount.ToString());
		}

		#endregion Handling Changing Node Data

		#region Paint Methods

		/// <summary>
		/// override paint
		/// </summary>
		/// <param name="e"></param>
		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			
			Image back = backGraphic;

			RectangleF textRect = new RectangleF(padLeft + 36+text_offset,  padTop, Size.Width - padLeft - padRight - 36, Size.Height - padTop - padBottom);
			RectangleF roundedRect = new RectangleF(padLeft, SkinningDefs.TheInstance.GetIntData("transition_service_lozenge_rounded_y_tweak", padTop), Size.Width - padLeft - padRight, Size.Height - padTop - padBottom);
			
			g.SmoothingMode = SmoothingMode.AntiAlias;
			g.CompositingQuality = CompositingQuality.HighQuality;
			g.PixelOffsetMode = PixelOffsetMode.HighQuality;

			g.DrawImage(back, roundedRect.X + 26+box_offset, roundedRect.Y + SkinningDefs.TheInstance.GetIntData("transition_service_lozenge_y_tweak"), 100, 30 - SkinningDefs.TheInstance.GetIntData("transition_service_lozenge_height_tweak", 2));
			g.DrawImage(icon, (int)padLeft+icon_offset, (int)padTop - 1, 30, 30);

            string desc = Strings.RemoveHiddenText(this.desc);
            if (desc.Length >= 20) // if the service name is too long that it may overflow the box
            {
                desc = shortdesc;
            }

			if (! string.IsNullOrEmpty(desc))
			{
				StringFormat sf = new StringFormat(StringFormatFlags.NoClip);
				sf.Trimming = StringTrimming.EllipsisCharacter;
				g.DrawString(desc, font, new SolidBrush(ForeColor), textRect, sf);
			}
		}

		#endregion Paint Methods

	}
}

