using System.Reflection;
using System.Runtime.Loader;
using Autofac;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.Logging;
using Smartstore.Collections;
using Smartstore.Engine.Builders;
using Smartstore.Engine.Initialization;
using Smartstore.Engine.Modularity;

namespace Smartstore.Engine
{
    public class SmartEngine : IEngine
    {
        const string SmartstoreNamespace = "Smartstore";

        private bool _isStarted;
        private IModuleReferenceResolver[] _referenceResolvers;

        public IApplicationContext Application { get; private set; }
        public ScopedServiceContainer Scope { get; set; }
        public bool IsInitialized { get; private set; }

        public bool IsStarted
        {
            get => _isStarted;
            set
            {
                if (_isStarted && value == false)
                {
                    throw new InvalidOperationException($"After the engine has been started the '{nameof(IsStarted)}' property is readonly.");
                }

                _isStarted = value;
            }
        }

        public virtual IEngineStarter Start(IApplicationContext application)
        {
            Guard.NotNull(application, nameof(application));

            Application = application;

            // Set IsInitialized prop after init completes.
            ApplicationInitializerMiddleware.Initialized += (s, e) => IsInitialized = true;

            // Assembly resolver event.
            _referenceResolvers = new IModuleReferenceResolver[]
            {
                new ModuleReferenceResolver(application),
                new AppBaseReferenceResolver(application)
            };
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;

            return new EngineStarter(this);
        }

        private Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (Application.ModuleCatalog == null)
            {
                // Too early: don't try to resolve assemblies before the module catalog is built.
                return null;
            }

            foreach (var resolver in _referenceResolvers)
            {
                var assembly = resolver.ResolveAssembly(args.RequestingAssembly, args.Name);
                if (assembly != null)
                {
                    return assembly;
                }
            }

            return null;
        }

        class EngineStarter : EngineStarter<SmartEngine>
        {
            public EngineStarter(SmartEngine engine)
                : base(engine)
            {
            }

            protected override IEnumerable<Assembly> ResolveCoreAssemblies()
            {
                var assemblies = new HashSet<Assembly>();
                var nsPrefix = SmartstoreNamespace + '.';

                var libs = DependencyContext.Default.CompileLibraries
                    .Where(x => IsCoreDependency(x.Name))
                    .Select(x => new CoreAssembly
                    {
                        Name = new AssemblyName(x.Name),
                        DependsOn = x.Dependencies
                            .Where(y => IsCoreDependency(y.Name))
                            .Select(y => y.Name)
                            .ToArray()
                    })
                    .ToArray()
                    .SortTopological()
                    .Cast<CoreAssembly>()
                    .ToArray();

                var appAssemblies = AssemblyLoadContext.Default.Assemblies
                    .Where(x => x.FullName.StartsWith(SmartstoreNamespace) && IsCoreDependency(x.GetName().Name))
                    .Select(x => new
                    {
                        Name = x.GetName(),
                        Assembly = x
                    })
                    .ToArray();

                foreach (var lib in libs)
                {
                    try
                    {
                        var assembly = appAssemblies.FirstOrDefault(x => x.Name.Name == lib.Name.Name)?.Assembly;
                        if (assembly == null)
                        {
                            assembly = AssemblyLoadContext.Default.LoadFromAssemblyName(lib.Name);
                        }
                        
                        if (assembly != null)
                        {
                            assemblies.Add(assembly);
                            Engine.Application.Logger.Debug("Core assembly '{0}' discovered and loaded.", lib.Name.Name);
                        }
                    }
                    catch (Exception ex)
                    {
                        Engine.Application.Logger.Error(ex);
                    }
                }

                return assemblies;

                bool IsCoreDependency(string name)
                {
                    return (name == SmartstoreNamespace || name.StartsWith(nsPrefix)) &&
                        // Exclude data provider projects (they are loaded dynamically)
                        !name.StartsWith(nsPrefix + "Data.");
                }
            }

            protected override IEnumerable<IModuleDescriptor> DiscoverModules()
            {
                var moduleExplorer = new ModuleExplorer(Engine.Application);
                return moduleExplorer.DiscoverModules();
            }

            public override void ConfigureServices(IServiceCollection services)
            {
                base.ConfigureServices(services);

                // Add MVC core services
                var mvcBuilder = services.AddControllersWithViews();

                // Configure all modular MVC starters
                foreach (var starter in Starters)
                {
                    // Call modular MVC configurers
                    starter.ConfigureMvc(mvcBuilder, services, Engine.Application);
                }
            }

            public override void ConfigureApplication(IApplicationBuilder app)
            {
                base.ConfigureApplication(app);

                // Configure all modular pipelines
                var pipelineBuilder = new RequestPipelineBuilder { ApplicationBuilder = app, ApplicationContext = Engine.Application };
                foreach (var starter in Starters)
                {
                    starter.BuildPipeline(pipelineBuilder);
                }
                pipelineBuilder.Build(app);

                // Map all modular endpoints
                app.UseEndpoints(endpoints =>
                {
                    var routeBuilder = new EndpointRoutingBuilder { ApplicationBuilder = app, ApplicationContext = Engine.Application, RouteBuilder = endpoints };
                    foreach (var starter in Starters)
                    {
                        starter.MapRoutes(routeBuilder);
                    }
                    routeBuilder.Build(endpoints);
                });
            }

            class CoreAssembly : ITopologicSortable<string>
            {
                public AssemblyName Name { get; init; }
                public Assembly Assembly { get; init; }
                string ITopologicSortable<string>.Key
                {
                    get => Name.Name;
                }

                public string[] DependsOn { get; init; }
            }
        }
    }
}
