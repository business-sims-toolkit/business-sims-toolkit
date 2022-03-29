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
	public class VariableHeightAWT : BasePanel
	{
		NodeTree MyTreeRootHandle;

		VariableHeightWarningLevelsDisplay warningLevelsDisplay;

		public VariableHeightAWT (NodeTree model, Color BackColor, Color newTitleForeColor, Color newTitleBackColor, Color newStatusEmptyColor)
			: this(model, BackColor, newTitleForeColor, newTitleBackColor, newStatusEmptyColor, -1, -1, -1)
		{
		}

		public VariableHeightAWT (NodeTree model, Color BackColor, Color newTitleForeColor, Color newTitleBackColor, Color newStatusEmptyColor,
			int groupStartIndex, int groupCount, int monitorHeight)
		{
			this.BackColor = BackColor;
			MyTreeRootHandle = model;
			warningLevelsDisplay = new VariableHeightWarningLevelsDisplay(model, true, BackColor, newTitleForeColor, newTitleBackColor, newStatusEmptyColor, groupStartIndex, groupCount, monitorHeight);
			warningLevelsDisplay.Location = new Point(0,0);
			warningLevelsDisplay.Size = new Size(320,170);
			this.Controls.Add(warningLevelsDisplay);
			warningLevelsDisplay.BringToFront();

			this.Resize += VariableHeightAWT_Resize;
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

		void VariableHeightAWT_Resize(object sender, EventArgs e)
		{
			warningLevelsDisplay.Size = this.Size;
		}
	}
}