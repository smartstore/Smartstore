using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using Autofac;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Smartstore.Collections;
using Smartstore.Diagnostics;
using Smartstore.Engine.Initialization;
using Smartstore.DependencyInjection;
using Smartstore.Engine.Builders;
using Smartstore.Pdf;
using Smartstore.ComponentModel;

namespace Smartstore.Engine
{
    public class SmartEngine : IEngine
    {
        public IApplicationContext Application { get; private set; }
        public ScopedServiceContainer Scope { get; set; }
        public bool IsStarted { get; private set; }
        public bool IsInitialized { get; private set; }

        public virtual IEngineStarter Start(IApplicationContext application)
        {
            Guard.NotNull(application, nameof(application));

            Application = application;

            // Set IsInitialized prop after init completes.
            RootApplicationInitializer.Initialized += (s, e) => IsInitialized = true;

            // Assembly resolver event. View rendering in modules can throw exceptions otherwise.
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;

            return new EngineStarter(this);
        }

        private Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            // Check for assembly already loaded
            var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName == args.Name);
            if (assembly != null)
                return assembly;

            // Get assembly from TypeScanner
            var typeScanner = Application.TypeScanner;
            if (typeScanner == null)
                return null;

            assembly = typeScanner.Assemblies.FirstOrDefault(a => a.FullName == args.Name);
            return assembly;
        }

        class EngineStarter : IEngineStarter
        {
            private SmartEngine _engine;
            private IApplicationContext _appContext;
            private IList<IStarter> _starters;

            public EngineStarter(SmartEngine engine)
            {
                _engine = engine;
                _appContext = engine.Application;

                _starters = _appContext.TypeScanner.FindTypes<IStarter>()
                    .Select(t => (IStarter)Activator.CreateInstance(t))
                    .Where(x => x.Matches(_appContext))
                    .ToList();
                _starters = SortStarters(_starters).ToList();
            }

            public SmartConfiguration AppConfiguration { get; private set; }

            public void ConfigureServices(IServiceCollection services)
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                var app = _engine.Application;

                services.AddOptions();
                services.AddSingleton(app.AppConfiguration);
                services.AddSingleton(app.TypeScanner);
                services.AddSingleton<IEngine>(_engine);
                services.AddSingleton(app);

                // Bind the config to host options
                services.Configure<HostOptions>(app.Configuration.GetSection("HostOptions"));

                // Add Async/Threading stuff
                services.AddAsyncRunner();
                services.AddLockFileManager();

                services.AddApplicationInitializer();

                services.AddSingleton(x => NullChronometer.Instance);
                services.AddSingleton<IJsonSerializer, NewtonsoftJsonSerializer>();
                services.AddSingleton<ILifetimeScopeAccessor, DefaultLifetimeScopeAccessor>();
                services.AddSingleton<IPdfConverter, NullPdfConverter>();
                services.AddHttpContextAccessor();

                // TODO: (core) Configuration for MemoryCache?
                services.AddMemoryCache();

                // TODO: (core) Register more system stuff
                services.AddMailKitMailService();
                services.AddTemplateEngine();

                // Configure all modular services
                foreach (var starter in _starters)
                {
                    starter.ConfigureServices(services, _appContext, IsActiveModule(starter));
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
                    starter.ConfigureContainer(builder, _appContext, IsActiveModule(starter));
                }
            }

            public void ConfigureApplication(IApplicationBuilder app)
            {
                var activeModuleStarters = _starters.Where(IsActiveModule).ToArray();

                // Configure all modular pipelines
                var pipelineBuilder = new RequestPipelineBuilder { ApplicationBuilder = app, ApplicationContext = _appContext };
                foreach (var starter in activeModuleStarters)
                {
                    starter.BuildPipeline(pipelineBuilder);
                }
                pipelineBuilder.Build(app);

                // Map all modular endpoints
                app.UseEndpoints(endpoints =>
                {
                    var routeBuilder = new EndpointRoutingBuilder { ApplicationBuilder = app, ApplicationContext = _appContext, RouteBuilder = endpoints };
                    foreach (var starter in activeModuleStarters)
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

            private bool IsActiveModule(IStarter starter)
            {
                return _engine.Application.ModuleCatalog.IsActiveModuleAssembly(starter.GetType().Assembly);
            }

            private bool IsActiveModule(IContainerConfigurer configurer)
            {
                return _engine.Application.ModuleCatalog.IsActiveModuleAssembly(configurer.GetType().Assembly);
            }

            public void Dispose()
            {
                _engine.IsStarted = true;

                _engine = null;
                _appContext = null;
                _starters.Clear();
                _starters = null;
            }
        }
    }
}
