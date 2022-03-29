using System.Collections;
using LibCore;
using Network;

namespace IncidentManagement
{
	public struct ConflictingBooleanAttribute
	{
		public string attribute;
		public bool defaultVal;
		public bool val;
		public bool forbidTurnOn;
		public bool forbidTurnOff;
		public string shortFailTurnOnReason;
		public string longFailTurnOnReason;
		public string shortFailTurnOffReason;
		public string longFailTurnOffReason;

		public ConflictingBooleanAttribute (string attribute, bool defaultVal, bool val, bool forbidTurnOn, bool forbidTurnOff, string shortFailTurnOnReason, string longFailTurnOnReason, string shortFailTurnOffReason, string longFailTurnOffReason)
		{
			this.attribute = attribute;
			this.defaultVal = defaultVal;
			this.val = val;
			this.forbidTurnOn = forbidTurnOn;
			this.forbidTurnOff = forbidTurnOff;
			this.shortFailTurnOnReason = shortFailTurnOnReason;
			this.longFailTurnOnReason = longFailTurnOnReason;
			this.shortFailTurnOffReason = shortFailTurnOffReason;
			this.longFailTurnOffReason = longFailTurnOffReason;
		}
	}

	/// <summary>
	/// Summary description for NonOverlappingIncidentConstraint.
	/// </summary>
	public static class NonOverlappingIncidentConstraint
	{
        static bool allowOverlapping;

        public static void AllowOverlapping ()
        {
            allowOverlapping = true;
        }

		/// <summary>
		/// Searches (depth first) on the tree for any biz_service_users that have an incident
		/// on them. If any do then it returns false. It also outputs a description of why the
		/// incident is not allowed.
		/// </summary>
		/// <param name="target">The node that the potential incident will be applied on.</param>
		/// <param name="fullReason">A full English reason on exactly why the incident is not allowed.</param>
		/// <param name="shortReason">A short (player allowed visible) reason on why the incident is not allowed.
		/// This reason is generally "Incident X is in place" rather than the longer "biz service X is down".</param>
		/// <returns>True on no overlapping incidents, false on an incident is present.</returns>
		public static bool IsNewIncidentAllowed(string ignore_incident, Node target, ref string fullReason, ref string shortReason, ArrayList conflictingStates, string newId)
		{
            if (allowOverlapping)
            {
                return false;
            }

			ArrayList hitNodes = new ArrayList();

			return CheckIfIncidentNotOnDependentService(ignore_incident, target, ref fullReason, ref shortReason, ref hitNodes, conflictingStates, newId);
		}

		//
		// Case 2678 : Overlapping incident detector should prevent incidents on the same business service. 
		//
		// Simplify this rule. Search down from this node to all biz_service_users and then look up at the
		// overall biz_service itself. If this has an incident on it then this incident would overlap.
		//
		static bool CheckIfIncidentNotOnDependentService(string ignore_incident, Node target, ref string fullReason, ref string shortReason, ref ArrayList hitNodes, ArrayList conflictingStates, string newId)
		{
			bool ret = true;
			//
			if(hitNodes.Contains(target))
			{
				return true;
			}
			hitNodes.Add(target);
			//
			string type = target.GetAttribute("type");
			if("biz_service" == type)
			{
				string incident_id = target.GetAttribute("incident_id");
				if( ("" != incident_id) && (ignore_incident != incident_id) )
				{
					fullReason += target.GetAttribute("reasondown");
					string tmp = "Incident ID " + target.GetAttribute("incident_id") + " is already active. ";

					string format;
					if (incident_id == newId)
					{
						format = CoreUtils.SkinningDefs.TheInstance.GetData("incident_overlap_message_format_same",
																			CoreUtils.SkinningDefs.TheInstance.GetData("incident_overlap_message_format"));
					}
					else
					{
						format = CoreUtils.SkinningDefs.TheInstance.GetData("incident_overlap_message_format");
					}
					if (! string.IsNullOrEmpty(format))
					{
						string serviceName = target.GetAttribute("name");
						string serviceDesc = target.GetAttribute("desc");
						string serviceShortDesc = target.GetAttribute("shortdesc");
						string bsuName = "";

						ArrayList downedNodes = target.Tree.GetNodesWithAttributeValue("incident_id", incident_id);
						foreach (Node node in downedNodes)
						{
							if (node.GetAttribute("type") == "biz_service_user")
							{
								bsuName = node.GetAttribute("name");
								break;
							}
						}

						tmp = CONVERT.Format(format, incident_id, serviceName, serviceDesc, serviceShortDesc, bsuName, newId);
					}

					if (shortReason.IndexOf(tmp) < 0)
					{
						shortReason += tmp;
					}

					return false;
				}

				foreach (ConflictingBooleanAttribute attribute in conflictingStates)
				{
					bool currentValue = target.GetBooleanAttribute(attribute.attribute, attribute.defaultVal);
					bool conflictingValue = attribute.val;

					bool turnedOn = conflictingValue && ! currentValue;
					bool turnedOff = (! conflictingValue) && currentValue;

					if (turnedOn && attribute.forbidTurnOn)
					{
						fullReason += attribute.longFailTurnOnReason;
						string tmp = attribute.shortFailTurnOnReason;
						if (shortReason.IndexOf(tmp) < 0)
						{
							shortReason += tmp;
							return false;
						}
					}
					else if (turnedOff && attribute.forbidTurnOff)
					{
						fullReason += attribute.longFailTurnOffReason;
						string tmp = attribute.shortFailTurnOffReason;
						if (shortReason.IndexOf(tmp) < 0)
						{
							shortReason += tmp;
							return false;
						}
					}
				}
			}
			else if("Connection" == type)
			{
				// Jump to the "to" node.
				LinkNode link = target as LinkNode;
				if(null != link)
				{
					if(!CheckIfIncidentNotOnDependentService(ignore_incident, link.To, ref fullReason, ref shortReason, ref hitNodes, conflictingStates, newId))
					{
						ret = false;
					}
				}
			}
			else if("biz_service_user" == type)
			{
				// Jump up to the biz_service.
				foreach(LinkNode backLink in target.BackLinks)
				{
					if(backLink.Parent.GetAttribute("type") == "biz_service")
					{
						if (!CheckIfIncidentNotOnDependentService(ignore_incident, backLink.Parent, ref fullReason, ref shortReason, ref hitNodes, conflictingStates, newId))
						{
							ret = false;
						}
					}
				}
			}
			else
			{
				// Run over children as long as they are dependsOn links.
				foreach(Node child in target)
				{
					if(child.GetAttribute("type") != "dependsOn")
					{
						if (!CheckIfIncidentNotOnDependentService(ignore_incident, child, ref fullReason, ref shortReason, ref hitNodes, conflictingStates, newId))
						{
							ret = false;
						}
					}
				}

				// : fix for 5105.  Bring down zone 1 power and then zone 5 power,
				// and we end up with Piquet being down both because of zone 1 and zone 5.
				// To prevent this, we need to run over backward dependencies:
				// if anything that depends on us, is already down or has downed children,
				// then we can't be brought down.
				foreach (LinkNode childLink in target.BackLinks)
				{
					if (childLink.GetAttribute("type") == "dependsOn")
					{
						if (!CheckIfIncidentNotOnDependentService(ignore_incident, childLink.From, ref fullReason, ref shortReason, ref hitNodes, conflictingStates, newId))
						{
							ret = false;
						}
					}
				}
			}
			//
			return ret;
		}
		//
		static bool HasImpact(Node target)
		{
			string impactkmph = target.GetAttribute("impactkmph");
			string impactsecsinpit = target.GetAttribute("impactsecsinpit");

			if( (impactkmph != "") && (impactkmph != "0") ) return true;
			if( (impactsecsinpit != "") && (impactsecsinpit != "0") ) return true;

			return false;
		}

		static bool CheckIsNewIncidentAllowed(Node target, ref string fullReason, ref string shortReason, bool climbUp)
		{
			bool okay = true;
			string targetType = target.GetAttribute("type");
			string name = target.GetAttribute("name");

			if(target.GetAttribute("up") == "false")
			{
				// We are only restricted if we have an impactkmph or an impactsecsinpit..
				if("biz_service_user" == targetType)
				{
					if(HasImpact(target))
					{
						okay = false;

						fullReason += target.GetAttribute("reasondown");
						string tmp = "Incident ID " + target.GetAttribute("incident_id") + " is already active. ";
						if (shortReason.IndexOf(tmp) < 0)
						{
							shortReason += tmp;
						}
					}
				}
				else
				{
					okay = false;
					fullReason += target.GetAttribute("reasondown");
					string id = target.GetAttribute("incident_id");
					if(id != "")
					{
						string tmp = "Incident ID " + target.GetAttribute("incident_id") + " is already active. ";
						if (shortReason.IndexOf(tmp) < 0)
						{
							shortReason += tmp;
						}
					}
					else
					{
						shortReason += target.GetAttribute("reasondown");
					}
				}
			}
			//
			if(target.GetIntAttribute("workingAround",0) != 0)
			{
				if("biz_service_user" == targetType)
				{
					if(HasImpact(target))
					{
						okay = false;

						fullReason += target.GetAttribute("reasondown");
						string tmp = "Incident ID " + target.GetAttribute("incident_id") + " is already active but is in workaround. ";
						if (shortReason.IndexOf(tmp) < 0)
						{
							shortReason += tmp;
						}
					}
				}
				else
				{
					okay = false;
					fullReason += target.GetAttribute("reasondown");
					string id = target.GetAttribute("incident_id");
					if(id != "")
					{
						string tmp = "Incident ID " + target.GetAttribute("incident_id") + " is already active but in workaround. ";
						if (shortReason.IndexOf(tmp) < 0)
						{
							shortReason += tmp;
						}
					}
				}
			}
			//
			if(target.GetAttribute("goingDownInSecs") != "")
			{
				// TODO : Get the incident ID and add to the short reason.
				if("biz_service_user" == targetType)
				{
					if(HasImpact(target))
					{
						okay = false;

						fullReason += target.GetAttribute("reasondown");
						string tmp = "Incident ID " + target.GetAttribute("incident_id") + " is already active. ";
						if (shortReason.IndexOf(tmp) < 0)
						{
							shortReason += tmp;
						}
					}
				}
				else
				{
					okay = false;
					fullReason += target.GetAttribute("reasondown");
					string id = target.GetAttribute("incident_id");
					if(id != "")
					{
						string tmp = "Incident ID " + target.GetAttribute("incident_id") + " is already active. ";
						if (shortReason.IndexOf(tmp) < 0)
						{
							shortReason += tmp;
						}
					}
				}
			}
			//
			string upByMirror = target.GetAttribute("upByMirror");
			if( (upByMirror != "") && (upByMirror != "false") )
			{
				// TODO : Get the incident ID and add to the short reason.
				if("biz_service_user" == targetType)
				{
					if(HasImpact(target))
					{
						okay = false;

						fullReason += target.GetAttribute("name") + " Is Already Only Up By Being Mirrored\r\n";
						string tmp = "Incident  " + target.GetAttribute("incident_id") + " is already active. ";
						if (shortReason.IndexOf(tmp) < 0)
						{
							shortReason += tmp;
						}
					}
				}
				else
				{
					okay = false;
					fullReason += target.GetAttribute("reasondown");
					string id = target.GetAttribute("incident_id");
					if(id != "")
					{
						string tmp = "Incident ID " + target.GetAttribute("incident_id") + " is already active. ";
						if (shortReason.IndexOf(tmp) < 0)
						{
							shortReason += tmp;
						}
					}
				}
			}
			//
			// Do we have any dependsOn back links?
			// If so then our target incident could bring down a seemingly unrelated part of the
			// network and cause an overlapping incident. Therefore, we must follow the dependsOn
			// back link and search down that tree as well.
			//
			foreach(LinkNode link in target.BackLinks)
			{
				if(link.GetAttribute("type") == "dependsOn")
				{
					if(!CheckIsNewIncidentAllowed(link.From, ref fullReason, ref shortReason, climbUp))
					{
						okay = false;
					}
				}
			}
			//
			if(climbUp)
			{
				if(null != target.Parent)
				{
					if(!CheckIsNewIncidentAllowed(target.Parent, ref fullReason, ref shortReason, climbUp))
					{
						okay = false;
					}
				}
			}
			else
			{
				foreach(Node child in target)
				{
					if(!CheckIsNewIncidentAllowed(child, ref fullReason, ref shortReason, climbUp))
					{
						okay = false;
					}
				}
			}
			//
			// Is this node a link?
			// If so then check the tree where we are going...
			//
			LinkNode alink = target as LinkNode;
			if(null != alink)
			{
				// Don't follow links of type "dependsOn" as such incidents would not overlap...
				if(alink.GetAttribute("type") != "dependsOn")
				{
					if(!CheckIsNewIncidentAllowed(alink.To, ref fullReason, ref shortReason, climbUp))
					{
						okay = false;
					}
				}
			}
			//
			return okay;
		}
	}
}
