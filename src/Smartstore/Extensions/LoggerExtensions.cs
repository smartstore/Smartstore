#nullable enable

namespace Microsoft.Extensions.Logging;

public static class LoggerExtensions
{
    #region Is[X]Enabled

    extension (ILogger l)
    {
        public bool IsTraceEnabled()
            => l.IsEnabled(LogLevel.Trace);

        public bool IsDebugEnabled()
            => l.IsEnabled(LogLevel.Debug);

        public bool IsInfoEnabled()
            => l.IsEnabled(LogLevel.Information);

        public bool IsWarnEnabled()
            => l.IsEnabled(LogLevel.Warning);

        public bool IsErrorEnabled()
            => l.IsEnabled(LogLevel.Error);

        public bool IsCriticalEnabled()
            => l.IsEnabled(LogLevel.Critical);
    }

    #endregion

    #region Log methods

    extension (ILogger l)
    {
        public void Trace(string? msg, params object?[] args)
        {
            l.LogTrace(msg, args);
        }

        public void Trace(Exception? ex, params object?[] args)
        {
            l.LogTrace(ex, null, args);
        }

        public void Trace(Func<string?> msgFactory, params object?[] args)
        {
            if (l.IsEnabled(LogLevel.Trace))
                l.LogTrace(msgFactory(), args);
        }

        public void Trace(Exception? ex, string? msg, params object?[] args)
        {
            l.LogTrace(ex, msg, args);
        }


        public void Debug(string? msg, params object?[] args)
        {
            l.LogDebug(msg, args);
        }

        public void Debug(Exception? ex, params object?[] args)
        {
            l.LogDebug(ex, null, args);
        }

        public void Debug(Func<string?> msgFactory, params object?[] args)
        {
            if (l.IsEnabled(LogLevel.Debug))
                l.LogDebug(msgFactory(), args);
        }

        public void Debug(Exception? ex, string? msg, params object?[] args)
        {
            l.LogDebug(ex, msg, args);
        }


        public void Info(string? msg, params object?[] args)
        {
            l.LogInformation(msg, args);
        }

        public void Info(Exception? ex, params object?[] args)
        {
            l.LogInformation(ex, null, args);
        }

        public void Info(Func<string?> msgFactory, params object?[] args)
        {
            if (l.IsEnabled(LogLevel.Information))
                l.LogInformation(msgFactory(), args);
        }

        public void Info(Exception? ex, string? msg, params object?[] args)
        {
            l.LogInformation(ex, msg, args);
        }


        public void Warn(string? msg, params object?[] args)
        {
            l.LogWarning(msg, args);
        }

        public void Warn(Exception? ex, params object?[] args)
        {
            l.LogWarning(ex, null, args);
        }

        public void Warn(Func<string?> msgFactory, params object?[] args)
        {
            if (l.IsEnabled(LogLevel.Warning))
                l.LogWarning(msgFactory(), args);
        }

        public void Warn(Exception? ex, string? msg, params object?[] args)
        {
            l.LogWarning(ex, msg, args);
        }


        public void Error(string? msg, params object?[] args)
        {
            l.LogError(msg, args);
        }

        public void Error(Exception? ex, params object?[] args)
        {
            l.LogError(ex, ex?.Message, args);
        }

        public void Error(Func<string?> msgFactory, params object?[] args)
        {
            if (l.IsEnabled(LogLevel.Error))
                l.LogError(msgFactory(), args);
        }

        public void Error(Exception? ex, string? msg, params object?[] args)
        {
            l.LogError(ex, msg, args);
        }

        public void ErrorsAll(Exception? exception)
        {
            if (!l.IsEnabled(LogLevel.Error))
            {
                return;
            }

            while (exception != null)
            {
                l.LogError(exception, exception?.Message);
                exception = exception?.InnerException;
            }
        }


        public void Critical(string? msg, params object?[] args)
        {
            l.LogCritical(msg, args);
        }

        public void Critical(Exception? ex, params object?[] args)
        {
            l.LogCritical(ex, null, args);
        }

        public void Critical(Func<string?> msgFactory, params object?[] args)
        {
            if (l.IsEnabled(LogLevel.Critical))
                l.LogCritical(msgFactory(), args);
        }

        public void Critical(Exception? ex, string? msg, params object?[] args)
        {
            l.LogCritical(ex, msg, args);
        }
    }

    #endregion
}
