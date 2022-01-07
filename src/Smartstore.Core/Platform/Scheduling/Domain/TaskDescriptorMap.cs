using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Smartstore.Scheduling
{
    internal class TaskDescriptorMap : IEntityTypeConfiguration<TaskDescriptor>
    {
        public void Configure(EntityTypeBuilder<TaskDescriptor> builder)
        {
            builder.ToTable("ScheduleTask");

            builder.Ignore(x => x.IsPending);
            builder.Ignore(x => x.LastExecution);

            builder.Property(x => x.Name).IsRequired().HasMaxLength(500);
            builder.Property(x => x.Type).IsRequired().HasMaxLength(400);
            builder.Property(x => x.Alias).HasMaxLength(500);
            builder.Property(x => x.CronExpression).HasMaxLength(1000);

            builder.HasIndex(nameof(TaskDescriptor.Type)).HasDatabaseName("IX_Type");
            builder.HasIndex(nameof(TaskDescriptor.NextRunUtc), nameof(TaskDescriptor.Enabled)).HasDatabaseName("IX_NextRun_Enabled");
        }
    }
}
