using System;
using System.Windows.Forms;
using System.Drawing;
using System.Collections;

using LibCore;
using Network;
using CoreUtils;

namespace DiscreteSimGUI
{
	/// <summary>
	/// The StockBoard is a pseudo stock view showing incoming and lost "trades".
	/// </summary>
	public class StockBoard : BasePanel, ITimedClass
	{
		// An array of TradeRowInfos.
		protected ArrayList rows;
		// A Hashtable of transaction nodes to TradeRowInfos.
		protected Hashtable transactionNodeToRow;
		// A Hashtable of transaction name and channel and time to TradeRowInfos.
		protected Hashtable transactionNameAndChannelAndTimeToRow;
		protected Font font;

		protected Timer timer;

		protected int first_row_displayed;
		protected int num_rows_displayed;

		protected int count_down;
		protected /*TradeRowInfo*/ ArrayList jiggleRows;

		protected Random random;

		protected NodeTree model;
		protected bool isTrainingGame;

		protected Node transactionsNode = null;

		protected bool previousSecondsKnown = false;
		protected int previousSeconds = 0;

		protected double timerFrequency = 0;

		public StockBoard(NodeTree _model, Boolean _isTrainingGame)
		{
			jiggleRows = new ArrayList();
			model = _model;
			isTrainingGame = _isTrainingGame;

			transactionNameAndChannelAndTimeToRow = new Hashtable();
			transactionNodeToRow = new Hashtable();

			this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			this.SetStyle(ControlStyles.UserPaint, true);
			this.SetStyle(ControlStyles.DoubleBuffer, true);

			random = new Random();

			//font = ConstantSizeFont.NewFont("Digital Readout ExpUpright", 12);
			//font = ConstantSizeFont.NewFont("Lucida Console", 12, FontStyle.Bold);
			font = CoreUtils.SkinningDefs.TheInstance.GetFont(12, FontStyle.Bold);
			rows = new ArrayList();

			this.timerFrequency = 1000 / 50;

			timer = new Timer();
			timer.Tick += timer_Tick;
			timer.Interval = (int) (1000 / this.timerFrequency);

			this.BackColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("stockboard_row_colour", Color.White);

			first_row_displayed = 0;
			num_rows_displayed = 0;

			transactionsNode = model.GetNamedNode("Transactions");
			transactionsNode.ChildAdded += transactionsNode_ChildAdded;
			transactionsNode.ChildRemoved += transactionsNode_ChildRemoved;

			TimeManager.TheInstance.ManageClass(this);
		}

		public TradeRowInfo AddRow(
			string _trade_name,
			string _channel,
			string _time,
			int _bank,
			string _market_cap)
		{
			TradeRowInfo tri = new TradeRowInfo(_trade_name, _channel, _time, _bank, _market_cap, random,
			                                    x1, x2, x3, x4, x5);
			rows.Add(tri);

			if(0 == num_rows_displayed) num_rows_displayed = 1;

			if(jiggleRows.Count == 0)
			{
				jiggleRows.Add(tri);
			}

			if(!timer.Enabled)
			{
				count_down = 10;
			}

			timer.Start();

			return tri;
		}

		public static int x1 = 3;
		public static int x2 = 200;
		public static int x3 = 310;
		public static int x4 = 380;
		public static int x5 = 470;
		public int maxRowsToDisplay = 8;
		public bool showHeaders = true;

		public void SetColumns (int x1, int x2, int x3, int x4, int x5)
		{
			StockBoard.x1 = x1;
			StockBoard.x2 = x2;
			StockBoard.x3 = x3;
			StockBoard.x4 = x4;
			StockBoard.x5 = x5;
		}
	
		protected override void OnPaint(PaintEventArgs e)
		{
			int yoffset;
			if (showHeaders)
			{
				yoffset = 3;

				e.Graphics.DrawString(" Trade",font,Brushes.White,x1,yoffset);
				e.Graphics.DrawString("Channel",font,Brushes.White,x2,yoffset);
				e.Graphics.DrawString(" Time",font,Brushes.White,x3,yoffset);
				e.Graphics.DrawString(" Exch",font,Brushes.White,x4,yoffset);

				yoffset += 25;
			}
			else
			{
				yoffset = 2;
			}

			int rowHeight = 18;
			for(int i=first_row_displayed; i<num_rows_displayed; ++i)
			{
				TradeRowInfo tri = (TradeRowInfo) rows[i];
				tri.Paint(e.Graphics, font, yoffset, Width, rowHeight, i);
				yoffset += rowHeight;
			}
		}

		void timer_Tick(object sender, EventArgs e)
		{
			// Find how long has passed in the model since the last tick.
			bool currentSecondsKnown = false;
			int duration = 0;
			bool durationKnown = false;

			if (model != null)
			{
				Node TimeNode = model.GetNamedNode("CurrentTime");

				if (TimeNode != null)
				{
					int currentSeconds = TimeNode.GetIntAttribute("seconds", -1);
					currentSecondsKnown = (currentSeconds != -1);

					if (currentSecondsKnown)
					{
						if (this.previousSecondsKnown)
						{
							duration = currentSeconds - this.previousSeconds;
							durationKnown = true;
						}

						this.previousSeconds = currentSeconds;
						this.previousSecondsKnown = true;
					}
				}
			}
			this.previousSecondsKnown = currentSecondsKnown;

			// Now, how many jiggles should we do this tick?
			int jiggleTurns = 1;
			if (durationKnown)
			{
				// Fudge factor allows us to make jiggling take longer than it should do
				// when in fast-forward mode (but still be quicker than if it weren't corrected at all
				// for being fast-forwareded).
				double fudgeFactor = 1.0 / 5;

				jiggleTurns = Math.Max(1, (int) (duration * this.timerFrequency * fudgeFactor));
			}

			if(jiggleRows.Count > 0)
			{
				ArrayList removeElements = new ArrayList();

				foreach(TradeRowInfo jiggleRow in jiggleRows)
				{
					for (int jiggleTurn = 0; jiggleTurn < jiggleTurns; jiggleTurn++)
					{
						if(jiggleRow.Jiggle())
						{
							// Jiggling has finished so add next row...

							removeElements.Add(jiggleRow);

							if(jiggleRow.removing)
							{
								rows.Remove(jiggleRow);
								--num_rows_displayed;
							}

							break;
						}
					}
				}

				foreach(TradeRowInfo tri in removeElements)
				{
					jiggleRows.Remove(tri);
				}
				//

				if( (jiggleRows.Count == 0) && (num_rows_displayed < rows.Count) )
				{
					if( (num_rows_displayed >= 0) && (num_rows_displayed < maxRowsToDisplay) )
					{
						jiggleRows.Add( rows[num_rows_displayed] );
						++num_rows_displayed;
					}
				}
				//
				this.Invalidate();
				return;
			}

			this.Invalidate();

			if(num_rows_displayed == rows.Count)
			{
				timer.Stop();
				return;
			}
		}

		protected string TimeToString(int time)
		{
			// We want a string of the form HH-MM.
			// Game starts at 14:00...
			int actual_time = 14 * 60 + time/60;
			int hours = actual_time / 60;
			int mins = actual_time - (60*hours);
			int secs = time % 60;
			//
			string time_str = LibCore.CONVERT.ToStr(hours).PadLeft(2,' ');
			time_str += ":" + LibCore.CONVERT.ToStr(mins).PadLeft(2,'0');
			time_str += ":" + LibCore.CONVERT.ToStr(secs).PadLeft(2,'0');
			//
			return time_str;
		}

		void AddTransaction(Node TransactionNode)
		{
			string name = TransactionNode.GetAttribute("name");
			string displayname = TransactionNode.GetAttribute("displayname");
			string event_type = TransactionNode.GetAttribute("event_type");
			int sequence = TransactionNode.GetIntAttribute("sequence",0);
			int time = TransactionNode.GetIntAttribute("time",0);
			string status = TransactionNode.GetAttribute("status");
			string store_id_str = TransactionNode.GetAttribute("store");

			string biz_name = CoreUtils.SkinningDefs.TheInstance.GetData("biz");
			store_id_str = store_id_str.Replace(biz_name.ToLower(),"");
			store_id_str = store_id_str.Replace(biz_name,"");
			store_id_str = store_id_str.Replace(" ","");

			int store_id = LibCore.CONVERT.ParseInt(store_id_str);

			// Any one row can have four transactions displayed on it.
			// Therefore, what we do is have a hashtable of transaction node to row.

			string display_time = TimeToString(time);
			string name_and_channel_and_time = displayname.ToLower() + event_type.ToLower() + display_time.ToLower();
			// transactionNodeToRow
			if(transactionNameAndChannelAndTimeToRow.ContainsKey(name_and_channel_and_time))
			{
				TradeRowInfo tri = (TradeRowInfo) transactionNameAndChannelAndTimeToRow[name_and_channel_and_time];
				tri.AddTransactionNode(TransactionNode);
				tri.SetBank(store_id);
				transactionNodeToRow[TransactionNode] = tri;
				tri.SetStatus(store_id, status);
				timer.Start();
			}
			else
			{
				TradeRowInfo tri = this.AddRow(displayname, event_type, display_time, store_id, "$  0 M");
				tri.AddTransactionNode(TransactionNode);
				transactionNameAndChannelAndTimeToRow[name_and_channel_and_time] = tri;
				transactionNodeToRow[TransactionNode] = tri;
				tri.SetStatus(store_id, status);
				timer.Start();
			}
			//
			TransactionNode.AttributesChanged += TransactionNode_AttributesChanged;
		}

		void RemoveTransaction(Node TransactionNode)
		{
			// TODO : Check this!
			string name = TransactionNode.GetAttribute("name");
			string displayname = TransactionNode.GetAttribute("displayname");
			string event_type = TransactionNode.GetAttribute("event_type");
			int sequence = TransactionNode.GetIntAttribute("sequence",0);
			int time = TransactionNode.GetIntAttribute("time",0);
			string status = TransactionNode.GetAttribute("status");

			string display_time = TimeToString(time);
			string name_and_channel_and_time = displayname.ToLower() + event_type.ToLower() + display_time.ToLower();

			if(transactionNameAndChannelAndTimeToRow.ContainsKey(name_and_channel_and_time))
			{
				TradeRowInfo tri = (TradeRowInfo) transactionNameAndChannelAndTimeToRow[name_and_channel_and_time];
				tri.RemoveTransactionNode(TransactionNode);
				if(tri.NumTransactionNodes() == 0)
				{
					tri.removing = true;
					tri.SetScreenInfo("","","",0,"");
					this.jiggleRows.Add(tri);
					timer.Start();
				}
			}

			TransactionNode.AttributesChanged -= TransactionNode_AttributesChanged;
			//SequenceToDispName.Remove(sequence);
			//TransactionNodes.Remove(TransactionNode);
			//RefreshDisplay();
		}

		void transactionsNode_ChildAdded(Node sender, Node child)
		{
			AddTransaction(child);
		}

		void transactionsNode_ChildRemoved(Node sender, Node child)
		{
			RemoveTransaction(child);
		}

		void TransactionNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			foreach(AttributeValuePair avp in attrs)
			{
				if (avp.Attribute == "status")
				{
					// Queued,    = Green
					// Delayed,   = Amber
					// Handled,   = BrightGreen(?) / LawnGreen
					// Cancelled. = Red
					//
					// status changes to "delayed" or "
					string status = sender.GetAttribute("status");
					string store_id_str = sender.GetAttribute("store");

					string biz_name = CoreUtils.SkinningDefs.TheInstance.GetData("biz");
					store_id_str = store_id_str.Replace(biz_name.ToLower(),"");
					store_id_str = store_id_str.Replace(biz_name,"");
					store_id_str = store_id_str.Replace(" ","");

					if(transactionNodeToRow.ContainsKey(sender))
					{
						int store_id = LibCore.CONVERT.ParseInt(store_id_str);
						TradeRowInfo tri = (TradeRowInfo) transactionNodeToRow[sender];
						tri.SetStatus(store_id, status);
						timer.Start();
					}
				}
			}
		}
		#region ITimedClass Members

		public void Start()
		{
			timer.Start();
		}

		public void FastForward(double timesRealTime)
		{
			// TODO:  Add StockBoard.FastForward implementation
		}

		public void Reset()
		{
			// TODO:  Add StockBoard.Reset implementation
		}

		public void Stop()
		{
			timer.Stop();
		}

		#endregion
	
		protected override void Dispose(bool disposing)
		{
			if(disposing)
			{
				if (transactionsNode != null)
				{
					transactionsNode.ChildAdded -= transactionsNode_ChildAdded;
					transactionsNode.ChildRemoved -= transactionsNode_ChildRemoved;
					transactionsNode = null;
				}

				// : Remove a stray event handler.
				timer.Tick -= timer_Tick;
			}

			if(disposing)
			{
				TimeManager.TheInstance.UnmanageClass(this);
			}
			base.Dispose (disposing);
		}
	}
}