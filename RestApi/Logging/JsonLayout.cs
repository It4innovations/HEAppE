using System;
using System.IO;
using log4net.Core;
using log4net.Layout;

namespace HEAppE.RestApi.Logging
{
    public class JsonLayout : LayoutSkeleton
    {
        public override void ActivateOptions()
        {
        }

        public override void Format(TextWriter writer, LoggingEvent loggingEvent)
        {
            var logEntry = new
            {
                timestamp = loggingEvent.TimeStampUtc.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                level = loggingEvent.Level?.Name,
                logger = loggingEvent.LoggerName,
                message = loggingEvent.RenderedMessage,
                requestId = loggingEvent.LookupProperty("requestId")?.ToString(),
                userId = loggingEvent.LookupProperty("userId")?.ToString(),
                userName = loggingEvent.LookupProperty("userName")?.ToString(),
                exception = loggingEvent.GetExceptionString()
            };

            var json = System.Text.Json.JsonSerializer.Serialize(logEntry);
            writer.WriteLine(json);
        }
    }
}
