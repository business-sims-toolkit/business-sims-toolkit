using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;

using LibCore;
using Network;
using CommonGUI;

using TransitionScreens;

namespace Polestar_PM.TransScreen
{
	/// <summary>
	/// Summary description for MS_PortfolioViewer.
	/// </summary>
	public class MS_PortfolioViewer : ServicePortfolioViewer
	{
		public MS_PortfolioViewer(NodeTree tree) : base(tree, Color.White)
		{
		}

		public override void BuildToggleButton ()
		{
			catalog_button_width = 110+10;
			catalog_button_height = 34+5; 

			toggleView = new ImageToggleButton(0, strbuttonImage_ShowRetired, strbuttonImage_ShowCatalog);
			toggleView.Size = new Size(catalog_button_width, catalog_button_height);
			toggleView.Location = new Point(246-2+9,308-2-1);
			toggleView.SetTransparent();
			toggleView.ButtonPressed += new CommonGUI.ImageButton.ImageButtonEventArgsHandler(toggleView_ButtonPressed);
			toggleView.Name = "Toggle Retired Service Portfolio View";
			this.Controls.Add(toggleView);
		}

		protected override ServiceMonitorItem CreateServiceMonitorItem(NodeTree tree, Node n, string functional_name)
		{
			return new MS_ServiceMonitorItem(tree, n, functional_name);
		}
	}
}
