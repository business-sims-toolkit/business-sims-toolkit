using System;

namespace BaseUtils
{
	/// <summary>
	/// Factory that returns one of three TypeFormatter implementations
	/// depending on the type of data supplied.
	/// </summary>
	internal class TypeFormatterFactory
	{
		static TypeFormatter arrayFormatter;
		static TypeFormatter exceptionFormatter;
		static TypeFormatter defaultFormatter;

		/// <summary>
		/// Returns an appropriate TypeFormatter implementation, based on
		/// the type of the object specified.
		/// </summary>
		/// <param name="data">The object to return the TypeFormatter for.</param>
		/// <returns>TypeFormatter</returns>
		public static TypeFormatter GetTypeFormatter(object data)
		{
			if (data == null)
				return DefaultFormatter();
			else if (data.GetType().IsArray)
				return ArrayFormatter();
			else if (data is Exception)
				return ExceptionFormatter();
			else
				return DefaultFormatter();
		}

		/// <summary>
		/// Creates an instance of ArrayTypeFormatter only when
		/// required.
		/// </summary>
		/// <returns>TypeFormatter</returns>
		static TypeFormatter ArrayFormatter()
		{
			if (arrayFormatter == null)
				arrayFormatter = new ArrayTypeFormatter();

			return arrayFormatter;
		}

		/// <summary>
		/// Creates an instance of ExceptionTypeFormatter only when
		/// required.
		/// </summary>
		/// <returns>TypeFormatter</returns>
		static TypeFormatter ExceptionFormatter()
		{
			if (exceptionFormatter == null)
				exceptionFormatter = new ExceptionTypeFormatter();

			return exceptionFormatter;
		}

		/// <summary>
		/// Creates an instance of DefaultTypeFormatter only when
		/// required.
		/// </summary>
		/// <returns>TypeFormatter</returns>
		static TypeFormatter DefaultFormatter()
		{
			if (defaultFormatter == null)
				defaultFormatter = new TypeFormatter();

			return defaultFormatter;
		}
	}
}
