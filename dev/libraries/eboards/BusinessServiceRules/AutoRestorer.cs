using System;
using System.Collections;
using System.Collections.Generic;

using Network;
using LibCore;
using IncidentManagement;

namespace BusinessServiceRules
{
	public class AutoRestorer : IDisposable
	{
		NodeTree model;

		public AutoRestorer (NodeTree model)
		{
			this.model = model;

			ArrayList services = model.GetNodesWithAttributeValue("type", "biz_service");
			foreach (Node service in services)
			{
				WatchService(service);
			}

			model.NodeAdded += model_NodeAdded;
		}

		public void Dispose ()
		{
		}

		void model_NodeAdded (NodeTree sender, Node newNode)
		{
			if (newNode.GetAttribute("type") == "biz_service")
			{
				WatchService(newNode);
			}
		}

		void WatchService (Node service)
		{
			service.AttributesChanged += service_AttributesChanged;
		}

		void service_AttributesChanged (Node sender, ArrayList attrs)
		{
			int downTime = 0;
			bool? awaitingSaaSAutoRestore = null;

			foreach (AttributeValuePair avp in attrs)
			{
				switch (avp.Attribute)
				{
					case "downforsecs":
					case "mirrorforsecs":
						downTime = CONVERT.ParseInt(avp.Value);
						awaitingSaaSAutoRestore = false;
						break;
				}
			}

			if (downTime > 0)
			{
				int maxTime = sender.GetIntAttribute("auto_restore_time", 0);

				string autoRestoreMessageName = "auto_restore_message_" + sender.GetAttribute("name").Replace(" ", "_");

				// Have we specified a maximum downtime?
				if (maxTime > 0)
				{
					// Are we a SaaS service?
					List<Node> apps = new List<Node> ();
					foreach (LinkNode link in sender.GetChildrenOfType("Connection"))
					{
						Node bsu = link.To;

						foreach (LinkNode backLink in bsu.BackLinks)
						{
							Node app = backLink.From;
							if (app.GetAttribute("type") == "App")
							{
								if (! apps.Contains(app))
								{
									apps.Add(app);
								}
							}
						}
					}

					bool allAppsAreSaaS = true;
					foreach (Node app in apps)
					{
						if (! app.GetBooleanAttribute("is_saas", false))
						{
							allAppsAreSaaS = false;
							break;
						}
					}

					if (allAppsAreSaaS && (apps.Count > 0))
					{
						awaitingSaaSAutoRestore = true;
					}

					// Emit a message as soon as the autorestore countdown starts.
					if ((downTime == 1)
						&& (awaitingSaaSAutoRestore ?? false))
					{
						List<AttributeValuePair> attributes = new List<AttributeValuePair> ();
						attributes.Add(new AttributeValuePair ("type", "error"));
						attributes.Add(new AttributeValuePair ("flash", "true"));
						attributes.Add(new AttributeValuePair ("title", "Cloud Provider Update"));
						attributes.Add(new AttributeValuePair ("text", CONVERT.Format("{0} is being automatically restored.",
																					  sender.GetAttribute("name"))));
						new Node (model.GetNamedNode("FacilitatorNotifiedErrors"), "error", autoRestoreMessageName, attributes);
					}

					// Have we been down that long?
					if (downTime >= (maxTime - 1))
					{
						string incident = sender.GetAttribute("incident_id");
						bool fixable = true;

						// Does the incident affect the hub?
						if ((incident != "") && (model.GetNamedNode("Hub").GetAttribute("incident_id") == incident))
						{
							fixable = false;
						}

						// Or facilities?
						if (sender.GetBooleanAttribute("thermal", false) || sender.GetBooleanAttribute("power", false) || sender.GetBooleanAttribute("nopower", false))
						{
							fixable = false;
						}

						if (fixable)
						{
							// Schedule a fix for later (as we have been called from the middle of who knows where,
							// and don't want to upset things by fixing nodes while others are still watching)
							string incidentFix = "<i id=\"AtStart\"> <apply i_name=\"" + sender.GetAttribute("name") + "\" just_been_auto_fixed=\"true\" /> <createNodes i_to=\"FixItQueue\"> <fix target=\"" + sender.GetAttribute("name") + "\" /> </createNodes> </i>";
							IncidentDefinition effect = new IncidentDefinition (incidentFix, model);
							GlobalEventDelayer.TheInstance.Delayer.AddEvent(effect, 1, model);

							Node autoRestoreMessage = model.GetNamedNode(autoRestoreMessageName);
							if (autoRestoreMessage != null)
							{
								autoRestoreMessage.Parent.DeleteChildTree(autoRestoreMessage);
							}
						}
					}
				}
			}

			if (awaitingSaaSAutoRestore.HasValue)
			{
				if (sender.GetBooleanAttribute("awaiting_saas_auto_restore", false) != awaitingSaaSAutoRestore.Value)
				{
					sender.SetAttribute("awaiting_saas_auto_restore", awaitingSaaSAutoRestore.Value);
				}
			}
		}
	}
}