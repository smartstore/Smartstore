using System.Reflection;
using Autofac;
using Autofac.Core;
using Autofac.Core.Registration;
using Autofac.Core.Resolving.Pipeline;
using Smartstore.Data;

namespace Smartstore.Core.Bootstrapping
{
    internal class CommonServicesModule : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<CommonServices>().As<ICommonServices>().InstancePerLifetimeScope();
        }

        protected override void AttachToComponentRegistration(IComponentRegistryBuilder componentRegistry, IComponentRegistration registration)
        {
            // Look for first settable property of type "ICommonServices" and inject
            var servicesProperty = FindCommonServicesProperty(registration.Activator.LimitType);

            if (servicesProperty == null)
                return;

            registration.Metadata.Add("Property.ICommonServices", servicesProperty);

            registration.PipelineBuilding += (sender, pipeline) =>
            {
                // Add our CommonServices middleware to the pipeline.
                pipeline.Use(PipelinePhase.ParameterSelection, (context, next) =>
                {
                    next(context);

                    if (!DataSettings.DatabaseIsInstalled())
                    {
                        return;
                    }

                    if (!context.NewInstanceActivated || context.Registration.Metadata.Get("Property.ICommonServices") is not PropertyInfo prop)
                    {
                        return;
                    }

                    if (context.TryResolve<ICommonServices>(out var services))
                    {
                        prop.SetValue(context.Instance, services);
                    }
                });
            };
        }

        private static PropertyInfo FindCommonServicesProperty(Type type)
        {
            var prop = type
                .GetProperties(BindingFlags.SetProperty | BindingFlags.Public | BindingFlags.Instance)
                .Select(p => new
                {
                    PropertyInfo = p,
                    p.PropertyType,
                    IndexParameters = p.GetIndexParameters(),
                    Accessors = p.GetAccessors(false)
                })
                .Where(x => x.PropertyType == typeof(ICommonServices)) // must be ICommonServices
                .Where(x => x.IndexParameters.Length == 0) // must not be an indexer
                .Where(x => x.Accessors.Length != 1 || x.Accessors[0].ReturnType == typeof(void)) //must have get/set, or only set
                .Select(x => x.PropertyInfo)
                .FirstOrDefault();

            return prop;
        }
    }
}
