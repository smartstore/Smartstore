#nullable enable

using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;

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
        /// <summary>
        /// Creates a new instance of <see cref="ComponentWidget"/>. 
        /// </summary>
        /// <param name="componentName">Name of component to invoke.</param>
        /// <param name="arguments">Arguments to pass to renderer.</param>
        public ComponentWidget(string componentName, object? arguments)
            : this(componentName, null, arguments)
        {
        }

        /// <inheritdoc/>
        /// <param name="module">Module system name in which the view component is located.</param>
        public ComponentWidget(string componentName, string? module, object? arguments)
        {
            Guard.NotEmpty(componentName, nameof(componentName));

            ComponentName = componentName;
            Module = module;
            Arguments = arguments;
        }

        /// <inheritdoc/>
        /// <param name="componentType">Type of component to invoke.</param>
        public ComponentWidget(Type componentType)
            : this(componentType, null)
        {
        }

        /// <inheritdoc/>
        /// <param name="arguments">Arguments to pass to renderer.</param>
        public ComponentWidget(Type componentType, object? arguments)
        {
            ComponentType = Guard.NotNull(componentType, nameof(componentType));
            Arguments = arguments;
        }

        public string ComponentName { get; } = default!;
        public Type ComponentType { get; } = default!;
        public string? Module { get; set; }
        public object? Arguments { get; set; }

        public override Task<IHtmlContent> InvokeAsync(WidgetContext context)
        {
            var invoker = context.HttpContext.RequestServices.GetRequiredService<IWidgetInvoker<ComponentWidget>>();
            return invoker.InvokeAsync(context, this);
        }
    }
}
