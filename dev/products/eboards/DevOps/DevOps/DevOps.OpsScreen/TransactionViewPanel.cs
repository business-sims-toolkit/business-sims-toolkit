using System.Drawing;
using CommonGUI;
using Network;
using ResizingUi;
using ResizingUi.TransactionsView;

namespace DevOps.OpsScreen
{
    public class TransactionViewPanel : FlickerFreePanel
    {
        readonly NodeTree network;

	    TransactionsView instoreView;
	    TransactionsView onlineView;

	    IWatermarker watermarker;

	    public IWatermarker Watermarker
	    {
		    get => watermarker;

		    set
		    {
			    instoreView.Watermarker = value;
			    onlineView.Watermarker = value;
			    watermarker = value;
		    }
	    }

		public TransactionViewPanel (NodeTree network)
        {
            this.network = network;
            
            Setup();
        }

	    void Setup()
        {
            instoreView = new TransactionsView (network.GetNamedNode("Transactions"), BusinessChannel.Instore) { LeftMargin = 0, TopMargin = 10, RightMargin = 10, BottomMargin = 0, Leading = 5, BuAlignment = StringAlignment.Far, StatusAlignment = StringAlignment.Center };
            Controls.Add(instoreView);
            
	        onlineView = new TransactionsView (network.GetNamedNode("Transactions"), BusinessChannel.Online) { LeftMargin = 10, TopMargin = 10, RightMargin = 0, BottomMargin = 0, Leading = 5, BuAlignment = StringAlignment.Far, StatusAlignment = StringAlignment.Center };
			Controls.Add(onlineView);

            DoLayout();
        }

        protected override void OnSizeChanged(System.EventArgs e)
        {
            base.OnSizeChanged(e);

            DoLayout();
        }

	    void DoLayout()
        {
            int widthPadding = 0;
            int heightPadding = 0;
            
            int transViewWidth = (Width - (4 * widthPadding)) / 2;
            int transViewHeight = (Height - (2 * heightPadding));

            instoreView.Size = new Size(transViewWidth, transViewHeight);
            instoreView.Location = new Point(widthPadding, heightPadding);

            onlineView.Size = new Size(transViewWidth, transViewHeight);
            onlineView.Location = new Point(instoreView.Right + (2 * widthPadding), heightPadding);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                instoreView?.Dispose();
                onlineView?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
