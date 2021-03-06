using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Algorithms;
using CommonGUI;
using LibCore;

namespace ResizingUi
{
	public class PopupMenu : Form
	{
		public static void AddMenuItem(PopupMenu menu, string title, EventHandler action, bool enabled, string imageFileName = null)
		{
			var item = menu.AddItem(title, imageFileName);
			item.Chosen += action;
			item.Enabled = enabled;
		}


		[DllImport("user32.dll", EntryPoint = "SetWindowPos", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
		static extern int SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

		[DllImport("user32.dll", EntryPoint = "SetWindowRgn", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
		static extern IntPtr SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);

		const int SWP_DRAWFRAME = 0x20;
		const int SWP_NOMOVE = 0x2;
		const int SWP_NOSIZE = 0x1;
		const int SWP_NOZORDER = 0x4;
		const int HWND_TOPMOST = -1;
		const int HWND_NOTOPMOST = -2;
		const int TOPMOST_FLAGS = SWP_NOMOVE | SWP_NOSIZE;

		public class PopupMenuItem : BasePanel
		{
			readonly Label label;
			readonly ImageButton image;

			readonly string imageFilename;
			readonly string checkedImageFilename;

			readonly int margin;
			readonly int imageSize;

			public event EventHandler Chosen;

			bool clickable;
			public bool Clickable
			{
				get => clickable;

				set => clickable = value;
			}

			bool isChecked;
			public bool Checked
			{
				get => isChecked;

				set
				{
					isChecked = value;

					SetImage();
				}
			}

			protected override void OnEnabledChanged(EventArgs e)
			{
				base.OnEnabledChanged(e);
				SetImage();
			}

			string GetDisabledImageName(string imageFileName)
			{
				return string.IsNullOrEmpty(imageFileName) ? null : 
					System.IO.Path.Combine(System.IO.Path.GetDirectoryName(imageFileName), System.IO.Path.GetFileNameWithoutExtension(imageFileName) + "_disabled" + System.IO.Path.GetExtension(imageFileName));
			}

			void SetImage()
			{
				var filenames = new List<string>();

				if (isChecked)
				{
					if (Enabled)
					{
						filenames.Add(checkedImageFilename);
						filenames.Add(imageFilename);
					}
					else
					{
						filenames.Add(GetDisabledImageName(checkedImageFilename));
						filenames.Add(checkedImageFilename);
						filenames.Add(GetDisabledImageName(imageFilename));
						filenames.Add(imageFilename);
					}
				}
				else
				{
					if (Enabled)
					{
						filenames.Add(imageFilename);
					}
					else
					{
						filenames.Add(GetDisabledImageName(imageFilename));
						filenames.Add(imageFilename);
					}
				}

				foreach (var filename in filenames)
				{
					if (!string.IsNullOrEmpty(filename))
					{
						var fullPath = AppInfo.TheInstance.Location + @"\images\" + filename;

						if (System.IO.File.Exists(fullPath))
						{
							image.SetButton(fullPath);
							break;
						}
					}
				}
			}

			Color backColour;

			bool isDivider;
			public bool IsDivider
			{
				get => isDivider;

				set
				{
					isDivider = value;

					label.Visible = !isDivider;
					if (image != null)
					{
						image.Visible = !isDivider;
					}

					Size = GetPreferredSize(Size.Empty);
				}
			}

			int cornerRounding;
			public int CornerRounding
			{
				get => cornerRounding;

				set
				{
					cornerRounding = value;
					Invalidate();
				}
			}

			int dividerHeight;
			public int DividerHeight
			{
				get => dividerHeight;

				set
				{
					dividerHeight = value;

					Size = GetPreferredSize(Size.Empty);
				}
			}

			bool drawDividerLine;
			public bool DrawDividerLine
			{
				get => drawDividerLine;

				set
				{
					drawDividerLine = value;
					Invalidate();
				}
			}

			public PopupMenuItem(string text, float fontSize, FontStyle style)
				: this(text, null, null, fontSize, style)
			{
			}

			public PopupMenuItem(string text, string imageFilename, float fontSize, FontStyle style)
				: this(text, imageFilename, null, fontSize, style)
			{
			}

			public PopupMenuItem(string text, string imageFilename, string checkedImageFilename, float fontSize, FontStyle style)
			{
				margin = 8;
				imageSize = 16;

				label = new Label
				{
					Text = text,
					TextAlign = ContentAlignment.MiddleLeft,
					Font = CoreUtils.SkinningDefs.TheInstance.GetFont(fontSize, style)
				};
				label.MouseEnter += label_MouseEnter;
				label.MouseLeave += label_MouseLeave;
				label.MouseDown += label_MouseDown;
				Controls.Add(label);

				this.imageFilename = imageFilename;
				this.checkedImageFilename = checkedImageFilename;

				image = new ImageButton(0) { Antialias = true };
				image.ButtonPressed += image_ButtonPressed;
				image.MouseEnter += label_MouseEnter;
				image.MouseLeave += label_MouseLeave;
				image.MouseDown += label_MouseDown;
				Controls.Add(image);

				Checked = false;

				backColour = Color.White;
				BackColor = backColour;

				Size = GetPreferredSize(Size.Empty);
			}

			PopupMenu Menu
			{
				get
				{
					var parent = Parent;
					while ((parent != null)
						&& !(parent is PopupMenu))
					{
						parent = parent.Parent;
					}

					return (PopupMenu)parent;
				}
			}

			public bool IsFirst
			{
				get
				{
					if (Menu == null)
					{
						return false;
					}

					return Menu.Items[0] == this;
				}
			}

			public bool IsLast
			{
				get
				{
					if (Menu == null)
					{
						return false;
					}

					return Menu.Items[Menu.Items.Count - 1] == this;
				}
			}

			public Color? IconModulateColour
			{
				get => image.IconModulateColour;

				set => image.IconModulateColour = value;
			}

			public override Size GetPreferredSize(Size proposedSize)
			{
				if (isDivider)
				{
					return new Size(1, dividerHeight);
				}
				else
				{
					var labelSize = label.GetPreferredSize(Size.Empty);

					if (IsLast)
					{
						labelSize.Height += 1;
					}

					return new Size(margin + imageSize + margin + labelSize.Width + margin, labelSize.Height);
				}
			}

			void label_MouseEnter(object sender, EventArgs e)
			{
				if (clickable)
				{
					BackColor = backColour.Shade(0.2f);
				}
			}
			
			protected override void OnMouseDown(MouseEventArgs e)
			{
				base.OnMouseDown(e);
				OnChosen();
			}

			void label_MouseLeave(object sender, EventArgs e)
			{
				if (clickable)
				{
					BackColor = backColour;
				}
			}

			void label_MouseDown(object sender, MouseEventArgs e)
			{
				OnChosen();
			}

			void image_ButtonPressed(object sender, ImageButtonEventArgs args)
			{
				OnChosen();
			}

			void OnChosen()
			{
				Chosen?.Invoke(this, EventArgs.Empty);
			}

			protected override void OnSizeChanged(EventArgs e)
			{
				base.OnSizeChanged(e);
				DoSize();
			}

			void DoSize()
			{
				if (imageFilename != null)
				{
					label.Location = new Point(margin + imageSize + margin, 0);
				}
				else
				{
					label.Location = new Point(margin, 0);
				}

				if (image != null)
				{
					image.Location = new Point(margin, 0);
					image.Size = new Size(imageSize, imageSize);
				}

				var labelHeight = Height;
				if (IsLast && (Menu.CornerRounding == 0))
				{
					labelHeight -= 2;
				}

				label.Size = new Size(Width - margin - label.Left, labelHeight);
			}

			public void SetColours(Color backColour, Color foreColour)
			{
				this.backColour = backColour;
				BackColor = backColour;
				label.ForeColor = foreColour;
			}

			protected override void OnPaint(PaintEventArgs e)
			{
				base.OnPaint(e);

				if (isDivider && drawDividerLine)
				{
					var dividerHeight = 1;
					var margin = 8;

					e.Graphics.FillRectangle(Brushes.Gray, margin, (Height - dividerHeight) / 2, Width - (2 * margin), dividerHeight);
				}

				e.Graphics.DrawLine(Pens.Black, 0, 0, 0, Height);
				e.Graphics.DrawLine(Pens.Black, Width - 2, 0, Width - 2, Height);

				if (IsLast)
				{
					e.Graphics.DrawLine(Pens.Black, cornerRounding, Height - 2, Width - 1, Height - 2);
				}
			}
		}

		readonly List<PopupMenuItem> items;

		int minimumWidth;
		public int MinimumWidth
		{
			get => minimumWidth;

			set
			{
				minimumWidth = value;
				CorrectSize();
			}
		}

		int cornerRounding;
		public int CornerRounding
		{
			get => cornerRounding;

			set => SetCornerRounding(value);
		}

		ImageBox graduation;

		public PopupMenu()
		{
			items = new List<PopupMenuItem>();

			ShowInTaskbar = false;
			FormBorderStyle = FormBorderStyle.None;
			StartPosition = FormStartPosition.Manual;

			Height = 0;
		}

		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);
			DoSize();
		}

		protected override void OnVisibleChanged(EventArgs e)
		{
			base.OnVisibleChanged(e);

			CorrectSize();
		}

		public void CorrectSize()
		{
			var width = minimumWidth;
			var height = 0;
			foreach (var item in items)
			{
				var size = item.GetPreferredSize(Size.Empty);
				width = Math.Max(width, size.Width);
			}

			foreach (var item in items)
			{
				var size = item.GetPreferredSize(Size.Empty);
				size.Width = width;
				if ((cornerRounding == 0)
					&& item.IsLast)
				{
					size.Height += 1;
				}
				item.Size = size;

				height += size.Height;
			}

			Size = new Size(width, height);
		}

		void DoSize()
		{
			var y = 0;
			foreach (var item in items)
			{
				item.Width = Width;

				if ((graduation != null)
					&& (item.Parent == graduation))
				{
					item.Location = new Point(-graduation.Left, y - graduation.Top);
					y = item.Bottom + graduation.Top;
				}
				else
				{
					item.Location = new Point(0, y);
					y = item.Bottom;
				}
			}

			using (var graphics = CreateGraphics())
			{
				using (var path = RoundedRectangle.GetRoundedRectanglePath(new Rectangle(0, 0, Width, Height), cornerRounding))
				{
					var region = new Region(path);
					SetWindowRgn(Handle, region.GetHrgn(graphics), true);
				}
			}

			if (graduation != null)
			{
				graduation.Location = new Point(-10, -10);
				graduation.Size = new Size(Width + (2 * 10), 42);
				graduation.SizeMode = PictureBoxSizeMode.StretchImage;
			}
		}

		void graduation_Click(object sender, EventArgs args)
		{
			OnClick(args);
		}

		public PopupMenuItem AddHeading(string text)
		{
			var item = new PopupMenuItem(text, null, 11, FontStyle.Bold);
			items.Add(item);
			item.Clickable = false;
			item.SetColours(Color.Transparent, Color.Black);
			item.CornerRounding = cornerRounding;
			Height += item.Height;
			CorrectSize();

			CreateGraduation();
			graduation.Controls.Add(item);
			DoSize();

			return item;
		}

		void CreateGraduation()
		{
			graduation = new ImageBox
			{
				ImageLocation = AppInfo.TheInstance.Location + @"\images\lozenges\graduation.png"
			};
			graduation.Click += graduation_Click;
			Controls.Add(graduation);

			DoSize();
		}

		public PopupMenuItem AddHeading(string text, string imageFilename)
		{
			var divider = AddDivider(6, false);
			Controls.Remove(divider);
			divider.SetColours(Color.Transparent, Color.Black);

			var item = new PopupMenuItem(text, imageFilename, 10, FontStyle.Bold);
			items.Add(item);
			item.Clickable = false;
			item.SetColours(Color.Transparent, Color.Black);
			item.CornerRounding = cornerRounding;
			CorrectSize();

			CreateGraduation();
			graduation.Controls.Add(divider);
			graduation.Controls.Add(item);
			DoSize();

			return item;
		}

		public PopupMenuItem AddItem(string text, string imageFilename = "", string checkedImageFilename = "")
		{
			var item = new PopupMenuItem(text, imageFilename, checkedImageFilename, 9, FontStyle.Regular);
			items.Add(item);
			item.Clickable = true;
			item.CornerRounding = cornerRounding;
			Controls.Add(item);
			CorrectSize();

			return item;
		}

		protected override void OnLostFocus(EventArgs e)
		{
			base.OnLostFocus(e);
			Close();
		}

		void SetCornerRounding(int cornerRounding)
		{
			this.cornerRounding = cornerRounding;
			DoSize();
		}

		public PopupMenuItem AddDivider()
		{
			return AddDivider(8, true);
		}

		public PopupMenuItem AddDivider(int height, bool drawLine)
		{
			var item = new PopupMenuItem("", null, 10, FontStyle.Regular)
			{
				IsDivider = true,
				DividerHeight = height,
				DrawDividerLine = drawLine,
				Clickable = false,
				CornerRounding = cornerRounding
			};
			items.Add(item);
			Height += item.Height;
			Controls.Add(item);
			CorrectSize();

			return item;
		}

		List<Control> GetAllControls(Control parent)
		{
			var controls = new List<Control> { parent };

			foreach (Control control in parent.Controls)
			{
				controls.AddRange(GetAllControls(control));
			}

			return controls;
		}

		public IList<PopupMenuItem> Items => new List<PopupMenuItem>(items);

		public void Show(Control parentContainer, Control showingControl, Point screenPosition, bool ignoreOverlap = false)
		{
			var menuLocation = screenPosition;

			var containerTopLeftScreen = parentContainer.PointToScreen(new Point(0, 0));
			var containerBottomRightScreen = parentContainer.PointToScreen(new Point(parentContainer.Width, parentContainer.Height));

			var containerBounds = new Rectangle(containerTopLeftScreen.X, containerTopLeftScreen.Y, containerBottomRightScreen.X - containerTopLeftScreen.X, containerBottomRightScreen.Y - containerTopLeftScreen.Y);

			menuLocation.X = Math.Max(menuLocation.X, containerBounds.Left);
			menuLocation.Y = Math.Max(menuLocation.Y, containerBounds.Top);

			menuLocation.X = Math.Min(menuLocation.X, containerBounds.Right - Width);
			menuLocation.Y = Math.Min(menuLocation.Y, containerBounds.Bottom - Height);

			// Would the popup overlap us?
			if (showingControl != null)
			{
				var ourTopLeft = showingControl.PointToScreen(new Point(0, 0));
				if ((new Rectangle(menuLocation, Size)).IntersectsWith(new Rectangle(ourTopLeft, showingControl.Size)) && !ignoreOverlap)
				{
					// Is there room to move it up?
					if ((ourTopLeft.Y - Height) >= containerBounds.Top)
					{
						menuLocation.Y = ourTopLeft.Y - Height;
					}
				}
			}

			Location = menuLocation;
			Show(parentContainer);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			e.Graphics.DrawRectangle(Pens.Black, 0, 0, Width - 2, Height - 2);
		}
	}

}
