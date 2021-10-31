using System;

namespace API.Extensions
{
    public static class DateTimeExtentions
    {
        public static int CalculateAge(this DateTime dt)
        {
            var today = DateTime.Today;
            var age = today.Year - dt.Year;

            if (dt.Date > today.AddYears(-age))
            {
                age -= 1;
            }

            return age;
        }
    }
}