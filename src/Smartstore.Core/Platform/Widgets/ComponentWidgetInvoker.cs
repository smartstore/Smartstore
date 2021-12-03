using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace Smartstore.Core.Widgets
{
    public class ComponentWidgetInvoker : WidgetInvoker
    {
        public ComponentWidgetInvoker(string componentName, object arguments)
            : this(componentName, null, arguments)
        {
        }

        public ComponentWidgetInvoker(string componentName, string module, object arguments)
        {
            Guard.NotEmpty(componentName, nameof(componentName));

            ComponentName = componentName;
            Module = module;
            Arguments = arguments;
        }

        public ComponentWidgetInvoker(Type componentType)
            : this(componentType, null)
        {
        }

        public ComponentWidgetInvoker(Type componentType, object arguments)
        {
            ComponentType = Guard.NotNull(componentType, nameof(componentType));
            Arguments = arguments;
        }

        public string ComponentName { get; }
        public string Module { get; }
        public Type ComponentType { get; }
        public object Arguments { get; }

        public override Task<IHtmlContent> InvokeAsync(ViewContext viewContext)
            => InvokeAsync(viewContext, null);

        public override async Task<IHtmlContent> InvokeAsync(ViewContext viewContext, object model)
        {
            var viewInvoker = viewContext.HttpContext.RequestServices.GetService<IViewInvoker>();
            return ComponentType != null
                ? await viewInvoker.InvokeComponentAsync(ComponentType, viewContext.ViewData, model ?? Arguments)
                : await viewInvoker.InvokeComponentAsync(ComponentName, Module, viewContext.ViewData, model ?? Arguments);
        }
    }
}
