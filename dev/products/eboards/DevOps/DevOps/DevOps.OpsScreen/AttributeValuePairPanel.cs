using System;
using System.Drawing;
using System.Windows.Forms;

using CommonGUI;

namespace DevOps.OpsScreen
{
	internal class AttributeValuePairPanel : FlickerFreePanel
	{
		string attributeSizingReference;
		string valueSizingReference;

		string attributeText = "";
		string valueText = "";

		public string AttributeText
		{
			get => attributeText;

			set
			{
				attributeText = value;
				Invalidate();
			}
		}

		public string ValueText
		{
			get => valueText;

			set
			{
				valueText = value;
				Invalidate();
			}
		}

		Color attributeColour = Color.Black;
		Color valueColour = Color.Black;

		public Color AttributeColour
		{
			get => attributeColour;

			set
			{
				attributeColour = value;
				Invalidate();
			}
		}

		public Color ValueColour
		{
			get => valueColour;

			set
			{
				valueColour = value;
				Invalidate();
			}
		}

		Color attributeBackColour = Color.White;
        Color valueBackColour = Color.White;

        public Color AttributeBackColour
        {
            get => attributeBackColour;

	        set
	        {
		        attributeBackColour = value;
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

		public string AttributeSizingRefefence
		{
			get => attributeSizingReference;

			set
			{
				attributeSizingReference = value;
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

		float attributeSizeFraction;

		public float AttributeSizeFraction
		{
			get => attributeSizeFraction;

			set
			{
				attributeSizeFraction = value;
				Invalidate();
			}
		}

		FontStyle attributeFontStyle = FontStyle.Regular;
		FontStyle valueFontStyle = FontStyle.Regular;

		public FontStyle AttributeFontStyle
		{
			get => attributeFontStyle;

			set
			{
				attributeFontStyle = value;
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

		public enum PanelLayout
		{
			LeftToRight,
			TopToBottom
		}

		PanelLayout orientation = PanelLayout.LeftToRight;

		public PanelLayout Orientation
		{
			get => orientation;

			set
			{
				orientation = value;
				Invalidate();
			}
		}

		public AttributeValuePairPanel (string attributeText, PanelLayout orientation, float attributeSizeFraction, string attributeSizingReference, string valueSizingReference)
		{
			this.attributeSizeFraction = attributeSizeFraction;
			this.attributeSizingReference = attributeSizingReference;
			this.valueSizingReference = valueSizingReference;

			AttributeText = attributeText;

			Orientation = orientation;
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);

			Invalidate();
		}

		protected override void OnPaint (PaintEventArgs e)
		{
			RectangleF label;
			RectangleF value;
			StringAlignment labelAlignment;
			StringAlignment valueAlignment;

            switch (Orientation)
            {
                case PanelLayout.LeftToRight:
	                label = new RectangleF (0, 0, Width * attributeSizeFraction, Height);
					labelAlignment = StringAlignment.Near;
					value = new RectangleF (label.Right, 0, Width - label.Right, Height);
	                valueAlignment = StringAlignment.Far;
					break;

				case PanelLayout.TopToBottom:
					label = new RectangleF (0, 0, Width, Height * attributeSizeFraction);
					labelAlignment = StringAlignment.Center;
					value = new RectangleF (0, Height / 2.0f, Width, Height / 2.0f);
					valueAlignment = StringAlignment.Center;
                    break;

				default:
					throw new Exception ("Unhandled layout");
			}

			if (attributeBackColour != BackColor)
			{
                using (Brush backBrush = new SolidBrush (attributeBackColour))
                {
                    e.Graphics.FillRectangle(backBrush, label);
                }
            }

			using (var attributeFont = ResizingUi.FontScalerExtensions.GetFontToFit(this, attributeFontStyle, attributeSizingReference, label.Size))
			using (Brush brush = new SolidBrush (attributeColour))
			{
				e.Graphics.DrawString(attributeText, attributeFont, brush, label, new StringFormat { Alignment = labelAlignment, LineAlignment = StringAlignment.Center });
			}
            
			if (valueBackColour != BackColor)
			{
                using (Brush backBrush = new SolidBrush (valueBackColour))
                {
	                e.Graphics.FillRectangle(backBrush, value);
                }
            }

			using (var valueFont = ResizingUi.FontScalerExtensions.GetFontToFit(this, valueFontStyle, valueSizingReference, value.Size))
			using (Brush brush = new SolidBrush (valueColour))
			{
				e.Graphics.DrawString(valueText, valueFont, brush, value, new StringFormat { Alignment = valueAlignment, LineAlignment = StringAlignment.Center });
			}
		}

        protected override void OnBackColorChanged (EventArgs e)
        {
            base.OnBackColorChanged(e);

            attributeBackColour = BackColor;
            valueBackColour = BackColor;
	        Invalidate();
		}
	}
}