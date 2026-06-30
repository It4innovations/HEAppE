using System;
using NodaTime;

namespace HEAppE.Utils;

public static class DateTimeZoneExtension
{
    private static string NormalizeZone(string zone)
    {
        if (string.IsNullOrEmpty(zone)) return zone;
        var z = zone.Trim();
        if (string.Equals(z, "CEST", StringComparison.OrdinalIgnoreCase))
        {
            return "Europe/Prague";
        }
        return z;
    }

    /// <summary>
    ///     Convert date with specific zone to UTC
    ///     Available zones https://nodatime.org/TimeZones
    /// </summary>
    /// <param name="dateInZone">Date in zone</param>
    /// <param name="zone">Zone</param>
    /// <returns></returns>
    public static DateTime Convert(this DateTime dateInZone, string zone)
    {
        zone = NormalizeZone(zone);
        var timeZone = string.IsNullOrEmpty(zone)
            ? DateTimeZoneProviders.Bcl.GetSystemDefault()
            : (DateTimeZoneProviders.Tzdb.GetZoneOrNull(zone) ?? DateTimeZoneProviders.Bcl.GetZoneOrNull(zone));

        if (timeZone == null)
        {
            timeZone = DateTimeZoneProviders.Bcl.GetSystemDefault();
        }

        var utcTime = LocalDateTime.FromDateTime(dateInZone).InZoneLeniently(timeZone).ToDateTimeUtc();
        return utcTime;
    }

    /// <summary>
    ///     Convert UTC date to local date in specific timezone
    /// </summary>
    public static DateTime? ConvertUtcToLocal(this DateTime? utcTime, string zone)
    {
        if (!utcTime.HasValue) return null;
        zone = NormalizeZone(zone);
        var timeZone = string.IsNullOrEmpty(zone)
            ? DateTimeZoneProviders.Bcl.GetSystemDefault()
            : (DateTimeZoneProviders.Tzdb.GetZoneOrNull(zone) ?? DateTimeZoneProviders.Bcl.GetZoneOrNull(zone));

        if (timeZone == null)
        {
            timeZone = DateTimeZoneProviders.Bcl.GetSystemDefault();
        }

        var instant = Instant.FromDateTimeUtc(DateTime.SpecifyKind(utcTime.Value, DateTimeKind.Utc));
        return instant.InZone(timeZone).ToDateTimeUnspecified();
    }

    /// <summary>
    ///     Convert UTC date to local date in specific timezone
    /// </summary>
    public static DateTime ConvertUtcToLocal(this DateTime utcTime, string zone)
    {
        zone = NormalizeZone(zone);
        var timeZone = string.IsNullOrEmpty(zone)
            ? DateTimeZoneProviders.Bcl.GetSystemDefault()
            : (DateTimeZoneProviders.Tzdb.GetZoneOrNull(zone) ?? DateTimeZoneProviders.Bcl.GetZoneOrNull(zone));

        if (timeZone == null)
        {
            timeZone = DateTimeZoneProviders.Bcl.GetSystemDefault();
        }

        var instant = Instant.FromDateTimeUtc(DateTime.SpecifyKind(utcTime, DateTimeKind.Utc));
        return instant.InZone(timeZone).ToDateTimeUnspecified();
    }

    /// <summary>
    ///     Convert date with local time to UTC
    /// </summary>
    /// <param name="dateInZone">Date in zone</param>
    /// <returns></returns>
    public static DateTime Convert(this DateTime dateInZone)
    {
        var timeZone = DateTimeZoneProviders.Bcl.GetSystemDefault();
        var utcTime = LocalDateTime.FromDateTime(dateInZone).InZoneLeniently(timeZone).ToDateTimeUtc();
        return utcTime;
    }

    /// <summary>
    ///     Get actual date in UTC
    /// </summary>
    /// <returns></returns>
    public static DateTime GetActualTimeInUtc()
    {
        return SystemClock.Instance.GetCurrentInstant().ToDateTimeUtc();
    }
}