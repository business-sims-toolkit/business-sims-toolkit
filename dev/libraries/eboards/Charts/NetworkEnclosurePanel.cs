using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml;

using Algorithms;
using CommonGUI;
using CoreUtils;
using ResizingUi;
using ResizingUi.Interfaces;

namespace Charts
{
	public class NetworkEnclosurePanel : BoxChart
	{ 
		public NetworkEnclosurePanel(XmlElement xml)
		:base(xml)
		{
			enclosureName = xml.GetAttribute("name");

			containingPanel = new Panel
			{
				AutoScroll = true
			};
			Controls.Add(containingPanel);

			sections = xml.ChildNodes.Cast<XmlElement>().Select(n =>
			{
				var section = new Section(n);
				section.FontSizeToFitChanged += section_FontSizeToFitChanged;
				containingPanel.Controls.Add(section);
				return section;
			}).ToList();
		}

		public override float FontSize
		{
			get => sections.Min(s => s.FontSize);
			set
			{
				foreach (var section in sections)
				{
					section.FontSize = value;
				}
			}
		}

		float fontSizeToFit;
		public override float FontSizeToFit
		{
			get => fontSizeToFit;
			protected set
			{
				if (Math.Abs(fontSizeToFit - value) > float.Epsilon)
				{

					fontSizeToFit = value;
					OnFontSizeToFitChanged();
				}
			}
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			DoSize();
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			using (var borderPen = new Pen(Color.FromArgb(229, 229, 229), 4))
			{
				e.Graphics.DrawRectangle(borderPen, new Rectangle(0, 0, Width, Height));
			}

			var titleBounds = new RectangleF(padding, padding, Width - 2 * padding, titleHeight);

			using (var titleTextBrush = new SolidBrush(Color.Black))
			{
				using (var titleFont = this.GetFontToFit(FontStyle.Bold, "XXXXXXXXXX", titleBounds.Size))
				{
					e.Graphics.DrawString(enclosureName, titleFont, titleTextBrush, titleBounds, 
						new StringFormat
						{
							Alignment = StringAlignment.Center,
							LineAlignment = StringAlignment.Center
						});
				}
			}
		}

		void DoSize()
		{
			titleHeight = Maths.Clamp(Height * 0.1f, 30, 50);

			containingPanel.Bounds = new Rectangle(0, 0, Width, Height).AlignRectangle(Width - 2 * padding, (int)(Height - titleHeight - 2 * padding), StringAlignment.Center, StringAlignment.Far, 0, padding);

			var sectionTitleHeight = Maths.Clamp(containingPanel.Height * 0.08f, Section.MinimumTitleHeight, Section.MaximumTitleHeight);

			foreach (var section in sections)
			{
				section.TitleHeight = sectionTitleHeight;
				section.Size = new Size(containingPanel.Width, (containingPanel.Height - (sections.Count - 1) * padding) / sections.Count);
			}
			
			var y = 0;
			foreach (var section in sections)
			{
				section.BackColor = Color.White;
				section.Height = section.PreferredHeight;
				section.Location = new Point(0, y);
				y = section.Bottom + padding;
			}

			UpdateFontSizes();

			Invalidate();
		}

		void UpdateFontSizes()
		{
			if (! sections.Any())
			{
				return;
			}
			FontSizeToFit = !sections.Any() ? 9 : sections.Min(s => s.FontSizeToFit);
		}
		

		void section_FontSizeToFitChanged(object sender, EventArgs e)
		{
			UpdateFontSizes();
		}

		readonly Panel containingPanel;
		float titleHeight;

		const int padding = 4;
		

		readonly string enclosureName;
		readonly List<Section> sections;



		// ******** Section ******** 
		// *************************

		class Section : FlickerFreePanel, IDynamicSharedFontSize
		{
			public Section(XmlElement xml)
			{
				sectionTitle = xml.GetAttribute("name");

				appNames = xml.ChildNodes.Cast<XmlElement>().Select(n => n.GetAttribute("display_text")).ToList();
			}

			public int PreferredHeight => (int)(titleHeight + appNames.Count * labelHeight + sectionPadding);

			public float TitleHeight
			{
				set
				{
					titleHeight = value;

					DoSize();
				}
			}
			float titleHeight;


			public float FontSize
			{
				get => fontSize;
				set
				{
					fontSize = value;
					Invalidate();
				}
			}
			float fontSize;

			float fontSizeToFit;

			public float FontSizeToFit
			{
				get => fontSizeToFit;
				private set
				{
					if (Math.Abs(fontSizeToFit - value) > float.Epsilon)
					{
						fontSizeToFit = value;
						OnFontSizeToFitChanged();
					}
				}
			}
			public event EventHandler FontSizeToFitChanged;

			public const float MaximumTitleHeight = 30;
			public const float MinimumTitleHeight = 20;

			protected override void OnSizeChanged(EventArgs e)
			{
				DoSize();
			}

			protected override void OnPaint(PaintEventArgs e)
			{
				using (var backBrush = new SolidBrush(BackColor))
				{
					e.Graphics.FillRectangle(backBrush, new Rectangle(0, 0, Width, Height));
				}

				var sf = new StringFormat
				{
					Alignment = StringAlignment.Center,
					LineAlignment = StringAlignment.Center
				};

				using (var titleBackBrush = new SolidBrush(SkinningDefs.TheInstance.GetColorData("network_report_section_back_colour", Color.FromArgb(238, 238, 238))))
				using (var titleTextBrush = new SolidBrush(SkinningDefs.TheInstance.GetColorData("network_report_section_text_colour", Color.Black)))
				{
					var titleBounds = new RectangleF(0, 0, Width, titleHeight);

					e.Graphics.FillRectangle(titleBackBrush, titleBounds);

					using (var titleFont = this.GetFontToFit(FontStyle.Bold, "XXXXXXXXXXXX", titleBounds.Size))
					{
						e.Graphics.DrawString(sectionTitle, titleFont, titleTextBrush, titleBounds, sf);
					}
				}

				var servicesForeColour = Color.Black;

				using (var textBrush = new SolidBrush(servicesForeColour))
				using (var appFont = SkinningDefs.TheInstance.GetPixelSizedFont(fontSize))
				{
					var y = titleHeight + sectionPadding;
					
					foreach (var app in appNames)
					{
						var labelBounds = new RectangleF(0, y, Width, labelHeight);
						e.Graphics.DrawString(app, appFont, textBrush, labelBounds, sf);
						y = labelBounds.Bottom;
					}
				}
			}


			void DoSize()
			{

				var remainingHeight = Height - titleHeight - sectionPadding;

				labelHeight = Math.Min(maximumLabelHeight,
					Math.Max(minimumLabelHeight, remainingHeight / appNames.Count));

				var labelSizes = appNames.Select(a => new SizeF(Width, labelHeight)).ToList();

				FontSizeToFit = !appNames.Any() ? 9 : Math.Max(9, this.GetFontSizeInPixelsToFit(FontStyle.Regular, appNames, labelSizes));

			}

			void OnFontSizeToFitChanged()
			{
				FontSizeToFitChanged?.Invoke(this, EventArgs.Empty);
			}

			readonly List<string> appNames;
			readonly string sectionTitle;

			const float minimumLabelHeight = 10;
			const float maximumLabelHeight = 20;

			const int sectionPadding = 4;

			float labelHeight;

		}



	}
}
