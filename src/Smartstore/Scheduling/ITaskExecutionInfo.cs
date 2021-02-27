using System;

namespace Smartstore.Scheduling
{
    public interface ITaskExecutionInfo : ICloneable<ITaskExecutionInfo>
    {
        /// <summary>
        /// Gets the task descriptor associated with this execution info.
        /// </summary>
        ITaskDescriptor Task { get; }
        
        /// <summary>
        /// A value indicating whether the task is currently running.
        /// </summary>
        bool IsRunning { get; set; }

        /// <summary>
        /// The date when the task was started. It is also the date when this entry was created.
        /// </summary>
        DateTime StartedOnUtc { get; set; }

        /// <summary>
        /// The date when the task has been finished.
        /// </summary>
        DateTime? FinishedOnUtc { get; set; }

        /// <summary>
        /// The date when the task succeeded.
        /// </summary>
        DateTime? SucceededOnUtc { get; set; }

        /// <summary>
        /// The last error message.
        /// </summary>
        string Error { get; set; }

        /// <summary>
        /// The current percentual progress for a running task.
        /// </summary>
        int? ProgressPercent { get; set; }

        /// <summary>
        /// The current progress message for a running task.
        /// </summary>
        string ProgressMessage { get; set; }
    }
}
