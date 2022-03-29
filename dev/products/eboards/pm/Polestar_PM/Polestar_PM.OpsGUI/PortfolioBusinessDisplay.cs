using System;
using System.Collections;
using System.Text;

using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

using Network;

using LibCore;
using CoreUtils;

namespace Polestar_PM.OpsGUI
{
	public class PortfolioBusinessDisplay : GameTargetsDisplay
	{
		Node transactionTargetNode;
		Node costReductionTargetNode;
		//Node costAvoidanceTargetNode;
		Node budgetNode;

		public PortfolioBusinessDisplay (NodeTree model, bool training)
			: base (model, training, true)
		{
			transactionTargetNode = model.GetNamedNode("BusinessTargetTransactions");
			transactionTargetNode.AttributesChanged += new Network.Node.AttributesChangedEventHandler (transactionTargetNode_AttributesChanged);

			costReductionTargetNode = model.GetNamedNode("BusinessTargetCostReduction");
			costReductionTargetNode.AttributesChanged += new Network.Node.AttributesChangedEventHandler (costReductionTargetNode_AttributesChanged);

			//costAvoidanceTargetNode = model.GetNamedNode("BusinessTargetCostAvoidance");
			//costAvoidanceTargetNode.AttributesChanged += new Network.Node.AttributesChangedEventHandler (costAvoidanceTargetNode_AttributesChanged);

			budgetNode = model.GetNamedNode("pmo_budget");
			budgetNode.AttributesChanged += new Network.Node.AttributesChangedEventHandler (budgetNode_AttributesChanged);
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing)
			{
				transactionTargetNode.AttributesChanged -= new Network.Node.AttributesChangedEventHandler (transactionTargetNode_AttributesChanged);
				costReductionTargetNode.AttributesChanged -= new Network.Node.AttributesChangedEventHandler (costReductionTargetNode_AttributesChanged);
				//costAvoidanceTargetNode.AttributesChanged -= new Network.Node.AttributesChangedEventHandler (costAvoidanceTargetNode_AttributesChanged);
				budgetNode.AttributesChanged -= new Network.Node.AttributesChangedEventHandler (budgetNode_AttributesChanged);
			}

			base.Dispose(disposing);
		}

		protected override void OnPaint (PaintEventArgs e)
		{
			e.Graphics.SmoothingMode = SmoothingMode.None;
			e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
			e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

			SizeF textsize = new SizeF (0,0);
				
			if (backImage != null)
			{
				e.Graphics.DrawImage(backImage,0,0,this.Width,this.Height);
			}
			Brush textBrush = new SolidBrush(Color.FromArgb(51,51,51));  //dark color deep Gray #333333
			e.Graphics.DrawString("Business",MyDefaultSkinFontBold12,textBrush,5,5-2);
			textBrush.Dispose();

			using (Brush brush = new SolidBrush (Polestar_PM.OpsEngine.PM_OpsEngine_Round3.GetBenefitColour(Polestar_PM.OpsEngine.BenefitType.Transactions)))
			{
				e.Graphics.DrawString("Transactions Target" ,MyDefaultSkinFontNormal10, brush, 20,40);
				e.Graphics.DrawString(FormatThousands(transactionTargetNode.GetIntAttribute("value", 0)), MyDefaultSkinFontBold10, brush, 200,40);
			}

			using (Brush brush = new SolidBrush(Polestar_PM.OpsEngine.PM_OpsEngine_Round3.GetBenefitColour(Polestar_PM.OpsEngine.BenefitType.CostReduction)))
			{
				e.Graphics.DrawString("Cost Reduction Target" ,MyDefaultSkinFontNormal10, brush,20,70);
				e.Graphics.DrawString(FormatMoney(costReductionTargetNode.GetIntAttribute("value", 0)), MyDefaultSkinFontBold10, brush,200,70);
			}

			//using (Brush brush = new SolidBrush (PM_Ops_Engine.PM_OpsEngine_Round3.GetBenefitColour(PM_Ops_Engine.BenefitType.CostAvoidance)))
			//{
			//  e.Graphics.DrawString("Cost Avoidance Target" ,MyDefaultSkinFontNormal10, brush,20,100);
			//  e.Graphics.DrawString(FormatMoney(costAvoidanceTargetNode.GetIntAttribute("value", 0)), MyDefaultSkinFontBold10, brush,200,100);
			//}

			e.Graphics.DrawString("Investment Budget" ,MyDefaultSkinFontNormal10,Brushes.Black,20,130);
			e.Graphics.DrawString(FormatMoney(budgetNode.GetIntAttribute("budget_allowed", 0)), MyDefaultSkinFontBold10,Brushes.Black,200,130);
		}

		private void transactionTargetNode_AttributesChanged (Node sender, ArrayList attrs)
		{
			this.Refresh();
		}

		//private void costAvoidanceTargetNode_AttributesChanged (Node sender, ArrayList attrs)
		//{
		//  this.Refresh();
		//}

		private void costReductionTargetNode_AttributesChanged (Node sender, ArrayList attrs)
		{
			this.Refresh();
		}

		private void budgetNode_AttributesChanged (Node sender, ArrayList attrs)
		{
			this.Refresh();
		}

		string FormatThousands (int a)
		{
			if (a < 0)
			{
				return "-" + FormatThousands(Math.Abs(a));
			}

			string raw = LibCore.CONVERT.ToStr(a);

			StringBuilder builder = new StringBuilder ("");
			int digits = 0;
			for (int character = raw.Length - 1; character >= 0; character--)
			{
				builder.Insert(0, raw[character]);
				digits++;

				if (((digits % 3) == 0) && (character > 0))
				{
					builder.Insert(0, ",");
				}				
			}

			return builder.ToString();
		}

		string FormatMoney (int a)
		{
			return "$" + FormatThousands(a);
		}
	}
}