using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using Network;
using LibCore;

namespace Cloud.OpsEngine
{
	public enum emPublicVendorEventType
	{
		OPENING,
		CLOSING,
		PRICE_CHANGE
	}

	public class PublicVendorEvent : IDisposable
	{
		public const int DisplayTime_ChangingState = 180;

		public Node PV_Node = null;
		public string CommonServiceName = string.Empty;
		public emPublicVendorEventType event_type = emPublicVendorEventType.OPENING;
		public int startTime = 0;
		public int stopTime = 0;
		public Hashtable on_active_attrs = new Hashtable();
		public Hashtable on_completed_attrs = new Hashtable();
		public string status = string.Empty; //pending, running, completed
		public int round = 1;

		NodeTree model;
		Node timeNode;

		public void Dispose ()
		{
			timeNode.AttributesChanged -= new Node.AttributesChangedEventHandler (timeNode_AttributesChanged);
		}

		public PublicVendorEvent (NodeTree model, int current_round, Node node)
		{
			this.model = model;
			round = current_round;

			PV_Node = model.GetNamedNode(node.GetAttribute("provider"));
			CommonServiceName = node.GetAttribute("common_service_name");
			startTime = node.GetIntAttribute("time", 0);
			stopTime = startTime + 120;

			on_active_attrs = new Hashtable();
			on_completed_attrs = new Hashtable();

			//Extract the Various Attributes and Event Type
			if (AddAttributes(on_active_attrs, node, "available"))
			{
				string avail_setting = (string) on_active_attrs["available"];
				if (avail_setting.Equals("true",StringComparison.InvariantCultureIgnoreCase))
				{
					event_type = emPublicVendorEventType.OPENING;
				}
				else
				{
					event_type = emPublicVendorEventType.CLOSING;
				}
			}
			else
			{
				//There is no "Available" setting so this is a Price Change
				event_type = emPublicVendorEventType.PRICE_CHANGE;
				AddAttributes(on_active_attrs, node, "cost_per_realtime_minute");
				AddAttributes(on_active_attrs, node, "cost_per_round");
				AddAttributes(on_active_attrs, node, "cost_per_user_per_round");
				AddAttributes(on_active_attrs, node, "cost_per_trade");
				on_active_attrs.Add("alert", "true");
				on_completed_attrs.Add("alert", "false");
			}
			timeNode = model.GetNamedNode("CurrentTime");
			timeNode.AttributesChanged += new Node.AttributesChangedEventHandler(timeNode_AttributesChanged);

			status = "pending";
		}

		void process_SetOpening()
		{
			PV_Node.SetAttribute("status", "opening");
			//System.Diagnostics.Debug.WriteLine("Opening " + PV_Node.GetAttribute("name"));
		}

		void process_SetCountDown(int count_time)
		{
			PV_Node.SetAttribute("count_down", CONVERT.ToStr(count_time));
			//System.Diagnostics.Debug.WriteLine("Count " + PV_Node.GetAttribute("name") + "  count_down " + CONVERT.ToStr(count_time));
		}

		void process_SetOpened()
		{
			PV_Node.SetAttribute("status", "available");
			PV_Node.SetAttribute("available", "true");
			PV_Node.SetAttribute("count_down", "-1");
			//System.Diagnostics.Debug.WriteLine("Opened " + PV_Node.GetAttribute("name"));
		}

		void process_SetClosing()
		{
			PV_Node.SetAttribute("status", "closing");
			//System.Diagnostics.Debug.WriteLine("Closing " + PV_Node.GetAttribute("name"));
		}

		void process_SetClosed()
		{
			PV_Node.SetAttribute("status", "not_available");
			PV_Node.SetAttribute("available", "false");
			PV_Node.SetAttribute("count_down", "-1");
			//System.Diagnostics.Debug.WriteLine("Closed " + PV_Node.GetAttribute("name"));
		}

		void process_PriceChange_Start()
		{
			//System.Diagnostics.Debug.WriteLine("Price Change Start" + PV_Node.GetAttribute("name"));
			
			foreach (Node nd in PV_Node.getChildren())
			{
				string csn = nd.GetAttribute("common_service_name");
				ArrayList new_attrs = new ArrayList();

				if (string.IsNullOrEmpty(csn) && string.IsNullOrEmpty(CommonServiceName))
				{
					foreach (string att_name in on_active_attrs.Keys)
					{
						if ((!string.IsNullOrEmpty(nd.GetAttribute(att_name)))
							|| (att_name == "alert"))
						{
							string att_value = (string)on_active_attrs[att_name];

							new_attrs.Add(new AttributeValuePair(att_name, att_value));
						}
					}
				}
				else
				{
					if (csn.Equals(CommonServiceName, StringComparison.InvariantCultureIgnoreCase))
					{
						//build a change list 
						foreach (string att_name in on_active_attrs.Keys)
						{
							string att_value = (string)on_active_attrs[att_name];

							new_attrs.Add(new AttributeValuePair(att_name, att_value));
						}
					}
				}

				if (new_attrs.Count > 0)
				{
					nd.SetAttributes(new_attrs);
				}
				status = "running";
			}

		}

		void process_PriceChange_Stop()
		{
			//PV_Node.SetAttribute("alert", "false");
			foreach (Node nd in PV_Node.getChildren())
			{
				string csn = nd.GetAttribute("common_service_name");
				ArrayList new_attrs = new ArrayList();

				if (string.IsNullOrEmpty(csn) && string.IsNullOrEmpty(CommonServiceName))
				{
					foreach (string att_name in on_completed_attrs.Keys)
					{
						if ((!string.IsNullOrEmpty(nd.GetAttribute(att_name)))
							|| (att_name == "alert"))
						{
							string att_value = (string)on_completed_attrs[att_name];

							new_attrs.Add(new AttributeValuePair(att_name, att_value));
						}
					}
				}
				else
				{
					if (csn.Equals(CommonServiceName, StringComparison.InvariantCultureIgnoreCase))
					{
						//build a change list 
						foreach (string att_name in on_completed_attrs.Keys)
						{
							string att_value = (string)on_completed_attrs[att_name];

							new_attrs.Add(new AttributeValuePair(att_name, att_value));
						}
					}
				}

				if (new_attrs.Count > 0)
				{
					nd.SetAttributes(new_attrs);
				}
			}
			status = "completed";
		}

		void timeNode_AttributesChanged (Node sender, ArrayList attrs)
		{
			int time = timeNode.GetIntAttribute("seconds", 0);

			int annoucement_time = (startTime - DisplayTime_ChangingState);
			bool isAnnoucementTime = (startTime - DisplayTime_ChangingState) == time;
			bool duringAnnoucementTime = (time >= annoucement_time) && ((time <= startTime));
			int count_down = DisplayTime_ChangingState - Math.Abs(time - (annoucement_time));

			switch (event_type)
			{ 
				case emPublicVendorEventType.OPENING:
					if (annoucement_time== time)
					{
						process_SetOpening();
					}
					if (duringAnnoucementTime)
					{
						process_SetCountDown(count_down);
					}
					if ((startTime) == time)
					{
						process_SetOpened();
					}
					break;
				case emPublicVendorEventType.CLOSING:
					if (annoucement_time == time)
					{
						process_SetClosing();
					}
					if (duringAnnoucementTime)
					{
						process_SetCountDown(count_down);
					}
					if ((startTime) == time)
					{
						process_SetClosed();
					}
					break;
				case emPublicVendorEventType.PRICE_CHANGE:
					if ((startTime) == time)
					{
						process_PriceChange_Start();
					}
					if ((stopTime) == time)
					{
						process_PriceChange_Stop();
					}
					break;
			}
		}

		bool AddAttributes (Hashtable attributeToNewValue, Node node, string attributeName)
		{
			string newAttributeValue = node.GetAttribute(attributeName);
			if (! string.IsNullOrEmpty(newAttributeValue))
			{
				attributeToNewValue[attributeName] = newAttributeValue;
				return true;
			}
			return false;
		}
	}

	public class PublicVendorManager
	{
		protected NodeTree model;
		protected int current_round = 1;
		List<PublicVendorEvent> changeEvents;

		public PublicVendorManager (NodeTree model, int round)
		{
			this.model = model;

			current_round = round;

			PreProcessVendors();
			Build_Change_Events();
		}

		public void Dispose ()
		{
			foreach (PublicVendorEvent change in changeEvents)
			{
				change.Dispose();
			}
		}

		protected void PreProcessVendors()
		{
			Node CP_list_node = model.GetNamedNode("Cloud Providers");
			foreach (Node cp_node in CP_list_node.getChildren())
			{
				string type = cp_node.GetAttribute("type");
				if (type.Equals("cloud_provider", StringComparison.InvariantCultureIgnoreCase))
				{
					int avail_round = cp_node.GetIntAttribute("available_from_round", -1);
					bool is_CP_Available = false;

					if (avail_round != -1)
					{
					 if (current_round >= avail_round)
					 {
						 is_CP_Available = true;
					 }
					}
					//set the status 
					if (is_CP_Available)
					{
						cp_node.SetAttribute("status", "available");
                        cp_node.SetAttribute("available", "true");
					}
					else
					{
                        cp_node.SetAttribute("status", "not_available");
                        cp_node.SetAttribute("available", "false");
					}
				}
			}
		}

		protected void Build_Change_Events ()
		{
			changeEvents = new List<PublicVendorEvent> ();

			foreach (Node roundChanges in model.GetNamedNode("Cloud Provider Changes").GetChildrenOfType("cloud_provider_round_changes"))
			{
				if (roundChanges.GetIntAttribute("round", 0) == current_round)
				{
					foreach (Node change in roundChanges.getChildren())
					{
						PublicVendorEvent changeEvent = new PublicVendorEvent(model, current_round, change);
						changeEvents.Add(changeEvent);
					}
				}
			}
		}

		public bool DoesSaaSServiceChangePriceThisRound (Node businessService)
		{
			foreach (PublicVendorEvent changeEvent in changeEvents)
			{
				if (changeEvent.CommonServiceName == businessService.GetAttribute("common_service_name"))
				{
					return true;
				}
			}

			return false;
		}
	}
}