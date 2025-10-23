using Autofac;
using Autofac.Builder;
using Humanizer;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Content.Media.Storage;
using Smartstore.Core.DataExchange;
using Smartstore.Core.DataExchange.Export;
using Smartstore.Core.Identity;
using Smartstore.Core.OutputCache;
using Smartstore.Core.AI;
using Smartstore.Core.Widgets;
using Smartstore.Data;
using Smartstore.Engine.Modularity;
using Microsoft.Extensions.FileProviders;
using Smartstore.Core.Theming;
using Smartstore.Core.Security;

namespace Smartstore.Core.Bootstrapping
{
    public sealed class ModularityModule : Module
    {
        private readonly IApplicationContext _appContext;

        public ModularityModule(IApplicationContext appContext)
        {
            _appContext = appContext;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<StoreModuleConstraint>().As<IModuleConstraint>().InstancePerLifetimeScope();
            builder.RegisterType<ProviderManager>().As<IProviderManager>().InstancePerLifetimeScope();
            builder.RegisterType<ModuleManager>().AsSelf().InstancePerLifetimeScope();

            // Register all module entry types in service container
            foreach (var descriptor in _appContext.ModuleCatalog.GetInstalledModules())
            {
                if (descriptor.Module.ModuleType == null)
                {
                    continue;
                }

                builder.RegisterType(descriptor.Module.ModuleType)
                    .As<IModule>()
                    .As(descriptor.Module.ModuleType)
                    .OnActivated(x =>
                    {
                        if (!DataSettings.DatabaseIsInstalled())
                        {
                            x.Context.InjectProperties(x.Instance);
                        }
                        
                        if (x.Instance is ModuleBase moduleBase)
                        {
                            moduleBase.Services = x.Context.Resolve<ICommonServices>();
                        }
                    })
                    .InstancePerDependency();
            }

            // Register custom resolver for IModule (by system name)
            builder.Register<Func<string, IModule>>(c =>
            {
                var cc = c.Resolve<IComponentContext>();
                var catalog = cc.Resolve<IModuleCatalog>();
                return (systemName) =>
                {
                    Guard.NotEmpty(systemName);
                    var descriptor = catalog.GetModuleByName(systemName) ?? throw new InvalidOperationException($"Cannot resolve module instance for '{systemName}' because it does not exist.");
                    return cc.Resolve<Func<IModuleDescriptor, IModule>>().Invoke(descriptor);
                };
            });

            // Register custom resolver for IModule (by descriptor)
            builder.Register<Func<IModuleDescriptor, IModule>>(c =>
            {
                var cc = c.Resolve<IComponentContext>();
                return (descriptor) =>
                {
                    Guard.NotNull(descriptor);
                    if (!descriptor.IsInstalled())
                    {
                        throw new InvalidOperationException($"Cannot resolve module instance for '{descriptor.SystemName}' because it is not installed.");
                    }

                    var instance = (IModule)cc.Resolve(descriptor.Module.ModuleType);
                    instance.Descriptor = descriptor;
                    return instance;
                };
            });

            var providerTypes = _appContext.TypeScanner.FindTypes<IProvider>().ToList();

            foreach (var type in providerTypes)
            {
                var moduleDescriptor = _appContext.ModuleCatalog.GetModuleByAssembly(type.Assembly);
                var groupName = ProviderTypeToKnownGroupName(type);
                var systemName = GetSystemName(type, moduleDescriptor);
                var friendlyName = GetFriendlyName(type, moduleDescriptor);
                var displayOrder = GetDisplayOrder(type, moduleDescriptor);
                var dependentWidgets = GetDependentWidgets(type);
                var resPattern = (moduleDescriptor != null ? "Plugins" : "Providers") + ".{1}.{0}"; // e.g. Plugins.FriendlyName.MySystemName
                var settingPattern = (moduleDescriptor != null ? "Plugins" : "Providers") + ".{0}.{1}"; // e.g. Plugins.MySystemName.DisplayOrder
                var isConfigurable = typeof(IConfigurable).IsAssignableFrom(type);
                var isEditable = typeof(IUserEditable).IsAssignableFrom(type);
                var isHidden = GetIsHidden(type);
                var exportFeature = GetExportFeature(type);

                var registration = builder
                    .RegisterType(type)
                    .Named<IProvider>(systemName)
                    .Keyed<IProvider>(systemName)
                    .InstancePerAttributedLifetime()
                    .PropertiesAutowired(PropertyWiringOptions.None);

                registration.WithMetadata<ProviderMetadata>(m =>
                {
                    m.For(em => em.ModuleDescriptor, moduleDescriptor);
                    m.For(em => em.GroupName, groupName);
                    m.For(em => em.SystemName, systemName);
                    m.For(em => em.ImplType, type);
                    m.For(em => em.ResourceKeyPattern, resPattern);
                    m.For(em => em.SettingKeyPattern, settingPattern);
                    m.For(em => em.FriendlyName, friendlyName.Name);
                    m.For(em => em.Description, friendlyName.Description);
                    m.For(em => em.DisplayOrder, displayOrder);
                    m.For(em => em.DependentWidgets, dependentWidgets);
                    m.For(em => em.IsConfigurable, isConfigurable);
                    m.For(em => em.IsEditable, isEditable);
                    m.For(em => em.IsHidden, isHidden);
                    m.For(em => em.ExportFeatures, exportFeature);
                });

                // Register specific provider type.
                RegisterAsSpecificProvider<ITaxProvider>(type, systemName, registration);
                RegisterAsSpecificProvider<IExchangeRateProvider>(type, systemName, registration);
                RegisterAsSpecificProvider<IShippingRateComputationMethod>(type, systemName, registration);
                RegisterAsSpecificProvider<IActivatableWidget>(type, systemName, registration);
                RegisterAsSpecificProvider<IPaymentMethod>(type, systemName, registration);
                RegisterAsSpecificProvider<IExportProvider>(type, systemName, registration);
                RegisterAsSpecificProvider<IOutputCacheProvider>(type, systemName, registration);
                RegisterAsSpecificProvider<IMediaStorageProvider>(type, systemName, registration);
                RegisterAsSpecificProvider<IExternalAuthenticationMethod>(type, systemName, registration);
                RegisterAsSpecificProvider<IAIProvider>(type, systemName, registration);
                RegisterAsSpecificProvider<ICaptchaProvider>(type, systemName, registration);
            }

            // Configure & register multi file provider for static assets
            var assetFileProvider = new AssetFileProvider(_appContext.WebRoot);

            assetFileProvider.AddSegmentedFileProvider("themes/", ResolveThemeFileProvider);
            assetFileProvider.AddSegmentedFileProvider("modules/", ResolveModuleFileProvider);

            builder.RegisterInstance<IAssetFileProvider>(assetFileProvider);
        }

        #region Helpers

        private static void RegisterAsSpecificProvider<T>(Type implType, string systemName, IRegistrationBuilder<object, ConcreteReflectionActivatorData, SingleRegistrationStyle> registration) where T : IProvider
        {
            if (typeof(T).IsAssignableFrom(implType))
            {
                try
                {
                    registration.As<T>().Named<T>(systemName);
                    registration.WithMetadata<ProviderMetadata>(m =>
                    {
                        m.For(em => em.ProviderType, typeof(T));
                    });
                }
                catch
                {
                }
            }
        }

        private static string GetSystemName(Type type, IModuleDescriptor descriptor)
        {
            if (type.TryGetAttribute<SystemNameAttribute>(false, out var attr))
            {
                return attr.Name;
            }

            if (typeof(IModule).IsAssignableFrom(type) && descriptor != null)
            {
                return descriptor.SystemName;
            }

            return type.FullName;
            //throw Error.Application("The 'SystemNameAttribute' must be applied to a provider type if the provider does not implement 'IModule' (provider type: {0}, plugin: {1})".FormatInvariant(type.FullName, descriptor != null ? descriptor.SystemName : "-"));
        }

        private static int GetDisplayOrder(Type type, IModuleDescriptor descriptor)
        {
            if (type.TryGetAttribute<OrderAttribute>(false, out var attr))
            {
                return attr.Order;
            }

            if (typeof(IModule).IsAssignableFrom(type) && descriptor != null)
            {
                return descriptor.Order;
            }

            return 0;
        }

        private static bool GetIsHidden(Type type)
        {
            if (type.TryGetAttribute<IsHiddenAttribute>(false, out var attr))
            {
                return attr.IsHidden;
            }

            return false;
        }

        private static ExportFeatures GetExportFeature(Type type)
        {
            if (type.TryGetAttribute<ExportFeaturesAttribute>(false, out var attr))
            {
                return attr.Features;
            }

            return ExportFeatures.None;
        }

        private static (string Name, string Description) GetFriendlyName(Type type, IModuleDescriptor descriptor)
        {
            string name = null;
            string description = name;

            if (type.TryGetAttribute<FriendlyNameAttribute>(false, out var attr))
            {
                name = attr.Name;
                description = attr.Description;
            }
            else if (typeof(IModule).IsAssignableFrom(type) && descriptor != null)
            {
                name = descriptor.FriendlyName;
                description = descriptor.Description;
            }
            else
            {
                name = type.Name.Titleize();
                //throw Error.Application("The 'FriendlyNameAttribute' must be applied to a provider type if the provider does not implement 'IPlugin' (provider type: {0}, plugin: {1})".FormatInvariant(type.FullName, descriptor != null ? descriptor.SystemName : "-"));
            }

            return (name, description);
        }

        private static string[] GetDependentWidgets(Type type)
        {
            if (!typeof(IActivatableWidget).IsAssignableFrom(type))
            {
                // don't let widgets depend on other widgets
                if (type.TryGetAttribute<DependentWidgetsAttribute>(false, out var attr))
                {
                    return attr.WidgetSystemNames;
                }
            }

            return [];
        }

        private static string ProviderTypeToKnownGroupName(Type implType)
        {
            if (typeof(ITaxProvider).IsAssignableFrom(implType))
            {
                return "Tax";
            }
            else if (typeof(IExchangeRateProvider).IsAssignableFrom(implType))
            {
                return "Payment";
            }
            else if (typeof(IShippingRateComputationMethod).IsAssignableFrom(implType))
            {
                return "Shipping";
            }
            else if (typeof(IPaymentMethod).IsAssignableFrom(implType))
            {
                return "Payment";
            }
            //else if (typeof(IExternalAuthenticationMethod).IsAssignableFrom(implType))
            //{
            //    return "Security";
            //}
            else if (typeof(IActivatableWidget).IsAssignableFrom(implType))
            {
                return "CMS";
            }
            else if (typeof(IExportProvider).IsAssignableFrom(implType))
            {
                return "Exporting";
            }
            else if (typeof(IOutputCacheProvider).IsAssignableFrom(implType))
            {
                return "OutputCache";
            }
            else if (typeof(IAIProvider).IsAssignableFrom(implType))
            {
                return "AI";
            }

            return null;
        }

        private static IFileProvider ResolveThemeFileProvider(string themeName, IApplicationContext appContext)
        {
            var themeRegistry = appContext.Services.Resolve<IThemeRegistry>();
            return themeRegistry?.GetThemeDescriptor(themeName)?.WebRoot;
        }

        private static IFileProvider ResolveModuleFileProvider(string moduleName, IApplicationContext appContext)
        {
            var module = appContext.ModuleCatalog.GetModuleByName(moduleName, false);
            if (module != null)
            {
                // Don't allow theme companion modules serving static files by "/modules" path
                return module.Theme.IsEmpty() ? module.WebRoot : null;
            }

            return null;
        }

        #endregion
    }
}
