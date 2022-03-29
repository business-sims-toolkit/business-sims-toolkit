using System;
using System.Collections;
using Network;
using Logging;

namespace BusinessServiceRules
{
	/// <summary>
	/// Summary description for ServiceSpaceCalculator.
	/// </summary>
	public class ServiceSpaceCalculator
	{
		public enum RuleStyle
		{
			THROW_ON_NO_RESOURCE,
			FAIL_APP_ON_NO_RESOURCE,
			LOG_ON_NO_RESOURCE
		}

		ArrayList serverNodes;
		ArrayList appNodes;

		RuleStyle _ruleStyle;

		NodeTree _model;

		public ServiceSpaceCalculator(NodeTree model, RuleStyle ruleStyle)
		{
			_model = model;
			model.NodeAdded += model_NodeAdded;
			_ruleStyle = ruleStyle;
			serverNodes = new ArrayList();
			appNodes = new ArrayList();
			// Get all server nods and set their level to zero and
			// watch for any children being added or removed...
			ArrayList types = new ArrayList();
			types.Add("Server");
			Hashtable nodes = model.GetNodesOfAttribTypes(types);
			foreach(Node serverNode in nodes.Keys)
			{
				AddServer(serverNode);
			}
		}

		void AddServer(Node serverNode)
		{
			serverNodes.Add(serverNode);

			CalculateServerDiskSpace(serverNode);
			CalculateServerMemory(serverNode);

			serverNode.Deleting += serverNode_Deleting;
			serverNode.ChildRemoved += serverNode_ChildRemoved;
			serverNode.ChildAdded += serverNode_ChildAdded;
			serverNode.AttributesChanged +=serverNode_AttributesChanged;

			AddAllAppsOnServer(serverNode);
		}

		void AddAllAppsOnServer(Node serverNode)
		{
			//we need to keep tabs on all childs
			//Apps can chnage, DBs can chnage and we can install stuff to slots
			foreach(Node n in serverNode)
			{
				if( (n.GetAttribute("type") == "App") || (n.GetAttribute("type") == "Database") || (n.GetAttribute("type") == "Slot"))
				{
					if(!appNodes.Contains(n))
					{
						//Attach the monitor
						n.AttributesChanged += appNode_AttributesChanged;
					}
				}
			}
		}

		/// <summary>
		/// Dispose
		/// </summary>
		public void Dispose()
		{
			foreach(Node serverNode in serverNodes)
			{
				serverNode.Deleting -= serverNode_Deleting;
				serverNode.ChildRemoved -= serverNode_ChildRemoved;
				serverNode.ChildAdded -= serverNode_ChildAdded;
				serverNode.AttributesChanged -=serverNode_AttributesChanged;
			}
			
			serverNodes.Clear();

			foreach(Node appNode in appNodes)
			{
				appNode.AttributesChanged -= appNode_AttributesChanged;
			}

			appNodes.Clear();

			_model.NodeAdded -= model_NodeAdded;
		}

		void CalculateServerDiskSpace(Node server)
		{
			int diskLeft = server.GetIntAttribute("disk",0);

			foreach(Node n in server)
			{
				if((n.GetAttribute("type") == "App") || (n.GetAttribute("type") == "Database"))
				{
					int diskRequired = n.GetIntAttribute("diskrequired",0);
					int newDiskLeft = diskLeft - diskRequired;
					if(newDiskLeft < 0)
					{
						// This App cannot run as there is no memory for it to work.
						if(_ruleStyle == RuleStyle.THROW_ON_NO_RESOURCE)
						{
							// Throw an exception
							throw( new Exception("Application " + n.GetAttribute("name") + " does not have enough disk on the server to load.") );
						}
						else if(_ruleStyle == RuleStyle.LOG_ON_NO_RESOURCE)
						{
							AppLogger.TheInstance.WriteLine("Application " + n.GetAttribute("name") + " does not have enough disk on the server to load.");
						}
						else
						{
							// State that the app is down
							ArrayList attrs = new ArrayList();
							attrs.Add( new AttributeValuePair("up","false") );
							attrs.Add( new AttributeValuePair("reasondown","Not enough disk space on server.") );
							n.SetAttributes(attrs);
						}
					}
					else
					{
						diskLeft = newDiskLeft;
					}
				}
			}

			server.SetAttribute("disk_left", diskLeft );
		}

		void CalculateServerLaptopSpace (Node server)
		{
			int left = server.GetIntAttribute("laptops", 0);

			foreach (Node n in server)
			{
				if ((n.GetAttribute("type") == "App") || (n.GetAttribute("type") == "Database"))
				{
					int required = n.GetIntAttribute("laptopsrequired", 0);
					int newLeft = left - required;
					if (newLeft < 0)
					{
						// This App cannot run as there is no memory for it to work.
						if (_ruleStyle == RuleStyle.THROW_ON_NO_RESOURCE)
						{
							// Throw an exception
							throw (new Exception("Application " + n.GetAttribute("name") + " does not have enough laptops on the server to load."));
						}
						else if (_ruleStyle == RuleStyle.LOG_ON_NO_RESOURCE)
						{
							AppLogger.TheInstance.WriteLine("Application " + n.GetAttribute("name") + " does not have enough laptops on the server to load.");
						}
						else
						{
							// State that the app is down
							ArrayList attrs = new ArrayList();
							attrs.Add(new AttributeValuePair("up", "false"));
							attrs.Add(new AttributeValuePair("reasondown", "Not enough laptops on server."));
							n.SetAttributes(attrs);
						}
					}
					else
					{
						left = newLeft;
					}
				}
			}

			server.SetAttribute("laptops_left", left);
		}

		protected void CalculateServerMemory(Node server)
		{
			string ns = server.GetAttribute("name");
			int memLeft = server.GetIntAttribute("mem",0);
			//System.Diagnostics.Debug.WriteLine(" Server " + ns + " has "+memLeft.ToString());

			foreach(Node n in server)
			{
				if((n.GetAttribute("type") == "App")|(n.GetAttribute("type") == "Database"))
				{
					string na = n.GetAttribute("name");
					int memRequired = n.GetIntAttribute("memrequired",0);
					int newMemLeft = memLeft - memRequired;
					//System.Diagnostics.Debug.WriteLine(" SW "+ na +" needs "+memRequired.ToString()+ " leaving "+ newMemLeft.ToString());

					if(newMemLeft < 0)
					{
						// This App cannot run as there is no memory for it to work.
						if(_ruleStyle == RuleStyle.THROW_ON_NO_RESOURCE)
						{
							// Throw an exception
							throw( new Exception("Application " + n.GetAttribute("name") + " does not have enough memory on the server to load.") );
						}
						else if(_ruleStyle == RuleStyle.LOG_ON_NO_RESOURCE)
						{
							AppLogger.TheInstance.WriteLine("Application " + n.GetAttribute("name") + " does not have enough memory on the server to load.");
						}
						else
						{
							// State that the app is down
							ArrayList attrs = new ArrayList();
							attrs.Add( new AttributeValuePair("up","false") );
							attrs.Add( new AttributeValuePair("reasondown","Not enough memory on server.") );
							n.SetAttributes(attrs);
						}
					}
					else
					{
						memLeft = newMemLeft;
					}
				}
			}

			server.SetAttribute("mem_left", memLeft );
		}

		void appNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			// If our memory or disk requirement has changed then alter the server this app is
			// sitting on.
			foreach(AttributeValuePair avp in attrs)
			{
				//handling change of Memory
				if("memrequired" == avp.Attribute)
				{
					CalculateServerMemory(sender.Parent);
				}
				//handling change of Disk
				if("diskrequired" == avp.Attribute)
				{
					CalculateServerDiskSpace(sender.Parent);
				}

				if ("laptopsrequired" == avp.Attribute)
				{
					CalculateServerLaptopSpace(sender.Parent);
				}

				//handling change of Use
				if("type" == avp.Attribute)
				{
					CalculateServerDiskSpace(sender.Parent);
					CalculateServerMemory(sender.Parent);
					CalculateServerLaptopSpace(sender.Parent);
				}
			}
		}

		void serverNode_ChildRemoved(Node sender, Node child)
		{
			// An app may have been removed from this server...
			if( (child.GetAttribute("type") == "App") || (child.GetAttribute("type") == "Database") )
			{
				CalculateServerDiskSpace(sender);
				CalculateServerMemory(sender);
				CalculateServerLaptopSpace(sender);
				appNodes.Remove(child);
				child.AttributesChanged -= appNode_AttributesChanged;
			}
		}

		void serverNode_ChildAdded(Node sender, Node child)
		{
			// An App may have been added to this server...
			if( (child.GetAttribute("type") == "App") || (child.GetAttribute("type") == "Database") )
			{
				CalculateServerDiskSpace(sender);
				CalculateServerMemory(sender);
				CalculateServerLaptopSpace(sender);
				appNodes.Add(child);
				child.AttributesChanged += appNode_AttributesChanged;
			}
		}

		void serverNode_Deleting(Node sender)
		{
			serverNodes.Remove(sender);
			sender.Deleting -= serverNode_Deleting;
			sender.ChildRemoved -= serverNode_ChildRemoved;
			sender.ChildAdded -= serverNode_ChildAdded;
		}

		void model_NodeAdded(NodeTree sender, Node newNode)
		{
			// If this is a server then start watching it...
			if(newNode.GetAttribute("type") == "Server")
			{
				AddServer(newNode);
			}
		}

		void serverNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			foreach(AttributeValuePair avp in attrs)
			{
				if("mem" == avp.Attribute)
				{
					CalculateServerMemory(sender);
				}
				else if("disk" == avp.Attribute)
				{
					CalculateServerDiskSpace(sender);
				}
				else if (avp.Attribute == "laptops")
				{
					CalculateServerLaptopSpace(sender);
				}
			}
		}
	}
}
