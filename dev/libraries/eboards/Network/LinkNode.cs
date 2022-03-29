using System;
using System.Xml;
using System.Collections;

namespace Network
{
	/// <summary>
	/// Summary description for LinkNode.
	/// A LinkNode is a specialization of Node that represents
	/// non-heirarchical links between nodes in a network.
	/// </summary>
	public class LinkNode : Node
	{
		protected Node _linkedTo = null;
		
		public Node To
		{
			get { return _linkedTo; }
			set { _linkedTo = value; /* TODO ! : Should fire tree changed event */ }
		}

		public Node From
		{
			get
			{
				return this.Parent;
			}
		}
		
		internal LinkNode(NodeTree tree, Node parent, Node toNode) : base(tree, parent, "link")
		{
			_linkedTo = toNode;
			if(null != _linkedTo)
			{
				_linkedTo.AddBackLink(this);
			}		
		}
		
		internal LinkNode(NodeTree tree, Node parent, XmlNode xnode) : base (tree,parent,xnode)
		{
			// Intentionally left blank. Construction is handled by the base class.
		}

		public LinkNode (Node parent, string type, string uniqueNodeName, System.Collections.Generic.List<AttributeValuePair> attrs)
			: base(parent, type, uniqueNodeName, attrs)
		{
			this._NodeType = "link";
			BuildLinkNodes();
		}

		// This is the constructor we can use at run time.
		public LinkNode(Node parent, string type, string uniqueNodeName, ArrayList attrs) : base (parent,type,uniqueNodeName,attrs)
		{
			this._NodeType = "link";
			BuildLinkNodes();
		}

		// This is the constructor we can use at run time.
		public LinkNode(Node parent, XmlNode xn) : base (parent,xn)
		{
			this._NodeType = "link";
			BuildLinkNodes();
		}

		public override void BuildLinkNodes()
		{
			//string type = GetAttribute("type"); // Dashed?
			string to = GetAttribute("to");
			if(to == "")
			{
				throw(new Exception("Cannot have link with no to attribute. (" + this.GetAttribute("name") + ")") );
			}

			_linkedTo = _tree.GetNamedNode(to);
			if(null != _linkedTo)
			{
				_linkedTo.AddBackLink(this);
			}
			else
			{
				throw(new Exception("Cannot link to Node " + to + " as it doesn't exist.") );
			}
		}
	}
}
