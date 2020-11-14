using System;
using Autofac;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Core.Common.DependencyInjection;
using Smartstore.Core.Configuration.DependencyInjection;
using Smartstore.Core.Data;
using Smartstore.Core.Data.DependecyInjection;
using Smartstore.Core.Localization.DependencyInjection;
using Smartstore.Core.Logging.DependencyInjection;
using Smartstore.Core.Logging.Serilog;
using Smartstore.Core.Stores.DependencyInjection;
using Smartstore.Engine;

namespace Smartstore.Core
{
    public class CoreStarter : StarterBase
    {
        public override int Order => int.MinValue + 100;

        public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext, bool isActiveModule)
        {
            var appConfig = appContext.AppConfiguration;

            // Application DbContext as pooled factory
            services.AddPooledApplicationDbContextFactory<SmartDbContext>(appContext);

            if (appContext.IsInstalled)
            {
                services.AddMiniProfiler(o =>
                {
                    // TODO: (more) Move to module and configure
                }).AddEntityFramework();
            }
        }

        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext, bool isActiveModule)
        {
            builder.RegisterModule(new CommonModule());
            builder.RegisterModule(new LoggingModule());
            builder.RegisterModule(new SettingsModule());
            builder.RegisterModule(new LocalizationModule());

            if (appContext.IsInstalled)
            {
                builder.RegisterModule(new DbHooksModule(appContext));
                builder.RegisterModule(new StoresModule());
            }
        }

        public override void ConfigureApplication(IApplicationBuilder app, IApplicationContext appContext)
        {
            if (appContext.IsInstalled)
            {
                app.UseMiddleware<SerilogHttpContextMiddleware>();
                app.UseMiniProfiler();
            }
        }
    }
}
