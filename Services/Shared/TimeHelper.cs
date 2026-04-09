using System;
using System.Globalization;

namespace Darb.Api.Helpers
{
    public static class TimeHelper
    {
        public static TimeOnly ParseTime(string timeStr)
        {
            if (string.IsNullOrWhiteSpace(timeStr))
            {
                throw new ArgumentException("Time string cannot be null or empty.");
            }

            // Possible formats: "02:30 PM", "2:30 PM", "14:30", "2:30"
            string[] formats = { "hh:mm tt", "h:mm tt", "HH:mm", "H:mm", "hh:mm", "h:mm" };
            
            if (TimeOnly.TryParseExact(timeStr, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
            {
                return time;
            }

            throw new FormatException($"Invalid time format: '{timeStr}'. Expected formats include '02:30 PM', '2:30 PM' or '14:30'.");
        }
    }
}
