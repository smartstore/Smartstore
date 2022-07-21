using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Engine;
using Smartstore.Engine.Builders;
using Smartstore.Google.Analytics.Filters;
using Smartstore.Google.Analytics.Services;
using Smartstore.Web.Controllers;

namespace Smartstore.Google.Analytics
{
    internal class Startup : StarterBase
    {
        public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext)
        {
            if (appContext.IsInstalled)
            {
                services.AddScoped<GoogleAnalyticsScriptHelper>();

                services.Configure<MvcOptions>(o =>
                {
                    o.Filters.AddConditional<CheckoutFilter>(
                        context => context.ControllerIs<CheckoutController>(x => x.Confirm()) && !context.HttpContext.Request.IsAjax());
                });
            }
        }
    }
}