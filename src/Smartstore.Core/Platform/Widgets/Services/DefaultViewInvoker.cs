#nullable enable

using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Smartstore.Core.Web;

namespace Smartstore.Core.Widgets
{
    public class DefaultViewInvoker : IViewInvoker
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IViewDataAccessor _viewDataAccessor;
        private readonly IActionContextAccessor _actionContextAccessor;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DefaultViewInvoker(
            IServiceProvider serviceProvider,
            IViewDataAccessor viewDataAccessor,
            IActionContextAccessor actionContextAccessor,
            IHttpContextAccessor httpContextAccessor)
        {
            _serviceProvider = serviceProvider;
            _viewDataAccessor = viewDataAccessor;
            _actionContextAccessor = actionContextAccessor;
            _httpContextAccessor = httpContextAccessor;
        }

        public ViewDataDictionary? ViewData
        {
            get => _viewDataAccessor.ViewData;
        }

        public ActionContext GetActionContext(ActionContext? context, string? module)
        {
            if (context == null)
            {
                context = _actionContextAccessor.ActionContext;
            }

            if (context == null)
            {
                var httpContext = _httpContextAccessor.HttpContext ?? new DefaultHttpContext { RequestServices = _serviceProvider };
                context = new ActionContext(
                    httpContext,
                    httpContext.GetRouteData() ?? new RouteData(),
                    new ActionDescriptor());
            }

            if (module.HasValue())
            {
                context = new ActionContext(context);
                context.RouteData = new RouteData(context.RouteData);
                context.RouteData.DataTokens["module"] = module;
            }

            return context;
        }

        public async Task<HtmlString> InvokeComponentAsync(ActionContext actionContext, ViewComponentResult result)
        {
            Guard.NotNull(actionContext);
            Guard.NotNull(result);

            var widget = result.ViewComponentType != null 
                ? new ComponentWidget(result.ViewComponentType, result.Arguments)
                : new ComponentWidget(result.ViewComponentName!, result.Arguments);

            var widgetContext = new WidgetContext(actionContext)
            {
                ViewData = result.ViewData,
                TempData = result.TempData
            };

            return (await widget.InvokeAsync(widgetContext)).ToHtmlString();
        }

        public async Task<HtmlString> InvokePartialViewAsync(ActionContext actionContext, PartialViewResult result)
        {
            Guard.NotNull(actionContext);
            Guard.NotNull(result);

            var widget = new PartialViewWidget(result.ViewName!, result.Model);
            var widgetContext = new WidgetContext(actionContext)
            {
                ViewData = result.ViewData,
                TempData = result.TempData
            };

            return (await widget.InvokeAsync(widgetContext)).ToHtmlString();
        }

        public async Task<HtmlString> InvokeViewAsync(ActionContext actionContext, ViewResult result)
        {
            Guard.NotNull(actionContext);
            Guard.NotNull(result);

            var widget = new PartialViewWidget(result.ViewName!, result.Model) { IsMainPage = true };
            var widgetContext = new WidgetContext(actionContext)
            {
                ViewData = result.ViewData,
                TempData = result.TempData
            };

            return (await widget.InvokeAsync(widgetContext)).ToHtmlString();
        }
    }
}
