using System;
using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Core;
using Smartstore.Core.Widgets;
using Smartstore.Forums.Components;
using Smartstore.Forums.Models.Public;
using Smartstore.Forums.Services;

namespace Smartstore.Forums.Filters
{
    public class CustomerInfoFilter : IResultFilter
    {
        private readonly IWorkContext _workContext;
        private readonly Lazy<IWidgetProvider> _widgetProvider;
        private readonly ForumSettings _forumSettings;

        public CustomerInfoFilter(
            IWorkContext workContext,
            Lazy<IWidgetProvider> widgetProvider,
            ForumSettings forumSettings)
        {
            _workContext = workContext;
            _widgetProvider = widgetProvider;
            _forumSettings = forumSettings;
        }

        public void OnResultExecuting(ResultExecutingContext filterContext)
        {
            // Forum signature.
            if (_forumSettings.ForumsEnabled && _forumSettings.SignaturesEnabled && filterContext.Result.IsHtmlViewResult())
            {
                var customer = _workContext.CurrentCustomer;
                var model = new ForumCustomerInfoModel
                {
                    Signature = customer.GenericAttributes.Get<string>(ForumService.SignatureKey)
                };

                var widget = new ComponentWidgetInvoker(typeof(ForumCustomerInfoViewComponent), model);

                _widgetProvider.Value.RegisterWidget(new[] { "customer_info_bottom" }, widget);
            }
        }

        public void OnResultExecuted(ResultExecutedContext filterContext)
        {
        }
    }
}
