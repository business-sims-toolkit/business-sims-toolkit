using System.Collections;
using System.IO;
//
using LibCore;
using Logging;
//
namespace ReportBuilder
{
	public class LatchedEvent
	{
		public Hashtable states;
		public double time;

		protected LatchEdgeTracker tracker;

		public LatchedEvent(LatchEdgeTracker _tracker, Hashtable s, double t)
		{
			tracker = _tracker;
			states = s;
			time = t;
		}

		public object Clone()
		{
			LatchedEvent le = new LatchedEvent(tracker,(Hashtable)states.Clone(), time);
			return le;
		}

		public bool GetBoolEventActive(string key)
		{
			if(states.ContainsKey(key))
			{
				return CONVERT.ParseBool((string) (states[key]), false);
			}
			//
			if(tracker.currentDefaultValues.ContainsKey(key))
			{
				return (bool) tracker.currentDefaultValues[key];
			}
			//
			return false;
		}

		public bool GetCounterEventActive(string key)
		{
			if(states.ContainsKey(key))
			{
				if( ( (string)states[key] ) == "1" )
				{
					return true;
				}
			}
			return false;
		}

		public string GetStringEvent(string key)
		{
			if(states.ContainsKey(key))
			{
				return (string) states[key];
			}

			return "";
		}
	}
	/// <summary>
	/// Summary description for LatchEdgeTracker.
	/// </summary>
	public class LatchEdgeTracker
	{
		/// <summary>
		/// There are only two types of attributes you want to graph on a Gantt chart:
		/// BOOL : Up = true/false
		/// COUNTER : A Rebooting Counter or a WorkingAround Counter, etc.
		/// </summary>
		public enum AttrType
		{
			BOOL,
			COUNTER,
			STRING
		}
		/// <summary>
		/// Store an array of the attributes we are latching on change.
		/// </summary>
		protected ArrayList watches = new ArrayList();
		/// <summary>
		/// Store an array of our latched changed states.
		/// </summary>
		protected ArrayList changes = new ArrayList();
		//
		protected LatchedEvent currentValues;
		//
		protected Hashtable currentAttrTypes = new Hashtable();
		//
		public Hashtable currentDefaultValues = new Hashtable();
		//
		protected bool compressToNearestSecond = true;

		public int Count
		{
			get
			{
				int num = changes.Count;
				if(currentValues.states.Count > 0) ++num;
				return num;
			}
		}

		public int NumChanges
		{
			get
			{
				return changes.Count;
			}
		}

		public LatchedEvent GetLatchedEvent(int n)
		{
			if(n < changes.Count-1)
			{
				return (LatchedEvent) changes[n];
			}

			return currentValues;
		}

		public void AddLatchedEvent(LatchedEvent le)
		{
			// TODO : improve merging of latched streams...
			// Don't add the start events during stream merging.
			changes.Add(le);
		}

		public void RemoveLatchedEvent(LatchedEvent le)
		{
			if(le == currentValues)
			{
				currentValues = new LatchedEvent(this, new Hashtable(),0);
			}
			changes.Remove(le);
		}

		public bool CompressToNearestSecond
		{
			set
			{
				compressToNearestSecond = value;
			}
		}
		//
		public LatchEdgeTracker()
		{
			currentValues = new LatchedEvent(this, new Hashtable(),0);
		}
		/// <summary>
		/// Track an attribute and latch an edge on it changing if required.
		/// </summary>
		/// <param name="attr"></param>
		/// <param name="latch"></param>
		public void TrackBooleanAttribute(string attr, bool defaultValue)
		{
			watches.Add(attr);
			this.currentAttrTypes[attr] = AttrType.BOOL;
			currentDefaultValues[attr] = defaultValue;
		}

		/// <summary>
		/// Given a current time, and an attribute/value pair, tell us how long has elapsed
		/// since the attribute last got assigned the given value, or zero if it's
		/// never been assigned that.
		/// </summary>
		public double TimeSinceStateChangedTo (double refTime, string attribute, string value)
		{
			LatchedEvent mostRecentRelevantEvent = null;

			foreach (LatchedEvent latchedEvent in changes)
			{
				if (latchedEvent.time >= refTime)
				{
					break;
				}

				foreach (string a in latchedEvent.states.Keys)
				{
					if ((a == attribute) && (((string) latchedEvent.states[a]) == value))
					{
						mostRecentRelevantEvent = latchedEvent;
					}
				}
			}

			if (mostRecentRelevantEvent != null)
			{
				return refTime - mostRecentRelevantEvent.time;
			}

			return 0;
		}

		public void TrackCounterAttribute(string attr)
		{
			watches.Add(attr);
			this.currentAttrTypes[attr] = AttrType.COUNTER;
		}

		public void TrackStringAttribute(string attr)
		{
			watches.Add(attr);
			this.currentAttrTypes[attr] = AttrType.STRING;
		}

		public void CheckLine(string line, double time)
		{
			bool pushChange = false;
			LatchedEvent curVals = (LatchedEvent) this.currentValues.Clone();

			foreach(string attr in watches)
			{
				string val;
				//
				if(!BasicIncidentLogReader.ExtractValue(line, attr, out val))
				{
					continue;
				}
				//
				AttrType at = (AttrType) currentAttrTypes[attr];
				//
				//
				if(!currentValues.states.ContainsKey(attr))
				{
					if("" != val)
					{
						pushChange = true;
						/*
						// Push current values on to the change stack...
						changes.Add( currentValues.Clone() );*/
						//
						switch(at)
						{
							case AttrType.BOOL:
							{
								if(val == "") val = "false";
							}
								break;

							case AttrType.COUNTER:
							{
								// Anything zero is the same as empty.
								// Anything else is the same as "1".
								//if(val == "0") val = "";
								//else if(val == "") val = "";
								//else val = "1";

								if(val == "0") val = "";
								else if(val != "") val = "1";
							}
								break;
						}
						//
						currentValues.states[attr] = val;
					}
				}
				else
				{
					//
					// We store this as a real state change depending on the value type.
					//
					string curVal = (string) currentValues.states[attr];
					//
					switch(at)
					{
						case AttrType.BOOL:
						{
							if(curVal == "") curVal = "false";
							if(val == "") val = "false";
						}
							break;

						case AttrType.COUNTER:
						{
							// Anything zero is the same as empty.
							// Anything else is the same as "1".
							//if(curVal == "0") curVal = "";
							//else if(curVal == "") curVal = "";
							//else curVal = "1";
							if(curVal != "") curVal = "1";

							//if(val == "0") val = "";
							//else if(val == "") val = "";
							//else val = "1";
							if(val == "0") val = "";
							else if(val != "") val = "1";
						}
							break;
					}
					//
					if(val != curVal)
					{
						//
						// Things have changed.
						//
						pushChange = true;
						//
						/*
						// Push current values on to the change stack...
						changes.Add( currentValues.Clone() );*/
						//
						currentValues.states[attr] = val;
					}
				}
			}
			//
			if(pushChange)
			{
				if(currentValues.states.Keys.Count > 0)
				{
					currentValues.time = time;
					//
					// Check to see if we should compress all changes that occur in the same
					// second together or not....
					//
					if(compressToNearestSecond)
					{
						if(changes.Count > 0)
						{
							LatchedEvent change = (LatchedEvent) changes[ changes.Count-1 ];
							if(change.time == time)
							{
								// The last change occured at the same time as this so the new
								// state overwrites the last.
								changes.RemoveAt( changes.Count - 1 );
							}
						}
					}
					changes.Add( currentValues.Clone() );
				}
			}
		}
		protected string SecsToString(double _secs)
		{
			int mins = (int)(_secs/60);
			int secs = (int)(_secs-mins*60);
			return mins.ToString().PadLeft(2,'0') + ":" + secs.ToString().PadLeft(2,'0');
		}
		/// <summary>
		/// Writes the latched stream out to a file.
		/// </summary>
		/// <param name="file"></param>
		public void StoreInFile(string file)
		{
			StreamWriter SW=File.CreateText(file);
			//
			if(currentValues.states.Count > 0)
			{
				foreach(LatchedEvent change in changes)
				{
					SW.WriteLine("--" + SecsToString(change.time) + "-" + change.time.ToString() + "----------------------------");
					//
					foreach(string attr in change.states.Keys)
					{
						SW.WriteLine(attr + "\t=\t" + (string)change.states[attr]);
					}
				}
				//
				SW.WriteLine("---" + SecsToString(currentValues.time) + "-" + currentValues.time.ToString() + "---------------------------");
				//
				foreach(string attr2 in currentValues.states.Keys)
				{
					SW.WriteLine(attr2 + "\t=\t" + (string)currentValues.states[attr2]);
				}
			}
			SW.Close();
		}
	}
}
