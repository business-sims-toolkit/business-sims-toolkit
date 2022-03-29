using System;
using System.Windows.Forms;
using System.Drawing;
using System.Collections;
using System.Xml;
using System.IO;

using GameManagement;
using ReportBuilder;
using CoreUtils;
using LibCore;

namespace GameDetails
{
	/// <summary>
	/// Summary description for PDFSummary.
	/// </summary>
	public class PDFSummary : GameDetailsSection
	{
		protected NetworkProgressionGameFile _gameFile;
		protected EditGamePanel _gamePanel;
		protected DateTime _gameGameCreated = DateTime.Now; 

		protected string overide_pdf_file = "";

		protected Font MyDefaultSkinFontBold9;
		protected Font MyDefaultSkinFontNormal8;
		protected Button _pdf_summary = null;
		protected Button _csv_summary = null;
		protected DateTimePicker datePicker = null;
		Label dateLabel = null;

		protected bool _ShowCSVButton= false;

		public void OveridePDF(string file)
		{
			overide_pdf_file = file;
		}

		public DateTime getDateCreated(string filename)
		{
			string firstPart = "";
			string lastPart = "";

			string shortname = GameUtils.FileNameToGameName(Path.GetFileName(filename),out firstPart, out lastPart);

			// Remove spurious end tags...
			firstPart = firstPart.Replace("-#","");
			char[] dash = { '_' };
			string[] dateParts = firstPart.Split(dash);
			int year = CONVERT.ParseInt(dateParts[0]);
			int month = CONVERT.ParseInt(dateParts[1]);
			int day = CONVERT.ParseInt(dateParts[2]);

			// Actually display the date on the screen in local format to help the user.
			DateTime date = new DateTime(year,month,day);
			return date;
		}

		public PDFSummary(NetworkProgressionGameFile gameFile, EditGamePanel gamePanel, bool showCSVButton)
		{
			_gameFile = gameFile;
			_gamePanel = gamePanel;
			_ShowCSVButton = showCSVButton;

			string fn = gameFile.Name;
			FileInfo fi = new FileInfo(fn);
			string fn2 = fi.Name;
			string fn3 = fi.FullName;
			
			_gameGameCreated = getDateCreated(fn2);

			string fontname =  SkinningDefs.TheInstance.GetData("fontname");
			MyDefaultSkinFontBold9 = ConstantSizeFont.NewFont(fontname, 9, FontStyle.Bold);
			MyDefaultSkinFontNormal8 = ConstantSizeFont.NewFont(fontname, 8);

			this.Title = "Generate Summary Report";
			this.Resize += ResizeHandler;
			
			//Create the PDF Button 
			_pdf_summary = new Button();
			_pdf_summary.Name = "_pdf_summary Button";
			_pdf_summary.Visible = false;
			_pdf_summary.Font = this.MyDefaultSkinFontBold9;
			_pdf_summary.Location = new Point(100,5+5);
			_pdf_summary.Size = new Size(150,30);
		    if (SkinningDefs.TheInstance.GetBoolData("windows_buttons_styled", true))
		    {
		        _pdf_summary.BackColor =
		            SkinningDefs.TheInstance.GetColorDataGivenDefault("windows_button_colour", Color.LightSteelBlue);
		        _pdf_summary.ForeColor =
		            SkinningDefs.TheInstance.GetColorDataGivenDefault("windows_button_foreground_colour", Color.Black);
		    }
		    _pdf_summary.Text = "Generate Summary";
			_pdf_summary.Enabled = true;
			_pdf_summary.Click += pdf_Click;
			panel.Controls.Add(_pdf_summary);

			_csv_summary = new Button();
      _csv_summary.Name = "_csv_summary Button";
			_csv_summary.Visible = false;
			_csv_summary.Font = this.MyDefaultSkinFontBold9;
			_csv_summary.Location = new Point(100,5+5);
			_csv_summary.Size = new Size(150,30);
		    if (SkinningDefs.TheInstance.GetBoolData("windows_buttons_styled", true))
		    {
		        _csv_summary.BackColor =
		            SkinningDefs.TheInstance.GetColorDataGivenDefault("windows_button_colour", Color.LightSteelBlue);
		        _csv_summary.ForeColor =
		            SkinningDefs.TheInstance.GetColorDataGivenDefault("windows_button_foreground_colour", Color.Black);
		    }
		    _csv_summary.Text = "Generate CSV Summary";
			_csv_summary.Enabled = true;
			_csv_summary.Click += csv_Click;
			panel.Controls.Add(_csv_summary);

			if (_ShowCSVButton)
			{
				_pdf_summary.Width = 160;
				_pdf_summary.Text = "Generate PDF Summary";
				_csv_summary.Width = 160;
				//Need to space the 2 button nicely and show both buttons
				int gap = (this.Width - (_csv_summary.Width+_pdf_summary.Width)) / 3;
				_pdf_summary.Left = gap;
				_csv_summary.Left = (2*gap) + _pdf_summary.Width;
				_pdf_summary.Visible = true;
				_csv_summary.Visible = true;
			}
			else
			{
				//Need to put the single button in the middle
				int gap = (this.Width - (_pdf_summary.Width)) / 2;
				_pdf_summary.Left = gap;
				_pdf_summary.Visible = true;
			}

			datePicker = new DateTimePicker ();

			// If it's a sales game, use the date of the release.
			if (_gameFile.IsSalesGame)
			{
				datePicker.Value = File.GetCreationTime(_gameFile.FileName);
			}
			else
			{
				datePicker.Value = _gameGameCreated;
			}
			datePicker.Size = new Size(_pdf_summary.Width, 30);
			datePicker.Font = this.MyDefaultSkinFontNormal8;
			datePicker.Format = DateTimePickerFormat.Custom;
			datePicker.CustomFormat = "d MMMM yyyy";
			panel.Controls.Add(datePicker);

			dateLabel = new Label ();
			dateLabel.Font = this.MyDefaultSkinFontNormal8;
			dateLabel.Text = "Date for report:";
			dateLabel.Size = new Size (200, 15);
			dateLabel.TextAlign = ContentAlignment.MiddleLeft;
			panel.Controls.Add(dateLabel);

			bool allowPDFSummary = SkinningDefs.TheInstance.GetBoolData("allow_pdf_report", true);
			if (allowPDFSummary == false)
			{
				_pdf_summary.Enabled = false;
				_csv_summary.Enabled = false;
				dateLabel.Enabled = false;
				datePicker.Enabled = false;
			}

			SetSize(460, 150);
		}

		void ResizeHandler(object sender, EventArgs e)
		{
			if (_ShowCSVButton)
			{
				//Need to space the 2 button nicely and show both buttons
				int gap = (this.Width - (_csv_summary.Width+_pdf_summary.Width)) / 3;
				_pdf_summary.Left = gap;
				_csv_summary.Left = (2*gap) + _pdf_summary.Width;
				_pdf_summary.Visible = true;
				_csv_summary.Visible = true;
			}
			else
			{
				//Need to put the single button in the middle
				int gap = (this.Width - (_pdf_summary.Width)) / 2;
				_pdf_summary.Left = gap;
				_pdf_summary.Visible = true;
			}

			datePicker.Location = new Point (_pdf_summary.Left, _pdf_summary.Bottom + 25);
			//dateLabel.Location = new Point (datePicker.Left - 10 - dateLabel.Width, datePicker.Top + ((datePicker.Height - dateLabel.Height) / 2));
			dateLabel.Location = new Point(_pdf_summary.Left, _pdf_summary.Bottom + 10);
		}

		#region Helper classes For CSV work

		protected string stripcomma(string textdata)
		{
			return textdata.Replace(",","");
		}

		protected float sumArray(float[] roundvalues)
		{
			float sum = 0f;
			if (roundvalues != null)
			{
				foreach (float val in roundvalues)
				{
					sum += val;
				}

			}
			return sum;
		}

		#endregion Helper classes For CSV work

		protected void csv_Click(object sender, EventArgs e)
		{
			if (!_gamePanel.ValidateFields())
			{
				MessageBox.Show(TopLevelControl, "Please complete game details information","Game Details Not Complete");
			}
			else
			{
				string name = _gameFile.Name;
				string reportfilename = name.Replace("gmz", "csv");
				GenerateCSV(reportfilename);
			}
		}

		Image CloneImageOntoColour (Image source, Color background)
		{
			Bitmap copy = new Bitmap(source.Width, source.Height);
			using (Graphics graphics = Graphics.FromImage(copy))
			{
				if (background != Color.Transparent)
				{
					using (Brush brush = new SolidBrush (background))
					{
						graphics.FillRectangle(brush, 0, 0, copy.Width, copy.Height);
					}
				}

				graphics.DrawImage(source, 0, 0, copy.Width, copy.Height);
			}

			return copy;
		}

		protected void pdf_Click(object sender, EventArgs e)
		{
			if(overide_pdf_file != "")
			{
				// Just show the overidden PDF.
				string pdf = LibCore.AppInfo.TheInstance.Location + "\\" + overide_pdf_file;
				if (File.Exists(pdf))
				{
					try
					{
						System.Diagnostics.Process.Start(pdf);		
					}
					catch (Exception evc)
					{
						if (evc.Message.IndexOf("No Application")>-1)
						{
							MessageBox.Show(_gamePanel.TopLevelControl, "Cannot present PDF Summary Sheet ", "No PDF Reader Application Installed"
								,MessageBoxButtons.OK,MessageBoxIcon.Error);
						}
						else
						{
							try
							{
								System.Diagnostics.Process.Start(pdf.Replace(" ", "%20"));
							}
							catch (Exception)
							{
								try
								{
									System.Diagnostics.Process.Start("\"" + pdf + "\"");
								}
								catch (Exception evc3)
								{
									MessageBox.Show(_gamePanel.TopLevelControl, "Cannot present PDF Summary Sheet because: " + evc3.Message + ".", "Failed to Start PDF Reader"
										, MessageBoxButtons.OK, MessageBoxIcon.Error);
								}
							}
						}
					}
				}
				return;
			}

			//check all data been entered for game before generating pdf
			if (!_gamePanel.ValidateFields())
			{
				return;
			}

			string name = _gameFile.Name;
			string reportfilename = name.Replace("gmz", "pdf");

			Bitmap back = Repository.TheInstance.GetImage(LibCore.AppInfo.TheInstance.Location + "\\images\\PDFTemplate.png");
			string fname = _gameFile.Dir + "\\global\\team_photo.png";
			Bitmap teamphoto;
			Bitmap tmpBMP;

			if (File.Exists(fname))
			{
				tmpBMP = new Bitmap(fname);
				teamphoto = (Bitmap)BitmapUtils.ConvertToApectRatio(tmpBMP, 1.45).Clone();

				tmpBMP.Dispose();
				tmpBMP = null;
				System.GC.Collect();
			}
			else
			{
				//If we have a proper place holder then use that else use the splashscreen image
				string placeholder_filename = LibCore.AppInfo.TheInstance.Location + "\\images\\PDF_Placeholder.png";
				if (File.Exists(placeholder_filename))
				{
					tmpBMP = (Bitmap)LibCore.Repository.TheInstance.GetImage(placeholder_filename);
				}
				else
				{
					tmpBMP = (Bitmap)LibCore.Repository.TheInstance.GetImage(LibCore.AppInfo.TheInstance.Location + "\\images\\v3SplashScreen.png");
					
				}
				teamphoto = (Bitmap)BitmapUtils.ConvertToApectRatio(tmpBMP, 1.45).Clone();
			}

			string logoName = _gameFile.Dir + @"\global\facil_logo.png";
			Bitmap facilitatorLogo;
			if (File.Exists(logoName))
			{
				using (Bitmap source = new Bitmap(logoName))
				{
					facilitatorLogo = (Bitmap) CloneImageOntoColour(source, Color.White);
				}
			}
			else
			{
				facilitatorLogo = (Bitmap) Repository.TheInstance.GetImage(LibCore.AppInfo.TheInstance.Location + @"\images\DefFacLogo.png");
			}

			if (facilitatorLogo != null)
			{
				facilitatorLogo = (Bitmap) BitmapUtils.ConvertToApectRatio(facilitatorLogo, 200 / 50).Clone();
				facilitatorLogo = (Bitmap) CloneImageOntoColour(facilitatorLogo, Color.White);
			}

			GeneratePDF(teamphoto, back, facilitatorLogo, reportfilename);
		}

		protected virtual void GenerateCSV(string reportfilename)
		{
			bool GenerationFailled = false;
			//FOR THE REFACTOR - pass all this info in as an object, 
			//no time to change all the pdf code just now
			string[] members = new string[20];
			string[] roles = new string[20];
			this.GetTeamMembersandRoles(members,roles);

			string title = "";
			string venue = "";
			GetTitleandVenue(ref title, ref venue);
			
			int NumRounds = 5;
			int NumTeams = 5;

			float[] avails = new float[NumRounds];
			float[] profits = new float[NumRounds];
			int[] pointsR1 = new int[NumTeams];
			int[] pointsR2 = new int[NumTeams];
			int[] pointsR3 = new int[NumTeams];
			int[] pointsR4 = new int[NumTeams];
			int[] pointsR5 = new int[NumTeams];
			int[] champPoints = new int[NumTeams];
			float[] fixedcosts = new float[NumRounds];
			float[] gains = new float[NumRounds];
			float[] indicators = new float[NumRounds];
			float[] mtrs = new float[NumRounds];
			float[] projcosts = new float[NumRounds];
			float[] revenues = new float[NumRounds];
			float[] supportbudgets = new float[NumRounds];
			float[] supportprofits = new float[NumRounds];
			float[] supportspends = new float[NumRounds];
			float[] points = new float[NumRounds];

			Hashtable champ = new Hashtable();

			int prevprofit = 0;
			int newservices = 0;
			for (int i=1; i<=_gameFile.LastOpsRoundPlayed; i++)
			{
				SupportSpendOverrides sso = new SupportSpendOverrides(_gameFile);
				RoundScores score = new RoundScores(_gameFile, i, prevprofit, newservices, sso);

				prevprofit = score.Profit;

				float tmp = (float)score.Availability;
				avails.SetValue(tmp,i-1);
				float profit = (float)score.Profit / 1000000;
				profits.SetValue(profit,i-1);
				tmp = (float)score.FixedCosts / 1000000;
				fixedcosts.SetValue(tmp,i-1);
				tmp = (float)score.Gain / 1000000;
				gains.SetValue(tmp,i-1);
				tmp = (float)score.IndicatorScore;
				indicators.SetValue(tmp,i-1);
				mtrs.SetValue((float)score.MTTR,i-1);
				tmp = (float)score.ProjectSpend / 1000000;
				projcosts.SetValue(tmp,i-1);
				tmp = (float)score.Revenue / 1000000;
				revenues.SetValue(tmp,i-1);
				tmp = (float)score.SupportBudget / 1000000;
				supportbudgets.SetValue(tmp,i-1);
				tmp = (float)score.SupportProfit / 1000000;
				supportprofits.SetValue(tmp,i-1);
				tmp = (float)score.SupportCostsTotal / 1000000;
				supportspends.SetValue(tmp,i-1);
				points.SetValue((float)score.Points,i-1);

				Hashtable teams = new Hashtable();
				teams = score.GetRoundPoints();
				ArrayList teamnames = new ArrayList(teams.Keys);
				teamnames.Sort();

				int val=0;
				foreach (string team in teamnames)
				{
					int pt = (int)teams[team];

					if (champ.ContainsKey(team))
					{
						int pts = (int)champ[team] + pt;
						champ[team] = pts;
					}
					else
					{
						champ[team] = pt;
					}

					if (i==1)
					{
						pointsR1.SetValue(pt, val);
						val++;
					}
					else if (i==2)
					{
						pointsR2.SetValue(pt, val);
						val++;
					}
					else if (i==3)
					{
						pointsR3.SetValue(pt, val);
						val++;
					}
					else if (i==4)
					{
						pointsR4.SetValue(pt, val);
						val++;
					}
					else if (i==5)
					{
						pointsR5.SetValue(pt, val);
						val++;
					}
				}

				val =0;
				foreach (string team in teamnames)
				{
					champPoints.SetValue((int)champ[team], val);
					val++;
				}
			}

			//
			try
			{
				//Create the file
				StreamWriter writer = new StreamWriter(reportfilename,false);
				if (writer != null)
				{
					//Basic Game Version (Just the totals) 
					//GameTitle, GameVenue, GameDate, ActualRevenue, SupportSpend, ProfitLoss, MTRS, IndicatorScore 
					string line = "";
					line += stripcomma(title) + ",";
					line += stripcomma(venue) + ",";
					line += "23/99/9999" + ",";
					writer.WriteLine(line);
					writer.Close();
				}
			}
			catch (Exception)
			{
				MessageBox.Show(_gamePanel.TopLevelControl, "Cannot present CSV Summary Sheet", "CSV Generation Failled"
					,MessageBoxButtons.OK,MessageBoxIcon.Error);
			}


			//Open the Report 
			if (GenerationFailled==false)
			{
				if (File.Exists(reportfilename))
				{
					try
					{
						System.Diagnostics.Process.Start(reportfilename);		
					}
					catch (Exception)
					{
						MessageBox.Show(_gamePanel.TopLevelControl, "Cannot present CSV Summary Sheet ", "No CSV Reader Application Installed"
							,MessageBoxButtons.OK,MessageBoxIcon.Error);
					}
				}
			}
		}

		protected virtual void GeneratePDF (Bitmap teamphoto, Bitmap back, Bitmap facilitatorLogo, string reportfilename)
		{
			//FOR THE REFACTOR - pass all this info in as an object, 
			//no time to change all the pdf code just now
			string[] members = new string[20];
			string[] roles = new string[20];
			this.GetTeamMembersandRoles(members,roles);

			string title = "";
			string venue = "";
			GetTitleandVenue(ref title, ref venue);
			
			int NumRounds = 5;
			int NumTeams = 5;

			float[] avails = new float[NumRounds];
			float[] profits = new float[NumRounds];
			int[] pointsR1 = new int[NumTeams];
			int[] pointsR2 = new int[NumTeams];
			int[] pointsR3 = new int[NumTeams];
			int[] pointsR4 = new int[NumTeams];
			int[] pointsR5 = new int[NumTeams];
			int[] champPoints = new int[NumTeams];
			float[] fixedcosts = new float[NumRounds];
			float[] gains = new float[NumRounds];
			float[] indicators = new float[NumRounds];
			float[] mtrs = new float[NumRounds];
			float[] projcosts = new float[NumRounds];
			float[] revenues = new float[NumRounds];
			float[] supportbudgets = new float[NumRounds];
			float[] supportprofits = new float[NumRounds];
			float[] supportspends = new float[NumRounds];
			float[] points = new float[NumRounds];

			Hashtable champ = new Hashtable();

			int prevprofit = 0;
			int newservices = 0;
			for (int i=1; i<=_gameFile.LastRoundPlayed; i++)
			{
				SupportSpendOverrides sso = new SupportSpendOverrides(_gameFile);
				RoundScores score = new RoundScores(_gameFile, i, prevprofit, newservices, sso);

				prevprofit = score.Profit;

				float tmp = (float)score.Availability;
				avails.SetValue(tmp,i-1);
				float profit = (float)score.Profit / 1000000;
				profits.SetValue(profit,i-1);
				tmp = (float)score.FixedCosts / 1000000;
				fixedcosts.SetValue(tmp,i-1);
				tmp = (float)score.Gain / 1000000;
				gains.SetValue(tmp,i-1);
				tmp = (float)score.IndicatorScore;
				indicators.SetValue(tmp,i-1);
				mtrs.SetValue((float)score.MTTR,i-1);
				tmp = (float)score.ProjectSpend / 1000000;
				projcosts.SetValue(tmp,i-1);
				tmp = (float)score.Revenue / 1000000;
				revenues.SetValue(tmp,i-1);
				tmp = (float)score.SupportBudget / 1000000;
				supportbudgets.SetValue(tmp,i-1);
				tmp = (float)score.SupportProfit / 1000000;
				supportprofits.SetValue(tmp,i-1);
				tmp = (float)score.SupportCostsTotal / 1000000;
				supportspends.SetValue(tmp,i-1);
				points.SetValue((float)score.Points,i-1);

				Hashtable teams = new Hashtable();
				teams = score.GetRoundPoints();
				ArrayList teamnames = new ArrayList(teams.Keys);
				teamnames.Sort();

				int val=0;
				foreach (string team in teamnames)
				{
					int pt = (int)teams[team];

					if (champ.ContainsKey(team))
					{
						int pts = (int)champ[team] + pt;
						champ[team] = pts;
					}
					else
					{
						champ[team] = pt;
					}

					if (i==1)
					{
						pointsR1.SetValue(pt, val);
						val++;
					}
					else if (i==2)
					{
						pointsR2.SetValue(pt, val);
						val++;
					}
					else if (i==3)
					{
						pointsR3.SetValue(pt, val);
						val++;
					}
					else if (i==4)
					{
						pointsR4.SetValue(pt, val);
						val++;
					}
					else if (i==5)
					{
						pointsR5.SetValue(pt, val);
						val++;
					}
				}

				val =0;
				foreach (string team in teamnames)
				{
					champPoints.SetValue((int)champ[team], val);
					val++;
				}
			}
			
			if (File.Exists(reportfilename))
			{
				try
				{
					System.Diagnostics.Process.Start(reportfilename);		
				}
				catch (Exception evc)
				{
					if (evc.Message.IndexOf("No Application")>-1)
					{
						MessageBox.Show(_gamePanel.TopLevelControl, "Cannot present PDF Summary Sheet ", "No PDF Reader Application Installed"
							,MessageBoxButtons.OK,MessageBoxIcon.Error);
					}
					else
					{
						MessageBox.Show(_gamePanel.TopLevelControl, "Cannot present PDF Summary Sheet ", "Failed to Start PDF Reader."
							,MessageBoxButtons.OK,MessageBoxIcon.Error);
					}
				}
			}
		}

		protected void GetTitleandVenue(ref string title, ref string venue)
		{
			string file = _gameFile.Dir + "\\global\\details.xml";
			if(File.Exists(file))
			{
				XmlDocument xdoc = new XmlDocument();
				xdoc.Load(file);
				
				title   = CoreUtils.XMLUtils.GetElementString(xdoc.DocumentElement, "Title");
				venue   = CoreUtils.XMLUtils.GetElementString(xdoc.DocumentElement, "Venue");
			}
		}

		protected void GetTeamMembersandRoles(string[] Members, string[] Roles)
		{
			string file = _gameFile.Dir + "\\global\\team.xml";
			if(File.Exists(file))
			{
				XmlDocument xdoc = new XmlDocument();
				xdoc.Load(file);
				//
				XmlNode members = xdoc.DocumentElement.SelectSingleNode("members");
				int count = 0;
				foreach(XmlNode n in members.ChildNodes)
				{
					if( (n.NodeType == XmlNodeType.Element) && (n.Name == "member") )
					{
						//Only handling 20 members in the array (we only have 20 slots on the report)
						//refactor array based system to remove internal restriction 
						// and make 20 limit a skin variable as other apps may have more space
						if (count < Members.Length)
						{
							Members.SetValue(CoreUtils.XMLUtils.GetElementString(n, "name"), count);
							Roles.SetValue(CoreUtils.XMLUtils.GetElementString(n, "role"), count);
							count++;
						}
					}
				}
			}
		}


	}
}
