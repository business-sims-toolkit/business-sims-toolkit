using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Reflection;
using System.IO;
using System.Data;

using System.Xml;

using Polestar_PM.OpsScreen;
using GameDetails;

using LibCore;
using CoreUtils;

using System.Threading;
using GameManagement;
using Licensor;
using Logging;

namespace Polestar_PM.Application
{
	/// <summary>
	/// Summary description for Form1.
	/// </summary>
	public class MainGameForm : System.Windows.Forms.Form
	{
		private CompleteGamePanel mainPanel;

		private IGameLicence license;
		TacPermissions tacPermissions;

		/// <summary>
		/// Constructor for the main form.
		/// </summary>
		public MainGameForm(IGameLicence _license, TacPermissions tacPermissions)
		{
			license = _license;
			this.tacPermissions = tacPermissions;

			InitializeComponent();
			//
			this.Icon = new Icon(AppInfo.TheInstance.InstallLocation + "\\App.ico");

			bool test_lang_system = true;

			if (test_lang_system)
			{
				string app_name = "Polestar PM";

				TextTranslator.TheInstance.DetermineLanguageSystemStatus(app_name);
			}			

			//
			mainPanel = new CompleteGamePanel (_license, tacPermissions, true);
			mainPanel.Size = this.ClientSize;
			this.Controls.Add(mainPanel);
			mainPanel.PlayPressed += new Polestar_PM.OpsScreen.CompleteGamePanel.PlayPressedHandler(mainPanel_PlayPressed);
			mainPanel.GameSetup += new Polestar_PM.OpsScreen.CompleteGamePanel.GameSetupdHandler(mainPanel_GameSetup);
			mainPanel.PasswordCheck += new Polestar_PM.OpsScreen.CompleteGamePanel.PasswordCheckHandler(mainPanel_PasswordCheck);
		}

		public void SetTacPermissions (TacPermissions tacPermissions)
		{
			this.tacPermissions = tacPermissions;
		}
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			mainPanel.Dispose();
			base.Dispose(disposing);
		}

		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainGameForm));
			this.SuspendLayout();
			// 
			// MainGameForm
			// 
			this.ClientSize = new System.Drawing.Size(1024, 768);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
			//this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "MainGameForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
			this.Text = BaseUtils.AssemblyExtensions.MainAssemblyTitle;
			this.ResumeLayout(false);

		}

		private void mainPanel_PlayPressed(GameManagement.GameFile game)
		{
			// Case 2466:   can create more games than have credits for 
			// We don't mint coupouns on pressing play. We mint them on game creation.
		}

		private Polestar_PM.OpsScreen.CompleteGamePanel.GameStartType mainPanel_GameSetup(GameFile game)
		{
			// Mint the coupon if we don't have one yet!
			string couponFileName = game.Dir + "/global/coupon.xml";
			if(!File.Exists(couponFileName))
			{
				//What type of game
				//What type of game
				// No coupon file so mint one and save it into the file.
				string detailsFileName = game.Dir + "/global/details.xml";
				XmlDocument xdoc = new XmlDocument();
				xdoc.Load(detailsFileName);

				CouponGameDetails gameDetails = new CouponGameDetails();
				gameDetails.Title = xdoc.DocumentElement.SelectSingleNode("Title").InnerText;
				gameDetails.Location = xdoc.DocumentElement.SelectSingleNode("Location").InnerText;
				gameDetails.Venue = xdoc.DocumentElement.SelectSingleNode("Venue").InnerText;
				gameDetails.Client = xdoc.DocumentElement.SelectSingleNode("Client").InnerText;
				string players = xdoc.DocumentElement.SelectSingleNode("Players").InnerText;
				gameDetails.PlayerCount = CONVERT.ParseInt(players);
				gameDetails.Country = xdoc.DocumentElement.SelectSingleNode("Country").InnerText;
				gameDetails.GeoRegion = xdoc.DocumentElement.SelectSingleNode("GeoRegion").InnerText;
				gameDetails.Purpose = xdoc.DocumentElement.SelectSingleNode("Purpose").InnerText;
				gameDetails.ChargeCompany = XMLUtils.GetOrCreateElement(xdoc.DocumentElement, "ChargeCompany").InnerText;

				//Handling Class Ident (
				XmlNode nTestNode = xdoc.DocumentElement.SelectSingleNode("ClassIdent");

				if (nTestNode != null)
				{
					gameDetails.ClassId = nTestNode.InnerText;
				}

				//
				XmlNode nNode = xdoc.DocumentElement.SelectSingleNode("Notes");
				string notes = "No Notes";
				if(nNode != null)
				{
					notes = nNode.InnerText;
				}

				gameDetails.Notes = notes;

				string gametype = string.Empty;
				XmlNode gameTypeNode = xdoc.DocumentElement.SelectSingleNode("GameType");
				if (gameTypeNode != null)
				{
					gameDetails.Type = gameTypeNode.InnerText;
				}
				//
				if (true)
				{
					return CoreScreens.BaseCompleteGamePanel.GameStartType.MintedNewCoupon;
				}
				else
				{
					game.Delete();
					game.Dispose();
					game = null;
					MessageBox.Show("There was a problem creating a Licensed Game. The Application will now exit. Please restart and try again. If problems persist, please contact support", "Licensing Error");
					System.Windows.Forms.Application.Exit();

					return CoreScreens.BaseCompleteGamePanel.GameStartType.Failed;
				}
			}
			return CoreScreens.BaseCompleteGamePanel.GameStartType.CouponAlreadyExisted;
		}

		private bool mainPanel_PasswordCheck(string password)
		{
			return true;
		}
	}
}