using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

using LibCore;
using CoreUtils;

namespace CommonGUI
{
	public class tabInfo
	{
		public string title = "";
		public bool enabled = true;
		/// <summary>
		/// This is will display as if it's not enabled, but will still be enabled and be clickable.
		/// </summary>
		public bool greyedOut = false;

		public bool Visible = true;
		public int code = 0;
		public int width = 0;
	}

	public class TabBarEventArgs : EventArgs
	{
		protected int code;

		public TabBarEventArgs(int tabCode)
		{
			code = tabCode;
		}

		public int Code
		{
			get { return code; }
		}
	}

	public class TabBar : BasePanel
	{
		// Store an array of panel titles.
		Dictionary<string, tabInfo> nameToTab = new Dictionary<string, tabInfo> ();
		List<tabInfo> tabs = new List<tabInfo> ();

		Font font;
		Font highlightFont;

		Image tabCentral, tabCentralSelected;
		Image tabLeftEnd, tabLeftEndSelected;
		Image tabRightEnd, tabRightEndSelected;
		Image tabRightOver, tabRightOverSelected;
		Image tabLeftOver, tabLeftOverSelected;

		int selectedTab = 0;
		int highlightTab = -1;
        Brush TextNormalBrush = Brushes.Black;
	    Brush TextHighlightBrush = Brushes.SteelBlue;
        Brush TextSelectedNormalBrush = Brushes.Black;
	    Brush TextSelectedHighlightBrush = Brushes.SteelBlue;
	    Brush TextDisabledBrush;

		public delegate void TabBarEventArgsHandler(object sender, TabBarEventArgs args);
		public event TabBarEventArgsHandler TabPressed;

        public bool UseAlternateTabDrawingMethod = false;

		bool useUpperCase;

		public TabBar (bool useBold = false, bool useUpperCase = false)
		{
			this.useUpperCase = useUpperCase;

			font = SkinningDefs.TheInstance.GetFont(SkinningDefs.TheInstance.GetFloatData("tab_bar_font_size", 10), useBold ? FontStyle.Bold : FontStyle.Regular);
			highlightFont = SkinningDefs.TheInstance.GetFont(SkinningDefs.TheInstance.GetFloatData("tab_bar_font_size", 10), FontStyle.Bold);

			string dir = AppInfo.TheInstance.Location;
			tabCentral = Repository.TheInstance.GetImage(dir + "\\images\\tabs\\tab_central.png",Color.Blue);
			tabLeftEnd = Repository.TheInstance.GetImage(dir + "\\images\\tabs\\tab_left_end.png",Color.Blue);
			tabRightEnd = Repository.TheInstance.GetImage(dir + "\\images\\tabs\\tab_right_end.png",Color.Blue);
			tabRightOver = Repository.TheInstance.GetImage(dir + "\\images\\tabs\\tab_right_over.png",Color.Blue);
			tabLeftOver = Repository.TheInstance.GetImage(dir + "\\images\\tabs\\tab_left_over.png",Color.Blue);

			if (tabLeftOver == null)
			{
				tabLeftOver = JoinImages(tabRightEnd, tabLeftEnd);
			}

			if (tabRightOver == null)
			{
				tabRightOver = JoinImages(tabRightEnd, tabLeftEnd);
			}

			// Load up the selected variants, defaulting to the unselected ones if not found.
			tabCentralSelected = Repository.TheInstance.GetImage(dir + "\\images\\tabs\\tab_central_selected.png",Color.Blue);
			tabLeftEndSelected = Repository.TheInstance.GetImage(dir + "\\images\\tabs\\tab_left_end_selected.png",Color.Blue);
			tabRightEndSelected = Repository.TheInstance.GetImage(dir + "\\images\\tabs\\tab_right_end_selected.png",Color.Blue);
			tabRightOverSelected = Repository.TheInstance.GetImage(dir + "\\images\\tabs\\tab_right_over_selected.png",Color.Blue);
			tabLeftOverSelected = Repository.TheInstance.GetImage(dir + "\\images\\tabs\\tab_left_over_selected.png",Color.Blue);
			if (tabCentralSelected == null)
			{
				tabCentralSelected = tabCentral;
			}
			if (tabLeftEndSelected == null)
			{
				tabLeftEndSelected = tabLeftEnd;
			}

			if (tabLeftOverSelected == null)
			{
				if (tabLeftEndSelected != null)
				{
					tabLeftOverSelected = JoinImages(tabRightEnd, tabLeftEndSelected);
				}
				else
				{
					tabLeftOverSelected = tabLeftOver;
				}
			}

			if (tabRightEndSelected == null)
			{
				tabRightEndSelected = tabRightEnd;
			}

			if (tabRightOverSelected == null)
			{
				if (tabRightEndSelected != null)
				{
					tabRightOverSelected = JoinImages(tabRightEndSelected, tabLeftEnd);
				}
				else
				{
					tabRightOverSelected = tabRightOver;
				}
			}

			SetStyle(ControlStyles.AllPaintingInWmPaint |	ControlStyles.UserPaint | ControlStyles.DoubleBuffer,true);

			Color textNormalColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("tab_text_normal_color", SkinningDefs.TheInstance.GetColorData("generated_tab_text_normal_color"));
			Color textHighlightColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("tab_text_highlight_color", SkinningDefs.TheInstance.GetColorData("generated_tab_text_highlight_color"));
			Color textSelectedNormalColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("tab_text_selected_normal_color", SkinningDefs.TheInstance.GetColorData("generated_tab_text_selected_normal_color"));
			Color textSelectedHighlightColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("tab_text_selected_highlight_color", SkinningDefs.TheInstance.GetColorData("generated_tab_text_selected_highlight_color"));
			
			TextNormalBrush = new SolidBrush(textNormalColor);
			TextHighlightBrush = new SolidBrush(textHighlightColor);
			TextSelectedNormalBrush = new SolidBrush(textSelectedNormalColor);
			TextSelectedHighlightBrush = new SolidBrush(textSelectedHighlightColor);
			TextDisabledBrush = new SolidBrush (Color.FromArgb(50, textNormalColor));

			Resize += TabBar_Resize;
			MouseUp += TabBar_MouseUp;
			MouseLeave += TabBar_MouseLeave;
			MouseMove += TabBar_MouseMove;
		}

		Image JoinImages (Image left, Image right)
		{
			if ((left == null) && (right == null))
			{
				return null;
			}

			Image joined = new Bitmap ((left?.Width ?? 0) + (right?.Width ?? 0), Math.Max(left?.Height ?? 0, right?.Height ?? 0));

			using (Graphics graphics = Graphics.FromImage(joined))
			{
				if (left != null)
				{
					graphics.DrawImageUnscaled(left, 0, 0);
				}

				if (right != null)
				{
					graphics.DrawImageUnscaled(right, left?.Width ?? 0, 0);
				}
			}

			return joined;
		}

        protected override void Dispose(bool disposing)
        {

            if (disposing)
            {
                TextNormalBrush.Dispose();
                TextHighlightBrush.Dispose();
                TextSelectedNormalBrush.Dispose();
                TextSelectedHighlightBrush.Dispose();
                TextDisabledBrush.Dispose(); 
            }

            base.Dispose(disposing);
        }

		public int SelectedTab
		{
			get { return selectedTab; }
			set
			{
				selectedTab = value;
				if (TabPressed != null)
				{
					tabInfo tab = null;
					if ((selectedTab >= 0) && (selectedTab < tabs.Count))
					{
						tab = (tabInfo) tabs[selectedTab];
					}

					TabPressed(this, new TabBarEventArgs((tab != null) ? tab.code : -1));
				}
				Invalidate();
			}
		}

		public int SelectedTabCode
		{
			get
			{
				return (tabs[selectedTab] as tabInfo).code;
			}

			set
			{
				foreach (tabInfo tab in tabs)
				{
					if (tab.code == value)
					{
						SelectedTab = tabs.IndexOf(tab);
						return;
					}
				}

				SelectedTab = 0;
			}
		}

		public void ClearTabs()
		{
			nameToTab.Clear();
			tabs.Clear();
			Invalidate();
		}

		public void RemoveTab (string title)
		{
			RemoveTab(title, true);
		}

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);

            Invalidate();
        }

		public void RemoveTab (string title, bool selectAnotherTab)
		{
			tabInfo previousSelectedTab = (tabInfo) tabs[selectedTab];

			if (nameToTab.ContainsKey(title))
			{
				tabInfo tab = nameToTab[title] as tabInfo;

				if (SelectedTabCode == tab.code)
				{
					// Highlight the first tab...
					int other = 0;
					// ...unless that's us, in which case highlight the next.
					if (tabs.IndexOf(tab) == 0)
					{
						other = Math.Min(tabs.Count - 1, 1 + tabs.IndexOf(tab));
					}

					if (selectAnotherTab)
					{
						SelectedTab = other;
					}

					nameToTab.Remove(title);
					tabs.Remove(tab);
				}
				else
				{
					nameToTab.Remove(title);
					tabs.Remove(tab);

					for (int i = 0; i < tabs.Count; i++)
					{
						if (tabs[i] == previousSelectedTab)
						{
							SelectedTab = i;
							break;
						}
					}
				}
			}
		}

		public void RemoveTabByCode (int code)
		{
			string title = "";

			foreach (tabInfo tab in tabs)
			{
				if (tab.code == code)
				{
					title = tab.title;
				}
			}

			if (title != "")
			{
				RemoveTab(title);
			}
		}

		public void TabEnabled(string title, bool enabled)
		{
			if(nameToTab.ContainsKey(title))
			{
				tabInfo ti = (tabInfo) nameToTab[title];
				ti.enabled = enabled;

				if ((ti == tabs[selectedTab]) && ! enabled)
				{
					selectedTab = -1;

					foreach (tabInfo otherTab in tabs)
					{
						if ((otherTab != ti) && (otherTab.enabled))
						{
							selectedTab = tabs.IndexOf(otherTab);
							break;
						}
					}
				}

				Invalidate();
			}
		}

		public void SetTabGreyedOut(string title, bool greyedOut)
		{
			if (nameToTab.ContainsKey(title))
			{
				tabInfo ti = (tabInfo)nameToTab[title];
				ti.greyedOut = greyedOut;
				Invalidate();
			}
		}

		public void SetTabTitle(int code, string title)
		{
			foreach(tabInfo t in tabs)
			{
				if(t.code == code)
				{
					nameToTab.Remove(t.title);
					t.title = title;
					nameToTab[title] = t;
					Invalidate();
					return;
				}
			}

			throw new Exception (CONVERT.Format("Tab code {0} not found!", code));
		}

		public void AddTabAtStart(string title, int code, bool enabled, bool greyedOut = false)
		{
			tabInfo t = new tabInfo();
			t.title = title;
			t.enabled = enabled;
			t.greyedOut = greyedOut;
			t.code = code;
			if (!nameToTab.ContainsKey(title))
			{
				tabs.Insert(0, t);
				nameToTab[title] = t;
			}
			Invalidate();
		}

		public void AddTab (string title, int code)
		{
			AddTab(title, code, true, false);
		}

		public void AddTab(string title, int code, bool enabled)
		{
			AddTab(title, code, enabled, false);
		}

		public void AddTab(string title, int code, bool enabled, bool greyedOut)
		{
			tabInfo t = new tabInfo();
			t.title = title;
			t.enabled = enabled;
			t.greyedOut = greyedOut;
			t.code = code;
			if(!nameToTab.ContainsKey(title))
			{
				tabs.Add(t);
				nameToTab[title] = t;
			}
			Invalidate();
		}

        void RenderTabsTest(Graphics g)
        {
            int gapWidth = 3;
            int tabCount = tabs.Count;
            int totalGapsWidth = ((tabCount + 1) * gapWidth);
            int tabWidth = (Width - totalGapsWidth) / tabCount;

            Image normalRightSelectedLeft = JoinImages(tabRightOver, tabLeftEnd);
            Image selectedRightNormalLeft = JoinImages(tabRightEnd, tabLeftOver);
            
            int x = 0;
            int y = 0;

            List<Point> tabTextLocations = new List<Point>();
            for (int pass = 0; pass < 2; pass++)
            {
                int currentTabIndex = 0;

                foreach (tabInfo ti in tabs)
                {
                    bool isSelected = currentTabIndex == selectedTab;
                    bool isOneBeforeSelected = (currentTabIndex + 1) == selectedTab;
                    bool isLeftMost = currentTabIndex == 0;
                    bool isRightMost = currentTabIndex == (tabCount - 1);

                    bool drawPass = pass == 0;
                    bool textPass = pass == 1;

                    Debug.Assert(drawPass ^ textPass);

                    if (drawPass)
                    {
                        
                        int leftWidth = gapWidth;
                        Image rightSide = tabRightOver;
                        int rightWidth = gapWidth;
                        if (isOneBeforeSelected)
                        {
                            rightSide = normalRightSelectedLeft;
                            rightWidth = (2 * gapWidth);
                        }
                        else if (isSelected)
                        {
                            rightSide = selectedRightNormalLeft;
                            rightWidth = (2 * gapWidth);
                        }
                        
                        Image centre = (isSelected) ? tabCentralSelected : tabCentral;

                        int widthOfTab = gapWidth;
                        if (isLeftMost)
                        {
                            Image leftSide = (isSelected) ? tabLeftEnd : tabLeftOver;
                            g.DrawImage(leftSide, x, y, leftWidth, Height);
                            x += gapWidth;
                        }
                        
                        g.DrawImage(centre, new Rectangle(x, y, tabWidth + gapWidth, Height));

                        tabTextLocations.Add(new Point(x, y));

                        x += tabWidth;
                        widthOfTab += tabWidth;

                        g.DrawImage(rightSide, x, y, rightWidth, Height);
                        widthOfTab += gapWidth;

                        x += rightWidth;

                        ti.width = widthOfTab;
                    }
                    
                    if (textPass)
                    {
                        Font textFont;
                        Brush textBrush;

                        bool isHighlighted = ti.enabled && currentTabIndex == highlightTab;

                        if (isHighlighted)
                        {
                            textFont = (isSelected) ? font : highlightFont;
                            textBrush = (isSelected) ? TextSelectedHighlightBrush : TextHighlightBrush;
                        }
                        else
                        {
                            bool isDisabled = (!ti.enabled || ti.greyedOut);

                            textFont = (isSelected) ? highlightFont : font;
                            textBrush = (isSelected)
                                ? TextSelectedNormalBrush
                                : (isDisabled) ? TextDisabledBrush : TextNormalBrush;

                        }

	                    var text = ti.title;
	                    if (useUpperCase)
	                    {
		                    text = text.ToUpper();
						}

						g.DrawString(text, textFont, textBrush,
                            new Rectangle(tabTextLocations[currentTabIndex], new Size(tabWidth, Height)),
                            new StringFormat
                            {
                                LineAlignment = StringAlignment.Center,
                                Alignment = StringAlignment.Center
                            });
                    }

                    currentTabIndex++;

                    Debug.Assert(currentTabIndex <= tabCount);
                }
            }
        }

		protected override void OnPaint(PaintEventArgs e)
		{
			if (SkinningDefs.TheInstance.GetBoolData("tabs_drawn_in_code", false))
			{
				RenderInCode(e.Graphics);
				return;
			}

			e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;

			if ((null != tabLeftEnd) && (null != tabCentral) && (null != tabRightEnd) && (null != tabRightOver)
				&& (null != tabLeftOver) && (tabs.Count > 0))
			{
                
                if (UseAlternateTabDrawingMethod)
                {
                    using (Brush backBrush = new SolidBrush(BackColor))
                    {
                        e.Graphics.FillRectangle(backBrush, new Rectangle(0, 0, Width, Height));
                    }

                    RenderTabsTest(e.Graphics);
                }
                else
                {
                    float width = Width - tabLeftEnd.Width - tabRightEnd.Width - tabRightOver.Width;
                    width -= (tabs.Count(t => t.Visible) - 1) * tabLeftOver.Width;
                    width /= tabs.Count(t => t.Visible);

                    // Because of the annoying way that the tabs' right edges are drawn as part of the next tab,
                    // we draw in two passes, first tabs, then text, so that text doesn't get cut off by the edges.
                    for (int pass = 0; pass < 2; pass++)
                    {
                        bool drawTabs = (pass == 0);
                        bool drawText = (pass == 1);

                        float offset = 0;
                        int count = 0;

                        tabInfo lastTab = null;

                        foreach (tabInfo t in tabs)
                        {
	                        if (! t.Visible)
	                        {
								continue;
	                        }

                            if (count > 0)
                            {
                                if (selectedTab < count)
                                {
                                    Image rightOver = tabRightOver;
                                    if (selectedTab == (count - 1))
                                    {
                                        rightOver = tabRightOverSelected;
                                    }

                                    if (drawTabs)
                                    {
                                        e.Graphics.DrawImageUnscaled(rightOver, (int) (0.5 + offset), 0);
                                    }

                                    offset += rightOver.Width;
                                    t.width = rightOver.Width / 2;
                                    lastTab.width += rightOver.Width / 2;
                                }
                                else
                                {
                                    Image leftOver = tabLeftOver;
                                    if (selectedTab == count)
                                    {
                                        leftOver = tabLeftOverSelected;
                                    }

                                    if (drawTabs)
                                    {
                                        e.Graphics.DrawImageUnscaled(leftOver, (int) (0.5 + offset), 0);
                                    }

                                    offset += leftOver.Width;
                                    t.width = leftOver.Width / 2;
                                    lastTab.width += leftOver.Width / 2;
                                }
                            }
                            else
                            {
                                Image leftEnd = tabLeftEnd;
                                if (selectedTab == count)
                                {
                                    leftEnd = tabLeftEndSelected;
                                }

                                t.width = leftEnd.Width;

                                if (drawTabs)
                                {
                                    e.Graphics.DrawImageUnscaled(leftEnd, 0, 0);
                                }
                                offset += leftEnd.Width;
                            }

                            Image central = tabCentral;
                            if (t.enabled
                                && (selectedTab == count))
                            {
                                central = tabCentralSelected;
                            }
                            // : Had typo where x0+width was being used instead of width,
                            // leading to a glitch at right-hand end of tab.  Fixed this but
                            // kept a slight fudge factor of 4 pixels to cover up gaps between
                            // the graphics.
                            if (drawTabs)
                            {
                                e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
                                float right = offset + width + 4;

                                if (SkinningDefs.TheInstance.GetBoolData("tabs_can_be_replicated_not_stretched", false))
                                {
                                    for (float x = offset; x < right; x += tabCentral.Width)
                                    {
                                        e.Graphics.DrawImageUnscaledAndClipped(central, new Rectangle ((int) (0.5 + x), 0, (int) (0.5 + Math.Min(tabCentral.Width, right - x)), (int) (0.5 + tabCentral.Height)));
                                    }
                                }
                                else
                                {
                                    e.Graphics.DrawImage(central, offset, 0, right - offset, tabCentral.Height);
                                }
                            }

                            SizeF s = e.Graphics.MeasureString(t.title, font);
                            SizeF s2 = e.Graphics.MeasureString(t.title, highlightFont);

                            if (drawText)
                            {
                                if (t.enabled && highlightTab == count)
                                {
                                    if (selectedTab == count)
                                    {
                                        e.Graphics.DrawString(t.title, font, TextSelectedHighlightBrush, (int) (0.5 + offset + width / 2 - s.Width / 2), 4);
                                    }
                                    else
                                    {
                                        e.Graphics.DrawString(t.title, highlightFont, TextHighlightBrush, (int) (0.5 + offset + width / 2 - s2.Width / 2), 4);
                                    }
                                }
                                else
                                {
                                    if (selectedTab == count)
                                    {
                                        e.Graphics.DrawString(t.title, highlightFont, TextSelectedNormalBrush, (int) (0.5 + offset + width / 2 - s2.Width / 2), 4);
                                    }
                                    else if (t.enabled && !t.greyedOut)
                                    {
                                        e.Graphics.DrawString(t.title, font, TextNormalBrush, (int) (0.5 + offset + width / 2 - s.Width / 2), 4);
                                    }
                                    else
                                    {
                                        e.Graphics.DrawString(t.title, font, TextDisabledBrush, (int) (0.5 + offset + width / 2 - s.Width / 2), 4);
                                    }
                                }
                            }

                            offset += width;
                            t.width += (int) (0.5 + width);

                            //offset += width;
                            ++count;
                            lastTab = t;
                        }

                        Image rightEnd = tabRightEnd;
                        if (selectedTab == (count - 1))
                        {
                            rightEnd = tabRightEndSelected;
                        }
                        if (drawTabs)
                        {
                            e.Graphics.DrawImageUnscaled(rightEnd, (int) (0.5 + offset), 0);
                        }
                    }
                }
			}
		}

		void RenderInCode (Graphics graphics)
		{
			using (Brush backBrush = new SolidBrush (BackColor))
			{
				graphics.FillRectangle(backBrush, new Rectangle (0, 0, Width, Height));
			}

			float x = 0;
			float tabWidth = Width / (float) tabs.Count(t => t.Visible);
			for (int i = 0; i < tabs.Count; i++)
			{
				var tab = tabs[i];

				if (! tab.Visible)
				{
					continue;
				}

				var tabBounds = new RectangleF (x, 0, tabWidth, Height);
				tab.width = (int) tabBounds.Width;
				x += tabWidth;

				var bold = ((highlightTab == i) || (selectedTab == i));
				var fontToUse = (bold ? highlightFont : font);

				Color backColour;
				Color foreColour;
				if (highlightTab == i)
				{
					if (selectedTab == i)
					{
						backColour = SkinningDefs.TheInstance.GetColorData("tab_selected_hover_background_colour");
						foreColour = SkinningDefs.TheInstance.GetColorData("tab_selected_hover_foreground_colour");
					}
					else
					{
						backColour = SkinningDefs.TheInstance.GetColorData("tab_hover_background_colour");
						foreColour = SkinningDefs.TheInstance.GetColorData("tab_hover_foreground_colour");
					}
				}
				else
				{
					if (selectedTab == i)
					{
						backColour = SkinningDefs.TheInstance.GetColorData("tab_selected_background_colour");
						foreColour = SkinningDefs.TheInstance.GetColorData("tab_selected_foreground_colour");
					}
					else
					{
						backColour = SkinningDefs.TheInstance.GetColorData("tab_normal_background_colour");
						foreColour = SkinningDefs.TheInstance.GetColorData("tab_normal_foreground_colour");
					}
				}

				using (var brush = new SolidBrush (backColour))
				{
					graphics.FillRectangle(brush, tabBounds);
				}

				using (var brush = new SolidBrush (foreColour))
				{
					var text = tab.title;
					if (useUpperCase)
					{
						text = text.ToUpper();
					}

					graphics.DrawString(text, fontToUse, brush, tabBounds, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
				}
			}
		}

		void TabBar_Resize(object sender, EventArgs e)
		{
			Invalidate();
		}

		void TabBar_MouseUp(object sender, MouseEventArgs e)
		{
			if(Capture)
			{
				int offset = 0;
				int count = 0;

				foreach(tabInfo t in tabs)
				{
					if (t.enabled
					    && (e.X >= offset)
					    && (e.X < offset+t.width))
					{
						SelectedTab = count;
						return;
					}

					offset += t.width;
					++count;
				}
			}
		}

		void TabBar_MouseLeave(object sender, EventArgs e)
		{
			highlightTab = -1;
			Invalidate();
		}

		void TabBar_MouseMove(object sender, MouseEventArgs e)
		{
			highlightTab = -1;
			int offset = 0;
			int count = 0;

			foreach(tabInfo t in tabs)
			{
				if (t.enabled
					&& (e.X >= offset)
					&& (e.X < offset + t.width))
				{
					highlightTab = count;
					Invalidate();
					return;
				}

				offset += t.width;
				++count;
			}
		}

		public int GetTabCount ()
		{
			return tabs.Count;
		}

		public string SelectedTabName
		{
			get
			{
				return (tabs[selectedTab] as tabInfo).title;
			}

			set
			{
				foreach (tabInfo tab in tabs)
				{
					if (tab.title == value)
					{
						SelectedTab = tabs.IndexOf(tab);
						return;
					}
				}

				SelectedTab = 0;
			}
		}

		public IList<tabInfo> GetAllTabs ()
		{
			var list = new List<tabInfo> ();

			foreach (tabInfo tab in tabs)
			{
				list.Add(tab);
			}

			return list;
		}

		public tabInfo GetTabByCode (int code)
		{
			return tabs.Single(t => t.code == code);
		}

		public tabInfo GetTabByTitle (string title)
		{
			return tabs.Single(t => t.title == title);
		}

		public void SetTabVisibility (string title, bool visible)
		{
			GetTabByTitle(title).Visible = visible;
			Invalidate();
		}
	}
}