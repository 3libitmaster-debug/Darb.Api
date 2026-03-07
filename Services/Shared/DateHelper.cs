using System;

namespace Darb.Api.Helpers
{
    public static class DateHelper
    {
        public static DateTime GetYemenTime()
        {
           
            return DateTime.UtcNow.AddHours(3);
        }
    }
}