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
    }
}
