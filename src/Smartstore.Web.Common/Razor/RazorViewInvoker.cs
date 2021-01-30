using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Smartstore.Web.Razor
{
    public class RazorViewInvoker : IRazorViewInvoker
    {
        private readonly IRazorViewEngine _viewEngine;
        private readonly ITempDataProvider _tempDataProvider;
        private readonly IActionContextAccessor _actionContextAccessor;
        private readonly IViewComponentHelper _viewComponentHelper;
        private readonly IModelMetadataProvider _metadataProvider;
        private readonly IOptions<MvcViewOptions> _mvcViewOptions;

        public RazorViewInvoker(
            IRazorViewEngine viewEngine,
            ITempDataProvider tempDataProvider,
            IActionContextAccessor actionContextAccessor,
            IViewComponentHelper viewComponentHelper,
            IModelMetadataProvider metadataProvider,
            IOptions<MvcViewOptions> mvcViewOptions)
        {
            _viewEngine = viewEngine;
            _tempDataProvider = tempDataProvider;
            _actionContextAccessor = actionContextAccessor;
            _viewComponentHelper = viewComponentHelper;
            _metadataProvider = metadataProvider;
            _mvcViewOptions = mvcViewOptions;
        }

        public async Task<IHtmlContent> InvokeViewAsync<TModel>(string viewName, TModel model, bool isMainPage = false)
        {
            Guard.NotNull(viewName, nameof(viewName));
            
            var actionContext = _actionContextAccessor.ActionContext ?? throw new InvalidOperationException("Could not resolve a current ActionContext.");
            var view = FindView(actionContext, viewName, isMainPage);
            var viewData = new ViewDataDictionary<TModel>(_metadataProvider, actionContext.ModelState)
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
                return new HtmlString(output.ToString());
            }
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

        public Task<IHtmlContent> InvokeViewComponentAsync(ViewDataDictionary viewData, string componentName, object arguments)
        {
            Guard.NotEmpty(componentName, nameof(componentName));

            var actionContext = _actionContextAccessor.ActionContext ?? throw new InvalidOperationException("Could not resolve a current ActionContext.");
            var selector = actionContext.HttpContext.RequestServices.GetRequiredService<IViewComponentSelector>();

            var descriptor = selector.SelectComponent(componentName)
                ?? throw new InvalidOperationException($"Could not resolve a component type for '{componentName}'.");

            return InvokeViewComponentAsync(viewData, descriptor.TypeInfo.AsType(), arguments);
        }

        public async Task<IHtmlContent> InvokeViewComponentAsync(ViewDataDictionary viewData, Type componentType, object arguments)
        {
            Guard.NotNull(viewData, nameof(viewData));
            Guard.NotNull(componentType, nameof(componentType));

            var actionContext = _actionContextAccessor.ActionContext ?? throw new InvalidOperationException("Could not resolve a current ActionContext.");

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

                await _viewComponentHelper.InvokeAsync(componentType, arguments);
                return new HtmlString(output.ToString());
            }
        }
    }
}
