using System;
using System.Drawing;

using System.Collections;
using CoreUtils;

using Network;

namespace DiscreteSimGUI
{
	/// <summary>
	/// TradeRowInfo stores the info required for a row on the StockBoard.
	/// </summary>
	public class TradeRowInfo
	{
		protected int tradeLength;
		protected int channelLength;
		protected int timeLength;
		protected int bankLength;
		protected int marketCapLength;

		protected static SolidBrush queuedBrush = new SolidBrush(SkinningDefs.TheInstance.GetColorDataGivenDefault("transaction_queued_colour", Color.Black));
		protected static SolidBrush handledBrush = new SolidBrush(SkinningDefs.TheInstance.GetColorDataGivenDefault("transaction_handled_colour", Color.Green));
		protected static SolidBrush cancelledBrush = new SolidBrush(SkinningDefs.TheInstance.GetColorDataGivenDefault("transaction_cancelled_colour", Color.Red));
		protected static SolidBrush delayedBrush = new SolidBrush(SkinningDefs.TheInstance.GetColorDataGivenDefault("transaction_delayed_colour", Color.DarkOrange));

		protected static SolidBrush queuedBrush_alternate = new SolidBrush(SkinningDefs.TheInstance.GetColorDataGivenDefault("transaction_queued_alternate_colour", Color.Black));
		protected static SolidBrush handledBrush_alternate = new SolidBrush(SkinningDefs.TheInstance.GetColorDataGivenDefault("transaction_handled_alternate_colour", Color.Green));
		protected static SolidBrush cancelledBrush_alternate = new SolidBrush(SkinningDefs.TheInstance.GetColorDataGivenDefault("transaction_cancelled_alternate_colour", Color.Red));
		protected static SolidBrush delayedBrush_alternate = new SolidBrush(SkinningDefs.TheInstance.GetColorDataGivenDefault("transaction_delayed_alternate_colour", Color.DarkOrange));

		protected static SolidBrush backBrush = new SolidBrush(SkinningDefs.TheInstance.GetColorDataGivenDefault("stockboard_row_colour", Color.White));
		protected static SolidBrush backBrush_alternate = new SolidBrush(SkinningDefs.TheInstance.GetColorDataGivenDefault("stockboard_alternate_row_colour", Color.White));

		// Actual strings we want to display.
		protected string trade_name;	// 16 chars
		protected string channel;		// 7  chars
		protected string time;			// 5  chars
		protected string banks;			// 4  chars
		protected string market_cap;	// 6  chars

		// Currently partial strings.
		// Any char that is not '^' is a real char.
		// Any char that is '^' is a random char.
		protected string r_trade_name;	// 16 chars
		protected string r_channel;		// 7  chars
		protected string r_time;		// 5  chars
		protected string r_banks;		// 4  chars
		protected string r_market_cap;	// 6  chars

		// Cuurently displayed strings.
		protected string d_trade_name;	// 16 chars
		protected string d_channel;		// 7  chars
		protected string d_time;		// 5  chars
		protected string d_banks;		// 4  chars
		protected string d_market_cap;	// 6  chars

		// Finished jiggling?
		protected bool done_trade_name;
		protected bool done_channel;
		protected bool done_time;
		protected bool done_banks;
		protected bool done_market_cap;

		protected Random random;

		protected ArrayList correction_order;

		protected ArrayList transactionNodes;

		protected bool bank1_set = false;
		protected bool bank2_set = false;
		protected bool bank3_set = false;
		protected bool bank4_set = false;

		public bool removing = false;

		public enum BankStatus
		{
			Queued = 0,
			Delayed,
			Handled,
			Cancelled
		}

		protected BankStatus main_status = BankStatus.Queued;

		protected BankStatus bank1_status = BankStatus.Queued;
		protected BankStatus bank2_status = BankStatus.Queued;
		protected BankStatus bank3_status = BankStatus.Queued;
		protected BankStatus bank4_status = BankStatus.Queued;

		protected int x1, x2, x3, x4, x5;

		void Dispose (bool bisposing)
		{
			backBrush.Dispose();
			backBrush_alternate.Dispose();
			queuedBrush.Dispose();
			queuedBrush_alternate.Dispose();
			delayedBrush.Dispose();
			delayedBrush_alternate.Dispose();
			handledBrush.Dispose();
			handledBrush_alternate.Dispose();
			cancelledBrush.Dispose();
			cancelledBrush_alternate.Dispose();

			backBrush = null;
			backBrush_alternate = null;
			queuedBrush = null;
			queuedBrush_alternate = null;
			delayedBrush = null;
			delayedBrush_alternate = null;
			handledBrush = null;
			handledBrush_alternate = null;
			cancelledBrush = null;
			cancelledBrush_alternate = null;
		}

		public void AddTransactionNode(Node tn)
		{
			transactionNodes.Add(tn);
		}

		public void RemoveTransactionNode(Node tn)
		{
			transactionNodes.Remove(tn);
		}

		public int NumTransactionNodes()
		{
			return transactionNodes.Count;
		}

		public BankStatus MainStatus
		{
			get { return main_status; }
		}

		public void SetStatus(int bank, string status)
		{
			// Queued,    = Green
			// Delayed,   = Amber
			// Handled,   = BrightGreen(?) / LawnGreen
			// Cancelled. = Red

			BankStatus b_status = BankStatus.Queued;
			switch(status.ToLower())
			{
				case "at risk":
				case "delayed":
					b_status = BankStatus.Delayed;
					break;

				case "handled":
					// Have we just become handled?
					if (b_status != BankStatus.Handled)
					{
						b_status = BankStatus.Handled;
					}
					break;

				case "canceled":
				case "cancelled":
					b_status = BankStatus.Cancelled;
					break;
			}

			if(1 == bank) bank1_status = b_status;
			else if(2 == bank) bank2_status = b_status;
			else if(3 == bank) bank3_status = b_status;
			else if(4 == bank) bank4_status = b_status;

			if( (BankStatus.Cancelled == bank1_status) || (BankStatus.Cancelled == bank2_status) ||
				(BankStatus.Cancelled == bank3_status) || (BankStatus.Cancelled == bank4_status) )
			{
				// If anyybody is cancelled then the line shows red as we've lost trades!
				main_status = BankStatus.Cancelled;
			}
			else if( (BankStatus.Delayed == bank1_status) || (BankStatus.Delayed == bank2_status) ||
				(BankStatus.Delayed == bank3_status) || (BankStatus.Delayed == bank4_status) )
			{
				// If anyybody is delayed then the line shows Amber as we may lose trades!
				main_status = BankStatus.Delayed;
			}
			else if( (BankStatus.Handled == bank1_status) || (BankStatus.Handled == bank2_status) ||
				(BankStatus.Handled == bank3_status) || (BankStatus.Handled == bank4_status) )
			{
				// If anyybody is Handled then the line shows bright green as we have made money!
				main_status = BankStatus.Handled;
			}
			else
			{
				main_status = BankStatus.Queued;
			}
		}

		protected string BanksToString()
		{
			string _banks = "";

			if(bank1_set) _banks += "1";
			else _banks += "-";

			if(bank2_set) _banks += "2";
			else _banks += "-";

			if(bank3_set) _banks += "3";
			else _banks += "-";

			if(bank4_set) _banks += "4";
			else _banks += "-";

			return _banks;
		}

		public void SetBank(int bank)
		{
			if(bank == 1) bank1_set = true;
			else if(bank == 2) bank2_set = true;
			else if(bank == 3) bank3_set = true;
			else if(bank == 4) bank4_set = true;

			banks = BanksToString();
		}

		public TradeRowInfo(
			string _trade_name,
			string _channel,
			string _time,
			int _bank,
			string _market_cap,
			Random _random,
			int x1,
			int x2,
			int x3,
			int x4,
			int x5)
		{
			random = _random;
			transactionNodes = new ArrayList();

			SetScreenInfo(_trade_name, _channel, _time, _bank, _market_cap);
			SetColumns(x1, x2, x3, x4, x5);
		}

		public void SetColumns (int x1, int x2, int x3, int x4, int x5)
		{
			this.x1 = x1;
			this.x2 = x2;
			this.x3 = x3;
			this.x4 = x4;
			this.x5 = x5;
		}

		public void SetScreenInfo(
			string _trade_name,
			string _channel,
			string _time,
			int _bank,
			string _market_cap)
		{
			trade_name = _trade_name;
			channel = GetDisplayableChannelName(_channel);
			time = _time;
			//banks = _banks;
			SetBank(_bank);

			tradeLength = 16;
			channelLength = 7;
			timeLength = 8;
			bankLength = 4;
			marketCapLength = 6;

			market_cap = _market_cap;
			// Default all random strings to full random.
			r_trade_name = new String ('^', tradeLength);
			r_channel = new String ('^', channelLength);
			r_time = new String ('^', timeLength);
			r_banks =  new String ('^', bankLength);
			r_market_cap = new String ('^', marketCapLength);
			// Set all display names to empty...
			d_trade_name = "";
			d_channel = "";
			d_time = "";
			d_banks = "";
			d_market_cap = "";
			//
			done_trade_name = false;
			done_channel = false;
			done_time = false;
			done_banks = false;
			done_market_cap = false;
			//
			// Build a random list of chars that will get corrected...
			//
			ArrayList nums = new ArrayList();
			int max_chars = tradeLength + channelLength + timeLength + bankLength + marketCapLength;
			for(int i=0; i<max_chars; ++i)
			{
				nums.Add(i);
			}

			correction_order = new ArrayList();

			while(nums.Count > 0)
			{
				int n = random.Next(nums.Count-1);
				correction_order.Add( nums[n] );
				nums.RemoveAt(n);
			}
		}

		// Returns true on finished jiggling.
		public bool Jiggle()
		{
			bool ret = false;

			if(!done_trade_name)
			{
				d_trade_name = Jiggle(r_trade_name, trade_name, out done_trade_name);
			}

			if(!done_channel)
			{
				d_channel = Jiggle(r_channel, channel, out done_channel);
			}

			if(!done_time)
			{
				d_time = Jiggle(r_time, time, out done_time);
			}

			if(!done_banks)
			{
				d_banks = Jiggle(r_banks, banks, out done_banks);
			}

			if(!done_market_cap)
			{
				d_market_cap = Jiggle(r_market_cap, market_cap, out done_market_cap);
			}

			if(correction_order.Count == 0) ret = true;

			CorrectOneChar();

			return ret;
		}

		protected string ReplaceChar(string orig, int offset, string target)
		{
			char newChar = ' ';

			if(offset < target.Length)
			{
				newChar = target[offset];
			}

			string str = "";
			if(offset == 0)
			{
				str += newChar;
				str += orig.Substring(1,orig.Length-1);
			}
			else
			{
				str = orig.Substring(0,offset);
				str += newChar;
				str += orig.Substring(offset+1,orig.Length-offset-1);
			}
			return str;
		}

		protected void CorrectOneChar()
		{
			if(correction_order.Count == 0) return;

			int cchar = (int)correction_order[correction_order.Count-1];
			correction_order.RemoveAt( correction_order.Count-1 );

			if(cchar < tradeLength) // trade name
			{
				r_trade_name = ReplaceChar(r_trade_name, cchar, trade_name);//[cchar]);
			}
			else if(cchar < tradeLength+channelLength) // channel
			{
				r_channel = ReplaceChar(r_channel, cchar-tradeLength, channel);//[cchar-16]);
			}
			else if(cchar < tradeLength+channelLength+timeLength) // time
			{
				r_time = ReplaceChar(r_time, cchar-(tradeLength+channelLength), time);//[cchar-16-7]);
			}
			else if(cchar < tradeLength+channelLength+timeLength+bankLength) // banks
			{
				r_banks = ReplaceChar(r_banks, cchar-(tradeLength+channelLength+timeLength), banks);//[cchar-16-7-5]);
			}
			else
			{
				r_market_cap = ReplaceChar(r_market_cap, cchar-(tradeLength+channelLength+timeLength+bankLength), market_cap);//[cchar-16-7-5-4]);
			}
		}

		protected string Jiggle(string r_current, string target, out bool done)
		{
			done = true;

			string str = "";
			char nc;

			foreach(char c in r_current)
			{
				if(c == '^')
				{
					done = false;
					nc = (char) (( (int) 'A' ) + random.Next(26));
				}
				else nc = c;

				str += nc;
			}

			return str;
		}

		protected SolidBrush GetBrushFromBankStatus(BankStatus status, int row)
		{
			if ((row % 2) == 1)
			{
				switch (status)
				{
					case BankStatus.Cancelled:
						return cancelledBrush_alternate;

					case BankStatus.Handled:
						return handledBrush_alternate;

					case BankStatus.Delayed:
						return delayedBrush_alternate;

					default:
						return queuedBrush_alternate;
				}
			}
			else
			{
				switch (status)
				{
					case BankStatus.Cancelled:
						return  cancelledBrush;

					case BankStatus.Handled:
						return handledBrush;

					case BankStatus.Delayed:
						return delayedBrush;

					default:
						return queuedBrush;
				}
			}
		}

		protected string GetDisplayableChannelName (string channelName)
		{
			string upper = channelName.ToUpper();

			switch (upper)
			{
				case "INSTORE":
					return "FLOOR";
			}

			return upper;
		}

		public void Paint(Graphics g, Font font, int yoffset, int width, int height, int row)
		{
			Brush brush = GetBrushFromBankStatus(BankStatus.Queued, row);
			g.FillRectangle(brush, 0, yoffset, width, height);

			DrawTextWithStatus(g, StockBoard.x1, yoffset, StockBoard.x2 - StockBoard.x1, height, row, font, d_trade_name, BankStatus.Queued);
			DrawTextWithStatus(g, StockBoard.x2, yoffset, StockBoard.x3 - StockBoard.x2, height, row, font, GetDisplayableChannelName(d_channel), BankStatus.Queued);
			DrawTextWithStatus(g, StockBoard.x3, yoffset, StockBoard.x4 - StockBoard.x3, height, row, font, d_time, BankStatus.Queued);

			if(d_banks.Length >= 4)
			{
				DrawTextWithStatus(g, StockBoard.x4,      yoffset, 15, height, row, font, d_banks.Substring(0,1), this.bank1_status);
				DrawTextWithStatus(g, StockBoard.x4 + 15, yoffset, 15, height, row, font, d_banks.Substring(1,1), this.bank2_status);
				DrawTextWithStatus(g, StockBoard.x4 + 30, yoffset, 15, height, row, font, d_banks.Substring(2,1), this.bank3_status);
				DrawTextWithStatus(g, StockBoard.x4 + 45, yoffset, 18, height, row, font, d_banks.Substring(3,1), this.bank4_status);
			}
		}

		void DrawTextWithStatus (Graphics g, int x, int y, int w, int h, int row, Font font, string text, BankStatus status)
		{
			Brush fore = Brushes.Black;
			Brush back = GetBrushFromBankStatus(status, row);

			switch (status)
			{
				case BankStatus.Cancelled:
				case BankStatus.Delayed:
					fore = Brushes.White;
					break;
			}

			g.FillRectangle(back, x, y, w, h);
			g.DrawString(text, font, fore, x, y);
		}
	}
}