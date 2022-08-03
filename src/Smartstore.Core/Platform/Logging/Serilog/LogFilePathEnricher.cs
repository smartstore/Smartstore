using Serilog.Core;
using Serilog.Events;

namespace Smartstore.Core.Logging.Serilog
{
    public sealed class LogFilePathEnricher : ILogEventEnricher
    {
        const string DefaultLogFilePath = "App_Data/Logs/Smartstore-.log";

        private string _cachedLogFilePath;
        private LogEventProperty _cachedLogFilePathProperty;

        public const string LogFilePathPropertyName = "LogFilePath";

        public void Enrich(LogEvent e, ILogEventPropertyFactory propertyFactory)
        {
            var logFilePath = ExtractPathFromSourceContext(e.GetSourceContext());

            if (logFilePath != null)
            {
                LogEventProperty logFilePathProperty;

                if (logFilePath.Equals(_cachedLogFilePath, StringComparison.OrdinalIgnoreCase))
                {
                    // Path hasn't changed, so let's use the cached property
                    logFilePathProperty = _cachedLogFilePathProperty;
                }
                else
                {
                    // We've got a new path for the log. Let's create a new property
                    // and cache it for future log events to use
                    _cachedLogFilePath = logFilePath;

                    _cachedLogFilePathProperty = logFilePathProperty =
                        propertyFactory.CreateProperty(LogFilePathPropertyName, logFilePath);
                }

                e.AddPropertyIfAbsent(logFilePathProperty);
            }
        }

        private static string ExtractPathFromSourceContext(string sourceContext)
        {
            if (string.IsNullOrEmpty(sourceContext))
            {
                return null;
            }

            var index = sourceContext.IndexOf('/');

            if (index == -1)
            {
                return DefaultLogFilePath;
            }

            var path = sourceContext[index..].Trim('/');
            var ext = Path.GetExtension(path);
            var isFullPath = ext == ".log" || ext == ".txt";

            if (!isFullPath)
            {
                path += "/log-.log";
            }

            return path;
        }
    }
}
