using System;
using System.Text.RegularExpressions;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using LibCore;

namespace Network
{
	/// <summary>
	/// A Node class that is a node on the XML tree.
	/// The class is enumerable and implements the IEnumerable
	/// interface.
	/// </summary>
	public class Node : BaseClass, IEnumerable
	{
		protected Node _parent;
		protected NodeTree _tree;
		protected string _NodeType;
		ulong _ID = 0; // No ID.

		protected StringDictionary _Attributes = new StringDictionary();
		/// <summary>
		/// The AttributesAsArrayList accessor clones the Attributes dictionary as an ArrayList
		/// of AttributeValuePairs.
		/// It does not pass back the actual dictionary as that would allow external changes
		/// to the class's internals.
		/// </summary>
		public ArrayList AttributesAsArrayList
		{
			get
			{
				ArrayList attrs = new ArrayList();
				foreach(string attr in _Attributes.Keys)
				{
					attrs.Add( new AttributeValuePair(attr, _Attributes[attr]) );
				}
				return attrs;
			}
		}
		/// <summary>
		/// The Attributes accessor clones the Attributes dictionary as a StringDictionary.
		/// It does not pass back the actual dictionary as that would allow external changes
		/// to the class's internals.
		/// </summary>
		public StringDictionary AttributesAsStringDictionary
		{
			get
			{
				StringDictionary attrs = new StringDictionary ();

				foreach(string attr in _Attributes.Keys)
				{
					attrs.Add(attr, _Attributes[attr]);
				}

				return attrs;
			}
		}

		public List<AttributeValuePair> AttributesAsList
		{
			get
			{
				List<AttributeValuePair> attributes = new List<AttributeValuePair> ();

				foreach (string attributeName in _Attributes.Keys)
				{
					attributes.Add(new AttributeValuePair (attributeName, _Attributes[attributeName]));
				}

				return attributes;
			}
		}

		public Dictionary<string, string> AttributesAsDictionary
		{
			get
			{
				Dictionary<string, string> attributes = new Dictionary<string, string> ();

				foreach (string attributeName in _Attributes.Keys)
				{
					attributes.Add(attributeName, _Attributes[attributeName]);
				}

				return attributes;
			}
		}

		/// <summary>
		/// Stores the child nodes in the network.
		/// </summary>
		protected ArrayList _Children = new ArrayList();

		protected ArrayList _ConnectionsFromOtherNodes = new ArrayList();

		public delegate void BackLinkChangedHandler(Node sender, LinkNode link);
		public event BackLinkChangedHandler BackLinkAdded;
		public event BackLinkChangedHandler BackLinkRemoved;
	
		protected ArrayList _AssociatedObjects = new ArrayList();

		//Transmitting Attributes Changed 
		public delegate void AttributesChangedEventHandler(Node sender, ArrayList attrs);
		public delegate void PreAttributesChangedEventHandler (Node sender, ref ArrayList attrs);

		public event AttributesChangedEventHandler AttributesChanged;

		public event PreAttributesChangedEventHandler PreAttributesChanged;
		public event AttributesChangedEventHandler PostAttributesChanged;

		//Transmitting This Node is deleting ("I'm melting")
		public delegate void NodeDeletingEventHandler(Node sender);
		public event NodeDeletingEventHandler Deleting;

		//Transmitting This Node has added a Child
		public delegate void NodeChildAddedEventHandler(Node sender, Node child);
		public event NodeChildAddedEventHandler ChildAdded;

		public event NodeChildAddedEventHandler ParentChanged;

		//Transmitting This Node has removed a Child
		public delegate void NodeChildRemovedEventHandler(Node sender, Node child);
		public event NodeChildRemovedEventHandler ChildRemoved;

		public ulong ID
		{
			get { return _ID; }
		}

		public void SaveToDoc (LibCore.BasicXmlDocument xdoc, bool saveUUIDs)
		{
			SaveToDoc(xdoc, saveUUIDs, false);
		}

		public void SaveToDoc(LibCore.BasicXmlDocument xdoc, bool saveUUIDs, bool prettifyNodeNames)
		{
			Save(xdoc, (XmlNode) xdoc, saveUUIDs, prettifyNodeNames);
		}

		public bool GetNodesWithAttribute(string attribute, ref Hashtable nodesToValue)
		{
			bool found = false;
			string val = this.GetAttribute(attribute);
			if("" != val)
			{
				found = true;
				nodesToValue.Add(this, val);
			}
			//
			foreach(Node child in this)
			{
				if(child.GetNodesWithAttribute(attribute, ref nodesToValue))
				{
					found = true;
				}
			}
			//
			return found;
		}

		public bool GetNodesWithAttributeValue(string attribute, string val, ref ArrayList nodesThatMatchAttributeValue)
		{
			if (attribute == "type")
			{
				ArrayList types = new ArrayList ();
				types.Add(val);
				Hashtable matchingNodes = _tree.GetNodesOfAttribTypes(types);
				nodesThatMatchAttributeValue.AddRange(matchingNodes.Keys);
				return (matchingNodes.Count > 0);
			}

			bool found = false;
			if(val == this.GetAttribute(attribute))
			{
				found = true;
				nodesThatMatchAttributeValue.Add(this);
			}
			//
			foreach(Node child in this)
			{
				if(child.GetNodesWithAttributeValue(attribute, val, ref nodesThatMatchAttributeValue))
				{
					found = true;
				}
			}
			//
			return found;
		}

		protected void Save(LibCore.BasicXmlDocument xdoc, XmlNode p, bool saveUUIDs, bool prettifyNodeNames)
		{
			string nodeType = _NodeType;

			if (string.IsNullOrEmpty(nodeType))
			{
				nodeType = "node";
			}

			if (prettifyNodeNames)
			{
				string [] parts = nodeType.Split('_', ' ');

				if (_Attributes.ContainsKey("type"))
				{
					parts = _Attributes["type"].Split('_', ' ');
				}

				System.Text.StringBuilder builder = new System.Text.StringBuilder ();
				foreach (string part in parts)
				{
					builder.Append(part.Substring(0, 1).ToUpper() + part.Substring(1));
				}

				nodeType = builder.ToString();
			}

			_NodeType = _NodeType.Replace(" ", "_");

			if (_NodeType == "Connection")
			{
				nodeType = "link";
			}

			XmlNode n = xdoc.CreateElement(nodeType.Replace(" ", "_"));
			p.AppendChild(n);

			if(saveUUIDs) ((XmlElement)n).SetAttribute("uuid", CONVERT.ToStr(_ID) );

			var attributeNames = new List<string>(new ArrayList(_Attributes.Keys).ToArray().Cast<string>());
			attributeNames.Sort();

			foreach (string k in attributeNames)
			{
				XmlAttribute att = xdoc.CreateAttribute(k);
				att.Value = _Attributes[k];
				string val = (string) _Attributes[k];
				val = val.Replace("\r\n","\\r\\n");
				((XmlElement)n).SetAttribute(k, val);
			}

			foreach(Node c in _Children)
			{
				c.Save(xdoc,n,saveUUIDs, prettifyNodeNames);
			}
		}

		public NodeTree Tree
		{
			get { return _tree; }
		}
/*
		public void AddAssociatedObject(INodeAssociate ob)
		{
			_AssociatedObjects.Add(ob);
		}*/

		internal void AddBackLink(LinkNode n)
		{
			_ConnectionsFromOtherNodes.Add(n);
			n.Deleting += OtherNodes_NodeDeleting;
			if(this.BackLinkAdded != null)
			{
				BackLinkAdded(this, n);
			}
		}

		public void OtherNodes_NodeDeleting (Node sender)
		{
		  if (sender != null)
		  {
			  _ConnectionsFromOtherNodes.Remove(sender);
			  if(this.BackLinkRemoved != null)
			  {
				  object[] orgs = new object[2];
				  orgs[0] = this;
				  orgs[1] = sender;
				  // We unroll the delegates so that if one fails then we can log the error
				  // and still call the rest...
				  System.Delegate[] delegates = BackLinkRemoved.GetInvocationList();
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
						  AppInfo.TheInstance.WriteLine("Node BackLinkRemoved Exception : " + ex.Message + ":\r\n" + ex.StackTrace);
					  }
#endif
				  }

				  //BackLinkRemoved(this, (LinkNode) sender);
			  }
		  }
		}

		public ArrayList BackLinks
		{
			get { return _ConnectionsFromOtherNodes; }
		}

		/// <summary>
		/// Gets the enumerator.
		/// </summary>
		/// <returns>A NodeEnumerator IEnumerator implementing class.</returns>
		public IEnumerator GetEnumerator()
		{
			return new NodeEnumerator(this);
		}
		/// <summary>
		/// An IEnumerator class that enumerates over the child nodes of
		/// a particular node in the XML tree.
		/// </summary>
		class NodeEnumerator : IEnumerator
		{
			/// <summary>
			/// Store the position withing the enumeration.
			/// </summary>
			int _position = -1;
			/// <summary>
			/// Store the node whose children we are enumerating over.
			/// </summary>
			Node _n;
			/// <summary>
			/// Simple constructor.
			/// </summary>
			/// <param name="n">The node to enumerate over its children.</param>
			public NodeEnumerator(Node n) { _n = n; }

			public bool MoveNext()
			{
				if (_position < _n._Children.Count - 1)
				{
					_position++;
					return true;
				}

				return false;
			}
			/// <summary>
			/// Declare the Reset method required by IEnumerator.
			/// </summary>
			public void Reset() { _position = -1; }
			/// <summary>
			/// Declare the Current property required by IEnumerator.
			/// </summary>
			public object Current
			{
				get { return _n._Children[_position]; }
			}
		}

		public string Type
		{
			get { return _NodeType; }
		}
		//
		public string GetAttribute (string attributeName, IEnumerable<AttributeValuePair> extraAttributes = null)
		{
			string attributeValue = "";

			if (_Attributes.ContainsKey(attributeName))
			{
				attributeValue = _Attributes[attributeName];
			}

			if (extraAttributes != null)
			{
				foreach (AttributeValuePair avp in extraAttributes)
				{
					if (avp.Attribute == attributeName)
					{
						attributeValue = avp.Value;
					}
				}
			}

			return attributeValue;
		}

		public string GetAttribute (string attr, string defaultVal, IEnumerable<AttributeValuePair> extraAttributes = null)
		{
			string val = GetAttribute(attr, extraAttributes);

			if (val == "")
			{
				val = defaultVal;
			}

			return val;
		}

		public double? GetDoubleAttribute (string attributeName, IEnumerable<AttributeValuePair> extraAttributes = null)
		{
			string attributeValue = GetAttribute(attributeName, extraAttributes);
			if (! string.IsNullOrEmpty(attributeValue))
			{
				return CONVERT.ParseDouble(attributeValue);
			}

			return null;
		}

		public double GetDoubleAttribute (string attributeName, double defaultValue, IEnumerable<AttributeValuePair> extraAttributes = null)
		{
			double? attributeValue = GetDoubleAttribute(attributeName, extraAttributes);

			if (attributeValue.HasValue)
			{
				return attributeValue.Value;
			}

			return defaultValue;
		}

		public long? GetLongAttribute (string attributeName, IEnumerable<AttributeValuePair> extraAttributes = null)
		{
			string attributeValue = GetAttribute(attributeName, extraAttributes);
			if (! string.IsNullOrEmpty(attributeValue))
			{
				return CONVERT.ParseLong(attributeValue);
			}

			return null;
		}

		public long GetLongAttribute (string attributeName, long defaultValue, IEnumerable<AttributeValuePair> extraAttributes = null)
		{
			long? attributeValue = GetLongAttribute(attributeName, extraAttributes);

			if (attributeValue.HasValue)
			{
				return attributeValue.Value;
			}

			return defaultValue;
		}

		public int? GetIntAttribute (string attributeName, IEnumerable<AttributeValuePair> extraAttributes = null)
		{
			string attributeValue = GetAttribute(attributeName, extraAttributes);

			if (! string.IsNullOrEmpty(attributeValue))
			{
				return CONVERT.ParseInt(attributeValue);
			}

			return null;
		}

		public int GetIntAttribute (string attributeName, int defaultValue, IEnumerable<AttributeValuePair> extraAttributes = null)
		{
			int? attributeValue = GetIntAttribute(attributeName, extraAttributes);

			if (attributeValue.HasValue)
			{
				return attributeValue.Value;
			}

			return defaultValue;
		}

		public int GetHmsAttribute (string attributeName, int defaultValue)
		{
			return CONVERT.ParseHmsToSeconds(GetAttribute(attributeName, "00:00:00"));
		}

		public bool? GetBooleanAttribute (string attributeName, IEnumerable<AttributeValuePair> extraAttributes = null)
		{
			string attributeValue = GetAttribute(attributeName, extraAttributes);

			if (! string.IsNullOrEmpty(attributeValue))
			{
				return CONVERT.ParseBool(attributeValue);
			}

			return null;
		}

		public bool GetBooleanAttribute (string attributeName, bool defaultValue, IEnumerable<AttributeValuePair> extraAttributes = null)
		{
			bool? attributeValue = GetBooleanAttribute(attributeName, extraAttributes);

			if (attributeValue.HasValue)
			{
				return attributeValue.Value;
			}

			return defaultValue;
		}

		public DateTime? GetDateTimeAttribute (string attributeName, IEnumerable<AttributeValuePair> extraAttributes = null)
		{
			string attributeValue = GetAttribute(attributeName, extraAttributes);
			if (! string.IsNullOrEmpty(attributeValue))
			{
				return CONVERT.ParseDateTime(attributeValue);
			}

			return null;
		}

		public DateTime GetDateTimeAttribute (string attributeName, DateTime defaultValue, IEnumerable<AttributeValuePair> extraAttributes = null)
		{
			DateTime? attributeValue = GetDateTimeAttribute(attributeName, extraAttributes);

			if (attributeValue.HasValue)
			{
				return attributeValue.Value;
			}

			return defaultValue;
		}

		public DateTime? GetDateAttribute (string attributeName, IEnumerable<AttributeValuePair> extraAttributes = null)
		{
			string attributeValue = GetAttribute(attributeName, extraAttributes);
			if (! string.IsNullOrEmpty(attributeValue))
			{
				return CONVERT.ParseDate(attributeValue);
			}

			return null;
		}

		public DateTime GetDateAttribute (string attributeName, DateTime defaultValue, IEnumerable<AttributeValuePair> extraAttributes = null)
		{
			DateTime? attributeValue = GetDateAttribute(attributeName, extraAttributes);

			if (attributeValue.HasValue)
			{
				return attributeValue.Value;
			}

			return defaultValue;
		}

		public bool HasAttribute(string attr)
		{
			return _Attributes.ContainsKey(attr);
		}

		//Set build an array list and reuse the setAttrs by ArrayList
		public void SetAttributes(string[] keys, string[] vals)
		{
		  ArrayList attrs = new ArrayList();
		  
		  int attrcount=0;
			for(int i=0; i<keys.Length; ++i)
			{
				_Attributes[keys[i]] = vals[i];
				AttributeValuePair avp1 = new AttributeValuePair(keys[i], vals[i]);
				attrs.Add(avp1);
				attrcount++;
			}
			if (attrcount>0)
			{	
				SetAttributes(attrs);
			}			
		}

		void SetAttribute(string key, string val, Boolean emitEvent)
		{
			AttributeValuePair avp = new AttributeValuePair(key, val);
			ArrayList attrs = new ArrayList();
			attrs.Add(avp);

			if (IsInTransaction)
			{
				AddAttributeChangeToTransactions(key, val);
			}
			
			if (emitEvent)
			{
				if(null != PreAttributesChanged)
				{
					object[] orgs = new object[2];
					orgs[0] = this;
					orgs[1] = attrs;
					// We unroll the delegates so that if one fails then we can log the error
					// and still call the rest...
					System.Delegate[] delegates = PreAttributesChanged.GetInvocationList();
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
							AppInfo.TheInstance.WriteLine("Node PreAttributesChanged Exception : " + ex.Message + ":\r\n" + ex.StackTrace);
						}
#endif
					}
				}
			}

			string oldName = "";
			if("name" == key)
			{
				// We are changing our unique name so we must warn the tree
				// before we change it.
				if(_Attributes.ContainsKey("name"))
				{
					oldName = (string) _Attributes["name"];
					_tree.ChangeUniqueName(this, oldName, val);
				}
			}

			if (key == "type")
			{
				_tree.RecordNodeTypeChange(this, _Attributes["type"], val);
			}

			_Attributes[key] = val;
			if ("name" == key)
			{
				_tree.AfterChangeUniqueName(this, oldName, val);
			}


			//
			if (emitEvent && ! IsInTransaction)
			{
				_tree.NodeAttributesChanged(this, attrs);
				_tree.NodeEarlyAttributesChanged(this, attrs);

				if(null != AttributesChanged)
				{
					object[] orgs2 = new object[2];
					orgs2[0] = this;
					orgs2[1] = attrs;
					// We unroll the delegates so that if one fails then we can log the error
					// and still call the rest...
					System.Delegate[] delegates2 = AttributesChanged.GetInvocationList();
					foreach(System.Delegate d in delegates2)
					{
#if !PASSEXCEPTIONS
						try
						{
#endif
							d.DynamicInvoke(orgs2);
#if !PASSEXCEPTIONS
						}
						catch(Exception ex)
						{
							AppInfo.TheInstance.WriteLine("Node AttributesChanged Exception : " + ex.Message + ":\r\n" + ex.StackTrace);
						}
#endif
					}

					//AttributesChanged(this,attrs);

					if(null != PostAttributesChanged)
					{
						object[] orgs = new object[2];
						orgs[0] = this;
						orgs[1] = attrs;
						// We unroll the delegates so that if one fails then we can log the error
						// and still call the rest...
						System.Delegate[] delegates = PostAttributesChanged.GetInvocationList();
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
								AppInfo.TheInstance.WriteLine("Node PostAttributesChanged Exception : " + ex.Message + ":\r\n" + ex.StackTrace);
							}
#endif
						}
						//PostAttributesChanged(this,attrs);
					}
				}
			}
		}

		public void SetAttributes (ArrayList attrsIn)
		{
			SetAttributes(attrsIn, true);
		}

		public void SetAttributes (List<AttributeValuePair> attrsIn)
		{
			SetAttributes(new ArrayList (attrsIn), true);
		}

		public void SetAttributes (ArrayList attrsIn, bool emitEvents)
		{
			// The list may get modified by the pre-change delegates, but we don't
			// want to overwrite the supplied copy, so we clone it here.
			ArrayList attrs = new ArrayList (attrsIn);

			int attrcount=0;

			if (IsInTransaction)
			{
				AddAttributeChangeToTransactions(attrsIn);
			}
			else if((null != PreAttributesChanged) && emitEvents)
			{
				object[] orgs = new object[2];
				orgs[0] = this;
				orgs[1] = attrs;
				// We unroll the delegates so that if one fails then we can log the error
				// and still call the rest...
				System.Delegate[] delegates = PreAttributesChanged.GetInvocationList();
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
						AppInfo.TheInstance.WriteLine("Node PreAttributesChanged Exception : " + ex.Message + ":\r\n" + ex.StackTrace);
					}
#endif
				}
				//PreAttributesChanged(this,attrs);
			}

			//Mind to notiofy if there is a type change in there 
			foreach (AttributeValuePair avp in attrs)
			{
				if (avp.Attribute == "type")
				{
					_tree.RecordNodeTypeChange(this, _Attributes["type"], avp.Value);
				}
			}


			foreach (AttributeValuePair avp in attrs)
			{
				if (avp != null)
				{
					if(avp.Attribute == "name")
					{
						// Double book check that this is unique in the tree!
						Node n = this._tree.GetNamedNode(avp.Value);
						if( (n == this) || (n == null) )
						{
							SetAttribute(avp.Attribute, avp.Value, false);
							attrcount++;

							if(n == null)
							{
								// We must add this as a unique named node.
								this._tree.AddNamedNode(avp.Value, this, emitEvents);
							}
						}
						else
						{
							throw( new Exception("Node with name [" + avp.Value + "] already exists!") );
						}
					}
					else
					{
						SetAttribute(avp.Attribute, avp.Value, false);
						attrcount++;
					}
				}
			}
			if ((attrcount>0) && ! IsInTransaction)
			{
				if (emitEvents)
				{
					_tree.NodeEarlyAttributesChanged(this, attrs);
				}
				if ((null != AttributesChanged) && emitEvents)
				{
					object[] orgs = new object[2];
					orgs[0] = this;
					orgs[1] = attrs;
					// We unroll the delegates so that if one fails then we can log the error
					// and still call the rest...
					System.Delegate[] delegates = AttributesChanged.GetInvocationList();
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
							AppInfo.TheInstance.WriteLine("Node AttributesChanged Exception : " + ex.Message + ":\r\n" + ex.StackTrace);
						}
#endif
					}
					//AttributesChanged(this,attrs);
				}
				if((null != PostAttributesChanged) && emitEvents)
				{
					object[] orgs = new object[2];
					orgs[0] = this;
					orgs[1] = attrs;
					// We unroll the delegates so that if one fails then we can log the error
					// and still call the rest...
					System.Delegate[] delegates = PostAttributesChanged.GetInvocationList();
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
							AppInfo.TheInstance.WriteLine("Node PostAttributesChanged Exception : " + ex.Message + ":\r\n" + ex.StackTrace);
						}
#endif
					}
					//PostAttributesChanged(this,attrs);
				}

				if (emitEvents)
				{
					_tree.NodeAttributesChanged(this, attrs);
				}
			}
		}

		public void SetAttributeIfNotEqual<T> (string key, T value)
		{
			var val = value.ToString();

			if (_Attributes[key] != val)
			{
				SetAttribute(key, val);
			}
		}

		public void SetAttribute(string key, double val)
		{
			SetAttribute(key,CONVERT.ToStr(val));
		}

		public void SetAttribute(string key, int val)
		{
			SetAttribute(key,CONVERT.ToStr(val));
		}

		public void SetAttribute(string key, string val)
		{
			SetAttribute(key, val, true);
		}

		public void SetAttribute (string key, bool val)
		{
			SetAttribute(key, CONVERT.ToStr(val));
		}

		public void SetAttribute (string key, DateTime val)
		{
			SetAttribute(key, CONVERT.ToStr(val));
		}

		public void SetDateAttribute (string key, DateTime val)
		{
			SetAttribute(key, CONVERT.ToDateStr(val));
		}
        
	    public void ModifyIntAttribute (string attributeName, int value, int defaultValue, Func<int, int, int> operation)
	    {
	        var currentValue = GetIntAttribute(attributeName, defaultValue);

	        currentValue = operation(currentValue, value);

            SetAttribute(attributeName, currentValue);
	    }

	    public void IncrementIntAttribute (string attributeName, int value, int defaultValue)
	    {
            ModifyIntAttribute(attributeName, value, defaultValue, (c,v) => c + v);
	    }

		public virtual void BuildLinkNodes()
		{
			foreach(Node c in this)
			{
				c.BuildLinkNodes();
			}
		}

		public Node(NodeTree tree, Node parent, string type)
		{
			_parent = parent;
			_tree = tree;
			_NodeType = type.Replace(" ", "_");
			//
			_ID = tree.AddNodeCreateID(this);

			SetAttribute("name", CONVERT.Format("uuid_{0}", _ID));
		}

		// Only use this constructor for dynamic stuff for now as
		// this is the only one that will fire the run time events!
        public Node(Node parent, string type, string uniqueNodeName, AttributeValuePair avp)
		{
			_parent = parent;
			_tree = parent._tree;
			_NodeType = type;
			if(null != avp) this.SetAttribute(avp.Attribute,avp.Value);
			//
			_ID = _tree.AddNodeCreateID(this);
			//
			if ("" != uniqueNodeName)
			{
				SetAttribute("name", uniqueNodeName, false);
			}
			else
			{
			}
			//
			if (_tree != null)
			{
				_tree.AddNamedNode(uniqueNodeName, this);
				_tree.FireAddedNode(this);
			}
			//
			parent.AddChild(this);
			//
		}

		// Runtime constructor...
		public Node(Node parent, XmlNode xnode)
		{
			Build(parent._tree, parent, xnode);
			//
			if(_tree != null)
			{
				_tree.FireAddedNode(this);
			}
			//
			parent.AddChild(this);
			//
			_tree = parent.Tree;
			_ID = _tree.AddNodeCreateID(this);
			//

			if (! _Attributes.ContainsKey("name"))
			{
				SetAttribute("name", CONVERT.Format("uuid_{0}", _ID));
			}
		}

		public Node(Node parent, XmlNode xnode, ArrayList extraAttributes)
		{
			Build(parent._tree, parent, xnode);

			if (extraAttributes != null)
			{
				SetAttributes(extraAttributes);
			}

			//
			if(_tree != null)
			{
				_tree.FireAddedNode(this);
			}
			//
			parent.AddChild(this);
			//
			_tree = parent.Tree;
			_ID = _tree.AddNodeCreateID(this);
			//
			if (! _Attributes.ContainsKey("name"))
			{
				SetAttribute("name", CONVERT.Format("uuid_{0}", _ID));
			}
		}

		public Node (Node parent, string type, string uniqueNodeName, List<AttributeValuePair> attributes)
			: this (parent, type, uniqueNodeName, new ArrayList (attributes))
		{
		}

		// string dictionary one!
		// Only use this constructor for dynamic stuff for now as
		// this is the only one that will fire the run time events!
		public Node(Node parent, string type, string uniqueNodeName, ArrayList attrs)
		{
			_parent = parent;
			_tree = parent._tree;
			_NodeType = type.Replace(" ", "_");
			//
			_ID = _tree.AddNodeCreateID(this);

			if ("" != uniqueNodeName)
			{
				SetAttribute("name", uniqueNodeName, false);
			}
			else
			{
				SetAttribute("name", CONVERT.Format("uuid_{0}", _ID));
			}
			//
			if(null != attrs) 
			{
				this.SetAttributes(attrs, parent.Tree.EmitApplyAttributesEventsOnNodeCreation);
			}

		    {
		        _tree.AddNamedNode(uniqueNodeName, this);
		    }

            parent.AddChild(this);
			//
			//if(uniqueNodeName != "")
			
		}
			
		internal Node(NodeTree tree, Node parent, XmlNode xnode)
		{
			Build(tree,parent,xnode);

			if(_tree != null)
			{
				_tree.FireAddedNode(this);
			}
		}

		protected void Build(NodeTree tree, Node parent, XmlNode xnode)
		{
			_parent = parent;
			_tree = tree;
			_NodeType = xnode.Name;

			string name = "";
			
			//System.Diagnostics.Debug.WriteLine("Node " + _NodeType);

            List<AttributeValuePair> attributes = new List<AttributeValuePair> ();
			
			// Pull Attributes first...
			foreach(XmlAttribute a in xnode.Attributes)
			{
				string val = CreateNodeAttributeFromXml(a.Value);
				//
				if(a.Name.ToLower() == "uuid")
				{
					ulong id = CONVERT.ParseULong(val);
					
					if(tree.HasID(id))
					{
						//Console.WriteLine("ID Exists in tree! : " + reader.Value);
						// Create with a new ID so that we can continue.
						_ID = tree.AddNodeCreateID(this);
					}
					else
					{
						_ID = id;
						tree.AddNodeID(this,id);
					}					
				}
				else
				{
					if(a.Name.ToLower() == "name")
					{
						name = val;
						//tree.AddNamedNode(a.Value,this);
					}
				    attributes.Add(new AttributeValuePair(a.Name.ToLower(), val));
				}

				if (a.Name.ToLower() == "type")
				{
					if (a.Value.Replace("_", "").ToLower() == _NodeType.ToLower())
					{
						_NodeType = a.Value;
					}
				}
			}

            foreach (AttributeValuePair avp in RemapSpecialAttributes(tree, attributes))
		    {
                _Attributes.Add(avp.Attribute, avp.Value);
		    }

		    if(0 == _ID) _ID = tree.AddNodeCreateID(this);

			tree.AddNamedNode(name,this);
				
			foreach(XmlNode child in xnode.ChildNodes)
			{
				if(child.NodeType == XmlNodeType.Element)
				{
					if((child.Name.ToLower() == "link")
						|| (child.Name.ToLower() == "connection"))
					{
						_Children.Add( new LinkNode(tree, this, child) );
					}
					else
					{
						_Children.Add( new Node(tree, this, child) );
					}
				}
			}

			if (_Attributes.ContainsKey("type"))
			{
				_tree.RecordNodeTypeChange(this, "", _Attributes["type"]);
			}
		}

        public static List<AttributeValuePair> RemapSpecialAttributes(NodeTree nt, System.Collections.ArrayList valuesToApply)
        {
            List<AttributeValuePair> attributes = new List<AttributeValuePair>();
            foreach (AttributeValuePair avp in valuesToApply)
            {
                attributes.Add(avp);
            }

            return RemapSpecialAttributes(nt, attributes);
        }

        public static List<AttributeValuePair> RemapSpecialAttributes(NodeTree nt, List<AttributeValuePair> valuesToApply)
        {
            List<AttributeValuePair> attributes = new List<AttributeValuePair>();
            foreach (AttributeValuePair avp in valuesToApply)
            {
				AttributeValuePair copiedAvp = new AttributeValuePair (avp);

                // Find all occurances of special token strings
                var matches = Regex.Matches(copiedAvp.Value, @"#([a-zA-Z0-9\-_]+).([a-zA-Z0-9\-_]+)#");
                if (matches.Count > 0)
                {
                    foreach (Match match in matches)
                    {
                        string nodeName = match.Groups[1].Value;
                        string attributeName = match.Groups[2].Value;

						copiedAvp.Value = copiedAvp.Value.Replace(match.Value, nt.GetNamedNode(nodeName).GetAttribute(attributeName));
                    }
                }

				attributes.Add(copiedAvp);
            }

            return attributes;
        }

		string CreateNodeAttributeFromXml (string attributeValue)
		{
			attributeValue = attributeValue.Replace("\\r\\n", "\r\n");

			if (attributeValue.StartsWith("#")
				&& attributeValue.EndsWith("#"))
			{
				string trimmed = attributeValue.Substring(1, attributeValue.Length - 2);
				string [] parts = trimmed.Split('.');
				attributeValue = Tree.GetNamedNode(parts[0]).GetAttribute(parts[1]);
			}

			return attributeValue;
		}

		public Node Parent
		{
			get { return _parent; }
		}

		public bool HasLinkTo(Node n)
		{
			foreach(Node c in _Children)
			{
				LinkNode ln = c as LinkNode;
				if(null != ln)
				{
					if(ln.To == n)
					{
						return true;
					}
				}
			}

			return false;
		}

		public void AddChild(Node child)
		{
			if((child.Parent != null) && (child.Parent != this))
			{
				child.Parent.RemoveFromChildren(child);
			}
			_Children.Add(child);
			child._parent = this;
			// TODO : Should fire any new events (added/lost child, changed parent etc).
			if(null != this.ChildAdded)
			{
				object[] orgs = new object[2];
				orgs[0] = this;
				orgs[1] = child;
				// We unroll the delegates so that if one fails then we can log the error
				// and still call the rest...
				System.Delegate[] delegates = ChildAdded.GetInvocationList();
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
						AppInfo.TheInstance.WriteLine("Node ChildAdded Exception : " + ex.Message + ":\r\n" + ex.StackTrace);
					}
#endif
				}
				//ChildAdded(this, child);
			}

			if(null != child.ParentChanged)
			{
				object[] orgs = new object[2];
				orgs[0] = this;
				orgs[1] = child;
				// We unroll the delegates so that if one fails then we can log the error
				// and still call the rest...
				System.Delegate[] delegates = child.ParentChanged.GetInvocationList();
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
						AppInfo.TheInstance.WriteLine("Node ParentChanged Exception : " + ex.Message + ":\r\n" + ex.StackTrace);
					}
#endif
				}
				//child.ParentChanged(this, child);
			}
		}

		public ArrayList getChildrenClone()
		{
			return (ArrayList) _Children.Clone();
		}

		public ArrayList getChildren()
		{
			return _Children;
		}

		public List<Node> GetChildren ()
		{
			return _Children.Cast<Node>().ToList();
		}

		public List<Node> GetChildrenAsList ()
		{
			return _Children.Cast<Node>().ToList();
		}

		public List<Node> GetChildrenWhere (Func<Node, bool> predicate)
		{
			return _Children.Cast<Node>().Where(predicate).ToList();
		}

		public Node GetFirstChild (Func<Node, bool> predicate)
		{
			return _Children.Cast<Node>().FirstOrDefault(predicate);
		}

		/// <summary>
		/// Will throw an exception if more than one child matches with the predicate.
		/// </summary>
		/// <param name="predicate"></param>
		/// <returns></returns>
		public Node GetSingleChild(Func<Node, bool> predicate)
		{
			return _Children.Cast<Node>().SingleOrDefault(predicate);
		}

		public ArrayList GetChildrenOfType (string type)
		{
			ArrayList children = new ArrayList ();

			foreach (Node child in _Children)
			{
				if (child.GetAttribute("type") == type)
				{
					children.Add(child);
				}
			}

			return children;
		}
		public List<Node> GetChildrenOfTypeAsList (string type)
		{
			return GetChildrenWhere(n => n.GetAttribute("type") == type);
		}

		public List<Node> GetChildrenWithAttribute(string attribute)
		{
			return GetChildrenWhere(n => n.HasAttribute(attribute));
        }


        public List<Node> GetChildrenWithAttributeValue(string attribute, string value)
        {
	        return GetChildrenWhere(n => n.GetAttribute(attribute) == value);
        }

        public Node GetChildWithAttributeValue(string attribute, string value)
        {
	        return GetSingleChild(n => n.GetAttribute(attribute) == value);
		}

		public Node TryGetChildWithAttributeValue (string attribute, string value)
		{
			var children = GetChildrenWithAttributeValue(attribute, value);

			// Handle case where there is more than one child with this attribute value pair
			if (children.Count > 1)
			{
				throw new Exception("Node has more than one child with that attribute-value pairing.");
			}
			
			return children.FirstOrDefault();
		}

		public class NodeNameComparer : IComparer
		{
			public int Compare (object x, object y)
			{
				Node a = x as Node;
				Node b = y as Node;

				string aName = a.GetAttribute("shortdesc");
				if (aName == "")
				{
					aName = a.GetAttribute("desc");
				}
				if (aName == "")
				{
					aName = a.GetAttribute("name");
				}

				string bName = b.GetAttribute("shortdesc");
				if (bName == "")
				{
					bName = b.GetAttribute("desc");
				}
				if (bName == "")
				{
					bName = b.GetAttribute("name");
				}

				return aName.CompareTo(bName);
			}
		}

		public ArrayList GetSortedChildrenOfType (string type)
		{
			ArrayList children = GetChildrenOfType(type);
			children.Sort(new NodeNameComparer ());

			return children;
		}

		public bool HasChild(Node child)
		{
			return _Children.Contains(child);
		}

		//Used in Delete a Node and promoting it's children into children of Root 	
		internal void AddAsChild(Node n)
		{
			_Children.Add(n); 
			//sending out child added events 
			if(null != this.ChildAdded)
			{
				object[] orgs = new object[2];
				orgs[0] = this;
				orgs[1] = n;
				// We unroll the delegates so that if one fails then we can log the error
				// and still call the rest...
				System.Delegate[] delegates = ChildAdded.GetInvocationList();
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
						AppInfo.TheInstance.WriteLine("Node ChildAdded Exception : " + ex.Message + ":\r\n" + ex.StackTrace);
					}
#endif
				}
				//ChildAdded(this, n);
			}			
		}

		void RemoveFromChildren(Node n)
		{
				_Children.Remove(n);
				//sending out child removed events 
				if(null != this.ChildRemoved)
				{
					object[] orgs = new object[2];
					orgs[0] = this;
					orgs[1] = n;
					// We unroll the delegates so that if one fails then we can log the error
					// and still call the rest...
					System.Delegate[] delegates = ChildRemoved.GetInvocationList();
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
							AppInfo.TheInstance.WriteLine("Node ChildRemoved Exception : " + ex.Message + ":\r\n" + ex.StackTrace);
						}
#endif
					}
					//ChildRemoved(this, n);
				}			
		}

		//This removes the Child node (and all it's children)
		//Best way to remove a node [node.Parent.RemoveFromChildrenDWC(node)]
		//only needs to emit Child Removed, other events handled in call methods
		public void DeleteChildTree(Node n)
		{
			DeleteChildTree(n, true);
		}

		internal void DeleteChildTree(Node n, bool do_invoke)
		{
			if (n != null)
			{
				DeleteWithChildren(n,do_invoke);
				_Children.Remove(n);
				//sending out child removed events 
				if(do_invoke && (null != this.ChildRemoved))
				{
					object[] orgs = new object[2];
					orgs[0] = this;
					orgs[1] = n;
					// We unroll the delegates so that if one fails then we can log the error
					// and still call the rest...
					System.Delegate[] delegates = ChildRemoved.GetInvocationList();
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
							AppInfo.TheInstance.WriteLine("Node ChildRemoved Exception : " + ex.Message + ":\r\n" + ex.StackTrace);
						}
#endif
					}
					//ChildRemoved(this, n);
				}			
			}
		}

		// This method can be sped up considerably by being smarter.
		// If we have checked a sub-tree for delability then we
		// don't have to do it again if it hasn't changed.
		protected bool Deletable()
		{
			// You are never allowed to delete the ultimate root
			// node as this holds the base root nodes for all sub trees.
			if(null == this._parent) return false;

			string str = GetAttribute("del");
			if( (null != str) && ("" != str) )
			{
				if(!CONVERT.ParseBool(str, false)) return false;
			}

			//
			// If we are deletable but we have any children that
			// are not deletable _and_ we are in autoDeleteOrphans
			// mode then we must mark ourselves as non-deletable
			// as well.
			//
			/* Remove for just now as this wiil destroy our links
			   that we have to destroy carefully.
			   
			if(_tree._autoDeleteOrphans)
			{
				foreach(Node c in this)
				{
					if(!c.Deletable())
					{
						return false;
					}
				}
			}*/

			return true;
		}
		

		/// <summary>
		/// Only to be called internally by class methods once any
		/// conflict and restriction checking has been carried out.
		/// </summary>
		protected void ForcedDelete() // Events?
		{
			// Shallow copy of list of children.
			ArrayList cc = (ArrayList) _Children.Clone();

			foreach(Node c in cc)
			{
				// Only delete children if we are set to do this.
				// Our default behaviour is now to only delete child link
				// nodes!
				if((c._NodeType.ToLower() == "link")
					|| (c._NodeType.ToLower() == "connection"))
				{
					//System.Diagnostics.Debug.WriteLine("Deleting a link node.");
					c.ForcedDelete();
					_Children.Remove(c);
				}
			}

			// Do our back links...
			foreach(LinkNode ln in _ConnectionsFromOtherNodes)
			{
				// TODO : can we check that we always can do this?
				ln.Delete();
			}
		
			if(_parent != null)
			{
				_parent.RemoveFromChildren(this);
			}
			
			//Send out the Deleting event to anyone who cares for this node
			if ( null != Deleting)
			{
				Deleting(this);
			}

			//Send out the Deleting Node event to anyone who cares for this Tree
			//Force fire of event
			_tree.NodeDeleted(this);
		}


		//
		//This prepares for the deletion of this Node 
		//It kills all the children and prepares the body 
		//This is called as part of Delete Child 
		protected bool DeleteWithChildren(Node n, bool do_invoke)
		{
			//Go through the children and kill them and thier children
			ArrayList cc = (ArrayList) n._Children.Clone();
			foreach(Node c in cc)
			{
				n.DeleteChildTree(c,do_invoke);
			}	
			
			//Send out the Deleting event to anyone who cares for the node which is being killed
			if(do_invoke && ( null != n.Deleting))
			{
				object[] orgs = new object[1];
				orgs[0] = n;
				// We unroll the delegates so that if one fails then we can log the error
				// and still call the rest...
				System.Delegate[] delegates = n.Deleting.GetInvocationList();
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
						AppInfo.TheInstance.WriteLine("Node Deleting Exception : " + ex.Message + ":\r\n" + ex.StackTrace);
					}
#endif
				}
				//n.Deleting(n);
			}
			//Send out the Deleting Node event to anyone who cares for this Tree
			//Force fire of event
			if(do_invoke)
			{
				_tree.NodeDeleted(n);
			}
			/*
			n._tree = null;
			n._AssociatedObjects.Clear();
			n._Attributes.Clear();
			n._Children.Clear();
			n._parent = null;
			n._ConnectionsFromOtherNodes.Clear();
			*/
			return true;
		}

		//This prepares for the deletion of this Node 
		//It kills all the children and prepares the body 
		// This is called as part of Delete Child 
		protected bool Delete()
		{
			//System.Diagnostics.Debug.WriteLine("Trying to Deleting Node");
		
			if(!Deletable())
			{
				//System.Diagnostics.Debug.WriteLine("Cannot Delete this Node");
				return false; // Not allowed to delete.
			}
			
			LinkNode ln = this as LinkNode;
			if(null != ln)
			{
				//System.Diagnostics.Debug.WriteLine("Deleting Link Node");
			}
			
			//System.Diagnostics.Debug.WriteLine("Deleting Node");
			
			//
			// Delete this node and deal with children as required.
			//
			
			//if(!_tree._autoDeleteOrphans)
			{
				// All our node children become sub-root nodes in their
				// own right.
				foreach(Node c in this)
				{
					if(c._NodeType == "node")
					{
						RemoveFromChildren(c);
						_tree._Root.AddAsChild(c);
					}
				}
			}
			//
			// Delete all Links that attach to this node...
			//
			
			ForcedDelete();

			return false;
		}

		public override string ToString ()
		{
			string s = "";

			ArrayList attrs = new ArrayList (_Attributes.Keys);

			// Display the name first, if present.
			if (attrs.Contains("name"))
			{
				s += "name=\"" + (string) (_Attributes["name"]) + "\"";
				attrs.Remove("name");
			}

			foreach (Object attrname in attrs)
			{
				if (attrname != null)
				{
					string str1 = (string)(attrname);
					string str2 = (string)(_Attributes[str1]);

					if (s.Length > 0)
					{
						s += " ";
					}
					s += str1 + "=\"" + str2 + "\"";
				}
			}

			return s;
		}

		//Useful for debug
		public string toDataString(Boolean IncludeChildren)
		{
			string st= "";
			st= "Node " + CONVERT.ToStr(_ID) + " Type:"+ _NodeType;
			foreach (Object attrname in _Attributes.Keys)
			{
				if (attrname != null)
				{
				  string str1 = (string)(attrname);
				  string str2 = (string)(_Attributes[str1]);
				  st += "[Attr:("+str1+") Value:("+str2+")]";
				}
			}			
			return st;
		}

		void toXmlString(Boolean IncludeChildren, ref string st)
		{
			string nodeType = this.Type.Replace(" ", "_");
			if (string.IsNullOrEmpty(nodeType))
			{
				nodeType = "node";
			}

			if (nodeType == "Connection")
			{
				st += "<link ";
			}
			else
			{
				st += "<" + nodeType + " ";
			}

			var attributeNames = new List<string> (new ArrayList(_Attributes.Keys).ToArray().Cast<string>());
			attributeNames.Sort();

			foreach (string attrname in attributeNames)
			{
				if (attrname != null)
				{
					string val = (string)(_Attributes[attrname].Replace("&", "&amp;"));
					st += attrname + "=\"" + val + "\" ";
				}
			}
			//
			if(IncludeChildren)
			{
				if(this.getChildren().Count == 0)
				{
					st += "/>";
				}
				else
				{
					st += ">";

					foreach(Node n in this)
					{
						n.toXmlString(IncludeChildren, ref st);
					}

					st += "</" + nodeType + ">";
				}
			}
			else
			{
				st += "/>";
			}
		}

		public string toXmlString(Boolean IncludeChildren)
		{
			string st = "";
			//
			toXmlString(IncludeChildren, ref st);
			//
			return st;
		}

		/// <summary>
		/// Property wrapper for the ToString() method so that VS 2002's debugger
		/// can understand nodes.
		/// </summary>
		public string AsString
		{
			get
			{
				return ToString();
			}
		}

		public Node GetFirstChildOfType (string type)
		{
			foreach (Node child in _Children)
			{
				if (child.GetAttribute("type") == type)
				{
					return child;
				}
			}

			return null;
		}

		public Node GetFirstChild ()
		{
			return _Children[0] as Node;
		}

		public ArrayList GetAttributes ()
		{
			ArrayList attributes = new ArrayList ();

			foreach (string name in _Attributes.Keys)
			{
				attributes.Add(new AttributeValuePair (name, _Attributes[name]));
			}

			return attributes;
		}

		public void DeleteChildren ()
		{
			foreach (Node child in getChildrenClone())
			{
				DeleteChildTree(child);
			}
		}

		public void DeleteChildrenWhere (Func<Node, bool> predicate)
		{
			var childrenToBeDeleted = GetChildrenAsList().Where(predicate);

			foreach (var child in childrenToBeDeleted)
			{
				DeleteChildTree(child);
			}
		}


		List<NodeChangeTransaction> currentTransactions;
		public NodeChangeTransaction CreateTransaction ()
		{
			NodeChangeTransaction transaction = new NodeChangeTransaction (this);

			if (currentTransactions == null)
			{
				currentTransactions = new List<NodeChangeTransaction> ();
			}

			currentTransactions.Add(transaction);

			return transaction;
		}

		internal void CommitTransaction (NodeChangeTransaction transaction, List<AttributeValuePair> newAttributes)
		{
			currentTransactions.Remove(transaction);
			if (currentTransactions.Count == 0)
			{
				currentTransactions = null;
			}

			if (newAttributes.Count > 0)
			{
				ArrayList newAttributesAsArrayList = new ArrayList(newAttributes);
				object [] args = new object [] { this, newAttributesAsArrayList };

				_tree.NodeAttributesChanged(this, newAttributesAsArrayList);
				_tree.NodeEarlyAttributesChanged(this, newAttributesAsArrayList);

				if (AttributesChanged != null)
				{
					foreach (System.Delegate d in AttributesChanged.GetInvocationList())
					{
#if !PASSEXCEPTIONS
					try
					{
#endif
						d.DynamicInvoke(args);
#if !PASSEXCEPTIONS
					}
					catch (Exception ex)
					{
						AppInfo.TheInstance.WriteLine("Node AttributesChanged Exception : " + ex.Message + ":\r\n" + ex.StackTrace);
					}
#endif
					}

					if (PostAttributesChanged != null)
					{
						foreach (System.Delegate d in PostAttributesChanged.GetInvocationList())
						{
#if !PASSEXCEPTIONS
						try
						{
#endif
							d.DynamicInvoke(args);
#if !PASSEXCEPTIONS
						}
						catch (Exception ex)
						{
							AppInfo.TheInstance.WriteLine("Node PostAttributesChanged Exception : " + ex.Message + ":\r\n" + ex.StackTrace);
						}
#endif
						}
					}
				}
			}
		}

		void AddAttributeChangeToTransactions (string attributeName, string attributeValue)
		{
			if (currentTransactions != null)
			{
				foreach (NodeChangeTransaction transaction in currentTransactions)
				{
					transaction.AddAttributeChange(attributeName, attributeValue);
				}
			}
		}

		void AddAttributeChangeToTransactions (AttributeValuePair avp)
		{
			AddAttributeChangeToTransactions(avp.Attribute, avp.Value);
		}

		void AddAttributeChangeToTransactions (List<AttributeValuePair> newAttributes)
		{
			foreach (AttributeValuePair avp in newAttributes)
			{
				AddAttributeChangeToTransactions(avp);
			}
		}

		void AddAttributeChangeToTransactions (ArrayList newAttributes)
		{
			foreach (AttributeValuePair avp in newAttributes)
			{
				AddAttributeChangeToTransactions(avp);
			}
		}

		bool IsInTransaction
		{
			get
			{
				return ((currentTransactions != null) && (currentTransactions.Count > 0));
			}
		}

		public void AddAttributesIfNotEqual (IList<AttributeValuePair> attributes, string attributeName, string attributeValue)
		{
			// Remove any other queued assignments to this attribute.
			foreach (AttributeValuePair avp in new List<AttributeValuePair> (attributes))
			{
				if (avp.Attribute == attributeName)
				{
					attributes.Remove(avp);
				}
			}

			// Add an assignment if needed.
			if (GetAttribute(attributeName) != attributeValue)
			{
				attributes.Add(new AttributeValuePair (attributeName, attributeValue));
			}
		}

		public void AddAttributesIfNotEqual (IList<AttributeValuePair> attributes, string attributeName, int attributeValue)
		{
			AddAttributesIfNotEqual(attributes, attributeName, CONVERT.ToStr(attributeValue));
		}

		public void AddAttributesIfNotEqual (IList<AttributeValuePair> attributes, string attributeName, double attributeValue)
		{
			AddAttributesIfNotEqual(attributes, attributeName, CONVERT.ToStr(attributeValue));
		}

		public void AddAttributesIfNotEqual (IList<AttributeValuePair> attributes, string attributeName, bool attributeValue)
		{
			AddAttributesIfNotEqual(attributes, attributeName, CONVERT.ToStr(attributeValue));
		}

		public Node CopyTo (Node newParent, string name = "")
		{
			List<AttributeValuePair> attributes = new List<AttributeValuePair> ();
			foreach (string attributeName in _Attributes.Keys)
			{
				if ((attributeName != "name") && (attributeName != "UUID"))
				{
					attributes.Add(new AttributeValuePair (attributeName, _Attributes[attributeName]));
				}
			}

			return new Node (newParent, Type, name, attributes);
		}

		public Node CreateChild (string type, string name, IList<AttributeValuePair> attributes)
		{
			if (! attributes.Any(avp => (avp.Attribute == "type")))
			{
				attributes.Add(new AttributeValuePair ("type", type));
			}
			return new Node (this, type, name, new ArrayList (attributes.ToArray()));
		}

		public Node CreateChild (string type, string name, params AttributeValuePair [] attributes)
		{
			var attributesList = new List<AttributeValuePair> (attributes);
			return CreateChild(type, name, attributesList);
		}

		public Node FindChildNodeWithSearchAttributes (string searchTerm)
		{
			var searchPrefix = ".$";
			var searchPrefixIndex = searchTerm.IndexOf(searchPrefix);
			string searchTail = null;

			if (searchPrefixIndex != -1)
			{
				searchTail = searchTerm.Substring(searchPrefixIndex + searchPrefix.Length);
				searchTerm = searchTerm.Substring(0, searchPrefixIndex);
			}

			var searchCriteria = searchTerm.Split('+').Select(s => NodeTree.CreateAttributeValuePairFromSearchTerm(s)).ToList();

			Node foundNode = null;
			foreach (Node child in _Children)
			{
				if (searchCriteria.All(avp => child.GetAttribute(avp.Attribute) == avp.Value))
				{
					foundNode = child;
					break;
				}
			}

			if (foundNode == null)
			{
				return null;
			}
			else if (string.IsNullOrEmpty(searchTail))
			{
				return foundNode;
			}
			else
			{
				return foundNode.FindChildNodeWithSearchAttributes(searchTail);
			}
		}

		public Node GetChildWithAttributeValues (params string [] attributesAndValues)
		{
			var attributeValuePairs = new List<AttributeValuePair> ();
			for (var i = 0; i < attributeValuePairs.Count; i += 2)
			{
				attributeValuePairs.Add(new AttributeValuePair (attributesAndValues[i], attributesAndValues[i + 1]));
			}

			return GetChildWithAttributeValues(attributeValuePairs);
		}

		public Node GetChildWithAttributeValues (IList<AttributeValuePair> attributesAndValues)
		{
			return GetChildWithAttributeValues(attributesAndValues.ToArray());
		}

		public Node GetChildWithAttributeValues (params AttributeValuePair [] attributesAndValues)
		{
			return _Children.Cast<Node>().SingleOrDefault(n => attributesAndValues.All(avp => n.GetAttribute(avp.Attribute) == avp.Value));
		}
	}
}