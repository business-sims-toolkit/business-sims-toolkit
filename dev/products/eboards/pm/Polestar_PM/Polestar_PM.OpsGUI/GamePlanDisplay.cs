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
using Polestar_PM.OpsEngine;

using GameManagement;

namespace Polestar_PM.OpsGUI
{
	public class GamePlanDisplay : FlickerFreePanel
	{
		IDataEntryControlHolder mainPanel;
		NodeTree model;
		ImageTextButton closeButton;
		GamePlanPanel MyGamePlanPanel = null;

		private Font MyDefaultSkinFontBold10 = null;
		private Font MyDefaultSkinFontBold12 = null;
		protected NetworkProgressionGameFile _gameFile;

		int round;

		public GamePlanDisplay (IDataEntryControlHolder mainPanel, 
			NetworkProgressionGameFile gameFile,  NodeTree model, int round)
		{
			BackColor = Color.White;

			_gameFile = gameFile;
			this.mainPanel = mainPanel;
			this.model = model;
			this.round = round;

			Node CurrDayNode = this.model.GetNamedNode("CurrentDay");
			int current_day = CurrDayNode.GetIntAttribute("day", 0);

			string fontname =  SkinningDefs.TheInstance.GetData("fontname");
			MyDefaultSkinFontBold10 = ConstantSizeFont.NewFont(fontname,10f,FontStyle.Bold);
			MyDefaultSkinFontBold12 = ConstantSizeFont.NewFont(fontname,12f,FontStyle.Bold);

			MyGamePlanPanel = new GamePlanPanel(round, false);
			MyGamePlanPanel.Location = new System.Drawing.Point(30, 0);
			MyGamePlanPanel.Size = new System.Drawing.Size(this.Width - 60, this.Height-40);
			MyGamePlanPanel.SetBackColor(Color.White);
			MyGamePlanPanel.setModel(this.model);
			MyGamePlanPanel.LoadData(gameFile);
			//MyGamePlanPanel.setModel(model);
			//MyGamePlanPanel.LoadData(gameFile);
			//MyGamePlanPanel.setChartType(emProjectPerfChartType.BOTH);
			this.Controls.Add(MyGamePlanPanel);

			closeButton = new ImageTextButton (0);
			closeButton.SetButton(AppInfo.TheInstance.Location + "\\images\\buttons\\button_70x25.png");
			closeButton.Location = new System.Drawing.Point(2+940,8+640+6);
			closeButton.Name = "newBtnCancel";
			closeButton.Size = new System.Drawing.Size(70,25);
			closeButton.TabIndex = 22;
			closeButton.ButtonFont = MyDefaultSkinFontBold10;
			closeButton.SetButtonText("Close",
				System.Drawing.Color.Black,System.Drawing.Color.Black,
				System.Drawing.Color.Green,System.Drawing.Color.Gray);
			closeButton.Click += new System.EventHandler (closeButton_Click);
			closeButton.BringToFront();
			this.Controls.Add(closeButton);

			this.Resize += new EventHandler(GamePlanDisplay_Resize);

			//RefreshTheTimeSheets(gameFile);
		}

		public void LoadData()
		{
			MyGamePlanPanel.LoadData(_gameFile);
		}

		/// <summary>
		/// Dispose ...
		/// </summary>
		new public void Dispose()
		{
			if (MyGamePlanPanel != null)
			{
				MyGamePlanPanel.Dispose();
				MyGamePlanPanel = null;
			}

			//get rid of the Font
			if (MyDefaultSkinFontBold10 != null)
			{
				MyDefaultSkinFontBold10.Dispose();
				MyDefaultSkinFontBold10 = null;
			}
			//get rid of the Font
			if (MyDefaultSkinFontBold12 != null)
			{
				MyDefaultSkinFontBold12.Dispose();
				MyDefaultSkinFontBold12 = null;
			}
		}

		public void RefreshTheTimeSheets(ProjectManager projectManager)
		{
			projectManager.RebuildFutureTimesheets();
		}

		private void GamePlanDisplay_Resize(object sender, EventArgs e)
		{
			if (MyGamePlanPanel != null)
			{
				MyGamePlanPanel.Size = new System.Drawing.Size(this.Width - 60, this.Height - 40);
			}
		}

		void closeButton_Click (object sender, EventArgs args)
		{
			mainPanel.DisposeEntryPanel();
		}
	}
}