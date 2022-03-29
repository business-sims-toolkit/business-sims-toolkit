using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using CommonGUI;
using CoreUtils;
using DevOps.OpsEngine;
using DevOps.OpsScreen.ServiceDevelopmentUi;
using ResizingUi.Button;
using Point = System.Drawing.Point;

namespace DevOps.OpsScreen
{
	internal class NewServiceProductPanel : FlowLayoutPanel
    {
        public int HeightPadding { get; } = 5;

        public int WidthPadding { get; } = 5;

        readonly Label titleLabel;
        const int buttonHeight = 30;
        const int buttonWidth = 115;

        StyledDynamicButton previouslyClicked;

        readonly List<StyledDynamicButton> productButtons;
        readonly List<string> productIds;
        readonly Dictionary<string, bool> productIdToIsOptimal;
        readonly Dictionary<string, string> productIdToPlatform;

        readonly RequestsManager requestsManager;
        
        public string SelectedProductId { get; private set; }

        public NewServiceProductPanel (RequestsManager requestsManager, string service, bool includeTitle = true)
        {
            this.requestsManager = requestsManager;
            
            productIds = requestsManager.GetProductsForService(service);
            productIdToIsOptimal = requestsManager.GetProductOptimalitiesForService(service);
            productIdToPlatform = requestsManager.GetProductPlatformsForService(service);

            if (includeTitle)
            {
                titleLabel = RequestsPanel.CreateSectionTitleLabel("Select Product");
                titleLabel.Location = new Point(WidthPadding, HeightPadding);
                Controls.Add(titleLabel);
            }

            productButtons = new List<StyledDynamicButton>();

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
                titleLabel.Size = new Size(Width - WidthPadding,
                SkinningDefs.TheInstance.GetIntData("request_inner_panel_title_height", 25));
            }

            foreach (var button in productButtons)
            {
                button.Click -= productButton_Click;
                Controls.Remove(button);
            }

            productButtons.Clear();

            foreach (var productId in productIds)
            {
                var productButton = AppDevelopmentButtonFactory.CreateButton(productId + " " + productIdToPlatform[productId], productId, productIdToIsOptimal[productId], buttonWidth, buttonHeight);

                if (! requestsManager.IsPlatformSupportedForProduct(productId))
                {
                    productButton.ForeColor = Color.Red;
                }

                productButton.Click += productButton_Click;
                productButton.Active = productId == SelectedProductId;
                Controls.Add(productButton);
                productButtons.Add(productButton);
            }
        }


		public event EventHandler ProductClicked;

        void productButton_Click(object sender, EventArgs e)
        {
            if (previouslyClicked != null)
            {
                previouslyClicked.Active = false;
            }

            var button = (StyledDynamicButton)sender;

            button.Active = true;
            previouslyClicked = button;

            SelectedProductId = (string)button.Tag;

            ProductClicked?.Invoke(sender, e);
        }
    }
}
