using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Core.Widgets;
using Smartstore.Forums.Components;

namespace Smartstore.Forums.Filters
{
    public class CustomerProfileFilter : IResultFilter
    {
        private readonly Lazy<IWidgetProvider> _widgetProvider;
        private readonly Lazy<IUrlHelper> _urlHelper;
        private readonly Lazy<ILocalizationService> _localizationService;
        private readonly ForumSettings _forumSettings;

        public CustomerProfileFilter(
            Lazy<IWidgetProvider> widgetProvider,
            Lazy<IUrlHelper> urlHelper,
            Lazy<ILocalizationService> localizationService,
            ForumSettings forumSettings)
        {
            _widgetProvider = widgetProvider;
            _urlHelper = urlHelper;
            _localizationService = localizationService;
            _forumSettings = forumSettings;
        }

        public void OnResultExecuting(ResultExecutingContext filterContext)
        {
            if (filterContext.Result.IsHtmlViewResult())
            {
                if (filterContext?.RouteData?.Values?.TryGetValue("id", out var customerIdObj) ?? false)
                {
                    var customerId = customerIdObj.ToString().ToInt();
                    if (customerId != 0)
                    {
                        // PM button.
                        if (_forumSettings.AllowPrivateMessages)
                        {
                            var link = _urlHelper.Value.Action("Send", "PrivateMessages", new { id = customerId });
                            var text = _localizationService.Value.GetResource("Forum.PrivateMessages.PM");
                            var html = $"<a href='{ link }' class='btn btn-outline-info btn-flat button-pm mt-2' rel='nofollow'>< i class='fa fa-user'></i>" +
                                $"<span>{ text }</span></a>";

                            _widgetProvider.Value.RegisterHtml("profile_page_info_userdetails", new HtmlString(html));
                        }

                        // Forum statistics.
                        if (_forumSettings.ForumsEnabled && _forumSettings.ShowCustomersPostCount)
                        {
                            var widget = new ComponentWidgetInvoker(typeof(ForumCustomerStatsViewComponent), customerId);

                            _widgetProvider.Value.RegisterWidget(new[] { "profile_page_info_userstats" }, widget);
                        }
                    }
                }
            }
        }

        public void OnResultExecuted(ResultExecutedContext filterContext)
        {
        }
    }
}
