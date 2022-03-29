
using System.Drawing;

using System.Diagnostics;


using System.Windows.Forms;

using System.Collections;

using CoreUtils;
using LibCore;
using Network;
using CommonGUI;
using Cloud.OpsEngine;
using ResizingUi;

namespace Cloud.OpsScreen
{
	public class RegionFinancePanel : CascadedBackgroundPanel
	{
		protected Font Font_Aspect;
		protected Font Font_Spend;
		protected Font Font_Turnover;
		protected Font Font_ProfitLoss;
		protected Font Font_Demand;
		protected Font Font_DemandBold;
		protected Font Font_DemandNameBold;

        protected Brush br_hiWhite = new SolidBrush(Color.FromArgb(224, 224, 224));
        protected Brush br_hiRed = new SolidBrush(Color.FromArgb(255, 0, 0));
        protected Brush br_hiGreen = new SolidBrush(Color.FromArgb(102, 204, 0));
        protected Brush br_hiAmber = new SolidBrush(Color.FromArgb(255, 204, 0));
        protected Brush br_hiOrangeRed = new SolidBrush(Color.FromArgb(255, 51, 0));


		string TargetBusinessNodeName = "";
        

		Node DemandListNode = null;
		Node businessNode = null;
		Node turnoverNode = null;
		Node timeNode = null;

		ArrayList activeDemands = new ArrayList();
		
		BauManager mybauManager = null;

		Hashtable DemandNodes = new Hashtable();

		ArrayList NodesAnnounced_Within4 = new ArrayList();
		ArrayList NodesAnnounced_Running = new ArrayList();

		double spend = 24600000;
		double revenue = 36600000;
		double potentialBenefit = -3750000;

	    NodeTree model;
        
        string businessTitle;
	    Color titleBackColour;
	    Color backColour;

	    bool isTraining;

		public RegionFinancePanel(NodeTree nt, BauManager bauManager, Node business, bool isTraining)
		{
            model = nt;
		    this.isTraining = isTraining;
            businessNode = business;

			mybauManager = bauManager;
            
			//get the fonts
			string font = SkinningDefs.TheInstance.GetData("fontname");
			Font_Aspect = FontRepository.GetFont(font, 10, FontStyle.Regular);
			Font_Spend = FontRepository.GetFont(font, 16, FontStyle.Bold);
			Font_Turnover = FontRepository.GetFont(font, 24, FontStyle.Bold);
			Font_ProfitLoss = FontRepository.GetFont(font, 16, FontStyle.Bold);

			Font_Demand = FontRepository.GetFont(font, 10, FontStyle.Regular);
			Font_DemandBold = FontRepository.GetFont(font, 10, FontStyle.Bold);
			Font_DemandNameBold = FontRepository.GetFont(font, 14, FontStyle.Bold);


		    Setup();
		}

        void Setup()
        {
            DoubleBuffered = true;

            SetTitle();

            SetBusinessNodeName();

        }

        void SetTitle()
        {
            businessTitle = businessNode.GetAttribute("desc");

            titleBackColour = SkinningDefs.TheInstance.GetColorDataGivenDefault(businessTitle + "_title_back_colour", Color.Orange);
            backColour = SkinningDefs.TheInstance.GetColorDataGivenDefault(businessTitle + "_alpha_back_colour", Color.HotPink);
            
        }

		void BusinessNode_AttributesChanged (Node sender, ArrayList attrs)
		{
			Invalidate();
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				ClearDemandnodes();
				NodesAnnounced_Within4.Clear();
				NodesAnnounced_Running.Clear();

				if (timeNode != null)
				{
					timeNode.AttributesChanged -= new Node.AttributesChangedEventHandler(timeNode_AttributesChanged);
					timeNode = null;
				}

				businessNode.AttributesChanged -= new Node.AttributesChangedEventHandler (BusinessNode_AttributesChanged);
			}
			base.Dispose(disposing);
		}
        

		private void ClearDemandnodes()
		{
			ArrayList names = new ArrayList();
			foreach (string dem_name in DemandNodes.Keys)
			{
				names.Add(dem_name);
			}
			foreach (string dem_name in names) 
			{
				Node dnode = (Node) DemandNodes[dem_name];
				if (dnode != null)
				{
					DemandNodes.Remove(dem_name);
				}
			}
		}

		private void AddDemandNode(Node dnode)
		{ 
			string name = dnode.GetAttribute("name");
			if (DemandNodes.ContainsKey(name) == false)
			{
				DemandNodes.Add(name, dnode);
			}
		}

        void SetBusinessNodeName()
        {
            TargetBusinessNodeName = businessNode.GetAttribute("name");

            turnoverNode = model.GetNamedNode("Turnover");

            timeNode = model.GetNamedNode("CurrentTime");
            timeNode.AttributesChanged += new Node.AttributesChangedEventHandler(timeNode_AttributesChanged);

            businessNode.AttributesChanged += new Node.AttributesChangedEventHandler(BusinessNode_AttributesChanged);

            //need to scan through the Demands to build a monitoring list 
            DemandNodes.Clear();
            DemandListNode = model.GetNamedNode("Demands");
            foreach (Node demandNode in DemandListNode.getChildren())
            {
                string name = demandNode.GetAttribute("name");
                string business = demandNode.GetAttribute("business");
                int delay = demandNode.GetIntAttribute("delay", 0);

                if (business.ToLower() == TargetBusinessNodeName.ToLower())
                {
                    AddDemandNode(demandNode);
                    //System.Diagnostics.Debug.WriteLine("SVP AF Demand "+name + "  " + CONVERT.ToStr(delay));
                }
            }

        }
        
		private int getCurrentTimePeriod()
		{
			int tp = 0;
			tp = timeNode.GetIntAttribute("seconds", 0);
			tp = (tp / 60) + 1 ;
			return tp; 
		}

		protected void timeNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			//Determine Number of active demands 
			ArrayList ScanList = new ArrayList();
			ArrayList killList = new ArrayList();

			//System.Diagnostics.Debug.WriteLine("===========================================");

			foreach (Node demandNode in DemandNodes.Values)
			{
				string name = demandNode.GetAttribute("name");
				bool active = demandNode.GetBooleanAttribute("active", false);
				string business = demandNode.GetAttribute("business");
				int delay = demandNode.GetIntAttribute("delay", 0);
				int delay_countdown = demandNode.GetIntAttribute("delay_countdown", 0);
				int duration = demandNode.GetIntAttribute("duration", 0);
				int duration_countdown = demandNode.GetIntAttribute("duration_countdown", 0);

				int linger_countdown = demandNode.GetIntAttribute("linger_countdown", 0);
				
				//switching off the linger mode until we fix the status issue 9325
				linger_countdown = 0;

				bool within4Mins = (demandNode.GetAttribute("status") == "announcing");
				bool running = (demandNode.GetAttribute("status") == "running");
				bool linger = (demandNode.GetAttribute("status") == "lingering");

				//System.Diagnostics.Debug.WriteLine(" " + name + "W4M" + CONVERT.ToStr(within4Mins) + " RUN" + CONVERT.ToStr(running)
				//  + " LIN" + CONVERT.ToStr(linger) + " Delay: " + CONVERT.ToStr(delay_countdown) + " Duration: " + CONVERT.ToStr(duration_countdown)
				//  + " linger: " + CONVERT.ToStr(linger_countdown));

				if ((within4Mins) | (running) | (linger)) 
				{
					if (NodesAnnounced_Within4.Contains(demandNode) == false)
					{
						NodesAnnounced_Within4.Add(demandNode);
						KlaxonSingleton.TheInstance.PlayAudio(AppInfo.TheInstance.Location + "\\audio\\woopwoop.wav", false);
					}
					if (NodesAnnounced_Running.Contains(demandNode) == false)
					{
						NodesAnnounced_Running.Add(demandNode);
						KlaxonSingleton.TheInstance.PlayAudio(AppInfo.TheInstance.Location + "\\audio\\woopwoop.wav", false);
					}
					if (ScanList.Contains(demandNode) == false)
					{
						ScanList.Add(demandNode);
					}
				}
				else
				{
					if (killList.Contains(demandNode) == false)
					{
						killList.Add(demandNode);
					}
				}
			}
			//now compare the list 
			foreach (Node n in ScanList)
			{
				if (activeDemands.Contains(n) == false)
				{
					activeDemands.Add(n);
				}
			}
			foreach (Node n in killList)
			{
				if (activeDemands.Contains(n))
				{
					activeDemands.Remove(n);
				}
			}
            
			Invalidate();
		}

        string GetMoneyString (double amount)
        {
            return "$" + ((amount < 0) ? "- " : "") + CONVERT.ToStrRounded((System.Math.Abs(amount) / 1000000), 2) + "M";
        }

		protected override void OnPaint(PaintEventArgs e)
		{
            int titleHeight = 25;
            Font titleFont = SkinningDefs.TheInstance.GetFont(12, FontStyle.Bold);

		    int financesWidth = Width * 3 / 5;
		    

            using (Brush titleBack = new SolidBrush(titleBackColour))
            {
                Rectangle titleRect = new Rectangle(0, 0, financesWidth, titleHeight);
                e.Graphics.FillRectangle(titleBack, titleRect);

                titleRect.X = 5;

                e.Graphics.DrawString(businessTitle, titleFont, Brushes.White, titleRect,
                    new StringFormat { LineAlignment = StringAlignment.Far, Alignment = StringAlignment.Near });
            }

            using (Brush backBrush = new SolidBrush(backColour))
            {
                e.Graphics.FillRectangle(backBrush, new Rectangle(0, titleHeight, financesWidth, Height - titleHeight));
            }
            
            
			
			int pos_x = 1;
			int pos_y = 10;

			int current_tp = 1;

			current_tp = getCurrentTimePeriod();

			e.Graphics.DrawString("INVESTMENT", Font_Aspect, br_hiWhite, pos_x, pos_y + 20);
			e.Graphics.DrawString("REVENUE GAINED", Font_Aspect, br_hiWhite, pos_x, pos_y + 58 + 5);
			e.Graphics.DrawString("PROFIT / LOSS", Font_Aspect, br_hiWhite, pos_x, pos_y + 110 - 4);
			e.Graphics.DrawString("POTENTIAL PROFIT", Font_Aspect, br_hiWhite, pos_x, pos_y + 145 + 4);

			spend = businessNode.GetDoubleAttribute("spend", 0);
			revenue = businessNode.GetDoubleAttribute("revenue_earned", 0);
			potentialBenefit = revenue + businessNode.GetDoubleAttribute("potential_extra_revenue_including_missed_demands", 0) - spend;

			double profitloss = revenue - spend;

            string investment_str = GetMoneyString(spend);
            string potential_str = GetMoneyString(potentialBenefit);
			string revgenerated_str = GetMoneyString(revenue);
			string profitloss_str = GetMoneyString(profitloss);

			//Spend is Green isd Zero and red otherwise
			if (spend == 0)
			{
				e.Graphics.DrawString(investment_str, Font_Spend, Brushes.White, pos_x, pos_y + 35);
			}
			else
			{
                e.Graphics.DrawString(investment_str, Font_Spend, Brushes.White, pos_x, pos_y + 35);
			}

			//Potential is always GREEN
            e.Graphics.DrawString(potential_str, Font_Spend, Brushes.White, pos_x, pos_y + 153 + 11); // 70);

			//Potential is always GREEN
            e.Graphics.DrawString(revgenerated_str, Font_Spend, Brushes.White, pos_x, pos_y + 70 + 9); // 123);

			//PL is GREEN OR RED 
			if (profitloss >= 0)
			{
                e.Graphics.DrawString(profitloss_str, Font_Spend, Brushes.White, pos_x, pos_y + 123 - 2); //157);
			}
			else
			{
                e.Graphics.DrawString(profitloss_str, Font_Spend, Brushes.White, pos_x, pos_y + 123 - 2); //157);
			}

			if (activeDemands.Count > 0)
			{
			    int demandTitleHeight = 25;
                int demandsWidth = Width - financesWidth;
			    float demandHeight = demandTitleHeight + 62.5f;
                

			    Debug.Assert(activeDemands.Count <= 2, "More than two demands currently active");

                Node roundVariablesNode = model.GetNamedNode("RoundVariables");
                string attributeName = CONVERT.Format("round_{0}_instances", roundVariablesNode.GetIntAttribute("current_round", 0));
                int currentRound = roundVariablesNode.GetIntAttribute("current_round", 0);

			    float demandY = titleHeight;

				foreach (Node demand in activeDemands)
				{
                    DrawDemand(e.Graphics, demand, demandHeight, demandTitleHeight, financesWidth, demandY, demandsWidth, attributeName, currentRound);
				    demandY += demandHeight;
				}

                if (demandY < Height)
                {
                    Color demandBackColour = SkinningDefs.TheInstance.GetColorDataGivenDefault(businessTitle + "_alpha_demand_back_colour", Color.HotPink);
                    using (Brush demandBackBrush = new SolidBrush(demandBackColour))
                    {
                        e.Graphics.FillRectangle(demandBackBrush,
                            new RectangleF(financesWidth, demandY, demandsWidth, Height - demandY));
                    }
                }
			}
		}

        void DrawDemand(Graphics g, Node demandNode, float totalDemandHeight, float titleHeight, float x, float y, float width, string roundInstancesAttr, int currentRound)
        {
            
            string longDemandName = demandNode.GetAttribute(CONVERT.Format("desc_round_{0}", currentRound));
            string shortDemandName = demandNode.GetAttribute(CONVERT.Format("short_desc_round_{0}", currentRound));
            
            string demandTitle = longDemandName;
            Brush demandTitleBack = Brushes.White;
            

            Color demandTitleTextColour = titleBackColour;

            // Is this check necessary? TODO
            if (demandNode.GetAttribute("status") == "running")
            {
                switch(demandNode.GetAttribute("met_status"))
                {
                    case "met":
                        demandTitle = shortDemandName + " Met";
                        //Set brushes
                        demandTitleBack = Brushes.White;
                        
                        demandTitleTextColour = Color.Black;
                        break;
                    case "unmet":
                        demandTitle = shortDemandName + "\nMissed";
                        demandTitleBack = Brushes.White;
                        
                        demandTitleTextColour = Color.Black;
                        break;
                }
            }

            // Draw the title background (white box)
            RectangleF titleRect = new RectangleF(x, y, width, titleHeight);
            g.FillRectangle(demandTitleBack, titleRect);

            Font demandTitleFont = SkinningDefs.TheInstance.GetFont(10, FontStyle.Bold);
            using (Brush demandTitleFore = new SolidBrush(Color.Black))
            {
                g.DrawString(demandTitle, demandTitleFont, demandTitleFore, titleRect, 
                    new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
            }
            

            // Draw the rest of the demand box
            float remHeight = totalDemandHeight - titleHeight;
            Color demandBackColour = SkinningDefs.TheInstance.GetColorDataGivenDefault(businessTitle + "_alpha_demand_back_colour", Color.HotPink);

            using (Brush backBrush = new SolidBrush(demandBackColour))
            {
                g.FillRectangle(backBrush, new RectangleF(x, y + titleHeight, width, remHeight));
            }
            //  ***** Draw the rest of the demand box

            float valuesY = y + titleHeight;

            float demandCodeHeight = 17.5f;
            Font demandCodeFont = SkinningDefs.TheInstance.GetFont(14, FontStyle.Bold);

            
            string demandCode = demandNode.GetAttribute("service_code");


            g.DrawString(demandCode, demandCodeFont, Brushes.White, new RectangleF( x, valuesY, width, demandCodeHeight),
                new StringFormat() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });

            valuesY += demandCodeHeight;
            

            int instances = demandNode.GetIntAttribute(roundInstancesAttr, 0);
            int trades = demandNode.GetIntAttribute("trades_per_realtime_minute", 0) * instances;
            string tradesStr = CONVERT.ToStr(trades) + " Trades";

            Font demandAttrFont = SkinningDefs.TheInstance.GetFont(10);

            float rowHeight = 15f;

            g.DrawString(tradesStr, demandAttrFont, Brushes.White, new RectangleF(x, valuesY, width, rowHeight),
                new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Center });

            valuesY += rowHeight;

            double revenuePerTrade = demandNode.GetDoubleAttribute("revenue_per_trade", 0);

            string demandPotentialStr = "$" + CONVERT.ToPaddedStrWithThousands(trades * revenuePerTrade, 0);

            g.DrawString(demandPotentialStr, demandAttrFont, Brushes.White, new RectangleF(x, valuesY, width, rowHeight),
                new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Center });

            valuesY += rowHeight;

            if (demandNode.GetAttribute("status") == "announcing")
            {
                int delayCountdown = demandNode.GetIntAttribute("delay_countdown", 0);
                // Number of time periods to go 
                int delayCountdown2 = (delayCountdown / 60) + 1;

                int currentTimePeriod = getCurrentTimePeriod();
                // Add current time period to get the absolute time period.
                delayCountdown2 += currentTimePeriod;

                // If we're exactly at a minute we need to remove 1 to display correctly.
                if ((delayCountdown % 60) == 0)
                {
                    delayCountdown2--;
                }

                string delayCountdownStr = "Expected " + CONVERT.ToStr(delayCountdown2);

                g.DrawString(delayCountdownStr, demandAttrFont, Brushes.White, new RectangleF(x, valuesY, width, rowHeight),
                new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Center });
            }



        }
	}
}