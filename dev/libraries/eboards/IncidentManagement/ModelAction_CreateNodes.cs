using System.Collections;
using System.Collections.Generic;
using System.Xml;

using LibCore;
using Network;

namespace IncidentManagement
{
	/// <summary>
	/// This event can create a new Node on the tree 
	/// </summary>
	public class ModelAction_CreateNodes : TargetedModelAction
	{
		string CreateNodesXML = null;

		bool deleteExisting = false;

		public IncidentDefinition OwningIncident = null;

		public override object Clone ()
		{
			ModelAction_CreateNodes newCreate = new ModelAction_CreateNodes();
			newCreate.CreateNodesXML = this.CreateNodesXML;
			newCreate.target = this.target;
			newCreate.doAfterSecs = this.doAfterSecs;
			newCreate.OwningIncident = this.OwningIncident;
			newCreate.deleteExisting = this.deleteExisting;
			return newCreate;
		}

		protected ModelAction_CreateNodes ()
		{
		}


		public ModelAction_CreateNodes (XmlNode n)
		{
			foreach (XmlAttribute a in n.Attributes)
			{
				if (a.Name == "i_to")
				{
					target = a.Value;
				}
				else if (a.Name == "i_doAfterSecs")
				{
					doAfterSecs = CONVERT.ParseInt(a.Value);
				}
				else if (a.Name == "i_cancelWithIncident")
				{
					cancelWithIncident = CONVERT.ParseInt(a.Value);
				}
				else if (a.Name == "i_deleteExisting")
				{
					deleteExisting = CONVERT.ParseBool(a.Value, false);
				}
			}
			//
			CreateNodesXML = n.OuterXml;
		}

		override public void ApplyAction (NodeTree nt)
		{
			if (doAfterSecs > 0)
			{
				GlobalEventDelayer.TheInstance.Delayer.AddEvent(this, doAfterSecs, nt);
			}
			else
			{
				ApplyActionNow(nt);
			}
		}

		public override void ApplyAction (INodeChanger nodeChanger)
		{
			ApplyActionNow(nodeChanger);
		}

		public override void ApplyActionNow (NodeTree nt)
		{
			ApplyActionNow(nt, null);
		}

		public override void ApplyActionNow (INodeChanger nodeChanger)
		{
			ApplyActionNow(null, nodeChanger);
		}

		void ApplyActionNow (NodeTree nt, INodeChanger nodeChanger)
		{
			if (nodeChanger != null)
			{
				nt = nodeChanger.Model;
			}
            var oldTargetAttribute = new List<AttributeValuePair> { new AttributeValuePair("target", target) };
            var newTargetAttribute = Node.RemapSpecialAttributes(nt, oldTargetAttribute);

            var newTarget = newTargetAttribute[0].Value;

            Node n = nt.GetNamedNode(newTarget);

			if ((n == null)
				&& newTarget.Contains("."))
			{
				var split = newTarget.IndexOf(".");
				var parent = newTarget.Substring(0, split);
				var stub = newTarget.Substring(split + 1);

				n = nt.GetNamedNode(parent).GetFirstChildOfType(stub);
			}

			if (n != null)
			{
				//Need to create the xmlnode from the string
				XmlDocument x1 = new XmlDocument();
				x1.LoadXml(CreateNodesXML);
				XmlNode CreateNodes = x1.DocumentElement;
				//
				foreach (XmlNode cn in CreateNodes)
				{
					if (cn.NodeType == XmlNodeType.Element)
					{
						if (deleteExisting)
						{
							XmlAttribute newNameAttribute = cn.Attributes["name"];
							if (newNameAttribute != null)
							{
								string newName = newNameAttribute.Value;

								if (newName != "")
								{
									Node existingNode = n.Tree.GetNamedNode(newName);
									if (existingNode != null)
									{
										if (nodeChanger != null)
										{
											nodeChanger.Delete(existingNode);
										}
										else
										{
											existingNode.Parent.DeleteChildTree(existingNode);
										}
									}
								}
							}
						}

						// Now we can create the node.
						// This node may be a link node...
						if ((cn.Name.ToLower() == "link")
							|| (cn.Name.ToLower() == "connection"))
						{
							LinkNode newNode = new LinkNode(n, cn);
						}
						else // 
						{
							ArrayList attributes = new ArrayList();

							// Some magic special processing.
							// CostedEvents, if of type incident or prevented_incident, ought
							// to have a failure attribute describing them.
							// If there isn't one, we can deduce it and slip it in at creation time.
							if (cn.Name == "CostedEvent")
							{
								XmlAttribute typeAttribute = cn.Attributes["type"];
								string type = "";
								if (typeAttribute != null)
								{
									type = typeAttribute.Value;
								}

								if ((type == "incident") || (type == "prevented_incident"))
								{
									XmlAttribute failureAttribute = cn.Attributes["failure"];
									if (failureAttribute == null)
									{
										if (OwningIncident != null)
										{
											ArrayList targetIds = OwningIncident.GetTargets();

											foreach (string targetId in targetIds)
											{
												Node targetNode = n.Tree.GetNamedNode(targetId);
												if (targetNode != null)
												{
													string nodeTypeName = targetNode.GetAttribute("type");
													bool handled = false;
													string failure = "";
													switch (nodeTypeName.ToLower())
													{
														case "application":
														case "app":
															nodeTypeName = CoreUtils.SkinningDefs.TheInstance.GetData("appname", "Application");
															handled = true;
															break;

														case "server":
															nodeTypeName = CoreUtils.SkinningDefs.TheInstance.GetData("servername", "Server");
															handled = true;
															break;

														case "router":
															handled = true;
															break;

														case "database":
														case "db":
															handled = true;
															break;

														case "hub":
															failure = "Hub";
															handled = true;
															break;
													}

													if (handled)
													{
														if (failure == "")
														{
															failure = nodeTypeName + " " + targetId;
														}
														attributes.Add(new AttributeValuePair("failure", failure));
														break;
													}
												}
											}
										}
									}
								}
							}

							if (nodeChanger != null)
							{
								System.Collections.Generic.List<AttributeValuePair> attributeList = new System.Collections.Generic.List<AttributeValuePair> ();
								string name = "";
								string type = "";
								foreach (XmlAttribute attribute in cn.Attributes)
								{
									bool includeAttribute = true;

									switch (attribute.Name)
									{
										case "name":
											name = attribute.Value;
											includeAttribute = false;
											break;

										case "type":
											type = attribute.Value;
											break;

										case "uuid":
											includeAttribute = false;
											break;
									}

									if (includeAttribute)
									{
										attributeList.Add(new AttributeValuePair (attribute.Name, attribute.Value));
									}
								}
								attributeList.AddRange((AttributeValuePair []) attributes.ToArray(typeof (AttributeValuePair)));

								nodeChanger.Create(n, name, type, attributeList);
							}
							else
							{
								new Node (n, cn, new ArrayList (Node.RemapSpecialAttributes(nt, attributes).ToArray()));
							}
						}
					}
				}
			}
		}

		public override Dictionary<Node, Dictionary<string, string>> GetAllTargetsAndAttributes (NodeTree model)
		{
			return new Dictionary<Node, Dictionary<string, string>>();
		}
	}
}