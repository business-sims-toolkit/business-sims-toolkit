using System;
using System.Drawing;
using System.Windows.Forms;

namespace ResizingUi
{
    public class CascadedBackgroundProperties
    {
        public event EventHandler PropertiesChanged;

        Control cascadedReferenceControl;

        public Control CascadedReferenceControl
        {
            get => cascadedReferenceControl;
            set
            {
                cascadedReferenceControl = value;
                OnPropertiesChanged();
            }
        }


        Image cascadedBackgroundImage;

        public Image CascadedBackgroundImage
        {
            get => cascadedBackgroundImage;
            set
            {
                cascadedBackgroundImage = value;
                OnPropertiesChanged();
            }
        }

        ZoomMode cascadedBackgroundImageZoomMode;

        public ZoomMode CascadedBackgroundImageZoomMode
        {
            get => cascadedBackgroundImageZoomMode;
            set
            {
                cascadedBackgroundImageZoomMode = value;
                OnPropertiesChanged();
            }
        }

        void OnPropertiesChanged ()
        {
            PropertiesChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
