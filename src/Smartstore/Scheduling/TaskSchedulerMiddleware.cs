using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Smartstore.Scheduling
{
    public class TaskSchedulerMiddleware
    {
        internal const string PollAction = "poll";
        internal const string RunAction = "run";
        internal const string NoopAction = "noop";

        private readonly ITaskScheduler _scheduler;

        public TaskSchedulerMiddleware(RequestDelegate next, ITaskScheduler scheduler)
        {
            _scheduler = scheduler;
        }

        public async Task Invoke(HttpContext context, ITaskStore taskStore, ITaskExecutor executor)
        {
            var urlSegments = context.Request.Path.Value.Trim('/').SplitSafe('/').ToArray();
            var action = urlSegments.Length > 1 ? urlSegments[1] : string.Empty;

            if (action == PollAction || action == RunAction)
            {
                if (!context.Request.IsPost())
                {
                    context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                    return;
                }

                if (!await _scheduler.VerifyAuthTokenAsync(context.Request.Headers[DefaultTaskScheduler.AuthTokenName]))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return;
                }

                var taskParameters = context.Request.Query.ToDictionary(x => x.Key, x => x.Value.ToString(), StringComparer.OrdinalIgnoreCase);

                if (action == RunAction)
                {
                    var taskId = urlSegments.Length > 2 ? urlSegments[2].Convert(0) : 0;
                    await Run(taskId, context, taskStore, executor, taskParameters);
                }
                else
                {
                    await Poll(context, taskStore, executor, taskParameters);
                }
            }
            else
            {
                await Noop(context);
            }
        }

        private static async Task Poll(HttpContext context, ITaskStore taskStore, ITaskExecutor executor, IDictionary<string, string> taskParameters)
        {
            var pendingTasks = await taskStore.GetPendingTasksAsync();
            var numTasks = pendingTasks.Count;
            var numExecuted = 0;

            if (numTasks > 0)
            {
                await Virtualize(context, taskParameters);
            }

            for (var i = 0; i < numTasks; i++)
            {
                var task = pendingTasks[i];

                if (i > 0 /*&& (DateTime.UtcNow - _sweepStart).TotalMinutes > _taskScheduler.SweepIntervalMinutes*/)
                {
                    // Maybe a subsequent Sweep call or another machine in a webfarm executed 
                    // successive tasks already.
                    // To be able to determine this, we need to reload the entity from the database.
                    // The TaskExecutor will exit when the task should be in running state then.
                    await taskStore.ReloadTaskAsync(task);
                    task.LastExecution = await taskStore.GetLastExecutionInfoByTaskIdAsync(task.Id);
                }

                if (task.IsPending)
                {
                    await executor.ExecuteAsync(task, context, taskParameters);
                    numExecuted++;
                }
            }

            context.Response.StatusCode = StatusCodes.Status200OK;
            await context.Response.WriteAsync($"{numExecuted} of {numTasks} pending tasks executed.");
        }

        private async Task Run(int taskId, HttpContext context, ITaskStore taskStore, ITaskExecutor executor, IDictionary<string, string> taskParameters)
        {
            var task = await taskStore.GetTaskByIdAsync(taskId);
            if (task == null)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            await Virtualize(context, taskParameters);

            await executor.ExecuteAsync(task, context, taskParameters);

            context.Response.StatusCode = StatusCodes.Status200OK;
            await context.Response.WriteAsync($"Task '{task.Name}' executed.");
        }

        private static Task Noop(HttpContext context)
        {
            context.Response.StatusCode = StatusCodes.Status200OK;
            return context.Response.WriteAsync("noop");
        }

        private static Task Virtualize(HttpContext context, IDictionary<string, string> taskParameters)
        {
            var virtualizer = context.RequestServices.GetService<ITaskContextVirtualizer>();
            if (virtualizer != null)
            {
                return virtualizer.VirtualizeAsync(context, taskParameters);
            }

            return Task.CompletedTask;
        }
    }
}
