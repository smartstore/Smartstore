using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Smartstore.Engine;

namespace Smartstore.Scheduling
{
    public static class TaskQueryExtensions
    {
        /// <summary>
        /// Filters by <see cref="TaskExecutionInfo.TaskDescriptorId"/> and orders by <see cref="TaskExecutionInfo.StartedOnUtc"/>
        /// and Id, both descending.
        /// </summary>
        public static IOrderedQueryable<TaskExecutionInfo> ApplyTaskFilter(this IQueryable<TaskExecutionInfo> query, int taskId, bool lastEntryOnly = false)
        {
            if (taskId != 0)
            {
                query = query.Where(x => x.TaskDescriptorId == taskId);
            }

            if (lastEntryOnly)
            {
                query =
                    from th in query
                    group th by th.TaskDescriptorId into grp
                    select grp
                        .OrderByDescending(x => x.StartedOnUtc)
                        .ThenByDescending(x => x.Id)
                        .FirstOrDefault();
            }

            return query
                .OrderByDescending(x => x.StartedOnUtc)
                .ThenByDescending(x => x.Id);
        }

        /// <summary>
        /// Filters by <see cref="TaskExecutionInfo.MachineName"/> == current machine name.
        /// </summary>
        public static IQueryable<TaskExecutionInfo> ApplyCurrentMachineNameFilter(this IQueryable<TaskExecutionInfo> query)
        {
            var appContext = EngineContext.Current.Application.Services.Resolve<IApplicationContext>();
            return query.Where(x => x.MachineName == appContext.MachineName);
        }
    }
}
