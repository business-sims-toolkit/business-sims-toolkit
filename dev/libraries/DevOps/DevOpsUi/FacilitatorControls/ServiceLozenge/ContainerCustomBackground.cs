using System.Drawing;

using LibCore;
using ResizingUi;

namespace DevOpsUi.FacilitatorControls.ServiceLozenge
{
	public class ContainerCustomBackground : PicturePanel
	{
		public ContainerCustomBackground (string imageNameWithExtension = "default_background.jpg")
		{
			var backgroundImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + $@"\images\panels\backgrounds\{imageNameWithExtension}");

			ZoomWithCropping(backgroundImage, new PointF(0.5f, 0.25f), new PointF(0.5f, 0.25f));
		}
	}
}
