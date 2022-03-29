using System;

namespace ResizingUi.Interfaces
{
	public interface IDynamicSharedFontSize
	{
		float FontSize { get; set; }

		float FontSizeToFit { get; }

		event EventHandler FontSizeToFitChanged;
	}
}
