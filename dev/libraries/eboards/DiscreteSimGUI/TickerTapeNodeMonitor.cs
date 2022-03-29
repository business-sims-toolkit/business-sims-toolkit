using System;
using System.Collections.Generic;
using System.Drawing;

using CommonGUI;

using Network;

namespace DiscreteSimGUI
{
	public class TickerTapeNodeMonitor : IDisposable
	{
		TickerTape tickerTape;
		bool isTrainingGame;

		Dictionary<Node, TickerTape.TickerTapeTextItem> nodeToTickerTapeItem;

		Node node;
		List<Node> watchedNodes;

		public TickerTapeNodeMonitor (TickerTape tickerTape, bool isTrainingGame, Node node)
		{
			this.tickerTape = tickerTape;
			this.node = node;
			this.isTrainingGame = isTrainingGame;

			watchedNodes = new List<Node> ();
			nodeToTickerTapeItem = new Dictionary<Node, TickerTape.TickerTapeTextItem> ();

			node.ChildAdded += node_ChildAdded;
			node.ChildRemoved += node_ChildRemoved;

			foreach (Node child in node.getChildren())
			{
				WatchNode(child);
			}
		}

		public void Dispose ()
		{
			node.ChildAdded -= node_ChildAdded;
			node.ChildRemoved -= node_ChildRemoved;

			foreach (Node child in new List<Node> (watchedNodes))
			{
				UnwatchNode(node);
			}
		}

		void WatchNode (Node node)
		{
			node.AttributesChanged += node_AttributesChanged;
			watchedNodes.Add(node);

			CreateTickerTapeItem(node);
		}

		void DeleteTickerTapeItem (Node node)
		{
			if (nodeToTickerTapeItem.ContainsKey(node))
			{
				tickerTape.RemoveItem(nodeToTickerTapeItem[node]);
				nodeToTickerTapeItem.Remove(node);
			}
		}

		void CreateTickerTapeItem (Node node)
		{
			string message = null;

			switch (node.GetAttribute("status"))
			{
				case "pending":
					if (this.isTrainingGame == false)
					{
						message = node.GetAttribute("pending_msg");
					}
					else
					{
						message = node.GetAttribute("pending_msg_training");
					}
					break;

				case "active":
					if (this.isTrainingGame == false)
					{
						message = node.GetAttribute("active_msg");
					}
					else
					{
						message = node.GetAttribute("active_msg_training");
					}
					break;
			}

			if (message != null)
			{
				TickerTape.TickerTapeTextItem tickerTapeItem = new TickerTape.TickerTapeTextItem (message, Color.FromArgb(0, 255, 0), tickerTape);
				tickerTape.AddItem(tickerTapeItem);
				nodeToTickerTapeItem.Add(node, tickerTapeItem);
			}
		}

		void UnwatchNode (Node node)
		{
			DeleteTickerTapeItem(node);

			node.AttributesChanged -= node_AttributesChanged;
			watchedNodes.Remove(node);
		}

		void node_ChildAdded (Node sender, Node child)
		{
			WatchNode(child);
		}

		void node_ChildRemoved (Node sender, Node child)
		{
			UnwatchNode(child);
		}

		void node_AttributesChanged (Node sender, System.Collections.ArrayList attrs)
		{
			bool recalculate = false;

			foreach (AttributeValuePair avp in attrs)
			{
				switch (avp.Attribute)
				{
					case "status":
					case "pending_msg":
					case "active_msg":
					case "pending_msg_training":
					case "active_msg_training":
						recalculate = true;
						break;
				}
			}

			if (recalculate)
			{
				DeleteTickerTapeItem(sender);
				CreateTickerTapeItem(sender);
			}
		}
	}
}