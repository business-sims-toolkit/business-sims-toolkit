using System;
using System.Collections;
using System.IO;
using Network;

using Media;

namespace DiscreteSimGUI
{
	/// <summary>
	/// Summary description for Voice Manager
	/// </summary>
	public class VoiceManager 
	{
		public static string VoiceModeBiz = "Biz";
		public static string VoiceModeBatch = "Batch";

		SoundPlayer soundPlayer;
		Hashtable voices = new Hashtable();
		NodeTree MyTreeRootHandle;
		int Stores_Count = 0;
		Node TransactionsNode = null;
		string singlevoicefile = string.Empty;
		string audio_file_prefix = string.Empty;

		public VoiceManager(string file_location)
		{
			audio_file_prefix = file_location;
			soundPlayer = new SoundPlayer();
		}

		public void Dispose()
		{
			if (TransactionsNode != null)
			{
				TransactionsNode.AttributesChanged -=TransactionsNode_AttributesChanged;
			}
			if (voices.Count>0)
			{
				foreach ( Node n in voices.Keys)
				{
					n.AttributesChanged -=storeNode_AttributesChanged;
				}
				voices.Clear();
			}

			soundPlayer.Dispose();
		}

		public void SetVoiceMode(NodeTree nt, string modeName, string voiceFiles)
		{
			MyTreeRootHandle = nt;
			string biz_name = CoreUtils.SkinningDefs.TheInstance.GetData("biz");
			string our_biz_name = CoreUtils.SkinningDefs.TheInstance.GetData("ourbiz");

			//=======================================================================
			//==Are we linking the voices with the individual Stores=================
			//==  need to link to each transition as we tell the store 
			//=======================================================================
			if (modeName.ToLower() == VoiceModeBiz.ToLower())
			{

				string[] voiceFilesParts = voiceFiles.Split(',');

				//Clear out the old voices 
				voices.Clear();
				//Connect up the voices to the stores 
				ArrayList types = new ArrayList();
				types.Add(biz_name);
				Hashtable allStores = MyTreeRootHandle.GetNodesOfAttribTypes(types);

				int step = 0;
				foreach (Node storeNode in allStores.Keys)
				{
					Boolean ourstores_status = storeNode.GetBooleanAttribute(our_biz_name,false);
					if (ourstores_status)					
					{
						//Connect up the event handler 
						storeNode.AttributesChanged +=storeNode_AttributesChanged;
						//Establish the relatioship lookup between the node and the voice file
						voices.Add(storeNode,voiceFilesParts[step]);
						step++;
						if (step > (voiceFilesParts.Length-1))
						{
							step=0;
						}
					}
				}
			}
			//=======================================================================
			//==Are we linking the voices with the Transactions as a batch=========== 
			//==  need to link to the transaction count (only the first of each batch) 
			//=======================================================================
			if (modeName.ToLower() == VoiceModeBatch.ToLower())
			{
				string[] voiceFilesParts = voiceFiles.Split(',');
				singlevoicefile = voiceFilesParts[0];
				//How many stores do we have 
				ArrayList types = new ArrayList();
				types.Add(biz_name);
				Hashtable allStores = MyTreeRootHandle.GetNodesOfAttribTypes(types);
				Stores_Count = 0;
				foreach (Node storeNode in allStores.Keys)
				{
					Boolean ourstores_status = storeNode.GetBooleanAttribute(our_biz_name,false);
					if (ourstores_status)					
					{
						Stores_Count++;
					}
				}
				//Connect to the 
				TransactionsNode = MyTreeRootHandle.GetNamedNode("Transactions");
				TransactionsNode.AttributesChanged +=TransactionsNode_AttributesChanged;
			}
		}

		public void EnableVoiceSystem()
		{
		}

		public void DisableVoiceSystem()
		{
		}
		
		
		public void PlayAudio(string file, bool loop)
		{
			soundPlayer.Play(file,loop);
		}

		void storeNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			if (attrs != null && attrs.Count > 0)
			{
				foreach(AttributeValuePair avp in attrs)
				{
					if (avp.Attribute == "last_transaction")
					{
						if (voices.ContainsKey(sender))
						{
							string voicefile = (string) voices[sender];
							if (File.Exists(audio_file_prefix + voicefile))
							{
								PlayAudio(audio_file_prefix + voicefile, false);
							}
						}
					}
				}
			}
		}

		void TransactionsNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			if (attrs != null && attrs.Count > 0)
			{
				foreach(AttributeValuePair avp in attrs)
				{
					if (avp.Attribute == "count_good")
					{
						int count = TransactionsNode.GetIntAttribute("count_good",0);
						//if ((count > 1) && ((count % Stores_Count)==0))
						if ((count > 1))
						{
							PlayAudio(audio_file_prefix + singlevoicefile, false);
						}
					}
				}
			}
		}
	}
}
