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
	public class OpsBusinessDisplay : GameTargetsDisplay
	{
		Node market_info_node;
		Node pmo_budget;
		Node current_time_node;
		string race_team_name = CoreUtils.SkinningDefs.TheInstance.GetData("race_team_name");
		bool alert_display_gain = false;
		bool invert_gain_display = false;

		public OpsBusinessDisplay(NodeTree model, bool training) : base (model, training, true)
		{
			race_team_name = SkinningDefs.TheInstance.GetData("race_team_name");

			market_info_node = model.GetNamedNode("market_info");
			market_info_node.AttributesChanged += new Network.Node.AttributesChangedEventHandler(market_info_node_AttributesChanged);

			pmo_budget = model.GetNamedNode("pmo_budget");

			current_time_node = model.GetNamedNode("CurrentTime");
			current_time_node.AttributesChanged += new Node.AttributesChangedEventHandler(current_time_node_AttributesChanged);
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing)
			{
				market_info_node.AttributesChanged -= new Network.Node.AttributesChangedEventHandler(market_info_node_AttributesChanged);
			}

			base.Dispose(disposing);
		}

		protected void current_time_node_AttributesChanged(Node sender, ArrayList attrs)
		{
			if (alert_display_gain)
			{
				invert_gain_display = !invert_gain_display;
				this.Invalidate();
			}
		}

		private void market_info_node_AttributesChanged(Node sender, ArrayList attrs)
		{
			alert_display_gain = market_info_node.GetBooleanAttribute("display_alert_gain", false);
			this.Invalidate();
		}

		string FormatThousands(int a)
		{
			if (a < 0)
			{
				return "-" + FormatThousands(Math.Abs(a));
			}

			string raw = LibCore.CONVERT.ToStr(a);

			StringBuilder builder = new StringBuilder("");
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

		string FormatMoney(int a)
		{
			return "$" + FormatThousands(a);
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
			
			//Brush textBrush = new SolidBrush(Color.FromArgb(51, 51, 51));  //dark color deep Gray #333333
			Brush textBrush = new SolidBrush(Color.FromArgb(255, 255, 255));  //dark color deep Gray #333333
			e.Graphics.DrawString(" Business",MyDefaultSkinFontBold10,textBrush,5,5-2);
			textBrush.Dispose();

			Brush normalTitleBrush = Brushes.Black;
			Brush normalValueBrush = Brushes.Black;

			e.Graphics.DrawString(" Market Leader", MyDefaultSkinFontNormal10, normalTitleBrush, 5, 23);
			e.Graphics.DrawString(market_info_node.GetAttribute("leader_transaction_volume"), MyDefaultSkinFontBold10, normalValueBrush, 180, 23);

			e.Graphics.DrawString(" " + race_team_name + " Targets:", MyDefaultSkinFontNormal10, normalTitleBrush, 5, 43 + 3);
			//e.Graphics.DrawString(market_info_node.GetAttribute("leader_market_share"), MyDefaultSkinFontBold10, Brushes.Black, 220, 63);

			e.Graphics.DrawString("  Cost Reduction", MyDefaultSkinFontNormal10, normalTitleBrush, 5, 85 + 3);
			e.Graphics.DrawString(FormatMoney(market_info_node.GetIntAttribute("cost_reduction", 0)), MyDefaultSkinFontBold10, normalValueBrush, 180, 85 + 3);

			if (invert_gain_display)
			{
				normalTitleBrush = Brushes.White;
				normalValueBrush = Brushes.White;
				e.Graphics.FillRectangle(Brushes.Black, 3, 64, 300, 20);
			}
			e.Graphics.DrawString("  Transactions Gain", MyDefaultSkinFontNormal10, normalTitleBrush, 5, 64 + 3);
			e.Graphics.DrawString(FormatThousands(market_info_node.GetIntAttribute("transactions_gain", 0)), MyDefaultSkinFontBold10, normalValueBrush, 180, 64 + 3);


//			e.Graphics.DrawString(" Cost Avoidance", MyDefaultSkinFontNormal10, Brushes.Black, 5, 106+3);
//			e.Graphics.DrawString(FormatMoney(market_info_node.GetIntAttribute("cost_avoidance", 0)), MyDefaultSkinFontBold10, Brushes.Black, 180, 106 + 3);
		}




	}
}