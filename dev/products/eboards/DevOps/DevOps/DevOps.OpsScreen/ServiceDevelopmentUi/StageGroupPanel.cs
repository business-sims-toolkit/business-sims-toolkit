using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

using CommonGUI;
using CoreUtils;
using DevOps.OpsEngine;
using Network;
using ResizingUi.Button;

namespace DevOps.OpsScreen.ServiceDevelopmentUi
{
    internal interface ILinkedStage
    {
        bool HasCompletedStage { get; }

        bool HasFailedStage { get; }

        bool IsStageIncomplete { get; }

        ILinkedStage PrecedingStage { get; }

        event EventHandler StageStatusChanged;
        
        ILinkedStage FinalStage { get; }
    }

    internal class StageGroupPanel : FlickerFreePanel, ILinkedStage
    {
        public StageGroupPanel (IEnumerable<StageGroupProperties> buttonGroupProperties, Func<Node, bool> canResetStageFunc, 
                                Node serviceNode, Node servicesCommandQueueNode, Size panelSize, string resetToStage, 
                                string stageStatusAttribute, ILinkedStage precedingStage, List<Node> monitorForChangesNodes = null)
        {
            PrecedingStage = precedingStage;

            if (PrecedingStage != null)
            {
                PrecedingStage.StageStatusChanged += precedingStage_StageStatusChanged;
                Visible = PrecedingStage.HasCompletedStage;
            }

            if (monitorForChangesNodes != null)
            {
                foreach (var monitorForChangesNode in monitorForChangesNodes)
                {
                    monitorForChangesNode.AttributesChanged += monitorForChangesNode_AttributesChanged;
                }
            }

            this.monitorForChangesNodes = monitorForChangesNodes;

            this.servicesCommandQueueNode = servicesCommandQueueNode;

            this.serviceNode = serviceNode;
            serviceNode.AttributesChanged += serviceNode_AttributesChanged;
            
            this.stageStatusAttribute = stageStatusAttribute;
            
            this.canResetStageFunc = canResetStageFunc;

            stageResetButton = new StyledDynamicButton("standard", "Reset")
            {
                Size = new Size(53, 30),
                Font = SkinningDefs.TheInstance.GetPixelSizedFont(12, FontStyle.Bold),
                Tag = resetToStage,
                Enabled = CanResetStage()
            };
            Controls.Add(stageResetButton);
            stageResetButton.Click += stageResetButton_Click;

            buttonGroups = new List<StageButtonGroupPanel>();
            foreach (var properties in buttonGroupProperties)
            {
                var buttonGroup = new StageButtonGroupPanel(properties, () => HasCompletedStage, serviceNode, servicesCommandQueueNode)
                {
                    Size = panelSize
                };

                Controls.Add(buttonGroup);
                buttonGroups.Add(buttonGroup);
            }

            PreferredHeight = buttonGroups.Sum(g => g.Height) + (buttonGroups.Count + 1) * innerPadding;
        }

        

        public int PreferredHeight { get; }

        public bool HasCompletedStage => serviceNode.GetAttribute(stageStatusAttribute) == ServiceStageStatus.Completed;
        public bool HasFailedStage => serviceNode.GetAttribute(stageStatusAttribute) == ServiceStageStatus.Failed;
        public bool IsStageIncomplete => serviceNode.GetAttribute(stageStatusAttribute) == ServiceStageStatus.Incomplete;
        public ILinkedStage PrecedingStage { get; }

        public event EventHandler StageStatusChanged;
        
        public ILinkedStage FinalStage
        {
            get => finalStage;
            set
            {
                if (finalStage != null)
                {
                    finalStage.StageStatusChanged -= finalStage_StageStatusChanged;
                }

                finalStage = value;

                if (finalStage != null)
                {
                    finalStage.StageStatusChanged += finalStage_StageStatusChanged;
                }
            }
        }
        ILinkedStage finalStage;
        
        void OnStageCompletionStateChanged()
        {
            StageStatusChanged?.Invoke(this, EventArgs.Empty);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var buttonGroup in buttonGroups)
                {
                    buttonGroup.Dispose();
                }

                if (PrecedingStage != null)
                {
                    PrecedingStage.StageStatusChanged -= precedingStage_StageStatusChanged;
                }

                if (FinalStage != null)
                {
                    FinalStage.StageStatusChanged -= finalStage_StageStatusChanged;
                }

                serviceNode.AttributesChanged -= serviceNode_AttributesChanged;

                if (monitorForChangesNodes != null)
                {
                    foreach (var monitorForChangesNode in monitorForChangesNodes)
                    {
                        monitorForChangesNode.AttributesChanged -= monitorForChangesNode_AttributesChanged;
                    }
                }
            }

            base.Dispose(disposing);
        }

        void precedingStage_StageStatusChanged(object sender, EventArgs e)
        {
            Visible = PrecedingStage.HasCompletedStage;

            if (Visible)
            {
                foreach (var buttonGroup in buttonGroups)
                {
                    buttonGroup.UpdateOptions();
                }
            }
        }

        void finalStage_StageStatusChanged(object sender, EventArgs e)
        {
            UpdateStage();
        }

        void serviceNode_AttributesChanged(Node sender, System.Collections.ArrayList attrs)
        {
            var attributes = attrs.Cast<AttributeValuePair>();

            if (attributes.Any(avp => avp.Attribute == stageStatusAttribute))
            {
                UpdateStage();
                OnStageCompletionStateChanged();
            }
        }
        void monitorForChangesNode_AttributesChanged(Node sender, System.Collections.ArrayList attrs)
        {
            UpdateStage();
        }

        bool CanResetStage ()
        {
            return (FinalStage?.IsStageIncomplete ?? true) && canResetStageFunc(serviceNode) && !IsStageIncomplete;
        }

        public void UpdateStage ()
        {
            foreach (var buttonGroup in buttonGroups)
            {
                buttonGroup.UpdateOptions();
            }

            stageResetButton.Enabled = CanResetStage();
        }

        protected override void OnSizeChanged (EventArgs e)
        {
            DoSize();
        }

        void DoSize ()
        {
            var y = innerPadding;

            foreach (var buttonGroup in buttonGroups)
            {
                buttonGroup.Location = new Point(innerPadding, y);

                y += buttonGroup.Height + innerPadding;
            }

            stageResetButton.Location = new Point(Width - innerPadding - stageResetButton.Width,
                buttonGroups.Max(g => g.Bottom) - stageResetButton.Height);
        }

        void stageResetButton_Click(object sender, EventArgs e)
        {
            ResetStage();
        }

        void ResetStage ()
        {
            // ReSharper disable once ObjectCreationAsStatement
            new Node(servicesCommandQueueNode, "reset_stage", "",
                new List<AttributeValuePair>
                {
                    new AttributeValuePair("type", "reset_stage"),
                    new AttributeValuePair("target_status", (string) stageResetButton.Tag),
                    new AttributeValuePair("service_name", serviceNode.GetAttribute("name"))
                });
        }

        readonly List<StageButtonGroupPanel> buttonGroups;
        readonly StyledDynamicButton stageResetButton;

        readonly string stageStatusAttribute;

        readonly Node servicesCommandQueueNode;
        readonly Node serviceNode;
        readonly List<Node> monitorForChangesNodes;

        readonly Func<Node, bool> canResetStageFunc;

        const int innerPadding = 5;


    }
}
