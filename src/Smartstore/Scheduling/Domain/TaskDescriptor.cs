using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Smartstore.Data.Caching;
using Smartstore.Data.Hooks;
using Smartstore.Domain;

namespace Smartstore.Scheduling
{
    public enum TaskPriority
    {
        Low = -1,
        Normal = 0,
        High = 1
    }

    [DebuggerDisplay("{Name} (Type: {Type})")]
    [Hookable(false)]
    [CacheableEntity(NeverCache = true)]
    public class TaskDescriptor : BaseEntity, ICloneable<TaskDescriptor>
    {
        public TaskDescriptor()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private TaskDescriptor(ILazyLoader lazyLoader)
            : base(lazyLoader)
        {
        }

        // Legacy compat
        public override string GetEntityName() => "ScheduleTask";

        /// <summary>
        /// The task name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the task alias (an optional key for advanced customization)
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        /// The CRON expression used to calculate future schedules.
        /// </summary>
        public string CronExpression { get; set; }

        /// <summary>
        /// The type name of corresponding <see cref="ITask"/> implementation class.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// A value indicating whether the task is enabled.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// The task priority. Tasks with higher priority run first when multiple tasks are pending.
        /// </summary>
        public TaskPriority Priority { get; set; } = TaskPriority.Normal;

        /// <summary>
        /// A value indicating whether a task should be stopped on any error.
        /// </summary>
        public bool StopOnError { get; set; }

        /// <summary>
        /// The next schedule time for the task.
        /// </summary>
        public DateTime? NextRunUtc { get; set; }

        /// <summary>
        /// A value indicating whether the task is hidden.
        /// </summary>
        public bool IsHidden { get; set; }

        /// <summary>
        /// Indicates whether the task is executed decidedly on each machine of a web farm.
        /// </summary>
        public bool RunPerMachine { get; set; }

        /// <summary>
        /// Gets a value indicating whether a task is scheduled for execution (Enabled = true and NextRunUtc &lt;= UtcNow and is not running).
        /// </summary>
        public bool IsPending
            => Enabled
               && NextRunUtc.HasValue
               && NextRunUtc <= DateTime.UtcNow
               && (LastExecution == null || !LastExecution.IsRunning);

        /// <summary>
        /// Gets info about the last (or current) execution.
        /// </summary>
        public TaskExecutionInfo LastExecution { get; set; }

        private ICollection<TaskExecutionInfo> _executionHistory;
        /// <summary>
        /// Gets infos about all past executions.
        /// </summary>
        [IgnoreDataMember]
        public ICollection<TaskExecutionInfo> ExecutionHistory
        {
            get => _executionHistory ?? LazyLoader.Load(this, ref _executionHistory) ?? (_executionHistory ??= new HashSet<TaskExecutionInfo>());
            protected set => _executionHistory = value;
        }

        object ICloneable.Clone()
            => Clone();

        public TaskDescriptor Clone()
        {
            return new TaskDescriptor
            {
                Name = Name,
                Alias = Alias,
                CronExpression = CronExpression,
                Type = Type,
                Enabled = Enabled,
                StopOnError = StopOnError,
                NextRunUtc = NextRunUtc,
                IsHidden = IsHidden,
                RunPerMachine = RunPerMachine
            };
        }
    }
}
