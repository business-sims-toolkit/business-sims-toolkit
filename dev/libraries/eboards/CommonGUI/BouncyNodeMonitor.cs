using System;
using System.Collections;
using System.Drawing;
using LibCore;
using Network;

using CoreUtils;
using IncidentManagement;

namespace CommonGUI
{
	/// <summary>
	/// A BouncyNodeMonitor has a very silly name.
	/// It provides a dynamic display of the status of a Monitored Node 
	/// Monitored Nodes are either Servers or Applications 
	/// We need to know if a monitored node is functionally up 
	///   It and all it's parents are working 
	/// This is provided by the AWT Watch Node class 
	/// So we created a AWT Watch Node for each of the monitored items 
	/// 
	/// The mapping codes to colours are 
	/// 
	/// 44 -- Static Red
	/// 43 -- Flicker Red to Orange 
	/// 42 -- Flicker Red to Yellow
	/// 41 -- Flicker Red to Green
	/// 40 -- Flicker Red to Green
	/// 33 -- Static Orange
	/// 32 -- Flicker Orange to Yellow
	/// 31 -- Flicker Orange to Green
	/// 30 -- Flicker Orange to Green
	/// 22 -- Static Yellow
	/// 21 -- Flicker Yellow to Green
	/// 20 -- Flicker Yellow to Green
	/// 11 -- Static Green (upper Green)
	/// 10 -- Flicker Green to Green
	/// 0 -- Static Green (lower Green)
	/// -1 -- Totally Empty Grey
	/// </summary>
	public class BouncyNodeMonitor : AudioStyleMonitorBar, ITimedClass
	{
		protected Random random;
		protected StopControlledTimer timer;
		protected AWT_WatchNode watcher;

		protected int ticksToBigJump;
		protected Node monitoredEntity;
		protected bool goingDown = false;
		protected bool up = true;
		protected bool workaround = false;

		protected int danger_level;			//The Requested values
		protected int danger_level_min;	//Min display value 
		protected int danger_level_max;	//Max display value 
		protected Boolean RisingValue = false;
		protected bool ignoreInvisibleNodes = false;

		protected Boolean AWT_Failure_Override = false; //The overall AWT system is down

		#region Constructor and Dispose 

		public BouncyNodeMonitor(Node _monitoredEntity, Random r) : base(5)
		{
			ignoreInvisibleNodes = SkinningDefs.TheInstance.GetBoolData("awt_ignore_invisible_nodes", false);


			monitoredEntity = _monitoredEntity;
			random = r;

			SetLevelColor(0, SkinningDefs.TheInstance.GetColorDataGivenDefault("awt_colour_ok", Color.Green));
            SetLevelColor(1, SkinningDefs.TheInstance.GetColorDataGivenDefault("awt_colour_ok", Color.Green));
            SetLevelColor(2, SkinningDefs.TheInstance.GetColorDataGivenDefault("awt_colour_warning", Color.Yellow));
            SetLevelColor(3, SkinningDefs.TheInstance.GetColorDataGivenDefault("awt_colour_danger", Color.Orange));
            SetLevelColor(4, SkinningDefs.TheInstance.GetColorDataGivenDefault("awt_colour_fault", Color.Red));

			// Pick a random time until the next big jump...
			ticksToBigJump = random.Next(8);

			if(null != monitoredEntity)
			{
				timer = new StopControlledTimer();
				timer.Interval = 250;
				timer.Tick += timer_Tick;
				
				danger_level = monitoredEntity.GetIntAttribute("danger_level",0);
				danger_level_min = danger_level % 10;
				danger_level_max = danger_level / 10;

				string name = monitoredEntity.GetAttribute("name");
				//System.Diagnostics.Debug.WriteLine("name "+name+"  STATIC danger_level"+danger_level.ToString());

				watcher = new AWT_WatchNode(monitoredEntity);
				monitoredEntity.AttributesChanged += monitoredEntity_AttributesChanged;
				monitoredEntity.Deleting += monitoredEntity_Deleting;
			}
			else
			{
				// Set them all to grey...
				min_level = -1;
				watcher = null;
			}

			CurrentLevel = -1;

			TimeManager.TheInstance.ManageClass(this);
		}


		protected override void Dispose(bool disposing)
		{
			if(disposing)
			{
				TimeManager.TheInstance.UnmanageClass(this);

				if (watcher != null)
				{
					watcher.Dispose();
				}

				if(null != monitoredEntity)
				{
					monitoredEntity.AttributesChanged -= monitoredEntity_AttributesChanged;
					monitoredEntity.Deleting -= monitoredEntity_Deleting;
				}
			}

			base.Dispose (disposing);
		}

		#endregion Constructor and Dispose 

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void timer_Tick(object sender, EventArgs e)
		{
			//Handle the Different Situations

			//General AWT System failure -- No Display [All Grey]
			if (AWT_Failure_Override==true)
			{
				CurrentLevel = -1;
				return;
			}

			if (ignoreInvisibleNodes)
			{
				if (watcher != null)
				{
					if (false == watcher.isVisible())
					{
						CurrentLevel = -1;
						return;
					}
				}
			}

			//WorkAround is a Static Orange 
			if (workaround==true)
			{	
				//string name = monitoredEntity.GetAttribute("name");
				//System.Diagnostics.Debug.WriteLine("name "+name+" AWT failure Override ");
				CurrentLevel = 3;
				return;
			}

			//General Required Node Failure -- Display Fail [RED]
			if (watcher != null)
			{
				if (false == watcher.isUP())
				{
					//string name = monitoredEntity.GetAttribute("name");
					//System.Diagnostics.Debug.WriteLine("name "+name+" SupportStatus False");
					CurrentLevel = 4;
					return;
				}
			}

			//This node is Down -- Display Fail [RED]
			if(up == false)
			{
				//string name = monitoredEntity.GetAttribute("name");
				//System.Diagnostics.Debug.WriteLine("name "+name+"  NOT UP danger_level"+danger_level.ToString());
				CurrentLevel = 4;
				return;
			}

			//This node is Down -- Display Warning [ORANGE]
			if(goingDown == true)
			{
				string name = monitoredEntity.GetAttribute("name");
				//System.Diagnostics.Debug.WriteLine("name "+name+"  GDOWN danger_level"+danger_level.ToString());
				CurrentLevel = 3;
				return;
			}

			//Handling the requested Levels 
			if ((danger_level==44)|(danger_level==33)|(danger_level==22)|(danger_level==11)|(danger_level==0))
			{
				//Danger level requesting static level 
				string name = monitoredEntity.GetAttribute("name");
				//System.Diagnostics.Debug.WriteLine("name "+name+"  danger_level static"+danger_level.ToString());
				CurrentLevel = danger_level_max;
			}
			else
			{
				//Danger level allowing flucation between values 
				//we go for smooth rise and fails but we vary the time until next jump 
				--ticksToBigJump;
				if (ticksToBigJump<0)
				{
					ticksToBigJump = random.Next(3);
					if (RisingValue)
					{
						CurrentLevel++;
						if (CurrentLevel > danger_level_max)
						{
							CurrentLevel = danger_level_max;
							RisingValue = false;
						}
					}
					else
					{
						CurrentLevel--;
						if (CurrentLevel < danger_level_min)
						{
							CurrentLevel = danger_level_min;
							RisingValue = true;
						}
					}
				}
			}

//			--ticksToBigJump;
//			if(ticksToBigJump <= 0)
//			{
//				CurrentLevel = CurrentLevel + random.Next(6)-3;
//				ticksToBigJump = random.Next(8);
//			}
//			else
//			{
//				CurrentLevel = CurrentLevel + random.Next(2)-1;
//			}

		}
		#region ITimedClass Members

		public void Start()
		{
			if(null != monitoredEntity)
			{
				timer.Start();
			}
		}

		public void FastForward(double timesRealTime)
		{
			// TODO:  Add BouncyNodeMonitor.FastForward implementation
		}

		public void Reset()
		{
			// TODO:  Add BouncyNodeMonitor.Reset implementation
		}

		public void Stop()
		{
			if(null != monitoredEntity)
			{
				timer.Stop();
			}
		}

		#endregion

		public bool isMonitoredNodeVisible()
		{
			bool vis = false;
			if (null != monitoredEntity)
			{
				vis = monitoredEntity.GetBooleanAttribute("visible", false);
			}
			return vis;
		}

		public void SetDisplayColors(Color newEmptyColor)
		{
			SetEmptyColor(newEmptyColor);
		}

		public void SetOverrideFailure(Boolean Override)
		{
			AWT_Failure_Override = Override;
			//Refresh();
			Invalidate();
		}

		#region Handling Monitored Node Changes Methods 

		/// <summary>
		/// If we need to disconnect the monitor 
		/// IBM Cloud -- When something is renamed 
		/// </summary>
		public void forceDisconnect()
		{
			if (null != timer)
			{
				if (null != watcher)
				{
					watcher.Dispose();
				}

				if (null != monitoredEntity)
				{
					monitoredEntity.AttributesChanged -= monitoredEntity_AttributesChanged;
					monitoredEntity.Deleting -= monitoredEntity_Deleting;
				}
				timer.Dispose();
				timer = null;
				monitoredEntity = null;
				min_level = -1;
				CurrentLevel = -1;
			}
		}

		void monitoredEntity_Deleting(Node sender)
		{
			if(null != timer)
			{
				if(null != watcher)
				{
					watcher.Dispose();
				}

				if(null != monitoredEntity)
				{
					monitoredEntity.AttributesChanged -= monitoredEntity_AttributesChanged;
					monitoredEntity.Deleting -= monitoredEntity_Deleting;
				}
				timer.Dispose();
				timer = null;
				monitoredEntity = null;
				min_level = -1;
				CurrentLevel = -1;
			}
		}

		/// <summary>
		/// We just store the changes with no immediate refesh
		/// the next timer tick will update the display 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="attrs"></param>
		void monitoredEntity_AttributesChanged(Node sender, ArrayList attrs)
		{
			bool changing_visiblity = false;

			foreach(AttributeValuePair avp in attrs)
			{
				if(avp.Attribute == "danger_level")
				{
					danger_level = monitoredEntity.GetIntAttribute("danger_level",0);
					danger_level_min = danger_level % 10;
					danger_level_max = danger_level / 10;

					//string name = monitoredEntity.GetAttribute("name");
					//System.Diagnostics.Debug.WriteLine("name "+name+"  DELTA danger_level"+danger_level.ToString());
				}
				
				if(avp.Attribute == "workingAround")
				{
					//System.Diagnostics.Debug.WriteLine("AWT Monitoring name "+name+" workingaround changes");
					int wat = sender.GetIntAttribute("workingAround",-1);
					if ((wat>0))
					{
						workaround = true;
					}
					else
					{
						workaround = false;
					}
				}

				if(avp.Attribute == "up")
				{
					if(avp.Value.ToLower() == "false")
					{
						up = false;
					}
					else
					{
						up = true;
					}
				}
				//
				Boolean GoingDownProcess = false;


				if(avp.Attribute == "goingDownInSecs")
				{
					if(avp.Value == "")
					{
						goingDown = false;
					}
					else
					{
						goingDown = true;
						GoingDownProcess = true;
					}
				}

				if(avp.Attribute == "goingDown")
				{
					if(avp.Value == "")
					{
						goingDown = false;
					}
					else
					{
						goingDown = true;
						GoingDownProcess = true;
					}
				}
				goingDown = GoingDownProcess;

				changing_visiblity = (avp.Attribute == "visible");
			}

			if (ignoreInvisibleNodes)
			{ 
				bool isVisible = sender.GetBooleanAttribute("visible", true);
				if (isVisible == false)
				{
					danger_level = -1;
				}
				if (changing_visiblity)
				{
					Refresh();
				}
			}
		}

		#endregion Handling Monitored Node Changes Methods 

	}
}
