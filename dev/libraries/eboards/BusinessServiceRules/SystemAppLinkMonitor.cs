using System.Collections;
using Network;

namespace BusinessServiceRules
{
	/// <summary>
	/// The SystemAppLinkMonitor handles the reseting of Application Links 
	/// When An application is upgraded then the xml will clear any incidents placed on that app
	/// But any incidents which are on the Links (which are children of the App) may have incidents.
	/// Reseting these incidents is difficult to do in the application upgrade xml (fragile and hardcoded)
	/// So the best option is to have a monitor that will bring up the links if needed 
	/// </summary>
	public class SystemAppLinkMonitor
	{
		protected NodeTree _model;
		protected Node _AppUpgradeResetLinksQueueNode;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="nt"></param>
		public SystemAppLinkMonitor(NodeTree nt)
		{
			_model = nt;
			_AppUpgradeResetLinksQueueNode = _model.GetNamedNode("AppUpgradeResetLinksQueue");
			_AppUpgradeResetLinksQueueNode.ChildAdded +=_AppUpgradeResetLinksQueueNode_ChildAdded;
		}

		/// <summary>
		/// Dispose all event handlers and anything else
		/// </summary>
		public void Dispose()
		{
			if (_AppUpgradeResetLinksQueueNode != null)
			{
				_AppUpgradeResetLinksQueueNode.ChildAdded -=_AppUpgradeResetLinksQueueNode_ChildAdded;
				_AppUpgradeResetLinksQueueNode = null;
			}
			_model = null;
		}

		//handle the incoming request to bring up the links 
		void _AppUpgradeResetLinksQueueNode_ChildAdded(Node sender, Node child)
		{
			string node_type = child.GetAttribute("type");
			string node_desc = child.GetAttribute("desc");
			string node_ref = child.GetAttribute("ref");
			ArrayList al = null;

			//The application to check
			Node app_node = _model.GetNamedNode(node_ref);
			if (app_node != null)
			{
				string node_name = app_node.GetAttribute("name");
				al = app_node.getChildren();

				foreach(Node kid in al)
				{
					string up_status = kid.GetAttribute("up");
					string incident_id = kid.GetAttribute("incident_id");
					string going_down_secs_status = kid.GetAttribute("goingDownInSecs");
					string node_kname = kid.GetAttribute("name");
					
					//If it's down then bring it up and clear 
					if (up_status.ToLower()=="false")
					{
						kid.SetAttribute("up","true");					
						kid.SetAttribute("incident_id","");					
						kid.SetAttribute("rebootingForSecs","");
					}
					//System.Diagnostics.Debug.WriteLine("LINK "+node_kname);
				}
			}
		}
	}
}
