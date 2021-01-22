using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;

namespace Smartstore.Web.UI
{
    public class ComponentWidgetInvoker : WidgetInvoker
    {
        private readonly Type _componentType;
        private readonly object _arguments;

        public ComponentWidgetInvoker(Type componentType, object arguments)
        {
            Guard.NotNull(componentType, nameof(componentType));

            _componentType = componentType;
            _arguments = arguments;
        }

        public override Task<IHtmlContent> InvokeAsync(ViewContext viewContext)
        {
            var helper = CreateViewComponentHelper(viewContext);
            return helper.InvokeAsync(_componentType, _arguments);
        }

        private static IViewComponentHelper CreateViewComponentHelper(ViewContext viewContext)
        {
            var viewComponentHelper = viewContext.HttpContext.RequestServices.GetService<IViewComponentHelper>();
            if (viewComponentHelper is IViewContextAware viewContextAware)
            {
                viewContextAware.Contextualize(viewContext);
            }

            return viewComponentHelper;
        }
    }
}
