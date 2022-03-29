using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Data;
using System.Windows.Forms;

using LibCore;
using Network;

using CoreUtils;
using IncidentManagement;
using CommonGUI;
using BusinessServiceRules;

namespace CommonGUI
{
		
	/// <summary>
	/// Summary description for PendingActionsControl.
	/// </summary>
	public class BusinessActivityView : FlickerFreePanel
	{
		protected NodeTree MyNodeTreeHandle;
		protected Boolean MyIsTrainingMode = false;
		protected Panel scrollablePanel;
		protected Font MyDefaultSkinFontBold14 = null;
		protected Font MyDefaultSkinFontBold10 = null;
		protected int corneringExtent = 10; //100 no gaps 
	
		public BusinessActivityView(NodeTree model, Boolean IsTrainingMode)
		{
			MyNodeTreeHandle = model;
			MyIsTrainingMode = IsTrainingMode;
			string fontname =  SkinningDefs.TheInstance.GetData("fontname");
			MyDefaultSkinFontBold14 = ConstantSizeFont.NewFont(fontname,14,FontStyle.Bold);
			MyDefaultSkinFontBold10 = ConstantSizeFont.NewFont(fontname,10,FontStyle.Bold);

			scrollablePanel = new Panel();
//			scrollablePanel.AutoScroll = true;
//			scrollablePanel.Size = new Size(100,100);
//			scrollablePanel.Location = new Point(10,10);
//			scrollablePanel.BackColor = Color.Black;
//			this.Controls.Add(scrollablePanel);


//			Panel p1 = new Panel();
//			p1.Size = new Size(5,500);
//			p1.Location = new Point(10,10);
//			p1.BackColor = Color.Magenta;
//			scrollablePanel.Controls.Add(p1);
		}

		protected override void Dispose(bool disposing)
		{
			if(disposing)
			{
				//focusJumper.Dispose();
				MyDefaultSkinFontBold10.Dispose();
				MyDefaultSkinFontBold14.Dispose();
				scrollablePanel.Dispose();

//				foreach (Node trans in MyTransNode.getChildren())
//				{
//					trans.AttributesChanged -= new Network.Node.AttributesChangedEventHandler (trans_AttributesChanged);
//				}

			}
			base.Dispose (disposing);
		}

		protected void trans_AttributesChanged(Node sender, System.Collections.ArrayList attrs)
		{
			Update();
		}

		protected void Update()
		{
			this.Invalidate();
		}

		private string BuildTimeString(int timevalue)
		{
			int time_mins = timevalue / 60;
			int time_secs = timevalue % 60;
			string displaystr = CONVERT.ToStr(time_mins)+":";
			if (time_secs<10)
			{
				displaystr += "0";
			}
			displaystr += CONVERT.ToStr(time_secs);
			return displaystr;
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			scrollablePanel.Location = new Point(10,25);
			scrollablePanel.Size = new Size(this.Width - 20, this.Height -(25+10));
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			//Setup the Quality and Rendering Options 
			e.Graphics.SmoothingMode = SmoothingMode.None;
			e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
			e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

			bool oldmode = false;
			if (oldmode)
			{
				e.Graphics.FillRectangle(Brushes.Black,0,0,this.Width, this.Height);
				e.Graphics.DrawRectangle(Pens.DarkGray,2,2,this.Width-4, this.Height-4);
				e.Graphics.DrawRectangle(Pens.DarkGray,3,3,this.Width-6, this.Height-6);
			}
			else
			{
				e.Graphics.FillRectangle(Brushes.Black,0,0,this.Width, this.Height);

				//Drawing the Corners 
				int PaintWidth = (corneringExtent * this.Width) / 100;
				int PaintHeight = (corneringExtent * this.Height) / 100;

				int OuterRectStartX=2; 
				int OuterRectStartY=2; 
				int OuterRectStopX=this.Width-4+2; 
				int OuterRectStopY=this.Height-4+2; 

				Pen tmpPen = new Pen(Brushes.Silver, 2);
				//Drawing the Horizental lines
				e.Graphics.DrawLine(tmpPen, OuterRectStartX, OuterRectStartY, PaintWidth, OuterRectStartY);
				e.Graphics.DrawLine(tmpPen, OuterRectStopX - PaintWidth, OuterRectStartY, OuterRectStopX, OuterRectStartY);
				e.Graphics.DrawLine(tmpPen, OuterRectStartX, OuterRectStopY, PaintWidth, OuterRectStopY);
				e.Graphics.DrawLine(tmpPen, OuterRectStopX - PaintWidth, OuterRectStopY, OuterRectStopX, OuterRectStopY);
				
				//Drawing the Vertical lines
				e.Graphics.DrawLine(tmpPen, OuterRectStartX, OuterRectStartY, OuterRectStartX,  PaintHeight);
				e.Graphics.DrawLine(tmpPen, OuterRectStartX, OuterRectStopY - PaintHeight, OuterRectStartX, OuterRectStopY);
				e.Graphics.DrawLine(tmpPen, OuterRectStopX, OuterRectStartY, OuterRectStopX,  PaintHeight);
				e.Graphics.DrawLine(tmpPen, OuterRectStopX, OuterRectStopY - PaintHeight, OuterRectStopX, OuterRectStopY);
				tmpPen.Dispose();
			}

//			e.Graphics.FillRectangle(Brushes.Black,0,0,this.Width, this.Height);
//			e.Graphics.DrawRectangle(Pens.DarkGray,2,2,this.Width-4, this.Height-4);
//			e.Graphics.DrawRectangle(Pens.DarkGray,3,3,this.Width-6, this.Height-6);
			e.Graphics.DrawString("Business Activity Reports", MyDefaultSkinFontBold14, Brushes.Silver,5,2);

			//Content
//			int y = 20;
//			foreach (Node trans in MyTransNode.getChildren())
//			{
//				string s = trans.GetAttribute("displayname");

//				string args = "";
//				for (int i = 0; i < 10; i++)
//				{
//					string arg = trans.GetAttribute("arg" + CONVERT.ToStr(i));
//
//					if (arg.Length > 0)
//					{
//						if (args.Length > 0)
//						{
//							args += " ";
//						}
//
//						args += arg;
//					}
//				}
//
//				//s += " '" + args + "' " + ModelTimeManager.TimeToStringWithDay(task.GetIntAttribute("start_time", 0)) + " " + task.GetAttribute("duration_total") + " " + task.GetAttribute("completed");
//				e.Graphics.DrawString(s, MyDefaultSkinFontBold10, Brushes.Silver, 10, y);
//				y += 20;
//			}
		}

	}
}
