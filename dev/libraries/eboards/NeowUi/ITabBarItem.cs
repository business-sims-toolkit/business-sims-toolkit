using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace NeowUi
{
	public interface ITabBarItem
	{
		string Name { get; }
		bool Enabled { get; set; }

		event EventHandler Changed;
	}
}