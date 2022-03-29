using System;
using System.IO;
using System.Collections;

using LibCore;

using Algorithms;

namespace Logging
{
	public class LogInfo
	{
		public string logDir;
		public DateTime created;
	}
	/// <summary>
	/// Summary description for AppLogger.
	/// </summary>
	public sealed class AppLogger : IComparer, ILogger
	{
		public static readonly AppLogger TheInstance = new AppLogger();

		string logDir;
		string curAppLogFile;

		FileStream app_fs = null;
		TextWriter app_tw = null;

		FileStream fs = null;
		TextWriter tw = null;

		string curLogDir;
		string curLogFile = "";

		string curLogPrefix;

		AppLogger()
		{
			logDir = AppInfo.TheInstance.Location + "\\logs";
			//
			// Make sure we have a valid log directory.
			//
			LibCore.GlobalAccess.EnsurePathExists(logDir);
			//
			// Only keep the last five times the App has been run.
			// Clear any logs prior to that.
			//
			string[] oldLogs = Directory.GetDirectories(logDir, "*.*");

			if(oldLogs.Length > 10)
			{
				ArrayList logs = new ArrayList();

				foreach(string dir in oldLogs)
				{
					LogInfo li = new LogInfo();
					li.logDir = dir;
					li.created = Directory.GetCreationTime(dir);
					logs.Add(li);
				}

				QuickSort quickSort = new QuickSort(this, new DefaultSwap());
				quickSort.Sort(logs);

				int delCount = oldLogs.Length - 10;

				for(int i=0; i<delCount; ++i)
				{
					LogInfo li = (LogInfo) logs[0];
					logs.RemoveAt(0);
					Directory.Delete(li.logDir, true);
				}
			}
			//
			// Create a new log directory.
			//
			curLogDir = logDir + "\\" + LibCore.CONVERT.ToStr(DateTime.Now).Replace(":","-");
			curLogPrefix = LibCore.CONVERT.ToStr(DateTime.Now).Replace(":", "-");

			while(Directory.Exists(curLogDir))
			{
				System.Threading.Thread.Sleep(1000);
				curLogDir = logDir + "\\" + LibCore.CONVERT.ToStr(DateTime.Now).Replace(":","-");
				curLogPrefix = LibCore.CONVERT.ToStr(DateTime.Now).Replace(":", "-");
			}
			//
			LibCore.GlobalAccess.EnsurePathExists(curLogDir);
			//
			CreateAppLog("MainApp.log");

			AppInfo.TheInstance.SetLogger(this);

			WriteLine("{0} v {1} started", System.Windows.Forms.Application.ProductName, System.Windows.Forms.Application.ProductVersion);
		}

		public void GWriteLine(string line)
		{
			if(null != tw)
			{
				tw.WriteLine(line);
				tw.Flush();
			}
		}

		public void GWrite(string line)
		{
			if(null != tw)
			{
				tw.Write(line);
				tw.Flush();
			}
		}

		string [] SplitStringAtString (string source, string split)
		{
			ArrayList strings = new ArrayList ();
			
			int position = 0;

			while (position < source.Length)
			{
				int nextSplit = source.IndexOf(split, position);
				int chunkLength = 0;
				int nextPosition;

				if (nextSplit == -1)
				{
					chunkLength = source.Length - position;
					nextPosition = source.Length;
				}
				else
				{
					chunkLength = nextSplit - position;
					nextPosition = nextSplit + split.Length;
				}

				if (chunkLength > 0)
				{
					strings.Add(source.Substring(position, chunkLength));
				}
				position = nextPosition;
			}

			return (string []) strings.ToArray(typeof(string));
		}

		public void WriteException (string type, Exception exception)
		{
			try
			{
				while (exception != null)
				{
					string [] preStack = SplitStringAtString(exception.StackTrace, "\r\n");
					string [] stack = SplitStringAtString(System.Environment.StackTrace, "\r\n");

					// The first few levels in the stack will be in System.Environment, so skip them...
					int skipLevels = 0;
					while ((skipLevels < stack.Length) && (stack[skipLevels].IndexOf("System.Environment") != -1))
					{
						skipLevels++;
					}

					// And one level is this function, and one is the function that caught the exception and called us,
					// which will also be present in the exception stack trace.
					skipLevels += 2;

					WriteLine(type + " : " + exception.Message);
					for (int i = 0; i < preStack.Length; i++)
					{
						WriteLine(preStack[i]);
					}
					for (int i = skipLevels; i < stack.Length; i++)
					{
						WriteLine(stack[i]);
					}

					if (exception.InnerException != null)
					{
						WriteLine("Inner exception:");
					}
					exception = exception.InnerException;
				}
			}
			catch
			{
				WriteLine("Stack trace unavailable");
			}
		}

		public void WriteLine(string line, params object [] args)
		{
			if(null != app_tw)
			{
				app_tw.WriteLine(DateTime.Now.ToString() + " : " + CONVERT.Format(line, args));
				app_tw.Flush();
			}

			Console.WriteLine(CONVERT.Format(line, args));
		}

		public void Write (string line, params object[] args)
		{
			if(null != app_tw)
			{
				app_tw.Write(DateTime.Now.ToString() + " : " + CONVERT.Format(line, args));
				app_tw.Flush();
			}

			Console.Write(CONVERT.Format(line, args));
		}

		public void CreateAppLog(string file)
		{
			CloseAppLog();
			curAppLogFile = this.curLogDir + "\\" + file;
			OpenAppLog();
		}

		public void CreateLog(string file)
		{
			CloseLog();
			if(file != "MainApp.log")
			{
				//string dt = LibCore.CONVERT.ToStr( DateTime.Now ).Replace(":","-");
				//curLogFile = this.curLogDir + "\\" + dt + "-" + file;
				curLogFile = this.curLogDir + "\\" + curLogPrefix + "-" + file;
			}
			else
			{
				curLogFile = this.curLogDir + "\\" + file;
			}
			OpenLog();
		}

		public void CloseGameLog()
		{
			CloseLog();
		}

		public void Close()
		{
			CloseLog();
			CloseAppLog();
		}

		void OpenAppLog()
		{
			if( (null == app_tw) && (curAppLogFile != "") )
			{
				app_fs = new FileStream(curAppLogFile, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
				app_tw = new StreamWriter(app_fs);
			}
		}

		void OpenLog()
		{
			if( (null == tw) && (curLogFile != "") )
			{
				fs = new FileStream(curLogFile, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
				tw = new StreamWriter(fs);
			}
		}

		void CloseAppLog()
		{
			if(null != app_tw)
			{
				app_tw.Close();
				app_fs.Close();
				app_tw = null;
				app_fs = null;
			}
		}

		void CloseLog()
		{
			if(null != tw)
			{
				tw.Close();
				fs.Close();
				tw = null;
				fs = null;
			}
		}

		#region IComparer Members

		public int Compare(object x, object y)
		{
			LogInfo li1 = (LogInfo) x;
			LogInfo li2 = (LogInfo) y;
			TimeSpan ts = li1.created - li2.created;
			return (int) ts.TotalSeconds;
		}

		#endregion
	}
}
