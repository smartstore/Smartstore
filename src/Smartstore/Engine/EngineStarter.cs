using System.Net;
using System.Reflection;
using Autofac;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Smartstore.Bootstrapping;
using Smartstore.Collections;
using Smartstore.ComponentModel;
using Smartstore.Data;
using Smartstore.Diagnostics;
using Smartstore.Engine.Builders;
using Smartstore.Engine.Modularity;
using Smartstore.Engine.Runtimes;
using Smartstore.IO;
using Smartstore.Pdf;

namespace Smartstore.Engine
{
    public abstract class EngineStarter<TEngine> : Disposable, IEngineStarter
        where TEngine : IEngine
    {
        private TEngine _engine;
        private IApplicationContext _appContext;
        private ModuleLoader _moduleLoader;
        private IStarter[] _starters;

        protected EngineStarter(TEngine engine)
        {
            _engine = engine;
            _appContext = engine.Application;
            _moduleLoader = new ModuleLoader(_appContext);

            AppConfiguration = _appContext.AppConfiguration;

            LoadModules();

            _starters = SortStarters(DiscoverStarters()).ToArray();
        }

        protected TEngine Engine => _engine;
        protected IStarter[] Starters => _starters;

        public SmartConfiguration AppConfiguration { get; protected set; }

        protected abstract IEnumerable<Assembly> ResolveCoreAssemblies();

        /// <summary>
        /// Discovers all deployed modules without loading their assemblies.
        /// </summary>
        /// <returns>All valid modules.</returns>
        protected virtual IEnumerable<IModuleDescriptor> DiscoverModules()
        {
            return Enumerable.Empty<IModuleDescriptor>();
        }

        protected virtual IEnumerable<IStarter> DiscoverStarters()
        {
            return _appContext.TypeScanner.FindTypes<IStarter>()
                .Select(t => (IStarter)Activator.CreateInstance(t))
                .Where(x => x.Matches(_appContext));
        }

        private void LoadModules()
        {
            // Create temporary type scanner
            var coreAssemblies = ResolveCoreAssemblies().ToArray();
            _appContext.TypeScanner = new DefaultTypeScanner(coreAssemblies);

            var modules = DiscoverModules();
            var appIsInstalled = _appContext.IsInstalled;
            var loadedModules = ModularState.Instance.InstalledModules;
            var pendingModules = ModularState.Instance.PendingModules;

            foreach (var module in modules)
            {
                if (!appIsInstalled || loadedModules.Contains(module.Name) || pendingModules.Contains(module.Name))
                {
                    LoadModule(module);
                }
            }

            // Provide module catalog
            _appContext.ModuleCatalog = new ModuleCatalog(modules);

            // Provide type scanner which also can reflect over module assemblies
            _appContext.TypeScanner = new DefaultTypeScanner(coreAssemblies, _appContext.ModuleCatalog, _appContext.Logger);
        }

        /// <summary>
        /// Loads a module's assembly.
        /// </summary>
        protected virtual void LoadModule(IModuleDescriptor descriptor)
        {
            try
            {
                _moduleLoader.LoadModule(descriptor as ModuleDescriptor);
            }
            catch (Exception ex)
            {
                _appContext.Logger.Error(ex, "Failed to load module '{0}'.", descriptor.Name);
            }
        }

        public virtual void ConfigureServices(IServiceCollection services)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            var app = _engine.Application;

            services.AddOptions();
            services.AddSingleton(app.AppConfiguration);
            services.AddSingleton(app.ModuleCatalog);
            services.AddSingleton(app.TypeScanner);
            services.AddSingleton(app.OSIdentity);
            services.AddSingleton<IEngine>(_engine);
            services.AddSingleton(app);

            if (DataSettings.Instance.DbFactory != null)
            {
                services.AddSingleton(DataSettings.Instance.DbFactory);
            }

            // Bind the config to host options
            services.Configure<HostOptions>(app.Configuration.GetSection("HostOptions"));

            // Add Async/Threading stuff
            services.AddAsyncRunner();
            services.AddLockFileManager();
            services.AddDistributedSemaphoreLockProvider();

            services.AddSingleton<INativeLibraryManager, NativeLibraryManager>();
            services.AddSingleton(x => NullChronometer.Instance);
            services.AddSingleton<IJsonSerializer, NewtonsoftJsonSerializer>();
            services.AddSingleton<IFilePermissionChecker, FilePermissionChecker>();
            services.AddSingleton<ILifetimeScopeAccessor, DefaultLifetimeScopeAccessor>();
            services.AddSingleton<IPdfConverter, NullPdfConverter>();
            services.AddScoped<IDisplayHelper, DefaultDisplayHelper>();
            services.AddHttpContextAccessor();

            services.AddMemoryCache(o =>
            {
                o.SizeLimit = app.AppConfiguration.MemoryCacheSizeLimit;

                if (app.AppConfiguration.MemoryCacheExpirationScanFrequency.HasValue)
                {
                    o.ExpirationScanFrequency = app.AppConfiguration.MemoryCacheExpirationScanFrequency.Value;
                }
            });

            services.AddMailKitMailService();
            services.AddTemplateEngine();

            // Configure all modular services
            foreach (var starter in _starters)
            {
                // Call modular service configurers
                starter.ConfigureServices(services, _appContext);
            }
        }

        public virtual void ConfigureContainer(ContainerBuilder builder)
        {
            builder.RegisterGeneric(typeof(Work<>)).SingleInstance();

            builder.RegisterModule(new CachingModule());
            builder.RegisterModule(new EventsModule(_appContext));

            // Configure all modular services by Autofac
            foreach (var starter in _starters.OfType<IContainerConfigurer>())
            {
                starter.ConfigureContainer(builder, _appContext);
            }
        }

        public virtual void ConfigureApplication(IApplicationBuilder app)
        {
            // Do nothing here
        }

        private static IEnumerable<IStarter> SortStarters(IEnumerable<IStarter> starters)
        {
            return starters
                .GroupBy(x => x.Order)
                .OrderBy(x => x.Key)
                .SelectMany(x => x.ToArray().SortTopological(StringComparer.OrdinalIgnoreCase))
                .Cast<IStarter>();
        }

        protected override void OnDispose(bool disposing)
        {
            if (disposing)
            {
                _engine.IsStarted = true;

                _engine = default;
                _appContext = null;
                _starters = null;
            }
        }
    }
}
