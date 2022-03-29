using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Text;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Runtime.InteropServices;

using LibCore;
using Network;

using CoreUtils;
using IncidentManagement;
using CommonGUI;
using BusinessServiceRules;

namespace OpsGUI
{

	/// <summary>
	/// TODO -- Needs to be REFACTOR as part of the general Display REFACTOR 
	/// </summary>
	public class TextAlertsDisplay : FlickerFreePanel
	{
		protected NodeTree MyNodeTreeHandle;
		protected Node AlertSrcNode;
		protected Boolean MyIsTrainingMode = false;
		protected Font MyDefaultSkinFontBold14 = null;
		protected Font MyDefaultSkinFontBold10 = null;
		protected bool display_mode_text = false;
		protected Hashtable AlertNodes = new Hashtable();

		protected string AlertTitle="ALERTS";
		protected Hashtable currentColors = new Hashtable();
		protected AutoScrollTextDisplay std = null;
		protected bool showAutoScrollTextPanel = true;
		protected bool showFlashPanel = false;
		protected string newfilename = "";
		protected string prefilename = "";
		protected string subtitle = "";
		protected bool showTitle = false;
		public string tag = "";


		protected FreeTimedFlashPlayer timedflashdisplay = null; 
		protected Node MyVideoNode = null;
		
		public TextAlertsDisplay(NodeTree model, Boolean IsTrainingMode)
		{
			MyNodeTreeHandle = model;
			MyIsTrainingMode = IsTrainingMode;
			string fontname =  SkinningDefs.TheInstance.GetData("fontname");
			MyDefaultSkinFontBold14 = ConstantSizeFont.NewFont(fontname,14,FontStyle.Bold);
			MyDefaultSkinFontBold10 = ConstantSizeFont.NewFont(fontname,10,FontStyle.Bold);

			std = new AutoScrollTextDisplay(IsTrainingMode);

			if (showTitle)
			{
				std.Location = new Point(50,25);
				std.Size = new Size(80,80);
			}
			else
			{
				std.Location = new Point(50,1);
				std.Size = new Size(80,80);
			}
			this.Controls.Add(std);

			timedflashdisplay = new FreeTimedFlashPlayer();
			//timedflashdisplay.AlterTimeManagement(false);
			timedflashdisplay.Location = new Point(0,25);
			timedflashdisplay.Size = new Size(100,100);
			timedflashdisplay.Visible = true;
			this.Controls.Add(timedflashdisplay);

			display_mode_text = false;
			BuildColors();
			this.Resize += new EventHandler(TextAlertsDisplay_Resize);
		}

		protected override void Dispose(bool disposing)
		{
			if(disposing)
			{
				//focusJumper.Dispose();
				MyDefaultSkinFontBold10.Dispose();
				MyDefaultSkinFontBold14.Dispose();
				if (std != null)
				{
					std.Dispose();
					std = null;
				}
				if (MyVideoNode != null)
				{
					MyVideoNode.AttributesChanged -=new Network.Node.AttributesChangedEventHandler(MyVideoNode_AttributesChanged);
				}
			}
			base.Dispose (disposing);
		}

		public void SetDisplayMode(string mode)
		{
			showAutoScrollTextPanel = true;
			showFlashPanel = false;

			switch (mode)
			{
				case "text":
					showAutoScrollTextPanel = true;
					showFlashPanel = false;
					break;
				case "flash":
					showAutoScrollTextPanel = false;
					showFlashPanel = true;
					break;
				case "both":
					showAutoScrollTextPanel = true;
					showFlashPanel = true;
					break;
			}
			this.HandleResize();
			this.Refresh();
		}

		private void HandleResize()
		{
			int title_buffer_y = 20;
			if (this.showTitle==false)
			{
			 title_buffer_y = 0;
			}


			if ((showAutoScrollTextPanel == true)&(showFlashPanel == false))
			{
				std.Location = new Point(5,5+title_buffer_y);
				std.Size = new Size(this.Width, this.Height-(10+title_buffer_y));
				std.Visible = true;
				timedflashdisplay.Visible = false;
			}
			if ((showAutoScrollTextPanel == true)&(showFlashPanel == true))
			{
				int half_width= (this.Width-6)/2;
				//split it
				std.Location = new Point(2,5+title_buffer_y);
				std.Size = new Size(half_width , this.Height-(10+title_buffer_y));
				std.Visible = true;
				timedflashdisplay.Location = new Point(half_width+4, 25+12); 
				timedflashdisplay.Size = new Size(half_width , this.Height-(20+title_buffer_y));
				timedflashdisplay.Visible = true;
			}
			if ((showAutoScrollTextPanel == false)&(showFlashPanel == true))
			{
				std.Location = new Point(5,5+title_buffer_y);
				std.Size = new Size(this.Width, this.Height-30);
				std.Visible = false;
				timedflashdisplay.Location = new Point(5,5+title_buffer_y); 
				timedflashdisplay.Size = new Size(this.Width, this.Height-(10+title_buffer_y));
				timedflashdisplay.Visible = true;
			}
		}

		private void TextAlertsDisplay_Resize(object sender, EventArgs e)
		{
			HandleResize();
		}

		public void BuildColors()
		{
			currentColors.Clear();
			currentColors.Add("green", Color.Green);
			currentColors.Add("red", Color.Red);
			currentColors.Add("orange", Color.Orange);
			currentColors.Add("white", Color.White);
			currentColors.Add("pink", Color.Pink);
			currentColors.Add("yellow", Color.Yellow);
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

		public void SetAlertSource(string main_title, ArrayList alert_node_names, string video_nodename)
		{
			bool rebuild = false;

			AlertTitle = main_title;
			if (alert_node_names != null)
			{
				if (alert_node_names.Count>0)
				{
					foreach (string alert_node_name_str in alert_node_names)
					{
						AlertSrcNode = MyNodeTreeHandle.GetNamedNode(alert_node_name_str);
						if (AlertSrcNode != null)
						{
							AlertNodes.Add(alert_node_name_str, AlertSrcNode);
							AlertSrcNode.AttributesChanged +=new Network.Node.AttributesChangedEventHandler(AlertSrcNode_AttributesChanged);
							AlertSrcNode.ChildAdded +=new Network.Node.NodeChildAddedEventHandler(AlertSrcNode_ChildAdded);
							AlertSrcNode.ChildRemoved +=new Network.Node.NodeChildRemovedEventHandler(AlertSrcNode_ChildRemoved);
							rebuild = true;
						}
					}
				}
			}
			if (video_nodename != "")
			{
				MyVideoNode = MyNodeTreeHandle.GetNamedNode(video_nodename);
				if (MyVideoNode != null)
				{
					MyVideoNode.AttributesChanged +=new Network.Node.AttributesChangedEventHandler(MyVideoNode_AttributesChanged);
				}
			}
			if (rebuild)
			{
				RebuildDisplayLines();
			}
		}
		
		private void RemoveMonitoring()
		{
			//copy the list out and then remove and clear down the structures
			ArrayList KillList = new ArrayList();
			foreach (Node alert_node_name in AlertNodes.Keys)
			{
				KillList.Add(alert_node_name);
			}
			foreach (Node node_name in KillList)
			{
				if (AlertNodes.Contains(node_name))
				{
					Node AlertSrcNode = (Node) AlertNodes[node_name];
					AlertSrcNode.AttributesChanged -=new Network.Node.AttributesChangedEventHandler(AlertSrcNode_AttributesChanged);
					AlertSrcNode.ChildAdded -=new Network.Node.NodeChildAddedEventHandler(AlertSrcNode_ChildAdded);
					AlertSrcNode.ChildRemoved -=new Network.Node.NodeChildRemovedEventHandler(AlertSrcNode_ChildRemoved);
				}
			}
			AlertNodes.Clear();
		}

		private void AlertSrcNode_ChildAdded(Node sender, Node child)
		{
			if (this.tag == "FM")
			{
				string ss="";
			}

			RebuildDisplayLines();
		}

		private void AlertSrcNode_ChildRemoved(Node sender, Node child)
		{
			if (this.tag == "FM")
			{
				string ss="";
			}

			RebuildDisplayLines();
		}

		private void AlertSrcNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			if (this.tag == "FM")
			{
				string ss="";
			}

			Boolean RefreshNeeded = false;
			if (attrs != null)
			{
				if (attrs.Count > 0)
				{
					foreach(AttributeValuePair avp in attrs)
					{
						string attribute = avp.Attribute;
						string newValue = avp.Value;
						if (attribute.ToLower() == "refresh")
						{
							RefreshNeeded=true;
						}
					}
				}
			}

			if (RefreshNeeded)
			{
				RebuildDisplayLines();
			}
		}
		

		private void MyVideoNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			if (this.tag == "FM")
			{
				string ss="";
			}

			bool Refresh = false;
			foreach(AttributeValuePair avp in attrs)
			{
				if(avp.Attribute == "start")
				{
					Refresh =true;
				}
			}
			if (Refresh)
			{
				int new_posx = sender.GetIntAttribute("position_x",0);
				int new_posy = sender.GetIntAttribute("position_y",0);
				int new_width = sender.GetIntAttribute("width",10);
				int new_height = sender.GetIntAttribute("height",10);
				newfilename = sender.GetAttribute("filename");
				prefilename = sender.GetAttribute("prefilename");
				subtitle = sender.GetAttribute("subtitle");
				if (Visible == true)
				{
					timedflashdisplay.Top = 40;
				}
				this.Refresh();
				//
				//timedflashdisplay.Location = new Point(new_posx,new_posy);
				//timedflashdisplay.Size = new Size(new_width,new_height);
				timedflashdisplay.Loop = true;
				timedflashdisplay.PlayFile(newfilename);
			}
		}



		protected void RebuildDisplayLines()
		{
			bool redraw_screen = false;
			//Clear the display lines 
//			display_lines.Clear();
			//Iterate over the alert nodes 
			int subnumber =0;
//			display_key_list.Clear();

			std.ClearTextLines();
			if (AlertNodes.Count>0)
			{
				foreach (string alert_node_name in AlertNodes.Keys)
				{
					redraw_screen = true;
					Node anode = (Node) AlertNodes[alert_node_name];
					if (anode != null)
					{
						string name = anode.GetAttribute("name");
						bool show = anode.GetBooleanAttribute("show",false);
						string dtext = anode.GetAttribute("displaytext_none");
						string colortext = anode.GetAttribute("color");
						int disp_order = anode.GetIntAttribute("disp_order",10);
						if (show)
						{
							ArrayList kidslist = anode.getChildren();
							if (kidslist.Count==0)
							{
								//No Kids so show the none text instead 
								DisplayLine dl = new DisplayLine();
								dl.flash_status = false;
								dl.text = dtext;
								dl.normal_color = getColor(colortext);
								if (std.isDisplayLineListed(dl)==false)
								{
									std.AddDisplayLine(disp_order,dl);
								}
								//display_lines.Add(disp_order*1000+subnumber, dl);
								//display_key_list.Add(disp_order*1000+subnumber);
								//subnumber++;
							}
							else
							{
								foreach (Node n1 in kidslist)
								{
									name = n1.GetAttribute("name");
									show = n1.GetBooleanAttribute("show",false);
									dtext = n1.GetAttribute("displaytext");
									colortext = n1.GetAttribute("color");

									DisplayLine dl = new DisplayLine();
									dl.flash_status = false;
									dl.text = dtext;
									dl.normal_color = getColor(colortext);

									if (std.isDisplayLineListed(dl)==false)
									{
										System.Diagnostics.Debug.WriteLine("ADDING "+name+" "+dl.toDataString());

										std.AddDisplayLine(disp_order,dl);
									}
									else
									{
										System.Diagnostics.Debug.WriteLine("LISTED "+name+" "+dl.toDataString());
	
									}
									//display_lines.Add(disp_order*1000+subnumber, dl);
									//display_key_list.Add(disp_order*1000+subnumber);
									//subnumber++;
								}
							}
						}
					}
				}
			}
			//Build and sort 
			//if (display_key_list.Count>0)
			//{
			//	display_key_list.Sort();
			//}
			if (redraw_screen)
			{
				this.Refresh();
			}
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			//Setup the Quality and Rendering Options 
			e.Graphics.SmoothingMode = SmoothingMode.None;
			e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
			e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
			int start_x =0;
			int start_y = 20;

			//Draw Background 
			e.Graphics.FillRectangle(Brushes.Black,0,0,this.Width, this.Height);
			
			if (this.showTitle)
			{
				e.Graphics.FillRectangle(Brushes.Red,2,2,this.Width-4, 20);
				SizeF textsize = new SizeF(0,0);
				textsize = e.Graphics.MeasureString(AlertTitle,MyDefaultSkinFontBold14);
				start_x = (this.Width - (int)textsize.Width) / 2;
				//e.Graphics.DrawString("Daily Financials", MyDefaultSkinFontBold14, Brushes.White,start_x,0);
				e.Graphics.DrawString(AlertTitle, MyDefaultSkinFontBold14, Brushes.White,start_x,0);
			}
			
			if (timedflashdisplay.Visible == true)
			{
				if (subtitle != null)
				{
					e.Graphics.DrawString(subtitle, MyDefaultSkinFontBold10, Brushes.White, timedflashdisplay.Left, start_y+2);
				}
			}
		}

	}
}
