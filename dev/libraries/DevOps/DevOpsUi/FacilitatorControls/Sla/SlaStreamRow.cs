using System;
using System.Drawing;
using System.Windows.Forms;

using Algorithms;
using CommonGUI;
using CoreUtils;
using LibCore;
using Network;
using ResizingUi;
using ResizingUi.Interfaces;

namespace DevOpsUi.FacilitatorControls.Sla
{
	internal class SlaStreamRow : Panel, IDynamicSharedFontSize
	{
		public SlaStreamRow (Node slaNode, SlaStream stream, bool includeStreamColumn)
		{
			this.slaNode = slaNode;

			if (includeStreamColumn)
			{
				streamLabel = new Label
				{
					Text = FormatStreamRange(stream.MinRevenueStreams, stream.MaxRevenueStreams),
					BackColor = SkinningDefs.TheInstance.GetColorData("sla_background", Color.Transparent),
					ForeColor = SkinningDefs.TheInstance.GetColorData("sla_foreground", Color.Black),
					TextAlign = ContentAlignment.MiddleCenter
				};
				Controls.Add(streamLabel);
			}

			mtrsBox = new FilteredTextBox(TextBoxFilterType.Custom)
			{
				ShortcutsEnabled = false,
				MaxLength = 1,
				TextAlign = HorizontalAlignment.Center,
				Text= CONVERT.ToStr(slaNode.GetIntAttribute("slalimit", 0) / 60)
			};
			Controls.Add(mtrsBox);

			mtrsBox.ValidateInput += mtrsBox_ValidateInput;
			mtrsBox.TextChanged += mtrsBox_TextChanged;

			tickLabel = new Label
			{
				BackgroundImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + @"\images\chart\tick.png"),
				BackgroundImageLayout = ImageLayout.Stretch,
				Visible = false
			};
			Controls.Add(tickLabel);

			interval = 500;
			timeoutDuration = 2000;
			timer = new Timer
			{
				Interval = interval
			};
			timer.Tick += timer_Tick;

			DoSize();
		}

		public float FontSize
		{
			get => fontSize;
			set
			{
				fontSize = value;
				UpdateFontSize();
			}
		}

		public float FontSizeToFit
		{
			get => fontSizeToFit;
			set
			{
				if (Math.Abs(fontSizeToFit - value) > float.Epsilon)
				{
					fontSizeToFit = value;
					FontSizeToFitChanged?.Invoke(this, EventArgs.Empty);
				}
			}
		}
		public event EventHandler FontSizeToFitChanged;

		public float StreamColumnFraction
		{
			set
			{
				streamColumnFraction = value;
				DoSize();
			}
		}

		public float MtrsColumnFraction
		{
			set
			{
				mtrsColumnFraction = value;
				DoSize();
			}
		}

		public int ColumnGap
		{
			set
			{
				columnGap = value;
				DoSize();
			}
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			DoSize();
		}

		protected override void OnVisibleChanged(EventArgs e)
		{
			if (Visible)
			{
				tickLabel.Hide();
				UpdateFields();
			}
		}

		void UpdateFontSize ()
		{
			if (streamLabel == null)
			{
				return;
			}

			streamLabel.Font = SkinningDefs.TheInstance.GetPixelSizedFont(fontSize);
			Invalidate();
		}

		void DoSize ()
		{
			var widthSansPadding = Width - (streamLabel != null ? 4 : 3) * columnGap;

			var streamColumnWidth = (int)(widthSansPadding * streamColumnFraction);

			if (streamLabel != null)
			{
				streamLabel.Bounds = new Rectangle(0, 0, streamColumnWidth, Height);

				FontSize = FontSizeToFit = this.GetFontSizeInPixelsToFit(FontStyle.Regular, streamLabel.Text, streamLabel.Size);
			}

			var boxLeft = (streamLabel?.Right + columnGap) ?? columnGap;

			var boxWidth = (int)(widthSansPadding * mtrsColumnFraction);

			mtrsBox.Bounds = new Rectangle(0, 0, Width, Height).AlignRectangle(boxWidth, mtrsBox.Height, StringAlignment.Near, StringAlignment.Center, boxLeft);

			var tickSize = mtrsBox.Height;

			tickLabel.Bounds = new Rectangle(mtrsBox.Right + columnGap, mtrsBox.Top, tickSize, tickSize);
		}

		void mtrsBox_TextChanged(object sender, EventArgs e)
		{
			if (!string.IsNullOrEmpty(mtrsBox.Text))
			{
				if (CONVERT.ParseIntSafe(mtrsBox.Text) == null)
				{
					return;
				}

				elapsedTime = 0;
				timer.Start();
				tickLabel.Hide();
			}
		}

		bool mtrsBox_ValidateInput(FilteredTextBox sender, KeyPressEventArgs e)
		{
			var digit = e.KeyChar - '0';

			return digit >= 1 && digit <= 9 || char.IsControl(e.KeyChar);
		}

		void timer_Tick(object sender, EventArgs e)
		{
			elapsedTime += interval;

			if (elapsedTime >= timeoutDuration)
			{
				timer.Stop();
				// TODO show tick
				tickLabel.Show();
				var value = CONVERT.ParseIntSafe(mtrsBox.Text);

				if (value != null)
				{
					var time = 60 * value;
					slaNode.SetAttribute("slalimit", time.Value);
				}

			}
		}

		void UpdateFields()
		{// TODO set flag to prevent timer starting from this
			mtrsBox.Text = CONVERT.ToStr(slaNode.GetIntAttribute("slalimit", 0) / 60);
		}

		int columnGap = 5;
		float streamColumnFraction;
		float mtrsColumnFraction;

		float fontSize;
		float fontSizeToFit;


		readonly Node slaNode;

		readonly Label streamLabel;
		readonly FilteredTextBox mtrsBox;
		readonly Label tickLabel;

		readonly Timer timer;
		readonly int interval;
		readonly int timeoutDuration;
		int elapsedTime;

		static string FormatStreamRange (int min, int max)
		{
			// Assuming min < max
			return max == min ? CONVERT.ToStr(min) : $"{min} - {max}";
		}

	}
}
