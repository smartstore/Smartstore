using System;
using System.Linq;
using System.Net.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using Smartstore.Engine;
using Smartstore.Engine.Builders;
using Smartstore.Paystack.Client;
using Smartstore.Paystack.Configuration;
using Smartstore.Web.Controllers;

namespace Smartstore.Paystack
{
    internal class Startup : StarterBase
    {
        public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext)
        {
            services.AddHttpClient("googleservice", (sp,c) =>
            {
                var settings = sp.GetRequiredService<PaystackSettings>();
                if (!settings.PrivateKey.IsEmpty() && !settings.BaseUrl.IsEmpty())
                {
                    c.BaseAddress = new Uri(settings.BaseUrl);
                    c.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", settings.PrivateKey);
                }

            }).AddTypedClient(r => RestService.For<IPaystackClient>(r));


           
        }
    }
}
