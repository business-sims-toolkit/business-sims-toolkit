using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;
using CoreUtils;
using LibCore;

namespace GameDetails
{
	/// <summary>
	/// Summary description for TeamMemberRolePanel.
	/// </summary>
	public class TeamMemberRolePanel : BasePanel
	{
		protected static Font font = ConstantSizeFont.NewFont("Arial Unicode MS", 10);
		protected TextBox name;
		protected ComboBox roles;

		public delegate void FocusHandler(TeamMemberRolePanel sender);
		public event FocusHandler GotTheFocus;
		public event FocusHandler LostTheFocus;

		protected Font MyDefaultSkinFontBold9;

		public new string Name
		{
			get
			{
				return name.Text;
			}

			set
			{
				name.Text = value;
			}
		}

		public string Role
		{
			get
			{
				if(roles.SelectedIndex == -1) return "";
				return (string) roles.Items[ roles.SelectedIndex ];
			}

			set
			{
				roles.Text = value;
			}
		}

		public override Font Font
		{
			set
			{
				name.Font = value;
				roles.Font = value;
			}
		}

		int? nameWidth;
		public int NameWidth
		{
			get
			{
				return name.Width;
			}

			set
			{
				nameWidth = value;
				DoSize();
			}
		}

		void BuildFont()
		{
			//string fontname =  SkinningDefs.TheInstance.GetData("fontname");
			//MyDefaultSkinFontBold9 = ConstantSizeFont.NewFont(fontname,9,FontStyle.Bold);
			//MyDefaultSkinFontBold9 = ConstantSizeFont.NewFont(fontname,9,FontStyle.Regular);
			//font = MyDefaultSkinFontBold9;
		}

		public TeamMemberRolePanel(ArrayList roleList)
		{
			nameWidth = null;

			BuildFont();
			Setup(roleList);
		}

		public TeamMemberRolePanel(string nameSet, string roleSet, ArrayList roleList)
		{
			//Don't use skin font as unicode needed for correct name display
			//BuildFont();
			Setup(roleList);
			name.Text = nameSet;
			roles.Text = roleSet;
		}

		protected void Setup(ArrayList roleList)
		{
		    name = new TextBox
		    {
		        Font = font,
                MaxLength = SkinningDefs.TheInstance.GetIntData("game_details_team_player_name_max_length", 0)
		    };
		    this.Controls.Add(name);

			roles = new ComboBox();
			roles.Font = font;
			foreach(string str in roleList)
			{
				roles.Items.Add(str);
			}
			roles.DropDownStyle = ComboBoxStyle.DropDownList;
			this.Controls.Add(roles);

			this.Resize += TeamMemberRolePanel_Resize;

			this.SetStyle(ControlStyles.Selectable, true);
			this.GotFocus += TeamMemberRolePanel_GotFocus;

			name.GotFocus += name_GotFocus;
			name.LostFocus += name_LostFocus;
			roles.GotFocus += roles_GotFocus;
			roles.LostFocus += roles_LostFocus;
		}

		void TeamMemberRolePanel_Resize(object sender, EventArgs e)
		{
			DoSize();
		}

		void DoSize ()
		{
			int nameWidth = Width / 2;
			if (this.nameWidth.HasValue)
			{
				nameWidth = this.nameWidth.Value;
			}

			name.Size = new Size (nameWidth, Height);
			roles.Location = new Point (name.Right, 0);
			roles.Size = new Size (Width - roles.Left, Height);
		}

		void TeamMemberRolePanel_GotFocus(object sender, EventArgs e)
		{
			//if(null != GotTheFocus) GotTheFocus(this);
			name.Focus();
		}

		public void Highlight(bool hlight)
		{
			if(hlight)
			{
				name.BackColor = Color.LightSteelBlue;
				name.ForeColor = Color.Black;
				roles.BackColor = Color.LightSteelBlue;
				roles.ForeColor = Color.Black;
			}
			else
			{
				name.BackColor = Color.White;
				name.ForeColor = Color.Black;
				roles.BackColor = Color.White;
				roles.ForeColor = Color.Black;
			}
		}

		void name_GotFocus(object sender, EventArgs e)
		{
			GotTheFocus?.Invoke(this);
		}

		void name_LostFocus(object sender, EventArgs e)
		{
			LostTheFocus?.Invoke(this);
		}

		void roles_GotFocus(object sender, EventArgs e)
		{
			GotTheFocus?.Invoke(this);
		}

		void roles_LostFocus(object sender, EventArgs e)
		{
			LostTheFocus?.Invoke(this);
		}
	}
}
