using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Core.Data;
using Smartstore.Core.Seo;
using Smartstore.Data;
using Smartstore.Data.Providers;
using Smartstore.Engine;
using Smartstore.Engine.Builders;
using Smartstore.Polls.Filters;
using Smartstore.Web.Controllers;

namespace Smartstore.Polls
{
    internal class Startup : StarterBase
    {
        public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext)
        {
            // Add robots.txt entry.
            SeoSettings.DefaultRobotDisallows.Add("/Poll/Vote");

            services.AddTransient<IDbContextConfigurationSource<SmartDbContext>, SmartDbContextConfigurer>();

            services.Configure<MvcOptions>(o =>
            {
                o.Filters.AddConditional<PollFilter>(
                    context => context.ControllerIs<PublicController>() && !context.HttpContext.Request.IsAjaxRequest());
            });
        }

        class SmartDbContextConfigurer : IDbContextConfigurationSource<SmartDbContext>
        {
            public void Configure(IServiceProvider services, DbContextOptionsBuilder builder)
            {
                builder.UseDbFactory(b => 
                {
                    b.AddModelAssembly(this.GetType().Assembly);
                });
            }
        }
    }
}
