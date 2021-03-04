using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Data;
using Smartstore.Data.Batching;
using Smartstore.Engine;
using Smartstore.Utilities;

namespace Smartstore.Scheduling
{
    public class DbTaskStore : ITaskStore
    {
        private readonly SmartDbContext _db;
        private readonly IApplicationContext _appContext;
        private readonly IDateTimeHelper _dtHelper;
        private readonly Lazy<CommonSettings> _commonSettings;

        public DbTaskStore(
            SmartDbContext db, 
            IApplicationContext appContext, 
            IDateTimeHelper dtHelper,
            Lazy<CommonSettings> commonSettings)
        {
            _db = db;
            _appContext = appContext;
            _dtHelper = dtHelper;
            _commonSettings = commonSettings;
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        public Localizer T { get; set; } = NullLocalizer.Instance;

        #region Task

        public IQueryable<TaskDescriptor> GetDescriptorQuery()
        {
            return _db.TaskDescriptors;
        }

        public TaskDescriptor CreateDescriptor(string name, Type type)
        {
            Guard.NotEmpty(name, nameof(name));
            Guard.NotNull(type, nameof(type));
            Guard.Implements<ITask>(type);

            return new TaskDescriptor
            {
                Name = name,
                Type = type.AssemblyQualifiedNameWithoutVersion(),
                Enabled = true
            };
        }

        public Type GetTaskClrType(TaskDescriptor task)
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

        public Task<TaskDescriptor> GetTaskByIdAsync(int taskId)
        {
            if (taskId == 0)
                return null;

            return Retry.RunAsync(
                () => _db.TaskDescriptors.FindByIdAsync(taskId).AsTask(),
                3, TimeSpan.FromMilliseconds(100),
                RetryOnTransientException);
        }

        public async Task<TaskDescriptor> GetTaskByTypeAsync(string type)
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

        public Task ReloadTaskAsync(TaskDescriptor task)
        {
            return _db.ReloadEntityAsync(task);
        }

        public Task<List<TaskDescriptor>> GetAllTasksAsync(bool includeDisabled = false)
        {
            var query = _db.TaskDescriptors.AsQueryable();

            if (!includeDisabled)
            {
                query = query.Where(t => t.Enabled);
            }

            query = query.OrderByDescending(t => t.Enabled);

            return Retry.RunAsync(
                async () => (await query.ToListAsync()),
                3, TimeSpan.FromMilliseconds(100),
                RetryOnTransientException);
        }

        public async Task<List<TaskDescriptor>> GetPendingTasksAsync()
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
                .Where(x => x.IsPending)
                .OrderByDescending(x => x.Priority)
                .ThenBy(x => x.NextRunUtc.Value)
                .ToList();

            return pendingTasks;
        }

        public Task InsertTaskAsync(TaskDescriptor task)
        {
            Guard.NotNull(task, nameof(task));

            _db.TaskDescriptors.Add(task);
            return _db.SaveChangesAsync();
        }

        public Task UpdateTaskAsync(TaskDescriptor task)
        {
            Guard.NotNull(task, nameof(task));

            try
            {
                _db.TryUpdate(task);
                return _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw;
            }
        }

        public Task DeleteTaskAsync(TaskDescriptor task)
        {
            Guard.NotNull(task, nameof(task));

            _db.TaskDescriptors.Remove(task);
            return _db.SaveChangesAsync();
        }

        public async Task<TaskDescriptor> GetOrAddTaskAsync<T>(Action<TaskDescriptor> createAction) where T : ITask
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
                _db.TaskDescriptors.Add(task);
                await _db.SaveChangesAsync();
            }

            return task;
        }

        public async Task CalculateFutureSchedulesAsync(IEnumerable<TaskDescriptor> tasks, bool isAppStart = false)
        {
            Guard.NotNull(tasks, nameof(tasks));

            foreach (var task in tasks)
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

        public DateTime? GetNextSchedule(TaskDescriptor task)
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

        public IQueryable<TaskExecutionInfo> GetExecutionInfoQuery()
        {
            return _db.TaskExecutionInfos;
        }

        public TaskExecutionInfo CreateExecutionInfo(TaskDescriptor task)
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

        public Task<TaskExecutionInfo> GetExecutionInfoByIdAsync(int id)
        {
            return _db.TaskExecutionInfos.FindByIdAsync(id).AsTask();
        }

        public Task<TaskExecutionInfo> GetLastExecutionInfoByTaskIdAsync(int taskId, bool? runningOnly = null)
        {
            throw new NotImplementedException();
        }

        public Task<TaskExecutionInfo> GetLastExecutionInfoByTaskAsync(TaskDescriptor task, bool? runningOnly = null)
        {
            throw new NotImplementedException();
        }

        public Task InsertExecutionInfoAsync(TaskExecutionInfo info)
        {
            Guard.NotNull(info, nameof(info));

            _db.TaskExecutionInfos.Add(info);
            return _db.SaveChangesAsync();
        }

        public Task UpdateExecutionInfoAsync(TaskExecutionInfo info)
        {
            Guard.NotNull(info, nameof(info));

            try
            {
                _db.TryUpdate(info);
                return _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                // Do not throw.
                return Task.CompletedTask;
            }
        }

        public Task DeleteExecutionInfoAsync(TaskExecutionInfo info)
        {
            Guard.NotNull(info, nameof(info));
            Guard.IsTrue(!info.IsRunning, nameof(info.IsRunning), "Cannot delete a running task execution info entry.");

            _db.TaskExecutionInfos.Remove(info);
            return _db.SaveChangesAsync();
        }

        public async Task<int> TrimExecutionInfosAsync(CancellationToken cancelToken = default)
        {
            var idsToDelete = new HashSet<int>();

            if (_commonSettings.Value.MaxScheduleHistoryAgeInDays > 0)
            {
                var earliestDate = DateTime.UtcNow.AddDays(-1 * _commonSettings.Value.MaxScheduleHistoryAgeInDays);
                var ids = await _db.TaskExecutionInfos
                    .AsNoTracking()
                    .Where(x => x.StartedOnUtc <= earliestDate && !x.IsRunning)
                    .Select(x => x.Id)
                    .ToListAsync(cancelToken);

                idsToDelete.AddRange(ids);
            }

            // We have to group by task otherwise we would only keep entries from very frequently executed tasks.
            if (_commonSettings.Value.MaxNumberOfScheduleHistoryEntries > 0)
            {
                var query =
                    from th in _db.TaskExecutionInfos.AsNoTracking()
                    where !th.IsRunning
                    group th by th.TaskDescriptorId into grp
                    select grp
                        .OrderByDescending(x => x.StartedOnUtc)
                        .ThenByDescending(x => x.Id)
                        .Skip(_commonSettings.Value.MaxNumberOfScheduleHistoryEntries)
                        .Select(x => x.Id);

                var ids = await query.SelectMany(x => x).ToListAsync(cancelToken);

                idsToDelete.AddRange(ids);
            }

            if (!idsToDelete.Any())
            {
                return 0;
            }

            var numDeleted = 0;

            try
            {
                using (var scope = new DbContextScope(_db, retainConnection: true))
                {
                    foreach (var batch in idsToDelete.Slice(128))
                    {
                        if (!cancelToken.IsCancellationRequested)
                        {
                            numDeleted += await _db.TaskExecutionInfos
                                .Where(x => batch.Contains(x.Id))
                                .BatchDeleteAsync(cancelToken);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

            return numDeleted;
        }

        #endregion
    }
}
