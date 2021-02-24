using System;
using System.Collections.Generic;

namespace Smartstore.Scheduling
{
    public enum TaskPriority
    {
        Low = -1,
        Normal = 0,
        High = 1
    }

    public interface ITaskDescriptor
    {
        /// <summary>
        /// Unique task identifier.
        /// </summary>
        int Id { get; }

        /// <summary>
        /// The task name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The CRON expression used to calculate future schedules.
        /// </summary>
        string CronExpression { get; }

        /// <summary>
        /// The type of corresponding <see cref="ITask"/> implementation class.
        /// </summary>
        string Type { get; }

        /// <summary>
        /// A value indicating whether the task is enabled.
        /// </summary>
        bool Enabled { get; set; }

        /// <summary>
        /// The task priority. Tasks with higher priority run first when multiple tasks are pending.
        /// </summary>
        TaskPriority Priority { get; }

        /// <summary>
        /// A value indicating whether a task should be stopped on any error.
        /// </summary>
        bool StopOnError { get; }

        /// <summary>
        /// The next schedule time for the task.
        /// </summary>
        DateTime? NextRunUtc { get; set; }

        /// <summary>
        /// A value indicating whether the task is hidden.
        /// </summary>
        public bool IsHidden { get; }

        /// <summary>
        /// Indicates whether the task is executed decidedly on each machine of a web farm.
        /// </summary>
        bool RunPerMachine { get; }

        /// <summary>
        /// Gets info about the last (or current) execution.
        /// </summary>
        ITaskExecutionInfo LastExecution { get; set; }

        /// <summary>
        /// Gets infos about all past executions.
        /// </summary>
        ICollection<ITaskExecutionInfo> ExecutionHistory { get; }
    }

    public static class ITaskDescriptorExtensions
    {
        public static bool IsPending(this ITaskDescriptor descriptor)
        {
            Guard.NotNull(descriptor, nameof(descriptor));
            
            return descriptor.Enabled && 
                descriptor.NextRunUtc.HasValue && 
                descriptor.NextRunUtc <= DateTime.UtcNow && 
                (descriptor.LastExecution == null || !descriptor.LastExecution.IsRunning);
        }
    }
}
