using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Smartstore.Data.Caching
{
    /// <summary>
    /// Formats and writes debug log messages.
    /// </summary>
    public interface IEfDebugLogger
    {
        /// <summary>
        /// Formats and writes a debug log message.
        /// </summary>
        void LogDebug(string message);

        /// <summary>
        /// Formats and writes a debug log message.
        /// </summary>
        void LogDebug(EventId eventId, string message);
    }

    internal class EfDebugLogger : IEfDebugLogger
    {
        private readonly bool _disableLogging;
        private readonly ILogger<EfDebugLogger> _logger;
        private readonly string _signature = $"InstanceId: {Guid.NewGuid()}, Started @{DateTime.UtcNow} UTC.";

        public EfDebugLogger(IOptions<EfCacheOptions> cacheOptions, ILogger<EfDebugLogger> logger)
        {
            _disableLogging = cacheOptions.Value.DisableLogging;
            _logger = logger;
        }

        public void LogDebug(string message)
        {
            if (!_disableLogging)
            {
                _logger.LogDebug($"{_signature} {message}");
            }
        }

        public void LogDebug(EventId eventId, string message)
        {
            if (!_disableLogging)
            {
                _logger.LogDebug(eventId, $"{_signature} {message}");
            }
        }
    }
}