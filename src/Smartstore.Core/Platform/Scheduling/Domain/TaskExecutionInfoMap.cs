using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Smartstore.Scheduling
{
    internal class TaskExecutionInfoMap : IEntityTypeConfiguration<TaskExecutionInfo>
    {
        public void Configure(EntityTypeBuilder<TaskExecutionInfo> builder)
        {
            builder.ToTable("ScheduleTaskHistory");
            builder.Property(x => x.TaskDescriptorId).HasColumnName("ScheduleTaskId");
            builder.Property(x => x.MachineName).IsRequired().HasMaxLength(400);
            builder.Property(x => x.ProgressMessage).HasMaxLength(1000);

            builder.HasIndex(nameof(TaskExecutionInfo.MachineName), nameof(TaskExecutionInfo.IsRunning)).HasDatabaseName("IX_MachineName_IsRunning");
            builder.HasIndex(nameof(TaskExecutionInfo.StartedOnUtc), nameof(TaskExecutionInfo.FinishedOnUtc)).HasDatabaseName("IX_Started_Finished");

            builder
                .HasOne(x => x.Task)
                .WithMany(x => x.ExecutionHistory)
                .HasForeignKey(x => x.TaskDescriptorId);
        }
    }
}
