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
using Smartstore.Diagnostics;
using Smartstore.Engine.Initialization;

namespace Smartstore.Engine
{
    public class SmartEngine : IEngine
    {
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
                    .ToList();
            }

            public SmartConfiguration AppConfiguration { get; private set; }

            public void ConfigureServices(IServiceCollection services)
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                var app = _engine.Application;

                services.AddOptions();
                services.AddSingleton(app.AppConfiguration);
                services.AddSingleton(app.TypeScanner);
                services.AddSingleton(_engine);
                services.AddSingleton(app);

                // Bind the config to host options
                services.Configure<HostOptions>(app.Configuration.GetSection("HostOptions"));

                // Add Async/Threading stuff
                services.AddAsyncRunner();
                services.AddLockFileManager();

                services.AddApplicationInitializer();

                services.AddSingleton(x => NullChronometer.Instance);
                services.AddSingleton<ILifetimeScopeAccessor, DefaultLifetimeScopeAccessor>();

                // TODO: (core) Register logging and more system stuff

                // Configure all modular services
                foreach (var starter in _starters.OrderBy(x => x.Order))
                {
                    starter.ConfigureServices(services, _appContext, IsActiveModule(starter));
                }
            }

            public void ConfigureContainer(ContainerBuilder builder)
            {
                var app = _engine.Application;

                // Configure all modular services by Autofac
                foreach (var starter in _starters.OrderBy(x => x.Order).OfType<IContainerConfigurer>())
                {
                    starter.ConfigureContainer(builder, _appContext, IsActiveModule(starter));
                }
            }

            public void ConfigureApplication(IApplicationBuilder app)
            {
                var providerContainer = (_appContext as IServiceProviderContainer)
                    ?? throw new ApplicationException($"The implementation of '${nameof(IApplicationContext)}' must also implement '${nameof(IServiceProviderContainer)}'.");

                providerContainer.ApplicationServices = app.ApplicationServices;

                // At this stage - after the service container was built - we can set the scoped service container.
                _engine.Scope = new ScopedServiceContainer(
                    app.ApplicationServices.GetRequiredService<ILifetimeScopeAccessor>(),
                    app.ApplicationServices.GetRequiredService<IHttpContextAccessor>(),
                    app.ApplicationServices.AsLifetimeScope());

                var activeModuleStarters = _starters.Where(IsActiveModule).ToArray();

                // Configure all modular pipelines
                foreach (var starter in activeModuleStarters.OrderBy(x => x.ApplicationOrder))
                {
                    starter.ConfigureApplication(app, _appContext);
                }

                app.UseEndpoints(endpoints =>
                {
                    // Configure all modular endpoints
                    foreach (var starter in activeModuleStarters.OrderBy(x => x.RoutesOrder))
                    {
                        starter.ConfigureRoutes(app, endpoints, _appContext);
                    }
                });
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

        public IApplicationContext Application { get; private set; }
        public ScopedServiceContainer Scope { get; private set; }
        public bool IsStarted { get; private set; }

        public virtual IEngineStarter Start(IApplicationContext application)
        {
            Guard.NotNull(application, nameof(application));

            Application = application;

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
    }
}
