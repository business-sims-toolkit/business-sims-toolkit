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
	public class AddOrRemoveMirrorControl : FlickerFreePanel
	{
		protected MirrorApplier _mirrorApplier;
		protected Node mirrorCommandQueueNode;

		protected IDataEntryControlHolder _mainPanel;

		protected string pickedLoc = "";
		protected string pickedServer = "";
		protected bool removePicked = false;

		protected ArrayList locButtons = new ArrayList();
		protected ArrayList serverButtons = new ArrayList();

		Panel confirm;
		Panel locations;

		string mirrorName = SkinningDefs.TheInstance.GetData("mirror_name", "Mirror");

		protected ImageTextButton ok;
		protected ImageTextButton cancel;
		protected Label title;
		protected Label explain1;
		protected Label explain2;
		protected Label SelectedOptionLabel;

		Boolean MyIsTrainingMode;

		protected FocusJumper focusJumper;

		//skin stuff
		Color MyGroupPanelBackColor;

		Color MyOperationsBackColor;
		Color MyTitleForeColor = Color.Black;

		Font MyDefaultSkinFontNormal11 = null;
		Font MyDefaultSkinFontBold11 = null;
		Font MyDefaultSkinFontBold12 = null;
		Font MyDefaultSkinFontBold9 = null;
		Font MyDefaultSkinFontBold10 = null;

		public AddOrRemoveMirrorControl(IDataEntryControlHolder mainPanel, NodeTree model, 
			MirrorApplier mirrorApplier, Boolean IsTrainingMode, Color OperationsBackColor, 
			Color GroupPanelBackColor)
		{
			SuspendLayout();

			focusJumper = new FocusJumper();

			Color upColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_text_colour", Color.Black);
			Color downColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_down_text_colour", Color.White);
			Color hoverColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_hover_text_colour", Color.Green);
			Color disabledColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_disabled_text_colour", Color.DarkGray);

			//all transition panel 
			MyOperationsBackColor = OperationsBackColor;
			MyGroupPanelBackColor = GroupPanelBackColor;

			//Is there an overriding Title Foreground colour
			string racetitlecolour =  SkinningDefs.TheInstance.GetData("race_paneltitleforecolor");
			if (racetitlecolour != "")
			{
				MyTitleForeColor =  SkinningDefs.TheInstance.GetColorData("race_paneltitleforecolor");
			}

			//If this existing, we use the group back as the back for the mirror confirm panel back color
			string switch_confirm_back = SkinningDefs.TheInstance.GetData("race_panel_mirror_switch_confirm_back","");

			string fontname =  SkinningDefs.TheInstance.GetData("fontname");
			MyDefaultSkinFontNormal11 = ConstantSizeFont.NewFont(fontname,11);
			MyDefaultSkinFontBold11 = ConstantSizeFont.NewFont(fontname,11,FontStyle.Bold);
			MyDefaultSkinFontBold12 = ConstantSizeFont.NewFont(fontname,12,FontStyle.Bold);
			MyDefaultSkinFontBold9 = ConstantSizeFont.NewFont(fontname, 9, FontStyle.Bold);
			MyDefaultSkinFontBold10 = ConstantSizeFont.NewFont(fontname, 10, FontStyle.Bold);

			MyIsTrainingMode = IsTrainingMode;
			
			if (SkinningDefs.TheInstance.GetBoolData("popups_use_image_background", true))
			{
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
			}

			_mainPanel = mainPanel;
			_mirrorApplier = mirrorApplier;
			mirrorCommandQueueNode = model.GetNamedNode("MirrorCommandQueue");
			//Create the Title
		    title = new Label
		    {
		        
		        Text = mirrorName + " Options",
		        TextAlign = ContentAlignment.MiddleLeft,
		        Size = SkinningDefs.TheInstance.GetSizeData("ops_popup_title_size", new Size(400, 20)),
		        Location = new Point (0, 0),
		        Font = SkinningDefs.TheInstance.GetFont(SkinningDefs.TheInstance.GetFloatData("ops_popup_title_font_size", 12),
		            SkinningDefs.TheInstance.GetBoolData("ops_title_use_bold_font", true) ? FontStyle.Bold : FontStyle.Regular),
                BackColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("popup_title_background_colour",
		            MyOperationsBackColor),
		        ForeColor = MyTitleForeColor,
		        
		    };
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
				p.Size = new Size(190,90);
				p.Location = new Point(xoffset,yoffset);
				p.BackColor = MyGroupPanelBackColor;
				Controls.Add(p);

				Label l = new Label();
				l.Text = name;
				l.Font = MyDefaultSkinFontBold11;
				l.Location = new Point(5,5);
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
				ImageTextButton button = new StyledDynamicButtonCommon("standard", text);
				button.Font = MyDefaultSkinFontBold9;
				button.Tag = option;
				button.Size = new Size(180,20);
				button.Location = new Point(5,30);
				button.Click += button_Click;
				p.Controls.Add(button);

				serverButtons.Add(button);

				focusJumper.Add(button);

				xoffset += 240;
			}

			xoffset = 110;
			yoffset = 40;

			
			// 23-04-2007 : We are only allowed to put the mirror in particular places...
			locations = new Panel();
			locations.SuspendLayout();

			locations.Size = new Size(450,95);
			locations.Location = new Point(55,40);
			//locations.BackColor = Color.LightGray;
			locations.BackColor = MyGroupPanelBackColor;
			locations.Visible = false;
			Controls.Add(locations);

			Label ltitle = new Label();
			ltitle.Width = 400;
			ltitle.Text = "Install " + mirrorName + " at Location";
			ltitle.Font = MyDefaultSkinFontNormal11;
			ltitle.Location = new Point(10,10);
			locations.Controls.Add(ltitle);

			ArrayList slots = model.GetNodesWithAttributeValue("type","Slot");
			foreach(Node n in slots)
			{
				string name = n.GetAttribute("name");

				string ptype = n.Parent.GetAttribute("type");

				if(ptype == "Router")
				{
					string disp = n.GetAttribute("name");

					// This is a valid slot for a mirrored server...
					ImageTextButton button = new StyledDynamicButtonCommon ("standard", disp);
					button.Font = MyDefaultSkinFontBold9;
					button.Size = new Size(80,20);
					button.Location = new Point(xoffset, yoffset);
					button.BackColor = MyGroupPanelBackColor;
					button.Click += loc_Click;
					button.Visible = true;
					locations.Controls.Add(button);
					locButtons.Add(button);

					focusJumper.Add(button);

					xoffset += button.Width + 5;
				}

				GotFocus += AddOrRemoveMirrorControl_GotFocus;
			}

			locations.ResumeLayout(false);

			confirm = new Panel();
			confirm.Size = new Size(450,95);
			confirm.Location = new Point(55,45);
			confirm.BackColor = MyOperationsBackColor;
			if (switch_confirm_back != "")
			{
				confirm.BackColor = MyGroupPanelBackColor;
			}
			confirm.Visible = false;
			Controls.Add(confirm);

			Label ctitle = new Label();
			ctitle.Width = 410;
			ctitle.Text = "Are you sure you want to remove the mirror?";
			ctitle.Font = MyDefaultSkinFontNormal11;
			ctitle.Location = new Point(30,35);
			confirm.Controls.Add(ctitle);

			cancel = new StyledDynamicButtonCommon ("standard", "Close");
			cancel.Font = MyDefaultSkinFontBold10;
			cancel.Size = new Size(80,20);
			cancel.Location = SkinningDefs.TheInstance.GetPointData("ops_popup_cancel_button_position", 500, 150);
			cancel.Click += cancel_Click;
			cancel.Visible = true;
			Controls.Add(cancel);

			ok = new StyledDynamicButtonCommon ("standard", "OK");
			ok.Font = MyDefaultSkinFontBold9;
			ok.Size = new Size(80, 20);
			ok.Location = new Point(cancel.Left - 10 - ok.Width, cancel.Top);
			ok.Click += ok_Click;
			ok.Visible = false;
			Controls.Add(ok);

			focusJumper.Add(ok);

			ResumeLayout(false);

			focusJumper.Add(cancel);

			Resize += AddOrRemoveMirrorControl_Resize;
		}

		void loc_Click(object sender, EventArgs e)
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

		void button_Click(object sender, EventArgs e)
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
				ok.SetButtonText("Yes");
				cancel.SetButtonText("No");

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

		void ok_Click(object sender, EventArgs e)
		{
			ArrayList attrs = new ArrayList();
			attrs.Add( new AttributeValuePair("target",pickedServer) );
			attrs.Add( new AttributeValuePair("type","remove_mirror") );
			Node command = new Node(mirrorCommandQueueNode,"add_mirror", "", attrs);
			_mainPanel.DisposeEntryPanel();
		}

		void cancel_Click(object sender, EventArgs e)
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

		void AddOrRemoveMirrorControl_GotFocus(object sender, EventArgs e)
		{
			cancel.Focus();
		}

		void AddOrRemoveMirrorControl_Resize(object sender, EventArgs e)
		{
			if (string.IsNullOrEmpty(SkinningDefs.TheInstance.GetData("ops_popup_cancel_button_position")))
			{
				cancel.Location = new Point (Width - cancel.Width - 5, Height - cancel.Height - 5);
				ok.Location = new Point (cancel.Left - ok.Width - 5, cancel.Top);
			}

		    if (SkinningDefs.TheInstance.GetBoolData("ops_popup_title_use_full_width", false))
		    {
		        title.Size = new Size (Width, 25);
		    }
        }
	}
}
