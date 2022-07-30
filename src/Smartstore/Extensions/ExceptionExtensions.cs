using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace Smartstore
{
    public static class ExceptionExtensions
    {
        /// <summary>
        /// Uses <see cref="ExceptionDispatchInfo.Capture"/> method to re-throw exception
        /// while preserving stack trace.
        /// </summary>
        public static void ReThrow(this Exception exception)
            => ExceptionDispatchInfo.Capture(exception).Throw();

        public static bool IsFatal(this Exception ex)
        {
            return ex is StackOverflowException ||
                ex is OutOfMemoryException ||
                ex is AccessViolationException ||
                ex is AppDomainUnloadedException ||
                ex is ThreadAbortException ||
                ex is SecurityException ||
                ex is SEHException;
        }

        public static void Dump(this Exception ex)
        {
            try
            {
                ex.StackTrace.Dump();
                ex.Message.Dump();
            }
            catch
            {
            }
        }

        /// <summary>
        /// Gets the message of the most inner exception.
        /// </summary>
        public static string GetInnerMessage(this Exception exception)
        {
            while (true)
            {
                if (exception.InnerException == null)
                {
                    return exception.Message;
                }

                exception = exception.InnerException;
            }
        }

        public static string ToAllMessages(this Exception exception, bool includeStackTrace = false)
        {
            if (exception == null)
            {
                return string.Empty;
            }

            var sb = new StringBuilder(includeStackTrace ? exception.Message.Length * 3 : exception.Message.Length);

            while (exception != null)
            {
                // TODO: (mg) (core) Find a better way to skip redundant messages
                if (includeStackTrace)
                {
                    if (sb.Length > 0)
                    {
                        sb.AppendLine();
                        sb.AppendLine();
                    }
                    sb.AppendLine(exception.ToString());
                }
                else
                {
                    sb.Grow(exception.Message, " * ");
                }

                exception = exception.InnerException;
            }

            return sb.ToString();
        }
    }
}
