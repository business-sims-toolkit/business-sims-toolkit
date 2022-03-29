using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibCore
{
	public class ComboBoxOption
	{
		public string Text;
		public object Tag;

		public override string ToString () => Text;

		public ComboBoxOption ()
		{
		}

		public ComboBoxOption (string text, object tag)
		{
			Text = text;
			Tag = tag;
		}
	}
}