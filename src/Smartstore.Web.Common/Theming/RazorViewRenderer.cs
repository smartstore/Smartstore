using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;

namespace Smartstore.Web.Common.Theming
{
    public class RazorViewRenderer : IRazorViewRenderer
    {
        private readonly IRazorViewEngine _viewEngine;
        private readonly ITempDataProvider _tempDataProvider;
        private readonly IServiceProvider _serviceProvider;

        public RazorViewRenderer(
            IRazorViewEngine viewEngine,
            ITempDataProvider tempDataProvider,
            IServiceProvider serviceProvider)
        {
            _viewEngine = viewEngine;
            _tempDataProvider = tempDataProvider;
            _serviceProvider = serviceProvider;
        }

        public async Task<string> RenderViewToStringAsync<TModel>(string viewName, TModel model, bool isMainPage = false)
        {
            var actionContext = GetActionContext();
            var view = FindView(actionContext, viewName, isMainPage);
            var viewData = new ViewDataDictionary<TModel>(new EmptyModelMetadataProvider(), new ModelStateDictionary())
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
                    new HtmlHelperOptions()
                );
                await view.RenderAsync(viewContext);
                return output.ToString();
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

        private ActionContext GetActionContext()
        {
            var httpContext = new DefaultHttpContext
            {
                RequestServices = _serviceProvider
            };

            return new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        }
    }
}
