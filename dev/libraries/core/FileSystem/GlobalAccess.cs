using System.IO;
using System.Security.AccessControl;

namespace FileSystem
{
	public static class GlobalAccess
	{
		/// <summary>
		/// If you're not the owner of the given file, then this will probably require verification via
		/// UAC under Vista and later.
		/// </summary>
		/// <param name="path"></param>
		public static void SetGlobalControl (string path)
		{
			System.Security.Principal.SecurityIdentifier everyone = new System.Security.Principal.SecurityIdentifier(System.Security.Principal.WellKnownSidType.WorldSid, null);

			if (Directory.Exists(path))
			{
				try
				{
					DirectorySecurity security = Directory.GetAccessControl(path);
					security.AddAccessRule(new FileSystemAccessRule(everyone, FileSystemRights.FullControl, InheritanceFlags.ContainerInherit, PropagationFlags.None, AccessControlType.Allow));
					security.AddAccessRule(new FileSystemAccessRule(everyone, FileSystemRights.FullControl, InheritanceFlags.ObjectInherit, PropagationFlags.InheritOnly, AccessControlType.Allow));
					Directory.SetAccessControl(path, security);
				}
				catch
				{
				}

				foreach (string child in Directory.GetDirectories(path))
				{
					SetGlobalControl(child);
				}
				foreach (string child in Directory.GetFiles(path))
				{
					SetGlobalControl(child);
				}
			}

			if (File.Exists(path))
			{
				try
				{
					FileSecurity security = File.GetAccessControl(path);
					security.AddAccessRule(new FileSystemAccessRule(everyone, FileSystemRights.FullControl, AccessControlType.Allow));
					File.SetAccessControl(path, security);
				}
				catch
				{
				}
			}
		}

		public static void EnsurePathExists (string path)
		{
			if (!Directory.Exists(path))
			{
				string parent = Path.GetDirectoryName(path);
				if ((parent != "") && (parent != null) && (parent != path) && ((parent.IndexOf('\\') != -1) || (parent.IndexOf("/") != -1)))
				{
					EnsurePathExists(parent);
				}

				Directory.CreateDirectory(path);
				SetGlobalControl(path);
			}
		}
	}
}