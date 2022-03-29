namespace LibCore
{
	public static class Plurals
	{
		public static string Format (int number, string singular)
		{
			return Format(number, singular, singular + "s");
		}

		public static string Format (int number, string singular, string plural)
		{
			return CONVERT.Format("{0} {1}", number, (number == 1) ? singular : plural);
		}
	}
}
