using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace UI
{
	public static class ShellIcons
	{
		[StructLayout(LayoutKind.Sequential)]
		struct SHFILEINFO
		{
			public IntPtr hIcon;
			public IntPtr iIcon;
			public uint dwAttributes;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
			public string szDisplayName;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
			public string szTypeName;
		};

		const uint SHGFI_ICON = 0x100;
		const uint SHGFI_LARGEICON = 0x0;
		const uint SHGFI_SMALLICON = 0x1;

		[DllImport("shell32.dll")]
		static extern IntPtr SHGetFileInfo (string pszPath,
											uint dwFileAttributes,
											ref SHFILEINFO psfi,
											uint cbSizeFileInfo,
											uint uFlags);

		[DllImport("user32")]
		static extern int DestroyIcon (IntPtr hIcon);

		public static System.Drawing.Image GetIconForFile (string file)
		{
			if (! (System.IO.File.Exists(file) || System.IO.Directory.Exists(file)))
			{
				throw new System.IO.FileNotFoundException ("File not found", file);
			}

			SHFILEINFO fileInfo = new SHFILEINFO ();
			IntPtr iconHandle = SHGetFileInfo(file, 0, ref fileInfo, (uint) Marshal.SizeOf (fileInfo), SHGFI_ICON | SHGFI_LARGEICON);

			System.Drawing.Icon icon = (System.Drawing.Icon) System.Drawing.Icon.FromHandle(fileInfo.hIcon);
			System.Drawing.Image image = icon.ToBitmap();
			DestroyIcon(fileInfo.hIcon);

			return image;
		}
	}
}