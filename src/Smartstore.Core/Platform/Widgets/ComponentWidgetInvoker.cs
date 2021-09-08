using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;

namespace Smartstore.Core.Widgets
{
    public class ComponentWidgetInvoker : WidgetInvoker
    {
        private readonly string _componentName;
        private readonly Type _componentType;
        private readonly object _arguments;

        public ComponentWidgetInvoker(string componentName, object arguments)
        {
            Guard.NotEmpty(componentName, nameof(componentName));

            _componentName = componentName;
            _arguments = arguments;
        }

        public ComponentWidgetInvoker(Type componentType, object arguments)
        {
            Guard.NotNull(componentType, nameof(componentType));

            _componentType = componentType;
            _arguments = arguments;
        }

        public override Task<IHtmlContent> InvokeAsync(ViewContext viewContext)
            => InvokeAsync(viewContext, null);

        public override Task<IHtmlContent> InvokeAsync(ViewContext viewContext, object model)
        {
            var helper = CreateViewComponentHelper(viewContext);
            return _componentType != null
                ? helper.InvokeAsync(_componentType, model ?? _arguments)
                : helper.InvokeAsync(_componentName, model ?? _arguments);
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
