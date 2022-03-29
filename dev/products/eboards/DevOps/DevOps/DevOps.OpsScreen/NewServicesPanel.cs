using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using CommonGUI;
using DevOps.OpsScreen.ServiceDevelopmentUi;
using ResizingUi.Button;

namespace DevOps.OpsScreen
{
	internal class NewServicesPanel : FlowLayoutPanel
    {
        public int HeightPadding { get; } = 5;

        public int WidthPadding { get; } = 5;

        readonly Label titleLabel;
        readonly int buttonWidth = 90;
        readonly int buttonHeight = 30;

        StyledDynamicButton previouslyClicked;

        readonly List<KeyValuePair<string, bool>> serviceIds;
        readonly List<StyledDynamicButton> serviceButtons;

        public string SelectedServiceId { get; private set; }

        static int count = 0;
        readonly int id;

        public NewServicesPanel(List<KeyValuePair<string, bool>> services, bool includeTitle = true)
        {
            id = count++;

            serviceIds = services;
			serviceButtons = new List<StyledDynamicButton> ();

            if (includeTitle)
            {
                titleLabel = RequestsPanel.CreateSectionTitleLabel("Select New App");
                titleLabel.Location = new Point(WidthPadding, HeightPadding);
                Controls.Add(titleLabel);
            }
            
            BasicLayout();
        }
        
        public void BasicLayout()
        {
            BackColor = Color.Transparent;
            DoubleBuffered = true;
            Padding = new Padding(HeightPadding);
            Margin = new Padding(WidthPadding);
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);

            DoLayout();
        }

        void DoLayout()
        {
            if (titleLabel != null)
            {
                titleLabel.Size = new Size(Width - (2 * WidthPadding), 25);
            }

	        foreach (var button in serviceButtons)
	        {
		        Controls.Remove(button);
	        }
			serviceButtons.Clear();
            
            foreach (var serviceId in serviceIds)
            {
                var service = AppDevelopmentButtonFactory.CreateButton(serviceId.Key, serviceId.Key, false, buttonWidth, buttonHeight);

                service.Enabled = serviceId.Value;
                service.Active = serviceId.Key == SelectedServiceId;
                service.Click += newService_Click;

                Controls.Add(service);
	            serviceButtons.Add(service);
            }
        }
        
        public event EventHandler NewServiceClicked;
        
        void OnNewServiceClicked(object sender)
        {
            if (previouslyClicked != null)
            {
                previouslyClicked.Active = false;
            }

            var button = (StyledDynamicButton)sender;
            button.Active = true;
            previouslyClicked = button;

            SelectedServiceId = button.Text;

            NewServiceClicked?.Invoke(sender,EventArgs.Empty);
        }

        void newService_Click(object sender, EventArgs eventArgs)
        {
            OnNewServiceClicked(sender);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                //foreach (var button in serviceButtons)
                //{
                //    button.Click -= newService_Click;
                //    button.Dispose();
                //}

                //serviceButtons.Clear();

                //previouslyClicked = null;
            }

            base.Dispose(disposing);
        }
    }
}
