using System;
using System.Drawing;

using CommonGUI;
using ResizingUi.Interfaces;

namespace DevOps.OpsScreen.FacilitatorControls.AppDevelopmentUi.Table
{
    // TODO replace "AppDevelopment" with something more generic and move to somewhere in common code

    

    interface IRenderableTableComponent
    {
        event EventHandler RedrawRequired;
        void Render (Graphics graphics, RectangleF bounds);
    }

    abstract class TableCell : IDynamicSharedFontSize, IRenderableTableComponent
    {
        protected TableCell (TableRow row)
        {
            parentRow = row;
        }

        public Color? BackColour { get; set; }
        
        public Color? BorderColour { get; set; }

        public float FontSize
        {
            get => fontSize;
            set
            {
                fontSize = value;
                OnRedrawRequired();
            }
        }

        public float FontSizeToFit
        {
            get => fontSizeToFit;
            protected set
            {
                if (Math.Abs(fontSizeToFit - value) > float.Epsilon)
                {
                    fontSizeToFit = value;
                    OnFontSizeToFitChanged();
                }
            }
        }
        public event EventHandler FontSizeToFitChanged;

        void OnFontSizeToFitChanged ()
        {
            FontSizeToFitChanged?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler RedrawRequired;

        void OnRedrawRequired ()
        {
            RedrawRequired?.Invoke(this, EventArgs.Empty);
        }

        public void Render (Graphics graphics, RectangleF cellBounds)
        {
            bounds = cellBounds;

            Render(graphics);
        }

        protected abstract void Render (Graphics graphics);

        protected RectangleF bounds;
        protected readonly TableRow parentRow;

        float fontSize;
        float fontSizeToFit;
    }

    class TableRow
    {

    }

    class AppDevelopmentTable : FlickerFreePanel
    {

    }
}
