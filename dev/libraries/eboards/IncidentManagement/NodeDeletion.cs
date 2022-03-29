using Network;

using LibCore;

namespace IncidentManagement
{
	public class NodeDeletion : INodeChangeOperation
	{
		Node node;

		public NodeDeletion (Node node)
		{
			this.node = node;
		}

		public void Apply ()
		{
			node.Parent.DeleteChildTree(node);
		}

		public override string ToString ()
		{
			return CONVERT.Format("Delete '{0}'", node.GetAttribute("name"));
		}
	}
}