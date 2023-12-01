using Microsoft.EntityFrameworkCore.Query;
using Polly;
using Polly.Retry;
using Smartstore.Caching.Tasks;
using Smartstore.Core.Catalog.Rules;
using Smartstore.Core.Common.Configuration;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Common.Tasks;
using Smartstore.Core.Content.Media.Tasks;
using Smartstore.Core.Data;
using Smartstore.Core.DataExchange.Export;
using Smartstore.Core.DataExchange.Import;
using Smartstore.Core.Identity.Rules;
using Smartstore.Core.Identity.Tasks;
using Smartstore.Core.Localization;
using Smartstore.Core.Logging.Tasks;
using Smartstore.Core.Messaging.Tasks;
using Smartstore.Core.Seo;
using Smartstore.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Scheduling
{
    public partial class DbTaskStore : Disposable, ITaskStore
    {
        private readonly static Dictionary<string, string> _legacyTypeNamesMap = new(StringComparer.OrdinalIgnoreCase)
        {
            { nameof(DataExportTask), "SmartStore.Services.DataExchange.Export.DataExportTask, SmartStore.Services" },
            { nameof(DataImportTask), "SmartStore.Services.DataExchange.Import.DataImportTask, SmartStore.Services" },
            { nameof(TargetGroupEvaluatorTask), "SmartStore.Services.Customers.TargetGroupEvaluatorTask, SmartStore.Services" },
            { nameof(ProductRuleEvaluatorTask), "SmartStore.Services.Catalog.ProductRuleEvaluatorTask, SmartStore.Services" },
            { nameof(RebuildXmlSitemapTask), "SmartStore.Services.Seo.RebuildXmlSitemapTask, SmartStore.Services" },
            { nameof(UpdateExchangeRateTask), "SmartStore.Services.Directory.UpdateExchangeRateTask, SmartStore.Services" },
            { nameof(ClearCacheTask), "SmartStore.Services.Caching.ClearCacheTask, SmartStore.Services" },
            { nameof(DeleteGuestsTask), "SmartStore.Services.Customers.DeleteGuestsTask, SmartStore.Services" },
            { nameof(DeleteLogsTask), "SmartStore.Services.Logging.DeleteLogsTask, SmartStore.Services" },
            { nameof(QueuedMessagesClearTask), "SmartStore.Services.Messages.QueuedMessagesClearTask, SmartStore.Services" },
            { nameof(QueuedMessagesSendTask), "SmartStore.Services.Messages.QueuedMessagesSendTask, SmartStore.Services" },
            { nameof(TempFileCleanupTask), "SmartStore.Services.Common.TempFileCleanupTask, SmartStore.Services" },
            { nameof(TransientMediaClearTask), "SmartStore.Services.Media.TransientMediaClearTask, SmartStore.Services" },

            { "IndexingTask", "SmartStore.MegaSearch.IndexingTask, SmartStore.MegaSearch" },
            { "ForumIndexingTask", "SmartStore.MegaSearch.ForumIndexingTask, SmartStore.MegaSearch" },
            { "BMEcatImportTask", "SmartStore.BMEcat.FileImportTask, SmartStore.BMEcat" },
            { "CleanupCartApprovalTask", "SmartStore.CartApproval.Tasks.DeleteObsoleteRecordsTask, SmartStore.CartApproval" },
            { "CleanupPersonalPromoTask", "SmartStore.PersonalPromo.CleanupTask, SmartStore.PersonalPromo" },
            { "NewsImportTask", "SmartStore.NewsImporter.NewsImportTask, SmartStore.NewsImporter" },
        };

        private readonly IDbContextFactory<SmartDbContext> _dbFactory;
        private readonly IApplicationContext _appContext;
        private readonly IDateTimeHelper _dtHelper;
        private readonly Lazy<CommonSettings> _commonSettings;
        private SmartDbContext _db;
        private AsyncRetryPolicy _retryPolicy;

        public DbTaskStore(
            IDbContextFactory<SmartDbContext> dbFactory,
            IApplicationContext appContext,
            IDateTimeHelper dtHelper,
            Lazy<CommonSettings> commonSettings)
        {
            _dbFactory = dbFactory;
            _appContext = appContext;
            _dtHelper = dtHelper;
            _commonSettings = commonSettings;
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        public Localizer T { get; set; } = NullLocalizer.Instance;

        protected SmartDbContext Db
        {
            get
            {
                if (_db == null)
                {
                    _db = _dbFactory.CreateDbContext();
                    _db.MinHookImportance = HookImportance.Essential;
                }

                return _db;
            }
        }

        protected override ValueTask OnDisposeAsync(bool disposing)
        {
            if (disposing && _db != null)
            {
                return _db.DisposeAsync();
            }

            return ValueTask.CompletedTask;
        }

        protected virtual Task<T> ExecuteWithRetry<T>(Func<Task<T>> action)
        {
            return GetRetryPolicy().ExecuteAsync(action);
        }

        private AsyncRetryPolicy GetRetryPolicy()
        {
            if (_retryPolicy == null)
            {
                _retryPolicy = Policy
                    .Handle<Exception>(Db.DataProvider.IsTransientException)
                    .WaitAndRetryAsync(3, attempt => TimeSpan.FromMilliseconds(100));
            }

            return _retryPolicy;
        }

        #region Task

        public virtual IQueryable<TaskDescriptor> GetDescriptorQuery()
        {
            return Db.TaskDescriptors;
        }

        public virtual TaskDescriptor CreateDescriptor(string name, Type type)
        {
            Guard.NotEmpty(name);
            Guard.NotNull(type);
            Guard.IsAssignableFrom<ITask>(type);

            return new TaskDescriptor
            {
                Name = name,
                Type = type.GetAttribute<TaskNameAttribute>(false)?.Name ?? type.Name,
                Enabled = true
            };
        }

        public virtual Task<TaskDescriptor> GetTaskByIdAsync(int taskId)
        {
            if (taskId == 0)
            {
                return Task.FromResult<TaskDescriptor>(null);
            }

            return ExecuteWithRetry(() => Db.TaskDescriptors.FindByIdAsync(taskId).AsTask());
        }

        public virtual async Task<TaskDescriptor> GetTaskByTypeAsync(string type)
        {
            try
            {
                if (type.HasValue())
                {
                    var query = _legacyTypeNamesMap.TryGetValue(type, out var legacyTypeName)
                        ? Db.TaskDescriptors.Where(t => t.Type == type || t.Type == legacyTypeName)
                        : Db.TaskDescriptors.Where(t => t.Type == type);

                    var task = await query
                        .OrderByDescending(t => t.Id)
                        .FirstOrDefaultAsync();

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

        public virtual Task ReloadTaskAsync(TaskDescriptor task)
        {
            return Db.ReloadEntityAsync(task);
        }

        public virtual Task<List<TaskDescriptor>> GetAllTasksAsync(bool includeDisabled = false, bool includeHidden = false)
        {
            var query = Db.TaskDescriptors.AsQueryable();

            if (!includeDisabled)
            {
                query = query.Where(t => t.Enabled);
            }

            if (!includeHidden)
            {
                query = query.Where(t => !t.IsHidden);
            }

            query = query.OrderByDescending(t => t.Enabled);

            return ExecuteWithRetry(() => query.ToListAsync());
        }

        public virtual async Task<List<TaskDescriptor>> GetPendingTasksAsync()
        {
            if (Db.DataProvider.ProviderType is (DbSystemType.MySql or DbSystemType.SQLite))
            {
                return await GetPendingTasksLiteAsync();
            }
            
            var now = DateTime.UtcNow;
            var machineName = _appContext.RuntimeInfo.MachineName;

            var query =
                from t in Db.TaskDescriptors
                where t.NextRunUtc.HasValue && t.NextRunUtc <= now && t.Enabled
                select new
                {
                    Task = t,
                    LastInfo = t.ExecutionHistory
                        .Where(th => !t.RunPerMachine || (t.RunPerMachine && th.MachineName == machineName))
                        .OrderByDescending(th => th.StartedOnUtc)
                        .ThenByDescending(th => th.Id)
                        .FirstOrDefault()
                };

            var tasks = await ExecuteWithRetry(() => query.ToListAsync());

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

        private async Task<List<TaskDescriptor>> GetPendingTasksLiteAsync()
        {
            var now = DateTime.UtcNow;
            var machineName = _appContext.RuntimeInfo.MachineName;

            var taskQuery =
               from t in Db.TaskDescriptors
               where t.NextRunUtc.HasValue && t.NextRunUtc <= now && t.Enabled
               select t;
            var tasks = await ExecuteWithRetry(() => taskQuery.ToListAsync());

            var executionInfoQuery =
                from info in Db.TaskExecutionInfos
                where taskQuery.Any(t => t.Id == info.TaskDescriptorId) &&
                        (!info.Task.RunPerMachine || (info.Task.RunPerMachine && info.MachineName == machineName))
                group info by info.TaskDescriptorId into infos
                select new
                {
                    TaskId = infos.Key,
                    LastInfo = infos
                        .OrderByDescending(h => h.StartedOnUtc)
                        .ThenByDescending(h => h.Id)
                        .FirstOrDefault()
                };
            var executionInfos = await ExecuteWithRetry(() => executionInfoQuery.ToDictionaryAsync(x => x.TaskId, x => x.LastInfo));

            var pendingTasks = tasks
                .Select(x =>
                {
                    x.LastExecution = executionInfos.Get(x.Id);
                    return x;
                })
                .Where(x => x.IsPending)
                .OrderByDescending(x => x.Priority)
                .ThenBy(x => x.NextRunUtc.Value)
                .ToList();

            return pendingTasks;
        }

        public virtual Task InsertTaskAsync(TaskDescriptor task)
        {
            Guard.NotNull(task);

            Db.TaskDescriptors.Add(task);
            return Db.SaveChangesAsync();
        }

        public virtual Task UpdateTaskAsync(TaskDescriptor task)
        {
            Guard.NotNull(task);

            try
            {
                Db.TryUpdate(task);
                return Db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw;
            }
        }

        public virtual Task DeleteTaskAsync(TaskDescriptor task)
        {
            Guard.NotNull(task);

            Db.TaskDescriptors.Remove(task);
            return Db.SaveChangesAsync();
        }

        public virtual async Task<TaskDescriptor> GetOrAddTaskAsync<T>(Action<TaskDescriptor> createAction) where T : ITask
        {
            Guard.NotNull(createAction);

            var type = typeof(T);

            if (type.IsAbstract || type.IsInterface)
            {
                throw new InvalidOperationException("Only concrete task types can be registered.");
            }

            var task = await this.GetTaskByTypeAsync<T>();

            if (task == null)
            {
                task = new TaskDescriptor
                {
                    Type = type.GetAttribute<TaskNameAttribute>(false)?.Name ?? type.Name
                };

                createAction(task);
                Db.TaskDescriptors.Add(task);
                await Db.SaveChangesAsync();
            }

            return task;
        }

        public virtual async Task CalculateFutureSchedulesAsync(IEnumerable<TaskDescriptor> tasks, bool isAppStart = false)
        {
            Guard.NotNull(tasks);

            foreach (var task in tasks)
            {
                task.NextRunUtc = GetNextSchedule(task);
                if (!isAppStart)
                {
                    await UpdateTaskAsync(task);
                }
            }

            if (isAppStart)
            {
                // On app start this method's execution is thread-safe, making it sufficient
                // to commit all changes in one go.
                await Db.SaveChangesAsync();

                // Normalize task history entries.
                // That is, no task can run when the application starts and therefore no entry may be marked as running.
                var history = await Db.TaskExecutionInfos
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

                    await Db.SaveChangesAsync();
                }
            }
        }

        public virtual DateTime? GetNextSchedule(TaskDescriptor task)
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

        #endregion

        #region History

        public virtual IQueryable<TaskExecutionInfo> GetExecutionInfoQuery(bool tracked = true)
        {
            return Db.TaskExecutionInfos.ApplyTracking(tracked);
        }

        public virtual TaskExecutionInfo CreateExecutionInfo(TaskDescriptor task)
        {
            Guard.NotNull(task);

            return new TaskExecutionInfo
            {
                TaskDescriptorId = task.Id,
                IsRunning = true,
                MachineName = _appContext.RuntimeInfo.MachineName.EmptyNull(),
                StartedOnUtc = DateTime.UtcNow
            };
        }

        public virtual Task<TaskExecutionInfo> GetExecutionInfoByIdAsync(int id)
        {
            if (id == 0)
            {
                return Task.FromResult<TaskExecutionInfo>(null);
            }

            return Db.TaskExecutionInfos.Include(x => x.Task).FindByIdAsync(id).AsTask();
        }

        public virtual Task<TaskExecutionInfo> GetLastExecutionInfoByTaskIdAsync(int taskId, bool? runningOnly = null)
        {
            if (taskId == 0)
            {
                return Task.FromResult<TaskExecutionInfo>(null);
            }

            var query = GetExecutionInfoQuery()
                .Include(x => x.Task)
                .ApplyTaskFilter(taskId, false)
                .ApplyCurrentMachineNameFilter();

            if (runningOnly.HasValue)
            {
                query = query.Where(x => x.IsRunning == runningOnly.Value);
            }

            return ExecuteWithRetry(() => query.FirstOrDefaultAsync());
        }

        public virtual async Task<TaskExecutionInfo> GetLastExecutionInfoByTaskAsync(TaskDescriptor task, bool? runningOnly = null)
        {
            Guard.NotNull(task);

            var query = Db.IsCollectionLoaded(task, x => x.ExecutionHistory)
                ? task.ExecutionHistory.AsQueryable()
                : GetExecutionInfoQuery().Include(x => x.Task).ApplyTaskFilter(task.Id);

            if (runningOnly.HasValue)
            {
                query = query.Where(x => x.IsRunning == runningOnly.Value);
            }

            query = query.ApplyCurrentMachineNameFilter();

            if (query is IAsyncQueryProvider)
            {
                return await ExecuteWithRetry(() => query.FirstOrDefaultAsync());
            }
            else
            {
                return query.FirstOrDefault();
            }
        }

        public virtual Task InsertExecutionInfoAsync(TaskExecutionInfo info)
        {
            Guard.NotNull(info);

            Db.TaskExecutionInfos.Add(info);
            return Db.SaveChangesAsync();
        }

        public virtual Task UpdateExecutionInfoAsync(TaskExecutionInfo info)
        {
            Guard.NotNull(info);

            try
            {
                Db.TryUpdate(info);
                return Db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                // Do not throw.
                return Task.CompletedTask;
            }
        }

        public virtual Task DeleteExecutionInfoAsync(TaskExecutionInfo info)
        {
            Guard.NotNull(info);
            Guard.IsTrue(!info.IsRunning, nameof(info.IsRunning), "Cannot delete a running task execution info entry.");

            Db.TaskExecutionInfos.Remove(info);
            return Db.SaveChangesAsync();
        }

        public virtual async Task<int> DeleteExecutionInfosByIdsAsync(IEnumerable<int> ids)
        {
            if (ids?.Any() ?? false)
            {
                return await GetExecutionInfoQuery()
                    .Where(x => ids.Contains(x.Id) && !x.IsRunning)
                    .ExecuteDeleteAsync();
            }

            return 0;
        }

        public virtual async Task<int> TrimExecutionInfosAsync(CancellationToken cancelToken = default)
        {
            var idsToDelete = new HashSet<int>();

            if (_commonSettings.Value.MaxScheduleHistoryAgeInDays > 0)
            {
                var earliestDate = DateTime.UtcNow.AddDays(-1 * _commonSettings.Value.MaxScheduleHistoryAgeInDays);
                var ids = await Db.TaskExecutionInfos
                    .AsNoTracking()
                    .Where(x => x.StartedOnUtc <= earliestDate && !x.IsRunning)
                    .Select(x => x.Id)
                    .ToListAsync(cancelToken);

                idsToDelete.AddRange(ids);
            }

            // We have to group by task otherwise we would only keep entries from very frequently executed tasks.
            if (_commonSettings.Value.MaxNumberOfScheduleHistoryEntries > 0)
            {
                // INFO: GroupBy the way it was before throws exception!
                var query = Db.TaskDescriptors
                    .AsNoTracking()
                    .Select(x => new
                    {
                        x.Id,
                        DeletableInfoIds = x.ExecutionHistory
                            .OrderByDescending(x => x.StartedOnUtc)
                            .ThenByDescending(x => x.Id)
                            .Skip(_commonSettings.Value.MaxNumberOfScheduleHistoryEntries)
                            .Select(x => x.Id)
                    })
                    .Where(x => x.DeletableInfoIds.Any());

                var ids = (await query.ToListAsync(cancelToken)).SelectMany(x => x.DeletableInfoIds);
                idsToDelete.AddRange(ids);
            }

            if (!idsToDelete.Any())
            {
                return 0;
            }

            var numDeleted = 0;

            try
            {
                using (var scope = new DbContextScope(Db, retainConnection: true))
                {
                    foreach (var batch in idsToDelete.Chunk(128))
                    {
                        if (!cancelToken.IsCancellationRequested)
                        {
                            numDeleted += await Db.TaskExecutionInfos
                                .Where(x => batch.Contains(x.Id))
                                .ExecuteDeleteAsync(cancelToken);
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
