using System;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LibCore;

namespace Network
{
	/// <summary>
	/// A generic node tree that can be read from an XML file and
	/// can be used to store Network, Visuals, Relationships etc.
	/// 
	/// Top level node is special and is not part of the normal tree
	/// so that multiple sub-trees can be safely defined.
	/// 
	/// "Node" - A Node on the network.
	/// "Link" - A Non-Heirrarchical link on the network.
	/// 
	/// </summary>
	public class NodeTree : BaseClass, ISave
	{
		public string uuid = Guid.NewGuid().ToString();

		/// <summary>
		/// Store a currently held maximum id so that all nodes can have a unique id.
		/// </summary>
		protected ulong currentMaxID = 1;

		/// <summary>
		/// The Universal Root of the modelled world.
		/// </summary>
		internal protected Node _Root;

		/// <summary>
		/// A Hashtable that links internal; unique ids to nodes in the model.
		/// </summary>
		protected Hashtable _IDToNode = new Hashtable();

		/// <summary>
		/// A Hashtable that links nodes in the tree to their unique id.
		/// </summary>
		protected Hashtable _NodeToID = new Hashtable();

		/// <summary>
		/// A Hashtable that links unique names for entries in the model to their specific node.
		/// </summary>
		protected Hashtable _namesToNodes = new Hashtable();

		/// <summary>
		/// A Hashtable that links nodes in the model to unique names for them.
		/// </summary>
		protected Hashtable _nodesToNames = new Hashtable();

		Dictionary<string, List<Node>> typeToNodes = new Dictionary<string, List<Node>>();

		/// <summary>
		/// Method to return all nodes in the model that are marked as being of particular types.
		/// </summary>
		/// <param name="types">An ArrayList of strings that define which types of node the caller is interested in.</param>
		/// <returns>A Hashtable of nodes (key) to type (value).</returns>
		public Hashtable GetNodesOfAttribTypes (ArrayList types)
		{
			Hashtable results = new Hashtable();

			foreach (string type in types)
			{
				if (typeToNodes.ContainsKey(type))
				{
					foreach (Node node in typeToNodes[type])
					{
						if (results.ContainsKey(node) == false)
						{
							results.Add(node, type);
						}
					}
				}
			}

			return results;
		}

		public Dictionary<Node, string> GetNodesOfAttribTypesAsDictionary (IList<string> types)
		{
			Dictionary<Node, string> results = new Dictionary<Node, string>();

			foreach (string type in types)
			{
				if (typeToNodes.ContainsKey(type))
				{
					foreach (Node node in typeToNodes[type])
					{
						if (! results.ContainsKey(node))
						{
							results.Add(node, type);
						}
					}
				}
			}

			return results;
		}

		bool emitApplyAttributesEventsOnNodeCreation = false;

		public bool EmitApplyAttributesEventsOnNodeCreation
		{
			get
			{
				return emitApplyAttributesEventsOnNodeCreation;
			}

			set
			{
				emitApplyAttributesEventsOnNodeCreation = value;
			}
		}

		internal bool AddNamedNode (string _name, Node node)
		{
			return AddNamedNode(_name, node, true);
		}

		internal bool AddNamedNode (string _name, Node node, bool emitEvents)
		{
			string name = _name.ToLower();

			//
			// Do loging pre-node-added.
			//
			/*
			if(null != PreNodeAdded)
			{
				object[] orgs = new object[2];
				orgs[0] = this;
				orgs[1] = node;
				// We unroll the delegates so that if one fails then we can log the error
				// and still call the rest...
				System.Delegate[] delegates = PreNodeAdded.GetInvocationList();
				foreach(System.Delegate d in delegates)
				{
#if !PASSEXCEPTIONS
						try
						{
#endif
					d.DynamicInvoke(orgs);
#if !PASSEXCEPTIONS
						}
						catch(Exception ex)
						{
							AppInfo.TheInstance.WriteLine("NodeTree NodeAdded Exception : " + ex.Message + ":\r\n" + ex.StackTrace);
						}
#endif
				}
			}*/

			if ("" == name)
			{
				if ((null != NodeAdded) && emitEvents)
				{
					object [] orgs = new object[2];
					orgs[0] = this;
					orgs[1] = node;
					// We unroll the delegates so that if one fails then we can log the error
					// and still call the rest...
					System.Delegate [] delegates = NodeAdded.GetInvocationList();
					foreach (System.Delegate d in delegates)
					{
#if !PASSEXCEPTIONS
						try
						{
#endif
						d.DynamicInvoke(orgs);
#if !PASSEXCEPTIONS
						}
						catch(Exception ex)
						{
							AppInfo.TheInstance.WriteLine("NodeTree NodeAdded Exception : " + ex.Message + ":\r\n" + ex.StackTrace);
						}
#endif
					}
					//NodeAdded(this,node);
				}
				return true;
			}

			if (! _namesToNodes.ContainsKey(name))
			{
				_namesToNodes[name] = node;
				_nodesToNames[node] = name;

				if ((null != NodeAdded) && emitEvents)
				{
					object [] orgs = new object[2];
					orgs[0] = this;
					orgs[1] = node;
					// We unroll the delegates so that if one fails then we can log the error
					// and still call the rest...
					System.Delegate [] delegates = NodeAdded.GetInvocationList();
					foreach (System.Delegate d in delegates)
					{
#if !PASSEXCEPTIONS
						try
						{
#endif
						d.DynamicInvoke(orgs);
#if !PASSEXCEPTIONS
						}
						catch(Exception ex)
						{
							AppInfo.TheInstance.WriteLine("NodeTree NodeAdded Exception : " + ex.Message + ":\r\n" + ex.StackTrace);
						}
#endif
					}
					//NodeAdded(this,node);
				}

				return true;
			}
			else
			{
				// This may actually be the same nod so just check...
				Node n = (Node) _namesToNodes[name];
				if (n != node)
				{
					// Throw for just now so that we can see that you are trying to add a named
					// node to a tree that already has this unique name.
#if !PASSEXCEPTIONS
					AppInfo.TheInstance.WriteLine("Node with name [" + name + "] already exists!");
#else
					throw(new Exception("Node with name [" + name + "] already exists!"));
#endif
				}
			}
			return false;
		}

		public void ChangeUniqueName (Node n, string _oldName, string _newName)
		{
			string newName = _newName.ToLower(); //names should be lowercase 
			string oldName = _oldName.ToLower(); //names should be lowercase 
			//Alter the Names to Nodes Mapping
			_namesToNodes.Remove(oldName); //remove the old name 			
			_namesToNodes[newName] = n; //add the new node into the by name look up table 
			//Alter the Nodes to Names Mapping
			_nodesToNames.Remove(n); //remove the old name 			
			_nodesToNames.Add(n, newName); //add the new name into the by node look up table 
		}

		public void AfterChangeUniqueName (Node n, string _oldName, string _newName)
		{
			if (null != NodePostNameChange)
			{
				object [] orgs = new object[4];
				orgs[0] = this;
				orgs[1] = n;
				orgs[2] = _oldName;
				orgs[3] = _newName;

				// We unroll the delegates so that if one fails then we can log the error
				// and still call the rest...
				System.Delegate [] delegates = NodePostNameChange.GetInvocationList();
				foreach (System.Delegate d in delegates)
				{
#if !PASSEXCEPTIONS
					try
					{
#endif
					d.DynamicInvoke(orgs);
#if !PASSEXCEPTIONS
					}
					catch(Exception ex)
					{
						AppInfo.TheInstance.WriteLine("NodeTree NodeAdded Exception : " + ex.Message + ":\r\n" + ex.StackTrace);
					}
#endif
				}
			}
		}

		/// <summary>
		/// Returns a hashtable of nodes to values of the specified attribute.
		/// Only nodes that have a valid non-empty value for the requested
		/// attribute will be returned.
		/// </summary>
		/// <param name="attribute"></param>
		/// <returns></returns>
		public Hashtable GetNodesWithAttribute (string attribute)
		{
			Hashtable nodesToValue = new Hashtable();
			Root.GetNodesWithAttribute(attribute, ref nodesToValue);
			return nodesToValue;
		}

		public ArrayList GetNodesWithAttributeValue (string attribute, string val)
		{
			ArrayList nodesThatMatchAttributeValue = new ArrayList();
			Root.GetNodesWithAttributeValue(attribute, val, ref nodesThatMatchAttributeValue);
			return nodesThatMatchAttributeValue;
		}

		public Node GetNamedNode (string _name)
		{
			// 11/09/2007 - Added Namespacing : NodeName/subnodetype
			string name = (_name ?? "").ToLower();

			char [] sep = { '/' };
			string [] namespaces = name.Split(sep);
			name = namespaces[0];

			if (_namesToNodes.ContainsKey(name))
			{
				Node primary = (Node) _namesToNodes[name];

				int cur_ns = 1;

				while (namespaces.Length > cur_ns)
				{
					// Search down the namespaces...
					//
					bool found_next = false;

					foreach (Node n in primary)
					{
						if (n.GetAttribute("type") == namespaces[cur_ns])
						{
							found_next = true;
							++cur_ns;
							break;
						}
					}
					// If we get here we have failed the namespace search.
					if (! found_next)
					{
						return null;
					}
				}
				return primary;
			}
			else if (name.StartsWith("UUID_"))
			{
				// Find the appropriate UUID node...
				string strID = name.Substring(5);
				ulong theID = CONVERT.ParseULong(strID);
				if (this._IDToNode.ContainsKey(theID))
				{
					return (Node) this._IDToNode[theID];
				}
			}

			// 10 years I've been here and I never knew about the special handling for names of type "a/b".
			// Leave the weird existing behaviour but failsafe to just find a node with the exact name given.
			if (_namesToNodes.ContainsKey(_name.ToLower()))
			{
				return (Node) _namesToNodes[_name.ToLower()];
			}

			return null;
		}

		internal bool HasID (ulong id)
		{
			return _IDToNode.ContainsKey(id);
		}

		internal ulong AddNodeCreateID (Node n)
		{
			++currentMaxID;
			ulong ret = currentMaxID;
			AddNodeID(n, this.currentMaxID);
			return ret;
		}

		internal protected void AddNodeID (Node n, ulong id)
		{
			if (! HasID(id))
			{
				_IDToNode[id] = n;
				_NodeToID[n] = id;
				//
				if (id > currentMaxID)
				{
					currentMaxID = id;
				}
			}
			else
			{
				throw(new Exception("Attempting To Create A Node With The Same UUID : " + CONVERT.ToStr(id)));
			}
		}

		internal protected Node GetNodeFromID (ulong id)
		{
			return (Node) _IDToNode[id];
		}

		public delegate void NodeTreeEventHandler (object sender, NodeTreeEventArgs args);

		public event NodeTreeEventHandler TreeChanged;
		public event NodeTreeEventHandler EarlyTreeChanged;

		public delegate void NodeAddedEventHandler (NodeTree sender, Node newNode);

		//public event NodeAddedEventHandler PreNodeAdded;
		public event NodeAddedEventHandler NodeAdded;

		public delegate void NodeMovedEventHandler (NodeTree sender, Node oldParent, Node movedNode);

		public event NodeMovedEventHandler NodeMoved;

		public delegate void NodePostNameChangedEventHandler (NodeTree sender, Node changedNode, string oldname,
		                                                      string newname);

		public event NodePostNameChangedEventHandler NodePostNameChange;

		public void FireMovedNode (Node oldParent, Node n)
		{
			string name = n.GetAttribute("name");
			if (null != NodeMoved)
			{

				object [] orgs = new object[3];
				orgs[0] = this;
				orgs[1] = oldParent;
				orgs[2] = n;
				// We unroll the delegates so that if one fails then we can log the error
				// and still call the rest...
				System.Delegate [] delegates = NodeMoved.GetInvocationList();
				foreach (System.Delegate d in delegates)
				{
#if !PASSEXCEPTIONS
					try
					{
#endif
					d.DynamicInvoke(orgs);
#if !PASSEXCEPTIONS
					}
					catch(Exception ex)
					{
						AppInfo.TheInstance.WriteLine("NodeTree NodeMoved Exception : " + ex.Message + ":\r\n" + ex.StackTrace);
					}
#endif
				}
				//NodeMoved(this,oldParent,n);
			}
		}

		public void FireAddedNode (Node n)
		{
			string name = n.GetAttribute("name");
			//if("" != name)
			{
				AddNamedNode(name, n);
			}
		}

		/// <summary>
		/// Default empty constructor.
		/// </summary>
		NodeTree ()
		{
			_IDToNode = new System.Collections.Hashtable();
		}

		/// <summary>
		/// Load a NetworkNode tree from a file.
		/// Throws if the file cannot be found or if the file does not
		/// contain a valid NetworkNode tree.
		/// </summary>
		/// <param name="xmldata">The file to load.</param>
		public NodeTree (string xmldata)
		{
			_IDToNode = new System.Collections.Hashtable();
			//
			LibCore.BasicXmlDocument xdoc = LibCore.BasicXmlDocument.Create(xmldata);
			XmlNode rootNode = xdoc.DocumentElement;
			System.Xml.XmlDeclaration xdec = rootNode as System.Xml.XmlDeclaration;
			while (xdec != null)
			{
				rootNode = rootNode.NextSibling;
				xdec = rootNode as System.Xml.XmlDeclaration;
			}
			_Root = new Node(this, null, rootNode);
			if (null != _Root)
			{
				_Root.BuildLinkNodes();
			}
		}

		public NodeTree (NodeTree original)
		{
			_IDToNode = new System.Collections.Hashtable();

			_Root = new Node(this, null, "root");

			CopyChildrenInto(original._Root, _Root);
		}

		void CopyChildrenInto (Node original, Node destination)
		{
			foreach (Node child in original.getChildren())
			{
				Node copy = new Node(destination, child.GetAttribute("type"), child.GetAttribute("name"), child.GetAttributes());

				CopyChildrenInto(child, copy);
			}
		}

		protected bool _saveUUIDs = true;

		public bool SaveUUIDs
		{
			set
			{
				_saveUUIDs = value;
			}
		}

		public void SaveToURL (string url, string fileName)
		{
			//if(null != xdoc)
			{
				//BuildUniqueIDs();
				// Have to rebuild the document...
				LibCore.BasicXmlDocument xdoc = LibCore.BasicXmlDocument.Create();
				_Root.SaveToDoc(xdoc, _saveUUIDs);
				xdoc.SaveToURL(url, fileName);
			}
		}

		public void SavePretty (string filename)
		{
			LibCore.BasicXmlDocument xml = LibCore.BasicXmlDocument.Create();
			_Root.SaveToDoc(xml, false, true);
			using (XmlTextWriter writer = new XmlTextWriter(filename, null))
			{
				writer.Formatting = Formatting.Indented;
				writer.Indentation = 1;
				writer.IndentChar = '\t';
				xml.Save(writer);
			}
		}

		/// <summary>
		/// Returns the Root node of the XML tree.
		/// </summary>
		public Node Root
		{
			get
			{
				return _Root;
			}
		}

		public void Dispose ()
		{
			// Cleans things up to make it easier for the garbage collector...
			ArrayList children = this.Root.getChildrenClone();
			foreach (Node n in children)
			{
				this.Root.DeleteChildTree(n, false);
			}
			children.Clear();
			this._Root = null;
			_IDToNode.Clear();
			_NodeToID.Clear();
			_namesToNodes.Clear();
			_nodesToNames.Clear();
			typeToNodes.Clear();
		}

		internal void NodeEarlyAttributesChanged (Node n, ArrayList attsChanged)
		{
			if (null != EarlyTreeChanged)
			{
				NodeTreeEventArgs args = new NodeTreeEventArgs(n, attsChanged);
				object [] orgs = new object [2];
				orgs[0] = this;
				orgs[1] = args;
				// We unroll the delegates so that if one fails then we can log the error
				// and still call the rest...
				System.Delegate [] delegates = EarlyTreeChanged.GetInvocationList();
				foreach (System.Delegate d in delegates)
				{
#if !PASSEXCEPTIONS
					try
					{
#endif
					d.DynamicInvoke(orgs);
#if !PASSEXCEPTIONS
					}
					catch(Exception ex)
					{
						AppInfo.TheInstance.WriteLine("NodeTree TreeChanged Exception : " + ex.Message + ":\r\n" + ex.StackTrace);
					}
#endif
				}
			}
		}

		internal protected void NodeAttributesChanged (Node n, ArrayList attsChanged)
		{
			if (null != TreeChanged)
			{
				NodeTreeEventArgs args = new NodeTreeEventArgs(n, attsChanged);
				object [] orgs = new object[2];
				orgs[0] = this;
				orgs[1] = args;
				// We unroll the delegates so that if one fails then we can log the error
				// and still call the rest...
				System.Delegate [] delegates = TreeChanged.GetInvocationList();
				foreach (System.Delegate d in delegates)
				{
#if !PASSEXCEPTIONS
					try
					{
#endif
					d.DynamicInvoke(orgs);
#if !PASSEXCEPTIONS
					}
					catch(Exception ex)
					{
						AppInfo.TheInstance.WriteLine("NodeTree TreeChanged Exception : " + ex.Message + ":\r\n" + ex.StackTrace);
					}
#endif
				}
			}
		}

		/// <summary>
		/// Should be called by a node to fire required events to all interested
		/// parties. Should be called from leaf to node in a deleted sub-tree
		/// thereby meaning that all deleted nodes indeed can seperate links
		/// from parents before the parent itself is deleted.
		/// </summary>
		internal protected void NodeDeleted (Node n)
		{
			if (null != TreeChanged)
			{
				NodeTreeEventArgs args = new NodeTreeEventArgs(n, NodeTreeEventArgs.EventType.Deleted);

				object [] orgs = new object[2];
				orgs[0] = this;
				orgs[1] = args;
				// We unroll the delegates so that if one fails then we can log the error
				// and still call the rest...
				System.Delegate [] delegates = TreeChanged.GetInvocationList();
				foreach (System.Delegate d in delegates)
				{
#if !PASSEXCEPTIONS
					try
					{
#endif
					d.DynamicInvoke(orgs);
#if !PASSEXCEPTIONS
					}
					catch(Exception ex)
					{
						AppInfo.TheInstance.WriteLine("NodeTree TreeChanged (Del) Exception : " + ex.Message + ":\r\n" + ex.StackTrace);
					}
#endif
				}

				foreach (string type in typeToNodes.Keys)
				{
					if (typeToNodes[type].Contains(n))
					{
						typeToNodes[type].Remove(n);
					}
				}
			}

			if (_NodeToID.ContainsKey(n))
			{
				string id = CONVERT.ToStr((ulong) _NodeToID[n]);
				_NodeToID.Remove(n);
				_IDToNode.Remove(id);
			}

			if (_nodesToNames.ContainsKey(n))
			{
				_nodesToNames.Remove(n);
			}

			_namesToNodes.Remove(n.GetAttribute("name").ToLower());

			foreach (string type in typeToNodes.Keys)
			{
				if (typeToNodes[type].Contains(n))
				{
					typeToNodes[type].Remove(n);
				}
			}
		}

		internal void RecordNodeTypeChange (Node node, string oldType, string newType)
		{
			//handle the old Value (removing from records)
			if (String.IsNullOrEmpty(oldType) == false)
			{
				if (typeToNodes.ContainsKey(oldType))
				{
					typeToNodes[oldType].Remove(node);

					if (typeToNodes[oldType].Count == 0)
					{
						typeToNodes.Remove(oldType);
					}
				}
			}

			//handle the new Value (adding to records)
			if (String.IsNullOrEmpty(newType) == false)
			{
				if (! typeToNodes.ContainsKey(newType))
				{
					typeToNodes.Add(newType, new List<Node>());
				}
				//Only add it if we dont have it already
				//typeToNodes[newType].Add(node);
				if (((List<Node>) typeToNodes[newType]).Contains(node) == false)
				{
					typeToNodes[newType].Add(node);
				}

			}
		}

		public void MoveNode (Node node, Node newParent)
		{
			Node oldParent = node.Parent;
			newParent.AddChild(node);
			FireMovedNode(oldParent, node);
		}

		public Node CreateNode (string type, string name)
		{
			return Root.CreateChild(type, name);
		}

		public string GetAttribute (string modelName)
		{
			var nodeName = modelName.Substring(0, modelName.IndexOf("."));
			var attributeName = modelName.Substring(nodeName.Length + 1);
			return GetNamedNode(nodeName).GetAttribute(attributeName);
		}

		public int? GetIntAttribute (string modelName)
		{
			var attribute = GetAttribute(modelName);
			if (string.IsNullOrEmpty(attribute))
			{
				return null;
			}
			else
			{
				return CONVERT.ParseInt(attribute);
			}
		}

		public int GetIntAttribute (string modelName, int defaultValue)
		{
			return GetIntAttribute(modelName) ?? defaultValue;
		}

		internal static AttributeValuePair CreateAttributeValuePairFromSearchTerm (string searchTerm)
		{
			var equalsIndex = searchTerm.IndexOf("=");
			if (equalsIndex == -1)
			{
				throw new Exception($"Search term '{searchTerm}' doesn't contain an =!");
			}

			return new AttributeValuePair(searchTerm.Substring(0, equalsIndex), searchTerm.Substring(equalsIndex + 1));
		}

		public Node FindNodeWithSearchAttributesIfPresent (string name)
		{
			var searchPrefix = ".$";
			var searchPrefixIndex = name.IndexOf(searchPrefix);
			string searchName;
			string tail = null;

			if (searchPrefixIndex == -1)
			{
				searchName = name;
			}
			else
			{
				searchName = name.Substring(0, searchPrefixIndex);
				tail = name.Substring(searchPrefixIndex + searchPrefix.Length);
			}

			var node = GetNamedNode(searchName);
			if (node == null)
			{
				return null;
			}
			else if (string.IsNullOrEmpty(tail))
			{
				return node;
			}
			else
			{
				return node.FindChildNodeWithSearchAttributes(tail);
			}
		}
	}

	/// <summary>
	/// EventArg class that is fired when a node tree has been altered.
	/// This allows NodeTree display classes to update their view
	/// appropriately.
	/// </summary>
	public class NodeTreeEventArgs : System.EventArgs
	{
		Node _node;
		public ArrayList AttributesChanged;

		public enum EventType
		{
			Deleted = 0,
			ArgsChanged,
			Added
		};

		EventType _eventType;
		/// <summary>
		/// Node is being deleted.
		/// </summary>
		/// <param name="n"></param>
		/// <param name="et"></param>
		public NodeTreeEventArgs(Node n, EventType et) : base()
		{
			this._node = n;
			this._eventType = et;
		}
		/// <summary>
		/// Node attribute has changed.
		/// </summary>
		/// <param name="n"></param>
		/// <param name="attsChanged"></param>
		public NodeTreeEventArgs(Node n, ArrayList attsChanged) : base()
		{
			this._node = n;
			this._eventType = EventType.ArgsChanged;
			AttributesChanged = attsChanged;
		}

		public Node NodeAffected
		{
			get { return _node; }
		}

		public EventType TypeOfEvent
		{
			get { return _eventType; }
		}
	}
}