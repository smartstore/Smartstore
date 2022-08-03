using Microsoft.AspNetCore.Http;
using Smartstore.Engine.Initialization;
using Smartstore.Scheduling;

namespace Smartstore.Core.Bootstrapping
{
    internal class TaskSchedulerInitializer : IApplicationInitializer
    {
        private readonly SmartConfiguration _appConfig;

        public TaskSchedulerInitializer(SmartConfiguration appConfig)
        {
            _appConfig = appConfig;
        }

        public int Order => int.MinValue + 20;
        public int MaxAttempts => 10;
        public bool ThrowOnError => false;

        public ILogger Logger { get; set; } = NullLogger.Instance;

        public async Task InitializeAsync(HttpContext httpContext)
        {
            var scheduler = httpContext.RequestServices.GetService<ITaskScheduler>();
            var taskStore = httpContext.RequestServices.GetService<ITaskStore>();
            if (scheduler == null || taskStore == null)
            {
                // No scheduler or store registered. Get out!
                Logger.Warn("Task scheduler has not been registered.");
                return;
            }

            var tasks = await taskStore.GetAllTasksAsync(true, true);
            await taskStore.CalculateFutureSchedulesAsync(tasks, true /* isAppStart */);

            scheduler.Activate(
                _appConfig.TaskSchedulerBaseUrl,
                _appConfig.TaskSchedulerPollInterval,
                httpContext);

            Logger.Info($"Initialized TaskScheduler with base url '{scheduler.BaseUrl}'");
        }

        public Task OnFailAsync(Exception exception, bool willRetry)
        {
            if (willRetry)
            {
                Logger.Error(exception, "Error while initializing Task Scheduler");
            }
            else
            {
                Logger.Warn("Stopped trying to initialize the Task Scheduler: too many failed consecutive attempts (10+). Maybe setting 'Smartstore.TaskSchedulerBaseUrl' to a valid base url in appsettings.json solves the problem?");
            }

            return Task.CompletedTask;
        }
    }
}
