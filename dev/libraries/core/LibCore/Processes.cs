using System.Text;
using System.Diagnostics;

namespace LibCore
{
	public static class Processes
	{
		public static string RunProcessCapturingOutput (string command, params string [] args)
		{
			ProcessStartInfo startInfo = new ProcessStartInfo ();
			startInfo.CreateNoWindow = true;
			startInfo.RedirectStandardInput = true;
			startInfo.RedirectStandardOutput = true;
			startInfo.UseShellExecute = false;
			startInfo.Arguments = string.Join(" ", args);
			startInfo.FileName = command;

			StringBuilder builder = new StringBuilder ();

			Process process = new Process ();
			process.StartInfo = startInfo;
			process.OutputDataReceived += delegate (object sender, DataReceivedEventArgs receivedArgs)
			                              {
												builder.AppendLine(receivedArgs.Data);
			                              };

			process.Start();
			process.BeginOutputReadLine();
			process.WaitForExit();

			return builder.ToString();
		}
	}
}