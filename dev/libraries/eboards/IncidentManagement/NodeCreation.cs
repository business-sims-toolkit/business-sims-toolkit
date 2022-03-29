using System.Collections.Generic;
using System.Text;

using Network;

namespace IncidentManagement
{
	public class NodeCreation : INodeChangeOperation
	{
		Node parent;
		string name;
		string type;
		List<AttributeValuePair> attributes;

		public NodeCreation (Node parent, string name, string type, List<AttributeValuePair> attributes)
		{
			this.parent = parent;
			this.name = name;
			this.type = type;
			this.attributes = attributes;
		}

		public void Apply ()
		{
			new Node (parent, type, name, attributes);
		}

		public override string ToString ()
		{
			StringBuilder builder = new StringBuilder ();
			builder.AppendFormat("Create {0} '{1}' ", type, name);

			bool first = true;
			foreach (AttributeValuePair avp in attributes)
			{
				if (first)
				{
					builder.Append(", ");
				}
				first = false;

				builder.AppendFormat("{0}='{1}'", avp.Attribute, avp.Value);
			}

			return builder.ToString();
		}
	}
}