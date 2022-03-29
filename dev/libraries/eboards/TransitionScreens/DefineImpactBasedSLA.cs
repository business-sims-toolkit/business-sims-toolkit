using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

using CommonGUI;
using LibCore;
using Network;
using CoreUtils;

using StyledDynamicButton = ResizingUi.Button.StyledDynamicButton;

namespace TransitionScreens
{
    public class DefineImpactBasedSLA : BaseSLAPanel
    {

        NodeTree model;
        Node slas;
        protected IDataEntryControlHolder _tcp;

        protected ImageTextButton closeButton;

        Label priorityLabelsHeading;
        Label priorityBoxesHeading;
        Label priorityRankingHeading;
        List<Label> priorityLabels;
        List<FilteredTextBox> priorityBoxes;
        List<Label> priorityRankings;
        
        int round;

        public DefineImpactBasedSLA(IDataEntryControlHolder tcp, NodeTree tree, Color OperationsBackColor, int round)
        {

            _tcp = tcp;
            string fontname = SkinningDefs.TheInstance.GetData("fontname");
            Font MyDefaultSkinFontBold9 = ConstantSizeFont.NewFont(fontname, 9, FontStyle.Bold);
            this.round = round;

            Color upColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_text_colour", Color.Black);
            Color downColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_down_text_colour", Color.White);
            Color hoverColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_hover_text_colour", Color.Green);
            Color disabledColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("button_disabled_text_colour", Color.DarkGray);

            model = tree;

            priorityLabelsHeading = new Label();
            priorityLabelsHeading.Font = SkinningDefs.TheInstance.GetFont(12, FontStyle.Bold);
			priorityLabelsHeading.Text = SkinningDefs.TheInstance.GetData("sla_revenue_streams_title", "Number of Revenue Streams");
            priorityLabelsHeading.ForeColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("transition_text_colour", Color.Black);
            Controls.Add(priorityLabelsHeading);

            priorityBoxesHeading = new Label();
            priorityBoxesHeading.Font = SkinningDefs.TheInstance.GetFont(12, FontStyle.Bold);
            priorityBoxesHeading.Text = "MTRS (Minutes)";
            priorityBoxesHeading.ForeColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("transition_text_colour", Color.Black);
            Controls.Add(priorityBoxesHeading);

            priorityRankingHeading = new Label();
            priorityRankingHeading.Font = SkinningDefs.TheInstance.GetFont(12, FontStyle.Bold);
            priorityRankingHeading.Text = "Priority";
            priorityRankingHeading.ForeColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("transition_text_colour", Color.Black);
            Controls.Add(priorityRankingHeading);

            closeButton = new StyledDynamicButton ("standard", "Close");
            closeButton.Font = MyDefaultSkinFontBold9;
            closeButton.Click += cancelButton_Click;
            Controls.Add(closeButton);

            slas = model.GetNamedNode("SLAs");
            priorityLabels = new List<Label>();
            priorityBoxes = new List<FilteredTextBox>();
            priorityRankings = new List<Label>();

            int counter = 1;
            foreach (Node sla in slas.GetChildrenOfType("sla"))
            {
                int min = sla.GetIntAttribute("revenue_streams_min", 0);
                int max = sla.GetIntAttribute("revenue_streams_max", 0);
				
                Label StreamLabel = new Label();
                StreamLabel.Font = SkinningDefs.TheInstance.GetFont(10);
                StreamLabel.Text = FormatRange(min, max);
                StreamLabel.ForeColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("transition_text_colour", Color.Black);
                priorityLabels.Add(StreamLabel);
                Controls.Add(StreamLabel);

                FilteredTextBox box = new FilteredTextBox(TextBoxFilterType.Custom);
                box.MaxLength = 1;
                box.ValidateInput += box_ValidateInput;
                box.Font = SkinningDefs.TheInstance.GetFont(10);
                box.Text = CONVERT.ToStr(sla.GetIntAttribute("slalimit", 0) / 60);
                box.Tag = sla;
                box.TextChanged += box_TextChanged;
                priorityBoxes.Add(box);
                Controls.Add(box);

                Label PriorityLabel = new Label();
                PriorityLabel.Font = SkinningDefs.TheInstance.GetFont(10);
                PriorityLabel.Text = CONVERT.ToStr(counter);
                PriorityLabel.ForeColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("transition_text_colour", Color.Black);
                priorityRankings.Add(PriorityLabel);
                Controls.Add(PriorityLabel);

                counter++;
            }

            BackColor = OperationsBackColor;
            ForeColor = Color.Black;
            BorderStyle = BorderStyle.None;
        }

        protected void cancelButton_Click(object sender, EventArgs e)
        {
            _tcp.DisposeEntryPanel();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);

            DoSize();
        }

        void DoSize()
        {
            int rowHeight = 25;
            Size streamLabelSize = new Size (priorityLabelsHeading.GetPreferredSize(Size.Empty).Width, rowHeight);
            Size boxSize = new Size (priorityBoxesHeading.GetPreferredSize(Size.Empty).Width, rowHeight);
			Size rankingLabelSize = new Size (priorityRankingHeading.GetPreferredSize(Size.Empty).Width, rowHeight);

			int leading = 0;
	        int columnGap = 0;
            int lineSpacing = rowHeight + leading;

            int textBoxVerticalTweak = 2;

            int leftColumn = 00;
            int midColumn = leftColumn + streamLabelSize.Width + columnGap;
			int rightColumn = midColumn + boxSize.Width + columnGap;

            int y = leading;

            priorityLabelsHeading.Location = new Point(leftColumn, y);
            priorityLabelsHeading.Size = streamLabelSize;
            priorityLabelsHeading.TextAlign = ContentAlignment.MiddleCenter;
            priorityBoxesHeading.Location = new Point(midColumn, y);
            priorityBoxesHeading.Size = boxSize;
            priorityBoxesHeading.TextAlign = ContentAlignment.MiddleRight;
            priorityRankingHeading.Location = new Point(rightColumn, y);
	        priorityRankingHeading.Size = new Size(Width - priorityRankingHeading.Left, rankingLabelSize.Height);
            priorityRankingHeading.TextAlign = ContentAlignment.MiddleCenter;

	        priorityLabelsHeading.BackColor = SkinningDefs.TheInstance.GetColorData("popup_title_background_colour", Color.White);
	        priorityLabelsHeading.ForeColor = SkinningDefs.TheInstance.GetColorData("popup_title_foreground_colour", Color.Black);
	        priorityBoxesHeading.BackColor = SkinningDefs.TheInstance.GetColorData("popup_title_background_colour", Color.White);
	        priorityBoxesHeading.ForeColor = SkinningDefs.TheInstance.GetColorData("popup_title_foreground_colour", Color.Black);
	        priorityRankingHeading.BackColor = SkinningDefs.TheInstance.GetColorData("popup_title_background_colour", Color.White);
	        priorityRankingHeading.ForeColor = SkinningDefs.TheInstance.GetColorData("popup_title_foreground_colour", Color.Black);

			y += lineSpacing;

            for (int i = 0; i < priorityLabels.Count; i++)
            {
                priorityLabels[i].Location = new Point(leftColumn, y);
                priorityLabels[i].Size = streamLabelSize;
                priorityLabels[i].TextAlign = ContentAlignment.MiddleCenter;
                priorityBoxes[i].Location = new Point(midColumn, y + textBoxVerticalTweak);
                priorityBoxes[i].Size = boxSize;
                priorityBoxes[i].TextAlign = HorizontalAlignment.Center;
                priorityRankings[i].Location = new Point(rightColumn, y);
                priorityRankings[i].Size = new Size (Right - priorityRankings[i].Left, rankingLabelSize.Height);
                priorityRankings[i].TextAlign = ContentAlignment.MiddleCenter;

                y += lineSpacing;
            }

	        int instep = 20;
	        closeButton.Size = new Size (80, 20);
	        closeButton.Location = new Point (Width - instep - closeButton.Width, Height - instep - closeButton.Height);
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

        void box_TextChanged(object sender, EventArgs e)
        {
            FilteredTextBox box = (FilteredTextBox)sender;
            if (!string.IsNullOrEmpty(box.Text))
            {
                int time = 60 * CONVERT.ParseIntSafe(box.Text, 1);

                Node sla = (Node)box.Tag;
                sla.SetAttribute("slalimit", time);
            }
        }

        bool box_ValidateInput(FilteredTextBox sender, KeyPressEventArgs e)
        {
            int digit = e.KeyChar - '0';
            return (((digit >= 1) && (digit <= 9))
                    || Char.IsControl(e.KeyChar));
        }
    }
}