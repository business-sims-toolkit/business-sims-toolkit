using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Algorithms;
using CommonGUI;
using Network;
using LibCore;
using CoreUtils;
using ResizingUi.Button;

namespace DevOps.OpsScreen
{
	internal class DevOpsImpactBasedSlaEditor : Panel
    {
        readonly NodeTree model;
        Node slas;

        Label priorityLabelsHeading;
        Label priorityBoxesHeading;
        Label priorityRankingHeading;
        List<Label> priorityLabels;
        List<FilteredTextBox> priorityBoxes;
        List<Label> priorityRankings;
        StyledDynamicButton closeButton;

        Font headingsFont;
        Font valuesFont;
        
        int round;

        readonly IDataEntryControlHolderWithShowPanel parentControl;

        readonly bool includeCloseButton;
        readonly int paddingBetweenRows;
        public DevOpsImpactBasedSlaEditor(int round, NodeTree model, IDataEntryControlHolderWithShowPanel parent, bool includeCloseButton = true, int paddingBetweenRows = 20)
        {
            this.model = model;
            this.round = round;

            this.includeCloseButton = includeCloseButton;
            this.paddingBetweenRows = paddingBetweenRows;

            parentControl = parent;

            headingsFont = SkinningDefs.TheInstance.GetFont(10, FontStyle.Bold);
            valuesFont = SkinningDefs.TheInstance.GetFont(10);

            CreateLayout();

        }

        public void RevealPanel()
        {
            PopulateFilteredBoxes();
        }
        
        void CreateLayout()
        {
            var priorityLabelsHeadingText = SkinningDefs.TheInstance.GetData("sla_revenue_streams_title", "Number of Revenue Streams");
            priorityLabelsHeading = CreateLabel(priorityLabelsHeadingText, headingsFont);
            Controls.Add(priorityLabelsHeading);

            var priorityBoxesHeadingText = "MTRS (Minutes)";
            priorityBoxesHeading = CreateLabel(priorityBoxesHeadingText, headingsFont);
            Controls.Add(priorityBoxesHeading);

            var priorityRankingHeadingText = "Priority";
            priorityRankingHeading = CreateLabel(priorityRankingHeadingText, headingsFont);
            Controls.Add(priorityRankingHeading);

            slas = model.GetNamedNode("SLAs");
            priorityLabels = new List<Label>();
            priorityBoxes = new List<FilteredTextBox>();
            priorityRankings = new List<Label>();

            var counter = 1;
            foreach (Node sla in slas.GetChildrenOfType("sla"))
            {
                var min = sla.GetIntAttribute("revenue_streams_min", 0);
                var max = sla.GetIntAttribute("revenue_streams_max", 0);

                var streamText = FormatRange(min, max);

                var streamLabel = CreateLabel(streamText, valuesFont);
                priorityLabels.Add(streamLabel);
                Controls.Add(streamLabel);

                var box = CreateBox(sla);

                priorityBoxes.Add(box);
                Controls.Add(box);

                var counterStr = CONVERT.ToStr(counter++);
                var rankingLabel = CreateLabel(counterStr, valuesFont);
                priorityRankings.Add(rankingLabel);
                Controls.Add(rankingLabel);
            }

            PopulateFilteredBoxes();

            closeButton = new StyledDynamicButton("standard", "Close")
            {
                Size = new Size(100, 30),
                Font = SkinningDefs.TheInstance.GetFontWithStyle("standard_popup_control_button_font")
            };
            closeButton.Click += closeButton_Click;
            if (includeCloseButton)
            {
                Controls.Add(closeButton);
            }
            
        }

        void closeButton_Click(object sender, EventArgs e)
        {
            parentControl.DisposeEntryPanel();
        }

        FilteredTextBox CreateBox(Node sla)
        {
            var box = new FilteredTextBox(TextBoxFilterType.Custom)
            {
                ShortcutsEnabled = false,
                MaxLength = 1,
                Font = valuesFont,
                Tag = sla,
                TextAlign = HorizontalAlignment.Center
            };
            box.ValidateInput += box_ValidateInput;
            box.TextChanged += box_TextChanged;

            return box;
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);

            if (Visible)
            {
                RevealPanel();
            }
        }

        void RemoveLayout()
        {
            priorityBoxesHeading.Dispose();
            Controls.Remove(priorityBoxesHeading);

            priorityLabelsHeading.Dispose();
            Controls.Remove(priorityLabelsHeading);

            priorityRankingHeading.Dispose();
            Controls.Remove(priorityRankingHeading);

            foreach (var label in priorityLabels)
            {
                label.Dispose();
                Controls.Remove(label);
            }
            priorityLabels.Clear();

            foreach (var label in priorityRankings)
            {
                label.Dispose();
                Controls.Remove(label);
            }
            priorityRankings.Clear();

            foreach(var box in priorityBoxes)
            {
                box.TextChanged -= box_TextChanged;
                box.ValidateInput -= box_ValidateInput;
                box.Dispose();
                Controls.Remove(box);
            }
            priorityBoxes.Clear();

            closeButton.ButtonPressed -= ClosePanel;

            Controls.Remove(closeButton);
            closeButton.Dispose();

        }

        void PopulateFilteredBoxes()
        {
            foreach (var box in priorityBoxes)
            {
                var sla = (Node) box.Tag;
                box.Text = CONVERT.ToStr(sla.GetIntAttribute("slalimit", 0) / 60);
            }
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);

            DoSize();
        }

	    void DoSize()
        {
            const int rowHeight = 20;
            var streamLabelSize = new Size(210, rowHeight);
            var boxSize = new Size(160, rowHeight);
            var rankingLabelSize = new Size(110, rowHeight);

            const int columnGap = 10;
            var lineSpacing = rowHeight + paddingBetweenRows;

            const int widthPadding = 10;
            const int heightPadding = 10;

            // wP sLS.W cG bS.W cG rLS.W wP

            var totalWidth = streamLabelSize.Width + boxSize.Width + rankingLabelSize.Width + 2 * columnGap;
            var totalHeight = (priorityLabels.Count + 1) * rowHeight + priorityLabels.Count * paddingBetweenRows;

            var contentBounds = new Rectangle(widthPadding, heightPadding, Width - 2 * widthPadding, Height).AlignRectangle(totalWidth, totalHeight,
                StringAlignment.Center, StringAlignment.Near);


            var leftColumn = contentBounds.Left;
            var midColumn = leftColumn + streamLabelSize.Width + columnGap;
            var rightColumn = midColumn + boxSize.Width + columnGap;

            var y = contentBounds.Top;

            priorityLabelsHeading.Location = new Point(leftColumn, y);
            priorityLabelsHeading.Size = streamLabelSize;

            priorityBoxesHeading.Location = new Point(midColumn, y);
            priorityBoxesHeading.Size = boxSize;

            priorityRankingHeading.Location = new Point(rightColumn, y);
            priorityRankingHeading.Size = rankingLabelSize;

            y += lineSpacing;

            for (var i = 0; i < priorityLabels.Count; i++)
            {
                priorityLabels[i].Location = new Point(leftColumn, y);
                priorityLabels[i].Size = streamLabelSize;

                priorityBoxes[i].Location = new Point(midColumn, y - 1);
                priorityBoxes[i].Size = boxSize;

                priorityRankings[i].Location = new Point(rightColumn, y);
                priorityRankings[i].Size = rankingLabelSize;

                y += lineSpacing;
            }

            
            closeButton.Location = SkinningDefs.TheInstance.GetPointData("ops_popup_cancel_button_position",
                Width - widthPadding - closeButton.Width, Height - heightPadding - closeButton.Height);

            closeButton.BringToFront();
        }



        public event EventHandler CloseButtonClicked;

        void OnCloseButtonClicked(object sender)
        {
            if (CloseButtonClicked != null)
            {
                CloseButtonClicked(sender, EventArgs.Empty);
            }
        }

        void ClosePanel(object sender, EventArgs eventArgs)
        {
            OnCloseButtonClicked(this);
        }

        void box_TextChanged(object sender, EventArgs e)
        {
            var box = (FilteredTextBox)sender;
            if (!string.IsNullOrEmpty(box.Text))
            {
                var time = 60 * CONVERT.ParseIntSafe(box.Text, 1);

                var sla = (Node)box.Tag;
                sla.SetAttribute("slalimit", time);
            }
        }

        bool box_ValidateInput(FilteredTextBox sender, KeyPressEventArgs e)
        {
            var digit = e.KeyChar - '0';

            var test = Char.IsControl(e.KeyChar);
            return (((digit >= 1) && (digit <= 9))
                    || Char.IsControl(e.KeyChar));
        }

        Label CreateLabel(string text, Font font)
        {
            var label = new Label();
            label.BackColor = SkinningDefs.TheInstance.GetColorData("sla_background", Color.Transparent);
            label.ForeColor = SkinningDefs.TheInstance.GetColorData("sla_foreground", Color.Black);
            label.Font = font;
            label.Text = text;
            label.TextAlign = ContentAlignment.MiddleCenter;

            return label;
        }

        string FormatRange(int min, int max)
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

    }
}
