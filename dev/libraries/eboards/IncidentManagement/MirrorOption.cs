using System.Collections;
using System.Xml;

using LibCore;
using Network;
using CoreUtils;

namespace IncidentManagement
{
	/// <summary>
	/// A MirrorOption is an option for mirroring that is present within the game.
	/// </summary>
	public class MirrorOption
	{
		protected Node target;
		//protected Node install_on;
		protected string[] locations;
		protected ArrayList allowedApps;
		protected IncidentDefinition postInstallScript;

		public Node Target
		{
			get
			{
				return target;
			}
		}
		
		public MirrorOption(NodeTree model, XmlNode xnode)
		{
			target = model.GetNamedNode(xnode.SelectSingleNode("target").InnerXml);
			char[] comma = { ',' };
			string[] apps = xnode.SelectSingleNode("apps").InnerText.Split(comma);

			allowedApps= new ArrayList(apps);

			postInstallScript = new IncidentDefinition(xnode.SelectSingleNode("i").OuterXml, model);
		}

		public void Dispose()
		{
		}

		public Node Apply(string location)
		{
			// We now install on the location's parent and remove the used slot.
			Node parent = target.Parent;
			//
			//
			ArrayList apps = new ArrayList();
			Node oldSlot = parent.Tree.GetNamedNode(location);
			Node newParent = oldSlot.Parent;
			//
			Node newServer = Apply(newParent, target, ref apps);
			//
			//Node oldSlot = parent.Tree.GetNamedNode(location);
			oldSlot.Parent.DeleteChildTree(oldSlot);
			newServer.SetAttribute("location", location);

			// case2 uses the zones in instead of locations so need to preserve proczone as parent zone not always the same
			// dont want to change behaviour for other skins just in case of unwanted effects
			if (SkinningDefs.TheInstance.GetBoolData("preserve_mirror_zones", false))
			{
				int procZone = oldSlot.GetIntAttribute("proczone", -1);
				newServer.SetAttribute("proczone", procZone);
				newServer.SetAttribute("zone", procZone);
			}

			//
			// Set Apps to new mirror locations...
			//
			string initialLetter = location.Substring(0,1);
			string initialNum = location.Substring(1);
			int num = CONVERT.ParseInt(initialNum);
			foreach(Node n in apps)
			{
				++num;
				n.SetAttribute("location", initialLetter + CONVERT.ToStr(num));
			}
			//
			if(null != postInstallScript)
			{
				postInstallScript.ApplyAction(target.Tree);
			}
			//
			return newServer;
		}

		public static Node Mirror (Node newParent, Node nodeToMirror, ref ArrayList apps)
		{
			return Apply(newParent, nodeToMirror, new ArrayList (), ref apps);
		}

		protected Node Apply (Node parentNode, Node nodeToMirror, ref ArrayList apps)
		{
			return Apply(parentNode, nodeToMirror, allowedApps, ref apps);
		}

		protected static Node Apply (Node parentNode, Node nodeToMirror, ArrayList allowedApps, ref ArrayList apps)
		{
			// This adds the mirror...
			// Copy all attributes from the original server (except the name of course).
			ArrayList originalAttributes = nodeToMirror.AttributesAsArrayList;
			AttributeValuePair.RemoveAttribute("name", ref originalAttributes);
			AttributeValuePair.RemoveAttribute("proczone", ref originalAttributes);
			AttributeValuePair.RemoveAttribute("zone", ref originalAttributes);
			//RemoveAttribute("type", ref originalAttributes);
			// Mark this as a mirrored item.
			originalAttributes.Add( new AttributeValuePair( "isMirror", "true" ) );

			AttributeValuePair.RemoveAttribute("location", ref originalAttributes);
			//
			// LP: 9-5-2007 : Bug 2356 : Removed required attribute from new mirror for fix.
			//
			AttributeValuePair.RemoveAttribute("up", ref originalAttributes);
			AttributeValuePair.RemoveAttribute("fixable", ref originalAttributes);
			AttributeValuePair.RemoveAttribute("incident_id", ref originalAttributes);
			AttributeValuePair.RemoveAttribute("workingaround", ref originalAttributes);
			AttributeValuePair.RemoveAttribute("goingdowninsecs", ref originalAttributes);
			AttributeValuePair.RemoveAttribute("goingdown", ref originalAttributes);
			AttributeValuePair.RemoveAttribute("fixable", ref originalAttributes);
			AttributeValuePair.RemoveAttribute("workaround", ref originalAttributes);
			AttributeValuePair.RemoveAttribute("canworkaround", ref originalAttributes);

			//Correct the zone references for the new item
			int destination_proc_zone = parentNode.GetIntAttribute("proczone",1);
			originalAttributes.Add(new AttributeValuePair("proczone", CONVERT.ToStr(destination_proc_zone)));
			originalAttributes.Add(new AttributeValuePair("zone", CONVERT.ToStr(destination_proc_zone)));

			string name = nodeToMirror.GetAttribute("name");
			string nodeType = nodeToMirror.Type;//GetAttribute("type");

			Node mirroredNode = null;

			string newName = name;
			if("" != newName) newName += "(M)";

			if ((nodeType.ToLower() == "link")
				|| (nodeType.ToLower() == "connection"))
			{
				LinkNode link = new LinkNode(parentNode, nodeType, newName, originalAttributes);
				mirroredNode = (Node) link;
			}
			else
			{
				mirroredNode = new Node(parentNode,nodeType, newName, originalAttributes);

				if(nodeToMirror.GetAttribute("type") == "App")
				{
					apps.Add(mirroredNode);
				}
			}

			// Crawl down the tree mirroring components...
			foreach(Node n in nodeToMirror)
			{
				// Do not mirror biz_service_user(s) ir slot(s).
				string type = n.GetAttribute("type");
				// Also, only mirror allowed Apps
				if(type == "App")
				{
					if(allowedApps.Contains(n.GetAttribute("name")))
					{
						// This is an App that should be mirrored.
						Apply(mirroredNode, n, allowedApps, ref apps);
					}
				}
				else if( (type != "biz_service_user") && (type != "Slot") )
				{
					// This is something that should be mirrored.
					Apply(mirroredNode, n, allowedApps, ref apps);
				}
			}

			return mirroredNode;
		}

		public void Remove()
		{
			// Remove the mirror...
			string name = target.GetAttribute("name");

			Node mirrorNode = target.Tree.GetNamedNode(name + "(M)");
			Node parent = mirrorNode.Parent;
			string location = mirrorNode.GetAttribute("location");
			string procZone = mirrorNode.GetAttribute("proczone");
			mirrorNode.Parent.DeleteChildTree(mirrorNode);
			//
			ArrayList attrs = new ArrayList();
			attrs.Add( new AttributeValuePair("location", location) );
			attrs.Add( new AttributeValuePair("proczone", procZone) );
			attrs.Add( new AttributeValuePair("type", "Slot") );
			Node n = new Node(parent,"Slot",location, attrs);
		}
	}
}
