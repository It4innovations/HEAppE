using NodaTime;
using System;

namespace HEAppE.Utils
{
    public static class DateTimeZoneExtension
    {
        /// <summary>
        /// Convert date with specific zone to UTC
        /// Available zones https://nodatime.org/TimeZones
        /// </summary>
        /// <param name="dateInZone">Date in zone</param>
        /// <param name="zone">Zone</param>
        /// <returns></returns>
        public static DateTime Convert(this DateTime dateInZone, string zone)
        {
            DateTimeZone timeZone = DateTimeZoneProviders.Tzdb.GetZoneOrNull(zone);
            if (timeZone == null)
            {
                throw new ArgumentException($"Argument 'zone' could not find in zones");
            }

            DateTime utcTime = LocalDateTime.FromDateTime(dateInZone).InZoneStrictly(timeZone).ToDateTimeUtc();
            return utcTime;
        }

        /// <summary>
        /// Convert date with local time to UTC
        /// </summary>
        /// <param name="dateInZone">Date in zone</param>
        /// <returns></returns>
        public static DateTime Convert(this DateTime dateInZone)
        {
            DateTimeZone timeZone = DateTimeZoneProviders.Bcl.GetSystemDefault();
            DateTime utcTime = LocalDateTime.FromDateTime(dateInZone).InZoneStrictly(timeZone).ToDateTimeUtc();
            return utcTime;
        }

        /// <summary>
        /// Get actual date in UTC
        /// </summary>
        /// <returns></returns>
        public static DateTime GetActualTimeInUtc()
        {
            return SystemClock.Instance.GetCurrentInstant().ToDateTimeUtc();
        }
    }
}
