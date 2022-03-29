using System;
using System.Xml;

using CommonGUI;
using ResizingUi.Interfaces;

namespace Charts
{
	public abstract class BoxChart : FlickerFreePanel, IDynamicSharedFontSize
	{
		protected BoxChart(XmlElement xml)
		{
		}

		public abstract float FontSize { get; set; }

		public abstract float FontSizeToFit { get; protected set; }

		public event EventHandler FontSizeToFitChanged;

		protected void OnFontSizeToFitChanged()
		{
			FontSizeToFitChanged?.Invoke(this, EventArgs.Empty);
		}
	}
}
