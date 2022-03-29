using System;
using System.Collections;
using LibCore;
using Network;

using CoreUtils;

namespace BusinessServiceRules
{
	/// <summary>
	/// The ServiceDownCounter Counts How Long Services Have Been Down.
	/// </summary>
	public class ServiceDownCounter : ITimedClass
	{
		ArrayList downedNodes = new ArrayList();
		protected NodeTree model;

		Node currentTimeNode;
		Boolean Enabled = true;

		public ServiceDownCounter(NodeTree nt)
		{
			model = nt;
			CoreUtils.TimeManager.TheInstance.ManageClass(this);

			//The count down time counter is always on as part of correct engine procedure 
			//that way we can handle the sla breach etc 
			//it's the displays that we switch on an off 
			Enabled = true;
				
			currentTimeNode = nt.GetNamedNode("CurrentTime");
			currentTimeNode.AttributesChanged += currentTimeNode_AttributesChanged;
		}

		/// <summary>
		/// Dispose ....
		/// </summary>
		public virtual void Dispose()
		{
			CoreUtils.TimeManager.TheInstance.UnmanageClass(this);
			currentTimeNode.AttributesChanged -= currentTimeNode_AttributesChanged;
		}

		/// <summary>
		/// Helper method for reseting the count for all our nodes
		/// </summary>
		void ClearCounts()
		{
			foreach (Node n in downedNodes)
			{
				n.SetAttribute("downforsecs","0");
			}
		}

		public void Clear()
		{
			lock(this)
			{
				ClearCounts();
				downedNodes.Clear();
			}
		}

		public void Reset()
		{
			lock(this)
			{
				ClearCounts();
				downedNodes.Clear();
			}
		}

		public void Start()
		{
		}

		public void Stop()
		{

		}

		public void FastForward(double timesRealTime)
		{
		}

		public void AddNode(Node n)
		{
			lock(this)
			{
				if(!downedNodes.Contains(n))
				{
					//string nodename = n.GetAttribute("name");
					//System.Diagnostics.Debug.WriteLine("SDC AddNode  "+nodename);
					downedNodes.Add(n);
				}
			}
		}

		void ResetNonZero(Node n, string attrname)
		{
			string t_down = n.GetAttribute(attrname);
			if("" == t_down) t_down = "0";
			int downFor = CONVERT.ParseInt(t_down);
			if (downFor != 0)
			{
				downFor=0;
			}
			n.SetAttribute(attrname,downFor);
		}

		public void RemoveNode(Node n, bool resetRequired)
		{
			lock(this)
			{
				try
				{
					//string nodename = n.GetAttribute("name");
					//System.Diagnostics.Debug.WriteLine("SDC RemoveNode  "+nodename + "  ResetRequired "+resetRequired);
					if(downedNodes.Contains(n))
					{
						if (resetRequired)
						{
							n.SetAttribute("downforsecs","0");
							//also reset Mirror Time to 0 if needed
							ResetNonZero(n,"mirrorforsecs");
						}
						downedNodes.Remove(n);
					}
				}
				catch { }
			}
		}

		protected virtual void IncrementAtt(Node n, string attrname)
		{
			string t_down = n.GetAttribute(attrname);
			if("" == t_down) t_down = "0";
			int downFor = CONVERT.ParseInt(t_down);
			++downFor;
			n.SetAttribute(attrname,downFor);
		}

		void currentTimeNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			lock(this)
			{
				if (Enabled)
				{
					foreach(AttributeValuePair avp in attrs)
					{
						if(avp.Attribute == "seconds")
						{
							foreach(Node n in downedNodes)
							{
								// 09-05-2007 : Don't count time down on nodes that have no impact!
								// Note that this fix is skin/metaphor/race dependent and should be reviewed.
								// Easily found by a grep on the code base...

								//Now Refactored for a seperate has Impact Flag
								//rather than having a non zero amount in either impactkmh or impactsecsinpit
								//as these terms are highly linked to the Race Scenario

								//old code 
								//int impactkmh = n.GetIntAttribute("impactkmh",0);
								//int impactsecsinpit = n.GetIntAttribute("impactsecsinpit",0);
								//if( (impactkmh != 0) || (impactsecsinpit != 0) )
								
								//new code 
								bool hasImpact = n.GetBooleanAttribute("has_impact",false);
								if (hasImpact)
								{
									bool Status_UP = n.GetBooleanAttribute("up",false);
									bool Status_UpByMirror = n.GetBooleanAttribute("uponlybymirror",false);
									bool Status_UpByVirtualMirror = n.GetBooleanAttribute("virtualmirrorinuse", false);

									int workingaround = n.GetIntAttribute("workingaround", 0);

									if (Status_UP)
									{
										if (Status_UpByMirror || Status_UpByVirtualMirror)
										{
											IncrementAtt(n, "mirrorforsecs");
										}
										// We still need to count even if we are in workaround (3404).
										else
										{
											if(workingaround > 0)
											{
												IncrementAtt(n, "downforsecs");
											}
										}
									}
									else
									{
										//Status up == false
										IncrementAtt(n, "downforsecs");
									}
									//
									//string t_down = n.GetAttribute("downforsecs");
									//if("" == t_down) t_down = "0";
									//int downFor = CONVERT.ParseInt(t_down);
									//++downFor;
									//n.SetAttribute("downforsecs",downFor);
								}
							}
						}
					}
				}
			}
		}

	}
}
