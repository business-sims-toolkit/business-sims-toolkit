using System.Collections;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;

using LibCore;
using CoreUtils;

namespace Charts
{
	public class Network_Slot
	{
		public string name;
		public string location;
		public string type;
		public string project;
		public string upgrade;
		public string proc_cap;
	}
	public class Network_Server : Network_Slot
	{		
		public string memory;
		public string storage;
		public bool memory_upgraded = false;
		public bool storage_upgraded = false;
		public ArrayList slots = new ArrayList();
		public bool mirror = false;
		public string affected_by = string.Empty;
	}

	/// <summary>
	/// Summary description for NetworkControl.
	/// </summary>
	public class NetworkControl : Chart
	{
		static System.Globalization.CultureInfo myCI_enGB = new System.Globalization.CultureInfo( "en-GB", false );

		ArrayList servers;

	    Color headerColour;

		System.Windows.Forms.Label label1;
		BasePanel panel1;
		BasePanel panel5;
		System.Windows.Forms.Label label61;
		BasePanel panel10;
		System.Windows.Forms.Label label102;
		BasePanel panel14;
		System.Windows.Forms.Label label133;
		BasePanel panel11;
		System.Windows.Forms.Label label164;
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
		BasePanel panel2;
		BasePanel G325;
		BasePanel G324;
		System.Windows.Forms.Label label2;
		BasePanel panel3;
		BasePanel B327;
		BasePanel B326;
		BasePanel B325;
		BasePanel B324;
		System.Windows.Forms.Label label3;

		Color boardBackgroundColour=Color.Transparent;
		Color boardViewZone1Colour=Color.Transparent;
		Color boardViewZone2Colour=Color.Transparent;
		Color boardViewZone3Colour=Color.Transparent;
		Color boardViewZone4Colour=Color.Transparent;
		Color boardViewZone5Colour=Color.Transparent;
		Color boardViewZone6Colour=Color.Transparent;
		Color boardViewZone7Colour=Color.Transparent;

		Color boardViewNetworkReportDefaultTextColour = Color.Transparent;
		Color boardViewNetworkReportDefaultUpgradedMemoryColour = Color.Transparent;
		Color boardViewNetworkReportDefaultUpgradedStorageColour = Color.Transparent;

		Color boardViewNetworkDefaultColor_CreatedbySIP = Color.Transparent;
		Color boardViewNetworkDefaultColor_UpgradedbySIP = Color.Transparent;

		protected bool AllowedExtendedServerName = false; 

		/// <summary> 
		/// Required designer variable.
		/// </summary>
		System.ComponentModel.Container components = null;

		public NetworkControl()
		{
		    headerColour = SkinningDefs.TheInstance.GetColorDataGivenDefault("boardview_header_text_colour", Color.Black);

            //Set Zone Colours from Skin File
            boardBackgroundColour =CoreUtils.SkinningDefs.TheInstance.GetColorData("boardview_background_colour");
			boardViewZone1Colour=CoreUtils.SkinningDefs.TheInstance.GetColorData("boardview_zone1_colour");
			boardViewZone2Colour=CoreUtils.SkinningDefs.TheInstance.GetColorData("boardview_zone2_colour");
			boardViewZone3Colour=CoreUtils.SkinningDefs.TheInstance.GetColorData("boardview_zone3_colour");
			boardViewZone4Colour=CoreUtils.SkinningDefs.TheInstance.GetColorData("boardview_zone4_colour");
			boardViewZone5Colour=CoreUtils.SkinningDefs.TheInstance.GetColorData("boardview_zone5_colour");
			boardViewZone6Colour=CoreUtils.SkinningDefs.TheInstance.GetColorData("boardview_zone6_colour");
			boardViewZone7Colour=CoreUtils.SkinningDefs.TheInstance.GetColorData("boardview_zone7_colour");

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
			string AllowedExtendedServerName_str  = CoreUtils.SkinningDefs.TheInstance.GetData("network_report_extendedservernames");
			if (AllowedExtendedServerName_str.ToLower()=="true")
			{
				AllowedExtendedServerName = true;
			}

			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
			servers = new ArrayList();
		}

		public override void LoadData(string xmldata)
		{
			BasicXmlDocument xdoc = LibCore.BasicXmlDocument.Create(xmldata);
			XmlNode rootNode = xdoc.DocumentElement;

			foreach(XmlNode child in rootNode.ChildNodes)
			{
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
			}

			BuildView();
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
							app.Text = "App " + slot.location.ToUpper();
						}
						else if (slot.type == "Database")
						{
							app.Text = "DB  " + slot.location.ToUpper();
						}
						else
						{
							app.Text = slot.location.ToUpper();
						}

						bool addSpace = true;

						if( (slot.proc_cap != string.Empty) && show_proc)
						{
							if (slot.proc_cap.Length > 1)
							{
								addSpace = false;
							}

							if (addSpace)
							{
								app.Text += " ";
							}

							app.Text += "(" + slot.proc_cap + ")";
						}

						//if created by SIP
						if (slot.project != string.Empty)
						{
							if (addSpace)
							{
								app.Text += " ";
							}
							
							app.Text += slot.project;
							app.BackColor = boardViewNetworkDefaultColor_CreatedbySIP;
						}
						//if upgraded by SIP
						if (slot.upgrade != string.Empty)
						{
							if (addSpace)
							{
								app.Text += " ";
							}

							app.Text += slot.upgrade;
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
					p.Controls.Add(mem);
					
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
					p.Controls.Add(storage);


					// Build the Processing Capability Label 
					proc = new Label();
					proc.BackColor = Color.Transparent;
					proc.Font = f;
					proc.ForeColor = this.boardViewNetworkReportDefaultTextColour;
					proc.Location = new Point(0, storage.Bottom); //title.Height+(p.Height-title.Height)/2);
					proc.Size = new Size(p.Width/2, (p.Height-title.Height)/3);
					proc.TextAlign = ContentAlignment.MiddleLeft;
					proc.Text = "P: " + ((Network_Server)servers[i]).proc_cap;
					proc.Visible = show_proc;

					p.Controls.Add(proc);

					p.ResumeLayout(false);
					
				}
			}
		}

		protected Label proc;
		protected bool show_proc = true;

		public void SetProcessingVisible(bool state)
		{
			proc.Visible = state;
			show_proc = state;
		}

		public void SetProcessingFlagVisible(bool state)
		{
			show_proc = state;
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
			this.label1 = new System.Windows.Forms.Label();
			this.panel1 = new BasePanel();
			this.F327 = new BasePanel();
			this.F326 = new BasePanel();
			this.F325 = new BasePanel();
			this.panel5 = new BasePanel();
			this.E331 = new BasePanel();
			this.E330 = new BasePanel();
			this.E329 = new BasePanel();
			this.E328 = new BasePanel();
			this.label61 = new System.Windows.Forms.Label();
			this.panel10 = new BasePanel();
			this.D323 = new BasePanel();
			this.D322 = new BasePanel();
			this.label102 = new System.Windows.Forms.Label();
			this.panel14 = new BasePanel();
			this.C326 = new BasePanel();
			this.C325 = new BasePanel();
			this.C324 = new BasePanel();
			this.label133 = new System.Windows.Forms.Label();
			this.panel11 = new BasePanel();
			this.H328 = new BasePanel();
			this.H327 = new BasePanel();
			this.H326 = new BasePanel();
			this.H325 = new BasePanel();
			this.label164 = new System.Windows.Forms.Label();
			this.panel2 = new BasePanel();
			this.G325 = new BasePanel();
			this.G324 = new BasePanel();
			this.label2 = new System.Windows.Forms.Label();
			this.panel3 = new BasePanel();
			this.B327 = new BasePanel();
			this.B326 = new BasePanel();
			this.B325 = new BasePanel();
			this.B324 = new BasePanel();
			this.label3 = new System.Windows.Forms.Label();
			this.panel1.SuspendLayout();
			this.panel5.SuspendLayout();
			this.panel10.SuspendLayout();
			this.panel14.SuspendLayout();
			this.panel11.SuspendLayout();
			this.panel2.SuspendLayout();
			this.panel3.SuspendLayout();
			this.SuspendLayout();

			// 
			// label1
			// 
			this.label1.BackColor = System.Drawing.Color.Transparent;
			//this.label1.BackColor =  boardViewZone2Colour;
			this.label1.Font = CoreUtils.SkinningDefs.TheInstance.GetFont(8.25F, System.Drawing.FontStyle.Bold);
		    this.label1.ForeColor = headerColour;
			this.label1.Location = new System.Drawing.Point(6, 6);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(183, 12);
			this.label1.TabIndex = 1;
			this.label1.Text = "Zone 2 : Platform Y";
			this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// panel1
			// 
			this.panel1.BackColor=boardViewZone2Colour;
			this.panel1.Controls.Add(this.F327);
			this.panel1.Controls.Add(this.F326);
			this.panel1.Controls.Add(this.F325);
			this.panel1.Controls.Add(this.label1);
			this.panel1.Location = new System.Drawing.Point(2, 2);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(193, 349);
			this.panel1.TabIndex = 4;
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
			this.panel5.BackColor=boardViewZone1Colour;
			this.panel5.Controls.Add(this.E331);
			this.panel5.Controls.Add(this.E330);
			this.panel5.Controls.Add(this.E329);
			this.panel5.Controls.Add(this.E328);
			this.panel5.Controls.Add(this.label61);
			this.panel5.Location = new System.Drawing.Point(196, 2);
			this.panel5.Name = "panel5";
			this.panel5.Size = new System.Drawing.Size(380, 240);
			this.panel5.TabIndex = 5;
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
			this.label61.BackColor = System.Drawing.Color.Transparent;
			//this.label61.BackColor=boardViewZone1Colour;
			this.label61.Font = CoreUtils.SkinningDefs.TheInstance.GetFont(8.25F, System.Drawing.FontStyle.Bold);
		    this.label61.ForeColor = headerColour;
			this.label61.Location = new System.Drawing.Point(6, 6);
			this.label61.Name = "label61";
			this.label61.Size = new System.Drawing.Size(364, 12);
			this.label61.TabIndex = 1;
			this.label61.Text = "Zone 1 : Platform X";
			this.label61.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// panel10
			// 
			this.panel10.BackColor=boardViewZone6Colour;
			this.panel10.Controls.Add(this.D323);
			this.panel10.Controls.Add(this.D322);
			this.panel10.Controls.Add(this.label102);
			this.panel10.Location = new System.Drawing.Point(580, 2);
			this.panel10.Name = "panel10";
			this.panel10.Size = new System.Drawing.Size(193, 240);
			this.panel10.TabIndex = 6;
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
			this.label102.BackColor = System.Drawing.Color.Transparent;
			//this.label102.BackColor = boardViewZone6Colour;
			this.label102.Font = CoreUtils.SkinningDefs.TheInstance.GetFont(8.25F, System.Drawing.FontStyle.Bold);
			this.label102.ForeColor = headerColour;
			this.label102.Location = new System.Drawing.Point(6, 6);
			this.label102.Name = "label102";
			this.label102.Size = new System.Drawing.Size(183, 12);
			this.label102.TabIndex = 1;
			this.label102.Text = "Zone 6 : Platform X";
			this.label102.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// panel14
			// 
			this.panel14.BackColor=boardViewZone7Colour;
			this.panel14.Controls.Add(this.C326);
			this.panel14.Controls.Add(this.C325);
			this.panel14.Controls.Add(this.C324);
			this.panel14.Controls.Add(this.label133);
			this.panel14.Location = new System.Drawing.Point(778, 2);
			this.panel14.Name = "panel14";
			this.panel14.Size = new System.Drawing.Size(193, 350);
			this.panel14.TabIndex = 7;
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
			this.label133.BackColor = System.Drawing.Color.Transparent;
			//this.label133.BackColor=boardViewZone7Colour;
			this.label133.Font = CoreUtils.SkinningDefs.TheInstance.GetFont(8.25F, System.Drawing.FontStyle.Bold);
			this.label133.ForeColor = headerColour;
			this.label133.Location = new System.Drawing.Point(6, 6);
			this.label133.Name = "label133";
			this.label133.Size = new System.Drawing.Size(183, 12);
			this.label133.TabIndex = 1;
            if(SkinningDefs.TheInstance.GetBoolData("network_report_zone7_isPlatformW", false))
            {
                this.label133.Text = "Zone 7 : Platform W";
            }
            else
            {
                this.label133.Text = "Zone 7 : Platform Z";
            }
			this.label133.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// panel11
			// 
			this.panel11.BackColor = boardViewZone5Colour;
			this.panel11.Controls.Add(this.H328);
			this.panel11.Controls.Add(this.H327);
			this.panel11.Controls.Add(this.H326);
			this.panel11.Controls.Add(this.H325);
			this.panel11.Controls.Add(this.label164);
			this.panel11.Location = new System.Drawing.Point(2, 357);
			this.panel11.Name = "panel11";
			this.panel11.Size = new System.Drawing.Size(380, 242);
			this.panel11.TabIndex = 8;
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
			this.label164.BackColor = Color.Transparent;
			this.label164.Font = CoreUtils.SkinningDefs.TheInstance.GetFont(8.25F, System.Drawing.FontStyle.Bold);
			this.label164.ForeColor = headerColour;
			this.label164.Location = new System.Drawing.Point(6, 6);
			this.label164.Name = "label164";
			this.label164.Size = new System.Drawing.Size(364, 10);
			this.label164.TabIndex = 1;
			this.label164.Text = "Zone 5 : Platform Z";
			this.label164.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// panel2
			// 
			this.panel2.BackColor=boardViewZone4Colour;
			this.panel2.Controls.Add(this.G325);
			this.panel2.Controls.Add(this.G324);
			this.panel2.Controls.Add(this.label2);
			this.panel2.Location = new System.Drawing.Point(388, 358);
			this.panel2.Name = "panel2";
			this.panel2.Size = new System.Drawing.Size(193, 240);
			this.panel2.TabIndex = 9;
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
			this.label2.BackColor = System.Drawing.Color.Transparent;
			//this.label2.BackColor = boardViewZone4Colour;
			this.label2.Font = CoreUtils.SkinningDefs.TheInstance.GetFont(8.25F, System.Drawing.FontStyle.Bold);
			this.label2.ForeColor = headerColour;
			this.label2.Location = new System.Drawing.Point(6, 6);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(183, 12);
			this.label2.TabIndex = 1;
			this.label2.Text = "Zone 4 : Platform Y";
			this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// panel3
			// 
			this.panel3.BackColor = boardViewZone3Colour;
			this.panel3.Controls.Add(this.B327);
			this.panel3.Controls.Add(this.B326);
			this.panel3.Controls.Add(this.B325);
			this.panel3.Controls.Add(this.B324);
			this.panel3.Controls.Add(this.label3);
			this.panel3.Location = new System.Drawing.Point(592, 358);
			this.panel3.Name = "panel3";
			this.panel3.Size = new System.Drawing.Size(380, 240);
			this.panel3.TabIndex = 10;
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
			this.label3.BackColor = System.Drawing.Color.Transparent;
			//this.label3.BackColor = boardViewZone3Colour;
			this.label3.Font = CoreUtils.SkinningDefs.TheInstance.GetFont(8.25F, System.Drawing.FontStyle.Bold);
			this.label3.ForeColor = headerColour;
			this.label3.Location = new System.Drawing.Point(6, 6);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(364, 12);
			this.label3.TabIndex = 1;
			this.label3.Text = "Zone 3 : Platform X";
			this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// NetworkControl
			// 
			this.BackColor = System.Drawing.Color.White;
			this.Controls.Add(this.panel3);
			this.Controls.Add(this.panel2);
			this.Controls.Add(this.panel11);
			this.Controls.Add(this.panel14);
			this.Controls.Add(this.panel10);
			this.Controls.Add(this.panel5);
			this.Controls.Add(this.panel1);
			this.Font = CoreUtils.SkinningDefs.TheInstance.GetFont(8.25F, System.Drawing.FontStyle.Bold);
			this.Name = "NetworkControl";
			this.Size = new System.Drawing.Size(1004, 627);
			this.panel1.ResumeLayout(false);
			this.panel5.ResumeLayout(false);
			this.panel10.ResumeLayout(false);
			this.panel14.ResumeLayout(false);
			this.panel11.ResumeLayout(false);
			this.panel2.ResumeLayout(false);
			this.panel3.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

	}
}
