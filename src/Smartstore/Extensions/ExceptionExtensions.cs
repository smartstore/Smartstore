using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using Smartstore.Utilities;

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
        public static string GetInnerMessage(this Exception ex)
        {
            while (true)
            {
                if (ex.InnerException == null)
                {
                    return ex.Message;
                }

                ex = ex.InnerException;
            }
        }

        public static string ToAllMessages(this Exception ex, bool includeStackTrace = false)
        {
            var sb = new StringBuilder();

            while (ex != null)
            {
                if (!sb.ToString().EmptyNull().Contains(ex.Message))
                {
                    if (includeStackTrace)
                    {
                        if (sb.Length > 0)
                        {
                            sb.AppendLine();
                            sb.AppendLine();
                        }
                        sb.AppendLine(ex.ToString());
                    }
                    else
                    {
                        sb.Grow(ex.Message, " * ");
                    }
                }

                ex = ex.InnerException;
            }

            return sb.ToString();
        }
    }
}
