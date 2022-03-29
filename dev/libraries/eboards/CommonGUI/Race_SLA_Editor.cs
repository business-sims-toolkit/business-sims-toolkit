using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;
using LibCore;
using Network;
using CoreUtils;

namespace CommonGUI
{
	/// <summary>
	/// Summary description for Race_SLA_Editor
	/// </summary>
	public class Race_SLA_Editor : FlickerFreePanel
	{
		protected ArrayList CurrentServices = new ArrayList();
		protected OpsControlPanelBase _tcp;
		protected NodeTree _tree;
		protected Node TransactionsTargetNode = null;

		protected int smi_columns = 4;
		protected int smi_width = 132;
		protected int smi_height = 31;
		protected int smi_textEntryOffset = 112;
		protected int smi_textEntryYOffset = 4;

		protected Label header;
		protected ImageTextButton okButton;
		protected ImageTextButton cancelButton;
		protected Label TransTargetTitle;
		protected FilteredTextBox tbTransTargetValue;
		Label transTargetError;
		protected ArrayList ctrls = new ArrayList();

		protected string filename_huge = "\\images\\buttons\\blank_huge.png";
		protected string filename_long = "\\images\\buttons\\blank_big.png";
		protected string filename_mid = "\\images\\buttons\\blank_med.png";
		protected string filename_short = "\\images\\buttons\\blank_small.png";
		protected string filename_ok = "\\images\\buttons\\ok_blank_small.png";

		protected FocusJumper focusJumper;

		protected Color MyOperationsBackColor;
		protected Color MyTitleForeColor;
		protected Font MyDefaultSkinFontBold12 = null;
		protected Font MyDefaultSkinFontBold11 = null;
		protected Font MyDefaultSkinFontBold10 = null;
		protected Font MyDefaultSkinFontBold9 = null;
		protected Font MyDefaultSkinFontBold8 = null;
		protected Font MyDefaultSkinFontNormal10 = null;
		Boolean MyIsTrainingMode = false;
		protected bool useSMIAlignment = false;
		string oldtext;
		
		protected int xoffset = 13;
		protected int yoffset = 21;
		protected bool auto_translate = true;
		protected bool displayTargetTransactionsSystem = true;
		protected Color displayTargetTrans_ForeColor = Color.White;
		protected Color displayTargetTrans_BackColor = Color.Black;
		protected int round;

		int maxTransactionsPossible;

		/// <summary>
		/// Set the service level agreement for projects 
		/// </summary>
		/// <param name="tcp"></param>
		/// <param name="tree"></param>
		public Race_SLA_Editor(OpsControlPanelBase tcp, NodeTree tree, Boolean IsTrainingMode, Color OperationsBackColor, int round)
		{
			TransactionsTargetNode = tree.GetNamedNode("TransactionsTarget");
			this.round = round;

			//Override smi size and layout if defined in skin file
			smi_columns = SkinningDefs.TheInstance.GetIntData("sla_lozenge_race_cols",smi_columns);
			smi_width = SkinningDefs.TheInstance.GetIntData("sla_lozenge_race_width",smi_width);
			smi_height = SkinningDefs.TheInstance.GetIntData("sla_lozenge_race_height",smi_height);
			smi_textEntryOffset = SkinningDefs.TheInstance.GetIntData("sla_lozenge_race_offentry",smi_textEntryOffset);

			displayTargetTransactionsSystem = SkinningDefs.TheInstance.GetBoolData("use_target_transactions_system", false);
			displayTargetTrans_ForeColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("use_target_transactions_forecolor", Color.White);
			displayTargetTrans_BackColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("use_target_transactions_backcolor", Color.Black);

			MyOperationsBackColor = OperationsBackColor;

			smi_textEntryOffset = SkinningDefs.TheInstance.GetIntData("smi_text_entry_x_offset", smi_textEntryOffset);
			smi_textEntryYOffset = SkinningDefs.TheInstance.GetIntData("smi_text_entry_y_offset", smi_textEntryYOffset);


			//Is there an overriding Title Foreground colour
			string racetitlecolour =  SkinningDefs.TheInstance.GetData("race_paneltitleforecolor");
			if (racetitlecolour != "")
			{
				MyTitleForeColor =  SkinningDefs.TheInstance.GetColorData("race_paneltitleforecolor");
			}

			string useSMIAlignmentStr =  SkinningDefs.TheInstance.GetData("race_smi_alignment","false");
			if (useSMIAlignmentStr.Equals("true"))
			{
				useSMIAlignment = true;
			}

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

			//Determine the training Mode and hence the Background Image
			MyIsTrainingMode = IsTrainingMode;
			if (MyIsTrainingMode)
			{
				BackgroundImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location +
					"\\images\\panels\\race_panel_back_training.png");
			}
			else
			{
				BackgroundImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location +
					"\\images\\panels\\race_panel_back_normal.png");
			}

			SuspendLayout();

			SetStyle(ControlStyles.Selectable, true);
			focusJumper = new FocusJumper();

			BackColor = MyOperationsBackColor;
			ForeColor = Color.Black;

			//Debug 
			//MyOperationsBackColor = Color.Violet;
			BackColor = MyOperationsBackColor;

			_tcp = tcp;
			_tree = tree;

			ctrls.Clear();

			header = new Label();
			header.Text = TextTranslator.TheInstance.Translate("Set MTRS for Services");
			header.Size = new Size(300,20);
			header.Font = MyDefaultSkinFontBold12;
			header.Location = new Point(5,5);
			header.BackColor = Color.Transparent;
			header.ForeColor = MyTitleForeColor;
			Controls.Add(header);

			ctrls.Add(header);

			Color upColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_text_colour", Color.Black);
			Color downColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_down_text_colour", Color.White);
			Color hoverColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_hover_text_colour", Color.Green);
			Color disabledColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_disabled_text_colour", Color.DarkGray);

			cancelButton = new ImageTextButton(0);
			cancelButton.ButtonFont = MyDefaultSkinFontBold10;
			cancelButton.SetVariants(filename_short);
			cancelButton.Size = new Size(80, 20);
			cancelButton.Location = SkinningDefs.TheInstance.GetPointData("ops_popup_cancel_button_position", 512, 185);
			cancelButton.SetButtonText("Close",upColor,upColor, hoverColor,disabledColor);
			cancelButton.Click += cancelButton_Click;
			cancelButton.Visible = true;
			Controls.Add(cancelButton);

			if (displayTargetTransactionsSystem)
			{
				TransTargetTitle = new Label();
				TransTargetTitle.Font = MyDefaultSkinFontBold10;
				TransTargetTitle.Text = "Target Number of " + Strings.SentenceCase(SkinningDefs.TheInstance.GetData("transactionname"));
				TransTargetTitle.Size = TransTargetTitle.GetPreferredSize(Size.Empty);
				TransTargetTitle.BringToFront();
				Controls.Add(TransTargetTitle);
				ctrls.Add(TransTargetTitle);

				int cv = TransactionsTargetNode.GetIntAttribute(string.Format("round_{0}_target", round), 0);

				tbTransTargetValue = new FilteredTextBox(TextBoxFilterType.Digits);
				oldtext = tbTransTargetValue.Text;
				tbTransTargetValue.ForeColor = displayTargetTrans_ForeColor;
				tbTransTargetValue.BackColor = displayTargetTrans_BackColor;
				tbTransTargetValue.Font = MyDefaultSkinFontBold10;
				tbTransTargetValue.Size = new Size(40, 20);
				tbTransTargetValue.Text = CONVERT.ToStr(cv);
				tbTransTargetValue.MaxLength = 2;
				tbTransTargetValue.TextAlign = HorizontalAlignment.Center;
				Controls.Add(tbTransTargetValue);
				tbTransTargetValue.TextChanged += tbTransTargetValue_TextChanged;

				ctrls.Add(tbTransTargetValue);
				focusJumper.Add(tbTransTargetValue);

				Node transactions = _tree.GetNamedNode("Transactions");
				maxTransactionsPossible = transactions.GetIntAttribute("count_max", 0);

				transTargetError = new Label();
				transTargetError.Font = MyDefaultSkinFontBold10;
				transTargetError.Size = new Size(150, 25);
				transTargetError.ForeColor = Color.Red;
				transTargetError.Text = CONVERT.Format("(Maximum {0})", maxTransactionsPossible);
				Controls.Add(transTargetError);
				ctrls.Add(transTargetError);
				UpdateErrorText();
				transTargetError.BringToFront();
			}
			ctrls.Add(cancelButton);
			focusJumper.Add(cancelButton);

			ResumeLayout(false);
          
			LoadDataDisplay();
			DoLayout();
			GotFocus += DefineSLA_GotFocus;

			Resize += Race_SLA_Editor_Resize;
		}

		void tbTransTargetValue_TextChanged(object sender, EventArgs e)
		{
			SetExternalStateFromText(tbTransTargetValue);

			UpdateErrorText();
		}

		void UpdateErrorText ()
		{
			transTargetError.Visible = (CONVERT.ParseIntSafe(tbTransTargetValue.Text, 0) > maxTransactionsPossible);
		}

		protected virtual void SetExternalStateFromText(FilteredTextBox entry)
		{
			if (TransactionsTargetNode != null)
			{
				string attributeName = CONVERT.Format("round_{0}_target", round);
				int value = CONVERT.ParseIntSafe(tbTransTargetValue.Text, 0);

				if (TransactionsTargetNode.GetIntAttribute(attributeName, 0) != value)
				{
					TransactionsTargetNode.SetAttribute(attributeName, value);
				}
			}
		}

		protected void cancelButton_Click(object sender, EventArgs e)
		{
			_tcp.DisposeEntryPanel();
		}

		protected void okButton_Click(object sender, EventArgs e)
		{
			if (TransactionsTargetNode != null)
			{
				int current_target = TransactionsTargetNode.GetIntAttribute(CONVERT.Format("round_{0}_target", round), 0);
				int new_value = CONVERT.ParseIntSafe(tbTransTargetValue.Text, -1);
				if (new_value != -1)
				{
					TransactionsTargetNode.SetAttribute(CONVERT.Format("round_{0}_target", round), new_value);

					_tcp.DisposeEntryPanel();
				}
				else
				{
					MessageBox.Show(TopLevelControl, "Target value incorrect", "Error");
				}
			}
			else
			{
				_tcp.DisposeEntryPanel();
			}
		}

		protected virtual Race_SLA_Item CreateItem (Node node)
		{
			return new Race_SLA_Item (_tree, node,
			                          MyDefaultSkinFontBold8, MyDefaultSkinFontBold8,
			                          MyDefaultSkinFontNormal10);
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

		public void LoadDataDisplay()
		{
			SuspendLayout();

			focusJumper.Dispose();
			focusJumper = new FocusJumper();

			CurrentServices.Clear();

			foreach(Control c in Controls)
			{
				if (ctrls.Contains(c) == false)
				{
					if (c != null)
					{
						c.Dispose();
					}
				}


				//if (displayTargetTransactionsSystem)
				//{
				//  if ((c != okButton) && (c != cancelButton) && (c != header))
				//  {
				//    if (c != null) c.Dispose();
				//  }
				//}
				//else
				//{
				//  if ((c != cancelButton) && (c != header))
				//  {
				//    if (c != null) c.Dispose();
				//  }
				//}
			}
			Controls.Clear();

			foreach(Control c in ctrls)
			{
				Controls.Add(c);
				focusJumper.Add(c);
			}

			//this.Controls.Add(cancelButton);
			//focusJumper.Add(cancelButton);

			//if (displayTargetTransactionsSystem)
			//{
			//  this.Controls.Add(okButton);
			//  focusJumper.Add(okButton);
			//}
			//this.Controls.Add(header);

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
					Race_SLA_Item smi = CreateItem(n);
					smi.BackColor = MyOperationsBackColor;
					smi.Size = new Size(smi_width,smi_height);
					smi.ChangeTextBoxOffsetLeft(smi_textEntryOffset, smi_textEntryYOffset);
					smi.setAlignToEntryBox(useSMIAlignment);
					Controls.Add(smi);
					CurrentServices.Add(smi);
			
					focusJumper.Add(smi);
				}
			}

			ResumeLayout(false);
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

		public void DoLayout()
		{
			//double numPerColumn = ((double)CurrentServices.Count) / ((double)this.columns);
			//numPerColumn = System.Math.Ceiling(numPerColumn);
			//
			int colCount = 0;
			int Vertical_Gap = 1;
			int Horizontal_Gap = 1;
			int start_x = 15;
			int start_y = 35;
			int xoffset = start_x;
			int yoffset = start_y;
			
			if (displayTargetTransactionsSystem)
			{
				int y_offset = SkinningDefs.TheInstance.GetIntData("MTRS_title_y_offset", 25);
				start_x = 20;
				start_y = y_offset;
				xoffset = start_x;
				yoffset = start_y;
			}

			//
			foreach(Race_SLA_Item smi in CurrentServices)
			{
				smi.Location = new Point(xoffset,yoffset);
				++colCount;
				if(colCount == smi_columns)
				{
					xoffset = start_x;
					yoffset += smi.Height+Vertical_Gap;
					colCount = 0;
				}
				else
				{
					xoffset += smi.Width + Horizontal_Gap;
				}
			}
		}
	
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
				Race_SLA_Item item = (Race_SLA_Item) CurrentServices[0];
				item.Focus();
			}
			else
			{
				cancelButton.Focus();
			}
		}

		protected void Race_SLA_Editor_Resize(object sender, EventArgs e)
		{
			if (string.IsNullOrEmpty(SkinningDefs.TheInstance.GetData("ops_popup_cancel_button_position")))
			{
				if (cancelButton != null)
				{
					int cancelOffset = SkinningDefs.TheInstance.GetIntData("cancel_button_x_offset", 8);
					cancelButton.Location = new Point(Width - cancelButton.Width - cancelOffset,
					                                  Height - cancelButton.Height - 8);
				}
				if (displayTargetTransactionsSystem)
				{
					if (okButton != null)
					{
						okButton.Location = new Point(cancelButton.Left - 10 - okButton.Width, Height - cancelButton.Height - 8);
					}
					if (TransTargetTitle != null)
					{
						int transLabelOffset = SkinningDefs.TheInstance.GetIntData("trans_target_title_x_offset", 10);
						TransTargetTitle.Location = new Point(transLabelOffset, Height - cancelButton.Height - 8);
						if (tbTransTargetValue != null)
						{
							tbTransTargetValue.Location = new Point(TransTargetTitle.Right, Height - cancelButton.Height - 12);
							transTargetError.Location = new Point(tbTransTargetValue.Right + 10, TransTargetTitle.Top);
						}
					}
				}
			}
		}
	}
}