using System;
using System.Collections;
using System.IO;
using System.Xml;

using Network;
using BaseUtils;
using CoreUtils;
using LibCore;

namespace Polestar_PM.DataLookup
{
	/// <summary>
	/// Helper class for handling the installation of new App representing new projects
	/// 
	/// 
	/// 
	/// </summary>
	public class AppInstaller
	{
		NodeTree MyNodeTree = null;

		public AppInstaller(NodeTree tmpNodeTree)
		{
			MyNodeTree = tmpNodeTree;
		}

		public void Dispose()
		{
			MyNodeTree = null;
		}

		public bool location_checks(string location, out bool exists, out bool empty, 
			out Node locationnode, out string errormsg)
		{
			bool passed = false;

			exists = false;
			empty = false;
			locationnode = null;
			errormsg = "";

			ArrayList locations = new ArrayList();
			locations = this.MyNodeTree.GetNodesWithAttributeValue("location",location);
			if (locations.Count>0)
			{
				exists = true;
				locationnode = (Node) locations[0];
				if (locationnode != null)
				{
					string nodetype = locationnode.GetAttribute("type");
					if (nodetype.ToLower() != "slot")
					{
						errormsg = "Location Used";
						passed = false;
					}
					else
					{
						empty = true;
						passed = true;
					}
				}
			}
			else
			{
				errormsg = "Unknown Location";
				passed = false;
				empty = true;
			}
			return passed;
		}

		private bool shouldApplyInstallConstraints()
		{
			bool apply = true;
			Node InstallConstraintsNode = MyNodeTree.GetNamedNode("install_constraints_switch");
			apply = InstallConstraintsNode.GetBooleanAttribute("apply", true);
			return apply;
		}

		private bool hardware_checks(Node ParentServer, int app_memory_required, int app_disk_required, out bool memoryOk, out bool diskOk, out string errmsg)
		{
			bool OpSuccess = false;
			errmsg = "";

			memoryOk = false;
			diskOk = false;

			int memory_capacity_total = ParentServer.GetIntAttribute("mem",0);	//in MB
			int disk_capacity_total = ParentServer.GetIntAttribute("disk",0);		//in GB

			int memory_capacity_used = 0;
			int disk_capacity_used = 0;

			if (shouldApplyInstallConstraints())
			{
				//scan through all children to check how much free space is left
				foreach (Node location_node in ParentServer.getChildren())
				{
					int memory_required = location_node.GetIntAttribute("memrequired", 0);
					int disk_required = location_node.GetIntAttribute("diskrequired", 0);

					memory_capacity_used += memory_required;
					disk_capacity_used += disk_required;
				}
				//Check if we have enough left 
				int memory_free = memory_capacity_total - memory_capacity_used;
				int disk_free = disk_capacity_total - disk_capacity_used;

				memoryOk = (memory_free >= app_memory_required);
				diskOk = (disk_free >= app_disk_required);

				if (memoryOk && diskOk)
				{
					OpSuccess = true;
				}
				else
				{
					if ((! memoryOk) && (! diskOk))
					{
						errmsg = "Memory Storage";
						OpSuccess = false;
					}
					else
					{
						if (! memoryOk)
						{
							errmsg = "Memory";
							OpSuccess = false;
						}
						if (! diskOk)
						{
							errmsg = "Storage";
							OpSuccess = false;
						}
					}
				}
			}
			else
			{
				memoryOk = true;
				diskOk = true;
				OpSuccess = true; 
			}
			return OpSuccess;
		}

		public bool hardware_checks (string location, Node projectNode,
		                             out bool platformOk, out bool ramOk, out bool discOk)
		{
			Node locationNode = null;
			ArrayList locations = MyNodeTree.GetNodesWithAttributeValue("location", location);
			if (locations.Count>0)
			{
				locationNode = (Node) locations[0];
			}

			platformOk = false;
			ramOk = false;
			discOk = false;

			Node projectData = projectNode.GetFirstChildOfType("project_data");
			int mem_requirement = projectData.GetIntAttribute("target_mem_requirement", 0);
			int disk_requirement = projectData.GetIntAttribute("target_disk_requirement", 0);
			int platform_id = projectNode.GetIntAttribute("platform_id", 0);

			Node ParentServer = locationNode.Parent;
			if (ParentServer != null)
			{
				string server_platform = ParentServer.GetAttribute("Platform");
				string new_app_platform = TranslatePlatformToStr(platform_id);
				int memory_required = mem_requirement;
				int disk_required = disk_requirement;

				if (server_platform.ToLower() == new_app_platform.ToLower())
				{
					platformOk = true;
					string errmsg;

					hardware_checks(ParentServer, memory_required, disk_required, out ramOk, out discOk, out errmsg);
				}
			}

			return platformOk && ramOk && discOk;
		}

		private string TranslatePlatformToStr(int platform_id)
		{
			string platform_display_str = "X";
			switch(platform_id)
			{
				case 1:
					platform_display_str = "X";
					break;
				case 2:
					platform_display_str = "Y";
					break;
				case 3:
					platform_display_str = "Z";
					break;
			}
			return platform_display_str;
		}

		private bool Create_New_ProjectAppBySIP(Node ParentServer, Node locationnode,	
			int projectid, int productid, int platformid, string desc, 
			string location, int memory_required, int disk_required)
		{
			bool OpSuccess = false;
			
			//kill off the old location 
			if (locationnode != null)
			{
				locationnode.Parent.DeleteChildTree(locationnode);
			}

			//Create the New App
			string platform_letter = TranslatePlatformToStr(platformid);
			string svr_proczone = ParentServer.GetAttribute("proczone");
			string newAppName = "App"+CONVERT.ToStr(projectid)+"_"+CONVERT.ToStr(productid)+"_"+platform_letter;

			//if (this.MyNodeTree.GetNamedNode(newAppName)==null)
			//{
				ArrayList attrs = new ArrayList ();
				attrs.Add(new AttributeValuePair ("type", "App"));
				attrs.Add(new AttributeValuePair ("proczone", svr_proczone));
				attrs.Add(new AttributeValuePair ("proccap", "1"));
				attrs.Add(new AttributeValuePair ("danger_level", "0"));
				attrs.Add(new AttributeValuePair ("name", newAppName));
				attrs.Add(new AttributeValuePair ("desc", desc));
				attrs.Add(new AttributeValuePair ("location", location));
				attrs.Add(new AttributeValuePair ("version", "1"));
				attrs.Add(new AttributeValuePair ("created_by_sip",CONVERT.ToStr(projectid)));
				attrs.Add(new AttributeValuePair ("source", "new"));
				attrs.Add(new AttributeValuePair ("new", "true"));  //required by flash board view 
				attrs.Add(new AttributeValuePair ("memrequired", CONVERT.ToStr(memory_required)));
				attrs.Add(new AttributeValuePair ("diskrequired", CONVERT.ToStr(disk_required)));
				attrs.Add(new AttributeValuePair ("platform", platform_letter));
				new Node (ParentServer, "node", "", attrs);	
			//}
			OpSuccess = true;

			return OpSuccess;
		}

		private bool Create_New_ProjectAppByCard(Node ParentServer, Node locationnode,
			string card_name, string new_app_name, string platform_letter, string desc, 
			string location, int memory_required, int disk_required)
		{
			bool OpSuccess = false;
			
			//kill off the old location 
			if (locationnode != null)
			{
				locationnode.Parent.DeleteChildTree(locationnode);
			}

			//Create the New App
			string svr_proczone = ParentServer.GetAttribute("proczone");
			string newAppName = new_app_name;

			//if (this.MyNodeTree.GetNamedNode(newAppName)==null)
			//{
			ArrayList attrs = new ArrayList ();
			attrs.Add(new AttributeValuePair ("type", "App"));
			attrs.Add(new AttributeValuePair ("proczone", svr_proczone));
			attrs.Add(new AttributeValuePair ("proccap", "1"));
			attrs.Add(new AttributeValuePair ("danger_level", "0"));
			attrs.Add(new AttributeValuePair ("name", newAppName));
			attrs.Add(new AttributeValuePair ("desc", desc));
			attrs.Add(new AttributeValuePair ("location", location));
			attrs.Add(new AttributeValuePair ("version", "1"));
			attrs.Add(new AttributeValuePair ("created_by_sip",card_name));
			attrs.Add(new AttributeValuePair ("source", "new"));
			attrs.Add(new AttributeValuePair ("new", "true"));  //required by flash board view 
			attrs.Add(new AttributeValuePair ("memrequired", CONVERT.ToStr(memory_required)));
			attrs.Add(new AttributeValuePair ("diskrequired", CONVERT.ToStr(disk_required)));
			attrs.Add(new AttributeValuePair ("platform", platform_letter));
			new Node (ParentServer, "node", "", attrs);	
			//}
			OpSuccess = true;

			return OpSuccess;
		}

		public bool install_project(int project_id, int product_id, int platform_id, 
			string desc, int mem_requirement, int disk_requirement, string location, 
			out string errmsg)
		{
			bool OpSuccess = false;
			bool exists = false;
			bool empty = false;
			Node locationnode = null;

			//No failure mode for ROUND 3


			OpSuccess = location_checks(location, out exists, out empty, out locationnode, out errmsg);
			if (OpSuccess)
			{
				Node ParentServer = locationnode.Parent;
				if (ParentServer != null)
				{
					//check for correct platform 
					string server_platform = ParentServer.GetAttribute("Platform");
					string new_app_platform = TranslatePlatformToStr(platform_id);
					int memory_required = mem_requirement;
					int disk_required = disk_requirement;

					if (server_platform.ToLower() == new_app_platform.ToLower())
					{
						bool ramOk, discOk;
						if (hardware_checks(ParentServer, memory_required, disk_required, out ramOk, out discOk, out errmsg))
						{
							OpSuccess = Create_New_ProjectAppBySIP(ParentServer, locationnode,	project_id, product_id, platform_id, 
								desc, location, memory_required, disk_required);
						}	
						else
						{
							OpSuccess = false;
						}
					}
					else
					{
						errmsg = "Invalid Platform";
						OpSuccess = false;
					}
				}
			}
			return OpSuccess;
		}


		public bool install_change_app(int change_card_id,	int mem_requirement, int disk_requirement, 
			string platform_required,	string new_app_name, string location, out string errmsg)
		{
			bool OpSuccess = false;
			bool exists = false;
			bool empty = false;
			Node locationnode = null;
			string card_name = "cc"+CONVERT.ToStr(change_card_id);

			string desc = "changecard_"+CONVERT.ToStr(change_card_id);

			OpSuccess = location_checks(location, out exists, out empty, 
				out locationnode, out errmsg);
			if (OpSuccess)
			{
				Node ParentServer = locationnode.Parent;
				if (ParentServer != null)
				{
					//check for correct platform 
					string server_platform = ParentServer.GetAttribute("Platform");
					string new_app_platform = platform_required;
					int memory_required = mem_requirement;
					int disk_required = disk_requirement;

					if (server_platform.ToLower() == new_app_platform.ToLower())
					{
						bool ramOk, discOk;
						if (hardware_checks(ParentServer, memory_required, disk_required, out ramOk, out discOk, out errmsg))
						{
							OpSuccess = Create_New_ProjectAppByCard(ParentServer, locationnode,	card_name,
								new_app_name,	platform_required, desc, location, memory_required, disk_required);
						}	
						else
						{
							OpSuccess = false;
						}
					}
					else
					{
						errmsg = "Invalid Platform";
						OpSuccess = false;
					}
				}
			}
			return OpSuccess;
		}

		public bool check_change_app(int mem_requirement, int disk_requirement, 
			string platform_required, string location, out string errmsg)
		{
			bool OpSuccess = true;
			bool exists = false;
			bool empty = false;
			Node locationnode = null;

			OpSuccess = location_checks(location, out exists, out empty, out locationnode, out errmsg);

			if (OpSuccess)
			{
				Node ParentServer = locationnode.Parent;
				if (ParentServer != null)
				{
					//check for correct platform 
					string server_platform = ParentServer.GetAttribute("Platform");
					string new_app_platform = platform_required;
					int memory_required = mem_requirement;
					int disk_required = disk_requirement;

					if (server_platform.ToLower() == new_app_platform.ToLower())
					{
						bool ramOk, discOk;
						if (hardware_checks(ParentServer, memory_required, disk_required, out ramOk, out discOk, out errmsg))
						{
							// NOP - Success.
						}	
						else
						{
							OpSuccess = false;
						}
					}
					else
					{
						errmsg = "Invalid Platform";
						OpSuccess = false;
					}
				}
			}
			return OpSuccess;
		}
	}
}