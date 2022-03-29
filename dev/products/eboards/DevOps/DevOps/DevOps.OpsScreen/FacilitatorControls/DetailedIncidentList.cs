using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using Algorithms;
using CommonGUI;
using CoreUtils;
using DevOps.OpsEngine;
using IncidentManagement;
using LibCore;
using Media;
using ResizingUi;

namespace DevOps.OpsScreen.FacilitatorControls
{
	internal class DetailedIncidentList : CascadedBackgroundPanel
	{
		readonly Panel containingPanel;

	    readonly List<DetailedIncidentView> incidents;
		
		readonly FilteredTextBox hiddenTextBox;
		readonly Label searchEntryLabel;
		readonly SoundPlayer soundPlayer;

		class CiIncidents
		{
			public IncidentDefinition BreakIncident { get; set; }
			public IncidentDefinition WarningIncident { get; set; }
        }

		public DetailedIncidentList (TradingOpsEngine opsEngine)
		{
			hiddenTextBox = new FilteredTextBox(TextBoxFilterType.Digits)
			{
				MaxLength = 2,
				Location = new Point(0, -40)
			};
			Controls.Add(hiddenTextBox);
			hiddenTextBox.Select();
			
			hiddenTextBox.TextChanged += hiddenTextBox_TextChanged;
			hiddenTextBox.KeyDown += hiddenTextBox_KeyDown;

			searchEntryLabel = new Label
			{
				Size = new Size(70, 50),
				BackColor = CONVERT.ParseHtmlColor("#787878"),
				ForeColor = Color.White,
				TextAlign = ContentAlignment.MiddleLeft,
				Padding = new Padding(10),
				Visible = false
			};
			Controls.Add(searchEntryLabel);

			soundPlayer = new SoundPlayer();

			containingPanel = new Panel
		    {
		        AutoScroll = true
		    };
            Controls.Add(containingPanel);

			incidents = new List<DetailedIncidentView> ();

			var incidentList = new List<IncidentDefinition> (opsEngine.TheIncidentApplier.GetIncidents().Where(a => CONVERT.ParseIntSafe(a.ID) != null));
			incidentList.Sort((a, b) => CONVERT.ParseInt(a.ID).CompareTo(CONVERT.ParseInt(b.ID)));
			
			var idToIncidentGroup = new Dictionary<string, CiIncidents> ();
			while (incidentList.Count >  0)
			{
				var incident = incidentList[0];
				incidentList.RemoveAt(0);

				var nodeNamesBroken = incident.GetNamesOfNodesBrokenByAction();
				if (nodeNamesBroken.Count > 0)
				{
					var ciIncidents = new CiIncidents { BreakIncident = incident };
					idToIncidentGroup.Add(incident.ID, ciIncidents);

					var warningIncident = incidentList.FirstOrDefault(i => i.GetNamesOfNodesBrokenByAction().Count == 0 && i.GetBusinessServicesAffected(opsEngine.Model)
						.SequenceEqual(incident.GetBusinessServicesAffected(opsEngine.Model)));

					if (warningIncident != null)
					{
						ciIncidents.WarningIncident = warningIncident;
					}
				}
				else
				{
					if (idToIncidentGroup.Values.All(g => g.WarningIncident != incident))
					{
						var ciIncidents = new CiIncidents { WarningIncident = incident };
						idToIncidentGroup.Add(incident.ID, ciIncidents);
					}
				}
			}

		    columnToBounds = new Dictionary<IncidentColumns, RectangleF>();

            var rowColours = new []
		    {
		        CONVERT.ParseHtmlColor("#f5f5f5"),
		        CONVERT.ParseHtmlColor("#fafafa")
		    };
		    var index = 0;
			foreach (var id in idToIncidentGroup.Keys)
			{
				var ciIncidents = idToIncidentGroup[id];
				var view = new DetailedIncidentView (opsEngine.Model, ciIncidents.BreakIncident, ciIncidents.WarningIncident)
				{
                    RowColour = rowColours[index++ % rowColours.Length]
				};

                view.VisibleChanged += incidentView_VisibleChanged;

				incidents.Add(view);
				containingPanel.Controls.Add(view);
				view.Click += View_Click;
				
			}

			Enabled = false;
		}

		public void SetFocus ()
		{
			hiddenTextBox.Select();
		}

		
		void hiddenTextBox_TextChanged(object sender, EventArgs e)
		{
			var text = hiddenTextBox.Text;
			
			searchEntryLabel.Visible = ! string.IsNullOrEmpty(text);
			searchEntryLabel.Text = text;

			if (searchEntryLabel.Visible)
			{
				searchEntryLabel.BringToFront();
			}
			
			foreach (var row in incidents.Where(r => r.Visible))
			{
				if (string.IsNullOrEmpty(text))
				{
					row.IsPartiallySelected = false;
					row.IsMatchedExactly = false;
					row.ActivelyNotSelected = false;
					continue;
				}

				var isPartiallySelected = row.BreakIncidentId.StartsWith(text) || row.WarningIncidentId.StartsWith(text);
				var isMatchedExactly = row.BreakIncidentId == text || row.WarningIncidentId == text;

				if (isMatchedExactly)
				{ }
				
				row.IsPartiallySelected = isPartiallySelected;

				row.IsMatchedExactly = isMatchedExactly;

				row.ActivelyNotSelected = ! (isPartiallySelected || isMatchedExactly);
			}

			incidents.Sort((a, b) =>
			{
				if ((a.BreakIncidentId == "12" || b.BreakIncidentId == "12") &&
				    (string.IsNullOrEmpty(a.BreakIncidentId) || string.IsNullOrEmpty(b.BreakIncidentId)))
				{

				}
				
				if (a.MatchState != b.MatchState)
				{
					return a.MatchState.CompareTo(b.MatchState);
				}

				var aIncidentId = Math.Min(CONVERT.ParseIntSafe(a.BreakIncidentId, int.MaxValue), CONVERT.ParseIntSafe(a.WarningIncidentId, int.MaxValue));
				var bIncidentId = Math.Min(CONVERT.ParseIntSafe(b.BreakIncidentId, int.MaxValue), CONVERT.ParseIntSafe(b.WarningIncidentId, int.MaxValue));

				return aIncidentId.CompareTo(bIncidentId);
				
			});

			DoLayout();
		}

		protected override void OnParentChanged (EventArgs e)
		{
			if (Parent != null)
			{
				hiddenTextBox.Focus();
			}
		}

		protected override void OnClick (EventArgs e)
		{
			SetFocus();
		}
		
		void View_Click(object sender, EventArgs e)
		{
			SetFocus();
		}

		void hiddenTextBox_KeyDown (object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Enter)
			{
				const string tetrisEgg = "42";

				Debug.Assert(incidents.All(i => i.BreakIncidentId != tetrisEgg && i.WarningIncidentId != tetrisEgg));

				if (hiddenTextBox.Text == tetrisEgg)
				{
					if (soundPlayer.MediaState == MediaState.Playing)
					{
						soundPlayer.Stop();
					}
					else
					{
						soundPlayer.Play(AppInfo.TheInstance.Location + @"\audio\korobeiniki.mp3", false);
					}

					
				}
				else
				{
					var incidentRow = incidents.FirstOrDefault(i => i.IsMatchedExactly);

					if (incidentRow == null)
					{
						return;
					}

					incidentRow.ProcessIncidentId(hiddenTextBox.Text);
				}

				

				hiddenTextBox.Text = "";

				e.Handled = true;
				e.SuppressKeyPress = true;
			}

			if (e.KeyCode == Keys.Escape)
			{
				hiddenTextBox.Text = "";

				e.Handled = true;
				e.SuppressKeyPress = true;
			}
		}

		void incidentView_VisibleChanged(object sender, EventArgs e)
        {
            DoLayout();
        }

        protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);

			DoLayout();
		}

		void DoLayout ()
		{
		    var x = 0f;
		    var baseColumnWidth = Width / (float)IncidentColumnInfo.ColumnToWidthFactor.Values.Sum();
		    const int titleBarHeight = 30;

		    const int titleY = 5;

		    columnToBounds.Clear();

            foreach (var column in IncidentColumnInfo.ColumnOrder)
		    {
		        var columnWidth = IncidentColumnInfo.ColumnToWidthFactor[column] * baseColumnWidth;
		        var columnBounds = new RectangleF(x, titleY, columnWidth, titleBarHeight);
		        x += columnWidth;
		        columnToBounds[column] = columnBounds;
		    }

		    titleBarBounds = new Rectangle(0, titleY, Width, titleBarHeight);

            containingPanel.Bounds = new RectangleFromBounds
            {
                Left = 0,
                Top = titleBarBounds.Bottom,
                Right = Width,
                Bottom = Height
            }.ToRectangle();

			searchEntryLabel.Size = new Size(Width / 3, Height / 4);
			searchEntryLabel.Font = searchEntryLabel.GetFontToFit(FontStyle.Bold | FontStyle.Italic,
				searchEntryLabel.Text, new SizeF(searchEntryLabel.Size));

			searchEntryLabel.Bounds = containingPanel.Bounds.CentreSubRectangle(searchEntryLabel.Size);
			
			const int baseRowHeight = 30;
			var y = 0;

			foreach (var incident in incidents)
			{
				if (!incident.Visible)
				{
					continue;
				}

				var rowHeight = Math.Max(incident.PreferredHeight, baseRowHeight);
				var scrollBarWidth = containingPanel.VerticalScroll.Visible
					? SystemInformation.VerticalScrollBarWidth : 0;
				incident.Bounds = new Rectangle(0, y, containingPanel.Width - scrollBarWidth, rowHeight);
				y = incident.Bottom;
			}

			Invalidate();
		}

	    protected override void OnPaint (PaintEventArgs e)
	    {
            base.OnPaint(e);

            e.Graphics.FillRectangle(Brushes.Black, titleBarBounds);
            
	        var fontSize = this.GetFontSizeInPixelsToFit(FontStyle.Bold, IncidentColumnInfo.ColumnToTitle.Values.ToList(),
	            columnToBounds.Values.Select(b => b.Size).ToList());

	        using (var font = SkinningDefs.TheInstance.GetPixelSizedFont(fontSize, FontStyle.Bold))
	        {
                foreach (var column in IncidentColumnInfo.ColumnOrder)
	            {
                
                    e.Graphics.DrawString(IncidentColumnInfo.ColumnToTitle[column], font, Brushes.White, columnToBounds[column], new StringFormat
                    {
                        LineAlignment = StringAlignment.Center,
                        Alignment = StringAlignment.Center
                    });
	            }
	        }

	    }

	    readonly Dictionary<IncidentColumns, RectangleF> columnToBounds;
	    Rectangle titleBarBounds;
	}
}