using System;
using System.IO;

namespace LibCore
{
	/// <summary>
	/// Summary description for HttpRequest.
	/// </summary>
	public class HttpRequest
	{
		protected string _file = "";

		public EventHandler StateChanged;

		public HttpRequest()
		{
		}

		public void Start(string file)
		{
			_file = file;
			// Should adjust file path so that it is relative to
			// where this app is installed rather than where it is
			// run.
			if(null != StateChanged)
			{
				// Load contents of file and then report...
				StateChanged(this,null);
			}
		}

		public HttpRequestResponse Response
		{
			get
			{
				StreamReader sr = File.OpenText(_file);
				string data = sr.ReadToEnd(); 
				sr.Close();
				sr = null;

				HttpRequestResponse r = new HttpRequestResponse(data);

				return r;
			}
		}
	}

	public class HttpRequestResponse
	{
		string _data;

		public HttpRequestResponse(string data)
		{
			_data = data;
		}

		public string Data
		{
			get { return _data; }
		}
	}
}
