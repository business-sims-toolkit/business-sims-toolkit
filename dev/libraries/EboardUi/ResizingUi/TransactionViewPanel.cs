using System;
using System.Drawing;
using System.Windows.Forms;
using Algorithms;
using CommonGUI;
using CoreUtils;
using GameManagement;
using LibCore;

namespace ResizingUi
{
    public class TransactionViewPanel : FlickerFreePanel
    {
        readonly TransactionsView.TransactionsView instoreView;
        readonly TransactionsView.TransactionsView onlineView;

        readonly ReadoutPanel.ReadoutPanel financialMetrics;

	    bool positionOnlineFirst;

	    public bool PositionOnlineFirst
	    {
		    get => positionOnlineFirst;

		    set
		    {
			    positionOnlineFirst = true;
				DoLayout();
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

                instoreView.CascadedBackgroundProperties = value;
                onlineView.CascadedBackgroundProperties = value;
                financialMetrics.CascadedBackgroundProperties = value;

                // TODO
                //financialMetrics.CascadedBackgroundProperties = value;

                if (cascadedBackgroundProperties != null)
                {
                    cascadedBackgroundProperties.PropertiesChanged += cascadedBackgroundProperties_PropertiesChanged;
                }

                Invalidate(new Rectangle(0, 0, Width, Height), true);
            }
        }

	    public TransactionsView.TransactionsView LeftTransactions => onlineView;
	    public TransactionsView.TransactionsView RightTransactions => instoreView;

		public TransactionViewPanel (NetworkProgressionGameFile gameFile, IWatermarker watermarker, CascadedBackgroundProperties properties)
        {
            var networkModel = gameFile.NetworkModel;

            instoreView = new TransactionsView.TransactionsView(networkModel.GetNamedNode("Transactions"), BusinessChannel.Instore, true)
            {
                BackColor = SkinningDefs.TheInstance.GetColorData("ops_background_colour"),
                ForeColor = Color.White,
                Watermarker = watermarker
            };
            Controls.Add(instoreView);

            onlineView = new TransactionsView.TransactionsView(networkModel.GetNamedNode("Transactions"), BusinessChannel.Online, true)
            {
                BackColor = SkinningDefs.TheInstance.GetColorData("ops_background_colour"),
                ForeColor = Color.White,
                Watermarker = watermarker
            };
            Controls.Add(onlineView);

            financialMetrics = new ReadoutPanel.ReadoutPanel(true)
            {
                BackColor = SkinningDefs.TheInstance.GetColorData("ops_background_colour"),
                Watermarker = watermarker,
                EntryLayout = ResizingUi.ReadoutPanel.Layout.Vertical,
                SeriesLayout = ResizingUi.ReadoutPanel.Layout.Vertical
            };
            financialMetrics.AddEntry(SkinningDefs.TheInstance.GetData("transactionname"), "99/99", Color.White, new[] { networkModel.GetNamedNode("Transactions") }, nodes => CONVERT.Format("{0}/{1}", nodes[0].GetIntAttribute("count_good", 0), nodes[0].GetIntAttribute("count_max", 0)));
            financialMetrics.AddCurrencyEntry("Revenue", 99999999, SkinningDefs.TheInstance.GetColorData("transaction_handled_back_colour", Color.Green), networkModel.GetNamedNode("Revenue"), "revenue");
            financialMetrics.AddCurrencyEntry("Revenue Lost", 99999999, SkinningDefs.TheInstance.GetColorData("transaction_cancelled_back_colour", Color.Green), networkModel.GetNamedNode("Revenue"), "revenue_lost");
            financialMetrics.AddCurrencyEntry("Max Revenue", 99999999, Color.White, networkModel.GetNamedNode("Revenue"), "max_revenue");
            Controls.Add(financialMetrics);

            CascadedBackgroundProperties = properties;
        }

        protected override void Dispose (bool disposing)
        {
            if (disposing)
            {
                instoreView.Dispose();
                onlineView.Dispose();
                financialMetrics.Dispose();
            }
        }
        
        protected override void OnPaint (PaintEventArgs e)
        {
            BackgroundPainter.Paint(this, e.Graphics, cascadedBackgroundProperties);
        }

        protected override void OnParentChanged (EventArgs e)
        {
            cascadedBackgroundProperties.CascadedReferenceControl = Parent;
        }

        protected override void OnSizeChanged (EventArgs e)
        {
            DoLayout();
        }

        void DoLayout ()
        {
            var sidePadding = 0.0125f * Width;
            var topPadding = 0 * Height;

            var remainingWidth = Width - 2 * sidePadding;
            const float transactionWidthFraction = 0.35f;
            var transactionWidth = transactionWidthFraction * remainingWidth;

            var sectionHeight = Height - 2 * topPadding;
            const int innerPadding = 10;

	        var leftView = instoreView;
	        var rightView = onlineView;

	        if (positionOnlineFirst)
	        {
		        leftView = onlineView;
		        rightView = instoreView;
	        }

	        leftView.Bounds = new Rectangle((int) sidePadding, topPadding, (int) transactionWidth, sectionHeight);

	        rightView.Bounds = new RectangleFromBounds
            {
                Right = Width - (int) sidePadding,
                Width = (int) transactionWidth,
                Height = sectionHeight,
                Top = topPadding
            }.ToRectangle();

	        foreach (var transactionView in new [] { instoreView, onlineView })
	        {
		        transactionView.Leading = 0;
		        transactionView.TopMargin = 0;
		        transactionView.BottomMargin = 0;
		        transactionView.LeftMargin = 0;
		        transactionView.RightMargin = 0;
		        transactionView.TimeTab = 0.0f;
		        transactionView.CodeTab = 0.25f;
		        transactionView.BuTab = 0.5f;
		        transactionView.StatusTab = 0.65f;
		        transactionView.BuAlignment = StringAlignment.Center;
		        transactionView.StatusAlignment = StringAlignment.Center;
		        transactionView.TitleProportionalHeight = 0.1f;
	        }

			var metricsWidth = remainingWidth - 2 * transactionWidth - 2 * innerPadding;

            financialMetrics.Bounds = new Rectangle (0, 0, Width, Height).CentreSubRectangle((int) metricsWidth, sectionHeight);

            Invalidate(new Rectangle (0, 0, Width, Height), true);

        }

        void cascadedBackgroundProperties_PropertiesChanged (object sender, EventArgs e)
        {
            Invalidate(new Rectangle (0, 0, Width, Height), true);
        }
    }
}