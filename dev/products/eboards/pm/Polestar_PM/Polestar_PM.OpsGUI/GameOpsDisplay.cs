using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Data;
using System.Windows.Forms;
using System.Xml;
using System.Globalization;
using System.Drawing.Text;
using System.IO;
using System.Runtime.InteropServices;

using LibCore;
using Network;
using CoreUtils;
using CommonGUI;

namespace Polestar_PM.OpsGUI
{
	/// <summary>
	/// Summary description for ProjectedRevenueView.
	/// </summary>
	public class GameOpsDisplay : FlickerFreePanel
	{
		static System.Globalization.CultureInfo myCI_enGB = new System.Globalization.CultureInfo( "en-GB", false );

		private NumberFormatInfo nfi = null;
		private Font MyDefaultSkinFontBold12 = null;

		protected Image backImage = null;
		protected Image blockImage_red = null;
		protected Image blockImage_yellow = null;

		private Node ops_worklist_node = null;
		protected bool ops_block = false;
		protected bool ops_install = false;
		protected string ops_name = "";

		
		/// <summary>
		/// Show Availability / Revenue
		/// </summary>
		/// <param name="model"></param>
		public GameOpsDisplay(NodeTree model, bool IsTrainingGame)
		{
			backImage = loadImage("gameopsback.png");
			blockImage_red = loadImage("game_stats_block_red.png");
			blockImage_yellow = loadImage("game_stats_block_yellow.png");

			//Just display in GB number format for the time being 
			nfi = new CultureInfo( "en-GB", false ).NumberFormat;
			string fontname =  SkinningDefs.TheInstance.GetData("fontname");
			MyDefaultSkinFontBold12 = ConstantSizeFont.NewFont(fontname,12f,FontStyle.Bold);

			ops_worklist_node = model.GetNamedNode("ops_worklist"); 
			ops_worklist_node.AttributesChanged +=new Network.Node.AttributesChangedEventHandler(ops_worklist_node_AttributesChanged);

			UpdateFigures();
			Refresh();
		}

		/// <summary>
		/// Dispose ...
		/// </summary>
		new public void Dispose()
		{
			//get rid of the Font
			if (MyDefaultSkinFontBold12 != null)
			{
				MyDefaultSkinFontBold12.Dispose();
				MyDefaultSkinFontBold12 = null;
			}

			//Disconnect from the nodes
			if (ops_worklist_node != null)
			{
				ops_worklist_node.AttributesChanged -=new Network.Node.AttributesChangedEventHandler(ops_worklist_node_AttributesChanged);
				ops_worklist_node = null;
			}
		}

		public Image loadImage(string imagename)
		{
			return Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images\\panels\\"+imagename);
		}

		private void UpdateFigures ()
		{
			if (ops_worklist_node != null)
			{
				ops_block = ops_worklist_node.GetBooleanAttribute("block",false);
				ops_install = ops_worklist_node.GetBooleanAttribute("install",false);
				ops_name = ops_worklist_node.GetAttribute("jobname");
			}
			this.Refresh();
		}

		private void ops_worklist_node_AttributesChanged(Node sender, ArrayList attrs)
		{
			UpdateFigures();
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			//Setup the Quality and Rendering Options 
			e.Graphics.SmoothingMode = SmoothingMode.None;
			e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
			e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
			SizeF textsize = new SizeF(0,0);
			
			//Draw the background 
			if (backImage != null)
			{
				e.Graphics.DrawImage(this.backImage,0,0,210,75);
			}

			//Draw the Operations area 
			e.Graphics.DrawString("Operations",MyDefaultSkinFontBold12,Brushes.White,5,5);

			//The blocking operatiosn is either external (Red) or Internal (yellow for FSC Upgrades etc)
			if (ops_block)
			{
				if (this.ops_install)
				{
					if (blockImage_yellow != null)
					{
						e.Graphics.DrawImage(blockImage_yellow,0,27,200,48);
					}
					e.Graphics.DrawString(ops_name,MyDefaultSkinFontBold12,Brushes.Black,10,40);
					e.Graphics.DrawString("1",MyDefaultSkinFontBold12,Brushes.Black,170,40);
				}
				else
				{
					if (blockImage_red != null)
					{
						e.Graphics.DrawImage(blockImage_red,0,27,200,48);
					}
					e.Graphics.DrawString(ops_name,MyDefaultSkinFontBold12,Brushes.Black,10,40);
				}
			}
		}
	}
}
