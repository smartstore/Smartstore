#nullable enable

using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Smartstore.Core.Widgets
{
    public class ComponentWidget<T> : ComponentWidget
        where T : ViewComponent
    {
        public ComponentWidget()
            : base(typeof(T), null)
        {
        }

        public ComponentWidget(object arguments)
            : base(typeof(T), arguments)
        {
        }
    }

    public class ComponentWidget : Widget
    {
        public ComponentWidget(string componentName, object arguments)
            : this(componentName, null, arguments)
        {
        }

        public ComponentWidget(string componentName, string? module, object? arguments)
        {
            Guard.NotEmpty(componentName, nameof(componentName));

            ComponentName = componentName;
            Module = module;
            Arguments = arguments;
        }

        public ComponentWidget(Type componentType)
            : this(componentType, null)
        {
        }

        public ComponentWidget(Type componentType, object? arguments)
        {
            ComponentType = Guard.NotNull(componentType, nameof(componentType));
            Arguments = arguments;
        }

        public string ComponentName { get; } = default!;
        public Type ComponentType { get; } = default!;
        public string? Module { get; }
        public object? Arguments { get; set; }

        public override async Task<IHtmlContent> InvokeAsync(ViewContext viewContext, object? model)
        {
            var viewInvoker = viewContext.HttpContext.RequestServices.GetService<IViewInvoker>();
            return ComponentType != null
                ? await viewInvoker.InvokeComponentAsync(ComponentType, viewContext.ViewData, model ?? Arguments)
                : await viewInvoker.InvokeComponentAsync(ComponentName, Module, viewContext.ViewData, model ?? Arguments);
        }

        public override Task<IHtmlContent> Invoke2Async(WidgetContext context)
        {
            var invoker = context.HttpContext.RequestServices.GetRequiredService<IWidgetInvoker<ComponentWidget>>();
            return invoker.InvokeAsync(context, this);
        }
    }
}
