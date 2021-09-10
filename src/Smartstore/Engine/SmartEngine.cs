using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Loader;
using Autofac;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Smartstore.Bootstrapping;
using Smartstore.Collections;
using Smartstore.ComponentModel;
using Smartstore.Data;
using Smartstore.DependencyInjection;
using Smartstore.Diagnostics;
using Smartstore.Engine.Builders;
using Smartstore.Engine.Initialization;
using Smartstore.Engine.Modularity;
using Smartstore.IO;
using Smartstore.Pdf;

namespace Smartstore.Engine
{
    public class SmartEngine : IEngine
    {
        const string SmartstoreNamespace = "Smartstore.";

        private ModuleReferenceResolver _moduleReferenceResolver;
        private readonly static ConcurrentDictionary<Assembly, IModuleDescriptor> _assemblyModuleMap = new();
        
        public IApplicationContext Application { get; private set; }
        public ScopedServiceContainer Scope { get; set; }
        public bool IsStarted { get; private set; }
        public bool IsInitialized { get; private set; }

        public virtual IEngineStarter Start(IApplicationContext application)
        {
            Guard.NotNull(application, nameof(application));

            Application = application;

            // Set IsInitialized prop after init completes.
            ApplicationInitializerMiddleware.Initialized += (s, e) => IsInitialized = true;

            // Assembly resolver event.
            _moduleReferenceResolver = new ModuleReferenceResolver(application);
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;

            return new EngineStarter(this);
        }

        private Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            var assembly = _moduleReferenceResolver.ResolveAssembly(args.RequestingAssembly, args.Name);
            
            if (assembly == null)
            {
                // Check for assembly already loaded
                assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName == args.Name);
                if (assembly != null)
                {
                    return assembly;
                }
                    
                // Get assembly from TypeScanner
                assembly = Application.TypeScanner?.Assemblies?.FirstOrDefault(a => a.FullName == args.Name);
            }

            return assembly;
        }

        class EngineStarter : IEngineStarter
        {
            private SmartEngine _engine;
            private IApplicationContext _appContext;
            private ModuleExplorer _moduleExplorer;
            private ModuleLoader _moduleLoader;
            private IList<IStarter> _starters;

            public EngineStarter(SmartEngine engine)
            {
                _engine = engine;
                _appContext = engine.Application;
                _moduleExplorer = new ModuleExplorer(_appContext);
                _moduleLoader = new ModuleLoader(_appContext);

                LoadModules();

                _starters = _appContext.TypeScanner.FindTypes<IStarter>()
                    .Select(t => (IStarter)Activator.CreateInstance(t))
                    .Where(x => x.Matches(_appContext))
                    .ToList();
                _starters = SortStarters(_starters).ToList();
            }

            public SmartConfiguration AppConfiguration { get; private set; }

            private void LoadModules()
            {
                // Create temporary type scanner
                var coreAssemblies = ResolveCoreAssemblies().ToArray();
                _appContext.TypeScanner = new DefaultTypeScanner(coreAssemblies);

                var modules = DiscoverModules();
                var isInstalled = _appContext.IsInstalled;
                var installedModules = ModularState.Instance.InstalledModules;

                foreach (var module in modules)
                {
                    if (!isInstalled || installedModules.Contains(module.Name))
                    {
                        LoadModule(module);
                    }
                }

                // Provide module catalog
                _appContext.ModuleCatalog = new ModuleCatalog(modules);

                // Provide type scanner which also can reflect over module assemblies
                _appContext.TypeScanner = new DefaultTypeScanner(coreAssemblies, _appContext.ModuleCatalog, _appContext.Logger);
            }

            private IEnumerable<Assembly> ResolveCoreAssemblies()
            {
                var assemblies = new HashSet<Assembly>();

                var libs = DependencyContext.Default.CompileLibraries
                    .Where(x => x.Name.StartsWith(SmartstoreNamespace))
                    .Select(x => new CoreAssembly
                    {
                        Name = x.Name,
                        DependsOn = x.Dependencies
                            .Where(y => y.Name.StartsWith(SmartstoreNamespace))
                            .Where(y => !y.Name.StartsWith(SmartstoreNamespace + ".Data.")) // Exclude data provider projects
                            .Select(y => y.Name)
                            .ToArray()
                    })
                    .ToArray()
                    .SortTopological()
                    .Cast<CoreAssembly>()
                    .ToArray();

                foreach (var lib in libs)
                {
                    try
                    {
                        var assembly = AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName(lib.Name));
                        assemblies.Add(assembly);

                        _appContext.Logger.Debug("Assembly '{0}' discovered and loaded.", lib.Name);
                    }
                    catch (Exception ex)
                    {
                        _appContext.Logger.Error(ex);
                    }
                }

                return assemblies;
            }

            public IEnumerable<IModuleDescriptor> DiscoverModules()
            {
                return _moduleExplorer.DiscoverModules();
            }

            public void LoadModule(IModuleDescriptor descriptor)
            {
                _moduleLoader.LoadModule(descriptor as ModuleDescriptor);
            }

            public void ConfigureServices(IServiceCollection services)
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

                services.AddSingleton(x => NullChronometer.Instance);
                services.AddSingleton<IJsonSerializer, NewtonsoftJsonSerializer>();
                services.AddSingleton<IFilePermissionChecker, FilePermissionChecker>();
                services.AddSingleton<ILifetimeScopeAccessor, DefaultLifetimeScopeAccessor>();
                services.AddSingleton<IPdfConverter, NullPdfConverter>();
                services.AddHttpContextAccessor();
                services.AddScoped<IDisplayHelper, DefaultDisplayHelper>();

                // TODO: (core) Configuration for MemoryCache?
                services.AddMemoryCache();

                // TODO: (core) Register more system stuff
                services.AddMailKitMailService();
                services.AddTemplateEngine();

                // Add MVC core services
                var mvcBuilder = services.AddControllersWithViews();

                // Configure all modular services
                foreach (var starter in _starters)
                {
                    // Call modular service configurers
                    starter.ConfigureServices(services, _appContext);

                    // Call modular MVC configurers
                    starter.ConfigureMvc(mvcBuilder, services, _appContext);
                }
            }

            public void ConfigureContainer(ContainerBuilder builder)
            {
                builder.RegisterModule(new WorkModule());
                builder.RegisterModule(new CachingModule());
                builder.RegisterModule(new EventsModule(_appContext));

                // Configure all modular services by Autofac
                foreach (var starter in _starters.OfType<IContainerConfigurer>())
                {
                    starter.ConfigureContainer(builder, _appContext);
                }
            }

            public void ConfigureApplication(IApplicationBuilder app)
            {
                // Configure all modular pipelines
                var pipelineBuilder = new RequestPipelineBuilder { ApplicationBuilder = app, ApplicationContext = _appContext };
                foreach (var starter in _starters)
                {
                    starter.BuildPipeline(pipelineBuilder);
                }
                pipelineBuilder.Build(app);

                // Map all modular endpoints
                app.UseEndpoints(endpoints =>
                {
                    var routeBuilder = new EndpointRoutingBuilder { ApplicationBuilder = app, ApplicationContext = _appContext, RouteBuilder = endpoints };
                    foreach (var starter in _starters)
                    {
                        starter.MapRoutes(routeBuilder);
                    }
                    routeBuilder.Build(endpoints);
                });
            }

            private static IEnumerable<IStarter> SortStarters(IEnumerable<IStarter> starters)
            {
                return starters
                    .GroupBy(x => x.Order)
                    .OrderBy(x => x.Key)
                    .SelectMany(x => x.ToArray().SortTopological(StringComparer.OrdinalIgnoreCase))
                    .Cast<IStarter>();
            }

            public void Dispose()
            {
                _engine.IsStarted = true;

                _engine = null;
                _appContext = null;
                _starters.Clear();
                _starters = null;
            }

            class CoreAssembly : ITopologicSortable<string>
            {
                public string Name { get; init; }
                public Assembly Assembly { get; init; }
                string ITopologicSortable<string>.Key 
                {
                    get => Name;
                }

                public string[] DependsOn { get; init; }
            }
        }
    }
}
