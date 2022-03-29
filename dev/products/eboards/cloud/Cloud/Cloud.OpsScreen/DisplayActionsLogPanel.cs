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
	public class DisplayActionsLogPanel : PopupPanel
	{
		protected OrderPlanner orderPlanner;

		protected NodeTree model;
		protected Node roundVariables;
		protected Node plannedOrders;
		protected Node orderQueue;
		protected Node incomingOrders;

		protected Label lblPanelTitle;
		protected ImageTextButton cancel;
		protected Bitmap FullBackgroundImage;
		protected Font Font_Title;
		protected Font Font_SubTitle;
		protected Font Font_Body;

		protected Panel contentPanel;

		protected Color TitleForeColour = Color.FromArgb(210, 210, 210);
		protected Color SelectedValueForeColour = Color.FromArgb(255, 237, 210); 

		public DisplayActionsLogPanel(NodeTree model, OrderPlanner orderPlanner)
		{
			this.model = model;
			this.orderPlanner = orderPlanner;

			incomingOrders = model.GetNamedNode("IncomingOrders");

			FullBackgroundImage = (Bitmap)Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images\\panels\\PopupBack_Requests.png");
			BackgroundImage = FullBackgroundImage;

			string font = SkinningDefs.TheInstance.GetData("fontname");
			Font_Title = FontRepository.GetFont(font, 16, FontStyle.Bold);
			Font_SubTitle = FontRepository.GetFont(font, 12, FontStyle.Bold);
			Font_Body = FontRepository.GetFont(font, 8, FontStyle.Bold);

			BuildMainControls();
			FillContentControls();
		}

		protected void BuildMainControls()
		{
			lblPanelTitle = new Label();
			lblPanelTitle.Location = new Point(0, 0);
			lblPanelTitle.Size = new Size(230, 30);
			lblPanelTitle.ForeColor = TitleForeColour;
			lblPanelTitle.BackColor = Color.Transparent;
			lblPanelTitle.Text = "Actions Log";
			lblPanelTitle.Font = Font_Title;
			lblPanelTitle.Visible = true;
			Controls.Add(lblPanelTitle);

			cancel = new ImageTextButton(@"images\buttons\button_85x25.png");
			cancel.SetAutoSize();
			cancel.SetButtonText("Cancel", Color.White, Color.White, Color.White, Color.DimGray);
			cancel.ButtonPressed += new ImageButton.ImageButtonEventArgsHandler(cancel_ButtonPressed);
			Controls.Add(cancel);
		}

		protected void FillContentControls()
		{
			contentPanel = new Panel();
			contentPanel.Location = new Point(10, lblPanelTitle.Bottom);
			contentPanel.Size = new Size(Width - 20, Height - (lblPanelTitle.Bottom + 10 + cancel.Height + 10));
			contentPanel.BackColor = Color.Black;
			contentPanel.AutoScroll = true;

			int posx = 0;
			int posy = 0;
			int row_height = 20;
			int row_height_gap = 2;

			//for (int step = 0; step < 20; step++)
			//{
			//  Label tmplbl = new Label();
			//  tmplbl.ForeColor = Color.White;
			//  tmplbl.BackColor = Color.Black;
			//  tmplbl.Font = Font_Body;
			//  tmplbl.Location = new Point(posx, posy);
			//  tmplbl.Size = new Size(300,20);
			//  tmplbl.Text = CONVERT.ToStr(step)+", Hello";
			//  contentPanel.Controls.Add(tmplbl);
			//  posy += row_height + row_height_gap;
			//}
			int count = 0;
			bool showItem = false;
			foreach (Node child in incomingOrders.getChildren())
			{
				showItem = false;
				string nodetype = child.GetAttribute("type");
				string displayText = "";
				switch (nodetype)
				{
					case "order":
						string t1 = child.GetAttribute("order");
						string t2 = child.GetAttribute("service");
						string t3 = child.GetAttribute("vm_type");
						displayText = CONVERT.ToStr(count) + ", " + t1 + " " + t2 + " " + t3;
						showItem = true;
						break;
					default:
						break;
				}

				if (showItem)
				{
					SizeF mt = MeasureString(Font_Body, displayText);

					Label tmplbl = new Label();
					tmplbl.ForeColor = Color.White;
					tmplbl.BackColor = Color.Black;
					tmplbl.Font = Font_Body;
					tmplbl.Location = new Point(posx, posy);

					tmplbl.Size = new Size(800 - 10, ((int)mt.Height));
					tmplbl.Text = displayText;
					contentPanel.Controls.Add(tmplbl);
					posy += row_height + row_height_gap;
					count++;
				}
			}

			Controls.Add(contentPanel);
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
			int allowanceForTitle = 20;
			
			if (lblPanelTitle != null)
			{
				allowanceForTitle = lblPanelTitle.Bottom;
			}

			cancel.Location = new Point(Width - (cancel.Width + 10), Height - (cancel.Height + 2));
			if (contentPanel != null)
			{
				contentPanel.Location = new Point(10, allowanceForTitle);
				contentPanel.Size = new Size(Width - 20, Height - (allowanceForTitle + 10 + cancel.Height + 10));
			}
		}

		public override Size getPreferredSize()
		{
			return new Size(825, 255);
		}

		public override bool IsFullScreen => true;
	}
}
