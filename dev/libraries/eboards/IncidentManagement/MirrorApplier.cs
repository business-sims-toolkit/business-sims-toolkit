using System;
using System.Collections;
using System.Xml;

using LibCore;
using Network;

namespace IncidentManagement
{
	/// <summary>
	/// The MirrorApplier listens to a MirrorCommandQueue for adding or removing mirrors.
	/// It reads a mirror_defs.xml file that instuct it how to apply the mirrors.
	/// </summary>
	public class MirrorApplier
	{
		protected ArrayList options;
		protected NodeTree _model;
		protected Node mirrorCommandQueueNode;

		public ArrayList Options
		{
			get
			{
				return options;
			}
		}

		public MirrorApplier(NodeTree model)
		{
			_model = model;
			options = new ArrayList();
			mirrorCommandQueueNode = model.GetNamedNode("MirrorCommandQueue");

			string xmlfile = AppInfo.TheInstance.Location + "\\data\\mirror_defs.xml";
			System.IO.StreamReader file = new System.IO.StreamReader(xmlfile);
			string xmldata = file.ReadToEnd();
			file.Close();
			file = null;

			XmlDocument xdoc = new XmlDocument();
			xdoc.LoadXml(xmldata);

			foreach(XmlNode xnode in xdoc.DocumentElement.ChildNodes)
			{
				if(xnode.NodeType == XmlNodeType.Element)
				{
					if(xnode.Name == "mirror")
					{
						MirrorOption option = new MirrorOption(model,xnode);
						options.Add(option);
					}
				}
			}

			mirrorCommandQueueNode.ChildAdded += mirrorCommandQueueNode_ChildAdded;
		}

		/// <summary>
		/// Dispose ...
		/// </summary>
		public void Dispose()
		{
			mirrorCommandQueueNode.ChildAdded -= mirrorCommandQueueNode_ChildAdded;
		}

		void mirrorCommandQueueNode_ChildAdded(Node sender, Node child)
		{
			// Carry out the instruction posted...
			string target = child.GetAttribute("target");
			Node targetServer = _model.GetNamedNode(target);
			//
			foreach(MirrorOption option in this.options)
			{
				if(option.Target == targetServer)
				{
					//
					string type = child.GetAttribute("type");
					switch(type)
					{
						case "add_mirror":
							option.Apply(child.GetAttribute("location"));
							break;

						case "remove_mirror":
							option.Remove();
							break;

						default:
							throw( new Exception("Unknown Mirror Command " + type));
					}
					//
					child.Parent.DeleteChildTree(child);
					return;
				}
			}
			// Consume this command node.
			child.Parent.DeleteChildTree(child);
		}
	}
}
