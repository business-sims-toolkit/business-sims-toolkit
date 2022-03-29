using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Xml;

using Network;
using CommonGUI;
using LibCore;
using CoreUtils;
using BusinessServiceRules;
using Polestar_PM.DataLookup;
using Polestar_PM.OpsEngine;

namespace Polestar_PM.OpsGUI
{
	/// <summary>
	/// Change PMO allows the players to change a number PMO based attributes
	/// Only one attribute is editable at this time "PMO Budget"
	/// </summary>

	public class PM_PopupAnalysisChartsDisplay  : FlickerFreePanel
	{
		protected IDataEntryControlHolder _mainPanel;
		protected NodeTree MyNodeTree = null;

		private System.Windows.Forms.Panel pnl_ChooseChart;
		private ImageTextButton newBtnCancel = new ImageTextButton(0);
		protected Font MyDefaultSkinFontBold12 = null;
		protected Font MyDefaultSkinFontBold10 = null;
		protected Font MyDefaultSkinFontNormal10 = null;
		protected bool showsmall = true;

		private System.Windows.Forms.Label titleLabel;
		private System.Windows.Forms.Label helpLabel;

		protected Point small_location = new Point(0,0);
		protected Size  small_size = new Size(10,10);
		protected Point large_location = new Point(0,0);
		protected Size  large_size = new Size(10,10);

		protected bool ChartChosen = false;
		protected bool display_small = true;

		Dictionary<string, string> displaynameToSmallImageName = new Dictionary<string, string>();
		Dictionary<string, string> displaynameToLargeImageName = new Dictionary<string, string>();

		protected Image normal_back = null;
		protected Image chart_back_small = null;
		protected Image chart_back_large = null;

		public PM_PopupAnalysisChartsDisplay(IDataEntryControlHolder mainPanel, NodeTree tree, int round)
		{
			MyNodeTree = tree;
			_mainPanel = mainPanel;

			normal_back = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images\\panels\\PM_opsback.png");

			string fontname = SkinningDefs.TheInstance.GetData("fontname");
			MyDefaultSkinFontBold10 = ConstantSizeFont.NewFont(fontname, 10, FontStyle.Bold);
			MyDefaultSkinFontBold12 = ConstantSizeFont.NewFont(fontname, 12, FontStyle.Bold); 
			MyDefaultSkinFontNormal10 = ConstantSizeFont.NewFont(fontname, 10, FontStyle.Regular);

			//string static_small_bubble_image = AppInfo.TheInstance.Location + "images\\bubbles\\small_bubbles_panel_"+CONVERT.ToStr(round)+".png";
			//my_back_image = Repository.TheInstance.GetImage(static_small_bubble_image);

			//this.ShowInTaskbar = false;
			//this.ClientSize = new Size (520,280);
			//this.FormBorderStyle = FormBorderStyle.None;
			//this.Opacity = 1.00;
			this.Size = new Size(520,255);

			//Build the Title and Help text buttons
			titleLabel = new System.Windows.Forms.Label();
			titleLabel.BackColor = System.Drawing.Color.Transparent;
			titleLabel.Font = MyDefaultSkinFontBold12;
			titleLabel.ForeColor = System.Drawing.Color.Black;
			titleLabel.Location = new System.Drawing.Point(110 - 25, 10 - 2);
			titleLabel.Name = "titleLabel";
			titleLabel.Size = new System.Drawing.Size(380, 18);
			titleLabel.TabIndex = 11;
			titleLabel.Text = "Select Analysis Chart";
			titleLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.Controls.Add(titleLabel);

			helpLabel = new System.Windows.Forms.Label();
			helpLabel.BackColor = System.Drawing.Color.Transparent;
			helpLabel.Font = MyDefaultSkinFontNormal10;
			helpLabel.ForeColor = System.Drawing.Color.Black;
			helpLabel.Location = new System.Drawing.Point(110 - 25, 50 - 20 - 1);
			helpLabel.Name = "helpLabel";
			helpLabel.Size = new System.Drawing.Size(380, 18);
			helpLabel.TabIndex = 20;
			helpLabel.Text = "Please select the chart that you require";
			helpLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.Controls.Add(helpLabel);

			Build_Chart_Choice_Controls();

			newBtnCancel.SetButton(AppInfo.TheInstance.Location+"\\images\\buttons\\blank.png");
			newBtnCancel.Location = new System.Drawing.Point(400, 210);
			newBtnCancel.Name = "newBtnCancel";
			newBtnCancel.Size = new System.Drawing.Size(65, 25);
			newBtnCancel.TabIndex = 22;
			newBtnCancel.ButtonFont = MyDefaultSkinFontBold10;
			//newBtn.ForeColor = System.Drawing.Color.Black;
			newBtnCancel.SetButtonText("Close",
				System.Drawing.Color.Black, System.Drawing.Color.Black,
				System.Drawing.Color.Green, System.Drawing.Color.Gray);
			//newBtn.ButtonPressed += new ProjectMngmntGamePanels.ImageButton.ImageButtonEventArgsHandler(playButton_ButtonPressed);
			newBtnCancel.Click += new System.EventHandler(this.newBtnCancel_Click);
			this.Controls.Add(newBtnCancel);
			newBtnCancel.BringToFront();
			this.Click += new EventHandler(PM_PopupAnalysisChartsDisplay_Click);
		}

		public void Build_Chart_Choice_Controls()
		{	
			//Build the panel 
			pnl_ChooseChart = new System.Windows.Forms.Panel();
			pnl_ChooseChart.Location = new System.Drawing.Point(10+75, 60);
			pnl_ChooseChart.Size = new System.Drawing.Size(390, 140);
			pnl_ChooseChart.Name = "pnl_ChooseSlot";
			//pnl_ChooseChart.BackColor = Color.FromArgb(218,218,203); //TODO SKIN
			pnl_ChooseChart.BackColor = Color.White;
			//pnl_ChooseChart.BackColor = Color.Yellow;
			pnl_ChooseChart.TabIndex = 13;
			pnl_ChooseChart.Visible = true;
			this.Controls.Add(pnl_ChooseChart);

			//Read the xml file
			string chart_definition_filename = LibCore.AppInfo.TheInstance.Location + "data//PMG_AnalysisCharts.xml";
			XmlDocument doc = new XmlDocument();
			doc.Load(chart_definition_filename);
			XmlNode root = doc.FirstChild;

			string root_text = root.InnerText;
			foreach (XmlElement xe in root.ChildNodes)
			{
				string xe_txt = xe.InnerText;
				string xe_txt2 = xe.InnerXml;
				string xe_txt3 = xe.InnerXml;
				string display_name = xe.Attributes["displayname"].Value;
				string small_image_name = xe.Attributes["small_image"].Value;
				string large_image_name = xe.Attributes["large_image"].Value;

				if (displaynameToSmallImageName.ContainsKey(display_name) == false)
				{
					displaynameToSmallImageName.Add(display_name, small_image_name);
				}
				if (displaynameToLargeImageName.ContainsKey(display_name) == false)
				{
					displaynameToLargeImageName.Add(display_name, large_image_name);
				}
			}
			//Now build the Buttons to allow the user to Select The correct Item

			int offset_x = 5;
			int offset_y = 5;
			int button_width = 130;
			int button_height = 25;
			int button_sep = 10;
			int stepx = 0;
			int stepy = 0;

			foreach (string dispName in displaynameToSmallImageName.Keys)
			{
				string display_text = dispName;
				//Build the button 
				ImageTextButton btnChartSelection = new ImageTextButton(0);
				btnChartSelection.SetButton(AppInfo.TheInstance.Location+"\\images\\buttons\\blank.png");
				//btnChartSelection.Location = new System.Drawing.Point(offset_x + (button_width + button_sep) * (step), 20);
				btnChartSelection.Location = new System.Drawing.Point(offset_x + (button_width + button_sep) * (stepx), offset_y + (button_height + button_sep) * (stepy));
				btnChartSelection.Name = "Button1";
				btnChartSelection.Size = new System.Drawing.Size(130, 25);
				btnChartSelection.TabIndex = 8;
				btnChartSelection.ButtonFont = MyDefaultSkinFontBold10;
				btnChartSelection.Tag = dispName;
				btnChartSelection.SetButtonText(display_text,
					System.Drawing.Color.Black, System.Drawing.Color.Black,
					System.Drawing.Color.Green, System.Drawing.Color.Gray);
				btnChartSelection.Click += new System.EventHandler(this.ChartSelect_Button_Click);
				pnl_ChooseChart.Controls.Add(btnChartSelection);
				stepy++;
				if (stepy > 3)
				{
					stepy = 0;
					stepx ++;
				}
			}
		}

		new public void Dispose()
		{
			if (MyDefaultSkinFontBold10 != null)
			{
				MyDefaultSkinFontBold10.Dispose();
				MyDefaultSkinFontBold10 = null;
			}
			if (MyDefaultSkinFontBold12 != null)
			{
				MyDefaultSkinFontBold12.Dispose();
				MyDefaultSkinFontBold12 = null;
			}
			if (MyDefaultSkinFontNormal10 != null)
			{
				MyDefaultSkinFontNormal10.Dispose();
				MyDefaultSkinFontNormal10 = null;
			}
		}

		protected Image GetIsolatedImage(string PreferedImageFileName)
		{
			Bitmap tmp_img = null;
			Bitmap hack = null;

			if (File.Exists(PreferedImageFileName))
			{
				tmp_img = new Bitmap(PreferedImageFileName);
				tmp_img = (Bitmap)tmp_img.Clone();
			}
			if (tmp_img != null)
			{
				hack = new Bitmap(tmp_img.Width, tmp_img.Height);
				Graphics g = Graphics.FromImage(hack);
				g.DrawImage(tmp_img, 0, 0, (int)hack.Width, (int)hack.Height);
				g.Dispose();
				tmp_img.Dispose();
				tmp_img = null;
				System.GC.Collect();
			}
			return hack;
		}

		private void ChartSelect_Button_Click(object sender, System.EventArgs e)
		{
			ImageTextButton tmpbtn = (ImageTextButton) sender;
			string dispname = (string) tmpbtn.Tag;
			if (displaynameToSmallImageName.ContainsKey(dispname))
			{
				string large_image_name  = displaynameToSmallImageName[dispname];
				string small_image_name  = displaynameToLargeImageName[dispname];

				string dirname = LibCore.AppInfo.TheInstance.Location + "images\\bubbles\\";
				string chart_back_small_filename = dirname + small_image_name;
				string chart_back_large_filename = dirname + large_image_name;

				chart_back_small = GetIsolatedImage(chart_back_small_filename);
				chart_back_large = GetIsolatedImage(chart_back_large_filename);
				showsmall = true;
				ChartChosen = true;
				this.pnl_ChooseChart.Visible = false;
				titleLabel.Visible = false;
				helpLabel.Visible = false;
				newBtnCancel.BringToFront();
				this.Invalidate();
			}
		}
		
		public void SetSmallPosAndSize(int xpos, int ypos, int new_width, int new_height)
		{
			small_location.X = xpos;
			small_location.Y = ypos;
			small_size.Width = new_width;
			small_size.Height = new_height;
		}
		public void SetLargePosAndSize(int xpos, int ypos, int new_width, int new_height)
		{
			large_location.X = xpos;
			large_location.Y = ypos;
			large_size.Width = new_width;
			large_size.Height = new_height;
		}

		void PM_PopupAnalysisChartsDisplay_Click(object sender, EventArgs e)
		{
			if (showsmall)
			{
				this.Visible = false;
				this.Location = large_location;
				this.Size = large_size;
				showsmall = false;
				this.Visible = true;
			}
			else
			{
				this.Visible = false;
				this.Location = small_location;
				this.Size = small_size;
				showsmall = true;
				this.Visible = true;
			}
			Refresh();
		}


		private void newBtnOK_Click(object sender, System.EventArgs e)
		{
		}

		private void newBtnCancel_Click(object sender, System.EventArgs e)
		{
			_mainPanel.DisposeEntryPanel();
		}

		public void SetFocus ()
		{
			Focus();
			newBtnCancel.Focus();
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			//Setup the Quality and Rendering Options 
			e.Graphics.SmoothingMode = SmoothingMode.None;
			e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
			e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

			if (ChartChosen == false)
			{
				if (normal_back != null)
				{
					e.Graphics.DrawImage(normal_back, 0, 0, this.Width, this.Height);
				}
			}
			else
			{
				if (display_small)
				{
					if (chart_back_small != null)
					{
						e.Graphics.DrawImage(chart_back_small, 0, 0, this.Width, this.Height);
					}
				}
				else
				{
					if (chart_back_large != null)
					{
						e.Graphics.DrawImage(chart_back_large, 0, 0, this.Width, this.Height);
					}
				}
			}
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);
			DoSize();
		}

		protected void DoSize()
		{
			if (showsmall)
			{
				newBtnCancel.Location = new System.Drawing.Point(this.Width - 80, this.Height - 40);
			}
			else
			{
				newBtnCancel.Location = new System.Drawing.Point(this.Width - 75, this.Height - 40);
			}
		}

	}
}