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
        var jobId = loggingEvent.LookupProperty("jobId");

        bool hasUser = userId != null || userName != null || userEmail != null;
        
        if (hasUser)
        {
            string uId = userId?.ToString() ?? "0";
            string uName = userName?.ToString();
            string uEmail = userEmail?.ToString();

            if (!string.IsNullOrEmpty(uName) && string.Equals(uName, uEmail, StringComparison.OrdinalIgnoreCase))
            {
                writer.Write($"{uId} {uName}");
            }
            else
            {
                writer.Write($"{uId} {uName ?? ""} {uEmail ?? ""}".Trim());
            }
        }
        else
        {
            var isUserAction = loggingEvent.LookupProperty("isUserAction");
            if (isUserAction != null && isUserAction.ToString().Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                writer.Write("0 ANONYMOUS");
            }
            else
            {
                writer.Write("SYSTEM");
            }
        }

        if (jobId != null)
        {
            writer.Write($" #{jobId}");
        }
    }
}
