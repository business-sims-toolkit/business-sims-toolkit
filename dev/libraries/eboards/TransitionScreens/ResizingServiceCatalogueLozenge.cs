using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Algorithms;
using CommonGUI;
using CoreUtils;
using LibCore;
using Network;

namespace TransitionScreens
{
	public class ResizingServiceCatalogueLozenge : FlickerFreePanel, IComparable<ResizingServiceCatalogueLozenge>
	{
		Node service;

		FontStyle fontStyle;

		int instep;
		Rectangle iconBox;
		RectangleF titleBox;

		string text;

		public ResizingServiceCatalogueLozenge (Node service)
		{
			this.service = service;
			service.AttributesChanged += service_AttributesChanged;
			service.ParentChanged += service_ParentChanged;

			fontStyle = FontStyle.Bold;
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing)
			{
				service.AttributesChanged -= service_AttributesChanged;
				service.ParentChanged -= service_ParentChanged;
			}

			base.Dispose(disposing);
		}

		void service_AttributesChanged (Node sender, ArrayList attributes)
		{
			DoSize();
		}

		void service_ParentChanged (Node parent, Node child)
		{
			DoSize();
			OnChanged();
		}

		public event EventHandler Changed;

		void OnChanged ()
		{
			Changed?.Invoke(this, EventArgs.Empty);
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);
			DoSize();
		}

		void DoSize ()
		{
			instep = Height / 16;
			iconBox = new Rectangle (instep, instep, Height - (2 * instep), Height - (2 * instep));
			titleBox = new RectangleFFromBounds { Left = iconBox.Right + (Height / 4), Top = 0, Right = Width - (Width / 16), Height = Height }.ToRectangleF();

			text = Description;
			desiredFontSize = ResizingUi.FontScalerExtensions.GetFontSizeInPixelsToFit(this, fontStyle, Description, titleBox.Size);

			if (desiredFontSize < 10)
			{
				var words = Description.SplitOnWhitespace();

				if (words.Length > 1)
				{
					var midpoint = words.Length / 2;

					text = string.Join(" ", words, 0, midpoint) + System.Environment.NewLine + string.Join(" ", words, midpoint, words.Length - midpoint);
					desiredFontSize = ResizingUi.FontScalerExtensions.GetFontSizeInPixelsToFit(this, fontStyle, text, titleBox.Size);
				}
			}

			DesiredFontSize = desiredFontSize;

			Invalidate();
		}

		public bool IsRetired => (service.Parent.GetAttribute("type") == "retired_biz_services");

		float desiredFontSize;

		public float DesiredFontSize
		{
			get => desiredFontSize;

			private set
			{
				if (value != desiredFontSize)
				{
					desiredFontSize = value;
					OnDesiredFontSizeChanged();
				}
			}
		}

		public event EventHandler DesiredFontSizeChanged;

		void OnDesiredFontSizeChanged ()
		{
			DesiredFontSizeChanged?.Invoke(this, EventArgs.Empty);
		}

		float fontSize;

		public float FontSize
		{
			get => fontSize;

			set
			{
				fontSize = value;
				Invalidate();
			}
		}

		public int CompareTo (ResizingServiceCatalogueLozenge other)
		{
			if (IsRetired && ! other.IsRetired)
			{
				return 1;
			}
			else if ((! IsRetired) && other.IsRetired)
			{
				return -1;
			}
			else
			{
				return Description.CompareTo(other.Description);
			}
		}

		public string Description => service.GetAttribute("desc").Replace("\n", " ");

		protected override void OnPaint (PaintEventArgs e)
		{
			base.OnPaint(e);

			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

			var colour = (IsRetired ? SkinningDefs.TheInstance.GetColorData("transition_service_lozenge_colour_retired") : SkinningDefs.TheInstance.GetColorData("transition_service_lozenge_colour_active"));
			using (var brush = new SolidBrush (colour))
			{
				e.Graphics.FillRectangle(brush, new Rectangle (0, 0, Width, Height));
			}

			e.Graphics.DrawImage(Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + @"\images\icons\" + service.GetAttribute("icon") + ".png"), iconBox);

			using (var font = SkinningDefs.TheInstance.GetPixelSizedFont(fontSize, fontStyle))
			using (var brush = new SolidBrush(Color.FromArgb(IsRetired ? 64 : 255, Color.White)))
			{
				e.Graphics.DrawString(text, font, brush, titleBox, new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center });
			}
		}
	}
}