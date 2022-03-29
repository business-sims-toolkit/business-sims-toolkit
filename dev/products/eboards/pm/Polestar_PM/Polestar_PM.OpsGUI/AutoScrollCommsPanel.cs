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

namespace Polestar_PM.OpsGUI
{
	/// <summary>
	/// Summary description for AutoScrollCommsPanel.
	/// </summary>
	public class AutoScrollCommsPanel : FlickerFreePanel
	{
		protected int cycle_drop_y = 1;
		protected int offset_y = 0;
		protected int flash_count_start = 20;
		protected int DecayCounter = 0;
		protected int sequence = 1;
		protected int timer_interval = 40; 

		protected Hashtable BoxesByObj = new Hashtable();
		protected Hashtable BoxesBySeq = new Hashtable();
		protected ArrayList display_key_list = new ArrayList();
		protected Image unknown_icon = null;

		//protected Hashtable displaylines_ObjtoID = new Hashtable();
		//protected Hashtable displaylines_IDtoObj = new Hashtable();
		//protected ArrayList display_key_list = new ArrayList();

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
		//protected Hashtable currentIcons = new Hashtable();
		protected int subnumber =0;
		protected bool MyIsTrainingMode = false;
		protected bool scrolling = false;
		protected SolidBrush backBrush = null;
		protected SolidBrush firstBrush = null;
		protected SolidBrush secondBrush = null;
		protected SolidBrush noteTextBrush = null;

		protected bool usefaces = true;

		protected bool ForceNoScroll = false;
		protected bool UseAlternateBackColors = true;
		protected Color firstAlternateBackNoteColor = Color.FromArgb(193,210,224);
		protected Color secondAlternateBackNoteColor = Color.FromArgb(208,215,221);

		public AutoScrollCommsPanel(Boolean IsTrainingMode)
		{
			string fontname = SkinningDefs.TheInstance.GetData("fontname");
			MyIsTrainingMode = IsTrainingMode;

			backBrush = new SolidBrush(Color.FromArgb(211,219,229));

			firstBrush = new SolidBrush(firstAlternateBackNoteColor);
			secondBrush = new SolidBrush(secondAlternateBackNoteColor);
			noteTextBrush = new SolidBrush(Color.FromArgb(51,51,51));

			unknown_icon = this.loadImage("unknown_msg.png");

			BuildColors();
			//LoadIcons();

			fontname =  SkinningDefs.TheInstance.GetData("fontname");
			MyDefaultSkinFontBold8 = ConstantSizeFont.NewFont(fontname,8f,FontStyle.Bold);
			MyDefaultSkinFontBold10 = ConstantSizeFont.NewFont(fontname, 10f, FontStyle.Bold);
			MyDefaultSkinFontBold14 = ConstantSizeFont.NewFont(fontname, 14f, FontStyle.Bold);
				
			this.timer1 = new System.Windows.Forms.Timer();
			this.timer1.Interval = timer_interval;
			this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
			this.timer1.Enabled = false;

			BuildOffScreenBuffer();
			this.Resize += new EventHandler(AutoScrollTextDisplay_Resize);
			DecayCounter =0;
			this.timer1.Enabled = true;
		}

		public Image loadImage(string imagename)
		{
			return Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "images\\icons\\"+imagename);
		}

		/*
		private void LoadIcons()
		{
			currentIcons.Clear();

			Image c1 = null;

			c1 = loadImage("comms1.png");
			currentIcons.Add("comms1",c1);


			if (usefaces)
			{
				//we use the PD icon for the any blocking day 
				c1 = loadImage("msg_face_pd.png");
				currentIcons.Add("block_msg",c1);

				c1 = loadImage("msg_face_pm.png");
				currentIcons.Add("prj_msg",c1);

				c1 = loadImage("msg_face_om.png");
				currentIcons.Add("ops_msg",c1);

				c1 = loadImage("msg_face_pd.png");
				currentIcons.Add("prg_msg",c1);
			}
			else
			{
				//we use the PD icon for the any blocking day 
				c1 = loadImage("msg_icon_pd.png");
				currentIcons.Add("block_msg",c1);
			}
		}*/


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
			if (currentColors.ContainsKey(tc))
			{
				c = (Color)currentColors[tc];
			}
			return c;
		}

		private void setUse_of_Alternaitive_Back_Colors(bool useAlts)
		{
			UseAlternateBackColors = useAlts;
		}

		private void setAlternativeNoteColors(Color firstColor, Color secondColor, Color textColor)
		{
			if (firstBrush != null)
			{
				firstBrush.Dispose();
			}
			if (secondBrush != null)
			{
				secondBrush.Dispose();
			}
			if (noteTextBrush != null)
			{
				noteTextBrush.Dispose();
			}

			firstAlternateBackNoteColor = firstColor;
			secondAlternateBackNoteColor = secondColor;
			firstBrush = new SolidBrush(firstAlternateBackNoteColor);
			secondBrush = new SolidBrush(secondAlternateBackNoteColor);
			noteTextBrush = new SolidBrush(textColor);
		}

		private void AutoScrollTextDisplay_Resize(object sender, EventArgs e)
		{
			BuildOffScreenBuffer();
		}

		protected override void Dispose( bool disposing )
		{
			ClearTextLines();

			if( disposing )
			{
				if (backBrush != null)
				{
					backBrush.Dispose();
				}
				if (firstBrush != null)
				{
					firstBrush.Dispose();
				}
				if (secondBrush != null)
				{
					secondBrush.Dispose();
				}
				if (noteTextBrush != null)
				{
					noteTextBrush.Dispose();
				}

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

			foreach (Node db1 in BoxesByObj.Keys)
			{
				//db1.update_event -=new ScrollTestCntrls.dataBox.updateEvent(db1_update_event);
				db1.AttributesChanged -=new Network.Node.AttributesChangedEventHandler(db1_AttributesChanged);
			}

			BoxesByObj.Clear();
			BoxesBySeq.Clear();
			display_key_list.Clear();
		}


		public void AddBox(Node db1)
		{
			if (BoxesByObj.ContainsKey(db1)==false)
			{
				BoxesByObj.Add(db1,sequence);
				BoxesBySeq.Add(sequence,db1);
				//db1.AttributesChanged.update_event +=new ScrollTestCntrls.dataBox.updateEvent(db1_update_event);
				db1.AttributesChanged +=new Network.Node.AttributesChangedEventHandler(db1_AttributesChanged);

				display_key_list.Add(sequence);
				sequence++;
				BuildOffScreenBuffer();
				this.Refresh();
			}
		}
		
		public void RemoveBox(Node db1)
		{
			if (BoxesByObj.ContainsKey(db1)==true)
			{
				int tmpseq = (int) BoxesByObj[db1];
				BoxesByObj.Remove(db1);
				//db1.update_event -=new ScrollTestCntrls.dataBox.updateEvent(db1_update_event);
				db1.AttributesChanged -=new Network.Node.AttributesChangedEventHandler(db1_AttributesChanged);
				
				BoxesBySeq.Remove(tmpseq);
				display_key_list.Remove(tmpseq);
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

		/*
		private Image getIconByName(string icon_name)
		{
			Image required_icon = unknown_icon;

			if (currentIcons.Contains(icon_name))
			{
				required_icon = (Image) currentIcons[icon_name];
			}
			return required_icon;
		}*/


		public void BuildOffScreenBuffer()
		{
			DisposeOffScreenBufferObjs();
			//Resize 
			bmp = new Bitmap(this.Width, this.Height*2);
			g = Graphics.FromImage(bmp);

			//we have a fixed size
			int singleSlotHeight = 18; // 36;
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
			
				if (ForceNoScroll == false)
				{
					scrolling = true;
				}
				//switchtimerOff = false;
				//switchtimerOn = true;
			}

			//make sure we are drawing to the best quality
			g.SmoothingMode = SmoothingMode.AntiAlias;
			g.CompositingQuality = CompositingQuality.HighQuality;
			g.PixelOffsetMode = PixelOffsetMode.HighQuality;					
			//Redraw
			g.FillRectangle(backBrush, 0, 0, this.Width, offscreenheight);

			int y_pos = 0;
			int x_pos = 2;
			int note_counter =0;

			display_key_list.Sort();
			if (display_key_list.Count>0)
			{
				foreach (int key_number in display_key_list)
				{
					if (BoxesBySeq.ContainsKey(key_number))
					{
						//Extract out the Information from the comms Node 
						Node dbz = (Node) BoxesBySeq[key_number];

						if (dbz != null)		
						{
							string node_name = dbz.GetAttribute("name");
							string node_display = dbz.GetAttribute("display_title");
							string node_status_str = dbz.GetAttribute("status");
							string display_title = dbz.GetAttribute("display_title");
							string display_content = dbz.GetAttribute("display_content");
							string display_icon = dbz.GetAttribute("display_icon");

							//draw everthing twice so that the scrolling works 
							if (UseAlternateBackColors)
							{
								if (note_counter % 2 ==0)
								{
									g.FillRectangle(this.firstBrush,0,y_pos-1,460, 17);//34);
									g.FillRectangle(this.firstBrush,0,y_pos+(offscreenheight/2)-1,460, 17);//34);
								}
								else
								{
									g.FillRectangle(this.secondBrush,0,y_pos-1,460, 17);//34);
									g.FillRectangle(this.secondBrush,0,y_pos+(offscreenheight/2)-1,460, 17);//34);
								}
							}

							//draw the Icon twice 
							/*
							Image c1 = getIconByName(display_icon);
							if (c1 != null)
							{
								g.DrawImage(c1,1,y_pos,32,32);
								g.DrawImage(c1,1,y_pos+(offscreenheight/2),32,32);
								x_pos = 34;
							}*/
							//Draw the bottom Divider Line 
							//g.DrawLine(Pens.Sienna,2,y_pos+34,this.Width-2,y_pos+34);
							//g.DrawLine(Pens.Sienna,2,y_pos+(offscreenheight/2)+34,this.Width-2,y_pos+(offscreenheight/2)+34);

							//Draw the Two lines of Message Text
							g.DrawString(display_title, MyDefaultSkinFontBold10, noteTextBrush, x_pos, y_pos);
							g.DrawString(display_title, MyDefaultSkinFontBold10, noteTextBrush, x_pos, y_pos+(offscreenheight/2));
							//g.DrawString(display_content, MyDefaultSkinFontBold10, noteTextBrush, x_pos, y_pos+16);
							//g.DrawString(display_content, MyDefaultSkinFontBold10, noteTextBrush, x_pos, y_pos+16+(offscreenheight/2));
							//System.Diagnostics.Debug.WriteLine("## "+displine.text+ " D "+count_tens.ToString());
							//Move the positioning on
							y_pos += singleSlotHeight;
						}
					}
					note_counter++;
				}
			}
		}

		private void StartTimer()
		{
			this.timer1.Enabled = true;
		}

		private void StopTimer()
		{
			this.timer1.Enabled = false;
		}

		private void timer1_Tick(object sender, System.EventArgs e)
		{
			if (scrolling)
			{
				ScrollVertical();
			}
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

//		private void db1_update_event()
//		{
//			BuildOffScreenBuffer();
//			this.Refresh();
//		}

		private void db1_AttributesChanged(Node sender, ArrayList attrs)
		{
			// Don't bother rebuilding if it's just the timeout that's changed.
			bool itMatters = false;
			foreach (AttributeValuePair avp in attrs)
			{
				if (avp.Attribute != "timeout")
				{
					itMatters = true;
				}
			}

			if (itMatters)
			{
				BuildOffScreenBuffer();
				this.Refresh();
			}
		}
	}
}