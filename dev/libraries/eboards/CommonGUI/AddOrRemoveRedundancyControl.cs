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
	/// Summary description for AddOrRemoveRedundancyControl.
	/// This is the CA Mirror Popup but it needs to say Redundancy  
	/// </summary>
	public class AddOrRemoveRedundancyControl : FlickerFreePanel
	{
		protected MirrorApplier _mirrorApplier;
		protected Node mirrorCommandQueueNode;
		protected NodeTree model;

		protected IDataEntryControlHolder _mainPanel;

		Panel confirm;

		protected ImageTextButton ok;
		protected ImageTextButton cancel;
		protected Label title;
		protected Label explain1;
		protected Label explain2;
		protected Label SelectedOptionLabel;

		string filename_long = "\\images\\buttons\\blank_big.png";
		//private string filename_mid = "\\images\\buttons\\blank_med.png";
		string filename_short = "\\images\\buttons\\blank_small.png";

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

		public AddOrRemoveRedundancyControl(IDataEntryControlHolder mainPanel, NodeTree model, 
			MirrorApplier mirrorApplier, Boolean IsTrainingMode, Color OperationsBackColor, 
			Color GroupPanelBackColor)
		{
			SuspendLayout();
			this.model = model;
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

			_mainPanel = mainPanel;
			_mirrorApplier = mirrorApplier;
			mirrorCommandQueueNode = model.GetNamedNode("MirrorCommandQueue");
			//Create the Title 
			title = new Label();
			title.Font = MyDefaultSkinFontBold12;
			title.Text = "Virtualization Options";
			title.TextAlign = ContentAlignment.MiddleLeft;
			title.Size = new Size(400,20);
			title.BackColor = MyOperationsBackColor;
			title.ForeColor = MyTitleForeColor;
			title.Location = new Point(15,10);
			Controls.Add(title);		

			//
			// Create the Add/Remove mirror buttons.
			//
			int xoffset = 65;
			int yoffset = 35+10;

			foreach(MirrorOption option in mirrorApplier.Options)
			{
				string name = option.Target.GetAttribute("name");
				string zone = string.Format("Zone {0}", option.Target.GetAttribute("proczone"));

				Panel p = new Panel();
				p.Size = new Size(190,90);
				p.Location = new Point(xoffset,yoffset);
				p.BackColor = Color.Transparent;
				Controls.Add(p);

				Label l = new Label();
				l.Text = zone;
				l.Font = MyDefaultSkinFontBold11;
				l.Location = new Point(5,5);
				p.Controls.Add(l);

				ImageTextButton button = new ImageTextButton(0);
				button.ButtonFont = MyDefaultSkinFontBold9;
				button.SetVariants(filename_long);

				if(null == model.GetNamedNode(name + "(M)"))
				{
					button.SetButtonText("Install Virtualization", upColor, upColor, hoverColor, disabledColor);
					button.Click += installButton_Click;
					p.Show();
				}
				else
				{
					button.SetButtonText("Remove Virtualization", upColor, upColor, hoverColor, disabledColor);
					button.Click += removeButton_Click;
					p.Hide();
				}

				button.Tag = option;
				button.Size = new Size(180,20);
				button.Location = new Point(5,30);

				p.Controls.Add(button);

				focusJumper.Add(button);

				xoffset += 240;
			}

			xoffset = 110;
			yoffset = 40;

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
			ctitle.Text = "Are you sure you want to remove virtualization?";
			ctitle.Font = MyDefaultSkinFontNormal11;
			ctitle.Location = new Point(30,35);
			confirm.Controls.Add(ctitle);

			ok = new ImageTextButton(0);
			ok.ButtonFont = MyDefaultSkinFontBold9;
			ok.SetVariants(filename_short);
			ok.Size = new Size(80, 20);
			ok.Location = new Point(415, 150);
			ok.SetButtonText("OK", upColor, upColor, hoverColor, disabledColor);
			ok.Click += ok_Click;
			ok.Visible = false;
			Controls.Add(ok);

			focusJumper.Add(ok);

			GotFocus += AddOrRemoveRedundancyControl_GotFocus;

			cancel = new ImageTextButton(0);
			cancel.ButtonFont = MyDefaultSkinFontBold10;
			cancel.SetVariants(filename_short);
			cancel.Size = new Size(80,20);
			cancel.Location = new Point(500,150);
			cancel.SetButtonText("Close", upColor, upColor, hoverColor, disabledColor);
			cancel.Click += cancel_Click;
			cancel.Visible = true;
			Controls.Add(cancel);

			ResumeLayout(false);

			focusJumper.Add(cancel);
			 
			Resize += AddOrRemoveRedundancyControl_Resize;
		}

		void ok_Click(object sender, EventArgs e)
		{
			ImageTextButton button = sender as ImageTextButton;
			MirrorOption option = (MirrorOption)button.Tag;
			string pickedServer = option.Target.GetAttribute("name");

			ArrayList attrs = new ArrayList();
			attrs.Add(new AttributeValuePair("target", pickedServer));
			attrs.Add(new AttributeValuePair("type", "remove_mirror"));
			Node command = new Node(mirrorCommandQueueNode, "add_mirror", "", attrs);
			_mainPanel.DisposeEntryPanel();
		}

		void cancel_Click(object sender, EventArgs e)
		{
			_mainPanel.DisposeEntryPanel();
		}

		void installButton_Click(object sender, EventArgs e)
		{
			ImageTextButton button = sender as ImageTextButton;
			MirrorOption option = (MirrorOption)button.Tag;
			string pickedServer = option.Target.GetAttribute("name");

			string pickedLoc = GetInstallLocation(option.Target);

			ArrayList attrs = new ArrayList();
			attrs.Add(new AttributeValuePair("target", pickedServer));
			attrs.Add(new AttributeValuePair("location", pickedLoc));
			attrs.Add(new AttributeValuePair("type", "add_mirror"));

			Node command = new Node(mirrorCommandQueueNode, "add_mirror", "", attrs);
			_mainPanel.DisposeEntryPanel();
		}

		// alway virtualizing to the same zone
		string GetInstallLocation(Node mirrorTarget)
		{
			int installZone = mirrorTarget.GetIntAttribute("proczone", -1);
			ArrayList slots = model.GetNodesWithAttributeValue("type", "Slot");
			
			foreach (Node n in slots)
			{
				string ptype = n.Parent.GetAttribute("type");

				if (ptype == "Router")
				{
					int procZone = n.GetIntAttribute("proczone", -1);

					if (procZone == installZone)
					{
						return n.GetAttribute("location");
					}
				}
			}

			return string.Empty;
		}

		void removeButton_Click(object sender, EventArgs e)
		{
			confirm.Visible = true;
			confirm.BringToFront();
			ok.Visible = true;
			ok.SetButtonText("Yes");
			ok.Tag = ((ImageButton)sender).Tag;
			cancel.SetButtonText("No");

			focusJumper.Dispose();
			focusJumper = new FocusJumper();
			focusJumper.Add(ok);
			focusJumper.Add(cancel);
			cancel.Focus();
		}

		protected override void Dispose(bool disposing)
		{
			if(disposing)
			{
				focusJumper.Dispose();
			}
			base.Dispose (disposing);
		}

		void AddOrRemoveRedundancyControl_GotFocus(object sender, EventArgs e)
		{
			cancel.Focus();
		}

		void AddOrRemoveRedundancyControl_Resize(object sender, EventArgs e)
		{
			cancel.Location = new Point( Width-cancel.Width-5, Height-cancel.Height-5);
			ok.Location = new Point( cancel.Left-ok.Width-5, cancel.Top);
		}
	}
}
