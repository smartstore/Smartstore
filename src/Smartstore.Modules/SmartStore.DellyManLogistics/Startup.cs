using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Engine.Builders;
using Smartstore.Engine;
using Smartstore.Shipping.Settings;
using Smartstore;
using SmartStore.DellyManLogistics.Client;
using Refit;

namespace SmartStore.DellyManLogistics
{
    internal class Startup : StarterBase
    {
        public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext)
        {
            services.AddHttpClient("dellymanservice", (sp, c) =>
            {
                var settings = sp.GetRequiredService<DellyManLogisticsSettings>();
                if (!settings.BaseUrl.IsEmpty())
                {
                    c.BaseAddress = new Uri(settings.BaseUrl);
                    c.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", settings.ApiKey);
                }

            }).AddTypedClient(r => RestService.For<IDellyManClient>(r));
        }
    }
}
