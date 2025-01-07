using log4net.Core;
using log4net.Layout;
using log4net.Layout.Pattern;

namespace HEAppE.DataStagingAPI.Logging;

/// <summary>
///     Mapper text log level to Enum log value
/// </summary>
public class LoggingPatternLayout : PatternLayout
{
    public LoggingPatternLayout()
    {
        AddConverter("level", typeof(MyReflectionReader));
    }
}

/// <summary>
///     Convert Reader for mapping text log level to Enum log value
/// </summary>
public class MyReflectionReader : PatternLayoutConverter
{
    protected override void Convert(TextWriter writer, LoggingEvent loggingEvent)
    {
#pragma warning disable CS8602
        if (loggingEvent.Level.Name == "DEBUG") writer.Write(1);
        else if (loggingEvent.Level.Name == "INFO") writer.Write(2);
        else if (loggingEvent.Level.Name == "WARN") writer.Write(3);
        else if (loggingEvent.Level.Name == "ERROR") writer.Write(4);
        else if (loggingEvent.Level.Name == "FATAL") writer.Write(5);
#pragma warning restore CS8602
    }
}