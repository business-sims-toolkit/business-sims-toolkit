using System;
using System.Collections.Generic;

namespace Network
{
	public class NodeChangeTransaction : IDisposable
	{
		Node node;
		Dictionary<string, string> attributeNameToNewValue;

		internal NodeChangeTransaction (Node node)
		{
			this.node = node;
		}

		public void Dispose ()
		{
			List<AttributeValuePair> newAttributes = new List<AttributeValuePair> ();
			if (attributeNameToNewValue != null)
			{
				foreach (string attributeName in attributeNameToNewValue.Keys)
				{
					newAttributes.Add(new AttributeValuePair (attributeName, attributeNameToNewValue[attributeName]));
				}
			}

			node.CommitTransaction(this, newAttributes);
		}

		internal void AddAttributeChange (string attributeName, string attributeValue)
		{
			if (attributeNameToNewValue == null)
			{
				attributeNameToNewValue = new Dictionary<string, string> ();
			}

			attributeNameToNewValue[attributeName] = attributeValue;
		}
	}
}