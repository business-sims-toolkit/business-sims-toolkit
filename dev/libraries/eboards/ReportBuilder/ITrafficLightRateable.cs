using System.Drawing;

namespace ReportBuilder
{
	public interface ITrafficLightRateable
	{
		Point Location { get; }

		Size Size { get; }

		Image Icon { get; }
	}
}
