using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Algorithms;
using CommonGUI;
using LibCore;
using Network;
using CoreUtils;

namespace TransitionScreens
{

	/// <summary>
	/// Summary description for WorkScheduleViewer.
	/// This could do with a refactor to use a Hashtable 
	/// which relates the name provided in the event to a Image read from the directory. 
	/// </summary>
	public class WorkScheduleViewer : BasePanel
	{
		protected const string AttrName_Name = "name";
		protected const string AttrName_Type = "type";
		protected const string RequiredNodeName_Calendar = "Calendar";
		protected const string AttrName_Day = "day";
		protected const string AttrName_CurrentDay = "CurrentDay";

		bool transparentBackground = (SkinningDefs.TheInstance.GetIntData("transition_panels_transparent_edges", 0) == 1);

		Image img_cross = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\calendar\\cross.png");

		Image img_trade = null;
		Image img_freeze = null;
		Image img_dataprep = null;

		Image img_race = null;
		Image img_press = null;
		Image img_test = null;
		Image img_qual = null;

		Image ug_memory_due = null;
		Image ug_hardware_due = null;
		Image ug_storage_due = null;
		Image ug_app_due = null;
		Image install_due = null;

		Image ug_memory_done = null;
		Image ug_hardware_done = null;
		Image ug_storage_done = null;
		Image ug_app_done = null;
		Image install_done = null;

		Image ug_memory_error = null;
		Image ug_hardware_error = null;
		Image ug_storage_error = null;
		Image ug_app_error = null;
		Image install_error = null; 

		/// <summary> 
		/// Required designer variable.
		/// </summary>
		protected System.ComponentModel.Container components = null;
		protected Font displayfont = SkinningDefs.TheInstance.GetFont(8f);
		protected Font titleFont = SkinningDefs.TheInstance.GetFont( SkinningDefs.TheInstance.GetFloatData("transition_calendar_title_font_size", 7.5f), FontStyle.Bold);

		protected NodeTree _NetworkModel;
		protected Node CalendarNode = null;
		protected Node CurrentDayNode =  null;

		//presentation varibles 
		protected int offset = 0;
		protected int calendarWidth = 0;
		protected int calendarHeight = 0;
		
		protected int calendarOffsetX = 14;//5;

		protected int calendarOffsetTopY = 32;
		public int CalendarOffsetTopY
		{
			get
			{
				return calendarOffsetTopY;
			}

			set
			{
				calendarOffsetTopY = value;
				DoSize();
			}
		}

		protected int calendarOffsetBottomY = 32;
		public int CalendarOffsetBottomY
		{
			get
			{
				return calendarOffsetBottomY;
			}

			set
			{
				calendarOffsetBottomY = value;
				DoSize();
			}
		}

		protected int icon_text_y_offset = 0;

		protected int DayCellWidth = 0;
		protected int DayCellHeight = 0;

		//Standard Calendar Format Defintions
		protected int calendarLength = 30;
		protected int calendarRows = 6;
		protected Hashtable CalendarEventNodes = new Hashtable();
		protected int DayCount = 1;
		//protected Font MyDefaultSkinFontNormal8;

		protected bool showCurrentDay = true;

		protected Brush brush;

		protected bool showTitleBar = false;
		protected int titleBarHeight;
		protected Color titleBarColour;
		protected string titleBarString;

		public Brush brushDefaultText;
	    public Brush day_text_brush;
        public Brush day_desc_brush;
		
		public Color box_colour = Color.DarkGray;
		public Color day_box_colour = Color.Gray;

		protected int ScheduleDayTaskTextOffsetY = 0;

		bool auto_translate = true;
		bool SelfDrawTranslatedTitle = false;

		IWatermarker watermarker;

		public IWatermarker Watermarker
		{
			get => watermarker;

			set
			{
				watermarker = value;
				Invalidate();
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="nt"></param>
		public WorkScheduleViewer(NodeTree nt)
		{
			var fontname =  SkinningDefs.TheInstance.GetData("fontname");
			//MyDefaultSkinFontNormal8 = ConstantSizeFont.NewFont(fontname,8f);
			//displayfont = MyDefaultSkinFontNormal8;
			displayfont = ConstantSizeFont.NewFont(fontname,8f);

			if (auto_translate)
			{
				displayfont.Dispose();
				displayfont = ConstantSizeFont.NewFont(TextTranslator.TheInstance.GetTranslateFont(fontname), 8f);
				titleFont.Dispose();
				titleFont = ConstantSizeFont.NewFont(TextTranslator.TheInstance.GetTranslateFont(fontname), SkinningDefs.TheInstance.GetFloatData("transition_calendar_title_font_size", 10), FontStyle.Bold);
			}

		    brushDefaultText = new SolidBrush (SkinningDefs.TheInstance.GetColorDataGivenDefault("transition_calendar_text_colour", SkinningDefs.TheInstance.GetColorDataGivenDefault("transition_text_colour", Color.Black)));
		    day_text_brush = new SolidBrush (SkinningDefs.TheInstance.GetColorDataGivenDefault("transition_calendar_text_colour", SkinningDefs.TheInstance.GetColorDataGivenDefault("transition_text_colour", Color.Black)));
		    day_desc_brush = new SolidBrush (SkinningDefs.TheInstance.GetColorDataGivenDefault("transition_calendar_text_colour", SkinningDefs.TheInstance.GetColorDataGivenDefault("transition_text_colour", Color.Gray)));

			ScheduleDayTaskTextOffsetY =  SkinningDefs.TheInstance.GetIntData("ScheduleDayTaskTextOffsetY",0);

			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			//this.BackColor = MyCommonBackColor;
			brush = new SolidBrush( BackColor );
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.DoubleBuffer,true);
			SetStyle(ControlStyles.UserPaint, true);

			BuildIconImages();

			// TODO: Add any initialization after the InitializeComponent call
			//this.Paint += new System.Windows.Forms.PaintEventHandler(this.WorkScheduleViewer_Paint);

			_NetworkModel = nt;

			//Connect Up the Calendar Node
			CalendarNode = _NetworkModel.GetNamedNode(RequiredNodeName_Calendar);

			RefreshCalendarProperties();
			//
			foreach(Node n in CalendarNode)
			{
				var cday = n.GetAttribute(AttrName_Day);
				if (cday != null)
				{
					//We only show items which are blocking 
					var block = n.GetAttribute("block");
					if (block.ToLower() == "true")
					{	
						CalendarEventNodes.Add(cday, n);
						n.AttributesChanged += n_AttributesChanged;
					}
				}
			}
			
			CalendarNode.AttributesChanged +=OpsEventsNode_AttributesChanged;
			CalendarNode.ChildAdded +=OpsEventsNode_ChildAdded;
			CalendarNode.ChildRemoved +=OpsEventsNode_ChildRemoved;
			CalendarNode.Deleting +=OpsEventsNode_Deleting;
			
			//Connect Up the Current Day Node
			CurrentDayNode = _NetworkModel.GetNamedNode(AttrName_CurrentDay);
			if (CurrentDayNode != null)
			{
				DayCount = CurrentDayNode.GetIntAttribute(AttrName_Day,0);
				CurrentDayNode.AttributesChanged += CurrentDayNode_AttributesChanged;
			}

			Resize += WorkScheduleViewer_Resize;

			var backgroundColour = SkinningDefs.TheInstance.GetData("work_schedule_viewer_background_color");
			if (backgroundColour != "")
			{
				BackColor = CONVERT.ParseComponentColor(backgroundColour);
			}
		}

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
			    brushDefaultText.Dispose();
                day_text_brush.Dispose();
                day_desc_brush.Dispose();

			    components?.Dispose();

			    Reset();

				if (CurrentDayNode != null)
				{
					CurrentDayNode.AttributesChanged -= CurrentDayNode_AttributesChanged;
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
			components = new System.ComponentModel.Container();
			this.BackColor = Color.Transparent;
			this.Size = new Size(429,420);
			this.BackgroundImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + 
				"\\images\\panels\\ows.png");
		}
		#endregion

		public void EnableSelfDrawTitle(bool newState)
		{
			SelfDrawTranslatedTitle = newState;
		}

		public void BuildIconImages()
		{
			//Common icons (always used)
			ug_memory_due = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\calendar\\memory_due.png");
			ug_hardware_due = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\calendar\\hardware_due.png");
			ug_storage_due = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\calendar\\storage_due.png");
			ug_app_due = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\calendar\\app_due.png");
			install_due = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\calendar\\install_due.png");

			ug_memory_done = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\calendar\\memory_done.png");
			ug_hardware_done = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\calendar\\hardware_done.png");
			ug_storage_done = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\calendar\\storage_done.png");
			ug_app_done = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\calendar\\app_done.png");
			install_done = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\calendar\\install_done.png");

			ug_memory_error = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\calendar\\memory_error.png");
			ug_hardware_error = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\calendar\\hardware_error.png");
			ug_storage_error = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\calendar\\storage_error.png");
			ug_app_error = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\calendar\\app_error.png");
			install_error = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\calendar\\install_error.png");

			img_press = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\calendar\\press.png");

			// : Fix for 3662 (Reckitt doesn't show correct icons for days).
			// The icon-loading code switched on the skin name, and had no case for RB.
			// Instead, we just try to load everything, relying on coping gracefully with
			// ones that aren't loaded.

			// These are used by just about everyone.
			img_trade = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\calendar\\trade.png");
			img_freeze = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\calendar\\freeze.png");
			img_dataprep= Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\calendar\\data_prep.png");

			// These are used by HP.
			img_race = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\calendar\\race.png");
			img_test = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\calendar\\test.png");
			img_qual = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\calendar\\qual.png");					
		}

		public virtual void SetTrainingMode(Boolean Tr)
		{
			if (Tr)
			{
				BackgroundImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + 
					"\\images\\panels\\t_ows.png");
			}
			else
			{
				BackgroundImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + 
					"\\images\\panels\\ows.png");
			}
		}

		void OpsEventsNode_ChildAdded(Node sender, Node child)
		{
			if (child != null)
			{
				var cday = child.GetAttribute(AttrName_Day);
				if (cday != null)
				{
					//We only show items which are blocking 
					var block = child.GetAttribute("block");
					if (block.ToLower() == "true")
					{
						CalendarEventNodes.Add(cday, child);
						CurrentDayNode.AttributesChanged += CurrentDayNode_AttributesChanged;
						//do we need to attach to the deleting event
						//Refresh();
						Invalidate();
					}
				}
			}
		}

		void OpsEventsNode_ChildRemoved(Node sender, Node child)
		{
			if (child != null)
			{
				var cday = child.GetAttribute(AttrName_Day);
				if (cday != null)
				{
					CalendarEventNodes.Remove(cday);
					child.AttributesChanged -= CurrentDayNode_AttributesChanged;
					//Refresh();
					Invalidate();
				}
			}
		}

		void SetCalendarLength(int length, Boolean displayRefresh)
		{
			calendarLength = length;
			var RandomClass = new Random();
			//int offset = RandomClass.Next(0, (35 - calendarLength));
			var offset = RandomClass.Next(0, 5);
			if (displayRefresh)
			{
				DoSize();
				//Refresh();
				Invalidate();
			}
		}

		void RefreshCalendarProperties ()
		{
			calendarLength = CalendarNode.GetIntAttribute("days", 0);

			// The calendar might override how many days we bother showing.
			calendarLength = CalendarNode.GetIntAttribute("showdays", calendarLength);

			offset = CalendarNode.GetIntAttribute("offset",0);
		}

		void OpsEventsNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			RefreshCalendarProperties();
		}

		void OpsEventsNode_Deleting(Node sender)
		{
			Reset();
			//Refresh();
			Invalidate();
		}

		/// <summary>
		/// 
		/// </summary>
		public void Reset()
		{
			calendarRows = 6;
			foreach(Node n in CalendarEventNodes.Values)
			{
				n.AttributesChanged -= CurrentDayNode_AttributesChanged;
			}
			CalendarEventNodes.Clear();
		}

		void DoSize()
		{
			calendarWidth = Width;
			calendarHeight = Height;
			DayCellWidth = (calendarWidth-calendarOffsetX*2) / calendar_columns;
			DayCellHeight = (calendarHeight-(calendarOffsetTopY + calendarOffsetBottomY)) / calendar_rows;
			Invalidate();
		}

		Color DetermineWhichColor(string status)
		{
			if(status == "completed_fail")
				return Color.DarkRed;

			return Color.Transparent;
		}

		Brush DetermineWhichTextBrush (string status)
		{
			if (status == "completed_fail")
			{
				return Brushes.White;
			}

			return brushDefaultText;
		}

		Image CheckNotNull(Image SuppliedImage)
		{
			if (SuppliedImage != null)
			{
				return SuppliedImage;
			}
			return img_cross;
		}

		Image DetermineWhichImage(string type, string status, string option, string name)
		{
			Image img = null;
			switch (type)	
			{
				case "Install":

					switch (status)
					{
						case "active":			img = CheckNotNull(install_due);	break;
						case "completed_ok":	img = CheckNotNull(install_done);	break;
						case "completed_fail":	img = CheckNotNull(install_error);break;
					}break;

				case "server_upgrade":
					if (option == "hardware")
					{
						switch (status)
						{
							case "active":			img = CheckNotNull(ug_hardware_due);	break;
							case "completed_ok":	img = CheckNotNull(ug_hardware_done);	break;
							case "completed_fail":	img = CheckNotNull(ug_hardware_error);break;
						}
					}
					if (option == "memory")	//server memory upgrade
					{
							switch (status)
							{
								case "active":			img = CheckNotNull(ug_memory_due);  break;
								case "completed_ok":	img = CheckNotNull(ug_memory_done); break;
								case "completed_fail":	img = CheckNotNull(ug_memory_error); break;
							}
					}
					if (option == "storage")	//server memory upgrade
					{
						switch (status)
						{
							case "active":			img = CheckNotNull(ug_storage_due);	break;
							case "completed_ok":	img = CheckNotNull(ug_storage_done);	break;
							case "completed_fail":	img = CheckNotNull(ug_storage_error);	break;
						}
					}
					break;

				case "app_upgrade":
				switch (status)
				{
					case "active":			img = CheckNotNull(ug_app_due);	break;
					case "completed_ok":	img = CheckNotNull(ug_app_done);	break;
					case "completed_fail":	img = CheckNotNull(ug_app_error);	break;
				}break;

				case "external":
				switch (name)
				{
					case "Data Prep":		img = CheckNotNull(img_dataprep); break;
					case "Freeze":			img = CheckNotNull(img_freeze); break;
					case "Trading":			img = CheckNotNull(img_trade); break;
					case "Shipping":		img = CheckNotNull(img_trade); break;
						//
					case "Race":		img = CheckNotNull(img_race);	break;
					case "Press":		img = CheckNotNull(img_press);break;
					case "Testing":		img = CheckNotNull(img_test);break;
					case "Qualify":	
					case "Qualifying":	img = CheckNotNull(img_qual);break;
				}
					break;
			}
			return img; 
		}

		protected int calendar_columns = 5;
		protected int calendar_rows = 7;

		public void SetCalendarRowsAndCols(int rows, int cols)
		{
			calendar_columns = cols;
			calendar_rows = rows;

			DoSize();
		}

		public int cell_border_x = 3;
		public int cell_border_y = 3;

		public int icon_y_offset = 0;

		public Brush text_brush = new SolidBrush (SkinningDefs.TheInstance.GetColorDataGivenDefault("transition_title_colour", SkinningDefs.TheInstance.GetColorDataGivenDefault("transition_text_colour", Color.DarkBlue)));

		/// <summary>
		/// Override paint ...
		/// </summary>
		/// <param name="e"></param>
		protected override void OnPaint(PaintEventArgs e)
		{
			var g = e.Graphics;

			if (transparentBackground)
			{
				g.Clear(BackColor);
			}

			if(null != BackgroundImage)
			{
				g.DrawImageUnscaled(BackgroundImage,0,0,BackgroundImage.Width,BackgroundImage.Height);
			}
			g.SmoothingMode = SmoothingMode.HighQuality;

			watermarker?.Draw(this, e.Graphics);

			if (SelfDrawTranslatedTitle)
			{
				var title_text = "Change Schedule";
				if (auto_translate)
				{
					title_text = TextTranslator.TheInstance.Translate(title_text);

                    TransitionScreen.DrawSectionTitle(this, g, title_text, titleFont, text_brush, new Point (10, 2));
				}
			}
			
			var typeColor = Color.White;
			var target = "";
			var name = "";
			var type = "";
			var product = "";
			var status = "";
			Brush DayTextBrush = null;
			Brush DescTextBrush = null;
		
			var DayStepper = 0;
			var step = 0;
			var boxX = 0;
			var boxY = 0;
			var boxY_bottom =0;
			var option = string.Empty;
			Image img = null;
		
			for (var step2 = 0; step2 < calendar_rows; step2++)
			{
				for (var step1 = 0; step1 < calendar_columns; step1++)
				{
					boxX = step1*DayCellWidth+calendarOffsetX;
					boxY = step2*DayCellHeight+calendarOffsetTopY;
					boxY_bottom = (step2+1)*DayCellHeight+calendarOffsetTopY;

					if(step++ >= offset)
					{
						DayStepper++;
					}

					// Not a real day on the calendar
					if(DayStepper == 0 || DayStepper > calendarLength)
					{
						//not a real day so just a empty box
						e.Graphics.DrawRectangle(new Pen(box_colour, 1), boxX, boxY,DayCellWidth-cell_border_x, DayCellHeight-cell_border_y);
					}
					else
					{
						//its a real day so start with the background box
						e.Graphics.DrawRectangle(new Pen(day_box_colour, 1), boxX, boxY, DayCellWidth - cell_border_x, DayCellHeight - cell_border_y);

						// If this today, then we need to change the background color
						if ((DayStepper == DayCount) && showCurrentDay)
						{
							e.Graphics.FillRectangle(Brushes.SteelBlue, boxX+1, boxY+1,DayCellWidth-cell_border_x-2, DayCellHeight-cell_border_y-2);
							DayTextBrush = Brushes.White;
							DescTextBrush = Brushes.White;
						}
						else
						{
							DayTextBrush = day_text_brush; // Brushes.Black;
							DescTextBrush = day_desc_brush; // Brushes.Gray;
						}

						//Draw the Event If needed 
						if (CalendarEventNodes.ContainsKey( CONVERT.ToStr(DayStepper)))
						{
							var n1 = (Node)CalendarEventNodes[DayStepper.ToString()];
							if (n1 != null)
							{
								name = n1.GetAttribute("showName");
								type = n1.GetAttribute("type");
								product = n1.GetAttribute("productid");
								status = n1.GetAttribute("status");
								target = n1.GetAttribute("target");

								var image = n1.GetAttribute("image");

								if (type=="server_upgrade")
								{
									option = n1.GetAttribute("upgrade_option");
								}

								if(image != "")
								{
									img = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\calendar\\" + image);
								}
								else
								{
									img = DetermineWhichImage(type, status, option, name);
								}

								var c = DetermineWhichColor(status);

								// If it's not a highlighted day, might need to change colour.
								if (! ((DayStepper == DayCount) && showCurrentDay))
								{
									DescTextBrush = DetermineWhichTextBrush(status);
								}

								name = n1.GetAttribute("showName");
			
								e.Graphics.FillRectangle(new SolidBrush(c), boxX+1, boxY+1,DayCellWidth-cell_border_x-2, DayCellHeight-cell_border_y-2);
                                
							    var textOffset = Math.Min(15, DayCellHeight * 0.3f);
							    var remainingHeight = DayCellHeight - textOffset;
							    var imageAreaBounds = new RectangleF(boxX + 1 + (DayCellWidth - remainingHeight) / 2,
							        boxY + 1 , remainingHeight, remainingHeight);
							    
							    var imageHeight = (remainingHeight * 0.8f);

								e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                                e.Graphics.DrawImage(img, imageAreaBounds.CentreSubRectangle(imageHeight, imageHeight));

								// Rectangle to wrap the text to.
								
								var rect = new RectangleF (boxX + 2, boxY_bottom - 15, 
									DayCellWidth - cell_border_x - 2, DayCellHeight - cell_border_y - 2 - 35 - icon_y_offset - icon_text_y_offset);

								var format = new StringFormat ();

								if(type == "external")
								{
									var newName = name;
									if (auto_translate)
									{
										newName = TextTranslator.TheInstance.Translate(name);
									}

									var size = g.MeasureString(newName, displayfont);
									if (size.Width > rect.Width)
									{
										rect.Y = rect.Y - size.Height;
									}
									if (ScheduleDayTaskTextOffsetY!=0)
									{
										rect.Y = rect.Y + ScheduleDayTaskTextOffsetY;
									}
									e.Graphics.DrawString(newName, displayfont, DescTextBrush, rect, format);
								}
								else if(type == "Install")
								{
									var installlocation = n1.GetAttribute("location");
									var installProjectDisplayStr = string.Empty;

									installProjectDisplayStr = product;
									if (installlocation.Length > 0)
									{
										installProjectDisplayStr += " - " + installlocation.ToUpper();
									}
									
									var size = g.MeasureString(installProjectDisplayStr, displayfont);
									if (size.Width > rect.Width)
									{
										rect.Y = rect.Y - size.Height;
									}
									if (ScheduleDayTaskTextOffsetY!=0)
									{
										rect.Y = rect.Y + ScheduleDayTaskTextOffsetY;
									}

									e.Graphics.DrawString(installProjectDisplayStr, displayfont, DescTextBrush, rect, format);
								}
								else if(type == "server_upgrade" || type == "app_upgrade")
								{
									var size = g.MeasureString(target, displayfont);
									if (size.Width > rect.Width)
									{
										rect.Y = rect.Y - size.Height;
										rect.Height += size.Height;
									}
									if (ScheduleDayTaskTextOffsetY!=0)
									{
										rect.Y = rect.Y + ScheduleDayTaskTextOffsetY;
									}

									e.Graphics.DrawString(target, displayfont, DescTextBrush, rect, format);
								}
							}
						}
						//Draw the Day number above any Day stuff 
						e.Graphics.DrawString(CONVERT.ToStr(DayStepper), displayfont, DayTextBrush, boxX+2, boxY+2);
					}
				}
			}

			if (showTitleBar)
			{
				Brush filledBrush = new SolidBrush(titleBarColour);
				Brush textBrush = new SolidBrush(Color.FromArgb(255, 255, 255));

				e.Graphics.FillRectangle(filledBrush, 0, 0, calendarLength * DayCellWidth, titleBarHeight);

				var rect = new RectangleF (0, 0, Width, titleBarHeight);
				var format = new StringFormat();
				format.Alignment = StringAlignment.Center;
				e.Graphics.DrawString(titleBarString, titleFont, textBrush, rect, format);		
			}
		}

		void CurrentDayNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			if (attrs != null)
			{
				if (attrs.Count > 0)
				{
					DayCount = sender.GetIntAttribute(AttrName_Day,0);
					//Refresh();
					Invalidate();
				}
			}
		}

		void n_AttributesChanged(Node sender, ArrayList attrs)
		{
			//Refresh();
			Invalidate();
		}

		void WorkScheduleViewer_Resize(object sender, EventArgs e)
		{
			DoSize();
		}

		public void EnableCurrentDayHighlight (bool enable)
		{
			showCurrentDay = enable;
		}

		public void SetDaysAndWidthsForSingleRowView (int Days, int DayWidth)
		{
			calendarLength = Days;
			DayCellWidth = DayWidth;
			DayCellHeight = Height - 2;
			calendarOffsetX = 0;
			calendarOffsetTopY = 0;
			calendarOffsetBottomY = 0;
			cell_border_x = 0;
			cell_border_y = 0;
			icon_y_offset = 15;
		}

		public void EnableTitleBar (int height, string text, Color colour)
		{
			showTitleBar = true;
			titleBarHeight = height;
			titleBarString = text;
			titleBarColour = colour;

			calendarOffsetTopY = height;
			calendarOffsetBottomY = height;
			DayCellHeight -= height;
		}

		public void SetBookedDayTextVerticalOffset (int offset)
		{
			icon_text_y_offset = offset;
		}
	}
}