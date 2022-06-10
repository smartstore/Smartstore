using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Smartstore.Core.Web;
using Smartstore.Engine.Modularity;

namespace Smartstore.Web.Razor
{
    public class DefaultViewInvoker : IViewInvoker
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ITempDataDictionaryFactory _tempDataFactory;
        private readonly IViewDataAccessor _viewDataAccessor;
        private readonly IActionContextAccessor _actionContextAccessor;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IModuleCatalog _moduleCatalog;

        public DefaultViewInvoker(
            IServiceProvider serviceProvider,
            ITempDataDictionaryFactory tempDataFactory,
            IViewDataAccessor viewDataAccessor,
            IActionContextAccessor actionContextAccessor, 
            IHttpContextAccessor httpContextAccessor,
            IModuleCatalog moduleCatalog)
        {
            _serviceProvider = serviceProvider;
            _tempDataFactory = tempDataFactory;
            _viewDataAccessor = viewDataAccessor;
            _actionContextAccessor = actionContextAccessor;
            _httpContextAccessor = httpContextAccessor;
            _moduleCatalog = moduleCatalog;
        }

        public ViewDataDictionary ViewData
        {
            get => _viewDataAccessor.ViewData;
        }

        #region Invoke*

        public Task<HtmlString> InvokeViewAsync(string viewName, string module, ViewDataDictionary viewData)
        {
            Guard.NotEmpty(viewName, nameof(viewName));

            var actionContext = GetActionContext(module);
            var result = new ViewResult
            {
                ViewName = viewName,
                ViewData = viewData ?? ViewData,
                TempData = _tempDataFactory.GetTempData(actionContext.HttpContext)
            };

            return ExecuteResultCapturedAsync(actionContext, result);
        }

        public Task<HtmlString> InvokePartialViewAsync(string viewName, string module, ViewDataDictionary viewData)
        {
            Guard.NotEmpty(viewName, nameof(viewName));

            var actionContext = GetActionContext(module);
            var result = new PartialViewResult
            {
                ViewName = viewName,
                ViewData = viewData ?? ViewData,
                TempData = _tempDataFactory.GetTempData(actionContext.HttpContext)
            };

            return ExecuteResultCapturedAsync(actionContext, result);
        }

        public Task<HtmlString> InvokeComponentAsync(string componentName, string module, ViewDataDictionary viewData, object arguments)
        {
            Guard.NotEmpty(componentName, nameof(componentName));

            var actionContext = GetActionContext(module);
            viewData ??= ViewData;
            var result = new ViewComponentResult
            {
                ViewComponentName = componentName,
                Arguments = arguments ?? viewData.Model,
                ViewData = viewData,
                TempData = _tempDataFactory.GetTempData(actionContext.HttpContext)
            };

            return ExecuteResultCapturedAsync(actionContext, result);
        }

        public Task<HtmlString> InvokeComponentAsync(Type componentType, ViewDataDictionary viewData, object arguments)
        {
            Guard.NotNull(componentType, nameof(componentType));

            var actionContext = GetActionContext(ResolveModule(componentType));
            viewData ??= ViewData;
            var result = new ViewComponentResult
            {
                ViewComponentType = componentType,
                Arguments = arguments ?? viewData.Model,
                ViewData = viewData,
                TempData = _tempDataFactory.GetTempData(actionContext.HttpContext)
            };

            return ExecuteResultCapturedAsync(actionContext, result);
        }

        #endregion

        protected virtual async Task<HtmlString> ExecuteResultCapturedAsync(ActionContext actionContext, IActionResult result)
        {
            var response = actionContext.HttpContext.Response;
            var body = response.Body;
            var statusCode = response.StatusCode;
            var contentType = response.ContentType;

            try
            {
                using var stream = new MemoryStream();
                response.Body = stream;
                await result.ExecuteResultAsync(actionContext);

                stream.Position = 0;
                using var reader = stream.ToStreamReader(true);
                var html = await reader.ReadToEndAsync();

                return new HtmlString(html);
            }
            finally
            {
                response.Body = body;
                response.ContentType = contentType;
                response.StatusCode = statusCode;
            }
        } 

        private ActionContext GetActionContext(string module)
        {
            var context = _actionContextAccessor.ActionContext;

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

        private string ResolveModule(Type componentType)
        {
            var moduleDescriptor = _moduleCatalog.GetModuleByAssembly(componentType.Assembly);
            return moduleDescriptor?.SystemName;
        }
    }
}
