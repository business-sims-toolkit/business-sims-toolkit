using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using CommonGUI;
using CoreUtils;
using DevOps.OpsEngine;
using DevOps.OpsScreen.ServiceDevelopmentUi;
using Network;
using ResizingUi;
using ResizingUi.Button;

namespace DevOps.OpsScreen.FacilitatorControls.AppDevelopmentUi
{
    internal class StartAppDevelopmentPanel : FlickerFreePanel
    {
        int preferredHeight;
        public int PreferredHeight
        {
            get => preferredHeight;
            private set
            {
                if (preferredHeight != value)
                {
                    preferredHeight = value;
                    OnPreferredHeightChanged();
                }
                
            }
        }

        public event EventHandler PreferredHeightChanged;

        void OnPreferredHeightChanged ()
        {
            PreferredHeightChanged?.Invoke(this, EventArgs.Empty);
        }

        public StartAppDevelopmentPanel (RequestsManager requestsManager, NodeTree model)
        {
            this.requestsManager = requestsManager;

            beginServicesNode = model.GetNamedNode("BeginNewServicesInstall");

            beginServicesNode.ChildRemoved += beginServicesNode_ChildRemoved;

            okButton = new StyledDynamicButton("standard", "OK")
            {
                Size = new Size(80, 30),
                Font = SkinningDefs.TheInstance.GetFontWithStyle("standard_popup_control_button_font"),
                Enabled = false
            };
            Controls.Add(okButton);
            okButton.Click += okButton_Click;

            cancelButton = new StyledDynamicButton("standard", "Cancel")
            {
                Size = new Size(80, 30),
                Font = SkinningDefs.TheInstance.GetFontWithStyle("standard_popup_control_button_font"),
                Enabled = false
            };
            Controls.Add(cancelButton);
            cancelButton.Click += cancelButton_Click;

            appInfoLabel = new Label
            {
                Font = SkinningDefs.TheInstance.GetFont(10),
                Text = "",
                // TODO
                BackColor = Color.Transparent,
                ForeColor = Color.White
            };
            Controls.Add(appInfoLabel);

            SetOkCancelButtonEnabled();

            ShowServicesPanel();

            DoSize();
        }
        

        protected override void Dispose (bool disposing)
        {
            if (disposing)
            {
                beginServicesNode.ChildRemoved -= beginServicesNode_ChildRemoved;
            }

            base.Dispose(disposing);
        }

        protected override void OnSizeChanged (EventArgs e)
        {
            DoSize();
        }

        void DoSize ()
        {
            const int padding = 10;
            
            servicesPanel.Location = new Point(padding, padding);
            servicesPanel.Size = new Size(Width - 2 * padding, 80);

            servicesPanel.Height = servicesPanel.PreferredHeight;
            servicesPanel.Width = servicesPanel.PreferredWidth;

            if (productsPanel != null)
            {
                productsPanel.Location = new Point(padding, servicesPanel.Bottom + 5);
                productsPanel.Size = new Size(okButton.Right - padding, 45);

                productsPanel.Height = productsPanel.PreferredHeight;
                productsPanel.Width = productsPanel.PreferredWidth;
            }

            cancelButton.Location = new Point(Width - padding - cancelButton.Width, (productsPanel?.Bottom ?? servicesPanel.Bottom) + padding);
            okButton.Location = new Point(cancelButton.Left - padding - okButton.Width, cancelButton.Top);

            appInfoLabel.Bounds = new Rectangle(padding, okButton.Top, okButton.Left - 2 * padding, okButton.Height);
            appInfoLabel.TextAlign = ContentAlignment.MiddleLeft;
            appInfoLabel.Font = appInfoLabel.GetFontToFit(FontStyle.Bold, "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
                new SizeF(appInfoLabel.Width, appInfoLabel.Height));

            PreferredHeight = cancelButton.Bottom + padding;
        }

        void RemoveServicesPanel ()
        {
            if (servicesPanel != null)
            {
                servicesPanel.ButtonClicked -= servicesPanel_ButtonClicked;
                servicesPanel.Dispose();
                servicesPanel = null;
            }

            RemoveProductsPanel();
        }

        void ShowServicesPanel ()
        {
            RemoveServicesPanel();

            List<StyledDynamicButton> CreateServiceButtons ()
            {
                var services = requestsManager.GetServices();

                return services.Select(s =>
                        AppDevelopmentButtonFactory.CreateButton(s.Key, s.Key, false, s.Key == selectedServiceId, s.Value, 60, 30))
                    .ToList();
            }
            
                
            servicesPanel = new StartAppDevelopmentButtonsPanel(CreateServiceButtons, new Size(50, 30), null, 35)
            {
                BackColor = SkinningDefs.TheInstance.GetColorData("facilitator_development_stages_back_colour", Color.Aqua)
            };

            servicesPanel.ButtonClicked += servicesPanel_ButtonClicked;
            
            Controls.Add(servicesPanel);

            DoSize();
        }
        
        void RemoveProductsPanel ()
        {
            if (productsPanel != null)
            {
                productsPanel.ButtonClicked -= productsPanel_ButtonClicked;
                productsPanel.Dispose();
                productsPanel = null;
            }
        }

        void ShowProductsPanel ()
        {
            RemoveProductsPanel();

            Debug.Assert(!string.IsNullOrEmpty(selectedServiceId), "Product panel being displayed before a service ID has been selected");

            List<StyledDynamicButton> CreateProductButtons ()
            {
                var productIds = requestsManager.GetProductsForService(selectedServiceId);
                var productIdToIsOptimal = requestsManager.GetProductOptimalitiesForService(selectedServiceId);
                var productIdToPlatform = requestsManager.GetProductPlatformsForService(selectedServiceId);

                return productIds.Select(p => AppDevelopmentButtonFactory.CreateButton($"{p} {productIdToPlatform[p]}",
                    p, productIdToIsOptimal[p], p == selectedProductId, true, 60, 30,
                    (!requestsManager.IsPlatformSupportedForProduct(p) ? Color.Red  : (Color?) null))).ToList();
            }

            productsPanel = new StartAppDevelopmentButtonsPanel(CreateProductButtons, new Size(50,30), 85, 35)
            {
                BackColor = SkinningDefs.TheInstance.GetColorData("facilitator_development_stages_back_colour", Color.Aqua)
            };

            productsPanel.ButtonClicked += productsPanel_ButtonClicked;
            
            Controls.Add(productsPanel);

            DoSize();
        }

        void servicesPanel_ButtonClicked(object sender, EventArgs e)
        {
            selectedServiceId = servicesPanel.SelectedId;
            selectedProductId = "";

            SetOkCancelButtonEnabled();

            ShowProductsPanel();

            UpdateInfoLabel();
        }

        void productsPanel_ButtonClicked (object sender, EventArgs e)
        {
            selectedProductId = productsPanel.SelectedId;

            SetOkCancelButtonEnabled();

            UpdateInfoLabel();
        }
        
        void okButton_Click (object sender, EventArgs e)
        {
            if (! string.IsNullOrEmpty(selectedServiceId) && ! string.IsNullOrEmpty(selectedProductId))
            {
                if (! requestsManager.HasNewServiceDevelopmentAlreadyStarted(selectedServiceId))
                {
                    requestsManager.StartServiceDevelopment(selectedServiceId, selectedProductId);
                    ResetSelections();
                }
            }
            else
            {
                okButton.Enabled = false;
            }
        }

        void beginServicesNode_ChildRemoved(Node sender, Node child)
        {
            ResetSelections();
        }

        void cancelButton_Click (object sender, EventArgs e)
        {
            ResetSelections();
        }

        void ResetSelections ()
        {
            selectedServiceId = selectedProductId = "";
            ShowServicesPanel();
            UpdateInfoLabel();
        }

        void SetOkCancelButtonEnabled ()
        {
            cancelButton.Enabled =
                ! string.IsNullOrEmpty(selectedServiceId) || ! string.IsNullOrEmpty(selectedProductId);

            if (!string.IsNullOrEmpty(selectedServiceId) && !string.IsNullOrEmpty(selectedProductId))
            {
                okButton.Enabled = !requestsManager.HasNewServiceDevelopmentAlreadyStarted(selectedServiceId);
            }
        }

        void UpdateInfoLabel ()
        {
            if (string.IsNullOrEmpty(selectedServiceId))
            {
                appInfoLabel.Text = "";
                return;
            }

            var serviceName = requestsManager.GetBusinessServiceNameFromServiceId(selectedServiceId);

            var infoText = $"{serviceName} - {selectedServiceId} - {selectedProductId}";
            string platformSupportedText = "";

            if (! string.IsNullOrEmpty(selectedProductId))
            {
                platformSupportedText = ! requestsManager.IsPlatformSupportedForProduct(selectedProductId)
                    ? " - Platform Not Supported"
                    : "";
            }

            infoText += platformSupportedText;

            appInfoLabel.Text = infoText;
            
        }

        StartAppDevelopmentButtonsPanel servicesPanel;
        StartAppDevelopmentButtonsPanel productsPanel;

        readonly StyledDynamicButton okButton;
        readonly StyledDynamicButton cancelButton;
        
        readonly RequestsManager requestsManager;
        
        string selectedServiceId;
        string selectedProductId;

        readonly Node beginServicesNode;

        readonly Label appInfoLabel;

    }
}
