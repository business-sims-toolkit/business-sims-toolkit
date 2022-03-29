using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

using LibCore;
using CoreUtils;

using Network;

namespace Cloud.OpsEngine
{
	public class Leaderboard : IDisposable
	{
		NodeTree model;
		List<Node> leaderboardEntries;
		Dictionary<Node, Node> leaderboardEntryToProfile;

		BauManager bauManager;

		Node time;

		public Leaderboard (NodeTree model, BauManager bauManager)
		{
			int round = model.GetNamedNode("RoundVariables").GetIntAttribute("current_round", 0);

			this.model = model;
			time = model.GetNamedNode("CurrentTime");
			time.AttributesChanged += new Node.AttributesChangedEventHandler (time_AttributesChanged);

			this.bauManager = bauManager;

			leaderboardEntries = new List<Node> ();
			leaderboardEntryToProfile = new Dictionary<Node, Node> ();

			Node leaderboard = model.GetNamedNode("Leaderboard");
			foreach (Node leaderboardEntry in leaderboard.GetChildrenOfType("leaderboard_entry"))
			{
				double startValue = 0;
				Node profile = null;

				foreach (Node tryProfile in leaderboardEntry.GetChildrenOfType("leaderboard_entry_round_details"))
				{
					if (tryProfile.GetIntAttribute("round", 0) == round)
					{
						profile = model.GetNamedNode(tryProfile.GetAttribute("profile"));
						startValue = tryProfile.GetDoubleAttribute("start_value", 0);
						break;
					}
				}

				if (! leaderboardEntry.GetBooleanAttribute("is_player", false))
				{
					Debug.Assert(profile != null);

					leaderboardEntryToProfile.Add(leaderboardEntry, profile);
				}

				leaderboardEntries.Add(leaderboardEntry);
				leaderboardEntry.SetAttribute("start_value", startValue);
			}

			Update();
		}

		public void Dispose ()
		{
			time.AttributesChanged -= new Node.AttributesChangedEventHandler (time_AttributesChanged);
		}

		void time_AttributesChanged (Node sender, System.Collections.ArrayList attrs)
		{
			Update();
		}

		void Update ()
		{
			int seconds = time.GetIntAttribute("seconds", 0);
			if ((seconds % 60) == 0)
			{
				foreach (Node leaderboardEntry in leaderboardEntries)
				{
					double? value = null;

					if (leaderboardEntry.GetBooleanAttribute("is_player", false))
					{
						value = leaderboardEntry.GetDoubleAttribute("start_value", 0);
						foreach (Node business in model.GetNodesWithAttributeValue("type", "business"))
						{
							value += business.GetDoubleAttribute("revenue_earned", 0);
							value += business.GetDoubleAttribute("potential_extra_revenue_excluding_missed_demands", 0);
						}
					}
					else
					{
						Node profile = leaderboardEntryToProfile[leaderboardEntry];

						Node profilePoint = null;
						foreach (Node tryProfilePoint in profile.GetChildrenOfType("leaderboard_profile_point"))
						{
							if ((60 * tryProfilePoint.GetIntAttribute("trading_period", 0)) == seconds)
							{
								profilePoint = tryProfilePoint;
								break;
							}
						}

						if (profilePoint != null)
						{
							double scale = profilePoint.GetDoubleAttribute("scale", 0);
							value = leaderboardEntry.GetDoubleAttribute("start_value", 0) * scale;
						}
					}

					if (value.HasValue)
					{
						leaderboardEntry.SetAttribute("current_value", value.Value);
					}
				}
			}
		}
	}
}