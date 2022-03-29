using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

using Network;
using LibCore;

namespace Polestar_Retail.OpsEngine
{
	public class RemoteControlWatcher : IDisposable
	{
		NodeTree model;
		Node businessServicesGroup;
		List<Node> watchedBusinessServices;

		TcpClient tcpClient;

		Queue<string> receivedCommands;

		public RemoteControlWatcher (NodeTree model)
		{
			this.model = model;

			receivedCommands = new Queue<string> ();

			// Listen for incoming TCP connections on port 4242.
			TcpListener tcpListener = new TcpListener (IPAddress.Any, 4242);
			tcpListener.Start();

			// Block until we have a connection.
			tcpClient = tcpListener.AcceptTcpClient();
			tcpListener.Stop();

			// Watch the relevant parts of the game model.
			watchedBusinessServices = new List<Node> ();

			businessServicesGroup = model.GetNamedNode("Business Services Group");
			businessServicesGroup.ChildAdded += businessServicesGroup_ChildAdded;
			businessServicesGroup.ChildRemoved += businessServicesGroup_ChildRemoved;
			foreach (Node businessService in businessServicesGroup.GetChildrenOfType("biz_service"))
			{
				WatchBusinessService(businessService);
			}			
		}

		public void Dispose ()
		{
			businessServicesGroup.ChildAdded -= businessServicesGroup_ChildAdded;
			businessServicesGroup.ChildRemoved -= businessServicesGroup_ChildRemoved;
			foreach (Node businessService in new List<Node> (watchedBusinessServices))
			{
				UnWatchBusinessService(businessService);
			}
			tcpClient.Close();
		}

		void WatchBusinessService (Node businessService)
		{
			businessService.AttributesChanged += businessService_AttributesChanged;
			watchedBusinessServices.Add(businessService);
		}

		void UnWatchBusinessService (Node businessService)
		{
			businessService.AttributesChanged -= businessService_AttributesChanged;
			watchedBusinessServices.Remove(businessService);
		}

		void businessServicesGroup_ChildAdded (Node parent, Node child)
		{
			WatchBusinessService(child);
		}

		void businessServicesGroup_ChildRemoved (Node parent, Node child)
		{
			UnWatchBusinessService(child);
		}

		void businessService_AttributesChanged (Node sender, System.Collections.ArrayList attributes)
		{
			int? incidentId = sender.GetIntAttribute("incident_id");
			string [] businessesAffected = sender.GetAttribute("users_down").Split(',');
		}

		public void Update ()
		{
			while (receivedCommands.Count > 0)
			{
				string command;
				lock (receivedCommands)
				{
					command = receivedCommands.Dequeue();
				}

				ExecuteCommand(command);
			}
		}

		void ExecuteCommand (string args)
		{
			string command = args;
			string tail = "";
			int parenthesisPosition = command.IndexOf("(");
			if (parenthesisPosition != -1)
			{
				command = args.Substring(0, parenthesisPosition);
				tail = args.Substring(parenthesisPosition + 1, args.Length - 1 - (parenthesisPosition + 1));
			}

			switch (command)
			{
				case "EnterIncident":
					EnterIncident(tail);
					break;

				case "RemoveIncident":
					RemoveIncident(tail);
					break;
			}
		}

		void EnterIncident (string incidentId)
		{
			Node entryQueue = model.GetNamedNode("enteredIncidents");

			new Node (entryQueue, "", "", new AttributeValuePair ("id", incidentId));
		}

		void RemoveIncident (string incidentId)
		{
			Node fixQueue = model.GetNamedNode("FixItQueue");

			new Node (fixQueue, "", "", new AttributeValuePair("incident_id", incidentId));
		}
	}
}