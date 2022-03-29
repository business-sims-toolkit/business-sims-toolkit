using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NeowUi
{
	public class TabItemSelectedEventArgs : EventArgs
	{
		public ITabBarItem Item;

		public TabItemSelectedEventArgs (ITabBarItem item)
		{
			Item = item;
		}
	}
}