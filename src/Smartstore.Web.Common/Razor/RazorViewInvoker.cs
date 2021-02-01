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

namespace Smartstore.Web.Razor
{
    public class RazorViewInvoker : IRazorViewInvoker
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IRazorViewEngine _viewEngine;
        private readonly ITempDataProvider _tempDataProvider;
        private readonly IActionContextAccessor _actionContextAccessor;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IViewComponentHelper _viewComponentHelper;
        private readonly IModelMetadataProvider _metadataProvider;
        private readonly IOptions<MvcViewOptions> _mvcViewOptions;

        public RazorViewInvoker(
            IServiceProvider serviceProvider,
            IRazorViewEngine viewEngine,
            ITempDataProvider tempDataProvider,
            IActionContextAccessor actionContextAccessor,
            IHttpContextAccessor httpContextAccessor,
            IViewComponentHelper viewComponentHelper,
            IModelMetadataProvider metadataProvider,
            IOptions<MvcViewOptions> mvcViewOptions)
        {
            _serviceProvider = serviceProvider;
            _viewEngine = viewEngine;
            _tempDataProvider = tempDataProvider;
            _actionContextAccessor = actionContextAccessor;
            _httpContextAccessor = httpContextAccessor;
            _viewComponentHelper = viewComponentHelper;
            _metadataProvider = metadataProvider;
            _mvcViewOptions = mvcViewOptions;
        }

        public async Task<string> InvokeViewAsync(string viewName, object model, bool isMainPage = false)
        {
            Guard.NotNull(viewName, nameof(viewName));
            
            var actionContext = GetActionContext();
            var view = FindView(actionContext, viewName, isMainPage);

            var viewData = new ViewDataDictionary(_metadataProvider, actionContext.ModelState)
            {
                Model = model
            };

            using (var output = new StringWriter())
            {
                var viewContext = new ViewContext(
                    actionContext,
                    view,
                    viewData,
                    new TempDataDictionary(actionContext.HttpContext, _tempDataProvider),
                    output,
                    _mvcViewOptions.Value.HtmlHelperOptions
                );

                await view.RenderAsync(viewContext);
                return output.ToString();
            }
        }

        public Task<string> InvokeViewComponentAsync(ViewDataDictionary viewData, string componentName, object arguments)
        {
            Guard.NotEmpty(componentName, nameof(componentName));
            return InvokeViewComponentInternal(viewData, arguments, () => _viewComponentHelper.InvokeAsync(componentName, arguments));
        }

        public Task<string> InvokeViewComponentAsync(ViewDataDictionary viewData, Type componentType, object arguments)
        {
            Guard.NotNull(componentType, nameof(componentType));
            return InvokeViewComponentInternal(viewData, arguments, () => _viewComponentHelper.InvokeAsync(componentType, arguments));
        }

        private async Task<string> InvokeViewComponentInternal(ViewDataDictionary viewData, object arguments, Func<Task<IHtmlContent>> invoker)
        {
            Guard.NotNull(viewData, nameof(viewData));

            var actionContext = GetActionContext();

            using (var output = new StringWriter())
            {
                var viewContext = new ViewContext(
                    actionContext,
                    NullView.Instance,
                    viewData,
                    new TempDataDictionary(actionContext.HttpContext, _tempDataProvider),
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

            return context ?? throw new InvalidOperationException("Could not resolve a current ActionContext.");
        }

        private IView FindView(ActionContext actionContext, string viewName, bool isMainPage)
        {
            var getViewResult = _viewEngine.GetView(executingFilePath: null, viewPath: viewName, isMainPage);
            if (getViewResult.Success)
            {
                return getViewResult.View;
            }

            var findViewResult = _viewEngine.FindView(actionContext, viewName, isMainPage);
            if (findViewResult.Success)
            {
                return findViewResult.View;
            }

            throw new ViewNotFoundException(viewName, getViewResult.SearchedLocations.Concat(findViewResult.SearchedLocations));
        }
    }
}