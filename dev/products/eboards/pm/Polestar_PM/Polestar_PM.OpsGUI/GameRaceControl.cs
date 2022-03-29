using System;
using System.Globalization;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Xml;

using Network;
using CommonGUI;
using LibCore;
using CoreUtils;
using BusinessServiceRules;
using Polestar_PM.DataLookup;

namespace Polestar_PM.OpsGUI
{

	public class RowPanel_Comm : Panel
	{
		protected Label[] labels = new Label[7];
		protected bool showExtended = false;
		protected Boolean ShowDisplayModeHP = true;

		public bool ShowExtended
		{
			set { showExtended = value; DoSize(); }
			get { return showExtended; }
		}

		public void SetColumn(int i, string t)
		{
			labels[i].Text = t;
		}

		public void SetDisplayMode(Boolean DispMode)
		{
			ShowDisplayModeHP = DispMode;
			DoSize();
		}

		public void WriteData(System.Xml.XmlTextWriter xw)
		{
			xw.WriteStartElement("Row");
			foreach(Label l in labels)
			{
				xw.WriteElementString("Cell", l.Text);
			}
			xw.WriteEndElement();
		}

		public RowPanel_Comm()
		{
			Font f = CoreUtils.SkinningDefs.TheInstance.GetFont(11, FontStyle.Bold);
			Color c = Color.FromArgb(23,54,126);

			Color fc = Color.Cyan;

			this.BackColor = Color.White;
			//
			for(int i=0; i<7;++i)
			{
				labels[i] = new Label();
			}
			//
			this.SuspendLayout();
			labels[0].Text = "0";
			labels[0].Font = f;
			labels[0].TextAlign = ContentAlignment.MiddleCenter;
			labels[0].ForeColor = Color.White;
			labels[0].BackColor = c;
			this.Controls.Add(labels[0]);
			labels[1].Text = "Driver";
			labels[1].Font = f;
			labels[1].ForeColor = Color.White;
			labels[1].TextAlign = ContentAlignment.MiddleCenter;
			labels[1].BackColor = c;
			this.Controls.Add(labels[1]);
			labels[2].Text = "Ecosse";
			labels[2].Font = f;
			labels[2].ForeColor = Color.White;
			labels[2].TextAlign = ContentAlignment.MiddleCenter;
			labels[2].BackColor = c;
			this.Controls.Add(labels[2]);
			labels[3].Text = "0";
			labels[3].Font = f;
			labels[3].ForeColor = Color.White;
			labels[3].TextAlign = ContentAlignment.MiddleCenter;
			labels[3].BackColor = c;
			this.Controls.Add(labels[3]);
			labels[4].Text = "0";
			labels[4].Font = f;
			labels[4].ForeColor = Color.White;
			labels[4].TextAlign = ContentAlignment.MiddleCenter;
			labels[4].BackColor = c;
			this.Controls.Add(labels[4]);
			labels[5].Text = "";
			labels[5].Font = f;
			labels[5].ForeColor = Color.White;
			labels[5].TextAlign = ContentAlignment.MiddleCenter;
			labels[5].BackColor = c;
			this.Controls.Add(labels[5]);
			labels[6].Text = "";
			labels[6].Font = f;
			labels[6].ForeColor = Color.White;
			labels[6].TextAlign = ContentAlignment.MiddleCenter;
			labels[6].BackColor = c;
			this.Controls.Add(labels[6]);
		
			this.ResumeLayout(false);
			DoSize();

			this.Resize += new EventHandler(Car_Resize);
		}

		public void DoSize()
		{
			this.SuspendLayout();

			if (ShowDisplayModeHP)
			{
				if(this.showExtended)
				{
					labels[0].Location = new Point(0,0);
					labels[0].Size = new Size(40,this.Height);
					labels[1].Location = new Point(labels[0].Width+2,0);
					labels[1].Size = new Size(102,this.Height);
					labels[2].Location = new Point(labels[1].Width+labels[1].Left+2,0);
					labels[2].Size = new Size(100,this.Height);
					labels[3].Location = new Point(labels[2].Left+labels[2].Width+2);
					labels[3].Size = new Size(50,this.Height);
					labels[5].Location = new Point(labels[3].Left+labels[3].Width+2,0);
					labels[5].Size = new Size(60,this.Height);
					labels[6].Location = new Point(labels[3].Left+labels[3].Width+2,0);
					labels[6].Size = new Size(100,this.Height);
					labels[0].Visible = true;
					labels[1].Visible = true;
					labels[2].Visible = true;
					labels[3].Visible = true;
					labels[4].Visible = true;
					labels[5].Visible = true;
					labels[6].Visible = true;
				}
				else
				{
					labels[0].Location = new Point(0,0);
					labels[0].Size = new Size(40,this.Height);
					labels[1].Location = new Point(labels[0].Width+2,0);
					labels[1].Size = new Size(102,this.Height);
					labels[2].Location = new Point(labels[1].Width+labels[1].Left+2,0);
					labels[2].Size = new Size(100,this.Height);
					labels[3].Location = new Point(labels[2].Left+labels[2].Width+2);
					labels[3].Size = new Size(50,this.Height);
					labels[4].Location = new Point(labels[3].Left+labels[3].Width+2,0);
					labels[4].Size = new Size(66,this.Height);
					labels[0].Visible = true;
					labels[1].Visible = true;
					labels[2].Visible = true;
					labels[3].Visible = true;
					labels[4].Visible = true;
					labels[5].Visible = false;
					labels[6].Visible = false;
				}
			}
			else
			{
				labels[0].Location = new Point(0, 0);
				labels[0].Size = new Size(40, this.Height);
				labels[1].Location = new Point(labels[0].Width + 2, 0);
				labels[1].Size = new Size(208 - 40, this.Height);
				labels[2].Location = new Point(labels[1].Width + labels[1].Left + 2, 0);
				labels[2].Size = new Size(110 + 40, this.Height);
				labels[0].Visible = true;
				labels[1].Visible = true;
				labels[2].Visible = true;
				labels[3].Visible = false;
				labels[4].Visible = false;
				labels[5].Visible = false;

				AdjustToNewBackDropWithAlignment();
			}
			this.ResumeLayout(false);
		}

		private void AdjustToNewBackDropWithAlignment()
		{
			labels[0].Location = new Point(0+6, 0);
			labels[0].Size = new Size(40, this.Height);
			labels[1].Location = new Point(labels[0].Width + 2 + 30-15, 0);
			labels[1].Size = new Size(208 - 40, this.Height);
			labels[2].Location = new Point(labels[1].Width + labels[1].Left + 2 + 5, 0);
			labels[2].Size = new Size(110, this.Height);
			labels[0].Visible = true;
			labels[1].Visible = true;
			labels[2].Visible = true;
			labels[3].Visible = false;
			labels[4].Visible = false;
			labels[5].Visible = false;
			labels[6].Visible = false;

			labels[1].TextAlign = ContentAlignment.MiddleLeft;
			labels[2].TextAlign = ContentAlignment.MiddleRight;
		}

		private void Car_Resize(object sender, EventArgs e)
		{
			DoSize();
		}

		public void ChangeBackColour(Color newBColor, bool ApplyToLabels)
		{
			this.BackColor = newBColor;
			if (ApplyToLabels)
			{
				labels[0].BackColor = newBColor;
				labels[1].BackColor = newBColor;
				labels[2].BackColor = newBColor;
				labels[3].BackColor = newBColor;
				labels[4].BackColor = newBColor;
			}
		}

		public void ChangeForeColour(Color newFColor, bool ApplyToLabels)
		{
			this.ForeColor = newFColor;
			if (ApplyToLabels)
			{
				labels[0].ForeColor = newFColor;
				labels[1].ForeColor = newFColor;
				labels[2].ForeColor = newFColor;
				labels[3].ForeColor = newFColor;
				labels[4].ForeColor = newFColor;
			}
		}

	}

	//=====================================================================================
	//=====================================================================================
	//=====================================================================================

	// Simple class that represents a car in the race.
	public class Car_Comm : RowPanel_Comm
	{
		public int StartPos = 0;
		public int EndPos = 0;
		public int posShouldBeIn = 0;
		public int curPos = 0;
		public int aimSpeed = 0;
		public int nonHPValue = 0;
		public int nonHPTag = 0;

		public int secs = 0;
		public bool gaining = false;

		public string Pos
		{
			get { return labels[0].Text; }
			set { labels[0].Text = value; }
		}

		public string Driver
		{
			get { return labels[1].Text; }
			set { labels[1].Text = value; }
		}

		public string Team
		{
			get { return labels[2].Text; }
			set { labels[2].Text = value; }
		}

		//used in non HP situations 
		public string RevenueDisplay
		{
			get { return labels[2].Text; }
			set { labels[2].Text = value; }
		}

		protected int _speed = 0;

		public int Speed
		{
			get { return _speed; }
			set { _speed = value; labels[4].Text = _speed.ToString(); }
		}

		protected int _maxspeed = 200;

		public int MaxSpeed
		{
			get { return _maxspeed; }
			set { _maxspeed = value; }
		}

		protected int _distance = 00;

		public int Distance
		{
			get { return _distance; }
			set { _distance = value; }
		}

		public int distanceToNextCar = 0;

		public int DistanceToNextCar
		{
			get { return distanceToNextCar; }
			set
			{
				distanceToNextCar = value;
				if(value > 0)
				{
					labels[3].Text = distanceToNextCar.ToString();
				}
				else
				{
					labels[3].Text = "-";
				}
			}
		}

		public Car_Comm()
		{

		}
	}

	//=====================================================================================
	//=====================================================================================
	//=====================================================================================

	public class LeaderBoardControl_Comm : Panel
	{
		protected Car_Comm[] cars = new Car_Comm[GameConstants.RACE_LEADERBOARD];
		protected Car_Comm[] poscars = new Car_Comm[GameConstants.RACE_LEADERBOARD];
		private Boolean ShowDisplayModeHP = true;
		private string SkinHeaderName = "Driver";

		public string[] drivers = { "Driver",
																"Coley", "McNab", "Betski", "McRay", "Mount", "De Trout",
																"Spangle", "Watt", "Ross", "Watson", "Voluvent",
																"Parker", "Meadows", "Bob", "Tanaka", "InSok",
																"Laing", "Salvata", "Petersen"};

		public string[] teams = {	"Team Test", "Ecosse", "GStar", "Katana", "RDR", "Rush", "Panama", "GSM",
															"LSS", "P.Racing", "Dekspeed", "Trout", "Bund", "Rapido",
															"Villanove", "Pronto", "Flyers", "Racers", "Eggheads", "Manna"};

		protected RowPanel_Comm headings = new RowPanel_Comm();

		public int NumCars
		{
			get { return GameConstants.RACE_LEADERBOARD; }
		}

		public Car_Comm GetCar(int i)
		{
			return cars[i];
		}

		public Car_Comm[] GetCars()
		{
			return cars;
		}

		public Car_Comm[] GetPosCars()
		{
			return poscars;
		}

		public void SetDisplayMode (Boolean DispMode, string tmpSkinHeaderName)
		{
			this.ShowDisplayModeHP = DispMode;
			this.SkinHeaderName = tmpSkinHeaderName;
			BuildHeadings();

			for (int step=0; step< GameConstants.RACE_LEADERBOARD; step++)
			{
				cars[step].SetDisplayMode(DispMode); 
				poscars[step].SetDisplayMode(DispMode); 
			}
		}

		public void BuildHeadings()
		{
			if (this.ShowDisplayModeHP)
			{
				headings.SetDisplayMode(ShowDisplayModeHP);
				headings.SetColumn(0,"Pos");
				headings.SetColumn(1,SkinHeaderName);
				headings.SetColumn(2,"Team");
				headings.SetColumn(3,"LagD");
				headings.SetColumn(4,"Km/h");
				
				headings.DoSize();
			}
			else
			{
				//OTHER Game
				headings.SetDisplayMode(ShowDisplayModeHP);
				headings.SetColumn(0,"Pos");
				headings.SetColumn(1,SkinHeaderName);
				headings.SetColumn(2,"Revenue");
				headings.DoSize();
			}
			headings.ChangeForeColour(Color.Black, true);
			headings.ChangeBackColour(Color.FromArgb(174, 188, 188), true);
		}

		public LeaderBoardControl_Comm()
		{
			this.BackColor = Color.White;
			//Adjustment
			//Color c = Color.FromArgb(23, 54, 126);
			Color c = Color.FromArgb(174, 188, 188);
			this.BackColor = c;

			//Build the Heading 
			BuildHeadings();
			this.Controls.Add(headings);

			for(int i=0; i<GameConstants.RACE_LEADERBOARD; ++i)
			{
				cars[i] = new Car_Comm();
				if (this.ShowDisplayModeHP)
				{
					cars[i].Driver = drivers[i];
					cars[i].Team = teams[i];
					poscars[i] = cars[i];
				}
				else
				{
					cars[i].Driver = drivers[i];
					cars[i].Team = teams[i];
					poscars[i] = cars[i];
				}
				this.Controls.Add(cars[i]);
			}

			
			for(int i=0; i<GameConstants.RACE_LEADERBOARD; ++i)
			{
				cars[i].ChangeForeColour(Color.FromArgb(32, 32, 32), true);
				cars[i].ChangeBackColour(Color.FromArgb(174, 188, 188), true);
			}

			//Mark Our Car in Yellow
			//cars[0].ChangeForeColour(Color.Yellow, true);
			cars[0].ChangeForeColour(Color.FromArgb(255,255,255), true);
			//Connect up the event handler for the race finished 
			this.Resize += new EventHandler(GameRaceControl_Resize);
			DoSize();
		}

		public void UpdateDriverNames()
		{
			for(int i=0; i<GameConstants.RACE_LEADERBOARD; ++i)
			{
				if (cars[i] != null)
				{
					if (this.ShowDisplayModeHP)
					{
						cars[i].Team = teams[i];
						cars[i].Driver = drivers[i];
					}
					else
					{
						cars[i].Driver = drivers[i];
					}
				}
			}
		}
		
		public void DoSize()
		{
			int count = 1;
			int all_offset = 0;
			int cars_offset = 0;
			int off = all_offset + cars_offset;

			//reset the size of the heading row
			headings.Location = new Point(0, 0 + all_offset);
			headings.Size = new Size(this.Width,this.Height/(cars.Length+1));
			off += headings.Height;

			//Effectivitly reposition each car in the array order 
			foreach(Car_Comm c in poscars)
			{
				c.Location = new Point(0, off);
				c.Size = new Size(this.Width,this.Height/(cars.Length+1));
				c.Pos = count.ToString();
				off += c.Height;
				++count;
			}
		}

		public void WriteData(System.Xml.XmlTextWriter xw)
		{
			xw.WriteStartElement("LeaderBoard");
			//
			int points = 15;
			System.Globalization.CultureInfo myCI_enGB = new System.Globalization.CultureInfo( "en-GB", false );
			//
			foreach(Car_Comm c in poscars)
			{
				c.SetColumn(5,points.ToString());
				float revenue = 0.6F * points;
				string s = revenue.ToString("$#,#0.0;($#,#0.0);Zero", myCI_enGB)+"M";
				c.WriteData(xw);
				//
				if(points > 0) --points;
			}
			//
			xw.WriteEndElement();
		}

		private void GameRaceControl_Resize(object sender, EventArgs e)
		{
			DoSize();
		}
	}

	//=====================================================================================
	//=====================================================================================
	//=====================================================================================
	/// <summary>
	/// Summary description for GameRaceControl.
	/// </summary>
	public class GameRaceControl : Panel
	{
		protected Random random = new Random();
		protected int interval_ms = 10;
		protected Timer tick = new Timer();
		CultureInfo ukCulture = null;

		bool skipping;

		//The central LeaderBoard Display
		protected LeaderBoardControl_Comm board = new LeaderBoardControl_Comm();

		protected TimedFlashPlayer startFlash = new TimedFlashPlayer();
		protected TimedFlashPlayer mainFlash = new TimedFlashPlayer();
		protected TimedFlashPlayer countDownFlash = new TimedFlashPlayer();

		protected string flash_file = "";

		//The Operations Buttons
		protected Button testRace = new Button();
		protected ImageTextButton playButton = new ImageTextButton(0);
		protected ImageTextButton skipButton;

		//The Race Data 
		protected int round = 1;								//Which Round are we playing 
		protected bool finish_playing = false;	//
		protected int lapGuideSeconds = 5;			// Silly light speed car!
		protected int lapWinSeconds = 0;				// Even faster car!
		protected int lapWinLevels = 0;					// Non time based value for fastest thing
		protected int start_pos = 15;						// Standard Start Position of Our Car
		protected int end_pos = 15;							// Standard End Position of Our Car

		protected PictureBox logo = new PictureBox();		//Top Marketing Logo
		protected int secondsGained = 0;								//
		private Boolean ShowDisplayMode_HP = false;			//Operating in HP Mode 
		
		protected int raceLength = 60;									//Standard Race Length 
		protected ArrayList roundDriverOrder;						//
		protected ArrayList roundLags;									//
		int[] carLags;																	//The delays for the different cars 

		protected ArrayList winTimes;										//
		protected ArrayList winLevels;									//
		protected ArrayList teamTimes;									//
		protected ArrayList drivers;										//
		protected ArrayList teams;											//

		private DateTime startRace = DateTime.Now;
		private DateTime started = DateTime.Now;
		private DateTime ends = DateTime.Now.AddMinutes(1);
		private DateTime endoffinishline = DateTime.Now;
		private int countToAimSpeedChange = 0;
		private int countToSpeedChange = 0;

		private string driverName = "Driver";
		private int TickCounter = 0;
		
		//Revuenue profiles for 
		//Fast means a lot of revenue money early on and low at the end 
		//Slow means a little of revenue money early on and a lost at the end 
		//Normal means that the revenue is directly proportional to the time taken
		//we could pack into a 2 d array but its more visible as seperate arrays
		private const int RevenueSlots = 100;
		private int[] RevenueProfile_VF = new int[RevenueSlots]; //Very Fast Reveneue Earning Profile 
		private int[] RevenueProfile_MF = new int[RevenueSlots]; //Medium Fast Reveneue Earning Profile 
		private int[] RevenueProfile_NM = new int[RevenueSlots]; //Normal Reveneue Earning Profile 
		private int[] RevenueProfile_MS = new int[RevenueSlots]; //Medium Slow Reveneue Earning Profile 
		private int[] RevenueProfile_VS = new int[RevenueSlots]; //Very Slow Reveneue Earning Profile 
		private int[] RevenueProfile_OurCarProfile = new int[RevenueSlots]; //Our Car (adjusted to start position)

		private const int RevProfile_OtherCar_VeryFast = 1; 
		private const int RevProfile_OtherCar_MedFast = 2; 
		private const int RevProfile_OtherCar_Normal = 3; 
		private const int RevProfile_OtherCar_MedSlow = 4;
		private const int RevProfile_OtherCar_VerySlow = 5;
		private const int RevProfile_OurCar = 6;
		
		//The event to indicate that the race is finished 
		public delegate void RaceEventArgsHandler(object sender);
		public event RaceEventArgsHandler RaceFinished;

		GameManagement.NetworkProgressionGameFile gameFile;

		/// <summary>
		/// Main Constructor 
		/// </summary>
		public GameRaceControl (GameManagement.NetworkProgressionGameFile gameFile)
		{
			this.gameFile = gameFile;

			CultureInfo ukCulture = new CultureInfo("en-GB");
			BuildProfiles();

			this.SuspendLayout();
			//
			this.Controls.Add(board);
			this.Controls.Add(mainFlash);
			board.BringToFront();

			startFlash.Visible = false;
			this.Controls.Add(startFlash);
			//
			this.Resize += new EventHandler(GameRaceControl_Resize);

			tick.Tick += new EventHandler(tick_Tick);
			tick.Interval = interval_ms;
			tick.Enabled = false;

			playButton.SetButton(AppInfo.TheInstance.Location + "\\images\\buttons\\race_play.png");
			//playButton.SetVariants(AppInfo.TheInstance.Location + "\\images\\buttons\\blank_med.png");
			playButton.SetButtonText("", Color.Black, Color.Black, Color.Black, Color.Black);
			//playButton.SetButton("images\\play_green.png","images\\play_red.png","images\\play_green.png","images\\play_grey.png",Color.Blue);
			playButton.Size = new System.Drawing.Size(37, 37); 
			playButton.Location = new Point(610+3+8,18+13);
			playButton.ButtonPressed +=new CommonGUI.ImageButton.ImageButtonEventArgsHandler(playButton_ButtonPressed);
			this.Controls.Add(playButton);

#if DEBUG
			skipButton = new ImageTextButton (0);
			skipButton.SetButton(AppInfo.TheInstance.Location + "\\images\\buttons\\race_play.png");
			skipButton.SetButtonText("Skip", Color.Black, Color.Black, Color.Black, Color.Black);
			skipButton.Size = new System.Drawing.Size (37, 37);
			skipButton.Location = new Point (340, 18 + 13);
			skipButton.ButtonPressed += new CommonGUI.ImageButton.ImageButtonEventArgsHandler (skipButton_ButtonPressed);
			Controls.Add(skipButton);
			skipButton.BringToFront();
#endif

			logo.Location = new Point(442, 40 + 563-12);
			logo.Image = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\logo2.png");
			logo.SizeMode = PictureBoxSizeMode.StretchImage;
			logo.Size = new Size(140,70);
			this.Controls.Add(logo);
			logo.BringToFront();
			logo.Image = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\logo2.png");
			logo.Visible = false;

			countDownFlash.Location = new Point(695-204-80-2,635-6-607+9);
			countDownFlash.Size = new Size(204-8,46-10);
			countDownFlash.BackColor = Color.Black;
			this.Controls.Add(countDownFlash);
			countDownFlash.BringToFront();

			playButton.BringToFront();

			this.ResumeLayout(false);

			this.board.SetDisplayMode(this.ShowDisplayMode_HP, "Driver");

			LoadXML();

			DoSize();
		}


		/// <summary>
		/// Main Constructor 
		/// </summary>
		new public void Dispose()
		{
			if (countDownFlash != null)
			{
				countDownFlash.Dispose();
				countDownFlash = null;
			}
			if (startFlash != null)
			{
				startFlash.Dispose();
				startFlash= null;
			}
			if (mainFlash != null)
			{
				mainFlash.Dispose();
				mainFlash = null;
			}
		}

		#region Revenue Based Util methods 

		/// <summary>
		/// Building up the Revenuve Profiles (How quickly do companies get money)
		/// </summary>
		public void BuildProfiles ()
		{
			//			int VF_Count=0;
			//			int MF_Count=0;
			//			int NM_Count=0;
			//			int MS_Count=0;
			//			int VS_Count=0;

			//System.Diagnostics.Debug.WriteLine("Start");
			//build normal profile as default 
			for (int step=0; step < RevenueSlots; step++)
			{
				RevenueProfile_VF[step]=1;
				RevenueProfile_MF[step]=1;
				RevenueProfile_NM[step]=1;
				RevenueProfile_MS[step]=1;
				RevenueProfile_VS[step]=1;
				RevenueProfile_OurCarProfile[step]=1;
			}

			//build the meduim fast by moving some money from the end to the start
			for (int step=0; step < 10; step++)
			{
				RevenueProfile_MF[step*2]=2;
				RevenueProfile_MF[(RevenueSlots-1) -(step*2)]=0;
			}

			//build the medium slow is merely a reversed version
			for (int step=0; step < RevenueSlots; step++)
			{
				RevenueProfile_MS[step] = RevenueProfile_MF[(RevenueSlots-1) - step]; 
			}
			RevenueProfile_MS[0] = RevenueProfile_MS[(RevenueSlots-1)];
			RevenueProfile_MS[(RevenueSlots-1)] = 0;

			//build the very fast by moving some money from the end to the start
			for (int step=0; step < 5; step++)
			{
				RevenueProfile_VF[step*2]=2;
				RevenueProfile_VF[(RevenueSlots-1) -(step*2)]=0;
			}
			
			RevenueProfile_VF[3]=2;
			RevenueProfile_VF[(RevenueSlots-1) - 3]=0;
			RevenueProfile_VF[5]=2;
			RevenueProfile_VF[(RevenueSlots-1) - 5]=0;
			RevenueProfile_VF[7]=2;
			RevenueProfile_VF[(RevenueSlots-1) - 7]=0;
			
			//build the very slow is merely a reversed version of the very fast
			for (int step=0; step < RevenueSlots; step++)
			{
				RevenueProfile_VS[step] = RevenueProfile_VF[(RevenueSlots-1) - step]; 
			}
			RevenueProfile_VS[0] = RevenueProfile_VS[(RevenueSlots-1)];
			RevenueProfile_VS[(RevenueSlots-1)] = 0;


			//Verify that we have 100
			//			for (int step=0; step < RevenueSlots; step++)
			//			{
			//				VF_Count += RevenueProfile_VF[step];
			//				MF_Count += RevenueProfile_MF[step];
			//				NM_Count += RevenueProfile_NM[step];
			//				MS_Count += RevenueProfile_MS[step];
			//				VS_Count += RevenueProfile_VS[step];
			//			}
			//			System.Diagnostics.Debug.WriteLine(" VF_Count "+VF_Count.ToString());
			//			System.Diagnostics.Debug.WriteLine(" MF_Count "+MF_Count.ToString());
			//			System.Diagnostics.Debug.WriteLine(" NM_Count "+NM_Count.ToString());
			//			System.Diagnostics.Debug.WriteLine(" MS_Count "+MS_Count.ToString());
			//			System.Diagnostics.Debug.WriteLine(" VS_Count "+VS_Count.ToString());

			//			//Verify what sort of profiles, we have 
			//			int xx = 0;
			//			string st = string.Empty;
			//			for (int step=0; step < RevenueSlots; step++)
			//			{
			//				st = " step "+ step.ToString();
			//				xx = TranslateTimeCompletedtoReveneueGenerated(step,1);
			//				st += " VF " + xx.ToString();
			//				xx = TranslateTimeCompletedtoReveneueGenerated(step,2);
			//				st += " MF " + xx.ToString();
			//				xx = TranslateTimeCompletedtoReveneueGenerated(step,3);
			//				st += " NM " + xx.ToString();
			//				xx = TranslateTimeCompletedtoReveneueGenerated(step,4);
			//				st += " MS " + xx.ToString();
			//				xx = TranslateTimeCompletedtoReveneueGenerated(step,5);
			//				st += " VS " + xx.ToString();
			//				System.Diagnostics.Debug.WriteLine(st);				
			//			}
		}

		private void RebuildOurCarProfile(int startPos)
		{
			//we alter the revenue profile to match how we want to start 
			int alterMode =2;
			if (startPos < 3)
			{
				alterMode =1;
			}
			else
			{
				if (startPos>15)
				{
					alterMode =3;
				}
			}

			switch (alterMode)
			{
				case 1: //Very Very Fast Start
					//rebuild the revenue profile to match intended start profile
					for (int step=0; step < RevenueSlots; step++)
					{
						RevenueProfile_OurCarProfile[step]=RevenueProfile_VF[step];
					}

					int departureIndex = RevenueSlots-1; 
					int destinationIndex = 0; 
					int transfered = 0; 
					for (int step=0; step < 15; step++)
					{
						if (RevenueProfile_OurCarProfile[departureIndex]>0)
						{
							RevenueProfile_OurCarProfile[destinationIndex] += 1;
							RevenueProfile_OurCarProfile[departureIndex] -= 1;
							transfered +=1;
							if (transfered % 2 == 0)
							{
								destinationIndex = destinationIndex + 1;
							}
						}
						departureIndex = departureIndex - 2;
					}
					break;
				case 2: //Medium Start
					//rebuild the revenue profile to match intended start profile
					for (int step=0; step < RevenueSlots; step++)
					{
						RevenueProfile_OurCarProfile[step]=RevenueProfile_VF[step];
					}
					break;
				case 3: //Very Slow Start building up to a win 
					for (int step=0; step < RevenueSlots; step++)
					{
						RevenueProfile_OurCarProfile[step]=RevenueProfile_VS[step];
					}
					break;
			}

			//Verify that we have 100
			int VF_Count = 0;
			for (int step=0; step < RevenueSlots; step++)
			{
				VF_Count += RevenueProfile_OurCarProfile[step];
			}
			string st = VF_Count.ToString();
		}

		/// <summary>
		/// This is the method that converts the Time Completed to the revenue generated.
		/// This is a hack where we anything above 94 is rated as 100
		/// this is all ensure all positions are correct in the last 3 seconds of display
		/// This is easier than recaculating the profiles 
		/// </summary>
		/// <param name="timePercent"></param>
		/// <param name="whichprofile"></param>
		/// <returns></returns>
		public int TranslateTimeCompletedtoReveneueGenerated(int timePercent, int whichprofile)
		{
			int timePercentage = timePercent;
			if (timePercentage > 85)
			{
				timePercentage=100;
			}

			int count=0;
			for (int step =0; step < timePercent; step ++)
			{
				switch (whichprofile)
				{
					case RevProfile_OtherCar_VeryFast: //1
						count += RevenueProfile_VF[step];
						break;
					case RevProfile_OtherCar_MedFast: //2
						count += RevenueProfile_MF[step];
						break;
					case RevProfile_OtherCar_Normal://3
						count += RevenueProfile_NM[step];
						break;
					case RevProfile_OtherCar_MedSlow://4
						count += RevenueProfile_MS[step];
						break;
					case RevProfile_OtherCar_VerySlow://5
						count += RevenueProfile_VS[step];
						break;
					case RevProfile_OurCar://6
						count += RevenueProfile_OurCarProfile[step];
						break;
					default:
						count += RevenueProfile_NM[step];
						break;
				}
			}
			return count;
		}

		#endregion Revenue Based Util methods 


		public void SetDriverName(string dn)
		{
			driverName = dn;
		}

		protected void AddDrivers(XmlNode root)
		{
			drivers = new ArrayList();
			teams = new ArrayList();

			foreach(XmlNode n in root.ChildNodes)
			{
				if(n.Name.ToLower() == "d")
				{
					drivers.Add(n.InnerText);
				}
				else if(n.Name.ToLower() == "t")
				{
					teams.Add(n.InnerText);
				}
			}
		}

		public void getDriverTeamInfo(out ArrayList DR, out ArrayList TN)
		{
			DR = drivers;
			TN = teams;
		}

		public void getRndInfo(int Rnd,  out ArrayList DO, out ArrayList RL, out ArrayList TT)
		{
			DO = roundDriverOrder;
			RL = roundLags;
			TT = teamTimes;
		}

		public void getBaseTimes(out ArrayList wt)
		{
			wt = winTimes;
		}

		public void getBaseLevels(out ArrayList wt)
		{
			wt = winLevels;
		}

		protected void AddRound(XmlNode root)
		{
			ArrayList lags = new ArrayList();
			ArrayList drs = new ArrayList();

			foreach(XmlNode n in root.ChildNodes)
			{
				if(n.Name.ToLower() == "l")
				{
					// Add this lag time...
					lags.Add( int.Parse(n.InnerText) );
				}
				else if(n.Name.ToLower() == "d")
				{
					// Add this lag time...
					drs.Add( int.Parse(n.InnerText) );
				}
				else if(n.Name.ToLower() == "wintime")
				{
					int secondsOfFastestCar = int.Parse(n.SelectSingleNode("hours").InnerText)*60*60;
					secondsOfFastestCar += int.Parse(n.SelectSingleNode("mins").InnerText)*60;
					secondsOfFastestCar += int.Parse(n.SelectSingleNode("secs").InnerText);
					//
					winTimes.Add(secondsOfFastestCar);
				}
				else if(n.Name.ToLower() == "winlevel")
				{
					// Test driver's time...
					winLevels.Add( int.Parse( n.InnerText ) );
				}
				else if(n.Name.ToLower() == "td")
				{
					// Test driver's time...
					teamTimes.Add( int.Parse( n.InnerText ) );
				}
			}

			if(lags.Count > 0)
			{
				roundLags.Add(lags);
			}

			if(lags.Count > 0)
			{
				roundDriverOrder.Add(drs);
			}
		}

		protected void LoadXML()
		{
			roundLags = new ArrayList();
			winLevels = new ArrayList();
			winTimes = new ArrayList();
			teamTimes = new ArrayList();
			roundDriverOrder = new ArrayList();

			string file = AppInfo.TheInstance.Location + "\\data\\race.xml";

			XmlDocument doc = new XmlDocument();
			doc.Load(file);
			XmlNode root = doc.FirstChild;

			foreach(XmlNode n in root.ChildNodes)
			{
				if(n.Name.ToLower() == "length")
				{
					// Store the length of the race...
					raceLength = int.Parse(n.InnerText);
				}
				else if(n.Name.ToLower() == "round")
				{
					AddRound(n);// Add this round
				}
				else if(n.Name.ToLower() == "drivers")
				{
					AddDrivers(n);// Add this round
				}
			}
		}

		public void SetSecondsGained (int sc)
		{
			secondsGained = sc;
			//LoggerSimple.TheInstance.Info("***** Seconds Gained = " + secondsGained.ToString());
			
			//Calculate end position based on seconds gained or lost.
			end_pos = 0;
			bool done = false;
			int secs = this.lapWinSeconds + this.lapGuideSeconds - secondsGained;

			//Go through the cars, working out our position
			for(int i=0; (i<19)&&(!done); ++i)
			{
				int otherSecs = this.lapWinSeconds + carLags[i];

				if( secs >= otherSecs)
				{
					// We are slower than this car.
					end_pos = i+1;
					//System.Diagnostics.Debug.WriteLine("Pos " + end_pos + " is behind " + secs.ToString() 
					//	+ " :: " + otherSecs.ToString() );
				}
				else
				{
					int pp = end_pos+1;
					//System.Diagnostics.Debug.WriteLine("Pos " + pp.ToString() + " is NOT behind " 
					//	+ secs.ToString() + " :: " + otherSecs.ToString() );
					end_pos = i;
					done = true;
				}
			}

			if(end_pos > 19)
			{
				end_pos = 19;
			}
			//Setup the cars with Our Cars Start and End Position 
			SetupCars(start_pos, end_pos);
			
			RebuildOurCarProfile(start_pos);
		}

		public void SetRound(int newRound)
		{
			//Reiseed the random number system
			random = new Random( (int)(DateTime.Now.Second + DateTime.Now.Minute) );
			Random r = random;

			//Sort out the flash stuff for the start 
			playButton.Enabled = true;
			startFlash.Visible = false;
			startFlash.Stop();
			//mainFlash.OpenFile("back.swf");
			mainFlash.PlayFile("back.swf",3);
			mainFlash.Stop();

			mainFlash.Visible = true;

			//Set the internal round value 
			round = newRound;

			//Extract out the details for each round
			switch(round)
			{
				case 1:
					carLags = (int[]) ((ArrayList)roundLags[0]).ToArray( typeof(int) );
					lapWinLevels = (int) winLevels[0];
					lapWinSeconds = (int) winTimes[0];
					lapGuideSeconds = (int) teamTimes[0];
					flash_file = "europe.swf";
					start_pos = 1;
					end_pos = 15;
					break;

				case 2:
					carLags = (int[]) ((ArrayList)roundLags[1]).ToArray( typeof(int) );
					lapWinLevels = (int) winLevels[1];
					lapWinSeconds = (int) winTimes[1];
					lapGuideSeconds = (int) teamTimes[1];
					flash_file = "asia.swf";
					start_pos = 18;
					end_pos = 15;
					break;

				case 3:
					carLags = (int[]) ((ArrayList)roundLags[2]).ToArray( typeof(int) );
					lapWinLevels = (int) winLevels[2];
					lapWinSeconds = (int) winTimes[2];
					lapGuideSeconds = (int) teamTimes[2];
					flash_file = "americas.swf";
					start_pos = 19;
					end_pos = 15;
					break;
			}
			//Ensure that the timer is stopped 
			tick.Stop();
		}

		public void setDisplayModeHP(Boolean DisplayModeSkin, string SkinHeaderName)
		{
			ShowDisplayMode_HP = DisplayModeSkin;
			board.SetDisplayMode(DisplayModeSkin,SkinHeaderName);
			if (DisplayModeSkin == false)
			{
				this.tick.Interval = 100;
			}
		}
		
//		public void setobjects(PerfData_RacePosData RPD)
//		{
//			MyRPD = RPD;
//		}

		private void GameRaceControl_Resize(object sender, EventArgs e)
		{
			DoSize();
		}

		protected void DoSize()
		{
			int offset = CoreUtils.SkinningDefs.TheInstance.GetIntData("race_leaderboard_x_offset", 0);

			board.Location = new Point(this.Width/2-364/2+3+3 + offset,122-50+20+20);
			board.Size = new Size(364-17 - offset,505-10-13-20); // 720-213
			//
			mainFlash.Location = new Point(0,0);
			mainFlash.Size = this.ClientSize;
			startFlash.Location = new Point(0,0);
			startFlash.Size = this.ClientSize;
			//
			testRace.Location = new Point(0,this.Height-50);
		}

		void RunRace (bool skip)
		{
			//System.Diagnostics.Debug.WriteLine("############## RUN RACE START");
			if(random == null)
			{
				random = new Random();
				SetupCars(start_pos, end_pos);
			}
			
			TickCounter = 0;

			finish_playing = false;

			startRace = DateTime.Now;
			started = startRace.AddSeconds(15);
			ends = started.AddMinutes(1);
			endoffinishline = ends.AddSeconds(CoreUtils.SkinningDefs.TheInstance.GetIntData("race_finish_flash_duration", 12));
			skipping = false;

#if DEBUG
			if (skip)
			{
				started = startRace.AddSeconds(0.1);
				ends = started.AddSeconds(0.1);
				endoffinishline = ends.AddSeconds(0.1);

				skipping = true;
			}
#endif

			startFlash.BringToFront();
			
			startFlash.PlayFile("start.swf", started.Subtract(startRace).Seconds);
			startFlash.Visible = true;

			//
			//
			board.DoSize();

			tick.Start();
		}

		/// <summary>
		/// 
		/// </summary>
		public void StoreResults ()
		{
			Polestar_PM.OpsEngine.PM_OpsEngine.StoreResults(gameFile, gameFile.NetworkModel, round);
		}
		
		private void SetupCars(int carOne_StartPos, int carOne_EndPos)
		{
			Car_Comm[] cars = board.GetCars();
			Car_Comm[] poscars = board.GetPosCars();

			//
			ArrayList drs = roundDriverOrder[round-1] as ArrayList;

			//Set all cars to start...
			for(int i=0; i<GameConstants.RACE_LEADERBOARD; ++i)
			{
				cars[i].DistanceToNextCar = 0;
				cars[i].Speed = 0;
				cars[i].EndPos = -1;

				if(i!=0)
				{
					int wd = (int) drs[i-1];
					cars[i].Driver = (string) drivers[wd];
					if (this.ShowDisplayMode_HP)
					{
						cars[i].Team = (string) teams[wd];
					}
					else
					{
						cars[i].RevenueDisplay = "0";
					}
				}
				else
				{
					if (this.ShowDisplayMode_HP == false)
					{
						cars[0].RevenueDisplay = "0";
					}
				}
			}
			
			// Do cars on starting grid.
			cars[0].Driver = driverName;
			cars[0].StartPos = carOne_StartPos*1000;
			cars[0].EndPos = carOne_EndPos*1000;
			cars[0].posShouldBeIn = carOne_StartPos*1000;
			poscars[carOne_StartPos] = cars[0];
			cars[0].DistanceToNextCar = 3;
			//
			ArrayList positions = new ArrayList();
			for(int pos=0; pos<20; ++pos)
			{
				if(pos != carOne_StartPos)
				{
					positions.Add(pos);
				}
			}
			//
			for(int pos=1; pos<20; ++pos)
			{
				int which = random.Next(positions.Count-1);
				int dist = (int) positions[which];
				positions.RemoveAt(which);
				cars[pos].posShouldBeIn = dist*1000;
				cars[pos].StartPos = dist*1000;
				cars[pos].DistanceToNextCar = 3;
			}
			
			//Setup the end positions 
			int xpos = 0;
			for(int i=0; i <19; ++i)
			{
				if(xpos == end_pos)
				{
					cars[i+1].EndPos = (++xpos)*1000;
					//SetPos(cars,i, ++xpos);
				}
				else
				{
					cars[i+1].EndPos = xpos*1000;
					//SetPos(cars,i, xpos);
				}
				++xpos;
			}
			//
			PlaceCars();
			board.DoSize();
		}

		private void tick_Tick(object sender, EventArgs e)
		{
			// Shift cars about...
			// Have a start position for our target car and an end position.
			//TimeSpan span = ends - DateTime.Now;
			if(DateTime.Now.Ticks < started.Ticks)
			{
				// Showing the starting video.
			}
			else if(DateTime.Now.Ticks > endoffinishline.Ticks)
			{
				//Race is finished, We call a black empty swf file to ensure that all sound is stopped 
				//rather that letting the finish.swf carry on. The Flashbox stop dosen't work properly.
				//This also ensure that there is nothing to play when the next round starts.
				countDownFlash.Stop();
				//countDownFlash.PlayFile("launch.swf"); // Fix for stopping never ending sound. Need to load a blank swf with no sound in it.
				countDownFlash.PlayFile("black_null.swf"); // 
				//
				tick.Stop();
				startFlash.Stop();
				startFlash.PlayFile("black_null.swf"); //
				// TODO. LP
				startFlash.Visible = true;
				mainFlash.Stop();
				mainFlash.PlayFile("black_null.swf"); 
				mainFlash.Visible = false;
				StoreResults();
				//
				if(null != RaceFinished)
				{
					RaceFinished(this);
				}
			}
			else if(DateTime.Now.Ticks > ends.Ticks)
			{
				//System.Diagnostics.Debug.WriteLine("############## END OF TIMER");
				if(!finish_playing)
				{
					countDownFlash.Stop();							//Stop the Timer Countdown
					startFlash.PlayFile("finish.swf", CoreUtils.SkinningDefs.TheInstance.GetIntData("race_finish_flash_duration", 12));	//Load the Finishing Line Flash
					startFlash.Show();									//Play the Finishing Line Flash
					mainFlash.Stop();										//Stop the Main Flash
					finish_playing = true;							//Set the finish playing toggle
				}
			}
			else
			{
				if(startFlash.Visible)
				{
					//Play the intro countdown Flash File					
					countDownFlash.Stop();	
					countDownFlash.Rewind();
					//TimedFlashPlayer.
					countDownFlash.PlayFile("60sec.swf",61);
					mainFlash.PlayFile(flash_file);
					startFlash.Visible = false;

#if DEBUG
					if (! skipping)
					{
#endif
						ends = DateTime.Now.AddMinutes(1);
						endoffinishline = ends.AddSeconds(CoreUtils.SkinningDefs.TheInstance.GetIntData("race_finish_flash_duration", 12));

#if DEBUG
					}
#endif
				}
				// If the starting video is still there then hide and
				// how the leader board...
				//

				TickCounter++;
				long ticksleft = ends.Ticks - DateTime.Now.Ticks;
				long totalTicks = ends.Ticks - started.Ticks;
				double percentFromFinalPos = ((double)ticksleft)/((double)totalTicks);
				//System.Diagnostics.Debug.WriteLine(" Percent "+percentFromFinalPos.ToString() 
				//	+ " tc "+TickCounter.ToString()+" T:"+DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss.fff"));
				//
				if (this.ShowDisplayMode_HP)
				{
					MoveCarsForHP(percentFromFinalPos);
				}
				else
				{
					//System.Diagnostics.Debug.WriteLine(" Percent -->"+percentFromFinalPos.ToString());
					percentFromFinalPos = percentFromFinalPos * 100;
					//System.Diagnostics.Debug.WriteLine(" Percent -->"+percentFromFinalPos.ToString());
					double percentFromStart = 100 - percentFromFinalPos;
					percentFromStart = MyNumericRounder((percentFromStart), 1, 1);
					//System.Diagnostics.Debug.WriteLine(" Percent (S)--> "+percentFromStart.ToString());
					//percentFromFinalPos = (double)TickCounter;
					MoveCarsForNonHP(percentFromStart);
				}
			}
		}

		public double MyNumericRounder(double x, int numerator, int denominator)
		{ // returns the number nearest x, with a precision of numerator/denominator
			// example: Round(12.1436, 5, 100) will round x to 12.15 (precision = 5/100 = 0.05)
			long y = (long)Math.Floor(x * denominator + (double)numerator / 2.0);
			return (double)(y - y % numerator)/(double)denominator;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="percentFromFinalPos">How far from End</param>
		private void MoveCarsForNonHP(double percentFromStart)
		{
			int gap_step = 0;
			double percentRevenueBooked = 0;

			// Show the end-of-round results for the last 5 seconds.
			if (percentFromStart >= 91)
			{
				percentFromStart = 100;
			}

			Car_Comm[] allcars = board.GetCars();
			for (int step=0; step < GameConstants.RACE_LEADERBOARD; step ++)
			{
				int car_secs = 0;
				int car_revenue_mult = 25000;
				double car_revenue = 0;
				int car_revenue_units = 0;
				string dr = allcars[step].Driver;
				//System.Diagnostics.Debug.WriteLine("##step:"+step.ToString() + " Driver:" + dr + " percentFromStart:"+percentFromStart.ToString());
				if (step == 0)
				{
					//Use different profiles for different rounds for our car 
					percentRevenueBooked = TranslateTimeCompletedtoReveneueGenerated((int)percentFromStart, 6);
					//Our Cars (different calculation)
					car_secs = this.lapWinLevels - (this.lapGuideSeconds - secondsGained);
				}
				else
				{
					//Use different profiles for different rounds for each car 
					percentRevenueBooked = TranslateTimeCompletedtoReveneueGenerated((int)percentFromStart, (step % 5) + 1);
					//Other Cars (different calculation) 
					car_secs = this.lapWinLevels - carLags[gap_step];
					gap_step++;
				}

				//System.Diagnostics.Debug.WriteLine("  car_secs:"+car_secs.ToString() + " percentRevenueBooked:"+percentRevenueBooked.ToString() );
				car_secs = (int)(((double)car_secs)*(percentRevenueBooked/100));
				car_revenue = car_secs * car_revenue_mult;
				//System.Diagnostics.Debug.WriteLine("  car_secs:"+car_secs.ToString() + " car_revenue:"+car_revenue.ToString() );
				car_revenue_units = (int) car_revenue;

				if (car_revenue_units>1000)
				{
					allcars[step].RevenueDisplay = car_revenue_units.ToString("0,0",ukCulture);
				}
				else
				{
					allcars[step].RevenueDisplay = car_revenue_units.ToString();
				}
				allcars[step].nonHPValue = car_revenue_units;
			}
			//need to sort out the cars into revenue order 
			PlaceCarsInOrder_NonHP();
			//need to alter the individual Row Positions from the positional data
			board.SuspendLayout();
			board.DoSize();
			board.ResumeLayout(false);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="percentFromFinalPos">How far from End</param>
		private void MoveCarsForHP(double percentFromFinalPos)
		{
			Car_Comm[] poscars = board.GetPosCars();
			Car_Comm[] allcars = board.GetCars();

			//Move the alteration counts down
			--countToAimSpeedChange;
			--countToSpeedChange;

			// Calculate where the cars should be.
			for(int i=0; i<GameConstants.RACE_LEADERBOARD; ++i)
			{
				int posShouldBeIn = (int)( ((double)(poscars[i].StartPos-poscars[i].EndPos))*percentFromFinalPos ) + poscars[i].EndPos;

				if(countToAimSpeedChange <=0)
				{
					int p = i*1000;

					if(p < posShouldBeIn)
					{
						poscars[i].aimSpeed = 210 - random.Next(0,10) + GameConstants.RACE_LEADERBOARD-i;
					}
					else if(p > posShouldBeIn)
					{
						poscars[i].aimSpeed = 210 + random.Next(0,10) + GameConstants.RACE_LEADERBOARD-i;
					}
					else
					{
						poscars[i].gaining = !poscars[i].gaining;
						poscars[i].aimSpeed = 210 + (random.Next(0,10)-5) + GameConstants.RACE_LEADERBOARD-i;
					}
				}
				else
				{
					if(countToSpeedChange <= 0)
					{
						if(poscars[i].aimSpeed > poscars[i].Speed)
						{
							poscars[i].Speed++;
						}
						else if(poscars[i].aimSpeed < poscars[i].Speed)
						{
							poscars[i].Speed--;
						}
					}
				}
				//
				//
				if(i*1000 < posShouldBeIn)
				{
					poscars[i].gaining = false;
				}
				else
				{
					poscars[i].gaining = true;
				}
				poscars[i].posShouldBeIn = posShouldBeIn;
				poscars[i].curPos = i;
			}

			Car_Comm[] cs = board.GetCars();
			for(int i=0; i<GameConstants.RACE_LEADERBOARD; ++i)
			{
				Car_Comm cc = cs[i];
				int ii = cc.posShouldBeIn;
				int jj = cc.Distance;
				string Dr = cc.Driver; 
				//System.Diagnostics.Debug.WriteLine("Pos "+i.ToString()+ "  " + Dr+ " Distance: "+jj.ToString() + "  PosShould:"+ ii.ToString());
				//System.Diagnostics.Debug.WriteLine(jj.ToString() + " : Car should be in pos " + ii.ToString() 
				//	+ " [" + this.end_pos.ToString() + "]");
			}

			if(countToAimSpeedChange <= 0)
			{
				countToAimSpeedChange = 100;
			}

			if(countToSpeedChange <= 0)
			{
				if(poscars[0].Speed < 100)
				{
					countToSpeedChange = 0;
				}
				else if(poscars[0].Speed < 200)
				{
					countToSpeedChange = 4;
				}
				else
				{
					countToSpeedChange = 10;
				}
			}
				
			// Order the cars into a definitive order.
			PlaceCars();
				
			// Calculate the cars' lag times...
			board.SuspendLayout();
			poscars[0].DistanceToNextCar = 0;
			//poscars[0].tocatch.Text = "-";
			//
			for(int i=19; i>=0; --i)
			{
				if(i>0)
				{
					int dtnc = (poscars[i].Distance - poscars[i-1].Distance)/10;
					if(dtnc < poscars[i].DistanceToNextCar)
					{
						if(poscars[i].Speed <= poscars[i-1].Speed)
						{
							poscars[i].Speed = poscars[i-1].Speed+1;
						}
					}
					else
					{
						if(poscars[i].Speed > poscars[i-1].Speed)
						{
							poscars[i].Speed = poscars[i-1].Speed - 1;
							if(poscars[i].Speed < 1) poscars[i].Speed = 1;
						}
					}
					poscars[i].DistanceToNextCar = dtnc;
				}
			}
			//
			board.DoSize();
			board.ResumeLayout(false);
		}

		/// <summary>
		/// </summary>
		private void PlaceCars()
		{
			// Order the cars into a definitive order.
			Car_Comm[] allcars = board.GetCars();
			Car_Comm[] poscars = board.GetPosCars();

			for(int i=0; i<20; ++i)
			{
				int lowestCar = 0;
				int lowestPos = 99000;
				for(int j=0; j<20; ++j)
				{
					if(allcars[j].posShouldBeIn < lowestPos)
					{
						lowestPos = allcars[j].posShouldBeIn;
						lowestCar = j;
					}
				}
				//
				if(lowestPos != 99000)
				{
					poscars[i] = allcars[lowestCar];
					allcars[lowestCar].Distance = allcars[lowestCar].posShouldBeIn;
					allcars[lowestCar].posShouldBeIn = 100000;
				}
			}
		}

		/// <summary>
		/// </summary>
		private void PlaceCarsInOrder_NonHP()
		{
			// Order the cars into a definitive order.
			Car_Comm[] allcars = board.GetCars();
			Car_Comm[] poscars = board.GetPosCars();

			//System.Diagnostics.Debug.WriteLine(" ##PreSlot");
//			for(int step=0; step<20; step++)
//			{
//			  int aa = poscars[step].nonHPValue;
//				System.Diagnostics.Debug.WriteLine(" Step "+step.ToString()+" Car "+ poscars[step].Driver + ":"+ aa.ToString());
//			}

			Boolean proceed= true;
			Boolean Tagsfound = false;
			for(int step=0; step<20; step++)
			{
				poscars[step] = allcars[step];
				allcars[step].nonHPTag = 0;
				if (allcars[step].nonHPValue !=0)
				{Tagsfound = true;}
			}
			int SortedIndex = 0;
			int CarCount = 0;
			proceed = Tagsfound;
			while (proceed)
			{
				int FoundIndex = -1;
				double max=-999;
				double car_value = 0;
				//System.Diagnostics.Debug.WriteLine(" Loop CarCount"+CarCount.ToString());
				for(int step=0; step<20; step++)
				{
					if ((int)allcars[step].nonHPTag == 0)
					{
						if (step==0)
						{
							car_value = allcars[step].nonHPValue;
						}
						else
						{
							car_value = (allcars[step].nonHPValue + (step+5) /100 );
						}
						
						if (car_value >= max)
						{
							FoundIndex = step;
							max = car_value;
							//System.Diagnostics.Debug.WriteLine(" Poss Car "+ allcars[FoundIndex].Driver + ":"+ max.ToString());
						}
					}
				}
				if (FoundIndex != -1)
				{
					//System.Diagnostics.Debug.WriteLine(" cand Car "+ allcars[FoundIndex].Driver + ":"+ max.ToString());
					poscars[SortedIndex] = allcars[FoundIndex];
					allcars[FoundIndex].nonHPTag = 1;
					CarCount++;
					SortedIndex++;
				}
				if (CarCount>19)
				{
					proceed = false;
				}
			}

//		System.Diagnostics.Debug.WriteLine(" ##PostSlot");
//		for(int step=0; step<20; step++)
//		{
//			int aa = poscars[step].nonHPValue;
//			System.Diagnostics.Debug.WriteLine(" Step "+step.ToString()+" Car "+ poscars[step].Driver + ":"+ aa.ToString());
//		}

		}

		private void playButton_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			playButton.Enabled = false;
			playButton.Visible = false;
			RunRace(false);
		}

		private void skipButton_ButtonPressed (object sender, ImageButtonEventArgs args)
		{
			playButton.Enabled = false;
			playButton.Visible = false;

			skipButton.Enabled = false;
			skipButton.Visible = false;
			RunRace(true);
		}
	}

}
