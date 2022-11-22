#nullable enable

using Microsoft.AspNetCore.Html;

namespace Smartstore.Core.Widgets
{
    public class PartialViewWidget : Widget
    {
        /// <summary>
        /// Creates a new instance of <see cref="PartialViewWidget"/>.
        /// </summary>
        /// <param name="viewName">Name of partial view to invoke.</param>
        /// <param name="module">Optional: system name of a module to additionally search for view files in.</param>
        public PartialViewWidget(string viewName, string? module = null)
        {
            Guard.NotEmpty(viewName, nameof(viewName));

            ViewName = viewName;
            Module = module;
        }

        /// <summary>
        /// Creates a new instance of <see cref="PartialViewWidget"/>.
        /// </summary>
        /// <param name="viewName">Name of view to invoke.</param>
        /// <param name="model">Model instance to pass to partial view.</param>
        /// <param name="module">Optional: system name of a module to additionally search for view files in.</param>
        public PartialViewWidget(string viewName, object? model, string? module = null)
        {
            Guard.NotEmpty(viewName, nameof(viewName));

            ViewName = viewName;
            Module = module;
            Model = model;
        }

        /// <summary>
        /// Gets the name of the view to render.
        /// </summary>
        public string ViewName { get; }

        /// <summary>
        /// Gets the module system name in which the view is located.
        /// </summary>
        public string? Module { get; }

        /// <summary>
        /// Gets the view model instance to pass.
        /// </summary>
        public object? Model { get; }

        /// <summary>
        /// Set this to <c>true</c> to render a full main page instead of a partial.
        /// </summary>
        public bool IsMainPage { get; set; }

        ///// <summary>
        ///// View to lookup if the view specified by <see cref="ViewName"/> cannot be located.
        ///// </summary>
        //public string? FallbackName { get; set; }

        public override Task<IHtmlContent> InvokeAsync(WidgetContext context)
        {
            var invoker = context.HttpContext.RequestServices.GetRequiredService<IWidgetInvoker<PartialViewWidget>>();
            return invoker.InvokeAsync(context, this);
        }
    }
}
