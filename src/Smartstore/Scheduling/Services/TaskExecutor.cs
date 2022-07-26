using Autofac;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Threading;
using Smartstore.Utilities;

namespace Smartstore.Scheduling
{
    public class TaskExecutor : ITaskExecutor
    {
        private readonly ITaskStore _taskStore;
        private readonly ITaskActivator _taskActivator;
        private readonly IComponentContext _componentContext;
        private readonly IAsyncState _asyncState;
        private readonly AsyncRunner _asyncRunner;

        public const string CurrentCustomerIdParamName = "CurrentCustomerId";
        public const string CurrentStoreIdParamName = "CurrentStoreId";

        public TaskExecutor(
            ITaskStore taskStore,
            ITaskActivator taskActivator,
            IComponentContext componentContext,
            IAsyncState asyncState,
            AsyncRunner asyncRunner)
        {
            _taskStore = taskStore;
            _taskActivator = taskActivator;
            _componentContext = componentContext;
            _asyncState = asyncState;
            _asyncRunner = asyncRunner;
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        public virtual async Task ExecuteAsync(
            TaskDescriptor task,
            HttpContext httpContext,
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

            if (task.LastExecution?.IsRunning == true)
            {
                return;
            }

            ITask job = null;
            Type taskType = null;
            Exception exception = null;
            bool faulted = false, canceled = false;
            string lastError = null, stateName = null, normalizedTypeName = null;

            var executionInfo = _taskStore.CreateExecutionInfo(task);

            try
            {
                normalizedTypeName = _taskActivator.GetNormalizedTypeName(task);
                taskType = _taskActivator.GetTaskClrType(normalizedTypeName, true);

                await _taskStore.InsertExecutionInfoAsync(executionInfo);
            }
            catch (TaskActivationException)
            {
                // Disable task
                task.Enabled = false;
                await _taskStore.UpdateTaskAsync(task);
                return;
            }
            catch (Exception)
            {
                // Get out on any other initialization error.
                return;
            }

            try
            {
                // Task history entry has been successfully added, now we execute the task.
                // Create task instance.
                job = _taskActivator.Activate(normalizedTypeName);
                stateName = task.Id.ToString();

                // Create & set a composite CancellationTokenSource which also contains the global app shoutdown token.
                var cts = CancellationTokenSource.CreateLinkedTokenSource(_asyncRunner.AppShutdownCancellationToken, cancelToken);
                await _asyncState.CreateAsync(task, stateName, false, cts);

                // Run the task
                Logger.Debug("Executing scheduled task: {0}", task.Type);
                var ctx = new TaskExecutionContext(_taskStore, httpContext, _componentContext, executionInfo, taskParameters);

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
                    if ((!canceled && task.StopOnError) || job == null)
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
                        await _asyncState.RemoveAsync<TaskDescriptor>(stateName);
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

                if (!canceled)
                {
                    await Throttle.CheckAsync(
                        "Delete old schedule task history entries",
                        TimeSpan.FromHours(4),
                        async () => await _taskStore.TrimExecutionInfosAsync(cancelToken) > 0);
                }
            }

            if (throwOnError && exception != null)
            {
                exception.ReThrow();
            }
        }
    }
}
