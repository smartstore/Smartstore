using System;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Core;
using Smartstore.Core.Localization;
using Smartstore.Core.Web;
using Smartstore.Core.Widgets;
using Smartstore.Engine;
using Smartstore.Events;

namespace Smartstore.Web.Razor
{
    public abstract class SmartRazorPage : SmartRazorPage<dynamic>
    {
    }

    public abstract class SmartRazorPage<TModel> : RazorPage<TModel>
    {
        [RazorInject]
        protected IDisplayHelper Display { get; set; }

        [RazorInject]
        protected Localizer T { get; set; }

        [RazorInject]
        protected IWorkContext WorkContext { get; set; }

        [RazorInject]
        protected IWebHelper WebHelper { get; set; }

        [RazorInject]
        protected IEventPublisher EventPublisher { get; set; }

        [RazorInject]
        protected IApplicationContext ApplicationContext { get; set; }

        [RazorInject]
        protected IPageAssetBuilder AssetBuilder { get; set; }

        [RazorInject]
        protected IUserAgent UserAgent { get; set; }

        /// <summary>
        /// Resolves a service from scoped service container.
        /// </summary>
        /// <typeparam name="T">Type to resolve</typeparam>
        protected T Resolve<T>() where T : notnull
        {
            return Context.RequestServices.GetRequiredService<T>();
        }
    }
}
