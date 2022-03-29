using System;
using System.Runtime.Serialization;

namespace Licensor
{
	[Serializable]
	public class ActivationException : Exception
	{
		private object reasonNotValid;

		public ActivationException()
		{
		}

		public ActivationException(object reasonNotValid)
		{
			this.reasonNotValid = reasonNotValid;
		}

		public ActivationException(string message) : base(message)
		{
		}

		public ActivationException(string message, Exception innerException) : base(message, innerException)
		{
		}

		protected ActivationException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}
	}
}