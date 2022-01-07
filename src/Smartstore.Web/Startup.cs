global using System;
global using System.Collections.Generic;
global using System.IO;
global using System.Linq;
global using System.Threading;
global using System.Threading.Tasks;
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;
global using Smartstore.Core;
global using Smartstore.Core.Common;
global using Smartstore.Core.Data;
global using Smartstore.Core.Widgets;
global using Smartstore.Domain;
global using Smartstore.Engine;
global using Smartstore.Web.Components;
global using Smartstore.Web.Controllers;
global using Smartstore.Web.Filters;
global using Smartstore.Web.Modelling;
global using EntityState = Smartstore.Data.EntityState;

using Autofac;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using MsLogger = Microsoft.Extensions.Logging.ILogger;

namespace Smartstore.Web
{
    public class Startup
    {
        private IEngineStarter _engineStarter;
        private SmartApplicationContext _appContext;

        public Startup(WebHostBuilderContext hostBuilderContext, MsLogger startupLogger)
        {
            Configuration = hostBuilderContext.Configuration;
            Environment = hostBuilderContext.HostingEnvironment;
            StartupLogger = startupLogger;
        }

        public MsLogger StartupLogger { get; }
        public IConfiguration Configuration { get; }
        public IWebHostEnvironment Environment { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            _appContext = new SmartApplicationContext(Environment, Configuration, StartupLogger);

            _engineStarter = EngineFactory
                .Create(_appContext.AppConfiguration)
                .Start(_appContext);

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
