using System.Reflection;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Smartstore.Engine;

namespace Smartstore.Web
{
    public class Startup
    {
        private IEngineStarter _engineStarter;
        private SmartApplicationContext _appContext;

        public Startup(IWebHostEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddJsonFile("Config/Connections.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"Config/Connections.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            this.Configuration = builder.Build();
            this.Environment = env;
        }

        public IConfiguration Configuration { get; }
        public IWebHostEnvironment Environment { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            var coreAssemblies = new Assembly[]
            {
                typeof(Smartstore.Engine.IEngine).Assembly,
                typeof(Smartstore.Core.CoreStarter).Assembly,
                typeof(Smartstore.Web.Startup).Assembly,
                typeof(Smartstore.Web.Common.WebStarter).Assembly
            };

            _appContext = new SmartApplicationContext(Environment, Configuration, coreAssemblies);

            _engineStarter = EngineFactory.Create(_appContext.AppConfiguration).Start(_appContext);

            _engineStarter.ConfigureServices(services);
        }

        public void ConfigureContainer(ContainerBuilder builder)
        {
            _engineStarter.ConfigureContainer(builder);
        }

        public void Configure(IApplicationBuilder app, IHostApplicationLifetime appLifetime)
        {
            appLifetime.ApplicationStarted.Register(OnStarted, app);

            _engineStarter.ConfigureApplication(app);
        }

        private void OnStarted(object app = null)
        {
            _appContext.Freeze();

            _engineStarter.Dispose();
            _engineStarter = null;
        }
    }
}
