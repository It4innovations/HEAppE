using System;
using log4net.Core;
using log4net.Layout.Pattern;
using System.IO;

namespace HEAppE.RestApi.Logging;

/// <summary>
///     Custom pattern layout converter to not display null properties in log
/// </summary>
public class EmptyIfNullPropertyConverter : PatternLayoutConverter
{
    protected override void Convert(TextWriter writer, LoggingEvent loggingEvent)
    {
        object propertyObj = loggingEvent.LookupProperty(Option);

        if (propertyObj != null)
        {
            writer.Write(propertyObj.ToString());
        }
        else if (Option == "userName" || Option == "userId" || Option == "userEmail")
        {
            var isUserAction = loggingEvent.LookupProperty("isUserAction");
            bool isUser = isUserAction != null && isUserAction.ToString()!.Equals("true", StringComparison.OrdinalIgnoreCase);

            if (isUser)
            {
                if (Option == "userId") writer.Write("0");
                else if (Option == "userName")
                {
                    var email = loggingEvent.LookupProperty("userEmail");
                    writer.Write(email?.ToString() ?? "ANONYMOUS");
                }
                else writer.Write("ANONYMOUS");
            }
            else
            {
                writer.Write("SYSTEM");
            }
        }
    }
}
