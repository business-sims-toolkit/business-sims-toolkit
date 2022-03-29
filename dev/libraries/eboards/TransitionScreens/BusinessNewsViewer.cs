using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;

using LibCore;
using Network;
using CommonGUI;
using CoreUtils;

namespace TransitionScreens
{
	public delegate void FlashFileChangedHandler (object sender, FlashFileChangedArgs e);

	public class FlashFileChangedArgs : EventArgs
	{
		protected string newFlashFileName;
		public string NewFlashFileName
		{
			get
			{
				return newFlashFileName;
			}
		}

		public FlashFileChangedArgs (string f)
		{
			newFlashFileName = f;
		}
	}

	/// <summary>
	/// Summary description for BusinessNewsViewer
	/// </summary>
	public class BusinessNewsViewer : FlickerFreePanel
	{
		TimedFlashPlayer flash;
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		System.ComponentModel.Container components = null;

		NodeTree _NetworkModel;
		Node _CurrentDayNode;

		Node businessNotifiedEvents;

		string currentSwfName;

		public event FlashFileChangedHandler FlashFileChanged;

		public Brush text_brush = new SolidBrush (SkinningDefs.TheInstance.GetColorDataGivenDefault("transition_title_colour", SkinningDefs.TheInstance.GetColorDataGivenDefault("transition_text_colour", Color.DarkBlue)));

		bool SelfDrawTranslatedTitle = false;
		string panelTitle = "Newsfeed";
		Font titleFont = null;
		bool auto_translate = true;

		public void SetFlashPos(int x, int y, int w, int h)
		{
			flash.Location = new Point(x,y);
			flash.Size = new Size(w,h);
		}

		/// <summary>
		/// 
		/// </summary>
		public BusinessNewsViewer(NodeTree nt)
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			SuspendLayout();

			flash = new TimedFlashPlayer();
			flash.BackColor = Color.Black;
			flash.Location = new Point(14,37);
			flash.BorderStyle = BorderStyle.None;
			flash.Size = new Size(400,180);
			flash.Name = "Biz News Viewer Flash";
			flash.ZoomWithCropping(new PointF (0.5f, 0), new PointF (0.5f, 0));
			Controls.Add(flash);

			ResumeLayout(false);

			string fontname =  SkinningDefs.TheInstance.GetData("fontname");
			titleFont = ConstantSizeFont.NewFont(fontname,12f);
			if (auto_translate)
			{
				titleFont.Dispose();
				titleFont = ConstantSizeFont.NewFont(TextTranslator.TheInstance.GetTranslateFont(fontname), 12f, FontStyle.Bold);
			}


			//Connect up the Required Node
			_NetworkModel = nt;
			_CurrentDayNode = _NetworkModel.GetNamedNode("CurrentDay");

			businessNotifiedEvents = nt.GetNamedNode("BusinessNotifiedEvents");
			businessNotifiedEvents.ChildAdded += businessNotifiedEvents_ChildAdded;

			ArrayList events = new ArrayList();
			// See if we have any events already there...
			foreach(Node n in businessNotifiedEvents.getChildren())
			{
				ProcessEventNode(n);
				events.Add(n);
			}
			//
			foreach(Node n in events)
			{
				businessNotifiedEvents.DeleteChildTree(n);
			}
		}

		protected virtual void OnFlashFileChanged (FlashFileChangedArgs e)
		{
			if (FlashFileChanged != null)
			{
				FlashFileChanged(this, e);
			}
		}

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				businessNotifiedEvents.ChildAdded -= businessNotifiedEvents_ChildAdded;
				flash.Dispose();

				if (titleFont != null)
				{
					titleFont.Dispose();
				}

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
			this.SuspendLayout();
			this.Size = new System.Drawing.Size(429, 230);
			this.BackColor = Color.Transparent;
			this.BackgroundImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + 
				"\\images\\panels\\bnf.png");

			this.Name = "TransitionPhaseBusinessNews";
			this.ResumeLayout(false);

		}
		#endregion

		public void EnableSelfDrawTitle(bool newState)
		{
			SelfDrawTranslatedTitle = newState;
		}

		void ProcessEventNode(Node child)
		{
			// Get the flash file to play and consume the child!
			currentSwfName = child.GetAttribute("swf");
			int time = child.GetIntAttribute("duration",0);
			flash.PlayFile(currentSwfName,time);
			flash.Loop = child.GetBooleanAttribute("loop", true);

			OnFlashFileChanged(new FlashFileChangedArgs (currentSwfName));
		}

		public void SetTrainingMode(Boolean Tr)
		{
			if (Tr)
			{
				BackgroundImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + 
					"\\images\\panels\\t_bnf.png");
			}
			else
			{
				BackgroundImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + 
					"\\images\\panels\\bnf.png");
			}
		}

		void businessNotifiedEvents_ChildAdded(Node sender, Node child)
		{
			ProcessEventNode(child);
			child.Parent.DeleteChildTree(child);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			//g.DrawRectangle(Pens.Cyan,0,0,this.Width-1,this.Height-1);
			if (SelfDrawTranslatedTitle)
			{
				string title_text = panelTitle;
				if (auto_translate)
				{
					title_text = TextTranslator.TheInstance.Translate(title_text);
					g.DrawString(title_text, titleFont, text_brush,10,0);
				}
			}
		}

		public void HideTitle ()
		{
			panelTitle = "";
			Invalidate();
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);
			DoSize();
		}

		void DoSize ()
		{
			if (flash != null)
			{
				flash.Bounds = new Rectangle(0, 0, Width, Height);
			}
		}
	}
}