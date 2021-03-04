using System;
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
            builder.RegisterType<DefaultTaskExecutor>().As<ITaskExecutor>().InstancePerLifetimeScope();

            // Register as hosted service
            builder.Register<IHostedService>(c => c.Resolve<ITaskScheduler>()).SingleInstance();

            // Register all ITask impls
            var taskTypes = _appContext.TypeScanner.FindTypes<ITask>(ignoreInactiveModules: true);

            foreach (var type in taskTypes)
            {
                var typeName = type.FullName;
                builder.RegisterType(type).Named<ITask>(typeName).Keyed<ITask>(type).InstancePerLifetimeScope();
            }

            // Register resolver delegates
            builder.Register<Func<Type, ITask>>(c =>
            {
                var cc = c.Resolve<IComponentContext>();
                return keyed => cc.ResolveKeyed<ITask>(keyed);
            });

            builder.Register<Func<string, ITask>>(c =>
            {
                var cc = c.Resolve<IComponentContext>();
                return named => cc.ResolveNamed<ITask>(named);
            });
        }
    }
}