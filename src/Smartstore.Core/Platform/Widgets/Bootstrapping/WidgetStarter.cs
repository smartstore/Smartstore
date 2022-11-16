using Smartstore.Core.Widgets;
using Smartstore.Engine.Builders;

namespace Smartstore.Core.Bootstrapping
{
    internal sealed class WidgetStarter : StarterBase
    {
        public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext)
        {
            services.AddScoped<IWidgetProvider, DefaultWidgetProvider>();
            services.AddScoped<IWidgetService, WidgetService>();
            services.AddScoped<IPageAssetBuilder, PageAssetBuilder>();
            services.AddSingleton<IAssetTagGenerator, NullAssetTagGenerator>();

            if (appContext.IsInstalled)
            {
                services.AddScoped<IWidgetSelector, DefaultWidgetSelector>();
            }
            else
            {
                services.AddScoped(x => NullWidgetSelector.Instance);
            }
        }
    }
}
