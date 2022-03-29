using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using CommonGUI;
using CoreUtils;
using ResizingUi.Button;

namespace DevOps.OpsScreen
{
	internal class EnclosurePanel : Panel
    {
        public int HeightPadding { get; } = 5;

        public int WidthPadding { get; } = 7;
        
	    int heightOfEachButton = 30;

	    int widthOfEachButton = 115;

        public int ButtonWidth
        {
            get => widthOfEachButton;
            set
            {

                if (value <= 0)
                {
                    throw new Exception("Button dimension can't be less than one.");
                }

                widthOfEachButton = value;
                
            }
        }

        public int ButtonHeight
        {
            get => heightOfEachButton;
            set
            {
                if (value <= 0)
                {
                    throw new Exception("Button dimension can't be less than one.");
                }
                heightOfEachButton = value;
                
            }
        }
        
        readonly List<string> enclosures;
        readonly string optimalEnclosure;
        readonly string selectedEnclosure;

        readonly List<Point> availableLocations;

        public EnclosurePanel(List<string> enclosures, string correctEnclosure = "", string selectedEnclosure = "")
        {
            this.enclosures = enclosures;
            optimalEnclosure = correctEnclosure;
            this.selectedEnclosure = selectedEnclosure;
            
            availableLocations = new List<Point>();

            BasicLayout();
        }

	    void BasicLayout()
        {
            DoubleBuffered = true;
        }

        void CreateLocations()
        {
            const int outerWidthPadding = 5;
            const int outerHeightPadding = 0;

            const int numColumns = 4;
            const int numRows = 4;

            availableLocations.Clear();

            var subTotalWidth = Width - 2 * outerWidthPadding;
            var columnWidth = Math.Max(subTotalWidth / numColumns, ButtonWidth);

            var subTotalHeight = Height - 2 * outerHeightPadding;
            var rowHeight = Math.Max(subTotalHeight / numRows, ButtonHeight);

            for (var row = 0; row < numRows; row++)
            {
                var yOffset = row * rowHeight;
                for (var column = 0; column < numColumns; column++)
                {
                    var xOffset = column * columnWidth;

                    var x = xOffset + (columnWidth - ButtonWidth) / 2;
                    var y = yOffset + (rowHeight - ButtonHeight) / 2;

                    availableLocations.Add(new Point(x, y));
                }
            }
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);

            DoLayout();
        }

        void DoLayout()
        {
            CreateLocations();

            if (enclosures.Count != availableLocations.Count)
            {
                throw new Exception("Different number of enclosures to available locations.");
            }

            var i = 0;
            
            foreach (var name in enclosures)
            {
                var enclosure = new StyledDynamicButton("standard", name)
                {
                                                Tag = false,
                                                Size = new Size(widthOfEachButton, heightOfEachButton),
                                                Anchor = AnchorStyles.None,
                                                Location = availableLocations[i++],
                                                Font = SkinningDefs.TheInstance.GetFont(10),
                                                Active = name == selectedEnclosure,
                                                Highlighted = name.Equals(optimalEnclosure)
                                            };
                
                enclosure.Click += EnclosureClick;

                Controls.Add(enclosure);

            }
        }
        
        public event EventHandler EnclosureButtonClicked;

        StyledDynamicButton previouslyClicked;

        void OnEnclosureButtonClicked(object sender)
        {
            if (EnclosureButtonClicked != null)
            {
                if (previouslyClicked != null)
                {
                    previouslyClicked.Active = false;
                }
                
                EnclosureButtonClicked(sender, EventArgs.Empty);
                var button = (StyledDynamicButton)sender;
                button.Active = true;
                previouslyClicked = button;
            }
        }

	    void EnclosureClick(object sender, EventArgs eventArgs)
        {
            OnEnclosureButtonClicked(sender);
        }
        
    }
}
