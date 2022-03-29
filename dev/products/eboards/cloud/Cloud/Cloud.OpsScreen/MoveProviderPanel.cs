using System;
using System.Drawing;
using System.Windows.Forms;
using CommonGUI;

using LibCore;
using CoreUtils;

using Network;

using Cloud.OpsEngine;

namespace Cloud.OpsScreen
{
	public class MoveProviderPanel : PopupPanel
	{
		protected NodeTree model;
		protected Node roundVariables;
		PublicVendorManager cloudVendorManager;

		protected Label lblPanelTitle;
		protected OrderPlanner orderPlanner;

		protected ImageTextButton cancel;

		protected Font Font_Title;
		protected Font Font_SubTitle;
		protected Font Font_Body;

		protected Color TitleForeColour = Color.FromArgb(64, 64, 64);

		protected Bitmap FullBackgroundImage;

		MoveCloudProviderPanel movePanel;

		public MoveProviderPanel(NodeTree model, OrderPlanner orderPlanner, PublicVendorManager cloudVendorManager)
		{
			this.model = model;
			this.orderPlanner = orderPlanner;
			this.cloudVendorManager = cloudVendorManager;

			FullBackgroundImage = (Bitmap)Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images\\panels\\PopupBack_Requests.png");
			BackgroundImage = FullBackgroundImage;

			string font = SkinningDefs.TheInstance.GetData("fontname");
			Font_Title = FontRepository.GetFont(font, 16, FontStyle.Bold);
			Font_SubTitle = FontRepository.GetFont(font, 12, FontStyle.Bold);
			Font_Body = FontRepository.GetFont(font, 10, FontStyle.Bold);

			roundVariables = model.GetNamedNode("RoundVariables");
			int current_round = roundVariables.GetIntAttribute("current_round", 1);

			lblPanelTitle = new Label();
			lblPanelTitle.Location = new Point(10, 0);
			lblPanelTitle.Size = new Size(230, 30);
			lblPanelTitle.ForeColor = Color.White;
			lblPanelTitle.BackColor = Color.Transparent;
			lblPanelTitle.Text = "Change Cloud Provider";
			lblPanelTitle.Font = Font_Title;
			lblPanelTitle.Visible = true;
			Controls.Add(lblPanelTitle);

			movePanel = new MoveCloudProviderPanel (orderPlanner, model, cloudVendorManager);
			Controls.Add(movePanel);
			movePanel.Location = new Point (0, 30);

			cancel = new ImageTextButton(@"images\buttons\button_85x25.png");
			cancel.SetAutoSize();
			cancel.SetButtonText("Close", Color.White, Color.White, Color.White, Color.DimGray);
			cancel.ButtonPressed += new ImageButton.ImageButtonEventArgsHandler(cancel_ButtonPressed);
			Controls.Add(cancel);

			DoSize();
		}

		protected void cancel_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			OnClosed();
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);
			DoSize();
		}

		protected void DoSize()
		{
			cancel.Location = new Point (Width - (cancel.Width + 10), Height - (cancel.Height + 2));
			movePanel.Size = new Size (Width - movePanel.Left, cancel.Top - movePanel.Top - 5);
		}

		public override Size getPreferredSize()
		{
			return new Size (800, 255);
		}

		public override bool IsFullScreen => false;
	}
}