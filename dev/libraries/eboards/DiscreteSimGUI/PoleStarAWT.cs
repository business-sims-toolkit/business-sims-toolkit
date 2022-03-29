using System;
using System.Drawing;
using LibCore;
using Network;
using CommonGUI;

namespace DiscreteSimGUI
{
	/// <summary>
	/// Summary description for OpenViewAWT.
	/// </summary>
	public class PoleStarAWT : BasePanel
	{
		NodeTree MyTreeRootHandle;

		WarningLevelsDisplay warningLevelsDisplay;

		public PoleStarAWT(NodeTree model, Color BackColor, Color newTitleForeColor, Color newTitleBackColor, 
			Color newStatusEmptyColor)
			: this(model, BackColor, newTitleForeColor, newTitleBackColor, newStatusEmptyColor, -1, -1, -1)
		{
		}

		public PoleStarAWT(NodeTree model, Color BackColor, Color newTitleForeColor, Color newTitleBackColor, 
			Color newStatusEmptyColor,  int groupStartIndex, int groupCount, int monitorHeight)
			: this(model, BackColor, newTitleForeColor, newTitleBackColor, newStatusEmptyColor, -1, -1, -1, 0, 0)
		{
		}


		public PoleStarAWT(NodeTree model, Color BackColor, Color newTitleForeColor, Color newTitleBackColor, 
			Color newStatusEmptyColor,  int groupStartIndex, int groupCount, int monitorHeight, int offsetX, int offsetY)
		{
			this.BackColor = BackColor;
			MyTreeRootHandle = model;
			warningLevelsDisplay = new WarningLevelsDisplay(model, true, BackColor, newTitleForeColor, newTitleBackColor, newStatusEmptyColor, groupStartIndex, groupCount, monitorHeight);
			warningLevelsDisplay.Location = new Point(offsetX,offsetY);
			warningLevelsDisplay.Size = new Size(320,170);
			this.Controls.Add(warningLevelsDisplay);
			warningLevelsDisplay.BringToFront();

			this.Resize += PoleStarAWT_Resize;
		}

		/// <summary>
		/// Dispose...
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose(bool disposing)
		{
			if(disposing)
			{
				warningLevelsDisplay.Dispose();
			}
			base.Dispose(disposing);
		}

		void PoleStarAWT_Resize(object sender, EventArgs e)
		{
			warningLevelsDisplay.Size = this.Size;
		}

		public void AddDaftVerticalDividers (int count)
		{
			warningLevelsDisplay.AddDaftVerticalDividers(count);
		}
	}
}