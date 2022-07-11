using System.Globalization;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Smartstore.Core.Web;

namespace Smartstore.Web.Razor
{
    public class DefaultViewInvoker : IViewInvoker
    {
        private const string ActionNameKey = "action";

        private readonly IServiceProvider _serviceProvider;
        private readonly IViewDataAccessor _viewDataAccessor;
        private readonly IActionContextAccessor _actionContextAccessor;
        private readonly IModelMetadataProvider _modelMetadataProvider;
        private readonly ITempDataDictionaryFactory _tempDataDictionaryFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IViewEngine _viewEngine;
        private readonly HtmlHelperOptions _htmlHelperOptions;
        private readonly HtmlEncoder _htmlEncoder;

        public DefaultViewInvoker(
            IServiceProvider serviceProvider,
            IViewDataAccessor viewDataAccessor,
            IActionContextAccessor actionContextAccessor,
            IModelMetadataProvider modelMetadataProvider,
            ITempDataDictionaryFactory tempDataDictionaryFactory,
            IHttpContextAccessor httpContextAccessor,
            ICompositeViewEngine viewEngine,
            IOptions<MvcViewOptions> mvcViewOptions,
            HtmlEncoder htmlEncoder)
        {
            _serviceProvider = serviceProvider;
            _viewDataAccessor = viewDataAccessor;
            _actionContextAccessor = actionContextAccessor;
            _modelMetadataProvider = modelMetadataProvider;
            _tempDataDictionaryFactory = tempDataDictionaryFactory;
            _httpContextAccessor = httpContextAccessor;
            _viewEngine = viewEngine;
            _htmlHelperOptions = mvcViewOptions.Value.HtmlHelperOptions;
            _htmlEncoder = htmlEncoder;
        }

        public ViewDataDictionary ViewData
        {
            get => _viewDataAccessor.ViewData;
        }

        public ActionContext GetActionContext(ActionContext context, string module)
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

        #region (Partial)View

        public async Task InvokeViewAsync(ActionContext context, ViewResult result, TextWriter writer)
        {
            Guard.NotNull(context, nameof(context));
            Guard.NotNull(result, nameof(result));
            Guard.NotNull(writer, nameof(writer));

            var viewEngineResult = FindView(context, result.ViewEngine, result.ViewName, true);
            viewEngineResult.EnsureSuccessful(originalLocations: null);

            var view = viewEngineResult.View;
            using (view as IDisposable)
            {
                await InvokeViewInternalAsync(
                    context,
                    view,
                    result.ViewData,
                    result.TempData,
                    writer);
            }
        }

        public async Task InvokePartialViewAsync(ActionContext context, PartialViewResult result, TextWriter writer)
        {
            Guard.NotNull(context, nameof(context));
            Guard.NotNull(result, nameof(result));
            Guard.NotNull(writer, nameof(writer));

            var viewEngineResult = FindView(context, result.ViewEngine, result.ViewName, false);
            viewEngineResult.EnsureSuccessful(originalLocations: null);

            var view = viewEngineResult.View;
            using (view as IDisposable)
            {
                await InvokeViewInternalAsync(
                    context,
                    view,
                    result.ViewData,
                    result.TempData,
                    writer);
            }
        }

        private async Task InvokeViewInternalAsync(
            ActionContext context,
            IView view,
            ViewDataDictionary viewData,
            ITempDataDictionary tempData,
            TextWriter writer)
        {
            viewData ??= new ViewDataDictionary(_modelMetadataProvider, context.ModelState);
            tempData ??= _tempDataDictionaryFactory.GetTempData(context.HttpContext);

            var viewContext = new ViewContext(
                context,
                view,
                viewData,
                tempData,
                writer,
                _htmlHelperOptions);

            await view.RenderAsync(viewContext);
        }

        private ViewEngineResult FindView(ActionContext actionContext, IViewEngine viewEngine, string viewName, bool isMainPage)
        {
            viewEngine ??= _viewEngine;
            viewName ??= GetActionName(actionContext).EmptyNull();

            var result = viewEngine.GetView(executingFilePath: null, viewPath: viewName, isMainPage: isMainPage);
            var originalResult = result;
            if (!result.Success)
            {
                result = viewEngine.FindView(actionContext, viewName, isMainPage: isMainPage);
            }

            if (!result.Success)
            {
                if (originalResult.SearchedLocations.Any())
                {
                    if (result.SearchedLocations.Any())
                    {
                        // Return a new ViewEngineResult listing all searched locations.
                        var locations = new List<string>(originalResult.SearchedLocations);
                        locations.AddRange(result.SearchedLocations);
                        result = ViewEngineResult.NotFound(viewName, locations);
                    }
                    else
                    {
                        // GetView() searched locations but FindView() did not. Use first ViewEngineResult.
                        result = originalResult;
                    }
                }
            }

            return result;
        }

        private static string GetActionName(ActionContext context)
        {
            if (!context.RouteData.Values.TryGetValue(ActionNameKey, out var routeValue))
            {
                return null;
            }

            var actionDescriptor = context.ActionDescriptor;
            string normalizedValue = null;
            if (actionDescriptor.RouteValues.TryGetValue(ActionNameKey, out var value) && !string.IsNullOrEmpty(value))
            {
                normalizedValue = value;
            }

            var stringRouteValue = Convert.ToString(routeValue, CultureInfo.InvariantCulture);
            if (string.Equals(normalizedValue, stringRouteValue, StringComparison.OrdinalIgnoreCase))
            {
                return normalizedValue;
            }

            return stringRouteValue;
        }

        #endregion

        #region Component

        public virtual async Task InvokeComponentAsync(
            ActionContext context,
            ViewComponentResult result,
            TextWriter writer)
        {
            Guard.NotNull(context, nameof(context));
            Guard.NotNull(result, nameof(result));
            Guard.NotNull(writer, nameof(writer));

            var viewData = result.ViewData ?? new ViewDataDictionary(_modelMetadataProvider, context.ModelState);
            var tempData = result.TempData ?? _tempDataDictionaryFactory.GetTempData(context.HttpContext);

            var viewContext = new ViewContext(
                context,
                NullView.Instance,
                viewData,
                tempData,
                writer,
                _htmlHelperOptions);

            // IViewComponentHelper is stateful, we want to make sure to retrieve it every time we need it.
            var viewComponentHelper = context.HttpContext.RequestServices.GetRequiredService<IViewComponentHelper>();
            (viewComponentHelper as IViewContextAware)?.Contextualize(viewContext);
            var viewComponentResult = await GetViewComponentResult(result, viewComponentHelper);

            viewComponentResult.WriteTo(viewContext.Writer, _htmlEncoder);
        }

        private static Task<IHtmlContent> GetViewComponentResult(ViewComponentResult result, IViewComponentHelper viewComponentHelper)
        {
            if (result.ViewComponentType == null && result.ViewComponentName == null)
            {
                throw new InvalidOperationException("View component name or type must be set.");
            }
            else if (result.ViewComponentType == null)
            {
                return viewComponentHelper.InvokeAsync(result.ViewComponentName, result.Arguments);
            }
            else
            {
                return viewComponentHelper.InvokeAsync(result.ViewComponentType, result.Arguments);
            }
        }

        #endregion
    }
}
