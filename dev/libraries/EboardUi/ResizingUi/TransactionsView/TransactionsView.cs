using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using CommonGUI;
using CoreUtils;
using Network;

namespace ResizingUi.TransactionsView
{
	public class TransactionsView : FlickerFreePanel
	{
	    readonly Node transactions;
	    readonly BusinessChannel channel;
	    readonly Dictionary<Node, TransactionRowView> transactionToView;

		int rows;

		int leftMargin;
		int topMargin;
		int rightMargin;
		int bottomMargin;
		int leading;

		float timeTab;
		float codeTab;
		float buTab;
		float statusTab;

		float titleProportionalHeight;
		Rectangle titleRectangle;

	    readonly bool useCascadedBackground;

		StringAlignment timeAlignment;
		StringAlignment codeAlignment;
		StringAlignment buAlignment;
		StringAlignment statusAlignment;

		IWatermarker watermarker;

		public IWatermarker Watermarker
		{
			get => watermarker;

			set
			{
				watermarker = value;
				Invalidate();
			}
		}

	    CascadedBackgroundProperties cascadedBackgroundProperties;
	    public CascadedBackgroundProperties CascadedBackgroundProperties
	    {
	        set
	        {
	            if (cascadedBackgroundProperties != null)
	            {
	                cascadedBackgroundProperties.PropertiesChanged -= cascadedBackgroundProperties_PropertiesChanged;
	            }

	            cascadedBackgroundProperties = value;

	            foreach (var row in transactionToView.Values)
                {
                    row.CascadedBackgroundProperties = value;
                }

                if (cascadedBackgroundProperties != null)
	            {
	                cascadedBackgroundProperties.PropertiesChanged += cascadedBackgroundProperties_PropertiesChanged;
	            }

	            Invalidate(new Rectangle(0, 0, Width, Height), true);
	        }
	    }

        public TransactionsView (Node transactions, BusinessChannel channel, bool useCascadedBackground = false)
		{
			this.transactions = transactions;
			transactions.ChildAdded += transactions_ChildAdded;
			transactions.ChildRemoved += transactions_ChildRemoved;

			this.channel = channel;

			transactionToView = new Dictionary<Node, TransactionRowView> ();

			rows = 8;
			leftMargin = 10;
			topMargin = 10;
			rightMargin = 10;
			bottomMargin = 10;
			leading = 10;

			timeTab = 0f;
			codeTab = 0.25f;
			buTab = 0.5f;
			statusTab = 0.65f;

			timeAlignment = StringAlignment.Far;
			codeAlignment = StringAlignment.Far;
			buAlignment = StringAlignment.Far;
			statusAlignment = StringAlignment.Near;

			titleProportionalHeight = 1.0f / 10;

			foreach (Node transaction in transactions.getChildren())
			{
				AddTransaction(transaction);
			}

		    this.useCascadedBackground = useCascadedBackground;

			DoSize();
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing)
			{
				transactions.ChildAdded -= transactions_ChildAdded;
				transactions.ChildRemoved -= transactions_ChildRemoved;
			}

			base.Dispose(disposing);
		}

		void transactions_ChildAdded (Node parent, Node child)
		{
			AddTransaction(child);
		}

		void AddTransaction (Node transaction)
		{
			if (GetTransactionChannel(transaction) == channel)
			{
				var row = new TransactionRowView (transaction, useCascadedBackground)
				{
					Visible = false,
					TimeAlignment = timeAlignment,
					CodeAlignment = codeAlignment,
					BuAlignment = buAlignment,
					StatusAlignment = statusAlignment,
					Watermarker = watermarker,
                    CascadedBackgroundProperties = cascadedBackgroundProperties
				};
				Controls.Add(row);

				transactionToView.Add(transaction, row);

				DoSize();
			}
		}

		void transactions_ChildRemoved (Node parent, Node child)
		{
			RemoveTransaction(child);
		}

		void RemoveTransaction (Node transaction)
		{
			if (transactionToView.ContainsKey(transaction))
			{
				var row = transactionToView[transaction];
				transactionToView.Remove(transaction);
				row.Dispose();

				DoSize();
			}
		}

		protected override void OnBackColorChanged (EventArgs e)
		{
			base.OnBackColorChanged(e);

			foreach (var row in transactionToView.Values)
			{
				row.BackColor = BackColor;
			}
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);
			DoSize();
		}

		public int Rows
		{
			get => rows;

			set
			{
				rows = value;
				DoSize();
			}
		}

		public int LeftMargin
		{
			get => leftMargin;

			set
			{
				leftMargin = value;
				DoSize();
			}
		}

		public int TopMargin
		{
			get => topMargin;

			set
			{
				topMargin = value;
				DoSize();
			}
		}

		public int RightMargin
		{
			get => rightMargin;

			set
			{
				rightMargin = value;
				DoSize();
			}
		}

		public int BottomMargin
		{
			get => bottomMargin;

			set
			{
				bottomMargin = value;
				DoSize();
			}
		}

		public int Leading
		{
			get => leading;

			set
			{
				leading = value;
				DoSize();
			}
		}

		public float TimeTab
		{
			get => timeTab;

			set
			{
				timeTab = value;
				DoSize();
			}
		}

		public float CodeTab
		{
			get => codeTab;

			set
			{
				codeTab = value;
				DoSize();
			}
		}

		public float BuTab
		{
			get => buTab;

			set
			{
				buTab = value;
				DoSize();
			}
		}

		public float StatusTab
		{
			get => statusTab;

			set
			{
				statusTab = value;
				DoSize();
			}
		}

		public float TitleProportionalHeight
		{
			get => titleProportionalHeight;

			set
			{
				titleProportionalHeight = value;
				DoSize();
			}
		}

		public StringAlignment TimeAlignment
		{
			get => timeAlignment;

			set
			{
				timeAlignment = value;
				UpdateAlignments();
			}
		}

		public StringAlignment CodeAlignment
		{
			get => codeAlignment;

			set
			{
				codeAlignment = value;
				UpdateAlignments();
			}
		}

		public StringAlignment BuAlignment
		{
			get => buAlignment;

			set
			{
				buAlignment = value;
				UpdateAlignments();
			}
		}

		public StringAlignment StatusAlignment
		{
			get => statusAlignment;

			set
			{
				statusAlignment = value;
				UpdateAlignments();
			}
		}

		void DoSize ()
		{
			var transactionsOrderedByTime = new List<Node> (transactionToView.Keys);
			transactionsOrderedByTime.Sort(delegate (Node a, Node b)
			{
				var timeA = a.GetIntAttribute("time", 0);
				var timeB = b.GetIntAttribute("time", 0);
				if (timeA != timeB)
				{
					return timeA - timeB;
				}

				var buNameA = a.Tree.GetNamedNode(a.GetAttribute("store")).GetAttribute("shortdesc");
				var buNameB = b.Tree.GetNamedNode(b.GetAttribute("store")).GetAttribute("shortdesc");

			    return buNameA != buNameB ? string.Compare(buNameA, buNameB, StringComparison.Ordinal) : 
			        string.Compare(a.GetAttribute("name"), b.GetAttribute("name"), StringComparison.Ordinal);
			});

			var y = topMargin;

			titleRectangle = new Rectangle (leftMargin, y, Width - rightMargin - leftMargin, (int) (Height * titleProportionalHeight));
			y = titleRectangle.Bottom + leading;

			var rowHeightWithLeading = (Height - bottomMargin - y) / rows;
			foreach (var transaction in transactionsOrderedByTime)
			{
				var row = transactionToView[transaction];

				row.BackColor = BackColor;
				row.Bounds = new Rectangle (leftMargin, y, Width - rightMargin - leftMargin, rowHeightWithLeading - leading);
				row.TabStops = new [] { timeTab, codeTab, buTab, statusTab };
				row.Visible = (row.Top >= 0) && (row.Bottom <= (Height - bottomMargin));

				y = row.Bottom + leading;
			}

			Invalidate();
		}

		BusinessChannel GetTransactionChannel (Node transaction)
		{
			switch (transaction.GetAttribute("event_type").ToLower())
			{
				case "online":
					return BusinessChannel.Online;

				case "instore":
					return BusinessChannel.Instore;

				default:
					throw new Exception ("Unhandled business channel");
			}
		}

	    static string GetTransactionChannelDisplayName (BusinessChannel channel)
		{
			switch (channel)
			{
				case BusinessChannel.Instore:
					return SkinningDefs.TheInstance.GetData("transaction_header_instore", "Instore");

				case BusinessChannel.Online:
					return SkinningDefs.TheInstance.GetData("transaction_header_online", "Online");

				default:
					throw new Exception ("Unhandled business channel");
			}
		}

		protected override void OnPaint (PaintEventArgs e)
		{
		    if (useCascadedBackground)
		    {
		        BackgroundPainter.Paint(this, e.Graphics, cascadedBackgroundProperties);
            }

            watermarker?.Draw(this, e.Graphics);

			using (var font = SkinningDefs.TheInstance.GetPixelSizedFont(Height * titleProportionalHeight, FontStyle.Bold))
            using (var brush = new SolidBrush(ForeColor))
            using (var backBrush = new SolidBrush(Color.FromArgb(useCascadedBackground ? SkinningDefs.TheInstance.GetIntData("cascaded_background_transparency", 255) : 255, BackColor)))
            {
                e.Graphics.FillRectangle(backBrush, new Rectangle(0, 0, Width, Height));
                e.Graphics.DrawString(GetTransactionChannelDisplayName(channel), font, brush, titleRectangle, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
            }
		}

		void UpdateAlignments ()
		{
			foreach (var transactionView in transactionToView.Values)
			{
				transactionView.TimeAlignment = timeAlignment;
				transactionView.CodeAlignment = codeAlignment;
				transactionView.BuAlignment = buAlignment;
				transactionView.StatusAlignment = statusAlignment;
			}
		}

		public override Size GetPreferredSize (Size proposedSize)
		{
			using (var row = new TransactionRowView (null)
			{
				TabStops = new [] { timeTab, codeTab, buTab, statusTab }
			})
			{
				return row.GetPreferredSize(proposedSize);
			}
		}

	    void cascadedBackgroundProperties_PropertiesChanged(object sender, EventArgs e)
	    {
	        Invalidate(new Rectangle(0, 0, Width, Height), true);
	    }
    }
}