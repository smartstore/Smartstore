using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Clickatell.Services;
using Smartstore.Engine;
using Smartstore.Engine.Builders;
using Smartstore.Net.Http;

namespace Smartstore.Clickatell
{
    internal class Startup : StarterBase
    {
        public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext)
        {
            services.AddHttpClient<ClickatellHttpClient>()
                .AddSmartstoreUserAgent()
                .ConfigureHttpClient(client =>
                {
                    client.BaseAddress = new Uri("https://platform.clickatell.com/messages");
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                });
        }
    }
}
