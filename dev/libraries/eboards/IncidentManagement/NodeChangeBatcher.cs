using System.Collections.Generic;
using Network;

using LibCore;

namespace IncidentManagement
{
	public class NodeChangeBatcher : INodeChanger
	{
		NodeTree model;
		List<INodeChangeOperation> operations;

		public NodeChangeBatcher (NodeTree model)
		{
			this.model = model;

			operations = new List<INodeChangeOperation> ();
		}

		public NodeTree Model
		{
			get
			{
				return model;
			}
		}

		public void SetAttribute (Node node, string attribute, string value)
		{
			operations.Add(new NodeAttributesChange (node, new AttributeValuePair (attribute, value)));
		}

		public void SetAttribute (Node node, string attribute, int value)
		{
			operations.Add(new NodeAttributesChange (node, new AttributeValuePair (attribute, value)));
		}

		public void SetAttribute (Node node, string attribute, bool value)
		{
			operations.Add(new NodeAttributesChange (node, new AttributeValuePair (attribute, value)));
		}

		public void SetAttribute (Node node, string attribute, float value)
		{
			operations.Add(new NodeAttributesChange (node, new AttributeValuePair (attribute, value)));
		}

		public void SetAttributes (Node node, List<AttributeValuePair> attributes)
		{
			operations.Add(new NodeAttributesChange (node, attributes));
		}

		public void Delete (Node node)
		{
			operations.Add(new NodeDeletion (node));
		}

		public void Create (Node parent, string name, string type, List<AttributeValuePair> attributes)
		{
			operations.Add(new NodeCreation (parent, name, type, attributes));
		}

		public void Apply ()
		{
			foreach (INodeChangeOperation operation in operations)
			{
				operation.Apply();
			}
		}

		public override string ToString ()
		{
			if (operations.Count == 0)
			{
				return "(no operation)";
			}
			else if (operations.Count == 1)
			{
				return CONVERT.Format("(1 operation: {0})", operations[0]);
			}
			else
			{
				return CONVERT.Format("({0} operations)", operations.Count);
			}
		}
	}
}