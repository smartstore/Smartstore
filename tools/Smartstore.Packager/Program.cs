using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Core.Packaging;
using Smartstore.Engine;
using Smartstore.IO;
using System.Diagnostics;

namespace Smartstore.Packager
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            var appContext = host.Services.GetRequiredService<IApplicationContext>();
            var providerContainer = (appContext as IServiceProviderContainer)
                ?? throw new ApplicationException($"The implementation of '${nameof(IApplicationContext)}' must also implement '${nameof(IServiceProviderContainer)}'.");
            providerContainer.ApplicationServices = host.Services;

            var engine = host.Services.GetRequiredService<IEngine>();
            var scopeAccessor = host.Services.GetRequiredService<ILifetimeScopeAccessor>();
            engine.Scope = new ScopedServiceContainer(
                scopeAccessor,
                host.Services.GetRequiredService<IHttpContextAccessor>(),
                host.Services.AsLifetimeScope());

            using (scopeAccessor.BeginContextAwareScope(out var scope))
            {
                ApplicationConfiguration.Initialize();
                Application.Run(scope.Resolve<MainForm>());
            }
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            IEngineStarter starter = null;
            
            var builder = Host.CreateDefaultBuilder(args)
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureAppConfiguration((context, builder) =>
                {
                    builder.AddJsonFile("appsettings.json", optional: true);
                    builder.AddEnvironmentVariables();
                })
                .ConfigureHostOptions((context, options) =>
                {
                    context.HostingEnvironment.EnvironmentName = Debugger.IsAttached ? Environments.Development : Environments.Production;
                })
                .ConfigureServices((context, services) =>
                {
                    var appContext = new SmartApplicationContext(
                        context.HostingEnvironment,
                        context.Configuration,
                        NullLogger.Instance);

                    appContext.AppConfiguration.EngineType = typeof(PackagerEngine).AssemblyQualifiedName;

                    starter = EngineFactory
                        .Create(appContext.AppConfiguration)
                        .Start(appContext);

                    starter.ConfigureServices(services);

                    services.AddScoped<IPackageBuilder, PackageBuilder>();
                    services.AddScoped<MainForm>();

                })
                .ConfigureContainer<ContainerBuilder>((context, builder) =>
                {
                    starter.ConfigureContainer(builder);
                    starter.Dispose();
                });

            return builder;
        }
    }
}