using System;
using System.Collections;

using System.Windows.Forms;
using System.Drawing;

using LibCore;

namespace CommonGUI
{
	public class CancelPendingCoolingUpgradePanel : Panel
	{
		TransUpgradeAppControl_DC parent;
		FocusJumper focusJumper = new FocusJumper ();

		public CancelPendingCoolingUpgradePanel (TransUpgradeAppControl_DC parent,
		                                         Hashtable pendingUpgradeTimeByName,
		                                         Color upColor, Color hoverColor, Color disabledColor,
		                                         Font font)
		{
			this.parent = parent;

			int x = 250;
			int y = 10;

			for (int zone = 1; zone <= 7; zone++)
			{
				string name = "Zone Cooling " + CONVERT.ToStr(zone);
				if (pendingUpgradeTimeByName.ContainsKey(name))
				{
					Label label = new Label ();
					label.Location = new Point (0, y);
					label.Text = "Zone " + CONVERT.ToStr(zone) + " Cooling upgrade due day " + (int) pendingUpgradeTimeByName[name];
					label.Size = new Size (x - 10, label.Height);
					label.Font = font;
					label.TextAlign = ContentAlignment.MiddleRight;
					Controls.Add(label);

					ImageTextButton cancel = new ImageTextButton (0);
					cancel.SetVariants(@"\images\buttons\blank_small.png");
					cancel.Font = font;
					cancel.Size = new Size(75, 20);
					cancel.Location = new Point(x + 10, y); 
					cancel.SetButtonText("Cancel", upColor, upColor, hoverColor, disabledColor);
					cancel.Tag = name;
					cancel.Click += cancel_Click;
					Controls.Add(cancel);
					focusJumper.Add(cancel);

					y += 30;
				}
			}

			ImageTextButton close = new ImageTextButton (0);
			close.SetVariants(@"\images\buttons\blank_small.png");
			close.Font = font;
			close.Size = new Size(75, 20);
			close.Location = new Point(450, 200); 
			close.SetButtonText("Close", upColor, upColor, hoverColor, disabledColor);
			close.Click += close_Click;
			Controls.Add(close);
			focusJumper.Add(close);
		}

		protected void cancel_Click (object sender, EventArgs e)
		{
			ImageTextButton button = sender as ImageTextButton;
			parent.CancelPendingUpgrade(null, button.Tag as String);
			parent.CloseCancelPendingCoolingUpgradePanel(this);
		}

		protected void close_Click (object sender, EventArgs e)
		{
			parent.CloseCancelPendingCoolingUpgradePanel(this);
		}
	}
}