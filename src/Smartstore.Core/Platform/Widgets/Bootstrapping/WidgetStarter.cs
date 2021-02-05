using Microsoft.Extensions.DependencyInjection;
using Smartstore.Core.Widgets;
using Smartstore.Engine;
using Smartstore.Engine.Builders;

namespace Smartstore.Core.Bootstrapping
{
    public sealed class WidgetStarter : StarterBase
    {
        public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext, bool isActiveModule)
        {
            services.AddScoped<IWidgetProvider, DefaultWidgetProvider>();
            services.AddScoped<IWidgetSelector, DefaultWidgetSelector>();
            services.AddScoped<IWidgetService, WidgetService>();
        }
    }
}
