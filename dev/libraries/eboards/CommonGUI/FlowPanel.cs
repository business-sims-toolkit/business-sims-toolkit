using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace CommonGUI
{
    public class FlowPanel : Panel
    {
        public FlowDirection FlowDirection;

        [DefaultValue(true)]
        public bool WrapContents;

        int outerMargin = 0;
        public int OuterMargin
        {
            get
            {
                return outerMargin;
            }
            set
            {
                outerMargin =
                    OuterHorizontalMargin =
                    OuterVerticalMargin = value;
            }
        }

        int outerHorizontalMargin = 0;
        public int OuterHorizontalMargin
        {
            get
            {
                return outerHorizontalMargin;
            }
            set
            {
                outerHorizontalMargin =
                    OuterLeftMargin =
                    OuterRightMargin = value;
            }
        }

        public int OuterLeftMargin;
        public int OuterRightMargin;


        int outerVerticalMargin = 0;
        public int OuterVerticalMargin
        {
            get
            {
                return outerVerticalMargin;
            }
            set
            {
                outerVerticalMargin =
                    OuterTopMargin =
                    OuterBottomMargin = value;
            }
        }

        public int OuterTopMargin;
        public int OuterBottomMargin;

        int innerPadding;
        public int InnerPadding
        {
            get
            {
                return innerPadding;
            }
            set
            {
                innerPadding =
                    InnerHorizontalPadding =
                    InnerVerticalPadding = value;
            }
        }

        public int InnerHorizontalPadding;

        public int InnerVerticalPadding;



        int currentX;
        int nextX;

        int currentY;
        int nextY;

        public FlowPanel()
        {
            currentX = OuterLeftMargin;
            currentY = OuterTopMargin;
            nextX = OuterLeftMargin;
            nextY = OuterTopMargin;
            
        }

        protected override void OnControlAdded(ControlEventArgs e)
        {
            base.OnControlAdded(e);
            
            if (FlowDirection == FlowDirection.LeftToRight)
            {
                currentX = nextX;

                if (WrapContents && (currentX + e.Control.Width > Width - OuterRightMargin))
                {
                    currentX = OuterLeftMargin;
                    currentY = nextY;
                }

                e.Control.Location = new Point(currentX, currentY);

                nextX = e.Control.Right + InnerHorizontalPadding;
                nextY = Math.Max(nextY, currentY + e.Control.Height + InnerVerticalPadding);

            }
            else if (FlowDirection == FlowDirection.TopDown)
            {
                currentY = nextY;

                if (WrapContents && (currentY + e.Control.Height > Height - OuterBottomMargin))
                {
                    currentY = OuterTopMargin;
                    currentX = nextX;
                }

                e.Control.Location = new Point(currentX, currentY);
                nextY = e.Control.Bottom + InnerVerticalPadding;
                nextX = Math.Max(nextX, currentX + e.Control.Width + InnerHorizontalPadding);
            }

        }

    }
}
