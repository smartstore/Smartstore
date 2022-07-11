using Autofac;
using Smartstore.Core.Data;

namespace Smartstore.Scheduling
{
    public static class TaskQueryExtensions
    {
        /// <summary>
        /// Filters by <see cref="TaskExecutionInfo.TaskDescriptorId"/> and orders by <see cref="TaskExecutionInfo.StartedOnUtc"/>
        /// and Id, both descending.
        /// </summary>
        /// <param name="taskId">Filter by task identifier.</param>
        /// <param name="lastInfoOnly">A value indicating whether to only return the last execution info per task.</param>
        public static IOrderedQueryable<TaskExecutionInfo> ApplyTaskFilter(this IQueryable<TaskExecutionInfo> query, int taskId, bool lastInfoOnly = false)
        {
            if (taskId != 0)
            {
                query = query.Where(x => x.TaskDescriptorId == taskId);
            }

            if (lastInfoOnly)
            {
                var db = query.GetDbContext<SmartDbContext>();

                query = query
                    .Select(x => x.TaskDescriptorId)
                    .Distinct()
                    .SelectMany(key => db.TaskExecutionInfos
                        .AsNoTracking()
                        .Where(x => x.TaskDescriptorId == key)
                        .OrderByDescending(x => x.StartedOnUtc)
                        .ThenByDescending(x => x.Id)
                        .Take(1));
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
            var machineName = EngineContext.Current.Application.RuntimeInfo.MachineName;
            return query.Where(x => x.MachineName == machineName);
        }
    }
}
