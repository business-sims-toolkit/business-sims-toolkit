using System.Drawing;
using System.Collections;
using CommonGUI;
using CoreUtils;
using LibCore;
using Network;

namespace DevOps.OpsScreen
{
	internal class DevOpsMetricView : FlickerFreePanel
    {
        Node transactionsNode;
        Node revenueNode;
        Node availabilityNode;
        Node slaBreachNode;

        AttributeValuePairPanel transactionsAVPPPanel;
        AttributeValuePairPanel revenueAVPPPanel;
        AttributeValuePairPanel availabilityAVPPPanel;
        AttributeValuePairPanel maxRevenueAVPPPanel;
        AttributeValuePairPanel lostRevenueAVPPPanel;
        AttributeValuePairPanel slaBreachAVPPPanel;

        Color fontColor = Color.White;

        protected NodeTree model;

		public DevOpsMetricView (NodeTree model)
        {
            this.model = model;

            Setup();
        }

        protected override void OnSizeChanged(System.EventArgs e)
        {
            base.OnSizeChanged(e);

            DoLayout();

			Invalidate();
        }

	    void DoLayout()
        {
	        int margin = Height / 16;
	        int columnGap = 8 * margin;
	        var revenuePanelSize = new Size ((Width - (2 * margin) - columnGap) * 3 / 5, (Height - (4 * margin)) / 3);

	        revenueAVPPPanel.Bounds = new Rectangle (margin, margin, revenuePanelSize.Width, revenuePanelSize.Height);
	        lostRevenueAVPPPanel.Bounds = new Rectangle (revenueAVPPPanel.Left, revenueAVPPPanel.Bottom + margin, revenuePanelSize.Width, revenuePanelSize.Height);
	        maxRevenueAVPPPanel.Bounds = new Rectangle (lostRevenueAVPPPanel.Left, lostRevenueAVPPPanel.Bottom + margin, revenuePanelSize.Width, revenuePanelSize.Height);

			transactionsAVPPPanel.Bounds = new Rectangle (revenueAVPPPanel.Right + columnGap, revenueAVPPPanel.Top, Width - margin - (revenueAVPPPanel.Right + columnGap), revenueAVPPPanel.Height);
			availabilityAVPPPanel.Bounds = new Rectangle (transactionsAVPPPanel.Left, transactionsAVPPPanel.Bottom + margin, transactionsAVPPPanel.Width, transactionsAVPPPanel.Height);
            slaBreachAVPPPanel.Bounds = new Rectangle (availabilityAVPPPanel.Left, availabilityAVPPPanel.Bottom + margin, availabilityAVPPPanel.Width, availabilityAVPPPanel.Height);
		}

        public new void Dispose()
        {
            slaBreachNode.AttributesChanged -= SlaBreachNodeAttributesChanged;
            availabilityNode.AttributesChanged -= AvailabilityNodeAttributesChanged;
            transactionsNode.AttributesChanged -= TransactionNodeAttributesChanged;
            revenueNode.AttributesChanged -= revenueNode_AttributesChanged;
        }

        protected void Setup()
        {
	        BackColor = Color.FromArgb(37, 37, 37);

            availabilityNode = model.GetNamedNode("Availability");
            availabilityNode.AttributesChanged += (AvailabilityNodeAttributesChanged);

            revenueNode = model.GetNamedNode("Revenue");
            revenueNode.AttributesChanged += revenueNode_AttributesChanged;

            slaBreachNode = model.GetNamedNode("SLA_Breach");
            slaBreachNode.AttributesChanged += (SlaBreachNodeAttributesChanged);

            transactionsNode = model.GetNamedNode("Transactions");
            transactionsNode.AttributesChanged += (TransactionNodeAttributesChanged);

	        var currencyAttributeSizingReference = "LOST REVENUE";
	        var currencyValueSizingReference = "$99,999,999";
			var currencyAttributeSizeFraction = 0.4f;
	        var smallAttributeSizingReference = "TRANSACTIONS";
	        var smallAttributeSizeFraction = 0.6f;

			revenueAVPPPanel = new AttributeValuePairPanel("Revenue", AttributeValuePairPanel.PanelLayout.LeftToRight, currencyAttributeSizeFraction, currencyAttributeSizingReference, currencyValueSizingReference)
	        {
		        BackColor = Color.Transparent,
		        AttributeColour = fontColor,
		        AttributeFontStyle = FontStyle.Regular,
		        ValueColour = fontColor,
		        ValueFontStyle = FontStyle.Bold
	        };
	        Controls.Add(revenueAVPPPanel);

	        maxRevenueAVPPPanel = new AttributeValuePairPanel("Max Revenue", AttributeValuePairPanel.PanelLayout.LeftToRight, currencyAttributeSizeFraction, currencyAttributeSizingReference, currencyValueSizingReference)
	        {
		        BackColor = Color.Transparent,
		        AttributeColour = fontColor,
		        AttributeFontStyle = FontStyle.Regular,
		        ValueColour = fontColor,
		        ValueFontStyle = FontStyle.Bold
	        };
	        Controls.Add(maxRevenueAVPPPanel);

	        lostRevenueAVPPPanel = new AttributeValuePairPanel("Lost Revenue", AttributeValuePairPanel.PanelLayout.LeftToRight, currencyAttributeSizeFraction, currencyAttributeSizingReference, currencyValueSizingReference)
	        {
		        BackColor = Color.Transparent,
		        AttributeColour = fontColor,
		        AttributeFontStyle = FontStyle.Regular,
		        ValueColour = fontColor,
		        ValueFontStyle = FontStyle.Bold
	        };
	        Controls.Add(lostRevenueAVPPPanel);

			transactionsAVPPPanel = new AttributeValuePairPanel("Transactions", AttributeValuePairPanel.PanelLayout.LeftToRight, smallAttributeSizeFraction, smallAttributeSizingReference, "99/99")
                                    {
                                        BackColor = Color.Transparent,
                                        AttributeColour = fontColor,
                                        AttributeFontStyle = FontStyle.Regular,
                                        ValueColour = fontColor,
                                        ValueFontStyle = FontStyle.Bold
                                    };
            Controls.Add(transactionsAVPPPanel);


            availabilityAVPPPanel = new AttributeValuePairPanel("Availability", AttributeValuePairPanel.PanelLayout.LeftToRight, smallAttributeSizeFraction, smallAttributeSizingReference, "100%")
									{
                                        BackColor = Color.Transparent,
                                        AttributeColour = fontColor,
                                        AttributeFontStyle = FontStyle.Regular,
                                        ValueColour = fontColor,
                                        ValueFontStyle = FontStyle.Bold
                                    };
            Controls.Add(availabilityAVPPPanel);

            slaBreachAVPPPanel = new AttributeValuePairPanel("SLA Breach", AttributeValuePairPanel.PanelLayout.LeftToRight, smallAttributeSizeFraction, smallAttributeSizingReference, "99")
                                    {
                                        BackColor = Color.Transparent,
                                        AttributeColour = fontColor,
                                        ValueColour = fontColor,
                                        ValueFontStyle = FontStyle.Bold
                                    };
            Controls.Add(slaBreachAVPPPanel);

            UpdateBreachCountDisplay();
            UpdateAvailabilityDisplay();
            UpdateTransactionDisplay();
            UpdateRevenueDisplay();

            DoLayout();
        }

	    void SlaBreachNodeAttributesChanged(Node sender, ArrayList attrs)
        {
            UpdateBreachCountDisplay();
        }

	    void UpdateBreachCountDisplay()
        {
            int slaCount = slaBreachNode.GetIntAttribute("biz_serv_count", 0);
            slaBreachAVPPPanel.ValueText = CONVERT.ToStr(slaCount);
        }

	    void AvailabilityNodeAttributesChanged(Node sender, ArrayList attrs)
        {
            if (availabilityAVPPPanel.Visible)
            {
                UpdateAvailabilityDisplay();
            }
        }

	    void UpdateAvailabilityDisplay()
        {
            double availability = availabilityNode.GetDoubleAttribute("availability", 0.0);
            availabilityAVPPPanel.ValueText = CONVERT.ToStr((int)availability) + "%";
        }

	    void TransactionNodeAttributesChanged(Node sender, ArrayList attrs)
        {
            UpdateTransactionDisplay();
        }

	    void UpdateTransactionDisplay()
        {
            int transCountGood = transactionsNode.GetIntAttribute("count_good", 0);
            int transCountMax = transactionsNode.GetIntAttribute("count_max", 0);
            transactionsAVPPPanel.ValueText = CONVERT.ToStr(transCountGood) + @"/" + CONVERT.ToStr(transCountMax);
        }

	    void revenueNode_AttributesChanged(Node sender, ArrayList attrs)
        {
            UpdateRevenueDisplay();
        }

	    void UpdateRevenueDisplay()
        {
            string currencySymbol = SkinningDefs.TheInstance.GetData("currency_symbol");

            int maxRevenue = revenueNode.GetIntAttribute("max_revenue", 0);
            maxRevenueAVPPPanel.ValueText = currencySymbol + CONVERT.ToPaddedStrWithThousands(maxRevenue, 0);

            int revenueMade = revenueNode.GetIntAttribute("revenue", 0);
            revenueAVPPPanel.ValueText = currencySymbol + CONVERT.ToPaddedStrWithThousands(revenueMade, 0);

            int revenueLost = revenueNode.GetIntAttribute("revenue_lost", 0);
            lostRevenueAVPPPanel.ValueText = currencySymbol + CONVERT.ToPaddedStrWithThousands(revenueLost, 0);
        }
    }
}
