using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;

using CommonGUI;
using CoreUtils;
using DiscreteSimGUI;
using LibCore;
using Media;
using Network;
using ResizingUi.Button;

namespace DevOps.OpsScreen
{
    public class OpsControlPanelBar : Panel
    {
        public IncidentEntryBox IncidentEntryBox { get; private set; }

        StyledDynamicButton requests;
        StyledDynamicButton slaButton;
        StyledDynamicButton activeIncidentsButton;

        readonly NodeTree model;

        readonly SoundPlayer soundPlayer;

        readonly int widthPadding = 5;
        readonly int heightPadding = 5;
        readonly int buttonHeights = 30;

        public void EnableOrDisableRequestsButton(int numLocations)
        {
            requests.Enabled = numLocations > 0;
        }
        
  
        public OpsControlPanelBar (NodeTree network)
        {
            model = network;

			soundPlayer = new SoundPlayer ();

            Setup();
        }

        void Setup ()
        {
            BackColor = Color.Transparent;

            IncidentEntryBox = new IncidentEntryBox(30, 30, 45, widthPadding)
                               {
                                   BackColor = Color.Transparent,
                                   Size = new Size(115, buttonHeights)
                               };
            Controls.Add(IncidentEntryBox);
            IncidentEntryBox.IncidentEntryQueue = model.GetNamedNode("enteredIncidents") ??
                                                  new Node(model.Root, "enteredIncidents", "enteredIncidents",
                                                      (ArrayList) null);

            requests = new StyledDynamicButton("standard", "New App")
            {
                Size = new Size(90, buttonHeights),
                Font = SkinningDefs.TheInstance.GetFont(12, FontStyle.Regular)
            };
            
            requests.Click += requests_Click;
            Controls.Add(requests);
            requests.BringToFront();

            slaButton = new StyledDynamicButton("standard", "SLA")
            {
                Size = new Size(50, buttonHeights),
                Font = SkinningDefs.TheInstance.GetFont(12, FontStyle.Regular)
            };
            
            slaButton.Click += slaButton_Click;
            Controls.Add(slaButton);
            slaButton.BringToFront();

            activeIncidentsButton = new StyledDynamicButton("standard", "Incidents")
            {
                Size = new Size(90, buttonHeights),
                Font = SkinningDefs.TheInstance.GetFont(12, FontStyle.Regular)
            };
            
            activeIncidentsButton.Click += activeIncidentsButton_Click;
	        activeIncidentsButton.DoubleClick += activeIncidentsButton_DoubleClick;

			Controls.Add(activeIncidentsButton);
            activeIncidentsButton.BringToFront();
        }
        
        void requests_Click(object sender, EventArgs e)
        {
            OnRequestsButtonClicked(e);
        }

        void slaButton_Click(object sender, EventArgs e)
        {
            OnSlaEditorButtonClicked(e);
        }

        void activeIncidentsButton_Click(object sender, EventArgs e)
        {
            OnActiveIncidentsButtonClicked();
        }
	    void activeIncidentsButton_DoubleClick (object sender, EventArgs e)
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

		protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);

            DoLayout();
        }

        void DoLayout()
        {
            requests.Location = new Point (widthPadding, heightPadding);

            IncidentEntryBox.Location = new Point (512 + widthPadding, heightPadding);

            slaButton.Location = new Point (IncidentEntryBox.Right + widthPadding, heightPadding);
            activeIncidentsButton.Location = new Point (slaButton.Right + widthPadding, heightPadding);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
				soundPlayer.Dispose();
                requests.Dispose();
                IncidentEntryBox.Dispose();
            }
        }

        public event EventHandler RequestsButtonClicked;

        void OnRequestsButtonClicked(EventArgs e)
        {
            RequestsButtonClicked?.Invoke(this, e);
        }
        
        public event EventHandler SlaEditorButtonClicked;

        void OnSlaEditorButtonClicked(EventArgs e)
        {
            SlaEditorButtonClicked?.Invoke(this, e);
        }
        
        public event EventHandler ActiveIncidentsButtonClicked;

        void OnActiveIncidentsButtonClicked()
        {
            ActiveIncidentsButtonClicked?.Invoke(this, EventArgs.Empty);
        }

        public void DisableButtons ()
        {
            foreach (var button in new [] { requests, slaButton, activeIncidentsButton })
            {
                button.Enabled = false;
            }
        }
    }
}