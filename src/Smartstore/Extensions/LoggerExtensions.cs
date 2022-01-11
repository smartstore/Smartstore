namespace Microsoft.Extensions.Logging
{
    public static class LoggerExtensions
    {
        #region Is[X]Enabled

        public static bool IsTraceEnabled(this ILogger l)
        {
            return l.IsEnabled(LogLevel.Trace);
        }

        public static bool IsDebugEnabled(this ILogger l)
        {
            return l.IsEnabled(LogLevel.Debug);
        }

        public static bool IsInfoEnabled(this ILogger l)
        {
            return l.IsEnabled(LogLevel.Information);
        }

        public static bool IsWarnEnabled(this ILogger l)
        {
            return l.IsEnabled(LogLevel.Warning);
        }

        public static bool IsErrorEnabled(this ILogger l)
        {
            return l.IsEnabled(LogLevel.Error);
        }

        public static bool IsCriticalEnabled(this ILogger l)
        {
            return l.IsEnabled(LogLevel.Critical);
        }

        #endregion

        #region Log methods

        public static void Trace(this ILogger l, string msg, params object[] args)
        {
            l.LogTrace(msg, args);
        }

        public static void Trace(this ILogger l, Exception ex, params object[] args)
        {
            l.LogTrace(ex, null, args);
        }

        public static void Trace(this ILogger l, Func<string> msgFactory, params object[] args)
        {
            if (l.IsEnabled(LogLevel.Trace))
                l.LogTrace(msgFactory(), args);
        }

        public static void Trace(this ILogger l, Exception ex, string msg, params object[] args)
        {
            l.LogTrace(ex, msg, args);
        }


        public static void Debug(this ILogger l, string msg, params object[] args)
        {
            l.LogDebug(msg, args);
        }

        public static void Debug(this ILogger l, Exception ex, params object[] args)
        {
            l.LogDebug(ex, null, args);
        }

        public static void Debug(this ILogger l, Func<string> msgFactory, params object[] args)
        {
            if (l.IsEnabled(LogLevel.Debug))
                l.LogDebug(msgFactory(), args);
        }

        public static void Debug(this ILogger l, Exception ex, string msg, params object[] args)
        {
            l.LogDebug(ex, msg, args);
        }


        public static void Info(this ILogger l, string msg, params object[] args)
        {
            l.LogInformation(msg, args);
        }

        public static void Info(this ILogger l, Exception ex, params object[] args)
        {
            l.LogInformation(ex, null, args);
        }

        public static void Info(this ILogger l, Func<string> msgFactory, params object[] args)
        {
            if (l.IsEnabled(LogLevel.Information))
                l.LogInformation(msgFactory(), args);
        }

        public static void Info(this ILogger l, Exception ex, string msg, params object[] args)
        {
            l.LogInformation(ex, msg, args);
        }


        public static void Warn(this ILogger l, string msg, params object[] args)
        {
            l.LogWarning(msg, args);
        }

        public static void Warn(this ILogger l, Exception ex, params object[] args)
        {
            l.LogWarning(ex, null, args);
        }

        public static void Warn(this ILogger l, Func<string> msgFactory, params object[] args)
        {
            if (l.IsEnabled(LogLevel.Warning))
                l.LogWarning(msgFactory(), args);
        }

        public static void Warn(this ILogger l, Exception ex, string msg, params object[] args)
        {
            l.LogWarning(ex, msg, args);
        }


        public static void Error(this ILogger l, string msg, params object[] args)
        {
            l.LogError(msg, args);
        }

        public static void Error(this ILogger l, Exception ex, params object[] args)
        {
            l.LogError(ex, ex.Message, args);
        }

        public static void Error(this ILogger l, Func<string> msgFactory, params object[] args)
        {
            if (l.IsEnabled(LogLevel.Error))
                l.LogError(msgFactory(), args);
        }

        public static void Error(this ILogger l, Exception ex, string msg, params object[] args)
        {
            l.LogError(ex, msg, args);
        }

        public static void ErrorsAll(this ILogger l, Exception exception)
        {
            if (!l.IsEnabled(LogLevel.Error))
            {
                return;
            }

            while (exception != null)
            {
                l.LogError(exception, exception.Message);
                exception = exception.InnerException;
            }
        }


        public static void Critical(this ILogger l, string msg, params object[] args)
        {
            l.LogCritical(msg, args);
        }

        public static void Critical(this ILogger l, Exception ex, params object[] args)
        {
            l.LogCritical(ex, null, args);
        }

        public static void Critical(this ILogger l, Func<string> msgFactory, params object[] args)
        {
            if (l.IsEnabled(LogLevel.Critical))
                l.LogCritical(msgFactory(), args);
        }

        public static void Critical(this ILogger l, Exception ex, string msg, params object[] args)
        {
            l.LogCritical(ex, msg, args);
        }

        #endregion
    }
}
