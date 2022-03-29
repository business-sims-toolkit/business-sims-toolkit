using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

using Algorithms;
using CommonGUI;
using CoreUtils;
using LibCore;
using Media;
using Network;
using ResizingUi.Button;

namespace ResizingUi
{
	public class TimedMessageDisplay : FlickerFreePanel
	{
		public TimedMessageDisplay(int displayDurationInSeconds = 10, bool participantOnlyMessages = true)
		{
			this.participantOnlyMessages = participantOnlyMessages;
			Visible = false;

			watchedNodesToAttributes = new Dictionary<Node, WatchedNodeAttributes>();
			messageQueue = new List<Message>();

			soundPlayer = new PolyphonicSoundPlayer();

			displayDurationMs = displayDurationInSeconds * 1000;
			displayIntervalMs = 100;
			displayTimer = new Timer
			{
				Interval = displayIntervalMs
			};
			displayTimer.Tick += displayTimer_Tick;

			closeButton = new StyledImageButton("progression_panel", 0, false)
			{
				BackColor = Color.Transparent,
				UseCircularBackground = true,
				Margin = new Padding(4),
				Size = new Size(20, 20)
			};
			closeButton.SetVariants(@"\images\buttons\cross.png");
			Controls.Add(closeButton);
			closeButton.Click += closeButton_Click;
		}

		public void AddNodeToWatch(Node nodeToWatch, string titleAttribute, string textAttribute)
		{
			watchedNodesToAttributes.Add(nodeToWatch, new WatchedNodeAttributes
			{
				TitleAttribute = titleAttribute,
				TextAttribute = textAttribute
			});

			nodeToWatch.ChildAdded += watchedNode_ChildAdded;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				foreach (var node in watchedNodesToAttributes.Keys)
				{
					node.ChildAdded -= watchedNode_ChildAdded;
				}
			}

			base.Dispose(disposing);
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			closeButton.Bounds = ClientRectangle.AlignRectangle(closeButton.Size, StringAlignment.Far, StringAlignment.Near, -10, 10);

			Invalidate();
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			if (!messageQueue.Any())
				return;

			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
			e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
			e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
			e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

			using (var backBrush = new SolidBrush(SkinningDefs.TheInstance.GetColorData("pop_up_panel_background_colour")))
			using (var textBrush = new SolidBrush(CONVERT.ParseHtmlColor("#ff2d4e")))
			using (var countdownBrush = new SolidBrush(Color.FromArgb(166, 94, 100, 104)))

			{
				var overallBounds = new RectangleF(0, 0, Width, Height);
				e.Graphics.FillRectangle(Brushes.Black, overallBounds);

				var borderSize = Math.Min(Width * 0.01f, Height * 0.01f);

				var contentBounds = overallBounds.ExpandByAmount(-borderSize);

				e.Graphics.FillRectangle(backBrush, contentBounds);

				const int countdownWidth = 40;
				const int padding = 10;

				var countdownBounds = contentBounds.AlignRectangle(countdownWidth, countdownWidth, StringAlignment.Far, StringAlignment.Far, -padding * 1.5f, -padding);

				var message = messageQueue[0];

				if (! string.IsNullOrEmpty(message.Sound) && !message.SoundPlayed)
				{
					message.SoundPlayed = true;
					
				}
				
				RectangleF? titleBounds = null;

				if (!string.IsNullOrEmpty(message.Title))
				{
					titleBounds = new RectangleFFromBounds
					{
						Left = contentBounds.Left,
						Top = contentBounds.Top,
						Height = Height * 0.3f,
						Right = countdownBounds.Left - padding
					}.ToRectangleF();
				}

				if (titleBounds != null)
				{
					using (var titleFont = SkinningDefs.TheInstance.GetFont(20))// this.GetFontToFit(FontStyle.Regular, message.Title, titleBounds.Value.Size))
					{
						e.Graphics.DrawString(message.Title, titleFont, textBrush, titleBounds.Value, new StringFormat
						{
							LineAlignment = StringAlignment.Center,
							Alignment = StringAlignment.Near
						});
					}
				}

				e.Graphics.FillPie(countdownBrush, countdownBounds.ToRectangle(), -90, -360 +
																					   (360 * ((displayElapsedTimeMs +
																								displayIntervalMs) /
																							   displayDurationMs)));

				if (messageQueue.Skip(1).Any())
				{
					e.Graphics.DrawString($"+{(messageQueue.Count - 1)}", SkinningDefs.TheInstance.GetFont(12), Brushes.Black, countdownBounds, new StringFormat
					{
						LineAlignment = StringAlignment.Center,
						Alignment = StringAlignment.Center
					});
				}

				var textHeight = contentBounds.Height - Math.Max(titleBounds?.Bottom + padding ?? padding,
									 closeButton.Bottom + padding) - padding;

				var textY = Math.Max(titleBounds?.Bottom + padding ?? padding, closeButton.Bottom + padding);

				var textBounds = new RectangleF(0, textY, contentBounds.Width, textHeight)
					.AlignRectangle(contentBounds.Width * 0.85f, textHeight, StringAlignment.Center, StringAlignment.Near);

				using (var textFont = SkinningDefs.TheInstance.GetPixelSizedFont(20))
				{
					e.Graphics.DrawString(message.Text, textFont, textBrush, textBounds, new StringFormat
					{
						LineAlignment = StringAlignment.Center,
						Alignment = StringAlignment.Center
					});
				}
			}
		}

		void watchedNode_ChildAdded(Node sender, Node child)
		{
			if (!watchedNodesToAttributes.ContainsKey(sender))
			{
				return;
			}

			if (participantOnlyMessages && !child.GetBooleanAttribute("display_to_participants", false))
			{
				return;
			}

			var attributes = watchedNodesToAttributes[sender];
			messageQueue.Add(new Message
			{
				Title = !string.IsNullOrEmpty(attributes.TitleAttribute) ? child.GetAttribute(attributes.TitleAttribute) : null,
				Text = child.GetAttribute(attributes.TextAttribute)
			});

			var soundFile = child.GetAttribute("sound");
			if (! string.IsNullOrEmpty(soundFile))
			{
				soundPlayer.PlaySound($@"audio\{soundFile}", false, false);
			}

			if (!displayTimer.Enabled)
			{
				displayElapsedTimeMs = 0;
				displayTimer.Start();
			}

			BringToFront();
			Visible = true;

			Invalidate();

		}

		void displayTimer_Tick(object sender, EventArgs e)
		{
			displayElapsedTimeMs += displayIntervalMs;

			if (displayElapsedTimeMs >= displayDurationMs)
			{
				messageQueue.RemoveAt(0);

				if (!messageQueue.Any())
				{
					displayTimer.Stop();
					Visible = false;
					return;
				}

				displayElapsedTimeMs = 0;
			}

			Invalidate();
		}

		void closeButton_Click(object sender, EventArgs e)
		{
			displayTimer.Stop();
			Hide();
			messageQueue.Clear();
		}


		readonly bool participantOnlyMessages;
		readonly Dictionary<Node, WatchedNodeAttributes> watchedNodesToAttributes;

		

		readonly List<Message> messageQueue;
		readonly PolyphonicSoundPlayer soundPlayer;
		readonly StyledImageButton closeButton;

		readonly Timer displayTimer;
		int displayElapsedTimeMs;
		readonly int displayIntervalMs;
		readonly float displayDurationMs;

		struct WatchedNodeAttributes
		{
			public string TitleAttribute { get; set; }
			public string TextAttribute { get; set; }
		}

		struct Message
		{
			public string Title { get; set; }
			public string Text { get; set; }
			public string Sound { get; set; }
			public bool SoundPlayed { get; set; }
		}
	}
}
