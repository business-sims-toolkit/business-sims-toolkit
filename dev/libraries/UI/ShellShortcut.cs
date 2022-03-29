using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using IWshRuntimeLibrary;

namespace UI
{
	public static class ShellShortcut
	{
		public static void CreateShortcut (string shortcutName, string targetName)
		{
			WshShell shell = new WshShellClass();
			IWshShortcut shortcut = (IWshShortcut) shell.CreateShortcut(shortcutName);
			shortcut.TargetPath = targetName;
			shortcut.Save();
		}
	}
}
