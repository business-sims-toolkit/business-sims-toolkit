namespace LibCore
{
	public class VersionCheckResults
	{
		bool success;

		public bool Success
		{
			get
			{
				return success;
			}
		}

		bool upToDate;

		public bool UpToDate
		{
			get
			{
				return upToDate;
			}
		}

		string message;

		public string Message
		{
			get
			{
				return message;
			}
		}

        string url;

        public string Url
        {
            get
            {
                return url;
            }
        }

        string username;

        public string Username
        {
            get
            {
                return username;
            }
        }

        string password;

        public string Password
        {
            get
            {
                return password;
            }
        }

        public VersionCheckResults (bool success, bool upToDate, string message, string url, string username, string password)
        {
            this.success = success;
            this.upToDate = upToDate;
            this.message = message;
            this.url = url;
            this.username = username;
            this.password = password;
        }
	}
}