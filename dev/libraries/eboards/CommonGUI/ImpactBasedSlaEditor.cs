using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Network;
using LibCore;
using CoreUtils;

namespace CommonGUI
{
	public class ImpactBasedSlaEditor : Panel
	{
		NodeTree model;
	    readonly Node slas;

	    readonly IDataEntryControlHolder controlPanel;

	    readonly Label priorityLabelsHeading;
	    readonly Label priorityBoxesHeading;
	    readonly Label priorityRankingHeading;
	    readonly List<Label> priorityLabels;
	    readonly List<FilteredTextBox> priorityBoxes;
	    readonly List<Label> priorityRankings;
	    readonly ImageTextButton closeButton;

	    readonly FocusJumper focusJumper;

		public ImpactBasedSlaEditor (IDataEntryControlHolder controlPanel, int round, NodeTree model, bool trainingMode)
		{
			this.controlPanel = controlPanel;
			this.model = model;

			if (SkinningDefs.TheInstance.GetBoolData("popups_use_image_background", true))
			{
				var backgroundImage = AppInfo.TheInstance.Location + @"\images\panels\race_panel_back_normal.png";
				if (trainingMode)
				{
					backgroundImage = AppInfo.TheInstance.Location + @"\images\panels\race_panel_back_training.png";
				}
				BackgroundImage = Repository.TheInstance.GetImage(backgroundImage);
				BackgroundImageLayout = ImageLayout.Stretch;
			}

			focusJumper = new FocusJumper ();

			var titleColour = SkinningDefs.TheInstance.GetColorDataGivenDefault("sla_title_foreground", SkinningDefs.TheInstance.GetColorData("sla_foreground", Color.Black));

		    priorityLabelsHeading = new Label
		    {
		        BackColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("popup_title_background_colour",
		            SkinningDefs.TheInstance.GetColorData("sla_background", Color.White)),
		        ForeColor = titleColour,
		        Font = SkinningDefs.TheInstance.GetFont(12,
		            SkinningDefs.TheInstance.GetBoolData("ops_title_use_bold_font", true)
		                ? FontStyle.Bold
		                : FontStyle.Regular),
		        Text = SkinningDefs.TheInstance.GetData("sla_revenue_streams_title", "Number of Revenue Streams")
		    };

		    Controls.Add(priorityLabelsHeading);

		    priorityBoxesHeading = new Label
		    {
		        BackColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("popup_title_background_colour",
		            SkinningDefs.TheInstance.GetColorData("sla_background", Color.White)),
		        ForeColor = titleColour,
		        Font = SkinningDefs.TheInstance.GetFont(12,
		            SkinningDefs.TheInstance.GetBoolData("ops_title_use_bold_font", true)
		                ? FontStyle.Bold
		                : FontStyle.Regular),
		        Text = "MTRS (Minutes)"
		    };
		    Controls.Add(priorityBoxesHeading);

		    priorityRankingHeading = new Label
		    {
		        BackColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("popup_title_background_colour",
		            SkinningDefs.TheInstance.GetColorData("sla_background", Color.White)),
		        ForeColor = titleColour,
		        Font = SkinningDefs.TheInstance.GetFont(12,
		            SkinningDefs.TheInstance.GetBoolData("ops_title_use_bold_font", true)
		                ? FontStyle.Bold
		                : FontStyle.Regular),
		        Text = "Priority"
		    };
		    Controls.Add(priorityRankingHeading);

			slas = model.GetNamedNode("SLAs");
            priorityLabels = new List<Label>();
			priorityBoxes = new List<FilteredTextBox> ();
            priorityRankings = new List<Label>();

            var counter = 1;
			foreach (Node sla in slas.GetChildrenOfType("sla"))
			{
				var min = sla.GetIntAttribute("revenue_streams_min", 0);
				var max = sla.GetIntAttribute("revenue_streams_max", 0);

			    var streamLabel = new Label
			    {
			        BackColor = SkinningDefs.TheInstance.GetColorData("sla_background", Color.White),
			        ForeColor = SkinningDefs.TheInstance.GetColorData("sla_foreground", Color.Black),
			        Font = SkinningDefs.TheInstance.GetFont(10),
			        Text = FormatRange(min, max),
                    TextAlign = ContentAlignment.MiddleCenter
			    };
			    priorityLabels.Add(streamLabel);
                Controls.Add(streamLabel);

			    var box = new FilteredTextBox(TextBoxFilterType.Custom)
			    {
			        ShortcutsEnabled = false,
			        MaxLength = 1,
			        Font = SkinningDefs.TheInstance.GetFont(10),
			        Text = CONVERT.ToStr(sla.GetIntAttribute("slalimit", 0) / 60),
			        Tag = sla
			    };
			    box.ValidateInput += box_ValidateInput;
                box.TextChanged += box_TextChanged;
				priorityBoxes.Add(box);
				Controls.Add(box);
			    focusJumper.Add(box);

			    var priorityLabel = new Label
			    {
			        BackColor = SkinningDefs.TheInstance.GetColorData("sla_background", Color.White),
			        ForeColor = SkinningDefs.TheInstance.GetColorData("sla_foreground", Color.Black),
			        Font = SkinningDefs.TheInstance.GetFont(10),
			        Text = CONVERT.ToStr(counter),
			        TextAlign = ContentAlignment.MiddleCenter
                };
			    priorityRankings.Add(priorityLabel);
                Controls.Add(priorityLabel);

                counter++;
			}

			closeButton = new StyledDynamicButtonCommon("standard", "Close") { Font = SkinningDefs.TheInstance.GetFont(10, FontStyle.Bold) };
			closeButton.ButtonPressed += closeButton_ButtonPressed;
			Controls.Add(closeButton);
			focusJumper.Add(closeButton);

			DoSize();
		}

	    protected override void Dispose (bool disposing)
	    {
	        if (disposing)
	        {
                focusJumper.Dispose();
	        }

	        base.Dispose(disposing);
	    }

	    protected override void OnGotFocus (EventArgs e)
	    {
	        base.OnGotFocus(e);

	        closeButton.Select();
	        closeButton.Focus();
	    }

	    protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);

			DoSize();
		}

		void DoSize ()
		{
		    var useCustomHeaders = SkinningDefs.TheInstance.GetBoolData("use_custom_mtrs_popup_headers", false);

            var rowHeight = 25;

		    var headerHeight = useCustomHeaders ? SkinningDefs.TheInstance.GetIntData("ops_popup_title_height") : rowHeight;

		    var preferredStreamWidth = priorityLabelsHeading.GetPreferredSize(Size.Empty).Width;
		    var preferredBoxWidth = priorityBoxesHeading.GetPreferredSize(Size.Empty).Width;
		    var preferredRankingWidth = priorityRankingHeading.GetPreferredSize(Size.Empty).Width;

		    var streamHeadingSize = new Size(preferredStreamWidth, headerHeight);
            var boxHeadingSize = new Size(preferredBoxWidth, headerHeight);
		    var rankingHeadingSize = new Size(preferredRankingWidth, headerHeight);

            var streamLabelSize = new Size (preferredStreamWidth, rowHeight);
			var boxSize = new Size (preferredBoxWidth, rowHeight);
			var rankingLabelSize = new Size (preferredRankingWidth, rowHeight);

			var leading = (Height - ((1 + priorityLabels.Count) * rowHeight)) / (priorityLabels.Count + 2);
			var columnGap = 20;
			var lineSpacing = rowHeight + leading;

			var columnTweak = SkinningDefs.TheInstance.GetIntData("sla_column_tweak", -100);
			var textBoxVerticalTweak = 2;

			var leftColumn = ((Width - (streamLabelSize.Width + columnGap + boxSize.Width)) / 2) + columnTweak;
            var midColumn = leftColumn + streamLabelSize.Width + columnGap;
            var rightColumn = midColumn + boxSize.Width + columnGap;
            
		    var streamTitleLeft = useCustomHeaders ? 0 : leftColumn;
		    var boxHeadingLeft = useCustomHeaders ? midColumn - columnGap / 2 : midColumn;
		    var rankingHeadingLeft = useCustomHeaders ? rightColumn - columnGap / 2 : rightColumn;

		    if (useCustomHeaders)
		    {
		        streamHeadingSize.Width += leftColumn + columnGap / 2;
		        rankingHeadingSize.Width = Width - rightColumn + columnGap / 2;
		        boxHeadingSize.Width += columnGap;

		        midColumn = (boxHeadingSize.Width - boxSize.Width) / 2 + boxHeadingLeft;
		        leftColumn = (streamHeadingSize.Width - streamLabelSize.Width) / 2;
		        rightColumn = (rankingHeadingSize.Width - rankingLabelSize.Width) / 2 + rankingHeadingLeft;
		    }

			priorityLabelsHeading.Location = new Point (streamTitleLeft, 0);
			priorityLabelsHeading.Size = streamHeadingSize;
			priorityLabelsHeading.TextAlign = ContentAlignment.MiddleCenter;
			priorityBoxesHeading.Location = new Point (boxHeadingLeft, 0);
			priorityBoxesHeading.Size = boxHeadingSize;
			priorityBoxesHeading.TextAlign = ContentAlignment.MiddleCenter;
            priorityRankingHeading.Location = new Point(rankingHeadingLeft, 0);
			priorityRankingHeading.Size = rankingHeadingSize;
            priorityRankingHeading.TextAlign = ContentAlignment.MiddleCenter;

			var y = priorityLabelsHeading.Top + lineSpacing + 10;

            for (var i = 0; i < priorityLabels.Count; i++)
            {
                priorityLabels[i].Location = new Point(leftColumn, y);
                priorityLabels[i].Size = streamLabelSize;
                priorityLabels[i].TextAlign = ContentAlignment.MiddleCenter;
                priorityBoxes[i].Location = new Point(midColumn, y + textBoxVerticalTweak);
                priorityBoxes[i].Size = boxSize;
                priorityBoxes[i].TextAlign = HorizontalAlignment.Center;
                priorityRankings[i].Location = new Point(rightColumn, y);
                priorityRankings[i].Size = rankingLabelSize;
                priorityRankings[i].TextAlign = ContentAlignment.MiddleCenter;
               
                y += lineSpacing;
            }

			closeButton.Size = new Size (80, 20);
			closeButton.Location = SkinningDefs.TheInstance.GetPointData("ops_popup_cancel_button_position",
			                                                                       Width - 10 - closeButton.Width,
																			       Height - 10 - closeButton.Height);
			closeButton.BringToFront();
		}

		string FormatRange (int min, int max)
		{
			if (max == min)
			{
				return CONVERT.ToStr(min);
			}
			else
			{
				return CONVERT.Format("{0} - {1}", min, max);
			}
		}

		void box_TextChanged (object sender, EventArgs e)
		{
			var box = (FilteredTextBox) sender;
			if (! string.IsNullOrEmpty(box.Text))
			{
				var time = 60 * CONVERT.ParseIntSafe(box.Text, 1);

				var sla = (Node) box.Tag;
				sla.SetAttribute("slalimit", time);
			}
		}

		void closeButton_ButtonPressed (object sender, ImageButtonEventArgs args)
		{
			controlPanel.DisposeEntryPanel();
		}

		bool box_ValidateInput (FilteredTextBox sender, KeyPressEventArgs e)
		{
			var digit = e.KeyChar - '0';

            var test = Char.IsControl(e.KeyChar);
			return (((digit >= 1) && (digit <= 9))
					|| Char.IsControl(e.KeyChar));
		}

		protected override void OnPaint (PaintEventArgs e)
		{
			base.OnPaint(e);

			using (var brush = new SolidBrush (SkinningDefs.TheInstance.GetColorDataGivenDefault("popup_title_background_colour", SkinningDefs.TheInstance.GetColorData("sla_background", Color.White))))
			{
				e.Graphics.FillRectangle(brush, new Rectangle (0, 0, Width, priorityBoxesHeading.Bottom));
			}
		}
	}
}