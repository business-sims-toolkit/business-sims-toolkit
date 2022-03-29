using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using Algorithms;
using CommonGUI;
using CoreUtils;
using Events;
using DevOps.ReportsScreen.Interfaces;
using GameManagement;
using LibCore;
using ResizingUi;

namespace DevOps.ReportsScreen
{

    internal delegate SharedMouseEventControl CreateContentHandler(ReportScreen reportScreen);

    internal class ReportScreenProperties
    {
        public CreateContentHandler ContentCreator { get; set; }
        public ComboBoxRow RoundComboBox { get; set; }
        public ComboBoxRow BusinessComboBox { get; set; }
        public Func<Size, Size> PreferredContentSizeFunc { get; set; }
        public int ReclaimFromReservedHeight { get; set; }
        public Func<bool> IsSelectedRoundValidFunc { get; set; }

    }

    internal class ReportScreen : SharedMouseEventControl
    {
        public ReportScreen (IRoundScoresUpdater<DevOpsRoundScores> roundScoresUpdater, 
                             NetworkProgressionGameFile gameFile, List<DevOpsRoundScores> roundScores,
                             ReportScreenProperties properties)
        {
            this.gameFile = gameFile;

            this.roundScoresUpdater = roundScoresUpdater;
            roundScoresUpdater.RoundScoresChanged += roundScoresUpdater_RoundScoresChanged;

            this.roundScores = roundScores;
            
            ChangeContent(properties);


            mousePollTimer = new Timer
            {
                Interval = 20
            };
            mousePollTimer.Tick += mousePollTimer_Tick;
            mousePollTimer.Start();
        }
		
        public ReportScreen LinkedReportScreen { get; set; }

        void mousePollTimer_Tick(object sender, EventArgs e)
        {
	        if (! Visible)
	        {
				StopCursorPolling();
	        }
			
	        var cursorPosition = Cursor.Position;
	        var screenBounds = RectangleToScreen(ClientRectangle);
			
            containsCursor = screenBounds.Contains(cursorPosition);

	        if (!containsCursor && !LinkedReportScreen.containsCursor)
				mouseCursorForm?.Hide();

			if (containsCursor)
            {
	            mouseCursorForm?.Hide();

	            string boundsId = null;
	            Rectangle? containingBounds = null;

	            foreach (var kvp in BoundIdsToRectangles)
		        {
		            if (kvp.Value.Contains(cursorPosition))
		            {
			            boundsId = kvp.Key;
			            containingBounds = kvp.Value;
			            break;
		            }
	            }

				if (!LinkedReportScreen.IsDisposed)
					LinkedReportScreen.ShowCursor(containingBounds, boundsId, cursorPosition);
            }
			
        }

	    bool containsCursor;
		
		void ShowCursor(Rectangle? containingBounds, string boundsId, Point cursorPosition)
	    {
		    if (containingBounds == null)
		    {
				mouseCursorForm?.Hide();
			    return;
		    }

		    cursorPosition = new Point(cursorPosition.X - containingBounds.Value.X, cursorPosition.Y - containingBounds.Value.Y);
			
			var xRatio = cursorPosition.X / (float)containingBounds.Value.Width;
		    var yRatio = cursorPosition.Y / (float)containingBounds.Value.Height;


			var boundsDictionary = BoundIdsToRectangles.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
		    var bounds = (!string.IsNullOrEmpty(boundsId) && boundsDictionary.ContainsKey(boundsId))
			    ? boundsDictionary[boundsId] // RectangleToScreen(boundsDictionary[boundsId])
			    : RectangleToScreen(ClientRectangle);


		    var cursorPoint = bounds.FindPointByRatio(xRatio, yRatio);


			
			if (mouseCursorForm == null)
			{
			    mouseCursorForm = new MouseCursorForm
			    {
				    AllowTransparency = true,
				    FormBorderStyle = FormBorderStyle.None,
				    BackColor = Color.LightGray,
					TransparencyKey = Color.LightGray,
				    Size = new Size(30, 45),
				    StartPosition = FormStartPosition.Manual,
				    ShowInTaskbar = false,
				    BackgroundImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + @"\images\mouse_cursor.png")
			    };
				
		    }

		    mouseCursorForm.Location = cursorPoint;
			mouseCursorForm.Size = new Size(30, 45);
			if (!mouseCursorForm.Visible)
				mouseCursorForm.Show();

		}
		

        public void ChangeContent (ReportScreenProperties properties)
        {
            createReportContentHandler = properties.ContentCreator;

            if (roundComboBox != null)
            {
                Controls.Remove(roundComboBox);
                roundComboBox.SelectedIndexChanged -= roundComboBox_SelectedIndexChanged;
                roundComboBox = null;
            }

            if (properties.RoundComboBox != null)
            {
                roundComboBox = properties.RoundComboBox;
                Controls.Add(roundComboBox);
                roundComboBox.SelectedIndexChanged += roundComboBox_SelectedIndexChanged;
            }

            if (businessComboBox != null)
            {
                Controls.Remove(businessComboBox);
                businessComboBox.SelectedIndexChanged -= businessComboBox_SelectedIndexChanged;
                businessComboBox = null;
            }

            if (properties.BusinessComboBox != null)
            {
                businessComboBox = properties.BusinessComboBox;
                Controls.Add(businessComboBox);
                businessComboBox.SelectedIndexChanged += businessComboBox_SelectedIndexChanged;
            }

            preferredContentSizeFunc = properties.PreferredContentSizeFunc;

            isSelectedRoundValidFunc = properties.IsSelectedRoundValidFunc ?? (() => SelectedRound <= gameFile.LastRoundPlayed);

            if (roundComboBox != null)
            {
                roundComboBox.SelectedIndex = Math.Max(gameFile.LastRoundPlayed - 1, 0);
            }
            else
            {
                UpdateReport();
            }
        }
        
        public int SelectedRound
        {
            get => roundComboBox?.SelectedIndex + 1 ?? gameFile.LastRoundPlayed;
            set
            {
                if (roundComboBox == null) { return; }

                if (SelectedRound == value) { return; }

                var index = value - 1;

                if (index < 0 || index >= roundComboBox.Items.Count)
                {
                    throw new Exception($"Requested round {value} is an invalid index for round combo box");
                }

                roundComboBox.SelectedIndex = index;
            }
        }

        public string SelectedBusiness
        {
            get => businessComboBox?.SelectedItem.Text;

            set
            {
                if (businessComboBox == null) { return; }

                if (SelectedBusiness == value) { return; }

                var item = businessComboBox.Items.FirstOrDefault(i => i.Text == value);

				if (item == null)
                {
                    throw new Exception($"Requested business {value} has an invalid index for business combo box");
                }

                businessComboBox.SelectedIndex = businessComboBox.Items.IndexOf(item);
			}
        }

        public int SelectedBusinessIndex
        {
            get => businessComboBox?.SelectedIndex ?? -1;
            set
            {
                if (businessComboBox == null) { return; }

                if (SelectedBusinessIndex == value) { return; }

                var index = value;

                if (index < 0 || index >= businessComboBox.Items.Count)
                {
                    throw new Exception($"Requested index {value} is an invalid index for business combo box");
                }

                businessComboBox.SelectedIndex = index;
            }
        }

        public Rectangle ContentBounds { get; private set; }

        public event EventHandler<ReadonlyEventArgs<int>> SelectedRoundChanged;
        public event EventHandler<ReadonlyEventArgs<string>> SelectedBusinessChanged;

	    //public override Dictionary<string, Rectangle> BoundIdsToRectangles =>
		   // content?.BoundIdsToRectangles ?? new Dictionary<string, Rectangle> { { "report_all", ClientRectangle } };

	    public override List<KeyValuePair<string, Rectangle>> BoundIdsToRectangles =>
		    content?.BoundIdsToRectangles ?? new List<KeyValuePair<string, Rectangle>>
		    {
			    new KeyValuePair<string, Rectangle>
				    ("report_all", ClientRectangle)
		    };

	    public override void ReceiveMouseEvent(SharedMouseEventArgs args)
        {
            content?.ReceiveMouseEvent(args);
            //ShowCursor(args);
        }
        
        void OnSelectedRoundChanged ()
        {
            SelectedRoundChanged?.Invoke(this, SelectedRoundChanged.CreateReadonlyArgs(SelectedRound));
        }

        void OnSelectedBusinessChanged ()
        {
            SelectedBusinessChanged?.Invoke(this, SelectedBusinessChanged.CreateReadonlyArgs(SelectedBusiness));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                roundScoresUpdater.RoundScoresChanged -= roundScoresUpdater_RoundScoresChanged;

                mouseCursorForm?.Dispose();
                mousePollTimer.Stop();
                mousePollTimer.Dispose();
	            
			}

            base.Dispose(disposing);
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            if (Visible)
            {
                UpdateReport();

	            StartCursorPolling();
            }
            else
            {
	            StopCursorPolling();
            }
        }

	    void StartCursorPolling ()
	    {
		    mousePollTimer.Start();
		    mouseCursorForm?.Show();
		}

	    void StopCursorPolling ()
	    {
		    mousePollTimer.Stop();
		    mouseCursorForm?.Hide();
		}

        protected override void OnSizeChanged (EventArgs e)
        {
            DoSize();
        }

		void DoSize ()
        {
            const int reservedHeight = 45;

            var roundComboBoxLocation = SkinningDefs.TheInstance.GetPointData("round_combo_box_position", new Point(0, 15));

            if (roundComboBox != null)
            {
                roundComboBox.Location = roundComboBoxLocation;
            }

            if (businessComboBox != null)
            {
                businessComboBox.Location = new Point(roundComboBox?.Right + 50 ?? roundComboBoxLocation.X,
                    roundComboBox?.Top ?? roundComboBoxLocation.Y);
            }

            if (content != null)
            {
                const int padding = 10;
                var contentSize = preferredContentSizeFunc?.Invoke(new Size(Width - 2 * padding, Height)) ?? new Size(Width - 2 * padding, Height - padding - reservedHeight);

                ContentBounds = new Rectangle(0, reservedHeight, Width, Height - reservedHeight).CentreSubRectangle(contentSize);
                
                content.Bounds = ContentBounds;
                content.Invalidate();
            }
            
            Invalidate();
        }

        void UpdateReport ()
        {
            //Hide();
            if (content != null)
            {
                content.Dispose();
                content = null;
            }

            if (!isSelectedRoundValidFunc.Invoke())
            {
                return;
            }

            if (createReportContentHandler == null)
            {
                return;
            }

            if (roundScores.Any(r => r?.WasUnableToGetData ?? true))
            {
                return;
            }

            // ReSharper disable once UnusedVariable
            using (var cursor = new WaitCursor(this))
            {
                content = createReportContentHandler(this);
                Controls.Add(content);

                content.MouseEventFired += content_MouseEventFired;
            }

            DoSize();
            //Show();
        }

        void roundComboBox_SelectedIndexChanged (object sender, EventArgs e)
        {
            UpdateReport();
            OnSelectedRoundChanged();
        }

        void businessComboBox_SelectedIndexChanged (object sender, EventArgs e)
        {
            UpdateReport();
            OnSelectedBusinessChanged();
        }
        
        void roundScoresUpdater_RoundScoresChanged(object sender, EventArgs<List<DevOpsRoundScores>> e)
        {
            roundScores = e.Parameter;

            UpdateReport();
        }

        void content_MouseEventFired(object sender, SharedMouseEventArgs e)
        {
            OnMouseEventFired(e);
        }

        CreateContentHandler createReportContentHandler;

        readonly IRoundScoresUpdater<DevOpsRoundScores> roundScoresUpdater;
        List<DevOpsRoundScores> roundScores;

        readonly NetworkProgressionGameFile gameFile;

        ComboBoxRow roundComboBox;
        ComboBoxRow businessComboBox;

        Func<bool> isSelectedRoundValidFunc;
        Func<Size, Size> preferredContentSizeFunc;
        SharedMouseEventControl content;

        readonly Timer mousePollTimer;

        Form mouseCursorForm;
    }
}
