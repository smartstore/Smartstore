using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Engine;
using Smartstore.Threading;
using Smartstore.Utilities;

namespace Smartstore.Scheduling
{
    public class DefaultTaskExecutor : ITaskExecutor
    {
        private readonly ITaskStore _taskStore;
        private readonly Func<Type, ITask> _taskResolver;
        private readonly IComponentContext _componentContext;
        private readonly IAsyncState _asyncState;
        private readonly AsyncRunner _asyncRunner;
        private readonly IApplicationContext _appContext;

        public const string CurrentCustomerIdParamName = "CurrentCustomerId";
        public const string CurrentStoreIdParamName = "CurrentStoreId";

        public DefaultTaskExecutor(
            ITaskStore taskStore,
            Func<Type, ITask> taskResolver,
            IComponentContext componentContext,
            IAsyncState asyncState,
            AsyncRunner asyncRunner,
            IApplicationContext appContext)
        {
            _taskStore = taskStore;
            _taskResolver = taskResolver;
            _componentContext = componentContext;
            _asyncState = asyncState;
            _asyncRunner = asyncRunner;
            _appContext = appContext;

            Logger = NullLogger.Instance;
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        public virtual async Task ExecuteAsync(
            ITaskDescriptor task, 
            IDictionary<string, string> taskParameters = null,
            bool throwOnError = false,
            CancellationToken cancelToken = default)
        {
            Guard.NotNull(task, nameof(task));

            if (_asyncRunner.AppShutdownCancellationToken.IsCancellationRequested)
            {
                return;
            }

            await _taskStore.LoadLastExecutionInfoAsync(task);

            if (task?.LastExecution?.IsRunning == true)
            {
                return;
            }

            bool faulted = false;
            bool canceled = false;
            string lastError = null;
            ITask job = null;
            string stateName = null;
            Type taskType = null;
            Exception exception = null;

            var executionInfo = _taskStore.CreateExecutionInfo(task);

            try
            {
                taskType = _taskStore.GetTaskClrType(task);
                if (taskType == null)
                {
                    Logger.Debug($"Invalid scheduled task type: {task.Type.NaIfEmpty()}");
                    return;
                }

                if (!_appContext.ModuleCatalog.IsActiveModuleAssembly(taskType.Assembly))
                {
                    return;
                }

                await _taskStore.InsertExecutionInfoAsync(executionInfo);
            }
            catch
            {
                // Get out on any initialization error.
                return;
            }

            try
            {
                // Task history entry has been successfully added, now we execute the task.
                // Create task instance.
                job = _taskResolver(taskType);
                stateName = task.Id.ToString();

                // Create & set a composite CancellationTokenSource which also contains the global app shoutdown token.
                var cts = CancellationTokenSource.CreateLinkedTokenSource(_asyncRunner.AppShutdownCancellationToken, cancelToken);
                await _asyncState.CreateAsync(task, stateName, false, cts);

                // Run the task
                Logger.Debug("Executing scheduled task: {0}", task.Type);
                var ctx = new TaskExecutionContext(_taskStore, _componentContext, executionInfo, taskParameters);
                await job.Run(ctx, cts.Token);
            }
            catch (Exception ex)
            {
                exception = ex;
                faulted = true;
                canceled = ex is OperationCanceledException;
                lastError = ex.ToAllMessages(true);

                if (canceled)
                {
                    Logger.Warn(ex, $"The scheduled task '{task.Name}' has been canceled.");
                }
                else
                {
                    Logger.Error(ex, string.Concat($"Error while running scheduled task '{task.Name}'", ": ", ex.Message));
                }
            }
            finally
            {
                var now = DateTime.UtcNow;
                var updateTask = false;

                executionInfo.IsRunning = false;
                executionInfo.ProgressPercent = null;
                executionInfo.ProgressMessage = null;
                executionInfo.Error = lastError;
                executionInfo.FinishedOnUtc = now;

                if (faulted)
                {
                    if ((!canceled && task.StopOnError) || task == null)
                    {
                        task.Enabled = false;
                        updateTask = true;
                    }
                }
                else
                {
                    executionInfo.SucceededOnUtc = now;
                }

                try
                {
                    Logger.Debug("Executed scheduled task: {0}. Elapsed: {1} ms.", task.Type, (now - executionInfo.StartedOnUtc).TotalMilliseconds);

                    // Remove from AsyncState.
                    if (stateName.HasValue())
                    {
                        // We don't just remove the cancellation token, but the whole state (along with the token)
                        // for the case that a state was registered during task execution.
                        await _asyncState.RemoveAsync<ITaskDescriptor>(stateName);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }

                if (task.Enabled)
                {
                    task.NextRunUtc = _taskStore.GetNextSchedule(task);
                    updateTask = true;
                }

                await _taskStore.UpdateExecutionInfoAsync(executionInfo);

                if (updateTask)
                {
                    await _taskStore.UpdateTaskAsync(task);
                }

                await Throttle.CheckAsync(
                    "Delete old schedule task history entries",
                    TimeSpan.FromDays(1),
                    async () => await _taskStore.TrimExecutionInfosAsync() > 0);
            }

            if (throwOnError && exception != null)
            {
                throw exception;
            }
        }
    }
}
