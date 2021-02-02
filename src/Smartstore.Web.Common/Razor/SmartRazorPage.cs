using System;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Smartstore.Core;
using Smartstore.Core.Localization;
using Smartstore.Engine;
using Smartstore.Events;
using Smartstore.Web.UI;

namespace Smartstore.Web.Razor
{
    public abstract class SmartRazorPage : SmartRazorPage<dynamic>
    {
    }

    public abstract class SmartRazorPage<TModel> : RazorPage<TModel>
    {
        [RazorInject]
        public Localizer T { get; set; }

        [RazorInject]
        public IWorkContext WorkContext { get; set; }

        [RazorInject]
        public IEventPublisher EventPublisher { get; set; }

        [RazorInject]
        public IApplicationContext ApplicationContext { get; set; }

        [RazorInject]
        public IPageAssetBuilder AssetBuilder { get; set; }
    }
}
