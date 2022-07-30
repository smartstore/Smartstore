using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Smartstore.Data.Caching;
using Smartstore.Data.Hooks;
using Smartstore.Domain;

namespace Smartstore.Scheduling
{
    [Hookable(false)]
    [CacheableEntity(NeverCache = true)]
    public class TaskExecutionInfo : BaseEntity, ICloneable<TaskExecutionInfo>
    {
        public TaskExecutionInfo()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private TaskExecutionInfo(ILazyLoader lazyLoader)
            : base(lazyLoader)
        {
        }

        // Legacy compat
        public override string GetEntityName() => "ScheduleTaskHistory";

        /// <summary>
        /// Gets or sets the schedule task identifier.
        /// </summary>
        public int TaskDescriptorId { get; set; }

        /// <summary>
        /// A value indicating whether the task is currently running.
        /// </summary>
        public bool IsRunning { get; set; }

        /// <summary>
        /// The server machine name that leased the task execution.
        /// </summary>
        public string MachineName { get; set; }

        /// <summary>
        /// The date when the task was started. It is also the date when this entry was created.
        /// </summary>
        public DateTime StartedOnUtc { get; set; }

        /// <summary>
        /// The date when the task has been finished.
        /// </summary>
        public DateTime? FinishedOnUtc { get; set; }

        /// <summary>
        /// The date when the task succeeded.
        /// </summary>
        public DateTime? SucceededOnUtc { get; set; }

        /// <summary>
        /// The last error message.
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// The current percentual progress for a running task.
        /// </summary>
        public int? ProgressPercent { get; set; }

        /// <summary>
        /// The current progress message for a running task.
        /// </summary>
        public string ProgressMessage { get; set; }


        private TaskDescriptor _task;
        /// <summary>
        /// Gets or sets the task descriptor associated with this execution info.
        /// </summary>
        public TaskDescriptor Task
        {
            get => _task ?? LazyLoader.Load(this, ref _task);
            set => _task = value;
        }

        object ICloneable.Clone()
            => Clone();

        public TaskExecutionInfo Clone()
        {
            var clone = (TaskExecutionInfo)MemberwiseClone();
            clone.Task = Task.Clone();
            return clone;
        }
    }
}
