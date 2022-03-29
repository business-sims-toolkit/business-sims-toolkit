using System;
using System.Text;

namespace BaseUtils
{
	/// <summary>
	/// Extends TypeFormatter to format array data
	/// into XML format.
	/// </summary>
	internal class ArrayTypeFormatter : TypeFormatter
	{
		/// <summary>
		/// Creates an instance of ArrayTypeFormatter.
		/// </summary>
		public ArrayTypeFormatter()
		{
		}

		/// <summary>
		/// Transforms the specified data into an XML fragment.
		/// </summary>
		/// <param name="data">The data to transform.</param>
		/// <returns>string</returns>
		public override string Format(object data)
		{
			string result = String.Empty;
			Array temp = data as Array;

			if (temp != null)
			{
				StringBuilder sb = new StringBuilder();

				sb.Append("<Array>\r\n");

				foreach (object d in temp)
				{
					if (d != null)
					{
						// Format each element in the array using the TypeFormatterFactory
						// to choose the appropriate TypeFormatter.
						string formatted = TypeFormatterFactory.GetTypeFormatter(d).Format(d);
						sb.Append(String.Format("<Element type=\"{0}\">{1}</Element>\r\n", d.GetType().Name, formatted));
					}
					else
					{
						sb.Append(String.Format("<Element type=\"{0}\">{1}</Element>\r\n", "Null", "Null"));
					}
				}

				sb.Append("</Array>\r\n");
				result = sb.ToString();
			}

			return result;
		}
	}
}
