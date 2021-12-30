using Autofac;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Engine;
using Smartstore.Engine.Modularity;

namespace Smartstore.Packager
{
    internal class SimpleEngine : IEngine
    {
        public IApplicationContext Application { get; set; }
        public ScopedServiceContainer Scope { get; set; }
        public bool IsStarted { get; set; }
        public bool IsInitialized { get; set; }

        public IEngineStarter Start(IApplicationContext application)
        {
            Guard.NotNull(application, nameof(application));

            Application = application;

            return new EngineStarter(this);
        }

        class EngineStarter : IEngineStarter
        {
            private SimpleEngine _engine;

            public EngineStarter(SimpleEngine engine)
            {
                _engine = engine;
                AppConfiguration = _engine.Application.AppConfiguration;

                // Provide module catalog
                engine.Application.ModuleCatalog = new ModuleCatalog(DiscoverModules());

                // Provide type scanner which also can reflect over module assemblies
                engine.Application.TypeScanner = new DefaultTypeScanner(typeof(IEngine).Assembly, typeof(Program).Assembly);
            }

            public SmartConfiguration AppConfiguration { get; set; }

            public void ConfigureApplication(IApplicationBuilder builder)
            {
            }

            public void ConfigureContainer(ContainerBuilder builder)
            {
            }

            public void ConfigureServices(IServiceCollection services)
            {
            }

            public IEnumerable<IModuleDescriptor> DiscoverModules()
            {
                return Enumerable.Empty<IModuleDescriptor>();
            }

            public void LoadModule(IModuleDescriptor descriptor)
            {
            }

            public void Dispose()
            {
                _engine.IsInitialized = true;
                _engine.IsStarted = true;

                _engine = null;
            }
        }
    }
}
