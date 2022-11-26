using System.Reflection;
using Autofac;
using Autofac.Core;
using Autofac.Core.Registration;
using Autofac.Core.Resolving.Pipeline;
using Smartstore.ComponentModel;
using Smartstore.Core.Data;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Data;

namespace Smartstore.Core.Bootstrapping
{
    internal sealed class DbQuerySettingsModule : Autofac.Module
    {
        const string PropName = "Property.DbQuerySettings";

        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(c =>
            {
                var storeContext = c.Resolve<IStoreContext>();
                var aclService = c.Resolve<IAclService>();

                return new DbQuerySettings(
                    aclService != null && !aclService.HasActiveAcl(),
                    storeContext?.IsSingleStoreMode() ?? false);
            })
            .InstancePerLifetimeScope();
        }

        protected override void AttachToComponentRegistration(IComponentRegistryBuilder componentRegistry, IComponentRegistration registration)
        {
            if (!DataSettings.DatabaseIsInstalled())
            {
                return;
            }

            var querySettingsProperty = FindQuerySettingsProperty(registration.Activator.LimitType);

            if (querySettingsProperty == null)
                return;

            registration.Metadata.Add(PropName, querySettingsProperty);

            registration.PipelineBuilding += (sender, pipeline) =>
            {
                // Add our QuerySettings middleware to the pipeline.
                pipeline.Use(PipelinePhase.ParameterSelection, (context, next) =>
                {
                    next(context);

                    if (!context.NewInstanceActivated || context.Registration.Metadata.Get(PropName) is not PropertyInfo prop)
                    {
                        return;
                    }

                    var querySettings = context.Resolve<DbQuerySettings>();
                    prop.SetValue(context.Instance, querySettings);
                });
            };
        }

        private static PropertyInfo FindQuerySettingsProperty(Type type)
        {
            var prop = type.GetProperty("QuerySettings", typeof(DbQuerySettings));
            if (prop?.SetMethod == null)
            {
                return null;
            }

            return prop;
        }
    }
}
