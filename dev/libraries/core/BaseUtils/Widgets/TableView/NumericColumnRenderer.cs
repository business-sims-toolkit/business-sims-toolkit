using System;

namespace BaseUtils
{
	/// <summary>
	/// Extends DefaultColumnRenderer to format
	/// numeric data.
	/// </summary>
	public class NumericColumnRenderer : DefaultColumnRenderer
	{
		string format;
		string pre;
		string post;

		/// <summary>
		/// Creates an instance of NumericColumnRenderer.
		/// </summary>
		/// <param name="format"></param>
		public NumericColumnRenderer(string format)
		{
			this.format = format;
			this.pre = String.Empty;
			this.post = String.Empty;
		}

		/// <summary>
		/// Creates an instance of NumericColumnRenderer.
		/// </summary>
		/// <param name="format"></param>
		/// <param name="pre"></param>
		/// <param name="post"></param>
		public NumericColumnRenderer(string format, string pre, string post)
		{
			this.format = format;
			this.pre = pre;
			this.post = post;
		}

		/// <summary>
		/// Formats the specified value.
		/// </summary>
		/// <param name="val"></param>
		/// <returns></returns>
		public override object FormatValue(object val)
		{
			float converted = Convert.ToSingle(val);
			return pre + converted.ToString(format) + post;
		}
	}
}
