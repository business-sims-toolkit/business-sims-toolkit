using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

using System.Collections;
using LibCore;
using Network;
using CommonGUI;

namespace DiscreteSimGUI
{
	/// <summary>
	/// Visually represents one business service being monitored.
	/// States are:
	///		Green : Service is Up And OK.
	///		Red   : Service is unavailable due to a failure. Service originally flashes red/green to
	///		        show that a failure has just occured, then after 30 seconds goes solid red.
	///		Lilac : Service is unavailable due to a reboot occuring. Time until reboot finished counts
	///		        down on the service bar.
	///		Blue  : Service has failed but is up temporarily due to a workaround. Time counts down in
	///		        white then fails to red again.
	///		Amber : Server is currently available but is running on a mirrored server.
	///		Purple: Service is available but is likely to fail in the next minute (i.e. goingDown is set).
	///		
	/// </summary>
	public class StatusMonitorItem : MonitorItem
	{
		protected string text;
		protected string info;
		protected Color colour;
		protected int imageIndex;
		protected Point location;
		protected Size size;
		protected Font font;
		protected Font infoFont;
		protected Rectangle lastRect;
		protected bool canFix = false;
		protected bool canWorkAround = false;
		protected bool impacted;
		protected bool warning1;
		protected bool warning2;
		//protected Event currentEvent;

		protected StatusMonitor parent;
		protected int countDown = 0;
		protected int flashCount = 0;
		protected int startTick = 0;
		//protected Service service;

		protected Rectangle fixRect;
		protected Rectangle waroundRect;
		//
		protected Image upImage1;
		protected Image upImage2;
		protected Image downImage1;
		protected Image downImage2;
		//
		protected Image bg;
		protected Image bg_flashon;
		protected Image bg_flashoff;

		protected bool button1down;
		protected bool button2down;

		//protected Image icon = null;

		protected Node monitoredItem;
		protected Node fixItQueue;
		protected Node workAroundCounter;

		public Node MonitoredBusinessService
		{
			get
			{
				return monitoredItem;
			}
		}

		/// <summary>
		/// Creates an instance of StatusMonitorItem.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="n"></param>
		public StatusMonitorItem(StatusMonitor parent, Node n)
		{
			this.parent = parent;
			this.size = parent.ItemSize;
			//this.imageIndex = s.Node.ConfigItem.ImageIndex;
			//this.text = s.Node.ConfigItem.Description;
			this.info = String.Empty;
			this.font = ConstantSizeFont.NewFont("Tahoma", 8f, FontStyle.Bold);
			this.infoFont = ConstantSizeFont.NewFont("Arial", 12f, FontStyle.Bold);
			//this.service = s;

			this.BackColor = Color.White;

			this.upImage1 = Repository.TheInstance.GetImage("/images/lozenges/check_s.png");
			this.upImage2 = Repository.TheInstance.GetImage("/images/lozenges/replace2_s.png");
			this.downImage1 = Repository.TheInstance.GetImage("/images/lozenges/check.png");
			this.downImage2 = Repository.TheInstance.GetImage("/images/lozenges/replace2.png");

			this.bg = Repository.TheInstance.GetImage("/images/lozenges/lozenge_green.png");
			this.bg_flashon = Repository.TheInstance.GetImage("/images/lozenges/lozenge_lightred.png");
			this.bg_flashoff = Repository.TheInstance.GetImage("/images/lozenges/lozenge_darkred.png");

			colour = Color.Green;

			// Find the appropriate icon for this business service.
			// This is application DTD specific.
			desc = n.GetAttribute("desc");
			GetIcon();

			// Register for status change notifications
			monitoredItem = n;
			n.AttributesChanged += n_AttributesChanged;

			// Get the FixItQueue
			fixItQueue = n.Tree.GetNamedNode("FixItQueue");
			//
			workAroundCounter = n.Tree.GetNamedNode("AppliedWorkArounds");
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics g = this.Parent.CreateGraphics();
			Render(g);
			g.Dispose();
		}

		/// <summary>
		/// Render the status monitor item.
		/// </summary>
		/// <param name="g"></param>
		public void Render(Graphics g)
		{
			Image back = (this.colour == Color.Green) ? bg : 
				((this.flashCount % 2) != 0) ? bg_flashoff : bg_flashon;

			lastRect = new Rectangle((int)location.X, (int)location.Y, (int)size.Width, (int)size.Height);
			RectangleF textRect = new RectangleF(location.X + padLeft + 36, location.Y + padTop, size.Width - padLeft - padRight - 36, size.Height - padTop - padBottom);
			RectangleF infoRect = new RectangleF(location.X + padLeft, location.Y + 16, size.Width - padLeft - padRight - 4, size.Height - padTop - padBottom);
			RectangleF roundedRect = new RectangleF(location.X + padLeft, location.Y + padTop, size.Width - padLeft - padRight, size.Height - padTop - padBottom);
			
			g.SmoothingMode = SmoothingMode.AntiAlias;
			g.CompositingQuality = CompositingQuality.HighQuality;
			g.PixelOffsetMode = PixelOffsetMode.HighQuality;


			//g.FillEllipse(new SolidBrush(colour), roundedRect.X, roundedRect.Y, roundedRect.Height, roundedRect.Height);
			//g.FillRectangle(new SolidBrush(colour), roundedRect.X + (roundedRect.Height / 2), roundedRect.Y, roundedRect.Width - roundedRect.Height, roundedRect.Height);
			//g.FillEllipse(new SolidBrush(colour), roundedRect.X + roundedRect.Width - roundedRect.Height, roundedRect.Y, roundedRect.Height, roundedRect.Height);
			g.DrawImage(back, roundedRect.X + 26, roundedRect.Y, 66, 28);
			g.DrawImage(icon, (int)location.X + padLeft, (int)location.Y + padTop - 1, 30, 30);

			// get rid of the crusty edges
			//g.DrawEllipse(new Pen(Brushes.Black, 3f), (int)location.X + padLeft, (int)location.Y + padTop - 2, 32, 32);

			if (info != null && info.Length > 0)
			{
				StringFormat sf = new StringFormat(StringFormatFlags.NoWrap);
				sf.Trimming = StringTrimming.EllipsisCharacter;
				sf.LineAlignment = StringAlignment.Near;
				sf.Alignment = StringAlignment.Far;
				g.DrawString(info, infoFont, new SolidBrush(Color.White), infoRect, sf);
			}

			if(canFix && (colour != Color.Green))
			{
				fixRect = new Rectangle((int)location.X + (int)size.Width - 60, (int)location.Y + 0, 24, 24);

				if (button1down)
				{
					g.DrawImageUnscaled(downImage1, fixRect.X, fixRect.Y);
				}
				else
				{
					g.DrawImageUnscaled(upImage1, fixRect.X, fixRect.Y);
				}
			}

			if(canWorkAround)
			{
				waroundRect = new Rectangle((int)location.X + (int)size.Width - 34, (int)location.Y + 0, 24, 24);

				if(button2down)
				{
					g.DrawImageUnscaled(downImage2, waroundRect.X, waroundRect.Y);
				}
				else
				{
					g.DrawImageUnscaled(upImage2, waroundRect.X, waroundRect.Y);
				}
			}
		}

		/// <summary>
		/// Converts the specified number of seconds
		/// to mins:secs format.
		/// </summary>
		/// <param name="ticks"></param>
		/// <returns></returns>
		string ConvertToTime(int ticks)
		{
			if(ticks == 0) return "";

			int mins = ticks / 60;
			int secs = ticks- (mins * 60);

			return CONVERT.ToStr(mins) + ":" + CONVERT.ToStr(secs).PadLeft(2, '0');
		}

		/// <summary>
		/// Gets or Sets the Text for the status monitor item.
		/// </summary>
		public override string Text
		{
			get { return desc; }
			set { text = value; }
		}

		/// <summary>
		/// Gets or Sets the Info for the status monitor item.
		/// </summary>
		public string Info
		{
			get { return info; }
			set 
			{ 
				info = value; 
			}
		}

		/// <summary>
		/// Gets or Sets the size of the status monitor item.
		/// </summary>
		new public Size Size
		{
			get { return size; }
			set
			{
				size = value;
			}
		}

		/// <summary>
		/// Gets or Sets the location of the status monitor item.
		/// </summary>
		new public Point Location
		{
			get { return location; }
			set { location = value; }
		}

		/// <summary>
		/// Gets the bounds of the status monitor item.
		/// </summary>
		new public Rectangle Bounds
		{
			get { return new Rectangle(location, size); }
		}

		/// <summary>
		/// Determines whether the fix or workaround button
		/// has been pressed.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		new public void MouseDown(int x, int y)
		{
			if (canFix || canWorkAround)
			{
				button1down = fixRect.Contains(x, y);
				button2down = waroundRect.Contains(x, y);
				parent.Invalidate(lastRect);
			}
		}

		/// <summary>
		/// Dispatches the Fix or Workaround event to
		/// the associated Service.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		new public void MouseUp(int x, int y)
		{
			if (canFix || canWorkAround)
			{
				if (canFix && fixRect.Contains(x, y))
				{
					if(fixItQueue != null)
					{
						AttributeValuePair avp = new AttributeValuePair();
						avp.Attribute="target";
						avp.Value=monitoredItem.GetAttribute("name");
						Node n = new Node(fixItQueue,"fix","",avp);
					}
					//service.Fix();
				}
				else if (canFix /*canWorkAround*/ && waroundRect.Contains(x, y))
				{
					if(fixItQueue != null)
					{
						AttributeValuePair avp = new AttributeValuePair();
						avp.Attribute="target";
						avp.Value=monitoredItem.GetAttribute("name");
						Node n = new Node(fixItQueue,"workaround","",avp);
					}
					//service.Workaround();
				}

				button1down = false;
				button2down = false;
				parent.Invalidate(lastRect);
			}
		}

		/// <summary>
		/// Clears the fix and workaround flags.
		/// </summary>
		new public void MouseLeave()
		{
			if (canFix || canWorkAround)
			{
				button1down = false;
				button2down = false;
				parent.Invalidate(lastRect);
			}
		}

		protected bool serviceIsUp = true;
		protected bool serviceIsGoingDown = false;
		protected bool upByMirror = false;
		protected int flashSecs = 0;

		void n_AttributesChanged(Node sender, ArrayList attrs)
		{ 
			if (attrs != null)
			{
				if (attrs.Count > 0)
				{
					foreach(AttributeValuePair avp in attrs)
					{
						//Extraction of the data attribute
						string attribute = avp.Attribute;
						string newValue = avp.Value;
						//Do the work 
						HandleAttributeChanged(sender, attribute, newValue);
					}
				}
			}
		}

		void HandleAttributeChanged(Node sender, string attribute, string newValue)
		{
			bool l_serviceIsUp = serviceIsUp;
			bool l_serviceIsGoingDown = serviceIsGoingDown;
			bool l_upByMirror = upByMirror;
			bool rebooting = false;
			bool workAround = false;
			//
			bool invalidate = false;
			//
			// Don't go Red if there is no impact!
			bool impact = false;
			//string impactKmph = this.monitoredItem.GetAttribute("impactKmh");
			//string impactSecsInPit = this.monitoredItem.GetAttribute("impactSecsInPit");
			Boolean hasImpact = monitoredItem.GetBooleanAttribute("has_impact",false);

			//
			//if( (impactSecsInPit != "") && (impactSecsInPit != "0") ) impact = true;
			//if( (impactKmph != "") && (impactKmph != "0") ) impact = true;
			if( hasImpact) impact = true;

			// If no impact then don't bother!
			if(!impact)
			{
				return;
			}
			//

			if(attribute == "up")
			{
				if(newValue.ToLower() == "false")
				{
					l_serviceIsUp = false;
					SetFlashCount(30);
				}
				else
				{
					l_serviceIsUp = true;
					SetFlashCount(0);
				}
			}
			else if(attribute == "goingDown")
			{
				if(newValue == "true")
				{
					l_serviceIsGoingDown = true;
					SetFlashCount(30);
				}
				else
				{
					l_serviceIsGoingDown = false;
				}
			}
			else if(attribute == "upByMirror")
			{
				if(newValue == "true")
				{
					l_upByMirror = true;
				}
				else
				{
					l_upByMirror = false;
				}
			}
			else if(attribute == "rebootingForSecs")
			{
				if( (newValue == "0") || ("" == newValue) )
				{
					info = "";
				}
				else
				{
					info = ConvertToTime( CONVERT.ParseInt(newValue) );
					rebooting = true;
				}
				//
				invalidate = true;
			}
			else if(attribute == "workingAround")
			{
				if( ("" == newValue) || ("0" == newValue) )
				{
					info = "";
				}
				else
				{
					info = ConvertToTime( CONVERT.ParseInt(newValue) );
					if(null != workAroundCounter)
					{
						string num = workAroundCounter.GetAttribute("num");
						if("" != num)
						{
							info += " " + num;
						}
					}
					workAround = true;
				}
				invalidate = true;
			}
			/*
			if( (upByMirror != l_upByMirror) || (l_serviceIsGoingDown != serviceIsGoingDown) ||
				(l_serviceIsUp != serviceIsUp) || impactChanged || workAround || rebooting || counting)*/
			{
				invalidate = true;
				serviceIsUp = l_serviceIsUp;
				serviceIsGoingDown = l_serviceIsGoingDown;
				upByMirror = l_upByMirror;
				//
				if(!serviceIsUp)
				{
					if(rebooting)
					{
						colour = Color.Magenta;
					}
					else
					{
						colour = Color.Red;
						string t_info = sender.GetAttribute("downforsecs");
						if(t_info != "")
						{
							info = ConvertToTime( CONVERT.ParseInt( t_info ) );
						}
					}
				}
				else
				{
					if(workAround)
					{
						colour = Color.Blue;
					}
					else if(upByMirror)
					{
						colour = Color.Orange;
					}
					else if(serviceIsGoingDown)
					{
						colour = Color.Purple;
					}
					else
					{
						colour = Color.Green;
					}
				}
			}

			if(attribute == "fixable")
			{
				canFix = CONVERT.ParseBool(newValue, false);
				invalidate = true;
			}
			else if(attribute == "canWorkAround")
			{
				canWorkAround = CONVERT.ParseBool(newValue, false);
				invalidate = true;
			}
			//
			if(invalidate)
			{
				parent.Invalidate();
			}
		}

		protected override void Dispose(bool disposing)
		{
			if(disposing)
			{
				monitoredItem.AttributesChanged -= n_AttributesChanged;
			}
			base.Dispose (disposing);
		}

		public void SetFlashCount(int c)
		{
			flashCount = c;
			this.parent.SetFlashCount(c);
		}

		public void DecrementFlashCounter()
		{
			if(flashCount > 0) --flashCount;
		}
	}
}
