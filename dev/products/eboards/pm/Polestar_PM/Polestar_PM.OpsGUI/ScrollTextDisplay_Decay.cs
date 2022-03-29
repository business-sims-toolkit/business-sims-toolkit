using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Data;
using System.Windows.Forms;

namespace ScrollTestCntrls
{

	public class FlickerFreePanel : Panel
	{
		public FlickerFreePanel()
		{
			this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			this.SetStyle(ControlStyles.UserPaint, true);
			this.SetStyle(ControlStyles.DoubleBuffer,true);
		}
	}


	public class DisplayLine
	{
		public string text;
		public Color normal_color = Color.Silver;
		public Color high_color = Color.White;
		public bool flash_status = false;
		public int flash_count = 20;
	}

	/// <summary>
	/// New Version with internal Timer for auto scrolling and arraylist data 
	/// </summary>
	public class AutoScrollTextDisplay : FlickerFreePanel
	{
		protected int cycle_drop_y = 2;
		protected int offset_y = 0;
		protected int flash_count_start = 20;
		protected int DecayCounter = 0;

		protected Hashtable displaylines_ObjtoID = new Hashtable();
		protected Hashtable displaylines_IDtoObj = new Hashtable();
		protected ArrayList display_key_list = new ArrayList();

		//private ArrayList MyStrs = new ArrayList();
		protected System.Windows.Forms.Timer timer1;
		protected Font MyDefaultSkinFontBold8 = null;
		protected Font MyDefaultSkinFontBold10 = null;
		protected Font MyDefaultSkinFontBold14 = null;
		protected int NumberofSlots = 10;
		protected Bitmap bmp = null;
		protected Graphics g = null;
		protected int offscreenheight=2;
		protected Hashtable currentColors = new Hashtable();
		protected int subnumber =0;
		protected bool MyIsTrainingMode = false;
		protected bool scrolling = false;

		public AutoScrollTextDisplay(Boolean IsTrainingMode)
		{
			string fontname =  "Verdana";
			MyIsTrainingMode = IsTrainingMode;

			//fontname =  SkinningDefs.TheInstance.GetData("fontname");
			fontname = "Verdana";
			MyDefaultSkinFontBold8 = new Font(fontname,8f,FontStyle.Bold);
			MyDefaultSkinFontBold10 = new Font(fontname,10f,FontStyle.Bold);
			MyDefaultSkinFontBold14 = new Font(fontname,14f,FontStyle.Bold);
				
			this.timer1 = new System.Windows.Forms.Timer();
			this.timer1.Interval = 100;
			this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
			this.timer1.Enabled = false;

			BuildOffScreenBuffer();
			this.Resize += new EventHandler(AutoScrollTextDisplay_Resize);
			DecayCounter =0;
			this.timer1.Enabled = true;
		}

		private void setTestData()
		{
			//MyStrs.Add("First Phase");
			//MyStrs.Add("Second Phase");
			//MyStrs.Add("Third Phase");
			//MyStrs.Add("Fourth Phase");
			//MyStrs.Add("Last Phase");
		}

		private void BuildColors()
		{
			currentColors.Clear();
			currentColors.Add("green", Color.Green);
			currentColors.Add("red", Color.Red);
			currentColors.Add("orange", Color.Orange);
			currentColors.Add("white", Color.White);
		}

		public Color getColor(string tc)
		{
			Color c = Color.White;
			if (currentColors.Contains(tc))
			{
				c = (Color)currentColors[tc];
			}
			return c;
		}

		private void AutoScrollTextDisplay_Resize(object sender, EventArgs e)
		{
			BuildOffScreenBuffer();
		}

		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if (MyDefaultSkinFontBold8 != null)
				{
					MyDefaultSkinFontBold8.Dispose();
					MyDefaultSkinFontBold8 = null;
				}
				if (MyDefaultSkinFontBold10 != null)
				{
					MyDefaultSkinFontBold10.Dispose();
					MyDefaultSkinFontBold10 = null;
				}
				if (MyDefaultSkinFontBold14 != null)
				{
					MyDefaultSkinFontBold14.Dispose();
					MyDefaultSkinFontBold14 = null;
				}
				if (timer1.Enabled)
				{
					timer1.Enabled = false;
				}
				g.Dispose();
				bmp.Dispose();
				this.timer1.Dispose();
			}
			base.Dispose( disposing );
		}

		public void ClearTextLines()
		{
			subnumber=0;
			displaylines_ObjtoID.Clear();
			displaylines_IDtoObj.Clear();
			display_key_list.Clear();
		}

		public void AddDisplayLine(int disp_order, DisplayLine tmpDL)
		{
			tmpDL.flash_status = true;
			tmpDL.flash_count = flash_count_start;
			int uniqueID = disp_order*1000+subnumber;
			displaylines_ObjtoID.Add(tmpDL, uniqueID);
			displaylines_IDtoObj.Add(uniqueID, tmpDL);
			display_key_list.Add(uniqueID);
			subnumber++;
			if (display_key_list.Count>0)
			{
				display_key_list.Sort();
			}
			//this.MyStrs.Add(tmpDL);
			BuildOffScreenBuffer();
			this.Refresh();
		}

		public int getDisplayLineCount()
		{
			return displaylines_ObjtoID.Count;
		}

		public bool isDisplayLineListed(DisplayLine tmpDL)
		{
			bool exists = false;
			if (displaylines_ObjtoID.Contains(tmpDL))
			{
				exists = true;
			}
			return exists;
		}


		public void RemoveDisplayLine(DisplayLine tmpDL)
		{
			if (displaylines_ObjtoID.Contains(tmpDL))
			{
				int uniqueID = (int) displaylines_ObjtoID[tmpDL];

				displaylines_ObjtoID.Remove(tmpDL);
				displaylines_IDtoObj.Remove(uniqueID);
				display_key_list.Remove(uniqueID);
				if (display_key_list.Count>0)
				{
					display_key_list.Sort();
				}
				//this.MyStrs.Add(tmpDL);
				BuildOffScreenBuffer();
				this.Refresh();
			}
		}

		private void DisposeOffScreenBufferObjs()
		{
			if (g != null)
			{
				g.Dispose();
				g = null;
			}
			if (bmp != null)
			{
				bmp.Dispose();
				bmp = null;
			}
		}

		public void BuildOffScreenBuffer()
		{
			bool switchtimerOff = false;
			bool switchtimerOn = true;

			DisposeOffScreenBufferObjs();
			//Resize 
			bmp = new Bitmap(this.Width, this.Height*2);
			g = Graphics.FromImage(bmp);

			StringFormat sf = new StringFormat(StringFormatFlags.DirectionRightToLeft);
			SizeF text_size = g.MeasureString("ALERT",MyDefaultSkinFontBold10,this.Width,sf);
			int singleSlotHeight = (int) text_size.Height + 2;
			NumberofSlots = this.Height / singleSlotHeight;

			if (NumberofSlots>=display_key_list.Count)
			{
				//We have spare slots so no need to scroll			
				offscreenheight = this.Height*2;
				//new
				scrolling = false;
				offset_y=0;
				//switchtimerOff = true;
				//switchtimerOn = false;
			}
			else
			{
				//We need to have a bigger off screen buffer
				offscreenheight = (display_key_list.Count+1)*2*singleSlotHeight;
				bmp = new Bitmap(this.Width, offscreenheight);
				g = Graphics.FromImage(bmp);
				scrolling = true;
				//switchtimerOff = false;
				//switchtimerOn = true;
			}

			//make sure we are drawing to the best quality
			g.SmoothingMode = SmoothingMode.AntiAlias;
			g.CompositingQuality = CompositingQuality.HighQuality;
			g.PixelOffsetMode = PixelOffsetMode.HighQuality;					
			//Redraw
			g.FillRectangle(Brushes.Black, 0, 0, this.Width, offscreenheight);
			int y_pos=0;


			if (display_key_list.Count>0)
			{
				foreach (int key_number in display_key_list)
				{
					if (displaylines_IDtoObj.Contains(key_number))
					{
						DisplayLine displine = (DisplayLine) displaylines_IDtoObj[key_number];
						Brush tmpNormBrush = new SolidBrush(displine.normal_color);
						Brush tmpHighBrush = new SolidBrush(displine.high_color);
						int count_tens = displine.flash_count;
						if (count_tens>0)
						{
							if ( count_tens % 2 == 0)
							{
								g.DrawString(displine.text +count_tens.ToString(), MyDefaultSkinFontBold10, tmpNormBrush, 0, y_pos);
								g.DrawString(displine.text +count_tens.ToString(), MyDefaultSkinFontBold10, tmpNormBrush, 0, y_pos+(offscreenheight/2));
								System.Diagnostics.Debug.WriteLine("## "+displine.text+ " N "+count_tens.ToString());
							}
							else
							{
								g.DrawString(displine.text+count_tens.ToString(), MyDefaultSkinFontBold10, tmpHighBrush, 0, y_pos);
								g.DrawString(displine.text+count_tens.ToString(), MyDefaultSkinFontBold10, tmpHighBrush, 0, y_pos+(offscreenheight/2));
								System.Diagnostics.Debug.WriteLine("## "+displine.text+ " H "+count_tens.ToString());
							}
						}
						else
						{
							g.DrawString(displine.text+count_tens.ToString(), MyDefaultSkinFontBold10, tmpNormBrush, 0, y_pos);
							g.DrawString(displine.text+count_tens.ToString(), MyDefaultSkinFontBold10, tmpNormBrush, 0, y_pos+(offscreenheight/2));
							System.Diagnostics.Debug.WriteLine("## "+displine.text+ " D "+count_tens.ToString());
						}
						y_pos += singleSlotHeight;
						tmpNormBrush.Dispose();
						tmpHighBrush.Dispose();
					}
				}
			}

//			foreach (string st in MyStrs)
//			{
//				g.DrawString(st,MyDefaultSkinFontBold14, Brushes.White,0,y_pos);
//				g.DrawString(st,MyDefaultSkinFontBold14, Brushes.White,0,y_pos+(offscreenheight/2));
//				y_pos+=singleSlotHeight;
//			}

			//IJM update
//			if (this.timer1.Enabled)
//			{
//				if (switchtimerOff)
//				{
//					this.StopTimer();
//					offset_y=0;
//				}
//			}
//			else
//			{
//				if (switchtimerOn)
//				{
//					this.StartTimer();
//				}
//			}
			//IJM Off
		}

		private void StartTimer()
		{
			this.timer1.Enabled = true;
		}

		private void StopTimer()
		{
			this.timer1.Enabled = false;
		}

		private bool ageDL()
		{
			bool any_item_aged = false;

			foreach (DisplayLine dl in displaylines_ObjtoID.Keys)
			{
				if (dl.flash_count>0)
				{
					dl.flash_count--;
					System.Diagnostics.Debug.WriteLine(dl.text+ "  "+ dl.flash_count.ToString());
					any_item_aged = true;
				}
			}
			return any_item_aged;
		}

		private void timer1_Tick(object sender, System.EventArgs e)
		{
			if (scrolling)
			{
				ScrollVertical();
			}
			System.Diagnostics.Debug.WriteLine("DecayCounter "+DecayCounter.ToString());
			if (DecayCounter % 5 == 0)
			{			
				System.Diagnostics.Debug.WriteLine("DecayCounter TEN");
				if (ageDL())
				{
					System.Diagnostics.Debug.WriteLine("DecayCounter BUILD");
					BuildOffScreenBuffer();
					Refresh();
				}
			}
			DecayCounter++;
		}

		private void ScrollVertical()
		{
			offset_y = (offset_y + cycle_drop_y) % (offscreenheight/2);
			this.Refresh();
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			if (bmp != null)
			{
				Rectangle srcRect = new Rectangle(0,offset_y,this.Width,this.Height);
				Rectangle destRect = new Rectangle(0,0,this.Width,this.Height);
				e.Graphics.DrawImage(bmp,destRect,srcRect,System.Drawing.GraphicsUnit.Pixel);
			}
		}

	}
}

