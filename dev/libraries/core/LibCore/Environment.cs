using System;
using System.Runtime.InteropServices;

namespace LibCore
{
	public static class Environment
	{
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
		struct UtsName
		{
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
			public string SystemName;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
			public string NodeName;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
			public string Release;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
			public string Version;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
			public string Machine;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1024)]
			public string Extra;
		}

		[DllImport("libc")]
		static extern void uname (out UtsName results);

		public static PlatformID Platform
		{
			get
			{
				PlatformID platform = System.Environment.OSVersion.Platform;

				// Mono reports Mac OS X as Unix for compatability reasons.
				if (platform == PlatformID.Unix)
				{
					UtsName results;
					uname(out results);

					if (results.SystemName == "Darwin")
					{
						platform = PlatformID.MacOSX;
					}
				}

				return platform;
			}
		}
	}
}
