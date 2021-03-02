using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Engine;
using Smartstore.Engine.Initialization;
using Smartstore.Events;
using Smartstore.Scheduling;

namespace Smartstore.Core.Bootstrapping
{
    public class TaskSchedulerInitializedEvent
    {
        public IEnumerable<ITaskDescriptor> Tasks { get; init; }
    }

    internal class TaskSchedulerInitializer : IApplicationInitializer
    {
        private readonly SmartConfiguration _appConfig;
        private readonly IEventPublisher _eventPublisher;

        public TaskSchedulerInitializer(SmartConfiguration appConfig, IEventPublisher eventPublisher)
        {
            _appConfig = appConfig;
            _eventPublisher = eventPublisher;
        }

        public int Order => int.MinValue;
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

            var tasks = await taskStore.GetAllTasksAsync(true);
            await taskStore.CalculateFutureSchedulesAsync(tasks, true /* isAppStart */);

            scheduler.Activate(
                _appConfig.TaskSchedulerBaseUrl, 
                _appConfig.TaskSchedulerPollInterval, 
                httpContext);

            Logger.Info("Initialized TaskScheduler with base url '{0}'".FormatInvariant(scheduler.BaseUrl));

            await _eventPublisher.PublishAsync(new TaskSchedulerInitializedEvent { Tasks = tasks });
        }

        public Task OnFailAsync(Exception exception, bool willRetry)
        {
            if (willRetry)
            {
                Logger.Error(exception, "Error while initializing Task Scheduler");
            }
            else
            {
                Logger.Warn("Stopped trying to initialize the Task Scheduler: too many failed attempts in succession (10+). Maybe setting 'Smartstore.TaskSchedulerBaseUrl' to a valid base url in appsettings.json solves the problem?");
            }

            return Task.CompletedTask;
        }
    }
}
