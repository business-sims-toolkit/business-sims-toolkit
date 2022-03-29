using System.Collections;
using LibCore;
using Network;

using CoreUtils;

namespace BusinessServiceRules
{
	/// <summary>
	/// The ML_MIE_IncidentRemover watches a fix queue "FixItQueue" 
	/// If it sees a "fix it" command it will perform the following 
	///   A, Remove all Event Log Messages with that incident code 
	///   B, Change the incident node back to false
	///   C, Change the playing video if needed
	/// 
	/// This is quite different from the stnadrd v3 incident system as there is no real network modelling
	/// in ML_MIE. The app is really just a display for various videos ,messages and a event log 
	/// 
	/// </summary>
	public class ML_MIE_IncidentRemover : ITimedClass
	{
		protected NodeTree targetTree;	//The network Tree
		protected Node fixItQueue;			//The Queue for Incident that need to be removed.
		
		public ML_MIE_IncidentRemover(NodeTree tmpTargetTree)
		{
			targetTree = tmpTargetTree;

			fixItQueue = targetTree.GetNamedNode("FixItQueue");
			fixItQueue.ChildAdded += fixItQueue_ChildAdded;
			
			//this.incidentApplier = applier;
			CoreUtils.TimeManager.TheInstance.ManageClass(this);
		}

		/// <summary>
		/// Displose ...
		/// </summary>
		public void Dispose()
		{
			CoreUtils.TimeManager.TheInstance.UnmanageClass(this);
			if (fixItQueue != null)
			{
				fixItQueue.ChildAdded -= fixItQueue_ChildAdded;
			}
		}

		#region ITimedClass Methods
		/// <summary>
		/// We clear our workarounds on a reset.
		/// </summary>
		public void Reset()
		{
		}
		/// <summary>
		/// We don't care if we the game is stopped or not. We don't have a timer.
		/// </summary>
		public void Start() { /* NOP */ }
		/// <summary>
		/// We don't care if we the game is stopped or not. We don't have a timer.
		/// </summary>
		public void Stop() { /* NOP */ }
		/// <summary>
		/// We don't care if we the game is fast forwarded. We don't have a timer.
		/// </summary>
		public void FastForward(double timesRealTime) { /* NOP */ }

		#endregion

		void handleIncidentRemoval(int IncidentNumber) 
		{
			string incident_node_name = "Incident"+ CONVERT.ToStr(IncidentNumber);

			//Reset the incindent Used flag
			Node incidentFlagNode = targetTree.GetNamedNode(incident_node_name);
			if (incidentFlagNode != null)
			{
				incidentFlagNode.SetAttribute("used", "false");
			}
			
			//Check if we have any items with that incident based reference in the newsfeed
			//If so then we need to remove them  
			Node NewsFeed_Node = targetTree.GetNamedNode("NewsFeed");

			ArrayList kill_items_list = new ArrayList();
			ArrayList existing_items = NewsFeed_Node.getChildrenClone();
			foreach (Node newsitem_node in existing_items)
			{
				int incident_ref = newsitem_node.GetIntAttribute("incident_ref", -1);
				if (incident_ref == IncidentNumber)
				{
					kill_items_list.Add(newsitem_node);
				}
			}
			foreach (Node item_node in kill_items_list)
			{
				item_node.Parent.DeleteChildTree(item_node);
			}
			//Handling the video system 
			Node VideoRequest_Node = targetTree.GetNamedNode("CustomerVideoRequest");
			int incident_ref2 = VideoRequest_Node.GetIntAttribute("incident", -1);
			if (incident_ref2 == IncidentNumber)
			{
				Node VideoDisplay_Node = targetTree.GetNamedNode("CustomerVideo");
				string reset_file = VideoDisplay_Node.GetAttribute("reset_file");

				ArrayList attrs = new ArrayList();
				attrs.Add(new AttributeValuePair("intro_fn", reset_file));
				attrs.Add(new AttributeValuePair("intro_time", "2"));
				attrs.Add(new AttributeValuePair("msg_fn", reset_file));
				attrs.Add(new AttributeValuePair("msg_time", "2"));
				attrs.Add(new AttributeValuePair("incident", "-1"));
				VideoRequest_Node.SetAttributes(attrs);
			}
		}

		void fixItQueue_ChildAdded(Node sender, Node child)
		{
			string target = child.GetAttribute("target");
			int incident_id = child.GetIntAttribute("incident_id",0);

			handleIncidentRemoval(incident_id);

			child.Parent.DeleteChildTree(child); //Remove from the Queue
		}

	}
}