using System;
using System.Text;

namespace BaseUtils
{
	/// <summary>
	/// Extends TypeFormatter to format Exception data
	/// into XML format.
	/// </summary>
	internal class ExceptionTypeFormatter : TypeFormatter
	{
		/// <summary>
		/// Creates an instance of ExceptionTypeFormatter.
		/// </summary>
		public ExceptionTypeFormatter() : base()
		{
		}

		/// <summary>
		/// Transforms the specified Exception into
		/// an XML fragment. If the Exception contains
		/// an InnerException, it will be transformed recursivelly.
		/// </summary>
		/// <param name="data">The Exception to transform.</param>
		/// <returns>string</returns>
		public override string Format(object data)
		{
			string result = String.Empty;
			Exception ex = data as Exception;

			if (ex != null)
			{
				StringBuilder sb = new StringBuilder();

				sb.Append("<Exception>\r\n");
				sb.Append(String.Format("<Type>{0}</Type>\r\n", ex.GetType()));
				sb.Append(String.Format("<Source>{0}</Source>\r\n", ex.Source));
				sb.Append(String.Format("<Message>{0}</Message>\r\n", ex.Message));
				sb.Append(String.Format("<StackTrace>\r\n{0}</StackTrace>\r\n", ex.StackTrace));

				// If there is an InnerException, transform it
				// using this formatter.
				if (ex.InnerException != null)
				{
					sb.Append("<InnerException>\r\n");
					sb.Append(Format(ex.InnerException));
					sb.Append("</InnerException>\r\n");
				}

				sb.Append("</Exception>\r\n");
				result = sb.ToString();
			}

			return result;
		}
	}
}
