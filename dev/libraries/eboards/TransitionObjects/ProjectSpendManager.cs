using System.Collections;
using LibCore;
using Network;

namespace TransitionObjects
{
	/// <summary>
	/// Summary description for ProjectSpendManager.
	/// </summary>
	public class ProjectSpendManager
	{
		NodeTree MyNodeTree;							//Root of World tree
		string CurrentRound = "1";				//Which round are we interested in 
		Node MyProjectsRunningNode;				//World Node that contain all Projects 
		Node MyDevelopmentSpendNode;			//World Node that contains the totals
		Hashtable MyRunningProjects;			//My List of the running projects 

		/// <summary>
		/// Constructor 
		/// attaches to project tag and handles any existing children
		/// </summary>
		/// <param name="tree"></param>
		/// <param name="Round"></param>
		public ProjectSpendManager(NodeTree tree, string Round)
		{
			CurrentRound = Round;
			MyRunningProjects = new Hashtable();
			MyNodeTree = tree;
			
			MyProjectsRunningNode = tree.GetNamedNode("Projects");
			MyDevelopmentSpendNode = tree.GetNamedNode("DevelopmentSpend");
			//connect up to existing children (only those for this round)
			ArrayList existingkids = MyProjectsRunningNode.getChildren();
			foreach (Node kid in existingkids)
			{
				string projectround = kid.GetAttribute("createdinround");
				if (projectround.ToLower()==CurrentRound.ToLower())
				{
					AddChild(kid);
				}
			}
			// TODO : Should for thouroughness run over the "Projects" node and create any projects that are already there.
			MyProjectsRunningNode.ChildAdded +=MyProjectsRunningNode_ChildAdded;
			MyProjectsRunningNode.ChildRemoved +=MyProjectsRunningNode_ChildRemoved;
		}


		/// <summary>
		/// Detaches this from all the objects 
		/// </summary>
		public void Dispose()
		{
			//Detach from any existing monitored project nodes 
			foreach (object o1 in MyRunningProjects.Values)
			{
				Node child = (Node) o1;
				child.AttributesChanged -= child_AttributesChanged;
			}
			//Clear my list of monitored nodes
			MyRunningProjects.Clear();
			//Detach from the main project node
			MyProjectsRunningNode.ChildAdded -=MyProjectsRunningNode_ChildAdded;
			MyProjectsRunningNode.ChildRemoved -=MyProjectsRunningNode_ChildRemoved;
		}

		void RebuildTheMoney()
		{
			int CurrentSpendTotal =0;
			int ActualCostTotal = 0;
			int StoredCurrentSpendTotal =0;
			int StoredActualCostTotal = 0;

			//Iterate over all projects 
			foreach (object o1 in MyRunningProjects.Values)
			{
				Node child = (Node) o1;
				string currentspendstr = child.GetAttribute("currentspend");
				int currentspend = CONVERT.ParseInt(currentspendstr);
				string actual_coststr = child.GetAttribute("actual_cost");
				int actual_cost = CONVERT.ParseInt(actual_coststr);

				CurrentSpendTotal += currentspend;
				ActualCostTotal += actual_cost;
			}
			//Extract the Money from the Main Total 

			StoredCurrentSpendTotal = MyDevelopmentSpendNode.GetIntAttribute("CurrentSpendTotal",0);
			StoredActualCostTotal = MyDevelopmentSpendNode.GetIntAttribute("ActualCostTotal",0);

			if ((StoredCurrentSpendTotal != CurrentSpendTotal)|(StoredActualCostTotal != ActualCostTotal))
			{
				ArrayList Attrs = new ArrayList();
				Attrs.Add( new AttributeValuePair("CurrentSpendTotal",CurrentSpendTotal));
				Attrs.Add( new AttributeValuePair("ActualCostTotal",ActualCostTotal));
				MyDevelopmentSpendNode.SetAttributes(Attrs);
			}
		}

		void AddChild(Node child)
		{
			//only connect those project that are created in the round that i am interested in 
			string projectround = child.GetAttribute("createdinround");
			if (projectround.ToLower()==CurrentRound.ToLower())
			{
				//attach to the node
				child.AttributesChanged +=child_AttributesChanged;			
				//Add to the montiored list
				string projectid = child.GetAttribute("projectid");
				MyRunningProjects.Add(projectid, child);
				//Rebuild the Money
				RebuildTheMoney();
			}
		}

		void RemoveChild(Node child)
		{
			string projectid = child.GetAttribute("projectid");
			if (MyRunningProjects.ContainsKey(projectid))
			{
				child.AttributesChanged -= child_AttributesChanged;
			}
			MyRunningProjects.Remove(projectid);
			RebuildTheMoney();
		}

		/// <summary>
		/// Handling a Project Created Node
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="child"></param>
		void  MyProjectsRunningNode_ChildAdded(Node sender, Node child)
		{
			AddChild(child);
		}

		/// <summary>
		/// Handling a Project Removed Node
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="child"></param>
		void MyProjectsRunningNode_ChildRemoved(Node sender, Node child)
		{
			RemoveChild(child);
		}

		void OutputError(string errorText)
		{
			Node errorsNode = MyNodeTree.GetNamedNode("FacilitatorNotifiedErrors");
			Node error = new Node(errorsNode, "error", "", new AttributeValuePair( "text", errorText ) );
		}

		/// <summary>
		/// Handling an Project Attribute changing 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="attrs"></param>
		void child_AttributesChanged(Node sender, ArrayList attrs)
		{
			if (attrs != null && attrs.Count > 0)
			{
				foreach(AttributeValuePair avp in attrs)
				{
					//Extraction of the data attribute
					string attribute = avp.Attribute;
					string newValue = avp.Value;
					if ((attribute=="currentspend")|(attribute=="actual_cost"))
					{
						RebuildTheMoney();
					}
				}
			}
		}
	}
}
