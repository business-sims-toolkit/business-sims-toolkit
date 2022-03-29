using System.Xml;

namespace BaseUtils
{
	/// <summary>
	/// The base class for transforming data of any type
	/// into strings. Provides basic implementation for transforming
	/// null data and XmlNode data.
	/// </summary>
	internal class TypeFormatter
	{
		/// <summary>
		/// Creates an instance of TypeFormatter.
		/// </summary>
		public TypeFormatter()
		{
		}

		/// <summary>
		/// Transforms the specified object into string format.
		/// </summary>
		/// <param name="data">The data to transform.</param>
		/// <returns>string</returns>
		public virtual string Format(object data)
		{
			if (data == null)
				return "NULL";
			else if (data is XmlNode)
				return (data as XmlNode).OuterXml;
			else
				return data.ToString();
		}
	}
}
