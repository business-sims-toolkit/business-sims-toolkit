using System;
using System.Drawing;
using System.Windows.Forms;
using IncidentManagement;
using LibCore;
using Network;
using CommonGUI;
using CoreUtils;

namespace TransitionScreens
{
	/// <summary>
	/// Summary description for SLA_Item.
	/// </summary>
	public class SLA_Item : MonitorItem, iTabPressed
	{
		Font font;
		Font infoFont;
		Font entryBoxFont;
		Image backGraphic;
		Node monitoredItem;
		EntryBox entry;
		int LimitNumber = 9;
		string oldtext;
		Color TextColor = Color.Black;
		bool auto_translate = true;

		/// <summary>
		/// Allow the user to get / set SLA limit
		/// </summary>
		/// <param name="nt"></param>
		/// <param name="n"></param>
		public SLA_Item(NodeTree nt, Node n, Font displayFontBold8, Font InfoFontBold8, Font entryBoxFontNormal10)
		{
			TextColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("sla_editor_textforecolor", Color.Black);

			font = displayFontBold8;
			infoFont = InfoFontBold8;
			entryBoxFont = entryBoxFontNormal10;

			SetStyle(ControlStyles.Selectable, true);
			//this.font = ConstantSizeFont.NewFont("Verdana", 8f, FontStyle.Bold);
			//this.infoFont = ConstantSizeFont.NewFont("Verdana", 12f, FontStyle.Bold);

			//
			backGraphic = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\servicelozenges\\long_lozenge_green.png");
			backGraphic = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\servicelozenges\\SLA_lozenge.png");
			
			monitoredItem = n;
			//
			BackColor = Color.White;

			desc = n.GetAttribute("desc");
			if(auto_translate)
			{
				desc = TextTranslator.TheInstance.Translate(desc);
			}

			desc = Strings.RemoveHiddenText(desc);

			entry = new EntryBox();
			entry.Size = new Size(15,15);
			entry.Font = entryBoxFont;
			entry.Location = new Point(112,4);
			entry.BorderStyle = BorderStyle.None;
			entry.Text = ""+(SLAManager.get_SLA(n) / 60);
			oldtext = entry.Text;
			entry.DigitsOnly = true;
			entry.numChars = 1;
			//entry.CharToIgnore('7');
			//entry.CharToIgnore('8');
			//entry.CharToIgnore('9');
			entry.CharToIgnore('0');

			SuspendLayout();
			Controls.Add(entry);
			ResumeLayout(false);

			entry.tabPressed += entry_tabPressed;

			padTop = 2;

			GotFocus += SLA_Item_GotFocus;
			LostFocus += SLA_Item_LostFocus;

			entry.TextChanged += entry_TextChanged;
		}

		public void ChangeTextBoxOffsetLeft(int offset) 
		{
			entry.Left = offset;
		}

		public void ChangeTextBoxOffset (int x, int y)
		{
			entry.Left = x;
			entry.Top = y;
		}

		/// <summary>
		/// Override paint
		/// </summary>
		/// <param name="e"></param>
		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			Render(g);
		}

		/// <summary>
		/// Render the status monitor item.
		/// </summary>
		/// <param name="g"></param>
		public void Render(Graphics g)
		{
			RectangleF textRect = new RectangleF(padLeft, padTop+1, Size.Width - padLeft - padRight - 36, Size.Height - padTop - padBottom);
			Rectangle test =      new Rectangle(padLeft, padTop-1, Size.Width - padLeft - padRight - 36, Size.Height - padTop - padBottom - 2);
			StringFormat sf = new StringFormat(StringFormatFlags.NoClip);
			sf.Trimming = StringTrimming.EllipsisCharacter;

			g.DrawImage(backGraphic,0,0,Width-1, Height-1);
			//g.FillRectangle(new SolidBrush(Color.FromArgb(218,218,218)),test);
			//g.DrawString(desc, font, new SolidBrush(Color.Black), textRect, sf);
			g.DrawString(desc, font, new SolidBrush(TextColor), test, sf);
		}

#if false
		/// <summary>
		/// override ...
		/// </summary>
		/// <param name="keyData"></param>
		/// <returns></returns>
		protected override bool ProcessDialogKey(Keys keyData)
		{
			/*
			if(keyData == Keys.D1)
				entry.Text = "1";
			else if(keyData == Keys.D2)
				entry.Text = "2";
			else if(keyData == Keys.D3)
				entry.Text = "3";
			else if(keyData == Keys.D4)
				entry.Text = "4";
			else if(keyData == Keys.D5)
				entry.Text = "5";
			else if(keyData == Keys.D6)
				entry.Text = "6";
				//			else if(keyData == Keys.D7)
				//				entry.Text = "7";
				//			else if(keyData == Keys.D8)
				//				entry.Text = "8";
				//			else if(keyData == Keys.D9)
				//				entry.Text = "9";

			else*/ if(keyData == Keys.Delete || keyData == Keys.Back)
			{
				entry.Text = "";
			}

			else if(keyData == Keys.Tab)
			{
				if(null != tabPressed)
				{
					tabPressed(this);
				}
				//((DefineSLA)this.Parent).NextFocus(this);
			}

			if(entry.Text != "")
			{
				monitoredItem.SetAttribute("slalimit",""+(Convert.ToInt32(entry.Text) * 60));
			}

			//entry.SelectAll();

			return true;
		}
#endif

		void SLA_Item_GotFocus(object sender, EventArgs e)
		{
			//this.entry.BorderStyle = BorderStyle.FixedSingle;
			//this.entry.SelectAll();
			entry.Focus();
		}

		void SLA_Item_LostFocus(object sender, EventArgs e)
		{
			//this.entry.BorderStyle = BorderStyle.Fixed3D;
		}
		#region iTabPressed Members

		public event cTabPressed.TabEventArgsHandler tabPressed;

		#endregion

		void entry_tabPressed(Control sender)
		{
			if(tabPressed != null)
			{
				tabPressed(this);
			}
		}

		void entry_TextChanged(object sender, EventArgs e)
		{
			if(entry.Text != "")
			{
				try
				{
					int ln = CONVERT.ParseInt(entry.Text);
					if (ln>LimitNumber)
					{
						entry.Text = oldtext;
					}
					else
					{
						monitoredItem.SetAttribute("slalimit",""+(Convert.ToInt32(entry.Text) * 60));
						oldtext = entry.Text;
					}
				}
				catch (Exception)
				{
					entry.Text = oldtext;
				}
			}
		}
	}
}