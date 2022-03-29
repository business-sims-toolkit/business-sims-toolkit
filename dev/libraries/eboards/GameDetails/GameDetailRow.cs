using System;
using System.Drawing;
using System.Windows.Forms;
using LibCore;

namespace GameDetails
{
	/// <summary>
	/// Summary description for GameDetailRow.
	/// </summary>
	public class GameDetailRow : BasePanel
	{
		protected static Font font = CoreUtils.SkinningDefs.TheInstance.GetFont(CoreUtils.SkinningDefs.TheInstance.GetFloatData("game_list_font_size", 10));
		protected Label fileName;
		protected Label date;
		protected string _fullFileName;
		protected Color baseColor;

		public delegate void GameSelectedHandler(GameDetailRow sender);
		public event GameSelectedHandler GameSelected;
		public event GameSelectedHandler GameChosen;

		public void SetBaseColor(Color c)
		{
			this.BackColor = c;
			baseColor = c;
		}

		public string FullFileName
		{
			get
			{
				return _fullFileName;
			}
		}

		public string FileName
		{
			get
			{
				return fileName.Text;
			}
		}

		public GameDetailRow(string file_name, string when, string fullFileName)
		{
			this.SetStyle(ControlStyles.Selectable, true);
			this.GotFocus += GameDetailRow_GotFocus;
			this.LostFocus += GameDetailRow_LostFocus;

			_fullFileName = fullFileName;
			fileName = new Label();
			fileName.Font = font;
			fileName.Text = file_name;
			this.Controls.Add(fileName);

			date = new Label();
			date.Text = when;
			date.Font = font;
			this.Controls.Add(date);

			fileName.Click += fileName_Click;
			fileName.DoubleClick += fileName_DoubleClick;
			date.Click += date_Click;
			date.DoubleClick += date_DoubleClick;

			this.Resize += GameDetailRow_Resize;
		}

		void GameDetailRow_Resize(object sender, EventArgs e)
		{
			date.Size = new Size (95, 20);
			date.Location = new Point (Width - date.Width, 0);

			fileName.Size = new Size (date.Left, 20);
			fileName.Location = new Point (0, 0);
		}

		void GameDetailRow_GotFocus(object sender, EventArgs e)
		{
			this.BackColor = Color.LightSteelBlue;

			GameSelected?.Invoke(this);
		}

		void GameDetailRow_LostFocus(object sender, EventArgs e)
		{
			this.BackColor = baseColor;
		}

		void fileName_Click(object sender, EventArgs e)
		{
			this.Focus();
		}

		void date_Click(object sender, EventArgs e)
		{
			this.Focus();
		}
	
		protected override void OnKeyPress(KeyPressEventArgs e)
		{
			if(e.KeyChar == 13)
			{
				GameChosen?.Invoke(this);
			}
			base.OnKeyPress (e);
		}

		void fileName_DoubleClick(object sender, EventArgs e)
		{
			GameChosen?.Invoke(this);
		}

		void date_DoubleClick(object sender, EventArgs e)
		{
			GameChosen?.Invoke(this);
		}
	}
}
