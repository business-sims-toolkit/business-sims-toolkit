using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;

using CommonGUI;
using Network;

namespace ResizingUi
{
	public enum PanelLayout
	{
		LeftToRight,
		TopToBottom
	}

	public class AttributeDisplayPanel : FlickerFreePanel
	{
		public AttributeDisplayPanel (Node nodeToWatch, string title, Func<Node, string> textFormatter, 
		                              string valueSizingReference, string titleSizingReference, float titleSizeFraction = 0.5f)
		{
			monitoredNode = nodeToWatch;
			this.title = title;
			this.textFormatter = textFormatter;
			this.valueSizingReference = valueSizingReference;
			this.titleSizingReference = titleSizingReference;
			this.titleSizeFraction = titleSizeFraction;

			monitoredNode.AttributesChanged += monitoredNode_AttributesChanged;
		}

		public string Title
		{
			get => title;
			set
			{
				title = value;
				Invalidate();
			}
		}

		public Color TitleForeColour
		{
			get => titleForeColour;
			set
			{
				titleForeColour = value;
				Invalidate();
			}
		}

		public Color TitleBackColour
		{
			get => titleBackColour;
			set
			{
				titleBackColour = value;
				Invalidate();
			}
		}

		public Color ValueForeColour
		{
			get => valueForeColour;
			set
			{
				valueForeColour = value;
				Invalidate();
			}
		}

		public Color ValueBackColour
		{
			get => valueBackColour;
			set
			{
				valueBackColour = value;
				Invalidate();
			}
		}

		public PanelLayout PanelOrientation
		{
			get => panelOrientation;
			set
			{
				panelOrientation = value;
				Invalidate();
			}
		}

		public float TitleSizeFraction
		{
			get => titleSizeFraction;
			set
			{
				titleSizeFraction = value;
				Invalidate();
			}
		}

		public string TitleSizingRefefence
		{
			get => titleSizingReference;

			set
			{
				titleSizingReference = value;
				Invalidate();
			}
		}

		public string ValueSizingReference
		{
			get => valueSizingReference;

			set
			{
				valueSizingReference = value;
				Invalidate();
			}
		}

		public FontStyle TitleFontStyle
		{
			get => titleFontStyle;

			set
			{
				titleFontStyle = value;
				Invalidate();
			}
		}

		public FontStyle ValueFontStyle
		{
			get => valueFontStyle;

			set
			{
				valueFontStyle = value;
				Invalidate();
			}
		}

		public StringAlignment? TitleAlignment
		{
			get => titleAlignment;
			set
			{
				titleAlignment = value;
				Invalidate();
			}
		}

		public StringAlignment TitleLineAlignment
		{
			get => titleLineAlignment;
			set
			{
				titleLineAlignment = value;
				Invalidate();
			}
		}

		public StringAlignment? ValueAlignment
		{
			get => valueAlignment;
			set
			{
				valueAlignment = value;
				Invalidate();
			}
		}

		public StringAlignment ValueLineAlignment
		{
			get => valueLineAlignment;
			set
			{
				valueLineAlignment = value;
				Invalidate();
			}
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing)
			{
				monitoredNode.AttributesChanged -= monitoredNode_AttributesChanged;
			}

			base.Dispose(disposing);
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			Invalidate();
		}

		protected override void OnPaint (PaintEventArgs e)
		{
			RectangleF titleBounds;
			RectangleF valueBounds;

			StringAlignment titleHorizontalAlignment;
			StringAlignment valueHorizontalAlignment;

			switch (panelOrientation)
			{
				case PanelLayout.LeftToRight:
					titleBounds = new RectangleF(0, 0, Width * titleSizeFraction, Height);
					valueBounds = new RectangleF(titleBounds.Right, 0, Width - titleBounds.Width, Height);

					titleHorizontalAlignment = titleAlignment ?? StringAlignment.Near;
					valueHorizontalAlignment = valueAlignment ?? StringAlignment.Far;
					break;
				case PanelLayout.TopToBottom:
					titleBounds = new RectangleF(0, 0, Width, Height * titleSizeFraction);
					valueBounds = new RectangleF(0, titleBounds.Bottom, Width, Height - titleBounds.Height);

					titleHorizontalAlignment = titleAlignment ?? StringAlignment.Center;
					valueHorizontalAlignment = valueAlignment ?? StringAlignment.Center;
					break;
				default:
					throw new Exception("Unhandled layout");
			}

			RenderText(e.Graphics, titleBounds, new StringFormat {Alignment = titleHorizontalAlignment, LineAlignment = titleLineAlignment}, titleBackColour, titleForeColour, title, titleSizingReference, titleFontStyle);

			var value = textFormatter(monitoredNode);

			RenderText(e.Graphics, valueBounds, new StringFormat { Alignment = valueHorizontalAlignment, LineAlignment = valueLineAlignment }, valueBackColour, valueForeColour, value, valueSizingReference, valueFontStyle);
		
		}

		void RenderText (Graphics graphics, RectangleF bounds, StringFormat alignment, Color backColour,
		                        Color foreColour, string text, string sizingReference, FontStyle fontStyle)
		{
			using (var titleBackBrush = new SolidBrush(backColour))
			{
				graphics.FillRectangle(titleBackBrush, bounds);
			}

			using (var titleFont = this.GetFontToFit(fontStyle, sizingReference, bounds.Size))
			using (var titleBrush = new SolidBrush(foreColour))
			{
				graphics.DrawString(text, titleFont, titleBrush, bounds, alignment);
			}
		}


		void monitoredNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			Invalidate();
		}


		readonly Node monitoredNode;

		readonly Func<Node, string> textFormatter;


		string title;

		PanelLayout panelOrientation;

		FontStyle valueFontStyle = FontStyle.Regular;
		FontStyle titleFontStyle = FontStyle.Regular;

		float titleSizeFraction;

		string valueSizingReference;
		string titleSizingReference;

		Color titleBackColour;
		Color titleForeColour;
		Color valueBackColour;
		Color valueForeColour;

		StringAlignment? titleAlignment;
		StringAlignment titleLineAlignment = StringAlignment.Center;

		StringAlignment? valueAlignment;
		StringAlignment valueLineAlignment = StringAlignment.Center;

	}
}
