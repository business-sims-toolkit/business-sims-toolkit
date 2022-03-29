using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using CommonGUI;
using ResizingUi.Button;

namespace DevOps.OpsScreen.FacilitatorControls.AppDevelopmentUi
{
    internal class StartAppDevelopmentButtonsPanel : FlickerFreePanel
    {
        public int PreferredHeight { get; private set; }
        public int PreferredWidth { get; private set; }
        public string SelectedId { get; private set; }

        public event EventHandler ButtonClicked;

        readonly DynamicGridLayoutPanel gridPanel;

        readonly List<StyledDynamicButton> buttons;
        
        public StartAppDevelopmentButtonsPanel (Func<List<StyledDynamicButton>> createButtonsFunc, Size minimumButtonSize, int? maximumButtonWidth, int? maximumButtonHeight)
        {
            buttons = createButtonsFunc();

            foreach (var button in buttons)
            {
                button.Click += button_Click;
            }

            gridPanel = new DynamicGridLayoutPanel(buttons, minimumButtonSize.Width, minimumButtonSize.Height)
            {
                HorizontalOuterMargin = 5,
                HorizontalInnerPadding = 5,
                VerticalOuterMargin = 5,
                VerticalInnerPadding = 5,
                AreItemsFixedSize = false,
                IsSpacingFixedSize = true,
                MaximumItemHeight = maximumButtonHeight,
                MaximumItemWidth = maximumButtonWidth
            };
            Controls.Add(gridPanel);
        }

        protected override void OnSizeChanged (EventArgs e)
        {
            DoSize();
        }

        void DoSize ()
        {
            gridPanel.Bounds = new Rectangle(0, 0, Width, Height);

            PreferredHeight = gridPanel.Height = gridPanel.PreferredGridHeight;
            PreferredWidth = gridPanel.Width = gridPanel.PreferredGridWidth;
        }

        void button_Click (object sender, EventArgs e)
        {
            foreach (var b in buttons)
            {
                b.Active = false;
            }
            
            var button = (StyledDynamicButton) sender;
            button.Active = true;

            SelectedId = (string) button.Tag;

            OnButtonClicked();
        }

        void OnButtonClicked ()
        {
            ButtonClicked?.Invoke(this, EventArgs.Empty);
        }
    }
}
