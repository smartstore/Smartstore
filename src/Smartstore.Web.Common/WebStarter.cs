using System;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Engine;
using Smartstore.Web.Common.TagHelpers;
using Smartstore.Web.Common.Theming;

namespace Smartstore.Web.Common
{
    public class WebStarter : StarterBase
    {
        public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext, bool isActiveModule)
        {
            services.AddScoped<ITagHelperComponent, LanguageDirTagHelperComponent>();
            services.AddScoped<IRazorViewRenderer, RazorViewRenderer>();
        }
    }
}
