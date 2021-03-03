using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Newtonsoft.Json;
using Smartstore.Data.Caching;
using Smartstore.Data.Hooks;
using Smartstore.Domain;

namespace Smartstore.Scheduling
{
    [DebuggerDisplay("{Name} (Type: {Type})")]
    [Hookable(false)]
    [CacheableEntity(NeverCache = true)]
    [Index(nameof(Type), Name = "IX_Type")]
    [Index(nameof(NextRunUtc), nameof(Enabled), Name = "IX_NextRun_Enabled")]
    [Table("ScheduleTask")]
    public class TaskDescriptor : EntityWithAttributes, ITaskDescriptor
    {
        private readonly ILazyLoader _lazyLoader;

        public TaskDescriptor()
        {
        }

        public TaskDescriptor(ILazyLoader lazyLoader)
        {
            _lazyLoader = lazyLoader;
        }

        /// <inheritdoc />
        [Required, StringLength(500)]
        public string Name { get; set; }

        /// <inheritdoc />
        [StringLength(500)]
        public string Alias { get; set; }

        /// <inheritdoc />
        [StringLength(1000)]
        public string CronExpression { get; set; }

        /// <inheritdoc />
        [Required, StringLength(800)]
        public string Type { get; set; }

        /// <inheritdoc />
        public bool Enabled { get; set; }

        /// <inheritdoc />
        public TaskPriority Priority { get; set; } = TaskPriority.Normal;

        /// <inheritdoc />
        public bool StopOnError { get; set; }

        /// <inheritdoc />
        public DateTime? NextRunUtc { get; set; }

        /// <summary>
        /// Gets or sets the value indicating whether the task is hidden/internal.
        /// </summary>
        public bool IsHidden { get; set; }

        /// <summary>
        /// Indicates whether the task is executed decidedly on each machine of a web farm.
        /// </summary>
        public bool RunPerMachine { get; set; }

        [NotMapped]
        public ITaskExecutionInfo LastExecution { get; set; }

        [NotMapped]
        IEnumerable<ITaskExecutionInfo> ITaskDescriptor.ExecutionHistory => ExecutionHistory;

        private ICollection<TaskExecutionInfo> _executionHistory;
        /// <summary>
        /// Gets or sets locale string resources
        /// </summary>
        [JsonIgnore]
        public ICollection<TaskExecutionInfo> ExecutionHistory
        {
            get => _lazyLoader?.Load(this, ref _executionHistory) ?? (_executionHistory ??= new HashSet<TaskExecutionInfo>());
            protected set => _executionHistory = value;
        }

        object ICloneable.Clone()
            => this.Clone();

        public ITaskDescriptor Clone()
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
