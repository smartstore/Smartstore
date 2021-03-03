using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Data.Caching;
using Smartstore.Data.Hooks;
using Smartstore.Domain;

namespace Smartstore.Scheduling
{
    public class TaskExecutionInfoMap : IEntityTypeConfiguration<TaskExecutionInfo>
    {
        public void Configure(EntityTypeBuilder<TaskExecutionInfo> builder)
        {
            builder
                .HasOne(x => x.Task)
                .WithMany(x => x.ExecutionHistory)
                .HasForeignKey(x => x.TaskDescriptorId);
        }
    }

    [Hookable(false)]
    [CacheableEntity(NeverCache = true)]
    [Index(nameof(MachineName), nameof(IsRunning), Name = "IX_MachineName_IsRunning")]
    [Index(nameof(StartedOnUtc), nameof(FinishedOnUtc), Name = "IX_Started_Finished")]
    [Table("ScheduleTaskHistory")]
    public class TaskExecutionInfo : BaseEntity, ITaskExecutionInfo
    {
        private readonly ILazyLoader _lazyLoader;

        public TaskExecutionInfo()
        {
        }

        public TaskExecutionInfo(ILazyLoader lazyLoader)
        {
            _lazyLoader = lazyLoader;
        }

        /// <summary>
        /// Gets or sets the schedule task identifier.
        /// </summary>
        [Column("ScheduleTaskId")]
        public int TaskDescriptorId { get; set; }

        /// <inheritdoc />
        public bool IsRunning { get; set; }

        /// <inheritdoc />
        [Required, StringLength(400)]
        public string MachineName { get; set; }

        /// <inheritdoc />
        public DateTime StartedOnUtc { get; set; }

        /// <inheritdoc />
        public DateTime? FinishedOnUtc { get; set; }

        /// <inheritdoc />
        public DateTime? SucceededOnUtc { get; set; }

        /// <inheritdoc />
        public string Error { get; set; }

        /// <inheritdoc />
        public int? ProgressPercent { get; set; }

        /// <inheritdoc />
        [StringLength(1000)]
        public string ProgressMessage { get; set; }

        /// <inheritdoc />
        ITaskDescriptor ITaskExecutionInfo.Task => Task;

        private TaskDescriptor _task;
        /// <summary>
        /// Gets or sets the task descriptor associated with this execution info.
        /// </summary>
        public TaskDescriptor Task
        {
            get => _lazyLoader?.Load(this, ref _task) ?? _task;
            set => _task = value;
        }

        object ICloneable.Clone()
            => this.Clone();

        public ITaskExecutionInfo Clone()
        {
            var clone = (TaskExecutionInfo)MemberwiseClone();
            clone.Task = (TaskDescriptor)Task.Clone();
            return clone;
        }
    }
}
