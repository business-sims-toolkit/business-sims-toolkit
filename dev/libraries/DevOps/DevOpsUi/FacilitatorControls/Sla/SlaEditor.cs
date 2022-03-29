using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using Algorithms;
using CommonGUI;
using CoreUtils;
using Network;
using ResizingUi.Button;

namespace DevOpsUi.FacilitatorControls.Sla
{
	public class SlaEditor : FlickerFreePanel
	{
		public SlaEditor (NodeTree model, IReadOnlyCollection<SlaStream> slaStreams, bool includeCloseButton)
		{
			Visible = false;

			includeStreamColumn = slaStreams.Count > 1;

			if (includeStreamColumn)
			{
				streamTitleLabel = CreateTitleLabel(SkinningDefs.TheInstance.GetData("sla_revenue_streams_title", "Number of Revenue Streams"));
				Controls.Add(streamTitleLabel);
			}

			mtrsTitleLabel = CreateTitleLabel("MTRS (Minutes)");
			Controls.Add(mtrsTitleLabel);


			var slaNodes = model.GetNamedNode("SLAs").GetChildrenAsList();

			streamRows = (from stream in slaStreams
			              join sla in slaNodes on
				              stream.MaxRevenueStreams equals sla.GetIntAttribute("revenue_streams_max", 0)
			              select new SlaStreamRow(sla, stream, includeStreamColumn)).ToList();

			foreach (var row in streamRows)
			{
				Controls.Add(row);
			}

			if (includeCloseButton)
			{
				closeButton = new StyledDynamicButton("standard", "Close", true);
				Controls.Add(closeButton);
				closeButton.Click += closeButton_Click;
			}

			DoSize();
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			DoSize();
		}

		void DoSize()
		{
			const int minRowHeight = 20;

			var streamColumnFraction = includeStreamColumn ? 0.4f : 0f;
			var mtrsColumnFraction = includeStreamColumn ? 0.35f : 0.65f;

			const int columnGap = 10;

			var widthSansPadding = Width - (streamTitleLabel != null ? 4 : 3) * columnGap;

			if (streamTitleLabel != null)
			{
				streamTitleLabel.Bounds = new Rectangle(new Point(columnGap, 0), new Size((int)(widthSansPadding * streamColumnFraction), minRowHeight));
			}

			mtrsTitleLabel.Bounds = new Rectangle(streamTitleLabel?.Right + columnGap ?? columnGap, 0, (int)(widthSansPadding * mtrsColumnFraction), minRowHeight);

			if (closeButton != null)
			{
				const int minButtonHeight = 20;
				const int maxButtonHeight = 30;

				var buttonHeight = (int)Maths.Clamp(Height * 0.15f, minButtonHeight, maxButtonHeight);

				var buttonWidth = Math.Max(40, Width - mtrsTitleLabel.Right - 2 * columnGap);

				closeButton.Bounds = new Rectangle(0, 0, Width, Height).AlignRectangle(buttonWidth, buttonHeight, StringAlignment.Far, StringAlignment.Far, -columnGap, -columnGap);
			}

			var remainingHeight = (closeButton?.Top ?? Height) - minRowHeight - 2 * columnGap;

			var rowHeight = Math.Max(minRowHeight, remainingHeight / streamRows.Count);

			var y = minRowHeight + columnGap;
			var rowStepAmount = rowHeight; // TODO might include padding between rows
			foreach (var row in streamRows)
			{
				row.MtrsColumnFraction = mtrsColumnFraction;
				row.StreamColumnFraction = streamColumnFraction;
				row.ColumnGap = columnGap;

				row.Bounds = new Rectangle(0, y, Width, rowHeight);
				y += rowStepAmount;
			}
		}

		void closeButton_Click(object sender, EventArgs e)
		{
			Hide();
		}

		readonly List<SlaStreamRow> streamRows;

		readonly bool includeStreamColumn;

		readonly Label streamTitleLabel;
		readonly Label mtrsTitleLabel;

		readonly StyledDynamicButton closeButton;

		static Label CreateTitleLabel(string text)
		{
			return new Label
			{
				Text = text,
				BackColor = SkinningDefs.TheInstance.GetColorData("sla_background", Color.Transparent),
				ForeColor = SkinningDefs.TheInstance.GetColorData("sla_foreground", Color.Black),
				TextAlign = ContentAlignment.MiddleCenter
			};
		}
	}
}
