#nullable enable

namespace Smartstore.Threading
{
    /// <summary>
    /// A convenience API for interacting with System.Threading.Timer in a way
    /// that doesn't capture the ExecutionContext.
    /// </summary>
    internal static class NonCapturingTimer
    {
        public static Timer Create(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            Guard.NotNull(callback);
            
            // Don't capture the current ExecutionContext and its AsyncLocals onto the timer
            bool restoreFlow = false;
            try
            {
                if (!ExecutionContext.IsFlowSuppressed())
                {
                    ExecutionContext.SuppressFlow();
                    restoreFlow = true;
                }

                return new Timer(callback, state, dueTime, period);
            }
            finally
            {
                // Restore the current ExecutionContext
                if (restoreFlow)
                {
                    ExecutionContext.RestoreFlow();
                }
            }
        }
    }
}
