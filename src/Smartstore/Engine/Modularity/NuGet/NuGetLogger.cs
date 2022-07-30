using NuGet.Common;
using MsLogger = Microsoft.Extensions.Logging.ILogger;
using MsLogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Smartstore.Engine.Modularity.NuGet
{
    internal class NuGetLogger : LoggerBase
    {
        private static readonly Func<string, Exception, string> _messageFormatter = MessageFormatter;

        private readonly MsLogger _logger;

        public NuGetLogger(MsLogger logger)
        {
            _logger = Guard.NotNull(logger, nameof(logger));
        }

        public override void Log(ILogMessage message)
        {
            _logger.Log(
                ConvertLevel(message.Level),
                0,
                message.FormatWithCode(),
                null,
                _messageFormatter);
        }

        public override Task LogAsync(ILogMessage message)
        {
            Log(message);
            return Task.CompletedTask;
        }

        protected override bool DisplayMessage(LogLevel messageLevel)
        {
            return _logger.IsEnabled(ConvertLevel(messageLevel));
        }

        private static MsLogLevel ConvertLevel(LogLevel level)
        {
            return level switch
            {
                LogLevel.Verbose => MsLogLevel.Trace,
                LogLevel.Information => MsLogLevel.Information,
                LogLevel.Minimal => MsLogLevel.Information,
                LogLevel.Warning => MsLogLevel.Warning,
                LogLevel.Error => MsLogLevel.Debug,
                _ => MsLogLevel.Debug
            };
        }

        private static string MessageFormatter(string state, Exception error) => state;
    }
}
