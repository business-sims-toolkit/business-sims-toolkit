using System;
using System.Windows.Forms;
using System.Drawing;

namespace CommonGUI
{
	public class PopUpPanel : Form
	{
        bool isShowingDialogueBox = false;

		public PopUpPanel ()
		{
			ShowInTaskbar = false;
			FormBorderStyle = FormBorderStyle.None;
		}

		protected override void OnDeactivate (EventArgs e)
		{
			base.OnDeactivate(e);
		    if (!isShowingDialogueBox)
		    {
		      // Close();
		    }
		}

        public DialogResult ShowDialogueBox(string message, MessageBoxButtons buttons, string caption = "")
        {
            isShowingDialogueBox = true;

            DialogResult result = MessageBox.Show(TopLevelControl, message, caption, buttons);
            
            isShowingDialogueBox = false;

            return result;
        }

		public void Show (Control parent, int x, int y, int width, int height)
		{
			Show(parent, new Rectangle (x, y, width, height));
		}

		public void Show (Control parent, Rectangle rectangle)
		{
			Bounds = MapRectangle(parent, parent.TopLevelControl, rectangle);
			Show(parent.TopLevelControl);
			Bounds = MapRectangle(parent, parent.TopLevelControl, rectangle);
		    Select();
		}

		public Rectangle MapRectangle (Control from, Control to, Rectangle rectangle)
		{
			Point topLeftFrom = new Point (rectangle.Left, rectangle.Top);
			Point bottomRightFrom = new Point (rectangle.Right, rectangle.Bottom);

			Point topLeftTo = to.PointToClient(from.PointToScreen(topLeftFrom));
			Point bottomRightTo = to.PointToClient(from.PointToScreen(bottomRightFrom));

			return new Rectangle (topLeftTo.X, topLeftTo.Y, bottomRightTo.X - topLeftTo.X, bottomRightTo.Y - topLeftTo.Y);
		}
	}
}