using System;

namespace CommonGUI
{
    internal interface IStyledButton
    {
        event EventHandler HighlightChanged;

        bool Highlighted { get; }
    }
}