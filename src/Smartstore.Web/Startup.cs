using System.Reflection;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Smartstore.Engine;

namespace Smartstore.Web
{
    public class Startup
    {
        private IEngineStarter _engineStarter;
        private SmartApplicationContext _appContext;

        public Startup(WebHostBuilderContext hostBuilderContext)
        {
            Configuration = hostBuilderContext.Configuration;
            Environment = hostBuilderContext.HostingEnvironment;
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
                typeof(Smartstore.Web.Common.Theming.IThemeRegistry).Assembly
            };

            _appContext = new SmartApplicationContext(
                Environment, 
                Configuration, 
                coreAssemblies);

            _engineStarter = EngineFactory.Create(_appContext.AppConfiguration).Start(_appContext);

            _engineStarter.ConfigureServices(services);
        }

        public void ConfigureContainer(ContainerBuilder builder)
        {
            _engineStarter.ConfigureContainer(builder);
        }

        public void Configure(IApplicationBuilder app, IHostApplicationLifetime appLifetime)
        {
            // Write streamlined request completion events, instead of the more verbose ones from the framework.
            // To use the default framework request logging instead, remove this line and set the "Microsoft"
            // level in appsettings.json to "Information".
            app.UseSerilogRequestLogging();
            
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
