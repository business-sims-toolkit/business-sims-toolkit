using System.Drawing;

using Network;
using LibCore;

namespace CommonGUI
{
	public class Race_AutoRestore_Item : Race_SLA_Item
	{
		public Race_AutoRestore_Item (NodeTree nt, Node n, Font displayFontBold8, Font InfoFontBold8, Font entryBoxFontNormal10)
			: base (nt, n, displayFontBold8, InfoFontBold8, entryBoxFontNormal10)
		{
			entry.CharNotToIgnore('0');
		}

		protected override void SetTextFromExternalState (EntryBox entry)
		{
			entry.Text = CONVERT.ToStr(monitoredItem.GetIntAttribute("auto_restore_time", 0) / 60);
		}

		protected override void SetExternalStateFromText (EntryBox entry)
		{
			monitoredItem.SetAttribute("auto_restore_time", CONVERT.ParseInt(entry.Text) * 60);
		}
	}
}