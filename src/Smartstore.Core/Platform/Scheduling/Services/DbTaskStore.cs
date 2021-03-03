using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Engine;
using Smartstore.Utilities;

namespace Smartstore.Scheduling
{
    public class DbTaskStore : ITaskStore
    {
        private readonly SmartDbContext _db;
        private readonly IApplicationContext _appContext;
        private readonly IDateTimeHelper _dtHelper;

        public DbTaskStore(
            SmartDbContext db, 
            IApplicationContext appContext, 
            IDateTimeHelper dtHelper)
        {
            _db = db;
            _appContext = appContext;
            _dtHelper = dtHelper;
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        public Localizer T { get; set; } = NullLocalizer.Instance;

        #region Task

        public Type GetTaskClrType(ITaskDescriptor task)
        {
            try
            {
                return Type.GetType(task.Type);
            }
            catch
            {
                // TODO: (core) Map old task types to new types.
                return null;
            }
        }

        public Task<ITaskDescriptor> GetTaskByIdAsync(int taskId)
        {
            if (taskId == 0)
                return null;

            return Retry.RunAsync(
                async () => (await _db.TaskDescriptors.FindByIdAsync(taskId)) as ITaskDescriptor,
                3, TimeSpan.FromMilliseconds(100),
                RetryOnTransientException);
        }

        public async Task<ITaskDescriptor> GetTaskByTypeAsync(string type)
        {
            try
            {
                if (type.HasValue())
                {
                    var query = _db.TaskDescriptors
                        .Where(t => t.Type == type)
                        .OrderByDescending(t => t.Id);

                    // TODO: (core) Map old task types to new types.
                    var task = await query.FirstOrDefaultAsync();
                    return task;
                }
            }
            catch (Exception ex)
            {
                // Do not throw an exception if the underlying provider failed on Open.
                ex.Dump();
            }

            return null;
        }

        public Task ReloadTaskAsync(ITaskDescriptor task)
        {
            Guard.IsTypeOf<TaskDescriptor>(task);
            return _db.ReloadEntityAsync((TaskDescriptor)task);
        }

        public Task<IEnumerable<ITaskDescriptor>> GetAllTasksAsync(bool includeDisabled = false)
        {
            var query = _db.TaskDescriptors.AsQueryable();

            if (!includeDisabled)
            {
                query = query.Where(t => t.Enabled);
            }

            query = query.OrderByDescending(t => t.Enabled);

            return Retry.RunAsync(
                async () => (await query.ToListAsync()).Cast<ITaskDescriptor>(),
                3, TimeSpan.FromMilliseconds(100),
                RetryOnTransientException);
        }

        public async Task<IEnumerable<ITaskDescriptor>> GetPendingTasksAsync()
        {
            var now = DateTime.UtcNow;
            var machineName = _appContext.MachineName;

            var query = (
                from t in _db.TaskDescriptors
                where t.NextRunUtc.HasValue && t.NextRunUtc <= now && t.Enabled
                select new
                {
                    Task = t,
                    LastInfo = t.ExecutionHistory
                        .Where(th => !t.RunPerMachine || (t.RunPerMachine && th.MachineName == machineName))
                        .OrderByDescending(th => th.StartedOnUtc)
                        .ThenByDescending(th => th.Id)
                        .FirstOrDefault()
                });

            var tasks = await Retry.RunAsync(
                () => query.ToListAsync(),
                3, TimeSpan.FromMilliseconds(100),
                RetryOnTransientException);

            var pendingTasks = tasks
                .Select(x =>
                {
                    x.Task.LastExecution = x.LastInfo;
                    return x.Task;
                })
                .Where(x => x.IsPending())
                .OrderByDescending(x => x.Priority)
                .ThenBy(x => x.NextRunUtc.Value)
                .ToList();

            return pendingTasks;
        }

        public Task InsertTaskAsync(ITaskDescriptor task)
        {
            Guard.IsTypeOf<TaskDescriptor>(task);

            _db.TaskDescriptors.Add((TaskDescriptor)task);
            return _db.SaveChangesAsync();
        }

        public Task UpdateTaskAsync(ITaskDescriptor task)
        {
            Guard.IsTypeOf<TaskDescriptor>(task);

            try
            {
                _db.TryUpdate((TaskDescriptor)task);
                return _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw;
            }
        }

        public Task DeleteTaskAsync(ITaskDescriptor task)
        {
            Guard.IsTypeOf<TaskDescriptor>(task);

            _db.TaskDescriptors.Remove((TaskDescriptor)task);
            return _db.SaveChangesAsync();
        }

        public async Task<ITaskDescriptor> GetOrAddTaskAsync<T>(Action<ITaskDescriptor> createAction) where T : ITask
        {
            Guard.NotNull(createAction, nameof(createAction));

            var type = typeof(T);

            if (type.IsAbstract || type.IsInterface || type.IsNotPublic)
            {
                throw new InvalidOperationException("Only concrete public task types can be registered.");
            }

            var task = await this.GetTaskByTypeAsync<T>();

            if (task == null)
            {
                task = new TaskDescriptor { Type = type.AssemblyQualifiedNameWithoutVersion() };
                createAction(task);
                _db.TaskDescriptors.Add((TaskDescriptor)task);
                await _db.SaveChangesAsync();
            }

            return task;
        }

        public async Task CalculateFutureSchedulesAsync(IEnumerable<ITaskDescriptor> tasks, bool isAppStart = false)
        {
            Guard.NotNull(tasks, nameof(tasks));

            foreach (var task in tasks.OfType<TaskDescriptor>())
            {
                task.NextRunUtc = GetNextSchedule(task);
                if (isAppStart)
                {
                    FixTypeName(task);
                }
                else
                {
                    await UpdateTaskAsync(task);
                }
            }

            if (isAppStart)
            {
                // On app start this method's execution is thread-safe, making it sufficient
                // to commit all changes in one go.
                await _db.SaveChangesAsync();
            }

            if (isAppStart)
            {
                // Normalize task history entries.
                // That is, no task can run when the application starts and therefore no entry may be marked as running.
                var history = await _db.TaskExecutionInfos
                    .Where(x =>
                        x.IsRunning ||
                        x.ProgressPercent != null ||
                        !string.IsNullOrEmpty(x.ProgressMessage) ||
                        (x.FinishedOnUtc != null && x.FinishedOnUtc < x.StartedOnUtc)
                    )
                    .ToListAsync();

                if (history.Any())
                {
                    string abnormalAbort = T("Admin.System.ScheduleTasks.AbnormalAbort");
                    foreach (var entry in history)
                    {
                        var invalidTimeRange = entry.FinishedOnUtc.HasValue && entry.FinishedOnUtc < entry.StartedOnUtc;
                        if (invalidTimeRange || entry.IsRunning)
                        {
                            entry.Error = abnormalAbort;
                        }

                        entry.IsRunning = false;
                        entry.ProgressPercent = null;
                        entry.ProgressMessage = null;
                        if (invalidTimeRange)
                        {
                            entry.FinishedOnUtc = entry.StartedOnUtc;
                        }
                    }

                    await _db.SaveChangesAsync();
                }
            }
        }

        private static void FixTypeName(TaskDescriptor task)
        {
            // TODO: (core) Map old task types to new types.
            // In versions prior V3 a double space could exist in ScheduleTask type name.
            if (task.Type.IndexOf(",  ") > 0)
            {
                task.Type = task.Type.Replace(",  ", ", ");
            }
        }

        public DateTime? GetNextSchedule(ITaskDescriptor task)
        {
            if (task.Enabled)
            {
                try
                {
                    var localTimeZone = _dtHelper.DefaultStoreTimeZone;
                    var baseTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, localTimeZone);
                    var next = CronExpression.GetNextSchedule(task.CronExpression, baseTime);
                    var utcTime = _dtHelper.ConvertToUtcTime(next, localTimeZone);

                    return utcTime;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Could not calculate next schedule time for task '{0}'", task.Name);
                }
            }

            return null;
        }

        private void RetryOnTransientException(int attemp, Exception ex)
        {
            if (!_db.DataProvider.IsTransientException(ex))
            {
                // We only want to retry on transient/deadlock stuff.
                throw ex;
            }
        }

        #endregion

        #region History

        public IQueryable<ITaskExecutionInfo> GetExecutionInfoQuery()
        {
            return _db.TaskExecutionInfos;
        }

        public ITaskExecutionInfo CreateExecutionInfo(ITaskDescriptor task)
        {
            Guard.NotNull(task, nameof(task));

            return new TaskExecutionInfo
            {
                TaskDescriptorId = task.Id,
                IsRunning = true,
                MachineName = _appContext.MachineName.EmptyNull(),
                StartedOnUtc = DateTime.UtcNow
            };
        }

        public Task<ITaskExecutionInfo> GetExecutionInfoByIdAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task<ITaskExecutionInfo> GetLastExecutionInfoByTaskIdAsync(int taskId, bool? runningOnly = null)
        {
            throw new NotImplementedException();
        }

        public Task<ITaskExecutionInfo> GetLastExecutionInfoByTaskAsync(ITask task, bool? runningOnly = null)
        {
            throw new NotImplementedException();
        }

        public Task LoadLastExecutionInfoAsync(ITaskDescriptor task, bool force = false)
        {
            throw new NotImplementedException();
        }

        public Task InsertExecutionInfoAsync(ITaskExecutionInfo info)
        {
            throw new NotImplementedException();
        }

        public Task UpdateExecutionInfoAsync(ITaskExecutionInfo info)
        {
            throw new NotImplementedException();
        }

        public Task DeleteExecutionInfoAsync(ITaskExecutionInfo info)
        {
            throw new NotImplementedException();
        }

        public Task<int> TrimExecutionInfosAsync()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
