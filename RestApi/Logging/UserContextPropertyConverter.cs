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

        var isUserAction = loggingEvent.LookupProperty("isUserAction");
        bool isUser = isUserAction != null && isUserAction.ToString().Equals("true", StringComparison.OrdinalIgnoreCase);

        if (userId != null || userName != null || userEmail != null)
        {
            string uId = userId?.ToString() ?? "0";
            string uName = userName?.ToString() ?? "ANONYMOUS";
            string uEmail = userEmail?.ToString() ?? "ANONYMOUS";
            
            writer.Write($"{uId} {uName} {uEmail}");
        }
        else if (isUser)
        {
            writer.Write("0 ANONYMOUS ANONYMOUS");
        }
        else
        {
            writer.Write("SYSTEM");
        }

        if (jobId != null)
        {
            writer.Write($" #{jobId}");
        }
    }
}
