using System;
using System.Drawing;
using System.Windows.Forms;

using System.Collections;
using LibCore;
using CoreUtils;
using Network;

namespace DiscreteSimGUI
{
	/// <summary>
	/// Summary description for TransactionView
	/// </summary>
	public class TransactionView : BasePanel
	{
		// : 29-4-2008 - Take some of the logic from the F1 game so that the view odes not try to over update.
		StopControlledTimer _timer = new StopControlledTimer();

		Color TransactionColor_Cancelled = Color.Orange;
		Color TransactionColor_Handled = Color.LawnGreen;
		Color TransactionColor_Delayed = Color.Yellow;
		Color TransactionColor_Queued = Color.White;

		Color TransactionBackColor_Cancelled = Color.Transparent;
		Color TransactionBackColor_Handled = Color.Transparent;
		Color TransactionBackColor_Delayed = Color.Transparent;
		Color TransactionBackColor_Queued = Color.Transparent;

		bool colourStatusOnly = false;

		int border = 1;
		public int Border
		{
			get
			{
				return border;
			}

			set
			{
				border = value;
				OnResize(new EventArgs ());
			}
		}

		bool padToRight = false;
		public bool PadToRight
		{
			get
			{
				return padToRight;
			}

			set
			{
				padToRight = value;
				OnResize(new EventArgs ());
			}
		}

		int TimeLabelLength = SkinningDefs.TheInstance.GetIntData("esm_transactions_time_label_length", 48);
		int CodeLabelLength = SkinningDefs.TheInstance.GetIntData("esm_transactions_code_label_length", 60);
		int StoreLabelLength = SkinningDefs.TheInstance.GetIntData("esm_transactions_store_label_length", 16);
		int StatusLabelLength = SkinningDefs.TheInstance.GetIntData("esm_transactions_status_label_length", 78);
		int DisplayNumber = SkinningDefs.TheInstance.GetIntData("esm_number_of_transactions_to_display", 8);
		int RowHeight = SkinningDefs.TheInstance.GetIntData("transactionview_rowheight", 17);

		Label ViewTitle;
		Node TransactionsNode;

		Label[] TransactionTimeSlot = null;
		Label[] TransactionCodeSlot = null;
		Label[] TransactionStoreSlot = null;
		Label[] TransactionStatusSlot = null;

		Hashtable SequenceToDispName = new Hashtable();
		ArrayList TransactionNodes = new ArrayList();

		string MyFilterName = string.Empty;
		Boolean ShowTitle = false;
		Boolean IsTrainingGame = false;

		protected bool changed = false;
		bool auto_translate = true;

		NodeTree model;
		Node timeNode;

		int numberOfTransactionsToDisplay = 4;
         
	    int business = -1;

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

		/// <summary>
		/// Position changed
		/// </summary>
		public bool Changed
		{
			set
			{
				changed = value;
				if(!_timer.Enabled)// && !stopped)
				{
					_timer.Start();
				}
				this.Invalidate();
			}
		}

		public void BuildControls()
		{
			_timer.Interval = 1000;
			_timer.Tick += _timer_Tick;

			//Font dispfont_title = ConstantSizeFont.NewFont("Verdana",12f, FontStyle.Bold);
			//Font dispfont_values = ConstantSizeFont.NewFont("Verdana",9.5f, FontStyle.Bold);
			string font_name = SkinningDefs.TheInstance.GetData("fontname", "Trebuchet MS");
			Font dispfont_title = null;
			Font dispfont_values = null;


			if(auto_translate)
			{
				font_name = TextTranslator.TheInstance.GetTranslateFont(font_name);
				dispfont_title = ConstantSizeFont.NewFont(font_name,12f, FontStyle.Bold);
				dispfont_values = ConstantSizeFont.NewFont(font_name,9.5f, FontStyle.Bold);
			}
			else
			{
				if (IsTrainingGame == false)
				{
					dispfont_title = ConstantSizeFont.NewFont(font_name,12f, FontStyle.Bold);
					dispfont_values = ConstantSizeFont.NewFont(font_name,9.5f, FontStyle.Bold);
				}
				else
				{
					dispfont_title = ConstantSizeFont.NewFont(font_name,8f, FontStyle.Regular);
					dispfont_values = ConstantSizeFont.NewFont(font_name,8f, FontStyle.Regular);
				}
			}
			
			int yoffset =	0;
		
			if (ShowTitle)
			{
				ViewTitle = new Label();
				ViewTitle.BackColor = this.BackColor;
				ViewTitle.ForeColor = Color.Yellow;
				ViewTitle.TextAlign = ContentAlignment.MiddleCenter;
				ViewTitle.Font = dispfont_title;
				ViewTitle.Size = new Size(120,18);
				ViewTitle.Location = new Point(0,0);
				this.Controls.Add(ViewTitle);
				yoffset = ViewTitle.Height - 1;
			}
			
			for (int step=0; step < DisplayNumber; step++)
			{
					TransactionTimeSlot[step] = new Label();
					TransactionTimeSlot[step].BackColor = this.BackColor;
					//TransactionTimeSlot[step].BackColor = Color.Magenta;
					TransactionTimeSlot[step].ForeColor = Color.White;
					TransactionTimeSlot[step].TextAlign = ContentAlignment.MiddleLeft;
					TransactionTimeSlot[step].Font = dispfont_values;
					TransactionTimeSlot[step].Size = new Size(TimeLabelLength,16);
					TransactionTimeSlot[step].Location = new Point(0,(RowHeight*step+1)+yoffset);
					this.Controls.Add(TransactionTimeSlot[step]);			

					TransactionCodeSlot[step] = new Label();
					TransactionCodeSlot[step].BackColor = this.BackColor;
					//TransactionCodeSlot[step].BackColor = Color.Magenta;
					TransactionCodeSlot[step].ForeColor = Color.White;
					TransactionCodeSlot[step].TextAlign = ContentAlignment.MiddleRight;
					TransactionCodeSlot[step].Font = dispfont_values;
					TransactionCodeSlot[step].Size = new Size(CodeLabelLength,16);
					TransactionCodeSlot[step].Location = new Point(0+TimeLabelLength,(RowHeight*step+1)+yoffset);
					this.Controls.Add(TransactionCodeSlot[step]);			

					TransactionStoreSlot[step] = new Label();
					TransactionStoreSlot[step].BackColor = this.BackColor;
					TransactionStoreSlot[step].ForeColor = Color.White;
					TransactionStoreSlot[step].TextAlign = ContentAlignment.MiddleLeft;
					TransactionStoreSlot[step].Font = dispfont_values;
					TransactionStoreSlot[step].Size = new Size(StoreLabelLength,16);
					TransactionStoreSlot[step].Location = new Point(0+TimeLabelLength+CodeLabelLength,(RowHeight*step+1)+yoffset);
					this.Controls.Add(TransactionStoreSlot[step]);			

					TransactionStatusSlot[step] = new Label();
					TransactionStatusSlot[step].BackColor = this.BackColor;
					//TransactionStatusSlot[step].BackColor = Color.Magenta;
					TransactionStatusSlot[step].ForeColor = Color.White;
					TransactionStatusSlot[step].TextAlign = ContentAlignment.MiddleLeft;
					TransactionStatusSlot[step].Font = dispfont_values;
					TransactionStatusSlot[step].Size = new Size(StatusLabelLength,16);
					TransactionStatusSlot[step].Location = new Point(0+TimeLabelLength+CodeLabelLength+StoreLabelLength,(RowHeight*step+1)+yoffset);
					this.Controls.Add(TransactionStatusSlot[step]);			
			}
		}

		public void ChangePresentationColours(Color BackgroundColor, Color TextForeColor)
		{
			this.BackColor = BackgroundColor;
			if (ShowTitle)
			{
				ViewTitle.BackColor = BackgroundColor;
				ViewTitle.ForeColor = TextForeColor;
			}
			for (int step=0; step < DisplayNumber; step++)
			{
				TransactionTimeSlot[step].BackColor = BackgroundColor;
				TransactionTimeSlot[step].ForeColor = TextForeColor;

				TransactionCodeSlot[step].BackColor = BackgroundColor;
				TransactionCodeSlot[step].ForeColor = TextForeColor;

			    TransactionStoreSlot[step].BackColor = BackgroundColor;
				TransactionStoreSlot[step].ForeColor = TextForeColor;

				TransactionStatusSlot[step].BackColor = BackgroundColor;
				TransactionStatusSlot[step].ForeColor = TextForeColor;
			}
		}

		public void ChangeStatusColours(Color StatusColourCancelled, Color StatusColourHandled,
			Color StatusColourDelayed, Color StatusColourQueued)
		{
			TransactionColor_Cancelled = StatusColourCancelled;	//Normal was Orange;
			TransactionColor_Handled = StatusColourHandled;			//handled was Green;
			TransactionColor_Delayed = StatusColourDelayed;			//Delayed was Yellow;
			TransactionColor_Queued = StatusColourQueued;				//Normal was White;
		}

		public void ChangeStatusColours (Color StatusColourCancelled, Color StatusColourHandled,
		                                 Color StatusColourDelayed, Color StatusColourQueued,
		                                 Color StatusBackColourCancelled, Color StatusBackColourHandled,
		                                 Color StatusBackColourDelayed, Color StatusBackColourQueued,
		                                 bool colourStatusOnly)
		{
			TransactionColor_Cancelled = StatusColourCancelled;	//Normal was Orange;
			TransactionColor_Handled = StatusColourHandled;			//handled was Green;
			TransactionColor_Delayed = StatusColourDelayed;			//Delayed was Yellow;
			TransactionColor_Queued = StatusColourQueued;				//Normal was White;

			TransactionBackColor_Cancelled = StatusBackColourCancelled;
			TransactionBackColor_Handled = StatusBackColourHandled;
			TransactionBackColor_Delayed = StatusBackColourDelayed;
			TransactionBackColor_Queued = StatusBackColourQueued;

			this.colourStatusOnly = colourStatusOnly;
		}

	    public TransactionView (NodeTree model, int businessNumber, Boolean isTrainingFlag)
	    {
            IsTrainingGame = isTrainingFlag;

            this.model = model;
            business = businessNumber;

            timeNode = model.GetNamedNode("CurrentTime");
            timeNode.AttributesChanged += AttributesChangedOnTimeNode;

            RowHeight = SkinningDefs.TheInstance.GetIntData("transactionview_rowheight", 17);

	        TransactionTimeSlot = new Label[numberOfTransactionsToDisplay];
            TransactionCodeSlot = new Label[numberOfTransactionsToDisplay];
            TransactionStoreSlot = new Label[numberOfTransactionsToDisplay];
            TransactionStatusSlot = new Label[numberOfTransactionsToDisplay];
	        DisplayNumber = numberOfTransactionsToDisplay;

            TimeLabelLength = 40;  
	        CodeLabelLength = 60;
	        StoreLabelLength = 50;  
	        StatusLabelLength = 70; 

            BackColor = Color.Black;

            //Build the controls 
            BuildControls();
            //connect up the Transactions node
            TransactionsNode = model.GetNamedNode("Transactions");
            TransactionsNode.ChildAdded += ChildAddedToTransactionNodeBasedOnBusiness;
            TransactionsNode.ChildRemoved += ChildRemovedFromTransactionNodeBasedOnBusiness;

            FilterBasedOnBusiness(businessNumber);

            //this.Resize += new EventHandler(TransactionView_Resize);
	    }

	    /// <summary>
		/// Show Availability / Revenue
		/// </summary>
		/// <param name="model"></param>
		public TransactionView(NodeTree model, string filterName, Boolean IsTrainingFlag)
		{
            Color backColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("esm_main_screen_background_colour", Color.Black);
			
			IsTrainingGame = IsTrainingFlag;

			this.model = model;

			timeNode = model.GetNamedNode("CurrentTime");
			timeNode.AttributesChanged += timeNode_AttributesChanged;

			RowHeight = SkinningDefs.TheInstance.GetIntData("transactionview_rowheight", 17);

			TransactionTimeSlot = new Label[DisplayNumber];
			TransactionCodeSlot = new Label[DisplayNumber];
			TransactionStoreSlot = new Label[DisplayNumber];
			TransactionStatusSlot = new Label[DisplayNumber];

    		if (filterName.IndexOf("line")>-1)
			{
				//this.BackColor = Color.Aqua;
				this.BackColor = backColor;
			}
			else
			{
				//this.BackColor = Color.Peru;
                this.BackColor = backColor;
			}
	
			//Build the controls 
			BuildControls();
			//connect up the Transactions node
			TransactionsNode = model.GetNamedNode("Transactions");
			TransactionsNode.ChildAdded +=TransactionsNode_ChildAdded;
			TransactionsNode.ChildRemoved +=TransactionsNode_ChildRemoved;

			if (ShowTitle)
			{
				ViewTitle.Text = filterName;
			}
			SetFilter(filterName);

			this.Resize += TransactionView_Resize;
            DoubleBuffered = true;
		}

	    /// <summary>
		/// Dispose ...
		/// </summary>
		public new void Dispose()
		{
			timeNode.AttributesChanged -= timeNode_AttributesChanged;
			ResetDataCaches();

			base.Dispose();
		}

		void TransactionView_Resize(object sender, EventArgs e)
		{
			if (ShowTitle)
			{
				ViewTitle.Left = 5;
				ViewTitle.Width = this.Width - 10;
			}

			for (int step=0; step < DisplayNumber; step++)
			{
				TransactionTimeSlot[step].Left = Border;
				TransactionTimeSlot[step].Width = TimeLabelLength;

				TransactionCodeSlot[step].Left = TransactionTimeSlot[step].Left + TransactionTimeSlot[step].Width + Border;
				TransactionCodeSlot[step].Width = CodeLabelLength;

				TransactionStoreSlot[step].Left = TransactionCodeSlot[step].Left + TransactionCodeSlot[step].Width + Border;
				TransactionStoreSlot[step].Width = StoreLabelLength;

				TransactionStatusSlot[step].Left = 	TransactionStoreSlot[step].Left + TransactionStoreSlot[step].Width + Border;
				if (padToRight)
				{
					StatusLabelLength = Width - TransactionStatusSlot[step].Left;
				}
				TransactionStatusSlot[step].Width = StatusLabelLength;
			}
		}

		string BuildTimeString (int secs)
		{
			if (timeNode.GetBooleanAttribute("show_world_time", false))
			{
				DateTime effectiveTime = timeNode.GetDateTimeAttribute("world_time_at_zero_seconds", DateTime.Now)
										 + TimeSpan.FromSeconds(secs);

				// Round to the nearest minute.
				if (effectiveTime.Second >= 30)
				{
					effectiveTime = effectiveTime.AddSeconds(60 - effectiveTime.Second);
				}
				else
				{
					effectiveTime = effectiveTime.Subtract(new TimeSpan (0, 0, effectiveTime.Second));
				}

				return CONVERT.Format("{0:00}:{1:00}", effectiveTime.Hour, effectiveTime.Minute);
			}
			else
			{
                string timeString = SkinningDefs.TheInstance.GetData("show_hour", "14");
			    if (SkinningDefs.TheInstance.GetBoolData("esm_sim", false))
			    {
			        int startTime = Int32.Parse(timeString);
                    int hours = secs / 60;
			        int mins = secs % 60;
			        String timeStr = String.Format("{0:00}:{1:00}", (startTime + hours), mins);
                    return timeStr;
			    }
			    else
			    {
			        int time = (secs / 60);
			        string time_str = timeString + ":";
			        if (time < 10)
			        {
			            time_str += "0";
			        }
			        time_str += CONVERT.ToStr(time);
                    return time_str;
			    }
			}
		}

		void ResetDataCaches()
		{
			SequenceToDispName.Clear();
			foreach( Node n in TransactionNodes)
			{
				n.AttributesChanged -= TransactionNode_AttributesChanged;
			}
			TransactionNodes.Clear();
		}

		void ClearDataCaches()
        {
            SequenceToDispName.Clear();
            foreach (Node n in TransactionNodes)
            {
                n.AttributesChanged -= AttributesChangedInTransactionNode;
            }
            TransactionNodes.Clear();
        }

		void AddTransaction(Node TransactionNode)
		{
			string name = TransactionNode.GetAttribute("name");
			string displayname = TransactionNode.GetAttribute("displayname");
			string event_type = TransactionNode.GetAttribute("event_type");
			int sequence = TransactionNode.GetIntAttribute("sequence",0);
			int time = TransactionNode.GetIntAttribute("time",0);
			string status = TransactionNode.GetAttribute("status");
		    string store_id = GetTransactionStoreLabel(TransactionNode);

			TransactionNodes.Add(TransactionNode);
			TransactionNode.AttributesChanged += TransactionNode_AttributesChanged;

		    string dispstr;
		    if (SkinningDefs.TheInstance.GetBoolData("esm_sim", false))
		    {
                dispstr = store_id + "," + BuildTimeString(time) + "," + status + "," + displayname;
		    }
		    else
		    {
		        dispstr = BuildTimeString(time) + "," + displayname + "," + status + "," + store_id;
		    }

		    SequenceToDispName.Add(sequence, dispstr);

			//RefreshDisplay();
			this.Changed = true;
		}

		void InsertTransaction(Node TransactionNode)
        {
            string name = TransactionNode.GetAttribute("name");
            string displayname = TransactionNode.GetAttribute("displayname");
            string event_type = TransactionNode.GetAttribute("event_type");
            int sequence = TransactionNode.GetIntAttribute("sequence", 0);
            int time = TransactionNode.GetIntAttribute("time", 0);
            string status = TransactionNode.GetAttribute("status");
            string store_id = GetTransactionStoreLabel(TransactionNode);

            TransactionNodes.Add(TransactionNode);
            TransactionNode.AttributesChanged += AttributesChangedInTransactionNode;

            string dispstr;
            if (SkinningDefs.TheInstance.GetBoolData("esm_sim", false))
            {
                dispstr = store_id + "," + BuildTimeString(time) + "," + status + "," + displayname;
            }
            else
            {
                dispstr = BuildTimeString(time) + "," + displayname + "," + status + "," + store_id;
            }

            SequenceToDispName.Add(sequence, dispstr);

            //RefreshDisplay();
            this.Changed = true;
        }

		void RemoveTransaction(Node TransactionNode)
		{
			string name = TransactionNode.GetAttribute("name");
			string displayname = TransactionNode.GetAttribute("displayname");
			string event_type = TransactionNode.GetAttribute("event_type");
			int sequence = TransactionNode.GetIntAttribute("sequence",0);
			int time = TransactionNode.GetIntAttribute("time",0);
			string status = TransactionNode.GetAttribute("status");

			TransactionNode.AttributesChanged -= TransactionNode_AttributesChanged;
			SequenceToDispName.Remove(sequence);
			TransactionNodes.Remove(TransactionNode);
		
			//RefreshDisplay();
			this.Changed = true;
		}

		void DeleteTransaction(Node TransactionNode)
        {
            string name = TransactionNode.GetAttribute("name");
            string displayname = TransactionNode.GetAttribute("displayname");
            string event_type = TransactionNode.GetAttribute("event_type");
            int sequence = TransactionNode.GetIntAttribute("sequence", 0);
            int time = TransactionNode.GetIntAttribute("time", 0);
            string status = TransactionNode.GetAttribute("status");

            TransactionNode.AttributesChanged -= AttributesChangedInTransactionNode;
            SequenceToDispName.Remove(sequence);
            TransactionNodes.Remove(TransactionNode);

            //RefreshDisplay();
            this.Changed = true;
        }

		void RefreshDisplay()
		{
			string[] parts = null;

			try 
			{
				ArrayList al = new ArrayList();
	
				//Clear 
				for (int step1=0; step1 < DisplayNumber; step1++)
				{
					TransactionTimeSlot[step1].Text = String.Empty;
					TransactionCodeSlot[step1].Text = String.Empty;
					TransactionStoreSlot[step1].Text = String.Empty;
					TransactionStatusSlot[step1].Text = String.Empty;

					TransactionTimeSlot[step1].BackColor = Color.Transparent;
					TransactionCodeSlot[step1].BackColor = Color.Transparent;
					TransactionStoreSlot[step1].BackColor = Color.Transparent;
					TransactionStatusSlot[step1].BackColor = Color.Transparent;
				}

				foreach (int v in SequenceToDispName.Keys)
				{
					al.Add(v);
				}
				al.Sort();

				int step = 0;
				foreach (int seq in al)
				{
                    if (SequenceToDispName.ContainsKey(seq))
                    {
                        string dispstr = (string)SequenceToDispName[seq];
                        if (step < TransactionTimeSlot.Length)
                        {
                            Boolean cancelled = (dispstr.ToLower().IndexOf("canceled") > -1) || (dispstr.ToLower().IndexOf("cancelled") > -1);
                            Boolean handled = dispstr.ToLower().IndexOf("handled") > -1;
                            Boolean delayed = (dispstr.ToLower().IndexOf("at risk") > -1) || (dispstr.ToLower().IndexOf("delayed") > -1);
                            Boolean queued = dispstr.ToLower().IndexOf("queued") > -1;
                            if (cancelled)
                            {
                                if (colourStatusOnly)
                                {
                                    TransactionTimeSlot[step].ForeColor = TransactionColor_Queued;
                                    TransactionCodeSlot[step].ForeColor = TransactionColor_Queued;
                                    TransactionStoreSlot[step].ForeColor = TransactionColor_Queued;
                                    TransactionStatusSlot[step].ForeColor = TransactionColor_Cancelled;

                                    TransactionTimeSlot[step].BackColor = TransactionBackColor_Queued;
                                    TransactionCodeSlot[step].BackColor = TransactionBackColor_Queued;
                                    TransactionStoreSlot[step].BackColor = TransactionBackColor_Queued;
                                    TransactionStatusSlot[step].BackColor = TransactionBackColor_Cancelled;
                                }
                                else
                                {
                                    TransactionTimeSlot[step].ForeColor = TransactionColor_Cancelled;
                                    TransactionCodeSlot[step].ForeColor = TransactionColor_Cancelled;
                                    TransactionStoreSlot[step].ForeColor = TransactionColor_Cancelled;
                                    TransactionStatusSlot[step].ForeColor = TransactionColor_Cancelled;

                                    TransactionTimeSlot[step].BackColor = TransactionBackColor_Cancelled;
                                    TransactionCodeSlot[step].BackColor = TransactionBackColor_Cancelled;
                                    TransactionStoreSlot[step].BackColor = TransactionBackColor_Cancelled;
                                    TransactionStatusSlot[step].BackColor = TransactionBackColor_Cancelled;
                                }
                            }
                            if (handled)
                            {
                                if (colourStatusOnly)
                                {
                                    TransactionTimeSlot[step].ForeColor = TransactionColor_Queued;
                                    TransactionCodeSlot[step].ForeColor = TransactionColor_Queued;
                                    TransactionStoreSlot[step].ForeColor = TransactionColor_Queued;
                                    TransactionStatusSlot[step].ForeColor = TransactionColor_Handled;

                                    TransactionTimeSlot[step].BackColor = TransactionBackColor_Queued;
                                    TransactionCodeSlot[step].BackColor = TransactionBackColor_Queued;
                                    TransactionStoreSlot[step].BackColor = TransactionBackColor_Queued;
                                    TransactionStatusSlot[step].BackColor = TransactionBackColor_Handled;
                                }
                                else
                                {
                                    TransactionTimeSlot[step].ForeColor = TransactionColor_Handled;
                                    TransactionCodeSlot[step].ForeColor = TransactionColor_Handled;
                                    TransactionStoreSlot[step].ForeColor = TransactionColor_Handled;
                                    TransactionStatusSlot[step].ForeColor = TransactionColor_Handled;

                                    TransactionTimeSlot[step].BackColor = TransactionBackColor_Handled;
                                    TransactionCodeSlot[step].BackColor = TransactionBackColor_Handled;
                                    TransactionStoreSlot[step].BackColor = TransactionBackColor_Handled;
                                    TransactionStatusSlot[step].BackColor = TransactionBackColor_Handled;
                                }
                            }
                            if (delayed)
                            {
                                if (colourStatusOnly)
                                {
                                    TransactionTimeSlot[step].ForeColor = TransactionColor_Queued;
                                    TransactionCodeSlot[step].ForeColor = TransactionColor_Queued;
                                    TransactionStoreSlot[step].ForeColor = TransactionColor_Queued;
                                    TransactionStatusSlot[step].ForeColor = TransactionColor_Delayed;

                                    TransactionTimeSlot[step].BackColor = TransactionBackColor_Queued;
                                    TransactionCodeSlot[step].BackColor = TransactionBackColor_Queued;
                                    TransactionStoreSlot[step].BackColor = TransactionBackColor_Queued;
                                    TransactionStatusSlot[step].BackColor = TransactionBackColor_Delayed;
                                }
                                else
                                {
                                    TransactionTimeSlot[step].ForeColor = TransactionColor_Delayed;
                                    TransactionCodeSlot[step].ForeColor = TransactionColor_Delayed;
                                    TransactionStoreSlot[step].ForeColor = TransactionColor_Delayed;
                                    TransactionStatusSlot[step].ForeColor = TransactionColor_Delayed;

                                    TransactionTimeSlot[step].BackColor = TransactionBackColor_Delayed;
                                    TransactionCodeSlot[step].BackColor = TransactionBackColor_Delayed;
                                    TransactionStoreSlot[step].BackColor = TransactionBackColor_Delayed;
                                    TransactionStatusSlot[step].BackColor = TransactionBackColor_Delayed;
                                }
                            }
                            if (queued)
                            {
                                if (colourStatusOnly)
                                {
                                    TransactionTimeSlot[step].ForeColor = TransactionColor_Queued;
                                    TransactionCodeSlot[step].ForeColor = TransactionColor_Queued;
                                    TransactionStoreSlot[step].ForeColor = TransactionColor_Queued;
                                    TransactionStatusSlot[step].ForeColor = TransactionColor_Queued;

                                    TransactionTimeSlot[step].BackColor = TransactionBackColor_Queued;
                                    TransactionCodeSlot[step].BackColor = TransactionBackColor_Queued;
                                    TransactionStoreSlot[step].BackColor = TransactionBackColor_Queued;
                                    TransactionStatusSlot[step].BackColor = TransactionBackColor_Queued;
                                }
                                else
                                {
                                    TransactionTimeSlot[step].ForeColor = TransactionColor_Queued;
                                    TransactionCodeSlot[step].ForeColor = TransactionColor_Queued;
                                    TransactionStoreSlot[step].ForeColor = TransactionColor_Queued;
                                    TransactionStatusSlot[step].ForeColor = TransactionColor_Queued;

                                    TransactionTimeSlot[step].BackColor = TransactionBackColor_Queued;
                                    TransactionCodeSlot[step].BackColor = TransactionBackColor_Queued;
                                    TransactionStoreSlot[step].BackColor = TransactionBackColor_Queued;
                                    TransactionStatusSlot[step].BackColor = TransactionBackColor_Queued;
                                }
                            }

                            parts = dispstr.Split(',');
                            TransactionTimeSlot[step].Text = parts[0];
                            TransactionCodeSlot[step].Text = parts[1];
                            TransactionStatusSlot[step].Text = MapStatusStringToDisplay(parts[2]);
                            TransactionStoreSlot[step].Text = parts[3];
                            TransactionTimeSlot[step].Invalidate(); //Refresh();
                            TransactionCodeSlot[step].Invalidate(); //Refresh();
                            TransactionStatusSlot[step].Invalidate(); //Refresh();
                            TransactionStoreSlot[step].Invalidate(); //Refresh();
                            step++;
                        }
                    }
				}
			}
			catch
			{
			}
		}

        void FilterBasedOnBusiness(int businessNumber)
        {
            business = businessNumber;

            ClearDataCaches();
            
            foreach (Node child in TransactionsNode.getChildren())
            {
                string store = child.GetAttribute("store");
                int biz = CONVERT.ParseInt(store.Split(" ".ToCharArray())[1]);

                if (biz == business)
                {
                    InsertTransaction(child);
                }
            }
        }

		void SetFilter(string filterName)
		{
			MyFilterName = filterName;
			
			ResetDataCaches();
			//Attach to Existing Children 
			foreach (Node child in TransactionsNode.getChildren())
			{
				string name = child.GetAttribute("name");
				string displayname = child.GetAttribute("displayname");
				string event_type = child.GetAttribute("event_type");
				int sequence = child.GetIntAttribute("sequence",0);
				int time = child.GetIntAttribute("time",0);
				string status = child.GetAttribute("status");

				if (event_type.ToLower() == MyFilterName.ToLower())
				{
					AddTransaction(child);
				}
			}
		}

	    string GetTransactionStoreLabel (Node transaction)
	    {
	        string storeId = transaction.GetAttribute("store");
            Node store = model.GetNamedNode(storeId);

	        string bizName = SkinningDefs.TheInstance.GetData("biz");
            string strippedStoreId = storeId.Replace(bizName.ToLower(), "").Replace(bizName, "").Replace(" ", "");
	        string storeIndex = store.GetAttribute("index", strippedStoreId);

	        return store.GetAttribute("shortdesc", storeIndex);
	    }

	    void UpdateTransaction (Node transaction)
	    {
            string name = transaction.GetAttribute("name");
            string displayname = transaction.GetAttribute("displayname");
            string event_type = transaction.GetAttribute("event_type");
            int sequence = transaction.GetIntAttribute("sequence", 0);
            int time = transaction.GetIntAttribute("time", 0);
            string status = transaction.GetAttribute("status");
	        string store_id = GetTransactionStoreLabel(transaction);

            if (SequenceToDispName.ContainsKey(sequence))
            {
                //remove the old display string 
                SequenceToDispName.Remove(sequence);
                //Rebuild and Add a New One
                string dispstr;
                if (SkinningDefs.TheInstance.GetBoolData("esm_sim", false))
                {
                    dispstr = store_id + "," + BuildTimeString(time) + "," + status + "," + displayname;
                }
                else
                {
                    dispstr = BuildTimeString(time) + "," + displayname + "," + status + "," + store_id;
                }

                SequenceToDispName.Add(sequence, dispstr);
                this.Changed = true;
                //RefreshDisplay();
            }
	    }

	    void RefreshTransaction (Node transaction)
		{
			string name = transaction.GetAttribute("name");
			string displayname = transaction.GetAttribute("displayname");
			string event_type = transaction.GetAttribute("event_type");
			int sequence = transaction.GetIntAttribute("sequence",0);
			int time = transaction.GetIntAttribute("time",0);
			string status = transaction.GetAttribute("status");
		    string store_id = GetTransactionStoreLabel(transaction);

			if (SequenceToDispName.ContainsKey(sequence))
			{
				//remove the old display string 
				SequenceToDispName.Remove(sequence);
				//Rebuild and Add a New One
                string dispstr;
                if (SkinningDefs.TheInstance.GetBoolData("esm_sim", false))
                {
                    dispstr = store_id + "," + BuildTimeString(time) + "," + status + "," + displayname;
                }
                else
                {
                    dispstr = BuildTimeString(time) + "," + displayname + "," + status + "," + store_id;
                }

				SequenceToDispName.Add(sequence, dispstr);
				this.Changed = true;
				//RefreshDisplay();
			}				
		}

	    void AttributesChangedInTransactionNode (Node sender, ArrayList attrs)
	    {
            foreach (AttributeValuePair avp in attrs)
            {
                if (avp.Attribute == "status")
                {
                    UpdateTransaction(sender);
                    break;
                }
            }
	    }

		void TransactionNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			foreach (AttributeValuePair avp in attrs)
			{
				if (avp.Attribute == "status")
				{
					RefreshTransaction(sender);
					break;
				}
			}
		}

        void UpdateTransactions()
        {
            foreach (Node transaction in TransactionNodes)
            {
                UpdateTransaction(transaction);
            }
        }

		void RefreshTransactions()
		{
			foreach (Node transaction in TransactionNodes)
			{
				RefreshTransaction(transaction);
			}
		}

		void TransactionsNode_ChildRemoved(Node sender, Node child)
		{
			string name = child.GetAttribute("name");
			string displayname = child.GetAttribute("displayname");
			string event_type = child.GetAttribute("event_type");
			int sequence = child.GetIntAttribute("sequence",0);
			int time = child.GetIntAttribute("time",0);
			string status = child.GetAttribute("status");

			if (event_type.ToLower() == MyFilterName.ToLower())
			{
				RemoveTransaction(child);
			}
		}

		void ChildRemovedFromTransactionNodeBasedOnBusiness(Node sender, Node child)
        {
            string name = child.GetAttribute("name");
            string displayname = child.GetAttribute("displayname");
            string event_type = child.GetAttribute("event_type");
            int sequence = child.GetIntAttribute("sequence", 0);
            int time = child.GetIntAttribute("time", 0);
            string status = child.GetAttribute("status");
            string store = child.GetAttribute("store");
            int biz = CONVERT.ParseInt(store.Split(" ".ToCharArray())[1]);

            if (biz == business)
            {
                DeleteTransaction(child);
            }
        }

		void TransactionsNode_ChildAdded(Node sender, Node child)
		{
			string name = child.GetAttribute("name");
			string displayname = child.GetAttribute("displayname");
			string event_type = child.GetAttribute("event_type");
			int sequence = child.GetIntAttribute("sequence",0);
			int time = child.GetIntAttribute("time",0);
			string status = child.GetAttribute("status");

			if (event_type.ToLower() == MyFilterName.ToLower())
			{
				AddTransaction(child);
			}
		}

		void ChildAddedToTransactionNodeBasedOnBusiness(Node sender, Node child)
        {
            string name = child.GetAttribute("name");
            string displayname = child.GetAttribute("displayname");
            string event_type = child.GetAttribute("event_type");
            int sequence = child.GetIntAttribute("sequence", 0);
            int time = child.GetIntAttribute("time", 0);
            string status = child.GetAttribute("status");
            string store = child.GetAttribute("store");
            int biz = CONVERT.ParseInt(store.Split(" ".ToCharArray())[1]);

            if (biz == business)
            {
                InsertTransaction(child);
            }
        }

		public string MapStatusStringToDisplay (string status)
		{
			string token = "status_name_" + status.ToLower();
			string translated = status;

			if(auto_translate)
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

        void AttributesChangedOnTimeNode(Node sender, ArrayList attrs)
        {
            foreach (AttributeValuePair avp in attrs)
            {
                if (avp.Attribute == "world_time_at_zero_seconds")
                {
                    UpdateTransactions();
                    break;
                }
            }
        }

		void timeNode_AttributesChanged (Node sender, ArrayList attrs)
		{
			foreach (AttributeValuePair avp in attrs)
			{
				if (avp.Attribute == "world_time_at_zero_seconds")
				{
					RefreshTransactions();
					break;
				}
			}
		}

	    protected override void OnBackColorChanged (EventArgs e)
	    {
	        base.OnBackColorChanged(e);

	        foreach (Control control in Controls)
	        {
	            control.BackColor = BackColor;
	        }
	    }
	}
}