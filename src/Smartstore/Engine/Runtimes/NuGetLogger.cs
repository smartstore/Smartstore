using System;
using System.Threading.Tasks;
using L = Microsoft.Extensions.Logging;
using NuGet.Common;

namespace Smartstore.Engine.Runtimes
{
    internal class NuGetLogger : LoggerBase
    {
        private static readonly Func<string, Exception, string> _messageFormatter = MessageFormatter;

        private readonly L.ILogger _logger;

        public NuGetLogger(L.ILogger logger)
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

        private static L.LogLevel ConvertLevel(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Debug:
                    return L.LogLevel.Debug;
                case LogLevel.Verbose:
                    return L.LogLevel.None;
                case LogLevel.Information:
                    return L.LogLevel.Information;
                case LogLevel.Warning:
                case LogLevel.Minimal:
                    return L.LogLevel.Warning;
                case LogLevel.Error:
                    return L.LogLevel.Error;
                default:
                    break;
            }

            return L.LogLevel.Debug;
        }

        private static string MessageFormatter(string state, Exception error) => state;
    }
}
