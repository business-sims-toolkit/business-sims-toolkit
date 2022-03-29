using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;

using CommonGUI;
using Network;
using LibCore;
using CoreUtils;

namespace Polestar_PM.OpsEngine
{
	public class AlarmHandler
	{
		protected NodeTree MyNodeTree = null;
		protected Node sound_alarm_node = null;  //Output node for messages

		public AlarmHandler(NodeTree tree)
		{
			//Build the nodes 
			MyNodeTree = tree;
			sound_alarm_node = MyNodeTree.GetNamedNode("sound_alarm");
			sound_alarm_node.AttributesChanged += new Node.AttributesChangedEventHandler(sound_alarm_node_AttributesChanged);
		}

		public void Dispose()
		{
			sound_alarm_node.AttributesChanged -= new Node.AttributesChangedEventHandler(sound_alarm_node_AttributesChanged);
			sound_alarm_node = null;
			MyNodeTree = null;
		}

		private void sound_alarm_node_AttributesChanged(Node sender, ArrayList attrs)
		{
			Boolean soundNeeded = false;

			if (attrs != null)
			{
				if (attrs.Count > 0)
				{
					foreach (AttributeValuePair avp in attrs)
					{
						string attribute_name = avp.Attribute;
						if (attribute_name == "emit_sound_count")
						{
							soundNeeded = true;
						}
					}
				}
			}
			if (soundNeeded)
			{
				string sound_file_name = sound_alarm_node.GetAttribute("sound_name");
				string file = AppInfo.TheInstance.Location + "\\audio\\" + sound_file_name;
				KlaxonSingleton.TheInstance.PlayAudio(file, false);
			}
		}


	}
}
