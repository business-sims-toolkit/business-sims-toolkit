using System.Collections;
using System.Collections.Generic;
using LibCore;

using Network;

namespace IncidentManagement
{
    public class WestRecurringIncidentDetector : RecurringIncidentDetector
    {

        Dictionary<string, List<Node>> incidentIdToNodes;

        public WestRecurringIncidentDetector (NodeTree model) :
            base(model)
        {
            foreach (Node node in watchedNodes.Keys)
            {
                node.PreAttributesChanged += node_PreAttributesChanged;
            }

            incidentIdToNodes = new Dictionary<string, List<Node>>();

        }

        protected void node_PreAttributesChanged(Node sender, ref ArrayList attrs)
        {
            if ((attrs != null) && (attrs.Count > 0))
            {
                foreach (AttributeValuePair avp in attrs)
                {
                    if (avp.Attribute.Equals("up"))
                    {
                        string incidentId = sender.GetAttribute("incident_id");
                        if (!string.IsNullOrEmpty(incidentId))
                        {
                            if (incidentIdToNodes.ContainsKey(incidentId))
                            {
                                incidentIdToNodes.Remove(incidentId);
                                return;
                            }
                        }
                        else
                        {
                            break;
                        }
                        
                    }
                }
            }
        
        }
        
        protected new void AddWatchedNode(Node n, string type)
        {
            base.AddWatchedNode(n, type);

            n.PreAttributesChanged += node_PreAttributesChanged;
        }

        protected new void RemoveWatchedNode(Node n)
        {
            base.RemoveWatchedNode(n);

            n.PreAttributesChanged -= node_PreAttributesChanged;
        }

        protected override void n_AttributesChanged(Node sender, ArrayList attrs)
        {
            Node effectiveNodeDown = sender;
            int numDown = 1;

            string incidentId = string.Empty;

            bool penalty = false;
            foreach (AttributeValuePair avp in attrs)
            {
                if (avp.Attribute.ToLower() == "penalty")
                {
                    penalty = (avp.Value.ToLower() == "yes");
                }
            }

            string thermal = "";
            if (sender.GetBooleanAttribute("thermal", false))
            {
                thermal = ";thermal";
            }

            string noPower = "";
            if (sender.GetBooleanAttribute("nopower", false))
            {
                noPower = ";nopower";
            }

            foreach (AttributeValuePair avp in attrs)
            {
                if (avp.Attribute == "incident_id")
                {
                    incidentId = avp.Value;

                    if (!string.IsNullOrEmpty(incidentId))
                    {

                       


                        // Find out what we should set as the number of biz services affected and what
                        // the effective node down is...
                        string type = sender.GetAttribute("type");
                        if ("App" == type)
                        {
                            numDown = 0;
                            foreach(Node child in sender)
                            {
                                if (child.GetAttribute("type") == "Connection")
                                {
                                    ++numDown;
                                }
                                else if (child.GetAttribute("type") == "SupportTech")
                                {
                                    numDown += child.getChildren().Count;
                                }
                            }
                        }
                        else if ("SupportTech" == type)
                        {
                            effectiveNodeDown = sender.Parent;
                            numDown = sender.getChildren().Count;
                        }
                        else if ("Connection" == type)
                        {
                            effectiveNodeDown = sender.Parent;
                            if (effectiveNodeDown.GetAttribute("type") == "SupportTech")
                            {
                                effectiveNodeDown = effectiveNodeDown.Parent;
                            }
                        }

                        string record = CONVERT.Format("{0}{1}{2}", numDown, thermal, noPower);


                        // Find out if this incident has recently been passed through (WEST-184)
                        if (incidentIdToNodes.ContainsKey(incidentId))
                        {
                            incidentIdToNodes[incidentId].Add(sender);
                            return;
                        }

                        incidentIdToNodes.Add(incidentId, new List<Node>());
                        incidentIdToNodes[incidentId].Add(sender);

                        // Is this a recurring incident?
                        ArrayList numDownArray = null;
                        if (previousIncidents.ContainsKey(effectiveNodeDown))
                        {
                            // Yes, so check to see if the same number down occurred...
                            numDownArray = (ArrayList) previousIncidents[effectiveNodeDown];
                            if (numDownArray.Contains(record))
                            {
                                // This is a recurring incident

                                // : fix for 4571 (install penalties shouldn't count as recurring incidents).
                                if (!penalty)
                                {
                                    ArrayList attrs2 = new ArrayList();
                                    attrs2.Add(new AttributeValuePair("type", "recurring_incident"));
                                    attrs2.Add(new AttributeValuePair("incident_id", incidentId));
                                    Node ce = new Node(costedEventsNode, "recurring_incident", "", attrs2);

                                    return;
                                }
                            }
                        }
                        else
                        {
                            // No.
                            numDownArray = new ArrayList();
                            previousIncidents.Add(effectiveNodeDown, numDownArray);
                        }

                        numDownArray.Add(record);
                    }
                }
            }

        }



    }

    
}
