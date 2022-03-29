using System;
using System.Collections;
using Algorithms;
using Network;

namespace DevOps.OpsEngine
{
	public class DangerLevelFlickerer : IDisposable
	{
		Node timeNode;
		Node businessServicesGroup;

		Random random;

		public DangerLevelFlickerer (NodeTree model)
		{
			timeNode = model.GetNamedNode("CurrentTime");
			timeNode.AttributesChanged += timeNode_AttributesChanged;

			businessServicesGroup = model.GetNamedNode("Business Services Group");

			random = new Random ();
		}

		public void Dispose ()
		{
			timeNode.AttributesChanged -= timeNode_AttributesChanged;
		}

		void timeNode_AttributesChanged (Node sender, ArrayList attributes)
		{
			foreach (Node businessService in businessServicesGroup.GetChildrenOfType("biz_service"))
			{
				if (businessService.GetIntAttribute("danger_level", 0) < 100)
				{
					if (random.NextDouble() < 0.5)
					{
						businessService.SetAttribute("danger_level", Maths.Clamp(businessService.GetIntAttribute("danger_level", 0) + random.Next(-20, 20), 0, 60));
					}
				}
			}
		}
	}
}