using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Xml;

using LibCore;
using Network;
using CoreUtils;

namespace IncidentManagement
{
	/// <summary>
	/// An IncidentApplier listens to an incoming incident ID queue and translates icident IDs
	/// into applied actions on the NodeTree to cause incidents. The translation from id to
	/// action is defined by an incident definition XML file.
	/// </summary>
	public class IncidentApplier : IDisposable
	{
		protected NodeTree targetTree;
		Hashtable incidentDefs = new Hashtable();
		Node incidentEntryQueue;
		Hashtable incidentTypeToIncidentArray = new Hashtable();
		ArrayList atStartIncidents = new ArrayList();

		Hashtable templates = new Hashtable();

		public Node IncidentEntryQueue
		{
			get => incidentEntryQueue;

			set
			{
				incidentEntryQueue = value;
				incidentEntryQueue.ChildAdded += incidentEntryQueue_ChildAdded;
			}
		}

		public int GetNumIncidents()
		{
			return incidentDefs.Keys.Count;
		}

		public string GetNthIncidentID(int n)
		{
			if(n < incidentDefs.Keys.Count)
			{
				foreach(string str in incidentDefs.Keys)
				{
					--n;
					if(n < 0)
					{
						return str;
					}
				}
			}

			throw(new Exception("IncidentApplier : Over-reading the incident def array.") );
		}

/*
		public NodeTree TargetTree
		{
			get { return targetTree; }
			set { targetTree = value; }
		}*/

		public IncidentDefinition SetDelayedIncident(string xmldata, NodeTree nt, int delayForSecs)
		{
			BasicXmlDocument xdoc = LibCore.BasicXmlDocument.Create(xmldata);
			return SetDelayedIncident(xdoc, nt, delayForSecs);
		}

		public IncidentDefinition SetDelayedIncident(BasicXmlDocument xdoc, NodeTree nt, int delayForSecs)
		{
			XmlNode rootNode = xdoc.DocumentElement;
			return SetDelayedIncident(rootNode,nt,delayForSecs);
		}

		public IncidentDefinition SetDelayedIncident(XmlNode rootNode, NodeTree nt, int delayForSecs)
		{
			IncidentDefinition idef = new IncidentDefinition(rootNode, nt);
			idef.doAfterSecs = delayForSecs;
			idef.ApplyAction(nt);
			return idef;
		}

		public bool GetIncidentsOfType(string type, out ArrayList incidentsArray)
		{
			if(incidentTypeToIncidentArray.ContainsKey(type))
			{
				incidentsArray = (ArrayList) incidentTypeToIncidentArray[type];
				return true;
			}
			else
			{
				incidentsArray = new ArrayList();
				return false;
			}
		}

		public IncidentDefinition GetIncident (string id)
		{
			if (incidentDefs.ContainsKey(id))
			{
				return incidentDefs[id] as IncidentDefinition;
			}

			return null;
		}

		public void SetIncidentDefinitions(string xmldata, NodeTree nt)
		{
			//incidentDefs.Clear();
			//
			BasicXmlDocument xdoc = LibCore.BasicXmlDocument.Create(xmldata);
			XmlNode rootNode = xdoc.DocumentElement;

			foreach(XmlNode child in rootNode.ChildNodes)
			{
				if(child.NodeType == XmlNodeType.Element)
				{
					// Add the ability to write simple text rewrite template definitions...
					if (child.Name == "template")
					{
						// A template has "name" and "requires" attributes...
						string t_name = child.Attributes["name"].Value;
						string t_requires = child.Attributes["requires"].Value;

						IncidentTemplate template = new IncidentTemplate();
						template.Name = t_name;

						string[] reqs = t_requires.Split(',');
						foreach (string tr in reqs)
						{
							string str = tr.Trim();
							if (tr.Length > 0)
							{
								template.required_arguments.Add(str);
							}
						}

						template.template = "<r>"+child.InnerXml+"</r>";
						templates[t_name] = template;
					}
					else if (child.Name == "use_template")
					{
						string t_name = child.Attributes["tname"].Value;

						IncidentTemplate template = (IncidentTemplate)templates[t_name];

						string xml_data = template.template;
						// TODO - we should check that all args are used!
						ArrayList used_args = (ArrayList) template.required_arguments.Clone();

						foreach (XmlAttribute att in child.Attributes)
						{
							if (att.Name != "tname")
							{
								// This is a mapped argument.
								xml_data = xml_data.Replace(att.Name, att.Value);
							}
						}

						SetIncidentDefinitions(xml_data, nt);
					}
					else if (child.Name == "include")
					{
						// Include another incident/event definition file...
						System.IO.StreamReader file = new System.IO.StreamReader(AppInfo.TheInstance.Location + "\\" + child.InnerXml);
						string incdata = file.ReadToEnd();
						file.Close();
						file = null;
						SetIncidentDefinitions(incdata, nt);
					}
					else if (child.Name == "i")
					{
						// This is an incident definition.
						//CreateIncidentDefinition(child);
						IncidentDefinition idef = new IncidentDefinition(child, nt);
						if (idef.ID != "")
						{
							// Incidents with an id of "AtStart" should be fired
							// straight away.
							if ("AtStart" == idef.ID)
							{
								idef.ApplyAction(nt);
								atStartIncidents.Add(idef);
							}
							else if (incidentDefs.ContainsKey(idef.ID))
							{
								// Error, should log to an error stream that a duplicate id has been used.
							}
							else
							{
								incidentDefs.Add(idef.ID, idef);
							}
							//
							// We may be an incident that also has a button so check if it has a description
							// and type...
							//
							if (idef.Type != "")
							{
								if (incidentTypeToIncidentArray.ContainsKey(idef.Type))
								{
									ArrayList incidentArray = (ArrayList)incidentTypeToIncidentArray[idef.Type];
									incidentArray.Add(idef);
								}
								else
								{
									ArrayList incidentArray = new ArrayList();
									incidentArray.Add(idef);
									incidentTypeToIncidentArray.Add(idef.Type, incidentArray);
								}
							}
							//
						}
					}
				}
			}

			/*
			foreach(XmlAttribute a in xnode.Attributes)
			{
			}*/

			if (rootNode.Name == "incidents")
			{
				List<string> keys = new List<string>((string[]) (new ArrayList(incidentDefs.Keys)).ToArray(typeof(string)));
				keys.Sort(delegate(string a, string b) { return CONVERT.ParseIntSafe(a, 0).CompareTo(CONVERT.ParseIntSafe(b, 0)); });
			}


			return;
		}

		public IncidentApplier(NodeTree tree)
		{
			targetTree = tree;
		}

		public void Dispose()
		{
			if(null != incidentEntryQueue)
			{
				incidentEntryQueue.ChildAdded -= incidentEntryQueue_ChildAdded;
				incidentEntryQueue = null;
			}
			targetTree = null;
		}

		public void ResetIncidents()
		{
			foreach(IncidentDefinition idef in atStartIncidents)
			{
				idef.ApplyAction(this.targetTree);
			}
		}

		protected void OutputError(string errorText)
		{
			Node errorsNode = this.targetTree.GetNamedNode("FacilitatorNotifiedErrors");
			Node error = new Node(errorsNode, "error", "", new AttributeValuePair( "text", errorText ) );
		}
		/// <summary>
		/// RecurseDefForRemappedTargets is a classic rule re-write rule.
		/// Such things are not pretty but our hand has been forced by the double entered incident
		/// hitting the mirror requirement.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="_IncidentActions"></param>
		/// <param name="mappedTargets"></param>
		/// <param name="fireOntoNewTarget"></param>
		/// <param name="okayOntoNewTarget"></param>
		/// <param name="okayOntoPrimaryTarget"></param>
		/// <param name="fullReason"></param>
		/// <param name="shortReason"></param>
		protected bool RecurseDefForRemappedTargets
			(
			string id,
			ArrayList _IncidentActions,
			ref StringDictionary mappedTargets,
			ref bool fireOntoNewTarget,
			ref bool okayOntoNewTarget,
			ref bool okayOntoPrimaryTarget,
			ref string fullReason,
			ref string shortReason
			)
		{
			// Allow for double entry of the same incident.
			// E.g. If incident X brings down server Y but it is mirrored the facilitator
			// can enter the same incident number again to bring down the mirror.
			// We acheive this by looking at the target node that is being brought down
			// and if it is already down / rebooting / etc. then we apply this incident
			// to the mirror instead.
			//
			// If this incident contains any IDef_Applys that set up="false" or
			// goingDown="true" then we must check to see if we are already down
			// and mirrored in which case we bring down the mirror instead.
			// If we are not mirrored then we should check to see if we will affect
			// any business service that already have an incident affecting them.
			//
			for(int j=0; j<_IncidentActions.Count; ++j)
			{
				ModelActionBase idefPart = (ModelActionBase) _IncidentActions[j];
				//
				// Check whether this IDef is an "if" statement that contains further statements...
				//
				ModelAction_If _if = idefPart as ModelAction_If;
				if(null != _if)
				{
					if (! RecurseDefForRemappedTargets(id, _if.IncidentActions, ref mappedTargets,
						ref fireOntoNewTarget, ref okayOntoNewTarget, ref okayOntoPrimaryTarget,
						ref fullReason, ref shortReason))
					{
						return false;
					}

					if (! RecurseDefForRemappedTargets(id, _if.ElseActions, ref mappedTargets,
						ref fireOntoNewTarget, ref okayOntoNewTarget, ref okayOntoPrimaryTarget,
						ref fullReason, ref shortReason))
					{
						return false;
					}
				}
				//
				// Check whether this IDef applies changes to the network...
				//
				ModelAction_Apply iApply = idefPart as ModelAction_Apply;
				if(null != iApply)
				{
					ArrayList conflictingAttributes = new ArrayList ();

					// We can't turn thermal off if it's currently on.
					bool defaultThermal = false;
					bool newThermal = defaultThermal;
					foreach (AttributeValuePair avp in iApply.valuesToApply)
					{
						switch (avp.Attribute.ToLower())
						{
							case "thermal":
								newThermal = CONVERT.ParseBool(avp.Value, false);
								break;
						}
					}
					conflictingAttributes.Add(new ConflictingBooleanAttribute ("thermal", defaultThermal, newThermal, false, true, "", "", "Already in Overheat. ", "Overheat"));

					// Run through each value to apply.
					// See if we have up="false" or goingDown="true".
					for(int i=0; i < iApply.valuesToApply.Count; ++i)
					{
						AttributeValuePair avp = (AttributeValuePair) iApply.valuesToApply[i];

						string target = iApply.GetTarget();
						Node targetNode = this.targetTree.GetNamedNode(target);

						if(null == targetNode)
						{
							fullReason = "Cannot apply Incident as " + target + " does not exist.";
							shortReason = target + " does not exist";
							return false;
						}

						string mirrorTarget = target + "(M)";
						Node mirror = this.targetTree.GetNamedNode(mirrorTarget);

						// Only check for redirecting onto the mirror if we have a mirror!
						// case2 mirrors are virtual and dont go down
						if (null != mirror && !SkinningDefs.TheInstance.GetBoolData("mirrors_are_virtual", false))
						{
							if( ("up" == avp.Attribute) && ("false" == avp.Value.ToLower()) )
							{
								//
								// This incident is going to push the target down.
								// If we are already down then we should push the mirror down.
								//
								if(! targetTree.GetNamedNode(target).GetBooleanAttribute("up", true))
								{
									// The target is already down so we may have to map over to the new
									// target.
									if(mirror.GetBooleanAttribute("up", true))
									{
										fireOntoNewTarget = true;
										mappedTargets[target] = mirrorTarget;
										//
										// Just in case we check whether there are any overlapping incidents
										// on the mirror...
										//
										/*
										 * We cannot do this anymore as there always will be as we will climb down
										 * the tree and then back up to the biz_service from the biz_service_user...
										 */
										if (!NonOverlappingIncidentConstraint.IsNewIncidentAllowed(id, mirror, ref fullReason, ref shortReason, conflictingAttributes, id))
										{
											okayOntoNewTarget = false;
										}

										//
									}
									else
									{
										// 
										// TODO We should pass an error to the facilitator saying that we can't
										// issue this incident because it is already in effect on both the
										// main target and the mirror.
										//
										string error = "Cannot apply Incident " + id + " because it has already been applied to\r\n";
										error += target + " and its mirror (" + mirrorTarget + ").";

										fullReason = error;
										shortReason = id + " already on " + target + " and mirror";
										return false;
									}
								}
								else
								{
									// Okay, this incident will not be applied to the mirror but we should still check to
									// see if we should apply it to the primary target or not. There may be some overlapping
									// incidents.
									if (!NonOverlappingIncidentConstraint.IsNewIncidentAllowed("", targetNode, ref fullReason, ref shortReason, conflictingAttributes, id))
									{
										okayOntoPrimaryTarget = false;
									}
								}
							}
							else if( ("goingDown" == avp.Attribute) && ("true" == avp.Value) )
							{
								//
								// This incident is going to break this item within the next minute.
								// If the primary target is down or is it is goingDownInSecs then we should
								// hit the mirror instead.
								//
								if( (targetTree.GetNamedNode(target).GetAttribute("goingDownInSecs") != "") ||
									! targetTree.GetNamedNode(target).GetBooleanAttribute("up", true))
								{
									// The target is going down so mark the mirror as going down as well.
									if( (mirror.GetAttribute("goingDownInSecs") != "") ||
										! targetTree.GetNamedNode(target).GetBooleanAttribute("up", true))
									{
										fireOntoNewTarget = true;
										mappedTargets[target] = mirrorTarget;
										//
										// Just in case we check whether there are any overlapping incidents
										// on the mirror...
										//
										if (!NonOverlappingIncidentConstraint.IsNewIncidentAllowed("", mirror, ref fullReason, ref shortReason, conflictingAttributes, id))
										{
											okayOntoNewTarget = false;
										}
									}
									else
									{
										// TODO We should pass an error to the facilitator saying that we can't
										// issue this incident because it is already in effect on both the
										// main target and the mirror.
										string error = "Cannot apply Incident " + id + " because it has already been applied to\r\n";
										error += target + " and its mirror (" + mirrorTarget + ").";

										fullReason = error;
										shortReason = id + " already on " + target + " and mirror";
										return false;
									}
								}
								else
								{
									// Okay, this incident will not be applied to the mirror but we should still check to
									// see if we should apply it to the primary target or not. There may be some overlapping
									// incidents.
									if (!NonOverlappingIncidentConstraint.IsNewIncidentAllowed("", targetNode, ref fullReason, ref shortReason, conflictingAttributes, id))
									{
										okayOntoPrimaryTarget = false;
									}
								}
							}
						}
						else if ((targetNode.GetBooleanAttribute("virtualmirrorinuse", false))
						         && (targetNode.GetAttribute("incident_id") == id))
						{
							// We can re-apply the same incident onto a node if it's currently in virtual mirror state.
							okayOntoPrimaryTarget = true;
						}
						else
						{
							if( ("up" == avp.Attribute) && ("false" == avp.Value.ToLower()) )
							{
								if(!NonOverlappingIncidentConstraint.IsNewIncidentAllowed("",targetNode,ref fullReason, ref shortReason, conflictingAttributes, id))
								{
									okayOntoPrimaryTarget = false;
								}
							}
							else if( ("goingDown" == avp.Attribute) && ("true" == avp.Value.ToLower()) )
							{
								if (!NonOverlappingIncidentConstraint.IsNewIncidentAllowed("", targetNode, ref fullReason, ref shortReason, conflictingAttributes, id))
								{
									okayOntoPrimaryTarget = false;
								}
							}
						}
					}
				}
			}

			return true;
		}

		protected virtual void incidentEntryQueue_ChildAdded(Node sender, Node child)
		{
			if(null != targetTree)
			{
				string id = child.GetAttribute("id");
				FireIncident(id);
				child.Parent.DeleteChildTree(child);
			}
		}

		public IList<IncidentDefinition> GetIncidents ()
		{
			List<IncidentDefinition> incidents = new List<IncidentDefinition> ();
			foreach (IncidentDefinition incidentDefinition in incidentDefs.Values)
			{
				incidents.Add(incidentDefinition);
			}

			return incidents;
		}

		public virtual bool FireIncident(string id)
		{
			bool ret = false;

			if(incidentDefs.ContainsKey(id))
			{
				ret = true;
				IncidentDefinition idef = (IncidentDefinition) incidentDefs[id];
				//
				bool fireOntoMirror = false;
				bool okayOntoMirror = true;
				bool okayOntoPrimaryTarget = true;
				StringDictionary mappedTargets = new StringDictionary();
				//
				string shortReason = "";
				string fullReason = "";
				//
				if (! SkinningDefs.TheInstance.GetBoolData("disable_incident_remapping", false))
				{
					if (! RecurseDefForRemappedTargets(id, idef.IncidentActions, ref mappedTargets,
						ref fireOntoMirror, ref okayOntoMirror, ref okayOntoPrimaryTarget,
						ref fullReason, ref shortReason))
					{
						// Error! This incident cannot be applied!
						return false;
					}
				}

				shortReason = RemapReason(shortReason);

				if(fireOntoMirror)
				{
					if(okayOntoMirror || idef.AllowOverlap)
					{
						// Alter all targets in this incident to the new set of targets.
						IncidentDefinition newIdef = (IncidentDefinition) idef.Clone();
						newIdef.AlterTargets(mappedTargets);
						idef = newIdef;
					}
					else
					{
						// There was an overlapping incident in the network tree from the mirror down.
						// Report this to the facilitator....
						string error = "Cannot apply Incident " + id + " because " + shortReason.Trim();
						OutputError(error);
						return false;
					}
				}
				else
				{
					if(!okayOntoPrimaryTarget && !idef.AllowOverlap)
					{
						// There was an overlapping incident in the network tree from the promary target down.
						// Report this to the facilitator....
						string error = "Cannot apply Incident " + id + " because " + shortReason.Trim();
						OutputError(error);
						return false;
					}
				}
				//
				idef.ApplyAction(targetTree);
			}

			return ret;
		}

		string RemapReason (string reason)
		{
			string originalReason = reason;

			string format = SkinningDefs.TheInstance.GetData("incident_overlap_message_format");

			if (string.IsNullOrEmpty(format)
				|| ! format.Contains("{0}"))
			{
				return reason;
			}

			string preFragment = format.Substring(0, format.IndexOf("{0}"));
			string postFragment = format.Substring(format.IndexOf("{0}") + "{0}".Length);

			List<string> incidents = new List<string> ();
			while (reason.StartsWith(preFragment))
			{
				int wordStart = preFragment.Length;
				int wordEnd = wordStart;
				while ((wordEnd < reason.Length)
				       && (! Char.IsWhiteSpace(reason[wordEnd]))
					   && (postFragment[0] != reason[wordEnd]))
				{
					wordEnd++;
				}

				string incident = reason.Substring(wordStart, wordEnd - wordStart);
				incidents.Add(incident);

				wordEnd = Math.Min(reason.Length, wordEnd + postFragment.Length);
				reason = reason.Substring(wordEnd);
			}

			if (incidents.Count > 1)
			{
				incidents.Sort((string a, string b) => { return CONVERT.ParseInt(a).CompareTo(CONVERT.ParseInt(b)); });
				return CONVERT.Format(SkinningDefs.TheInstance.GetData("incident_overlap_message_format_multiple"),
									  LibCore.Strings.CollapseList(incidents));
			}
			else
			{
				return originalReason;
			}
		}
    }
}