using System;
using System.Collections;
using System.Xml;

using LibCore;
using Network;
namespace IncidentManagement
{
	/// <summary>
	/// Summary description for RequirementsChecker.
	/// </summary>
	public class RequirementsChecker
	{
		public RequirementsChecker()
		{
		}

		/// <summary>
		/// Main Handler for all requirement processing  
		/// </summary>
		/// <param name="xmlRequirements"></param>
		/// <param name="model"></param>
		/// <param name="reason"></param>
		/// <param name="short_reason"></param>
		/// <returns></returns>
		public static bool AreRequirementsMet(string xmlRequirements, NodeTree model, out string reason, out string short_reason)
		{
			reason = "";
			short_reason = "";

			XmlDocument xdoc = new XmlDocument();
			xdoc.LoadXml(xmlRequirements);

			foreach(XmlNode xnode in xdoc.DocumentElement.ChildNodes)
			{
				if(xnode.NodeType == XmlNodeType.Element)
				{
					switch(xnode.Name)
					{
						case "requireLocExists":
							if(!CheckRequireLocExists(xnode, model, ref reason, ref short_reason))
							{
								return false;
							}
							break;

						case "requireLocNotUsed":
							if(!CheckRequireLocNotUsed(xnode, model, ref reason, ref short_reason))
							{
								return false;
							}
							break;

						case "requireMustMatchExisting":
							if(!CheckRequireMustMatchExistingLocation(xnode, model, ref reason, ref short_reason))
							{
								return false;
							}
							break;

						case "requireNotExists":
							if(!CheckRequireNotExists(xnode, model, ref reason, ref short_reason))
							{
								return false;
							}
							break;

						case "requireExists":
							if(!CheckRequireExists(xnode, model, ref reason, ref short_reason))
							{
								return false;
							}
							break;

						case "requireAttrs":
							if(!CheckRequireAttrs(xnode, model, ref reason, ref short_reason))
							{
								return false;
							}
							break;

						case "requireNoAttrs":
							if(!CheckRequireNoAttrs(xnode, model, ref reason, ref short_reason))
							{
								return false;
							}
							break;

						case "ifAttrs":
							if(!IfAttrs(xnode, model, ref reason, ref short_reason))
							{
								return false;
							}
							break;

						case "requireMin":
							if(!CheckRequireMin(xnode, model, ref reason, ref short_reason))
							{
								return false;
							}
							break;
						default:
							throw( new Exception("Requirement " + xnode.Name + " unknown (No Handler defined).") );
					}
				}
			}

			return true;
		}

		/// <summary>
		/// Check that if a node which has a particular location exists within the network
		/// used to ensure that location is not 'totally bogus' Z999 etc
		/// </summary>
		/// <param name="xnode"></param>
		/// <param name="model"></param>
		/// <param name="reason"></param>
		/// <param name="short_reason"></param>
		/// <returns></returns>
		protected static bool CheckRequireLocExists(XmlNode xnode, NodeTree model, ref string reason, ref string short_reason)
		{
			//A node has a name and a location 
			//If the name == location then it is not being used 
			string checklocation = xnode.Attributes["i_name"].Value;
			ArrayList al = null;

			al = model.GetNodesWithAttributeValue("location",checklocation);
			if(al.Count==0)
			{
				reason = GetPossiblyCaselessAttribute(xnode, "i_failReason");
				short_reason = GetPossiblyCaselessAttribute(xnode, "i_failShortReason");

				return false;
			}
			return true;
		}

		/// <summary>
		/// Check that if a node is not used (it must have name == location and location == suppliedlocation)
		/// Empty nodes have the same names as thier location
		/// </summary>
		/// <param name="xnode"></param>
		/// <param name="model"></param>
		/// <param name="reason"></param>
		/// <param name="short_reason"></param>
		/// <returns></returns>
		protected static bool CheckRequireLocNotUsed(XmlNode xnode, NodeTree model, ref string reason, ref string short_reason)
		{
			//A node has a name and a location 
			//If the name == location then it is not being used 
			string checklocation = xnode.Attributes["i_name"].Value;

			Node testNode = model.GetNamedNode(checklocation);
			Boolean checkOK = false;
			if (testNode != null)
			{
				string location = testNode.GetAttribute("location");
				if (location == checklocation)
				{
					checkOK = true;
				}
			}
			
			if(checkOK == false)
			{
				reason = GetPossiblyCaselessAttribute(xnode, "i_failReason");
				short_reason = GetPossiblyCaselessAttribute(xnode, "i_failShortReason");

				return false;
			}
			return true;
		}

		/// <summary>
		/// Check if a node exists, if so then the passed attribute must match the xisting node 
		/// Used for optional Installs
		///   Composite Fuel Tank (Jarier) --- 305 for version 1 and 501 for version 2 
		/// Option A --> install the v1 in round 3 so that by round 5, Jarier exists at a location
		///              When checking for round 5, we need to ensure that we match the intalled location
		/// Option B --> DO NOT install the v1 in round 3 so that by round 5, Jarier does not exist at a location
		///              When checking for round 5, we have no node to check against and have a free hand in the required location
		/// </summary>
		/// <param name="xnode"></param>
		/// <param name="model"></param>
		/// <param name="reason"></param>
		/// <param name="short_reason"></param>
		/// <returns></returns>
		protected static bool CheckRequireMustMatchExistingLocation(XmlNode xnode, NodeTree model, ref string reason, ref string short_reason)
		{
			//A node has a name and a location 
			//If the name == location then it is not being used 
			string checkname = xnode.Attributes["i_name"].Value;
			string checklocation = xnode.Attributes["location"].Value;

			Node testNode = model.GetNamedNode(checkname);
			Boolean checkOK = false;
			if (testNode != null)
			{
				string location = testNode.GetAttribute("location");
				if (location == checklocation)
				{
					checkOK = true;  //It exists and has the correct location
				}
			}
			else
			{
				checkOK = true; //it does not exist, so we have a free hand
			}
			
			if(checkOK == false)
			{
				reason = GetPossiblyCaselessAttribute(xnode, "i_failReason");
				short_reason = GetPossiblyCaselessAttribute(xnode, "i_failShortReason");

				return false;
			}
			return true;
		}


		/// <summary>
		///	Check that if a node with a particular names exist within the network
		/// </summary>
		/// <param name="xnode"></param>
		/// <param name="model"></param>
		/// <param name="reason"></param>
		/// <param name="short_reason"></param>
		/// <returns></returns>
		protected static bool CheckRequireExists(XmlNode xnode, NodeTree model, ref string reason, ref string short_reason)
		{
			if(null == model.GetNamedNode( xnode.Attributes["i_name"].Value ))
			{
				reason = GetPossiblyCaselessAttribute(xnode, "i_failReason");
				short_reason = GetPossiblyCaselessAttribute(xnode, "i_failShortReason");

				return false;
			}

			return true;
		}


		/// <summary>
		///	Check that if a node with a particular name does not exist within the network
		/// </summary>
		/// <param name="xnode"></param>
		/// <param name="model"></param>
		/// <param name="reason"></param>
		/// <param name="short_reason"></param>
		/// <returns></returns>
		protected static bool CheckRequireNotExists(XmlNode xnode, NodeTree model, ref string reason, ref string short_reason)
		{
			if(null != model.GetNamedNode( xnode.Attributes["i_name"].Value ))
			{
				reason = GetPossiblyCaselessAttribute(xnode, "i_failReason");
				short_reason = GetPossiblyCaselessAttribute(xnode, "i_failShortReason");

				return false;
			}

			return true;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="xnode"></param>
		/// <param name="model"></param>
		/// <param name="reason"></param>
		/// <param name="short_reason"></param>
		/// <returns></returns>
		protected static bool IfAttrs(XmlNode xnode, NodeTree model, ref string reason, ref string short_reason)
		{
			Node target = null;
			XmlAttribute i_name = xnode.Attributes["i_name"];
			if(null != i_name)
			{
				target = model.GetNamedNode( i_name.Value );
			}
			if(null == target)
			{
				// Try to see if we are targeting a parent node...
				target = model.GetNamedNode( xnode.Attributes["i_parentOf"].Value );
				if(null != target)
				{
					target = target.Parent;
				}
			}
			//
			foreach(XmlAttribute attr in xnode.Attributes)
			{
				if(!attr.Name.StartsWith("i_"))
				{
					if(target.GetAttribute(attr.Name) == attr.Value)
					{
						// Attribute-Value pair do not match.
						reason = GetPossiblyCaselessAttribute(xnode, "i_failReason");
						short_reason = GetPossiblyCaselessAttribute(xnode, "i_failShortReason");

						return false;
					}
				}
			}
			//
			return true;
		}

		/// <summary>
		/// Checks if a node has attributes with particular values
		/// </summary>
		/// <param name="xnode"></param>
		/// <param name="model"></param>
		/// <param name="reason"></param>
		/// <param name="short_reason"></param>
		/// <returns></returns>
		protected static bool CheckRequireAttrs(XmlNode xnode, NodeTree model, ref string reason, ref string short_reason)
		{
			Node target = null;
			XmlAttribute i_name = xnode.Attributes["i_name"];
			if(null != i_name)
			{
				target = model.GetNamedNode( i_name.Value );
			}
			if(null == target)
			{
				string parentName = xnode.Attributes["i_parentOf"].Value;

				// Try to see if we are targeting a parent node...
				target = model.GetNamedNode( parentName );
				if(null != target)
				{
					target = target.Parent;
				}
				else
				{
					// The parent must already be in use: look for it as a location rather than as a node name.
					ArrayList nodes = model.GetNodesWithAttributeValue("location", parentName);

					// Find a non-project nodes.
					foreach (Node node in nodes)
					{
						if (node.Type.ToLower() == "node")
						{
							target = node;
						}
					}
				}
			}
			//
			foreach(XmlAttribute attr in xnode.Attributes)
			{
				if(!attr.Name.StartsWith("i_"))
				{
					string targetAttribute = target.GetAttribute(attr.Name);
					// "*" matches any value.
					if (! ((targetAttribute == "*") || (targetAttribute == attr.Value)))
					{
						// Attribute-Value pair do not match.
						reason = GetPossiblyCaselessAttribute(xnode, "i_failReason");
						short_reason = GetPossiblyCaselessAttribute(xnode, "i_failShortReason");

						return false;
					}
				}
			}
			//
			return true;
		}

		static string GetPossiblyCaselessAttribute (XmlNode node, string attribute)
		{
			XmlNode attr = node.Attributes.GetNamedItem(attribute);
			if (attr == null)
			{
				attr = node.Attributes.GetNamedItem(attribute.ToLower());
			}

			string s = "";
			if (attr != null)
			{
				s = attr.Value.Replace("\\r\\n","\r\n");
			}

			return s;
		}

		/// <summary>
		/// Checks if a node has no attributes with the given values (ie not ((a1 == v1) || (a2 == v2) ||...))
		/// </summary>
		/// <param name="xnode"></param>
		/// <param name="model"></param>
		/// <param name="reason"></param>
		/// <param name="short_reason"></param>
		/// <returns></returns>
		protected static bool CheckRequireNoAttrs(XmlNode xnode, NodeTree model, ref string reason, ref string short_reason)
		{
			Node target = null;
			XmlAttribute i_name = xnode.Attributes["i_name"];
			if(null != i_name)
			{
				target = model.GetNamedNode( i_name.Value );
			}

			if(null == target)
			{
				if (xnode.Attributes["i_parentOf"] != null)
				{
					string parentName = xnode.Attributes["i_parentOf"].Value;

					// Try to see if we are targeting a parent node...
					target = model.GetNamedNode( parentName );
					if(null != target)
					{
						target = target.Parent;
					}
					else
					{
						// The parent must already be in use: look for it as a location rather than as a node name.
						ArrayList nodes = model.GetNodesWithAttributeValue("location", parentName);

						// Find a non-project nodes.
						foreach (Node node in nodes)
						{
							if (node.Type.ToLower() == "node")
							{
								target = node;
							}
						}
					}
				}
				else if (xnode.Attributes["i_zoneOf"] != null)
				{
					string zoneOfName = xnode.Attributes["i_zoneOf"].Value;

					Node zoneOfTarget = model.GetNamedNode(zoneOfName);
					if (zoneOfTarget == null)
					{
						// Must already be in use: look for it as a location rather than a name.
						ArrayList nodes = model.GetNodesWithAttributeValue("location", zoneOfName);

						// Find a non-project node.
						foreach (Node node in nodes)
						{
							if (node.Type.ToLower() == "node")
							{
								zoneOfTarget = node;
							}
						}
					}
					if (zoneOfTarget != null)
					{
						string zone = zoneOfTarget.GetAttribute("zone");
						if (zone == "")
						{
							zone = zoneOfTarget.GetAttribute("proczone");
						}

						target = model.GetNamedNode("Zone" + zone);
					}
				}
				else if (xnode.Attributes["i_serverFor"] != null)
				{
					string startName = xnode.Attributes["i_serverFor"].Value;

					Node startNode = model.GetNamedNode(startName);
					if (startNode == null)
					{
						ArrayList nodes = model.GetNodesWithAttributeValue("location", startName);
						foreach (Node node in nodes)
						{
							if (node.Type.ToLower() == "node")
							{
								startNode = node;
							}
						}
					}

					while ((startNode.Parent != null) && (startNode.GetAttribute("Desc").ToLower() != "server"))
					{
						startNode = startNode.Parent;
					}

					target = startNode;
				}
			}
			//
			if (target == null)
			{
				// Target doesn't exist.  Strictly that means the test has passed!  Allow others
				// to detect that it doesn't exist and complain later.

				return true;
			}

			foreach(XmlAttribute attr in xnode.Attributes)
			{
				if(!attr.Name.StartsWith("i_"))
				{
					if(target.GetAttribute(attr.Name) == attr.Value)
					{
						// Attribute-Value pair match.
						reason = GetPossiblyCaselessAttribute(xnode, "i_failReason");
						short_reason = GetPossiblyCaselessAttribute(xnode, "i_failShortReason");

						return false;
					}
				}
			}
			//
			return true;
		}

		/// <summary>
		/// Checks to see if a node has attributes with minimum levels
		/// </summary>
		/// <param name="xnode"></param>
		/// <param name="model"></param>
		/// <param name="reason"></param>
		/// <param name="short_reason"></param>
		/// <returns></returns>
		protected static bool CheckRequireMin(XmlNode xnode, NodeTree model, ref string reason, ref string short_reason)
		{
			Node target = null;
			XmlAttribute i_name = xnode.Attributes["i_name"];
			if(null != i_name)
			{
				target = model.GetNamedNode( i_name.Value );
			}
			if(null == target)
			{
				// Try to see if we are targeting a parent node...
				if (xnode.Attributes["i_parentOf"] != null)
				{
					string nodename = xnode.Attributes["i_parentOf"].Value;
					if (nodename != "")
					{
						target = model.GetNamedNode( nodename );
						if(null != target)
						{
							target = target.Parent;
						}
					}
				}
			}
			if(null == target)
			{
				if (xnode.Attributes["i_locationOf"] != null)
				{
					string nodelocation = xnode.Attributes["i_locationOf"].Value;
					if (nodelocation != "")
					{
						//extract all the node with the specified location 
						ArrayList al = model.GetNodesWithAttributeValue("location", nodelocation);
						Boolean foundNode = false;
						//check over the possibles nodes with that location
						//we are only interesrted in the first one in the network (app or location)
						//there cane be only one uch location within the network
						foreach (Node n1 in al)
						{
							if (foundNode == false)
							{
								string nodetype = n1.GetAttribute("type");
								if ((nodetype.ToLower() == "app")|(nodetype.ToLower() == "slot"))
								{
									foundNode = true;
									if (n1.Parent != null)
									{
										target = n1.Parent;
										foundNode = true;
									}
								}
							}
						}
						if (foundNode == false)
						{
							reason = GetPossiblyCaselessAttribute(xnode, "i_failReason");
							short_reason = GetPossiblyCaselessAttribute(xnode, "i_failShortReason");
							return false;
						}
					}
				}
//				// Try to see if we are targeting a defined location 
//				string defined_location = xnode.Attributes["i_locationOf"].Value;
//				ArrayList al = model.GetNodesWithAttributeValue("location", defined_location);
//				if (al.Count == 1)
//				{
//					target = (Node)al[0];
//				}
			}

			if (null == target)
			{
				throw( new Exception("No Requirements Target extracted from [" + xnode.InnerXml + "]") );
			}
			//
			foreach(XmlAttribute attr in xnode.Attributes)
			{
				if(!attr.Name.StartsWith("i_"))
				{
					int currentValue = target.GetIntAttribute(attr.Name,int.MinValue);
					int requiredMinValue = CONVERT.ParseInt(attr.Value);

					if(currentValue < requiredMinValue)
					{
						reason = GetPossiblyCaselessAttribute(xnode, "i_failReason");
						short_reason = GetPossiblyCaselessAttribute(xnode, "i_failShortReason");

						return false;
					}
				}
			}
			return true;
		}
	}
}
