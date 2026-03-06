using System;
using log4net.Core;
using log4net.Layout.Pattern;
using System.IO;

namespace HEAppE.RestApi.Logging;

/// <summary>
///     Custom pattern layout converter to output user context in single string
/// </summary>
public class UserContextPropertyConverter : PatternLayoutConverter
{
    protected override void Convert(TextWriter writer, LoggingEvent loggingEvent)
    {
        var userId = loggingEvent.LookupProperty("userId");
        var userName = loggingEvent.LookupProperty("userName");
        var userEmail = loggingEvent.LookupProperty("userEmail");

        if (userId != null || userName != null || userEmail != null)
        {
            string uName = userName?.ToString();
            string uEmail = userEmail?.ToString();

            if (!string.IsNullOrEmpty(uName) && string.Equals(uName, uEmail, StringComparison.OrdinalIgnoreCase))
            {
                writer.Write($"{userId} {uName}".TrimEnd());
            }
            else
            {
                writer.Write($"{userId} {userName} {userEmail}".TrimEnd());
            }
        }
        else
        {
            var isUserAction = loggingEvent.LookupProperty("isUserAction");
            if (isUserAction != null && isUserAction.ToString().Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                writer.Write("Anonymous");
            }
            else
            {
                writer.Write("System");
            }
        }
    }
}
