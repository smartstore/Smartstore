using Microsoft.Extensions.DependencyInjection;
using Smartstore.Engine;
using Smartstore.Engine.Builders;
using Smartstore.Google.Analytics.Services;

namespace Smartstore.Google.Analytics
{
    internal class Startup : StarterBase
    {
        public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext)
        {
            if (appContext.IsInstalled)
            {
                services.AddScoped<GoogleAnalyticsScriptHelper>();
            }
        }
    }
}