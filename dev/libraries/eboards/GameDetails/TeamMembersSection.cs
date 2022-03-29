using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.Xml;
using System.IO;

using GameManagement;
using LibCore;
using CoreUtils;

namespace GameDetails
{
	internal class TeamMembersSection : Panel
	{
		NetworkProgressionGameFile gameFile;

		Label titleLabel;
		Label nameLabel;
		Label roleLabel;

		Panel scrollingPanel;
		List<TeamMemberRolePanel> members;
		TeamMemberRolePanel selectedMember;
		TeamMemberRolePanel lastSelectedMember;

		Button addButton;
		Button removeButton;

		List<string> roles;

		string TeamFile
		{
			get
			{
				return gameFile.Dir + @"\global\team.xml";
			}
		}

		public TeamMembersSection (NetworkProgressionGameFile gameFile)
		{
			this.gameFile = gameFile;

			members = new List<TeamMemberRolePanel>();

			roles = new List<string>(File.ReadAllLines(AppInfo.TheInstance.Location + @"\data\roles.txt"));
			while (string.IsNullOrEmpty(roles[roles.Count - 1]))
			{
				roles.RemoveAt(roles.Count - 1);
			}

			BuildControls();
		}

		void BuildControls ()
		{
			titleLabel = new Label();
			titleLabel.Font = SkinningDefs.TheInstance.GetFont(10);
			titleLabel.Text = "Team Members";
            titleLabel.ForeColor = SkinningDefs.TheInstance.GetColorData("game_details_screen_text_colour", Color.Black);
            Controls.Add(titleLabel);

			nameLabel = new Label();
			nameLabel.Font = SkinningDefs.TheInstance.GetFont(10);
			nameLabel.Text = "Name";
            nameLabel.ForeColor = SkinningDefs.TheInstance.GetColorData("game_details_screen_text_colour", Color.Black);
            Controls.Add(nameLabel);

			roleLabel = new Label();
			roleLabel.Font = SkinningDefs.TheInstance.GetFont(10);
			roleLabel.Text = "Role";
            roleLabel.ForeColor = SkinningDefs.TheInstance.GetColorData("game_details_screen_text_colour", Color.Black);
            Controls.Add(roleLabel);

			scrollingPanel = new Panel();
			scrollingPanel.AutoScroll = true;
			scrollingPanel.BorderStyle = BorderStyle.FixedSingle;
			Controls.Add(scrollingPanel);

			addButton = SkinningDefs.TheInstance.CreateWindowsButton(FontStyle.Bold);
			addButton.Text = "Add";
		    addButton.Click += addButton_Click;
			Controls.Add(addButton);

			removeButton = SkinningDefs.TheInstance.CreateWindowsButton(FontStyle.Bold);
			removeButton.Text = "Remove";
		    removeButton.Click += removeButton_Click;
			Controls.Add(removeButton);

			DoLayout();
			LoadData();
		}

		void DoLayout ()
		{
			titleLabel.Location = new Point(0, 20);
			titleLabel.Size = new Size(120, 25);

			scrollingPanel.Location = new Point(titleLabel.Right, 20);
			scrollingPanel.Size = new Size(360, 100);

			nameLabel.Size = new Size(((scrollingPanel.Width - 20) / 2) - 30, 20);
			nameLabel.Location = new Point(titleLabel.Right, scrollingPanel.Top - nameLabel.Height);

			roleLabel.Location = new Point(nameLabel.Right, nameLabel.Top);
			roleLabel.Size = new Size(scrollingPanel.Right - roleLabel.Left, 20);

			int y = 0;
			foreach (TeamMemberRolePanel member in members)
			{
				member.Location = new Point(0, y);
				member.Size = new Size(scrollingPanel.Width - 20 - member.Left, 20);
				y = member.Bottom;
			}

			addButton.Location = new Point(titleLabel.Right, scrollingPanel.Bottom + 10);
			addButton.Size = new Size(75, 25);

			removeButton.Location = new Point(addButton.Right + 10, scrollingPanel.Bottom + 10);
			removeButton.Size = addButton.Size;

			Size = new Size(500, addButton.Bottom + 10);
		}

		public void LoadData ()
		{
			BasicXmlDocument xml = BasicXmlDocument.LoadOrCreate(TeamFile);
			XmlElement root = XMLUtils.GetOrCreateElement(xml, "team");
			XmlElement members = XMLUtils.GetOrCreateElement(root, "members");

			this.members.Clear();
			scrollingPanel.Controls.Clear();

			foreach (XmlElement child in members.ChildNodes)
			{
				AddMember(child.SelectSingleNode("name").InnerText,
						  child.SelectSingleNode("role").InnerText);
			}

			SelectMember(null);
		}

		public void SaveData ()
		{
			BasicXmlDocument xml = BasicXmlDocument.LoadOrCreate(TeamFile);
			XmlElement root = XMLUtils.GetOrCreateElement(xml, "team");
			XmlElement membersElement = XMLUtils.GetOrCreateElement(root, "members");
			membersElement.RemoveAll();

			foreach (TeamMemberRolePanel member in members)
			{
				XmlElement memberElement = CoreUtils.XMLUtils.CreateElement(membersElement, "member");
				CoreUtils.XMLUtils.CreateElementString(memberElement, "name", member.Name);
				CoreUtils.XMLUtils.CreateElementString(memberElement, "role", member.Role);
			}

			xml.Save(TeamFile);
		}

		TeamMemberRolePanel AddMember (string name, string role)
		{
			TeamMemberRolePanel member = new TeamMemberRolePanel(new System.Collections.ArrayList(roles));
			member.GotTheFocus += member_GotTheFocus;
			member.LostTheFocus += member_LostTheFocus;

			member.Name = name;

			if (!string.IsNullOrEmpty(role))
			{
				member.Role = role;
			}

			members.Add(member);
			scrollingPanel.Controls.Add(member);

			member.NameWidth = nameLabel.Width;
			member.Font = SkinningDefs.TheInstance.GetFont(9);

			DoLayout();

			return member;
		}

		void SelectMember (TeamMemberRolePanel member)
		{
			selectedMember = null;
			lastSelectedMember = null;
			removeButton.Enabled = false;
			foreach (TeamMemberRolePanel tmp_member in members)
			{
				if (tmp_member == member)
				{
					selectedMember = member;
					selectedMember.Highlight(true);
					removeButton.Enabled = true;
					lastSelectedMember = member;
				}
				else
				{
					tmp_member.Highlight(false);
				}
			}
		}

		void member_GotTheFocus (TeamMemberRolePanel sender)
		{
			SelectMember(sender);
		}

		void member_LostTheFocus (TeamMemberRolePanel sender)
		{
			SaveData();
		}

		void addButton_Click (object sender, EventArgs args)
		{
			TeamMemberRolePanel member = AddMember("", "");
			SelectMember(member);
			member.Select();
			scrollingPanel.ScrollControlIntoView(member);
		}

		void removeButton_Click (object sender, EventArgs args)
		{
			if (lastSelectedMember != null)
			{
				members.Remove(lastSelectedMember);
				scrollingPanel.Controls.Remove(lastSelectedMember);
				DoLayout();
				lastSelectedMember = null;
				selectedMember = null;
				removeButton.Enabled = false;
				SaveData();
			}
		}
	}
}