using System;

namespace ResizingUi.Button
{
	internal interface IStyledButton
	{
		event EventHandler HighlightChanged;

		bool Highlighted { get; }
	}
}
