using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Engine;

namespace Smartstore.Web.Common
{
    public class WebStarter : StarterBase
    {
        public override int Order => int.MinValue + 100;

        public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext, bool isActiveModule)
        {
            services.AddSmartstoreMvc(appContext);
        }

        public override void ConfigureApplication(IApplicationBuilder app, IApplicationContext appContext)
        {
            app.UseSmartstoreMvc(appContext);
        }
    }
}
