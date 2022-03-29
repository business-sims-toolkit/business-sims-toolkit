using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Xml;

using LibCore;
using CoreUtils;

namespace GameDetails
{
	/// <summary>
	/// Summary description for TeamMemberDetails.
	/// </summary>
	public class TeamMemberDetails : GameDetailsSection
	{
		protected Panel scrollPanel;
		protected Panel basePanel;
		protected GameManagement.NetworkProgressionGameFile _gameFile;

		protected Label nameTitle;
		protected Label roleTitle;

		protected Button addButton;
		protected Button removeButton;

		protected ArrayList members;
		protected ArrayList roleList;

		protected Font MyDefaultSkinFontBold9;

		protected TeamMemberRolePanel selectedMember;

		string Filename
		{
			get
			{
				return _gameFile.Dir + @"\global\team.xml";
			}
		}

		public void GetMember(int num, out string name, out string role)
		{
			TeamMemberRolePanel tm = (TeamMemberRolePanel) members[num];
			name = tm.Name;
			role = tm.Role;
		}

		public TeamMemberDetails(GameManagement.NetworkProgressionGameFile gameFile)
		{
			_gameFile = gameFile;
			this.Title = "Team Members";

			string fontname =  SkinningDefs.TheInstance.GetData("fontname");
			MyDefaultSkinFontBold9 = ConstantSizeFont.NewFont(fontname,9,FontStyle.Bold);

			nameTitle = new Label();
			nameTitle.Location = new Point(5,5);
			nameTitle.Size = new Size(225,20);
			nameTitle.Font = MyDefaultSkinFontBold12;
			nameTitle.Text = "Member";
			panel.Controls.Add(nameTitle);

			roleTitle = new Label();
			roleTitle.Location = new Point(230,5);
			roleTitle.Size = new Size(225,20);
			roleTitle.Font = nameTitle.Font;
			roleTitle.Text = "Role";
			panel.Controls.Add(roleTitle);

			scrollPanel = new Panel();
			scrollPanel.Location = new Point(5,25);
			scrollPanel.Size = new Size(450,200);
			scrollPanel.BorderStyle = BorderStyle.FixedSingle;
			scrollPanel.AutoScroll = true;
			panel.Controls.Add(scrollPanel);

			basePanel = new Panel();
			scrollPanel.Controls.Add(basePanel);

			addButton = SkinningDefs.TheInstance.CreateWindowsButton(FontStyle.Bold);
			addButton.Name = "addButton Button";
			addButton.Location = new Point(5,230);
			addButton.Size = new Size(80,20);
		    addButton.Text = "Add";
			addButton.Click += addButton_Click;
			panel.Controls.Add(addButton);

			removeButton = SkinningDefs.TheInstance.CreateWindowsButton(FontStyle.Bold);
			removeButton.Name = "removeButton Button";
			removeButton.Location = new Point(190,230);
			removeButton.Size = new Size(80,20);
		    removeButton.Text = "Remove";
			removeButton.Enabled = false;
			removeButton.Click += removeButton_Click;
			panel.Controls.Add(removeButton);

			members = new ArrayList();

			roleList = new ArrayList();

			StreamReader SR=File.OpenText(LibCore.AppInfo.TheInstance.Location + "\\data\\roles.txt");
			string S=SR.ReadLine();
			while(S!=null)
			{
				roleList.Add(S);
				S=SR.ReadLine();
			}
			SR.Close();

			LoadData();

			this.Resize += TeamMemberDetails_Resize;
		}

		void TeamMemberDetails_Resize(object sender, EventArgs e)
		{
			int half_width = (this.Width - 15)/2; 

			nameTitle.Location = new Point(5, 5);
			nameTitle.Size = new Size(half_width, 20);

			roleTitle.Location = new Point(half_width+10, 5);
			roleTitle.Size = new Size(half_width, 20);

			scrollPanel.Location = new Point(5, 25);
			scrollPanel.Size = new Size(this.Width - 15, 200);
		}

		void addButton_Click(object sender, EventArgs e)
		{
			TeamMemberRolePanel tm = AddMember("","");
			tm.Focus();
		}

		public TeamMemberRolePanel AddMember(string name, string role)
		{
			TeamMemberRolePanel tm = new TeamMemberRolePanel(roleList);
			tm.Size = new Size(scrollPanel.Width-20, 20);
			tm.Location = new Point(0,members.Count*20);
			basePanel.Size = new Size(tm.Width, (members.Count+1)*20);
			basePanel.Controls.Add(tm);
			members.Add(tm);

			tm.Name = name;
			if("" != role) tm.Role = role;

			tm.GotTheFocus += tm_GotTheFocus;
			tm.LostTheFocus += tm_LostTheFocus;

			return tm;
		}

		void tm_GotTheFocus(TeamMemberRolePanel sender)
		{
			removeButton.Enabled = true;
			if(null != selectedMember)
			{
				selectedMember.Highlight(false);
			}
			selectedMember = sender;
			selectedMember.Highlight(true);
		}

		void tm_LostTheFocus(TeamMemberRolePanel sender)
		{
		}

		void removeButton_Click(object sender, EventArgs e)
		{
			if(null != selectedMember)
			{
				members.Remove(selectedMember);
				selectedMember.Dispose();
				selectedMember = null;
				removeButton.Enabled = false;
				//
				int offset = 0;
				//
				foreach(TeamMemberRolePanel tm in members)
				{
					tm.Location = new Point(0,offset);
					offset += tm.Height;
				}
			}
		}

		public override bool SaveData ()
		{
			BasicXmlDocument xml = BasicXmlDocument.LoadOrCreate(Filename);
			XmlElement root = XMLUtils.GetOrCreateElement(xml, "team");
			XmlElement membersElement = XMLUtils.GetOrCreateElement(root, "members");
			membersElement.RemoveAll();

			for (int i = 0; i < members.Count; ++i)
			{
				string name;
				string role;
				GetMember(i, out name, out role);

				XmlElement memb = CoreUtils.XMLUtils.CreateElement(membersElement, "member");
				CoreUtils.XMLUtils.CreateElementString(memb, "name", name);
				CoreUtils.XMLUtils.CreateElementString(memb, "role", role);
			}

			xml.Save(Filename);

			return true;
		}

		public override void LoadData ()
		{
			BasicXmlDocument xml = BasicXmlDocument.LoadOrCreate(Filename);
			XmlElement root = XMLUtils.GetOrCreateElement(xml, "team");
			XmlElement members = XMLUtils.GetOrCreateElement(root, "members");
			foreach (XmlElement child in members.ChildNodes)
			{
				AddMember(child.SelectSingleNode("name").InnerText,
					      child.SelectSingleNode("role").InnerText);
			}
		}

		public int Members
		{
			get
			{
				return members.Count;
			}
		}
	}
}