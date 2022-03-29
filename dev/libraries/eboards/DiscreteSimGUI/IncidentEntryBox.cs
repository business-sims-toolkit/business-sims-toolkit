using System;
using System.Drawing;
using System.Windows.Forms;

using CoreUtils;
using CommonGUI;
using IncidentManagement;
using Network;

using StyledImageButton = ResizingUi.Button.StyledImageButton;

namespace DiscreteSimGUI
{
	/// <summary>
	/// Summary description for IncidentEntryBox.
	/// </summary>
	public class IncidentEntryBox : Panel, ITimedClass
	{
		protected EntryBox textBox1;

		bool alwaysEnabled;

		// HACK!!!! 29-04-2007 - To stop windoze internally throwing a very expensive exception
		// when we disable the incident box we create a button that we can pass focus to and place it
		// off screen.
		Button hidden;
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		readonly System.ComponentModel.Container components = null;

		protected Node incidentEntryQueue;

		protected ImageButton enterIncident;
		protected ImageButton removeIncident;
		readonly int commonheight;
		readonly int commonwidth;
		int textentrywidth;
		readonly int controlSeparation;

		readonly Font font;
		readonly string enterLegend;
		readonly string removeLegend;
		readonly Color legendColour;
		readonly Color legendActiveColour;
		readonly Color legendHoverColour;
		readonly Color legendDisabledColour;

		bool allowAlpha;

		public IncidentApplier IncidentApplier;

		public Node IncidentEntryQueue
		{
			set => incidentEntryQueue = value;
		}

		public virtual string GetText()
		{
			return textBox1.Text;
		}

		public void Clear()
		{
			textBox1.Text = "";
		}

		public void SetFocus()
		{
			textBox1.Focus();
		}

		public new virtual void Enter()
		{
			if (enterIncident.Enabled)
			{
				var avp = new AttributeValuePair("id", textBox1.Text.ToUpper());
				new Node(incidentEntryQueue, "IncidentNumber", "", avp);
				Clear();
			}
		}

		public void AllowAlphaIncidents()
		{
			allowAlpha = true;
			textBox1.DigitsOnly = false;
		}

		public void SetEnabled(bool enabled)
		{
			if(enabled || alwaysEnabled)
			{

			    textBox1.Enabled = true;
				
				// Will flip the buttons on if they need to be...
				UpdateButtonStateFromTextBox();
			}
			else
			{

				hidden.Focus();
				textBox1.Enabled = false;

				enterIncident.Enabled = false;
				removeIncident.Enabled = false;
			}
		}

		public IncidentEntryBox (int newCommonHeight, int newCommonWidth, int newtextentrywidth, int newcontrolseperation)
			: this (newCommonHeight, newCommonWidth, newtextentrywidth, newcontrolseperation, null, null, null, Color.Black, Color.Black, Color.Black, Color.Black)
		{
		}

		public IncidentEntryBox(int newCommonHeight, int newCommonWidth, int newtextentrywidth, int newcontrolseperation, Font font, string enterLegend, string removeLegend, Color colour, Color activeColour, Color hoverColour, Color disabledColour)
		{
			commonheight = newCommonHeight;
			commonwidth = newCommonWidth;
			textentrywidth = newtextentrywidth;
			controlSeparation = newcontrolseperation;

			this.font = font;
			this.enterLegend = enterLegend;
			this.removeLegend = removeLegend;
			legendColour = colour;
			legendActiveColour = activeColour;
			legendHoverColour = hoverColour;
			legendDisabledColour = disabledColour;

			BaseBuild();
		}

		public IncidentEntryBox()
		{
			commonheight = SkinningDefs.TheInstance.GetIntData("incident_entry_box_common_height", 35);
            commonwidth = SkinningDefs.TheInstance.GetIntData("incident_entry_box_common_width", 75);
            textentrywidth = SkinningDefs.TheInstance.GetIntData("incident_entry_box_text_width", 50);
			controlSeparation = SkinningDefs.TheInstance.GetIntData("incident_entry_box_padding", 1);
			font = null;
			enterLegend = null;
			removeLegend = null;
			BaseBuild();
		}
		

		public void BaseBuild()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			textBox1.KeyPress += textBox1_KeyPress;
			textBox1.Location = new Point(0,0);
			textBox1.Size = new Size(textentrywidth,commonheight);
			textBox1.TextChanged += textBox1_TextChanged;
			Resize += IncidentEntryBox_Resize;

			var useStyledImageButton = SkinningDefs.TheInstance.GetBoolData("use_styled_image_button", false);

			if (enterLegend != null)
			{
				enterIncident = new ImageTextButton (0);
				((ImageTextButton) enterIncident).ButtonFont = font;
			}
			else
			{
				enterIncident = useStyledImageButton ? new StyledImageButton ("incident_image") : new ImageButton(0);
			}

			enterIncident.SetVariants("/images/buttons/enter.png");
			enterIncident.Size = new Size(commonwidth, commonheight);
			enterIncident.Location = new Point(textentrywidth + controlSeparation, 0);
			enterIncident.ButtonPressed += enterIncident_ButtonPressed;
			enterIncident.Name = "Enter Incident Button";
			enterIncident.Enabled = false;

			if (enterIncident is ImageTextButton button)
			{
				button.SetButtonText(enterLegend, legendColour, legendActiveColour, legendHoverColour, legendDisabledColour);
			}
			Controls.Add(enterIncident);

			if (removeLegend != null)
			{
				removeIncident = new ImageTextButton (0);
				((ImageTextButton) removeIncident).ButtonFont = font;
			}
			else
			{
				removeIncident = useStyledImageButton ? new StyledImageButton("incident_image") : new ImageButton(0);
			}
			removeIncident.SetVariants("/images/buttons/remove.png");
			removeIncident.Size = new Size(commonwidth,commonheight);
			removeIncident.Location = new Point(textentrywidth+ controlSeparation + commonwidth+ controlSeparation, 0);
			removeIncident.ButtonPressed += removeIncident_ButtonPressed;
			removeIncident.Name = "Remove Incident Button";
			removeIncident.Enabled = false;
			if (removeIncident is ImageTextButton textButton)
			{
				textButton.SetButtonText(removeLegend, legendColour, legendActiveColour, legendHoverColour, legendDisabledColour);
			}
			Controls.Add(removeIncident);

			hidden = new Button();
			hidden.Location = new Point(1000,1000);
			hidden.Name = "IEB Button";
			Controls.Add(hidden);

		    BackColor = Color.Orange;

			TimeManager.TheInstance.ManageClass(this);
		}

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				TimeManager.TheInstance.UnmanageClass(this);

				components?.Dispose();
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
			this.textBox1 = new EntryBox();
			this.SuspendLayout();
			// 
			// textBox1
			// 
			this.textBox1.Font = CoreUtils.SkinningDefs.TheInstance.GetFont(20.25F, System.Drawing.FontStyle.Bold);
			this.textBox1.Location = new System.Drawing.Point(10, 10);
			this.textBox1.Name = "textBox1";
			this.textBox1.Size = new System.Drawing.Size(60, 40);
			this.textBox1.TabIndex = 0;
			this.textBox1.MaxLength = 2;
			this.textBox1.DigitsOnly = true;
			this.textBox1.Text = "";
			this.textBox1.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
			// 
			// IncidentEntryBox
			// 
			this.Controls.Add(this.textBox1);
			this.Name = "IncidentEntryBox";
			this.Size = new System.Drawing.Size(80, 60);
			this.BackColor = Color.Transparent;
			this.ResumeLayout(false);

		}
		#endregion

		public int MaxDigits
		{
			get => textBox1.MaxLength;

			set => textBox1.MaxLength = value;
		}

		public void ReduceTextBox()
		{
			textentrywidth = 30;
			var offsetX = 2;
			var offsetY = 2;
			textBox1.Font = SkinningDefs.TheInstance.GetFont(16F, FontStyle.Bold);
			textBox1.Size = new Size(textentrywidth, commonheight);
			//this.textBox1.BackColor = Color.Yellow;
			textBox1.Location = new Point(offsetX, offsetY);
			removeIncident.Location = new Point(offsetX + textentrywidth+ controlSeparation + commonwidth+ controlSeparation, offsetY);
			enterIncident.Location = new Point(offsetX + textentrywidth+ controlSeparation, offsetY);
			Invalidate(); //Refresh();
		}

		public void SetTextEntryBorderFlat()
		{
			textBox1.BorderStyle = BorderStyle.None;
		    textBox1.AutoSize = false;
		    textBox1.Height = Height - textBox1.Top;
		    enterIncident.Height = Height - enterIncident.Top;
		    removeIncident.Height = Height - removeIncident.Top;
		}

		void textBox1_KeyPress(object sender, KeyPressEventArgs e)
		{
			if(e.KeyChar == 13)
			{
				if((null != incidentEntryQueue) && (textBox1.Text.Length > 0))
				{
					Enter();
				}
				//
				textBox1.Text = "";
			}
			else
			{
				// Remove any non-numeric entries...
				var text = textBox1.Text;
				var newText = "";
				foreach(var c in text)
				{
					var isCharDigit = char.IsDigit(c);
					var isCharLetter = char.IsLetter(c);

					if (allowAlpha)
					{
						if ((isCharDigit) | (isCharLetter))
						{
							newText += c;
						}
					}
					else
					{
						if (isCharDigit)
						{
							newText += c;
						}
					}
				}
				//
				textBox1.Text = newText;
			}
		}

		void IncidentEntryBox_Resize(object sender, EventArgs e)
		{
			//this.textBox1.Location = new Point(0,0);
			//this.textBox1.Size = this.Size;
		}

		void enterIncident_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			Enter();
		}

		void removeIncident_ButtonPressed(object sender, ImageButtonEventArgs args)
		{
			var fixItQueue = incidentEntryQueue.Tree.GetNamedNode("FixItQueue");

			var avp = new AttributeValuePair();
			avp.Attribute="incident_id";
			avp.Value=textBox1.Text.ToUpper();
			var n = new Node(fixItQueue,"entrypanel_fix","",avp);
			textBox1.Text = "";
		}

		public void ShowRemoveButton (bool enable)
		{
			removeIncident.Visible = enable;
		}

		protected virtual void UpdateButtonStateFromTextBox ()
		{
			var enable = (textBox1.Text.Length > 0);

			enterIncident.Enabled = enable;
			removeIncident.Enabled = enable;
		}

		void textBox1_TextChanged(object sender, EventArgs e)
		{
			UpdateButtonStateFromTextBox();
		}

		#region ITimedClass Members

		public void Start()
		{
			SetEnabled(true);
			textBox1.Focus();
			textBox1.SelectAll();
		}

		public void FastForward(double timesRealTime)
		{
			// TODO:  Add IncidentEntryBox.FastForward implementation
		}

		public void Reset()
		{
			SetEnabled(false);
		}

		public void Stop()
		{
			SetEnabled(false);
		}

		#endregion

	    public void SetAutoButtonSizes (int gap)
	    {
            enterIncident.SetAutoSize();
	        removeIncident.Left = enterIncident.Right + gap;
            removeIncident.SetAutoSize();
	        Width = removeIncident.Right + gap;
	    }

		public bool AlwaysEnabled
		{
			get => alwaysEnabled;

			set
			{
				alwaysEnabled = value;
				SetEnabled(true);
			}
		}
	}
}