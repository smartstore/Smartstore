using System;

namespace Smartstore.Scheduling
{
    public interface ITaskExecutionInfo
    {
        /// <summary>
        /// A value indicating whether the task is currently running.
        /// </summary>
        bool IsRunning { get; }
    }
}
