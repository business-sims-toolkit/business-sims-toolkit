using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using CommonGUI;
using CoreUtils;
using Network;
using ResizingUi.Button;

namespace DevOps.OpsScreen.ServiceDevelopmentUi
{
    internal struct ButtonTextTags
    {
        public string ButtonId { get; set; }
        public string ButtonText { get; set; }
        public string ButtonTag { get; set; }

        public bool IsEnabled { get; set; }
    }

    internal struct StageButtonProperties
    {
        public string Text { get; set; }
        public string Tag { get; set; }
        public bool IsCorrectOption { get; set; }
        public bool IsAlreadySelected { get; set; }
        public bool IsEnabled { get; set; }
    }

    internal class StageButtonGroupPanel : FlickerFreePanel
    { 
        public StageButtonGroupPanel (StageGroupProperties properties, Func<bool> hasPassedStage, Node serviceNode, Node servicesCommandQueueNode)
        {
            getCorrectOption = properties.GetCorrectOption;
            getCurrentSelection = properties.GetCurrentSelection;
            getOptions = properties.GetOptions;
            this.hasPassedStage = hasPassedStage;

            this.serviceNode = serviceNode;
            serviceNode.AttributesChanged += serviceNode_AttributesChanged;
            
            this.servicesCommandQueueNode = servicesCommandQueueNode;
            commandTypes = properties.CommandTypes;

            buttonSize = SkinningDefs.TheInstance.GetSizeData("stage_button_size", new Size(53, 30));
            buttonFlowDirection = properties.ButtonFlowDirection;
            
            titleLabel = new Label
            {
                Text = properties.Title,
                Location = new Point(widthPadding, heightPadding),
                TextAlign = properties.TitleAlignment,
                Font = SkinningDefs.TheInstance.GetFont(10f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };

            Controls.Add(titleLabel);

            buttonPanel = new FlowPanel
            {
                WrapContents = properties.WrapContents,
                FlowDirection = buttonFlowDirection,
                OuterMargin = 0,
                InnerPadding = heightPadding
            };

            buttons = new List<StyledDynamicButton>();

            Controls.Add(buttonPanel);
        }
        

        public void UpdateOptions()
        {
            var buttonOptions = getOptions().GroupBy(b => b.ButtonId).Select(g => g.First()).ToList();
            var correctOption = getCorrectOption(serviceNode);
            var currentSelection = getCurrentSelection(serviceNode);

            var buttonIdToProperties = buttonOptions.ToDictionary(b => b.ButtonId, b => new StageButtonProperties
            {
                IsAlreadySelected = b.ButtonTag == currentSelection,
                IsCorrectOption = b.ButtonTag == correctOption,
                Text = b.ButtonText,
                Tag = b.ButtonTag,
                IsEnabled = b.IsEnabled
            });
            

            Debug.Assert(buttonIdToProperties.Count(o => o.Value.IsAlreadySelected) <= 1, "More than one button is set to be active.");


            if (buttons.Any())
            {
                // Update the properties for the existing buttons

                Debug.Assert(buttons.Count == buttonIdToProperties.Count);

                foreach (var button in buttons)
                {
                    var id = (string) button.Tag;

                    var properties = buttonIdToProperties[id];

                    button.Active = properties.IsAlreadySelected;
                    button.Highlighted = properties.IsCorrectOption;
                    button.Enabled = properties.IsEnabled;
                    button.Text = properties.Text;
                }
            }
            else
            {
                // Otherwise make wid dee buttons
                foreach (var option in buttonOptions.Select(o => o.ButtonId))
                {
                    var isAlreadySelected = buttonIdToProperties[option].IsAlreadySelected;

                    var button = new StyledDynamicButton("standard", buttonIdToProperties[option].Text)
                    {
                        Size = buttonSize,
                        Font = SkinningDefs.TheInstance.GetFont(10),
                        Active = isAlreadySelected,
                        Tag = buttonIdToProperties[option].Tag,
                        Enabled = buttonIdToProperties[option].IsEnabled,
                        Highlighted = buttonIdToProperties[option].IsCorrectOption
                    };

                    button.Click += button_Click;

                    buttons.Add(button);
                    buttonPanel.Controls.Add(button);
                    button.BringToFront();

                    if (isAlreadySelected)
                    {
                        selectedOption = option;
                    }
                }
            }
            
            EnableButtons(!hasPassedStage());
        }
        
        public string SelectedOption
        {
            get => selectedOption;

            set
            {
                selectedOption = value;

                if (! string.IsNullOrEmpty(selectedOption))
                {
                    AddSelectionCommand();
                    OnOptionSelected();
                }
            }
        }

        public event EventHandler OptionSelected;

        public void EnableButtons (bool enable)
        {
            buttonPanel.Enabled = enable;
        }

        public IEnumerable<StyledDynamicButton> Buttons => buttons;

        protected override void Dispose (bool disposing)
        {
            if (disposing)
            {
                serviceNode.AttributesChanged -= serviceNode_AttributesChanged;
            }
            base.Dispose(disposing);
        }

        protected override void OnSizeChanged (EventArgs e)
        {
            base.OnSizeChanged(e);

            DoSize();
        }

        protected override void OnEnabledChanged (EventArgs e)
        {
            titleLabel.Enabled = true;
            foreach (var button in buttons)
            {
                button.Enabled = Enabled;
            }
        }

        void AddSelectionCommand ()
        {
            foreach (var command in commandTypes)
            {
                // ReSharper disable once ObjectCreationAsStatement
                new Node(servicesCommandQueueNode, command, "",
                    new List<AttributeValuePair>
                    {
                        new AttributeValuePair("type", command),
                        new AttributeValuePair("selection", SelectedOption),
                        new AttributeValuePair("service_name", serviceNode.GetAttribute("name"))
                    });
            }
        }

        void serviceNode_AttributesChanged(Node sender, System.Collections.ArrayList attrs)
        {
            UpdateOptions();
        }
        
        void DoSize()
        {
            titleLabel.Size = new Size(75, Height); 

            switch (buttonFlowDirection)
            {
                case FlowDirection.LeftToRight:
                    titleLabel.Location = new Point(0, 0);

                    buttonPanel.Location = new Point(titleLabel.Right, titleLabel.Top);
                    buttonPanel.Size = new Size(Width - titleLabel.Width, Height);
                    break;
                case FlowDirection.TopDown:
                    buttonPanel.Location = new Point(widthPadding, titleLabel.Bottom + heightPadding);
                    buttonPanel.Size = new Size(Width - (2 * widthPadding), Height - titleLabel.Height - heightPadding);
                    break;
            }

            UpdateOptions();
        }
        
        void OnOptionSelected()
        {
            OptionSelected?.Invoke(this, EventArgs.Empty);
        }

        void button_Click (object sender, EventArgs e)
        {
            var selectedButton = (StyledDynamicButton) sender;

            foreach (var button in buttons)
            {
                button.Active = false;
            }

            selectedButton.Active = true;

            SelectedOption = (string) selectedButton.Tag;
        }

        readonly Label titleLabel;
        readonly FlowPanel buttonPanel;
        readonly List<StyledDynamicButton> buttons;
        
        readonly Func<Node, string> getCorrectOption;
        readonly Func<Node, string> getCurrentSelection;
        readonly Func<List<ButtonTextTags>> getOptions;
        readonly Func<bool> hasPassedStage;
        
        readonly FlowDirection buttonFlowDirection;
        readonly Size buttonSize;

        const int widthPadding = 0;
        const int heightPadding = 5;

        string selectedOption;

        readonly List<string> commandTypes;
        readonly Node servicesCommandQueueNode;
        readonly Node serviceNode;
        
    }

    
}
