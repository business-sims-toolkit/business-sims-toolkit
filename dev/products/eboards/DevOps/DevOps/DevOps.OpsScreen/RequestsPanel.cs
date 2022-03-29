using System;
using System.Windows.Forms;
using System.Drawing;

using CommonGUI;
using CoreUtils;
using DevOps.OpsEngine;
using Network;
using ResizingUi.Button;

namespace DevOps.OpsScreen
{
	internal class RequestsPanel : FlickerFreePanel
    {
        public static Label CreateSectionTitleLabel(string text)
        {
            var title = new Label
                          {
                              Text = text,
                              TextAlign = ContentAlignment.MiddleLeft,
                              Font = SkinningDefs.TheInstance.GetFont(SkinningDefs.TheInstance.GetFloatData("request_panel_sub_title_size", 15.0f), FontStyle.Regular),
                              ForeColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("request_panel_text_colour", Color.Black),
                              BackColor = Color.Transparent
                          };

            return title;
        }

        public static StyledDynamicButton CreateSectionButton(string productId, string label, bool isOptimal, int width, int height)
        {
            var button = new StyledDynamicButton("standard", label)
            {
                Size = new Size(width, height),
                Tag = productId,
                Highlighted = isOptimal
            };

            return button;
        }

        readonly RequestsManager requestsManager;
        
        public int HeightPadding { get; } = 5;

        public int WidthPadding { get; } = 5;

        public int Round { get; }

        NewServicesPanel newService;
        NewServiceProductPanel productPanel;
        
        Label title;
        StyledDynamicButton okButton;
        StyledDynamicButton cancelButton;
        
        
        string newServicesSelected;
        
        string productSelected;

        readonly IDataEntryControlHolderWithShowPanel parentControl;

	    NodeTree model;
        
        public RequestsPanel(RequestsManager rm, int roundNumber, NodeTree model, IDataEntryControlHolderWithShowPanel parent)
        {
            parentControl = parent;
	        this.model = model;

            requestsManager = rm;
            newServicesSelected = string.Empty;
            
            Round = roundNumber;

            BasicLayout();
        }

	    void BasicLayout()
        {
            DoubleBuffered = true;
            BackColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("pop_up_panel_background_colour", Color.White);

            title = new Label
                    {
                        Location = new Point(WidthPadding, 10),
                        Size = new Size(Width - WidthPadding, 25),
                        Text = "New Requests",
                        TextAlign = ContentAlignment.MiddleLeft,
                        Font = SkinningDefs.TheInstance.GetFont(SkinningDefs.TheInstance.GetFloatData("request_panel_main_title_size", 20.0f), FontStyle.Bold),
                        ForeColor = SkinningDefs.TheInstance.GetColorDataGivenDefault("request_panel_text_colour", Color.Black),
                        BackColor = Color.Transparent
                    };
            Controls.Add(title);

            cancelButton = new StyledDynamicButton("standard", "Cancel")
            {
                Size = new Size(100, 30),
                Font = SkinningDefs.TheInstance.GetFontWithStyle("standard_popup_control_button_font")
            };
            cancelButton.Click += CancelPanel;
            Controls.Add(cancelButton);

            okButton = new StyledDynamicButton("standard", "Ok")
            {
                Size = new Size(100, 30),
                Font = SkinningDefs.TheInstance.GetFontWithStyle("standard_popup_control_button_font")
            };
            okButton.Click += OkPanel;
            Controls.Add(okButton);
        }

        protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);

			DoSize();
		}

        protected void DoSize()
        {
            RemoveNewServicesPanel();
            CreateNewServicesPanel();

            HideNewServicesProductPanel();

            okButton.Enabled = false;

            cancelButton.Location = new Point(Width - (2 * WidthPadding) - cancelButton.Width, Height - cancelButton.Height - HeightPadding - 2);
            okButton.Location = new Point(cancelButton.Left - (2 * WidthPadding) - okButton.Width, Height - okButton.Height - HeightPadding - 2);
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);

            if (Visible)
            {
                ShowNewServicesPanel();
                HideNewServicesProductPanel();
            }
            else
            {
                RemoveNewServicesPanel();
                RemoveNewServicesProductPanel();
            }
        }

        void ShowNewServicesPanel()
        {
            RemoveNewServicesPanel();
            CreateNewServicesPanel();
        }

	    void CreateNewServicesPanel()
        {
            var serviceIds = requestsManager.GetServices();

            newService = new NewServicesPanel(serviceIds)
                         {
                             WrapContents = true,
                             FlowDirection = FlowDirection.LeftToRight,
                             Location = new Point(0, title.Bottom),
                             Size = new Size(Width, 220),
                             BackColor = Color.Transparent
                         };
            
            newService.NewServiceClicked += NewServiceClicked;
            Controls.Add(newService);
        }

	    void RemoveNewServicesPanel()
        {
            if (newService != null)
            {
                newService.NewServiceClicked -= NewServiceClicked;
                newService.Dispose();
                Controls.Remove(newService);
            }
        }

        void ShowNewServicesProductPanel()
        {
            RemoveNewServicesProductPanel();
            CreateNewServicesProductPanel();
        }

        void HideNewServicesProductPanel()
        {
            RemoveNewServicesProductPanel();
        }

        void CreateNewServicesProductPanel()
        {
            if (string.IsNullOrEmpty(newServicesSelected))
            {
                throw new Exception("Product panel being displayed when a new service hasn't been selected yet.");
            }

            productPanel = new NewServiceProductPanel(requestsManager, newServicesSelected)
                           {
                               Location = new Point(0, newService.Bottom),
                               FlowDirection = FlowDirection.LeftToRight,
                               WrapContents = true,
                               Size = new Size(Width, 65),
                               BackColor = Color.Transparent
                           };

            productPanel.ProductClicked += productPanel_ProductClicked;
            Controls.Add(productPanel);
        }

        void RemoveNewServicesProductPanel()
        {
            if (productPanel != null)
            {
                productPanel.ProductClicked -= productPanel_ProductClicked;
                productPanel.Dispose();
                Controls.Remove(productPanel);
            }
        }
        

        //All event handlers

        void productPanel_ProductClicked(object sender, EventArgs e)
        {
            var productButton = (StyledDynamicButton)sender;
            productSelected = (string) productButton.Tag;
            
            
            if (!string.IsNullOrEmpty(newServicesSelected) && !string.IsNullOrEmpty(productSelected))
            {
                okButton.Enabled = !requestsManager.HasNewServiceDevelopmentAlreadyStarted(newServicesSelected);
            }
            
        }
        

        public void NewServiceClicked(object sender, EventArgs eventArgs)
        {
            var newServiceButton = (StyledDynamicButton) sender;
            newServicesSelected = newServiceButton.Text;
            productSelected = "";

            okButton.Enabled = false;

            ShowNewServicesProductPanel();
        }
        

       
        public event EventHandler OkButtonClicked;

	    void OnOkButtonClicked(object sender)
	    {
	        OkButtonClicked?.Invoke(sender, EventArgs.Empty);
	    }

	    void OkPanel(object sender, EventArgs eventArgs)
        {
            if (!string.IsNullOrEmpty(newServicesSelected) && !string.IsNullOrEmpty(productSelected))
            {
                if (!requestsManager.HasNewServiceDevelopmentAlreadyStarted(newServicesSelected))
                {
                    requestsManager.StartServiceDevelopment(newServicesSelected, productSelected);
                    ResetAllPanelsToInitialState();
                }
            }
            else
            {
                okButton.Enabled = false;
            }

            parentControl.DisposeEntryPanel();

        }


        public event EventHandler CancelButtonClicked;

	    void OnCancelButtonClicked()
	    {
	        CancelButtonClicked?.Invoke(this, EventArgs.Empty);
	    }

	    void ResetAllPanelsToInitialState()
        {
            newServicesSelected = string.Empty;
            productSelected = string.Empty;
            
        }

	    void CancelPanel (object sender, EventArgs eventArgs)
        {
            ResetAllPanelsToInitialState();
            parentControl.DisposeEntryPanel();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                RemoveNewServicesPanel();
                
                title.Dispose();
                okButton.Dispose();
                cancelButton.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
