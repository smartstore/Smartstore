using Autofac;
using Microsoft.Extensions.Hosting;
using Smartstore.Engine;
using Smartstore.Scheduling;

namespace Smartstore.Bootstrapping
{
    internal sealed class SchedulerModule : Module
    {
        private readonly IApplicationContext _appContext;

        public SchedulerModule(IApplicationContext appContext)
        {
            _appContext = appContext;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<DefaultTaskScheduler>().As<ITaskScheduler>().SingleInstance();
            builder.RegisterType<TaskExecutor>().As<ITaskExecutor>().InstancePerLifetimeScope();
            builder.RegisterType<TaskActivator>().As<ITaskActivator>().InstancePerLifetimeScope();

            // Register as hosted service
            builder.Register<IHostedService>(c => c.Resolve<ITaskScheduler>()).SingleInstance();

            // Register all ITask impls
            var taskTypes = _appContext.TypeScanner.FindTypes<ITask>();

            foreach (var taskType in taskTypes)
            {
                var typeName = taskType.GetAttribute<TaskNameAttribute>(true)?.Name ?? taskType.Name;

                var registration = builder.RegisterType(taskType)
                    .Named<ITask>(typeName)
                    .Keyed<ITask>(taskType)
                    .InstancePerAttributedLifetime()
                    .WithMetadata<TaskMetadata>(m =>
                    {
                        m.For(em => em.Name, typeName);
                        m.For(em => em.Type, taskType);
                    });
            }
        }
    }
}