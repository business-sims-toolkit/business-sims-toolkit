using System;
using System.Collections.Generic;
using System.Text;

using LibCore;
using Network;

namespace Cloud.OpsEngine
{
	public class Biller
	{
		NodeTree model;
		Node turnover;

		public Biller (NodeTree model)
		{
			this.model = model;
			turnover = model.GetNamedNode("Turnover");
		}

		public Node CreateOrUpdateTurnoverItem (List<AttributeValuePair> originalAttributes)
		{
			List<AttributeValuePair> attributes = new List<AttributeValuePair> ();
			foreach (AttributeValuePair avp in originalAttributes)
			{
				attributes.Add(new AttributeValuePair (avp));
			}
			 
			Node itemToUpdate = null;

			foreach (Node item in turnover.getChildren())
			{
				bool itemMatches = true;

				foreach (AttributeValuePair avp in attributes)
				{
					if (avp.Attribute != "value")
					{
						if (item.GetAttribute(avp.Attribute) != avp.Value)
						{
							itemMatches = false;
							break;
						}
					}
				}

				if (itemMatches)
				{
					itemToUpdate = item;
					break;
				}
			}

			if (itemToUpdate != null)
			{
				foreach (AttributeValuePair avp in attributes)
				{
					if (avp.Attribute == "value")
					{
						avp.Value = CONVERT.ToStr(itemToUpdate.GetDoubleAttribute("value", 0) + CONVERT.ParseDouble(avp.Value));
					}
				}

				itemToUpdate.SetAttributes(attributes);

				return itemToUpdate;
			}
			else
			{
				string type = "";
				foreach (AttributeValuePair avp in attributes)
				{
					if (avp.Attribute == "type")
					{
						type = avp.Value;
						break;
					}
				}

				return new Node (turnover, type, CONVERT.Format("turnover_item_{0}", 1 + turnover.getChildren().Count), attributes);
			}
		}
	}
}