using log4net.Core;
using log4net.Layout;
using log4net.Layout.Pattern;
using System.IO;

namespace HEAppE.RestApi
{
    /// <summary>
    /// Mapper text log level to Enum log value
    /// </summary>
    public class LoggingPatternLayout : PatternLayout
    {

        public LoggingPatternLayout()
        {
            AddConverter("level", typeof(MyReflectionReader));
        }
    }

    /// <summary>
    /// Convert Reader for mapping text log level to Enum log value
    /// </summary>
    public class MyReflectionReader : PatternLayoutConverter
    {
        protected override void Convert(TextWriter writer, LoggingEvent loggingEvent)
        {
            if (loggingEvent.Level.Name == "DEBUG") writer.Write(1);
            if (loggingEvent.Level.Name == "INFO") writer.Write(2);
            if (loggingEvent.Level.Name == "WARN") writer.Write(3);
            if (loggingEvent.Level.Name == "ERROR") writer.Write(4);
            if (loggingEvent.Level.Name == "FATAL") writer.Write(5);
        }
    }
}
