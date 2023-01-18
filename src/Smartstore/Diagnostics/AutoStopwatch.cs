using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Smartstore.Diagnostics
{
    public sealed class AutoStopwatch : IDisposable
    {
        private readonly ILogger _logger;
        private readonly string _message;
        private readonly Stopwatch _watch;
        private bool _disposed;

        public AutoStopwatch(ILogger logger, string message) =>
            (_logger, _message, _watch) = (logger, message, Stopwatch.StartNew());

        public AutoStopwatch(string message) =>
            (_message, _watch) = (message, Stopwatch.StartNew());

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            var message = $"{_message}: {_watch.ElapsedMilliseconds} ms. ({_watch.ElapsedTicks} ticks)";

            if (_logger != null)
            {
                _logger.LogDebug(message);
            }
            else
            {
                Debug.WriteLine(message);
            }    

            _disposed = true;
        }
    }
}
