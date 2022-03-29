using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;

using Algorithms;
using CommonGUI;
using CoreUtils;
using DevOps.OpsEngine;
using DevOps.OpsScreen.ServiceDevelopmentUi;
using Network;
using ResizingUi.Button;

namespace DevOps.OpsScreen.FacilitatorControls.AppDevelopmentUi
{
    internal class EnclosurePanel : FlickerFreePanel
    {

        public EnclosurePanel(Node service, Node servicesCommandQueueNode, RequestsManager requestsManager)
        {
            this.service = service;

            service.AttributesChanged += service_AttributesChanged;

            this.servicesCommandQueueNode = servicesCommandQueueNode;
            this.requestsManager = requestsManager;

            enclosureButtons = new List<StyledDynamicButton>();

            buttonSize = SkinningDefs.TheInstance.GetSizeData("enclosure_button_size", new Size(115, 30));

            UpdateEnclosures();

            gridPanel = new DynamicGridLayoutPanel(enclosureButtons, 80, 30)
            {
                ColumnCount = 4,
                MaximumColumns = 4,
                AreItemsFixedSize = false,
                HorizontalOuterMargin = 5,
                VerticalOuterMargin = 5,
                HorizontalInnerPadding = 5,
                VerticalInnerPadding = 5,
                IsSpacingFixedSize = true,
                MaximumItemWidth = 150,
                MaximumItemHeight = 50
            };
            Controls.Add(gridPanel);
        }

        public void UpdateEnclosures()
        {
            var correctOption = service.GetAttribute("server");
            var currentSelection = service.GetAttribute("enclosure_selection");

            var enclosureNames = requestsManager.GetEnclosureNames();
            enclosureNames.Sort();

            var enclosureNameToButtonProperties = enclosureNames.ToDictionary(e => e, e =>
                new StageButtonProperties
                {
                    IsAlreadySelected = e == currentSelection,
                    IsCorrectOption = e == correctOption,
                    IsEnabled = true,
                    Tag = e,
                    Text = e
                });

            if (enclosureButtons.Any())
            {
                Debug.Assert(enclosureNameToButtonProperties.Count == enclosureButtons.Count);

                foreach (var button in enclosureButtons)
                {
                    var properties = enclosureNameToButtonProperties[(string)button.Tag];

                    button.Active = properties.IsAlreadySelected;
                    button.Highlighted = properties.IsCorrectOption;
                }

            }
            else
            {
                foreach (var enclosure in enclosureNames)
                {
                    var properties = enclosureNameToButtonProperties[enclosure];

                    var button = new StyledDynamicButton("standard", properties.Text)
                    {
                        Size = buttonSize,
                        Font = SkinningDefs.TheInstance.GetFont(10),
                        Active = properties.IsAlreadySelected,
                        Tag = properties.Tag,
                        Enabled = properties.IsEnabled,
                        Highlighted = properties.IsCorrectOption
                    };

                    button.Click += enclosureButton_Click;
                    enclosureButtons.Add(button);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                service.AttributesChanged -= service_AttributesChanged;
            }

            base.Dispose(disposing);
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            DoSize();
        }

        void DoSize()
        {
            var bounds = new Rectangle(0, 0, Width, Height);

            gridPanel.Size = bounds.Size;

            gridPanel.Bounds = bounds.AlignRectangle(gridPanel.PreferredGridWidth, gridPanel.PreferredGridHeight, 
                StringAlignment.Center, StringAlignment.Center);
        }

        void enclosureButton_Click(object sender, EventArgs e)
        {
            foreach (var b in enclosureButtons)
            {
                b.Active = false;
            }

            var button = (StyledDynamicButton)sender;
            button.Active = true;

            var selectedEnclosure = (string)button.Tag;

            // ReSharper disable once ObjectCreationAsStatement
            new Node(servicesCommandQueueNode, AppDevelopmentCommandType.EnclosureSelection, "",
                new List<AttributeValuePair>
                {
                    new AttributeValuePair("type", AppDevelopmentCommandType.EnclosureSelection),
                    new AttributeValuePair("selection", selectedEnclosure),
                    new AttributeValuePair("service_name", service.GetAttribute("name"))
                });
        }

        void service_AttributesChanged(Node sender, System.Collections.ArrayList attrs)
        {
            UpdateEnclosures();
        }
        
        readonly Node servicesCommandQueueNode;
        readonly Node service;
        readonly RequestsManager requestsManager;

        readonly Size buttonSize;
        readonly List<StyledDynamicButton> enclosureButtons;

        readonly DynamicGridLayoutPanel gridPanel;
    }

}
