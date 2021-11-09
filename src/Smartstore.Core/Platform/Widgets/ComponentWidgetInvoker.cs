using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace Smartstore.Core.Widgets
{
    public class ComponentWidgetInvoker : WidgetInvoker
    {
        private readonly string _componentName;
        private readonly string _module;
        private readonly Type _componentType;
        private readonly object _arguments;

        public ComponentWidgetInvoker(string componentName, object arguments)
            : this(componentName, null, arguments)
        {
        }

        public ComponentWidgetInvoker(string componentName, string module, object arguments)
        {
            Guard.NotEmpty(componentName, nameof(componentName));

            _componentName = componentName;
            _module = module;
            _arguments = arguments;
        }

        public ComponentWidgetInvoker(Type componentType)
            : this(componentType, null)
        {
        }

        public ComponentWidgetInvoker(Type componentType, object arguments)
        {
            _componentType = Guard.NotNull(componentType, nameof(componentType));
            _arguments = arguments;
        }

        public override Task<IHtmlContent> InvokeAsync(ViewContext viewContext)
            => InvokeAsync(viewContext, null);

        public override async Task<IHtmlContent> InvokeAsync(ViewContext viewContext, object model)
        {
            var viewInvoker = viewContext.HttpContext.RequestServices.GetService<IViewInvoker>();
            return _componentType != null
                ? await viewInvoker.InvokeComponentAsync(_componentType, viewContext.ViewData, model ?? _arguments)
                : await viewInvoker.InvokeComponentAsync(_componentName, _module, viewContext.ViewData, model ?? _arguments);
        }
    }
}
