using System;
using System.Runtime.Serialization;

namespace Licensor
{
	[Serializable]
	public class LicensorException : Exception
	{
		public LicensorException()
		{
		}

		public LicensorException(string message) : base(message)
		{
		}

		public LicensorException(string message, Exception innerException) : base(message, innerException)
		{
		}

		protected LicensorException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}
	}
}