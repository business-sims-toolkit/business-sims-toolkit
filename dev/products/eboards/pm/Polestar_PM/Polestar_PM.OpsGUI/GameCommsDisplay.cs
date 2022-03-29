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
	public class GameCommsDisplay : FlickerFreePanel
	{
		static System.Globalization.CultureInfo myCI_enGB = new System.Globalization.CultureInfo( "en-GB", false );

		private NumberFormatInfo nfi = null;
		private Font MyDefaultSkinFontBold12 = null;

		protected Image backImage = null;
		protected Image blockImage_red = null;
		protected Image blockImage_yellow = null;

		//private Node ops_worklist_node = null;
		private Node comms_list_node = null;
		protected bool ops_block = false;
		protected bool ops_install = false;
		protected string ops_name = "";

		protected AutoScrollCommsPanel ascp = null;
		protected bool showempty = false;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="model"></param>
		/// <param name="IsTrainingGame"></param>
		/// <param name="showempty">Used for reports as shown empty</param>
		public GameCommsDisplay(NodeTree model, bool IsTrainingGame, bool ShowEmpty)
		{
			backImage = loadImage("BusinessCommsPanel.PNG");
			showempty = ShowEmpty;

			//Just display in GB number format for the time being 
			nfi = new CultureInfo( "en-GB", false ).NumberFormat;
			string fontname =  SkinningDefs.TheInstance.GetData("fontname");
			MyDefaultSkinFontBold12 = ConstantSizeFont.NewFont(fontname,12f);//,FontStyle.Bold);

			if (showempty==false)
			{
				ascp = new AutoScrollCommsPanel(false);
				ascp.Location = new Point(10,30);
				ascp.Size = new Size(460,210);
				this.Controls.Add(ascp);

				comms_list_node = model.GetNamedNode("comms_list"); 
				comms_list_node.ChildAdded +=new Network.Node.NodeChildAddedEventHandler(comms_list_node_ChildAdded);
				comms_list_node.ChildRemoved +=new Network.Node.NodeChildRemovedEventHandler(comms_list_node_ChildRemoved);

				foreach (Node kid in comms_list_node.getChildren())
				{
					ascp.AddBox(kid);
				}
				//UpdateFigures();
			}
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

			if (comms_list_node != null)
			{
				comms_list_node.ChildAdded -=new Network.Node.NodeChildAddedEventHandler(comms_list_node_ChildAdded);
				comms_list_node.ChildRemoved -=new Network.Node.NodeChildRemovedEventHandler(comms_list_node_ChildRemoved);
				comms_list_node = null;
			}
		}

		public Image loadImage(string imagename)
		{
			return Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images\\panels\\"+imagename);
		}

		private void comms_list_node_ChildAdded(Node sender, Node child)
		{
			if (ascp != null)
			{
				this.ascp.AddBox(child);
			}
		}

		private void comms_list_node_ChildRemoved(Node sender, Node child)
		{
			if (ascp != null)
			{
				this.ascp.RemoveBox(child);
			}
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
				e.Graphics.DrawImage(this.backImage,0,0,this.Width, this.Height);
			}
			//Draw the Communication area 
			Brush textBrush = new SolidBrush(Color.FromArgb(51,51,51));  //dark color deep Gray #333333
			e.Graphics.DrawString("Communications",MyDefaultSkinFontBold12,textBrush,5,5-2);
			textBrush.Dispose();
		}

	}
}
