using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;

using LibCore;
using Network;

using CoreUtils;
using IncidentManagement;

namespace CommonGUI
{
	/// <summary>
	/// Summary description for AddOrRemoveMirrorControl.
	/// </summary>
	public class TransAddOrRemoveMirrorControl : FlickerFreePanel
	{
		protected MirrorApplier _mirrorApplier;
		protected Node mirrorCommandQueueNode;

		protected IDataEntryControlHolder _mainPanel;

		protected string pickedLoc = "";
		protected string pickedServer = "";
		protected bool removePicked = false;

		protected ArrayList locButtons = new ArrayList();
		protected ArrayList serverButtons = new ArrayList();
		protected Panel confirm;
		protected Panel locations;
		protected ImageTextButton ok;
		protected ImageTextButton cancel;
		protected Label title;
		protected Label explain1;
		protected Label explain2;
		protected Label SelectedOptionLabel;

		protected string filename_long = "\\images\\buttons\\blank_big.png";
		protected string filename_mid = "\\images\\buttons\\blank_med.png";
		protected string filename_short = "\\images\\buttons\\blank_small.png";
		protected string filename_ok = "\\images\\buttons\\ok_blank_small.png";
		
		protected Color MyGroupPanelBackColor;
		protected Color MyOperationsBackColor;
		protected Font MyDefaultSkinFontNormal12 = null;
		protected Font MyDefaultSkinFontNormal11 = null;
		protected Font MyDefaultSkinFontNormal8 = null;
		protected Font MyDefaultSkinFontBold12 = null;
		protected Font MyDefaultSkinFontBold11 = null;
		protected Font MyDefaultSkinFontBold10 = null;
		protected Font MyDefaultSkinFontBold9 = null;

		protected string mirrorName = SkinningDefs.TheInstance.GetData("mirror_name", "Mirror");

		protected FocusJumper focusJumper;

		public TransAddOrRemoveMirrorControl(IDataEntryControlHolder mainPanel, NodeTree model, 
			MirrorApplier mirrorApplier, Color OperationsBackColor, Color GroupPanelBackColor)
		{
			focusJumper = new FocusJumper();

			//all transition panel 
			MyOperationsBackColor = OperationsBackColor;
			MyGroupPanelBackColor = GroupPanelBackColor;

			string fontname =  SkinningDefs.TheInstance.GetData("fontname");
			MyDefaultSkinFontNormal12 = ConstantSizeFont.NewFont(fontname,12);
			MyDefaultSkinFontNormal11 = ConstantSizeFont.NewFont(fontname,11);
			MyDefaultSkinFontNormal8 = ConstantSizeFont.NewFont(fontname,8);
			MyDefaultSkinFontBold12 = ConstantSizeFont.NewFont(fontname,12,FontStyle.Bold);
			MyDefaultSkinFontBold11 = ConstantSizeFont.NewFont(fontname,11,FontStyle.Bold);
			MyDefaultSkinFontBold9 = ConstantSizeFont.NewFont(fontname,9,FontStyle.Bold);

			BackColor = MyOperationsBackColor;
			BorderStyle = BorderStyle.None;//.Fixed3D;
			_mainPanel = mainPanel;
			_mirrorApplier = mirrorApplier;
			mirrorCommandQueueNode = model.GetNamedNode("MirrorCommandQueue");

			BuildScreenControls(model, mirrorApplier);

			GotFocus += TransAddOrRemoveMirrorControl_GotFocus;
		}

		public virtual void BuildScreenControls(NodeTree model, MirrorApplier mirrorApplier)
		{
			Color upColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_text_colour", Color.Black);
			Color downColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_down_text_colour", Color.White);
			Color hoverColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_hover_text_colour", Color.Green);
			Color disabledColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_disabled_text_colour", Color.DarkGray);

			//Create the Title 
			title = new Label();
			title.Font = MyDefaultSkinFontBold12;
			title.Text = mirrorName + " Options";
			title.TextAlign = ContentAlignment.MiddleLeft;
			title.Size = new Size(400,20);
			title.BackColor = MyOperationsBackColor;
            title.ForeColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("transition_text_colour", Color.Black);
            title.Location = new Point(10, 10);
			Controls.Add(title);		

			//
			// Create the Add/Remove mirror buttons.
			//
			int xoffset = 65;
			int yoffset = 35+10;

			foreach(MirrorOption option in mirrorApplier.Options)
			{
				string name = option.Target.GetAttribute("name");

				Panel p = new Panel();
				p.Size = new Size(200,100);
				p.Location = new Point(xoffset,yoffset);
				p.BackColor = MyGroupPanelBackColor;
				Controls.Add(p);

				Label l = new Label();
				l.Text = name;
				l.Font = MyDefaultSkinFontBold11;
				l.Location = new Point(5,5);
                l.ForeColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("transition_text_colour", Color.Black);
                p.Controls.Add(l);

				string text;
				if(null == model.GetNamedNode(name + "(M)"))
				{
					text = "Install " + mirrorName;
				}
				else
				{
					text = "Remove " + mirrorName;
				}
				ImageTextButton button = new StyledDynamicButtonCommon ("standard", text);
				button.Font = MyDefaultSkinFontBold9;
				button.Tag = option;
				button.Size = new Size(180,20);
				button.Location = new Point(10, 30);
				button.Click += button_Click;
				p.Controls.Add(button);

				serverButtons.Add(button);

				focusJumper.Add(button);

				xoffset += 210;
			}

			xoffset = 80;
			yoffset = 40;
			
			// 23-04-2007 : We are only allowed to put the mirror in particular places...
			locations = new Panel();
			locations.Size = new Size(420,110);
			locations.Location = new Point(65,45);
			locations.BackColor = MyGroupPanelBackColor;
			locations.Visible = false;
			Controls.Add(locations);

			Label ltitle = new Label();
			ltitle.Width = 400;
			ltitle.Text = "Install " + mirrorName + " at Location";
			ltitle.Font = MyDefaultSkinFontBold11;
            ltitle.ForeColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("transition_text_colour", Color.Black);
            ltitle.Location = new Point(10, 10);
			locations.Controls.Add(ltitle);

			ArrayList slots = model.GetNodesWithAttributeValue("type","Slot");
			foreach(Node n in slots)
			{
				string name = n.GetAttribute("name");

				string ptype = n.Parent.GetAttribute("type");

				if(ptype == "Router")
				{
					int buttonWidth = CreateMirroredServerButton(n, xoffset, yoffset, upColor, hoverColor, disabledColor);

					xoffset += buttonWidth + 25;
				}
			}

			confirm = new Panel();
			confirm.Size = new Size(420,110);
			confirm.Location = new Point(65,45);
			confirm.BackColor = MyGroupPanelBackColor;
			confirm.Visible = false;
			Controls.Add(confirm);

			Label ctitle = new Label();
			ctitle.Width = 420;
			ctitle.Text = "Are you sure you want to remove the " + mirrorName + "?";
			ctitle.Font = MyDefaultSkinFontNormal11;
			ctitle.Location = new Point(0,45);
			ctitle.TextAlign = ContentAlignment.MiddleCenter;
			confirm.Controls.Add(ctitle);

			ok = new StyledDynamicButtonCommon ("standard", "OK");
			ok.Font = MyDefaultSkinFontBold9;
			ok.Size = new Size(80,20);
			ok.Location = new Point(355,180);
			ok.Click += ok_Click;
			ok.Visible = false;
			Controls.Add(ok);

			focusJumper.Add(ok);

			cancel = new StyledDynamicButtonCommon ("standard", "Close");
			cancel.Font = MyDefaultSkinFontBold9;
			cancel.Size = new Size(80,20);
			cancel.Location = new Point(445,180);
			cancel.Click += cancel_Click;
			Controls.Add(cancel);

			focusJumper.Add(cancel);
		}

		protected virtual int CreateMirroredServerButton(Node n, int xoffset, int yoffset, Color upColor, Color hoverColor, Color disabledColor)
		{
			string disp = n.GetAttribute("name");

			// This is a valid slot for a mirrored server...
			ImageTextButton button = new StyledDynamicButtonCommon ("standard", disp);
			button.Font = MyDefaultSkinFontBold9;
			button.Size = new Size(80, 20);
			button.Location = new Point(xoffset, yoffset);
			button.BackColor = MyGroupPanelBackColor;
			button.Click += loc_Click;
			locations.Controls.Add(button);
			locButtons.Add(button);

			focusJumper.Add(button);

			return button.Width;
		}

		protected void loc_Click(object sender, EventArgs e)
		{
			// Set the picked location...
			ImageTextButton b = sender as ImageTextButton;
			pickedLoc = b.GetButtonText();

			ArrayList attrs = new ArrayList();
			attrs.Add( new AttributeValuePair("target",pickedServer) );
			attrs.Add( new AttributeValuePair("location",pickedLoc) );
			attrs.Add( new AttributeValuePair("type","add_mirror") );

			Node command = new Node(mirrorCommandQueueNode,"add_mirror", "", attrs);
			_mainPanel.DisposeEntryPanel();

		}

		protected void button_Click(object sender, EventArgs e)
		{
			ImageTextButton button = sender as ImageTextButton;
			MirrorOption option = (MirrorOption) button.Tag;
			pickedServer = option.Target.GetAttribute("name");

			title.Text = mirrorName + " Options > "+pickedServer;

			string buttonText = button.GetButtonText();

			if(buttonText.StartsWith("Remove"))
			{
				confirm.Visible = true;
				confirm.BringToFront();
				ok.Visible = true;
				ok.SetButtonText("Remove");

				focusJumper.Dispose();
				focusJumper = new FocusJumper();
				focusJumper.Add(ok);
				focusJumper.Add(cancel);
				cancel.Focus();
			}
			else
			{
				locations.Visible = true;
				locations.BringToFront();
				cancel.SetButtonText("Close");

				focusJumper.Dispose();
				focusJumper = new FocusJumper();
				focusJumper.Add(cancel);
				foreach(Control c in locButtons)
				{
					focusJumper.Add(c);
				}
				cancel.Focus();
			}
		}

		protected void ok_Click(object sender, EventArgs e)
		{
			ArrayList attrs = new ArrayList();
			attrs.Add( new AttributeValuePair("target",pickedServer) );
			attrs.Add( new AttributeValuePair("type","remove_mirror") );
			Node command = new Node(mirrorCommandQueueNode,"add_mirror", "", attrs);
			_mainPanel.DisposeEntryPanel();
		}

		protected void cancel_Click(object sender, EventArgs e)
		{
			_mainPanel.DisposeEntryPanel();
		}

	
		protected override void Dispose(bool disposing)
		{
			if(disposing)
			{
				focusJumper.Dispose();
			}
			base.Dispose (disposing);
		}

		protected void TransAddOrRemoveMirrorControl_GotFocus(object sender, EventArgs e)
		{
			cancel.Focus();
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);
			DoSize();
		}

		void DoSize ()
		{
			int instep = 20;

			cancel.Location = new Point (Width - instep - cancel.Width, Height - instep - cancel.Height);
			ok.Location = new Point (cancel.Left - ok.Width - instep, cancel.Top);

			title.Bounds = new Rectangle(0, 0, Width, 25);
			title.BackColor = SkinningDefs.TheInstance.GetColorData("popup_title_background_colour", Color.White);
			title.ForeColor = SkinningDefs.TheInstance.GetColorData("popup_title_foreground_colour", Color.Black);
		}
	}
}