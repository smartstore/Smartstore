using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Smartstore.Diagnostics
{
    public sealed class AutoStopwatch : IDisposable
    {
        private readonly ILogger _logger;
        private readonly string _message;
        private readonly Stopwatch _stopwatch;
        private bool _disposed;

        public AutoStopwatch(ILogger logger, string message) =>
            (_logger, _message, _stopwatch) = (logger, message, Stopwatch.StartNew());

        public AutoStopwatch(string message) =>
            (_message, _stopwatch) = (message, Stopwatch.StartNew());

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            if (_logger != null)
            {
                _logger.LogInformation($"{_message}: {_stopwatch.ElapsedMilliseconds}ms");
            }
            else
            {
                Debug.WriteLine($"{_message}: {_stopwatch.ElapsedMilliseconds}ms");
            }    

            _disposed = true;
        }
    }
}
