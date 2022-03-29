using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Algorithms;
using Network;
using CommonGUI;
using CoreUtils;
using LibCore;

namespace DiscreteSimGUI
{
    public class ItilToolTip_Quad : Form
    {
		[DllImport(@"user32.dll", EntryPoint="SetWindowPos", CallingConvention=CallingConvention.StdCall, SetLastError=true)]
		public static extern int SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

		const int SWP_DRAWFRAME = 0x20;
		const int SWP_NOMOVE = 0x2;
		const int SWP_NOSIZE = 0x1;
		const int SWP_NOZORDER = 0x4;
		const int HWND_TOPMOST = -1;
		const int HWND_NOTOPMOST = -2;
		const int TOPMOST_FLAGS = SWP_NOMOVE | SWP_NOSIZE;

	    class BlockedItem
		{
			public string target = "";
			public bool fix_allowed = true;
			public bool fixconsult_allowed = true;
			public bool workaround_allowed = true;
			public string reason = "";
		}

	    class ToolTipMenuItem_Quad : BasePanel
		{
			public Node FixQueue;
			public string Target;
			string fixlabel;
			Hashtable myblocks = new Hashtable();

			public ToolTipMenuItem_Quad(string text, string image)
			{
			    Label label = new Label
			                  {
			                      Location = new Point(17, 0),
			                      Text = text,
			                      TextAlign = ContentAlignment.MiddleLeft,
			                      Size = new Size(95, 20),
			                      Font = SkinningDefs.TheInstance.GetFont(SkinningDefs.TheInstance.GetFloatData("lozenge_menu_item_font_size", 7))
			                  };
			    label.MouseEnter += label_MouseEnter;
				label.MouseLeave += label_MouseLeave;
				label.MouseDown  += cancel_MouseDown;
				Controls.Add(label);

			    ImageButton button = new ImageButton(0)
			                         {
			                             Size = new Size(16, 16),
			                             Location = new Point(0, 1),
			                             Name = "ITIL Tool Tip Quad Button"
			                         };
			    button.SetButton(AppInfo.TheInstance.Location + "/" + image);
				Controls.Add(button);

				Size = new Size(SkinningDefs.TheInstance.GetIntData("tool_tip_width", 120), 20);
			}

			public ToolTipMenuItem_Quad(string text, string image, string fixlabel)
			{
				this.fixlabel = fixlabel;

			    Label label = new Label
			                  {
			                      Location = new Point(17, 0),
			                      Text = text,
			                      TextAlign = ContentAlignment.MiddleLeft,
			                      Size = new Size(95, 20),
			                      Font = SkinningDefs.TheInstance.GetFont(SkinningDefs.TheInstance.GetFloatData("lozenge_menu_item_font_size", 7))
			                  };
			    label.MouseEnter += label_MouseEnter;
				label.MouseLeave += label_MouseLeave;
				label.MouseDown  += label_MouseDown;
				Controls.Add(label);

			    ImageButton button = new ImageButton(0)
			                         {
			                             Size = new Size(16, 16),
			                             Location = new Point(0, 1),
			                             Name = "Tool Tip Quad Button"
			                         };
			    button.SetButton(AppInfo.TheInstance.Location + "/" + image);
				Controls.Add(button);

				Size = new Size(SkinningDefs.TheInstance.GetIntData("tool_tip_width", 120), 20);
			}

			public void setMyBlocks(Hashtable mybi)
			{
				myblocks = mybi;
			}

			#region MouseOver/Out Functions

			void label_MouseEnter(object sender, EventArgs e)
			{
				BackColor = Color.LightGray;
			}

			void label_MouseLeave(object sender, EventArgs e)
			{
				BackColor = Color.White;
			}

			#endregion

			#region MouseClick Functions

			void label_MouseDown(object sender, MouseEventArgs e)
			{
				if (myblocks.ContainsKey(Target) == false)
				{
					// Add the request to the fix queue
				    AttributeValuePair avp = new AttributeValuePair
				                             {
				                                 Attribute = "incident_id",
				                                 Value = Target
				                             };
				    Node n = new Node(FixQueue, fixlabel, "", avp);
				}
				else
				{
					BlockedItem bi = (BlockedItem)myblocks[Target];
					string reason = "Action Failed";
					if (bi != null)
					{
						reason = bi.reason;
					}
					MessageBox.Show(TopLevelControl, reason, "Action Failed");
				}

				TheInstance.ShowFullPanel = false;	
				TheInstance.Hide();
			}

			void cancel_MouseDown(object sender, MouseEventArgs e)
			{
				TheInstance.ShowFullPanel = false;	
				TheInstance.Hide();
			}

			#endregion
		}
		
		/// <summary>
		/// Singleton instance of the tooltip
		/// </summary>
		public static readonly ItilToolTip_Quad TheInstance = new ItilToolTip_Quad();

	    QuadStatusLozenge _parent;
	    QuadStatusLozenge_ESM _parent_ESM;
	    bool isESM = SkinningDefs.TheInstance.GetBoolData("esm_sim", false);

	    Timer timer;

	    Panel basePanel;
	    Label text_label;

	    ToolTipMenuItem_Quad fix, fix_by_consult, workaround, cancel, first_line_fix;
	    string fontname = SkinningDefs.TheInstance.GetData("fontname");
	    float fontsize = SkinningDefs.TheInstance.GetFloatData("lozenge_menu_item_font_size", 7);

	    bool servicename_translate = true;

		readonly int width = SkinningDefs.TheInstance.GetIntData("tool_tip_width", 130);
	    SizeF tooltipWidth;
		public Hashtable blocks = new Hashtable();

		int DoLayout ()
		{
            ResizeTitle();

			int y = text_label.Bottom + 4;

			fix.Location = new Point(2, y);
			y += 20;

			if (fix_by_consult != null)
			{
				fix_by_consult.Location = new Point(2, y);
				y += 20;
			}

            if (workaround != null)
            {
                if (isESM)
                {
                    if ((_parent_ESM != null) && _parent_ESM.canWorkAround)
                    {
                        workaround.Location = new Point(2, y);
                        workaround.Show();
                        y += 20;
                    }
                    else
                    {
                        workaround.Hide();
                    }
                }
                else
                {
                    if ((_parent != null) && _parent.canWorkAround)
                    {
                        workaround.Location = new Point(2, y);
                        workaround.Show();
                        y += 20;
                    }
                    else
                    {
                        workaround.Hide();
                    }
                }
            }

            if (SkinningDefs.TheInstance.GetBoolData("allow_first_line_fix", true))
            {
                if (first_line_fix != null)
                {
                    if (((isESM)?(_parent_ESM != null):(_parent != null)))
                    {
                        first_line_fix.Location = new Point(2, y);
                        first_line_fix.Show();
                        y += 20;
                    }
                    else
                    {
                        first_line_fix.Hide();
                    }
                }
            }

			cancel.Location = new Point(2, y);
			y += 20;

			return y + 2;
		}

		public bool ShowFullPanel
		{
			get { return _showAll; }
			set 
			{
			    if (value)
			    {
			        int height = DoLayout();

			        basePanel.Size = new Size(width, height);
			        Size = new Size(width, height);
			    }
			    else
			    {
			        var height = ResizeTitle();

			        basePanel.Size = new Size(width, height + (text_label.Top * 2));
			        Size = new Size(width, height + (text_label.Top * 2));
			    }

			    _showAll = value; 
			}
		}

	    bool _showAll = true;

        int ResizeTitle ()
        {
            using (var graphics = CreateGraphics())
            {
                int height = (int) graphics.MeasureString(text_label.Text, text_label.Font, text_label.Width).Height;
                text_label.Height = height;

                return height;
            }
        }

        /// <summary>
		/// Tooltip over status lozenge
		/// </summary>
		public ItilToolTip_Quad()
		{
			bool allowWorkarounds = (SkinningDefs.TheInstance.GetIntData("allow_workarounds", 1) == 1);
			bool allowConsultancy = (SkinningDefs.TheInstance.GetIntData("allow_consultancy", 1) == 1);

			Setup(allowWorkarounds, allowConsultancy);
		}

		public ItilToolTip_Quad(bool allow_workaround, bool allow_consultancy)
		{
			Setup(allow_workaround, allow_consultancy);
		}

		#region Blocking Activity

		public void SetBlock(string target_id, string reason)
		{
		    BlockedItem bi = new BlockedItem
		                     {
		                         target = target_id,
		                         fix_allowed = false,
		                         fixconsult_allowed = false,
		                         workaround_allowed = false,
		                         reason = reason
		                     };
		    blocks.Add(target_id, bi);
		}

		#endregion Blocking Activity

		protected void Setup(bool allow_workaround, bool allow_consultancy)
		{
			ShowInTaskbar = false;
			FormBorderStyle = FormBorderStyle.None;
			BackColor = Color.White;
			Visible = false;

		    timer = new Timer { Interval = 200 };
		    timer.Tick += timer_Tick;

		    basePanel = new Panel
		                {
		                    BorderStyle = BorderStyle.FixedSingle,
		                    Size = new Size(Width, 90)
		                };
		    Controls.Add(basePanel);

		    text_label = new Label
		                 {
		                     Size = new Size((basePanel.Width), 15),
		                     Location = new Point(19, 5),
		                     Font = ConstantSizeFont.NewFont(TextTranslator.TheInstance.GetTranslateFont(fontname),
		                                                     TextTranslator.TheInstance.GetTranslateFontSize(fontname,
		                                                     SkinningDefs.TheInstance.GetFloatData("lozenge_menu_title_font_size", 7)),
		                                                     FontStyle.Bold)
		                 };
		    text_label.Click += text_label_Click;
			basePanel.Controls.Add(text_label);
            
            tooltipWidth = new Size(SkinningDefs.TheInstance.GetIntData("tool_tip_width", 130), 0);

		    fix = new ToolTipMenuItem_Quad("Fix", "/images/lozenges/server_edit.png", "fix")
		          {
		              Name = "Fix Tool Tip Menu (Quad)"
		          };
		    fix.setMyBlocks(blocks);
			basePanel.Controls.Add(fix);

			if (allow_consultancy)
			{
			    fix_by_consult = new ToolTipMenuItem_Quad("Fix By Consultancy", "/images/lozenges/server_lightning.png", "fix by consultancy")
                                 {
                                     Name = "Fix By Consultancy Tool Tip Menu (Quad)"
                                 };
			    fix_by_consult.setMyBlocks(blocks);
				basePanel.Controls.Add(fix_by_consult);
			}

			if (allow_workaround)
			{
			    workaround = new ToolTipMenuItem_Quad("Workaround", "/images/lozenges/arrow_rotate_clockwise.png", "workaround")
			                 {
			                     Name = "WorkAround Tool Tip Menu (Quad)"
			                 };
			    workaround.setMyBlocks(blocks);
				basePanel.Controls.Add(workaround);
			}

            if(SkinningDefs.TheInstance.GetBoolData("allow_first_line_fix", true))
            {
                first_line_fix = new ToolTipMenuItem_Quad("First Line Fix", "/images/lozenges/1stline_fix.png", "first_line_fix")
                                 {
                                     Name = "First Line Fix Tool Tip Menu (Quad)"
                                 };
                first_line_fix.setMyBlocks(blocks);
                basePanel.Controls.Add(first_line_fix);
            }

		    cancel = new ToolTipMenuItem_Quad("Close Menu", "/images/lozenges/cancel.png")
		             {
		                 Name = "Cancel Tool Tip Menu (Quad)"
		             };
		    basePanel.Controls.Add(cancel);

			basePanel.MouseLeave += basePanel_MouseLeave;
			basePanel.MouseDown  += basePanel_MouseDown;

			DoLayout();

			ShowFullPanel = false;
		}

		public void toggleShow()
		{
		    if (isESM)
		    {
                if (_parent_ESM.fixable || _parent_ESM.canWorkAround)
                {
                    ShowFullPanel = !ShowFullPanel;
                    //Focus();
                }
		    }
		    else
		    {
		        if (_parent.fixable || _parent.canWorkAround)
		        {
		            ShowFullPanel = !ShowFullPanel;
		            //Focus();
		        }
		    }
		}

        public void ShowToolTip_Quad(QuadStatusLozenge_ESM parent, Node fix, string name, string target, int x, int y)
        {
            SetWindowPos(Handle, (IntPtr)HWND_TOPMOST, 0, 0, 0, 0, TOPMOST_FLAGS);

            _parent_ESM = parent;

            this.fix.Target = target;
            if (fix_by_consult != null)
            {
                fix_by_consult.Target = target;
                fix_by_consult.FixQueue = fix;
            }
            if (workaround != null)
            {
                workaround.Target = target;
                workaround.FixQueue = fix;
            }
            if (SkinningDefs.TheInstance.GetBoolData("allow_first_line_fix", true))
            {
                if (first_line_fix != null)
                {
                    first_line_fix.Target = target;
                    first_line_fix.FixQueue = fix;
                }
            }
            this.fix.FixQueue = fix;

            // We need to translate the Business Names 
            text_label.Text = name.Replace("&", "&&");
            if (servicename_translate)
            {
                text_label.Text = TextTranslator.TheInstance.Translate(name).Replace("&", "&&");

                string RequiredFontname = TextTranslator.TheInstance.GetTranslateFont(fontname);

                if (TextTranslator.TheInstance.areTranslationsLoaded() == false)
                {
                    RequiredFontname = SkinningDefs.TheInstance.GetData("fontname");
                }

                if (fontname != RequiredFontname)
                {
                    text_label.Font = ConstantSizeFont.NewFont(
                        TextTranslator.TheInstance.GetTranslateFont(RequiredFontname),
                        TextTranslator.TheInstance.GetTranslateFontSize(RequiredFontname, fontsize),
                        FontStyle.Bold);

                    fontname = RequiredFontname;
                }
            }

            Show();

            Location = new Point(x, y);
            ShowFullPanel = ((_parent_ESM.fixable || _parent_ESM.canWorkAround) && ShowFullPanel);

            using (Graphics graphics = Graphics.FromImage(new Bitmap(1, 1)))
            {
                tooltipWidth = graphics.MeasureString(text_label.Text, text_label.Font);
            }

            Width = width;
            text_label.Left = 5;
            text_label.Width = Width;
            text_label.TextAlign = ContentAlignment.MiddleLeft;

            DoLayout();

            timer.Stop();
        }

		public void ShowToolTip_Quad(QuadStatusLozenge parent, Node fix, string name, string target, int x, int y)
		{
			SetWindowPos(Handle, (IntPtr)HWND_TOPMOST, 0, 0, 0, 0, TOPMOST_FLAGS);

			_parent = parent;

			this.fix.Target = target;
			if (fix_by_consult != null)
			{
				fix_by_consult.Target = target;
				fix_by_consult.FixQueue = fix;
			}
			if (workaround != null)
			{
				workaround.Target = target;
				workaround.FixQueue = fix;
			}
            if (SkinningDefs.TheInstance.GetBoolData("allow_first_line_fix", true))
            {
                if (first_line_fix != null)
                {
                    first_line_fix.Target = target;
                    first_line_fix.FixQueue = fix;
                }
            }
			this.fix.FixQueue = fix;

			// We need to translate the Business Names 
			text_label.Text = name.Replace("&", "&&");
			if (servicename_translate)
			{
				text_label.Text = TextTranslator.TheInstance.Translate(name).Replace("&", "&&");

				string RequiredFontname = TextTranslator.TheInstance.GetTranslateFont(fontname);
 
				if (TextTranslator.TheInstance.areTranslationsLoaded() == false)
				{
					RequiredFontname = SkinningDefs.TheInstance.GetData("fontname");
				}

				if (fontname!= RequiredFontname)
				{
					text_label.Font = ConstantSizeFont.NewFont(
						TextTranslator.TheInstance.GetTranslateFont(RequiredFontname),
						TextTranslator.TheInstance.GetTranslateFontSize(RequiredFontname,fontsize),
						FontStyle.Bold);

					fontname = RequiredFontname;
				}
			}

			Show();

			ShowFullPanel = ((_parent.fixable || _parent.canWorkAround) && ShowFullPanel);
            
            using (Graphics graphics = Graphics.FromImage(new Bitmap(1, 1)))
            {
                tooltipWidth = graphics.MeasureString(text_label.Text, text_label.Font);
            }

            Width = width;
			text_label.Left = 5;
			text_label.Width = Width;
            text_label.TextAlign = ContentAlignment.MiddleLeft;

			var location = parent.PointToScreen(new Point(x, y));
			var screen = Screen.FromPoint(location);

			Location = new Point(Maths.Clamp(location.X, screen.Bounds.Left, screen.Bounds.Right - Width),
								Maths.Clamp(location.Y, screen.Bounds.Top, screen.Bounds.Bottom - Height));

			DoLayout();

			timer.Stop();
		}

		public void HideToolTip()
		{
			timer.Start();
		}

	    void timer_Tick(object sender, EventArgs e)
        {
            // if the mouse is outwith the panel, hide it, otherwise it was a false
            // mouse out ( mousing from panel -> this, or mouseover child of this )
            if (!Bounds.Contains(MousePosition))
            {
                Hide();
                timer.Stop();
                ShowFullPanel = false;
            }
        }

	    void basePanel_MouseLeave(object sender, EventArgs e)
		{
			HideToolTip();
		}

	    void text_label_Click(object sender, EventArgs e)
		{
			toggleShow();
		}

	    void basePanel_MouseDown(object sender, MouseEventArgs e)
		{
			toggleShow();
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);

			DoSize();
		}

		void DoSize ()
		{
			foreach (Control control in Controls)
			{
				if (control is Label)
				{
					control.Width = Width - 10 - control.Left;
				}

                foreach (Control innerControl in control.Controls)
                {
                    if (!(innerControl is Label))
                    {
                        innerControl.Left += 4;
                    }
                }
			}
		}
	}
}