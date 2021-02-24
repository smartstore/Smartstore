using System;
using System.Threading;
using System.Threading.Tasks;

namespace Smartstore.Scheduling
{
    /// <summary>
    /// Represents a scheduled background task.
    /// </summary>
    public partial interface ITask
    {
        /// <summary>
        /// Runs a task implementation.
        /// </summary>
		/// <param name="ctx">The execution context</param>
        Task Run(TaskExecutionContext ctx, CancellationToken cancelToken = default);
    }
}