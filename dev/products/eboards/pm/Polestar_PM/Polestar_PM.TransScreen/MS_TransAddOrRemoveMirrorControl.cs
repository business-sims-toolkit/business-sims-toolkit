using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;

using LibCore;
using Network;

using CoreUtils;
using IncidentManagement;
using CommonGUI;

namespace Polestar_PM.TransScreen
{
	/// <summary>
	/// Summary description for AddOrRemoveMirrorControl.
	/// </summary>
	public class MS_TransAddOrRemoveMirrorControl : TransAddOrRemoveMirrorControl
	{

		public MS_TransAddOrRemoveMirrorControl(IDataEntryControlHolder mainPanel, NodeTree model, 
			MirrorApplier mirrorApplier, Color OperationsBackColor, Color GroupPanelBackColor)
			:base (mainPanel, model, mirrorApplier, OperationsBackColor, GroupPanelBackColor)
		{
		}

		public override void BuildScreenControls(NodeTree model, MirrorApplier mirrorApplier)
		{
			//Create the Title 
			title = new Label();
			title.Font = this.MyDefaultSkinFontBold12;
			title.Text = "Mirror Options";
			title.TextAlign = ContentAlignment.MiddleLeft;
			title.Size = new Size(350,20);
			title.BackColor = MyOperationsBackColor;
			title.Location = new Point(10,10);
			this.Controls.Add(title);		

			this.Width = 380;
			this.Height = 350;

			int xoffset = 15;
			int yoffset = 40;

			foreach(MirrorOption option in mirrorApplier.Options)
			{
				string name = option.Target.GetAttribute("name");

				Panel p = new Panel();
				p.Size = new Size(150,100);
				p.Location = new Point(xoffset,yoffset);
				p.BackColor = MyGroupPanelBackColor;
				this.Controls.Add(p);

				Label l = new Label();
				l.Text = name;
				l.Font = this.MyDefaultSkinFontBold11;
				l.Location = new Point(5,5);
				p.Controls.Add(l);

				ImageTextButton button = new ImageTextButton(0);
				button.ButtonFont = this.MyDefaultSkinFontBold9;
				button.SetVariants(filename_long);

				if(null == model.GetNamedNode(name + "(M)"))
				{
					//button.Text = "Install Mirror ";
					button.SetButtonText("Install Mirror",
						System.Drawing.Color.Black,System.Drawing.Color.Black,
						System.Drawing.Color.Green,System.Drawing.Color.DarkGray);
				}
				else
				{
					//button.Text = "Remove Mirror";
					button.SetButtonText("Remove Mirror",
						System.Drawing.Color.Black,System.Drawing.Color.Black,
						System.Drawing.Color.Green,System.Drawing.Color.DarkGray);
				}
				button.Tag = option;
				//button.FlatStyle = FlatStyle.System;
				button.Size = new Size(130,20);
				button.Location = new Point(10, 30);
				button.Click += new EventHandler(button_Click);
				p.Controls.Add(button);

				serverButtons.Add(button);

				focusJumper.Add(button);

				xoffset += (160);
			}

			xoffset = 80;
			yoffset = 40;
			
			// 23-04-2007 : We are only allowed to put the mirror in particular places...
			locations = new Panel();
			locations.Size = new Size(340,110);
			locations.Location = new Point(15,45-5);
			locations.BackColor = MyGroupPanelBackColor;
			locations.Visible = false;
			this.Controls.Add(locations);

			Label ltitle = new Label();
			ltitle.Width = 400;
			ltitle.Text = "Install mirror at location : ";
			ltitle.Font = this.MyDefaultSkinFontBold11;
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
					ImageTextButton button = new ImageTextButton(0);
					button.ButtonFont = this.MyDefaultSkinFontBold9;
					button.SetVariants(filename_mid);
					button.Size = new Size(80,20);
					button.Location = new Point(xoffset, yoffset);
					button.BackColor = MyGroupPanelBackColor;
					button.SetButtonText(disp,
						System.Drawing.Color.Black,System.Drawing.Color.Black,
						System.Drawing.Color.Green,System.Drawing.Color.DarkGray);
					button.Click += new EventHandler(loc_Click);
					button.Visible = true;
					locations.Controls.Add(button);
					locButtons.Add(button);

					focusJumper.Add(button);

					xoffset += button.Width + 25;
				}
			}

			confirm = new Panel();
			confirm.Size = new Size(340,110);
			confirm.Location = new Point(10,40);
			confirm.BackColor = MyGroupPanelBackColor;
			confirm.Visible = false;
			this.Controls.Add(confirm);

			Label ctitle = new Label();
			ctitle.Width = 340;
			ctitle.Text = "Are you sure you want to remove the mirror ?";
			ctitle.Font = this.MyDefaultSkinFontNormal11;
			ctitle.Location = new Point(20,45);
			confirm.Controls.Add(ctitle);

			ok = new ImageTextButton(0);
			ok.ButtonFont = this.MyDefaultSkinFontBold9;
			ok.SetVariants(filename_short);
			ok.Size = new Size(80,20);
			ok.Location = new Point(180,300);
			ok.SetButtonText("OK",
				System.Drawing.Color.Black,System.Drawing.Color.Black,
				System.Drawing.Color.Green,System.Drawing.Color.DarkGray);
			ok.Click += new EventHandler(ok_Click);
			ok.Visible = false;
			this.Controls.Add(ok);

			focusJumper.Add(ok);

			cancel = new ImageTextButton(0);
			cancel.ButtonFont = this.MyDefaultSkinFontBold9;
			cancel.SetVariants(filename_short);
			cancel.Size = new Size(80,20);
			cancel.Location = new Point(270,300);
			cancel.SetButtonText("Close",
				System.Drawing.Color.Black,System.Drawing.Color.Black,
				System.Drawing.Color.Green,System.Drawing.Color.DarkGray);
			cancel.Click += new EventHandler(cancel_Click);
			cancel.Visible = true;
			this.Controls.Add(cancel);

			focusJumper.Add(cancel);

		}

	}
}
