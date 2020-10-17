using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Autofac.Core;
using Autofac.Core.Activators.Reflection;
using Autofac.Core.Registration;
using Autofac.Core.Resolving.Pipeline;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Smartstore.ComponentModel;
using Smartstore.Data;
using Smartstore.Engine;

namespace Smartstore.Core.Logging.DependencyInjection
{
    public sealed class LoggingStarter : StarterBase
    {
        public override int ApplicationOrder 
            => int.MinValue;

        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext, bool isActiveModule)
        {
            builder.RegisterModule(new LoggingModule());
        }

        public override void ConfigureApplication(IApplicationBuilder app, IApplicationContext appContext)
        {
            app.UseMiddleware<SerilogLocalContextMiddleware>();
        }
    }

    internal class LoggingModule : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // TODO: (core) Impl and register IChronometer (=> Diagnostics)
            //builder.RegisterType<NullChronometer>().As<IChronometer>().SingleInstance();

            // Call GetLogger in response to the request for an ILogger implementation
            if (DataSettings.DatabaseIsInstalled())
            {
                builder.Register(GetContextualLogger).As<ILogger>().ExternallyOwned();
            }
            else
            {
                // The install logger should append to a rolling text file only.
                builder.Register(GetInstallLogger).As<ILogger>().ExternallyOwned();
            }
        }

        protected override void AttachToComponentRegistration(IComponentRegistryBuilder componentRegistry, IComponentRegistration registration)
        {
            bool hasCtorLogger = false;
            bool hasPropertyLogger = false;

            FastProperty[] loggerProperties = null;

            var ra = registration.Activator as ReflectionActivator;
            if (ra != null)
            {
                // // Look for ctor parameters of type "ILogger" 
                var ctors = ra.ConstructorFinder.FindConstructors(ra.LimitType);
                var loggerParameters = ctors.SelectMany(ctor => ctor.GetParameters()).Where(pi => pi.ParameterType == typeof(ILogger));
                hasCtorLogger = loggerParameters.Any();

                // Autowire properties
                // Look for settable properties of type "ILogger" 
                loggerProperties = ra.LimitType
                    .GetProperties(BindingFlags.SetProperty | BindingFlags.Public | BindingFlags.Instance)
                    .Select(p => new
                    {
                        PropertyInfo = p,
                        p.PropertyType,
                        IndexParameters = p.GetIndexParameters(),
                        Accessors = p.GetAccessors(false)
                    })
                    .Where(x => x.PropertyType == typeof(ILogger)) // must be a logger
                    .Where(x => x.IndexParameters.Count() == 0) // must not be an indexer
                    .Where(x => x.Accessors.Length != 1 || x.Accessors[0].ReturnType == typeof(void)) //must have get/set, or only set
                    .Select(x => FastProperty.Create(x.PropertyInfo))
                    .ToArray();

                hasPropertyLogger = loggerProperties.Length > 0;

                // Ignore components known to be without logger dependencies
                if (!hasCtorLogger && !hasPropertyLogger)
                    return;

                if (hasPropertyLogger)
                {
                    registration.Metadata.Add("LoggerProperties", loggerProperties);
                }
            }

            registration.PipelineBuilding += (sender, pipeline) =>
            {
                //if (hasCtorLogger)
                //{
                //    pipeline.Use(PipelinePhase.ParameterSelection, (context, next) =>
                //    {
                //        var logger = GetLoggerFor(context.Registration.Activator.LimitType, context);
                //        //context.Parameters = new[] { TypedParameter.From(logger) }.Concat(args.Parameters);
                //        context.ChangeParameters(new[] { TypedParameter.From(logger) }.Concat(context.Parameters));

                //        // Call the next middleware in the pipeline.
                //        next(context);
                //    });
                //}

                if (hasPropertyLogger)
                {
                    pipeline.Use(PipelinePhase.Activation, (context, next) =>
                    {
                        // Call the next middleware in the pipeline.
                        next(context);

                        var logger = GetLoggerFor(context.Registration.Activator.LimitType, context);
                        var loggerProps = context.Registration.Metadata.Get("LoggerProperties") as FastProperty[];
                        if (loggerProps != null)
                        {
                            foreach (var prop in loggerProps)
                            {
                                prop.SetValue(context.Instance, logger);
                            }
                        }
                    });
                }

            };
        }

        private static ILogger GetLoggerFor(Type componentType, IComponentContext ctx)
        {
            return ctx.Resolve<ILogger>(new TypedParameter(typeof(Type), componentType));
        }

        private static ILogger GetContextualLogger(IComponentContext context, IEnumerable<Parameter> parameters)
        {
            // return an ILogger in response to Resolve<ILogger>(componentTypeParameter)
            var loggerFactory = context.Resolve<ILoggerFactory>();

            Type containingType = null;

            if (parameters != null && parameters.Any())
            {
                if (parameters.Any(x => x is TypedParameter))
                {
                    containingType = parameters.TypedAs<Type>();
                }
                else if (parameters.Any(x => x is NamedParameter))
                {
                    containingType = parameters.Named<Type>("Autofac.AutowiringPropertyInjector.InstanceType");
                }
            }

            if (containingType != null)
            {
                return loggerFactory.CreateLogger(containingType);
            }
            else
            {
                return loggerFactory.CreateLogger("SmartStore");
            }
        }

        private static ILogger GetInstallLogger(IComponentContext context)
        {
            return context.Resolve<ILoggerFactory>().CreateLogger("Install");
        }
    }
}