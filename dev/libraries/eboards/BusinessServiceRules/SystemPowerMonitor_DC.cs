using System;
using System.Collections;
using LibCore;
using Network;
using CoreUtils;

namespace BusinessServiceRules
{
	/// <summary>
	/// This is a monitor to determine the use of system power
	/// </summary>
	public class SystemPowerMonitor_DC : SystemPowerMonitorBase, ITimedClass
	{
		//DC Specials 
		protected Node CoolerEffectNode = null;
		protected Node DemandContractLimitNode = null;
		protected Node CostedEventNode = null;
		int[] CoolingDeduction = new int[7];
		bool applyCoolingDeductions = true;
		bool ForceSoftwareScoreRefreshOnStart = true;

		bool refreshOnNextTick = false;

		protected int round;
		protected bool opsPhase;

		//Normal 
		bool NoServerRefresh = false; //Don't refresh the server values (use pre round defaults)

		bool IgnoreUpStatus = false;  //We use power no mattter if UP or Down 

		protected NodeTree _model;
		protected Node _powerLevelNode;
		protected Node timeNode;

		//We handle the foillowing types 
		protected ArrayList KnownPowerStations = new ArrayList ();
		protected ArrayList KnownMegaServers = new ArrayList();			//MegaServer (virtual)
		protected ArrayList KnownServers = new ArrayList();					//Servers
		protected ArrayList KnownSoftware = new ArrayList();				//App and Database
		protected ArrayList KnownSlots = new ArrayList();		//Slot
		protected ArrayList KnownZones = new ArrayList ();
		
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="nt"></param>
		public SystemPowerMonitor_DC(NodeTree nt, int round, bool opsPhase):base(nt)
		{
			this.round = round;
			this.opsPhase = opsPhase;
			TimeManager.TheInstance.ManageClass(this);
			Construct(nt);
		}

		protected void Construct(NodeTree nt)
		{
			_model = nt;

			for (int step =0; step <7; step++)
			{
				CoolingDeduction[step] = 0;
			}

			_powerLevelNode = _model.GetNamedNode("PowerLevel");
			DemandContractLimitNode = _model.GetNamedNode("DemandContractLimit");
			DemandContractLimitNode.AttributesChanged += DemandContractLimitNode_AttributesChanged;
			CostedEventNode = _model.GetNamedNode("CostedEvents");
			CoolerEffectNode = _model.GetNamedNode("CoolerEffect");
			CoolerEffectNode.AttributesChanged +=CoolerEffectNode_AttributesChanged;
			ExtractCoolerScores();

			//We don't refresh the base numbers, we just rely on the network defaults 
			NoServerRefresh = false;
			string NoServerRefresh_str = CoreUtils.SkinningDefs.TheInstance.GetData("power_system_no_server_refresh");
			if (NoServerRefresh_str.ToLower()=="true")
			{
				NoServerRefresh = true;
			}

			//We dont care if the item is up or down, it still drains power
			IgnoreUpStatus = false;
			string IgnoreUpStatus_str = CoreUtils.SkinningDefs.TheInstance.GetData("power_system_ignore_up_status");
			if (IgnoreUpStatus_str.ToLower()=="true")
			{
				IgnoreUpStatus = true;
			}

			timeNode = _model.GetNamedNode("CurrentTime");
			if (timeNode != null)
			{
				timeNode.AttributesChanged += timeNode_AttributesChanged;
			}

			BuildMonitoring(); 
			this.BuildPowerScore();
			//Need to handle Changes to network 
			_model.NodeAdded +=_model_NodeAdded;
		}

		/// <summary>
		/// Dispose all event handlers and anything else
		/// </summary>
		public override void Dispose()
		{
			TimeManager.TheInstance.UnmanageClass(this);

			DisposeMonitoring();

			if (_model != null)
			{
				_model.NodeAdded -=_model_NodeAdded;
			}
			_model = null;

			if (CoolerEffectNode != null)
			{
				CoolerEffectNode.AttributesChanged -=CoolerEffectNode_AttributesChanged;
				CoolerEffectNode = null;
			}
			if (DemandContractLimitNode != null)
			{
				DemandContractLimitNode.AttributesChanged -= DemandContractLimitNode_AttributesChanged;
				DemandContractLimitNode = null;
			}
			if (CostedEventNode != null)
			{
				CostedEventNode = null;
			}
			if (_powerLevelNode != null)
			{
				_powerLevelNode = null;
			}

			if (timeNode != null)
			{
				timeNode.AttributesChanged -= timeNode_AttributesChanged;
				timeNode = null;
			}
			//System.Diagnostics.Debug.WriteLine("SYSTEM POWER MONITOR DISPOSE");
		}

		public void Start()
		{
			//Force the system to update the apps values at the start
			//provides initial data for the power graph to bite on 
			ForceSoftwareScoreRefreshOnStart = true;
			BuildPowerScore();
			ForceSoftwareScoreRefreshOnStart = false;
		}

		public void Stop()
		{
		}

		public void Reset()
		{
		}

		public void FastForward(double timesRealTime)
		{
		}

		#region Helper Methods For PowerStation Monitoring

		public void AddPowerStation (Node tmpPowerNode)
		{
			if (tmpPowerNode != null)
			{
				if (KnownPowerStations.Contains(tmpPowerNode)==false)
				{
					KnownPowerStations.Add(tmpPowerNode);
					tmpPowerNode.AttributesChanged +=PowerStationNode_AttributesChanged;
					tmpPowerNode.Deleting +=PowerStationNode_Deleting;
				}
			}
		}

		public void RemovePowerStation (Node tmpPowerNode)
		{
			if (tmpPowerNode != null)
			{
				if (KnownServers.Contains(tmpPowerNode))
				{
					tmpPowerNode.AttributesChanged -=PowerStationNode_AttributesChanged;
					tmpPowerNode.Deleting -=PowerStationNode_Deleting;
					KnownServers.Remove(tmpPowerNode);
				}
			}
		}
		
		#endregion Helper Methods For PowerStation Monitoring

		#region Helper Methods For Zone Monitoring

		public void AddZone (Node tmpZoneNode)
		{
			if (tmpZoneNode != null)
			{
				if (KnownZones.Contains(tmpZoneNode)==false)
				{
					KnownZones.Add(tmpZoneNode);
					tmpZoneNode.AttributesChanged +=ZoneNode_AttributesChanged;
					tmpZoneNode.Deleting +=ZoneNode_Deleting;
				}
			}
		}

		public void RemoveZone (Node tmpZoneNode)
		{
			if (tmpZoneNode != null)
			{
				if (KnownZones.Contains(tmpZoneNode))
				{
					tmpZoneNode.AttributesChanged -=ZoneNode_AttributesChanged;
					tmpZoneNode.Deleting -=ZoneNode_Deleting;
					KnownZones.Remove(tmpZoneNode);
				}
			}
		}
		
		#endregion Helper Methods For Zone Monitoring

		#region Helper Methods For MegaServers Monitoring

		public void AddMegaServer(Node tmpMegaServerNode)
		{
			if (tmpMegaServerNode != null)
			{
				if (KnownMegaServers.Contains(tmpMegaServerNode)==false)
				{
					//Add to stores 
					string tmpname = tmpMegaServerNode.GetAttribute("name");
					KnownMegaServers.Add(tmpMegaServerNode);
					int zone = tmpMegaServerNode.GetIntAttribute("proczone",0);
					//Connect handlers 
					tmpMegaServerNode.AttributesChanged +=MegaServerNode_AttributesChanged;
					tmpMegaServerNode.Deleting +=MegaServerNode_Deleting;
					//Debug String 
					//System.Diagnostics.Debug.WriteLine("=====Adding MegaServer "+tmpname+" Zone "+zone.ToString());
				}
			}
		}

		public void RemoveMegaServer(Node tmpMegaServerNode)
		{
			if (tmpMegaServerNode != null)
			{
				if (KnownMegaServers.Contains(tmpMegaServerNode))
				{
					//get attriubutes
					string tmpname = tmpMegaServerNode.GetAttribute("name");
					int zone = tmpMegaServerNode.GetIntAttribute("proczone",0);
					//disconnect handlers 
					tmpMegaServerNode.AttributesChanged -=MegaServerNode_AttributesChanged;
					tmpMegaServerNode.Deleting -=MegaServerNode_Deleting;
					//Remove from stores 
					KnownMegaServers.Remove(tmpMegaServerNode);
					//Debug
					//System.Diagnostics.Debug.WriteLine("=====Removed MegaServer "+tmpname+" Zone "+zone.ToString());
				}
			}
		}
		
		#endregion Helper Methods For MegaServers Monitoring

		#region Helper Methods For Servers Monitoring

		public void AddServer(Node tmpServerNode)
		{
			if (tmpServerNode != null)
			{
				if (KnownServers.Contains(tmpServerNode)==false)
				{
					//Add to stores 
					string tmpname = tmpServerNode.GetAttribute("name");
					KnownServers.Add(tmpServerNode);
					int zone = tmpServerNode.GetIntAttribute("proczone",0);
					//Connect handlers 
					tmpServerNode.AttributesChanged +=ServerNode_AttributesChanged;
					tmpServerNode.Deleting +=ServerNode_Deleting;
					//Debug String 
					//System.Diagnostics.Debug.WriteLine("=====Adding Server "+tmpname+" Zone "+zone.ToString());
				}
			}
		}

		public void RemoveServer(Node tmpServerNode)
		{
			if (tmpServerNode != null)
			{
				if (KnownServers.Contains(tmpServerNode))
				{
					//get attriubutes
					string tmpname = tmpServerNode.GetAttribute("name");
					int zone = tmpServerNode.GetIntAttribute("proczone",0);
					//disconnect handlers 
					tmpServerNode.AttributesChanged -=ServerNode_AttributesChanged;
					tmpServerNode.Deleting -=ServerNode_Deleting;
					//Remove from stores 
					KnownServers.Remove(tmpServerNode);
					//Debug
					//System.Diagnostics.Debug.WriteLine("=====Removed Server "+tmpname+" Zone "+zone.ToString());
				}
			}
		}
		
		#endregion Helper Methods For Servers Monitoring
	
		#region Helper Methods For Software Monitoring 

		public void AddSoftware(Node tmpSoftwareNode)
		{
			if (tmpSoftwareNode != null)
			{
				if (KnownSoftware.Contains(tmpSoftwareNode)==false)
				{
					//Add to stores 
					string tmpname = tmpSoftwareNode.GetAttribute("name");
					KnownSoftware.Add(tmpSoftwareNode);
					int zone = tmpSoftwareNode.GetIntAttribute("proczone",0);
					//Connect handlers 
					tmpSoftwareNode.AttributesChanged +=SoftwareNode_AttributesChanged;
					tmpSoftwareNode.Deleting +=SoftwareNode_Deleting;
					//Debug String 
					//System.Diagnostics.Debug.WriteLine("=====Adding Software "+tmpname+" Zone "+zone.ToString());
				}
			}
		}

		public void RemoveSoftware(Node tmpSoftwareNode)
		{
			if (tmpSoftwareNode != null)
			{
				if (KnownSoftware.Contains(tmpSoftwareNode))
				{
					//get attriubutes
					string tmpname = tmpSoftwareNode.GetAttribute("name");
					int zone = tmpSoftwareNode.GetIntAttribute("proczone",0);
					//disconnect handlers 
					tmpSoftwareNode.AttributesChanged -=SoftwareNode_AttributesChanged;
					tmpSoftwareNode.Deleting -=SoftwareNode_Deleting;
					//Remove from stores 
					KnownSoftware.Remove(tmpSoftwareNode);
					//Debug
					//System.Diagnostics.Debug.WriteLine("=====Removed Software "+tmpname+" Zone "+zone.ToString());
				}
			}
		}
		
		#endregion Helper Methods For Software Monitoring
	
		#region Helper Methods For Slots Monitoring

		public void AddSlot(Node tmpSlotNode)
		{
			if (tmpSlotNode != null)
			{
				if (KnownSlots.Contains(tmpSlotNode)==false)
				{
					//Add to stores 
					string tmpname = tmpSlotNode.GetAttribute("name");
					KnownSlots.Add(tmpSlotNode);
					int zone = tmpSlotNode.GetIntAttribute("proczone",0);
					//Connect handlers 
					tmpSlotNode.AttributesChanged +=SlotNode_AttributesChanged;
					tmpSlotNode.Deleting +=SlotNode_Deleting;
					//Debug String 
					//System.Diagnostics.Debug.WriteLine("=====Adding Slot "+tmpname+" Zone "+zone.ToString());
				}
			}
		}

		public void RemoveSlot(Node tmpSlotNode)
		{
			if (tmpSlotNode != null)
			{
				if (KnownSlots.Contains(tmpSlotNode))
				{
					//get attributes
					string tmpname = tmpSlotNode.GetAttribute("name");
					int zone = tmpSlotNode.GetIntAttribute("proczone",0);
					//disconnect handlers 
					tmpSlotNode.AttributesChanged -=SlotNode_AttributesChanged;
					tmpSlotNode.Deleting -=SlotNode_Deleting;
					//Remove from stores 
					KnownSoftware.Remove(tmpSlotNode);
					//Debug
					//System.Diagnostics.Debug.WriteLine("=====Removed Slot "+tmpname+" Zone "+zone.ToString());
				}
			}
		}
		
		#endregion Helper Methods For Slots Monitoring

		#region PowerStation Change Handlers 

		protected void PowerStationNode_AttributesChanged (Node sender, ArrayList attrs)
		{
			Boolean rescore = false;

			foreach(AttributeValuePair avp in attrs)
			{
				if (avp.Attribute == "up")
				{
					rescore = true;
				}
			}
			if (rescore)
			{
				BuildPowerScore();
			}
		}

		protected void PowerStationNode_Deleting(Node sender)
		{
			if (sender != null)
			{	
				RemovePowerStation(sender);
				BuildPowerScore();
			}
		}

		#endregion PowerStation Change Handlers 


		#region Zone Change Handlers 

		protected void ZoneNode_AttributesChanged (Node sender, ArrayList attrs)
		{
			Boolean rescore = false;

			foreach(AttributeValuePair avp in attrs)
			{
				if (avp.Attribute == "activated")
				{
					rescore = true;
				}
			}
			if (rescore)
			{
				BuildPowerScore();
			}
		}

		protected void ZoneNode_Deleting(Node sender)
		{
			if (sender != null)
			{	
				RemoveZone(sender);
				BuildPowerScore();
			}
		}

		#endregion Zone Change Handlers 

		#region MegaServer Change Handlers 

		protected void MegaServerNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			Boolean rescore = false;

			foreach(AttributeValuePair avp in attrs)
			{
				if((avp.Attribute == "up")|(avp.Attribute == "procdown"))
				{
					string Name = sender.GetAttribute("name");
					string Status = sender.GetAttribute("up");
					Boolean procdown = sender.GetBooleanAttribute("procdown",false);
					string full_status = determineStatus(Status, procdown);
					rescore = true;
					//System.Diagnostics.Debug.WriteLine("=====MegaServer Up Changed ("+ Name+") Status:"+Status+" FullStatus:"+full_status);
				}
				if(avp.Attribute == "proccap")
				{
					string Name = sender.GetAttribute("name");
					int procAbility = sender.GetIntAttribute("proccap",0);
					rescore = true;
					//System.Diagnostics.Debug.WriteLine("=====MegaServer proc Changed ("+ Name+")");
				}
			}
			if (rescore)
			{
				BuildPowerScore();
			}
		}

		protected void MegaServerNode_Deleting(Node sender)
		{
			if (sender != null)
			{	
				string name = sender.GetAttribute("name");
				//System.Diagnostics.Debug.WriteLine("Deleting MegaServer Item : "+name);
				RemoveMegaServer(sender);
				BuildPowerScore();
			}
		}

		#endregion MegaServer Change Handlers 

		#region Server Change Handlers 

		protected void ServerNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			Boolean rescore = false;

			foreach(AttributeValuePair avp in attrs)
			{
				if((avp.Attribute == "up")|(avp.Attribute == "procdown"))
				{
					string Name = sender.GetAttribute("name");
					string Status = sender.GetAttribute("up");
					Boolean procdown = sender.GetBooleanAttribute("procdown",false);
					string full_status = determineStatus(Status, procdown);
					rescore = true;
					//System.Diagnostics.Debug.WriteLine("  Server Up Changed ("+ Name+") Status:"+Status+" FullStatus:"+full_status);
				}
				if(avp.Attribute == "proccap")
				{
					string Name = sender.GetAttribute("name");
					int procAbility = sender.GetIntAttribute("proccap",0);
					rescore = true;
					//System.Diagnostics.Debug.WriteLine("  Server proc Changed ("+ Name+")");
				}
				else if (avp.Attribute == "turnedoff")
				{
					// Turning servers on may fix incidents...
					if (avp.Value == "false")
					{
						Node FixQueue = sender.Tree.GetNamedNode("fixItQueue");

						ArrayList fixAttrs = new ArrayList ();
						fixAttrs.Add(new AttributeValuePair ("incident_id", sender.GetAttribute("name") + "_turn_off"));
						Node fixEvent = new Node (FixQueue, "fix", "", fixAttrs);
					}
				}
			}
			if (rescore)
			{
				BuildPowerScore();
			}
		}

		protected void ServerNode_Deleting(Node sender)
		{
			if (sender != null)
			{	
				string name = sender.GetAttribute("name");
				//System.Diagnostics.Debug.WriteLine("Deleting Server Item : "+name);
				RemoveMegaServer(sender);
				BuildPowerScore();
			}
		}

		#endregion Server Change Handlers 

		#region Software Change Handlers 

		protected void SoftwareNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			Boolean rescore = false;

			foreach(AttributeValuePair avp in attrs)
			{
				if((avp.Attribute == "up")|(avp.Attribute == "procdown"))
				{
					string Name = sender.GetAttribute("name");
					string Status = sender.GetAttribute("up");
					Boolean procdown = sender.GetBooleanAttribute("procdown",false);
					string full_status = determineStatus(Status, procdown);
					rescore = true;
					//System.Diagnostics.Debug.WriteLine("  Software Up Changed ("+ Name+") Status:"+Status+" FullStatus:"+full_status);
				}
				if(avp.Attribute == "proccap")
				{
					string Name = sender.GetAttribute("name");
					int procAbility = sender.GetIntAttribute("proccap",0);
					rescore = true;
					//System.Diagnostics.Debug.WriteLine("  Software proc Changed ("+ Name+")");
				}
				if(avp.Attribute == "name")
				{
					string Name = sender.GetAttribute("name");
					//System.Diagnostics.Debug.WriteLine("  Software proc Changed ("+ Name+")  NAME XXXX");
				}
			}
			if (rescore)
			{
				BuildPowerScore();
			}
		}

		protected void SoftwareNode_Deleting(Node sender)
		{
			if (sender != null)
			{	
				string name = sender.GetAttribute("name");
				//System.Diagnostics.Debug.WriteLine("Deleting Software Item : "+name);
				RemoveSoftware(sender);
				BuildPowerScore();
			}
		}

		#endregion Server Change Handlers 

		#region Slot Change Handlers 

		protected void SlotNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			Boolean rescore = false;
			Boolean shift_type = false;
			string shift_newtype = "";

			foreach(AttributeValuePair avp in attrs)
			{
				if((avp.Attribute == "up")|(avp.Attribute == "procdown"))
				{
					string Name = sender.GetAttribute("name");
					string Status = sender.GetAttribute("up");
					Boolean procdown = sender.GetBooleanAttribute("procdown",false);
					string full_status = determineStatus(Status, procdown);
					rescore = true;
					//System.Diagnostics.Debug.WriteLine("  Slot Up Changed ("+ Name+") Status:"+Status+" FullStatus:"+full_status);
				}
				if(avp.Attribute == "proccap")
				{
					string Name = sender.GetAttribute("name");
					int procAbility = sender.GetIntAttribute("proccap",0);
					rescore = true;
					//System.Diagnostics.Debug.WriteLine("  Slot proc Changed ("+ Name+")");
				}
				if(avp.Attribute == "name")
				{
					string Name = sender.GetAttribute("name");
					//System.Diagnostics.Debug.WriteLine("  Slot name Changed ("+ Name+")");
				}
				if(avp.Attribute == "type")
				{
					shift_type =true;
					shift_newtype = avp.Value;
					//System.Diagnostics.Debug.WriteLine("  Slot type Changed ("+ shift_newtype+")");
				}
			}
			if (shift_type)
			{
				//need to remove from Slot and add to new 
				this.RemoveSlot(sender);
				//need to add to the new slot 
				rescore = HandleNewNode(shift_newtype, sender);
			}
			if (rescore)
			{
				BuildPowerScore();
			}
		}

		protected void SlotNode_Deleting(Node sender)
		{
			if (sender != null)
			{	
				string name = sender.GetAttribute("name");
				//System.Diagnostics.Debug.WriteLine("Deleting Software Item : "+name);
				RemoveSlot(sender);
				BuildPowerScore();
			}
		}

		#endregion Server Change Handlers 

		#region Overall Monitoring 

		public void DisposeMonitoring()
		{
			ArrayList KillList = new ArrayList();
			foreach (Node tn in KnownPowerStations)
			{
				KillList.Add(tn);
			}
			foreach (Node tn in KillList)
			{
				this.RemovePowerStation(tn);
			}

			KillList = new ArrayList();
			foreach (Node tn in KnownMegaServers)
			{
				KillList.Add(tn);
			}
			foreach (Node tn in KillList)
			{
				this.RemoveMegaServer(tn);
			}

			KillList = new ArrayList();
			foreach (Node tn in KnownServers)
			{
				KillList.Add(tn);
			}
			foreach (Node tn in KillList)
			{
				this.RemoveServer(tn);
			}

			KillList = new ArrayList();
			foreach (Node tn in KnownSoftware)
			{
				KillList.Add(tn);
			}
			foreach (Node tn in KillList)
			{
				this.RemoveSoftware(tn);
			}

			KillList = new ArrayList();
			foreach (Node tn in KnownSlots)
			{
				KillList.Add(tn);
			}
			foreach (Node tn in KillList)
			{
				this.RemoveSlot(tn);
			}

			KillList = new ArrayList ();
			foreach (Node tn in KnownZones)
			{
				KillList.Add(tn);
			}
			foreach (Node tn in KillList)
			{
				this.RemoveZone(tn);
			}
		}

		public void BuildMonitoring()
		{
			//System.Diagnostics.Debug.WriteLine("Build Monitor");
			ArrayList types = new ArrayList();
			Hashtable ht = new Hashtable();

			// First the power stations.
			types.Clear();
			types.Add("powerstation");
			ht = _model.GetNodesOfAttribTypes(types);
			foreach (Node tmpNode in ht.Keys)
			{
				string namestr = tmpNode.GetAttribute("name");
				AddPowerStation(tmpNode);
			}

			//handling the MegaServers 
			types.Clear();
			types.Add("MegaServer");
			ht = _model.GetNodesOfAttribTypes(types);
			foreach(Node tmpNode in ht.Keys)
			{
				string namestr = tmpNode.GetAttribute("name");
				//System.Diagnostics.Debug.WriteLine("Mega Server Discovered: "+namestr);
				AddMegaServer(tmpNode);
			}

			//handling the Servers 
			types.Clear();
			types.Add("Server");
			ht = _model.GetNodesOfAttribTypes(types);
			foreach(Node tmpNode in ht.Keys)
			{
				string namestr = tmpNode.GetAttribute("name");
				//System.Diagnostics.Debug.WriteLine("Server Discovered: "+namestr);
				AddServer(tmpNode);
			}

			//handling the Apps
			types.Clear();
			types.Add("App");
			ht = _model.GetNodesOfAttribTypes(types);
			foreach(Node tmpNode in ht.Keys)
			{
				string namestr = tmpNode.GetAttribute("name");
				//System.Diagnostics.Debug.WriteLine("App Discovered: "+namestr);
				AddSoftware(tmpNode);
			}

			//handling the Databases
			types.Clear();
			types.Add("Database");
			ht = _model.GetNodesOfAttribTypes(types);
			foreach(Node tmpNode in ht.Keys)
			{
				string namestr = tmpNode.GetAttribute("name");
				//System.Diagnostics.Debug.WriteLine("DB Discovered: "+namestr);
				AddSoftware(tmpNode);
			}

			//handling the Slots
			types.Clear();
			types.Add("Slot");
			ht = _model.GetNodesOfAttribTypes(types);
			foreach(Node tmpNode in ht.Keys)
			{
				string namestr = tmpNode.GetAttribute("name");
				//System.Diagnostics.Debug.WriteLine("Slot Discovered: "+namestr);
				AddSlot(tmpNode);
			}

			types.Clear();
			types.Add("zone");
			ht = _model.GetNodesOfAttribTypes(types);
			foreach (Node tmpNode in ht.Keys)
			{
				AddZone(tmpNode);
			}
		}

		bool HandleNewNode(string newNodeType, Node newNode)
		{
			bool refreshscores = false;

			switch (newNodeType.ToLower())
			{
				case "app": 
					this.AddSoftware(newNode);
					refreshscores = true;
					break;
				case "database": 
					this.AddSoftware(newNode);
					refreshscores = true;
					break;
				case "server": 
					this.AddServer(newNode);
					refreshscores = true;
					break;
				case "megaserver": 
					this.AddMegaServer(newNode);
					refreshscores = true;
					break;
				case "powerstation":
					this.AddPowerStation(newNode);
					refreshscores = true;
					break;
				case "zone":
					this.AddZone(newNode);
					refreshscores = true;
					break;
			}
			return refreshscores;
		}

		void _model_NodeAdded(NodeTree sender, Node newNode)
		{
			string newNodeName = newNode.GetAttribute("name");
			string newNodeType = newNode.GetAttribute("type");
			//System.Diagnostics.Debug.WriteLine("New Node in Tree Discovered: " + newNodeName + " type "+newNodeType);
			if (newNodeType != null)
			{
				//handle thye node and refresh if needed
				if (HandleNewNode(newNodeType.ToLower(), newNode))
				{
					this.BuildPowerScore();
				}
			}
		}

		#endregion Overall Monitoring 

		#region Cooling System 

		/// <summary>
		/// 
		/// </summary>
		/// <returns>Whether we are changed and need to rebuild scores</returns>
		protected bool ExtractCoolerScores()
		{
			bool refreshcalcs = false;

			if (CoolerEffectNode != null)
			{
				//Extract the Cooler Deduction rate 
				int coolingDeductionZone1 = this.CoolerEffectNode.GetIntAttribute("z1_rate",0);
				int coolingDeductionZone2 = this.CoolerEffectNode.GetIntAttribute("z2_rate",0);
				int coolingDeductionZone3 = this.CoolerEffectNode.GetIntAttribute("z3_rate",0);
				int coolingDeductionZone4 = this.CoolerEffectNode.GetIntAttribute("z4_rate",0);
				int coolingDeductionZone5 = this.CoolerEffectNode.GetIntAttribute("z5_rate",0);
				int coolingDeductionZone6 = this.CoolerEffectNode.GetIntAttribute("z6_rate",0);
				int coolingDeductionZone7 = this.CoolerEffectNode.GetIntAttribute("z7_rate",0);

				if (CoolingDeduction[0] != coolingDeductionZone1)
				{
					CoolingDeduction[0] = coolingDeductionZone1;
					refreshcalcs = true;
				}
				if (CoolingDeduction[1] != coolingDeductionZone2)
				{
					CoolingDeduction[1] = coolingDeductionZone2;
					refreshcalcs = true;
				}
				if (CoolingDeduction[2] != coolingDeductionZone3)
				{
					CoolingDeduction[2] = coolingDeductionZone3;
					refreshcalcs = true;
				}
				if (CoolingDeduction[3] != coolingDeductionZone4)
				{
					CoolingDeduction[3] = coolingDeductionZone4;
					refreshcalcs = true;
				}
				if (CoolingDeduction[4] != coolingDeductionZone5)
				{
					CoolingDeduction[4] = coolingDeductionZone5;
					refreshcalcs = true;
				}
				if (CoolingDeduction[5] != coolingDeductionZone6)
				{
					CoolingDeduction[5] = coolingDeductionZone6;
					refreshcalcs = true;
				}
				if (CoolingDeduction[6] != coolingDeductionZone7)
				{
					CoolingDeduction[6] = coolingDeductionZone7;
					refreshcalcs = true;
				}
			}
			return refreshcalcs;
		}

		protected void CoolerEffectNode_AttributesChanged(Node sender, ArrayList attrs)
		{	
			if (ExtractCoolerScores())
			{
				BuildPowerScore();
			}
		}

		#endregion Cooling System 

		#region Utils  

		protected string determineStatus(string status, Boolean procdown)
		{
			string outcome="false";
			if (procdown==false)
			{
				if ((status.ToLower()=="true")|((status=="")))
				{
					outcome="true";
				}
			}	
			return outcome;
		}
		
		#endregion Utils  

		#region Power Caculations and Export 

		protected bool ServerIsPowered (Node server)
		{
			Node zoneNode = _model.GetNamedNode("Zone" + server.GetAttribute("proczone"));
			if ((zoneNode == null) || (zoneNode.GetAttribute("activated") == "false"))
			{
				return false;
			}

			if (server.GetAttribute("turnedoff").ToLower() == "true")
			{
				return false;
			}

			if (server.GetAttribute("nopower").ToLower() == "true")
			{
				return false;
			}

			foreach (Node powerStation in KnownPowerStations)
			{
				if (server.HasLinkTo(powerStation))
				{
					if (powerStation.GetBooleanAttribute("up", true))
					{
						return true;
					}
				}
			}

			return false;
		}

		/// <summary>
		/// Return false only if the given server is turned off: return true if
		/// its zone has lost power through a failure.
		/// </summary>
		protected bool ServerWantsPower (Node server)
		{
			Node zoneNode = _model.GetNamedNode("Zone" + server.GetAttribute("proczone"));
			if ((zoneNode == null) || (zoneNode.GetAttribute("activated") == "false"))
			{
				return false;
			}

			if (server.GetAttribute("turnedoff").ToLower() == "true")
			{
				return false;
			}

			return true;
		}

		/// <summary>
		/// Build the Power Score for All the zones 
		/// using the cached values 
		/// </summary>
		protected void BuildPowerScore()
		{
			int[] TotalServerPower = new int[7];
			int[] TotalDesiredPower = new int[7];
			int[] TotalAppPower = new int[7];
			int[] TotalDatabasePower = new int[7];
			string[] DebugServerStr = new string[7];
			string[] DebugAppStr = new string[7];
			int zone = 1;
			int proccap =0;

			for (int step=0; step < 7; step++)
			{
				TotalServerPower[step]=0;
				TotalAppPower[step]=0;
				TotalDatabasePower[step]=0;
				DebugServerStr = new string[7];
				DebugAppStr = new string[7];
			}

			Hashtable powerUsedByServer = new Hashtable ();

			//We only count proper Servers for Power 
			if (KnownServers.Count>0)
			{
				foreach (Node ServerNode in KnownServers)
				{
					string Name = ServerNode.GetAttribute("name");
					string status = ServerNode.GetAttribute("up");
					proccap = ServerNode.GetIntAttribute("proccap",0);
					
					Boolean procdown = ServerNode.GetBooleanAttribute("procdown",false);
					string full_status = determineStatus(status, procdown);
					zone =  ServerNode.GetIntAttribute("proczone",0);
					zone = zone - 1;

					bool process = false;
					if (IgnoreUpStatus==false)
					{
						if ((status.ToLower()== "true")||(status.ToLower()== ""))
						{
							process = true;
						}
					}
					else
					{
						process = true;
					}

					if (! ServerIsPowered(ServerNode))
					{
						process = false;
					}

					if (process==true)
					{
						TotalServerPower[zone] += proccap;
						DebugServerStr[zone] += " " + Name + "["+ proccap.ToString() + "]";
					}

					powerUsedByServer.Add(ServerNode, 0);
				}
			}

			if (KnownSoftware.Count>0)
			{
				foreach (Node swNode in KnownSoftware)
				{
					string Name = swNode.GetAttribute("name");
					string type = swNode.GetAttribute("type");
					string status = swNode.GetAttribute("up");
					proccap = swNode.GetIntAttribute("proccap",0);

					Node server = swNode.Parent;
					if (server.GetAttribute("type").ToLower() == "server")
					{
						powerUsedByServer[server] = proccap + (int) powerUsedByServer[server];
					}

					Boolean procdown = swNode.GetBooleanAttribute("procdown",false);
					string full_status = determineStatus(status, procdown);
					zone =  swNode.GetIntAttribute("proczone",0);
					zone = zone - 1;

					Boolean ConsiderThisNode = false;
					if ((type.ToLower()=="app")|(type.ToLower()=="database"))
					{
						ConsiderThisNode = true;
					}

					if (ConsiderThisNode)
					{
						bool process = false;
						if (IgnoreUpStatus==false)
						{
							if ((status.ToLower()== "true")||(status.ToLower()== ""))
							{
								process = true;
							}
						}
						else
						{
							process = true;
						}

						// Is our server turned off?
						Node serverNode = swNode.Parent;
						if (! ServerIsPowered(serverNode))
						{
							process = false;
						}

						if (ServerWantsPower(serverNode))
						{
							TotalDesiredPower[zone] += proccap;
						}

						if (process)
						{
							if (type.ToLower()=="app")
							{
								TotalAppPower[zone] += proccap;
								DebugAppStr[zone] += " " + Name + "["+ proccap.ToString() + "]";
							}
							if (type.ToLower()=="database")
							{
								TotalDatabasePower[zone] += proccap;
								DebugAppStr[zone] += " " + Name + "["+ proccap.ToString() + "]";
							}
						}
					}
				}
			}

			// Check if any server is in overload.
			foreach (Node server in powerUsedByServer.Keys)
			{
				if (! server.GetBooleanAttribute("turnedoff", false))
				{
					int used = (int) powerUsedByServer[server];
					server.SetAttribute("power_used", used);

					int max = server.GetIntAttribute("maxpower", 0);
					if ((max > 0) && (used > max))
					{
						ArrayList attributes = new ArrayList ();
//						attributes.Add(new AttributeValuePair ("turnedoff", "true"));
//						attributes.Add(new AttributeValuePair ("nopower", "true"));
						attributes.Add(new AttributeValuePair("power_overload", "true"));
//						attributes.Add(new AttributeValuePair("power_tripped", "true"));
//						attributes.Add(new AttributeValuePair ("up", "false"));
//						attributes.Add(new AttributeValuePair ("incident_id", server.GetAttribute("name") + "_power_overload"));
//						attributes.Add(new AttributeValuePair ("penalised", "false"));
//						attributes.Add(new AttributeValuePair ("powering_down", "false"));
						server.SetAttributes(attributes);
					}
					else
					{
						if (server.GetBooleanAttribute("power_overload", false))
						{
							server.SetAttribute("power_overload", false);
						}
					}
				}
			}

			//Mega Servers are Virtual Servers and they are involved in the power drain in a Zone
			//WE don't care whether they are up or down 
			//The propcap for Virtual Server is Negative or Zero 
			if (this.KnownMegaServers.Count >0)
			{
				foreach (Node vsNode in this.KnownMegaServers)
				{
					string Name = vsNode.GetAttribute("name");
					string type = vsNode.GetAttribute("type");
					proccap = vsNode.GetIntAttribute("proccap",0);
					zone =  vsNode.GetIntAttribute("proczone",0);
					zone = zone - 1;

					if (type.ToLower()=="megaserver")
					{
						TotalAppPower[zone] += proccap;
						DebugAppStr[zone] += " " + Name + "["+ proccap.ToString() + "]";
					}
				}
			}

			for (int step = 0; step <7; step++)
			{
				//System.Diagnostics.Debug.WriteLine("Zone "+step.ToString()+" : "+ (TotalServerPower[step]).ToString()+"   " + DebugServerStr[step]);
				int currentpowerNeeds = TotalAppPower[step] + TotalDatabasePower[step];
				int currentPowerRequest = TotalDesiredPower[step];

				if (applyCoolingDeductions)
				{
					currentpowerNeeds = currentpowerNeeds - CoolingDeduction[step];
					currentPowerRequest -= CoolingDeduction[step];
				}
				
				//System.Diagnostics.Debug.WriteLine("SPM_DC  Zone "+step.ToString()+" : " + currentpowerNeeds.ToString() + "   "+ DebugAppStr[step]);
				UpdatePowerFactorForAppDBs(step, currentpowerNeeds, currentPowerRequest);

				int currentpowerSupply = TotalServerPower[step];
				UpdatePowerFactorForServers(step, currentpowerSupply);
			}
		}

		protected void UpdatePowerFactorForAppDBs(int zone, int powerFactor, int powerDesiredFactor)
		{
			if (_powerLevelNode != null)
			{
				//Build the tag name 
				string tag_name = "z"+CONVERT.ToStr(zone+1)+"_now";
				//Extract the previous value for this item 
				string prev_power = _powerLevelNode.GetAttribute(tag_name);
				//Construct the new Power 
				string new_power = CONVERT.ToStr(Math.Max(0, powerFactor));

				ArrayList attributes = new ArrayList ();
				attributes.Add(new AttributeValuePair (tag_name, new_power));
				attributes.Add(new AttributeValuePair ("z" + CONVERT.ToStr(zone + 1) + "_desired", powerDesiredFactor));
				_powerLevelNode.SetAttributes(attributes);
			
				//if(ForceSoftwareScoreRefreshOnStart)
				//{System.Diagnostics.Debug.WriteLine("SPM_DC "+tag_name+ "  " + new_power + "  FORCE ");}
				//else
				//{System.Diagnostics.Debug.WriteLine("SPM_DC "+tag_name+ "  " + new_power + "  NO FORCE  ");}

				//Build the contract tag name 
				string contract_tag_name = "z"+CONVERT.ToStr(zone+1)+"_limit";
				//Extract the contract value 
			
				if (DemandContractLimitNode != null)
				{
					int contract_level = DemandContractLimitNode.GetIntAttribute(contract_tag_name,0);
					if (powerFactor > contract_level)
					{
						string zoneName = CONVERT.ToStr(zone + 1);
						string reason = "Zone " + zoneName + " Power Breach";

						// : Fix for 4709 (should only get fined once per zone per round).
						// Check that this round hasn't already been fined.
						Node powerNode = _model.GetNamedNode("P" + zoneName);
						ArrayList nodes = CostedEventNode.Tree.GetNodesWithAttributeValue("zone", zoneName);
						bool alreadyFined = powerNode.GetBooleanAttribute("fined_in_round_" + CONVERT.ToStr(round), false);
						if ((! alreadyFined) && opsPhase)
						{
							// Don't apply fines until time has started (bug 4918).
							Node timeNode = _model.GetNamedNode("CurrentTime");
							int seconds = 0;
							if (timeNode != null)
							{
								seconds = timeNode.GetIntAttribute("seconds", 0);
							}

							if (seconds > 0)
							{
								ArrayList attrs = new ArrayList();
								attrs.Add( new AttributeValuePair("zone",  zoneName));
								attrs.Add( new AttributeValuePair("type","power_breach") );
								attrs.Add( new AttributeValuePair("desc",reason));
								attrs.Add( new AttributeValuePair("ref","power monitor"));
								Node newCost = new Node(CostedEventNode, "power_breach", "", attrs);

								powerNode.SetAttribute("fined_in_round_"+ CONVERT.ToStr(round), "true");
							}
							else
							{
								refreshOnNextTick = true;
							}
						}
					}
				}
			}
		}

		protected  void UpdatePowerFactorForServers(int zone, int powerFactor)
		{
			if (this.NoServerRefresh == false)
			{
				if (_powerLevelNode != null)
				{
					string tag_name = "z"+CONVERT.ToStr(zone+1)+"_base";
					string tag_power = CONVERT.ToStr(powerFactor);
					//System.Diagnostics.Debug.WriteLine(" ## "+tag_name + "  "+ tag_power);
					_powerLevelNode.SetAttribute(tag_name,tag_power);
				}
			}
		}

		void timeNode_AttributesChanged (Node sender, ArrayList attrs)
		{
			foreach (AttributeValuePair avp in attrs)
			{
				if (avp.Attribute.ToLower() == "seconds")
				{
					if (refreshOnNextTick)
					{
						bool oldForce = ForceSoftwareScoreRefreshOnStart;
						ForceSoftwareScoreRefreshOnStart = true;
						BuildPowerScore();
						ForceSoftwareScoreRefreshOnStart = oldForce;
						refreshOnNextTick = false;
					}
				}
			}
		}

		#endregion Power Caculations and Export

		void DemandContractLimitNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			bool oldForce = ForceSoftwareScoreRefreshOnStart;
			ForceSoftwareScoreRefreshOnStart = true;
			BuildPowerScore();
			ForceSoftwareScoreRefreshOnStart = oldForce;
			refreshOnNextTick = true;
		}
	}
}