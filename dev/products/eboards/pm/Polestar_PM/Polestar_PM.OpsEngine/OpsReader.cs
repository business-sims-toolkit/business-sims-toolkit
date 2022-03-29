using System;
using System.Collections;
using System.IO;
using System.Xml;

using Network;

using LibCore;
using CoreUtils;
using Polestar_PM.DataLookup;

namespace Polestar_PM.OpsEngine
{

	/// <summary>
	/// The Ops Reader allows Tyhe Ui obkject to be able to read information from the Ops Node System
	/// </summary>
	public class OpsReader
	{
		protected NodeTree MyNodeTree = null;
		protected Node current_day_node = null;
		protected Node ops_worklist_node = null;
		protected Node fsc_list_node = null;
		protected Node changecard_list_node = null;
		protected Node PMO_BudgetNode = null;
		protected Node ops_round_results_node = null;

		public OpsReader(NodeTree tree)
		{
			MyNodeTree = tree;
			//Where to take the money from 
			PMO_BudgetNode = MyNodeTree.GetNamedNode("pmo_budget"); 
			//list of work to do (each ops job is a sub node)
			ops_worklist_node = tree.GetNamedNode("ops_worklist");

			//Needed for End of Round Calculations 
			fsc_list_node = tree.GetNamedNode("fsc_list");
			changecard_list_node = tree.GetNamedNode("change_list");
			ops_round_results_node = tree.GetNamedNode("operational_results");
		}

		public void Dispose()
		{
			MyNodeTree = null;

			PMO_BudgetNode = null;
			ops_worklist_node = null;

			fsc_list_node = null;
			changecard_list_node = null;
		}

		public void isDayFree(int requested_day, out bool totallyFree, out bool projects_allowed)
		{
			totallyFree = true;
			projects_allowed = true;

			//check if we are doing a job at the moment
			foreach (Node ops_job in ops_worklist_node.getChildren())
			{
				string type = ops_job.GetAttribute("type");
				string action = ops_job.GetAttribute("action");
				string status = ops_job.GetAttribute("status");
				int day = ops_job.GetIntAttribute("day",-1);
				int days_left = ops_job.GetIntAttribute("days_left",-1);
				int duration = ops_job.GetIntAttribute("duration",-1);
				string dest = ops_job.GetAttribute("dest");

				if (day == requested_day)
				{
					totallyFree = false;
				}
				if (duration > 1)
				{
					if ((requested_day>=day)&(requested_day<=(day+(duration-1))))
					{
						totallyFree = false;
					}
				}
			}
		}

	}
}
