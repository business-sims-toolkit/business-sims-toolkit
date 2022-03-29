using System.Collections;

using LibCore;
using Network;
using CoreUtils;

namespace IncidentManagement
{
	/// <summary>
	/// Summary description for DayRunner.
	/// </summary>
	public class DayRunner : ITimedClass
	{
		const string Node_CurrentDay = "CurrentDay";
		//
		NodeTree _model = null;

		Node MyCurrentDayNode = null;
		protected Node currentTimeNode;
		protected int reset_day_value =1;
		int lastKnownSeconds = 0;

		/// <summary>
		/// The constructor for DayRunner.
		/// </summary>
		public DayRunner(NodeTree model)
		{
			_model = model;
			//RESET IS SKINNED, In PM we start at 0, in V3 we reset to day 1
			reset_day_value = CoreUtils.SkinningDefs.TheInstance.GetIntData("day_runner_reset", 1);
			currentTimeNode = model.GetNamedNode("CurrentTime");
			MyCurrentDayNode = model.GetNamedNode(Node_CurrentDay);
			currentTimeNode.AttributesChanged += DayRunner_AttributesChanged;
		}

		public void Dispose()
		{
			currentTimeNode.AttributesChanged -= DayRunner_AttributesChanged;
		}

		/// <summary>
		/// 
		/// </summary>
		public void Start()
		{
		}

		/// <summary>
		/// 
		/// </summary>
		public void Stop()
		{

		}

		public void Reset()
		{
			//On Reset we go back to day 1.
			//Actually go back to day zero so you can enter things before day 1!
			//RESET IS SKINNED, In PM we start at 0, in V3 we reset to day 1
			MyCurrentDayNode.SetAttribute("day",CONVERT.ToStr(reset_day_value));
		}

		public void FastForward(double timesRealTime)
		{
		}

		void DayRunner_AttributesChanged(Node sender, ArrayList attrs)
		{
			foreach(AttributeValuePair avp in attrs)
			{
				if(avp.Attribute == "seconds")
				{
					int secs = CONVERT.ParseInt(avp.Value);
					if(secs > lastKnownSeconds)
					{
						if(secs%60 == 0)
						{
							int daycount = MyCurrentDayNode.GetIntAttribute("day",0);
							++daycount;

							MyCurrentDayNode.SetAttribute("day",daycount);

							lastKnownSeconds = secs;
						}
					}
				}
			}
		}
	}
}

