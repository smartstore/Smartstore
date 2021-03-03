using System;
using Microsoft.EntityFrameworkCore;
using Smartstore.Scheduling;

namespace Smartstore.Core.Data
{
    public partial class SmartDbContext
    {
        public DbSet<TaskDescriptor> TaskDescriptors { get; set; }
        public DbSet<TaskExecutionInfo> TaskExecutionInfos { get; set; }
    }
}
