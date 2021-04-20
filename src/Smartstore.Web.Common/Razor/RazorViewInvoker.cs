using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Smartstore.Utilities;

namespace Smartstore.Web.Razor
{
    public class RazorViewInvoker : IRazorViewInvoker
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IRazorViewEngine _viewEngine;
        private readonly ITempDataDictionaryFactory _tempDataFactory;
        private readonly IActionContextAccessor _actionContextAccessor;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IViewComponentHelper _viewComponentHelper;
        private readonly IModelMetadataProvider _metadataProvider;
        private readonly IOptions<MvcViewOptions> _mvcViewOptions;

        public RazorViewInvoker(
            IServiceProvider serviceProvider,
            IRazorViewEngine viewEngine,
            ITempDataDictionaryFactory tempDataFactory,
            IActionContextAccessor actionContextAccessor,
            IHttpContextAccessor httpContextAccessor,
            IViewComponentHelper viewComponentHelper,
            IModelMetadataProvider metadataProvider,
            IOptions<MvcViewOptions> mvcViewOptions)
        {
            _serviceProvider = serviceProvider;
            _viewEngine = viewEngine;
            _tempDataFactory = tempDataFactory;
            _actionContextAccessor = actionContextAccessor;
            _httpContextAccessor = httpContextAccessor;
            _viewComponentHelper = viewComponentHelper;
            _metadataProvider = metadataProvider;
            _mvcViewOptions = mvcViewOptions;
        }

        public Task<string> InvokeViewAsync(string viewName, object model, bool isPartial = true)
        {
            var actionContext = GetActionContext();
            var viewData = new ViewDataDictionary(_metadataProvider, actionContext.ModelState)
            {
                Model = model
            };

            return InvokeViewAsync(viewName, actionContext, viewData, isPartial);
        }

        public Task<string> InvokeViewAsync(string viewName, ViewDataDictionary viewData, bool isPartial = true)
        {
            return InvokeViewAsync(viewName, GetActionContext(), viewData, isPartial);
        }

        protected virtual async Task<string> InvokeViewAsync(string viewName, ActionContext actionContext, ViewDataDictionary viewData, bool isPartial = true)
        {
            Guard.NotEmpty(viewName, nameof(viewName));
            Guard.NotNull(actionContext, nameof(actionContext));
            Guard.NotNull(viewData, nameof(viewData));

            var view = FindView(actionContext, viewName, isPartial);

            using var psb = StringBuilderPool.Instance.Get(out var sb);
            using (var output = new StringWriter(sb))
            {
                var viewContext = new ViewContext(
                    actionContext,
                    view,
                    viewData,
                    _tempDataFactory.GetTempData(actionContext.HttpContext),
                    output,
                    _mvcViewOptions.Value.HtmlHelperOptions
                );

                await view.RenderAsync(viewContext);
                return output.ToString();
            }
        }

        public Task<string> InvokeViewComponentAsync(string componentName, ViewDataDictionary viewData, object arguments)
        {
            Guard.NotEmpty(componentName, nameof(componentName));
            return InvokeViewComponentInternal(viewData, () => _viewComponentHelper.InvokeAsync(componentName, arguments));
        }

        public Task<string> InvokeViewComponentAsync(Type componentType, ViewDataDictionary viewData, object arguments)
        {
            Guard.NotNull(componentType, nameof(componentType));
            return InvokeViewComponentInternal(viewData, () => _viewComponentHelper.InvokeAsync(componentType, arguments));
        }

        private async Task<string> InvokeViewComponentInternal(ViewDataDictionary viewData, Func<Task<IHtmlContent>> invoker)
        {
            Guard.NotNull(viewData, nameof(viewData));

            var actionContext = GetActionContext();

            using var psb = StringBuilderPool.Instance.Get(out var sb);
            using (var output = new StringWriter(sb))
            {
                var viewContext = new ViewContext(
                    actionContext,
                    NullView.Instance,
                    viewData,
                    _tempDataFactory.GetTempData(actionContext.HttpContext),
                    output,
                    _mvcViewOptions.Value.HtmlHelperOptions
                );

                if (_viewComponentHelper is IViewContextAware viewContextAware)
                {
                    viewContextAware.Contextualize(viewContext);
                }

                await invoker();
                return output.ToString();
            }
        }

        private ActionContext GetActionContext()
        {
            var context = _actionContextAccessor.ActionContext;

            if (context == null)
            {
                var httpContext = _httpContextAccessor.HttpContext ?? new DefaultHttpContext { RequestServices = _serviceProvider };
                context = new ActionContext(httpContext, httpContext.GetRouteData() ?? new RouteData(), new ActionDescriptor());
            }

            return context;
        }

        private IView FindView(ActionContext actionContext, string viewName, bool isPartial)
        {
            var getViewResult = _viewEngine.GetView(executingFilePath: null, viewPath: viewName, !isPartial);
            if (getViewResult.Success)
            {
                return getViewResult.View;
            }

            var findViewResult = _viewEngine.FindView(actionContext, viewName, !isPartial);
            if (findViewResult.Success)
            {
                return findViewResult.View;
            }

            throw new ViewNotFoundException(viewName, getViewResult.SearchedLocations.Concat(findViewResult.SearchedLocations));
        }
    }
}