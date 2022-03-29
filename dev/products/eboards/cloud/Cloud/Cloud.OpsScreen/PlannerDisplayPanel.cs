using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

using LibCore;
using CoreUtils;
using Network;
using CommonGUI;
using GameManagement;
using Charts;

using Cloud.OpsEngine;

namespace Cloud.OpsScreen
{
	public class PlannerDisplayPanel : PopupPanel
	{
		protected NodeTree model;
		protected Node datacenter;
		protected Node roundVariables;
		protected Node time;

		protected ImageTextButton close;
		ImageTextButton plannerCloseButton;

		protected Panel plannerPanel;
		protected OrderPlanner orderPlanner;

		protected NetworkProgressionGameFile gameFile;

		protected Bitmap FullBackgroundImage;
		protected Bitmap ExtractedBackgroundImage;

		protected Hashtable ExchangeNodes = new Hashtable();
		protected ArrayList ExchangeNodeNameList = new ArrayList();
		protected Hashtable ExchangeButtons = new Hashtable();

		protected Font Font_Title;
		protected bool AutoCloseonNextClick = false;

		StackBarChartWithBackground barChart;

		public PlannerDisplayPanel(NetworkProgressionGameFile gameFile, NodeTree model, OrderPlanner orderPlanner)
		{
			this.gameFile = gameFile;
			this.model = model;

			FullBackgroundImage = (Bitmap)Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\consolidate\\general.png");
			BackgroundImage = FullBackgroundImage;

			ExtractedBackgroundImage = new Bitmap(FullBackgroundImage.Width, FullBackgroundImage.Height - 40);
			Graphics g = Graphics.FromImage(ExtractedBackgroundImage);

			g.SmoothingMode = SmoothingMode.AntiAlias;
			g.CompositingQuality = CompositingQuality.HighQuality;
			g.PixelOffsetMode = PixelOffsetMode.HighQuality;
			//Redraw
			int w = FullBackgroundImage.Width;
			int h = FullBackgroundImage.Height;
			Rectangle destRect = new Rectangle(0, 0, w, h);
			Rectangle srcRect = new Rectangle(0, 40, w, h-40);
			g.DrawImage(FullBackgroundImage, destRect, srcRect, GraphicsUnit.Pixel);
			g.Dispose();//dispose the Graphics 

			string font = SkinningDefs.TheInstance.GetData("fontname");
			Font_Title = FontRepository.GetFont(font, 16, FontStyle.Bold);

			roundVariables = model.GetNamedNode("RoundVariables");

			if (AutoCloseonNextClick)
			{
				time = model.GetNamedNode("CurrentTime");
				time.AttributesChanged += new Node.AttributesChangedEventHandler(time_AttributesChanged);
			}

			AutoScroll = true;

			this.orderPlanner = orderPlanner;

			Build_Controls();

			DoSize_New();
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing)
			{
				if (AutoCloseonNextClick)
				{
					if (time != null)
					{
						time.AttributesChanged -= new Node.AttributesChangedEventHandler(time_AttributesChanged);
					}
				}
			}
			base.Dispose(disposing);
		}

		protected void Build_Controls()
		{
			int startx = 10;
			int starty = 50;
			int posx = startx;
			int posy = starty;
			int count = 0;

			//Build the close Button 
			close = new ImageTextButton(@"images\buttons\button_85x25.png");
			close.SetAutoSize();
			close.SetButtonText("Cancel", Color.White, Color.White, Color.White, Color.DimGray);
			close.ButtonPressed += new ImageButton.ImageButtonEventArgsHandler(close_ButtonPressed);
			Controls.Add(close);
		
			//Get the Regions
			foreach (Node business in orderPlanner.GetBusinesses())
			{
				string display_str = business.GetAttribute("desc");
				if (ExchangeNodes.ContainsKey(display_str) == false)
				{
					ExchangeNodes.Add(display_str, business);
					ExchangeNodeNameList.Add(display_str);
				}
			}

			ArrayList al = new ArrayList();
			foreach (string ex in ExchangeNodeNameList)
			{
				Node tmpBusinessNode = (Node)ExchangeNodes[ex];

				al.Add(new ToggleButtonBarItem(count, ex, tmpBusinessNode));
				count++;
				if (ex.Equals("Asia", StringComparison.InvariantCultureIgnoreCase))
				{
					datacenter = model.GetNamedNode(tmpBusinessNode.GetAttribute("datacenter"));
				}
			}

			ToggleButtonBar tbb = new ToggleButtonBar(false);
			tbb.SetAllowNoneSelected(false);
			tbb.BackColor = Color.Transparent;
			tbb.SetOptions(al, 75, 32, 10, 4, "images/buttons/button_70x32_on.png", "images/buttons/button_70x32_active.png",
				"images/buttons/button_70x32_disabled.png", "images/buttons/button_70x32_hover.png", Color.White, "Asia");
			tbb.Size = new Size(420, 50);
			tbb.Location = new Point(15, 0);
			tbb.sendItemSelected += new ToggleButtonBar.ItemSelectedHandler(tbb_sendItemSelected);

			Controls.Add(tbb);

			BuildPlanner();
			Refresh();
		}

		protected void tbb_sendItemSelected(object sender, string item_name_selected, object selected_object)
		{
			string hh = item_name_selected;

			Node BusinessNode = (Node)selected_object;

			datacenter = model.GetNamedNode(BusinessNode.GetAttribute("datacenter"));

			BuildPlanner();
			Refresh();
		}

		void time_AttributesChanged (Node sender, ArrayList attrs)
		{
			if (time.GetIntAttribute("seconds", 0) > 0)
			{
				OnClosed();
			}
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);
			DoSize_New();
		}

		protected void DoSize_New()
		{
			close.Location = new Point(Width - 10 - close.Width, Height - 10 - close.Height);
			plannerPanel.Location = new Point(0, 40);
			plannerPanel.Size = new Size(Width, Height - plannerPanel.Top);
			plannerCloseButton.Location = new Point(plannerPanel.Width - 10 - plannerCloseButton.Width, plannerPanel.Height - 10 - plannerCloseButton.Height);
			barChart.Size = new Size(plannerPanel.Width - barChart.Left, plannerPanel.Height - barChart.Top);
		}

		void plannerButton_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			BuildPlanner();
		}

		void BuildPlanner()
		{
			plannerPanel = new Panel ();
			plannerPanel.Location = new Point(0, 40);
			plannerPanel.Size = new Size(1020, 685 - 40);

			using (WaitCursor cursor = new WaitCursor(this))
			{
                ReportsScreen.CpuUsageReport report = new ReportsScreen.CpuUsageReport(gameFile, gameFile.CurrentRound, true);
				string reportFile = report.BuildReport(new List<string>(new string[] { datacenter.GetAttribute("name") }), new List<string>(new string[] { "floor", "online" }));

				barChart = new StackBarChartWithBackground(BasicXmlDocument.CreateFromFile(reportFile).DocumentElement);
				barChart.SetBackImageFromBitmap(ExtractedBackgroundImage, true);
				barChart.Size = plannerPanel.Size;
				plannerPanel.Controls.Add(barChart);

				barChart.BackColor = Color.White;
				barChart.YAxisWidth = 50;
				barChart.XAxisHeight = 50;
				barChart.LeftMargin = 0;
				barChart.TopMargin = 50;
				barChart.RightMargin = 200;
				barChart.BottomMargin = 8;
				barChart.LegendX = 100;
				barChart.LegendY = 0;
				barChart.YAxisLegendMargin = 20;

				plannerCloseButton = new ImageTextButton(@"images\buttons\button_85x25.png");
				plannerCloseButton.SetButtonText("Close", Color.White, Color.White, Color.White, Color.DimGray);
				plannerCloseButton.ButtonPressed += new ImageButton.ImageButtonEventArgsHandler(plannerCloseButton_ButtonPressed);
				barChart.Controls.Add(plannerCloseButton);
				plannerCloseButton.SetAutoSize();
				plannerCloseButton.Location = new Point(barChart.Width - 50 - plannerCloseButton.Width, barChart.Height - 50 - plannerCloseButton.Height);

				Controls.Add(plannerPanel);
				plannerPanel.BringToFront();
			}
		}

		void plannerCloseButton_ButtonPressed (object sender, ImageButtonEventArgs args)
		{
			ClosePlanner();
			OnClosed();
		}

		void ClosePlanner()
		{
			plannerPanel.Parent.Controls.Remove(plannerPanel);
		}

		void close_ButtonPressed (object sender, ImageButtonEventArgs args)
		{
			OnClosed();
		}

		public override Size getPreferredSize()
		{
			return new Size(1020, 685);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
			e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
			e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

			e.Graphics.FillRectangle(Brushes.Black, 0, 0, Width, Height);
			if (FullBackgroundImage != null)
			{
				//Draw the back
				e.Graphics.DrawImage(FullBackgroundImage, 0, 0, Width, Height);
			}
		}

		public override bool IsFullScreen => true;

	}
}