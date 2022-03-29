using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CoreUtils;

namespace CommonGUI
{
    public interface IOpsPopupSubPanel
    {
        string Title { get; }
        Control Control { get; }

        void HandleOkClicked ();

        event EventHandler CloseRequested;
    }

    public class OpsPopupPanel : FlickerFreePanel
    {
        readonly IOpsPopupSubPanel subPanel;
        readonly IDataEntryControlHolder mainPanel;

        readonly ImageTextButton okButton;
        readonly ImageTextButton cancelButton;

        readonly Label titleLabel;

        public OpsPopupPanel (IOpsPopupSubPanel subPanel, IDataEntryControlHolder mainPanel)
        {
            this.subPanel = subPanel;

            subPanel.CloseRequested += subPanel_CloseRequested;

            this.mainPanel = mainPanel;

            titleLabel = new Label
            {
                Text = subPanel.Title,
                Font = SkinningDefs.TheInstance.GetFont(24, FontStyle.Bold),
                BackColor = Color.Orange, //TODO
                ForeColor = Color.White,
                Location = new Point(0, 0)
            };
            Controls.Add(titleLabel);

            cancelButton = new ImageTextButton(0)
            {
                ButtonFont = SkinningDefs.TheInstance.GetFont(10, FontStyle.Bold),
                Size = new Size(80, 20),
                Visible = true
            };
            Controls.Add(cancelButton);
            cancelButton.Click += cancelButton_Click;

            okButton = new ImageTextButton(0)
            {
                ButtonFont = SkinningDefs.TheInstance.GetFont(10, FontStyle.Bold),
                Size = new Size(80, 20),
                Visible = false
            };
            Controls.Add(okButton);
            okButton.Click += okButton_Click;
        }

        protected override void OnSizeChanged (EventArgs e)
        {
            DoLayout();
        }

        void DoLayout ()
        {
            titleLabel.Size = new Size(Width, 50);

            const int padding = 10;

            cancelButton.Location = new Point(Width - padding - cancelButton.Width, Height - padding - cancelButton.Height);
            okButton.Location = new Point(cancelButton.Left - padding - okButton.Width, cancelButton.Top);
            

            subPanel.Control.Bounds = new Rectangle(0, titleLabel.Bottom + padding, Width, okButton.Top - titleLabel.Bottom - 2 * padding);

        }

        void ClosePopup ()
        {
            mainPanel.DisposeEntryPanel();
        }

        void subPanel_CloseRequested(object sender, EventArgs e)
        {
            // TODO anything else

            ClosePopup();
        }

        void cancelButton_Click(object sender, EventArgs e)
        {
            ClosePopup();
        }

        void okButton_Click(object sender, EventArgs e)
        {
            subPanel.HandleOkClicked();
        }
    }
    
}
