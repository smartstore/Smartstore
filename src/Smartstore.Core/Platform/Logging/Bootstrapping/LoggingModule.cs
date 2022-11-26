using System.Reflection;
using Autofac;
using Autofac.Core;
using Autofac.Core.Activators.Reflection;
using Autofac.Core.Registration;
using Smartstore.ComponentModel;
using Smartstore.Core.Logging;
using Smartstore.Data;

namespace Smartstore.Core.Bootstrapping
{
    internal class LoggingModule : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<DbLogService>().As<IDbLogService>().InstancePerLifetimeScope();
            builder.RegisterType<ActivityLogger>().As<IActivityLogger>().InstancePerLifetimeScope();
            builder.RegisterType<Notifier>().As<INotifier>().InstancePerLifetimeScope();

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
            //// Ignore components that provide loggers (and thus avoid a circular dependency below)
            //if (registration.Services.OfType<TypedService>().Any(ts => ts.ServiceType == typeof(ILogger)))
            //    return;

            bool hasCtorLogger = false;
            bool hasPropertyLogger = false;

            PropertyInfo[] loggerProperties = null;

            var ra = registration.Activator as ReflectionActivator;
            if (ra != null)
            {
                // // Look for ctor parameters of type "ILogger" 
                var ctors = GetConstructorsSafe(ra);
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
                    .Where(x => x.IndexParameters.Length == 0) // must not be an indexer
                    .Where(x => x.Accessors.Length != 1 || x.Accessors[0].ReturnType == typeof(void)) //must have get/set, or only set
                    .Select(x => x.PropertyInfo)
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
                // Add our middleware to the pipeline.
                pipeline.Use(new AutofacSerilogMiddleware(registration.Activator.LimitType, hasCtorLogger, hasPropertyLogger));
            };
        }

        static ConstructorInfo[] GetConstructorsSafe(ReflectionActivator ra)
        {
            // As of Autofac v4.7.0 "FindConstructors" will throw "NoConstructorsFoundException" instead of returning an empty array
            // See: https://github.com/autofac/Autofac/pull/895 & https://github.com/autofac/Autofac/issues/733
            ConstructorInfo[] ctors;
            try
            {
                ctors = ra.ConstructorFinder.FindConstructors(ra.LimitType);
            }
            catch (NoConstructorsFoundException)
            {
                ctors = Array.Empty<ConstructorInfo>();
            }

            return ctors;
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