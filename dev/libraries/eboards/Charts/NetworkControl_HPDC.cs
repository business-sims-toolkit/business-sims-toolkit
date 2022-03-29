using System.Collections;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;

using LibCore;

namespace Charts
{
	/// <summary>
	/// Summary description for NetworkControl_HPDC.
	/// </summary>
	public class NetworkControl_HPDC : Chart
	{
		static System.Globalization.CultureInfo myCI_enGB = new System.Globalization.CultureInfo( "en-GB", false );

		ArrayList servers;

		System.Windows.Forms.Label Zone2Title;
		BasePanel Zone2BackPanel;
		BasePanel Zone1BackPanel;
		System.Windows.Forms.Label Zone1Title;
		BasePanel Zone6BackPanel;
		System.Windows.Forms.Label Zone6Title;
		BasePanel Zone7BackPanel;
		System.Windows.Forms.Label Zone7Title;
		BasePanel Zone5BackPanel;
		System.Windows.Forms.Label Zone5Title;
		BasePanel F326;
		BasePanel H328;
		BasePanel H327;
		BasePanel H326;
		BasePanel H325;
		BasePanel F325;
		BasePanel F327;
		BasePanel E330;
		BasePanel E329;
		BasePanel E328;
		BasePanel E331;
		BasePanel D323;
		BasePanel D322;
		BasePanel C326;
		BasePanel C325;
		BasePanel C324;
		BasePanel Zone4BackPanel;
		BasePanel G325;
		BasePanel G324;
		System.Windows.Forms.Label Zone4Title;
		BasePanel Zone3BackPanel;
		BasePanel B327;
		BasePanel B326;
		BasePanel B325;
		BasePanel B324;
		System.Windows.Forms.Label Zone3Title;

		BasePanel ZonePowerBackPanel;
		BasePanel ZonePowerCorePanel;
		System.Windows.Forms.Label ZonePowerTitle;

		Color boardBackgroundColour=Color.Transparent;
		Color boardViewZone1Colour=Color.Transparent;
		Color boardViewZone2Colour=Color.Transparent;
		Color boardViewZone3Colour=Color.Transparent;
		Color boardViewZone4Colour=Color.Transparent;
		Color boardViewZone5Colour=Color.Transparent;
		Color boardViewZone6Colour=Color.Transparent;
		Color boardViewZone7Colour=Color.Transparent;
		Color boardViewPowerColour= Color.Transparent;

		Color boardViewNetworkReportDefaultTextColour = Color.Transparent;
		Color boardViewNetworkReportDefaultUpgradedMemoryColour = Color.Transparent;
		Color boardViewNetworkReportDefaultUpgradedStorageColour = Color.Transparent;

		Color boardViewNetworkDefaultColor_CreatedbySIP = Color.Transparent;
		Color boardViewNetworkDefaultColor_UpgradedbySIP = Color.Transparent;

		protected bool AllowedExtendedServerName = false; 
		protected bool ShowIndividualPower = false; 
		protected int[] PowerCurrentDemand = new int[7];
		protected int[] PowerCurrentContract = new int[7];
		protected bool [] ZoneActive = new bool [7];
		protected int [] ZoneTier = new int [7];
		
		//protected Label proc;
		//protected bool show_proc = true;

		/// <summary> 
		/// Required designer variable.
		/// </summary>
		System.ComponentModel.Container components = null;

		public NetworkControl_HPDC()
		{
			//Set Zone Colours from Skin File
			boardBackgroundColour=CoreUtils.SkinningDefs.TheInstance.GetColorData("boardview_background_colour");
			boardViewZone1Colour=CoreUtils.SkinningDefs.TheInstance.GetColorData("boardview_zone1_colour");
			boardViewZone2Colour=CoreUtils.SkinningDefs.TheInstance.GetColorData("boardview_zone2_colour");
			boardViewZone3Colour=CoreUtils.SkinningDefs.TheInstance.GetColorData("boardview_zone3_colour");
			boardViewZone4Colour=CoreUtils.SkinningDefs.TheInstance.GetColorData("boardview_zone4_colour");
			boardViewZone5Colour=CoreUtils.SkinningDefs.TheInstance.GetColorData("boardview_zone5_colour");
			boardViewZone6Colour=CoreUtils.SkinningDefs.TheInstance.GetColorData("boardview_zone6_colour");
			boardViewZone7Colour=CoreUtils.SkinningDefs.TheInstance.GetColorData("boardview_zone7_colour");
			boardViewPowerColour=CoreUtils.SkinningDefs.TheInstance.GetColorData("boardview_powerzone_colour");

			// : to fix bug 3544 (invisible storage upgrade text), we now skin the text colours
			// used in the network reports, but make the defaults match the existing (unreadable)
			// behaviour, because PS's colour scheme relies on it.
			boardViewNetworkReportDefaultTextColour = CoreUtils.SkinningDefs.TheInstance.GetColorDataGivenDefault("report_network_default_text_colour", Color.Black);
			boardViewNetworkReportDefaultUpgradedMemoryColour = CoreUtils.SkinningDefs.TheInstance.GetColorDataGivenDefault("report_network_upgraded_memory_text_colour", Color.Red);
			boardViewNetworkReportDefaultUpgradedStorageColour = CoreUtils.SkinningDefs.TheInstance.GetColorDataGivenDefault("report_network_upgraded_storage_text_colour", Color.White);
			boardViewNetworkDefaultColor_CreatedbySIP = CoreUtils.SkinningDefs.TheInstance.GetColorDataGivenDefault("report_network_createdsip_text_colour", Color.LightSteelBlue);
			boardViewNetworkDefaultColor_UpgradedbySIP = CoreUtils.SkinningDefs.TheInstance.GetColorDataGivenDefault("report_network_upgradedsip_text_colour", Color.White);

			// Allow use of longer server names (shift app names down)
			AllowedExtendedServerName = false;
			string AllowedExtendedServerName_str = CoreUtils.SkinningDefs.TheInstance.GetData("network_report_extendedservernames");
			if (AllowedExtendedServerName_str.ToLower()=="true")
			{
				AllowedExtendedServerName = true;
			}

			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
			BuildPowerDisplay();
			servers = new ArrayList();
		}

		public void BuildPowerDisplay()
		{
			this.SuspendLayout();

			ZonePowerBackPanel = new BasePanel();
			ZonePowerBackPanel.BackColor=boardViewPowerColour;
//			ZonePowerBackPanel.Controls.Add(this.ZonePowerTitle);
			ZonePowerBackPanel.Location = new System.Drawing.Point(196, 243);
			ZonePowerBackPanel.Name = "";
			ZonePowerBackPanel.Size = new System.Drawing.Size(576, 108);
			ZonePowerBackPanel.TabIndex = 8;
			
			this.Controls.Add(ZonePowerBackPanel);
			ZonePowerBackPanel.BringToFront();

			this.ResumeLayout();
		}

		public void ShowPowerDisplay()
		{
			ZonePowerBackPanel.SuspendLayout();
			foreach(Control c in ZonePowerBackPanel.Controls)
			{
				if(c!=null) c.Dispose();
			}

			ZonePowerCorePanel = new BasePanel();
			ZonePowerCorePanel.BackColor= Color.White;
			//			ZonePowerBackPanel.Controls.Add(this.ZonePowerTitle);
			ZonePowerCorePanel.Location = new System.Drawing.Point(4, 20);
			ZonePowerCorePanel.Name = "";
			ZonePowerCorePanel.Size = new System.Drawing.Size(ZonePowerBackPanel.Width-8, ZonePowerBackPanel.Height-24);
			ZonePowerCorePanel.TabIndex = 9;
			ZonePowerBackPanel.Controls.Add(ZonePowerCorePanel);

			Font DF_Bold = CoreUtils.SkinningDefs.TheInstance.GetFont(8.25F, System.Drawing.FontStyle.Bold);
			Font DF = CoreUtils.SkinningDefs.TheInstance.GetFont(8.25F, System.Drawing.FontStyle.Regular);

			//Build the Title
			ZonePowerTitle = new Label();
			ZonePowerTitle.BackColor = System.Drawing.Color.Transparent;
			//ZonePowerTitle.BackColor = System.Drawing.Color.Violet;
			ZonePowerTitle.Font = DF_Bold;
			ZonePowerTitle.ForeColor = System.Drawing.Color.Black;
			ZonePowerTitle.Location = new System.Drawing.Point(6, 4);
			ZonePowerTitle.Name = "l";
			ZonePowerTitle.Size = new System.Drawing.Size(565, 12);
			ZonePowerTitle.TabIndex = 1;
			ZonePowerTitle.Text = "Power Demand";
			ZonePowerTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			ZonePowerBackPanel.Controls.Add(this.ZonePowerTitle);
			
			//Column 1 
			Label Column1_ZoneTitle = new Label();
			Column1_ZoneTitle.BackColor = System.Drawing.Color.Transparent;
			//Column1_ZoneTitle.BackColor = System.Drawing.Color.GreenYellow;
			Column1_ZoneTitle.Font = DF;
			Column1_ZoneTitle.ForeColor = System.Drawing.Color.Black;
			Column1_ZoneTitle.Location = new System.Drawing.Point(6, 3);
			Column1_ZoneTitle.Size = new System.Drawing.Size(40, 12);
			Column1_ZoneTitle.Text = "Zone";
			Column1_ZoneTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			ZonePowerCorePanel.Controls.Add(Column1_ZoneTitle);

			Label Column1_DemandTitle = new Label();
			//Column1_DemandTitle.BackColor = System.Drawing.Color.Turquoise;
			Column1_DemandTitle.BackColor = System.Drawing.Color.Transparent;
			Column1_DemandTitle.Font = DF;
			Column1_DemandTitle.ForeColor = System.Drawing.Color.Black;
			Column1_DemandTitle.Location = new System.Drawing.Point(50, 3);
			Column1_DemandTitle.Size = new System.Drawing.Size(115, 12);
			Column1_DemandTitle.Text = "Current Demand";
			Column1_DemandTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			ZonePowerCorePanel.Controls.Add(Column1_DemandTitle);

			//Column 2 
			Label Column2_ZoneTitle = new Label();
			//Column2_ZoneTitle.BackColor = System.Drawing.Color.GreenYellow;
			Column2_ZoneTitle.BackColor = System.Drawing.Color.Transparent;
			Column2_ZoneTitle.Font = DF;
			Column2_ZoneTitle.ForeColor = System.Drawing.Color.Black;
			Column2_ZoneTitle.Location = new System.Drawing.Point(6+300-10, 3);
			Column2_ZoneTitle.Size = new System.Drawing.Size(40, 12);
			Column2_ZoneTitle.Text = "Zone";
			Column2_ZoneTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			ZonePowerCorePanel.Controls.Add(Column2_ZoneTitle);

			Label Column2_DemandTitle = new Label();
			//Column2_DemandTitle.BackColor = System.Drawing.Color.Turquoise;
			Column2_DemandTitle.BackColor = System.Drawing.Color.Transparent;
			Column2_DemandTitle.Font = DF;
			Column2_DemandTitle.ForeColor = System.Drawing.Color.Black;
			Column2_DemandTitle.Location = new System.Drawing.Point(50+300-10, 3);
			Column2_DemandTitle.Size = new System.Drawing.Size(115, 12);
			Column2_DemandTitle.Text = "Current Demand";
			Column2_DemandTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			ZonePowerCorePanel.Controls.Add(Column2_DemandTitle);

			//
			int OffsetY=0;
			int OffsetX=0;
			for (int step=0; step< 7; step++)
			{
				if (ZoneActive[step])
				{
					if (step<4)
					{
						OffsetX = 0;
						OffsetY = 37 - 20 + (step)*17;
					}
					else
					{
						OffsetX = 0+300-10;
						OffsetY = 37 - 20 + (step-4)*17;
					}

					Label tmpZone = new Label();
					//tmpZone.BackColor = System.Drawing.Color.GreenYellow;
					tmpZone.Font = DF;
					tmpZone.ForeColor = System.Drawing.Color.Black;
					tmpZone.Location = new System.Drawing.Point(6+OffsetX, OffsetY);
					tmpZone.Size = new System.Drawing.Size(40, 12);
					tmpZone.Text = CONVERT.ToStr(step+1);
					tmpZone.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
					ZonePowerCorePanel.Controls.Add(tmpZone);

					Label tmpDemand = new Label();
					//tmpDemand.BackColor = System.Drawing.Color.Turquoise;
					tmpDemand.Font = DF;
					tmpDemand.ForeColor = System.Drawing.Color.Black;
					tmpDemand.Location = new System.Drawing.Point(50+OffsetX, OffsetY);
					tmpDemand.Size = new System.Drawing.Size(115, 12);
					tmpDemand.Text = CONVERT.ToStr(PowerCurrentDemand[step]);
					tmpDemand.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
					ZonePowerCorePanel.Controls.Add(tmpDemand);
				}
			}
			ZonePowerBackPanel.ResumeLayout();
		}

		Control GetPanelByZone (int zone)
		{
			switch (zone)
			{
				case 0:
					return Zone1BackPanel;

				case 1:
					return Zone2BackPanel;

				case 2:
					return Zone3BackPanel;

				case 3:
					return Zone4BackPanel;

				case 4:
					return Zone5BackPanel;

				case 5:
					return Zone6BackPanel;

				case 6:
					return Zone7BackPanel;
			}

			return null;
		}

		public override void LoadData(string xmldata)
		{
			BasicXmlDocument xdoc = LibCore.BasicXmlDocument.Create(xmldata);
			XmlNode rootNode = xdoc.DocumentElement;

			foreach(XmlNode child in rootNode.ChildNodes)
			{
				//Handle the Servers
				if (child.Name == "server")
				{
					Network_Server server = new Network_Server();

					//get server name and location
					foreach(XmlAttribute att in child.Attributes)
					{
						if (att.Name == "name")
						{
							server.name = att.Value;
						}
						if (att.Name == "location")
						{
							server.location = att.Value;
						}
						if (att.Name == "memory")
						{
							server.memory = att.Value;
						}
						if (att.Name == "storage")
						{
							server.storage = att.Value;
						}
						if (att.Name == "memory_upgraded")
						{
							if (att.Value == "true")
								server.memory_upgraded = true;
							else
								server.memory_upgraded = false;
						}
						if (att.Name == "storage_upgraded")
						{
							if (att.Value == "true")
								server.storage_upgraded = true;
							else
								server.storage_upgraded = false;
						}
						if (att.Name == "proccap")
						{
							server.proc_cap = att.Value;
						}
						server.mirror = false;
						if (att.Name == "mirror")
						{
							server.mirror = true;		
						}
						if (att.Name == "affected_by")
						{
							server.affected_by = att.Value;
						}
					}
					
					//get apps
					foreach (XmlNode n in child.ChildNodes)
					{
						if (n.Name == "slot")
						{
							Network_Slot slot = new Network_Slot();
							slot.project = "";
							slot.upgrade = "";
							foreach(XmlAttribute att in n.Attributes)
							{
								if (att.Name == "name")
								{
									slot.name = att.Value;
								}
								if (att.Name == "location")
								{
									slot.location = att.Value;
								}
								if (att.Name == "type")
								{
									slot.type = att.Value;
								}
								if (att.Name == "project")
								{
									slot.project = att.Value;
								}
								if (att.Name == "upgrade")
								{
									slot.upgrade = att.Value;
								}
								if (att.Name == "proccap")
								{
									slot.proc_cap = att.Value;
								}
							}
							server.slots.Add(slot);
						}
					}
					servers.Add(server);
				}
				else if (child.Name == "power")
				{
					for(int step=0; step<7; step++)
					{
						string pwrdemand_name = "powerdemand_zone"+CONVERT.ToStr((step+1));
						XmlNode xn1 = child.Attributes.GetNamedItem(pwrdemand_name);
						if (xn1 != null)
						{
							int datavalue = 0;
							if (xn1.InnerText != "")
							{
								datavalue = CONVERT.ParseInt(xn1.InnerText);
							}
							PowerCurrentDemand[step] = datavalue;
						}
						string pwrcontract_name = "powercontract_zone"+CONVERT.ToStr((step+1));
						XmlNode xn2 = child.Attributes.GetNamedItem(pwrcontract_name);
						if (xn2 != null)
						{
							int datavalue = 0;
							if (xn2.InnerText != "")
							{
								datavalue = CONVERT.ParseInt(xn2.InnerText);
							}
							PowerCurrentContract[step] = datavalue;
						}
					}
				}
				else if (child.Name == "zones")
				{
					for(int step=0; step<7; step++)
					{
						string pwrdemand_name = "zone"+CONVERT.ToStr((step+1)) + "_active";
						XmlNode xn1 = child.Attributes.GetNamedItem(pwrdemand_name);
						if (xn1 != null)
						{
							bool active = false;
							if (xn1.InnerText != "")
							{
								active = CONVERT.ParseBool(xn1.InnerText, false);
							}
							ZoneActive[step] = active;
							GetPanelByZone(step).Visible = active;
						}

						string tier_name = "zone"+CONVERT.ToStr((step+1)) + "_tier";
						XmlNode xn2 = child.Attributes.GetNamedItem(tier_name);
						if (xn2 != null)
						{
							int tier = 0;
							if (xn2.InnerText != "")
							{
								tier = CONVERT.ParseInt(xn2.InnerText);
							}
							ZoneTier[step] = tier;
						}
					}
				}
			}

			BuildView();
			ShowPowerDisplay();
		}

		void BuildView()
		{
			Font f = CoreUtils.SkinningDefs.TheInstance.GetFont(8F);
			
			this.BackColor=boardBackgroundColour;

			for(int i=0; i<servers.Count; ++i)
			{
				Panel p = null;

				switch( ((Network_Server)servers[i]).location )
				{
					case "F325":
						p = this.F325;
						break;

					case "F326":
						p = this.F326;
						break;

					case "F327":
						p = this.F327;
						break;

					case "H325":
						p = this.H325;
						break;

					case "H326":
						p = this.H326;
						break;

					case "H327":
						p = this.H327;
						break;

					case "H328":
						p = this.H328;
						break;

					case "E328":
						p = this.E328;
						break;

					case "E329":
						p = this.E329;
						break;

					case "E330":
						p = this.E330;
						break;

					case "E331":
						p = this.E331;
						break;

					case "D322":
						p = this.D322;
						break;

					case "D323":
						p = this.D323;
						break;

					case "C324":
						p = this.C324;
						break;

					case "C325":
						p = this.C325;
						break;

					case "C326":
						p = this.C326;
						break;

					case "G324":
						p = this.G324;
						break;

					case "G325":
						p = this.G325;
						break;

					case "B324":
						p = this.B324;
						break;

					case "B325":
						p = this.B325;
						break;

					case "B326":
						p = this.B326;
						break;

					case "B327":
						p = this.B327;
						break;
				}

				if(null!=p)
				{
					p.SuspendLayout();

					//makes the panel the same colour as the main background
					p.BackColor=boardBackgroundColour;

					foreach(Control c in p.Controls)
					{
						if(c!=null) c.Dispose();
					}
					p.Controls.Clear();
					// Title
					Label title = new Label();
					title.BackColor = Color.Transparent;
					//don't show mirrors

					title.Font = f;
					title.ForeColor = this.boardViewNetworkReportDefaultTextColour;
					if (AllowedExtendedServerName)
					{
						title.Size = new Size(p.Width,16);
					}
					else
					{
						title.Size = new Size(p.Width/2-19,20);
					}
					title.TextAlign = ContentAlignment.MiddleLeft;
					title.Text = ((Network_Server)servers[i]).name;
					if ((title.Text == null)|(title.Text == string.Empty))
					{
						title.Text = ((Network_Server)servers[i]).location;
					}
					p.Controls.Add(title);
					

					// Apps and DBs...
					int offset = 0;
					if (AllowedExtendedServerName)
					{
						offset = 15;
					}

					foreach(Network_Slot slot in ((Network_Server)servers[i]).slots)
					{
						//ignore empty slots
						if(slot.type == "Slot") continue;

						Label app = new Label();
						app.Font = f;
						app.BackColor = Color.Transparent;
						app.ForeColor = this.boardViewNetworkReportDefaultTextColour;
						app.Location = new Point(p.Width/2-13,offset);
						app.Size = new Size(p.Width/2+13,15);
						app.TextAlign = ContentAlignment.MiddleLeft;
						if(slot.type == "App")
						{
							app.Text = "Srv " + slot.location.ToUpper();
						}
						else if (slot.type == "Database")
						{
							app.Text = "DB  " + slot.location.ToUpper();
						}
						else
						{
							app.Text = slot.location.ToUpper();
						}

						if( (slot.proc_cap != string.Empty) && ShowIndividualPower)
						{
							app.Text += " (" + slot.proc_cap + ")";
						}
						//if created by SIP
						if (slot.project != string.Empty)
						{
							app.Text += " " + slot.project;
							app.BackColor = boardViewNetworkDefaultColor_CreatedbySIP;
						}
						//if upgraded by SIP
						if (slot.upgrade != string.Empty)
						{
							app.Text += " " + slot.upgrade;
							app.BackColor = boardViewNetworkDefaultColor_UpgradedbySIP;
						}
						p.Controls.Add(app);
						offset += app.Height;
					}
					
					// Building the Memory Label (Mind it could have been upgraded)
					Label mem = new Label();
					mem.BackColor = Color.Transparent;
					mem.Font = f;
					mem.ForeColor = this.boardViewNetworkReportDefaultTextColour;
					mem.Location = new Point(0,title.Height);
					mem.Size = new Size(p.Width/2, (p.Height-title.Height)/3);
					mem.TextAlign = ContentAlignment.MiddleLeft;
					mem.Text = "M: " + ((Network_Server)servers[i]).memory;
					if(((Network_Server)servers[i]).memory_upgraded)
					{
						mem.ForeColor = this.boardViewNetworkReportDefaultUpgradedMemoryColour;
					}
					if (((Network_Server)servers[i]).affected_by != string.Empty)
					{
						mem.ForeColor = this.boardViewNetworkReportDefaultUpgradedMemoryColour;
						mem.Text += " (" + ((Network_Server)servers[i]).affected_by + ")";
					}
//					p.Controls.Add(mem);
					
					// Building the Storage Label (Mind it could have been upgraded)
					Label storage = new Label();
					storage.BackColor = Color.Transparent;
					storage.Font = f;
					storage.ForeColor = this.boardViewNetworkReportDefaultTextColour;
					storage.Location = new Point(0,mem.Bottom); //(p.Height-title.Height)/2);
					storage.Size = new Size(p.Width/2, (p.Height-title.Height)/3);
					storage.TextAlign = ContentAlignment.MiddleLeft;
					storage.Text = "S: " + ((Network_Server)servers[i]).storage;
					if(((Network_Server)servers[i]).storage_upgraded)
					{
						storage.ForeColor = this.boardViewNetworkReportDefaultUpgradedStorageColour;
					}
					if (((Network_Server)servers[i]).affected_by != string.Empty)
					{
						storage.ForeColor = this.boardViewNetworkReportDefaultUpgradedStorageColour;
						storage.Text += " (" + ((Network_Server)servers[i]).affected_by + ")";
					}
//					p.Controls.Add(storage);

					if (ShowIndividualPower)
					{
						// Build the Processing Capability Label 
						Label proc = new Label();
						proc.BackColor = Color.Transparent;
						proc.Font = f;
						proc.ForeColor = this.boardViewNetworkReportDefaultTextColour;
						proc.Location = new Point(0, storage.Bottom); //title.Height+(p.Height-title.Height)/2);
						proc.Size = new Size(p.Width/2, (p.Height-title.Height)/3);
						proc.TextAlign = ContentAlignment.MiddleLeft;
						proc.Text = "P: " + ((Network_Server)servers[i]).proc_cap;
						p.Controls.Add(proc);
					}

					p.ResumeLayout(false);
					
				}
			}

			this.Zone1Title.Text = "Zone 1 (Platform X, Tier " + ZoneTier[0] + ")";
			this.Zone2Title.Text = "Zone 2 (Platform Y, Tier " + ZoneTier[1] + ")";
			this.Zone3Title.Text = "Zone 3 (Platform X, Tier " + ZoneTier[2] + ")";
			this.Zone4Title.Text = "Zone 4 (Platform Y, Tier " + ZoneTier[3] + ")";
			this.Zone5Title.Text = "Zone 5 (Platform Z, Tier " + ZoneTier[4] + ")";
			this.Zone6Title.Text = "Zone 6 (Platform X, Tier " + ZoneTier[5] + ")";
			this.Zone7Title.Text = "Zone 7 (Platform Z, Tier " + ZoneTier[6] + ")";
		}

		public void SetProcessingVisible(bool state)
		{
			//proc.Visible = state;
			ShowIndividualPower = state;
			//show_proc = state;
		}

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Component Designer generated code
		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.Zone2Title = new System.Windows.Forms.Label();
			this.Zone2BackPanel = new BasePanel();
			this.F327 = new BasePanel();
			this.F326 = new BasePanel();
			this.F325 = new BasePanel();
			this.Zone1BackPanel = new BasePanel();
			this.E331 = new BasePanel();
			this.E330 = new BasePanel();
			this.E329 = new BasePanel();
			this.E328 = new BasePanel();
			this.Zone1Title = new System.Windows.Forms.Label();
			this.Zone6BackPanel = new BasePanel();
			this.D323 = new BasePanel();
			this.D322 = new BasePanel();
			this.Zone6Title = new System.Windows.Forms.Label();
			this.Zone7BackPanel = new BasePanel();
			this.C326 = new BasePanel();
			this.C325 = new BasePanel();
			this.C324 = new BasePanel();
			this.Zone7Title = new System.Windows.Forms.Label();
			this.Zone5BackPanel = new BasePanel();
			this.H328 = new BasePanel();
			this.H327 = new BasePanel();
			this.H326 = new BasePanel();
			this.H325 = new BasePanel();
			this.Zone5Title = new System.Windows.Forms.Label();
			this.Zone4BackPanel = new BasePanel();
			this.G325 = new BasePanel();
			this.G324 = new BasePanel();
			this.Zone4Title = new System.Windows.Forms.Label();
			this.Zone3BackPanel = new BasePanel();
			this.B327 = new BasePanel();
			this.B326 = new BasePanel();
			this.B325 = new BasePanel();
			this.B324 = new BasePanel();
			this.Zone3Title = new System.Windows.Forms.Label();
			this.Zone2BackPanel.SuspendLayout();
			this.Zone1BackPanel.SuspendLayout();
			this.Zone6BackPanel.SuspendLayout();
			this.Zone7BackPanel.SuspendLayout();
			this.Zone5BackPanel.SuspendLayout();
			this.Zone4BackPanel.SuspendLayout();
			this.Zone3BackPanel.SuspendLayout();
			this.SuspendLayout();

			// 
			// label1
			// 
			this.Zone2Title.BackColor = System.Drawing.Color.Transparent;
			//this.label1.BackColor =  boardViewZone2Colour;
			this.Zone2Title.Font = CoreUtils.SkinningDefs.TheInstance.GetFont(8.25F, System.Drawing.FontStyle.Bold);
			this.Zone2Title.ForeColor = System.Drawing.Color.Black;
			this.Zone2Title.Location = new System.Drawing.Point(6, 6);
			this.Zone2Title.Name = "label1";
			this.Zone2Title.Size = new System.Drawing.Size(183, 12);
			this.Zone2Title.TabIndex = 1;
			this.Zone2Title.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// panel1
			// 
			this.Zone2BackPanel.BackColor=boardViewZone2Colour;
			this.Zone2BackPanel.Controls.Add(this.F327);
			this.Zone2BackPanel.Controls.Add(this.F326);
			this.Zone2BackPanel.Controls.Add(this.F325);
			this.Zone2BackPanel.Controls.Add(this.Zone2Title);
			this.Zone2BackPanel.Location = new System.Drawing.Point(2, 2);
			this.Zone2BackPanel.Name = "panel1";
			this.Zone2BackPanel.Size = new System.Drawing.Size(193, 349);
			this.Zone2BackPanel.TabIndex = 4;
			// 
			// F327
			// 
			this.F327.BackColor = System.Drawing.Color.White;
			this.F327.Location = new System.Drawing.Point(5, 239);
			this.F327.Name = "F327";
			this.F327.Size = new System.Drawing.Size(183, 105);
			this.F327.TabIndex = 4;
			// 
			// F326
			// 
			this.F326.BackColor = System.Drawing.Color.White;
			this.F326.Location = new System.Drawing.Point(5, 130);
			this.F326.Name = "F326";
			this.F326.Size = new System.Drawing.Size(183, 105);
			this.F326.TabIndex = 3;
			// 
			// F325
			// 
			this.F325.BackColor = System.Drawing.Color.White;
			this.F325.Location = new System.Drawing.Point(5, 22);
			this.F325.Name = "F325";
			this.F325.Size = new System.Drawing.Size(183, 105);
			this.F325.TabIndex = 2;
			// 
			// panel5
			// 
			this.Zone1BackPanel.BackColor=boardViewZone1Colour;
			this.Zone1BackPanel.Controls.Add(this.E331);
			this.Zone1BackPanel.Controls.Add(this.E330);
			this.Zone1BackPanel.Controls.Add(this.E329);
			this.Zone1BackPanel.Controls.Add(this.E328);
			this.Zone1BackPanel.Controls.Add(this.Zone1Title);
			this.Zone1BackPanel.Location = new System.Drawing.Point(196, 2);
			this.Zone1BackPanel.Name = "panel5";
			this.Zone1BackPanel.Size = new System.Drawing.Size(380, 240);
			this.Zone1BackPanel.TabIndex = 5;
			// 
			// E331
			// 
			this.E331.BackColor = System.Drawing.Color.White;
			this.E331.Location = new System.Drawing.Point(190, 130);
			this.E331.Name = "E331";
			this.E331.Size = new System.Drawing.Size(183, 105);
			this.E331.TabIndex = 5;
			// 
			// E330
			// 
			this.E330.BackColor = System.Drawing.Color.White;
			this.E330.Location = new System.Drawing.Point(5, 130);
			this.E330.Name = "E330";
			this.E330.Size = new System.Drawing.Size(183, 105);
			this.E330.TabIndex = 4;
			// 
			// E329
			// 
			this.E329.BackColor = System.Drawing.Color.White;
			this.E329.Location = new System.Drawing.Point(190, 22);
			this.E329.Name = "E329";
			this.E329.Size = new System.Drawing.Size(183, 105);
			this.E329.TabIndex = 3;
			// 
			// E328
			// 
			this.E328.BackColor = System.Drawing.Color.White;
			this.E328.Location = new System.Drawing.Point(5, 22);
			this.E328.Name = "E328";
			this.E328.Size = new System.Drawing.Size(183, 105);
			this.E328.TabIndex = 2;
			// 
			// label61
			// 
			this.Zone1Title.BackColor = System.Drawing.Color.Transparent;
			//this.label61.BackColor=boardViewZone1Colour;
			this.Zone1Title.Font = CoreUtils.SkinningDefs.TheInstance.GetFont(8.25F, System.Drawing.FontStyle.Bold);
			this.Zone1Title.ForeColor = System.Drawing.Color.Black;
			this.Zone1Title.Location = new System.Drawing.Point(6, 6);
			this.Zone1Title.Name = "label61";
			this.Zone1Title.Size = new System.Drawing.Size(364, 12);
			this.Zone1Title.TabIndex = 1;
			this.Zone1Title.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// panel10
			// 
			this.Zone6BackPanel.BackColor=boardViewZone6Colour;
			this.Zone6BackPanel.Controls.Add(this.D323);
			this.Zone6BackPanel.Controls.Add(this.D322);
			this.Zone6BackPanel.Controls.Add(this.Zone6Title);
			this.Zone6BackPanel.Location = new System.Drawing.Point(580, 2);
			this.Zone6BackPanel.Name = "panel10";
			this.Zone6BackPanel.Size = new System.Drawing.Size(193, 240);
			this.Zone6BackPanel.TabIndex = 6;
			// 
			// D323
			// 
			this.D323.BackColor = System.Drawing.Color.White;
			this.D323.Location = new System.Drawing.Point(6, 130);
			this.D323.Name = "D323";
			this.D323.Size = new System.Drawing.Size(183, 105);
			this.D323.TabIndex = 3;
			// 
			// D322
			// 
			this.D322.BackColor = System.Drawing.Color.White;
			this.D322.Location = new System.Drawing.Point(5, 22);
			this.D322.Name = "D322";
			this.D322.Size = new System.Drawing.Size(183, 105);
			this.D322.TabIndex = 2;
			// 
			// label102
			// 
			this.Zone6Title.BackColor = System.Drawing.Color.Transparent;
			//this.label102.BackColor = boardViewZone6Colour;
			this.Zone6Title.Font = CoreUtils.SkinningDefs.TheInstance.GetFont(8.25F, System.Drawing.FontStyle.Bold);
			this.Zone6Title.ForeColor = System.Drawing.Color.Black;
			this.Zone6Title.Location = new System.Drawing.Point(6, 6);
			this.Zone6Title.Name = "label102";
			this.Zone6Title.Size = new System.Drawing.Size(183, 12);
			this.Zone6Title.TabIndex = 1;
			this.Zone6Title.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// panel14
			// 
			this.Zone7BackPanel.BackColor=boardViewZone7Colour;
			this.Zone7BackPanel.Controls.Add(this.C326);
			this.Zone7BackPanel.Controls.Add(this.C325);
			this.Zone7BackPanel.Controls.Add(this.C324);
			this.Zone7BackPanel.Controls.Add(this.Zone7Title);
			this.Zone7BackPanel.Location = new System.Drawing.Point(778, 2);
			this.Zone7BackPanel.Name = "panel14";
			this.Zone7BackPanel.Size = new System.Drawing.Size(193, 350);
			this.Zone7BackPanel.TabIndex = 7;
			// 
			// C326
			// 
			this.C326.BackColor = System.Drawing.Color.White;
			this.C326.Location = new System.Drawing.Point(5, 239);
			this.C326.Name = "C326";
			this.C326.Size = new System.Drawing.Size(183, 105);
			this.C326.TabIndex = 4;
			// 
			// C325
			// 
			this.C325.BackColor = System.Drawing.Color.White;
			this.C325.Location = new System.Drawing.Point(6, 130);
			this.C325.Name = "C325";
			this.C325.Size = new System.Drawing.Size(183, 105);
			this.C325.TabIndex = 3;
			// 
			// C324
			// 
			this.C324.BackColor = System.Drawing.Color.White;
			this.C324.Location = new System.Drawing.Point(5, 22);
			this.C324.Name = "C324";
			this.C324.Size = new System.Drawing.Size(183, 105);
			this.C324.TabIndex = 2;
			// 
			// label133
			// 
			this.Zone7Title.BackColor = System.Drawing.Color.Transparent;
			//this.label133.BackColor=boardViewZone7Colour;
			this.Zone7Title.Font = CoreUtils.SkinningDefs.TheInstance.GetFont(8.25F, System.Drawing.FontStyle.Bold);
			this.Zone7Title.ForeColor = System.Drawing.Color.Black;
			this.Zone7Title.Location = new System.Drawing.Point(6, 6);
			this.Zone7Title.Name = "label133";
			this.Zone7Title.Size = new System.Drawing.Size(183, 12);
			this.Zone7Title.TabIndex = 1;
			this.Zone7Title.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// panel11
			// 
			this.Zone5BackPanel.BackColor = boardViewZone5Colour;
			this.Zone5BackPanel.Controls.Add(this.H328);
			this.Zone5BackPanel.Controls.Add(this.H327);
			this.Zone5BackPanel.Controls.Add(this.H326);
			this.Zone5BackPanel.Controls.Add(this.H325);
			this.Zone5BackPanel.Controls.Add(this.Zone5Title);
			this.Zone5BackPanel.Location = new System.Drawing.Point(2, 357);
			this.Zone5BackPanel.Name = "panel11";
			this.Zone5BackPanel.Size = new System.Drawing.Size(380, 242);
			this.Zone5BackPanel.TabIndex = 8;
			// 
			// H328
			// 
			this.H328.BackColor = System.Drawing.Color.White;
			this.H328.Location = new System.Drawing.Point(190, 132);
			this.H328.Name = "H328";
			this.H328.Size = new System.Drawing.Size(183, 105);
			this.H328.TabIndex = 5;
			// 
			// H327
			// 
			this.H327.BackColor = System.Drawing.Color.White;
			this.H327.Location = new System.Drawing.Point(5, 132);
			this.H327.Name = "H327";
			this.H327.Size = new System.Drawing.Size(183, 105);
			this.H327.TabIndex = 4;
			// 
			// H326
			// 
			this.H326.BackColor = System.Drawing.Color.White;
			this.H326.Location = new System.Drawing.Point(190, 24);
			this.H326.Name = "H326";
			this.H326.Size = new System.Drawing.Size(183, 105);
			this.H326.TabIndex = 3;
			// 
			// H325
			// 
			this.H325.BackColor = System.Drawing.Color.White;
			this.H325.Location = new System.Drawing.Point(5, 23);
			this.H325.Name = "H325";
			this.H325.Size = new System.Drawing.Size(183, 105);
			this.H325.TabIndex = 2;
			// 
			// label164
			// 
			this.Zone5Title.BackColor = Color.Transparent;
			this.Zone5Title.Font = CoreUtils.SkinningDefs.TheInstance.GetFont(8.25F, System.Drawing.FontStyle.Bold);
			this.Zone5Title.ForeColor = System.Drawing.Color.Black;
			this.Zone5Title.Location = new System.Drawing.Point(6, 6);
			this.Zone5Title.Name = "label164";
			this.Zone5Title.Size = new System.Drawing.Size(364, 10);
			this.Zone5Title.TabIndex = 1;
			this.Zone5Title.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// panel2
			// 
			this.Zone4BackPanel.BackColor=boardViewZone4Colour;
			this.Zone4BackPanel.Controls.Add(this.G325);
			this.Zone4BackPanel.Controls.Add(this.G324);
			this.Zone4BackPanel.Controls.Add(this.Zone4Title);
			this.Zone4BackPanel.Location = new System.Drawing.Point(388, 358);
			this.Zone4BackPanel.Name = "panel2";
			this.Zone4BackPanel.Size = new System.Drawing.Size(193, 240);
			this.Zone4BackPanel.TabIndex = 9;
			// 
			// G325
			// 
			this.G325.BackColor = System.Drawing.Color.White;
			this.G325.Location = new System.Drawing.Point(6, 130);
			this.G325.Name = "G325";
			this.G325.Size = new System.Drawing.Size(183, 105);
			this.G325.TabIndex = 3;
			// 
			// G324
			// 
			this.G324.BackColor = System.Drawing.Color.White;
			this.G324.Location = new System.Drawing.Point(5, 22);
			this.G324.Name = "G324";
			this.G324.Size = new System.Drawing.Size(183, 105);
			this.G324.TabIndex = 2;
			// 
			// label2
			// 
			this.Zone4Title.BackColor = System.Drawing.Color.Transparent;
			//this.Zone4Title.BackColor = boardViewZone4Colour;
			this.Zone4Title.Font = CoreUtils.SkinningDefs.TheInstance.GetFont(8.25F, System.Drawing.FontStyle.Bold);
			this.Zone4Title.ForeColor = System.Drawing.Color.Black;
			this.Zone4Title.Location = new System.Drawing.Point(6, 6);
			this.Zone4Title.Name = "label2";
			this.Zone4Title.Size = new System.Drawing.Size(183, 12);
			this.Zone4Title.TabIndex = 1;
			this.Zone4Title.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// panel3
			// 
			this.Zone3BackPanel.BackColor = boardViewZone3Colour;
			this.Zone3BackPanel.Controls.Add(this.B327);
			this.Zone3BackPanel.Controls.Add(this.B326);
			this.Zone3BackPanel.Controls.Add(this.B325);
			this.Zone3BackPanel.Controls.Add(this.B324);
			this.Zone3BackPanel.Controls.Add(this.Zone3Title);
			this.Zone3BackPanel.Location = new System.Drawing.Point(592, 358);
			this.Zone3BackPanel.Name = "panel3";
			this.Zone3BackPanel.Size = new System.Drawing.Size(380, 240);
			this.Zone3BackPanel.TabIndex = 10;
			// 
			// B327
			// 
			this.B327.BackColor = System.Drawing.Color.White;
			this.B327.Location = new System.Drawing.Point(190, 130);
			this.B327.Name = "B327";
			this.B327.Size = new System.Drawing.Size(183, 105);
			this.B327.TabIndex = 5;
			// 
			// B326
			// 
			this.B326.BackColor = System.Drawing.Color.White;
			this.B326.Location = new System.Drawing.Point(5, 130);
			this.B326.Name = "B326";
			this.B326.Size = new System.Drawing.Size(183, 105);
			this.B326.TabIndex = 4;
			// 
			// B325
			// 
			this.B325.BackColor = System.Drawing.Color.White;
			this.B325.Location = new System.Drawing.Point(190, 22);
			this.B325.Name = "B325";
			this.B325.Size = new System.Drawing.Size(183, 105);
			this.B325.TabIndex = 3;
			// 
			// B324
			// 
			this.B324.BackColor = System.Drawing.Color.White;
			this.B324.Location = new System.Drawing.Point(5, 22);
			this.B324.Name = "B324";
			this.B324.Size = new System.Drawing.Size(183, 105);
			this.B324.TabIndex = 2;
			// 
			// label3
			// 
			this.Zone3Title.BackColor = System.Drawing.Color.Transparent;
			//this.label3.BackColor = boardViewZone3Colour;
			this.Zone3Title.Font = CoreUtils.SkinningDefs.TheInstance.GetFont(8.25F, System.Drawing.FontStyle.Bold);
			this.Zone3Title.ForeColor = System.Drawing.Color.Black;
			this.Zone3Title.Location = new System.Drawing.Point(6, 6);
			this.Zone3Title.Name = "label3";
			this.Zone3Title.Size = new System.Drawing.Size(364, 12);
			this.Zone3Title.TabIndex = 1;
			this.Zone3Title.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// NetworkControl_HPDC
			// 
			this.BackColor = System.Drawing.Color.White;
			this.Controls.Add(this.Zone3BackPanel);
			this.Controls.Add(this.Zone4BackPanel);
			this.Controls.Add(this.Zone5BackPanel);
			this.Controls.Add(this.Zone7BackPanel);
			this.Controls.Add(this.Zone6BackPanel);
			this.Controls.Add(this.Zone1BackPanel);
			this.Controls.Add(this.Zone2BackPanel);
			this.Font = ConstantSizeFont.NewFont("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold);
			this.Name = "NetworkControl_HPDC";
			this.Size = new System.Drawing.Size(1004, 627);
			this.Zone2BackPanel.ResumeLayout(false);
			this.Zone1BackPanel.ResumeLayout(false);
			this.Zone6BackPanel.ResumeLayout(false);
			this.Zone7BackPanel.ResumeLayout(false);
			this.Zone5BackPanel.ResumeLayout(false);
			this.Zone4BackPanel.ResumeLayout(false);
			this.Zone3BackPanel.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

	}
}
