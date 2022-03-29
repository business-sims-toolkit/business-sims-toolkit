using System;
using System.Drawing;
using System.Windows.Forms;

using System.Collections;
using LibCore;
using CoreUtils;
using Network;
using CommonGUI;

namespace DiscreteSimGUI
{
	/// <summary>
	/// This is full transaction view for CA Security
	/// It shows booth Online and Call Centre items in the same window
	/// </summary>
	public class CASM_FullTransactionView : FlickerFreePanel
	{
		protected StopControlledTimer _timer = new StopControlledTimer();

		protected Color TransactionColor_Cancelled = Color.Orange;
		protected Color TransactionColor_Handled = Color.LawnGreen;
		protected Color TransactionColor_Delayed = Color.Yellow;
		protected Color TransactionColor_Queued = Color.White;

		protected Color TransactionBackColor_Cancelled = Color.Transparent;
		protected Color TransactionBackColor_Handled = Color.Transparent;
		protected Color TransactionBackColor_Delayed = Color.Transparent;
		protected Color TransactionBackColor_Queued = Color.Transparent;

		protected Hashtable currentTransactionsbySequence = new Hashtable();
		protected Hashtable transtypenames = new Hashtable();
		protected Node TransactionsNode;
		protected Node currentTimeNode;

		protected int Col_Start_Day = 8;
		protected int Col_Start_Type = 50;
		protected int Col_Start_Region1 = 142;
		protected int Col_Start_Region2 = 212;
		protected int Col_Start_Region3 = 282;
		protected int Col_Start_Region4 = 352;

		protected Boolean IsTrainingGame = false;
		protected bool changed = false;
		protected bool auto_translate = true;
		protected Image background_Image = null;
		protected bool colourStatusOnly = false;
		protected int border = 1;
		protected bool padToRight = false;
		protected bool PaintBackground = true;
		protected Font dispfont_title = null;
		protected Font dispfont_values = null;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="model"></param>
		public CASM_FullTransactionView(NodeTree model, string filterName, Boolean IsTrainingFlag)
		{
			this.BackColor = Color.Transparent;
			IsTrainingGame = IsTrainingFlag;

			currentTimeNode = model.GetNamedNode("CurrentTime");

			TransactionsNode = model.GetNamedNode("Transactions");
			foreach (Node TransactionNode in TransactionsNode.getChildren())
			{
				AddTransaction(TransactionNode);
			}
			TransactionsNode.ChildAdded += TransactionsNode_ChildAdded;
			TransactionsNode.ChildRemoved += TransactionsNode_ChildRemoved;
			TransactionsNode.AttributesChanged += TransactionsNode_AttributesChanged;

			transtypenames.Add("online", "Online");
			transtypenames.Add("instore", "Call Center");

			BuildControls();
		}

		/// <summary>
		/// Dispose ...
		/// </summary>
		public new void Dispose()
		{
			ResetDataCaches();

			if (currentTimeNode != null)
			{
				currentTimeNode = null;
			}
			if (TransactionsNode != null)
			{
				TransactionsNode.ChildAdded -= TransactionsNode_ChildAdded;
				TransactionsNode.ChildRemoved -= TransactionsNode_ChildRemoved;
				TransactionsNode.AttributesChanged -= TransactionsNode_AttributesChanged;
			}
			TransactionsNode = null;
		}

		/// <summary>
		/// when the transactin posts count processed, we can 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="attrs"></param>
		protected void TransactionsNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			bool needs_handled = false;
			foreach (AttributeValuePair avp in attrs)
			{
				if (avp.Attribute == "count_processed")
				{
					needs_handled = true;
				}
			}
			if (needs_handled)
			{
				this.setChanged();
			}
		}

		void ResetDataCaches()
		{
			foreach( Node n in currentTransactionsbySequence.Values)
			{
				n.AttributesChanged -= TransactionNode_AttributesChanged;
			}
			currentTransactionsbySequence.Clear();
		}

		void AddTransaction(Node TransactionNode)
		{
			string name = TransactionNode.GetAttribute("name");
			string displayname = TransactionNode.GetAttribute("displayname");
			string event_type = TransactionNode.GetAttribute("event_type");
			int sequence = TransactionNode.GetIntAttribute("sequence",0);
			int time = TransactionNode.GetIntAttribute("time",0);
			string status = TransactionNode.GetAttribute("status");
			string store_id = TransactionNode.GetAttribute("store");

			if (currentTransactionsbySequence.Contains(sequence) == false)
			{
				currentTransactionsbySequence.Add(sequence, TransactionNode);
				TransactionNode.AttributesChanged +=TransactionNode_AttributesChanged;
				this.setChanged();
			}
		}

		void RemoveTransaction(Node TransactionNode)
		{
			string name = TransactionNode.GetAttribute("name");
			string displayname = TransactionNode.GetAttribute("displayname");
			string event_type = TransactionNode.GetAttribute("event_type");
			int sequence = TransactionNode.GetIntAttribute("sequence", 0);
			int time = TransactionNode.GetIntAttribute("time", 0);
			string status = TransactionNode.GetAttribute("status");
			string store_id = TransactionNode.GetAttribute("store");

			if (currentTransactionsbySequence.Contains(sequence))
			{
				TransactionNode.AttributesChanged -= TransactionNode_AttributesChanged;
				currentTransactionsbySequence.Remove(sequence);
				this.setChanged();
			}
		}

		void TransactionsNode_ChildRemoved(Node sender, Node child)
		{
		  RemoveTransaction(child);
		}

		void TransactionsNode_ChildAdded(Node sender, Node child)
		{
			AddTransaction(child);
		}

		void TransactionNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			bool needs_handled = false;
			foreach (AttributeValuePair avp in attrs)
			{
				if (avp.Attribute == "status")
				{
					string name = sender.GetAttribute("name");
					string displayname = sender.GetAttribute("displayname");
					string event_type = sender.GetAttribute("event_type");
					int sequence = sender.GetIntAttribute("sequence", 0);
					int time = sender.GetIntAttribute("time", 0);
					string status = sender.GetAttribute("status");
					string store_id = sender.GetAttribute("store");

					if (currentTransactionsbySequence.Contains(sequence))
					{
						needs_handled = true;
					}
				}
			}
			if (needs_handled)
			{
				this.setChanged();
			}
		}

		/// <summary>
		/// Position changed
		/// </summary>
		public void setChanged()
		{
			changed = true;
			if (!_timer.Enabled)// && !stopped)
			{
				_timer.Start();
			}
			this.Invalidate();
		}

		void _timer_Tick(object sender, EventArgs e)
		{
			if(!changed)
			{
				_timer.Stop();
			}
			else
			{
				RefreshDisplay();
				this.Invalidate();
			}
			changed = false;
		}

		public void BuildControls()
		{
			_timer.Interval = 1000;
			_timer.Tick += _timer_Tick;

			//Font dispfont_title = ConstantSizeFont.NewFont("Verdana",12f, FontStyle.Bold);
			//Font dispfont_values = ConstantSizeFont.NewFont("Verdana",9.5f, FontStyle.Bold);
			string font_name = SkinningDefs.TheInstance.GetData("fontname", "Trebuchet MS");
			dispfont_title = null;
			dispfont_values = null;


			if(auto_translate)
			{
				font_name = TextTranslator.TheInstance.GetTranslateFont(font_name);
				dispfont_title = ConstantSizeFont.NewFont(font_name, 10f, FontStyle.Bold);
				dispfont_values = ConstantSizeFont.NewFont(font_name, 8f, FontStyle.Bold);
			}
			else
			{
				if (IsTrainingGame == false)
				{
					dispfont_title = ConstantSizeFont.NewFont(font_name, 10f, FontStyle.Bold);
					dispfont_values = ConstantSizeFont.NewFont(font_name, 8f, FontStyle.Bold);
				}
				else
				{
					dispfont_title = ConstantSizeFont.NewFont(font_name, 10f, FontStyle.Regular);
					dispfont_values = ConstantSizeFont.NewFont(font_name, 8f, FontStyle.Regular);
				}
			}
		}

		public void ChangePresentationColours(Color BackgroundColor, Color TextForeColor)
		{
			//this.BackColor = BackgroundColor;
			//if (ShowTitle)
			//{
			//  ViewTitle.BackColor = BackgroundColor;
			//  ViewTitle.ForeColor = TextForeColor;
			//}
			//for (int step=0; step < DisplayNumber; step++)
			//{
			//  TransactionTimeSlot[step].BackColor = BackgroundColor;
			//  TransactionTimeSlot[step].ForeColor = TextForeColor;

			//  TransactionCodeSlot[step].BackColor = BackgroundColor;
			//  TransactionCodeSlot[step].ForeColor = TextForeColor;

			//  TransactionStoreSlot[step].BackColor = BackgroundColor;
			//  TransactionStoreSlot[step].ForeColor = TextForeColor;

			//  TransactionStatusSlot[step].BackColor = BackgroundColor;
			//  TransactionStatusSlot[step].ForeColor = TextForeColor;
			//}
		}

		public void ChangeStatusColours(Color StatusColourCancelled, Color StatusColourHandled,
			Color StatusColourDelayed, Color StatusColourQueued)
		{
			//TransactionColor_Cancelled = StatusColourCancelled;	//Normal was Orange;
			//TransactionColor_Handled = StatusColourHandled;			//handled was Green;
			//TransactionColor_Delayed = StatusColourDelayed;			//Delayed was Yellow;
			//TransactionColor_Queued = StatusColourQueued;				//Normal was White;
		}

		public void ChangeStatusColours (Color StatusColourCancelled, Color StatusColourHandled,
		                                 Color StatusColourDelayed, Color StatusColourQueued,
		                                 Color StatusBackColourCancelled, Color StatusBackColourHandled,
		                                 Color StatusBackColourDelayed, Color StatusBackColourQueued,
		                                 bool colourStatusOnly)
		{
			//TransactionColor_Cancelled = StatusColourCancelled;	//Normal was Orange;
			//TransactionColor_Handled = StatusColourHandled;			//handled was Green;
			//TransactionColor_Delayed = StatusColourDelayed;			//Delayed was Yellow;
			//TransactionColor_Queued = StatusColourQueued;				//Normal was White;

			//TransactionBackColor_Cancelled = StatusBackColourCancelled;
			//TransactionBackColor_Handled = StatusBackColourHandled;
			//TransactionBackColor_Delayed = StatusBackColourDelayed;
			//TransactionBackColor_Queued = StatusBackColourQueued;

			//this.colourStatusOnly = colourStatusOnly;
		}

		string BuildTimeString(int secs)
		{
			int time = (secs / 60);
			string time_str = "14:";
			if (time<10)
			{
				time_str += "0";
			}
			time_str += CONVERT.ToStr(time);
			return time_str;
		}

		public string MapTypeStringToDisplay(string transtype)
		{
			string translated = transtype;

			if (transtypenames.ContainsKey(transtype.ToLower()))
			{
				translated = (string)transtypenames[transtype.ToLower()];
			}
			return translated;
		}

		public string MapStatusStringToDisplay (string status)
		{
			string token = "status_name_" + status.ToLower();
			string translated = status;

			if (auto_translate)
			{
				//skin based translation
				string tmpReplacement = SkinningDefs.TheInstance.GetData(token);
				if (tmpReplacement != "")
				{
					translated = tmpReplacement;
				}
				//Then Language based Translation 
				translated = TextTranslator.TheInstance.Translate(translated);
			}
			else
			{
				string tmpReplacement = SkinningDefs.TheInstance.GetData(token);
				if (tmpReplacement != "")
				{
					translated = tmpReplacement;
				}
			}
			return translated;
		}

		void RefreshDisplay()
		{
			this.Refresh();
		}

		void Render_Transactions(Graphics g)
		{
			if (PaintBackground == true)
			{
				if (background_Image != null)
				{
					g.DrawImage(background_Image, 0, 0);
				}
			}
			//now draw the titles 
			g.DrawString("Day", dispfont_title, Brushes.White, Col_Start_Day-2, 0);
			g.DrawString("Type", dispfont_title, Brushes.White, Col_Start_Type, 0);
			g.DrawString("Region 1", dispfont_title, Brushes.White, Col_Start_Region1, 0);
			g.DrawString("Region 2", dispfont_title, Brushes.White, Col_Start_Region2, 0);
			g.DrawString("Region 3", dispfont_title, Brushes.White, Col_Start_Region3, 0);
			g.DrawString("Region 4", dispfont_title, Brushes.White, Col_Start_Region4, 0);

			int trans_count = 0;
			int row_stepper = 0;
			int row_number = -1;
			int offset_x = 0;
			int offset_y = 0;
			int number_of_display_rows = 6;
			bool drawHighlightBack = false;

			int global_time = currentTimeNode.GetIntAttribute("seconds", 0);
			int global_minute = (global_time / 60) + 1;



			for (int step = 1; step < 97; step++)
			{
				if (currentTransactionsbySequence.ContainsKey(step))
				{
					trans_count++;
					if (trans_count < ((number_of_display_rows * 4) + 1))
					{
						Node tn = (Node)currentTransactionsbySequence[step];
						if (tn != null)
						{
							string name = tn.GetAttribute("name");
							string displayname = tn.GetAttribute("displayname");
							string event_type = tn.GetAttribute("event_type");
							int sequence = tn.GetIntAttribute("sequence", 0);
							int time = tn.GetIntAttribute("time", 0);
							int trans_time_minute = time / 60;
							string status = tn.GetAttribute("status");
							string store_id = tn.GetAttribute("store");

							row_stepper = (trans_count - 1) / 4;
							offset_y = (row_stepper) * 22 + 25;

							if (row_stepper != row_number)
							{
								if (trans_time_minute == global_minute)
								{
									g.FillRectangle(Brushes.LightBlue, Col_Start_Day + 3-6, offset_y, 405, 20);
								}
								g.DrawString(CONVERT.ToStr(trans_time_minute), dispfont_title, Brushes.Black, Col_Start_Day + 3, offset_y);
								g.DrawString(MapTypeStringToDisplay(event_type), dispfont_title, Brushes.Black, Col_Start_Type, offset_y);
								row_number = row_stepper;
							}

							bool region1 = store_id.IndexOf('1') > -1;
							bool region2 = store_id.IndexOf('2') > -1;
							bool region3 = store_id.IndexOf('3') > -1;
							bool region4 = store_id.IndexOf('4') > -1;

							offset_x = Col_Start_Region1;
							if (region2)
							{
								offset_x = Col_Start_Region2;
							}
							if (region3)
							{
								offset_x = Col_Start_Region3;
							}
							if (region4)
							{
								offset_x = Col_Start_Region4;
							}
							if (drawHighlightBack)
							{
								bool cancelled = (status.ToLower().IndexOf("canceled") > -1) || (status.ToLower().IndexOf("cancelled") > -1);
								bool handled = status.ToLower().IndexOf("handled") > -1;
								bool delayed = (status.ToLower().IndexOf("at risk") > -1) || (status.ToLower().IndexOf("delayed") > -1);
								bool queued = status.ToLower().IndexOf("queued") > -1;

								if (cancelled)
								{

								}
								if (handled)
								{

								}
								if (delayed)
								{

								}
							}
							g.DrawString(MapStatusStringToDisplay(status), dispfont_title, Brushes.Black, offset_x, offset_y);
						}
					}
				}
			}
		}

		/// <summary>
		/// Override on Paint
		/// </summary>
		/// <param name="e"></param>
		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			Render_Transactions(g);
		}

	}
}