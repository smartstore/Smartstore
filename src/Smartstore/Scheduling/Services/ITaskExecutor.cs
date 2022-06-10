using Microsoft.AspNetCore.Http;

namespace Smartstore.Scheduling
{
    /// <summary>
    /// Responsible for executing a scheduled task.
    /// </summary>
    public interface ITaskExecutor
    {
        /// <summary>
        /// Executes the task asynchronously.
        /// </summary>
        /// <param name="task">The task descriptor.</param>
        /// <param name="httpContext">The current HTTP context.</param>
        /// <param name="taskParameters">Optional task parameters.</param>
        /// <param name="throwOnError">Whether to re-throw any exception.</param>
        Task ExecuteAsync(
            TaskDescriptor task,
            HttpContext httpContext,
            IDictionary<string, string> taskParameters = null,
            bool throwOnError = false,
            CancellationToken cancelToken = default);
    }
}
