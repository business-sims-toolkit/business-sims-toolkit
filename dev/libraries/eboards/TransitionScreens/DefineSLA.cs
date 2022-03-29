using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;

using CommonGUI;
using LibCore;
using Network;
using CoreUtils;

namespace TransitionScreens
{
	/// <summary>
	/// Summary description for DefineSLA.
	/// </summary>
	public class DefineSLA : BaseSLAPanel
	{
		protected ArrayList CurrentServices = new ArrayList();
		protected TransitionControlPanel _tcp;
		protected NodeTree _tree;
		protected int smi_columns = 4;
		protected int smi_width = 132;
		protected int smi_height = 31;
		protected int smi_textEntryOffset = 112;
		protected int smi_textEntryYOffset = 4;

		protected ImageTextButton cancelButton;
		protected Label header;
		protected Label transTargetLabel;
		Label transTargetError;
		protected FilteredTextBox transTargetTextBox;

		protected string filename_long = "\\images\\buttons\\blank_big.png";
		protected string filename_mid = "\\images\\buttons\\blank_med.png";
		protected string filename_short = "\\images\\buttons\\blank_small.png";
		protected string filename_ok = "\\images\\buttons\\OK_blank_small.png";

		protected FocusJumper focusJumper;

		protected Color MyOperationsBackColor;
		protected Font MyDefaultSkinFontBold12 = null;
		protected Font MyDefaultSkinFontBold11 = null;
		protected Font MyDefaultSkinFontBold10 = null;
		protected Font MyDefaultSkinFontBold9 = null;
		protected Font MyDefaultSkinFontBold8 = null;
		protected Font MyDefaultSkinFontNormal10 = null;
		protected bool auto_translate = true;

		protected bool displayTargetTransactionsSystem = false;
		protected Node transactionsTargetNode;
		protected string oldTransactionTargetText;
		protected int round;

		int maxTransactionsPossible;

		/// <summary>
		/// Set the service level agreement for projects 
		/// </summary>
		/// <param name="tcp"></param>
		/// <param name="tree"></param>
		public DefineSLA(TransitionControlPanel tcp, NodeTree tree, Color OperationsBackColor, int round)
		{
			MyOperationsBackColor = OperationsBackColor;
			this.round = round;

			smi_textEntryOffset = SkinningDefs.TheInstance.GetIntData("smi_text_entry_x_offset", smi_textEntryOffset);
			smi_textEntryYOffset = SkinningDefs.TheInstance.GetIntData("smi_text_entry_y_offset", smi_textEntryYOffset);

			displayTargetTransactionsSystem = SkinningDefs.TheInstance.GetBoolData("use_target_transactions_system", false);
			transactionsTargetNode = tree.GetNamedNode("TransactionsTarget");

			string fontname =  SkinningDefs.TheInstance.GetData("fontname");
			if (auto_translate)
			{
				fontname = TextTranslator.TheInstance.GetTranslateFont(fontname);
			}
			MyDefaultSkinFontNormal10 = ConstantSizeFont.NewFont(fontname,10);
			MyDefaultSkinFontBold8 = ConstantSizeFont.NewFont(fontname,8,FontStyle.Bold);
			MyDefaultSkinFontBold9 = ConstantSizeFont.NewFont(fontname,9,FontStyle.Bold);
			MyDefaultSkinFontBold10 = ConstantSizeFont.NewFont(fontname,10,FontStyle.Bold);
			MyDefaultSkinFontBold11 = ConstantSizeFont.NewFont(fontname,11,FontStyle.Bold);
			MyDefaultSkinFontBold12 = ConstantSizeFont.NewFont(fontname,12,FontStyle.Bold);

			SuspendLayout();

			SetStyle(ControlStyles.Selectable, true);
			focusJumper = new FocusJumper();

			BackColor = MyOperationsBackColor;
			ForeColor = Color.Black;
			BorderStyle = BorderStyle.None;//.Fixed3D;

			_tcp = tcp;
			_tree = tree;

			BuildScreenControls();

			focusJumper.Add(cancelButton);

			ResumeLayout(false);
          
			LoadDataDisplay();
			DoLayout();
			GotFocus += DefineSLA_GotFocus;
		}

		public virtual void BuildScreenControls()
		{
			Color upColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_text_colour", Color.Black);
			Color downColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_down_text_colour", Color.White);
			Color hoverColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_hover_text_colour", Color.Green);
			Color disabledColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_disabled_text_colour", Color.DarkGray);

			header = new Label();
			header.Text = TextTranslator.TheInstance.Translate("Set MTRS for Services");
			header.Size = new Size(300, 25);
			header.Font = MyDefaultSkinFontBold12;
			header.Location = new Point(5, 5);
			Controls.Add(header);

			cancelButton = new ImageTextButton(0);
			cancelButton.ButtonFont = MyDefaultSkinFontBold9;
			cancelButton.SetVariants(filename_short);
			cancelButton.Size = new Size(80,20);
			cancelButton.Location = new Point(445,180);
			cancelButton.SetButtonText("Close",
				upColor,upColor,
				hoverColor,disabledColor);
			cancelButton.Click += cancelButton_Click;
			cancelButton.Visible = true;
			Controls.Add(cancelButton);
		}

		protected void cancelButton_Click(object sender, EventArgs e)
		{
			_tcp.DisposeEntryPanel();
		}

		class CompareNodesByPriority : IComparer
		{
			protected Hashtable nodeFromShortName;

			public CompareNodesByPriority (Hashtable nodeFromShortName)
			{
				this.nodeFromShortName = nodeFromShortName;
			}

			public int Compare (object x, object y)
			{
				string nameX = (string) x;
				string nameY = (string) y;

				Node nodeX = (Node) nodeFromShortName[nameX];
				Node nodeY = (Node) nodeFromShortName[nameY];

				int priorityX = nodeX.GetIntAttribute("priority", 0);
				int priorityY = nodeY.GetIntAttribute("priority", 0);

				int priorityCompare = 0;
                
				// If both nodes have a priority, the one with the lower number comes first.
				// If only one has a priority, it comes first.
				// If neither has a priority, they are equal.
				if ((priorityX != 0) && (priorityY != 0))
				{
					priorityCompare = priorityX - priorityY;
				}
				else if ((priorityX != 0) && (priorityY == 0))
				{
					priorityCompare = +1;
				}
				else if ((priorityX == 0) && (priorityY != 0))
				{
					priorityCompare = -1;
				}

				// If they are currently equal (ie have equal priorities, or both have none),
				// then fall back to sorting by name.
				if (priorityCompare == 0)
				{
					priorityCompare = nameX.CompareTo(nameY);
				}
				
				return priorityCompare;
			}
		}

		public override void LoadDataDisplay()
		{
			SuspendLayout();

			focusJumper.Dispose();
			focusJumper = new FocusJumper();

			CurrentServices.Clear();

			foreach(Control c in Controls)
			{
				if( (c != cancelButton) && (c != header))
				{
					if(c!=null) c.Dispose();
				}
			}
			Controls.Clear();

			Controls.Add(cancelButton);
			focusJumper.Add(cancelButton);

			Controls.Add(header);

			ArrayList types = new ArrayList();
			types.Add("biz_service");
			Hashtable services = _tree.GetNodesOfAttribTypes(types);

			ArrayList al = new ArrayList();
			Hashtable ht = new Hashtable();

			foreach(Node n in services.Keys)
			{

				bool active = (n.GetAttribute("retired") == null || n.GetAttribute("retired") != "true");
		
				if(active)
				{
					bool isupgrade = find(services,n.GetAttribute("biz_service_function"));

					if(!(isupgrade && n.getChildren().Count > 0))
					{
						// : Changed to order by display name, not full name (bug 3607).
						string nodename = n.GetAttribute("desc");
						al.Add(nodename);
						ht.Add(nodename,n);
					}
				}
			}
			al.Sort(new CompareNodesByPriority (ht));

			foreach (string nname in al)
			{
				Node n = (Node) ht[nname];
				if (n != null)
				{
					SLA_Item smi = new SLA_Item(_tree, n, MyDefaultSkinFontBold8, 
						MyDefaultSkinFontBold8, MyDefaultSkinFontNormal10);
					smi.Size = new Size(smi_width,smi_height);
					smi.ChangeTextBoxOffset(smi_textEntryOffset, smi_textEntryYOffset);
					smi.BackColor = MyOperationsBackColor;
					Controls.Add(smi);
					CurrentServices.Add(smi);
			
					focusJumper.Add(smi);
				}
			}

			if (displayTargetTransactionsSystem)
			{
				// todo get different length than for ops screen
				transTargetLabel = new Label();
				transTargetLabel.Text = TextTranslator.TheInstance.Translate("Target Number of " + Strings.SentenceCase(CONVERT.ToLower(SkinningDefs.TheInstance.GetData("transactionname"))));
				transTargetLabel.Font = MyDefaultSkinFontBold10;
				transTargetLabel.Size = transTargetLabel.GetPreferredSize(Size.Empty);
				transTargetLabel.Location = new Point(5, cancelButton.Top);
				Controls.Add(transTargetLabel);

				int transactionsTargetValue = transactionsTargetNode.GetIntAttribute(string.Format("round_{0}_target", round), 0);

				transTargetTextBox = new FilteredTextBox(TextBoxFilterType.Digits);
				transTargetTextBox.Size = new Size(40, 20);
				transTargetTextBox.Location = new Point(transTargetLabel.Right, transTargetLabel.Top - 4);
				transTargetTextBox.Font = MyDefaultSkinFontBold10;
				transTargetTextBox.MaxLength = 2;
				transTargetTextBox.Text = CONVERT.ToStr(transactionsTargetValue);
				transTargetTextBox.TextAlign = HorizontalAlignment.Center;
				transTargetTextBox.TextChanged += transTargetTextBox_TextChanged;
				Controls.Add(transTargetTextBox);

				Node transactions = _tree.GetNamedNode("Transactions");
				maxTransactionsPossible = transactions.GetIntAttribute("count_max", 0);

				transTargetError = new Label ();
				transTargetError.Font = MyDefaultSkinFontBold10;
				transTargetError.Size = new Size (150, 25);
				transTargetError.Location = new Point (transTargetTextBox.Right + 10, cancelButton.Top);
				transTargetError.ForeColor = Color.Red;
				transTargetError.Text = CONVERT.Format("(Maximum {0})", maxTransactionsPossible);
				Controls.Add(transTargetError);

				UpdateErrorText();

				oldTransactionTargetText = transTargetTextBox.Text;
				focusJumper.Add(transTargetTextBox);
			}

			ResumeLayout(false);
		}

		void transTargetTextBox_TextChanged(object sender, EventArgs e)
		{
			if (transTargetTextBox.Text != "")
			{
				int newValue = CONVERT.ParseIntSafe(transTargetTextBox.Text, -1);

				if (newValue == -1)
				{
					transTargetTextBox.Text = oldTransactionTargetText;
				}
				else
				{
					oldTransactionTargetText = transTargetTextBox.Text;
					ChangeTransactionTarget(newValue);
				}
			}

			UpdateErrorText();
		}

		void UpdateErrorText ()
		{
			transTargetError.Visible = (CONVERT.ParseIntSafe(transTargetTextBox.Text, 0) > maxTransactionsPossible);
		}

		protected void ChangeTransactionTarget (int value)
		{
			if (transactionsTargetNode != null)
			{
				string attributeName = CONVERT.Format("round_{0}_target", round);

				if (transactionsTargetNode.GetIntAttribute(attributeName, 0) != value)
				{
					transactionsTargetNode.SetAttribute(attributeName, value);
				}
			}
		}

		protected bool find(Hashtable list, string bizsf)
		{
			bool foundonce = false;
			foreach(Node n in list.Keys)
			{
				if( n.GetAttribute("biz_service_function") == bizsf
					&&	n.GetAttribute("retired") != "true")
				{
					if(foundonce)
					{
						return true;
					}
					else
					{
						foundonce = true;
					}
				}
			}
			return false;
		}

		public override void DoLayout()
		{
			//double numPerColumn = ((double)CurrentServices.Count) / ((double)this.columns);
			//numPerColumn = System.Math.Ceiling(numPerColumn);
			//
			int xoffset = 3;
			int yoffset = 30;
			int colCount = 0;
			//
			foreach(SLA_Item smi in CurrentServices)
			{
				smi.Location = new Point(xoffset,yoffset);
				++colCount;
				if(colCount == smi_columns)
				{
					xoffset = 3;
					yoffset += smi.Height+2;
					colCount = 0;
				}
				else
				{
					xoffset += smi.Width+2;
				}
			}
		}
/*
		/// <summary>
		/// Focus on the next sla item
		/// </summary>
		/// <param name="item"></param>
		public void NextFocus(SLA_Item item)
		{
			int found = 0;

			foreach(SLA_Item smi in CurrentServices)
			{
				if(smi == item)
					break;
				
				found++;
			}

			int per_col = Convert.ToInt32(System.Math.Ceiling(
				(double)CurrentServices.Count / this.columns));

			int index = found + per_col;

			if(index > (CurrentServices.Count - 1) && index < per_col * per_col)
				index = (found + (2 * per_col)) - ((per_col * per_col)-1);

			else if(index > (CurrentServices.Count - 1))
				index = (found + per_col) - ((per_col * per_col)-1);

			if(index < CurrentServices.Count && index > 0)
				((SLA_Item)CurrentServices[index]).Focus();
		}*/
	
		protected override void Dispose(bool disposing)
		{
			if( disposing )
			{
				focusJumper.Dispose();
			}
			base.Dispose (disposing);
		}

		protected void DefineSLA_GotFocus(object sender, EventArgs e)
		{
			if(CurrentServices.Count > 0)
			{
				SLA_Item item = (SLA_Item) CurrentServices[0];
				item.Focus();
			}
			else
			{
				cancelButton.Focus();
			}
		}

		protected override void OnResize (EventArgs e)
		{
			// Keep the close button inside the panel.
			if (cancelButton.Bottom >= Height)
			{
				cancelButton.Top = Height - cancelButton.Height - 3;

				// move trans target label and text box if required
				if (displayTargetTransactionsSystem)
				{
					transTargetLabel.Top = cancelButton.Top;
					transTargetTextBox.Top = cancelButton.Top;
				}
			}
		}		
	}
}
