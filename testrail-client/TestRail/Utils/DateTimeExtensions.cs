using System;

namespace TestRail.Utils
{
    /// <summary>extension methods for the datetime class</summary>
    public static class DateTimeExtensions
    {
        /// <summary>converts the date to a unix timestamp</summary>
        /// <returns>a unix time stamp representing the birthday</returns>
        public static double ToUnixTimestamp(this DateTime dt)
        {
            return dt.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
        }

        /// <summary>generates a datetime from a unix timestamp</summary>
        /// <param name="timestamp">a unix timestamp</param>
        /// <returns>datetime corresponding to the supplied unix timestamp</returns>
        public static DateTime FromUnixTimeStamp(double timestamp)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(timestamp).ToLocalTime();
        }
    }
}
