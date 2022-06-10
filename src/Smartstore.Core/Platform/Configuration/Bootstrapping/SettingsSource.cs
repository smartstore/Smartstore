using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Autofac;
using Autofac.Builder;
using Autofac.Core;
using Smartstore.Core.Configuration;
using Smartstore.Core.Stores;
using Smartstore.Data;

namespace Smartstore.Core.Bootstrapping
{
    internal class SettingsSource : IRegistrationSource
    {
        static readonly MethodInfo BuildMethod = typeof(SettingsSource).GetMethod(
            "BuildRegistration",
            BindingFlags.Static | BindingFlags.NonPublic);

        public IEnumerable<IComponentRegistration> RegistrationsFor(Service service, Func<Service, IEnumerable<ServiceRegistration>> registrations)
        {
            if (service is TypedService ts && typeof(ISettings).IsAssignableFrom(ts.ServiceType))
            {
                var buildMethod = BuildMethod.MakeGenericMethod(ts.ServiceType);
                yield return (IComponentRegistration)buildMethod.Invoke(null, null);
            }
        }

        [SuppressMessage("CodeQuality", "IDE0051", Justification = "Called by reflection")]
        static IComponentRegistration BuildRegistration<TSettings>() where TSettings : ISettings, new()
        {
            return RegistrationBuilder
                .ForDelegate((c, p) =>
                {
                    TSettings settings = default;

                    if (DataSettings.DatabaseIsInstalled())
                    {
                        int currentStoreId = c.ResolveOptional<IStoreContext>()?.CurrentStore?.Id ?? 0;
                        var settingFactory = c.ResolveOptional<ISettingFactory>();
                        if (settingFactory != null)
                        {
                            settings = settingFactory.LoadSettings<TSettings>(currentStoreId);
                        }
                    }

                    return settings ?? new TSettings();
                })
                //.InstancePerLifetimeScope()
                .ExternallyOwned()
                .CreateRegistration();
        }

        public bool IsAdapterForIndividualComponents => false;
    }
}
