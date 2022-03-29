using System.Collections.Generic;
using Network;

namespace DevOps.ReportsScreen
{
	internal class BusinessServiceComparer: IComparer<string>
    {
	    NodeTree model;
        public BusinessServiceComparer(NodeTree network)
        {
            model = network;
        }

        public int Compare(string leftHandSide, string rightHandSide)
        {
            Node leftNode = model.GetNamedNode(leftHandSide);
            Node rightNode = model.GetNamedNode(rightHandSide);
            int comparison = 0;
           
            if (leftNode == null && rightNode == null)
            {
                return 0;
            }
            else if (leftNode == null)
            {
                return 1;
            }
            else if (rightNode == null)
            {
                return -1;
            }

            // Now handle the actual comparison.
            int leftHandSideGanttOrder = leftNode.GetIntAttribute("gantt_order", 0);
            int rightHandSideGanttOrder = rightNode.GetIntAttribute("gantt_order", 0);

            if (leftHandSideGanttOrder == rightHandSideGanttOrder)
            {
                comparison = 0;
            }
            else if(leftHandSideGanttOrder > rightHandSideGanttOrder)
            {
                comparison = 1;
            }
            else
            {
                comparison = -1;
            }

            return comparison;
        }
    }
}
