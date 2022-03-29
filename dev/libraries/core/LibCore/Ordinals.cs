namespace LibCore
{
    public static class Ordinals
    {
        public static string GetOrdinal(int number)
        {
            int modTen = number % 100;
            int modHundred = number % 100;

            string suffix;

            if (modTen == 1 && modHundred != 11)
            {
                suffix = "st";
            }
            else if (modTen == 2 && modHundred != 12)
            {
                suffix = "nd";
            }
            else if (modTen == 3 && modHundred != 13)
            {
                suffix = "rd";
            }
            else
            {
                suffix = "th";
            }
            
            return CONVERT.Format("{0}{1}", number, suffix);
        }

    }
}
