using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace NeowUi
{
	public class TabBarItem : ITabBarItem
	{
		string name;
		bool enabled;

		public TabBarItem (string name)
		{
			this.name = name;
		}

		public string Name
		{
			get
			{
				return name;
			}
		}

		public bool Enabled
		{
			get
			{
				return enabled;
			}

			set
			{
				enabled = value;
				OnChanged();
			}
		}

		public event EventHandler Changed;

		void OnChanged ()
		{
			if (Changed != null)
			{
				Changed(this, EventArgs.Empty);
			}
		}
	}
}