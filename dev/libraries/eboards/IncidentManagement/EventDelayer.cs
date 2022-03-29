using System;
using System.Collections;

using System.Xml;

using LibCore;
using Network;
using CoreUtils;

namespace IncidentManagement
{
	public sealed class GlobalEventDelayer
	{
		public static readonly GlobalEventDelayer TheInstance = new GlobalEventDelayer();

		IEventDelayer eventDelayer;

		public IEventDelayer Delayer
		{
			get
			{
				return eventDelayer;
			}
		}

		public void SetEventDelayer (IEventDelayer _eventDelayer)
		{
			eventDelayer = _eventDelayer;
		}

		public void DestroyEventDelayer()
		{
			if(eventDelayer != null)
			{
				eventDelayer.Dispose();
				eventDelayer = null;
			}
		}

		GlobalEventDelayer()
		{
		}
	}

	/// <summary>
	/// The EventDelayer holds classes that define events and delays them until
	/// an appropriate delay has occured and then fires them into the world model.
	/// If the game/world is ever paused then this Singleton should also be paused.
	/// When the game/world is resumed this Singleton should also be resumed.
	/// </summary>
	public sealed class EventDelayer : ITimedClass, IEventDelayer
	{
		EventDelayManager eventDelayManager;

		StopControlledTimer _timer = new StopControlledTimer();
		internal int _tick = 0;

		string targetAttribute;

		//public static readonly EventDelayer TheInstance = new EventDelayer();

		Node modelCounter;

		bool stopped = true;

		Node addToExtendedModelCounter = null;

		public void SetCurrentExtendedModelCounter(Node n)
		{
			addToExtendedModelCounter = n;
		}

		public Hashtable GetAllFutureEvents()
		{
			Hashtable eventToTime = new Hashtable();
			//
			foreach(int time in eventDelayManager.eventsDue.Keys)
			{
				ArrayList events = (ArrayList) eventDelayManager.eventsDue[time];
				foreach(PendingEvent pe in events)
				{
					IncidentDefinition idef = pe.theEvent as IncidentDefinition;
					if(idef != null)
					{
						eventToTime[idef] = time;
					}
				}
			}
			return eventToTime;
		}

		public Hashtable GetAllFutureIDefEvents()
		{
			Hashtable eventToTime = new Hashtable();

			foreach (int time in eventDelayManager.eventsDue.Keys)
			{
				ArrayList events = (ArrayList) eventDelayManager.eventsDue[time];
				foreach (PendingEvent pe in events)
				{
					ModelActionBase idef = pe.theEvent as ModelActionBase;
					if(idef != null)
					{
						eventToTime[idef] = time;
					}
				}
			}
			return eventToTime;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public Hashtable GetFutureEventsOfType(string type)
		{
			Hashtable eventToTime = new Hashtable();
			//
			foreach(int time in eventDelayManager.eventsDue.Keys)
			{
				ArrayList events = (ArrayList) eventDelayManager.eventsDue[time];
				foreach(PendingEvent pe in events)
				{
					IncidentDefinition idef = pe.theEvent as IncidentDefinition;
					if(idef != null)
					{
						if(idef.Type == type)
						{
							eventToTime[idef] = time;
						}
					}
				}
			}

			return eventToTime;
		}

		public Hashtable GetFutureEventsWithAttributeValues(ArrayList attrs)
		{
			Hashtable eventToTime = new Hashtable();
			//
			foreach(int time in eventDelayManager.eventsDue.Keys)
			{
				ArrayList events = (ArrayList) eventDelayManager.eventsDue[time];
				foreach(PendingEvent pe in events)
				{
					IncidentDefinition idef = pe.theEvent as IncidentDefinition;
					if(idef != null)
					{
						bool misMatch = false;
						for(int i=0; !misMatch && (i<attrs.Count); ++i)
						{
							AttributeValuePair avp = (AttributeValuePair) attrs[i];
							if(!idef._Attributes.ContainsKey( avp.Attribute ))
							{
								misMatch = true;
							}
							else
							{
								string val = (string) idef._Attributes[ avp.Attribute ];

								if(val != avp.Value)
								{
									misMatch = true;
								}
							}
						}
						//
						if(!misMatch)
						{
							eventToTime[idef] = time;
						}
					}
				}
			}

			return eventToTime;
		}


		public Hashtable GetFutureEventsWithAttributeValuesAsEvents(ArrayList attrs)
		{
			Hashtable eventToTime = new Hashtable();
			//
			foreach(int time in eventDelayManager.eventsDue.Keys)
			{
				ArrayList events = (ArrayList) eventDelayManager.eventsDue[time];
				foreach(PendingEvent pe in events)
				{
					IncidentDefinition idef = pe.theEvent as IncidentDefinition;
					if(idef != null)
					{
						bool misMatch = false;
						for(int i=0; !misMatch && (i<attrs.Count); ++i)
						{
							AttributeValuePair avp = (AttributeValuePair) attrs[i];
							if(!idef._Attributes.ContainsKey( avp.Attribute ))
							{
								misMatch = true;
							}
							else
							{
								string val = (string) idef._Attributes[ avp.Attribute ];

								if(val != avp.Value)
								{
									misMatch = true;
								}
							}
						}
						//
						if(!misMatch)
						{
							eventToTime[pe] = time;
						}
					}
				}
			}

			return eventToTime;
		}
		
		/// <summary>
		/// 
		/// </summary>
		public void Clear()
		{
			// TODO : If events can fire events on being discarded/fired then these should
			// be fired rather than letting the events just fade into oblivion.
			foreach(Node n in eventDelayManager.modelCounterToEventsDue.Keys)
			{
				modelCounter.AttributesChanged -= modelCounter_AttributesChanged;
			}
			eventDelayManager.Clear();

			_tick = 0;
			//
			//ClearModelCounter();
		}

		public void Detach()
		{
			Clear();
			ClearModelCounter();
		}

		public void Dispose()
		{
			TimeManager.TheInstance.UnmanageClass(this);
			Detach();
		}

		public void Reset()
		{
			//eventDelayManager.Clear();
		}

		void ClearModelCounter()
		{
			if(null != modelCounter)
			{
				modelCounter.AttributesChanged -= modelCounter_AttributesChanged;
				modelCounter = null;
				if(!stopped)
				{
					_timer.Start();
				}
			}
		}

		public void SetModelCounter(NodeTree model, string nodeName, string _targetAttribute)
		{
			Clear();
			ClearModelCounter();
			modelCounter = model.GetNamedNode(nodeName);
			targetAttribute = _targetAttribute;
			modelCounter.AttributesChanged += modelCounter_AttributesChanged;
			_timer.Stop();
		}

		/// <summary>
		/// Adds another model counter so you can have multiple model counters.
		/// </summary>
		/// <param name="model"></param>
		/// <param name="nodeName"></param>
		/// <param name="_targetAttribute"></param>
		public void AddModelCounter(NodeTree model, string nodeName, string _targetAttribute)
		{
			Node modelCounterNode = model.GetNamedNode(nodeName);
			if(!eventDelayManager.modelCounterToEventsDue.ContainsKey(modelCounterNode))
			{
				eventDelayManager.modelCounterToEventsDue[modelCounterNode] = new Hashtable();
				eventDelayManager.modelCounterToTargetAttribute[modelCounterNode] = _targetAttribute;
				modelCounterNode.AttributesChanged += modelCounter_AttributesChanged;
			}
		}

		public void SetEventManager(EventDelayManager _eventDelayManager)
		{
			eventDelayManager.Clear();
			eventDelayManager = _eventDelayManager;
		}

		public EventDelayer()
		{
			// Starts automatically on creation.
			// If the user wants it to be initially stopped they must call
			// Stop on the EventDelayer as the first call to this singleton.
			eventDelayManager = new OpsEventDelayManager();
			_timer.Interval = 1000;
			_timer.Tick += _timer_Tick;

			TimeManager.TheInstance.ManageClass(this);
		}

		public bool AddCreatedNodeEvent(Node parent, ArrayList incidentAttrs, ArrayList attrs, int timeToDelay, NodeTree nt)
		{
			string xmldata = "<i ";
			foreach(AttributeValuePair avp in incidentAttrs)
			{
				xmldata += avp.Attribute + "=\"" + avp.Value + "\" ";
			}
			xmldata += "><createNodes i_to=\"" + parent.GetAttribute("name") + "\">";
			xmldata += "<node ";
			foreach(AttributeValuePair avp in attrs)
			{
				xmldata += avp.Attribute + "=\"" + avp.Value + "\" ";
			}
			xmldata += "/></createNodes></i>";
			//
			return AddEvent(xmldata, timeToDelay, nt);
		}

		public bool AddEvent(string xmldata, int timeToDelay, NodeTree nt)
		{
			BasicXmlDocument xdoc = LibCore.BasicXmlDocument.Create(xmldata);
			return AddEvent(xdoc, timeToDelay, nt);
		}

		public bool AddEvent(BasicXmlDocument xdoc, int timeToDelay, NodeTree nt)
		{
			XmlNode rootNode = xdoc.DocumentElement;
			return AddEvent(rootNode, timeToDelay, nt);
		}

		public bool AddEvent(XmlNode rootNode, int timeToDelay, NodeTree nt)
		{
			IncidentDefinition idef = new IncidentDefinition(rootNode, nt);
			idef.doAfterSecs = 0;
			return AddEvent((IEvent) idef, timeToDelay, nt);
		}

		public bool AddEvent(IEvent e, int timeToDelay, NodeTree nt)
		{
			lock(this)
			{
				if(timeToDelay <= 0)
				{
					e.ApplyActionNow(nt);
				}
				else
				{
					if(addToExtendedModelCounter == null)
					{
						eventDelayManager.AddEvent(e, _tick + timeToDelay, nt);
					}
					else
					{
						eventDelayManager.AddEvent(addToExtendedModelCounter, e, _tick + timeToDelay, nt);
					}
				}
			}

			return true;
		}
		/// <summary>
		/// Removes an Event 
		/// </summary>
		/// <param name="e">The event to kill</param>
		public bool RemoveEvent(IEvent e)
		{
			lock(this)
			{
				eventDelayManager.RemoveEvent(e);
			}

			return false;
		}
		/// <summary>
		/// 
		/// </summary>
		public void Start()
		{
			if(null == modelCounter)
			{
				_timer.Start();
			}
			stopped = false;
		}

		/// <summary>
		/// 
		/// </summary>
		public void Stop()
		{
			if(null == modelCounter)
			{
				_timer.Stop();
			}
			stopped = true;
		}

		public void FastForward(double timesRealTime)
		{
			_timer.Interval = Math.Max(1, (int)(1000.0/timesRealTime));
		}

		void _timer_Tick(object sender, EventArgs e)
		{
			// Assumming single thread for now for NeoSwiff apps.
			// Should add LibCore.LockAcquire / LockRelease funcs to
			// be really thread safe.
			++_tick;
			FireTick(eventDelayManager.eventsDue);
		}

		void FireTick(Hashtable eventsDue)
		{
			if(eventsDue.ContainsKey(_tick))
			{
				ArrayList array = (ArrayList) eventsDue[_tick];
				foreach(PendingEvent pe in array)
				{
					eventDelayManager.FireEvent(pe);
				}
				//
				eventsDue.Remove(_tick);
			}
		}

		void modelCounter_AttributesChanged(Node sender, ArrayList attrs)
		{
			string attribute = targetAttribute;
			Hashtable events = eventDelayManager.eventsDue;

			if(sender != this.modelCounter)
			{
				// Do we have another model counter that we are also watching?
				if(!eventDelayManager.modelCounterToEventsDue.ContainsKey(sender)) return;
				events = (Hashtable) eventDelayManager.modelCounterToEventsDue[sender];
				attribute = (string) eventDelayManager.modelCounterToTargetAttribute[sender];
			}

			foreach(AttributeValuePair avp in attrs)
			{
				if(avp.Attribute == attribute)
				{
					_tick = CONVERT.ParseInt(avp.Value);
					FireTick(events);
				}
			}
		}
	}
}



