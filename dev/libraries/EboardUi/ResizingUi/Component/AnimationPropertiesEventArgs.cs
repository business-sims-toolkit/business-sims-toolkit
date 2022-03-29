using System;

namespace ResizingUi.Component
{
    public class AnimationPropertiesEventArgs : EventArgs
    {
        public AnimationProperties Properties { get; }

        public AnimationPropertiesEventArgs(AnimationProperties properties)
        {
            Properties = properties;
        }
    }
}
