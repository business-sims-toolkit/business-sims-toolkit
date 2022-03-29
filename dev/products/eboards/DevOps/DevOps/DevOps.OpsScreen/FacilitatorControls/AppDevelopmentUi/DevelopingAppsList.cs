using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Algorithms;
using CommonGUI;
using CoreUtils;
using Events;
using DevOps.OpsEngine;
using LibCore;
using Network;
using ResizingUi;

namespace DevOps.OpsScreen.FacilitatorControls.AppDevelopmentUi
{
    internal class DevelopingAppsList : FlickerFreePanel
    {
        public DevelopingAppsList (NodeTree model, DevelopingAppTerminator appTerminator)
        {
            this.appTerminator = appTerminator;

	        scrollingPanel = new Panel
	        {
		        AutoScroll = true
	        };
			Controls.Add(scrollingPanel);


            appRows = new List<DevelopingAppView>();

            rowHeight = 30;
            rowPadding = 0;

            beginInstallNode = model.GetNamedNode("BeginNewServicesInstall");
            beginInstallNode.ChildAdded += beginInstallNode_ChildAdded;
            beginInstallNode.ChildRemoved += beginInstallNode_ChildRemoved;

            columnsToBounds = new Dictionary<AppDevelopmentColumn, RectangleF>();

            foreach (var service in beginInstallNode.GetChildrenWithAttributeValue("type", "BeginNewServicesInstall"))
            {
                AddRow(service);
            }
        }

        public event EventHandler<EventArgs<Node>> ServiceSelected;

        Node selectedServiceNode;

        public Node SelectedServiceNode
        {
            get => selectedServiceNode;
            private set
            {
                selectedServiceNode = value;

                if (selectedServiceNode != null)
                {
                    OnServiceSelected();
                }
            }
        }

        void OnServiceSelected()
        {
            ServiceSelected?.Invoke(this, ServiceSelected.CreateArgs(SelectedServiceNode));
        }


        protected override void Dispose (bool disposing)
        {
            if (disposing)
            {
                beginInstallNode.ChildAdded -= beginInstallNode_ChildAdded;
                beginInstallNode.ChildRemoved -= beginInstallNode_ChildRemoved;
            }

            base.Dispose(disposing);
        }

        protected override void OnPaint (PaintEventArgs e)
        {
            base.OnPaint(e);

            using (var titleBrush = new SolidBrush(CONVERT.ParseHtmlColor("#CDCDCD")))
            {
                e.Graphics.FillRectangle(titleBrush, titleBounds);
            }

            var fontSize = this.GetFontSizeInPixelsToFit(FontStyle.Bold,
                AppDevelopmentColumnInfo.ColumnToTitle.Values.ToList(),
                columnsToBounds.Values.Select(b => b.Size).ToList());
            using (var font = SkinningDefs.TheInstance.GetPixelSizedFont(fontSize, FontStyle.Bold))
            {
                foreach (var column in AppDevelopmentColumnInfo.ColumnOrder)
                {
                    e.Graphics.DrawString(AppDevelopmentColumnInfo.ColumnToTitle[column], font, Brushes.Black, columnsToBounds[column],
                        new StringFormat
                        {
                            LineAlignment = StringAlignment.Center,
                            Alignment = StringAlignment.Center
                        });
                }
            }
                
        }

        protected override void OnSizeChanged (EventArgs e)
        {
            DoSize();
        }

        void DoSize ()
        {
            const int titleHeight = 30;
            
            columnsToBounds.Clear();
	        titleBounds = new Rectangle(0, 0, Width, titleHeight);

	        scrollingPanel.Bounds = new RectangleFromBounds
	        {
		        Left = 0,
		        Right = Width,
		        Top = titleBounds.Bottom,
		        Bottom = Height
	        }.ToRectangle();

			var scrollBarWidth = scrollingPanel.VerticalScroll.Visible ? SystemInformation.VerticalScrollBarWidth : 0;

	        var availableWidth = scrollingPanel.Width - scrollBarWidth;

            var columnWidth = availableWidth / (float)AppDevelopmentColumnInfo.ColumnOrder.Count;
            var x = 0f;
            foreach (var column in AppDevelopmentColumnInfo.ColumnOrder)
            {
                columnsToBounds.Add(column, new RectangleF(x, 0, columnWidth, titleHeight));
                x += columnWidth;
            }

            if (! appRows.Any())
            {
                return;
            }
            
            var rowSize = new Size(availableWidth, rowHeight);

            var stride = rowHeight + rowPadding;

            var rowColours = new []
            {
                SkinningDefs.TheInstance.GetColorData("facilitator_developing_apps_row_colour"),
                SkinningDefs.TheInstance.GetColorData("facilitator_developing_apps_alt_row_colour")
            };

            var y = titleHeight;
            
            for (var i = 0; i < appRows.Count; i++)
            {
                var row = appRows[i];
                row.Size = rowSize;
                row.AnimateTo(new PointF(0, y), rowColours[i % rowColours.Length]);

                y += stride;
            }

            Invalidate();
        }

        void AddRow (Node serviceNode)
        {
            var bottom = appRows.Max(r => r.Bottom as int?) ?? 0;
            var row = new DevelopingAppView(serviceNode, appTerminator)
            {
                Size = new Size(Width, rowHeight),
                Location = new Point(0, bottom)
            };
            scrollingPanel.Controls.Add(row);
            appRows.Add(row);
            row.BringToFront();
            row.Click += row_Click;
            row.FontSizeToFitChanged += row_FontSizeToFitChanged;

            DoSize();
        }

        void UpdateFontSizes ()
        {
            if (! appRows.Any())
            {
                return;
            }

            var size = appRows.Min(r => r.FontSizeToFit);
            foreach (var row in appRows)
            {
                row.FontSize = size;
            }
        }

        void row_Click(object sender, EventArgs e)
        {
            // Would rather there was a way to stop propagation from the sender itself
            if (((MouseEventArgs)e).Button != MouseButtons.Left)
            {
                return;
            }
            SelectedServiceNode = ((DevelopingAppView) sender).ServiceNode;
        }

        void row_FontSizeToFitChanged (object sender, EventArgs e)
        {
            UpdateFontSizes();
        }
        
        void RemoveRow (Node serviceNode)
        {
            var row = appRows.First(r => r.ServiceNode == serviceNode);

            appRows.Remove(row);
            Controls.Remove(row);
            row.Dispose();


            DoSize();
        }
        
        void beginInstallNode_ChildAdded(Node sender, Node child)
        {
            AddRow(child);
        }

        void beginInstallNode_ChildRemoved(Node sender, Node child)
        {
            RemoveRow(child);
        }

        readonly Node beginInstallNode;
        readonly Dictionary<AppDevelopmentColumn, RectangleF> columnsToBounds;
        Rectangle titleBounds;
        readonly List<DevelopingAppView> appRows;
        readonly int rowHeight;
        readonly int rowPadding;

	    readonly Panel scrollingPanel;

        readonly DevelopingAppTerminator appTerminator;

    }
}
